namespace GroupLN.MarketData.Core.Settings;

public class CrawlerSettings
{
    public const string Section = "CrawlerSettings";

    // ── Veiligheidsschakelaar ───────────────────────────────────────────────
    /// <summary>
    /// Globale schakelaar. false = worker start wel maar crawlt nooit.
    /// Zet op true alleen als je bewust wil crawlen.
    /// </summary>
    public bool EnableCrawler { get; set; } = true;

    /// <summary>
    /// Schrijf niets naar de database. Log enkel wat zou worden opgeslagen.
    /// Verplicht true in development totdat je zeker bent van de werking.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// EF migraties automatisch toepassen bij opstart.
    /// true in development (handige automatisatie), false in productie (bewuste controle).
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; } = false;

    // ── Limieten ────────────────────────────────────────────────────────────
    /// <summary>
    /// Stop na N listings per crawl-run. 0 = onbeperkt.
    /// Gebruik bijv. 5 voor eerste test.
    /// </summary>
    public int MaxListingsPerRun { get; set; } = 0;

    /// <summary>
    /// MarkInactive draait alleen als minstens dit aantal listings gevonden werd.
    /// Beschermt tegen het deactiveren van alles bij een gebroken crawl.
    /// </summary>
    public int MinListingsBeforeMarkInactive { get; set; } = 20;

    // ── Geografische filters ────────────────────────────────────────────────
    /// <summary>
    /// Lege lijst = alle postcodes toelaten.
    /// Vul in voor testtoeloop: ["8730", "8000", "8020"].
    /// </summary>
    public List<string> AllowedPostalCodes { get; set; } = new();

    /// <summary>
    /// Lege lijst = alle gemeenten toelaten.
    /// Vul in als alternatief voor postcodes: ["Beernem", "Brugge"].
    /// </summary>
    public List<string> AllowedCities { get; set; } = new();

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
}
