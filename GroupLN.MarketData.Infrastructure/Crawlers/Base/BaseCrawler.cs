using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers.Base;

public abstract class BaseCrawler : IRealEstateCrawler
{
    protected readonly IMarketPropertyService PropertyService;
    protected readonly IPropertyNormalizer Normalizer;
    protected readonly CrawlerSettings Settings;
    protected readonly ILogger Logger;

    protected BaseCrawler(
        IMarketPropertyService propertyService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        ILogger logger)
    {
        PropertyService = propertyService;
        Normalizer = normalizer;
        Settings = settings;
        Logger = logger;
    }

    public abstract string SourceName { get; }

    public async Task<CrawlerResult> CrawlAsync(CrawlerSource source, CancellationToken cancellationToken)
    {
        var result = new CrawlerResult { StartedAt = DateTime.UtcNow };
        var isDryRun = Settings.DryRun;
        var maxListings = Settings.MaxListingsPerRun;

        Logger.LogInformation(
            "[{Source}] ══ Crawl gestart ══ DryRun={DryRun} | MaxListings={Max} | PostcodeFilter={PostalFilter} | GemeenteFilter={CityFilter}",
            SourceName,
            isDryRun,
            maxListings == 0 ? "onbeperkt" : maxListings.ToString(),
            Settings.AllowedPostalCodes.Count > 0 ? string.Join(",", Settings.AllowedPostalCodes) : "geen",
            Settings.AllowedCities.Count > 0 ? string.Join(",", Settings.AllowedCities) : "geen");

        if (isDryRun)
            Logger.LogWarning("[{Source}] DryRun actief — GEEN data wordt opgeslagen.", SourceName);

        try
        {
            var searchUrls = (await GetSearchPageUrlsAsync(source, cancellationToken)).ToList();
            Logger.LogInformation("[{Source}] {Count} zoekpagina(s) te verwerken.", SourceName, searchUrls.Count);

            var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var limitReached = false;
            var filteredOut = 0;

            foreach (var searchUrl in searchUrls)
            {
                if (cancellationToken.IsCancellationRequested || limitReached) break;

                Logger.LogInformation("[{Source}] ▶ Zoekpagina ophalen: {Url}", SourceName, searchUrl);

                try
                {
                    var listingUrls = (await FetchListingUrlsFromSearchPageAsync(searchUrl, cancellationToken)).ToList();

                    Logger.LogInformation("[{Source}]   → {Count} listing-URL's gevonden op deze pagina.", SourceName, listingUrls.Count);

                    if (listingUrls.Count == 0)
                    {
                        Logger.LogInformation("[{Source}]   Geen listings op {Url} — verdere pagina's worden overgeslagen.", SourceName, searchUrl);
                        break;
                    }

                    var pageProcessed = 0;
                    foreach (var listingUrl in listingUrls)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        if (maxListings > 0 && result.ListingsFound >= maxListings)
                        {
                            Logger.LogWarning(
                                "[{Source}] MaxListingsPerRun ({Max}) bereikt na {Processed} verwerkte listings. Crawl stopgezet.",
                                SourceName, maxListings, result.ListingsFound);
                            limitReached = true;
                            break;
                        }

                        try
                        {
                            await ApplyRateLimitAsync(cancellationToken);
                            pageProcessed++;

                            Logger.LogDebug("[{Source}]   Detailpagina {N}: {Url}", SourceName, pageProcessed, listingUrl);

                            var listing = await FetchAndParseListingAsync(listingUrl, source, cancellationToken);
                            if (listing is null)
                            {
                                Logger.LogDebug("[{Source}]   ⚠ Parsing mislukt of geen data: {Url}", SourceName, listingUrl);
                                continue;
                            }

                            // Geografische filter
                            if (!IsAllowed(listing))
                            {
                                filteredOut++;
                                Logger.LogDebug(
                                    "[{Source}]   ⊘ Overgeslagen (filter): postcode={PostalCode}, gemeente={City}",
                                    SourceName, listing.PostalCode ?? "?", listing.City ?? "?");
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
                            }
                            else
                            {
                                var wasCreated = await PropertyService.UpsertPropertyAsync(normalized, cancellationToken);
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
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            result.Errors++;
                            result.ErrorMessages.Add($"[{listingUrl}] {ex.Message}");
                            Logger.LogWarning(ex, "[{Source}]   ✗ Fout bij listing {Url}.", SourceName, listingUrl);
                        }
                    }

                    Logger.LogInformation(
                        "[{Source}]   Pagina klaar. Verwerkt: {Processed} | Gevonden (cumulatief): {Found} | Gefilterd: {Filtered}",
                        SourceName, pageProcessed, result.ListingsFound, filteredOut);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    result.Errors++;
                    result.ErrorMessages.Add($"[{searchUrl}] {ex.Message}");
                    Logger.LogWarning(ex, "[{Source}] ✗ Fout bij zoekpagina {Url}.", SourceName, searchUrl);
                }
            }

            // ── MarkInactive bescherming ─────────────────────────────────────
            LogMarkInactiveDecision(isDryRun, limitReached, seenExternalIds.Count);

            var canMarkInactive = !isDryRun
                && !limitReached
                && seenExternalIds.Count >= Settings.MinListingsBeforeMarkInactive;

            if (canMarkInactive)
                await PropertyService.MarkInactiveAsync(source.Id, seenExternalIds, cancellationToken);

            result.Success = result.Errors == 0 || result.ListingsFound > 0;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("[{Source}] Crawl geannuleerd.", SourceName);
            result.Success = false;
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

    protected abstract Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken);

    protected abstract Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken);

