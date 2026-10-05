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
        private string Euro(decimal value) => value.ToString("C", Culture);

        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                // "Body vult de ruimte op, blok hangt boven de voet" (35d): drie technieken geprobeerd
                // (ExtendVertical als spacer, ExtendVertical op Body, Extend().AlignBottom().ShowEntire()
                // op het laatste item), geen enkele werkt in deze QuestPDF-versie — zie
                // DOCUMENTLAYOUT_VOORTGANG.md voor het diagnose-bewijs per poging. Voorlopig gewoon een
                // vaste afstand na de totalen.
                col.Item().Element(Body);
                col.Item().PaddingTop(16).Element(VoorwaardenEnAkkoord);
            });
        }

        private void Body(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().PaddingBottom(14).Element(MetaAdres);

                col.Item().Element(x => SectionLabel(x, _m.IsQuote ? "Omschrijving" : "Opdracht", first: true));
                col.Item().PaddingBottom(4).Text(string.IsNullOrWhiteSpace(_m.Description) ? "—" : _m.Description)
                    .FontFamily(BodyFont).FontSize(8.5f).FontColor(GlV2PdfTheme.Inkt).LineHeight(1.5f);

                col.Item().PaddingTop(10).Element(LinesTable);

                var comment = ChangeOrderDocument.HtmlToPlainText(_m.CommentHtml);
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    col.Item().Element(x => SectionLabel(x, "Opmerkingen"));
                    col.Item().Text(comment).FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt).LineHeight(1.5f);
                }

                col.Item().PaddingTop(14).Element(Totals);

                if (_m.IsQuote)
                {
                    col.Item().PaddingTop(16).Text(
                            $"Deze offerte is geldig tot {_m.ExpirationDate.ToString("dd/MM/yyyy", Culture)}. Gaat u akkoord, laat het ons dan weten: u ontvangt daarna een wijzigingsopdracht ter ondertekening.")
                        .FontFamily(BodyFont).FontSize(8.4f).Bold().FontColor(GlV2PdfTheme.Inkt).LineHeight(1.5f);
                }
                else
                {
                    col.Item().PaddingTop(16).Text(
                            $"Gelieve, indien u akkoord gaat, deze wijzigingsopdracht voor akkoord ondertekend terug te bezorgen tegen ten laatste {_m.ExpirationDate.ToString("dd/MM/yyyy", Culture)}.")
                        .FontFamily(BodyFont).FontSize(8.4f).Bold().FontColor(GlV2PdfTheme.Inkt).LineHeight(1.5f);
                }
            });
        }

        /// <summary>Voorwaarden links, "Voor akkoord" rechts — twee kolommen naast elkaar, letterlijk
        /// zoals het offerte-voorbeeld (35d): "Voor akkoord" is geen sectielabel (niet groen/hoofdletters)
        /// maar gewone vette tekst, met "naam, datum en handtekening" als instructie erboven en het
        /// handtekenvak eronder.</summary>
        private void VoorwaardenEnAkkoord(IContainer c)
        {
            c.Row(row =>
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

        private void LinesTable(IContainer c)
        {
            c.Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    d.RelativeColumn(3.6f);   // Omschrijving
                    d.RelativeColumn(1.1f);   // Eenheid
                    d.RelativeColumn(1.0f);   // Type
                    d.RelativeColumn(0.9f);   // Hoev.
                    d.RelativeColumn(1.4f);   // EH-prijs
                    d.RelativeColumn(1.5f);   // Totaal
                });

                t.Header(h =>
                {
                    void Head(string text, bool right = false)
                    {
                        var cell = h.Cell().Background(GlV2PdfTheme.Groen).PaddingVertical(6).PaddingHorizontal(7);
                        var txt = cell.Text(text.ToUpperInvariant()).FontFamily(BodyFont).FontSize(6.5f).Bold().FontColor(GlV2PdfTheme.Wit).LetterSpacing(0.10f);
                        if (right) txt.AlignRight();
                    }
                    Head("Omschrijving");
                    Head("Eenheid");
                    Head("Type");
                    Head("Hoev.", right: true);
                    Head("EH-prijs", right: true);
                    Head("Totaal", right: true);
                });

                if (_m.Lines.Count == 0)
                {
                    t.Cell().ColumnSpan(6).PaddingVertical(8).PaddingHorizontal(7)
                        .Text("Geen regels.").Italic().FontColor(GlV2PdfTheme.Licht);
                    return;
                }

                foreach (var line in _m.Lines)
                {
                    IContainer Cell() => t.Cell().BorderBottom(0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn).PaddingVertical(5).PaddingHorizontal(7);

                    Cell().Text(line.Description ?? "").FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt).LineHeight(1.4f);
                    Cell().Text(line.UnitLabel ?? "—").FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt);
                    Cell().Text(line.TypeLabel ?? "—").FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt);
                    Cell().AlignRight().Text(line.Number.ToString("#,##0.##", Culture)).FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt)
                        .EnableFontFeature(FontFeatures.TabularFigures);
                    Cell().AlignRight().Text(Euro(line.UnitPrice)).FontFamily(BodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt)
                        .EnableFontFeature(FontFeatures.TabularFigures);
                    Cell().AlignRight().Text(Euro(line.RowTotal)).FontFamily(BodyFont).FontSize(8).Bold().FontColor(GlV2PdfTheme.Inkt)
                        .EnableFontFeature(FontFeatures.TabularFigures);
                }
            });
        }

        private void Totals(IContainer c)
        {
            GlV2PdfComponents.Totalenblok(c, BodyFont,
                rows: new[]
                {
                    ("Totaal excl. btw", Euro(_m.TotalExcl)),
                    ($"Btw {_m.VatPercentage.ToString("0.##", Culture)} %", Euro(_m.VatAmount)),
                },
                grandTotal: ("Totaal incl. btw", Euro(_m.TotalIncl)));
        }
    }
}
