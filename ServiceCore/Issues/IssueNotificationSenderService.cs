using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ServiceCore.Issues;

public class IssueNotificationSenderService : IIssueNotificationSenderService
{
    private readonly cpmRunningContext _db;
    private readonly IConstructionIssueReportService _reportService;
    private readonly IConfiguration _configuration;
    private readonly IPortalInviteNotifier? _portalInviteNotifier;
    private readonly IResendInviteUrlBuilder? _resendUrlBuilder;

    // Statuses always included in reminder emails (regardless of planned date)
    private static readonly int[] ReminderStatuses = { 0, 3, 7 };
    // 0=Open, 3=WaitingInspection, 7=Reopened
    // Status 2=Gepland is included only when PlannedDate is in the past (see query below)

    // Statuses excluded from evening update emails
    private static readonly int[] EveningExcludeStatuses = { 2, 4, 6 };
    // 2=Gepland, 4=Resolved, 6=Rejected

    private const int StatusWaitingInspection = 3;
    private const int PriorityHigh = 2;
    private const int PriorityCritical = 3;

    public IssueNotificationSenderService(
        cpmRunningContext db,
        IConstructionIssueReportService reportService,
        IConfiguration configuration,
        IPortalInviteNotifier? portalInviteNotifier = null,
        IResendInviteUrlBuilder? resendUrlBuilder = null)
    {
        _db = db;
        _reportService = reportService;
        _configuration = configuration;
        _portalInviteNotifier = portalInviteNotifier;
        _resendUrlBuilder = resendUrlBuilder;
    }

