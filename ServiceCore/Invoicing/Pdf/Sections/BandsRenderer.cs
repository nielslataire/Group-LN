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
        if (bands == null)
            return;

        var topBand = bands.Top;
        var bottomBand = bands.Bottom;

        var hasTopBand = topBand is { Height: > 0 };
        var hasBottomBand = bottomBand is { Height: > 0 };

        if (!hasTopBand && !hasBottomBand)
            return;

        page.Background().Element(container =>
        {
            container.Column(column =>
            {
                if (hasTopBand)
                {
                    var color = string.IsNullOrWhiteSpace(topBand!.Color) ? "#FFFFFF" : topBand.Color;

                    column.Item()
                        .Height(topBand.Height)
                        .Background(color);
                }

                column.Item().Expand();

                if (hasBottomBand)
                {
                    var color = string.IsNullOrWhiteSpace(bottomBand!.Color) ? "#FFFFFF" : bottomBand.Color;

                    column.Item()
                        .Height(bottomBand.Height)
                        .Background(color);
                }
            });
        });
    }
}