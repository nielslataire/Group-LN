using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CPMCore.Models.Instellingen
{
    using BOCore;
    using CPMCore.Models.Invoicing;
    public class IssuerCompanyVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Naam is verplicht.")]
        [Display(Name = "Weergave naam")]
        public string Name { get; set; }

        [Display(Name = "Wettelijke naam")]
        public string? LegalName { get; set; }

        [Display(Name = "BTW-nummer")]
        public string? VatNumber { get; set; }

        [Display(Name = "Ondernemingsnr")]
        public string? EnterpriseNumber { get; set; }

        [Display(Name = "Adres")]
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }

        [Display(Name = "Postcode")]
        public string? PostalCode { get; set; }
        [Display(Name = "Gemeente")]
        public string? City { get; set; }

        [Display(Name = "Landcode")]
        public string? CountryCode { get; set; }

        [EmailAddress(ErrorMessage = "Geen geldig e-mailadres.")]
        public string? Email { get; set; }
        [Display(Name = "Telefoon")]
        public string? Phone { get; set; }
        [Display(Name = "Telefoon 2")]
        public string? Phone2 { get; set; }
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "Vennootschapsvorm")]
        public int? CompanyLegalFormId { get; set; }
        public string? CompanyLegalFormAbbreviation { get; set; }

        [Display(Name = "Logo pad")]
        public string? LogoPath { get; set; }
        [Display(Name = "Logo")]
        public IFormFile? LogoUpload { get; set; }

        [Display(Name = "Sjabloon sleutel")]
        public string? TemplateKey { get; set; }

        [Display(Name = "Sjabloon JSON")]
        public string? TemplateJson { get; set; }

        [Display(Name = "Primaire kleur")]
        public string? BrandPrimaryColor { get; set; }

        [Display(Name = "Secundaire kleur")]
        public string? BrandSecondaryColor { get; set; }

        [Display(Name = "Lettertype")]
        public string? FontFamily { get; set; }

        public byte[]? LogoBytes { get; set; }

        [Display(Name = "Standaard betaaltermijn")]
        public int? DefaultPaymentTermId { get; set; }

        [Display(Name = "Standaard btw-code")]
        public int? DefaultVatTypeId { get; set; }

        [Display(Name = "Actief")]
        public bool IsActive { get; set; } = true;
        [Display(Name = "E-invoicing")]
        public bool EInvoiceEnabled { get; set; }
        [Display(Name = "Peppol ID")]
        public string? PeppolParticipantId { get; set; }
        [Display(Name = "UBL - PDF als bijlage")]
        public bool UblAttachPdf { get; set; } = true;

        [Display(Name = "Email onderwerp template")]
        public string? EmailSubjectTemplate { get; set; }
        [Display(Name = "Email body template")]
        public string? EmailBodyTemplate { get; set; }
        [Display(Name = "Email body template (betaald)")]
        public string? EmailPaidBodyTemplate { get; set; }

        [Display(Name = "Afzender e-mailadres facturen")]
        [EmailAddress]
        public string? InvoiceSendEmail { get; set; }

        [Display(Name = "Factuur voettekst")]
        public string? InvoiceFooterHtml { get; set; }
        [Display(Name = "Standaardtaal")]
        public string? DefaultLanguage { get; set; }   // "nl-BE"
        [Display(Name = "Standaard valuta")]
        public string? DefaultCurrency { get; set; }   // "EUR"
        [Display(Name = "Patroon Factuurnummer")]
        public string? InvoiceNumberPattern { get; set; } // "{num:0000}-{date:MM-yyyy}"

        [Display(Name = "Juridische footer")]
        public string? FooterLegalText { get; set; }

        [Display(Name = "Peppol actief")]
        public bool PeppolEnabled { get; set; }

        [Display(Name = "EPC QR weergeven")]
        public bool EpcQrEnabled { get; set; }
        [Display(Name = "Naam begunstigde")]
        public string? EpcBeneficiaryName { get; set; }
        [Display(Name = "IBAN")]
        public string? EpcIban { get; set; }
        [Display(Name = "BIC")]
        public string? EpcBic { get; set; }
        [Display(Name = "Overschrijving Type")]
        public string? EpcRemittanceType { get; set; }       // 'CHAR'|'SCOR'
        [Display(Name = "Overschrijving Template")]
        public string? EpcRemittanceTemplate { get; set; }   // "Factuur {PublicId}"

        [Display(Name = "Octopus gebruikersnaam")]
        public string? OctopusUsername { get; set; }

        [Display(Name = "Octopus wachtwoord")]
        [DataType(DataType.Password)]
        public string? OctopusPassword { get; set; }

        public string? OctopusAuthenticateToken { get; set; }

        public DateTime? OctopusAuthenticateTokenValidUntil { get; set; }

        public string? OctopusDossierToken { get; set; }

        public DateTime? OctopusDossierTokenValidUntil { get; set; }

        [Display(Name = "Octopus dossiernummer")]
        public string? OctopusDossierNumber { get; set; }

        [Display(Name = "Aankoopdagboek prefix")]
        public string? OctopusPurchaseJournalKeyPrefix { get; set; }

        public string? OctopusCustomFieldsJson { get; set; }

        public string? OctopusCustomFieldMappingsJson { get; set; }

        public int? OctopusDownloadLinkCustomFieldKeyId { get; set; }

        public List<OctopusCustomFieldMappingVM> CustomFieldMappings { get; set; } = new();

        public List<VatTypeVM> VatTypes { get; set; } = new();
        public List<PaymentTermVM> PaymentTerms { get; set; } = new();
        public IReadOnlyList<InvoiceLayoutTemplateOptionVM> InvoiceTemplates { get; set; } = new List<InvoiceLayoutTemplateOptionVM>();

        // Standaard tarieven voor coördinatieprojecten
        [Display(Name = "Km-tarief verplaatsing (€/km)")]
        public decimal? RatePerKm { get; set; }

        public List<IssuerUserRateVM> UserRates { get; set; } = new();

        // Beschikbare gebruikers voor tarief-koppeling (readonly, voor de dropdown)
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AvailableUsers { get; set; } = new();
    }

    public class IssuerUserRateVM
    {
        public string UserId { get; set; } = "";
        public string UserFullName { get; set; } = "";

        [Display(Name = "Uurtarief (€)")]
        [Range(0, 9999.99, ErrorMessage = "Uurtarief moet positief zijn.")]
        public decimal HourlyRate { get; set; }
    }

    public class PaymentTermVM
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Days { get; set; }
        public string? Description { get; set; }
        public PaymentTermType TermType { get; set; } = PaymentTermType.Days;
        public PaymentTermDisplayMode DisplayMode { get; set; } = PaymentTermDisplayMode.DueDate;
        public string? DisplayText { get; set; }
        public bool IsDeleted { get; set; }
    }


    public class OctopusCustomFieldMappingVM
    {
        public int CustomFieldKeyId { get; set; }
        public string? TemplateCode { get; set; }
        public string? DescriptionNl { get; set; }
        public string? DescriptionFr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionDe { get; set; }
        public string? InvoiceField { get; set; }

        public string DisplayName => !string.IsNullOrWhiteSpace(TemplateCode)
            ? $"{TemplateCode} — {DescriptionNl ?? DescriptionEn ?? DescriptionFr ?? DescriptionDe}"
            : DescriptionNl ?? DescriptionEn ?? DescriptionFr ?? DescriptionDe ?? $"Field {CustomFieldKeyId}";
    }
}
