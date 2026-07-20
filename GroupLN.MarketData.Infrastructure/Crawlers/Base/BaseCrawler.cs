using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers.Base;

public abstract class BaseCrawler : IRealEstateCrawler
{
    protected readonly IMarketListingService ListingService;
    protected readonly IPropertyNormalizer Normalizer;
    protected readonly CrawlerSettings Settings;
    protected readonly ILogger Logger;

    protected BaseCrawler(
        IMarketListingService listingService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        ILogger logger)
    {
        ListingService = listingService;
        Normalizer = normalizer;
        Settings = settings;
        Logger = logger;
    }

    public abstract string SourceName { get; }

    /// <summary>
    /// Geeft de source-specifieke instellingen terug. Subklassen overschrijven dit
    /// zodat rate-limit helpers de juiste source kunnen opzoeken.
    /// </summary>
    protected virtual SourceSettings? GetCurrentSourceSettings() => null;

    // ── Effective rate-limit helpers (source ?? global) ───────────────────────
    protected int EffectiveMaxRequestsPerMinute      => GetCurrentSourceSettings()?.MaxRequestsPerMinute      ?? Settings.MaxRequestsPerMinute;
    protected int EffectiveDelayBetweenRequestsSeconds => GetCurrentSourceSettings()?.DelayBetweenRequestsSeconds ?? Settings.DelayBetweenRequestsSeconds;
    protected int EffectiveHttpTimeoutSeconds        => GetCurrentSourceSettings()?.HttpTimeoutSeconds        ?? Settings.HttpTimeoutSeconds;
    protected int EffectivePlaywrightTimeoutMs       => GetCurrentSourceSettings()?.PlaywrightTimeoutMs       ?? Settings.PlaywrightTimeoutMs;

    /// <summary>
    /// Geeft de manuele test-URL's terug. Subklassen overschrijven dit om
    /// source-specifieke ManualTestListingUrls te leveren vanuit Sources[name].
    /// </summary>
    protected virtual IReadOnlyList<string> GetManualTestUrls() => [];

    /// <summary>
    /// true = Fase 2 (detailpagina's) wordt overgeslagen.
    /// Subklassen overschrijven op basis van Sources[name].SearchDebugMode.
    /// </summary>
    protected virtual bool SearchDebugMode => false;

