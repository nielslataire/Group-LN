using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ServiceCore.Invoicing.Pdf;

namespace CPMCore.Documents.GlV2
{
    /// <summary>
    /// Factuur in de gl-v2-documentlayout (design-handoff 35b "Factuur · A4 staand") — de opmaak van "layoutA" (Niels, 2026-10-07; layoutB en
    /// de overige blijven de JSON-layouts van <c>ServiceCore/Invoicing/Pdf</c>). Zelfde bouwstenen als offerte/wijzigingsopdracht
    /// (<see cref="GlV2PdfDocumentBase"/>, <see cref="GlV2PdfComponents"/>): kop met logo, meta + adres, BESCHRIJVING, typetabel met groepen per
    /// soort detaillijn (eenheid/schijven, wijzigingsopdrachten per WO, overige), BTW-vermelding + btw-overzicht + "Te betalen", bijkomende vermelding,
    /// betaalblok met QR tegen de voet. Het datamodel is het bestaande <see cref="InvoiceVm"/> — geen eigen queries.
    /// </summary>
    public sealed class InvoiceDocumentV2 : GlV2PdfDocumentBase
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("nl-BE");
        private readonly InvoiceVm _vm;
        private readonly TemplateContext _ctx;
        private readonly bool _proforma;
        private readonly bool _creditNote;
        private readonly string _accent;

