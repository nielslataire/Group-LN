using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CPMCore.Services.Octopus
{
    public class OctopusInvoiceCreateRequest
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKeyRef? BookyearKey { get; set; }

        [JsonPropertyName("journalKey")]
        public string? JournalKey { get; set; }

        [JsonPropertyName("documentSequenceNr")]
        public int DocumentSequenceNr { get; set; }

        [JsonPropertyName("bookyearPeriodeNr")]
        public int BookyearPeriodeNr { get; set; }

        [JsonPropertyName("documentDate")]
        public DateOnly DocumentDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateOnly ExpiryDate { get; set; }

        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; } = "EUR";

        [JsonPropertyName("exchangeRate")]
        public decimal ExchangeRate { get; set; }

        [JsonPropertyName("relationIdentificationServiceData")]
        public OctopusRelationIdentificationServiceData? RelationIdentificationServiceData { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("orderReference")]
        public string? OrderReference { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("financialDiscount")]
        public decimal FinancialDiscount { get; set; }

        [JsonPropertyName("customFieldValueList")]
        public List<OctopusCustomFieldValue> CustomFieldValueList { get; set; } = new();

        [JsonPropertyName("invoiceLines")]
        public List<OctopusInvoiceLineRequest> InvoiceLines { get; set; } = new();
    }

    public class OctopusBookyearKeyRef
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class OctopusRelationIdentificationServiceData
    {
        [JsonPropertyName("relationKey")]
        public OctopusRelationKeyRef? RelationKey { get; set; }

        [JsonPropertyName("externalRelationId")]
        public int ExternalRelationId { get; set; }
    }

    public class OctopusRelationKeyRef
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class OctopusCustomFieldValue
    {
        [JsonPropertyName("customFieldKey")]
        public OctopusCustomFieldKeyRef? CustomFieldKey { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class OctopusCustomFieldKeyRef
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class OctopusInvoiceLineRequest
    {
        [JsonPropertyName("externProductNr")]
        public string? ExternProductNr { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("count")]
        public decimal Count { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discountPercentage")]
        public decimal DiscountPercentage { get; set; }

        [JsonPropertyName("vatCodeKey")]
        public string? VatCodeKey { get; set; }

        [JsonPropertyName("bookingAccountNr")]
        public int BookingAccountNr { get; set; }

        [JsonPropertyName("costCentreKey")]
        public OctopusCostCentreKeyRef? CostCentreKey { get; set; }

        [JsonPropertyName("customFieldValueList")]
        public List<OctopusCustomFieldValue> CustomFieldValueList { get; set; } = new();

        [JsonPropertyName("intrastatServiceData")]
        public OctopusIntrastatServiceData? IntrastatServiceData { get; set; }
    }
    public class OctopusInvoiceAttachmentUploadRequest
    {
        public int BookyearId { get; set; }

        public string? JournalKey { get; set; }

        public int DocumentSequenceNumber { get; set; }

        // InvoiceNumber en AttachmentType bestaan niet in de Octopus API — worden niet verstuurd.
        // Behouden voor lokaal gebruik (bv. logging, PDF-opmaak).
        public string InvoiceNumber { get; set; } = string.Empty;

        public string AttachmentType { get; set; } = "Invoice";

        public string FileName { get; set; } = string.Empty;

        // ContentType wordt genegeerd: de API verwacht altijd application/octet-stream.
        public string ContentType { get; set; } = "application/pdf";

        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
    public class OctopusCostCentreKeyRef
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class OctopusIntrastatServiceData
    {
        [JsonPropertyName("isoCountrycode")]
        public string? IsoCountrycode { get; set; }

        [JsonPropertyName("transactionCode")]
        public int TransactionCode { get; set; }

        [JsonPropertyName("productCode")]
        public string? ProductCode { get; set; }

        [JsonPropertyName("region")]
        public int Region { get; set; }

        [JsonPropertyName("weight")]
        public decimal Weight { get; set; }

        [JsonPropertyName("unitCount")]
        public decimal UnitCount { get; set; }

        [JsonPropertyName("transportCode")]
        public int TransportCode { get; set; }

        [JsonPropertyName("incoTerms")]
        public string? IncoTerms { get; set; }

        [JsonPropertyName("originCountry")]
        public string? OriginCountry { get; set; }
    }
    public class OctopusInvoiceSendRequest
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKeyRef? BookyearKey { get; set; }

        [JsonPropertyName("journal")]
        public string? Journal { get; set; }

        [JsonPropertyName("documentSequenceNr")]
        public int DocumentSequenceNr { get; set; }

        [JsonPropertyName("toDocumentSequenceNr")]
        public int ToDocumentSequenceNr { get; set; }

        [JsonPropertyName("fromMailAddress")]
        public string? FromMailAddress { get; set; }

        [JsonPropertyName("ccMailAddress")]
        public string? CcMailAddress { get; set; }

        [JsonPropertyName("bccMailAddress")]
        public string? BccMailAddress { get; set; }

        [JsonPropertyName("excludeOctopusPdf")]
        public bool ExcludeOctopusPdf { get; set; } = true;

        [JsonPropertyName("forceUseEmail")]
        public bool ForceUseEmail { get; set; }
    }

    public class OctopusInvoiceSendResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("sendInvoiceStatusList")]
        public List<OctopusSendInvoiceStatusServiceData> SendInvoiceStatusList { get; set; } = new();
    }

    public class OctopusInvoiceBookResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("bookingStatusList")]
        public List<OctopusBookingStatusServiceData> BookingStatusList { get; set; } = new();
    }

    public class OctopusBookingStatusServiceData
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("documentKey")]
        public OctopusDocumentKeyServiceData? DocumentKey { get; set; }
    }

    //public class OctopusDocumentKeyServiceData
    //{
    //    [JsonPropertyName("bookyearKey")]
    //    public OctopusBookyearKey? BookyearKey { get; set; }

    //    [JsonPropertyName("journal")]
    //    public string? Journal { get; set; }

    //    [JsonPropertyName("documentSequenceNr")]
    //    public int DocumentSequenceNr { get; set; }
    //}

    public class OctopusBuySellBookingAndAttachmentRequest
    {
        [JsonPropertyName("buySellBookingServiceData")]
        public OctopusBuySellBookingServiceData? BuySellBookingServiceData { get; set; }

        [JsonPropertyName("attachments")]
        public List<OctopusAttachmentItem> Attachments { get; set; } = new();
    }

    public class OctopusBuySellBookingServiceData
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKeyRef? BookyearKey { get; set; }

        [JsonPropertyName("journalKey")]
        public string? JournalKey { get; set; }

        [JsonPropertyName("documentSequenceNr")]
        public int DocumentSequenceNr { get; set; }

        [JsonPropertyName("relationIdentificationServiceData")]
        public OctopusRelationIdentificationServiceData? RelationIdentificationServiceData { get; set; }

        [JsonPropertyName("bookyearPeriodeNr")]
        public int BookyearPeriodeNr { get; set; }

        [JsonPropertyName("documentDate")]
        public DateOnly DocumentDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateOnly ExpiryDate { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("orderReference")]
        public string? OrderReference { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchangeRate")]
        public decimal ExchangeRate { get; set; }

        [JsonPropertyName("bookingLines")]
        public List<OctopusBuySellBookingLineServiceData> BookingLines { get; set; } = new();

        [JsonPropertyName("paymentMethod")]
        public int PaymentMethod { get; set; }
    }

    public class OctopusBuySellBookingLineServiceData
    {
        [JsonPropertyName("accountKey")]
        public int AccountKey { get; set; }

        [JsonPropertyName("baseAmount")]
        public decimal BaseAmount { get; set; }

        [JsonPropertyName("vatCodeKey")]
        public string? VatCodeKey { get; set; }

        [JsonPropertyName("vatAmount")]
        public decimal VatAmount { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("costCentreKey")]
        public OctopusCostCentreKeyRef? CostCentreKey { get; set; }

        [JsonPropertyName("vatRecupPercentage")]
        public decimal VatRecupPercentage { get; set; }

        [JsonPropertyName("intrastatServiceData")]
        public OctopusIntrastatServiceData? IntrastatServiceData { get; set; }

        [JsonPropertyName("overflowBookingServiceData")]
        public OctopusOverflowBookingServiceData? OverflowBookingServiceData { get; set; }
    }

    public class OctopusOverflowBookingServiceData
    {
        [JsonPropertyName("beginDate")]
        public DateOnly BeginDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly EndDate { get; set; }

        [JsonPropertyName("overflowBookingPeriodType")]
        public int OverflowBookingPeriodType { get; set; }

        [JsonPropertyName("accountNr")]
        public int AccountNr { get; set; }
    }

    public class OctopusAttachmentItem
    {
        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("fileData")]
        public string? FileData { get; set; }
    }

    public class OctopusSendInvoiceStatusServiceData
    {
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("documentKey")]
        public OctopusDocumentKeyServiceData? DocumentKey { get; set; }

        [JsonPropertyName("sendMethod")]
        public string? SendMethod { get; set; }
    }

    public class OctopusDocumentKeyServiceData
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKeyRef? BookyearKey { get; set; }

        [JsonPropertyName("journal")]
        public string? Journal { get; set; }

        [JsonPropertyName("documentSequenceNr")]
        public int DocumentSequenceNr { get; set; }
    }

    public class OctopusDocumentSelectionData
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKeyRef? BookyearKey { get; set; }

        [JsonPropertyName("journal")]
        public string? Journal { get; set; }

        [JsonPropertyName("documentSequenceNr")]
        public int DocumentSequenceNr { get; set; }

        [JsonPropertyName("toDocumentSequenceNr")]
        public int ToDocumentSequenceNr { get; set; }
    }

    public class OctopusDocumentDeliveryState
    {
        [JsonPropertyName("documentKey")]
        public OctopusDocumentKeyServiceData? DocumentKey { get; set; }

        [JsonPropertyName("deliveryState")]
        public string? DeliveryState { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("deliveryDateTime")]
        public DateTime? DeliveryDateTime { get; set; }
    }
}