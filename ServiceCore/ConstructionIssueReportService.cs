using System.Net;
using System.Net.Mail;
using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ServiceCore.Issues;

public class ConstructionIssueReportService : IConstructionIssueReportService
{
    private readonly cpmRunningContext _db;
    private readonly IConstructionIssueService _issueService;
    private readonly IConfiguration _configuration;

    public ConstructionIssueReportService(cpmRunningContext db, IConstructionIssueService issueService, IConfiguration configuration)
    {
        _db = db;
        _issueService = issueService;
        _configuration = configuration;
    }

    public async Task<ConstructionIssueReport> CreateReportEntity(int projectId, int reportType, int responsiblePartyType, int? responsiblePartyId, string? responsibleOtherName, string? responsibleOtherEmail, List<int> issueIds, string? userId)
    {
        var report = new ConstructionIssueReport
        {
            ProjectId = projectId,
            ReportType = reportType,
            ResponsiblePartyType = responsiblePartyType,
            ResponsiblePartyId = responsiblePartyId,
            ResponsibleOtherName = responsibleOtherName,
            ResponsibleOtherEmail = responsibleOtherEmail,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };
        _db.ConstructionIssueReport.Add(report);
        await _db.SaveChangesAsync();

        foreach (var issueId in issueIds.Distinct())
        {
            _db.ConstructionIssueReportItem.Add(new ConstructionIssueReportItem
            {
                ReportId = report.Id,
                IssueId = issueId
            });
        }
        await _db.SaveChangesAsync();
        return report;
    }