    public async Task<int> SendNotificationsAsync(
        int? scheduleId = null,
        bool eveningOnly = false,
        int? filterSupplierId = null,
        int? filterProjectId = null)
    {
        // 1. Fetch all relevant open issues
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.ConstructionIssue
            .Where(x => !x.DoNotAutoNotify && (
                ReminderStatuses.Contains(x.Status) ||
                (x.Status == 2 && x.PlannedDate.HasValue && x.PlannedDate < today)  // Gepland maar vervallen
            ));

        if (filterProjectId.HasValue)
            query = query.Where(x => x.ProjectId == filterProjectId.Value);

        if (filterSupplierId.HasValue)
            query = query.Where(x => x.ResponsiblePartyId == filterSupplierId.Value);

        if (eveningOnly)
        {
            var todayDt = DateTime.UtcNow.Date;
            query = query
                .Where(x => !EveningExcludeStatuses.Contains(x.Status))
                .Where(x =>
                    x.CreatedDate.Date == todayDt ||
                    (x.LastUpdatedDate != null && x.LastUpdatedDate.Value.Date == todayDt));
        }

        var issues = await query
            .Where(x => x.ResponsiblePartyId != null)
            .ToListAsync();

        if (issues.Count == 0)
            return 0;

        // 2. Group by (aannemer, project) — one email per project per contractor
        var groups = issues
            .GroupBy(x => new { x.ResponsiblePartyType, x.ResponsiblePartyId, x.ProjectId })
            .ToList();

        // Prefetch all needed lookup data in one pass
        var allProjectIds  = groups.Select(g => g.Key.ProjectId).Distinct().ToList();
        var allCompanyIds  = groups.Select(g => g.Key.ResponsiblePartyId!.Value).Distinct().ToList();
        var allUnitIds     = issues.Where(x => x.UnitId.HasValue).Select(x => x.UnitId!.Value).Distinct().ToList();

        var projectMap = await _db.Project
            .Where(x => allProjectIds.Contains(x.ProjectId))
            .Select(x => new { x.ProjectId, x.ProjectName, x.AspNetUserId })
            .ToDictionaryAsync(x => x.ProjectId);

        var companyMap = await _db.CompanyInfo
            .Where(x => allCompanyIds.Contains(x.CompanyId))
            .Select(x => new { x.CompanyId, x.Email, x.BedrijfsNaam })
            .ToDictionaryAsync(x => x.CompanyId);

        // Contract site-manager contact emails: key = (projectId, companyId)
        // These take priority over CompanyInfo.Email so updates to the contact are reflected immediately
        var contractContactMap = await _db.Contract
            .Where(c => allProjectIds.Contains(c.ProjectId) && allCompanyIds.Contains(c.CompanyId) && c.SiteManagerContactId != null)
            .Select(c => new {
                c.ProjectId,
                c.CompanyId,
                c.SiteManagerContactId,
                c.SiteManagerContact!.Email,
                ContactFirstName = c.SiteManagerContact.ContactVoornaam ?? "",
                ContactLastName  = c.SiteManagerContact.ContactNaam ?? "",
                ContactName = (c.SiteManagerContact.ContactVoornaam + " " + c.SiteManagerContact.ContactNaam).Trim()
            })
            .ToListAsync();
        var contractContactLookup = contractContactMap
            .GroupBy(c => (c.ProjectId, c.CompanyId))
            .ToDictionary(g => g.Key, g => g.First());

        var managerUserIds = projectMap.Values
            .Where(x => x.AspNetUserId != null)
            .Select(x => x.AspNetUserId!)
            .Distinct()
            .ToList();

        var managerEmailMap = await _db.Users
            .Where(x => managerUserIds.Contains(x.UserId) && x.Email != null)
            .ToDictionaryAsync(x => x.UserId!, x => x.Email!);

        var unitNames = allUnitIds.Count > 0
            ? await _db.Units
                .Where(x => allUnitIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name)
            : new Dictionary<int, string>();

        var sentDate   = DateTime.UtcNow;
        var emailsSent = 0;

        foreach (var group in groups)
        {
            var groupIssues = group.ToList();

            // 3. Skip if ALL issues are WaitingInspection — aannemer hoeft niets te doen
            if (groupIssues.All(x => x.Status == StatusWaitingInspection))
                continue;

            // For reminders (non-evening): only send if there is at least 1 truly open issue
            if (!eveningOnly && !groupIssues.Any(x => x.Status == 0))
                continue;

            // 4. Resolve company + contact email
            if (!companyMap.TryGetValue(group.Key.ResponsiblePartyId!.Value, out var company))
                continue;

            var contractContact = contractContactLookup.TryGetValue((group.Key.ProjectId, group.Key.ResponsiblePartyId!.Value), out var cc) ? cc : null;
            var recipientEmail = !string.IsNullOrWhiteSpace(contractContact?.Email) ? contractContact!.Email : company.Email;
            var recipientName  = !string.IsNullOrWhiteSpace(contractContact?.ContactName) ? contractContact!.ContactName : company.BedrijfsNaam;

            if (string.IsNullOrWhiteSpace(recipientEmail))
                continue;

            // 5. Resolve project
            if (!projectMap.TryGetValue(group.Key.ProjectId, out var project))
                continue;
            var projectName = project.ProjectName ?? $"Project {group.Key.ProjectId}";

            // 6. Reply-to = werfleider van dit project; CC if different from recipient
            var replyTo = project.AspNetUserId != null
                && managerEmailMap.TryGetValue(project.AspNetUserId, out var mgrEmail)
                ? mgrEmail : null;

            var ccEmails = replyTo != null && replyTo != recipientEmail
                ? new List<string> { replyTo }
                : new List<string>();

            // 7. PDF rapport entity (WaitingInspection excluded) — no longer attached, served via portal download link
            var attachments      = new List<(string FileName, byte[] Content)>();
            var reportIdsByIssue = new Dictionary<int, int>();

            var pdfIssueIds = groupIssues
                .Where(x => x.Status != StatusWaitingInspection)
                .Select(x => x.Id)
                .ToList();

            string? pdfDownloadUrl = null;
            if (pdfIssueIds.Count > 0)
            {
                var report = await _reportService.CreateReportEntity(
                    group.Key.ProjectId,
                    (int)ConstructionIssueReportType.SiteInspection,
                    group.Key.ResponsiblePartyType,
                    group.Key.ResponsiblePartyId,
                    null, null,
                    pdfIssueIds,
                    null);

                foreach (var id in pdfIssueIds)
                    reportIdsByIssue[id] = report.Id;

                var portalBase = (_configuration["App:BaseUrl"] ?? "").TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(portalBase))
                    pdfDownloadUrl = $"{portalBase}/Werfportaal/Rapport/{report.Id}";
            }

            // 8. Subject + HTML
            var subject = eveningOnly
                ? $"Update werfpunten van vandaag – {projectName} – {sentDate:dd/MM/yyyy}"
                : $"Herinnering openstaande werfpunten – {projectName} – {sentDate:dd/MM/yyyy}";

            // null → builder generates the standard herinnering intro from projectName
            string? intro = eveningOnly
                ? "Hieronder vindt u de werfpunten die <strong>vandaag</strong> werden aangemaakt of gewijzigd."
                : null;

            var portalBaseUrl = (_configuration["App:BaseUrl"] ?? "").TrimEnd('/');
            var portalLoginUrl = string.IsNullOrWhiteSpace(portalBaseUrl) ? null : $"{portalBaseUrl}/Werfportaal";

            var resendInviteUrl = _resendUrlBuilder?.Build(
                recipientEmail,
                group.Key.ResponsiblePartyId!.Value,
                portalBaseUrl);

            var html = IssueEmailHtmlBuilder.BuildIssueTableEmail(
                recipientName,
                projectName,
                groupIssues,
                unitNames,
                intro,
                sentDate,
                portalLoginUrl: portalLoginUrl,
                pdfDownloadUrl: pdfDownloadUrl,
                werfleiderEmail: replyTo,
                resendInviteUrl: resendInviteUrl);

            // 9. Send (no PDF attachment — download link is in the email body)
            await SendAsync(recipientEmail, subject, html, attachments, ccEmails, replyTo);
            emailsSent++;

            // 9b. Portal uitnodiging voor de ontvanger — ook als nog geen interne gebruiker bestaat
            if (_portalInviteNotifier != null && !string.IsNullOrWhiteSpace(portalBaseUrl) && !string.IsNullOrWhiteSpace(recipientEmail))
            {
                try
                {
                    // Bepaal of de ontvanger een SiteManagerContact is (beperkt) of het bedrijf zelf (volledig)
                    bool isFullAdmin  = contractContact == null;
                    int? invContactId = contractContact != null ? contractContact.SiteManagerContactId : null;
                    string firstName  = contractContact != null ? contractContact.ContactFirstName : "";
                    string lastName   = contractContact != null ? contractContact.ContactLastName  : (recipientName ?? "");

                    await _portalInviteNotifier.EnsureRecipientInvitedAsync(
                        group.Key.ResponsiblePartyId!.Value,
                        recipientEmail,
                        firstName,
                        lastName,
                        isFullAdmin,
                        invContactId,
                        portalBaseUrl,
                        invitedByUserId: null);
                }
                catch { /* uitnodiging is best-effort, niet kritiek */ }
            }

            // 10. Persist notifications
            foreach (var issue in groupIssues)
            {
                issue.LastSentDate = sentDate;
                reportIdsByIssue.TryGetValue(issue.Id, out var reportId);

                _db.ConstructionIssueNotification.Add(new ConstructionIssueNotification
                {
                    IssueId           = issue.Id,
                    RecipientType     = group.Key.ResponsiblePartyType,
                    RecipientId       = group.Key.ResponsiblePartyId,
                    RecipientEmail    = recipientEmail,
                    SentDate          = sentDate,
                    SentByUserId      = null,
                    Channel           = (int)ConstructionIssueNotificationChannel.Email,
                    ReportId          = reportId == 0 ? null : reportId,
                    IsReminder        = true
                });
            }
        }

