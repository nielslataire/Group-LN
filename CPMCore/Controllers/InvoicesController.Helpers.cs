using BOCore;
using CPMCore.Documents;
using CPMCore.Extensions;
using CPMCore.Models.Invoicing;
using CPMCore.Services;
using CPMCore.Services.Octopus;
using CPMCore.Services.Peppol;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using ServiceCore;
using ServiceCore.Invoicing;
using ServiceCore.Invoicing.Pdf;
using SmartBreadcrumbs.Nodes;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

namespace CPMCore.Controllers
{
    /// <summary>Gedeelde helpers: breadcrumbs, e-mailsjabloonmodel, sortering, custom fields, bestandsnamen, statusbepaling, issuer-scope; geen eigen views. Opgesplitst uit InvoicesController.cs (okt. 2026, structureren) - views in Views/Invoices/Helpers/. Zelfde partial class: alle private velden/services van InvoicesController.cs blijven gewoon bruikbaar.</summary>
    public partial class InvoicesController
    {
        private async Task SetIssuerViewBagsAsync(int issuerId, CancellationToken ct)
        {
            ViewBag.CompanyId = issuerId;
            ViewBag.CompanyName = await _companies.GetIssuerNameAsync(issuerId, ct);
        }

        private static MvcBreadcrumbNode CreateHomeNode()
        {
            return new MvcBreadcrumbNode("Index", "Home", "Dashboard");
        }

        private MvcBreadcrumbNode CreateIndexNode(int issuerId, string? companyName)
        {
            var home = CreateHomeNode();
            var title = string.IsNullOrWhiteSpace(companyName) ? "Facturen" : $"Facturen - {companyName.Trim()}";
            var index = new MvcBreadcrumbNode(nameof(Index), ControllerName, title)
            {
                Parent = home,
                RouteValues = issuerId > 0 ? new { issuerCompanyId = issuerId } : null
            };
            return index;
        }

        private void SetIndexBreadcrumb(int issuerId, string? companyName)
        {
            ViewData["BreadcrumbNode"] = CreateIndexNode(issuerId, companyName);
        }

        // Kruimelpad stopt bij "Facturen - {bedrijf}" (wáár dit zit) i.p.v. nog een knoop toe te voegen
        // die letterlijk de paginatitel herhaalt (design-handoff punt 13: het laatste kruimelitem is
        // nooit de titel zelf — de Detail-pagina se eigen ViewData["Title"] is die "wát"-knoop al).
        // Send/Edit blijven wél hun eigen "Factuur X"-tussenstap gebruiken (SetSendBreadcrumb/
        // SetEditBreadcrumb hieronder) — daar ís het geen dubbel, want de paginatitel daar is
        // "Verzenden"/"Bewerken", niet de factuur zelf.
        private void SetDetailBreadcrumb(int issuerId, string? companyName)
        {
            ViewData["BreadcrumbNode"] = CreateIndexNode(issuerId, companyName);
        }

        private void SetSendBreadcrumb(int issuerId, string? companyName, int invoiceId, string detailTitle)
        {
            var index = CreateIndexNode(issuerId, companyName);
            var title = string.IsNullOrWhiteSpace(detailTitle) ? "Factuur detail" : detailTitle;
            var detail = new MvcBreadcrumbNode(nameof(Detail), ControllerName, title)
            {
                Parent = index,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };
            var send = new MvcBreadcrumbNode(nameof(Send), ControllerName, "Verzenden")
            {
                Parent = detail,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };
            ViewData["BreadcrumbNode"] = send;
        }
        private void SetEditBreadcrumb(int issuerId, string? companyName, int invoiceId, string detailTitle)
        {
            var index = CreateIndexNode(issuerId, companyName);
            var title = string.IsNullOrWhiteSpace(detailTitle) ? "Factuur detail" : detailTitle;
            var detail = new MvcBreadcrumbNode(nameof(Detail), ControllerName, title)
            {
                Parent = index,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };

            var edit = new MvcBreadcrumbNode(nameof(Edit), ControllerName, "Bewerken")
            {
                Parent = detail,
                RouteValues = issuerId > 0 ? new { id = invoiceId, issuerCompanyId = issuerId } : new { id = invoiceId }
            };

            ViewData["BreadcrumbNode"] = edit;
        }
        private void SetCreateBreadcrumb(int issuerId, string? companyName)
        {
            var index = CreateIndexNode(issuerId, companyName);
            var create = new MvcBreadcrumbNode(nameof(Create), ControllerName, "Nieuwe factuur")
            {
                Parent = index,
                RouteValues = issuerId > 0 ? new { issuerId } : null
            };

            ViewData["BreadcrumbNode"] = create;
        }
        private static string? GetDueDateDisplayText(InvoiceDetailBO detail, CultureInfo culture)
        {
            if (detail.ExpirationDate.HasValue)
            {
                return detail.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", culture);
            }

            if (detail.PaymentTermDisplayMode == PaymentTermDisplayMode.Text
                && !string.IsNullOrWhiteSpace(detail.PaymentTermDisplayText))
            {
                return detail.PaymentTermDisplayText.Trim();
            }

            return null;
        }


