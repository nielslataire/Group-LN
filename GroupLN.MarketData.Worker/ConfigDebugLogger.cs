using System.Text.Json;
using GroupLN.MarketData.Core.Settings;
using Microsoft.Extensions.Configuration;

namespace GroupLN.MarketData.Worker;

/// <summary>
/// Logt bij startup een volledige samenvatting van de actieve configuratie.
/// Logt NOOIT gevoelige waarden (ConnectionString, AnthropicApiKey).
/// </summary>
internal static class ConfigDebugLogger
{
    private const string Sep = "────────────────────────────────────────────────────────";

    /// <summary>
    /// Valideert appsettings.Development.json als geldige JSON vóór de host wordt gebouwd.
    /// Geeft een duidelijke foutmelding als het bestand ongeldige JSON of comments bevat.
    /// Gooit een <see cref="InvalidOperationException"/> bij een fatale fout.
    /// </summary>
    internal static void ValidateDevConfigJson()
    {
        var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                   ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                   ?? "Production";

        if (!envName.Equals("Development", StringComparison.OrdinalIgnoreCase))
            return;

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json")
        };

        var devPath = candidates.FirstOrDefault(File.Exists);

        if (devPath is null)
        {
            Console.Error.WriteLine("[Config] WAARSCHUWING: appsettings.Development.json niet gevonden. " +
                                    "Verwacht in: " + string.Join(" of ", candidates));
            return;
        }

