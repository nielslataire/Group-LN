using System.Collections.Generic;
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

        /// <summary>Totalenblok rechts (punt 36.5/36.6, 78mm breed): losse regels, de laatste als een
        /// volle groene balk met witte, grotere cijfers — gebruikt door offerte/wijzigingsopdracht en
        /// later de factuur.</summary>
        public static void Totalenblok(IContainer c, string bodyFont, IReadOnlyList<(string Label, string Value)> rows, (string Label, string Value) grandTotal)
        {
            c.AlignRight().Width(78, Unit.Millimetre).Column(col =>
            {
                foreach (var row in rows)
                {
                    col.Item().PaddingVertical(1.4f, Unit.Millimetre).Row(r =>
                    {
                        r.RelativeItem().Text(row.Label).FontFamily(bodyFont).FontSize(8).FontColor(GlV2PdfTheme.Inkt);
                        r.ConstantItem(26, Unit.Millimetre).AlignRight().Text(row.Value).FontFamily(bodyFont).FontSize(8).Bold()
                            .FontColor(GlV2PdfTheme.Inkt).EnableFontFeature(FontFeatures.TabularFigures);
                    });
                }
                col.Item().PaddingTop(1.5f, Unit.Millimetre).Background(GlV2PdfTheme.Groen).Padding(8).Row(r =>
                {
                    r.RelativeItem().AlignMiddle().Text(grandTotal.Label).FontFamily(bodyFont).FontSize(8).SemiBold().FontColor(GlV2PdfTheme.Wit);
                    r.ConstantItem(32, Unit.Millimetre).AlignRight().AlignMiddle().Text(grandTotal.Value).FontFamily(bodyFont).FontSize(12).Bold()
                        .FontColor(GlV2PdfTheme.Wit).EnableFontFeature(FontFeatures.TabularFigures);
                });
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
