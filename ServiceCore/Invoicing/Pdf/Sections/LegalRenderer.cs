using QuestPDF.Fluent;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class LegalRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;

    public LegalRenderer(TemplateInterpolator interpolator)
    {
        _interpolator = interpolator;
    }

    public string SectionType => "legal";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not LegalSectionConfig legal || !section.Visible)
            return;

        var text = _interpolator.Interpolate(legal.Text, vm);
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(ctx.FooterLegalText))
            return;

        var finalText = string.IsNullOrWhiteSpace(text) ? ctx.FooterLegalText ?? string.Empty : text;
        column.Item().Text(finalText).FontSize(legal.Size ?? 8f).FontColor(ctx.SecondaryColorHex ?? "#666666");
    }
}