using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Worker.Workers;

public class CrawlerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CrawlerWorker> _logger;
    private readonly CrawlerSettings _settings;
    // Voorkomt overlappende runs wanneer de interne timer en een externe HTTP-trigger
    // (bv. smarterasp.net Task Scheduler) rond hetzelfde moment afgaan.
    private readonly SemaphoreSlim _runLock = new(1, 1);


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
        if (!_settings.EnableCrawler)
        {
            _logger.LogWarning(
                "CrawlerSettings.EnableCrawler = false. " +
                "De worker is actief maar zal NOOIT crawlen. " +
                "Zet EnableCrawler = true in appsettings om te activeren.");
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            return;
        }

        var checkIntervalMinutes = _settings.WorkerCheckIntervalMinutes > 0
            ? _settings.WorkerCheckIntervalMinutes
            : 30;
        var checkInterval = TimeSpan.FromMinutes(checkIntervalMinutes);

        _logger.LogInformation(
            "[Scheduler] WorkerCheckInterval={Interval}m | DryRun={DryRun} | GlobalForceCrawl={Force}",
            checkIntervalMinutes, _settings.DryRun, _settings.ForceCrawl);

        // Eenmalige volledige deduplicatie-scan bij opstart
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

        // Eerste check direct bij opstart
        await RunLockedAsync(stoppingToken, "startup");

        using var timer = new PeriodicTimer(checkInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunLockedAsync(stoppingToken, "scheduler");
        }

        _logger.LogInformation("CrawlerWorker gestopt.");
    }

    /// <summary>
    /// Extern aanroepbaar (bv. vanaf het /api/trigger/crawl HTTP-endpoint) om buiten de
    /// interne timer om een crawl-check te forceren. Slaat over als er al een run bezig is.
    /// </summary>
    public async Task TriggerRunAsync(string triggeredBy, CancellationToken cancellationToken = default)
        => await RunLockedAsync(cancellationToken, triggeredBy);

    private async Task RunLockedAsync(CancellationToken cancellationToken, string triggeredBy)
    {
        if (!await _runLock.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            _logger.LogWarning("Crawl-run ({TriggeredBy}) overgeslagen: vorige run is nog actief.", triggeredBy);
            return;
        }

        try
        {
            await RunAllDueCrawlsAsync(cancellationToken);
        }
        finally
        {
            _runLock.Release();
        }
    }

    private async Task RunAllDueCrawlsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("═══ Bronnen controleren ({Time}) ═══", DateTime.Now.ToString("HH:mm:ss"));

        await using var scope = _scopeFactory.CreateAsyncScope();

        var sourceService        = scope.ServiceProvider.GetRequiredService<ICrawlerSourceService>();
        var runService           = scope.ServiceProvider.GetRequiredService<ICrawlerRunService>();
        var crawlerFactory       = scope.ServiceProvider.GetRequiredService<ICrawlerFactory>();
        var historySummaryService  = scope.ServiceProvider.GetRequiredService<IHistorySummaryService>();
        var areaStatisticsService  = scope.ServiceProvider.GetRequiredService<IMarketAreaStatisticsService>();

        IReadOnlyList<CrawlerSource> activeSources;
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

        bool anyCrawled = false;

        foreach (var source in activeSources)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                // Controleer appsettings Sources[name].Enabled
                var hasSrcSettings  = _settings.Sources.TryGetValue(source.Name, out var srcSettings);
                var settingsEnabled = !hasSrcSettings || (srcSettings!.Enabled);

                _logger.LogInformation(
                    "[{Source}] DB.IsActive=true | Settings.Enabled={Enabled}",
                    source.Name, settingsEnabled);

                if (!settingsEnabled)
                {
                    _logger.LogWarning(
                        "[{Source}] Overgeslagen — Sources.{Source}.Enabled = false in appsettings.",
                        source.Name, source.Name);
                    continue;
                }

                // ── Per-source interval + ForceCrawl ────────────────────────────
                var intervalMinutes = (hasSrcSettings && srcSettings!.CrawlIntervalMinutes > 0)
                    ? srcSettings.CrawlIntervalMinutes
                    : (_settings.WorkerCheckIntervalMinutes > 0 ? _settings.WorkerCheckIntervalMinutes * 2 : 60);

                var retryIntervalMinutes = _settings.RetryIntervalMinutes > 0
                    ? _settings.RetryIntervalMinutes
                    : 30;

                var forceCrawl = (hasSrcSettings && srcSettings!.ForceCrawl.HasValue)
                    ? srcSettings.ForceCrawl!.Value
                    : _settings.ForceCrawl;

                var status = await sourceService.GetOrCreateStatusAsync(source.Name, cancellationToken);

                bool isDue;
                if (forceCrawl)
                {
                    isDue = true;
                }
                else if (status.NextCrawlAt.HasValue)
                {
                    isDue = DateTime.UtcNow >= status.NextCrawlAt.Value;
                }
                else
                {
                    // Geen NextCrawlAt → val terug op legacy IsDue
                    isDue = await sourceService.IsDueCrawlAsync(source.Id, cancellationToken);
                }

                var lastSuccess = status.LastSuccessfulCrawlAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "—";
                var nextCrawl   = status.NextCrawlAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "—";

                _logger.LogInformation(
                    "[Scheduler] Source={Source} | Enabled={Enabled} | ForceCrawl={ForceCrawl} | LastSuccess={LastSuccess} | Interval={Interval}m | Next={Next} | Due={Due}",
                    source.Name, settingsEnabled, forceCrawl, lastSuccess, intervalMinutes, nextCrawl, isDue);

                if (!isDue)
                {
                    if (status.NextCrawlAt.HasValue)
                    {
                        var remaining = status.NextCrawlAt.Value - DateTime.UtcNow;
                        _logger.LogInformation(
                            "[Scheduler] Waiting | Source={Source} | Remaining={Remaining}",
                            source.Name, remaining.ToString(@"hh\:mm\:ss"));
                    }
                    continue;
                }

                anyCrawled = true;
                await CrawlSourceAsync(
                    source, srcSettings, intervalMinutes, retryIntervalMinutes,
                    sourceService, runService, crawlerFactory,
                    historySummaryService, areaStatisticsService,
                    cancellationToken);

                await sourceService.UpdateLastCrawledAsync(source.Id, cancellationToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij crawlen van {Source}.", source.Name);
            }
        }

        if (!anyCrawled)
        {
            // Wachtstatus tonen voor de bron met de vroegste NextCrawlAt
            await using var statusScope = _scopeFactory.CreateAsyncScope();
            var svc = statusScope.ServiceProvider.GetRequiredService<ICrawlerSourceService>();
            var sources = (await svc.GetActiveSourcesAsync(cancellationToken)).ToList();
            DateTime? earliest = null;
            string? nextSource = null;
            foreach (var s in sources)
            {
                var st = await svc.GetOrCreateStatusAsync(s.Name, cancellationToken);
                if (st.NextCrawlAt.HasValue && (earliest is null || st.NextCrawlAt < earliest))
                {
                    earliest   = st.NextCrawlAt;
                    nextSource = s.Name;
                }
            }
            if (earliest.HasValue && nextSource is not null)
            {
                var remaining = earliest.Value - DateTime.UtcNow;
                _logger.LogInformation(
                    "[Scheduler] Waiting | NextDueSource={Source} | NextCrawlAt={NextCrawlAt} | Remaining={Remaining}",
                    nextSource,
                    earliest.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    remaining > TimeSpan.Zero ? remaining.ToString(@"hh\:mm\:ss") : "00:00:00");
            }
        }

        _logger.LogInformation("═══ Bronnencheck klaar ═══");
    }

    private async Task CrawlSourceAsync(
        CrawlerSource source,
        SourceSettings? srcSettings,
        int intervalMinutes,
        int retryIntervalMinutes,
        ICrawlerSourceService sourceService,
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

        if (!crawler.SourceName.Equals(source.Name, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "[{Source}] Crawler-mismatch: DB.Name='{DbName}' maar crawler.SourceName='{CrawlerName}'. Bron overgeslagen.",
                source.Name, source.Name, crawler.SourceName);
            return;
        }

        _logger.LogInformation("[{Source}] ▶ Crawl starten met {CrawlerType}...", source.Name, crawler.GetType().Name);

        _logger.LogInformation("[Scheduler] Source={Source} | Due=true — crawl starten...", source.Name);
        await sourceService.MarkCrawlStartedAsync(source.Name, cancellationToken);
        var runId = await runService.StartRunAsync(source.Id, cancellationToken);
        var nextCrawlAtOnSuccess = DateTime.UtcNow.AddMinutes(intervalMinutes);
        var nextCrawlAtOnRetry   = DateTime.UtcNow.AddMinutes(retryIntervalMinutes);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromHours(4));

            var result = await crawler.CrawlAsync(source, timeoutCts.Token);

            await runService.CompleteRunAsync(runId, result, cancellationToken);
            await sourceService.MarkCrawlSucceededAsync(source.Name, result, nextCrawlAtOnSuccess, cancellationToken);

            _logger.LogInformation(
                "[{Source}] Crawl voltooid. Gevonden: {Found} | Nieuw: {Created} | Bijgewerkt: {Updated} | Fouten: {Errors}.",
                source.Name, result.ListingsFound, result.ListingsCreated, result.ListingsUpdated, result.Errors);
            _logger.LogInformation(
                "[Scheduler] Source={Source} | Completed | Success=true | NextCrawlAt={Next}",
                source.Name, nextCrawlAtOnSuccess.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));

            // History + statistics
            try { await historySummaryService.WriteHistorySummaryAsync(result.StartedAt, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "[{Source}] History summary mislukt.", source.Name); }

            try { await historySummaryService.WriteTopProjectsReportAsync(cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "[{Source}] Top-projecten rapport mislukt.", source.Name); }

            try { await areaStatisticsService.UpdateStatisticsAsync(cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "[{Source}] MarketAreaStatistics mislukt.", source.Name); }

            if (!_settings.DryRun)
            {
                var canMarkStale = result.Success
                    && !result.IsPartialRun
                    && string.IsNullOrEmpty(result.MarkInactiveSkipReason)
                    && _settings.MaxListingsPerRun == 0;

                if (canMarkStale)
                {
                    await using var staleScope   = _scopeFactory.CreateAsyncScope();
                    var listingService = staleScope.ServiceProvider.GetRequiredService<IMarketListingService>();
                    var staleResult    = await listingService.MarkStaleListingsInactiveAsync(
                        source.Id, _settings.MarkInactiveAfterDays, cancellationToken);
                    if (staleResult.ListingsDeactivated > 0)
                        _logger.LogInformation(
                            "[{Source}] Stale cleanup (>{Days}d): {Count} listings inactief | {LikelySold} LikelySold | {Inactive} IsActive=false.",
                            source.Name, _settings.MarkInactiveAfterDays,
                            staleResult.ListingsDeactivated, staleResult.AssetsLikelySold, staleResult.AssetsIsActiveSetFalse);
                }
                else
                {
                    var reason = !result.Success ? "Crawl niet succesvol"
                        : result.IsPartialRun ? "Gedeeltelijke run"
                        : !string.IsNullOrEmpty(result.MarkInactiveSkipReason) ? result.MarkInactiveSkipReason
                        : $"MaxListingsPerRun={_settings.MaxListingsPerRun} actief";
                    _logger.LogWarning("[{Source}] MarkStaleListingsInactive overgeslagen — {Reason}", source.Name, reason);
                }

                // Deduplicatie
                try
                {
                    await using var dedupScope = _scopeFactory.CreateAsyncScope();
                    var dedupService = dedupScope.ServiceProvider.GetRequiredService<IDeduplicationService>();
                    var dedupSummary = await dedupService.RunAsync(result.StartedAt, cancellationToken);
                    _logger.LogInformation(
                        "[{Source}] Deduplicatie: {Scanned} gescand, {Projects} project-candidates, {Units} unit-candidates, {Saved} opgeslagen.",
                        source.Name, dedupSummary.NewAssetsScanned, dedupSummary.ProjectCandidatesFound,
                        dedupSummary.UnitCandidatesFound, dedupSummary.CandidatesSaved);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "[{Source}] Deduplicatie mislukt.", source.Name); }

                // Canonical projects herberekenen
                if (_settings.RebuildCanonicalProjectsAfterDedup)
                {
                    try
                    {
                        await using var canonicalScope = _scopeFactory.CreateAsyncScope();
                        var canonicalCmd = canonicalScope.ServiceProvider.GetRequiredService<RebuildCanonicalProjectsCommand>();
                        await canonicalCmd.ExecuteAsync(cancellationToken);
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "[{Source}] Canonical projects rebuild mislukt.", source.Name); }
                }

                // Canonical units herberekenen na canonical projects
                if (_settings.RebuildCanonicalProjectsAfterDedup)
                {
                    try
                    {
                        await using var cuScope = _scopeFactory.CreateAsyncScope();
                        var cuCmd = cuScope.ServiceProvider.GetRequiredService<RebuildCanonicalUnitsCommand>();
                        await cuCmd.ExecuteAsync(null, cancellationToken);
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "[{Source}] Canonical units rebuild mislukt.", source.Name); }
                }
            }
            else
            {
                _logger.LogInformation("[{Source}] [DRYRUN] Post-crawl stappen overgeslagen.", source.Name);
            }
        }
        catch (OperationCanceledException)
        {
            await runService.FailRunAsync(runId, "Timeout of geannuleerd.", cancellationToken);
            await sourceService.MarkCrawlFailedAsync(source.Name, "Timeout of geannuleerd.", nextCrawlAtOnRetry, cancellationToken);
            _logger.LogWarning(
                "[Scheduler] Source={Source} | Completed | Success=false (geannuleerd) | RetryAt={Retry}",
                source.Name, nextCrawlAtOnRetry.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        }
        catch (Exception ex)
        {
            await runService.FailRunAsync(runId, ex.Message, cancellationToken);
            await sourceService.MarkCrawlFailedAsync(source.Name, ex.Message, nextCrawlAtOnRetry, cancellationToken);
            _logger.LogError(ex, "[{Source}] Crawl mislukt.", source.Name);
            _logger.LogInformation(
                "[Scheduler] Source={Source} | Completed | Success=false | RetryAt={Retry}",
                source.Name, nextCrawlAtOnRetry.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        }
    }
}