        private object BuildEmailTemplateModel(InvoiceDetailBO detail, IssuerCompanyBO issuer, string? bankAccount, string currency)
        {
            var culture = CultureInfo.GetCultureInfo("nl-BE");
            var dueDateDisplay = GetDueDateDisplayText(detail, culture);
            var formattedBankAccount = FormatBankAccount(bankAccount);
            return new
            {
                Invoice = new
                {
                    detail.Id,
                    detail.PublicId,
                    IssueDate = detail.InvoiceDate,
                    DueDate = detail.ExpirationDate,
                    DueDateDisplay = dueDateDisplay,
                    TotalExcl = detail.TotalExclVat,
                    TotalVat = detail.TotalVat,
                    TotalIncl = detail.TotalInclVat,
                    Type = detail.IsCreditNote ? "Creditnota" : "Factuur",
                    IsCreditNote = detail.IsCreditNote
                },
                Client = new
                {
                    Name = detail.ClientName,
                    VatNumber = detail.ClientVatNumber,
                    Email = detail.ClientEmail
                },
                Issuer = new
                {
                    issuer.Name,
                    issuer.LegalName,
                    issuer.Email,
                    issuer.Phone
                },
                Payment = new
                {
                    BankAccount = formattedBankAccount,
                    Currency = currency,
                    StructuredMessage = detail.StructuredMessage,
                    DueDateDisplay = dueDateDisplay
                }
            };
        }

        private static string BuildDefaultEmailBody(InvoiceDetailBO detail, IssuerCompanyBO issuer, string currency, string? bankAccount)
        {
            var culture = CultureInfo.GetCultureInfo("nl-BE");
            var amount = WebUtility.HtmlEncode(detail.TotalInclVat.ToString("C", culture));
            var invoiceId = WebUtility.HtmlEncode(detail.PublicId ?? detail.Id.ToString(CultureInfo.InvariantCulture));
            var clientName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(detail.ClientName) ? "klant" : detail.ClientName);
            var builder = new StringBuilder();
            _ = currency;
            var formattedBankAccount = FormatBankAccount(bankAccount);

            builder.Append("<p>Beste ");
            builder.Append(clientName);
            builder.Append(",</p>");

            builder.Append("<p>In de bijlage vind je factuur ");
            builder.Append(invoiceId);
            builder.Append(" met een totaalbedrag van <strong>");
            builder.Append(amount);
            builder.Append("</strong>.");

            var dueDateDisplay = GetDueDateDisplayText(detail, culture);
            if (!string.IsNullOrWhiteSpace(dueDateDisplay))
            {
                var dueDate = detail.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", culture);
                builder.Append(" Gelieve dit bedrag te voldoen vóór ");
                builder.Append(WebUtility.HtmlEncode(dueDateDisplay));
                builder.Append('.');
            }

            builder.Append("</p>");

            if (!string.IsNullOrWhiteSpace(formattedBankAccount))
            {
                builder.Append("<p>Betaling kan via <strong>");
                builder.Append(WebUtility.HtmlEncode(formattedBankAccount));
                builder.Append("</strong>.</p>");
            }

