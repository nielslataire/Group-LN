using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ServiceCore.Invoicing.Pdf;
using ServiceCore.Invoicing.Pdf.Sections;
using ServiceCore.Invoicing.Pdf.Templates;

public sealed class BandsRenderer : ISectionRenderer
{
    public string SectionType => "bands";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        // bands worden vóór content getekend via DrawBackground
    }

    public void DrawBackground(PageDescriptor page, LayoutConfig layout)
    {
        var bands = layout.Page?.Bands;
        if (bands == null) return;

        // Belangrijk: Background maar 1x nemen en hergebruiken
        var bg = page.Background();

        // TOP band – full bleed
        if (bands.Top is { Height: > 0 } top)
        {
            var color = string.IsNullOrWhiteSpace(top.Color) ? "#FFFFFF" : top.Color;
            bg.Element(e => e
                .AlignTop()
                .Height(top.Height)
                .Background(color));
        }

        // BOTTOM band – full bleed
        if (bands.Bottom is { Height: > 0 } bottom)
        {
            var color = string.IsNullOrWhiteSpace(bottom.Color) ? "#FFFFFF" : bottom.Color;
            bg.Element(e => e
                .AlignBottom()
                .Height(bottom.Height)
                .Background(color));
        }
    }
}
