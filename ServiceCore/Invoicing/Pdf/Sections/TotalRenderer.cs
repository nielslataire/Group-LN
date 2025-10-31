using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class TotalsRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;

    public TotalsRenderer(TemplateInterpolator interpolator)
    {
        _interpolator = interpolator;
    }

    public string SectionType => "totals";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not TotalsSectionConfig totals || !section.Visible)
            return;

        column.Item().Row(row =>
        {
            IContainer target;
            if (string.Equals(totals.Layout, "left", StringComparison.OrdinalIgnoreCase))
            {
                target = row.RelativeItem();
            }
            else
            {
                row.RelativeItem();
                target = row.ConstantItem(220);
            }

            target.Column(col =>
            {
                foreach (var totalRow in totals.Rows)
                {
                    var label = _interpolator.Interpolate(totalRow.Label, vm);
                    var value = _interpolator.Interpolate(totalRow.Value, vm);
                    col.Item().Row(inner =>
                    {
                        inner.RelativeItem().Text(label);
                        var text = inner.RelativeItem().AlignRight().Text(value);
                        if (totalRow.Bold == true)
                            text.SemiBold();
                        if (totalRow.Size.HasValue)
                            text.FontSize(totalRow.Size.Value);
                    });
                }
            });
        });
    }
}