            builder.Append("<p>Met vriendelijke groeten,<br/>");
            builder.Append(WebUtility.HtmlEncode(issuer.Name ?? string.Empty));
            builder.Append("</p>");

            return builder.ToString();
        }

        private static decimal RoundCurrency(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength);
        }

        private static (int? Number, int? Month, int? Year) ParseInvoicePublicId(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return (null, null, null);

            var parts = publicId
                .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            int? ParsePart(int index)
            {
                if (index >= parts.Length)
                    return null;

                return int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : (int?)null;
            }

            var number = ParsePart(0);
            var month = ParsePart(1);
            var year = ParsePart(2);

            return (number, month, year);
        }

        private static long BuildInvoiceSortValue(DateOnly invoiceDate, int? number, int? month, int? year, int fallbackId)
        {
            if (year.HasValue)
                return ComposeSortValue(year.Value, month, number);

            var fallbackSequence = (invoiceDate.DayNumber % 1_000_000) * 10 + Math.Abs(fallbackId % 10);
            return ComposeSortValue(invoiceDate.Year, invoiceDate.Month, fallbackSequence);
        }

        private static long ComposeSortValue(int year, int? month, int? number)
        {
            var monthValue = Clamp(month, 0, 999);
            var numberValue = Clamp(number, 0, 999_999);
            return (long)year * 1_000_000_000L + (long)monthValue * 1_000_000L + numberValue;
        }

        private static int Clamp(int? value, int min, int max)
        {
            if (!value.HasValue)
                return min;

            if (value.Value < min)
                return min;

            if (value.Value > max)
                return max;

            return value.Value;
        }

        private static string? NormalizeMultiline(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            return normalized.Replace("\n", Environment.NewLine);
        }

        private List<OctopusCustomFieldValue> BuildInvoiceCustomFieldValues(Invoices invoice, IssuerCompany issuer, string? formattedPublicId)
        {
            var results = new List<OctopusCustomFieldValue>();
            var mappings = DeserializeInvoiceCustomFieldMappings(issuer.OctopusCustomFieldMappingsJson);

            foreach (var mapping in mappings)
            {
                var value = ResolveInvoiceFieldValue(invoice, mapping.InvoiceField, formattedPublicId);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    results.Add(new OctopusCustomFieldValue
                    {
                        CustomFieldKey = new OctopusCustomFieldKeyRef { Id = mapping.CustomFieldKeyId },
                        Value = value
                    });
                }
            }

            if (issuer.OctopusDownloadLinkCustomFieldKeyId.HasValue)
            {
                var link = BuildInvoiceDownloadLink(invoice, formattedPublicId);
                if (!string.IsNullOrWhiteSpace(link))
                {
                    var existing = results.FirstOrDefault(r => r.CustomFieldKey?.Id == issuer.OctopusDownloadLinkCustomFieldKeyId.Value);
                    if (existing != null)
                    {
                        existing.Value = link;
                    }
                    else
                    {
                        results.Add(new OctopusCustomFieldValue
                        {
                            CustomFieldKey = new OctopusCustomFieldKeyRef { Id = issuer.OctopusDownloadLinkCustomFieldKeyId.Value },
                            Value = link
                        });
                    }
                }
            }

            return results;
        }

        private static List<OctopusCustomFieldMappingBO> DeserializeInvoiceCustomFieldMappings(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<OctopusCustomFieldMappingBO>();
            }

            return JsonSerializer.Deserialize<List<OctopusCustomFieldMappingBO>>(json)
                   ?? new List<OctopusCustomFieldMappingBO>();
        }

        private static string? ResolveInvoiceFieldValue(Invoices invoice, string? invoiceField, string? formattedPublicId)
        {
            return invoiceField?.ToUpperInvariant() switch
            {
                "PUBLICID" => string.IsNullOrWhiteSpace(invoice.PublicId) ? formattedPublicId : invoice.PublicId,
                "STRUCTUREDCOMMOGM" => invoice.StructuredCommOgm,
                "CLIENTNAME" => invoice.ClientName
                    ?? JoinNameForename(invoice.ClientIdClientAccountNavigation?.Name, invoice.ClientIdClientAccountNavigation?.Forename)
                    ?? JoinNameForename(invoice.ClientIdClientContactsNavigation?.Name, invoice.ClientIdClientContactsNavigation?.Forename),
                "VATNUMBER" => invoice.VatNumber,
                "VATREGIME" => invoice.VatRegime,
                "CURRENCYCODE" => invoice.CurrencyCode,
                "BANKACCOUNT" => invoice.BankAccount,
                "HEADERDESCRIPTION" => invoice.HeaderDescription,
                "TEXT" => invoice.Text,
                "EXTRAINFO" => invoice.ExtraInfo,
                "QREPCPAYLOAD" => invoice.QrEpcPayload,
                "INVOICEMODE" => invoice.InvoiceMode?.ToString(CultureInfo.InvariantCulture),
                "DATE" => invoice.Date.ToString("yyyy-MM-dd"),
                "EXPIRATIONDATE" => invoice.ExpirationDate?.ToString("yyyy-MM-dd"),
                _ => null
            };
        }

        private string BuildInvoiceDownloadLink(Invoices invoice, string? formattedPublicId)
        {
            var tokenPayload = $"{invoice.Id}|{invoice.PublicId ?? string.Empty}|{formattedPublicId ?? string.Empty}";
            var token = _downloadLinkProtector.Protect(tokenPayload);

            return Url.Action(nameof(OctopusDownload), ControllerName, new { id = invoice.Id, token }, Request.Scheme)
                ?? string.Empty;
        }
       
        private static InvoiceStatus TranslateStatus(string? status) => InvoiceStatusExtensions.FromCode(status);

        private static InvoiceStatus TranslateStatus(int? statusId, string? statusName)
            => statusId.HasValue ? InvoiceStatusExtensions.FromId(statusId) : InvoiceStatusExtensions.FromCode(statusName);

        private static string BuildInvoicePdfFileName(InvoiceDetailBO detail, InvoiceDto dto)
        {
            var publicId = string.IsNullOrWhiteSpace(dto.PublicId) ? detail.PublicId : dto.PublicId;
            var recipient = BuildInvoiceRecipientName(detail);
            var baseName = string.IsNullOrWhiteSpace(publicId)
                ? $"Factuur_{detail.Id}"
                : $"{publicId} - {recipient}";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(baseName.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());
            return $"{sanitized}.pdf";
        }

        private static string BuildInvoiceRecipientName(InvoiceDetailBO detail)
        {
            if (detail == null)
                return "Onbekend";

            if (detail.IsSupplier || detail.ClientType == (int)InvoicePartyType.Supplier)
                return string.IsNullOrWhiteSpace(detail.ClientName) ? "Leverancier" : detail.ClientName;

            if (detail.ClientType == (int)InvoicePartyType.ClientContact)
                return FormatContactName(detail.ClientName);

            return string.IsNullOrWhiteSpace(detail.ClientName) ? "Klant" : detail.ClientName;
        }

        private static string FormatContactName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Contact";

            if (name.Contains(" - ", StringComparison.Ordinal))
                return name;

            if (name.Contains(",", StringComparison.Ordinal))
            {
                var parts = name.Split(',', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                    return $"{parts[0]} - {parts[1]}";
            }

            var tokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length >= 2)
            {
                var lastName = tokens[0];
                var firstName = string.Join(" ", tokens.Skip(1));
                return $"{lastName} - {firstName}";
            }

            return name;
        }

        private static bool IsDraftStatus(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
                return false;

            return string.Equals(statusName, "Draft", StringComparison.OrdinalIgnoreCase)
                || string.Equals(statusName, InvoiceStatus.Draft.GetDisplayName(), StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> IsInvoiceGeneratingAsync(int invoiceId, CancellationToken ct)
        {
            var statusId = await _db.Invoices
                .Where(i => i.Id == invoiceId)
                .Select(i => i.StatusId)
                .FirstOrDefaultAsync(ct);

            return InvoiceStatusExtensions.FromId(statusId) == InvoiceStatus.Generating;
        }
        private static bool WasSentViaPeppol(OctopusInvoiceSendResponse? sendResponse)
        {
            return sendResponse?.SendInvoiceStatusList?.Any(status =>
                !string.IsNullOrWhiteSpace(status.SendMethod)
                && status.SendMethod.Contains("PEPPOL", StringComparison.OrdinalIgnoreCase))
                ?? false;
        }

        private static string DetermineSendMethod(string? deliveryState, bool hasEmailLog, bool needsPrint)
        {
            if (!string.IsNullOrWhiteSpace(deliveryState))
            {
                var normalized = deliveryState.ToUpperInvariant();
                if (normalized.Contains("PEPPOL"))
                    return "Peppol";

                if (normalized.Contains("EMAIL"))
                    return "E-mail";
            }

            if (hasEmailLog)
                return "E-mail";

            if (needsPrint)
                return "Print";

            return "Niet verzonden";
        }
        private static bool DetermineCreditNote(bool isCreditSeries, string? status, decimal? totalInclVat)
        {
            if (isCreditSeries)
                return true;

            if (!string.IsNullOrWhiteSpace(status) && status.Contains("credit", StringComparison.OrdinalIgnoreCase))
                return true;

            if (totalInclVat.HasValue && totalInclVat.Value < 0m)
                return true;

            return false;
        }
        private static string BuildAddress(string? street, string? house) =>
    string.IsNullOrWhiteSpace(street) ? "" : (street + (string.IsNullOrWhiteSpace(house) ? "" : $" {house}")).Trim();

        // helper om standaardtekst te bouwen
        private static string BuildStageDescription(UnitStageRow r, decimal ownerPct = 100m)
        {
            var unitTypePart = string.IsNullOrWhiteSpace(r.UnitType) ? "" : (r.UnitType + " ");
            var unitAddr = BuildAddress(r.UnitStreet, r.UnitHouseNumber);
            var projAddr = BuildAddress(r.ProjectStreet, r.ProjectHouseNumber);
            var addr = !string.IsNullOrWhiteSpace(unitAddr) ? unitAddr : projAddr;

            var line1 = $"Voor de bouwwaarde van {unitTypePart}{r.UnitName} in project {r.ProjectName}, {addr} te {r.ProjectCity} ingevolge verkoopsovereenkomst.";
            var line2 = $"{ownerPct.ToString("0.##", CultureInfo.InvariantCulture)} % van de bouwwaarde van {unitTypePart}{r.UnitName} : {r.UnitConstructionTotal.ToString("N2")} €";

            // in één regel zodat je disabled input het netjes toont
            return $"{line1} {line2}";
        }

        private async Task<InvoicingIssuerScope> ResolveInvoicingIssuerScopeAsync(PermissionAccessType accessType, CancellationToken ct)
        {
            var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            await permissionService.EnsureLoadedAsync(ct);

            var hasMainAccess = accessType switch
            {
                PermissionAccessType.Read => permissionService.HasRead(PermissionCodes.Invoicing),
                PermissionAccessType.Write => permissionService.HasWrite(PermissionCodes.Invoicing),
                PermissionAccessType.Delete => permissionService.HasDelete(PermissionCodes.Invoicing),
                _ => false
            };

            if (!hasMainAccess)
            {
                return new InvoicingIssuerScope(false, false, new HashSet<int>());
            }

            var scopedIds = permissionService.EffectivePermissions
                .Where(x => x.Key.StartsWith(InvoicingCompanyPermissionPrefix, StringComparison.OrdinalIgnoreCase))
                .Where(x => accessType switch
                {
                    PermissionAccessType.Read => x.Value.Read,
                    PermissionAccessType.Write => x.Value.Write,
                    PermissionAccessType.Delete => x.Value.Delete,
                    _ => false
                })
                .Select(x =>
                {
                    var suffix = x.Key[InvoicingCompanyPermissionPrefix.Length..];
                    return int.TryParse(suffix, out var companyId) ? (int?)companyId : null;
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (scopedIds.Count == 0)
            {
                return new InvoicingIssuerScope(true, true, new HashSet<int>());
            }

            return new InvoicingIssuerScope(true, false, scopedIds.ToHashSet());
        }
        public enum InvoiceSendFormMode
        {
            Standard,
            Copy
        }
        private sealed record InvoicingIssuerScope(bool HasAccess, bool HasAllIssuers, HashSet<int> AllowedIssuerIds);

    }
}
