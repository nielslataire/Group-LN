namespace GroupLN.MarketData.Core.Settings;

public class CrawlerSettings
{
    public const string Section = "CrawlerSettings";

    // ── Veiligheidsschakelaar ───────────────────────────────────────────────
    public bool EnableCrawler { get; set; } = true;
    public bool DryRun { get; set; } = false;
    public bool ApplyMigrationsOnStartup { get; set; } = false;

    // ── Limieten ────────────────────────────────────────────────────────────
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

    // ── Per-bron instellingen ────────────────────────────────────────────────
    public Dictionary<string, SourceSettings> Sources { get; set; } = new();

    // ── Debug-instellingen ───────────────────────────────────────────────────
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

    /// <summary>
    /// true = enkel zoekpagina's bezoeken en debug-bestanden schrijven.
    /// Geen detailpagina's openen, geen database-writes.
    /// </summary>
    public bool SearchDebugMode { get; set; } = false;

    /// <summary>
    /// Maximaal aantal zoekpagina's per locatie (bijv. per gemeente).
    /// In SearchDebugMode wordt Debug.MaxPagesInSearchDebugMode gebruikt.
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
    /// Leeg = geen geografisch filter (alle resultaten worden verwerkt).
    /// </summary>
    public List<LocationSettings> AllowedLocations { get; set; } = new();
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
    /// </summary>
    public int MaxPagesInSearchDebugMode { get; set; } = 3;
}
