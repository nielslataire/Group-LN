using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CPMCore.Documents.GlV2
{
    /// <summary>Paginaformaten uit design-handoff punt 35a/36.4. Welk document welk formaat gebruikt
    /// staat in DESIGN.md ("PDF-documenten (gl-v2)"); de volle-breedte groene band en de 18mm
    /// zijmarge (<see cref="GlV2PdfTheme.ZijMargeMm"/>) gelden voor alle vier identiek.</summary>
    public enum GlV2PdfFormat
    {
        /// <summary>210×297mm — brieven/documenten: factuur, offerte, wijzigingsopdracht, klantenlijst.</summary>
        A4Staand,
        /// <summary>297×210mm — brede lijsten: aannemers, leveranciers.</summary>
        A4Liggend,
        /// <summary>297×420mm — overzicht met plan/afbeelding: prijslijst eenheden.</summary>
        A3Staand,
        /// <summary>420×297mm — brede overzichten met veel kolommen: budget.</summary>
        A3Liggend,
    }

    public static class GlV2PdfFormatExtensions
    {
        public static PageSize ToPageSize(this GlV2PdfFormat format) => format switch
        {
            GlV2PdfFormat.A4Staand => PageSizes.A4.Portrait(),
            GlV2PdfFormat.A4Liggend => PageSizes.A4.Landscape(),
            GlV2PdfFormat.A3Staand => PageSizes.A3.Portrait(),
            GlV2PdfFormat.A3Liggend => PageSizes.A3.Landscape(),
            _ => PageSizes.A4.Portrait(),
        };
    }
}
