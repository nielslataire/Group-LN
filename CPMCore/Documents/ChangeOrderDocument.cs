using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents
{
    /// <summary>
    /// De wijzigingsopdracht als QuestPDF-document (ONDERTEKENEN_VOORTGANG.md, fase 1). Inhoudelijk
    /// een getrouwe overzetting van Views/Projecten/ChangeOrderPDF.cshtml (de Rotativa/wkhtmltopdf-
    /// versie): titel, project, klant, eenheden, opdracht, de regeltabel met dezelfde prijsformule
    /// (EH-prijs = prijs + commissie%, totaal = EH-prijs × hoeveelheid), opmerkingen, totalen excl./btw/
    /// incl. met het btw-% van het project, de akkoordvraag met vervaldatum en onderaan het blok
    /// Voorwaarden / Datum / Handtekening voor akkoord.
    ///
    /// Waarom QuestPDF en niet de bestaande Rotativa-view: (1) de signingmodule heeft een <c>byte[]</c>
    /// nodig vanuit een service, zonder MVC-<c>ControllerContext</c> of een wkhtmltopdf-binary op de
    /// server; (2) het definitieve ondertekende document en het auditrapport worden in fase 2 óók met
    /// QuestPDF gemaakt, dus één engine voor het hele dossier; (3) de andere nieuwe PDF's van Group LN
    /// (facturen, werf-lijsten) staan al op QuestPDF/<see cref="GroupLnPdfDocument"/>. De legacy actie
    /// <c>ProjectenController.ChangeOrderPDF</c> gebruikt sinds fase 1 ook dit document, zodat de klant
    /// op papier en digitaal exact hetzelfde stuk ziet. Staand A4, huisstijl van de basisklasse.
    /// </summary>
    public class ChangeOrderDocument : GroupLnPdfDocument
    {
        private readonly ChangeOrderPdfModel _m;

        public ChangeOrderDocument(ChangeOrderPdfModel model, byte[] logoBytes = null, string fontFamily = null, int version = 1)
            : base(logoBytes, fontFamily, version)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
        }

        public override DocumentMetadata GetMetadata() => new DocumentMetadata
        {
            Title = $"Wijzigingsopdracht {_m.Reference} - {_m.ProjectName}",
            Author = "Group LN",
            Subject = _m.Description
        };

        protected override PageSize PageSize => PageSizes.A4.Portrait();

        protected override string DocumentTitle => "Wijzigingsopdracht";

        protected override string WerfTitel => _m.ProjectName;

        protected override string HeaderMetaLine =>
            $"{_m.Reference} · opgemaakt {_m.Date.ToString("dd/MM/yyyy", Culture)} · geldig tot {_m.ExpirationDate.ToString("dd/MM/yyyy", Culture)}";

        protected override string FooterNote => _m.Reference;

        private string Euro(decimal value) => value.ToString("C", Culture);

        // ── Inhoud ────────────────────────────────────────────────────────────────
        protected override void Content(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().Element(x => SectionLabel(x, "Opdrachtfiche", first: true));
                col.Item().PaddingBottom(14).Element(Fiche);

                col.Item().Element(x => SectionLabel(x, "Opdracht"));
                col.Item().PaddingBottom(4).Text(string.IsNullOrWhiteSpace(_m.Description) ? "—" : _m.Description)
                    .FontSize(9.5f).SemiBold().FontColor(Heading).LineHeight(1.4f);

                col.Item().PaddingTop(10).Element(LinesTable);

                var comment = HtmlToPlainText(_m.CommentHtml);
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    col.Item().Element(x => SectionLabel(x, "Opmerkingen"));
                    col.Item().Text(comment).FontSize(8).FontColor(Ink).LineHeight(1.5f);
                }

                col.Item().PaddingTop(14).Element(Totals);

                col.Item().PaddingTop(16).Text(
                        $"Gelieve, indien u akkoord gaat, deze wijzigingsopdracht voor akkoord ondertekend terug te bezorgen tegen ten laatste {_m.ExpirationDate.ToString("dd/MM/yyyy", Culture)}.")
                    .FontSize(8.4f).SemiBold().FontColor(Heading).LineHeight(1.5f);

                col.Item().PaddingTop(18).Element(SignatureBlock);
            });
        }

        private void Fiche(IContainer c)
        {
            var boxes = new List<(string Label, string Value, string Small)>
            {
                ("Project", _m.ProjectName, string.Join(", ", new[] { _m.ProjectAddressLine, _m.ProjectCityLine }.Where(s => !string.IsNullOrWhiteSpace(s)))),
                ("Klant", string.Join(" ", new[] { _m.ClientSalutation, _m.ClientName }.Where(s => !string.IsNullOrWhiteSpace(s))), _m.UnitsLine),
                ("Referentie", _m.Reference, $"datum {_m.Date.ToString("dd/MM/yyyy", Culture)} · geldig tot {_m.ExpirationDate.ToString("dd/MM/yyyy", Culture)}"),
            };
            FicheGrid(c, boxes.ToArray(), columns: 3);
        }

        private void LinesTable(IContainer c)
        {
            c.Border(0.75f).BorderColor(BorderCol).Table(t =>
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
                        var cell = h.Cell().Background(Green900).PaddingVertical(6).PaddingHorizontal(7);
                        var txt = cell.Text(text.ToUpperInvariant()).FontSize(6.4f).Bold().FontColor("#ffffff").LetterSpacing(0.14f);
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
                        .Text("Geen regels.").Italic().FontColor(Muted);
                    return;
                }

                var zebra = false;
                foreach (var line in _m.Lines)
                {
                    zebra = !zebra;
                    var bg = zebra ? "#ffffff" : Zebra;
                    IContainer Cell() => t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(RowLine).PaddingVertical(5).PaddingHorizontal(7);

                    Cell().Text(line.Description ?? "").FontSize(8).FontColor(Ink).LineHeight(1.4f);
                    Cell().Text(line.UnitLabel ?? "—").FontSize(8).FontColor(Ink);
                    Cell().Text(line.TypeLabel ?? "—").FontSize(8).FontColor(Ink);
                    Cell().AlignRight().Text(line.Number.ToString("#,##0.##", Culture)).FontSize(8).FontColor(Ink);
                    Cell().AlignRight().Text(Euro(line.UnitPrice)).FontSize(8).FontColor(Ink);
                    Cell().AlignRight().Text(Euro(line.RowTotal)).FontSize(8).SemiBold().FontColor(Heading);
                }
            });
        }

        private void Totals(IContainer c)
        {
            c.AlignRight().Width(260).Column(col =>
            {
                void Row(string label, string value, bool grand = false)
                {
                    var item = col.Item().PaddingVertical(grand ? 6 : 3);
                    if (grand) item = item.BorderTop(1).BorderColor(BorderCol);
                    item.Row(r =>
                    {
                        r.RelativeItem().Text(label).FontSize(grand ? 8.6f : 8).FontColor(grand ? Heading : Ink).SemiBold();
                        r.ConstantItem(90).AlignRight().Text(value).FontSize(grand ? 8.6f : 8).FontColor(grand ? Green900 : Ink).Bold();
                    });
                }
                Row("Totaal excl. btw", Euro(_m.TotalExcl));
                Row($"Btw {_m.VatPercentage.ToString("0.##", Culture)} %", Euro(_m.VatAmount));
                Row("Totaal incl. btw", Euro(_m.TotalIncl), grand: true);
            });
        }

        /// <summary>Het papieren akkoordblok: Voorwaarden / Datum / Handtekening. Blijft ook in het
        /// digitale dossier staan — de elektronische handtekening komt in fase 2 als apart blad
        /// achter dit document, het brondocument zelf verandert nooit.</summary>
        private void SignatureBlock(IContainer c)
        {
            c.Border(0.75f).BorderColor(BorderCol).Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    d.RelativeColumn(4);
                    d.RelativeColumn(3);
                    d.RelativeColumn(3);
                });
                void Head(string text)
                {
                    t.Cell().Background(Green700).PaddingVertical(6).PaddingHorizontal(8)
                        .Text(text.ToUpperInvariant()).FontSize(6.4f).Bold().FontColor("#ffffff").LetterSpacing(0.14f);
                }
                Head("Voorwaarden");
                Head("Datum");
                Head("Handtekening voor akkoord");

                t.Cell().BorderRight(0.75f).BorderColor(BorderCol).MinHeight(56).PaddingVertical(6).PaddingHorizontal(8)
                    .Text(string.IsNullOrWhiteSpace(_m.Conditions) ? "" : _m.Conditions).FontSize(7.6f).FontColor(Ink).LineHeight(1.45f);
                t.Cell().BorderRight(0.75f).BorderColor(BorderCol).MinHeight(56).Text("");
                t.Cell().MinHeight(56).Text("");
            });
        }

        /// <summary>De opmerking wordt in CPM met een rich-text-editor bewaard (HTML); de PDF toont
        /// platte tekst met de regelstructuur (p/br/li) behouden. Geen externe HTML-parser nodig
        /// voor dit ene veld.</summary>
        internal static string HtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var s = Regex.Replace(html, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</\s*(p|div|li|h[1-6]|tr)\s*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<\s*li[^>]*>", "• ", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<[^>]+>", string.Empty);
            s = WebUtility.HtmlDecode(s);
            s = Regex.Replace(s, @"[ \t]+\n", "\n");
            s = Regex.Replace(s, @"\n{3,}", "\n\n");
            return s.Trim();
        }
    }

    /// <summary>Alles wat <see cref="ChangeOrderDocument"/> nodig heeft, los van EF/BO-klassen, zodat
    /// zowel de legacy afdrukactie als de signingbron (<c>ChangeOrderSigningSource</c>) het via
    /// <c>ChangeOrderPdfBuilder</c> uit dezelfde query vullen.</summary>
    public sealed class ChangeOrderPdfModel
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ClientAccountId { get; set; }
        public DateOnly Date { get; set; }
        public DateOnly ExpirationDate { get; set; }
        public string ProjectName { get; set; } = "";
        public string ProjectAddressLine { get; set; }
        public string ProjectCityLine { get; set; }
        public string ClientSalutation { get; set; }
        public string ClientName { get; set; } = "";
        public string ClientEmail { get; set; }
        public string UnitsLine { get; set; }
        public string Description { get; set; } = "";
        public string CommentHtml { get; set; }
        public string Conditions { get; set; }
        public decimal VatPercentage { get; set; }
        public List<ChangeOrderPdfLine> Lines { get; set; } = new();

        /// <summary>Zelfde nummering als de titel van de oude PDF ("WO {datum} - {id}"), kort genoteerd.</summary>
        public string Reference => $"WO-{Id}";

        /// <summary>Zelfde formule als ChangeOrderBO.Totaal: som van hoeveelheid × prijs × (1 + commissie).</summary>
        public decimal TotalExcl => Lines.Sum(l => l.RowTotal);
        public decimal VatAmount => VatPercentage * TotalExcl / 100m;
        public decimal TotalIncl => TotalExcl + VatAmount;
    }

    public sealed class ChangeOrderPdfLine
    {
        public string Description { get; set; }
        public string UnitLabel { get; set; }
        public string TypeLabel { get; set; }
        public int Number { get; set; }
        public decimal Price { get; set; }
        /// <summary>Commissie als percentage (bv. 20 voor 20 %), zoals ChangeOrderDetail.Commission.</summary>
        public decimal CommissionPercentage { get; set; }
        public decimal UnitPrice => Price + Price * CommissionPercentage / 100m;
        public decimal RowTotal => UnitPrice * Number;
    }
}
