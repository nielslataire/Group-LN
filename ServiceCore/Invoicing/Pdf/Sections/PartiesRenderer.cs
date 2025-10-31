using QuestPDF.Fluent;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class PartiesRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;

    public PartiesRenderer(TemplateInterpolator interpolator)
    {
        _interpolator = interpolator;
    }

    public string SectionType => "parties";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not PartiesSectionConfig parties || !section.Visible)
            return;

        column.Item().Row(row =>
        {
            foreach (var cfg in parties.Columns)
            {
                row.RelativeItem().Column(col =>
                {
                    var title = _interpolator.Interpolate(cfg.Title, vm);
                    if (!string.IsNullOrWhiteSpace(title))
                        col.Item().Text(title).SemiBold();

                    foreach (var line in cfg.Lines)
                    {
                        var text = _interpolator.Interpolate(line, vm);
                        if (string.IsNullOrWhiteSpace(text))
                            continue;
                        col.Item().Text(text);
                    }
                });
            }
        });
    }
}