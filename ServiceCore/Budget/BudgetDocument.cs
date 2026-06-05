using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BOCore.Budget;
using DALCore.Models;

namespace ServiceCore.Budget
{
    public class BudgetDocument : IDocument
    {
        private readonly BudgetResultaatBO _resultaat;
        private readonly BudgetVersie _versie;
        private readonly List<BudgetActivityLijnen> _activiteitLijnen;
        private readonly List<Activity> _activiteiten;
        private readonly List<ActivityGroup> _groepen;
        private readonly List<BudgetOppervlaktes> _oppervlaktes;
        private readonly decimal _gewogenFactor;

        public BudgetDocument(
            BudgetResultaatBO resultaat,
            BudgetVersie versie,
            List<BudgetActivityLijnen> activiteitLijnen,
            List<Activity> activiteiten,
            List<ActivityGroup> groepen,
            List<BudgetOppervlaktes> oppervlaktes,
            decimal gewogenFactor)
        {
            _resultaat        = resultaat;
            _versie           = versie;
            _activiteitLijnen = activiteitLijnen;
            _activiteiten     = activiteiten;
            _groepen          = groepen;
            _oppervlaktes     = oppervlaktes;
            _gewogenFactor    = gewogenFactor;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(_versie.BudgetMaster?.Naam ?? "Budget")
                            .Bold().FontSize(18).FontColor(Colors.Blue.Medium);
                        c.Item().Text($"Versie {_versie.Versienummer} — {_versie.VersieNaam}")
                            .FontSize(11).FontColor(Colors.Grey.Medium);
                        c.Item().Text(_versie.BudgetGegevens?.Adres ?? "")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(120).Column(c =>
                    {
                        c.Item().AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"))
                            .FontColor(Colors.Grey.Medium);
                        c.Item().AlignRight().Text($"S-index: {_versie.BudgetGegevens?.SIndexStart:F2} → {_versie.BudgetGegevens?.SIndexHuidig:F2}")
                            .FontSize(8);
                        c.Item().AlignRight().Text($"I-index: {_versie.BudgetGegevens?.IIndexStart:F2} → {_versie.BudgetGegevens?.IIndexHuidig:F2}")
                            .FontSize(8);
                        c.Item().AlignRight().Text($"Factor: {_gewogenFactor:F4}")
                            .FontSize(8).Bold();
                    });
                });
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Blue.Medium);
                col.Item().PaddingBottom(8);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(12).Row(row =>
                {
                    void KpiBox(string label, string waarde, string kleur)
                    {
                        row.RelativeItem().Border(1).BorderColor(kleur).Padding(8).Column(c =>
                        {
                            c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text(waarde).Bold().FontSize(13).FontColor(kleur);
                        });
                        row.ConstantItem(4);
                    }
                    KpiBox("Totale kostprijs", $"€ {_resultaat.TotaalKosten:N0}",    Colors.Blue.Medium);
                    KpiBox("Per eenheid",      $"€ {_resultaat.TotaalPerEenheid:N0}", Colors.Green.Medium);
                    KpiBox("Per m² GBA",       $"€ {_resultaat.TotaalPerM2GBA:N0}",   Colors.Grey.Darken1);
                    row.RelativeItem();
                });

                col.Item().PaddingBottom(16).Element(ComposeKostenTabel);
                col.Item().PaddingBottom(16).Element(ComposeActiviteitenTabel);
                col.Item().Element(ComposeOppervlaktesTabel);
            });
        }

        private void ComposeKostenTabel(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(4).Text("Kostenoverzicht").Bold().FontSize(11).FontColor(Colors.Blue.Medium);

                col.Item().Table(tbl =>
                {
                    tbl.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                    });

                    tbl.Header(h =>
                    {
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text("Kostenpost").Bold();
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignRight().Text("Bedrag").Bold();
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignRight().Text("% totaal").Bold();
                    });

                    void GroepHeader(string label)
                    {
                        tbl.Cell().ColumnSpan(3).Background(Colors.Blue.Lighten5).Padding(4)
                            .Text(label).Bold().FontSize(8).FontColor(Colors.Blue.Medium);
                    }

                    void KostenRij(string label, decimal bedrag, bool indent = true)
                    {
                        var perc = _resultaat.TotaalKosten > 0 ? bedrag / _resultaat.TotaalKosten * 100m : 0m;
                        tbl.Cell().Padding(3).PaddingLeft(indent ? 12 : 4).Text(label);
                        tbl.Cell().Padding(3).AlignRight().Text($"€ {bedrag:N0}");
                        tbl.Cell().Padding(3).AlignRight().Text($"{perc:F1}%").FontColor(Colors.Grey.Darken1);
                    }

                    GroepHeader("Bouwkost (activiteiten)");
                    KostenRij("Totaal bouwkost", _resultaat.TotaalBouw, false);

                    if (_resultaat.KostenOpBouw.Any())
                    {
                        GroepHeader("Erelonen & Studies");
                        foreach (var k in _resultaat.KostenOpBouw)
                            KostenRij(k.Omschrijving, k.Bedrag);
                    }

                    if (_resultaat.Forfaits.Any(f => f.Bedrag > 0))
                    {
                        GroepHeader("Forfaits");
                        foreach (var k in _resultaat.Forfaits.Where(f => f.Bedrag > 0))
                            KostenRij(k.Omschrijving, k.Bedrag);
                    }

                    if (_resultaat.Financiering.Any(f => f.Bedrag > 0))
                    {
                        GroepHeader("Financiering");
                        foreach (var k in _resultaat.Financiering.Where(f => f.Bedrag > 0))
                            KostenRij(k.Omschrijving, k.Bedrag);
                    }

                    if (_resultaat.Onvoorzien > 0)
                    {
                        GroepHeader("Onvoorzien");
                        KostenRij("Onvoorzien", _resultaat.Onvoorzien);
                    }

                    tbl.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text("TOTALE KOSTPRIJS").Bold();
                    tbl.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignRight()
                        .Text($"€ {_resultaat.TotaalKosten:N0}").Bold();
                    tbl.Cell().Background(Colors.Blue.Lighten4).Padding(4).AlignRight().Text("100%").Bold();

                    tbl.Cell().Padding(3).Text("Per eenheid").Italic();
                    tbl.Cell().Padding(3).AlignRight().Text($"€ {_resultaat.TotaalPerEenheid:N0}").Italic();
                    tbl.Cell().Padding(3).AlignRight().Text("").Italic();

                    tbl.Cell().Padding(3).Text("Per m² GBA").Italic();
                    tbl.Cell().Padding(3).AlignRight().Text($"€ {_resultaat.TotaalPerM2GBA:N0}").Italic();
                    tbl.Cell().Padding(3).AlignRight().Text($"{_resultaat.TotaalGBA:N0} m²").Italic().FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private void ComposeActiviteitenTabel(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(4).Text("Activiteiten per lot (VMSW)").Bold().FontSize(11).FontColor(Colors.Blue.Medium);

                var gesorteerd = _groepen.OrderBy(g => g.Lot).ToList();
                foreach (var grp in gesorteerd)
                {
                    var lijnen = _activiteitLijnen
                        .Where(l => _activiteiten.Any(a => a.ActivityId == l.ActivityId && a.GroupId == grp.GroupId))
                        .ToList();

                    if (!lijnen.Any()) continue;

                    col.Item().PaddingTop(6).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        tbl.Header(h =>
                        {
                            h.Cell().ColumnSpan(5).Background(Colors.Blue.Lighten5).Padding(4)
                                .Text($"Lot {grp.Lot} — {grp.Name}").Bold().FontColor(Colors.Blue.Medium);
                        });

                        tbl.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Activiteit").Bold().FontSize(8);
                        tbl.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Alt. €/eenh.").Bold().FontSize(8);
                        tbl.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Alt. totaal").Bold().FontSize(8);
                        tbl.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Nacalc geïnd.").Bold().FontSize(8);
                        tbl.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignRight().Text("Verschil").Bold().FontSize(8);

                        decimal totAlt = 0, totNac = 0;
                        foreach (var lijn in lijnen)
                        {
                            var act      = _activiteiten.FirstOrDefault(a => a.ActivityId == lijn.ActivityId);
                            var altPpu   = lijn.AlternatievePrijsPerEenheid ?? 0m;
                            var nacPpu   = lijn.NacalcPrijsPerEenheid ?? 0m;
                            var nacGeind = nacPpu * lijn.Correctiefactor * _gewogenFactor;
                            var altTot   = altPpu * _resultaat.AantalEenheden;
                            var nacTot   = nacGeind * _resultaat.AantalEenheden;
                            var verschil = altTot - nacTot;
                            totAlt += altTot;
                            totNac += nacTot;

                            tbl.Cell().Padding(3).Text(act?.Omschrijving ?? "—").FontSize(8);
                            tbl.Cell().Padding(3).AlignRight().Text($"€ {altPpu:N2}").FontSize(8);
                            tbl.Cell().Padding(3).AlignRight().Text($"€ {altTot:N0}").FontSize(8);
                            tbl.Cell().Padding(3).AlignRight().Text($"€ {nacGeind:N2}").FontSize(8);
                            tbl.Cell().Padding(3).AlignRight()
                                .Text($"€ {verschil:N0}").FontSize(8)
                                .FontColor(verschil > 0 ? Colors.Orange.Medium
                                         : verschil < 0 ? Colors.Blue.Medium
                                         : Colors.Green.Medium);
                        }

                        tbl.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text($"Subtotaal lot {grp.Lot}").Bold().FontSize(8);
                        tbl.Cell().Background(Colors.Blue.Lighten4).Padding(3).AlignRight().Text("").FontSize(8);
                        tbl.Cell().Background(Colors.Blue.Lighten4).Padding(3).AlignRight().Text($"€ {totAlt:N0}").Bold().FontSize(8);
                        tbl.Cell().Background(Colors.Blue.Lighten4).Padding(3).AlignRight().Text($"€ {totNac:N0}").Bold().FontSize(8);
                        tbl.Cell().Background(Colors.Blue.Lighten4).Padding(3).AlignRight()
                            .Text($"€ {(totAlt - totNac):N0}").Bold().FontSize(8);
                    });
                }
            });
        }

        private void ComposeOppervlaktesTabel(IContainer container)
        {
            if (!_oppervlaktes.Any()) return;

            container.Column(col =>
            {
                col.Item().PaddingBottom(4).Text("Oppervlaktes").Bold().FontSize(11).FontColor(Colors.Blue.Medium);

                col.Item().Table(tbl =>
                {
                    tbl.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                    });

                    tbl.Header(h =>
                    {
                        foreach (var lbl in new[] { "Eenheid", "GBA (m²)", "Tuin", "Terras", "Dakterras", "Grondopp." })
                            h.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text(lbl).Bold().FontSize(8);
                    });

                    foreach (var o in _oppervlaktes)
                    {
                        tbl.Cell().Padding(3).Text(o.EenheidNaam ?? "—").FontSize(8);
                        tbl.Cell().Padding(3).AlignRight().Text($"{o.BewoonbareOpp:N2}").FontSize(8);
                        tbl.Cell().Padding(3).AlignRight().Text($"{o.Tuin:N2}").FontSize(8);
                        tbl.Cell().Padding(3).AlignRight().Text($"{(o.TerrasPrefab + o.TerrasGelijkvloers):N2}").FontSize(8);
                        tbl.Cell().Padding(3).AlignRight().Text($"{o.Dakterras:N2}").FontSize(8);
                        tbl.Cell().Padding(3).AlignRight().Text($"{o.Grondopp:N2}").FontSize(8);
                    }
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text($"{_versie.BudgetMaster?.Naam} — {_versie.VersieNaam} v{_versie.Versienummer}")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
                row.ConstantItem(100).AlignRight().Text(txt =>
                {
                    txt.Span("Pagina ").FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                    txt.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }
    }
}
