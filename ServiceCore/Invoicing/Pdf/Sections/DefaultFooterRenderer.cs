using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Elements;
using QuestPDF.Elements.Table;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceCore.Invoicing.Pdf.Templates;

namespace ServiceCore.Invoicing.Pdf.Sections;

public sealed class DefaultFooterRenderer : ISectionRenderer
{
    private const float PointsPerMillimeter = 72f / 25.4f;
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("nl-BE");
    private static readonly Color BorderColor = Colors.Black;

    public string SectionType => "defaultFooter";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not FooterSectionConfig footer || !section.Visible)
            return;

        column.Item()
                    .ExtendVertical()
                    .AlignBottom()
                    .Height(Mm(60f))
                    .Column(col =>
                    {
                        col.Spacing(Mm(4f));
                        col.Item()
                           .AlignRight()
                           .MinimalBox()                // krimpt tot minimale inhoudsbreedte
                           .Element(c => ComposeVatSummary(c, vm, ctx));
                        col.Item().Element(container => ComposePaymentSection(container, vm));
                        col.Item().Element(container => ComposeContactSection(container, vm));
                    });
    }

    private static void ComposeVatSummary(IContainer container, InvoiceVm vm, TemplateContext ctx)
    {
        var summaries = (vm.VatSummary?.Count > 0 ? vm.VatSummary : new[]
        {
            new VatRateSummaryVm { Rate = 0m, Net = vm.Totals.Excl, Vat = vm.Totals.Vat }
        }).ToList();

        var primary = string.IsNullOrWhiteSpace(ctx.PrimaryColorHex)
            ? Colors.Black
            : Color.FromHex(ctx.PrimaryColorHex);

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(Mm(26f));
                foreach (var _ in summaries)
                    columns.ConstantColumn(Mm(40f));
                columns.ConstantColumn(Mm(40f));
            });

            AddLabelCell(table.Cell(), "BTW-tarief", TextHorizontalAlignment.Left);
            foreach (var summary in summaries)
                AddHeaderCell(table.Cell(), FormatRate(summary.Rate), primary, TextHorizontalAlignment.Left);
            AddHeaderCell(table.Cell(), "Totaal", primary, TextHorizontalAlignment.Right);

            AddLabelCell(table.Cell(), "MVH", TextHorizontalAlignment.Left);
            foreach (var summary in summaries)
                AddValueCell(table.Cell(), FormatCurrency(summary.Net), TextHorizontalAlignment.Left);
            AddValueCell(table.Cell(), FormatCurrency(summaries.Sum(s => s.Net)), TextHorizontalAlignment.Right);

            AddLabelCell(table.Cell(), "BTW", TextHorizontalAlignment.Left);
            foreach (var summary in summaries)
                AddValueCell(table.Cell(), FormatCurrency(summary.Vat), TextHorizontalAlignment.Left);
            AddValueCell(table.Cell(), FormatCurrency(summaries.Sum(s => s.Vat)), TextHorizontalAlignment.Right);

            AddLabelCell(table.Cell(), string.Empty, TextHorizontalAlignment.Left);
            foreach (var _ in summaries)
                AddEmptyValueCell(table.Cell());
            var invoiceTotal = vm.Totals?.Incl ?? summaries.Sum(s => s.Total);
            AddValueCell(table.Cell(), FormatCurrency(invoiceTotal),TextHorizontalAlignment.Right, FontWeight.Bold);
        });
    }


    private static void ComposePaymentSection(IContainer container, InvoiceVm vm)
    {
        var dueDateText = FormatDate(vm.Invoice.DueDate);
        var paymentDate = string.IsNullOrWhiteSpace(dueDateText) ? "—" : dueDateText;

        var account = !string.IsNullOrWhiteSpace(vm.Payment?.Iban)
            ? vm.Payment!.Iban
            : !string.IsNullOrWhiteSpace(vm.Payment?.BankAccount)
                ? vm.Payment!.BankAccount
                : vm.IssuerCompany.IBAN;
        var accountText = string.IsNullOrWhiteSpace(account) ? "—" : account;

        var structured = string.IsNullOrWhiteSpace(vm.Payment?.Structured)
            ? "—"
            : vm.Payment!.Structured!;

        var line = $"Te betalen voor {paymentDate} op rekening {accountText} met gestructureerde mededeling {structured}";

        container.Column(col =>
        {
            col.Item().Text(text =>
            {
                var span = text.Span(line);
                span.FontSize(9);
                ApplyFont(span);
            });

            if (!string.IsNullOrWhiteSpace(vm.ExtraInfo))
            {
                col.Item().Text(text =>
                {
                    var span = text.Span(vm.ExtraInfo);
                    span.FontSize(7);
                    span.Italic();
                    ApplyFont(span);
                });
            }
        });
    }

    private static void ComposeContactSection(IContainer container, InvoiceVm vm)
    {
        var iban = vm.Payment?.Iban ?? vm.IssuerCompany.IBAN;

        container.Row(row =>
        {
            row.RelativeItem().Element(col => ComposeContactColumn(col, new[]
            {
                vm.IssuerCompany.AddressLine,
                FormatPostalCity(vm.IssuerCompany.Postal, vm.IssuerCompany.City)
            }));

            row.RelativeItem().Element(col => ComposeContactColumn(col, new[]
            {
                vm.IssuerCompany.Phone,
                vm.IssuerCompany.Phone2
            }));

            row.RelativeItem().Element(col => ComposeContactColumn(col, new[]
            {
                vm.IssuerCompany.Email,
                vm.IssuerCompany.Website
            }));

            row.RelativeItem().Element(col => ComposeContactColumn(col, new[]
            {
                FormatCompanyVat(vm.IssuerCompany.Name, vm.IssuerCompany.VAT),
                iban
            }));
        });
    }
    private static void ComposeContactColumn(IContainer container, IEnumerable<string?> lines)
    {
        container.Column(col =>
        {
            foreach (var line in lines)
            {
                col.Item().Text(text =>
                {
                    var span = text.Span(!string.IsNullOrWhiteSpace(line) ? line : "—");
                    ApplyFont(span);
                });
            }
        });
    }

    private static void ComposeLabeledValue(IContainer container, string label, string? value)
    {
        container.Column(col =>
        {
            col.Item().Text(text =>
            {
                var span = text.Span(label);
                span.Bold();
                ApplyFont(span);
            });

            var content = string.IsNullOrWhiteSpace(value) ? "—" : value;
            col.Item().Text(text =>
            {
                var span = text.Span(content);
                ApplyFont(span);
            });
        });
    }

    private static void AddLabelCell(
        ITableCellContainer cell,
        string text,
        TextHorizontalAlignment align)
    {
        cell.Element(c =>
        {
            // Alleen nodig als je het hele element wil positioneren in de cel
            var cont = align switch
            {
                TextHorizontalAlignment.Center => c.AlignCenter(),
                TextHorizontalAlignment.Right => c.AlignRight(),
                _ => c
            };

            cont.Padding(3).Text(t =>
            {
                // Tekst-uitlijning binnen het blok
                switch (align)
                {
                    case TextHorizontalAlignment.Left: t.AlignLeft(); break;
                    case TextHorizontalAlignment.Center: t.AlignCenter(); break;
                    case TextHorizontalAlignment.Right: t.AlignRight(); break;
                }

                var span = t.Span(text);
                span.FontSize(8);
                ApplyFont(span);
            });
        });
    }

    private static void AddHeaderCell(
        ITableCellContainer cell,
        string text,
        string background,
        TextHorizontalAlignment align)
    {
        cell.Element(c =>
        {
            c.Background(background)
             .Border(0.25f).BorderColor(BorderColor)
             .Padding(3)
             .PaddingLeft(7)
             .PaddingRight(7)
             .Text(t =>
             {
                 // uitlijning op de TextDescriptor
                 switch (align)
                 {
                     case TextHorizontalAlignment.Left: t.AlignLeft(); break;
                     case TextHorizontalAlignment.Center: t.AlignCenter(); break;
                     case TextHorizontalAlignment.Right: t.AlignRight(); break;
                 }

                 var span = t.Span(text);
                 ApplyFont(span);
                 span.Medium();
                 span.FontSize(8);
                 span.FontColor(Colors.White);
             });
        });
    }

    private static void AddValueCell(
        ITableCellContainer cell,
        string text,
        TextHorizontalAlignment align = TextHorizontalAlignment.Right, FontWeight weight = FontWeight.Normal)
    {
        cell.Element(c =>
        {
            c.Border(0.25f)
                .BorderColor(BorderColor)
                .Padding(3)
                 .PaddingLeft(7)
                 .PaddingRight(7)
                .Text(t =>
                {
                    // (optioneel) tekstregels zelf ook uitlijnen
                    switch (align)
                    {
                        case TextHorizontalAlignment.Left: t.AlignLeft(); break;
                        case TextHorizontalAlignment.Center: t.AlignCenter(); break;
                        case TextHorizontalAlignment.Right: t.AlignRight(); break;
                    }

                    var span = t.Span(text);
                    ApplyFont(span);
                    span.FontSize(8);
                    if (weight == FontWeight.Bold)
                        span.Bold();
                });
        });
    }
    private static void AddEmptyValueCell(ITableCellContainer cell)
    {
        cell.Element(c =>
        {
            c.Padding(3);
        });
    }


    private static string FormatCurrency(decimal value) => value.ToString("C", Culture);

    private static string FormatRate(decimal rate) => $"{rate:0.##}%";

    private static string? FormatPostalCity(string? postal, string? city)
    {
        if (string.IsNullOrWhiteSpace(postal) && string.IsNullOrWhiteSpace(city))
            return null;

        if (string.IsNullOrWhiteSpace(postal))
            return city;

        if (string.IsNullOrWhiteSpace(city))
            return postal;

        return $"{postal} {city}";
    }

    private static string? FormatCompanyVat(string? name, string? vat)
    {
        if (string.IsNullOrWhiteSpace(name))
            return vat;
        if (string.IsNullOrWhiteSpace(vat))
            return name;
        return $"{name} - {vat}";
    }
    private static string FormatDate(DateOnly? date)
        => date.HasValue ? date.Value.ToString("dd/MM/yyyy", Culture) : string.Empty;

    private static void ApplyFont(TextSpanDescriptor span)
    {
        span.FontFamily("Avenir");
    }

    private static float Mm(float value) => value * PointsPerMillimeter;
}