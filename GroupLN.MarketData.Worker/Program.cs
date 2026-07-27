using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Commands;
using GroupLN.MarketData.Infrastructure.Extensions;
using GroupLN.MarketData.Persistence;
using GroupLN.MarketData.Persistence.Extensions;
using GroupLN.MarketData.Worker;
using GroupLN.MarketData.Worker.Workers;
using Microsoft.EntityFrameworkCore;

// ── Stap 0: valideer appsettings.Development.json vóór de host gebouwd wordt ─
// Geeft een duidelijke foutmelding als het bestand ongeldige JSON of comments bevat.
ConfigDebugLogger.ValidateDevConfigJson();

// Helperfunctie: gebruik een scoped service en voer een actie uit, daarna exit
static async Task RunScopedCommandAsync<T>(IHost host, Func<T, Task> action) where T : notnull
{
    using var scope = host.Services.CreateScope();
    var cmd = scope.ServiceProvider.GetRequiredService<T>();
    await action(cmd);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMarketDataPersistence(builder.Configuration);
builder.Services.AddMarketDataInfrastructure(builder.Configuration);
builder.Services.AddSingleton<CrawlerWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CrawlerWorker>());

var host = builder.Build();


var logger = host.Services.GetRequiredService<ILogger<Program>>();

// ── Database migraties ────────────────────────────────────────────────────
using (var scope = host.Services.CreateScope())
{
    var settings = scope.ServiceProvider.GetRequiredService<CrawlerSettings>();

    if (settings.ApplyMigrationsOnStartup)
    {
        logger.LogInformation("ApplyMigrationsOnStartup = true. Database migraties worden toegepast...");
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<MarketDataDbContext>();
            var pending = await db.Database.GetPendingMigrationsAsync();
            var pendingList = pending.ToList();

            if (pendingList.Count > 0)
            {
                logger.LogInformation("{Count} openstaande migratie(s): {Migrations}",
                    pendingList.Count, string.Join(", ", pendingList));
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migraties succesvol toegepast.");
            }
            else
            {
                logger.LogInformation("Database is up-to-date. Geen migraties nodig.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fout bij toepassen van database migraties. Worker wordt toch gestart.");
        }
    }
    else
    {
        logger.LogInformation(
            "ApplyMigrationsOnStartup = false. Migraties worden NIET automatisch toegepast. " +
            "Voer manueel uit: dotnet ef database update --project GroupLN.MarketData.Persistence --startup-project GroupLN.MarketData.Worker");
    }
}

// ── Startup samenvatting ──────────────────────────────────────────────────
using (var scope = host.Services.CreateScope())
{
    var settings = scope.ServiceProvider.GetRequiredService<CrawlerSettings>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    logger.LogInformation(
        "GroupLN MarketData Worker gestart. " +
        "EnableCrawler={Enabled} | DryRun={DryRun} | MaxListings={Max} | Env={Env}",
        settings.EnableCrawler,
        settings.DryRun,
        settings.MaxListingsPerRun == 0 ? "onbeperkt" : settings.MaxListingsPerRun.ToString(),
        env.EnvironmentName);

    if (!settings.EnableCrawler)
        logger.LogWarning("⚠️  EnableCrawler = false. Crawling is uitgeschakeld. Wijzig in appsettings om te activeren.");

    if (settings.DryRun)
        logger.LogWarning("⚠️  DryRun = true. Er wordt NIETS naar de database geschreven.");

    if (settings.MaxListingsPerRun > 0)
        logger.LogWarning("⚠️  MaxListingsPerRun = {Max}. Testmodus actief.", settings.MaxListingsPerRun);

    // ── AI extractie — configuratie-check (key nooit tonen) ──────────────────
    var ai = settings.AiExtraction;
    var apiKeyConfigured = !string.IsNullOrEmpty(ai.AnthropicApiKey);
    logger.LogInformation(
        "AiExtraction | Enabled={Enabled} | Model={Model} | ApiKeyConfigured={KeyOk} | " +
        "MinConfidence={MinConf}% | MaxPerRun={Max}",
        ai.EnableAiProjectExtraction,
        ai.AnthropicModel,
        apiKeyConfigured,
        ai.AiExtractionMinConfidence,
        ai.MaxAiExtractionsPerRun.ToLimitLabel());

    if (ai.EnableAiProjectExtraction && !apiKeyConfigured)
        logger.LogWarning(
            "⚠️  AiExtraction.EnableAiProjectExtraction=true maar AnthropicApiKey is NIET geconfigureerd. " +
            "Stel in via user secrets: dotnet user-secrets set " +
            "\"CrawlerSettings:AiExtraction:AnthropicApiKey\" \"<key>\"");

    // ── Config debug samenvatting ─────────────────────────────────────────────
    var configuration = host.Services.GetRequiredService<IConfiguration>();
    ConfigDebugLogger.LogAll(logger, configuration, settings, env);
}

