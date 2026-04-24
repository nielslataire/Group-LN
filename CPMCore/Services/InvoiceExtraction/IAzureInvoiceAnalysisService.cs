using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.InvoiceExtraction
{
    public interface IAzureInvoiceAnalysisService
    {
        /// <summary>True als Azure geconfigureerd is (endpoint + API-key aanwezig).</summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Analyseert een PDF via Azure Document Intelligence (prebuilt-invoice).
        /// Geeft een leeg resultaat terug als Azure niet geconfigureerd is — gooit nooit een uitzondering.
        /// </summary>
        Task<AzureInvoiceResult> AnalyzeAsync(byte[] pdfBytes, CancellationToken ct = default);
    }

    public class AzureInvoiceResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public string InvoiceId { get; set; }
        public DateOnly? InvoiceDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public string VendorName { get; set; }
        public string VendorTaxId { get; set; }
        public string OrderReference { get; set; }
        public string DocumentType { get; set; }  // "Invoice" of "CreditNote"
        public IReadOnlyList<AzureInvoiceLine> Lines { get; set; } = new List<AzureInvoiceLine>();
    }

    public class AzureInvoiceLine
    {
        public string Description { get; set; }
        public decimal? Amount { get; set; }
    }
}