    public async Task<CrawlerResult> CrawlAsync(CrawlerSource source, CancellationToken cancellationToken)
    {
        var result = new CrawlerResult { StartedAt = DateTime.UtcNow };
        var isDryRun = Settings.DryRun;
        var maxListings = Settings.MaxListingsPerRun;

        Logger.LogInformation(
            "[{Source}] ══ Crawl gestart ══ DryRun={DryRun} | MaxListings={Max} | SearchDebugMode={DbgMode}",
            SourceName,
            isDryRun,
            maxListings == 0 ? "onbeperkt" : maxListings.ToString(),
            SearchDebugMode);

        Logger.LogInformation(
            "[{Source}] Effective rate limit: MaxRequestsPerMinute={Rpm} | Delay={Delay}s | " +
            "HttpTimeout={Http}s | PlaywrightTimeout={Pw}ms",
            SourceName,
            EffectiveMaxRequestsPerMinute,
            EffectiveDelayBetweenRequestsSeconds,
            EffectiveHttpTimeoutSeconds,
            EffectivePlaywrightTimeoutMs);

        if (isDryRun)
            Logger.LogWarning("[{Source}] DryRun actief — GEEN data wordt opgeslagen.", SourceName);

        try
        {
            // ── ManualTestListingUrls: zoekpagina-scraping overslaan ───────────
            var manualTestUrls = GetManualTestUrls();
            if (manualTestUrls.Count > 0)
            {
                Logger.LogWarning(
                    "[{Source}] ManualTestListingUrls actief ({Count} URL(s)) — zoekpagina-scraping wordt overgeslagen.",
                    SourceName, manualTestUrls.Count);

                await ProcessListingUrlsAsync(
                    source, manualTestUrls, result,
                    isDryRun, maxListings, skipAllowedFilter: true, cancellationToken);

                result.Success = result.Errors == 0 || result.ListingsFound > 0;
                result.IsPartialRun = true;
                result.MarkInactiveSkipReason = "ManualTestListingUrls actief — geen volledige crawl";
                result.FinishedAt = DateTime.UtcNow;
                var d = result.FinishedAt.Value - result.StartedAt;
                Logger.LogInformation(
                    "[{Source}] ══ ManualTest klaar ({Duration:mm\\:ss}) ══ Gevonden: {Found} | Fouten: {Errors}{DryRun}",
                    SourceName, d, result.ListingsFound, result.Errors,
                    isDryRun ? " [DRYRUN - niets opgeslagen]" : "");
                return result;
            }

            // ══════════════════════════════════════════════════════════════════════
            // FASE 1 — URL-verzameling: alle zoekpagina's doorlopen, GEEN verwerking
            // MaxListingsPerRun wordt hier NIET toegepast.
            // ══════════════════════════════════════════════════════════════════════
            var searchUrls = (await GetSearchPageUrlsAsync(source, cancellationToken)).ToList();
            Logger.LogInformation(
                "[{Source}] Fase 1 — URL-verzameling: {Count} zoekpagina(s) te doorzoeken.",
                SourceName, searchUrls.Count);

            var allCollectedUrls = new List<string>();
            var pageIndex = 0;

            foreach (var searchUrl in searchUrls)
            {
                if (cancellationToken.IsCancellationRequested) break;
                pageIndex++;

                Logger.LogInformation("[{Source}] ▶ Zoekpagina {Current}/{Total}: {Url}",
                    SourceName, pageIndex, searchUrls.Count, searchUrl);

                List<string> pageUrls;
                try
                {
                    pageUrls = (await FetchListingUrlsFromSearchPageAsync(searchUrl, cancellationToken)).ToList();
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"[{searchUrl}] {ex.Message}");
                    Logger.LogWarning(ex, "[{Source}] ✗ Fout bij zoekpagina {Url}.", SourceName, searchUrl);
                    continue;
                }

                Logger.LogInformation(
                    "[{Source}]   Zoekpagina {Current}/{Total} klaar: {PageCount} URL's | cumulatief: {Total}",
                    SourceName, pageIndex, searchUrls.Count, pageUrls.Count, allCollectedUrls.Count + pageUrls.Count);

                if (pageUrls.Count == 0)
                {
                    Logger.LogInformation(
                        "[{Source}]   Pagina {Current}/{Total} leeg — wordt overgeslagen, volgende pagina gaat door.",
                        SourceName, pageIndex, searchUrls.Count);
                    continue;
                }

                allCollectedUrls.AddRange(pageUrls);
            }

            // Dedupliceren op URL-string over alle pagina's
            var uniqueListingUrls = allCollectedUrls
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Logger.LogInformation(
                "[{Source}] Fase 1 klaar. TotalCollectedListingUrls={Collected} | TotalUniqueListingUrls={Unique}",
                SourceName, allCollectedUrls.Count, uniqueListingUrls.Count);

            // MaxListingsPerRun: nu pas toepassen, na volledige deduplicatie
            var limitReached = maxListings > 0 && uniqueListingUrls.Count > maxListings;
            var listingsToProcess = limitReached
                ? uniqueListingUrls.Take(maxListings).ToList()
                : uniqueListingUrls;

            if (limitReached)
                result.IsPartialRun = true;

            if (maxListings > 0)
                Logger.LogInformation(
                    "[{Source}] MaxListingsPerRun={Max} | Beschikbaar={Unique} | ListingsToProcessAfterMaxLimit={ToProcess}",
                    SourceName, maxListings, uniqueListingUrls.Count, listingsToProcess.Count);
            else
                Logger.LogInformation(
                    "[{Source}] MaxListingsPerRun=onbeperkt | ListingsToProcessAfterMaxLimit={Count}",
                    SourceName, listingsToProcess.Count);

            // ══════════════════════════════════════════════════════════════════════
            // FASE 2 — Detailpagina verwerking
            // Overgeslagen als ImmowebSearchDebugMode=true — enkel URL-analyse.
            // ══════════════════════════════════════════════════════════════════════
            if (SearchDebugMode)
            {
                Logger.LogWarning(
                    "[{Source}] SearchDebugMode=true — Fase 2 overgeslagen. " +
                    "{Count} URL(s) verzameld, geen detailpagina's geopend. " +
                    "Zet SearchDebugMode=false voor volledige crawl.",
                    SourceName, uniqueListingUrls.Count);

                result.Success = true;
                result.IsPartialRun = true;
                result.MarkInactiveSkipReason = "SearchDebugMode actief — geen detailpagina's verwerkt";
                result.FinishedAt = DateTime.UtcNow;
                var debugDuration = result.FinishedAt.Value - result.StartedAt;
                Logger.LogInformation(
                    "[{Source}] ══ SearchDebugMode klaar ({Duration:mm\\:ss}) ══ Unieke listing-URLs: {Count}",
                    SourceName, debugDuration, uniqueListingUrls.Count);
                return result;
            }

            Logger.LogInformation(
                "[{Source}] Fase 2 — Detailpagina verwerking: {Count} listing(s) te verwerken.",
                SourceName, listingsToProcess.Count);

            var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var protectedExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var filteredOut = 0;
            var processed = 0;

            foreach (var listingUrl in listingsToProcess)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await ApplyRateLimitAsync(cancellationToken);
                    processed++;
                    Logger.LogDebug("[{Source}]   Detailpagina {N}/{Total}: {Url}",
                        SourceName, processed, listingsToProcess.Count, listingUrl);

                    var listing = await FetchAndParseListingAsync(listingUrl, source, cancellationToken);
                    if (listing is null)
                    {
                        Logger.LogDebug("[{Source}]   ⚠ Parsing mislukt of geen data: {Url}", SourceName, listingUrl);
                        continue;
                    }

                    if (!IsAllowed(listing))
                    {
                        filteredOut++;
                        Logger.LogInformation(
                            "[{Source}] ListingSkippedOutsideAllowedLocation | Url={Url} | PostalCode={PostalCode} | City={City}",
                            SourceName, listingUrl, listing.PostalCode ?? "?", listing.City ?? "?");
                        continue;
                    }

                    var normalized = Normalizer.Normalize(listing, source.Id);
                    if (string.IsNullOrEmpty(normalized.ExternalId))
                    {
                        Logger.LogDebug("[{Source}]   ⚠ ExternalId leeg na normalisatie: {Url}", SourceName, listingUrl);
                        continue;
                    }

                    seenExternalIds.Add(normalized.ExternalId);
                    result.ListingsFound++;
                    var assetId = await PersistOrLogAsync(normalized, result, isDryRun, source, cancellationToken);
                    await AfterPersistAsync(normalized, assetId, isDryRun, source, cancellationToken);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"[{listingUrl}] {ex.Message}");
                    Logger.LogWarning(ex, "[{Source}]   ✗ Fout bij listing {Url}.", SourceName, listingUrl);

