using QuestPDF.Fluent;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class HeadlineRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;

    public HeadlineRenderer(TemplateInterpolator interpolator)
    {
        _interpolator = interpolator;
    }

    public string SectionType => "headline";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not HeadlineSectionConfig headline || !section.Visible)
            return;

        column.Item().Column(col =>
        {
            var title = _interpolator.Interpolate(headline.Title, vm);
            if (!string.IsNullOrWhiteSpace(title))
            {
                col.Item().Text(title)
                    .FontSize(16)
                    .SemiBold()
                    .FontColor(ctx.PrimaryColorHex ?? "#000000");
            }

            foreach (var meta in headline.Meta)
            {
                var text = _interpolator.Interpolate(meta, vm);
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                col.Item().Text(text).FontColor(ctx.SecondaryColorHex ?? "#666666");
            }
        });
    }
}