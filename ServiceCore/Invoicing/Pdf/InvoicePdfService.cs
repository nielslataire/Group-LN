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
        var backgroundImage = ResolveImageAsset(layout.Page?.BackgroundImage, company);

        byte[]? qr = null;
        if (company.EpcQrEnabled)
        {
            if (!string.IsNullOrWhiteSpace(invoice.QrPayload))
            {
                qr = _qrService.CreatePngFromPayload(invoice.QrPayload);
            }
            else if (!string.IsNullOrWhiteSpace(company.EpcBeneficiaryName)
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
            PageBackgroundImage = backgroundImage,
            Layout = layout
        };
    }

    private static InvoiceVm Map(InvoiceDto dto, IssuerCompanyBO company, string? structuredMessage)
    {
        var defaultIban = !string.IsNullOrWhiteSpace(company.DefaultBankAccountIban)
            ? company.DefaultBankAccountIban
            : dto.Issuer.BankAccount;
        var defaultBic = !string.IsNullOrWhiteSpace(company.DefaultBankAccountBic)
            ? company.DefaultBankAccountBic
            : dto.Issuer.Bic;
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
                IBAN = dto.Issuer.BankAccount ?? defaultIban ?? company.EpcIban,
                BIC = dto.Issuer.Bic ?? defaultBic ?? company.EpcBic,
                Website = dto.Issuer.Website ?? company.Website,
                LegalFormAbbreviation = dto.Issuer.LegalFormAbbreviation ?? company.CompanyLegalFormAbbreviation,
                DefaultIban = defaultIban ?? company.EpcIban,
                DefaultBic = defaultBic ?? company.EpcBic
            },
            Client = new CompanyVm
            {
                Name = dto.Client.Name,
                LegalName = dto.Client.LegalName,
                VAT = FormatEuropeanVatNumber(dto.Client.VatNumber, dto.Client.Country ?? company.CountryCode),
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
                Iban = !string.IsNullOrWhiteSpace(dto.BankAccount)
                    ? dto.BankAccount
                    : dto.Issuer.BankAccount ?? company.EpcIban,
                Bic = dto.Issuer.Bic ?? company.EpcBic,
                QrEnabled = company.EpcQrEnabled && (!string.IsNullOrWhiteSpace(structuredMessage ?? dto.StructuredMessage) || !string.IsNullOrWhiteSpace(dto.QrPayload)),
                Terms = BuildPaymentTerms(dto)
            },
            VatSummary = BuildVatSummary(dto),
            VatMentions = BuildVatMentions(dto),
            ExtraInfo = dto.ExtraInfo,
            HeaderDescription = dto.HeaderDescription,
            DetailDescription = dto.DetailDescription,
            Lines = dto.Lines.Select(line => new InvoiceLineVm
            {
                Key = line.Key,
                Description = line.Description,
                LineType = line.LineType,
                GroupName = line.GroupName,
                UnitId = line.UnitId,
                PaymentStageId = line.PaymentStageId,
                ChangeOrderDetailId = line.ChangeOrderDetailId,
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
        return ResolveImageAsset(logoSource, company) ?? company.LogoBytes;
    }

    private static string? FormatEuropeanVatNumber(string? vatNumber, string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(vatNumber))
            return vatNumber;

        var cleaned = new string(vatNumber.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(cleaned))
            return vatNumber;

        var country = (countryCode ?? string.Empty).Trim();
        if (country.Length > 2)
            country = country.Substring(0, 2);
        country = country.ToUpperInvariant();

        if (country.Length == 2 && cleaned.StartsWith(country, StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(country.Length);
        }

        var digitsOnly = new string(cleaned.Where(char.IsDigit).ToArray());

        if (country == "BE")
        {
            var belgianNumber = digitsOnly;
            if (belgianNumber.Length == 9)
                belgianNumber = "0" + belgianNumber;

            if (belgianNumber.Length == 10)
            {
                return $"{country} {belgianNumber.Substring(0, 4)}.{belgianNumber.Substring(4, 3)}.{belgianNumber.Substring(7, 3)}";
            }
        }

        var numberPart = digitsOnly.Length > 0 ? digitsOnly : cleaned;
        return country.Length == 2 ? $"{country} {numberPart}" : numberPart;
    }

    private static byte[]? ResolveImageAsset(string? source, IssuerCompanyBO company)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = source.IndexOf(',', StringComparison.Ordinal);
            if (commaIndex >= 0 && commaIndex < source.Length - 1)
            {
                var base64 = source[(commaIndex + 1)..];
                try
                {
                    return Convert.FromBase64String(base64);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        if (source.StartsWith("db:", StringComparison.OrdinalIgnoreCase))
        {
            var key = source[3..];
            if (key.Equals("IssuerCompany.LogoBytes", StringComparison.OrdinalIgnoreCase))
                return company.LogoBytes;
        }

        if (source.StartsWith("theme:", StringComparison.OrdinalIgnoreCase))
        {
            var key = source[6..];
            if (key.Equals("logo", StringComparison.OrdinalIgnoreCase))
                return company.LogoBytes;
        }

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

    private static IReadOnlyList<string> BuildVatMentions(InvoiceDto dto)
    {
        if (dto?.VatTypes == null || dto.VatTypes.Count == 0 || dto.Lines == null || dto.Lines.Count == 0)
        {
            return Array.Empty<string>();
        }

        var usedTypeIds = dto.Lines
            .Select(l => l.VatTypeId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var usedRates = dto.Lines
            .Select(l => l.Vat)
            .ToList();

        var mentions = new List<string>();
        foreach (var vat in dto.VatTypes)
        {
            if (string.IsNullOrWhiteSpace(vat.InvoiceMention))
                continue;

            var matchesType = usedTypeIds.Contains(vat.Id);
            var matchesRate = usedRates.Any(rate => Math.Abs(rate - vat.BasePercentage) < 0.0001m);
            if (!matchesType && !matchesRate)
                continue;

            var mention = vat.InvoiceMention.Trim();
            if (string.IsNullOrWhiteSpace(mention))
                continue;

            if (mentions.Any(m => string.Equals(m, mention, StringComparison.OrdinalIgnoreCase)))
                continue;

            mentions.Add(mention);
        }

        return mentions;
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