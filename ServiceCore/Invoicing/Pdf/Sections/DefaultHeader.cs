using System;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class DefaultHeaderRenderer : ISectionRenderer
{
    private const float PointsPerMillimeter = 72f / 25.4f;
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("nl-BE");

    public string SectionType => "defaultHeader";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not DefaultHeaderSectionConfig || !section.Visible)
            return;

        column.Item().Height(Mm(82)).Row(row =>
        {
            RenderLeftColumn(row, vm, ctx);
            RenderRightColumn(row, vm, ctx);
        });
    }

    private static void RenderLeftColumn(RowDescriptor row, InvoiceVm vm, TemplateContext ctx)
    {
        row.ConstantItem(Mm(104)).Column(left =>
        {
            left.Spacing(4);

            // LOGO (vast kader 45x45 mm, geen overflow)
            left.Item().Height(Mm(45)).Element(c =>
            {
                if (ctx.Logo is { Length: > 0 })
                {
                    c.AlignLeft()
                     .Element(box => box
                         .Width(Mm(45))
                         .Height(Mm(45))
                         .AlignMiddle()
                         .Image(ctx.Logo)
                         .FitArea());
                }
            });

            // INFO
            left.Item().Height(Mm(35)).Column(info =>
            {
                info.Spacing(2);

                AddInfoRow(info, "Datum", FormatDate(vm.Invoice.IssueDate), ctx);

                var invoiceType = ResolveInvoiceType(vm);
                AddInfoRow(info, "Type factuur", invoiceType?.ToUpper(Culture), ctx, span =>
                {
                    span.FontColor(ctx.PrimaryColorHex ?? "#000000");
                    span.Black();
                    ApplyFont(span);
                });

                var invoiceNumber = ResolveInvoiceNumber(vm);
                AddInfoRow(info, "Factuurnummer", invoiceNumber?.ToUpper(Culture), ctx, span =>
                {
                    span.FontColor(ctx.PrimaryColorHex ?? "#000000");
                    span.Black();
                    ApplyFont(span);
                });

                if (!string.IsNullOrWhiteSpace(vm.Client.VAT))
                    AddInfoRow(info, "BTW-nummer", vm.Client.VAT, ctx);

                var dueDateText = FormatDate(vm.Invoice.DueDate);
                AddInfoRow(info, "Vervaldatum", string.IsNullOrWhiteSpace(dueDateText) ? "-" : dueDateText, ctx);
            });
        });
    }


    private static void RenderRightColumn(RowDescriptor row, InvoiceVm vm, TemplateContext ctx)
    {
        row.RelativeItem().Column(right =>
        {
            right.Item().Element(container => container.MinHeight(Mm(32)));

            right.Item().Column(customer =>
            {
                customer.Spacing(2);

                var name = !string.IsNullOrWhiteSpace(vm.Client.Name)
                    ? vm.Client.Name
                    : vm.Client.LegalName;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    customer.Item().Text(text =>
                    {
                        var span = text.Span(name.ToUpper(Culture));
                        span.FontSize(9);
                        span.Bold();
                        ApplyFont(span);

                    });
                }

                if (!string.IsNullOrWhiteSpace(vm.Client.AddressLine))
                {
                    customer.Item().Text(text =>
                    {
                        var span = text.Span(vm.Client.AddressLine);
                        span.FontSize(9);
                        ApplyFont(span);
                    });
                }

                var cityLine = BuildCityLine(vm.Client.Postal, vm.Client.City);
                if (!string.IsNullOrWhiteSpace(cityLine))
                {
                    customer.Item().Text(text =>
                    {
                        var span = text.Span(cityLine);
                        span.FontSize(9);
                        ApplyFont(span);
                    });
                }

                if (ShouldDisplayCountry(vm.Client.Country))
                {
                    customer.Item().Text(text =>
                    {
                        var span = text.Span(vm.Client.Country);
                        span.FontSize(9);
                        ApplyFont(span);
                    });
                }
            });
        });
    }

    private static void AddInfoRow(ColumnDescriptor column, string label, string? value, TemplateContext ctx, Action<TextSpanDescriptor>? configureValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        column.Item().Row(row =>
        {
            row.ConstantItem(Mm(45)).Text(text =>
            {
                var span = text.Span($"{label}:");
                span.FontSize(9);
                span.Bold();
                ApplyFont(span);
            });

            row.RelativeItem().Text(text =>
            {
                var span = text.Span(value);
                span.FontSize(9);
                ApplyFont(span);
                configureValue?.Invoke(span);
            });
        });
    }

    private static string ResolveInvoiceType(InvoiceVm vm)
    {
        if (IsProformaInvoice(vm))
            return "Proforma factuur";
        if (vm.Totals.Incl < 0)
            return "Creditnota";

        var status = vm.Invoice.Status;
        if (!string.IsNullOrWhiteSpace(status) && status.Contains("credit", StringComparison.OrdinalIgnoreCase))
            return "Creditnota";

        return "Factuur";
    }

    private static string ResolveInvoiceNumber(InvoiceVm vm)
    {
        if (IsProformaInvoice(vm))
            return "-";
        if (!string.IsNullOrWhiteSpace(vm.Invoice.PublicId))
            return vm.Invoice.PublicId;

        return vm.Invoice.Id > 0 ? vm.Invoice.Id.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string FormatDate(DateOnly date) => date.ToString("dd/MM/yyyy", Culture);

    private static string FormatDate(DateOnly? date)
        => date.HasValue ? date.Value.ToString("dd/MM/yyyy", Culture) : string.Empty;

    private static string? BuildCityLine(string? postal, string? city)
    {
        if (string.IsNullOrWhiteSpace(postal) && string.IsNullOrWhiteSpace(city))
            return null;

        if (string.IsNullOrWhiteSpace(postal))
            return city?.ToUpper(Culture);

        if (string.IsNullOrWhiteSpace(city))
            return postal;

        return $"{postal} {city.ToUpper(Culture)}";
    }

    private static bool ShouldDisplayCountry(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
            return false;

        var normalized = country.Trim().ToUpperInvariant();
        return normalized is not ("BE" or "BELGIUM" or "BELGIE" or "BELGIË");
    }

    private static void ApplyFont(TextSpanDescriptor span, FontWeight weight = FontWeight.Normal)
    {
        span.FontFamily("Avenir");
    }
    private static bool IsProformaInvoice(InvoiceVm vm)
    {
        var status = vm?.Invoice?.Status;
        if (string.IsNullOrWhiteSpace(status))
            return false;

        return status.Trim().Equals("Draft", StringComparison.OrdinalIgnoreCase)
            || status.Trim().Equals("Concept", StringComparison.OrdinalIgnoreCase);
    }
    private static float Mm(float value) => value * PointsPerMillimeter;
}