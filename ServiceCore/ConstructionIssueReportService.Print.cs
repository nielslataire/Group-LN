using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ServiceCore.Issues;

/// <summary>Afdrukmodal van de puntenlijst (design-handoff 40u). Drie soorten: puntenlijst (A4 liggend, tabel zoals de lijst),
/// per aannemer (A4 staand, een aannemer per pagina) en rondgang (A4 staand, met afvinkvakje en schrijfruimte). Daarbovenop:
/// groeperen, sorteren en wat er mee op papier komt (foto's, plannen met aanduidingen, historiek, afgesloten punten, interne notities).</summary>
public partial class ConstructionIssueReportService
{
    private sealed record PrintHistoryLine(DateTime Timestamp, string Author, string Text, bool IsInternal);

    public async Task<byte[]> GeneratePrintPdf(int projectId, ConstructionIssuePrintOptions options)
    {
        var ids = (options.IssueIds ?? new List<int>()).Distinct().ToList();
        var type = options.Type is "aannemer" or "rondgang" ? options.Type : "lijst";
        var group = type == "aannemer" ? "aannemer" : (options.Group is "aannemer" or "eenheid" or "fase" ? options.Group : "geen");

        var issues = await _db.ConstructionIssue
            .Include(x => x.Unit)
            .Include(x => x.Category)
            .Include(x => x.ConstructionIssueMedia)
            .Where(x => x.ProjectId == projectId && ids.Contains(x.Id))
            .ToListAsync();

        if (!options.Closed)
        {
            var closed = new HashSet<int> { (int)ConstructionIssueStatus.Closed, (int)ConstructionIssueStatus.Resolved };
            issues = issues.Where(x => !closed.Contains(x.Status)).ToList();
        }

        var project = await _db.Project
            .Include(x => x.PostalCode)
            .Include(x => x.AspNetUser)
            .Include(x => x.IssuerCompanyIdBuilderNavigation)
            .FirstOrDefaultAsync(x => x.ProjectId == projectId);
        var projectName = project?.ProjectName ?? $"Project {projectId}";
        var siteManager = string.Join(" ", new[] { project?.AspNetUser?.Voornaam, project?.AspNetUser?.Familienaam }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (string.IsNullOrWhiteSpace(siteManager)) siteManager = "-";
        var primaryColor = !string.IsNullOrWhiteSpace(project?.IssuerCompanyIdBuilderNavigation?.BrandPrimaryColor)
            ? project.IssuerCompanyIdBuilderNavigation.BrandPrimaryColor
            : "#01532d";

        var responsibleIds = issues.Where(x => x.ResponsiblePartyId.HasValue).Select(x => x.ResponsiblePartyId!.Value).Distinct().ToList();
        var responsibleNames = await _db.CompanyInfo
            .Where(x => responsibleIds.Contains(x.CompanyId))
            .Select(x => new { x.CompanyId, x.BedrijfsNaam })
            .ToDictionaryAsync(x => x.CompanyId, x => x.BedrijfsNaam);

        // Historiek en interne notities: enkel opgehaald als ze mee op papier komen.
        var historyByIssue = new Dictionary<int, List<PrintHistoryLine>>();
        if ((options.History || options.Internal) && issues.Count > 0)
        {
            var issueIds = issues.Select(i => i.Id).ToList();
            var rows = await _db.ConstructionIssueHistory.AsNoTracking()
                .Where(h => issueIds.Contains(h.IssueId) && h.Comment != null && h.Comment != "")
                .OrderBy(h => h.Timestamp)
                .ToListAsync();
            var userKeys = rows.Select(h => h.UserId).Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();
            var users = await _db.Users.AsNoTracking()
                .Where(u => userKeys.Contains(u.UserId))
                .Select(u => new { u.UserId, Name = (u.Voornaam + " " + u.Familienaam).Trim() })
                .ToListAsync();
            foreach (var h in rows)
            {
                // Intern: enkel met "Interne notities"; de rest (berichten) enkel met "Historiek en opmerkingen".
                if (h.IsInternal ? !options.Internal : !options.History) continue;
                var author = users.FirstOrDefault(u => u.UserId == h.UserId)?.Name;
                if (!historyByIssue.TryGetValue(h.IssueId, out var list)) historyByIssue[h.IssueId] = list = new List<PrintHistoryLine>();
                list.Add(new PrintHistoryLine(h.Timestamp.ToLocalTime(), string.IsNullOrWhiteSpace(author) ? "Systeem" : author!, h.Comment!, h.IsInternal));
            }
        }

        string PhaseLabel(int phase) => (ConstructionIssuePhase)phase switch
        {
            ConstructionIssuePhase.DuringConstruction => "Tijdens uitvoering",
            ConstructionIssuePhase.PreDeliveryInspection => "Vooroplevering",
            ConstructionIssuePhase.AfterDelivery => "Voorlopige oplevering",
            ConstructionIssuePhase.WarrantyPeriod => "Garantieperiode",
            _ => "Zonder fase"
        };
        string UnitName(ConstructionIssue i) => string.IsNullOrWhiteSpace(i.Unit?.Name) ? "Algemeen" : i.Unit!.Name;
        string Responsible(ConstructionIssue i) { var r = GetResponsibleDisplay(i, responsibleNames); return string.IsNullOrWhiteSpace(r) ? "Zonder aannemer" : r; }
        string Nr(ConstructionIssue i) => "P-" + (i.PuntNr ?? i.Id).ToString("000");

        IEnumerable<ConstructionIssue> Sorted(IEnumerable<ConstructionIssue> src) => options.Sort switch
        {
            "nr" => src.OrderByDescending(i => i.PuntNr ?? i.Id),
            "eenheid" => src.OrderBy(UnitName).ThenBy(i => i.DueDate ?? DateOnly.MaxValue),
            "status" => src.OrderBy(i => i.Status).ThenBy(i => i.DueDate ?? DateOnly.MaxValue),
            _ => src.OrderBy(i => i.DueDate ?? DateOnly.MaxValue).ThenBy(i => i.PuntNr ?? i.Id)
        };

        // Groepen in de volgorde van de koppen; zonder groepering een enkele naamloze groep.
        List<(string? Title, List<ConstructionIssue> Items)> groups = group switch
        {
            "aannemer" => issues.GroupBy(Responsible).OrderBy(g => g.Key == "Zonder aannemer" ? 1 : 0).ThenBy(g => g.Key).Select(g => ((string?)g.Key, Sorted(g).ToList())).ToList(),
            "eenheid" => issues.GroupBy(UnitName).OrderBy(g => g.Key == "Algemeen" ? 0 : 1).ThenBy(g => g.Key).Select(g => ((string?)g.Key, Sorted(g).ToList())).ToList(),
            "fase" => issues.GroupBy(i => i.IssuePhase).OrderBy(g => g.Key).Select(g => ((string?)PhaseLabel(g.Key), Sorted(g).ToList())).ToList(),
            _ => new List<(string?, List<ConstructionIssue>)> { (null, Sorted(issues).ToList()) }
        };
        var ordered = groups.SelectMany(g => g.Items).ToList();
        var numberById = ordered.Select((i, idx) => new { i.Id, N = i.PuntNr ?? (idx + 1) }).ToDictionary(x => x.Id, x => x.N);
        var planSections = options.Plans && ordered.Count > 0
            ? await BuildPlanSections(projectId, ordered, numberById)
            : new List<IssuePlanSection>();

        var title = type switch { "aannemer" => "PUNTEN PER AANNEMER", "rondgang" => "RONDGANG", _ => "PUNTENLIJST" };
        var now = DateTime.Now;
        var landscape = type == "lijst";
        var gutter = landscape ? 28 : 30;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor("#1f2937"));

                page.Header().Background(primaryColor).PaddingHorizontal(gutter).PaddingVertical(16).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(title).FontSize(18).SemiBold().FontColor(Colors.White);
                        c.Item().PaddingTop(2).Text($"{projectName} · {ordered.Count} punt(en)").FontColor("#e6f4ee");
                    });
                    row.AutoItem().AlignBottom().Text($"{now:dd/MM/yyyy}").FontColor("#e6f4ee");
                });

                page.Content().PaddingHorizontal(gutter).PaddingTop(14).PaddingBottom(18).Column(col =>
                {
                    if (ordered.Count == 0)
                    {
                        col.Item().PaddingTop(30).AlignCenter().Text("Geen punten om af te drukken met deze keuzes.").FontColor("#6b7280");
                        return;
                    }

                    var firstGroup = true;
                    foreach (var g in groups)
                    {
                        if (g.Items.Count == 0) continue;
                        // Per aannemer: elke aannemer begint op een nieuwe pagina, om mee te geven.
                        if (type == "aannemer" && !firstGroup) col.Item().PageBreak();
                        firstGroup = false;

                        if (g.Title != null)
                        {
                            col.Item().PaddingTop(type == "aannemer" ? 0 : 10).PreventPageBreak().Column(head =>
                            {
                                head.Item().Background(primaryColor).CornerRadius(5).PaddingVertical(6).PaddingHorizontal(10).Row(r =>
                                {
                                    r.RelativeItem().Text(g.Title).FontColor(Colors.White).SemiBold().FontSize(10);
                                    r.AutoItem().Text($"{g.Items.Count} punt(en)").FontColor(Colors.White).SemiBold();
                                });
                                if (type == "aannemer")
                                    head.Item().PaddingTop(4).Text($"Werfleider: {siteManager}   •   Project: {projectName}").FontSize(8).FontColor("#6b7280");
                            });
                        }

                        switch (type)
                        {
                            case "aannemer":
                                foreach (var issue in g.Items)
                                {
                                    col.Item().PaddingTop(10).Column(block =>
                                    {
                                        block.Item().Element(c => RenderIssueCard(c, issue, numberById[issue.Id], primaryColor, responsibleNames, this, options.Photos));
                                        RenderPrintHistory(block, historyByIssue, issue.Id);
                                    });
                                }
                                break;
                            case "rondgang":
                                foreach (var issue in g.Items)
                                    col.Item().PaddingTop(8).ShowEntire().Element(c => RenderRondgangItem(c, issue, Nr(issue), UnitName(issue), Responsible(issue), options.Photos, historyByIssue, primaryColor));
                                break;
                            default:
                                col.Item().PaddingTop(6).Element(c => RenderListTable(c, g.Items, Nr, UnitName, Responsible, options.Photos, historyByIssue, primaryColor));
                                break;
                        }
                    }

                    if (planSections.Count > 0)
                    {
                        col.Item().PageBreak();
                        col.Item().Text("PLANNEN MET AANDUIDINGEN").SemiBold().FontSize(11).FontColor(primaryColor);
                        foreach (var section in planSections)
                            col.Item().PaddingTop(10).Element(c => RenderPlanBlock(c, section, this));
                    }
                });

                page.Footer().PaddingHorizontal(gutter).PaddingBottom(14).PaddingTop(6).Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(primaryColor);
                    footer.Item().PaddingTop(5).AlignRight().Text(text =>
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

    private static void RenderPrintHistory(ColumnDescriptor block, IReadOnlyDictionary<int, List<PrintHistoryLine>> history, int issueId)
    {
        if (!history.TryGetValue(issueId, out var lines) || lines.Count == 0) return;
        block.Item().PaddingLeft(10).PaddingTop(4).Column(h =>
        {
            foreach (var l in lines)
            {
                h.Item().PaddingTop(2).Text(t =>
                {
                    t.Span($"{l.Timestamp:dd/MM/yyyy} · {l.Author}: ").FontSize(8).SemiBold().FontColor("#374151");
                    if (l.IsInternal) t.Span("INTERN ").FontSize(7).SemiBold().FontColor("#8a6a32");
                    t.Span(l.Text).FontSize(8).FontColor("#4b5563");
                });
            }
        });
    }

    private void RenderListTable(IContainer container, List<ConstructionIssue> items, Func<ConstructionIssue, string> nr, Func<ConstructionIssue, string> unit,
        Func<ConstructionIssue, string> responsible, bool photos, IReadOnlyDictionary<int, List<PrintHistoryLine>> history, string primaryColor)
    {
        IContainer Head(IContainer c) => c.BorderBottom(1).BorderColor(primaryColor).PaddingVertical(4).PaddingRight(6);
        IContainer Body(IContainer c) => c.BorderBottom(1).BorderColor("#e5e7eb").PaddingVertical(5).PaddingRight(6);
        uint columns = photos ? 8u : 7u;

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(44);
                if (photos) c.ConstantColumn(54);
                c.RelativeColumn(4);
                c.RelativeColumn(1.4f);
                c.RelativeColumn(1.6f);
                c.RelativeColumn(2f);
                c.ConstantColumn(86);
                c.ConstantColumn(50);
            });
            table.Header(h =>
            {
                h.Cell().Element(Head).Text("PUNT").FontSize(7).SemiBold().FontColor("#6b7280");
                if (photos) h.Cell().Element(Head).Text("FOTO").FontSize(7).SemiBold().FontColor("#6b7280");
                h.Cell().Element(Head).Text("OMSCHRIJVING").FontSize(7).SemiBold().FontColor("#6b7280");
                h.Cell().Element(Head).Text("EENHEID").FontSize(7).SemiBold().FontColor("#6b7280");
                h.Cell().Element(Head).Text("KAMER / ZONE").FontSize(7).SemiBold().FontColor("#6b7280");
                h.Cell().Element(Head).Text("AANNEMER").FontSize(7).SemiBold().FontColor("#6b7280");
                h.Cell().Element(Head).Text("STATUS").FontSize(7).SemiBold().FontColor("#6b7280");
                h.Cell().Element(Head).Text("DEADLINE").FontSize(7).SemiBold().FontColor("#6b7280");
            });

            foreach (var issue in items)
            {
                var status = GetStatusPresentation(issue.Status);
                table.Cell().Element(Body).Text(nr(issue)).FontSize(8).SemiBold();
                if (photos)
                {
                    table.Cell().Element(Body).Height(36).Element(c => RenderFirstPhoto(c, issue));
                }
                table.Cell().Element(Body).Text(t =>
                {
                    t.Span(issue.Title ?? "(zonder titel)").SemiBold();
                    if (GetPriorityPresentation(issue.Priority).Show) t.Span("  KRITIEK").FontSize(7).SemiBold().FontColor("#b91c1c");
                });
                table.Cell().Element(Body).Text(unit(issue));
                table.Cell().Element(Body).Text(GetLocationDisplay(issue) is { Length: > 0 } loc ? loc : "—");
                table.Cell().Element(Body).Text(responsible(issue));
                table.Cell().Element(Body).Text(status.Label).FontSize(8);
                table.Cell().Element(Body).Text(issue.DueDate.HasValue ? issue.DueDate.Value.ToString("dd/MM/yyyy") : "—");

                if (history.TryGetValue(issue.Id, out var lines) && lines.Count > 0)
                {
                    table.Cell().ColumnSpan(columns).Background("#f7f8f6").PaddingVertical(3).PaddingLeft(44).Column(h =>
                    {
                        foreach (var l in lines)
                            h.Item().Text(t =>
                            {
                                t.Span($"{l.Timestamp:dd/MM/yyyy} · {l.Author}: ").FontSize(7.5f).SemiBold().FontColor("#374151");
                                if (l.IsInternal) t.Span("INTERN ").FontSize(7).SemiBold().FontColor("#8a6a32");
                                t.Span(l.Text).FontSize(7.5f).FontColor("#4b5563");
                            });
                    });
                }
            }
        });
    }

    private void RenderRondgangItem(IContainer container, ConstructionIssue issue, string nr, string unit, string responsible, bool photos,
        IReadOnlyDictionary<int, List<PrintHistoryLine>> history, string primaryColor)
    {
        var status = GetStatusPresentation(issue.Status);
        container.Border(1).BorderColor("#d9dde2").CornerRadius(6).Padding(10).Row(row =>
        {
            row.ConstantItem(22).PaddingTop(1).Width(14).Height(14).Border(1.2f).BorderColor("#4b5563");
            row.RelativeItem().Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span(nr + "  ").SemiBold().FontColor(primaryColor);
                        t.Span(issue.Title ?? "(zonder titel)").SemiBold().FontSize(10.5f);
                    });
                    r.AutoItem().Text(issue.DueDate.HasValue ? $"Deadline {issue.DueDate.Value:dd/MM/yyyy}" : string.Empty).FontSize(8).FontColor("#6b7280");
                });
                var meta = new[] { unit, GetLocationDisplay(issue), responsible, status.Label }.Where(x => !string.IsNullOrWhiteSpace(x));
                c.Item().PaddingTop(2).Text(string.Join("   •   ", meta)).FontSize(8).FontColor("#6b7280");
                if (!string.IsNullOrWhiteSpace(issue.Description))
                    c.Item().PaddingTop(4).Text(issue.Description).FontSize(9);
                RenderPrintHistory(c, history, issue.Id);
                // Schrijfruimte voor de notitie op de werf.
                c.Item().PaddingTop(14).LineHorizontal(0.6f).LineColor("#9ca3af");
                c.Item().PaddingTop(16).LineHorizontal(0.6f).LineColor("#9ca3af");
            });
            if (photos)
                row.ConstantItem(96).PaddingLeft(8).Height(72).Element(p => RenderFirstPhoto(p, issue));
        });
    }

    private void RenderFirstPhoto(IContainer container, ConstructionIssue issue)
    {
        var fileId = issue.ConstructionIssueMedia
            .Where(m => (m.MediaType == (int)ConstructionIssueMediaType.Photo || m.MediaType == (int)ConstructionIssueMediaType.DetailPhoto) && !string.IsNullOrWhiteSpace(m.FileId))
            .Select(m => m.FileId)
            .FirstOrDefault();
        var bytes = string.IsNullOrWhiteSpace(fileId) ? null : LoadAssetBytes(fileId, "pictures");
        if (bytes != null && IsLikelyImage(bytes)) container.Image(bytes).FitArea();
    }
}
