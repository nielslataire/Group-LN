using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using CPMCore.Services.InvoiceExtraction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.QuoteExtraction
{
    /// <summary>Offerte inlezen (design-handoff 20c) — leest de tabelstructuur uit een client-
    /// bijgesneden regio-afbeelding via Azure Document Intelligence, model "prebuilt-layout" (generieke
    /// rij/kolom-detectie, in tegenstelling tot AzureInvoiceAnalysisService se "prebuilt-invoice" dat
    /// hele-document-analyse met semantische velden doet). Herbruikt dezelfde
    /// AzureDocumentIntelligenceOptions (endpoint/API-key) als die service.</summary>
    public class AzureQuoteAnalysisService : IQuoteRegionAnalysisService
    {
        private const string LayoutModelId = "prebuilt-layout";
        private static readonly string[] UnitTokens = { "m²", "m2", "m³", "m3", "lm", "sog", "st", "stuk", "stuks" };

        private readonly AzureDocumentIntelligenceOptions _azure;
        private readonly ILogger<AzureQuoteAnalysisService> _logger;

        public bool IsEnabled => _azure.IsConfigured;

        public AzureQuoteAnalysisService(IOptions<InvoiceExtractionOptions> options, ILogger<AzureQuoteAnalysisService> logger)
        {
            _azure = options.Value.Azure;
            _logger = logger;
        }

        public async Task<QuoteRegionResult> AnalyzeTableAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return new QuoteRegionResult { ErrorMessage = "Azure Document Intelligence is niet geconfigureerd." };

            try
            {
                var client = new DocumentAnalysisClient(new Uri(_azure.Endpoint), new AzureKeyCredential(_azure.ApiKey));
                using var ms = new MemoryStream(imageBytes);
                var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, LayoutModelId, ms, cancellationToken: ct);

                var table = operation.Value.Tables.OrderByDescending(t => t.Cells.Count).FirstOrDefault();
                if (table is null)
                {
                    _logger.LogInformation("Azure Document Intelligence (prebuilt-layout): geen tabel herkend in de regio.");
                    return new QuoteRegionResult { Success = true };
                }

                var rows = table.Cells
                    .GroupBy(c => c.RowIndex)
                    .OrderBy(g => g.Key)
                    .Select(g => g.OrderBy(c => c.ColumnIndex).Select(c => (c.ColumnIndex, Text: (c.Content ?? "").Trim())).ToList())
                    .ToList();

                // Eerste rij overslaan als het een kop lijkt (geen enkele cel numeriek) en er meerdere rijen zijn.
                if (rows.Count > 1 && rows[0].All(c => !LooksNumeric(c.Text)))
                    rows = rows.Skip(1).ToList();

                var lines = rows.Select(MapRow).Where(l => !string.IsNullOrWhiteSpace(l.Description) || l.Price.HasValue).ToList();
                return new QuoteRegionResult { Success = true, Lines = lines };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure Document Intelligence (prebuilt-layout) analyse mislukt.");
                return new QuoteRegionResult { ErrorMessage = ex.Message };
            }
        }

        /// <summary>Kolomtoewijzing is een heuristiek, geen semantische AI-herkenning (prebuilt-layout
        /// geeft enkel rij/kolom-cellen terug): kolom 0 = omschrijving, een cel die op een gekende eenheid
        /// lijkt = eenheid, de LAATSTE numerieke cel = prijs (typisch de rechtse kolom in een offerte),
        /// de numerieke cel ervoor = aantal. Alles wat niet overtuigend herkend wordt → NeedsReview.</summary>
        private static QuoteRegionLine MapRow(List<(int ColumnIndex, string Text)> cells)
        {
            if (cells.Count == 0) return new QuoteRegionLine { NeedsReview = true };

            var description = cells[0].Text;
            var rest = cells.Skip(1).ToList();

            var unitMatch = rest.Where(c => UnitTokens.Contains(c.Text.ToLowerInvariant())).Select(c => (int?)c.ColumnIndex).FirstOrDefault();
            var numericCells = rest.Where(c => c.ColumnIndex != unitMatch && LooksNumeric(c.Text)).ToList();

            decimal? price = null, number = null;
            if (numericCells.Count > 0)
            {
                price = ParseDecimal(numericCells[^1].Text);
                if (numericCells.Count > 1) number = ParseDecimal(numericCells[^2].Text);
            }

            var unitText = unitMatch.HasValue ? rest.First(c => c.ColumnIndex == unitMatch.Value).Text : null;
            var needsReview = string.IsNullOrWhiteSpace(description) || !price.HasValue;
            return new QuoteRegionLine
            {
                Description = description,
                Unit = unitText,
                Number = number ?? 1m,
                Price = price,
                NeedsReview = needsReview,
            };
        }

        private static bool LooksNumeric(string text)
        {
            var cleaned = text.Replace("€", "").Replace(" ", "").Trim();
            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                || decimal.TryParse(cleaned.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        private static decimal? ParseDecimal(string text)
        {
            var cleaned = text.Replace("€", "").Replace(" ", "").Trim();
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var direct)) return direct;
            if (decimal.TryParse(cleaned.Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var beStyle)) return beStyle;
            return null;
        }
    }
}
