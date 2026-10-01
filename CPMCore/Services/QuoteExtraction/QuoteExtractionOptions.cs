namespace CPMCore.Services.QuoteExtraction
{
    /// <summary>Configuratie voor "Offerte inlezen" (20c), sectie "QuoteExtraction". De Anthropic-sleutel is
    /// dezelfde als die van de MarketData-crawler (CrawlerSettings:AiExtraction:AnthropicApiKey) — die
    /// draait als aparte host met eigen config, dus CPMCore heeft hier zijn eigen ingang nodig. Lokaal via
    /// user-secrets zetten, nooit in appsettings.*.json (staat onder git).</summary>
    public class QuoteExtractionOptions
    {
        public const string Section = "QuoteExtraction";

        public string AnthropicApiKey { get; set; }

        /// <summary>Vision-capabel model: de bijgesneden regio gaat als afbeelding mee. Zelfde standaard als
        /// de crawler; voor rommelige/gefotografeerde offertes kan een sterker model (Sonnet) beter lezen.</summary>
        public string AnthropicModel { get; set; } = "claude-haiku-4-5-20251001";

        public int TimeoutSeconds { get; set; } = 60;

        public bool AnthropicConfigured => !string.IsNullOrWhiteSpace(AnthropicApiKey);
    }
}
