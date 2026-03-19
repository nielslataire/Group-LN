using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace ServiceCore.Issues;

public class ConstructionIssueReportService : IConstructionIssueReportService
{
    private readonly cpmRunningContext _db;
    private readonly IConstructionIssueService _issueService;
    private readonly IConfiguration _configuration;
    private static SixLabors.Fonts.Font? _markerFont;

    private static void EnsureFontLoaded()
    {
        if (_markerFont != null)
            return;

        var fontPath = System.IO.Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "fonts",
            "Avenir-Heavy.ttf");

        if (!File.Exists(fontPath))
            return;

        var collection = new SixLabors.Fonts.FontCollection();
        var family = collection.Add(fontPath);

        _markerFont = family.CreateFont(32, SixLabors.Fonts.FontStyle.Regular);
    }

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
        var printableStatuses = new HashSet<int>
        {
            (int)ConstructionIssueStatus.Open,
            (int)ConstructionIssueStatus.Assigned,
            (int)ConstructionIssueStatus.InProgress,
            (int)ConstructionIssueStatus.Reopened
        };
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
        issues = issues
            .Where(x => printableStatuses.Contains(x.Status))
            .ToList();
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
              .GroupBy(x => new
              {
                  x.UnitId,
                  UnitName = string.IsNullOrWhiteSpace(x.Unit?.Name) ? "Zonder eenheid" : x.Unit!.Name
              })
              .OrderBy(x => x.Key.UnitName)
              .ToList();
        var issueIndexById = groupedByUnit
              .SelectMany(unitGroup => unitGroup)
              .Select((issue, index) => new { issue.Id, Number = index + 1 })
            .ToDictionary(x => x.Id, x => x.Number);
        var planSections = await BuildPlanSections(projectId, issues, issueIndexById);

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
                        var unitPlanSections = planSections
                            .Where(x => x.UnitId == unitGroup.Key.UnitId)
                            .OrderBy(x => x.PlanName)
                            .ThenBy(x => x.PageNumber)
                            .ToList();

                        col.Item().PaddingHorizontal(30).PaddingTop(10).PaddingBottom(10).Column(unitCol =>
                        {
                            unitCol.Item().PreventPageBreak().Column(firstBlock =>
                            {
                                firstBlock.Item().Background(primaryColor).CornerRadius(6).PaddingVertical(7).PaddingHorizontal(10).Row(row =>
                                {
                                    row.RelativeItem().Text($"Eenheid: {unitGroup.Key.UnitName}").FontColor(Colors.White).SemiBold();
                                    row.ConstantItem(80).AlignRight().Text($"{unitIssues.Count} punt(en)").FontColor(Colors.White).SemiBold();
                                });

                                firstBlock.Item().PaddingTop(10).Element(cardContainer =>
                                    RenderIssueCard(cardContainer, firstIssue, issueNumber++, primaryColor, responsibleNames, this));
                            });

                            foreach (var issue in unitIssues.Skip(1))
                            {
                                unitCol.Item().PaddingTop(10).Element(cardContainer =>
                                    RenderIssueCard(cardContainer, issue, issueNumber++, primaryColor, responsibleNames, this));
                            }

                            foreach (var section in unitPlanSections)
                            {
                                unitCol.Item().PaddingTop(12).Element(planContainer =>
                                    RenderPlanBlock(planContainer, section, this));
                            }
                        });
                    }
                });

                page.Footer().PaddingHorizontal(30).PaddingBottom(18).PaddingTop(8).Column(footer =>
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

    private static void RenderIssueCard(IContainer container, ConstructionIssue issue, int badgeNumber, string primaryColor, IReadOnlyDictionary<int, string> responsibleNames, ConstructionIssueReportService service)
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

            var photoIds = issue.ConstructionIssueMedia
                .Where(m => (m.MediaType == (int)ConstructionIssueMediaType.Photo || m.MediaType == (int)ConstructionIssueMediaType.DetailPhoto) && !string.IsNullOrWhiteSpace(m.FileId))
                .Select(m => m.FileId)
                .Distinct()
                .Take(4)
                .ToList();

            if (photoIds.Any())
            {
                card.Item().PaddingTop(4).Column(photos =>
                {
                    photos.Item().Text("Foto's").FontSize(8).SemiBold().FontColor("#374151");

                    foreach (var rowGroup in photoIds.Chunk(2))
                    {
                        photos.Item().PaddingTop(4).Row(row =>
                        {
                            row.Spacing(8);
                            foreach (var fileId in rowGroup)
                            {
                                var bytes = service.LoadAssetBytes(fileId, "pictures");
                                row.ConstantItem(184).Height(160).Element(photo =>
                                {
                                    if (bytes != null && IsLikelyImage(bytes))
                                    {
                                        photo.Image(bytes).FitArea();
                                    }
                                    else
                                    {
                                        photo.AlignCenter().AlignMiddle().Text("Foto niet beschikbaar").FontSize(7).FontColor("#9ca3af");
                                    }
                                });
                            }
                        });
                    }
                });
            }
        });
    }

    private async Task<List<IssuePlanSection>> BuildPlanSections(int projectId, List<ConstructionIssue> issues, IReadOnlyDictionary<int, int> issueIndexById)
    {
        var issuesWithPlan = issues
             .Where(i =>
                 i.UnitId.HasValue &&
                 i.PlanXnormalized.HasValue &&
                 i.PlanYnormalized.HasValue &&
                 i.PlanPageNumber.HasValue)
             .ToList();

        if (!issuesWithPlan.Any())
            return new List<IssuePlanSection>();

        var unitIds = issuesWithPlan.Select(i => i.UnitId!.Value).Distinct().ToList();
        var units = await _db.Units
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var executionPlanIds = issuesWithPlan
            .Where(i => i.PlanDocumentId > 0)
            .Select(i => i.PlanDocumentId!.Value)
            .Distinct()
            .ToList();

        var executionPlans = await _db.UnitExecutionPlan
            .Where(p => executionPlanIds.Contains(p.Id) && p.DeletedDate == null)
            .ToDictionaryAsync(p => p.Id);

        return issuesWithPlan
            .GroupBy(i => new { i.UnitId, i.PlanDocumentId, Page = i.PlanPageNumber ?? 1 })
            .Select(g =>
            {
                var first = g.First();
                var unitName = first.Unit?.Name ?? (first.UnitId.HasValue && units.TryGetValue(first.UnitId.Value, out var unit) ? unit.Name : "Zonder eenheid");
                var planName = "Plan";
                string? fileId = null;

                if ((!first.PlanDocumentId.HasValue || first.PlanDocumentId == 0) && first.UnitId.HasValue && units.TryGetValue(first.UnitId.Value, out var fallbackUnit))
                {
                    fileId = fallbackUnit.Plan;
                    planName = string.IsNullOrWhiteSpace(fallbackUnit.Name) ? "Plan" : fallbackUnit.Name;
                }
                else if (first.PlanDocumentId.HasValue && executionPlans.TryGetValue(first.PlanDocumentId.Value, out var executionPlan))
                {
                    fileId = executionPlan.FileId;
                    planName = string.IsNullOrWhiteSpace(executionPlan.Name) ? "Plan" : executionPlan.Name;
                }

                var markers = g
                    .Where(i => i.PlanXnormalized.HasValue && i.PlanYnormalized.HasValue && issueIndexById.ContainsKey(i.Id))
                    .Select(i => new IssuePlanMarker(
                        issueIndexById[i.Id],
                        ClampNormalized(i.PlanXnormalized!.Value),
                        ClampNormalized(i.PlanYnormalized!.Value)))
                    .OrderBy(m => m.Number)
                    .ToList();

                return new IssuePlanSection(first.UnitId, unitName, planName, first.PlanPageNumber ?? 1, fileId, markers);
            })
            .Where(s => s.Markers.Any())
            .OrderBy(s => s.UnitName)
            .ThenBy(s => s.PlanName)
            .ThenBy(s => s.PageNumber)
            .ToList();
    }

    private static void RenderPlanBlock(IContainer container, IssuePlanSection section, ConstructionIssueReportService service)
    {
        container.ShowEntire().Column(col =>
        {
            col.Item().Background("#dfe8e6").CornerRadius(4).PaddingVertical(6).PaddingHorizontal(10).Row(r =>
            {
                r.RelativeItem().Text($"Plan: {section.PlanName}").FontSize(9).SemiBold().FontColor("#15322b");
                r.ConstantItem(140).AlignRight().Text($"{section.Markers.Count} punt(en) aangeduid").FontSize(9).SemiBold().FontColor("#15322b");
            });

            var planImageBytes = string.IsNullOrWhiteSpace(section.FileId)
                ? null
                : service.ResolvePlanPageImageBytes(section.FileId, section.PageNumber);

            col.Item().PaddingTop(4).Border(1).BorderColor("#d9dde2").CornerRadius(6).Padding(8).Height(320).Element(x =>
            {
                if (planImageBytes != null && IsLikelyImage(planImageBytes))
                {
                    var composedBytes = BuildPlanImageWithMarkers(planImageBytes, section.Markers);
                    x.Image(composedBytes).FitArea();
                }
                else
                {
                    x.AlignCenter().AlignMiddle().Text("Plan niet beschikbaar").FontSize(8).FontColor("#9ca3af");
                }
            });
        });
    }

    private static byte[] BuildPlanImageWithMarkers(byte[] imageBytes, IReadOnlyCollection<IssuePlanMarker> markers)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

        foreach (var marker in markers)
        {
            var x = marker.X * image.Width;
            var y = marker.Y * image.Height;

            DrawPinMarker(image, x, y, marker.Number);
        }

        using var output = new MemoryStream();
        image.SaveAsJpeg(output, new JpegEncoder { Quality = 92 });
        return output.ToArray();
    }

    private static void DrawPinMarker(
     SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image,
     float x,
     float y,
     int number)
    {
        var orange = SixLabors.ImageSharp.Color.ParseHex("#f59e0b");
        var white = SixLabors.ImageSharp.Color.White;

        var radius = 32f;

        image.Mutate(ctx =>
        {
            ctx.Fill(
                orange,
                new SixLabors.ImageSharp.Drawing.EllipsePolygon(x, y, radius));
        });

        TryDrawMarkerNumber(image, number.ToString(), x, y, white);
    }

    private static void TryDrawMarkerNumber(
      SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> image,
      string text,
      float centerX,
      float centerY,
      SixLabors.ImageSharp.Color color)
    {
        try
        {
            EnsureFontLoaded();
            if (_markerFont == null)
                return;

            var options = new SixLabors.ImageSharp.Drawing.Processing.RichTextOptions(_markerFont)
            {
                HorizontalAlignment = SixLabors.Fonts.HorizontalAlignment.Center,
                VerticalAlignment = SixLabors.Fonts.VerticalAlignment.Center,
                Origin = new SixLabors.ImageSharp.PointF(centerX, centerY)
            };

            image.Mutate(ctx => ctx.DrawText(options, text, color));
        }
        catch
        {
            // fallback zonder nummer
        }
    }

    private static float ClampNormalized(decimal value)
    {
        var floatValue = (float)value;
        if (floatValue < 0f) return 0f;
        if (floatValue > 1f) return 1f;
        return floatValue;
    }

    private static string BuildMarkerOverlaySvg(IReadOnlyCollection<IssuePlanMarker> markers)
    {
        var svg = new StringBuilder();
        svg.Append("<svg xmlns='http://www.w3.org/2000/svg' width='1000' height='700' viewBox='0 0 1000 700'>");

        foreach (var marker in markers)
        {
            var x = marker.X * 1000f;
            var y = marker.Y * 700f;

            svg.Append(BuildPinSvg(x, y, marker.Number));
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    private static string BuildPinSvg(float x, float y, int number)
    {
        var pinTopY = y - 30f;
        var textY = pinTopY + 16f;

        return $@"
        <g transform='translate({x:0.##},{y:0.##})'>
            <path d='M 0 -28
                     C 10 -28 18 -20 18 -10
                     C 18 2 8 10 0 24
                     C -8 10 -18 2 -18 -10
                     C -18 -20 -10 -28 0 -28 Z'
                  fill='#dc2626'
                  stroke='#ffffff'
                  stroke-width='2' />
            <circle cx='0' cy='-10' r='9' fill='#ffffff' />
            <text x='0' y='-6'
                  text-anchor='middle'
                  font-size='11'
                  font-family='Arial'
                  font-weight='700'
                  fill='#dc2626'>{number}</text>
        </g>";
    }


    private static bool IsLikelyImage(byte[] bytes)
    {
        return bytes.Length > 4
               && ((bytes[0] == 0xFF && bytes[1] == 0xD8)
                   || (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                   || (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46));
    }

    private static string DetectImageMime(byte[] bytes)
    {
        if (bytes.Length > 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "image/png";
        if (bytes.Length > 3 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
            return "image/gif";
        return "image/jpeg";
    }

    private static bool IsLikelyPdf(byte[] bytes)
    {
        return bytes.Length > 4
               && bytes[0] == 0x25
               && bytes[1] == 0x50
               && bytes[2] == 0x44
               && bytes[3] == 0x46;
    }

    private byte[]? ResolvePlanPageImageBytes(string fileId, int pageNumber)
    {
        var directBytes = LoadAssetBytes(fileId, "plans");
        if (directBytes == null || directBytes.Length == 0)
            return null;

        if (IsLikelyImage(directBytes))
            return directBytes;

        if (!IsLikelyPdf(directBytes))
            return null;

        return LoadPlanPageImageFromStorage(fileId, pageNumber);
    }

    private byte[]? LoadPlanPageImageFromStorage(string fileId, int pageNumber)
    {
        var safeFileName = System.IO.Path.GetFileName(fileId ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileName))
            return null;

        var baseUrl = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        var readKey = _configuration["StorageApi:ReadApiKey"];
        var writeKey = _configuration["StorageApi:WriteApiKey"];
        var key = !string.IsNullOrWhiteSpace(readKey) ? readKey : writeKey;

        if (string.IsNullOrWhiteSpace(key))
            return null;

        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var endpoint = $"{baseUrl}/api/assets/plans/{Uri.EscapeDataString(safeFileName)}/page-image?page={safePageNumber}";

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", key);

            var response = httpClient.GetAsync(endpoint).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return bytes.Length > 0 && IsLikelyImage(bytes) ? bytes : null;
        }
        catch
        {
            return null;
        }
    }

    private byte[]? LoadAssetBytes(string fileId, string folder)
    {
        var url = GetSignedAssetUrlByFileName(fileId, folder);
        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                using var httpClient = new HttpClient();
                var bytes = httpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                if (bytes.Length > 0)
                    return bytes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Thumbnail download failed: {url} - {ex.Message}");
            }
        }

        return null;
    }

    private string? GetSignedAssetUrlByFileName(string fileName, string folder)
    {
        var safeFileName = System.IO.Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileName))
            return null;

        var baseUrl = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        var readKey = _configuration["StorageApi:ReadApiKey"];
        if (!string.IsNullOrWhiteSpace(readKey))
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", readKey);

                var signUrl = $"{baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(safeFileName)}/sign";
                var response = httpClient.PostAsync(signUrl, content: null).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var payload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    using var jsonDoc = JsonDocument.Parse(payload);
                    if (jsonDoc.RootElement.TryGetProperty("url", out var urlElement))
                    {
                        var relativeOrAbsolute = urlElement.GetString();
                        if (!string.IsNullOrWhiteSpace(relativeOrAbsolute))
                        {
                            return relativeOrAbsolute.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                ? relativeOrAbsolute
                                : $"{baseUrl}{relativeOrAbsolute}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Thumbnail download failed: {ex.Message}");
            }
        }

        return $"{baseUrl}/{folder}/{Uri.EscapeDataString(safeFileName)}";
    }

    private sealed record IssuePlanSection(int? UnitId, string UnitName, string PlanName, int PageNumber, string? FileId, IReadOnlyList<IssuePlanMarker> Markers);
    private sealed record IssuePlanMarker(int Number, float X, float Y);

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
            ConstructionIssueStatus.WaitingInspection => ("Klaar voor controle", "#fde68a", "#713f12"),
            ConstructionIssueStatus.Resolved => ("Opgelost", "#0f766e", "#ffffff"),
            ConstructionIssueStatus.Rejected => ("Afgewezen", "#ef4444", "#ffffff"),
            ConstructionIssueStatus.Reopened => ("Heropend", "#a855f7", "#ffffff"),
            ConstructionIssueStatus.Closed => ("Afgerond", "#01532d", "#ffffff"),
            _ => ("Onbekend", "#e5e7eb", "#374151")
        };
    }
}