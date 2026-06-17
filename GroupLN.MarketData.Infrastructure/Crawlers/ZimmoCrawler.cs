using System.Collections.Concurrent;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Browser;
using GroupLN.MarketData.Infrastructure.Crawlers.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

/// <summary>
/// Volledige Zimmo crawler. Twee modi via Sources["Zimmo"].OpenDetailPages:
///   false — zoekkaart-data uit __NEXT_DATA__ van de zoekpagina (geen detail-navigatie).
///           Lat/Lon zijn nullable; geo-resolving via bestaande geo-resolver op postcode/adres.
///   true  — detailpagina's openen via __NEXT_DATA__ + JSON-LD fallback (kan Cloudflare 403 geven).
/// Zoekpagina's gebruiken altijd een geïsoleerde ephemeral context per pagina (geen persistent context).
/// </summary>
public class ZimmoCrawler : BaseCrawler
{
    private readonly PlaywrightBrowserService _browser;

    // searchPageUrl → locatie-context, gevuld in GetSearchPageUrlsAsync
    private readonly Dictionary<string, LocationContext> _urlContext = new(StringComparer.OrdinalIgnoreCase);

    // ExternalId → gecachte search-card data, gevuld in Fase 1 wanneer OpenDetailPages=false
    private readonly Dictionary<string, ListingDto> _searchCardCache = new(StringComparer.OrdinalIgnoreCase);

    // Project-units gebufferd per crawl-sessie, geconsumeerd door AfterPersistAsync
    private readonly Dictionary<string, List<ProjectGroupUnitDto>> _pendingProjectUnits = new();

    private static readonly LocationSettings[] DefaultLocations =
    [
        new() { City = "Brugge",        PostalCode = "8000" },
        new() { City = "Sint-Michiels", PostalCode = "8200" },
        new() { City = "Beernem",       PostalCode = "8730" },
    ];

    public ZimmoCrawler(
        IMarketListingService listingService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        PlaywrightBrowserService browser,
        ILogger<ZimmoCrawler> logger)
        : base(listingService, normalizer, settings, logger)
    {
        _browser = browser;
    }

    public override string SourceName => "Zimmo";

    protected override bool SearchDebugMode => GetSourceSettings().SearchDebugMode;

    protected override IReadOnlyList<string> GetManualTestUrls() =>
        GetSourceSettings().ManualTestListingUrls is { Count: > 0 } src ? src : base.GetManualTestUrls();

    private SourceSettings GetSourceSettings() =>
        Settings.Sources.TryGetValue("Zimmo", out var s) ? s : new SourceSettings();

    private bool OpenDetailPages => GetSourceSettings().OpenDetailPages;

    protected override bool IsAllowed(ListingDto listing)
    {
        var locs = GetSourceSettings().AllowedLocations;
        if (locs.Count == 0) return true;
        var codes = locs
            .Where(l => !string.IsNullOrEmpty(l.PostalCode))
            .Select(l => l.PostalCode!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return codes.Count == 0
            || (!string.IsNullOrEmpty(listing.PostalCode)
                && codes.Contains(listing.PostalCode.Trim()));
    }

    // Geen rate-limit delay in Fase 2 wanneer we enkel de cache lezen (geen netwerk)
    protected override Task ApplyRateLimitAsync(CancellationToken cancellationToken) =>
        OpenDetailPages
            ? base.ApplyRateLimitAsync(cancellationToken)
            : Task.CompletedTask;

    // ── Fase 1: zoek-URL's genereren ─────────────────────────────────────────

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        _urlContext.Clear();
        _searchCardCache.Clear();
        _pendingProjectUnits.Clear();

        var src = GetSourceSettings();
        var locations = src.AllowedLocations.Count > 0
            ? (IReadOnlyList<LocationSettings>)src.AllowedLocations
            : DefaultLocations;
        var maxPages = src.MaxSearchPagesPerLocation > 0 ? src.MaxSearchPagesPerLocation : 5;

        Logger.LogInformation(
            "[Zimmo] {Count} locatie(s) | MaxPagesPerLocatie={Max} | OpenDetailPages={Detail}",
            locations.Count, maxPages, OpenDetailPages);

        var urls = new List<string>();
        foreach (var loc in locations)
        {
            int? placeId = null;
            if (!string.IsNullOrEmpty(loc.PostalCode)
                && ZimmoSearchUrlBuilder.PlaceIdByPostalCode.TryGetValue(loc.PostalCode, out var id))
                placeId = id;

            var searchUrl = ZimmoSearchUrlBuilder.Build(placeId);
            _urlContext[searchUrl] = new LocationContext(loc.PostalCode ?? "", loc.City ?? "", placeId, maxPages);

            Logger.LogInformation(
                "[Zimmo] {City} ({PostalCode}) placeId={PlaceId} → {Url}",
                loc.City, loc.PostalCode, placeId?.ToString() ?? "–", searchUrl);

            urls.Add(searchUrl);
        }

        return Task.FromResult<IEnumerable<string>>(urls);
    }

