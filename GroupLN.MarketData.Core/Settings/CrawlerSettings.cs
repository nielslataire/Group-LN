namespace GroupLN.MarketData.Core.Settings;

public class CrawlerSettings
{
    public const string Section = "CrawlerSettings";

    // ── Veiligheidsschakelaar ───────────────────────────────────────────────
    public bool EnableCrawler { get; set; } = true;
    public bool DryRun { get; set; } = false;
    public bool ForceCrawl { get; set; } = false;
    public bool ApplyMigrationsOnStartup { get; set; } = false;

    // ── Limieten ────────────────────────────────────────────────────────────
    /// <summary>Maximaal aantal listings per crawl-run. 0 = onbeperkt.</summary>
    public int MaxListingsPerRun { get; set; } = 0;
    public int MinListingsBeforeMarkInactive { get; set; } = 20;

    // ── HTTP ─────────────────────────────────────────────────────────────────
    public int MaxRequestsPerMinute { get; set; } = 20;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    public int DelayBetweenRequestsSeconds { get; set; } = 4;
    public int HttpTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;

    // ── Playwright ───────────────────────────────────────────────────────────
    public int PlaywrightTimeoutMs { get; set; } = 30000;

    // ── Opruimen ─────────────────────────────────────────────────────────────
    public int MarkInactiveAfterDays { get; set; } = 30;

    // Aantal opeenvolgende crawls dat een listing ontbreekt voor deactivatie
    public int MissingListingThreshold { get; set; } = 3;

    // ── Scheduler ────────────────────────────────────────────────────────────
    /// <summary>Hoe vaak de worker controleert of een source aan de beurt is (in minuten). 0 = fallback 30 min.</summary>
    public int WorkerCheckIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Wachttijd na een mislukte crawl voordat opnieuw geprobeerd wordt (in minuten).
    /// 0 = gebruik CrawlIntervalMinutes van de source als fallback.
    /// </summary>
    public int RetryIntervalMinutes { get; set; } = 30;

    // ── Per-bron instellingen ────────────────────────────────────────────────
    public Dictionary<string, SourceSettings> Sources { get; set; } = new();

    // ── Deduplicatie ─────────────────────────────────────────────────────────
    /// <summary>
    /// true = voer één keer een volledige deduplicatie-scan uit bij opstart (alle bestaande assets).
    /// Zet terug op false na de eerste run.
    /// </summary>
    public bool FullDeduplicationScanOnStartup { get; set; } = false;

    // ── Canonical Projects ────────────────────────────────────────────────────
    /// <summary>
    /// true = herberekent canonical projects na elke deduplicatieronde.
    /// </summary>
    public bool RebuildCanonicalProjectsAfterDedup { get; set; } = true;

    // ── Debug-instellingen ───────────────────────────────────────────────────
    public PhotoHashingSettings PhotoHashing { get; set; } = new();

    public AiExtractionSettings AiExtraction { get; set; } = new();

    public DebugSettings Debug { get; set; } = new();
}

/// <summary>
/// Instellingen specifiek voor één crawler-bron (bijv. "Immoweb").
/// Sleutel in CrawlerSettings.Sources is de naam van de bron (exact gelijk aan SourceName in de crawler).
/// </summary>
public class SourceSettings
{
    /// <summary>false = bron wordt overgeslagen, geen URLs gegenereerd, geen verwerking.</summary>
    public bool Enabled { get; set; } = true;

    // ── Source-specifieke rate-limit overrides (null = gebruik globale waarde) ─
    public int? MaxRequestsPerMinute { get; set; }
    public int? DelayBetweenRequestsSeconds { get; set; }
    public int? HttpTimeoutSeconds { get; set; }
    public int? PlaywrightTimeoutMs { get; set; }

    /// <summary>
    /// true = enkel zoekpagina's bezoeken en debug-bestanden schrijven.
    /// Geen detailpagina's openen, geen database-writes.
    /// </summary>
    public bool SearchDebugMode { get; set; } = false;

    /// <summary>
    /// Maximaal aantal zoekpagina's per locatie (bijv. per gemeente).
    /// In SearchDebugMode wordt Debug.MaxPagesInSearchDebugMode gebruikt.
    /// 0 = onbeperkt (stop wanneer geen volgende pagina meer bestaat).
    /// </summary>
    public int MaxSearchPagesPerLocation { get; set; } = 10;

    /// <summary>
    /// true = altijd MaxSearchPagesPerLocation pagina's ophalen, ook als het geschatte
    /// resultaat al volledig verwerkt is.
    /// false (standaard) = stop na min(EstimatedPages, MaxSearchPagesPerLocation) pagina's.
    /// </summary>
    public bool ForceMaxSearchPages { get; set; } = false;

    /// <summary>
    /// Lijst van zoek-URL-templates. Ondersteunde placeholders:
    /// {city}, {citySlug}, {postalCode}, {page}, {transactionType}, {propertyType}.
    /// Templates met locatie-placeholders worden per AllowedLocations-item uitgebreid.
    /// Templates zonder locatie-placeholders worden globaal uitgevoerd (enkel {page} uitgebreid).
    /// </summary>
    public List<string> SearchUrls { get; set; } = new();

    /// <summary>
    /// Manuele test-URL's. Als niet leeg: zoekpagina-scraping overgeslagen,
    /// enkel deze URL's worden als detailpagina verwerkt.
    /// </summary>
    public List<string> ManualTestListingUrls { get; set; } = new();

