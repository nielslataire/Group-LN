using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceCore.Invoicing.Pdf.Sections;

namespace ServiceCore.Invoicing.Pdf;

public sealed class JsonInvoiceTemplate : NamedInvoiceTemplate
{
    private readonly SectionRendererFactory _factory;
    private readonly BandsRenderer _bandsRenderer;

    public JsonInvoiceTemplate(string name, SectionRendererFactory factory, BandsRenderer bandsRenderer)
        : base(name)
    {
        _factory = factory;
        _bandsRenderer = bandsRenderer;
    }

    public override void Compose(IDocumentContainer container, InvoiceVm vm, TemplateContext ctx)
    {
        if (ctx.Layout == null)
            throw new InvalidOperationException("Layout configuration is missing.");

        var layout = ctx.Layout;
        var margin = layout.Page?.Margin ?? 30f;

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.DefaultTextStyle(style =>
            {
                var s = style.FontSize(10);

                if (!string.IsNullOrWhiteSpace(ctx.SecondaryColorHex))
                    s = s.FontColor(ctx.SecondaryColorHex);

                if (!string.IsNullOrWhiteSpace(ctx.FontFamily))
                    s = s.FontFamily(ctx.FontFamily);

                return s;
            });

            // Background bands are temporarily disabled due to QuestPDF rendering issues.
            // _bandsRenderer.DrawBackground(page, layout);

            page.Content().Element(content =>
            {
                content.Padding(margin).Column(column =>
                {
                    foreach (var section in layout.Sections)
                    {
                        if (!section.Visible)
                            continue;

                        if (string.Equals(section.Type, "bands", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var renderer = _factory.Resolve(section.Type);
                        renderer?.Render(column, section, vm, ctx);
                    }
                });
            });
        });
    }
}
