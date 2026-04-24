using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.InvoiceExtraction
{
    public class AzureInvoiceAnalysisService : IAzureInvoiceAnalysisService
    {
        private readonly AzureDocumentIntelligenceOptions _azure;
        private readonly ILogger<AzureInvoiceAnalysisService> _logger;

        public bool IsEnabled => _azure.IsConfigured;

        public AzureInvoiceAnalysisService(
            IOptions<InvoiceExtractionOptions> options,
            ILogger<AzureInvoiceAnalysisService> logger)
        {
            _azure = options.Value.Azure;
            _logger = logger;
        }

        public async Task<AzureInvoiceResult> AnalyzeAsync(byte[] pdfBytes, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return new AzureInvoiceResult { ErrorMessage = "Azure Document Intelligence is niet geconfigureerd." };

            try
            {
                var client = new DocumentAnalysisClient(
                    new Uri(_azure.Endpoint),
                    new AzureKeyCredential(_azure.ApiKey));

                using var ms = new MemoryStream(pdfBytes);
                var operation = await client.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    _azure.ModelId,
                    ms,
                    cancellationToken: ct);

                var doc = operation.Value.Documents.Count > 0
                    ? operation.Value.Documents[0]
                    : null;

                if (doc == null)
                {
                    _logger.LogWarning("Azure Document Intelligence: geen document herkend in de PDF.");
                    return new AzureInvoiceResult { Success = true };
                }

                var result = new AzureInvoiceResult
                {
                    Success = true,
                    InvoiceId     = GetString(doc, "InvoiceId"),
                    VendorName    = GetString(doc, "VendorName"),
                    VendorTaxId   = GetString(doc, "VendorTaxId"),
                    OrderReference = GetString(doc, "PurchaseOrder"),
                    InvoiceDate   = GetDate(doc, "InvoiceDate"),
                    DueDate       = GetDate(doc, "DueDate"),
                    DocumentType  = doc.DocumentType?.IndexOf("credit", StringComparison.OrdinalIgnoreCase) >= 0
                                        ? "CreditNote" : "Invoice"
                };

                // Regelitems (Items-lijst)
                if (doc.Fields.TryGetValue("Items", out var itemsField)
                    && itemsField.FieldType == DocumentFieldType.List)
                {
                    var lines = new List<AzureInvoiceLine>();
                    foreach (var item in itemsField.Value.AsList())
                    {
                        if (item.FieldType != DocumentFieldType.Dictionary) continue;
                        var dict = item.Value.AsDictionary();

                        var desc = GetDictString(dict, "Description");
                        decimal? amount = null;

                        if (dict.TryGetValue("Amount", out var amtField)
                            && amtField.FieldType == DocumentFieldType.Currency)
                            amount = (decimal)amtField.Value.AsCurrency().Amount;

                        if (!string.IsNullOrWhiteSpace(desc) || amount.HasValue)
                            lines.Add(new AzureInvoiceLine { Description = desc, Amount = amount });
                    }
                    result.Lines = lines;
                }

                _logger.LogInformation(
                    "Azure Document Intelligence: factuur '{Id}', vervaldatum={Due}, {LineCount} regelitems.",
                    result.InvoiceId ?? "(geen)", result.DueDate, result.Lines.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Document Intelligence analyse mislukt.");
                return new AzureInvoiceResult { ErrorMessage = ex.Message };
            }
        }

        private static string GetString(AnalyzedDocument doc, string key)
        {
            if (!doc.Fields.TryGetValue(key, out var f)) return null;
            return f.Content?.Trim();
        }

        private static string GetDictString(IReadOnlyDictionary<string, DocumentField> dict, string key)
        {
            if (!dict.TryGetValue(key, out var f)) return null;
            return f.Content?.Trim();
        }

        private static DateOnly? GetDate(AnalyzedDocument doc, string key)
        {
            if (!doc.Fields.TryGetValue(key, out var f)) return null;
            if (f.FieldType != DocumentFieldType.Date) return null;
            try { return DateOnly.FromDateTime(f.Value.AsDate().DateTime); }
            catch { return null; }
        }
    }
}