    /// <summary>
    /// Locatiefilter voor URL-generatie en detail-filtering.
    /// Leeg = geen geografisch filter; combineer met AllowDefaultLocations.
    /// </summary>
    public List<LocationSettings> AllowedLocations { get; set; } = new();

    /// <summary>
    /// true (standaard) = gebruik hardcoded DefaultLocations als fallback wanneer AllowedLocations leeg is.
    /// false = crawler weigert te starten als AllowedLocations leeg is (aanbevolen voor productie).
    /// Een WARNING wordt altijd gelogd wanneer de fallback actief is.
    /// </summary>
    public bool AllowDefaultLocations { get; set; } = true;

    /// <summary>
    /// false = detailpagina's worden NIET geopend. Enkel zoekkaart-data uit
    /// __NEXT_DATA__ van de zoekpagina wordt gebruikt. Lat/Lon blijven nullable.
    /// true (standaard) = detailpagina's openen voor volledige parsing.
    /// </summary>
    public bool OpenDetailPages { get; set; } = true;

    /// <summary>
    /// true (standaard) = detailpagina openen voor Zimmo nieuwbouwprojecten
    /// (URL bevat /nieuwbouwproject/ of type=PROJECT in search-card).
    /// </summary>
    public bool OpenProjectDetailPages { get; set; } = true;

    /// <summary>
    /// false (standaard) = gewone huizen/appartementen alleen via search-card data.
    /// true = ook voor losse listings detailpagina's openen (risico op Cloudflare-blokkering).
    /// </summary>
    public bool OpenDetailPagesForLooseListings { get; set; } = false;

    /// <summary>
    /// Maximaal aantal projectdetailpagina's per crawl-run. 0 = onbeperkt.
    /// </summary>
    public int MaxProjectDetailPagesPerRun { get; set; } = 100;

    /// <summary>
    /// Minimaal aantal minuten tussen opeenvolgende crawls voor deze bron.
    /// 0 (standaard) = gebruik de globale fallback van 30 minuten.
    /// </summary>
    public int CrawlIntervalMinutes { get; set; } = 0;

    /// <summary>
    /// ForceCrawl per bron. null = gebruik globale ForceCrawl instelling.
    /// true overschrijft NextCrawlAt voor alleen deze bron.
    /// </summary>
    public bool? ForceCrawl { get; set; } = null;
}

/// <summary>
/// Één locatie-entry: stad + slug + postcode.
/// CitySlug is de schrijfwijze die de site verwacht in het URL-pad.
/// Stel altijd expliciet in — vermijd automatische slugify voor Belgische gemeentenamen.
/// </summary>
public class LocationSettings
{
    /// <summary>Weergavenaam, bijv. "Knokke-Heist".</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// URL-slug die de site verwacht, bijv. "knokke-heist".
    /// Als leeg: fallback naar City.ToLower().Replace(' ', '-') met een waarschuwing in de logs.
    /// </summary>
    public string? CitySlug { get; set; }

    /// <summary>Postcode, bijv. "8730".</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Zimmo-specifiek place ID, bijv. 1108 voor Beernem.</summary>
    public int? PlaceId { get; set; }
}

/// <summary>
/// Debug-uitvoer instellingen. Schakel individuele bestanden aan/uit.
/// Debug.Enabled=false overschrijft alles: geen enkel debug-bestand wordt geschreven.
/// </summary>
public class DebugSettings
{
    /// <summary>Hoofdschakelaar. false = geen debug-bestanden, alle onderstaande flags worden genegeerd.</summary>
    public bool Enabled { get; set; } = false;

    public bool SaveHtml { get; set; } = true;
    public bool SaveScreenshots { get; set; } = true;
    public bool SaveNetworkResponses { get; set; } = true;
    public bool SaveAcceptedUrls { get; set; } = true;
    public bool SaveBodyText { get; set; } = true;
    public bool SavePaginationSummary { get; set; } = true;

    /// <summary>Map relatief t.o.v. AppContext.BaseDirectory.</summary>
    public string DebugDirectory { get; set; } = "debug/search";

    /// <summary>
    /// Maximaal aantal zoekpagina's in SearchDebugMode.
    /// Overschrijft MaxSearchPagesPerLocation wanneer SearchDebugMode=true.
    /// 0 = onbeperkt.
    /// </summary>
    public int MaxPagesInSearchDebugMode { get; set; } = 3;
}

public class PhotoHashingSettings
{
    public bool EnableProjectPhotoHashing { get; set; } = false;
    public bool DownloadProjectPhotos { get; set; } = false;
    /// <summary>Maximaal aantal foto's per project voor perceptuele hashing. 0 = onbeperkt.</summary>
    public int MaxProjectPhotosPerProject { get; set; } = 20;
    public int PhotoHashTimeoutSeconds { get; set; } = 20;
    public int RehashPhotosAfterDays { get; set; } = 30;
    public int PhotoHashMaxBytes { get; set; } = 10_485_760; // 10 MB
    public int PhotoPerceptualHashMaxDistance { get; set; } = 8;
}

public class AiExtractionSettings
{
    public bool EnableAiProjectExtraction { get; set; } = false;
    public string? AnthropicApiKey { get; set; }
    public string AnthropicModel { get; set; } = "claude-haiku-4-5-20251001";
    public int AiExtractionMinConfidence { get; set; } = 70;
    /// <summary>Maximaal aantal AI-extracties per run. 0 = onbeperkt.</summary>
    public int MaxAiExtractionsPerRun { get; set; } = 50;
    public int AiExtractionTimeoutSeconds { get; set; } = 30;
}
