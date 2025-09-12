using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CPMCore.Models.Projecten;

namespace CPMCore.Documents
{

        public class RecalculationReportDocument : IDocument
        {
            private readonly ProjectContractsModel _m;
            private readonly byte[] _logoBytes;     // mag null zijn
            private readonly string _fontFamily;    // mag null zijn; gebruik exacte internal family name
            private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("nl-BE");
            private const string GreenHex = "#01532d";

            public RecalculationReportDocument(ProjectContractsModel model, byte[] logoBytes = null, string fontFamily = null)
            {
                _m = model ?? throw new ArgumentNullException(nameof(model));
                _logoBytes = logoBytes;
                _fontFamily = string.IsNullOrWhiteSpace(fontFamily) ? null : fontFamily;
            }

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

            public void Compose(IDocumentContainer container)
            {
                // Eerste pagina (globaal overzicht)
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x =>
                        x.FontSize(9)
                         .FontFamily(!string.IsNullOrWhiteSpace(_fontFamily) ? _fontFamily : "Lato")
                    );

                    page.Header().Element(Header);
                    page.Content().Element(ComposeFirstPageContent);
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span($"Nacalculatie – {_m.ProjectName} · ");
                        txt.CurrentPageNumber();
                        txt.Span(" / ");
                        txt.TotalPages();
                    });
                });

            // Detailpagina's per groep
            foreach (var g in _m.ActivityGroups)
            {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(25);
                        page.DefaultTextStyle(x =>
                            x.FontSize(9)
                             .FontFamily(!string.IsNullOrWhiteSpace(_fontFamily) ? _fontFamily : "Lato")
                        );

                        page.Header().PaddingBottom(12).Element(c => GroupHeader(c, g.Name));
                        page.Content().Element(c => ComposeGroupPageContent(c, g.ID, g.Name));
                        page.Footer().AlignCenter().Text(txt =>
                        {
                            txt.Span($"Nacalculatie – {_m.ProjectName} · ");
                            txt.CurrentPageNumber();
                            txt.Span(" / ");
                            txt.TotalPages();
                        });
                    });
                }
            }

            private void Header(IContainer c)
            {
                c.Row(row =>
                {
                    if (_logoBytes != null && _logoBytes.Length > 0)
                        row.ConstantItem(140).Image(_logoBytes).FitArea();

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text(_m.ProjectName).SemiBold().FontSize(16);
                        col.Item().AlignRight().Text($"Project-ID: {_m.ProjectId}").Light().FontSize(9);
                        col.Item().AlignRight().Text($"Datum: {DateTime.Now:dd/MM/yyyy}").Light().FontSize(9);
                    });
                });
            }

            private void GroupHeader(IContainer c, string groupName)
            {
                c.Row(row =>
                {
                    if (_logoBytes != null && _logoBytes.Length > 0)
                        row.ConstantItem(120).Image(_logoBytes).FitArea();

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text(_m.ProjectName).SemiBold().FontSize(14);
                        col.Item().AlignRight().Text(groupName).SemiBold().FontSize(10);
                        col.Item().AlignRight().Text($"Datum: {DateTime.Now:dd/MM/yyyy}").Light().FontSize(9);
                    });
                });
            }

            // -------------------------
            // Eerste pagina (overzicht)
            // -------------------------
            private void ComposeFirstPageContent(IContainer c)
            {
                c.Column(col =>
                {
                    col.Item().PaddingTop(12).Element(ComposeGlobalSummary);

                    col.Item().PaddingTop(12).PaddingBottom(12).Text("Overzicht per categorie").SemiBold().FontSize(12);

                    col.Item().Element(ComposeGroupOverviewTable);
                });
            }

            private void ComposeGlobalSummary(IContainer c)
            {
                var totalBudget = SumBudgetAll();
                var totalContract = SumContractAll();
                var totalInvoiced = SumInvoicedAll();
                var diffBC = totalBudget - totalContract;
                var diffCF = totalContract - totalInvoiced;
                var pct = SafePct(totalInvoiced, totalContract);

                c.Container().Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
                {
                    col.Item().Text("Samenvatting").SemiBold().FontSize(12);

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Totaal Begroot: {Cur(totalBudget)}");
                        r.RelativeItem().Text($"Totaal Gecontracteerd: {Cur(totalContract)}");
                    });

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Totaal Gefactureerd: {Cur(totalInvoiced)}");
                        r.RelativeItem().Text(txt =>
                        {
                            txt.Span("Verschil Begroot vs Contract: ");
                            if (diffBC < 0)
                                txt.Span(CurWithSign(diffBC)).FontColor(Colors.Red.Medium);
                            else
                                txt.Span(CurWithSign(diffBC));
                        });
                    });

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(txt =>
                        {
                            txt.Span("Verschil Contract vs Factuur: ");
                            if (diffCF < 0)
                                txt.Span(CurWithSign(diffCF)).FontColor(Colors.Red.Medium);
                            else
                                txt.Span(CurWithSign(diffCF));
                        });
                        r.RelativeItem().Text($"Percentage Gefactureerd: {pct:0.0}%");
                    });
                });
            }

            private void ComposeGroupOverviewTable(IContainer c)
            {
                var rows = _m.ActivityGroups
                    .Select(g =>
                    {
                        var budget = SumBudgetByGroup(g.ID);
                        var contract = SumContractByGroup(g.ID);
                        var invoice = SumInvoicedByGroup(g.ID);
                        return new
                        {
                            g.ID,
                            g.Name,
                            Budget = budget,
                            Contract = contract,
                            Invoiced = invoice,
                            DiffBC = budget - contract,
                            DiffCF = contract - invoice,
                            Pct = SafePct(invoice, contract)
                        };
                    })
                    .ToList();

                var totalBudget = rows.Sum(x => x.Budget);
                var totalContract = rows.Sum(x => x.Contract);
                var totalInvoiced = rows.Sum(x => x.Invoiced);
                var totalDiffBC = totalBudget - totalContract;
                var totalDiffCF = totalContract - totalInvoiced;
                var totalPct = SafePct(totalInvoiced, totalContract);

                // cell-helpers (zonder TextStyle op container)
                Func<IContainer, IContainer> HeaderCell = x =>
                    x.Background(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(8);

                Func<IContainer, IContainer> HeaderRight = x => HeaderCell(x).AlignRight();

                Func<IContainer, IContainer> DataCellLeft = x =>
                    x.PaddingVertical(4).PaddingHorizontal(8);

                Func<IContainer, IContainer> DataCellRight = x =>
                    DataCellLeft(x).AlignRight();

                Func<IContainer, IContainer> FooterCell = x =>
                    x.Background(GreenHex).PaddingVertical(6).PaddingHorizontal(8);

                Func<IContainer, IContainer> FooterRight = x => FooterCell(x).AlignRight();

                c.Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3); // categorie
                        cols.RelativeColumn(2); // begroot
                        cols.RelativeColumn(2); // gecontracteerd
                        cols.RelativeColumn(2); // gefactureerd
                        cols.RelativeColumn(2); // diff B vs C
                        cols.RelativeColumn(2); // diff C vs F
                        cols.RelativeColumn(1); // %
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Categorie").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Begroot").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Gecontracteerd").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Gefactureerd").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Verschil B vs C").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Verschil C vs F").SemiBold();
                        h.Cell().Element(HeaderRight).Text("%").SemiBold();
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Element(DataCellLeft).Text(r.Name);
                        table.Cell().Element(DataCellRight).Text(Cur(r.Budget));
                        table.Cell().Element(DataCellRight).Text(Cur(r.Contract));
                        table.Cell().Element(DataCellRight).Text(Cur(r.Invoiced));

                        table.Cell().Element(DataCellRight).Text(txt =>
                        {
                            var s = CurWithSign(r.DiffBC);
                            if (r.DiffBC < 0) txt.Span(s).FontColor(Colors.Red.Medium);
                            else txt.Span(s);
                        });

                        table.Cell().Element(DataCellRight).Text(txt =>
                        {
                            var s = CurWithSign(r.DiffCF);
                            if (r.DiffCF < 0) txt.Span(s).FontColor(Colors.Red.Medium);
                            else txt.Span(s);
                        });

                        table.Cell().Element(DataCellRight).Text($"{r.Pct:0.0}%");
                    }

                    // Totaalrij in groen + witte tekst
                    table.Cell().Element(FooterCell).Text("TOTAAL").SemiBold().FontColor(Colors.White);
                    table.Cell().Element(FooterRight).Text(Cur(totalBudget)).SemiBold().FontColor(Colors.White);
                    table.Cell().Element(FooterRight).Text(Cur(totalContract)).SemiBold().FontColor(Colors.White);
                    table.Cell().Element(FooterRight).Text(Cur(totalInvoiced)).SemiBold().FontColor(Colors.White);
                    table.Cell().Element(FooterRight).Text(CurWithSign(totalDiffBC)).SemiBold().FontColor(Colors.White);
                    table.Cell().Element(FooterRight).Text(CurWithSign(totalDiffCF)).SemiBold().FontColor(Colors.White);
                    table.Cell().Element(FooterRight).Text($"{totalPct:0.0}%").SemiBold().FontColor(Colors.White);
                });
            }

            // -------------------------
            // Groepspagina (detail)
            // -------------------------
            private void ComposeGroupPageContent(IContainer c, int groupId, string groupName)
            {
                var budget = SumBudgetByGroup(groupId);
                var contract = SumContractByGroup(groupId);
                var invoice = SumInvoicedByGroup(groupId);
                var diffBC = budget - contract;
                var diffCF = contract - invoice;
                var pct = SafePct(invoice, contract);

                c.Column(col =>
                {
                    // Bovenaan samenvatting
                    col.Item().Container().Background(Colors.Grey.Lighten4).Padding(10).Column(s =>
                    {
                        s.Item().Text($"Samenvatting – {groupName}").SemiBold().FontSize(12);

                        s.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"Begroot: {Cur(budget)}");
                            r.RelativeItem().Text($"Gecontracteerd: {Cur(contract)}");
                            r.RelativeItem().Text($"Gefactureerd: {Cur(invoice)}");
                        });

                        s.Item().Row(r =>
                        {
                            r.RelativeItem().Text(txt =>
                            {
                                txt.Span("Verschil B vs C: ");
                                if (diffBC < 0) txt.Span(CurWithSign(diffBC)).FontColor(Colors.Red.Medium);
                                else txt.Span(CurWithSign(diffBC));
                            });

                            r.RelativeItem().Text(txt =>
                            {
                                txt.Span("Verschil C vs F: ");
                                if (diffCF < 0) txt.Span(CurWithSign(diffCF)).FontColor(Colors.Red.Medium);
                                else txt.Span(CurWithSign(diffCF));
                            });

                            r.RelativeItem().Text($"Percentage Gefactureerd: {pct:0.0}%");
                        });
                    });

                    // Tabel met activiteiten
                    col.Item().PaddingTop(10).Element(cn => ComposeActivitiesTable(cn, groupId));

                    // Groene samenvattingsrij onderaan
                    col.Item().PaddingTop(6).Row(r =>
                    {
                        r.RelativeItem(4).Background(GreenHex).Padding(8).Text("Samenvatting").SemiBold().FontColor(Colors.White);
                        r.RelativeItem(2).Background(GreenHex).Padding(8).AlignRight().Text(Cur(budget)).SemiBold().FontColor(Colors.White);
                        r.RelativeItem(2).Background(GreenHex).Padding(8).AlignRight().Text(Cur(contract)).SemiBold().FontColor(Colors.White);
                        r.RelativeItem(2).Background(GreenHex).Padding(8).AlignRight().Text(Cur(invoice)).SemiBold().FontColor(Colors.White);
                        r.RelativeItem(2).Background(GreenHex).Padding(8).AlignRight().Text(CurWithSign(diffBC)).SemiBold().FontColor(Colors.White);
                        r.RelativeItem(2).Background(GreenHex).Padding(8).AlignRight().Text(CurWithSign(diffCF)).SemiBold().FontColor(Colors.White);
                        r.ConstantItem(60).Background(GreenHex).Padding(8).AlignRight().Text($"{pct:0.0}%").SemiBold().FontColor(Colors.White);
                        r.RelativeItem(2).Background(GreenHex).Padding(8).AlignRight().Text("").SemiBold().FontColor(Colors.White);
                    });
                });
            }

            private void ComposeActivitiesTable(IContainer c, int groupId)
            {
                var activities = _m.ActivityGroups.First(g => g.ID == groupId)
                    .Activities
                    .Where(a =>
                        _m.BudgetActivities.Any(b => b.Activity.ID == a.ID) ||
                        _m.Contracts.SelectMany(x => x.Activities).Any(ac => ac.Activity.ID == a.ID) ||
                        _m.IncommingInvoicesActivities.Any(ii => ii.Activity.ID == a.ID))
                    .OrderBy(a => a.Name)
                    .Select(a =>
                    {
                        var budget = _m.BudgetActivities.Where(b => b.Activity.ID == a.ID).Sum(x => (decimal?)x.Price) ?? 0m;
                        var contract = _m.Contracts
                            .SelectMany(cn => cn.Activities.Where(w => w.Activity.ID == a.ID))
                            .GroupBy(g => g.ContractId)
                            .Sum(g => g.Sum(x => (decimal?)x.Price) ?? 0m);
                        var invoice = _m.IncommingInvoicesActivities.Where(ii => ii.Activity.ID == a.ID).Sum(x => (decimal?)x.Price) ?? 0m;
                        var diffBC = budget - contract;
                        var diffCF = contract - invoice;
                        var pct = SafePct(invoice, contract);

                        var status = invoice == contract ? "Volledig"
                                   : invoice < contract ? "Gedeeltelijk"
                                   : "Over";

                        return new
                        {
                            a.ID,
                            a.Name,
                            Budget = budget,
                            Contract = contract,
                            Invoiced = invoice,
                            DiffBC = diffBC,
                            DiffCF = diffCF,
                            Pct = pct,
                            Status = status
                        };
                    })
                    .ToList();

                // Helpers
                Func<IContainer, IContainer> HeaderCell = x =>
                    x.Background(Colors.Grey.Lighten2).PaddingVertical(6).PaddingHorizontal(8);
                Func<IContainer, IContainer> HeaderRight = x => HeaderCell(x).AlignRight();
                Func<IContainer, IContainer> DataLeft = x =>
                    x.PaddingVertical(4).PaddingHorizontal(8);
                Func<IContainer, IContainer> DataRight = x =>
                    DataLeft(x).AlignRight();

                c.Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(4);  // Activiteit
                        cols.RelativeColumn(2);  // Begroot
                        cols.RelativeColumn(2);  // Gecontracteerd
                        cols.RelativeColumn(2);  // Gefactureerd
                        cols.RelativeColumn(2);  // Diff B vs C
                        cols.RelativeColumn(2);  // Diff C vs F
                        cols.ConstantColumn(60); // %
                        cols.RelativeColumn(2);  // Status
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Activiteit").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Begroot").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Gecontracteerd").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Gefactureerd").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Verschil B vs C").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Verschil C vs F").SemiBold();
                        h.Cell().Element(HeaderRight).Text("%").SemiBold();
                        h.Cell().Element(HeaderRight).Text("Status").SemiBold();
                    });

                    foreach (var a in activities)
                    {
                        table.Cell().Element(DataLeft).Text(a.Name);
                        table.Cell().Element(DataRight).Text(Cur(a.Budget));
                        table.Cell().Element(DataRight).Text(Cur(a.Contract));
                        table.Cell().Element(DataRight).Text(Cur(a.Invoiced));

                        table.Cell().Element(DataRight).Text(txt =>
                        {
                            var s = CurWithSign(a.DiffBC);
                            if (a.DiffBC < 0) txt.Span(s).FontColor(Colors.Red.Medium);
                            else txt.Span(s);
                        });

                        table.Cell().Element(DataRight).Text(txt =>
                        {
                            var s = CurWithSign(a.DiffCF);
                            if (a.DiffCF < 0) txt.Span(s).FontColor(Colors.Red.Medium);
                            else txt.Span(s);
                        });

                        table.Cell().Element(DataRight).Text($"{a.Pct:0.0}%");

                        // Status met subtiele achtergrond
                        table.Cell().Element(c =>
                        {
                            var bg = a.Status == "Over" ? Colors.Red.Lighten4
                                   : a.Status == "Volledig" ? Colors.Green.Lighten4
                                   : Colors.Grey.Lighten4;

                            return DataRight(c).Background(bg);
                        })
                        .Text(a.Status);
                    }
                });
            }

            // -------------------------
            // Helpers (sommen / format)
            // -------------------------
            private decimal SumBudgetAll() =>
                _m.BudgetActivities.Sum(s => (decimal?)s.Price) ?? 0m;

            private decimal SumContractAll() =>
                _m.Contracts.SelectMany(s => s.Activities).Sum(s => (decimal?)s.Price) ?? 0m;

            private decimal SumInvoicedAll() =>
                _m.IncommingInvoicesActivities.Sum(s => (decimal?)s.Price) ?? 0m;

            private decimal SumBudgetByGroup(int groupId) =>
                _m.BudgetActivities.Where(b => b.Activity.Group.ID == groupId)
                  .Sum(x => (decimal?)x.Price) ?? 0m;

            private decimal SumContractByGroup(int groupId) =>
                _m.Contracts.SelectMany(c => c.Activities.Where(a => a.Activity.Group.ID == groupId))
                  .Sum(x => (decimal?)x.Price) ?? 0m;

            private decimal SumInvoicedByGroup(int groupId) =>
                _m.IncommingInvoicesActivities.Where(ii => ii.Activity.Group.ID == groupId)
                  .Sum(x => (decimal?)x.Price) ?? 0m;

            private string Cur(decimal v) => string.Format(_culture, "{0:C}", v);

            private string CurWithSign(decimal v)
            {
                var abs = Math.Abs(v);
                var sign = v < 0 ? "-" : "";
                return sign + string.Format(_culture, "{0:C}", abs);
            }

            private decimal SafePct(decimal part, decimal whole) =>
                whole == 0 ? 0 : Math.Round((part / whole) * 100m, 1);
        }
    }

