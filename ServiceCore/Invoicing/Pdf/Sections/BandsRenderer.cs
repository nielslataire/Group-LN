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

    public void DrawBackground(PageDescriptor page, LayoutConfig layout, TemplateContext ctx)
    {
        var bands = layout.Page?.Bands;

        page.Background()
            .Element(c => c       // let op: één ketting op 'c'
                .Extend()
                .Layers(layers =>
                {
                    layers.PrimaryLayer();

                    if (ctx.PageBackgroundImage is { Length: > 0 })
                    {
                        layers.Layer()
                            .Extend()
                            .Image(ctx.PageBackgroundImage)
                            .FitArea();
                    }

                    if (bands?.Top is { Height: > 0 } top)
                    {
                        var color = string.IsNullOrWhiteSpace(top.Color) ? "#FFFFFF" : top.Color;

                        layers.Layer()
                            .AlignTop()
                            .Height(top.Height)
                            .Background(color);
                    }

                    if (bands?.Bottom is { Height: > 0 } bottom)
                    {
                        var color = string.IsNullOrWhiteSpace(bottom.Color) ? "#FFFFFF" : bottom.Color;

                        layers.Layer()
                            .AlignBottom()
                            .Height(bottom.Height)
                            .Background(color);
                    }
                }));
    }


}