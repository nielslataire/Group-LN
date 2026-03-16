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
        var project = await _db.Project
            .Include(x => x.PostalCode)
            .Include(x => x.Status)
            .Include(x => x.AspNetUser)
            .Include(x => x.IssuerCompanyIdBuilderNavigation)
            .FirstOrDefaultAsync(x => x.ProjectId == projectId);

        var projectName = project?.ProjectName ?? $"Project {projectId}";
        var projectAddress = string.Join(" ", new[]
        {
            project?.Street,
            project?.Number,
            project?.PostalCode?.Postcode,
            project?.PostalCode?.Gemeente
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var projectStatus = project?.Status?.StatusName ?? "-";
        var siteManager = string.Join(" ", new[] { project?.AspNetUser?.Voornaam, project?.AspNetUser?.Familienaam }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        if (string.IsNullOrWhiteSpace(siteManager))
            siteManager = "-";

        var primaryColor = !string.IsNullOrWhiteSpace(project?.IssuerCompanyIdBuilderNavigation?.BrandPrimaryColor)
            ? project.IssuerCompanyIdBuilderNavigation.BrandPrimaryColor
            : "#01532d";

        var responsibleIds = issues
            .Where(x => x.ResponsiblePartyId.HasValue)
            .Select(x => x.ResponsiblePartyId!.Value)
            .Distinct()
            .ToList();
        var responsibleNames = await _db.CompanyInfo
            .Where(x => responsibleIds.Contains(x.CompanyId))
            .Select(x => new { x.CompanyId, x.BedrijfsNaam })
            .ToDictionaryAsync(x => x.CompanyId, x => x.BedrijfsNaam);

        var enableQr = _configuration.GetValue<bool>("Features:EnableQRCode");
        var now = DateTime.Now;
        var groupedByUnit = issues
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Unit?.Name) ? "Zonder eenheid" : x.Unit!.Name)
            .OrderBy(x => x.Key)
            .ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1f2937"));

                page.Header().Background(primaryColor).PaddingHorizontal(30).PaddingTop(22).PaddingBottom(20).Column(col =>
                {
                    col.Item().Text("PUNTENLIJST").FontSize(22).SemiBold().FontColor(Colors.White);
                    col.Item().PaddingTop(2).Text($"Gegenereerd: {now:dd/MM/yyyy} — {issues.Count} punt(en)").FontColor("#e6f4ee");
                });

                page.Content().PaddingTop(20).PaddingBottom(24).Column(col =>
                {
                    var openCount = issues.Count(x => x.Status != (int)ConstructionIssueStatus.Closed);
                    var overdueCount = issues.Count(x => x.DueDate.HasValue && x.DueDate < DateOnly.FromDateTime(DateTime.Today) && x.Status != (int)ConstructionIssueStatus.Closed);

                    col.Item().ShowOnce().PaddingHorizontal(30).Column(firstPage =>
                    {
                        firstPage.Item().Background("#e4efec").CornerRadius(8).Padding(14).Column(info =>
                        {
                            info.Item().Text(projectName).FontSize(13).SemiBold().FontColor("#15322b");
                            info.Item().PaddingTop(4).Text($"Adres: {(string.IsNullOrWhiteSpace(projectAddress) ? "-" : projectAddress)}  •  Werfleider: {siteManager}  •  Status: {projectStatus}")
                                .FontSize(9)
                                .FontColor("#24473d");
                        });

                        if (enableQr)
                        {
                            firstPage.Item().PaddingTop(8).Border(1).BorderColor("#d1d5db").CornerRadius(6).Padding(8)
                                .Text("QR placeholder (feature flag EnableQRCode=true)").FontSize(8).FontColor("#6b7280");
                        }
                    });

                    col.Item().ShowOnce().Height(24);

                    var issueNumber = 1;
                    foreach (var unitGroup in groupedByUnit)
                    {
                        var unitIssues = unitGroup.ToList();
                        if (!unitIssues.Any())
                            continue;

                        var firstIssue = unitIssues[0];

                        col.Item().PaddingHorizontal(30).PaddingTop(10).PaddingBottom(10).Column(unitCol =>
                        {
                            unitCol.Item().PreventPageBreak().Column(firstBlock =>
                            {
                                firstBlock.Item().Background(primaryColor).CornerRadius(6).PaddingVertical(7).PaddingHorizontal(10).Row(row =>
                                {
                                    row.RelativeItem().Text($"Eenheid: {unitGroup.Key}").FontColor(Colors.White).SemiBold();
                                    row.ConstantItem(80).AlignRight().Text($"{unitIssues.Count} punt(en)").FontColor(Colors.White).SemiBold();
                                });

                                firstBlock.Item().PaddingTop(10).Element(cardContainer => RenderIssueCard(cardContainer, firstIssue, issueNumber++, primaryColor, responsibleNames));
                            });

                            foreach (var issue in unitIssues.Skip(1))
                            {
                                unitCol.Item().PaddingTop(10).Element(cardContainer => RenderIssueCard(cardContainer, issue, issueNumber++, primaryColor, responsibleNames));
                            }
                        });

                    }
                });

                page.Footer().PaddingHorizontal(30).PaddingBottom(8).PaddingTop(8).Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(primaryColor);
                    footer.Item().PaddingTop(6).AlignRight().Text(text =>
                    {
                        text.Span("Pagina ").FontSize(8).FontColor("#6b7280");
                        text.CurrentPageNumber().FontSize(8).FontColor("#6b7280");
                        text.Span("/").FontSize(8).FontColor("#6b7280");
                        text.TotalPages().FontSize(8).FontColor("#6b7280");
                    });
                });
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

    private static void RenderIssueCard(IContainer container, ConstructionIssue issue, int badgeNumber, string primaryColor, IReadOnlyDictionary<int, string> responsibleNames)
    {
        var priority = GetPriorityPresentation(issue.Priority);
        var status = GetStatusPresentation(issue.Status);
        var responsible = GetResponsibleDisplay(issue, responsibleNames);
        var location = GetLocationDisplay(issue);

        container.Border(1).BorderColor("#d9dde2").CornerRadius(8).Padding(12).Column(card =>
        {
            card.Item().Row(row =>
            {
                row.RelativeItem().Row(head =>
                {
                    head.ConstantItem(42).Background(primaryColor).CornerRadius(4).PaddingVertical(4).AlignCenter()
                        .Text($"#{badgeNumber}").FontSize(9).SemiBold().FontColor(Colors.White);
                    head.RelativeItem().PaddingLeft(8).AlignMiddle().Text(issue.Title ?? "(zonder titel)").SemiBold().FontSize(11);
                });

                row.ConstantItem(74).Background(status.BackgroundColor).CornerRadius(4).PaddingVertical(4).AlignCenter()
                    .Text(status.Label).FontSize(8).SemiBold().FontColor(status.TextColor);
                if (priority.Show)
                {
                    row.ConstantItem(8).Text(string.Empty);
                    row.ConstantItem(74).Background(priority.Color).CornerRadius(4).PaddingVertical(4).AlignCenter()
                        .Text(priority.Label).FontSize(8).SemiBold().FontColor(Colors.White);
                }
            });

            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(location))
                metaParts.Add($"Kamer/Plaats: {location}");
            if (!string.IsNullOrWhiteSpace(responsible))
                metaParts.Add($"Verantw.: {responsible}");
            if (issue.DueDate.HasValue)
                metaParts.Add($"Deadline: {issue.DueDate.Value:dd/MM/yyyy}");

            if (metaParts.Any())
            {
                card.Item().PaddingTop(8).Text(string.Join("   •   ", metaParts))
                    .FontSize(8)
                    .FontColor("#6b7280");
            }

            card.Item().PaddingTop(6).PaddingBottom(10).Text(issue.Description ?? "-").FontSize(9);
        });
    }

    private static string GetLocationDisplay(ConstructionIssue issue)
    {
        var locationParts = new[] { issue.RoomOrZone, issue.LocationText }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return locationParts.Any() ? string.Join(" - ", locationParts) : string.Empty;
    }

    private static string GetResponsibleDisplay(ConstructionIssue issue, IReadOnlyDictionary<int, string> responsibleNames)
    {
        if (issue.ResponsiblePartyId.HasValue && responsibleNames.TryGetValue(issue.ResponsiblePartyId.Value, out var companyName) && !string.IsNullOrWhiteSpace(companyName))
            return companyName;
        if (!string.IsNullOrWhiteSpace(issue.ResponsibleOtherName))
            return issue.ResponsibleOtherName;
        if (!string.IsNullOrWhiteSpace(issue.ResponsibleOtherEmail))
            return issue.ResponsibleOtherEmail;
        return string.Empty;
    }

    private static (string Label, string Color, bool Show) GetPriorityPresentation(int priority)
    {
        return (ConstructionIssuePriority)priority switch
        {
            ConstructionIssuePriority.High => ("Hoog", "#dc3545", true),
            _ => (string.Empty, string.Empty, false)
        };
    }

    private static (string Label, string BackgroundColor, string TextColor) GetStatusPresentation(int status)
    {
        return (ConstructionIssueStatus)status switch
        {
            ConstructionIssueStatus.Open => ("Open", "#e4efec", "#15322b"),
            ConstructionIssueStatus.Assigned => ("Toegewezen", "#cfe3d8", "#15322b"),
            ConstructionIssueStatus.InProgress => ("In uitvoering", "#8fbea5", "#ffffff"),
            ConstructionIssueStatus.Closed => ("Afgerond", "#01532d", "#ffffff"),
            _ => ("Onbekend", "#e5e7eb", "#374151")
        };
    }
}