                    // Bestaande listing beschermen: een fetch-fout betekent niet dat de listing verdwenen is.
                    try
                    {
                        var existingId = await ListingService.FindExternalIdByUrlAsync(listingUrl, source.Id, cancellationToken);
                        if (existingId is not null)
                        {
                            protectedExternalIds.Add(existingId);
                            Logger.LogDebug(
                                "[{Source}]   Beschermd: {ExternalId} (fetch-fout — telt niet als ontbrekend).",
                                SourceName, existingId);
                        }
                    }
                    catch { /* bescherming is best-effort, negeer fouten in de lookup */ }
                }
            }

            Logger.LogInformation(
                "[{Source}]   Fase 2 klaar. Verwerkt: {Processed} | Gefilterd: {Filtered} | Gevonden: {Found}",
                SourceName, processed, filteredOut, result.ListingsFound);

            result.Success = result.Errors == 0 || result.ListingsFound > 0;

            var (canMarkInactive, skipReason) = EvaluateCanMarkInactive(
                isDryRun, result.Success, limitReached, seenExternalIds.Count);

            result.MarkInactiveSkipReason = string.IsNullOrEmpty(skipReason) ? null : skipReason;

            if (canMarkInactive)
            {
                var safeIds = protectedExternalIds.Count > 0
                    ? seenExternalIds.Concat(protectedExternalIds)
                    : (IEnumerable<string>)seenExternalIds;

                if (protectedExternalIds.Count > 0)
                    Logger.LogInformation(
                        "[{Source}] MarkInactive: {Seen} gezien + {Protected} beschermd = {Total} veilige IDs.",
                        SourceName, seenExternalIds.Count, protectedExternalIds.Count,
                        seenExternalIds.Count + protectedExternalIds.Count);

                var markResult = await ListingService.MarkInactiveAsync(
                    source.Id, safeIds, Settings.MissingListingThreshold, cancellationToken);

                result.ListingsMissingCount = markResult.ListingsMissing;
                result.AssetsMarkedLikelySold += markResult.AssetsLikelySold;
                result.AssetsMarkedSoldConfirmed += markResult.AssetsSoldConfirmed;

                if (markResult.AssetsLikelySold > 0 || markResult.AssetsIsActiveSetFalse > 0)
                    Logger.LogInformation(
                        "[{Source}] Lifecycle-update: {LikelySold} LikelySold | {IsActiveFalse} IsActive=false.",
                        SourceName, markResult.AssetsLikelySold, markResult.AssetsIsActiveSetFalse);
            }
            else
            {
                Logger.LogWarning(
                    "[{Source}] MarkInactive OVERGESLAGEN — {Reason}",
                    SourceName, skipReason);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("[{Source}] Crawl geannuleerd.", SourceName);
            result.Success = false;
            result.IsPartialRun = true;
            result.MarkInactiveSkipReason = "Crawl geannuleerd";
            result.Message = "Geannuleerd";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Source}] Onverwachte fout tijdens crawl.", SourceName);
            result.Success = false;
            result.ErrorMessages.Add(ex.Message);
        }

        result.FinishedAt = DateTime.UtcNow;
        var duration = result.FinishedAt.Value - result.StartedAt;

        Logger.LogInformation(
            "[{Source}] ══ Crawl klaar ({Duration:mm\\:ss}) ══ Gevonden: {Found} | Nieuw: {Created} | Bijgewerkt: {Updated} | Fouten: {Errors}{DryRun}",
            SourceName,
            duration,
            result.ListingsFound,
            result.ListingsCreated,
            result.ListingsUpdated,
            result.Errors,
            isDryRun ? " [DRYRUN - niets opgeslagen]" : "");

        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Verwerkt een vaste lijst listing-URL's (manuele testmodus).</summary>
    private async Task ProcessListingUrlsAsync(
        CrawlerSource source,
        IReadOnlyList<string> listingUrls,
        CrawlerResult result,
        bool isDryRun,
        int maxListings,
        bool skipAllowedFilter,
        CancellationToken cancellationToken)
    {
        var processed = 0;
        foreach (var listingUrl in listingUrls)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (maxListings > 0 && result.ListingsFound >= maxListings)
            {
                Logger.LogWarning(
                    "[{Source}] MaxListingsPerRun ({Max}) bereikt — manuele test stopgezet.",
                    SourceName, maxListings);
                break;
            }

            try
            {
                await ApplyRateLimitAsync(cancellationToken);
                processed++;
                Logger.LogInformation("[{Source}] [MANUAL {N}/{Total}] {Url}",
                    SourceName, processed, listingUrls.Count, listingUrl);

                var listing = await FetchAndParseListingAsync(listingUrl, source, cancellationToken);
                if (listing is null)
                {
                    Logger.LogWarning("[{Source}]   ⚠ Parsing mislukt: {Url}", SourceName, listingUrl);
                    continue;
                }

                if (!skipAllowedFilter && !IsAllowed(listing))
                {
                    Logger.LogInformation(
                        "[{Source}] ListingSkippedOutsideAllowedLocation | Url={Url} | PostalCode={PostalCode} | City={City}",
                        SourceName, listingUrl, listing.PostalCode ?? "?", listing.City ?? "?");
                    continue;
                }

                var normalized = Normalizer.Normalize(listing, source.Id);
                if (string.IsNullOrEmpty(normalized.ExternalId))
                {
                    Logger.LogWarning("[{Source}]   ⚠ ExternalId leeg na normalisatie.", SourceName);
                    continue;
                }

                result.ListingsFound++;
                var manualAssetId = await PersistOrLogAsync(normalized, result, isDryRun, source, cancellationToken);
                await AfterPersistAsync(normalized, manualAssetId, isDryRun, source, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result.Errors++;
                result.ErrorMessages.Add($"[{listingUrl}] {ex.Message}");
                Logger.LogWarning(ex, "[{Source}]   ✗ Fout bij {Url}.", SourceName, listingUrl);
            }
        }
    }

    private async Task<long?> PersistOrLogAsync(
        NormalizedPropertyDto normalized,
        CrawlerResult result,
        bool isDryRun,
        CrawlerSource source,
        CancellationToken cancellationToken)
    {
        if (isDryRun)
        {
            result.ListingsCreated++;
            Logger.LogInformation(
                "[{Source}]   [DRYRUN] Zou aanmaken/bijwerken → ID={ExternalId} | {City} {PostalCode} | €{Price} | {Type} {SubType}",
                SourceName,
                normalized.ExternalId,
                normalized.City ?? "?",
                normalized.PostalCode ?? "?",
                normalized.AskingPrice.HasValue ? normalized.AskingPrice.Value.ToString("N0") : "?",
                normalized.PropertyType,
                normalized.PropertySubType);
            return null;
        }

        var (wasCreated, assetId) = await ListingService.UpsertListingAsync(normalized, cancellationToken);
        if (wasCreated)
        {
            result.ListingsCreated++;
            Logger.LogInformation(
                "[{Source}]   ✚ NIEUW: {ExternalId} | {City} {PostalCode} | €{Price}",
                SourceName, normalized.ExternalId, normalized.City, normalized.PostalCode,
                normalized.AskingPrice?.ToString("N0") ?? "?");
        }
        else
        {
            result.ListingsUpdated++;
            Logger.LogDebug(
                "[{Source}]   ↺ BIJGEWERKT: {ExternalId} | {City}",
                SourceName, normalized.ExternalId, normalized.City);
        }
        return assetId;
    }

    /// <summary>
    /// Hook die na elke persist (of dry-run log) wordt aangeroepen.
    /// Subklassen gebruiken dit om aanvullende data op te slaan (bijv. project-units).
    /// assetId is null in DryRun.
    /// </summary>
    protected virtual Task AfterPersistAsync(
        NormalizedPropertyDto normalized,
        long? assetId,
        bool isDryRun,
        CrawlerSource source,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected abstract Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken);

    protected abstract Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken);

    protected abstract Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken);

    protected virtual async Task ApplyRateLimitAsync(CancellationToken cancellationToken)
    {
        if (EffectiveDelayBetweenRequestsSeconds > 0)
            await Task.Delay(TimeSpan.FromSeconds(EffectiveDelayBetweenRequestsSeconds), cancellationToken);
    }

    /// <summary>
    /// Geografische filter. Subklassen overschrijven om source-specifieke
    /// AllowedLocations te controleren. Standaard: alles toelaten.
    /// </summary>
    protected virtual bool IsAllowed(ListingDto listing) => true;

    // Evalueert of inactive-markering veilig mag lopen voor deze crawl-run.
    // Geeft (canMark=true, skipReason="") terug bij goedkeuring, anders (false, reden).
    private (bool canMark, string skipReason) EvaluateCanMarkInactive(
        bool isDryRun, bool crawlSuccess, bool limitReached, int seenCount)
    {
        if (isDryRun)
            return (false, "DryRun actief");
        if (Settings.MaxListingsPerRun > 0)
            return (false, $"MaxListingsPerRun={Settings.MaxListingsPerRun} actief — gedeeltelijke run mogelijk");
        if (SearchDebugMode)
            return (false, "SearchDebugMode actief");
        if (!crawlSuccess)
            return (false, "Crawl niet succesvol");
        if (seenCount < Settings.MinListingsBeforeMarkInactive)
            return (false, $"Te weinig listings gevonden ({seenCount} < minimum {Settings.MinListingsBeforeMarkInactive})");
        return (true, string.Empty);
    }
}
