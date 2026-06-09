using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace GroupLN.MarketData.Worker.Workers;

public class CrawlerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CrawlerWorker> _logger;
    private readonly CrawlerSettings _settings;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public CrawlerWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<CrawlerWorker> logger,
        CrawlerSettings settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ── Veiligheidscheck bij opstart ─────────────────────────────────────
        if (!_settings.EnableCrawler)
        {
            _logger.LogWarning(
                "CrawlerSettings.EnableCrawler = false. " +
                "De worker is actief maar zal NOOIT crawlen. " +
                "Zet EnableCrawler = true in appsettings om te activeren.");

            // Wacht netjes op afsluiting — crawl nooit
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            return;
        }

        _logger.LogInformation(
            "CrawlerWorker gestart. Check-interval: {Interval} min. DryRun={DryRun}.",
            _checkInterval.TotalMinutes, _settings.DryRun);

        // Eenmalige volledige deduplicatie-scan bij opstart (alle bestaande assets)
        if (_settings.FullDeduplicationScanOnStartup && !_settings.DryRun)
        {
            _logger.LogInformation("[Deduplicatie] FullDeduplicationScanOnStartup = true — volledige scan starten...");
            try
            {
                await using var dedupScope   = _scopeFactory.CreateAsyncScope();
                var dedupService = dedupScope.ServiceProvider.GetRequiredService<IDeduplicationService>();
                var summary = await dedupService.RunAsync(DateTime.MinValue, stoppingToken);
                _logger.LogInformation(
                    "[Deduplicatie] Volledige scan klaar. {Scanned} assets, {Projects} project-candidates, {Units} unit-candidates, {Saved} opgeslagen.",
                    summary.NewAssetsScanned, summary.ProjectCandidatesFound, summary.UnitCandidatesFound, summary.CandidatesSaved);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Deduplicatie] Volledige scan bij opstart mislukt.");
            }
        }

        // Eerste check direct bij opstart uitvoeren
        await RunAllDueCrawlsAsync(stoppingToken);

        using var timer = new PeriodicTimer(_checkInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAllDueCrawlsAsync(stoppingToken);
        }

        _logger.LogInformation("CrawlerWorker gestopt.");
    }

    private async Task RunAllDueCrawlsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("═══ Bronnen controleren ({Time}) ═══", DateTime.Now.ToString("HH:mm:ss"));

        await using var scope = _scopeFactory.CreateAsyncScope();

        var sourceService = scope.ServiceProvider.GetRequiredService<ICrawlerSourceService>();
        var runService = scope.ServiceProvider.GetRequiredService<ICrawlerRunService>();
        var crawlerFactory = scope.ServiceProvider.GetRequiredService<ICrawlerFactory>();
        var historySummaryService = scope.ServiceProvider.GetRequiredService<IHistorySummaryService>();
        var areaStatisticsService = scope.ServiceProvider.GetRequiredService<IMarketAreaStatisticsService>();

        IReadOnlyList<Core.Entities.CrawlerSource> activeSources;
        try
        {
            activeSources = (await sourceService.GetActiveSourcesAsync(cancellationToken)).ToList();
            _logger.LogInformation("{Count} actieve bron(nen) gevonden: {Names}",
                activeSources.Count,
                activeSources.Count > 0 ? string.Join(", ", activeSources.Select(s => s.Name)) : "geen");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij ophalen van actieve bronnen uit database.");
            return;
        }

        if (activeSources.Count == 0)
        {
            _logger.LogInformation("Geen actieve bronnen — niets te doen.");
            return;
        }

        foreach (var source in activeSources)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var isDue = await sourceService.IsDueCrawlAsync(source.Id, cancellationToken);
                if (!isDue)
                {
                    _logger.LogDebug("[{Source}] Nog niet aan de beurt (elke {Hours}u).", source.Name, source.CrawlFrequencyHours);
                    continue;
                }

                await CrawlSourceAsync(source, runService, crawlerFactory, historySummaryService, areaStatisticsService, cancellationToken);
                await sourceService.UpdateLastCrawledAsync(source.Id, cancellationToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij crawlen van {Source}.", source.Name);
            }
        }

        _logger.LogInformation("═══ Bronnencheck klaar ═══");
    }

    private async Task CrawlSourceAsync(
        Core.Entities.CrawlerSource source,
        ICrawlerRunService runService,
        ICrawlerFactory crawlerFactory,
        IHistorySummaryService historySummaryService,
        IMarketAreaStatisticsService areaStatisticsService,
        CancellationToken cancellationToken)
    {
        var crawler = crawlerFactory.GetCrawler(source.Name);
        if (crawler is null)
        {
            _logger.LogWarning("[{Source}] Geen crawler-implementatie gevonden. Bron wordt overgeslagen.", source.Name);
            return;
        }

        _logger.LogInformation("[{Source}] ▶ Crawl starten...", source.Name);

        var runId = await runService.StartRunAsync(source.Id, cancellationToken);

        try
        {
            // Max 4 uur per bron om vastlopen te voorkomen
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromHours(4));

            var result = await crawler.CrawlAsync(source, timeoutCts.Token);

            await runService.CompleteRunAsync(runId, result, cancellationToken);

            _logger.LogInformation(
                "[{Source}] Crawl voltooid. Gevonden: {Found} | Nieuw: {Created} | Bijgewerkt: {Updated} | Fouten: {Errors}.",
                source.Name, result.ListingsFound, result.ListingsCreated, result.ListingsUpdated, result.Errors);

            // History summary schrijven (ook bij DryRun voor analyse)
            try
            {
                await historySummaryService.WriteHistorySummaryAsync(result.StartedAt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{Source}] History summary schrijven mislukt.", source.Name);
            }

            // Top-projecten rapport bijwerken
            try
            {
                await historySummaryService.WriteTopProjectsReportAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{Source}] Top-projecten rapport schrijven mislukt.", source.Name);
            }

            // Marktstatistieken per gemeente/postcode bijwerken
            try
            {
                await areaStatisticsService.UpdateStatisticsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{Source}] MarketAreaStatistics bijwerken mislukt.", source.Name);
            }

            // Verouderde listings markeren — alleen in echte run, niet DryRun
            if (!_settings.DryRun)
            {
                await using var staleScope = _scopeFactory.CreateAsyncScope();
                var listingService = staleScope.ServiceProvider.GetRequiredService<IMarketListingService>();
                var staleCount = await listingService.MarkStaleListingsInactiveAsync(
                    source.Id, _settings.MarkInactiveAfterDays, cancellationToken);

                if (staleCount > 0)
                    _logger.LogInformation("[{Source}] {Count} verouderde listings (>{Days} dagen) gemarkeerd als inactief.",
                        source.Name, staleCount, _settings.MarkInactiveAfterDays);

                // Deduplicatie: detecteer mogelijke duplicaten onder nieuwe/bijgewerkte assets
                try
                {
                    await using var dedupScope = _scopeFactory.CreateAsyncScope();
                    var dedupService = dedupScope.ServiceProvider.GetRequiredService<IDeduplicationService>();
                    var dedupSummary = await dedupService.RunAsync(result.StartedAt, cancellationToken);
                    _logger.LogInformation(
                        "[{Source}] Deduplicatie: {Scanned} assets gescand, {Projects} project-candidates, {Units} unit-candidates, {Saved} opgeslagen.",
                        source.Name,
                        dedupSummary.NewAssetsScanned,
                        dedupSummary.ProjectCandidatesFound,
                        dedupSummary.UnitCandidatesFound,
                        dedupSummary.CandidatesSaved);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[{Source}] Deduplicatie mislukt.", source.Name);
                }
            }
            else
            {
                _logger.LogInformation("[{Source}] [DRYRUN] MarkStaleListingsInactive en deduplicatie overgeslagen.", source.Name);
            }
        }
        catch (OperationCanceledException)
        {
            await runService.FailRunAsync(runId, "Timeout of geannuleerd.", cancellationToken);
            _logger.LogWarning("[{Source}] Crawl geannuleerd (timeout 4u of shutdown).", source.Name);
        }
        catch (Exception ex)
        {
            await runService.FailRunAsync(runId, ex.Message, cancellationToken);
            _logger.LogError(ex, "[{Source}] Crawl mislukt.", source.Name);
        }
    }
}
