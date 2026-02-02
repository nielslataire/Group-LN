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
    private static readonly string DefaultPrimaryColor = Colors.Black;
    private static readonly string DefaultSecondaryColor = Colors.Grey.Darken2;

    public string SectionType => "defaultFooter";

    public void Render(ColumnDescriptor column, SectionConfig section, InvoiceVm vm, TemplateContext ctx)
    {
        if (section is not FooterSectionConfig footer || !section.Visible)
            return;

        column.Item()
            .ExtendVertical()
            .AlignBottom()
            .Column(col =>
            {
                col.Spacing(Mm(4f));
                col.Item()
                    .AlignRight()
                    .Shrink()
/*                    .MinimalBox() */               // krimpt tot minimale inhoudsbreedte
                    .Element(c => ComposeVatSummary(c, vm, ctx));
                col.Item().Element(container => ComposePaymentSection(container, vm, ctx));
                col.Item()
                    .Element(container => container
                        .PaddingTop(Mm(6f))
                        .Element(inner => ComposeContactSection(inner, vm)));
            });
    }

    private static void ComposeVatSummary(IContainer container, InvoiceVm vm, TemplateContext ctx)
    {
        var summaries = (vm.VatSummary?.Count > 0 ? vm.VatSummary : new[]
        {
            new VatRateSummaryVm { Rate = 0m, Net = vm.Totals.Excl, Vat = vm.Totals.Vat }
        }).ToList();

        var primary = string.IsNullOrWhiteSpace(ctx.PrimaryColorHex)
            ? DefaultPrimaryColor
            : ctx.PrimaryColorHex!;
        var borderColor = string.IsNullOrWhiteSpace(ctx.SecondaryColorHex)
            ? DefaultSecondaryColor
            : ctx.SecondaryColorHex!;

        var useFixedWidth = summaries.Count <= 4;
        var labelColumnWidth = Mm(20f);
        var vatColumnWidth = Mm(32f);
        var totalColumnWidth = Mm(32f);
        var maxVatColumnWidth = useFixedWidth ? vatColumnWidth : (float?)null;
        var tableWidth = useFixedWidth
            ? labelColumnWidth + (summaries.Count * vatColumnWidth) + totalColumnWidth
            : (float?)null;

        var tableContainer = container.AlignRight();
        if (tableWidth.HasValue)
            tableContainer = tableContainer.Width(tableWidth.Value);
        else
            tableContainer = tableContainer.Shrink();

        tableContainer.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                if (useFixedWidth)
                {
                    columns.ConstantColumn(labelColumnWidth);
                    foreach (var _ in summaries)
                        columns.ConstantColumn(vatColumnWidth);
                    columns.ConstantColumn(totalColumnWidth);
                }
                else
                {
                    columns.RelativeColumn(0.6f);
                    foreach (var _ in summaries)
                        columns.RelativeColumn();
                    columns.RelativeColumn(1.2f);
                }
            });

            AddLabelCell(table.Cell(), "BTW-tarief", TextHorizontalAlignment.Left);
            foreach (var summary in summaries)
                AddHeaderCell(table.Cell(), FormatRate(summary.Rate), primary, borderColor, TextHorizontalAlignment.Left, maxVatColumnWidth);
            AddHeaderCell(table.Cell(), "Totaal", primary, borderColor, TextHorizontalAlignment.Right);

            AddLabelCell(table.Cell(), "MVH", TextHorizontalAlignment.Left);
            foreach (var summary in summaries)
                AddValueCell(table.Cell(), FormatCurrency(summary.Net), borderColor, TextHorizontalAlignment.Left, FontWeight.Normal, maxVatColumnWidth);
            AddValueCell(table.Cell(), FormatCurrency(summaries.Sum(s => s.Net)), borderColor, TextHorizontalAlignment.Right);

            AddLabelCell(table.Cell(), "BTW", TextHorizontalAlignment.Left);
            foreach (var summary in summaries)
                AddValueCell(table.Cell(), FormatCurrency(summary.Vat), borderColor, TextHorizontalAlignment.Left, FontWeight.Normal, maxVatColumnWidth);
            AddValueCell(table.Cell(), FormatCurrency(summaries.Sum(s => s.Vat)), borderColor, TextHorizontalAlignment.Right);

            AddLabelCell(table.Cell(), string.Empty, TextHorizontalAlignment.Left);
            foreach (var _ in summaries)
                AddEmptyValueCell(table.Cell(), maxVatColumnWidth);
            var invoiceTotal = vm.Totals?.Incl ?? summaries.Sum(s => s.Total);
            AddTotalCell(table.Cell(), FormatCurrency(invoiceTotal), borderColor, TextHorizontalAlignment.Right, FontWeight.Bold);
        });
    }


    private static void ComposePaymentSection(IContainer container, InvoiceVm vm, TemplateContext ctx)
    {
        if (IsPrepaidInvoice(vm))
        {
            var invoiceType = IsCreditNote(vm) ? "creditnota" : "factuur";
            var message = $"Wij bevestigen de ontvangst van uw betaling. Het openstaand saldo van deze {invoiceType} bedraagt {FormatCurrency(0m)}.";

            container.Text(text =>
            {
                var span = text.Span(message);
                span.FontSize(9);
                span.Bold();
                ApplyFont(span);
            });
            return;
        }
        if (IsCreditNote(vm))
        {
            container.Column(col =>
            {
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

                ComposeVatMentions(col, vm);
            });

            return;
        }

        var isProforma = IsProformaInvoice(vm);
        var dueDateText = FormatDate(vm.Invoice.DueDate);
        var paymentDate = string.IsNullOrWhiteSpace(dueDateText) ? "—" : dueDateText;
        var customTerms = vm.Payment?.UsePaymentTermsText == true ? vm.Payment.Terms : null;

        var account = !string.IsNullOrWhiteSpace(vm.Payment?.Iban)
            ? vm.Payment!.Iban
            : !string.IsNullOrWhiteSpace(vm.Payment?.BankAccount)
                ? vm.Payment!.BankAccount
                : vm.IssuerCompany.IBAN;
        var formattedAccount = FormatBankAccount(account);
        var accountText = string.IsNullOrWhiteSpace(formattedAccount) ? "—" : formattedAccount!;

        var structured = !string.IsNullOrWhiteSpace(ctx.StructuredMessage)
            ? ctx.StructuredMessage
            : vm.Payment?.Structured;
        var structuredText = string.IsNullOrWhiteSpace(structured) ? "—" : structured!;

        var primaryColor = string.IsNullOrWhiteSpace(ctx.PrimaryColorHex)
            ? DefaultPrimaryColor
            : ctx.PrimaryColorHex!;

        container.Row(row =>
        {
            row.Spacing(Mm(5f));

            row.RelativeItem().Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(customTerms))
                {
                    col.Item().Text(text =>
                    {
                        var span = text.Span(customTerms);
                        span.FontSize(9);
                        span.Bold();
                        ApplyFont(span);
                    });
                }

                col.Item().Text(text =>
                {
                    var prefix = customTerms == null
                        ? text.Span($"Te betalen voor {paymentDate} op rekening ")
                        : text.Span("Gelieve te betalen op rekening ");
                    prefix.FontSize(9);
                    prefix.Bold();
                    ApplyFont(prefix);

                    var accountSpan = text.Span(accountText);
                    accountSpan.FontSize(9);
                    accountSpan.Bold();
                    if (!string.IsNullOrWhiteSpace(formattedAccount))
                        accountSpan.FontColor(primaryColor);
                    ApplyFont(accountSpan);


                    if (!isProforma)
                    {
                        var suffix = text.Span($" met gestructureerde mededeling {structuredText}");
                        suffix.FontSize(9);
                        suffix.Bold();
                        ApplyFont(suffix);
                    }
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
                ComposeVatMentions(col, vm);
            });

            if (!isProforma && vm.Payment?.QrEnabled == true && ctx.EpcQrPng is { Length: > 0 })
            {
                row.ConstantItem(Mm(32f))
                    .AlignRight()
                    .Image(ctx.EpcQrPng);
            }
        });
    }
    private static void ComposeVatMentions(ColumnDescriptor col, InvoiceVm vm)
    {
        if (vm?.VatMentions == null || vm.VatMentions.Count == 0)
            return;

        foreach (var mention in vm.VatMentions.Where(m => !string.IsNullOrWhiteSpace(m)))
        {
            col.Item().Text(text =>
            {
                var span = text.Span(mention);
                span.FontSize(7);
                span.Italic();
                ApplyFont(span);
            });
        }
    }
    private static bool IsCreditNote(InvoiceVm vm)
    {
        if (vm?.Totals?.Incl < 0)
            return true;

        var status = vm?.Invoice?.Status;
        return !string.IsNullOrWhiteSpace(status) && status.Contains("credit", StringComparison.OrdinalIgnoreCase);
    }
    private static bool IsPrepaidInvoice(InvoiceVm vm)
    {
        return vm?.Invoice?.IsPrepaid == true;
    }
    private static bool IsProformaInvoice(InvoiceVm vm)
    {
        var status = vm?.Invoice?.Status;
        if (string.IsNullOrWhiteSpace(status))
            return false;

        return status.Trim().Equals("Draft", StringComparison.OrdinalIgnoreCase)
            || status.Trim().Equals("Concept", StringComparison.OrdinalIgnoreCase);
    }
    private static void ComposeContactSection(IContainer container, InvoiceVm vm)
    {
        var iban = vm.IssuerCompany.DefaultIban ?? vm.Payment?.Iban ?? vm.IssuerCompany.IBAN;
        var formattedIban = FormatBankAccount(iban);

        var columns = new (IEnumerable<string?> Lines, TextHorizontalAlignment Align, bool Flexible)[]
        {
            (new[]
            {
                vm.IssuerCompany.AddressLine,
                FormatPostalCity(vm.IssuerCompany.Postal, vm.IssuerCompany.City)
            }, TextHorizontalAlignment.Left, false),
            (new[]
            {
                vm.IssuerCompany.Phone,
                vm.IssuerCompany.Phone2
            }, TextHorizontalAlignment.Left, false),
            (new[]
            {
                vm.IssuerCompany.Email,
                vm.IssuerCompany.Website
            }, TextHorizontalAlignment.Left, false),
            (new[]
            {
                FormatCompanyVat(vm.IssuerCompany.Name, vm.IssuerCompany.VAT, vm.IssuerCompany.LegalFormAbbreviation),
                formattedIban
            }, TextHorizontalAlignment.Right, true)
        };

        container.Row(row =>
        {
            row.Spacing(Mm(5f));

            foreach (var column in columns)
            {
                var lines = column.Lines
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l!.Trim())
                    .ToList();
                if (lines.Count == 0)
                    continue;

                if (column.Flexible)
                {
                    row.RelativeItem().Element(col => ComposeContactColumn(col, lines, column.Align));
                }
                else
                {
                    row.AutoItem()
                        .Element(col => col
                            .MinimalBox()
                            .Element(inner => ComposeContactColumn(inner, lines, column.Align)));
                }
            }
        });
    }
    private static void ComposeContactColumn(IContainer container, IEnumerable<string> lines, TextHorizontalAlignment align)
    {
        container.Column(col =>
        {
            foreach (var line in lines)
            {
                col.Item().Text(text =>
                {
                    switch (align)
                    {
                        case TextHorizontalAlignment.Center:
                            text.AlignCenter();
                            break;
                        case TextHorizontalAlignment.Right:
                            text.AlignRight();
                            break;
                        default:
                            text.AlignLeft();
                            break;
                    }

                    var span = text.Span(line);
                    span.FontSize(6.5f);
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
         string borderColor,
         TextHorizontalAlignment align,
         float? maxWidth = null)
    {
        cell.Element(c =>
        {
            var container = maxWidth.HasValue ? c.MaxWidth(maxWidth.Value) : c;

            container.Background(background)
             .Border(0.25f).BorderColor(borderColor)
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
          string borderColor,
          TextHorizontalAlignment align = TextHorizontalAlignment.Right,
          FontWeight weight = FontWeight.Normal,
          float? maxWidth = null)
    {
        cell.Element(c =>
        {
            var container = maxWidth.HasValue ? c.MaxWidth(maxWidth.Value) : c;

            container.Border(0.25f)
                .BorderColor(borderColor)
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
    private static void AddTotalCell(
      ITableCellContainer cell,
      string text,
      string borderColor,
      TextHorizontalAlignment align = TextHorizontalAlignment.Right,
      FontWeight weight = FontWeight.Normal,
      float? maxWidth = null)
    {
        cell.Element(c =>
        {
            var container = maxWidth.HasValue ? c.MaxWidth(maxWidth.Value) : c;

            container.Border(0.25f)
                .BorderColor(borderColor)
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
                    span.FontSize(11);
                    if (weight == FontWeight.Bold)
                        span.Bold();
                });
        });
    }
    private static void AddEmptyValueCell(ITableCellContainer cell, float? maxWidth = null)
    {
        cell.Element(c =>
        {
            var container = maxWidth.HasValue ? c.MaxWidth(maxWidth.Value) : c;
            container.Padding(3);
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

    private static string? FormatCompanyVat(string? name, string? vat, string? legalFormAbbreviation)
    {
        var displayName = AppendLegalForm(name, legalFormAbbreviation);

        if (string.IsNullOrWhiteSpace(displayName))
            return vat;
        if (string.IsNullOrWhiteSpace(vat))
            return displayName;
        return $"{displayName} - {vat}";
    }

    private static string? AppendLegalForm(string? name, string? legalFormAbbreviation)
    {
        var trimmedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var trimmedForm = string.IsNullOrWhiteSpace(legalFormAbbreviation) ? null : legalFormAbbreviation.Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
            return trimmedForm;

        if (string.IsNullOrWhiteSpace(trimmedForm))
            return trimmedName;

        return $"{trimmedName} {trimmedForm}";
    }

    private static string? FormatBankAccount(string? account)
    {
        if (string.IsNullOrWhiteSpace(account))
            return null;

        var normalized = new string(account.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        if (normalized.StartsWith("BE", StringComparison.Ordinal) &&
            normalized.Length == 16 &&
            normalized.Skip(2).All(char.IsDigit))
        {
            var checkDigits = normalized.Substring(2, 2);
            var remainder = normalized.Substring(4);
            return $"BE{checkDigits} {remainder.Substring(0, 4)} {remainder.Substring(4, 4)} {remainder.Substring(8, 4)}";
        }

        return account.Trim();
    }
    private static string FormatDate(DateOnly? date)
        => date.HasValue ? date.Value.ToString("dd/MM/yyyy", Culture) : string.Empty;

    private static void ApplyFont(TextSpanDescriptor span)
    {
        span.FontFamily("Avenir");
    }

    private static float Mm(float value) => value * PointsPerMillimeter;
}