        try
        {
            var json = File.ReadAllText(devPath);
            using var _ = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = false });
            // geldig
        }
        catch (JsonException ex)
        {
            var msg = $"appsettings.Development.json bevat ONGELDIG JSON.{Environment.NewLine}" +
                      $"  Fout   : {ex.Message}{Environment.NewLine}" +
                      $"  Pad    : {devPath}{Environment.NewLine}" +
                      $"  Let op : JSON ondersteunt GEEN // of /* */ comments.{Environment.NewLine}" +
                      $"           Verwijder comments of gebruik een geldig JSON-alternatief.";
            Console.Error.WriteLine("[FATAL] " + msg);
            throw new InvalidOperationException(msg, ex);
        }
    }

    /// <summary>
    /// Logt de volledige configuratiesamenvatting na het bouwen van de host.
    /// </summary>
    internal static void LogAll(
        ILogger logger,
        IConfiguration configuration,
        CrawlerSettings settings,
        IHostEnvironment env)
    {
        logger.LogInformation(Sep);
        logger.LogInformation("[Config] CONFIGURATIE DEBUG SAMENVATTING");
        logger.LogInformation(Sep);

        // ── 1. Environment ──────────────────────────────────────────────────────
        var dotnetEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "(niet gezet)";
        var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(niet gezet)";
        logger.LogInformation(
            "[Config] Environment | DOTNET_ENVIRONMENT={DotNet} | ASPNETCORE_ENVIRONMENT={AspNet} | IHostEnvironment={Host}",
            dotnetEnv, aspnetEnv, env.EnvironmentName);

        // ── 2. Configuration providers ──────────────────────────────────────────
        if (configuration is IConfigurationRoot root)
        {
            var providers = root.Providers.ToList();
            logger.LogInformation("[Config] Providers ({Count} in volgorde van prioriteit, laatste wint):", providers.Count);
            for (int i = 0; i < providers.Count; i++)
                logger.LogInformation("[Config]   [{Index}] {Provider}", i + 1, providers[i].ToString());
        }

        // ── 3. Effective CrawlerSettings ────────────────────────────────────────
        logger.LogInformation(Sep);
        logger.LogInformation(
            "[Config] CrawlerSettings | EnableCrawler={EnableCrawler} | DryRun={DryRun} | ForceCrawl={ForceCrawl} | " +
            "MaxListingsPerRun={MaxListings} | Debug.Enabled={Debug} | " +
            "PhotoHashing.EnableProjectPhotoHashing={PhotoHash} | AiExtraction.EnableAiProjectExtraction={AiExtract}",
            settings.EnableCrawler,
            settings.DryRun,
            settings.ForceCrawl,
            settings.MaxListingsPerRun == 0 ? "onbeperkt" : settings.MaxListingsPerRun.ToString(),
            settings.Debug.Enabled,
            settings.PhotoHashing.EnableProjectPhotoHashing,
            settings.AiExtraction.EnableAiProjectExtraction);

        var checkMin  = settings.WorkerCheckIntervalMinutes > 0 ? settings.WorkerCheckIntervalMinutes : 30;
        var retryMin  = settings.RetryIntervalMinutes > 0 ? settings.RetryIntervalMinutes : 30;
        logger.LogInformation(
            "[Config] Scheduler | WorkerCheckInterval={CheckInterval}m | RetryInterval={RetryInterval}m",
            checkMin, retryMin);

        // ── 5. ForceCrawl inherited warning ─────────────────────────────────────
        if (settings.ForceCrawl && configuration is IConfigurationRoot forceCfg)
        {
            const string forceCrawlKey = "CrawlerSettings:ForceCrawl";
            foreach (var provider in forceCfg.Providers.Reverse())
            {
                if (!provider.TryGet(forceCrawlKey, out _)) continue;

                var providerName = provider.ToString() ?? "";
                var fromBase = providerName.Contains("appsettings.json", StringComparison.OrdinalIgnoreCase)
                            && !providerName.Contains("Development", StringComparison.OrdinalIgnoreCase)
                            && !providerName.Contains("secrets", StringComparison.OrdinalIgnoreCase);

                if (fromBase)
                    logger.LogWarning(
                        "[Config] ⚠️  ForceCrawl=true inherited from base appsettings — " +
                        "niet overschreven door appsettings.Development.json, user secrets of omgevingsvariabelen. " +
                        "Provider: {Provider}", providerName);
                break;
            }
        }

        // ── 4. Per source ───────────────────────────────────────────────────────
        logger.LogInformation(Sep);
        logger.LogInformation("[Config] Sources ({Count}):", settings.Sources.Count);

        foreach (var (name, src) in settings.Sources)
        {
            var locationSummary = src.AllowedLocations.Count == 0
                ? "(leeg)"
                : string.Join(", ", src.AllowedLocations.Select(l =>
                {
                    var id = l.PlaceId.HasValue ? $"/{l.PlaceId}" : "";
                    return string.IsNullOrEmpty(l.PostalCode)
                        ? $"{l.City}{id}"
                        : $"{l.PostalCode}{id}";
                }));

            var crawlIntervalLabel = src.CrawlIntervalMinutes > 0 ? $"{src.CrawlIntervalMinutes}m" : "(globale fallback)";
            var forceCrawlLabel    = src.ForceCrawl.HasValue ? src.ForceCrawl.Value.ToString().ToLower() : "(globaal)";

            logger.LogInformation(
                "[Config] Source={Source} | Enabled={Enabled} | ForceCrawl={ForceCrawl} | CrawlInterval={CrawlInterval} | " +
                "SearchDebugMode={SearchDebug} | " +
                "MaxSearchPagesPerLocation={MaxSearchPages} | MaxProjectDetailPagesPerRun={MaxDetail} | " +
                "AllowedLocations={LocCount} [{Locations}] | SearchUrls={UrlCount} | " +
                "MaxRequestsPerMinute={RPM} | DelayBetweenRequestsSeconds={Delay}",
                name,
                src.Enabled,
                forceCrawlLabel,
                crawlIntervalLabel,
                src.SearchDebugMode,
                src.MaxSearchPagesPerLocation.ToLimitLabel(),
                src.MaxProjectDetailPagesPerRun.ToLimitLabel(),
                src.AllowedLocations.Count,
                locationSummary,
                src.SearchUrls.Count,
                src.MaxRequestsPerMinute.HasValue ? src.MaxRequestsPerMinute.Value.ToString() : "(globaal)",
                src.DelayBetweenRequestsSeconds.HasValue ? src.DelayBetweenRequestsSeconds.Value.ToString() : "(globaal)");

            if (!src.Enabled)
                logger.LogWarning("[Config]   ⚠️  Source={Source} is uitgeschakeld (Enabled=false).", name);

            if (src.SearchDebugMode)
                logger.LogWarning("[Config]   ⚠️  Source={Source} staat in SearchDebugMode — geen detailpagina's, geen DB-writes.", name);
        }

        logger.LogInformation(Sep);
        logger.LogInformation("[Config] EINDE CONFIGURATIE DEBUG SAMENVATTING");
        logger.LogInformation(Sep);
    }
}
