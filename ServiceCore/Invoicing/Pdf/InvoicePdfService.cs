using BOCore;
using DALCore.Models;
using ServiceCore.Invoicing.Pdf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using ServiceCore.Invoicing.Pdf.Templates;
using System;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using System.Globalization;

namespace ServiceCore.Invoicing.Pdf;

public sealed class InvoicePdfService : IInvoicePdfService
{
    private readonly IInvoiceTemplateRegistry _templates;
    private readonly IEpcQrService _qrService;
    private readonly IStructuredReferenceService _structured;

    public InvoicePdfService(IInvoiceTemplateRegistry templates, IEpcQrService qrService, IStructuredReferenceService structured)
    {
        _templates = templates;
        _qrService = qrService;
        _structured = structured;
    }

    public byte[] Render(InvoiceDto invoice, IssuerCompanyBO company)
    {
        if (invoice == null)
            throw new ArgumentNullException(nameof(invoice));
        if (company == null)
            throw new ArgumentNullException(nameof(company));

        var templateKey = company.TemplateKey ?? "layoutA";
        var layout = LoadLayout(company.TemplateJson, templateKey);
        var structuredMessage = NormalizeStructuredMessage(invoice.StructuredMessage);
        var context = BuildContext(invoice, company, layout, structuredMessage);
        var vm = Map(invoice, company, structuredMessage);

        var template = _templates.Resolve(templateKey);
        return Document.Create(container => template.Compose(container, vm, context)).GeneratePdf();
    }

    private static LayoutConfig LoadLayout(string? templateJson, string templateKey)
    {
        var json = string.IsNullOrWhiteSpace(templateJson)
            ? DefaultLayouts.Get(templateKey)
            : templateJson;

        var token = JToken.Parse(json);
        var schema = LayoutSchemaProvider.GetSchema();
        IList<ValidationError> errors;
        if (!token.IsValid(schema, out errors))
        {
            var message = string.Join("; ", errors.Select(e => e.Message));
            throw new InvalidOperationException($"Layout JSON invalid: {message}");
        }

        var config = JsonConvert.DeserializeObject<LayoutConfig>(json);
        if (config == null)
            throw new InvalidOperationException("Kon layout niet laden.");
        return config;
    }

    private TemplateContext BuildContext(InvoiceDto invoice, IssuerCompanyBO company, LayoutConfig layout, string? structuredMessage)
    {
        var theme = layout.Theme ?? new ThemeConfig();
        var primary = !string.IsNullOrWhiteSpace(theme.Primary) ? theme.Primary : company.BrandPrimaryColor;
        var secondary = !string.IsNullOrWhiteSpace(theme.Secondary) ? theme.Secondary : company.BrandSecondaryColor;
        const string fontFamily = "Avenir";
        var logo = ResolveLogo(theme.LogoSource, company);

        byte[]? qr = null;
        if (company.EpcQrEnabled && !string.IsNullOrWhiteSpace(company.EpcBeneficiaryName)
            && !string.IsNullOrWhiteSpace(company.EpcIban)
            && !string.IsNullOrWhiteSpace(structuredMessage))
        {
            qr = _qrService.CreatePng(
                company.EpcBeneficiaryName,
                company.EpcIban,
                company.EpcBic ?? string.Empty,
                invoice.Totals.Incl,
                structuredMessage ?? string.Empty);
        }

        return new TemplateContext
        {
            PrimaryColorHex = primary,
            SecondaryColorHex = secondary,
            FontFamily = fontFamily,
            Logo = logo,
            FooterLegalText = company.FooterLegalText,
            StructuredMessage = structuredMessage,
            EpcQrPng = qr,
            Layout = layout
        };
    }

