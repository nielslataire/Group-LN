using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CPMCore.Models.Invoicing
{
    public class InvoiceSendVM
    {
        [BindRequired]
        public int InvoiceId { get; set; }

        [BindRequired]
        public int IssuerCompanyId { get; set; }

        [DisplayName("Factuurnummer")]
        [BindNever]
        public string? PublicId { get; set; }

        [DisplayName("Factuurdatum")]
        [BindNever]
        public DateOnly InvoiceDate { get; set; }

        [DisplayName("Klant")]
        [BindNever]
        public string ClientName { get; set; } = string.Empty;

        [BindNever]
        public string? ClientVatNumber { get; set; }

        [BindNever]
        public string? ClientEnterpriseNumber { get; set; }

        [BindNever]
        public string? ClientEmail { get; set; }

        [BindNever]
        public bool RequiresDigitalInvoice { get; set; }

        [BindNever]
        public bool DefaultAttachUbl { get; set; }

        [BindNever]
        public bool IsSupplier { get; set; }

        [BindNever]
        public decimal TotalExclVat { get; set; }

        [BindNever]
        public decimal TotalVat { get; set; }

        [BindNever]
        public decimal TotalInclVat { get; set; }

        [DisplayName("Aan")]
        [Required(ErrorMessage = "E-mailadres is verplicht.")]
        public string To { get; set; } = string.Empty;

        [DisplayName("Cc")]
        public string? Cc { get; set; }

        [DisplayName("Onderwerp")]
        [Required(ErrorMessage = "Onderwerp is verplicht.")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [DisplayName("Bericht")]
        [Required(ErrorMessage = "Bericht is verplicht.")]
        public string Body { get; set; } = string.Empty;

        [DisplayName("PDF bijvoegen")]
        public bool AttachPdf { get; set; } = true;

        [DisplayName("UBL bijvoegen")]
        public bool AttachUbl { get; set; } = true;

        [DisplayName("Verstuur via Peppol")]
        public bool SendToPeppol { get; set; }

        [BindNever]
        public bool CanSendViaPeppol { get; set; }

        [BindNever]
        public bool PeppolAccountFound { get; set; }

        [BindNever]
        public string? PeppolParticipantId { get; set; }

        [BindNever]
        public string? PeppolStatusMessage { get; set; }

        [BindNever]
        public bool IssuerPeppolEnabled { get; set; }

        [BindNever]
        public string? IssuerPeppolId { get; set; }

        [BindNever]
        public string IssuerName { get; set; } = string.Empty;

        [BindNever]
        public string? IssuerEmail { get; set; }

        [BindNever]
        public string? BankAccount { get; set; }

        [BindNever]
        public string Currency { get; set; } = "EUR";

        [BindNever]
        public bool ExistingUblSentViaPeppol { get; set; }

        [BindNever]
        public DateTime? ExistingUblGeneratedAt { get; set; }

        [BindNever]
        public string? ExistingUblDocumentId { get; set; }

        [BindNever]
        public IReadOnlyList<InvoiceEmailLogItemVM> EmailLogs { get; set; } = Array.Empty<InvoiceEmailLogItemVM>();

        [BindNever]
        public DateTime? LastEmailSentAt { get; set; }

        [BindNever]
        public string? ExistingUblProfile { get; set; }

        [BindNever]
        public string? ExistingUblVersion { get; set; }

        [BindNever]
        public bool ForcePdfOnly { get; set; }

        [BindNever]
        public bool CanRetryOctopusSend { get; set; }

        public bool IsCopyRequest { get; set; }

        /// <summary>gl-v2 (11g): gekozen kanaal. "email" (standaard), "peppol" of "both". Leeg = het oude scherm: altijd e-mail, Peppol enkel via SendToPeppol.</summary>
        public string? Channel { get; set; }

        /// <summary>Peppol-controle van de klant in de openbare Peppol-directory: "found", "notfound" of "unknown" (geen btw- of ondernemingsnummer).</summary>
        [BindNever]
        public string PeppolCheck { get; set; } = "unknown";

        /// <summary>Doorsturen naar Octopus (dat via Peppol naar de klant verstuurt) kan enkel als Octopus gekoppeld is en de factuur nog niet naar Octopus is verstuurd of dat mislukt is.</summary>
        [BindNever]
        public bool CanSendViaOctopus { get; set; }

        [BindNever]
        public string? OctopusNote { get; set; }

        [BindNever]
        public DateOnly? ExpirationDate { get; set; }

        [BindNever]
        public string? StructuredMessage { get; set; }

        [BindNever]
        public string? CurrentUserEmail { get; set; }

        /// <summary>Verzendgeschiedenis met kanaal (e-mail, Peppol), nieuwste eerst.</summary>
        [BindNever]
        public IReadOnlyList<InvoiceSendHistoryItemVM> History { get; set; } = Array.Empty<InvoiceSendHistoryItemVM>();
    }

    public class InvoiceSendHistoryItemVM
    {
        public DateTime At { get; set; }
        public string Recipient { get; set; } = string.Empty;
        /// <summary>"E-mail" of "Peppol".</summary>
        public string Channel { get; set; } = "E-mail";
        public string Detail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        /// <summary>success · neutral · strong · danger, zie .gl-v2-is-send-status in gl-v2-invoices-send.css.</summary>
        public string Tone { get; set; } = "neutral";
    }

    public class InvoiceEmailLogItemVM
    {
        public DateTime SentAt { get; set; }
        public string To { get; set; } = string.Empty;
        public string? Cc { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}