    public async Task<byte[]> GenerateReportPdf(int projectId, int reportId)
    {
        var report = await _db.ConstructionIssueReport.FirstAsync(x => x.Id == reportId && x.ProjectId == projectId);
        var items = await _db.ConstructionIssueReportItem
            .Where(x => x.ReportId == reportId)
            .Select(x => x.IssueId)
            .ToListAsync();
        var issues = await _db.ConstructionIssue
            .Include(x => x.Unit)
            .Include(x => x.Category)
            .Include(x => x.ConstructionIssueMedia)
            .Where(x => x.ProjectId == projectId && items.Contains(x.Id))
            .OrderBy(x => x.Status)
            .ThenBy(x => x.DueDate)
            .ToListAsync();
        var projectName = await _db.Project.Where(x => x.ProjectId == projectId).Select(x => x.ProjectName).FirstOrDefaultAsync() ?? $"Project {projectId}";

        var enableQr = _configuration.GetValue<bool>("Features:EnableQRCode");
        var now = DateTime.Now;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Column(col =>
                {
                    col.Item().Text($"Puntenlijst - {projectName}").FontSize(18).SemiBold();
                    col.Item().Text($"Verantwoordelijke: {GetResponsibleDisplay(report)}");
                    col.Item().Text($"Datum: {now:dd/MM/yyyy HH:mm} | Type: {(ConstructionIssueReportType)report.ReportType}");
                });
                page.Content().Column(col =>
                {
                    var openCount = issues.Count(x => x.Status != (int)ConstructionIssueStatus.Closed);
                    var overdueCount = issues.Count(x => x.DueDate.HasValue && x.DueDate < DateOnly.FromDateTime(DateTime.Today) && x.Status != (int)ConstructionIssueStatus.Closed);
                    col.Item().PaddingBottom(8).Text($"Open: {openCount} | Vervallen: {overdueCount}");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(def =>
                        {
                            def.ConstantColumn(25);
                            def.RelativeColumn(1.2f);
                            def.RelativeColumn(1.2f);
                            def.RelativeColumn(1.5f);
                            def.RelativeColumn(2.5f);
                            def.RelativeColumn(1);
                            def.RelativeColumn(1);
                            def.RelativeColumn(1.1f);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Nr").SemiBold();
                            h.Cell().Text("Unit").SemiBold();
                            h.Cell().Text("Categorie").SemiBold();
                            h.Cell().Text("Locatie").SemiBold();
                            h.Cell().Text("Beschrijving").SemiBold();
                            h.Cell().Text("Prioriteit").SemiBold();
                            h.Cell().Text("Vervaldatum").SemiBold();
                            h.Cell().Text("Status").SemiBold();
                        });

                        var nr = 1;
                        foreach (var issue in issues)
                        {
                            table.Cell().Text((nr++).ToString());
                            table.Cell().Text(issue.Unit?.Name ?? "-");
                            table.Cell().Text(issue.Category?.Name ?? "-");
                            table.Cell().Text(issue.LocationText ?? "-");
                            table.Cell().Text(issue.Description ?? "-");
                            table.Cell().Text(((ConstructionIssuePriority)issue.Priority).ToString());
                            table.Cell().Text(issue.DueDate?.ToString("dd/MM/yyyy") ?? "-");
                            table.Cell().Text(((ConstructionIssueStatus)issue.Status).ToString());
                        }
                    });

                    col.Item().PaddingTop(8).Text($"Bijlagen: {issues.Sum(x => x.ConstructionIssueMedia?.Count ?? 0)} totaal");

                    if (enableQr)
                    {
                        col.Item().PaddingTop(10).Border(1).Padding(8).Text("QR placeholder (feature flag EnableQRCode=true)");
                    }
                });
                page.Footer().AlignRight().Text(x => x.Span("Group LN Puntenlijst"));
            });
        }).GeneratePdf();
    }

    public async Task<int> SendSelectedIssues(int projectId, ConstructionIssueSendRequestBO request, string? userId, string? comment = null)
    {
        var ids = request.IssueIds?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any()) return 0;

        var issues = await _db.ConstructionIssue
            .Where(x => x.ProjectId == projectId && ids.Contains(x.Id))
            .ToListAsync();

        var groups = request.GroupByResponsible
            ? issues.GroupBy(x => new { x.ResponsiblePartyType, x.ResponsiblePartyId, x.ResponsibleOtherName, x.ResponsibleOtherEmail })
            : new[] { issues.GroupBy(x => new { ResponsiblePartyType = (int)ConstructionIssueResponsiblePartyType.Contractor, ResponsiblePartyId = (int?)null, ResponsibleOtherName = string.Empty, ResponsibleOtherEmail = string.Empty }).First() };

        var sentCount = 0;
        foreach (var g in groups)
        {
            var issueIds = g.Select(x => x.Id).ToList();
            var email = await ResolveRecipientEmail(g.Key.ResponsiblePartyType, g.Key.ResponsiblePartyId, g.Key.ResponsibleOtherEmail);
            if (string.IsNullOrWhiteSpace(email))
                continue;

            var report = await CreateReportEntity(projectId, request.ReportType, g.Key.ResponsiblePartyType, g.Key.ResponsiblePartyId, g.Key.ResponsibleOtherName, g.Key.ResponsibleOtherEmail, issueIds, userId);
            var pdf = await GenerateReportPdf(projectId, report.Id);
            await SendReportEmail(projectId, report.Id, email, pdf, comment);

            var sentDate = DateTime.UtcNow;
            foreach (var issue in g)
            {
                issue.LastSentDate = sentDate;
                _db.ConstructionIssueNotification.Add(new ConstructionIssueNotification
                {
                    IssueId = issue.Id,
                    RecipientType = g.Key.ResponsiblePartyType,
                    RecipientId = g.Key.ResponsiblePartyId,
                    RecipientEmail = email,
                    SentDate = sentDate,
                    SentByUserId = userId,
                    Channel = (int)ConstructionIssueNotificationChannel.Email,
                    ReportId = report.Id,
                    IsReminder = false
                });
                await _issueService.AddHistory(issue.Id, (int)ConstructionIssueHistoryAction.SentToResponsible, userId, null, null, $"Puntenlijst verzonden naar {email}");
                sentCount++;
            }
        }

        await _db.SaveChangesAsync();
        return sentCount;
    }

    public async Task<int> SendReminder(int projectId, List<int> issueIds, string? userId)
    {
        var request = new ConstructionIssueSendRequestBO { IssueIds = issueIds, GroupByResponsible = true, ReportType = (int)ConstructionIssueReportType.SiteInspection };
        var ids = request.IssueIds?.Distinct().ToList() ?? new List<int>();
        var issues = await _db.ConstructionIssue.Where(x => x.ProjectId == projectId && ids.Contains(x.Id)).ToListAsync();
        var sentDate = DateTime.UtcNow;
        var count = 0;

        foreach (var issue in issues)
        {
            var email = await ResolveRecipientEmail(issue.ResponsiblePartyType, issue.ResponsiblePartyId, issue.ResponsibleOtherEmail);
            if (string.IsNullOrWhiteSpace(email)) continue;

            var report = await CreateReportEntity(projectId, request.ReportType, issue.ResponsiblePartyType, issue.ResponsiblePartyId, issue.ResponsibleOtherName, issue.ResponsibleOtherEmail, new List<int> { issue.Id }, userId);
            var pdf = await GenerateReportPdf(projectId, report.Id);
            await SendReportEmail(projectId, report.Id, email, pdf, null);

            issue.LastSentDate = sentDate;
            _db.ConstructionIssueNotification.Add(new ConstructionIssueNotification
            {
                IssueId = issue.Id,
                RecipientType = issue.ResponsiblePartyType,
                RecipientId = issue.ResponsiblePartyId,
                RecipientEmail = email,
                SentDate = sentDate,
                SentByUserId = userId,
                Channel = (int)ConstructionIssueNotificationChannel.Email,
                ReportId = report.Id,
                IsReminder = true
            });
            await _issueService.AddHistory(issue.Id, (int)ConstructionIssueHistoryAction.ReminderSent, userId, null, null, $"Herinnering verzonden naar {email}");
            count++;
        }

        await _db.SaveChangesAsync();
        return count;
    }

    private async Task SendReportEmail(int projectId, int reportId, string toEmail, byte[] pdfBytes, string? comment = null)
    {
        var projectName = await _db.Project.Where(x => x.ProjectId == projectId).Select(x => x.ProjectName).FirstOrDefaultAsync() ?? $"Project {projectId}";
        var subject = $"[{projectName}] Puntenlijst – {DateTime.Now:yyyy-MM-dd}";
        var commentBlock = string.IsNullOrWhiteSpace(comment) ? string.Empty : $"<p><strong>Opmerking:</strong><br/>{System.Net.WebUtility.HtmlEncode(comment)}</p>";
        var body = $"<p>Beste,</p><p>In bijlage vindt u de puntenlijst.</p>{commentBlock}<p>Bekijk details in CPMCore: /Projects/{projectId}/Issues</p>";

        var smtpUser = _configuration["EmailSettings:SmtpUser"] ?? throw new InvalidOperationException("EmailSettings:SmtpUser missing");
        var smtpPass = _configuration["EmailSettings:SmtpPass"] ?? throw new InvalidOperationException("EmailSettings:SmtpPass missing");

        using var message = new MailMessage
        {
            From = new MailAddress(smtpUser),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail));
        message.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), $"puntenlijst_{reportId}.pdf", "application/pdf"));

        using var client = new SmtpClient("smtp.office365.com", 587)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        await client.SendMailAsync(message);
    }

    private async Task<string?> ResolveRecipientEmail(int responsiblePartyType, int? responsiblePartyId, string? responsibleOtherEmail)
    {
        if (responsiblePartyId.HasValue)
        {
            var companyEmail = await _db.CompanyInfo
                .Where(x => x.CompanyId == responsiblePartyId.Value)
                .Select(x => x.Email)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(companyEmail))
                return companyEmail;
        }

        return string.IsNullOrWhiteSpace(responsibleOtherEmail) ? null : responsibleOtherEmail;
    }

    private static string GetResponsibleDisplay(ConstructionIssueReport report)
    {
        if (!string.IsNullOrWhiteSpace(report.ResponsibleOtherName))
            return report.ResponsibleOtherName;
        if (!string.IsNullOrWhiteSpace(report.ResponsibleOtherEmail))
            return report.ResponsibleOtherEmail;
        if (report.ResponsiblePartyId.HasValue)
            return $"Partij #{report.ResponsiblePartyId}";
        return "Onbekend";
    }
}