// ── Tijdelijke Zimmo detail-test ─────────────────────────────────────────
if (args.Contains("--zimmo-detail-test"))
{
    logger.LogInformation("[Program] --zimmo-detail-test modus — normale worker wordt NIET gestart.");
    using (var scope = host.Services.CreateScope())
    {
        var test = scope.ServiceProvider.GetRequiredService<ZimmoDetailDiscoveryTest>();
        await test.RunAsync();
    }
    logger.LogInformation("[Program] Zimmo detail-test voltooid. Afsluiten.");
    return;
}

// ── Database reset: verwijder alle crawler/immo-data ─────────────────────
if (args.Contains("--reset-market-data"))
{
    var confirm            = args.Contains("--confirm");
    var includeGeoCache    = args.Contains("--include-geocoding-cache");

    logger.LogInformation(
        "[Program] --reset-market-data modus — normale worker wordt NIET gestart. " +
        "Confirm={Confirm} | IncludeGeocodingCache={Geo}",
        confirm, includeGeoCache);

    await RunScopedCommandAsync<ResetMarketDataCommand>(host, async cmd =>
    {
        var result = await cmd.ExecuteAsync(
            confirm: confirm,
            includeGeocodingCache: includeGeoCache);

        if (result.Confirmed)
            logger.LogInformation("[Program] --reset-market-data voltooid. Database is leeg.");
        else
            logger.LogWarning(
                "[Program] --reset-market-data preview klaar. " +
                "Herhaal met --confirm om effectief te wissen.");
    });

    return;
}

// ── Canonical units herbouwen ─────────────────────────────────────────────
if (args.Contains("--rebuild-canonical-units"))
{
    var projectIdArg = args.SkipWhile(a => a != "--canonical-project-id").Skip(1).FirstOrDefault();
    long? canonicalProjectId = null;
    if (projectIdArg is not null && long.TryParse(projectIdArg, out var parsedId))
        canonicalProjectId = parsedId;

    logger.LogInformation(
        "[Program] --rebuild-canonical-units modus — normale worker wordt NIET gestart. " +
        "CanonicalProjectId={Id}",
        canonicalProjectId?.ToString() ?? "alle");

    await RunScopedCommandAsync<RebuildCanonicalUnitsCommand>(host, async cmd =>
    {
        var result = await cmd.ExecuteAsync(canonicalProjectId);
        logger.LogInformation(
            "[Program] --rebuild-canonical-units klaar. " +
            "Projects={P} | SourceUnits={S} | CanonicalUnits={C} | Merged={M} | Conflicts={Conf}",
            result.ProjectsProcessed,
            result.SourceUnitsScanned,
            result.CanonicalUnitsCreated,
            result.UnitsMerged,
            result.ConflictsDetected);
    });

    return;
}

// ── Losse listings matchen aan canonical units ────────────────────────────
if (args.Contains("--match-loose-listings-to-units"))
{
    logger.LogInformation("[Program] --match-loose-listings-to-units modus — normale worker wordt NIET gestart.");

    await RunScopedCommandAsync<MatchLooseListingsCommand>(host, async cmd =>
    {
        var result = await cmd.ExecuteAsync();
        logger.LogInformation(
            "[Program] --match-loose-listings-to-units klaar. " +
            "Candidates={C} | Matched={M} | Ambiguous={A} | Unmatched={U}",
            result.CandidatesEvaluated,
            result.Matched,
            result.Ambiguous,
            result.Unmatched);
    });

    return;
}

// ── Volledige deduplicatie uitvoeren ──────────────────────────────────────
if (args.Contains("--run-deduplication"))
{
    // Optioneel: --since yyyy-MM-dd  (default = alles)
    var sinceArg = args.SkipWhile(a => a != "--since").Skip(1).FirstOrDefault();
    DateTime? since = null;
    if (sinceArg is not null)
    {
        if (DateTime.TryParse(sinceArg, out var parsedSince))
            since = parsedSince;
        else
            logger.LogWarning("[Program] Ongeldig --since formaat '{V}' — volledige scan wordt uitgevoerd.", sinceArg);
    }

    logger.LogInformation(
        "[Program] --run-deduplication modus — normale worker wordt NIET gestart. Since={Since}",
        since?.ToString("yyyy-MM-dd HH:mm") ?? "alle records");

    await RunScopedCommandAsync<RunDeduplicationCommand>(host, async cmd =>
    {
        var result = await cmd.ExecuteAsync(since);
        logger.LogInformation(
            "[Program] --run-deduplication klaar. " +
            "Gescand={Scanned} | ProjectCandidates={Projects} | UnitCandidates={Units} | Opgeslagen={Saved}",
            result.NewAssetsScanned,
            result.ProjectCandidatesFound,
            result.UnitCandidatesFound,
            result.CandidatesSaved);
    });

    return;
}

