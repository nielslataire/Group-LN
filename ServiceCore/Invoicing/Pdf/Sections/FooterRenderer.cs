using QuestPDF.Fluent;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class FooterRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;

    public FooterRenderer(TemplateInterpolator interpolator)
    {
        _interpolator = interpolator;
    }

    public string SectionType => "footer";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not FooterSectionConfig footer || !section.Visible)
            return;

        var text = _interpolator.Interpolate(footer.Center, vm);
        if (string.IsNullOrWhiteSpace(text))
            return;

        column.Item().AlignCenter().Text(text).FontSize(footer.Size ?? 8f).FontColor(ctx.SecondaryColorHex ?? "#666666");
    }
}