    private static InvoiceVm Map(InvoiceDto dto, IssuerCompanyBO company, string? structuredMessage)
    {
        return new InvoiceVm
        {
            Invoice = new InvoiceInfoVm
            {
                Id = dto.Id,
                PublicId = dto.PublicId,
                IssueDate = dto.IssueDate,
                DueDate = dto.DueDate,
                Status = dto.Status
            },
            IssuerCompany = new CompanyVm
            {
                Name = dto.Issuer.Name ?? company.Name,
                LegalName = dto.Issuer.LegalName ?? company.LegalName,
                VAT = dto.Issuer.VatNumber ?? company.VatNumber,
                AddressLine = dto.Issuer.AddressLine1 ?? CombineAddress(company.AddressLine1, company.AddressLine2),
                Postal = dto.Issuer.PostalCode ?? company.PostalCode,
                City = dto.Issuer.City ?? company.City,
                Country = dto.Issuer.Country ?? company.CountryCode,
                Email = dto.Issuer.Email ?? company.Email,
                Phone = dto.Issuer.Phone ?? company.Phone,
                Phone2 = dto.Issuer.Phone2 ?? company.Phone2,
                IBAN = dto.Issuer.BankAccount ?? company.EpcIban,
                BIC = dto.Issuer.Bic ?? company.EpcBic,
                Website = dto.Issuer.Website ?? company.Website
            },
            Client = new CompanyVm
            {
                Name = dto.Client.Name,
                LegalName = dto.Client.LegalName,
                VAT = dto.Client.VatNumber,
                AddressLine = dto.Client.AddressLine1,
                Postal = dto.Client.PostalCode,
                City = dto.Client.City,
                Country = dto.Client.Country,
                Email = dto.Client.Email,
                Phone = dto.Client.Phone
            },
            Project = new ProjectVm { Name = dto.Project.Name },
            Unit = new UnitVm { Name = dto.Unit.Name, Address = dto.Unit.Address },
            Totals = new TotalsVm { Excl = dto.Totals.Excl, Vat = dto.Totals.Vat, Incl = dto.Totals.Incl },
            Payment = new PaymentVm
            {
                Structured = structuredMessage ?? dto.StructuredMessage,
                BankAccount = dto.BankAccount,
                Iban = dto.Issuer.BankAccount ?? company.EpcIban,
                Bic = dto.Issuer.Bic ?? company.EpcBic,
                QrEnabled = company.EpcQrEnabled,
                Terms = BuildPaymentTerms(dto)
            },
            VatSummary = BuildVatSummary(dto),
            ExtraInfo = dto.ExtraInfo,
            Lines = dto.Lines.Select(line => new InvoiceLineVm
            {
                Key = line.Key,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Vat = line.Vat,
                Total = line.Total,
                Extras = line.Extras
            }).ToList()
        };
    }

    private static byte[]? ResolveLogo(string? logoSource, IssuerCompanyBO company)
    {
        if (string.IsNullOrWhiteSpace(logoSource))
            return company.LogoBytes;

        if (logoSource.Equals("db:IssuerCompany.LogoBytes", StringComparison.OrdinalIgnoreCase))
            return company.LogoBytes;

        return null;
    }

    private string? NormalizeStructuredMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var trimmed = message.Trim();
        if (trimmed.StartsWith("+++"))
            return trimmed;
        if (trimmed.StartsWith("RF", StringComparison.OrdinalIgnoreCase))
            return trimmed.ToUpperInvariant();
        return trimmed.All(char.IsDigit) && trimmed.Length == 10
            ? _structured.CreateOgm(trimmed)
            : _structured.CreateRf(trimmed);
    }
    private static IReadOnlyList<VatRateSummaryVm> BuildVatSummary(InvoiceDto dto)
    {
        if (dto == null)
            return Array.Empty<VatRateSummaryVm>();

        if (dto.Lines == null || dto.Lines.Count == 0)
        {
            return new[]
            {
                new VatRateSummaryVm
                {
                    Rate = 0m,
                    Net = dto.Totals?.Excl ?? 0m,
                    Vat = dto.Totals?.Vat ?? 0m
                }
            };
        }

        var summaries = dto.Lines
            .GroupBy(line => line.Vat)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var net = group.Sum(line => RoundCurrency(line.UnitPrice * line.Quantity));
                var gross = group.Sum(line => RoundCurrency(line.Total));
                var vat = RoundCurrency(gross - net);
                return new VatRateSummaryVm
                {
                    Rate = group.Key,
                    Net = net,
                    Vat = vat
                };
            })
            .ToList();

        if (summaries.Count == 0)
        {
            summaries.Add(new VatRateSummaryVm
            {
                Rate = 0m,
                Net = dto.Totals?.Excl ?? 0m,
                Vat = dto.Totals?.Vat ?? 0m
            });
        }

        return summaries;
    }

    private static string? BuildPaymentTerms(InvoiceDto dto)
    {
        if (dto?.DueDate is not { } due)
            return null;

        var issue = dto.IssueDate;
        var days = due.DayNumber - issue.DayNumber;
        if (days > 0)
            return $"Te betalen binnen {days} dagen (vóór {due:dd/MM/yyyy})";

        return $"Te betalen vóór {due:dd/MM/yyyy}";
    }

    private static decimal RoundCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string? CombineAddress(string? line1, string? line2)
    {
        if (string.IsNullOrWhiteSpace(line1))
            return line2;
        if (string.IsNullOrWhiteSpace(line2))
            return line1;
        return $"{line1}, {line2}";
    }
}