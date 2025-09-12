using System;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CPMCore.Models.Projecten;

namespace CPMCore.Documents
{

    public class CostCalculationDocument : IDocument
    {
        private readonly ProjectCostCalculationVM _vm;
        private readonly string _title;

        public CostCalculationDocument(ProjectCostCalculationVM vm, string title = "Kostencalculatie")
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _title = title;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            var ci = new CultureInfo("nl-BE");

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(t => t.FontSize(10));

                //page.Header().Column(col =>
                //{
                //    col.Item().Text(_title).FontSize(16).SemiBold();
                //    col.Item().Text($"Project #{_vm.ProjectId} • {_vm.UnitCount} eenheid(en)");
                //    col.Item().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm", ci)).FontColor(Colors.Grey.Medium);
                //});

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    // 1) Groene hoofdregel: Raming aankoopprijs + namen
                    var unitNames = string.Join(", ", _vm.Lines.Select(l => string.IsNullOrWhiteSpace(l.Code) ? $"U{l.UnitId}" : l.Code));
                    col.Item().Element(e => SectionBarGreen(e, $"Raming aankoopprijs – {unitNames}"));

                    // 2) Per eenheid: kostprijs-blok (grijs) + 2 regels
                    foreach (var ln in _vm.Lines)
                    {
                        var name = string.IsNullOrWhiteSpace(ln.Code) ? $"U{ln.UnitId}" : ln.Code;
                        col.Item().Element(e => SectionBarGrey(e, $"Kostprijs – {name}"));
                        col.Item().Element(e => RowMoney(e, "Grondwaarde", ln.NetLandBase, ci));
                        col.Item().Element(e => RowMoney(e, "Bouwwaarde", ln.NetBuildBase, ci));
                    }

                    // 3) Registratie op grondwaardes (alleen tonen als er iets is)
                    var anyReg = _vm.Lines.Any(l => l.RegistrationAmount > 0);
                    if (anyReg)
                    {
                        col.Item().Element(e => SectionBarGrey(e, $"Registratie ({_vm.RegistrationPercent:0.##}%) op de grondwaardes"));
                        foreach (var ln in _vm.Lines.Where(x => x.RegistrationAmount > 0))
                        {
                            var name = string.IsNullOrWhiteSpace(ln.Code) ? $"U{ln.UnitId}" : ln.Code;
                            col.Item().Element(e => RowMoney(e, name, ln.RegistrationAmount, ci));
                        }
                    }

                    // 4) Raming kosten (alleen regels tonen met waarde > 0) – EXCL.
                    col.Item().Element(e => SectionBarGrey(e, "Raming kosten"));
                    AddLineIfPositive(col, "Aandeel basisakte", _vm.CostTotals.BaseDeedExcl, ci);
                    AddLineIfPositive(col, "Landmeter", _vm.CostTotals.SurveyorExcl, ci);
                    AddLineIfPositive(col, "Notariskosten (ereloon)", _vm.CostTotals.NotaryExcl, ci);
                    AddLineIfPositive(col, "Vaste aktekost", _vm.CostTotals.FixedActeExcl, ci);
                    AddLineIfPositive(col, "Hypotheekkantoor", _vm.CostTotals.MortgageExcl, ci);
                    AddLineIfPositive(col, "Aandeel verkavelingsakte", _vm.CostTotals.ParcelExcl, ci);

                    // 5) Raming aansluitkosten – totaal EXCL.
                    col.Item().Element(e => SectionBarGrey(e, "Raming aansluitkosten"));
                    col.Item().Element(e => RowMoney(e, "Aansluitkosten (excl. btw)", _vm.CostTotals.ConnectionExcl, ci));

                    // 6) BTW-blok
                    col.Item().Element(e => SectionBarGrey(e, "BTW"));

                    // 6a) per eenheid: BTW op bouwwaarde (voor Mixed of Vat)
                    bool vatOnBuildActive = _vm.RegistrationType != RegistrationType.Registration;
                    if (vatOnBuildActive)
                    {
                        foreach (var ln in _vm.Lines)
                        {
                            var vatOnBuild = Percent(ln.NetBuildBase, _vm.VatPercent);
                            if (vatOnBuild <= 0) continue;
                            var name = string.IsNullOrWhiteSpace(ln.Code) ? $"U{ln.UnitId}" : ln.Code;
                            col.Item().Element(e => RowMoney(e, $"{name} – btw op bouwwaarde", vatOnBuild, ci));
                        }
                    }

                    // 6b) over de (samengevatte) kosten
                    AddLineIfPositive(col, "BTW op aansluitkosten", _vm.CostTotals.ConnectionVat, ci);
                    AddLineIfPositive(col, "BTW op ereloon notaris", _vm.CostTotals.NotaryVat, ci);
                    AddLineIfPositive(col, "BTW op vaste aktekost", _vm.CostTotals.FixedActeVat, ci);
                    AddLineIfPositive(col, "BTW op aandeel verkavelingsakte", _vm.CostTotals.ParcelVat, ci);
                    AddLineIfPositive(col, "BTW op aandeel basisakte", _vm.CostTotals.BaseDeedVat, ci);
                    AddLineIfPositive(col, "BTW op landmeter", _vm.CostTotals.SurveyorVat, ci);

                    // 7) Groene eindregel: totaalprijs
                    col.Item().Element(e => SectionBarGreen(e, "Raming totaalprijs", _vm.GrandTotal, ci));

                    // 8) Nota
                    col.Item().PaddingTop(8).Text("Ereloon van de notaris, registratiekosten en btw zijn berekend op basis van de huidige tarifering.")
                                              .Italic().FontColor(Colors.Grey.Darken1);
                });

                //page.Footer().AlignRight().Text(txt =>
                //{
                //    txt.Span("Pagina ");
                //    txt.CurrentPageNumber();
                //    txt.Span(" / ");
                //    txt.TotalPages();
                //});
            });
        }

        // ========== Helpers ==========
        private static decimal Percent(decimal baseAmount, decimal pct) =>
            baseAmount <= 0 || pct <= 0 ? 0 : Math.Round(baseAmount * pct / 100m, 2);

        private static void AddLineIfPositive(ColumnDescriptor col, string label, decimal amount, CultureInfo ci)
        {
            if (amount > 0)
                col.Item().Element(e => RowMoney(e, label, amount, ci));
        }

        private static void SectionBarGreen(IContainer c, string text)
        {
            c.Background("#01532d").Padding(6).Row(r =>
            {
                r.RelativeItem().Text(text).FontColor(Colors.White).SemiBold();
            });
        }

        private static void SectionBarGreen(IContainer c, string text, decimal amount, CultureInfo ci)
        {
            c.Background("#01532d").Padding(6).Row(r =>
            {
                r.RelativeItem().Text(text).FontColor(Colors.White).SemiBold();
                r.ConstantItem(160).AlignRight().Text(amount.ToString("C", ci)).FontColor(Colors.White).SemiBold();
            });
        }

        private static void SectionBarGrey(IContainer c, string text)
        {
            c.Background(Colors.Grey.Lighten3).Padding(5).Row(r =>
            {
                r.RelativeItem().Text(text).FontColor(Colors.Black).SemiBold();
            });
        }

        private static void RowMoney(IContainer c, string label, decimal amount, CultureInfo ci)
        {
            c.PaddingVertical(3)
             .BorderBottom(1)
             .BorderColor(Colors.Grey.Lighten3)
             .Row(r =>
             {
                 r.RelativeItem().Text(label);
                 r.ConstantItem(160).AlignRight().Text(amount.ToString("C", ci)).SemiBold();
             });
        }
    }
}