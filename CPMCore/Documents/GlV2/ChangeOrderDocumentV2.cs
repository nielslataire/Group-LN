using System;
using System.Collections.Generic;
using System.Linq;
using CPMCore.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2
{
    /// <summary>
    /// Offerte/wijzigingsopdracht in de nieuwe gl-v2-huisstijl (design-handoff punt 35c "Wijzigings-
    /// opdracht" / 35d "Offerte"). Eerste document van de gl-v2-documentlayout
    /// (DOCUMENTLAYOUT_VOORTGANG.md); gebruikt hetzelfde datamodel (<see cref="ChangeOrderPdfModel"/>)
    /// als de bestaande <see cref="ChangeOrderDocument"/>, enkel de opmaak is nieuw — zo blijft
    /// <c>ChangeOrderPdfBuilder.LoadAsync</c> gedeeld en verandert er niets aan wat een niet-gl-v2
    /// sessie te zien krijgt (<c>ChangeOrderPdfBuilder.Render</c> kiest op <c>UseGlV2Layout</c>).
    ///
    /// Twee bewuste afwijkingen van de ontwerptekst, door wat <see cref="ChangeOrderPdfModel"/>
    /// vandaag draagt (zie DOCUMENTLAYOUT_VOORTGANG.md "Bekende afwijkingen"):
    /// (1) de offerte-tabel heeft geen postnummer/groepscodes (35d) — de regels komen plat uit
    /// <c>ChangeOrderDetail</c>, zonder budget-activiteitcode of sectie-groepering, dus offerte en
    /// wijzigingsopdracht delen hier dezelfde kolomopbouw (35c);
    /// (2) "VOOR AKKOORD" is één handtekenvak voor de klant — er is geen lijst van mede-eigenaars in
    /// dit model, dus geen vak per eigenaar zoals 35c voor meerdere eigenaars beschrijft.
    /// </summary>
    public sealed class ChangeOrderDocumentV2 : GlV2PdfDocumentBase
    {
        private readonly ChangeOrderPdfModel _m;

        public ChangeOrderDocumentV2(ChangeOrderPdfModel model, GlV2PdfCompanyInfo company, bool fontsAvailable)
            : base(company, fontsAvailable)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
        }

        public override DocumentMetadata GetMetadata() => new()
        {
            Title = $"{DocumentTitle} {_m.Reference} - {_m.ProjectName}",
            Author = Company.Name,
            Subject = _m.Description,
        };

        protected override GlV2PdfFormat Format => GlV2PdfFormat.A4Staand;
        protected override string DocumentTitle => _m.IsQuote ? "Offerte" : "Wijzigingsopdracht";
        // Punt 35c geeft "MEERWERK" als kicker voor de wijzigingsopdracht; Niels bevestigde dezelfde
        // kicker voor de offerte (2026-10-05) — beide documenten gaan over meerwerken op het project.
        protected override string? Kicker => "Meerwerk";
        protected override string? DocumentNumber => _m.Reference;
        protected override string? CompactSubtitle => _m.ProjectName;

        private static readonly System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");
        private static string Euro(decimal value) => GlV2PdfComponents.Euro(value);

        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                // "Body vult de ruimte op, blok hangt boven de voet" (35d): Extend().AlignBottom().ShowEntire()
                // staat op het column-item dat de volledige Row bevat (niet op een item binnen de Row);
                // binnen de Row staat Voorwaarden bovenaan, op dezelfde hoogte als "Voor akkoord" (zoals 35d). Zie DOCUMENTLAYOUT_VOORTGANG.md.
                col.Item().Element(Body);
                col.Item().Extend().AlignBottom().ShowEntire().Element(_m.IsQuote ? VoorwaardenEnAkkoord : VoorwaardenEnAkkoordWijziging);
            });
        }

        /// <summary>Onderwerp (9pt 600 Inkt) + inleiding (8pt/1,5 Gedempt), 1,5mm ertussen (35d). Leeg veld = regel weggelaten.</summary>
        private void OnderwerpEnInleiding(IContainer c)
        {
            c.Column(col =>
            {
                col.Spacing(1.5f, Unit.Millimetre);
                if (!string.IsNullOrWhiteSpace(_m.Subject))
                    col.Item().Text(_m.Subject).FontFamily(BodyFont).FontSize(9).SemiBold().FontColor(GlV2PdfTheme.Inkt);
                if (!string.IsNullOrWhiteSpace(_m.Description))
                    col.Item().Text(_m.Description).FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Gedempt).LineHeight(1.5f);
            });
        }

        private void Body(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().PaddingBottom(14).Element(MetaAdres);

                col.Item().PaddingBottom(4).Element(OnderwerpEnInleiding);

                col.Item().PaddingTop(10).Element(LinesTable);

                var comment = ChangeOrderDocument.HtmlToPlainText(_m.CommentHtml);
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    col.Item().Element(x => SectionLabel(x, "Opmerkingen"));
                    col.Item().Text(comment).FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt).LineHeight(1.5f);
                }

                col.Item().PaddingTop(10).Element(Totals);

                // "Bijkomende vermelding" (35b/35d): het veld "Extra factuurinfo" van de klantenaccount, onder het totaalblok.
                if (!string.IsNullOrWhiteSpace(_m.ClientInvoiceExtra))
                {
                    col.Item().PaddingTop(6, Unit.Millimetre).Column(extra =>
                    {
                        extra.Spacing(1.4f, Unit.Millimetre);
                        extra.Item().Text("BIJKOMENDE VERMELDING").FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.16f).FontColor(GlV2PdfTheme.Groen);
                        extra.Item().Text(_m.ClientInvoiceExtra.Trim()).FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt).LineHeight(1.5f);
                    });
                }

                if (!_m.IsQuote && _m.Terms.Count > 0)
                    col.Item().PaddingTop(7, Unit.Millimetre).Element(Facturatieplan);
            });
        }

        /// <summary>Voorwaarden links, "Voor akkoord" rechts — twee kolommen naast elkaar, letterlijk
        /// zoals het offerte-voorbeeld (35d): "Voor akkoord" is geen sectielabel (niet groen/hoofdletters)
        /// maar gewone vette tekst, met "naam, datum en handtekening" als instructie erboven en het
        /// handtekenvak eronder.</summary>
        private void VoorwaardenEnAkkoord(IContainer c)
        {
            c.PaddingBottom(4, Unit.Millimetre).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    if (string.IsNullOrWhiteSpace(_m.Conditions)) return;
                    col.Item().Text("VOORWAARDEN").FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.14f).FontColor(GlV2PdfTheme.Groen);
                    col.Item().PaddingTop(2).Text(_m.Conditions).FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt).LineHeight(1.55f);
                });
                row.ConstantItem(10, Unit.Millimetre);
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Voor akkoord").FontFamily(BodyFont).FontSize(7.5f).SemiBold().FontColor(GlV2PdfTheme.Inkt);
                    col.Item().PaddingTop(1.2f, Unit.Millimetre).Text("naam, datum en handtekening").FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt);
                    col.Item().PaddingTop(1, Unit.Millimetre).Element(x => GlV2PdfComponents.Handtekeningvak(x, BodyFont, "handtekening · datum"));
                });
            });
        }

        /// <summary>Facturatieplan (35i): per termijn "<b>Voorschot</b> — na ondertekening", percentage en bedrag incl. btw
        /// (het bedrag excl. btw wordt bewust niet vermeld — Niels, 2026-10-06). Rijen 8pt, 1,7mm, fijne lijn.</summary>
        private void Facturatieplan(IContainer c)
        {
            c.Column(col =>
            {
                col.Spacing(2, Unit.Millimetre);
                col.Item().Text("FACTURATIEPLAN").FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.14f).FontColor(GlV2PdfTheme.Groen);
                col.Item().Column(rows =>
                {
                    foreach (var t in _m.Terms)
                    {
                        rows.Item().ShowEntire().BorderBottom(0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn)
                            .PaddingVertical(1.7f, Unit.Millimetre).Row(r =>
                            {
                                r.Spacing(3, Unit.Millimetre);
                                r.RelativeItem().Text(x =>
                                {
                                    x.DefaultTextStyle(st => st.FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt));
                                    x.Span(t.Label).Bold();
                                    if (!string.IsNullOrWhiteSpace(t.TriggerText)) x.Span(" — " + t.TriggerText);
                                });
                                r.ConstantItem(16, Unit.Millimetre).AlignRight().Text(t.Percentage.HasValue ? GlV2PdfComponents.Btw(t.Percentage.Value) : "")
                                    .FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                                r.ConstantItem(26, Unit.Millimetre).AlignRight().Text(Euro(_m.TermAmountIncl(t)))
                                    .FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                            });
                    }
                });
                col.Item().Text("bedragen incl. btw").FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Licht);
            });
        }

        /// <summary>Wijzigingsopdracht (35i): eventuele voorwaarden, dan "VOOR AKKOORD" met de ondertekeningstekst en een
        /// handtekenvak per eigenaar die moet tekenen (naam + "eigenaar · x %"). Is er geen vast aantal (ondertekenregel "één
        /// volstaat" of geen eigenaars gekend), dan één vak zonder naam en percentage.</summary>
        private void VoorwaardenEnAkkoordWijziging(IContainer c)
        {
            var specific = _m.AllSignersRequired && _m.Signers.Count > 0;
            var manyOwners = specific && _m.Signers.Count > 1;
            var text = "Door te ondertekenen gaat u akkoord met de uitvoering en de facturatie zoals hierboven beschreven. "
                + "Ondertekenen kan bij voorkeur via de link die u in de mail ontvangen heeft, indien gewenst kan u dit ook via mail ondertekend naar ons terugsturen."
                + (manyOwners ? " Iedere vermelde eigenaar dient te ondertekenen." : "");

            c.PaddingBottom(4, Unit.Millimetre).Column(col =>
            {
                col.Spacing(3, Unit.Millimetre);

                if (!string.IsNullOrWhiteSpace(_m.Conditions))
                    col.Item().Column(v =>
                    {
                        v.Spacing(1.5f, Unit.Millimetre);
                        v.Item().Text("VOORWAARDEN").FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.14f).FontColor(GlV2PdfTheme.Groen);
                        v.Item().Text(_m.Conditions).FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt).LineHeight(1.55f);
                    });

                col.Item().Text("VOOR AKKOORD").FontFamily(BodyFont).FontSize(6.5f).Bold().LetterSpacing(0.14f).FontColor(GlV2PdfTheme.Groen);
                col.Item().Text(text).FontFamily(BodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt).LineHeight(1.5f);

                col.Item().Grid(grid =>
                {
                    grid.Columns(12);
                    grid.HorizontalSpacing(6, Unit.Millimetre);
                    grid.VerticalSpacing(3, Unit.Millimetre);
                    if (!specific)
                    {
                        grid.Item(6).Element(x => GlV2PdfComponents.Handtekeningvak(x, BodyFont, "handtekening · datum"));
                        return;
                    }
                    foreach (var s in _m.Signers)
                    {
                        grid.Item(6).ShowEntire().Column(o =>
                        {
                            o.Spacing(1.2f, Unit.Millimetre);
                            o.Item().Text(s.Name).FontFamily(BodyFont).FontSize(7.5f).SemiBold().FontColor(GlV2PdfTheme.Inkt);
                            o.Item().Text("eigenaar" + (manyOwners && s.Percentage.HasValue ? " · " + GlV2PdfComponents.Btw(s.Percentage.Value) : ""))
                                .FontFamily(BodyFont).FontSize(6.5f).FontColor(GlV2PdfTheme.Gedempt);
                            o.Item().Element(x => GlV2PdfComponents.Handtekeningvak(x, BodyFont, "handtekening · datum"));
                        });
                    }
                });
            });
        }

        /// <summary>Links Datum/Geldig tot/Werfadres/Eenheid, rechts de klant met zijn eigen adres
        /// (punt 35a "ADRES &amp; META", 35d; Niels, 2026-10-05: Werfadres toont straat/nummer/gemeente
        /// zonder postcode, Referentie is vervangen door Eenheid, en rechts staat nu het klantadres
        /// i.p.v. de eenheid — <see cref="ChangeOrderPdfBuilder"/> laadt dat sinds vandaag mee).</summary>
        private void MetaAdres(IContainer c)
        {
            var rows = new List<(string Label, string Value, bool Highlight)>
            {
                ("Datum", _m.Date.ToString("dd/MM/yyyy", Culture), false),
                ("Geldig tot", _m.ExpirationDate.ToString("dd/MM/yyyy", Culture), true),
                ("Werfadres", string.Join(", ", new[] { _m.ProjectAddressLine, _m.ProjectMunicipality }.Where(s => !string.IsNullOrWhiteSpace(s))), false),
                ("Eenheid", _m.UnitsLine, false),
            };
            var recipientName = string.Join(" ", new[] { _m.ClientSalutation, _m.ClientName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var recipientLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(_m.ClientStreetLine)) recipientLines.Add(_m.ClientStreetLine);
            if (!string.IsNullOrWhiteSpace(_m.ClientCityLine)) recipientLines.Add(_m.ClientCityLine);
            GlV2PdfComponents.MetaAdres(c, BodyFont, rows, recipientName, recipientLines);
        }

        /// <summary>Regels in de standaard-typetabel (<see cref="GlV2PdfComponents.Tabel"/>, 35d) zonder de
        /// kolom Code; groepsrijen en subtotalen komen later.</summary>
        private void LinesTable(IContainer c)
        {
            string VatKleur(decimal rate) => rate == 6m ? GlV2PdfTheme.VlakGroepsrij : GlV2PdfTheme.VlakWarm;

            GlV2PdfComponents.Tabel(c, BodyFont,
                new[]
                {
                    new GlV2PdfComponents.TabelKolom("Omschrijving"),
                    new GlV2PdfComponents.TabelKolom("Eenh.", 12),
                    new GlV2PdfComponents.TabelKolom("Hoev.", 14, Rechts: true),
                    new GlV2PdfComponents.TabelKolom("Eenheidspr.", 22, Rechts: true),
                    new GlV2PdfComponents.TabelKolom("Btw", 12, Rechts: true),
                    new GlV2PdfComponents.TabelKolom("Totaal", 24, Rechts: true),
                },
                _m.Lines.Select(l => (IReadOnlyList<GlV2PdfComponents.TabelCel>)new[]
                {
                    // l.VatPercentage ?? ... : zelfde terugval als ChangeOrderPdfModel.VatBreakdown
                    new GlV2PdfComponents.TabelCel(l.Description ?? ""),
                    new GlV2PdfComponents.TabelCel(l.UnitLabel ?? ""),
                    new GlV2PdfComponents.TabelCel(l.Number.ToString("#,##0.##", Culture)),
                    new GlV2PdfComponents.TabelCel(Euro(l.UnitPrice)),
                    new GlV2PdfComponents.TabelCel(GlV2PdfComponents.Btw(l.VatPercentage ?? _m.VatPercentage), VatKleur(l.VatPercentage ?? _m.VatPercentage)),
                    new GlV2PdfComponents.TabelCel(Euro(l.RowTotal)),
                }));
        }

        private void Totals(IContainer c) =>
            GlV2PdfComponents.TotalenBtw(c, BodyFont, _m.VatBreakdown, _m.TotalExcl, _m.VatTotalByLines, _m.TotalInclByLines, _m.VatMentions);
    }
}
