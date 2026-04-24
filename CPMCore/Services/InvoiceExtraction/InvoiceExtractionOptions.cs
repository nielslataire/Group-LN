namespace CPMCore.Services.InvoiceExtraction
{
    public class InvoiceExtractionOptions
    {
        public AzureDocumentIntelligenceOptions Azure { get; set; } = new();

        /// <summary>
        /// Aantal woorden per pagina waaronder een PDF als scan/foto wordt beschouwd.
        /// Standaard 10 — digitale PDFs hebben doorgaans tientallen woorden per pagina.
        /// </summary>
        public int ScanDetectionWordThreshold { get; set; } = 10;
    }

    public class AzureDocumentIntelligenceOptions
    {
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }

        /// <summary>Model-ID in Azure Document Intelligence. Standaard "prebuilt-invoice".</summary>
        public string ModelId { get; set; } = "prebuilt-invoice";

        /// <summary>True als zowel Endpoint als ApiKey geconfigureerd zijn.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Endpoint) &&
            !string.IsNullOrWhiteSpace(ApiKey);
    }
}