    // ── Fase 1b: listing-URL's per locatie verzamelen (paginatie) ─────────────

    protected override async Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
    {
        _urlContext.TryGetValue(searchPageUrl, out var ctx);
        ctx ??= new LocationContext("", "?", null, 5);

        var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allUrls = new List<string>();
        var visitedPageUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? currentPageUrl = searchPageUrl;
        var pageNum = 0;

        while (currentPageUrl is not null && pageNum < ctx.MaxPages)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (!visitedPageUrls.Add(currentPageUrl))
            {
                Logger.LogWarning("[Zimmo] {City}: paginatie-loop gedetecteerd — gestopt.", ctx.City);
                break;
            }

            pageNum++;

            var (found, nextPageUrl) = await LoadSearchPageAsync(
                currentPageUrl, ctx.City, pageNum, cancellationToken);

            var newCount = 0;
            foreach (var u in found)
            {
                var extId = ZimmoListingParser.ExtractExternalIdFromUrl(u.Split('?')[0]);
                if (extId is null || seenExternalIds.Add(extId))
                {
                    allUrls.Add(u);
                    newCount++;
                }
            }

            Logger.LogInformation(
                "[Zimmo] {City} p{N}: gevonden={Found} | nieuw={New} | uniek={Total} | volgende={Next}",
                ctx.City, pageNum, found.Count, newCount, seenExternalIds.Count,
                nextPageUrl ?? "(geen)");

            if (found.Count == 0)
            {
                Logger.LogInformation("[Zimmo] {City} p{N}: geen listings — paginatie stopt.", ctx.City, pageNum);
                break;
            }

            currentPageUrl = nextPageUrl;

            // Rate-limit altijd tussen zoekpagina's (ook in search-card modus)
            if (currentPageUrl is not null && pageNum < ctx.MaxPages)
                await base.ApplyRateLimitAsync(cancellationToken);
        }

        Logger.LogInformation(
            "[Zimmo] {City} ({PostalCode}) — {Count} unieke listing-URL's | {Cached} search-cards in cache",
            ctx.City, ctx.PostalCode, allUrls.Count, _searchCardCache.Count);