        public InvoiceDocumentV2(InvoiceVm vm, TemplateContext ctx, GlV2PdfCompanyInfo company, bool fontsAvailable)
            : base(company, fontsAvailable)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            var status = _vm.Invoice.Status?.Trim();
            _proforma = string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "Concept", StringComparison.OrdinalIgnoreCase)
                        || string.IsNullOrWhiteSpace(_vm.Invoice.PublicId);
            // Zelfde regel als de bestaande layouts (DefaultFooterRenderer.IsCreditNote): negatief totaal of een status met "credit".
            // Huiskleur van het uitgevende bedrijf (instellingen → factuurkleuren, BrandPrimaryColor; zonder ingesteld = het standaardgroen).
            _accent = GlV2PdfTheme.IsHex(_ctx.PrimaryColorHex) ? _ctx.PrimaryColorHex!.Trim() : GlV2PdfTheme.Groen;
            _creditNote = _vm.Totals.Incl < 0m || (!string.IsNullOrWhiteSpace(status) && status.Contains("credit", StringComparison.OrdinalIgnoreCase));
        }

        public override DocumentMetadata GetMetadata() => new()
        {
            Title = $"Factuur {_vm.Invoice.PublicId}".Trim(),
            Author = Company.Name,
            Subject = _vm.HeaderDescription,
        };

        protected override string Accent => _accent;

        protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Staand;
        // Titel = type van het document (Factuur / Creditnota / Proforma factuur), zelfde label als de rij "Type" in de meta.
        protected override string DocumentTitle => TypeLabel();
        // 35b: kicker "VERKOOP". Dat het een proforma is, staat nu in de titel; zonder nummer valt het nummer weg.
        protected override string? Kicker => "VERKOOP";
        protected override string? DocumentNumber => string.IsNullOrWhiteSpace(_vm.Invoice.PublicId) ? null : _vm.Invoice.PublicId;
        protected override string? CompactSubtitle => _vm.Project.Name;

        private static string Euro(decimal v) => GlV2PdfComponents.Euro(v);
        private static string Datum(DateOnly d) => d.ToString("dd/MM/yyyy", Culture);

        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Element(Body);
                // Het betaalblok hangt tegen de voet (36 · 7 "Lege ruimte": tabel groeit naar beneden, het onderste blok staat tegen de voet);
                // het splitst nooit over twee pagina's.
                col.Item().Extend().AlignBottom().ShowEntire().PaddingBottom(6, Unit.Millimetre).Element(Betaling);
            });
        }

        private void Body(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().PaddingBottom(14).Element(MetaAdres);

                if (!string.IsNullOrWhiteSpace(_vm.HeaderDescription) || !string.IsNullOrWhiteSpace(_vm.DetailDescription))
                    col.Item().PaddingBottom(4).Element(Beschrijving);

                col.Item().PaddingTop(10).Element(Lijnen);

                col.Item().PaddingTop(10).Element(Totalen);

                if (!string.IsNullOrWhiteSpace(_vm.ExtraInfo))
                {
                    col.Item().PaddingTop(5, Unit.Millimetre).Column(extra =>
                    {
                        extra.Spacing(1.4f, Unit.Millimetre);
                        extra.Item().Text("BIJKOMENDE VERMELDING").FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.16f).FontColor(_accent);
                        extra.Item().Text(_vm.ExtraInfo!.Trim()).FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt).LineHeight(1.5f);
                    });
                }
            });
        }

        /// <summary>Type van het document: "Factuur", "Creditnota" of "Proforma factuur" (Niels, 2026-10-07) — niet meer welke soorten lijnen erop staan.</summary>
        private string TypeLabel() => _proforma ? (_creditNote ? "Proforma creditnota" : "Proforma factuur") : _creditNote ? "Creditnota" : "Factuur";

        private void MetaAdres(IContainer c)
        {
            var due = _vm.Payment.UsePaymentTermsText && !string.IsNullOrWhiteSpace(_vm.Payment.Terms)
                ? _vm.Payment.Terms!
                : _vm.Invoice.DueDate is DateOnly d ? Datum(d) : "—";
            var number = string.IsNullOrWhiteSpace(_vm.Invoice.PublicId) ? "—" : _vm.Invoice.PublicId!;
            var rows = new List<(string Label, string Value, bool Highlight)>
            {
                ("Type", TypeLabel(), false),
                (_creditNote ? "Creditnotanummer" : "Factuurnummer", number, false),
                (_creditNote ? "Datum" : "Factuurdatum", Datum(_vm.Invoice.IssueDate), false),
                ("Vervaldatum", due, false),
            };
            if (!string.IsNullOrWhiteSpace(_vm.Project.Name)) rows.Add(("Project", _vm.Project.Name!, false));
            if (!string.IsNullOrWhiteSpace(_vm.Unit.Name)) rows.Add(("Eenheid", _vm.Unit.Name!, false));

            var name = !string.IsNullOrWhiteSpace(_vm.Client.LegalName) ? _vm.Client.LegalName! : _vm.Client.Name ?? "";
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(_vm.Client.AddressLine)) lines.Add(_vm.Client.AddressLine!);
            var city = string.Join(" ", new[] { _vm.Client.Postal, _vm.Client.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (city != "") lines.Add(city);
            if (!string.IsNullOrWhiteSpace(_vm.Client.VAT)) lines.Add(_vm.Client.VAT!);
            GlV2PdfComponents.MetaAdres(c, BodyFont, rows, name.ToUpperInvariant(), lines);
        }

        /// <summary>BESCHRIJVING (35b): lopende tekst, daarna de slotregel(s) vet voor bedrag of basis.</summary>
        private void Beschrijving(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Element(x => SectionLabel(x, "Beschrijving", first: true));
                if (!string.IsNullOrWhiteSpace(_vm.HeaderDescription))
                    col.Item().Text(_vm.HeaderDescription!.Trim()).FontFamily(BodyFont).FontSize(8.5f).FontColor(GlV2PdfTheme.Inkt).LineHeight(1.5f);
                if (!string.IsNullOrWhiteSpace(_vm.DetailDescription))
                    col.Item().PaddingTop(1.5f, Unit.Millimetre).Text(_vm.DetailDescription!.Trim()).FontFamily(BodyFont).FontSize(8.5f).Bold()
                        .FontColor(GlV2PdfTheme.Inkt).LineHeight(1.5f);
            });
        }

        /// <summary>De typetabel (35b): kolommen 1fr · 14 · 22 · 12 · 24 mm; groepen per soort detaillijn (eenheid/schijven, per wijzigingsopdracht, overige).</summary>
        private void Lijnen(IContainer c)
        {
            var regels = new List<GlV2PdfComponents.TabelRegel>();
            var groepen = _vm.Lines.GroupBy(GroepSleutel).ToList();
            foreach (var groep in groepen)
            {
                var eerste = groep.First();
                // Eén enkele soort/groep in de tabel: de groepskop zegt niets extra en valt weg (Niels, 2026-10-07).
                if (groepen.Count > 1) regels.Add(GlV2PdfComponents.TabelRegel.Groep(GroepTitel(eerste)));
                foreach (var l in groep)
                {
                    var qty = l.Quantity == 0m ? 1m : l.Quantity;
                    var unit = l.UnitPrice == 0m ? l.Total / qty : l.UnitPrice;
                    var pill = l.Vat == 6m ? (_accent == GlV2PdfTheme.Groen ? GlV2PdfTheme.VlakGroepsrij : GlV2PdfTheme.Tint(_accent, 0.09)) : GlV2PdfTheme.VlakWarm;
                    regels.Add(GlV2PdfComponents.TabelRegel.Rij(new[]
                    {
                        new GlV2PdfComponents.TabelCel(l.Description ?? ""),
                        new GlV2PdfComponents.TabelCel(qty.ToString("0.##", Culture)),
                        new GlV2PdfComponents.TabelCel(Euro(unit)),
                        new GlV2PdfComponents.TabelCel(GlV2PdfComponents.Btw(l.Vat), pill),
                        new GlV2PdfComponents.TabelCel(Euro(l.Total)),
                    }));
                }
            }

            GlV2PdfComponents.Tabel(c, BodyFont,
                new[]
                {
                    new GlV2PdfComponents.TabelKolom("Omschrijving"),
                    new GlV2PdfComponents.TabelKolom("Aantal", 14, Rechts: true),
                    new GlV2PdfComponents.TabelKolom("Eenheidsprijs", 22, Rechts: true),
                    new GlV2PdfComponents.TabelKolom("Btw", 12, Rechts: true),
                    new GlV2PdfComponents.TabelKolom("Totaal excl.", 24, Rechts: true),
                },
                regels, accent: _accent);
        }

        private static string GroepSleutel(InvoiceLineVm l)
        {
            var type = l.LineType?.Trim();
            if (string.Equals(type, "Stages", StringComparison.OrdinalIgnoreCase)) return "S:" + (l.GroupName ?? "");
            if (string.Equals(type, "ChangeOrders", StringComparison.OrdinalIgnoreCase)) return "C:" + (l.GroupName ?? "");
            return "O:";
        }

        private static string GroepTitel(InvoiceLineVm l)
        {
            var type = l.LineType?.Trim();
            if (string.Equals(type, "Stages", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(l.GroupName) ? "Schijven" : l.GroupName!.Trim();
            if (string.Equals(type, "ChangeOrders", StringComparison.OrdinalIgnoreCase))
            {
                // Nieuwe facturen dragen "WO-2026-001 · onderwerp" als GroupName; oudere enkel "Wijzigingsopdrachten".
                var g = l.GroupName?.Trim();
                return string.IsNullOrWhiteSpace(g) || string.Equals(g, "Wijzigingsopdrachten", StringComparison.OrdinalIgnoreCase)
                    ? "Wijzigingsopdrachten" : "Wijzigingsopdracht — " + g;
            }
            return "Overige";
        }

        private void Totalen(IContainer c)
        {
            var tarieven = _vm.VatSummary.OrderBy(v => v.Rate).Select(v => (v.Rate, Base: v.Net, v.Vat)).ToList();
            GlV2PdfComponents.TotalenBtw(c, BodyFont, tarieven, _vm.Totals.Excl, _vm.Totals.Vat, _vm.Totals.Incl, _vm.VatMentionsByRate, eindLabel: "Te betalen", accent: _accent);
        }

        private void Betaling(IContainer c)
        {
            var beneficiary = string.Join(" ", new[] { _vm.IssuerCompany.LegalName ?? _vm.IssuerCompany.Name, _vm.IssuerCompany.LegalFormAbbreviation }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            var due = _vm.Payment.UsePaymentTermsText && !string.IsNullOrWhiteSpace(_vm.Payment.Terms)
                ? _vm.Payment.Terms
                : _vm.Invoice.DueDate is DateOnly d ? Datum(d) : null;
            var iban = FormatIban(_vm.Payment.Iban);

            var velden = new List<(string, string?, bool)>
            {
                ("Bedrag", Euro(_vm.Totals.Incl), true),
                ("Vóór", _proforma ? null : due, true),
                ("Begunstigde", beneficiary, false),
                ("IBAN", iban, true),
                ("BIC", _vm.Payment.Bic, false),
                ("Mededeling", _proforma ? null : _vm.Payment.Structured, true),
            };
            var toelichting = _proforma
                ? "Proforma factuur — dit is nog geen definitieve factuur en nog niet te betalen."
                : !string.IsNullOrWhiteSpace(_vm.Payment.Structured)
                    ? "Vermeld de gestructureerde mededeling, zo wordt je betaling automatisch gekoppeld."
                    : null;
            GlV2PdfComponents.Betaalblok(c, BodyFont, velden, toelichting, _proforma ? null : _ctx.EpcQrPng, _accent);
        }

        /// <summary>IBAN in groepen van 4 (36 · 7 "Data en nummers").</summary>
        private static string? FormatIban(string? iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return null;
            var compact = new string(iban.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
            return string.Join(" ", Enumerable.Range(0, (compact.Length + 3) / 4).Select(i => compact.Substring(i * 4, Math.Min(4, compact.Length - i * 4))));
        }
    }
}
