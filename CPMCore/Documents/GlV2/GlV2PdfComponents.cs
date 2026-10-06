using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2
{
    /// <summary>Gedeelde, projectwijde bouwstenen uit design-handoff punt 36.5 ("Bouwstenen") die niet
    /// aan één documenttype gebonden zijn. Ontbreken hier nog (volgen bij factuur/lijsten):
    /// btw-overzicht, betaalblok, projectfiche-rooster voor lijsten. <see cref="GlV2PdfDocumentBase"/>
    /// draagt de kop/voet en <c>SectionLabel</c> al zelf.</summary>
    public static class GlV2PdfComponents
    {
        private static readonly System.Globalization.CultureInfo NlBe = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");

        /// <summary>Bedrag in de nl-BE-standaard ("€ 1.149,60", negatief "€ -640,00") — Niels (2026-10-06):
        /// bewust niet het "– € 640,00" van het ontwerp.</summary>
        public static string Euro(decimal value) => value.ToString("C", NlBe);

        /// <summary>Btw-tarief met spatie: "6 %", "21 %" (36 · 7 Regels).</summary>
        public static string Btw(decimal rate) => rate.ToString("0.##", NlBe) + " %";

        /// <summary>Kolom van de standaard-typetabel (<see cref="Tabel"/>). <c>BreedteMm == null</c> = de
        /// kolom neemt de resterende breedte (hoogstens één per tabel).</summary>
        public record TabelKolom(string Kop, float? BreedteMm = null, bool Rechts = false);

        /// <summary>Cel van de typetabel. Met <c>PilKleur</c> wordt de tekst als tag getoond (btw-tarief,
        /// status), zoals de btw-tag in 35d.</summary>
        public record TabelCel(string Tekst, string? PilKleur = null);

        /// <summary>De standaard-typetabel voor alle gl-v2-PDF's (35d): kop zonder vlak met enkel een
        /// groene lijn eronder (6.5pt 700, letterspatiëring .1em), rijen 8pt met fijne scheidingslijn en
        /// 1.7mm verticale padding, kolomafstand 3mm. De kop herhaalt op elke pagina; een rij splitst nooit
        /// over twee pagina's (ShowEntire op elke cel). Groepsrijen en
        /// subtotalen (35d) zijn nog niet gebouwd; hier komen ze later bij als extra rijtype.
        /// QuestPDF-tabellen kennen geen "gap": elke kolom is de ontwerpbreedte + 3mm, de 3mm zit als
        /// rechterpadding in de cel (de laatste kolom heeft die niet).</summary>
        public static void Tabel(IContainer c, string bodyFont, IReadOnlyList<TabelKolom> kolommen,
            IEnumerable<IReadOnlyList<TabelCel>> rijen, string legeTekst = "Geen regels.")
        {
            const float Gap = 3f;
            var laatste = kolommen.Count - 1;

            c.Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    for (var i = 0; i < kolommen.Count; i++)
                    {
                        var k = kolommen[i];
                        if (k.BreedteMm is null) d.RelativeColumn();
                        else d.ConstantColumn(k.BreedteMm.Value + (i == laatste ? 0 : Gap), Unit.Millimetre);
                    }
                });

                t.Header(h =>
                {
                    for (var i = 0; i < kolommen.Count; i++)
                    {
                        var k = kolommen[i];
                        var txt = h.Cell().BorderBottom(0.5f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Groen)
                            .PaddingBottom(1.6f, Unit.Millimetre).PaddingRight(i == laatste ? 0 : Gap, Unit.Millimetre)
                            .Text(k.Kop.ToUpperInvariant()).FontFamily(bodyFont).FontSize(6.5f).Bold()
                            .FontColor(GlV2PdfTheme.Inkt).LetterSpacing(0.10f);
                        if (k.Rechts) txt.AlignRight();
                    }
                });

                var leeg = true;
                foreach (var rij in rijen)
                {
                    leeg = false;
                    for (var i = 0; i < kolommen.Count; i++)
                    {
                        var cel = i < rij.Count ? rij[i] : new TabelCel("");
                        var cell = t.Cell().BorderBottom(0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn)
                            .PaddingVertical(1.7f, Unit.Millimetre).PaddingRight(i == laatste ? 0 : Gap, Unit.Millimetre)
                            .ShowEntire() // rij nooit splitsen over twee pagina's (36 · 7 Regels)
                            .AlignMiddle(); // waarden per rij verticaal gecentreerd (Niels, 2026-10-06)
                        if (kolommen[i].Rechts) cell = cell.AlignRight();

                        if (cel.PilKleur is not null)
                        {
                            cell.Background(cel.PilKleur).CornerRadius(1, Unit.Millimetre)
                                .PaddingHorizontal(1.4f, Unit.Millimetre).PaddingVertical(0.3f, Unit.Millimetre)
                                .Text(cel.Tekst).FontFamily(bodyFont).FontSize(8).SemiBold().FontColor(GlV2PdfTheme.Inkt)
                                .EnableFontFeature(FontFeatures.TabularFigures);
                        }
                        else
                        {
                            cell.Text(cel.Tekst).FontFamily(bodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt)
                                .EnableFontFeature(FontFeatures.TabularFigures);
                        }
                    }
                }

                if (leeg)
                    t.Cell().ColumnSpan((uint)kolommen.Count).PaddingVertical(1.7f, Unit.Millimetre)
                        .Text(legeTekst).Italic().FontFamily(bodyFont).FontSize(8).FontColor(GlV2PdfTheme.Licht);
            });
        }

        /// <summary>Meta + adres (punt 35a "ADRES &amp; META", 36.5) — letterlijk zoals het offerte-
        /// voorbeeld (35d): links een ongekaderd label/waarde-rooster (label 8pt 600, waarde 8pt 400
        /// tabular-nums; een rij kan "Highlight" meekrijgen — groen+vet, zoals "Geldig tot"), rechts
        /// een 72mm adresblok (vetgedrukte eerste regel, overige regels gewoon). Geen rand, geen
        /// sectielabel — dit is geen kaart, het is gewoon de kop van de brief.</summary>
        public static void MetaAdres(IContainer c, string bodyFont,
            IReadOnlyList<(string Label, string Value, bool Highlight)> rows,
            string recipientName, IReadOnlyList<string> recipientLines)
        {
            c.Row(row =>
            {
                row.RelativeItem().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.ConstantColumn(28, Unit.Millimetre); d.RelativeColumn(); });
                    foreach (var r in rows)
                    {
                        t.Cell().PaddingVertical(0.7f, Unit.Millimetre).Text(r.Label)
                            .FontFamily(bodyFont).FontSize(8).SemiBold().FontColor(GlV2PdfTheme.Inkt);
                        var valueText = t.Cell().PaddingVertical(0.7f, Unit.Millimetre).Text(r.Value)
                            .FontFamily(bodyFont).FontSize(8).FontColor(r.Highlight ? GlV2PdfTheme.Groen : GlV2PdfTheme.Inkt)
                            .EnableFontFeature(FontFeatures.TabularFigures);
                        if (r.Highlight) valueText.SemiBold();
                    }
                });
                row.ConstantItem(10, Unit.Millimetre);
                row.ConstantItem(72, Unit.Millimetre).Column(col =>
                {
                    col.Item().Text(recipientName).FontFamily(bodyFont).FontSize(8.5f).Bold().LetterSpacing(0.02f).FontColor(GlV2PdfTheme.Inkt);
                    foreach (var line in recipientLines)
                        col.Item().PaddingTop(0.8f, Unit.Millimetre).Text(line).FontFamily(bodyFont).FontSize(8.5f).FontColor(GlV2PdfTheme.Inkt);
                });
            });
        }

        /// <summary>De afsluitrij van offerte/wijzigingsopdracht, zoals 35c (meerdere tarieven) en 35i (één tarief):
        /// links "BTW-VERMELDING" met per tarief de factuurvermelding van het facturatiebedrijf (Vattype.InvoiceMention; geen vermelding = geen regel) (7pt gedempt, tarief vet in 8mm), rechts een
        /// 92mm breed blok. Eén tarief: regels "Totaal excl. btw" en "Btw x %"; meerdere tarieven: tabel
        /// TARIEF/MAATSTAF/BTW/TOTAAL (groene kop, rij per tarief, "Totaal"-rij). Beide eindigen met de groene
        /// balk "Totaal incl. btw" (8pt 600 + 12pt 700, 1,5mm eronder). 4mm boven, 8mm tussen de kolommen.</summary>
        public static void TotalenBtw(IContainer c, string bodyFont, IReadOnlyList<(decimal Rate, decimal Base, decimal Vat)> tarieven,
            decimal totalExcl, decimal totalVat, decimal totalIncl, IReadOnlyDictionary<decimal, string> btwVermeldingen)
        {
            var vermeldingen = tarieven
                .Where(t => btwVermeldingen.TryGetValue(t.Rate, out var m) && !string.IsNullOrWhiteSpace(m))
                .Select(t => (t.Rate, Tekst: btwVermeldingen[t.Rate])).ToList();

            c.PaddingTop(4, Unit.Millimetre).Row(row =>
            {
                row.Spacing(8, Unit.Millimetre);

                row.RelativeItem().PaddingTop(1, Unit.Millimetre).Column(col =>
                {
                    if (vermeldingen.Count == 0) return;
                    col.Spacing(1.4f, Unit.Millimetre);
                    col.Item().Text("BTW-VERMELDING").FontFamily(bodyFont).FontSize(6.5f).Bold().LetterSpacing(0.16f).FontColor(GlV2PdfTheme.Groen);
                    col.Item().Column(list =>
                    {
                        list.Spacing(1.6f, Unit.Millimetre);
                        foreach (var v in vermeldingen)
                            list.Item().Row(r =>
                            {
                                r.Spacing(2, Unit.Millimetre);
                                r.ConstantItem(8, Unit.Millimetre).Text(Btw(v.Rate)).FontFamily(bodyFont).FontSize(7).Bold().FontColor(GlV2PdfTheme.Inkt);
                                r.RelativeItem().Text(v.Tekst).FontFamily(bodyFont).FontSize(7).FontColor(GlV2PdfTheme.Gedempt).LineHeight(1.5f);
                            });
                    });
                });

                row.ConstantItem(92, Unit.Millimetre).Column(col =>
                {
                    if (tarieven.Count > 1)
                    {
                        col.Item().Background(GlV2PdfTheme.Groen).PaddingVertical(1.5f, Unit.Millimetre).PaddingHorizontal(3, Unit.Millimetre).Row(r =>
                        {
                            r.Spacing(2, Unit.Millimetre);
                            void Kop(IContainer x, string t, bool rechts) { var tx = x.Text(t).FontFamily(bodyFont).FontSize(6).Bold().LetterSpacing(0.10f).FontColor(GlV2PdfTheme.Wit); if (rechts) tx.AlignRight(); }
                            Kop(r.ConstantItem(14, Unit.Millimetre), "TARIEF", false);
                            Kop(r.RelativeItem(), "MAATSTAF", true);
                            Kop(r.RelativeItem(), "BTW", true);
                            Kop(r.RelativeItem(), "TOTAAL", true);
                        });
                        foreach (var t in tarieven)
                            TariefRij(col, bodyFont, Btw(t.Rate), Euro(t.Base), Euro(t.Vat), Euro(t.Base + t.Vat), vet: false);
                        TariefRij(col, bodyFont, "Totaal", Euro(totalExcl), Euro(totalVat), Euro(totalIncl), vet: true);
                    }
                    else
                    {
                        void Regel(string label, string waarde) => col.Item().BorderBottom(0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn)
                            .PaddingVertical(1.4f, Unit.Millimetre).PaddingHorizontal(3, Unit.Millimetre).Row(r =>
                            {
                                r.RelativeItem().Text(label).FontFamily(bodyFont).FontSize(8).FontColor(GlV2PdfTheme.Gedempt);
                                r.AutoItem().Text(waarde).FontFamily(bodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                            });
                        Regel("Totaal excl. btw", Euro(totalExcl));
                        Regel(tarieven.Count == 1 ? $"Btw {Btw(tarieven[0].Rate)}" : "Btw", Euro(totalVat));
                    }

                    col.Item().PaddingTop(1.5f, Unit.Millimetre).Background(GlV2PdfTheme.Groen)
                        .PaddingVertical(2.6f, Unit.Millimetre).PaddingHorizontal(3, Unit.Millimetre).Row(r =>
                        {
                            r.RelativeItem().AlignMiddle().Text("Totaal incl. btw").FontFamily(bodyFont).FontSize(8).SemiBold().FontColor(GlV2PdfTheme.Wit);
                            r.AutoItem().AlignMiddle().Text(Euro(totalIncl)).FontFamily(bodyFont).FontSize(12).Bold()
                                .FontColor(GlV2PdfTheme.Wit).EnableFontFeature(FontFeatures.TabularFigures);
                        });
                });
            });
        }

        private static void TariefRij(ColumnDescriptor col, string bodyFont, string tarief, string maatstaf, string btw, string totaal, bool vet)
        {
            col.Item().BorderBottom(0.2f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn)
                .PaddingVertical(1.3f, Unit.Millimetre).PaddingHorizontal(3, Unit.Millimetre).Row(r =>
                {
                    r.Spacing(2, Unit.Millimetre);
                    void Cel(IContainer x, string t, bool rechts, int gewicht)
                    {
                        var tx = x.Text(t).FontFamily(bodyFont).FontSize(7.8f).FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                        if (gewicht == 2) tx.Bold(); else if (gewicht == 1) tx.SemiBold();
                        if (rechts) tx.AlignRight();
                    }
                    Cel(r.ConstantItem(14, Unit.Millimetre), tarief, false, vet ? 2 : 1);
                    Cel(r.RelativeItem(), maatstaf, true, vet ? 2 : 0);
                    Cel(r.RelativeItem(), btw, true, vet ? 2 : 0);
                    Cel(r.RelativeItem(), totaal, true, vet ? 2 : 0);
                });
        }

        /// <summary>Eén handtekenvak: 18mm hoog, hairline-rand (afgerond 1mm), label onderaan — letterlijk
        /// zoals het offerte-voorbeeld (35d, "handtekening · datum"). Leeg op te vullen door de klant,
        /// dus geen voorgedrukte naam erin. Geen vak voor het eigen bedrijf — dit vak is uitsluitend
        /// voor de klant.</summary>
        public static void Handtekeningvak(IContainer c, string bodyFont, string label)
        {
            c.Border(0.25f, Unit.Millimetre).BorderColor(GlV2PdfTheme.Lijn).CornerRadius(1, Unit.Millimetre)
                .Padding(8).Height(18, Unit.Millimetre).Column(col =>
            {
                col.Item().ExtendVertical().AlignBottom().Text(label.ToLowerInvariant()).FontFamily(bodyFont).FontSize(6).FontColor(GlV2PdfTheme.Licht);
            });
        }
    }
}