        await _db.SaveChangesAsync();
        return emailsSent;
    }

    public async Task SendTestEmailAsync(string toEmail)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fakeIssues = new List<ConstructionIssue>
        {
            new() { Id = 1, Title = "Buitendeur type B2 niet geplaatst",       Status = 0, Priority = 3, DueDate = today.AddDays(-3),  UnitId = null, RoomOrZone = "Inkomhal" },
            new() { Id = 2, Title = "Afwerking vloer veranda onvolledig",       Status = 0, Priority = 2, DueDate = today.AddDays(2),   UnitId = 1,    RoomOrZone = "Veranda" },
            new() { Id = 3, Title = "Stopcontacten achterkeuken ontbreken",     Status = 7, Priority = 1, DueDate = today.AddDays(5),   UnitId = 1,    RoomOrZone = "Keuken" },
            new() { Id = 4, Title = "Dakgoot rechts los",                       Status = 3, Priority = 1, DueDate = today.AddDays(10),  UnitId = null, RoomOrZone = null },
            new() { Id = 5, Title = "Schilderwerk garage: vlekken zichtbaar",   Status = 0, Priority = 1, DueDate = null,               UnitId = 2,    RoomOrZone = "Garage" },
        };

        var unitNames = new Dictionary<int, string> { { 1, "Lot 3A" }, { 2, "Lot 3B" } };

        var portalBase = (_configuration["App:BaseUrl"] ?? "https://portal.groupln.be").TrimEnd('/');

        var html = IssueEmailHtmlBuilder.BuildIssueTableEmail(
            recipientName: "Testgebruiker",
            projectName: "Residentie De Linde – TEST",
            issues: fakeIssues,
            unitNames: unitNames,
            intro: null,
            sentDate: DateTime.UtcNow,
            portalLoginUrl: $"{portalBase}/Werfportaal",
            pdfDownloadUrl: $"{portalBase}/Werfportaal/Rapport/999",
            werfleiderEmail: "werfleider@groupln.be",
            resendInviteUrl: $"{portalBase}/Uitnodiging/Hernieuw?e=aannemer%40voorbeeld.be&c=1&x=9999999999999&s=test");

        var subject = $"[TEST] Voorbeeld puntenlijst – {DateTime.UtcNow:dd/MM/yyyy HH:mm}";
        await SendAsync(toEmail, subject, html, new List<(string, byte[])>(), new List<string>());
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }

    // ──────────────────────────────────────────────────────────────
    // SMTP SEND  (same pattern as ConstructionIssueReportService)
    // ──────────────────────────────────────────────────────────────

    private async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        List<(string FileName, byte[] Content)> attachments,
        List<string> ccEmails,
        string? replyToEmail = null)
    {
        var smtpUser = _configuration["EmailSettings:SmtpUser"]
            ?? throw new InvalidOperationException("EmailSettings:SmtpUser is niet geconfigureerd.");
        var smtpPass = _configuration["EmailSettings:SmtpPass"]
            ?? throw new InvalidOperationException("EmailSettings:SmtpPass is niet geconfigureerd.");
        var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUser;

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, "Group LN – Werfpunten"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(toEmail));

        if (!string.IsNullOrWhiteSpace(replyToEmail))
            message.ReplyToList.Add(new MailAddress(replyToEmail));

        foreach (var cc in ccEmails)
            message.CC.Add(new MailAddress(cc));

        foreach (var (fileName, content) in attachments)
            message.Attachments.Add(new Attachment(new MemoryStream(content), fileName, "application/pdf"));

        using var client = new SmtpClient("smtp.office365.com", 587)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        await client.SendMailAsync(message);
    }
}
