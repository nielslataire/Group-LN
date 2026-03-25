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

    // Open statuses that trigger a notification
    private static readonly int[] OpenStatuses = { 0, 1, 2, 3, 7 };
    // 0=Open, 1=Assigned, 2=InProgress, 3=WaitingInspection, 7=Reopened

    private const int StatusWaitingInspection = 3;
    private const int PriorityHigh = 2;
    private const int PriorityCritical = 3;

    public IssueNotificationSenderService(
        cpmRunningContext db,
        IConstructionIssueReportService reportService,
        IConfiguration configuration)
    {
        _db = db;
        _reportService = reportService;
        _configuration = configuration;
    }

    public async Task<int> SendNotificationsAsync(
        int? scheduleId = null,
        bool eveningOnly = false,
        int? filterSupplierId = null,
        int? filterProjectId = null)
    {
        // 1. Fetch all relevant open issues
        var query = _db.ConstructionIssue
            .Where(x => OpenStatuses.Contains(x.Status) && !x.DoNotAutoNotify);

        if (filterProjectId.HasValue)
            query = query.Where(x => x.ProjectId == filterProjectId.Value);

        if (filterSupplierId.HasValue)
            query = query.Where(x => x.ResponsiblePartyId == filterSupplierId.Value);

        if (eveningOnly)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(x =>
                x.CreatedDate.Date == today ||
                (x.LastUpdatedDate != null && x.LastUpdatedDate.Value.Date == today));
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

            // 4. Resolve company
            if (!companyMap.TryGetValue(group.Key.ResponsiblePartyId!.Value, out var company))
                continue;
            if (string.IsNullOrWhiteSpace(company.Email))
                continue;

            // 5. Resolve project
            if (!projectMap.TryGetValue(group.Key.ProjectId, out var project))
                continue;
            var projectName = project.ProjectName ?? $"Project {group.Key.ProjectId}";

            // 6. Reply-to = werfleider van dit project; CC if different from recipient
            var replyTo = project.AspNetUserId != null
                && managerEmailMap.TryGetValue(project.AspNetUserId, out var mgrEmail)
                ? mgrEmail : null;

            var ccEmails = replyTo != null && replyTo != company.Email
                ? new List<string> { replyTo }
                : new List<string>();

            // 7. PDF for this project (WaitingInspection excluded)
            var attachments      = new List<(string FileName, byte[] Content)>();
            var reportIdsByIssue = new Dictionary<int, int>();

            var pdfIssueIds = groupIssues
                .Where(x => x.Status != StatusWaitingInspection)
                .Select(x => x.Id)
                .ToList();

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

                var pdfBytes = await _reportService.GenerateReportPdf(group.Key.ProjectId, report.Id);
                attachments.Add(($"Werfpunten_{SanitizeFileName(projectName)}_{sentDate:yyyyMMdd}.pdf", pdfBytes));

                foreach (var id in pdfIssueIds)
                    reportIdsByIssue[id] = report.Id;
            }

            // 8. Subject + HTML
            var subject = eveningOnly
                ? $"Update werfpunten van vandaag – {projectName} – {sentDate:dd/MM/yyyy}"
                : $"Herinnering openstaande werfpunten – {projectName} – {sentDate:dd/MM/yyyy}";

            var html = BuildEmailHtml(
                company.BedrijfsNaam,
                projectName,
                groupIssues,
                unitNames,
                eveningOnly,
                sentDate);

            // 9. Send
            await SendAsync(company.Email, subject, html, attachments, ccEmails, replyTo);
            emailsSent++;

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
                    RecipientEmail    = company.Email,
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

    // ──────────────────────────────────────────────────────────────
    // HTML EMAIL BUILDER
    // ──────────────────────────────────────────────────────────────

    private static string BuildEmailHtml(
        string contractorName,
        string projectName,
        IList<ConstructionIssue> issues,
        Dictionary<int, string> unitNames,
        bool isEveningUpdate,
        DateTime sentDate)
    {
        const string primary      = "#0a5a3b";
        const string primaryLight = "#d4ede4";

        var intro = isEveningUpdate
            ? "Hieronder vindt u de werfpunten die <strong>vandaag</strong> werden aangemaakt of gewijzigd."
            : "Hieronder vindt u een overzicht van uw <strong>openstaande werfpunten</strong>. Gelieve deze zo snel mogelijk te behandelen.";

        var issuesForEmail = issues
            .OrderBy(x => x.Priority == 3 ? 0 : x.Priority == 2 ? 1 : 2)
            .ThenBy(x => x.DueDate)
            .ToList();

        var sb = new StringBuilder();
        sb.Append($@"<!DOCTYPE html>
<html lang=""nl"">
<head>
  <meta charset=""utf-8""/>
  <meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
  <title>Werfpunten – Group LN</title>
</head>
<body style=""margin:0;padding:0;background:#f4f6f8;font-family:Arial,Helvetica,sans-serif;"">

<!-- HEADER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" bgcolor=""{primary}"">
  <tr>
    <td style=""padding:24px 32px;"">
      <div style=""font-size:22px;font-weight:bold;color:#ffffff;letter-spacing:1px;"">Group LN</div>
      <div style=""font-size:12px;color:{primaryLight};margin-top:4px;"">{WebUtility.HtmlEncode(projectName)} · {sentDate:dd MMMM yyyy}</div>
    </td>
  </tr>
</table>

<!-- WRAPPER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"">
  <tr>
    <td align=""center"" style=""padding:24px 16px;"">
      <table width=""620"" cellpadding=""0"" cellspacing=""0"" bgcolor=""#ffffff""
             style=""border-radius:8px;border:1px solid #dde3e9;"">
        <tr>
          <td style=""padding:28px 32px;"">

            <!-- GREETING -->
            <p style=""font-size:15px;color:#1f2937;margin:0 0 8px;"">Beste <strong>{WebUtility.HtmlEncode(contractorName)}</strong>,</p>
            <p style=""font-size:14px;color:#374151;margin:0 0 24px;line-height:1.6;"">{intro}</p>

            <!-- ISSUES TABLE -->
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:28px;"">
              <tr>
                <td style=""background:{primary};border-radius:6px 6px 0 0;padding:10px 14px;"">
                  <span style=""color:#ffffff;font-size:13px;font-weight:bold;"">{WebUtility.HtmlEncode(projectName)}</span>
                  <span style=""color:{primaryLight};font-size:11px;margin-left:8px;"">({issuesForEmail.Count} punt{(issuesForEmail.Count == 1 ? "" : "en")})</span>
                </td>
              </tr>
              <tr>
                <td style=""border:1px solid #dde3e9;border-top:none;border-radius:0 0 6px 6px;padding:0;"">
                  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;"">
                    <thead>
                      <tr style=""background:#f9fafb;"">
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;width:36px;"">#</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;"">Omschrijving</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;width:120px;"">Eenheid / Locatie</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;width:110px;"">Status</th>
                        <th style=""padding:8px 12px;text-align:left;font-size:11px;color:#6b7280;font-weight:600;border-bottom:1px solid #e5e7eb;width:90px;"">Deadline</th>
                      </tr>
                    </thead>
                    <tbody>
");

        var rowIdx = 1;
        foreach (var issue in issuesForEmail)
        {
            var rowBg         = rowIdx % 2 == 0 ? "#f9fafb" : "#ffffff";
            var statusLabel   = GetStatusLabel(issue.Status);
            var statusColor   = GetStatusColor(issue.Status);
            var deadline      = issue.DueDate.HasValue ? issue.DueDate.Value.ToString("dd/MM/yyyy") : "–";
            var priorityBadge = GetPriorityBadgeHtml(issue.Priority);
            var title         = WebUtility.HtmlEncode(issue.Title ?? "(zonder titel)");
            var unitName      = issue.UnitId.HasValue && unitNames.TryGetValue(issue.UnitId.Value, out var un) ? WebUtility.HtmlEncode(un) : null;
            var roomOrZone    = !string.IsNullOrWhiteSpace(issue.RoomOrZone) ? WebUtility.HtmlEncode(issue.RoomOrZone) : null;
            var locationCell  = BuildLocationCellHtml(unitName, roomOrZone);

            sb.Append($@"
                      <tr style=""background:{rowBg};"">
                        <td style=""padding:9px 12px;font-size:12px;color:#6b7280;border-bottom:1px solid #f3f4f6;"">{rowIdx}</td>
                        <td style=""padding:9px 12px;font-size:12px;color:#1f2937;border-bottom:1px solid #f3f4f6;"">{title}{priorityBadge}</td>
                        <td style=""padding:9px 12px;font-size:11px;color:#374151;border-bottom:1px solid #f3f4f6;"">{locationCell}</td>
                        <td style=""padding:9px 12px;border-bottom:1px solid #f3f4f6;"">
                          <span style=""background:{statusColor.Bg};color:{statusColor.Text};font-size:10px;font-weight:600;padding:3px 10px;border-radius:10px;white-space:nowrap;display:inline-block;"">{statusLabel}</span>
                        </td>
                        <td style=""padding:9px 12px;font-size:12px;color:#374151;border-bottom:1px solid #f3f4f6;"">{deadline}</td>
                      </tr>
");
            rowIdx++;
        }

        sb.Append($@"
                    </tbody>
                  </table>
                </td>
              </tr>
            </table>

            <!-- CLOSING -->
            <p style=""font-size:13px;color:#6b7280;margin:16px 0 0;line-height:1.6;"">
              De openstaande punten zijn ook beschikbaar als PDF-rapport in bijlage.<br/>
              Gelieve bij vragen contact op te nemen met uw projectleider.
            </p>

          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>

<!-- FOOTER -->
<table width=""100%"" cellpadding=""0"" cellspacing=""0"">
  <tr>
    <td align=""center"" style=""padding:16px;color:#9ca3af;font-size:11px;"">
      Group LN &nbsp;·&nbsp; Automatisch gegenereerd bericht &nbsp;·&nbsp; {sentDate:dd/MM/yyyy HH:mm} UTC
    </td>
  </tr>
</table>

</body>
</html>");

        return sb.ToString();
    }

    // ──────────────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────────────

    private static string BuildLocationCellHtml(string unitName, string roomOrZone)
    {
        if (unitName == null && roomOrZone == null)
            return "<span style=\"color:#9ca3af;\">–</span>";

        var sb = new StringBuilder();
        if (unitName != null)
            sb.Append($"<span style=\"font-weight:600;\">{unitName}</span>");
        if (roomOrZone != null)
        {
            if (unitName != null) sb.Append("<br/>");
            sb.Append($"<span style=\"color:#6b7280;\">{roomOrZone}</span>");
        }
        return sb.ToString();
    }

    private static string GetStatusLabel(int status) => status switch
    {
        0 => "Open",
        1 => "Toegewezen",
        2 => "In uitvoering",
        3 => "Wacht op keuring",
        7 => "Heropend",
        _ => "Open"
    };

    private static (string Bg, string Text) GetStatusColor(int status) => status switch
    {
        0 => ("#fee2e2", "#b91c1c"),   // red
        1 => ("#fef3c7", "#92400e"),   // amber
        2 => ("#dbeafe", "#1e40af"),   // blue
        3 => ("#ede9fe", "#5b21b6"),   // purple
        7 => ("#fce7f3", "#9d174d"),   // pink/dark red
        _ => ("#f3f4f6", "#374151")
    };

    private static string GetPriorityBadgeHtml(int priority) => priority switch
    {
        2 => " &nbsp;<span style=\"background:#fff3cd;color:#856404;font-size:9px;font-weight:700;padding:1px 5px;border-radius:8px;\">DRINGEND</span>",
        3 => " &nbsp;<span style=\"background:#fee2e2;color:#b91c1c;font-size:9px;font-weight:700;padding:1px 5px;border-radius:8px;\">KRITIEK</span>",
        _ => string.Empty
    };

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
