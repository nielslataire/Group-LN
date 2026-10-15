namespace CPMCore.Services.PuntenSpraak
{
    /// <summary>Configuratie voor "Inspreken" en "Rondgang" bij Punten (design-handoff 41c/41d), sectie "PuntenSpraak".
    /// Spraak naar tekst gaat via OpenAI, het herkennen van punten via Anthropic. De Anthropic-sleutel valt terug op die van
    /// "Offerte inlezen" (QuoteExtraction:AnthropicApiKey) zodat er niet twee sleutels nodig zijn. Sleutels lokaal via user-secrets,
    /// nooit in appsettings.*.json (staat onder git).</summary>
    public class PuntenSpraakOptions
    {
        public const string Section = "PuntenSpraak";

        public string OpenAiApiKey { get; set; }
        /// <summary>Korte berichten: snel en goedkoop.</summary>
        public string TranscribeModel { get; set; } = "gpt-4o-mini-transcribe";
        /// <summary>Rondgang: enkel dit model geeft tijdstempels per zin terug, nodig om foto's aan punten te koppelen.</summary>
        public string RondgangTranscribeModel { get; set; } = "whisper-1";

        public string AnthropicApiKey { get; set; }
        public string AnthropicModel { get; set; } = "claude-sonnet-5-5";

        public int TimeoutSeconds { get; set; } = 120;
        /// <summary>OpenAI aanvaardt bestanden tot 25 MB.</summary>
        public int MaxAudioMb { get; set; } = 24;

        public bool Configured => !string.IsNullOrWhiteSpace(OpenAiApiKey) && !string.IsNullOrWhiteSpace(AnthropicApiKey);
    }
}
