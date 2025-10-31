using QuestPDF.Fluent;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class PaymentRenderer : ISectionRenderer
{
    private readonly TemplateInterpolator _interpolator;

    public PaymentRenderer(TemplateInterpolator interpolator)
    {
        _interpolator = interpolator;
    }

    public string SectionType => "payment";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not PaymentSectionConfig payment || !section.Visible)
            return;

        column.Item().Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                if (payment.ShowStructuredMessage)
                {
                    var label = _interpolator.Interpolate(payment.StructuredLabel, vm);
                    var value = _interpolator.Interpolate(payment.StructuredValue, vm);
                    if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                    {
                        col.Item().Row(inner =>
                        {
                            inner.ConstantItem(180).Text(label).SemiBold();
                            inner.RelativeItem().Text(value);
                        });
                    }
                }

                if (!string.IsNullOrWhiteSpace(payment.Note))
                {
                    var note = _interpolator.Interpolate(payment.Note, vm);
                    if (!string.IsNullOrWhiteSpace(note))
                        col.Item().Text(note);
                }
            });

            if (payment.ShowQr && ctx.EpcQrPng != null && ctx.EpcQrPng.Length > 0)
            {
                var size = payment.QrSize ?? 120f;
                row.ConstantItem(size).AlignRight().Image(ctx.EpcQrPng);
            }
        });
    }
}