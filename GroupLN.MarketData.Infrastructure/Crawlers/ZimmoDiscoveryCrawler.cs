using System.Collections.Concurrent;
using System.Text.Json;
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
/// Puur discovery-crawler voor Zimmo: laadt zoekpagina's, slaat debug-materiaal op
/// en rapporteert gevonden selectors / API-endpoints / URL-patronen.
/// Geen MarketAssets worden aangemaakt of gewijzigd.
/// </summary>
public class ZimmoDiscoveryCrawler : BaseCrawler
{
    private readonly PlaywrightBrowserService _browser;

    // URL → locatie-context, gevuld in GetSearchPageUrlsAsync
    private readonly Dictionary<string, LocationContext> _urlContext = new(StringComparer.OrdinalIgnoreCase);

    // Bijhouden welke listing-types al detail-gecrawled zijn (max 1 per type per run)
    private readonly HashSet<string> _sampledTypes = new(StringComparer.OrdinalIgnoreCase);

    private static readonly LocationSettings[] DefaultLocations =
    [
        new() { City = "Brugge",        PostalCode = "8000" },
        new() { City = "Sint-Michiels", PostalCode = "8200" },
        new() { City = "Beernem",       PostalCode = "8730" },
    ];

    // API-calls die interessant zijn voor Zimmo-analyse
    private static readonly string[] ApiHints =
        ["api", "graphql", "_next/data", "nuxt", "search", "listing", "property", "result", "real-estate"];

    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions CompactJson = new() { WriteIndented = false };

    public ZimmoDiscoveryCrawler(
        IMarketListingService listingService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        PlaywrightBrowserService browser,
        ILogger<ZimmoDiscoveryCrawler> logger)
        : base(listingService, normalizer, settings, logger)
    {
        _browser = browser;
    }

    public override string SourceName => "ZimmoDiscovery";

    // Discovery sloeg altijd fase 2 over — geen detail-pages
    protected override bool SearchDebugMode => true;

    private SourceSettings GetSourceSettings() =>
        Settings.Sources.TryGetValue("ZimmoDiscovery", out var s) ? s : new SourceSettings();

    private string GetDebugDir() =>
        Path.Combine(AppContext.BaseDirectory, Settings.Debug.DebugDirectory, "zimmo-discovery");

    // ── Fase 1: zoek-URLs genereren ───────────────────────────────────────────

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        _urlContext.Clear();
        _sampledTypes.Clear();

        var src = GetSourceSettings();
        var locations = src.AllowedLocations.Count > 0
            ? (IReadOnlyList<LocationSettings>)src.AllowedLocations
            : DefaultLocations;

        var maxPages = src.MaxSearchPagesPerLocation > 0 ? src.MaxSearchPagesPerLocation : 3;

        Logger.LogInformation(
            "[ZimmoDiscovery] {Count} locatie(s) | MaxPagesPerLocatie={Max} | DebugDir={Dir}",
            locations.Count, maxPages, GetDebugDir());

        var urls = new List<string>();

        foreach (var loc in locations)
        {
            int? placeId = null;
            if (!string.IsNullOrEmpty(loc.PostalCode)
                && ZimmoSearchUrlBuilder.PlaceIdByPostalCode.TryGetValue(loc.PostalCode, out var foundId))
                placeId = foundId;

            if (placeId is null)
                Logger.LogWarning(
                    "[ZimmoDiscovery] Geen placeId voor postcode '{Postcode}' ({City}) — zoek-URL zonder locatiefilter.",
                    loc.PostalCode, loc.City);

            var searchUrl = ZimmoSearchUrlBuilder.Build(placeId);
            var decoded   = ZimmoSearchUrlBuilder.DecodeSearchParam(ExtractRawSearchParam(searchUrl) ?? "");

            Logger.LogInformation(
                "[ZimmoDiscovery] {City} ({Postcode}) placeId={PlaceId} → {Url}",
                loc.City, loc.PostalCode, placeId?.ToString() ?? "–", searchUrl);
            Logger.LogInformation(
                "[ZimmoDiscovery] {City} zoek-JSON: {Json}",
                loc.City, decoded);

            _urlContext[searchUrl] = new LocationContext(loc.PostalCode ?? "", loc.City ?? "", placeId, maxPages);
            urls.Add(searchUrl);
        }

