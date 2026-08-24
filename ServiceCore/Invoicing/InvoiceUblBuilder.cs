using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using BOCore;

namespace ServiceCore.Invoicing
{
    public class InvoiceUblBuilder : IInvoiceUblBuilder
    {
        private const string CustomizationId =
            "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0";

        private const string ProfileId =
            "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0";

        public InvoiceUblDocument Build(InvoiceDetailBO detail, IssuerCompanyBO issuer)
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));
            if (issuer == null) throw new ArgumentNullException(nameof(issuer));

            var currency = !string.IsNullOrWhiteSpace(issuer.DefaultCurrency)
                ? issuer.DefaultCurrency
                : "EUR";

            var invoiceId = string.IsNullOrWhiteSpace(detail.PublicId)
                ? detail.Id.ToString(CultureInfo.InvariantCulture)
                : detail.PublicId;

            var issueDate = detail.InvoiceDate.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dueDate = detail.ExpirationDate?.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var lines = detail.Lines ?? new List<InvoiceLineBO>();
            var lineData = lines.Select((line, index) => BuildLineData(line, index + 1)).ToList();

            // Btw per tarief op de maatstaf berekenen en terugverdelen over de lijnen,
            // zodat TaxAmount = TaxableAmount × tarief (Peppol BR-CO-17) en alles optelt.
            var allocatedVat = InvoiceVatCalculator.AllocateLineVat(
                lineData.Select(l => (l.Net, l.VatRate)).ToList());
            lineData = lineData
                .Select((l, i) => l with
                {
                    VatAmount = allocatedVat[i],
                    Gross = Math.Round(l.Net + allocatedVat[i], 2, MidpointRounding.AwayFromZero)
                })
                .ToList();

            var totals = CalculateTotals(lineData);
            var taxGroups = lineData
                .GroupBy(l => l.VatRate)
                .Select(g => new TaxGroup(g.Key, g.Sum(x => x.Net), g.Sum(x => x.VatAmount)))
                .ToList();

            var nsInvoice = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");
            var cac = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
            var cbc = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            var ext = XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");

            var invoiceElement = new XElement(nsInvoice + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cbc", cbc),
                new XAttribute(XNamespace.Xmlns + "cac", cac),
                new XAttribute(XNamespace.Xmlns + "ext", ext),
                //new XElement(ext + "UBLExtensions",
                //    new XElement(ext + "UBLExtension",
                //        new XElement(ext + "ExtensionContent",
                //            new XElement(XName.Get("Placeholder", "urn:dummy-namespace"))
                //        )
                //    )
                //),
                new XElement(cbc + "CustomizationID", CustomizationId),
                new XElement(cbc + "ProfileID", ProfileId),
                new XElement(cbc + "ID", invoiceId),
                new XElement(cbc + "IssueDate", issueDate),
                dueDate != null ? new XElement(cbc + "DueDate", dueDate) : null,
                new XElement(cbc + "InvoiceTypeCode", "380"),
                new XElement(cbc + "DocumentCurrencyCode", currency),
                new XElement(cbc + "BuyerReference", detail.PublicId),
                BuildSupplierParty(cac, cbc, issuer),
                BuildCustomerParty(cac, cbc, detail, issuer),
                BuildPaymentMeans(cac, cbc, detail, issuer),
                BuildPaymentTerms(cac, cbc, detail),
                BuildTaxTotal(cac, cbc, currency, taxGroups),
                BuildLegalMonetaryTotal(cac, cbc, currency, totals),
                lineData.Select(line => BuildInvoiceLine(cac, cbc, currency, line))
            );

            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), invoiceElement);

            var xml = doc.ToString(SaveOptions.DisableFormatting);
            var fileName = $"{invoiceId}.xml";

            return new InvoiceUblDocument
            {
                Xml = xml,
                FileName = fileName,
                GeneratedAt = DateTime.UtcNow,
                Profile = ProfileId,
                UblVersion = "2.1"
            };
        }

        private static LineData BuildLineData(InvoiceLineBO line, int index)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            var discount = line.DiscountAmount
                ?? (line.DiscountPercent.HasValue
                    ? Math.Round(line.Price * (line.DiscountPercent.Value / 100m), 2, MidpointRounding.AwayFromZero)
                    : 0m);

            var net = Math.Round(line.Price - discount, 2, MidpointRounding.AwayFromZero);
            var vatRate = Math.Max(0m, line.VatPercentage);

            // VatAmount en Gross worden na de per-tarief-verdeling ingevuld (zie Build).
            return new LineData(index, line.Text ?? string.Empty, net, 0m, net, vatRate);
        }

        private static Totals CalculateTotals(IEnumerable<LineData> lines)
        {
            var totalNet = lines.Sum(l => l.Net);
            var totalVat = lines.Sum(l => l.VatAmount);
            var totalGross = lines.Sum(l => l.Gross);
            return new Totals(totalNet, totalVat, totalGross);
        }

        private static XElement BuildSupplierParty(XNamespace cac, XNamespace cbc, IssuerCompanyBO issuer)
        {
            var countryCode = string.IsNullOrWhiteSpace(issuer.CountryCode) ? "BE" : issuer.CountryCode;
            var legalName = !string.IsNullOrWhiteSpace(issuer.LegalName) ? issuer.LegalName : issuer.Name;

            return new XElement(cac + "AccountingSupplierParty",
                new XElement(cac + "Party",
                   new XElement(cbc + "EndpointID",
                        new XAttribute("schemeID", "9956"),
                        issuer.EnterpriseNumber?.Replace("BE", "").Replace(" ", "").Replace(".", "")
                    ),


                    new XElement(cac + "PartyName",
                        new XElement(cbc + "Name", legalName ?? string.Empty)
                    ),

                    BuildPostalAddress(cac, cbc, issuer.AddressLine1, issuer.AddressLine2, issuer.PostalCode, issuer.City, countryCode),

                    BuildTaxScheme(cac, cbc, issuer.VatNumber),

                    BuildLegalEntity(cac, cbc, legalName, issuer.EnterpriseNumber),

                    BuildContact(cac, cbc, issuer.Email, issuer.Phone)
                )
            );
        }


        private static XElement BuildCustomerParty(
            XNamespace cac,
            XNamespace cbc,
            InvoiceDetailBO detail,
            IssuerCompanyBO issuer)
        {
            // ------------------------------------------------------------
            // ✅ 1. Juiste country-code
            // ------------------------------------------------------------
            var countryCode = (!string.IsNullOrWhiteSpace(detail.ClientCountryName) &&
                              detail.ClientCountryName.Length == 2)
                ? detail.ClientCountryName.ToUpperInvariant()
                : (issuer.CountryCode ?? "BE");

            // ------------------------------------------------------------
            // ✅ 2. Legale naam (fallbacks)
            // ------------------------------------------------------------
            var legalName = !string.IsNullOrWhiteSpace(detail.ClientName)
                ? detail.ClientName
                : detail.ClientAddress;

            // ------------------------------------------------------------
            // ✅ 3. EndpointID (PEPPOL EAS verplicht)
            // Rules:
            // - Indien ondernemingsnummer aanwezig → schemeID="9956"
            // - Indien NIET aanwezig → schemeID="9915" + "NA"
            // ------------------------------------------------------------
            string endpointValue;
            string endpointScheme;

            if (!string.IsNullOrWhiteSpace(detail.ClientEnterpriseNumber))
            {
                // Strip alle spaties, BE, punten
                endpointValue = detail.ClientEnterpriseNumber
                    .Replace("BE", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(".", "")
                    .Replace(" ", "");

                endpointScheme = "9956"; // ✅ Belgisch ondernemingsnummer
            }
            else
            {
                // ✅ BIS fallback voor particulieren / klanten zonder ondernemingsnummer
                endpointValue = "NA";
                endpointScheme = "9915"; // "general unspecified identifier"
            }

            // ------------------------------------------------------------
            // ✅ 4. Party bouwen volgens PEPPOL BIS 3.0 volgorde
            // ------------------------------------------------------------
            return new XElement(cac + "AccountingCustomerParty",
                new XElement(cac + "Party",

                    // ✅ PEPPOL verplicht
                    new XElement(cbc + "EndpointID",
                        new XAttribute("schemeID", endpointScheme),
                        endpointValue
                    ),

                    // ✅ PartyName (optioneel maar goed om te houden)
                    !string.IsNullOrWhiteSpace(legalName)
                        ? new XElement(cac + "PartyName",
                            new XElement(cbc + "Name", legalName))
                        : null,

                    // ✅ Adres volgens BIS volgorde (Street, AdditionalStreet, CityName, PostalZone, Country)
                    BuildPostalAddress(
                        cac,
                        cbc,
                        detail.ClientAddress,
                        null,                                 // Geen AdditionalStreetName
                        detail.ClientPostalCode,
                        detail.ClientCity,
                        countryCode
                    ),

                    // ✅ VAT-nummer blok (mag null zijn)
                    BuildTaxScheme(cac, cbc, detail.ClientVatNumber),

                    // ✅ Legal entity (mag present zijn bij particulieren)
                    BuildLegalEntity(cac, cbc, legalName, detail.ClientEnterpriseNumber),

                    // ✅ Contact (telephone vóór email)
                    BuildContact(cac, cbc, detail.ClientEmail, null)
                )
            );
        }



        private static XElement BuildPostalAddress(XNamespace cac, XNamespace cbc, string? line1, string? line2,
            string? postalCode, string? city, string countryCode)
        {
            return new XElement(cac + "PostalAddress",
                !string.IsNullOrWhiteSpace(line1) ? new XElement(cbc + "StreetName", line1) : null,
                !string.IsNullOrWhiteSpace(line2) ? new XElement(cbc + "AdditionalStreetName", line2) : null,

                // ✅ Correcte volgorde voor BIS/UBL:
                !string.IsNullOrWhiteSpace(city) ? new XElement(cbc + "CityName", city) : null,
                !string.IsNullOrWhiteSpace(postalCode) ? new XElement(cbc + "PostalZone", postalCode) : null,

                new XElement(cac + "Country",
                    new XElement(cbc + "IdentificationCode", countryCode.ToUpperInvariant())
                )
            );
        }



        private static XElement? BuildTaxScheme(XNamespace cac, XNamespace cbc, string? vatNumber)
        {
            if (string.IsNullOrWhiteSpace(vatNumber))
                return null;

            return new XElement(cac + "PartyTaxScheme",
                new XElement(cbc + "CompanyID", vatNumber.Trim()),
                new XElement(cac + "TaxScheme",
                    new XElement(cbc + "ID", "VAT")));
        }

        private static XElement? BuildLegalEntity(XNamespace cac, XNamespace cbc, string? name, string? companyId)
        {
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(companyId))
                return null;

            var elements = new List<object>
            {
                !string.IsNullOrWhiteSpace(name) ? new XElement(cbc + "RegistrationName", name) : null,
                !string.IsNullOrWhiteSpace(companyId) ? new XElement(cbc + "CompanyID", companyId) : null
            };

            return new XElement(cac + "PartyLegalEntity", elements);
        }

        private static XElement? BuildContact(XNamespace cac, XNamespace cbc, string? email, string? phone)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                return null;

            return new XElement(cac + "Contact",
                !string.IsNullOrWhiteSpace(phone) ? new XElement(cbc + "Telephone", phone) : null,
                !string.IsNullOrWhiteSpace(email) ? new XElement(cbc + "ElectronicMail", email) : null
            );
        }



        private static XElement BuildPaymentMeans(XNamespace cac, XNamespace cbc, InvoiceDetailBO detail, IssuerCompanyBO issuer)
        {
            var bankAccount = !string.IsNullOrWhiteSpace(detail.BankAccount)
                ? detail.BankAccount
                : (!string.IsNullOrWhiteSpace(issuer.DefaultBankAccountIban) ? issuer.DefaultBankAccountIban : issuer.EpcIban);

            if (string.IsNullOrWhiteSpace(bankAccount))
                return new XElement(cac + "PaymentMeans",
                    new XElement(cbc + "PaymentMeansCode", new XAttribute("name", "BankTransfer"), "31"));

            return new XElement(cac + "PaymentMeans",
                new XElement(cbc + "PaymentMeansCode", new XAttribute("name", "BankTransfer"), "31"),
                new XElement(cac + "PayeeFinancialAccount",
                    new XElement(cbc + "ID", bankAccount.Trim())));
        }

        private static XElement? BuildPaymentTerms(XNamespace cac, XNamespace cbc, InvoiceDetailBO detail)
        {
            var noteText = !string.IsNullOrWhiteSpace(detail.PaymentTermDisplayText)
                ? detail.PaymentTermDisplayText.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(noteText))
            {
                if (!detail.ExpirationDate.HasValue)
                    return null;

                var due = detail.ExpirationDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                noteText = $"Gelieve te betalen vóór {due}.";
            }

            return new XElement(cac + "PaymentTerms",
                new XElement(cbc + "Note", noteText));
        }


        private static XElement BuildTaxTotal(XNamespace cac, XNamespace cbc, string currency, IEnumerable<TaxGroup> groups)
        {
            var totalTax = groups.Sum(g => g.TaxAmount);
            return new XElement(cac + "TaxTotal",
                new XElement(cbc + "TaxAmount",
                    new XAttribute("currencyID", currency),
                    FormatDecimal(totalTax)),
                groups.Select(group => new XElement(cac + "TaxSubtotal",
                    new XElement(cbc + "TaxableAmount",
                        new XAttribute("currencyID", currency),
                        FormatDecimal(group.TaxableAmount)),
                    new XElement(cbc + "TaxAmount",
                        new XAttribute("currencyID", currency),
                        FormatDecimal(group.TaxAmount)),
                    new XElement(cac + "TaxCategory",
                        new XElement(cbc + "ID", ResolveTaxCategoryId(group.Rate)),
                        new XElement(cbc + "Percent", FormatPercent(group.Rate)),
                        new XElement(cac + "TaxScheme",
                            new XElement(cbc + "ID", "VAT"))))));
        }

        private static XElement BuildLegalMonetaryTotal(XNamespace cac, XNamespace cbc, string currency, Totals totals)
        {
            return new XElement(cac + "LegalMonetaryTotal",
                new XElement(cbc + "LineExtensionAmount",
                    new XAttribute("currencyID", currency),
                    FormatDecimal(totals.Net)),
                new XElement(cbc + "TaxExclusiveAmount",
                    new XAttribute("currencyID", currency),
                    FormatDecimal(totals.Net)),
                new XElement(cbc + "TaxInclusiveAmount",
                    new XAttribute("currencyID", currency),
                    FormatDecimal(totals.Gross)),
                new XElement(cbc + "PayableAmount",
                    new XAttribute("currencyID", currency),
                    FormatDecimal(totals.Gross)));
        }

        private static XElement BuildInvoiceLine(XNamespace cac, XNamespace cbc, string currency, LineData line)
        {
            return new XElement(cac + "InvoiceLine",
                new XElement(cbc + "ID", line.Index.ToString(CultureInfo.InvariantCulture)),
                new XElement(cbc + "InvoicedQuantity",
                    new XAttribute("unitCode", "EA"),
                    FormatQuantity(1m)),
                new XElement(cbc + "LineExtensionAmount",
                    new XAttribute("currencyID", currency),
                    FormatDecimal(line.Net)),
                new XElement(cac + "Item",
                    new XElement(cbc + "Name", line.Description),
                    new XElement(cac + "ClassifiedTaxCategory",
                        new XElement(cbc + "ID", ResolveTaxCategoryId(line.VatRate)),
                        new XElement(cbc + "Percent", FormatPercent(line.VatRate)),
                        new XElement(cac + "TaxScheme",
                            new XElement(cbc + "ID", "VAT")))),
                new XElement(cac + "Price",
                    new XElement(cbc + "PriceAmount",
                        new XAttribute("currencyID", currency),
                        FormatDecimal(line.Net))));
        }

        private static string ResolveTaxCategoryId(decimal vatRate)
        {
            return vatRate == 0m ? "Z" : "S";
        }

        private static string FormatDecimal(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string FormatPercent(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static string FormatQuantity(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private sealed record LineData(int Index, string Description, decimal Net, decimal VatAmount, decimal Gross, decimal VatRate);

        private sealed record Totals(decimal Net, decimal Vat, decimal Gross)
        {
            public decimal VatAmount => Vat;
        }

        private sealed record TaxGroup(decimal Rate, decimal TaxableAmount, decimal TaxAmount);
    }
}