using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Commands;
using GroupLN.MarketData.Infrastructure.Extensions;
using GroupLN.MarketData.Persistence;
using GroupLN.MarketData.Persistence.Extensions;
using GroupLN.MarketData.Worker.Workers;
using Microsoft.EntityFrameworkCore;

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
    logger.LogInformation(
        "GroupLN MarketData Worker gestart. " +
        "EnableCrawler={Enabled} | DryRun={DryRun} | MaxListings={Max} | Env={Env}",
        settings.EnableCrawler,
        settings.DryRun,
        settings.MaxListingsPerRun == 0 ? "onbeperkt" : settings.MaxListingsPerRun.ToString(),
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production");

    if (!settings.EnableCrawler)
        logger.LogWarning("⚠️  EnableCrawler = false. Crawling is uitgeschakeld. Wijzig in appsettings om te activeren.");

    if (settings.DryRun)
        logger.LogWarning("⚠️  DryRun = true. Er wordt NIETS naar de database geschreven.");

    if (settings.MaxListingsPerRun > 0)
        logger.LogWarning("⚠️  MaxListingsPerRun = {Max}. Testmodus actief.", settings.MaxListingsPerRun);
}

await host.RunAsync();