        return Task.FromResult<IEnumerable<string>>(urls);
    }

    // ── Fase 2: listing-URLs verzamelen (per locatie) ─────────────────────────

    protected override async Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
    {
        _urlContext.TryGetValue(searchPageUrl, out var ctx);
        ctx ??= new LocationContext("", "onbekend", null, 3);

        var debugDir = GetDebugDir();
        Directory.CreateDirectory(debugDir);

        // Deduplicatie op ExternalId (laatste padsegment), cross-pagina
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
                Logger.LogWarning(
                    "[ZimmoDiscovery] {City}: paginatie-loop gedetecteerd — gestopt.", ctx.City);
                break;
            }

            pageNum++;

            var (discovered, nextPageUrl) = await DiscoverPageAsync(
                currentPageUrl, ctx, pageNum, debugDir, cancellationToken);

            var newCount = 0;
            foreach (var u in discovered)
            {
                var extId = ExtractExternalId(u.Split('?')[0]);
                if (extId is null || seenExternalIds.Add(extId))
                {
                    allUrls.Add(u);
                    newCount++;
                }
            }

            Logger.LogInformation(
                "[ZimmoDiscovery] {City} p{N}: gevonden={Found} | nieuw={New} | totaal={Total} | volgende={Next}",
                ctx.City, pageNum, discovered.Count, newCount, seenExternalIds.Count,
                nextPageUrl ?? "(geen)");

            if (discovered.Count == 0)
            {
                Logger.LogInformation(
                    "[ZimmoDiscovery] {City} p{N}: geen listings → paginatie stopt.", ctx.City, pageNum);
                break;
            }

            currentPageUrl = nextPageUrl;

            if (currentPageUrl is not null && pageNum < ctx.MaxPages)
                await ApplyRateLimitAsync(cancellationToken);
        }

        Logger.LogInformation(
            "[ZimmoDiscovery] {City} ({Postcode}) klaar — {Count} unieke listing-URLs.",
            ctx.City, ctx.PostalCode, allUrls.Count);

        return allUrls;
    }

    // ── Kern: één pagina openen, alles opslaan ────────────────────────────────

    private async Task<(IReadOnlyList<string> ListingUrls, string? NextPageUrl)> DiscoverPageAsync(
        string pageUrl,
        LocationContext ctx,
        int pageNum,
        string debugDir,
        CancellationToken ct)
    {
        var prefix      = $"zimmo-{SanitizeName(ctx.City)}-p{pageNum}";
        var listingUrls = new List<string>();
        var notes       = new List<string>();
        var apiCaptures = new List<ZimmoApiCapture>();

        // Collectioneer API-responses voordat we de pagina laden
        var pendingResponses = new ConcurrentBag<IResponse>();

        var page = await _browser.NewPageAsync(Settings.UserAgent);

        try
        {
            page.Response += (_, r) =>
            {
                if (IsInterestingApiResponse(r))
                    pendingResponses.Add(r);
            };

            // Pagina laden
            var navResponse = await page.GotoAsync(pageUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout   = Settings.PlaywrightTimeoutMs
            });

            await page.WaitForTimeoutAsync(2500);

            // ── Pagina-staat ophalen ───────────────────────────────────────────
            var httpStatus = navResponse?.Status ?? 0;
            var pageTitle  = await page.TitleAsync();
            var html       = await page.ContentAsync();
            var bodyText   = await page.EvaluateAsync<string>(
                "() => (document.body?.innerText ?? '').slice(0, 50000)");

            // ── Challenge-detectie (specifiek — geen brede Cloudflare-tekst) ──
            bool challengeDetected = IsChallengePage(httpStatus, pageTitle, bodyText);
            bool challengeResolved = !challengeDetected;
            double challengeDurationSeconds = 0;

            if (challengeDetected)
            {
                Logger.LogWarning(
                    "[ZimmoDiscovery] {City} p{N}: CloudflareBlocked (HTTP {Status}, titel='{Title}') — wacht max 60s op property-items...",
                    ctx.City, pageNum, httpStatus, pageTitle);

                for (int sec = 0; sec < 60 && !ct.IsCancellationRequested; sec++)
                {
                    await page.WaitForTimeoutAsync(1_000);
                    challengeDurationSeconds = sec + 1;

                    var itemCount = await page.EvaluateAsync<int>(
                        "() => document.querySelectorAll('.property-item').length");
                    if (itemCount > 0) { challengeResolved = true; break; }

                    var snippet = await page.EvaluateAsync<string>(
                        "() => (document.body?.innerText ?? '').slice(0, 3000)");
                    if (snippet.Contains("resultaten", StringComparison.OrdinalIgnoreCase))
                    { challengeResolved = true; break; }
                }

                // Re-read werkelijke staat na wachten
                pageTitle = await page.TitleAsync();
                html      = await page.ContentAsync();
                bodyText  = await page.EvaluateAsync<string>(
                    "() => (document.body?.innerText ?? '').slice(0, 50000)");

                Logger.LogInformation(
                    "[ZimmoDiscovery] {City} p{N}: ChallengeResolved={Resolved} | ChallengeDurationSeconds={Duration}",
                    ctx.City, pageNum, challengeResolved, challengeDurationSeconds);
            }

            Logger.LogInformation(
                "[ZimmoDiscovery] {City} p{N}: HTTP {Status} | \"{Title}\" | HTML {Bytes} bytes | " +
                "CloudflareDetected={CF} | ChallengeResolved={Resolved}",
                ctx.City, pageNum, httpStatus, pageTitle, html.Length,
                challengeDetected, challengeResolved);

            // Challenge niet opgelost → locatie overslaan, niet als lege pagina behandelen
            if (challengeDetected && !challengeResolved)
            {
                notes.Add($"CloudflareBlocked=true | HTTP {httpStatus} | titel='{pageTitle}'");

                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path     = Path.Combine(debugDir, $"{prefix}-cloudflare.png"),
                    FullPage = false
                });

                var cfSummary = new
                {
                    status            = "CloudflareBlocked",
                    timestamp         = DateTime.UtcNow.ToString("O"),
                    location          = new { ctx.PostalCode, ctx.City, ctx.PlaceId },
                    pageNumber        = pageNum,
                    searchUrl         = pageUrl,
                    httpStatus,
                    pageTitle,
                    challengeDurationSeconds,
                };
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, $"{prefix}-summary.json"),
                    JsonSerializer.Serialize(cfSummary, PrettyJson), ct);

                Logger.LogWarning(
                    "[ZimmoDiscovery] {City} p{N}: CloudflareBlocked — locatie overgeslagen.",
                    ctx.City, pageNum);

                return ([], null);
            }

            // ── Listing-URLs extraheren via .property-item a.property-item_link ────

            var propertyItemCount = await page.EvaluateAsync<int>(
                "() => document.querySelectorAll('.property-item').length");
            notes.Add($".property-item elementen: {propertyItemCount}");

            // getAttribute('href') geeft het relatieve pad terug (/nl/gemeente/...)
            var rawLinkHrefs = await page.EvaluateAsync<string[]>(
                "() => Array.from(document.querySelectorAll('.property-item a.property-item_link[href]'))" +
                ".map(a => a.getAttribute('href') || '').filter(h => h.length > 0)");

            notes.Add($".property-item_link hrefs: {rawLinkHrefs?.Length ?? 0}");

            const string ZimmoBase = "https://www.zimmo.be";
            foreach (var href in rawLinkHrefs ?? [])
            {
                var absolute = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? href
                    : ZimmoBase + href;
                // Bewaar volledige URL (incl. ?search=...) voor navigatie
                listingUrls.Add(absolute);
            }

            // Dedup binnen pagina op canonieke URL (zonder querystring)
            var distinctListings = listingUrls
                .GroupBy(u => u.Split('?')[0], StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var listingDetails = distinctListings
                .Select(u => new
                {
                    NavigationUrl = u,
                    CanonicalUrl  = u.Split('?')[0],
                    Type          = DetectListingType(u),
                    ExternalId    = ExtractExternalId(u.Split('?')[0]),
                })
                .ToList();

            // Log eerste 5 gevonden URLs + ExternalIds
            for (var i = 0; i < Math.Min(5, listingDetails.Count); i++)
            {
                var d = listingDetails[i];
                Logger.LogInformation(
                    "[ZimmoDiscovery]   #{Nr}: {Type} | ExternalId={Id} | {CanonicalUrl}",
                    i + 1, d.Type, d.ExternalId ?? "–", d.CanonicalUrl);
            }

            var typeSummary = listingDetails
                .GroupBy(d => d.Type)
                .Select(g => $"{g.Key}={g.Count()}")
                .ToList();
            notes.Add($"Listings gevonden: {distinctListings.Count}" +
                      (typeSummary.Count > 0 ? $" | {string.Join(", ", typeSummary)}" : ""));

            var totalAnchors = await page.EvaluateAsync<int>(
                "() => document.querySelectorAll('a[href]').length");
            notes.Add($"Totaal anchors op pagina: {totalAnchors}");

            // ── Listing-card selectors testen ─────────────────────────────────

            string[] cardSelectors =
            [
                "[data-cy='property-item']",
                "[data-cy='listing-card']",
                "[data-cy='property-card']",
                "[data-qa='listing']",
                "article.property-item",
                ".property-item",
                "[class*='PropertyCard']",
                "[class*='property-card']",
                "[class*='listing-card']",
                "[data-property-id]",
                "[data-listing-id]",
                "li[class*='property']",
            ];

            foreach (var sel in cardSelectors)
            {
                try
                {
                    var cnt = await page.EvaluateAsync<int>(
                        $"() => document.querySelectorAll({JsonSerializer.Serialize(sel)}).length");
                    if (cnt > 0)
                        notes.Add($"Selector '{sel}': {cnt} elements");
                }
                catch { /* ongeldige selector of script-fout */ }
            }

            // ── Embedded JS-globals detecteren ────────────────────────────────

            var embeddedKeys = await page.EvaluateAsync<string[]>(@"() => {
                const found = [];
                ['__NEXT_DATA__','__NUXT_DATA__','__INITIAL_STATE__','__APP_STATE__','__STORE__',
                 'NUXT_PAGE_DATA','__REDUX_STATE__'].forEach(k => {
                    if (window[k]) found.push(k);
                });
                return found;
            }") ?? [];

            if (embeddedKeys.Length > 0)
                notes.Add($"JS-globals gevonden: {string.Join(", ", embeddedKeys)}");

            // __NEXT_DATA__ opslaan als aanwezig
            if (embeddedKeys.Contains("__NEXT_DATA__"))
            {
                try
                {
                    var nextData = await page.EvaluateAsync<string>(
                        "() => JSON.stringify(window.__NEXT_DATA__)");
                    if (nextData?.Length > 0)
                        await File.WriteAllTextAsync(
                            Path.Combine(debugDir, $"{prefix}-next-data.json"),
                            nextData, ct);
                }
                catch { /* body niet serialiseerbaar */ }
            }

            // ── Volgende pagina-URL extracteren ───────────────────────────────
            // Zimmo encodeert paginatie in de Base64 search-param — nooit zelf &page=N opbouwen.

            var nextPageUrl = await page.EvaluateAsync<string?>(@"() => {
                // 1. HTML-standaard rel=next
                const relNext = document.querySelector('a[rel=""next""]');
                if (relNext?.href) return relNext.href;

                // 2. Paginatie-containers: zoek link met tekst/aria 'volgende' of 'next'
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

                // 3. Willekeurig anker met aria-label 'volgende'/'next'
                for (const a of document.querySelectorAll('a[href]')) {
                    const aria = (a.getAttribute('aria-label') || '').toLowerCase();
                    if (aria.includes('volgende') || aria === 'next') return a.href;
                }

                // 4. Zoek-link (bevat search=) met › / » / > / 'volgende' als tekst
                for (const a of document.querySelectorAll('a[href]')) {
                    if (!(a.getAttribute('href') || '').includes('search=')) continue;
                    const txt = (a.textContent || '').trim();
                    if (txt === '›' || txt === '»' || txt === '>'
                        || nextWords.some(w => txt.toLowerCase().includes(w)))
                        return a.href;
                }

                return null;
            }");

            notes.Add(nextPageUrl is not null
                ? $"Volgende pagina-URL: {nextPageUrl}"
                : "Geen volgende pagina-link gevonden.");

            // Alle paginering-links voor debug
            var paginationLinks = await page.EvaluateAsync<string[]>(@"() => {
                const patterns = ['page', 'volgende', 'next', 'pagina'];
                return Array.from(document.querySelectorAll('a[href]'))
                    .filter(a => patterns.some(p => a.href.toLowerCase().includes(p)
                              || (a.textContent || '').toLowerCase().includes(p)))
                    .map(a => a.href)
                    .slice(0, 20);
            }") ?? [];

            if (paginationLinks.Length > 0)
                notes.Add($"Paginering-links debug: {string.Join(", ", paginationLinks.Take(5))}");

            // ── Network API-responses lezen ───────────────────────────────────

            foreach (var r in pendingResponses)
            {
                try
                {
                    var body = await r.TextAsync();
                    r.Headers.TryGetValue("content-type", out var ctype);
                    apiCaptures.Add(new ZimmoApiCapture(
                        r.Url, r.Request.Method, r.Status,
                        ctype ?? "", body.Length > 4000 ? body[..4000] + "…" : body));
                }
                catch { /* response verlopen */ }
            }

            if (apiCaptures.Count > 0)
                notes.Add($"API-calls onderschept: {apiCaptures.Count}");

            // ── Debug-bestanden wegschrijven ──────────────────────────────────

            await File.WriteAllTextAsync(Path.Combine(debugDir, $"{prefix}.html"),    html,     ct);
            await File.WriteAllTextAsync(Path.Combine(debugDir, $"{prefix}-body.txt"), bodyText, ct);
            Logger.LogInformation("[ZimmoDiscovery] HTML + body opgeslagen → {Prefix}.*", prefix);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path     = Path.Combine(debugDir, $"{prefix}.png"),
                FullPage = false
            });
            Logger.LogInformation("[ZimmoDiscovery] Screenshot → {Prefix}.png", prefix);

            if (apiCaptures.Count > 0)
            {
                var jsonLines = apiCaptures.Select(c => JsonSerializer.Serialize(c, CompactJson));
                await File.WriteAllLinesAsync(
                    Path.Combine(debugDir, $"{prefix}-network.jsonl"), jsonLines, ct);
                Logger.LogInformation(
                    "[ZimmoDiscovery] {Count} API-calls → {Prefix}-network.jsonl", apiCaptures.Count, prefix);
            }

            if (distinctListings.Count > 0)
                await File.WriteAllLinesAsync(
                    Path.Combine(debugDir, $"{prefix}-urls.txt"),
                    listingDetails.Select(d => $"{d.CanonicalUrl}\t{d.NavigationUrl}"), ct);

            // Samenvatting per pagina
            var decodedJson = ZimmoSearchUrlBuilder.DecodeSearchParam(
                ExtractRawSearchParam(pageUrl) ?? "");

            var summary = new
            {
                timestamp         = DateTime.UtcNow.ToString("O"),
                location          = new { ctx.PostalCode, ctx.City, ctx.PlaceId },
                pageNumber        = pageNum,
                searchUrl         = pageUrl,
                decodedSearchJson = string.IsNullOrEmpty(decodedJson) ? null : decodedJson,
                httpStatus,
                pageTitle,
                htmlBytes         = html.Length,
                totalAnchors,
                propertyItemCount,
                listingUrlsFound  = distinctListings.Count,
                listings          = listingDetails,
                nextPageUrl,
                paginationLinks,
                embeddedJsGlobals = embeddedKeys,
                networkApiCalls   = apiCaptures.Select(c => new
                {
                    c.Url, c.Method, c.Status, c.ContentType,
                    bodyPreviewChars = c.BodyPreview.Length
                }),
                detectionNotes    = notes
            };

            await File.WriteAllTextAsync(
                Path.Combine(debugDir, $"{prefix}-summary.json"),
                JsonSerializer.Serialize(summary, PrettyJson), ct);

            Logger.LogInformation(
                "[ZimmoDiscovery] {City} p{N} samenvatting: {Listings} listings | {Api} API-calls | {Notes} notities",
                ctx.City, pageNum, distinctListings.Count, apiCaptures.Count, notes.Count);

            foreach (var n in notes)
                Logger.LogInformation("[ZimmoDiscovery]   · {Note}", n);

            return (distinctListings, nextPageUrl);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    // FetchAndParseListingAsync wordt nooit aangeroepen (SearchDebugMode = true)
    protected override Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken)
        => Task.FromResult<ListingDto?>(null);

    // ── Hulpmethoden ─────────────────────────────────────────────────────────

    private static string DetectListingType(string url)
    {
        if (url.Contains("/project/", StringComparison.OrdinalIgnoreCase)
            || url.Contains("/nieuwbouwproject/", StringComparison.OrdinalIgnoreCase))
            return "project";
        if (url.Contains("/te-koop/huis/", StringComparison.OrdinalIgnoreCase))
            return "huis";
        if (url.Contains("/te-koop/appartement/", StringComparison.OrdinalIgnoreCase))
            return "appartement";
        if (url.Contains("/te-koop/", StringComparison.OrdinalIgnoreCase))
            return "te-koop";
        if (url.Contains("/te-huur/", StringComparison.OrdinalIgnoreCase))
            return "te-huur";
        return "onbekend";
    }

    private static string? ExtractExternalId(string absoluteUrl)
    {
        // /nl/loker-8958/te-koop/huis/LOU1X/?search=... → LOU1X
        var withoutQuery = absoluteUrl.Split('?')[0].TrimEnd('/');
        var lastSlash    = withoutQuery.LastIndexOf('/');
        if (lastSlash < 0) return null;
        var segment = withoutQuery[(lastSlash + 1)..];
        return string.IsNullOrEmpty(segment) ? null : segment;
    }

    private static bool IsInterestingApiResponse(IResponse response)
    {
        var url = response.Url;
        if (!url.Contains("zimmo.be", StringComparison.OrdinalIgnoreCase)) return false;

        response.Headers.TryGetValue("content-type", out var ct);
        var isJsonOrJs = ct is not null
            && (ct.Contains("json", StringComparison.OrdinalIgnoreCase)
                || ct.Contains("javascript", StringComparison.OrdinalIgnoreCase));

        if (!isJsonOrJs) return false;

        var lowerUrl = url.ToLowerInvariant();
        return ApiHints.Any(hint => lowerUrl.Contains(hint));
    }

    private static string? ExtractRawSearchParam(string url)
    {
        var start = url.IndexOf("?search=", StringComparison.Ordinal);
        if (start < 0) return null;
        start += "?search=".Length;
        var end = url.IndexOfAny(['&', '#'], start);
        return end >= 0 ? url[start..end] : url[start..];
    }

    private static string SanitizeName(string input)
    {
        if (string.IsNullOrEmpty(input)) return "onbekend";
        return new string(input.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray()).Trim('-');
    }

    // ── Detail-discovery: max 1 voorbeeld per listing-type ───────────────────

    private async Task DiscoverDetailExamplesAsync(
        IReadOnlyList<string> listingUrls, string debugDir, CancellationToken ct)
    {
        if (_sampledTypes.Count >= 3) return;

        var detailDir = Path.Combine(debugDir, "details");
        Directory.CreateDirectory(detailDir);

        foreach (var url in listingUrls)
        {
            if (ct.IsCancellationRequested || _sampledTypes.Count >= 3) break;

            var type = DetectListingType(url);
            if (type is not ("appartement" or "huis" or "project")) continue;
            if (!_sampledTypes.Add(type)) continue;

            Logger.LogInformation(
                "[ZimmoDiscovery] Detail-discovery: type={Type} → {Url}", type, url);

            await CrawlDetailPageAsync(url, type, detailDir, ct);

            if (_sampledTypes.Count < 3)
                await ApplyRateLimitAsync(ct);
        }

        Logger.LogInformation(
            "[ZimmoDiscovery] Detail-discovery klaar — {Count}/3 type(s) gesampled: {Types}",
            _sampledTypes.Count, string.Join(", ", _sampledTypes));
    }

    private async Task CrawlDetailPageAsync(
        string url, string type, string detailDir, CancellationToken ct)
    {
        // url = volledige NavigationUrl (incl. ?search=...) — canonical = zonder querystring
        var canonicalUrl    = url.Split('?')[0];
        var externalId      = ExtractExternalId(canonicalUrl) ?? "unknown";
        var prefix          = $"detail-{type}-{externalId}";
        var apiCaptures     = new List<ZimmoApiCapture>();
        var pendingResponses = new ConcurrentBag<IResponse>();

        var page = await _browser.NewPageAsync(Settings.UserAgent);
        try
        {
            page.Response += (_, r) =>
            {
                if (IsInterestingApiResponse(r)) pendingResponses.Add(r);
            };

            // Navigeer met volledige URL (simuleert klik vanuit zoekresultaten)
            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout   = Settings.PlaywrightTimeoutMs
            });
            await page.WaitForTimeoutAsync(2500);

            await HandleDetailCookieConsentAsync(page);

            // ── Controleer of detail-pagina een challenge toont ───────────────
            {
                var dcTitle = await page.TitleAsync();
                var dcBody  = await page.EvaluateAsync<string>(
                    "() => (document.body?.innerText ?? '').slice(0, 5000)");
                var detailStillChallenge = IsChallengePage(0, dcTitle, dcBody);

                Logger.LogInformation(
                    "[ZimmoDiscovery] Detail [{Type}] detailPageStillChallenge={Still}",
                    type, detailStillChallenge);

                if (detailStillChallenge)
                {
                    Logger.LogWarning(
                        "[ZimmoDiscovery] Detail [{Type}] toont nog Cloudflare — wacht max 30s...", type);

                    try
                    {
                        await page.WaitForFunctionAsync(
                            "() => document.readyState === 'complete'",
                            options: new PageWaitForFunctionOptions { Timeout = 30_000 });
                    }
                    catch (PlaywrightException) { }

                    await page.WaitForTimeoutAsync(5_000);

                    await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path     = Path.Combine(detailDir, $"{prefix}-after-security.png"),
                        FullPage = false
                    });

                    Logger.LogInformation(
                        "[ZimmoDiscovery] Detail [{Type}] na wacht: titel='{Title}' | URL={Url}",
                        type, await page.TitleAsync(), page.Url);
                }
            }

            var html      = await page.ContentAsync();
            var bodyText  = await page.EvaluateAsync<string>(
                "() => (document.body?.innerText ?? '').slice(0, 100000)");
            var pageTitle = await page.TitleAsync();

            // ── __NEXT_DATA__ ─────────────────────────────────────────────────
            var nextDataJson = await page.EvaluateAsync<string?>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); " +
                "return el ? el.textContent : null; }");

            // ── JSON-LD scripts ───────────────────────────────────────────────
            var jsonLdScripts = await page.EvaluateAsync<string[]>(@"() =>
                Array.from(document.querySelectorAll('script[type=""application/ld+json""]'))
                    .map(s => s.textContent || '').filter(t => t.trim().length > 0)") ?? [];

            // ── Overige script-tags met JSON-inhoud ───────────────────────────
            var jsonScripts = await page.EvaluateAsync<string[]>(@"() =>
                Array.from(document.querySelectorAll('script:not([src])'))
                    .map(s => (s.textContent || '').trim())
                    .filter(t => (t.startsWith('{') || t.startsWith('[')) && t.length > 20)
                    .slice(0, 20)") ?? [];

            // ── Network responses lezen ───────────────────────────────────────
            foreach (var r in pendingResponses)
            {
                try
                {
                    var body = await r.TextAsync();
                    r.Headers.TryGetValue("content-type", out var ctype);
                    apiCaptures.Add(new ZimmoApiCapture(
                        r.Url, r.Request.Method, r.Status, ctype ?? "",
                        body.Length > 4000 ? body[..4000] + "…" : body));
                }
                catch { }
            }

            // ── Parsen via bestaande parsers (canonical URL voor opslag) ─────
            ListingDto? parsedDto = null;
            if (!string.IsNullOrEmpty(nextDataJson))
            {
                if (type == "project")
                    parsedDto = ZimmoProjectParser.TryParseNextData(nextDataJson, canonicalUrl, Logger);
                parsedDto ??= ZimmoListingParser.TryParseNextData(nextDataJson, canonicalUrl, Logger);
            }
            if (parsedDto is null)
                parsedDto = ZimmoListingParser.TryParseJsonLd(html, canonicalUrl, Logger);

            if (parsedDto is not null)
                parsedDto.NavigationUrl = url;

            // ── Project-units ─────────────────────────────────────────────────
            List<ProjectGroupUnitDto> units = [];
            if (type == "project" && !string.IsNullOrEmpty(nextDataJson))
                units = ZimmoProjectParser.ParseUnitsFromJson(
                    nextDataJson,
                    parsedDto?.ExternalId ?? externalId,
                    parsedDto?.ProjectName,
                    Logger);

            // ── Log analyse-resultaat ─────────────────────────────────────────
            LogDetailAnalysis(canonicalUrl, type, parsedDto, pageTitle, units);

            // ── Bestanden opslaan ─────────────────────────────────────────────
            await File.WriteAllTextAsync(Path.Combine(detailDir, $"{prefix}.html"), html, ct);
            await File.WriteAllTextAsync(Path.Combine(detailDir, $"{prefix}-body.txt"), bodyText ?? "", ct);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(detailDir, $"{prefix}.png"), FullPage = false
            });

            if (!string.IsNullOrEmpty(nextDataJson))
                await File.WriteAllTextAsync(
                    Path.Combine(detailDir, $"{prefix}-next-data.json"), nextDataJson, ct);

            if (jsonLdScripts.Length > 0)
                await File.WriteAllTextAsync(
                    Path.Combine(detailDir, $"{prefix}-json-ld.json"),
                    JsonSerializer.Serialize(jsonLdScripts, PrettyJson), ct);

            if (jsonScripts.Length > 0)
                await File.WriteAllTextAsync(
                    Path.Combine(detailDir, $"{prefix}-scripts-json.json"),
                    JsonSerializer.Serialize(jsonScripts, PrettyJson), ct);

            if (apiCaptures.Count > 0)
            {
                var lines = apiCaptures.Select(c => JsonSerializer.Serialize(c, CompactJson));
                await File.WriteAllLinesAsync(
                    Path.Combine(detailDir, $"{prefix}-network.jsonl"), lines, ct);
            }

            // ── summary.json ──────────────────────────────────────────────────
            var summary = new
            {
                timestamp    = DateTime.UtcNow.ToString("O"),
                navigationUrl = url,
                canonicalUrl,
                type,
                externalId,
                pageTitle,
                htmlBytes       = html.Length,
                hasNextData     = !string.IsNullOrEmpty(nextDataJson),
                jsonLdCount     = jsonLdScripts.Length,
                jsonScriptCount = jsonScripts.Length,
                networkApiCalls = apiCaptures.Count,
                parsed          = parsedDto is null ? null : new
                {
                    externalId   = parsedDto.ExternalId,
                    title        = parsedDto.Title,
                    price        = parsedDto.AskingPrice,
                    postalCode   = parsedDto.PostalCode,
                    city         = parsedDto.City,
                    street       = parsedDto.Street,
                    houseNumber  = parsedDto.HouseNumber,
                    latitude     = parsedDto.Latitude,
                    longitude    = parsedDto.Longitude,
                    livingArea   = parsedDto.LivingArea,
                    landArea     = parsedDto.LandArea,
                    bedrooms     = parsedDto.Bedrooms,
                    epcLabel     = parsedDto.EPCLabelRaw,
                    projectName  = parsedDto.ProjectName,
                    isNewBuild   = parsedDto.IsNewBuild,
                    propertyType = parsedDto.PropertyTypeRaw,
                },
                units = units.Take(10).Select(u => new
                {
                    u.UnitId,
                    status  = u.SaleStatus.ToString(),
                    price   = u.Price,
                    surface = u.Surface,
                    beds    = u.BedroomCount,
                    floor   = u.Floor,
                }).ToArray()
            };

            await File.WriteAllTextAsync(
                Path.Combine(detailDir, $"{prefix}-summary.json"),
                JsonSerializer.Serialize(summary, PrettyJson), ct);

            Logger.LogInformation(
                "[ZimmoDiscovery] Detail opgeslagen → {Dir}/{Prefix}.*",
                Path.GetFileName(detailDir), prefix);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[ZimmoDiscovery] Detail-discovery fout bij {Url}", url);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static readonly string[] DetailConsentSelectors =
    [
        "button[data-testid='uc-accept-all-button']",
        "#didomi-notice-agree-button",
        "#onetrust-accept-btn-handler",
        "button:has-text('Alles accepteren')",
        "button:has-text('Accept all')",
        "button:has-text('Akkoord')",
    ];

    private static async Task HandleDetailCookieConsentAsync(IPage page)
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

            foreach (var sel in DetailConsentSelectors)
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

    private void LogDetailAnalysis(
        string url, string type, ListingDto? dto, string pageTitle,
        List<ProjectGroupUnitDto> units)
    {
        if (dto is null)
        {
            Logger.LogWarning(
                "[ZimmoDiscovery] Detail [{Type}] geen data geparsed | title=\"{Title}\" | {Url}",
                type, pageTitle, url);
            return;
        }

        Logger.LogInformation(
            "[ZimmoDiscovery] Detail [{Type}]\n" +
            "  title      : {Title}\n" +
            "  price      : €{Price}\n" +
            "  address    : {Street} {Nr}, {PostalCode} {City}\n" +
            "  coords     : lat={Lat} lon={Lon}\n" +
            "  livingArea : {Living} m²\n" +
            "  landArea   : {Land} m²\n" +
            "  bedrooms   : {Beds}\n" +
            "  projectName: {Project}",
            type,
            dto.Title ?? pageTitle,
            dto.AskingPrice?.ToString("N0") ?? "?",
            dto.Street ?? "?", dto.HouseNumber ?? "", dto.PostalCode ?? "?", dto.City ?? "?",
            dto.Latitude?.ToString() ?? "?", dto.Longitude?.ToString() ?? "?",
            dto.LivingArea?.ToString("N0") ?? "?",
            dto.LandArea?.ToString("N0") ?? "?",
            dto.Bedrooms?.ToString() ?? "?",
            dto.ProjectName ?? "(geen)");

        if (units.Count > 0)
        {
            var stats = units
                .GroupBy(u => u.SaleStatus.ToString())
                .Select(g => $"{g.Key}={g.Count()}");
            Logger.LogInformation(
                "[ZimmoDiscovery] Detail [{Type}] units: totaal={Total} | {Stats}",
                type, units.Count, string.Join(" | ", stats));

            foreach (var u in units.Take(5))
                Logger.LogInformation(
                    "[ZimmoDiscovery]   Unit {Id} | status={Status} | €{Price} | {Surface}m²",
                    u.UnitId, u.SaleStatus,
                    u.Price?.ToString("N0") ?? "?",
                    u.Surface?.ToString("N0") ?? "?");
        }
    }

    // ── Challenge-detectie ────────────────────────────────────────────────────

    // Detecteert alleen concrete challenge-signalen. Matcht NIET op brede tekst
    // zoals "Cloudflare" — dat staat in footer/scripts van gewone Zimmo-pagina's.
    private static bool IsChallengePage(int httpStatus, string title, string body) =>
        httpStatus == 403
        || title.Contains("Even geduld", StringComparison.OrdinalIgnoreCase)
        || title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase)
        || body.Contains("Beveiliging wordt geverifieerd", StringComparison.OrdinalIgnoreCase)
        || body.Contains("Checking your browser", StringComparison.OrdinalIgnoreCase);

    // ── Interne types ─────────────────────────────────────────────────────────

    private sealed record LocationContext(
        string PostalCode,
        string City,
        int?   PlaceId,
        int    MaxPages);

    private sealed record ZimmoApiCapture(
        string Url,
        string Method,
        int    Status,
        string ContentType,
        string BodyPreview);
}
