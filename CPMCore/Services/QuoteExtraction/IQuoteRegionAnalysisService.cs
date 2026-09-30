using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.QuoteExtraction
{
    public interface IQuoteRegionAnalysisService
    {
        /// <summary>True als Azure geconfigureerd is (endpoint + API-key aanwezig) — herbruikt dezelfde
        /// AzureDocumentIntelligenceOptions als IAzureInvoiceAnalysisService.</summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Leest de tabelstructuur uit een client-bijgesneden regio-afbeelding (design-handoff 20c,
        /// "kader rond een tabel"). Gebruikt model "prebuilt-layout" — dat herkent enkel rij/kolom-
        /// cellen, GEEN semantische velden (geen "dit is de prijskolom") — de kolomtoewijzing hieronder
        /// is dus een heuristiek, geen AI-garantie. Elke rij waar aantal/prijs niet overtuigend als getal
        /// herkend werden komt terug met NeedsReview=true (spec §8, de goud "controleer"-vlag).
        /// Geeft een leeg resultaat terug als Azure niet geconfigureerd is of niets herkend werd — gooit
        /// nooit een uitzondering.
        /// </summary>
        Task<QuoteRegionResult> AnalyzeTableAsync(byte[] imageBytes, CancellationToken ct = default);
    }

    public class QuoteRegionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public IReadOnlyList<QuoteRegionLine> Lines { get; set; } = new List<QuoteRegionLine>();
    }

    public class QuoteRegionLine
    {
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public decimal? Number { get; set; }
        public decimal? Price { get; set; }
        public bool NeedsReview { get; set; }
    }
}