    protected abstract Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken);

    protected virtual async Task ApplyRateLimitAsync(CancellationToken cancellationToken)
    {
        if (Settings.DelayBetweenRequestsSeconds > 0)
            await Task.Delay(TimeSpan.FromSeconds(Settings.DelayBetweenRequestsSeconds), cancellationToken);
    }

    /// <summary>
    /// Geeft true als de listing door de postcode/gemeente-filter mag.
    /// Lege filterlijsten = alles toelaten.
    /// </summary>
    protected virtual bool IsAllowed(ListingDto listing)
    {
        if (Settings.AllowedPostalCodes.Count > 0)
        {
            if (string.IsNullOrEmpty(listing.PostalCode))
                return false;
            if (!Settings.AllowedPostalCodes.Contains(listing.PostalCode.Trim(), StringComparer.OrdinalIgnoreCase))
                return false;
        }

        if (Settings.AllowedCities.Count > 0)
        {
            if (string.IsNullOrEmpty(listing.City))
                return false;
            var cityMatches = Settings.AllowedCities.Any(c =>
                listing.City.Contains(c, StringComparison.OrdinalIgnoreCase));
            if (!cityMatches)
                return false;
        }

        return true;
    }

    private void LogMarkInactiveDecision(bool isDryRun, bool limitReached, int seenCount)
    {
        if (isDryRun)
        {
            Logger.LogWarning(
                "[{Source}] MarkInactive OVERGESLAGEN — DryRun actief. Geen panden worden gedeactiveerd.",
                SourceName);
        }
        else if (limitReached)
        {
            Logger.LogWarning(
                "[{Source}] MarkInactive OVERGESLAGEN — MaxListingsPerRun was actief (gedeeltelijke run). " +
                "Panden worden niet gedeactiveerd om onterechte deactivaties te voorkomen.",
                SourceName);
        }
        else if (seenCount < Settings.MinListingsBeforeMarkInactive)
        {
            Logger.LogWarning(
                "[{Source}] MarkInactive OVERGESLAGEN — Te weinig listings gevonden ({Seen} < minimum {Min}). " +
                "Mogelijke crawl-fout. Geen panden worden gedeactiveerd.",
                SourceName, seenCount, Settings.MinListingsBeforeMarkInactive);
        }
        else
        {
            Logger.LogInformation(
                "[{Source}] MarkInactive wordt uitgevoerd voor {Count} geziene listings (minimum={Min}).",
                SourceName, seenCount, Settings.MinListingsBeforeMarkInactive);
        }
    }
}