        return allUrls;
    }

    // ── Kern: één zoekpagina laden + search-cards cachen ────────────────────

    private async Task<(IReadOnlyList<string> Urls, string? NextPageUrl)> LoadSearchPageAsync(
        string pageUrl, string city, int pageNum, CancellationToken ct)
    {
        var page = await _browser.NewPageAsync(Settings.UserAgent);
        try
        {
            IResponse? navResponse = null;
            try
            {
                navResponse = await page.GotoAsync(pageUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout   = Settings.PlaywrightTimeoutMs
                });
            }
            catch (PlaywrightException ex)
            {
                Logger.LogWarning("[Zimmo] {City} p{N}: GotoAsync fout: {Msg}", city, pageNum, ex.Message);
            }

            await page.WaitForTimeoutAsync(2000);

            var httpStatus = navResponse?.Status ?? 0;
            var pageTitle  = await page.TitleAsync();
            var bodyText   = await page.EvaluateAsync<string>(
                "() => (document.body?.innerText ?? '').slice(0, 10000)");

            if (IsChallengePage(httpStatus, pageTitle, bodyText))
            {
                Logger.LogWarning(
                    "[Zimmo] {City} p{N}: CloudflareBlocked (HTTP {Status}, '{Title}') — locatie overgeslagen.",
                    city, pageNum, httpStatus, pageTitle);
                return ([], null);
            }

            // Listing-URL's extracteren uit DOM
            var rawHrefs = await page.EvaluateAsync<string[]>(
                "() => Array.from(document.querySelectorAll('.property-item a.property-item_link[href]'))" +
                ".map(a => a.getAttribute('href') || '').filter(h => h.length > 0)") ?? [];

            const string ZimmoBase = "https://www.zimmo.be";
            // Bewaar volledige href (incl. ?search=...) voor navigatie in OpenDetailPages-modus.
            // Dedup binnen pagina op canonical URL (zonder querystring).
            var listingUrls = rawHrefs
                .Select(h => h.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? h : ZimmoBase + h)
                .GroupBy(u => u.Split('?')[0], StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            Logger.LogInformation(
                "[Zimmo] {City} p{N}: HTTP {Status} | {Count} listing-URL's",
                city, pageNum, httpStatus, listingUrls.Count);

            // __NEXT_DATA__ ophalen — altijd, maar search-card parsing alleen bij OpenDetailPages=false
            var nextDataJson = await page.EvaluateAsync<string?>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); " +
                "return el ? el.textContent : null; }");

            if (!string.IsNullOrEmpty(nextDataJson) && !OpenDetailPages)
            {
                var cards = ZimmoSearchCardParser.ParseSearchCards(nextDataJson, Logger);
                var added = 0;
                foreach (var card in cards)
                {
                    if (!string.IsNullOrEmpty(card.ExternalId))
                    {
                        _searchCardCache[card.ExternalId] = card;
                        added++;
                    }
                }
                Logger.LogInformation(
                    "[Zimmo] {City} p{N}: {Added} search-cards gecached (totaal={Total})",
                    city, pageNum, added, _searchCardCache.Count);
            }

            var nextPageUrl = await ExtractNextPageUrlAsync(page);
            return (listingUrls, nextPageUrl);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[Zimmo] Fout bij zoekpagina {Url}", pageUrl);
            return ([], null);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    // ── Fase 2: listing parsen ────────────────────────────────────────────────

    protected override async Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken)
    {
        var navigationUrl = listingUrl;
        var canonicalUrl  = listingUrl.Split('?')[0];
        var externalId    = ZimmoListingParser.ExtractExternalIdFromUrl(canonicalUrl);

        // ── Search-card modus: geen detail-navigatie ───────────────────────────
        if (!OpenDetailPages)
        {
            if (externalId is not null && _searchCardCache.TryGetValue(externalId, out var cached))
            {
                cached.NavigationUrl = navigationUrl;
                cached.CanonicalUrl  = canonicalUrl;
                LogParsedListing(cached, "search-card cache");
                return cached;
            }

            Logger.LogDebug(
                "[Zimmo] ExternalId '{Id}' niet in search-card cache — listing overgeslagen.", externalId ?? "?");
            return null;
        }

        // ── Detail-navigatie modus (OpenDetailPages: true) ────────────────────
        return await FetchDetailPageAsync(navigationUrl, canonicalUrl, cancellationToken);
    }

    private async Task<ListingDto?> FetchDetailPageAsync(
        string navigationUrl, string canonicalUrl, CancellationToken ct)
    {
        var page = await _browser.NewPageAsync(Settings.UserAgent);
        try
        {
            var capturedApiUrls = new ConcurrentBag<string>();
            page.Response += (_, r) =>
            {
                if (r.Url.Contains("zimmo.be", StringComparison.OrdinalIgnoreCase)
                    && r.Headers.TryGetValue("content-type", out var ctype)
                    && ctype.Contains("json", StringComparison.OrdinalIgnoreCase))
                    capturedApiUrls.Add(r.Url);
            };

            IResponse? navResponse = null;
            try
            {
                navResponse = await page.GotoAsync(navigationUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout   = Settings.PlaywrightTimeoutMs
                });
            }
            catch (PlaywrightException ex)
            {
                Logger.LogWarning("[Zimmo] Detail GotoAsync fout: {Msg}", ex.Message);
            }

            await page.WaitForTimeoutAsync(1500);

            var httpStatus = navResponse?.Status ?? 0;
            var pageTitle  = await page.TitleAsync();
            var bodySnip   = await page.EvaluateAsync<string>(
                "() => (document.body?.innerText ?? '').slice(0, 5000)");

            if (IsChallengePage(httpStatus, pageTitle, bodySnip))
            {
                Logger.LogWarning(
                    "[Zimmo] DetailPageCloudflare | HTTP {Status} | '{Title}' | {Url}",
                    httpStatus, pageTitle, canonicalUrl);
                return null;
            }

            await HandleCookieConsentAsync(page);

            var nextDataJson = await page.EvaluateAsync<string?>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); " +
                "return el ? el.textContent : null; }");

            var isProjectUrl = IsProjectUrl(canonicalUrl);

            if (!string.IsNullOrEmpty(nextDataJson))
            {
                ListingDto? dto = null;

                if (isProjectUrl)
                    dto = ZimmoProjectParser.TryParseNextData(nextDataJson, canonicalUrl, Logger);

                dto ??= ZimmoListingParser.TryParseNextData(nextDataJson, canonicalUrl, Logger);

                if (dto is not null)
                {
                    dto.NavigationUrl = navigationUrl;
                    LogParsedListing(dto, "__NEXT_DATA__");

                    if (isProjectUrl || dto.PropertyTypeRaw == "PROJECT")
                    {
                        var units = ZimmoProjectParser.ParseUnitsFromJson(
                            nextDataJson, dto.ExternalId, dto.ProjectName, Logger);
                        if (units.Count > 0)
                            _pendingProjectUnits[dto.ExternalId] = units;
                    }

                    return dto;
                }
            }

            Logger.LogWarning(
                "[Zimmo] __NEXT_DATA__ niet bruikbaar voor {Url} (API-calls: {Count}) — JSON-LD fallback.",
                canonicalUrl, capturedApiUrls.Count);

            var html      = await page.ContentAsync();
            var jsonLdDto = ZimmoListingParser.TryParseJsonLd(html, canonicalUrl, Logger);
            if (jsonLdDto is not null)
            {
                jsonLdDto.NavigationUrl = navigationUrl;
                LogParsedListing(jsonLdDto, "JSON-LD");
                return jsonLdDto;
            }

            await WriteDetailDebugAsync(canonicalUrl, html, nextDataJson, page, ct);
            Logger.LogWarning("[Zimmo] Geen data gevonden op {Url}", canonicalUrl);
            return null;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout"))
        {
            Logger.LogWarning("[Zimmo] Timeout bij detail {Url}", navigationUrl);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[Zimmo] Fout bij detail {Url}", navigationUrl);
            return null;
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    // ── AfterPersistAsync: project-units opslaan (enkel bij OpenDetailPages) ──

    protected override async Task AfterPersistAsync(
        NormalizedPropertyDto normalized,
        long? assetId,
        bool isDryRun,
        CrawlerSource source,
        CancellationToken cancellationToken)
    {
        if (!_pendingProjectUnits.TryGetValue(normalized.ExternalId, out var units)) return;
        _pendingProjectUnits.Remove(normalized.ExternalId);

        if (isDryRun)
        {
            Logger.LogInformation(
                "[Zimmo] [DRYRUN] Project {ExternalId}: {Count} units zou worden opgeslagen.",
                normalized.ExternalId, units.Count);
            await ListingService.UpsertProjectUnitsAsync(
                0, normalized, units, dryRun: true, Settings.MissingListingThreshold, cancellationToken);
            return;
        }

        if (!assetId.HasValue) return;

        var result = await ListingService.UpsertProjectUnitsAsync(
            assetId.Value, normalized, units, dryRun: false, Settings.MissingListingThreshold, cancellationToken);

        Logger.LogInformation(
            "[Zimmo] ProjectGroupSaved | AssetId={AssetId} | ExternalId={Id} | " +
            "Units={Total} (nieuw={New}, bijgewerkt={Upd}) | " +
            "Beschikbaar={Avail} | Verkocht={Sold} | Gereserveerd={Res}",
            assetId.Value, normalized.ExternalId,
            result.UnitsFound, result.UnitsCreated, result.UnitsUpdated,
            result.AvailableUnits, result.SoldUnits, result.ReservedUnits);
    }

    // ── Hulpmethoden ─────────────────────────────────────────────────────────

    private static Task<string?> ExtractNextPageUrlAsync(IPage page) =>
        page.EvaluateAsync<string?>(@"() => {
            const relNext = document.querySelector('a[rel=""next""]');
            if (relNext?.href) return relNext.href;

            const nextWords = ['volgende', 'next'];
            const pagSelectors = [
                '[class*=""pagination"" i]', '[class*=""Pagination""]',
                '[class*=""pager"" i]', 'nav[aria-label*=""paginering"" i]',
                '[role=""navigation""]'
            ];
            for (const sel of pagSelectors) {
                try {
                    for (const c of document.querySelectorAll(sel)) {
                        for (const a of c.querySelectorAll('a[href]')) {
                            const lbl = (a.getAttribute('aria-label') || a.getAttribute('title') || a.textContent || '').trim().toLowerCase();
                            if (nextWords.some(w => lbl.includes(w))) return a.href;
                        }
                    }
                } catch (_) {}
            }
            for (const a of document.querySelectorAll('a[href]')) {
                const aria = (a.getAttribute('aria-label') || '').toLowerCase();
                if (aria.includes('volgende') || aria === 'next') return a.href;
            }
            for (const a of document.querySelectorAll('a[href]')) {
                if (!(a.getAttribute('href') || '').includes('search=')) continue;
                const txt = (a.textContent || '').trim();
                if (txt === '›' || txt === '»' || txt === '>'
                    || nextWords.some(w => txt.toLowerCase().includes(w)))
                    return a.href;
            }
            return null;
        }");

    // Strict challenge detection — niet matchen op brede tekst zoals "Cloudflare"
    // (staat gewoon in footer/scripts van normale Zimmo-pagina's)
    private static bool IsChallengePage(int httpStatus, string title, string body) =>
        httpStatus == 403
        || title.Contains("Even geduld", StringComparison.OrdinalIgnoreCase)
        || title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase)
        || body.Contains("Beveiliging wordt geverifieerd", StringComparison.OrdinalIgnoreCase)
        || body.Contains("Checking your browser", StringComparison.OrdinalIgnoreCase);

    private static bool IsProjectUrl(string url) =>
        url.Contains("/te-koop/project/", StringComparison.OrdinalIgnoreCase)
        || url.Contains("/nl/project/", StringComparison.OrdinalIgnoreCase)
        || url.Contains("/nieuwbouwproject/", StringComparison.OrdinalIgnoreCase);

    private void LogParsedListing(ListingDto dto, string source)
    {
        Logger.LogInformation(
            "[Zimmo] [{Source}] {Id} | {PostalCode} {City} — {Street} {Nr} | " +
            "€{Price} | {Type} | {Area}m² | {Beds}k | lat={Lat} lon={Lon}",
            source,
            dto.ExternalId ?? "?",
            dto.PostalCode ?? "?", dto.City ?? "?",
            dto.Street ?? "?", dto.HouseNumber ?? "?",
            dto.AskingPrice.HasValue ? dto.AskingPrice.Value.ToString("N0") : "?",
            dto.PropertyTypeRaw ?? "?",
            dto.LivingArea?.ToString("N0") ?? "?",
            dto.Bedrooms?.ToString() ?? "?",
            dto.Latitude?.ToString() ?? "?",
            dto.Longitude?.ToString() ?? "?");
    }

    private async Task WriteDetailDebugAsync(
        string url, string html, string? nextDataJson, IPage page, CancellationToken ct)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, Settings.Debug.DebugDirectory, "zimmo-detail");
            Directory.CreateDirectory(dir);
            var safe = string.Join("_", url.Split(Path.GetInvalidFileNameChars())).Replace('/', '_');
            if (safe.Length > 80) safe = safe[^80..];
            await File.WriteAllTextAsync(Path.Combine(dir, $"{safe}.html"), html, ct);
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(dir, $"{safe}.png"), FullPage = false
            });
            if (!string.IsNullOrEmpty(nextDataJson))
                await File.WriteAllTextAsync(Path.Combine(dir, $"{safe}-next-data.json"), nextDataJson, ct);
        }
        catch { }
    }

    private static readonly string[] ConsentSelectors =
    [
        "button[data-testid='uc-accept-all-button']",
        "#didomi-notice-agree-button",
        "#onetrust-accept-btn-handler",
        "button:has-text('Alles accepteren')",
        "button:has-text('Accept all')",
        "button:has-text('Akkoord')",
    ];

    private async Task HandleCookieConsentAsync(IPage page)
    {
        try
        {
            var clicked = await page.EvaluateAsync<bool>(@"() => {
                const root = document.querySelector('#usercentrics-root');
                if (!root?.shadowRoot) return false;
                for (const btn of root.shadowRoot.querySelectorAll('button')) {
                    const t = (btn.innerText || '').toLowerCase();
                    if (t.includes('accept') || t.includes('akkoord') || t.includes('alles')) {
                        btn.click(); return true;
                    }
                }
                return false;
            }");
            if (clicked) { await page.WaitForTimeoutAsync(1000); return; }
            foreach (var sel in ConsentSelectors)
            {
                try
                {
                    var btn = page.Locator(sel).First;
                    if (await btn.IsVisibleAsync())
                    {
                        await btn.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                        await page.WaitForTimeoutAsync(800);
                        return;
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private sealed record LocationContext(
        string PostalCode, string City, int? PlaceId, int MaxPages);
}
