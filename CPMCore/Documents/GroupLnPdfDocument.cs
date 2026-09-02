using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents
{
    /// <summary>
    /// Gedeelde look &amp; feel voor de werf-PDF's van Group LN (aannemerslijst, klantenlijst, ...):
    /// A4 liggend, groene huisstijl, kop met logo/wordmark, een PROJECTFICHE-raster en een voet
    /// met bedrijfsgegevens/paginanummering. Concrete documenten leveren enkel hun titel, ondertitel
    /// en inhoud (<see cref="Content"/>); kleuren, kop en voet komen uit deze basisklasse zodat nieuwe
    /// werf-PDF's er 1-op-1 hetzelfde uitzien.
    /// </summary>
    public abstract class GroupLnPdfDocument : IDocument
    {
        // Kleurtokens uit de Claude-Design-referentie (gedeeld door alle Group LN-werf-PDF's)
        protected const string Green900 = "#0d3a24";   // thead-achtergrond, bandrij-tekst
        protected const string Green700 = "#0f5132";   // koplijn, lot-tekst, vinkje
        protected const string GreenAcc = "#2f6b46";   // titelaccent, sectielabels, e-mail
        protected const string GreenSoft = "#e7eee8";  // bandrij-achtergrond
        protected const string Sand = "#cdb885";       // linkerrand legende, opsommingsteken
        protected const string Heading = "#36423a";    // titel, fiche-waarden, bedrijfsnaam
        protected const string Ink = "#2c322e";        // bodytekst, telefoon
        protected const string Muted = "#76807a";      // meta, subtekst, labels
        protected const string BorderCol = "#dde1d9";  // fiche-randen, scheidingslijnen
        protected const string RowLine = "#ecefe9";    // onderlijn tabelrijen
        protected const string GrpLine = "#c8d6cc";    // boven-/onderlijn bandrij
        protected const string HeadSep = "#356047";    // verticale scheiding in de kop
        protected const string Zebra = "#fafbf9";      // even datarijen
        protected const string No = "#c9cec6";         // streepje / n.v.t.
        protected const string BrandName = "#5b6660";  // "GROUP"
        protected const string BrandSub = "#97a09a";   // "PROJECTONTWIKKELING"

        // Group LN-gegevens (kunnen later uit een IssuerCompany komen).
        private const string LnName = "Group LN";
        private const string LnStreet = "Klaverdries 53";
        private const string LnCity = "9031 Drongen";
        private const string LnEmail = "info@groupln.be";

        protected readonly byte[] LogoBytes;
        protected readonly string FontFamilyName;
        protected readonly int Version;
        protected readonly CultureInfo Culture = CultureInfo.GetCultureInfo("nl-BE");

        protected GroupLnPdfDocument(byte[] logoBytes, string fontFamily, int version = 1)
        {
            LogoBytes = logoBytes;
            FontFamilyName = string.IsNullOrWhiteSpace(fontFamily) ? "Lato" : fontFamily;
            Version = version < 1 ? 1 : version;
        }

        public abstract DocumentMetadata GetMetadata();

        /// <summary>Titel bovenaan de kop en in de voet, bv. "Aannemerslijst" of "Klantenlijst".</summary>
        protected abstract string DocumentTitle { get; }

        /// <summary>Naam van de werf/het project, rechts uitgelijnd onder de titel in de kop.</summary>
        protected abstract string WerfTitel { get; }

        /// <summary>Derde regel in de kop, bv. "Opgemaakt dd/mm/jjjj · versie N · t.b.v. ...".</summary>
        protected abstract string HeaderMetaLine { get; }

        /// <summary>De eigenlijke pagina-inhoud (projectfiche, tabellen, legende, ...).</summary>
        protected abstract void Content(IContainer c);

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginVertical(26);
                page.MarginHorizontal(28);
                page.DefaultTextStyle(x => x.FontSize(7.4f).FontFamily(FontFamilyName).FontColor(Ink).LineHeight(1.45f));

                page.Header().Element(Header);
                page.Content().PaddingTop(14).Element(Content);
                page.Footer().PaddingTop(12).Element(Footer);
            });
        }

        // ── Kop ───────────────────────────────────────────────────────────────────
        private void Header(IContainer c)
        {
            c.Column(col =>
            {
                col.Item().PaddingBottom(9).Row(row =>
                {
                    row.AutoItem().AlignBottom().Row(b =>
                    {
                        if (LogoBytes is { Length: > 0 })
                            b.ConstantItem(26).AlignMiddle().Image(LogoBytes).FitWidth();
                        b.ConstantItem(11);
                        b.AutoItem().AlignMiddle().Column(t =>
                        {
                            t.Item().Text(x =>
                            {
                                x.Span("GROUP ").FontSize(19f).Black().FontColor(BrandName).LetterSpacing(0.02f);
                                x.Span("LN").FontSize(19f).Black().FontColor(GreenAcc).LetterSpacing(0.02f);
                            });
                            t.Item().PaddingTop(3).Text("PROJECTONTWIKKELING")
                                .FontSize(6.4f).Medium().FontColor(BrandSub).LetterSpacing(0.23f);
                        });
                    });

                    row.RelativeItem().PaddingLeft(24).AlignBottom().Column(r =>
                    {
                        r.Item().AlignRight().Text(DocumentTitle)
                            .FontSize(15.75f).Medium().FontColor(Heading).LetterSpacing(0.01f);
                        r.Item().PaddingTop(4).AlignRight().Text(WerfTitel)
                            .FontSize(8.5f).SemiBold().FontColor(GreenAcc).LetterSpacing(0.03f);
                        r.Item().PaddingTop(3).AlignRight().Text(HeaderMetaLine)
                            .FontSize(6.8f).FontColor(Muted).LetterSpacing(0.02f);
                    });
                });

                col.Item().LineHorizontal(2).LineColor(Green700);
            });
        }

        // ── Herbruikbare bouwstenen ───────────────────────────────────────────────
        protected void SectionLabel(IContainer c, string text, bool first = false)
        {
            c.PaddingTop(first ? 4 : 12).PaddingBottom(8).Row(row =>
            {
                row.AutoItem().Text(text.ToUpperInvariant())
                    .FontSize(6.8f).Bold().FontColor(GreenAcc).LetterSpacing(0.26f);
                row.ConstantItem(12);
                row.RelativeItem().AlignMiddle().LineHorizontal(1).LineColor(BorderCol);
            });
        }

        /// <summary>Het PROJECTFICHE-raster: N kolommen breed, zoveel rijen als nodig, met randen.</summary>
        protected void FicheGrid(IContainer c, (string Label, string Value, string Small)[] boxes, int columns = 4)
        {
            c.Border(0.75f).BorderColor(BorderCol).Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    for (int i = 0; i < columns; i++) d.RelativeColumn();
                });

                var rows = (int)Math.Ceiling(boxes.Length / (double)columns);
                for (int i = 0; i < boxes.Length; i++)
                {
                    var (label, value, small) = boxes[i];
                    var col = i % columns;
                    var row = i / columns;

                    IContainer cell = t.Cell().BorderColor(BorderCol);
                    if (col != columns - 1) cell = cell.BorderRight(0.75f);
                    if (row != rows - 1) cell = cell.BorderBottom(0.75f);
                    cell.PaddingVertical(8).PaddingHorizontal(11).Column(b =>
                    {
                        b.Item().Text(label.ToUpperInvariant())
                            .FontSize(6.4f).Bold().FontColor(Muted).LetterSpacing(0.15f);
                        b.Item().PaddingTop(3).Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                            .FontSize(8).SemiBold().FontColor(Heading).LineHeight(1.4f).WrapAnywhere();
                        if (!string.IsNullOrWhiteSpace(small))
                            b.Item().PaddingTop(1).Text(small).FontSize(7).FontColor(Muted).LineHeight(1.4f).WrapAnywhere();
                    });
                }
            });
        }

        /// <summary>Zet een telefoon-/gsm-nummer om naar het Belgisch leesformaat (04XX XX XX XX voor
        /// gsm, 0X XXX XX XX / 0XX XX XX XX voor vaste lijnen). Onherkenbare invoer blijft ongewijzigd.</summary>
        protected static string FormatBelgianPhone(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return raw.Trim();

            if (digits.StartsWith("0032")) digits = "0" + digits.Substring(4);
            else if (digits.StartsWith("32") && digits.Length > 9) digits = "0" + digits.Substring(2);
            if (!digits.StartsWith("0")) digits = "0" + digits;

            static string Group(string d, params int[] sizes)
            {
                var parts = new List<string>();
                var i = 0;
                foreach (var size in sizes)
                {
                    if (i >= d.Length) break;
                    parts.Add(d.Substring(i, Math.Min(size, d.Length - i)));
                    i += size;
                }
                return string.Join(" ", parts);
            }

            if (digits.Length == 10 && digits.StartsWith("04"))
                return Group(digits, 4, 2, 2, 2);                                  // GSM: 04XX XX XX XX
            if (digits.Length == 9 && (digits[1] is '2' or '3' or '4' or '9'))
                return Group(digits, 2, 3, 2, 2);                                  // vast, 1-cijferig zonenummer
            if (digits.Length == 9)
                return Group(digits, 3, 2, 2, 2);                                  // vast, 2-cijferig zonenummer

            return raw.Trim();
        }

        // ── Voet ──────────────────────────────────────────────────────────────────
        private void Footer(IContainer c)
        {
            c.Column(fc =>
            {
                fc.Item().LineHorizontal(1).LineColor(BorderCol);
                fc.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text($"{LnName} · {LnStreet}, {LnCity} · {LnEmail}")
                        .FontSize(6.6f).FontColor(Muted).LetterSpacing(0.034f);
                    row.RelativeItem().AlignMiddle().AlignCenter().Text(
                        $"{DocumentTitle} — {WerfTitel} · v{Version} · {DateTime.Now.ToString("dd/MM/yyyy", Culture)}")
                        .FontSize(6.6f).FontColor(Muted).LetterSpacing(0.034f);
                    row.RelativeItem().AlignMiddle().AlignRight().Text(t =>
                    {
                        t.Span("Vertrouwelijk — enkel voor projectbetrokkenen · ").FontSize(6.6f).FontColor(Muted).LetterSpacing(0.034f);
                        t.CurrentPageNumber().FontSize(6.6f).FontColor(Muted);
                        t.Span("/").FontSize(6.6f).FontColor(Muted);
                        t.TotalPages().FontSize(6.6f).FontColor(Muted);
                    });
                });
            });
        }
    }
}
