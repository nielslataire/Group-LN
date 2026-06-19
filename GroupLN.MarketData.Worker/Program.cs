using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Commands;
using GroupLN.MarketData.Infrastructure.Extensions;
using GroupLN.MarketData.Persistence;
using GroupLN.MarketData.Persistence.Extensions;
using GroupLN.MarketData.Worker.Workers;
using Microsoft.EntityFrameworkCore;

// Helperfunctie: gebruik een scoped service en voer een actie uit, daarna exit
static async Task RunScopedCommandAsync<T>(IHost host, Func<T, Task> action) where T : notnull
{
    using var scope = host.Services.CreateScope();
    var cmd = scope.ServiceProvider.GetRequiredService<T>();
    await action(cmd);
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMarketDataPersistence(context.Configuration);
        services.AddMarketDataInfrastructure(context.Configuration);
        services.AddHostedService<CrawlerWorker>();
    })
    .Build();


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
        ai.MaxAiExtractionsPerRun);

    if (ai.EnableAiProjectExtraction && !apiKeyConfigured)
        logger.LogWarning(
            "⚠️  AiExtraction.EnableAiProjectExtraction=true maar AnthropicApiKey is NIET geconfigureerd. " +
            "Stel in via user secrets: dotnet user-secrets set " +
            "\"CrawlerSettings:AiExtraction:AnthropicApiKey\" \"<key>\"");
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

await host.RunAsync();
