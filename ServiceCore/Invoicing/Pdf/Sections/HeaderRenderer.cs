using System;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class HeaderRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;

    public HeaderRenderer(TemplateInterpolator interpolator)
    {
        _interpolator = interpolator;
    }

    public string SectionType => "header";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not HeaderSectionConfig header || !section.Visible)
            return;

        column.Item().Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                foreach (var line in header.Left)
                {
                    var text = _interpolator.Interpolate(line, vm);
                    if (string.IsNullOrWhiteSpace(text))
                        continue;
                    col.Item().Text(text).FontColor(ctx.PrimaryColorHex ?? "#000000");
                }
            });

            if (header.Right is { } right)
            {
                var width = right.Width ?? 120f;
                var image = ResolveImage(right.Image, ctx);
                if (image != null)
                {
                    row.ConstantItem(width)
                        .AlignRight()
                        .Image(image);
                }
            }
        });
    }

    private static byte[]? ResolveImage(string? source, TemplateContext ctx)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        if (source.StartsWith("theme:logo", StringComparison.OrdinalIgnoreCase))
            return ctx.Logo;

        return null;
    }
}