// ── AI Anthropic projectnaam-extractie uitvoeren ──────────────────────────
if (args.Contains("--run-ai-extraction"))
{
    // Optioneel: --source <naam>     (bijv. "Immoweb")
    // Optioneel: --force             (herextraheer ook gecachete assets)
    var sourceArg  = args.SkipWhile(a => a != "--source").Skip(1).FirstOrDefault();
    var forceArg   = args.Contains("--force");

    logger.LogInformation(
        "[Program] --run-ai-extraction modus — normale worker wordt NIET gestart. " +
        "Source={Source} | Force={Force}",
        sourceArg ?? "alle", forceArg);

    await RunScopedCommandAsync<RunAiExtractionCommand>(host, async cmd =>
    {
        var result = await cmd.ExecuteAsync(
            sourceName: sourceArg,
            skipCached: !forceArg);

        logger.LogInformation(
            "[Program] --run-ai-extraction klaar. " +
            "Candidates={C} | FromCache={Cache} | Nieuw={New} | " +
            "GeenProjectnaam={NoPrj} | Overgeslagen={Skip} | Fouten={Err}",
            result.Candidates,
            result.FromCache,
            result.NewExtractions,
            result.NoProjectName,
            result.Skipped,
            result.Errors);
    });

    return;
}

// ── Directe Zimmo detailpagina-test (geen warmup, geen cookies) ──────────
if (args.Contains("--zimmo-direct-detail-test"))
{
    var urlArg = args.SkipWhile(a => a != "--url").Skip(1).FirstOrDefault();
    if (string.IsNullOrWhiteSpace(urlArg))
    {
        logger.LogError("[Program] --url <url> argument is vereist bij --zimmo-direct-detail-test.");
        return;
    }

    logger.LogInformation("[Program] --zimmo-direct-detail-test modus — normale worker wordt NIET gestart.");
    using (var scope = host.Services.CreateScope())
    {
        var test = scope.ServiceProvider.GetRequiredService<ZimmoDirectDetailTest>();
        await test.RunAsync(urlArg);
    }
    logger.LogInformation("[Program] Zimmo direct detail-test voltooid. Afsluiten.");
    return;
}

// ── Externe HTTP-triggers (bv. smarterasp.net Task Scheduler) ────────────
// /ping   → keep-alive, voorkomt dat IIS de app pool idle-recycled
// /crawl  → forceert een crawl-check buiten de interne 30-min timer om
host.MapGet("/api/trigger/ping", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }));

host.MapGet("/api/trigger/crawl", async (string? key, IConfiguration cfg, CrawlerWorker worker) =>
{
    var expectedKey = cfg["TriggerKeys:MarketDataCrawl"];
    if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
        return Results.Unauthorized();

    _ = worker.TriggerRunAsync("http-trigger");
    return Results.Accepted(value: new { status = "Accepted", timestamp = DateTime.UtcNow });
});

// /diag → toont welke omgeving/config effectief geladen is (nooit geheime waarden zelf).
host.MapGet("/api/trigger/diag", (string? key, IConfiguration cfg, IHostEnvironment env) =>
{
    var expectedKey = cfg["TriggerKeys:MarketDataCrawl"];
    if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        environment = env.EnvironmentName,
        dotnetEnvironmentVar = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
        aspnetEnvironmentVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        enableCrawler = cfg["CrawlerSettings:EnableCrawler"],
        dryRun = cfg["CrawlerSettings:DryRun"],
        workerCheckIntervalMinutes = cfg["CrawlerSettings:WorkerCheckIntervalMinutes"],
        debugEnabled = cfg["CrawlerSettings:Debug:Enabled"],
        anthropicKeyConfigured = !string.IsNullOrEmpty(cfg["CrawlerSettings:AiExtraction:AnthropicApiKey"]),
        connectionStringConfigured = !string.IsNullOrEmpty(cfg.GetConnectionString("MarketData"))
    });
});

await host.RunAsync();
