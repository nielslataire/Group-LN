using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Browser;
using GroupLN.MarketData.Infrastructure.Crawlers.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

public partial class ImmowebCrawler : BaseCrawler
{
    private readonly PlaywrightBrowserService _browser;

    // State over de volledige crawl-sessie — reset in GetSearchPageUrlsAsync (start fase 1)
    private int? _detectedResultCount;
    private int? _estimatedMaxPages;
    private readonly List<PageStat> _pageStats = [];

    // Units per project, gevuld door HandleProjectGroupAsync, geconsumeerd door AfterPersistAsync
    private readonly Dictionary<string, List<ProjectGroupUnitDto>> _pendingProjectUnits = new();

    // Field availability summary — accumulatie over de volledige crawl-sessie
    private readonly Dictionary<string, (int Found, int Missing)> _fieldSummary = new(StringComparer.Ordinal);

    // Listing-URL's afkomstig van een nieuwbouwzoekopdracht (isNewlyBuilt=true in zoek-URL)
    private readonly ConcurrentDictionary<string, bool> _newBuildSearchListings =
        new(StringComparer.OrdinalIgnoreCase);

    private record PageStat(int PageNum, string Location, int Href, int Api, int Sponsor, int Unique);

    private sealed record SearchUrlContext(
        IReadOnlySet<string> AllowedPostalCodes,
        bool IsNewBuildSearch);

    // Geldig: /nl/zoekertje/TYPE/te-koop/GEMEENTE/POSTCODE/ID
    //         /fr/annonce/...
    //         /en/classified/...
    // Variant A: volledig pad met 6+ cijferig ID als laatste numeriek segment
    private static readonly Regex ClassifiedUrlPattern =
        new(@"/(classified|zoekertje|annonce)/[^/?#]+/[^/?#]+/[^/?#]+/\d{4,}/(\d{6,})([?#].*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Variant B: /classified/ID (korte vorm zonder gemeente/type)
    private static readonly Regex ClassifiedIdOnlyPattern =
        new(@"/(classified|zoekertje|annonce)/(\d{6,})([?#].*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Classified ID uit detail-URL extraheren (laatste numeriek segment vóór ? of einde)
    private static readonly Regex UrlIdPattern =
        new(@"/(\d{6,})(?:[?#]|$)", RegexOptions.Compiled);

    // Postcode extractie uit listing-URL: /GEMEENTE/POSTCODE/ID  (bijv. /brugge/8000/12345678)
    private static readonly Regex PostalCodeInListingUrl =
        new(@"/([1-9]\d{3})/\d{6,}", RegexOptions.Compiled);

    // Regex voor HTML-tekst scan (fase 4 fallback)
    private static readonly Regex HtmlClassifiedUrlPattern =
        new(@"(?:https?://www\.immoweb\.be)?/(nl/zoekertje|fr/annonce|en/classified)/[^""'<>\s]+/(\d{6,})[^""'<>\s]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // URL-fragmenten die duiden op een API-call met zoekresultaten
    private static readonly string[] ApiUrlKeywords =
    [
        "search", "classified", "classifieds", "property", "properties",
        "listing", "listings", "result", "results",
        "graphql", "real-estate", "realestate", "/api/", "_next/data"
    ];

    private static readonly string[] BlockIndicators =
    [
        "cookie consent", "captcha", "access denied", "bot protection",
        "javascript required", "blocked", "cloudflare", "just a moment",
        "verifying you are human", "enable javascript"
    ];

    public ImmowebCrawler(
        IMarketListingService listingService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        PlaywrightBrowserService browser,
        ILogger<ImmowebCrawler> logger)
        : base(listingService, normalizer, settings, logger)
    {
        _browser = browser;
    }

    public override string SourceName => "Immoweb";

    // ── Helpers ────────────────────────────────────────────────────────────────

    private SourceSettings GetSourceSettings() =>
        Settings.Sources.TryGetValue("Immoweb", out var s) ? s : new SourceSettings();

    private string GetDebugDir() =>
        Path.Combine(AppContext.BaseDirectory, Settings.Debug.DebugDirectory);

    protected override bool SearchDebugMode => GetSourceSettings().SearchDebugMode;

    protected override IReadOnlyList<string> GetManualTestUrls() =>
        GetSourceSettings().ManualTestListingUrls is { Count: > 0 } src
            ? src
            : base.GetManualTestUrls();

    protected override bool IsAllowed(ListingDto listing)
    {
        var locations = GetSourceSettings().AllowedLocations;
        if (locations.Count == 0) return true;

        var postalCodes = locations
            .Where(l => !string.IsNullOrEmpty(l.PostalCode))
            .Select(l => l.PostalCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return postalCodes.Count == 0
            || (!string.IsNullOrEmpty(listing.PostalCode)
                && postalCodes.Contains(listing.PostalCode.Trim()));
    }

    // ── Zoekpagina-URL's genereren ─────────────────────────────────────────────
    // URL-templates staan in appsettings — geen hardcoded URLs.
    // Placeholders: {city}, {citySlug}, {postalCode}, {page}, {transactionType}, {propertyType}.
    // Templates met locatie-placeholders worden per AllowedLocations-entry uitgebreid.
    // Templates zonder locatie-placeholders worden globaal uitgevoerd (alleen {page}).

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        // Per-crawl state resetten
        _detectedResultCount = null;
        _estimatedMaxPages = null;
        _pageStats.Clear();
        _pendingProjectUnits.Clear();
        _newBuildSearchListings.Clear();

        // Verwijder afgelopen crawl's rejected-urls bestand (herstart per crawl)
        try
        {
            var rejectedPath = Path.Combine(AppContext.BaseDirectory, "debug", "search",
                "immoweb-rejected-listing-urls.txt");
            if (File.Exists(rejectedPath)) File.Delete(rejectedPath);
        }
        catch { /* niet-kritiek */ }

        var src = GetSourceSettings();
        var debug = Settings.Debug;
        var maxPages = src.SearchDebugMode
            ? debug.MaxPagesInSearchDebugMode
            : src.MaxSearchPagesPerLocation;

        Logger.LogInformation(
            "[Immoweb] ══ Instellingen ══ Enabled={En} | SearchDebugMode={Dbg} | MaxPages={MaxPg} | " +
            "SearchUrls={UrlCount} | AllowedLocations={LocCount} | DryRun={Dry} | MaxListingsPerRun={MaxL} | DebugDir={Dir}",
            src.Enabled, src.SearchDebugMode, maxPages,
            src.SearchUrls.Count, src.AllowedLocations.Count,
            Settings.DryRun,
            Settings.MaxListingsPerRun == 0 ? "onbeperkt" : Settings.MaxListingsPerRun.ToString(),
            debug.DebugDirectory);

        if (src.SearchDebugMode)
            Logger.LogWarning("[Immoweb] SearchDebugMode actief — max {MaxPages} pagina's per locatie, geen detailpagina's.", maxPages);

        if (src.SearchUrls.Count == 0)
        {
            Logger.LogWarning("[Immoweb] Sources.Immoweb.SearchUrls is leeg — voeg templates toe aan appsettings.");
            return Task.FromResult<IEnumerable<string>>([]);
        }

        var urls = new List<string>();

        foreach (var template in src.SearchUrls)
        {
            var needsLocation =
                template.Contains("{city}", StringComparison.OrdinalIgnoreCase) ||
                template.Contains("{citySlug}", StringComparison.OrdinalIgnoreCase) ||
                template.Contains("{postalCode}", StringComparison.OrdinalIgnoreCase);

            if (needsLocation)
            {
                if (src.AllowedLocations.Count == 0)
                {
                    Logger.LogWarning(
                        "[Immoweb] Template bevat locatie-placeholders maar AllowedLocations is leeg — template overgeslagen: {Template}",
                        template);
                    continue;
                }

                foreach (var loc in src.AllowedLocations)
                {
                    var slug = loc.CitySlug;
                    if (string.IsNullOrWhiteSpace(slug))
                    {
                        slug = loc.City.ToLowerInvariant().Replace(' ', '-');
                        Logger.LogWarning(
                            "[Immoweb] CitySlug ontbreekt voor '{City}' — fallback slugify: '{Slug}'. " +
                            "Stel CitySlug expliciet in voor correcte Immoweb-URL's.",
                            loc.City, slug);
                    }

                    for (var p = 1; p <= maxPages; p++)
                        urls.Add(ExpandUrl(template, loc, slug, p));
                }
            }
            else
            {
                Logger.LogInformation("[Immoweb] Globale template (geen locatie-placeholders): {Template}", template);
                for (var p = 1; p <= maxPages; p++)
                    urls.Add(ExpandUrl(template, null, null, p));
            }
        }

        Logger.LogInformation("[Immoweb] {Count} zoek-URL(s) gegenereerd:", urls.Count);
        foreach (var u in urls)
            Logger.LogInformation("[Immoweb]   Zoek-URL: {Url}", u);

        return Task.FromResult<IEnumerable<string>>(urls);
    }

    private static string ExpandUrl(string template, LocationSettings? loc, string? slug, int page) =>
        template
            .Replace("{city}", loc?.City ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{citySlug}", slug ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{postalCode}", loc?.PostalCode ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{page}", page.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{transactionType}", "te-koop", StringComparison.OrdinalIgnoreCase)
            .Replace("{propertyType}", "huis-en-appartement", StringComparison.OrdinalIgnoreCase);

    // ── Listing-URL's ophalen van één zoekpagina ───────────────────────────────

    protected override async Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
    {
        IPage? page = null;

        // ── Paginering: overgeslagen pagina's voorbij schatting ───────────────
        {
            var pageMatch = Regex.Match(searchPageUrl, @"[?&]page=(\d+)", RegexOptions.IgnoreCase);
            var currentPage = pageMatch.Success ? int.Parse(pageMatch.Groups[1].Value) : 1;

            if (currentPage == 1)
                _estimatedMaxPages = null; // Reset voor nieuwe locatie/template

            var src2 = GetSourceSettings();
            if (currentPage > 1 && _estimatedMaxPages.HasValue && !src2.ForceMaxSearchPages
                && currentPage > _estimatedMaxPages.Value)
            {
                Logger.LogInformation(
                    "[Immoweb] Pagina {Page} overgeslagen — voorbij EstimatedPages={Est} (ForceMaxSearchPages=false).",
                    currentPage, _estimatedMaxPages.Value);
                return Enumerable.Empty<string>();
            }
        }

        try
        {
            page = await _browser.NewPageAsync(Settings.UserAgent);

            // ── Fase 2: Netwerkonderschepping VÓÓR navigatie ──────────────────
            var captured = new ConcurrentBag<CapturedResponse>();
            var allRequests = new ConcurrentBag<string>();

            page.Request += (_, request) =>
            {
                try { allRequests.Add($"{request.Method} [{request.ResourceType}] {request.Url}"); }
                catch { /* async void */ }
            };

            page.Response += async (_, response) =>
            {
                try
                {
                    var ct = response.Headers.GetValueOrDefault("content-type", "");
                    if (!ct.Contains("json", StringComparison.OrdinalIgnoreCase)) return;

                    var url = response.Url;
                    if (!IsInterestingApiUrl(url)) return;

                    string body;
                    try { body = await response.TextAsync(); }
                    catch { return; }

                    captured.Add(new CapturedResponse(
                        url,
                        response.Request.Method,
                        response.Status,
                        ct,
                        body));
                }
                catch { /* async void handler — nooit laten crashen */ }
            };

            Logger.LogInformation("[Immoweb] Navigeren naar: {Url}", searchPageUrl);

            await page.GotoAsync(searchPageUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = Settings.PlaywrightTimeoutMs
            });

            await page.WaitForTimeoutAsync(3000);

            // ── Screenshot VOOR consent ────────────────────────────────────────
            await TakeDebugScreenshotAsync(page, "search-before-consent.png", cancellationToken);

            // ── Paginadiagnose VOOR cookie consent ────────────────────────────
            var anchorCountBefore = await page.EvaluateAsync<int>("() => document.querySelectorAll('a').length");
            var scriptCountBefore = await page.EvaluateAsync<int>("() => document.querySelectorAll('script').length");
            var bodyTextBefore = await page.EvaluateAsync<string>("() => document.body?.innerText ?? ''");

            Logger.LogInformation(
                "[Immoweb] Pagina vóór consent → URL: {Url} | Titel: '{Title}' | BodyLength: {Len} | <a>: {A} | <script>: {S}",
                page.Url, await page.TitleAsync(), bodyTextBefore?.Length ?? 0, anchorCountBefore, scriptCountBefore);

            // ── Cookie consent afhandelen ─────────────────────────────────────
            await HandleCookieConsentAsync(page, anchorCountBefore, scriptCountBefore,
                bodyTextBefore?.Length ?? 0, cancellationToken);

            // ── Paginadiagnose NA cookie consent ──────────────────────────────
            var actualUrl = page.Url;
            var title = await page.TitleAsync();
            var anchorCount = await page.EvaluateAsync<int>("() => document.querySelectorAll('a').length");
            var scriptCount = await page.EvaluateAsync<int>("() => document.querySelectorAll('script').length");
            var htmlContent = await page.ContentAsync();
            var bodyText = await page.EvaluateAsync<string>("() => document.body?.innerText ?? ''");

            // ── Resultaatcount uit pagina extraheren ──────────────────────────
            var resultCount = await TryExtractResultCountAsync(page, bodyText ?? string.Empty);

            Logger.LogInformation(
                "[Immoweb] Pagina geladen → URL: {Url} | Titel: '{Title}' | ResultCount: {ResultCount} | <a>: {Anchors} | <script>: {Scripts} | Netwerkverz.: {ReqCount} | JSON-resp.: {ApiCount} | Body-tekst: {TextLen} tekens",
                actualUrl, title,
                resultCount.HasValue ? resultCount.Value.ToString() : "onbekend",
                anchorCount, scriptCount, allRequests.Count, captured.Count,
                bodyText?.Length ?? 0);

            if (resultCount.HasValue && _detectedResultCount == null)
            {
                // Eerste pagina: sla op en bereken schattingen
                _detectedResultCount = resultCount;
            }

            if (_detectedResultCount.HasValue)
            {
                // Gebruik werkelijk gevonden listings op pagina 1 als basis voor schatting
                // (pas beschikbaar na href-scan, wordt bijgewerkt in paginering-samenvatting)
                var resultsPerPage = 38; // Beernem-pagina gaf 39 hrefs; gebruik 38 als conservatieve schatting
                var estimatedPages = (int)Math.Ceiling(_detectedResultCount.Value / (double)resultsPerPage);
                Logger.LogInformation(
                    "[Immoweb] ResultCount={Count} | ResultsPerPage=~{PerPage} | EstimatedPages={Pages}",
                    _detectedResultCount.Value, resultsPerPage, estimatedPages);
            }

            // ── Blokkade-detectie ──────────────────────────────────────────────
            var contentLower = htmlContent.ToLowerInvariant();
            foreach (var indicator in BlockIndicators)
                if (contentLower.Contains(indicator))
                    Logger.LogWarning("[Immoweb] ⚠ Mogelijke blokkade gedetecteerd: '{Indicator}'", indicator);

            var apiEndpointLines = new List<string>();

            // ════════════════════════════════════════════════════════════════════
            // BRON 1 (primair): Href-scan — altijd uitvoeren, niet enkel als fallback
            // ════════════════════════════════════════════════════════════════════
            var (hrefListingUrls, rawHrefCount) = await ScanHrefsAsync(page);
            Logger.LogInformation("[Immoweb] Bron 1 (hrefs): {Candidates} listing-kandidaten uit {Raw} rauwe hrefs.",
                hrefListingUrls.Count, rawHrefCount);

            if (hrefListingUrls.Count > 0)
            {
                var sample = hrefListingUrls.Take(10).ToList();
                Logger.LogInformation("[Immoweb] Eerste {N} href-URL's:", sample.Count);
                foreach (var u in sample)
                    Logger.LogInformation("[Immoweb]   {Url}", u);
            }

            // ════════════════════════════════════════════════════════════════════
            // BRON 2 (aanvullend): API/JSON-responses — sponsor apart behandelen
            // ════════════════════════════════════════════════════════════════════
            var apiListingUrls = new List<string>();
            var sponsorListingUrls = new List<string>();

            foreach (var resp in captured.OrderBy(r => r.Url))
            {
                var isSponsored = resp.Url.Contains("/sponsor", StringComparison.OrdinalIgnoreCase)
                               || resp.Url.Contains("sponsor?", StringComparison.OrdinalIgnoreCase)
                               || resp.Url.Contains("sponsored", StringComparison.OrdinalIgnoreCase);

                Logger.LogInformation(
                    "[Immoweb] [NETWERK]{Sponsor} {Method} {Status} {ContentType,-30} {Url}",
                    isSponsored ? "[SPONSOR]" : "", resp.Method, resp.Status,
                    resp.ContentType.Split(';')[0].Trim(), resp.Url);

                var ids = TryExtractClassifiedIds(resp.Body).Distinct().ToList();
                var urlsFromJson = ExtractListingUrlsFromJson(resp.Body)
                    .Where(IsListingHref).Distinct().ToList();

                var logLine = $"{(isSponsored ? "[SPONSOR] " : "")}{resp.Method} {resp.Status} | ids={ids.Count,4} | urls={urlsFromJson.Count,4} | {resp.Url}";
                apiEndpointLines.Add(logLine);

                var targetList = isSponsored ? sponsorListingUrls : apiListingUrls;

                foreach (var id in ids)
                {
                    var u = $"https://www.immoweb.be/nl/zoekertje/{id}";
                    if (!targetList.Contains(u)) targetList.Add(u);
                }
                foreach (var u in urlsFromJson)
                {
                    if (!targetList.Contains(u)) targetList.Add(u);
                }
            }

            Logger.LogInformation(
                "[Immoweb] Bron 2 (API): {ApiCount} niet-sponsor URL's | {SponsorCount} sponsor URL's. ({Total} JSON-responses)",
                apiListingUrls.Count, sponsorListingUrls.Count, captured.Count);

            // ════════════════════════════════════════════════════════════════════
            // BRON 3 (fallback): HTML regex-scan — alleen als hrefs én API leeg zijn
            // ════════════════════════════════════════════════════════════════════
            var regexListingUrls = new List<string>();
            if (hrefListingUrls.Count == 0 && apiListingUrls.Count == 0)
            {
                Logger.LogWarning("[Immoweb] Hrefs + API leeg — fallback naar HTML regex-scan.");
                regexListingUrls.AddRange(ScanHtmlForClassifiedUrls(htmlContent));
                Logger.LogInformation("[Immoweb] Bron 3 (regex): {Count} kandidaat-URL's gevonden.", regexListingUrls.Count);
            }

            // ════════════════════════════════════════════════════════════════════
            // SAMENVOEGEN: hrefs eerst, dan API, dan sponsor, dan regex
            // Dedupliceren op ExternalId (numeriek segment in URL)
            // ════════════════════════════════════════════════════════════════════
            List<string> listingUrls = MergeAndDeduplicate(hrefListingUrls, apiListingUrls, sponsorListingUrls, regexListingUrls);
            var candidateCount = listingUrls.Count;

            // ════════════════════════════════════════════════════════════════════
            // POSTCODE-FILTER: verwijder listings buiten de gezochte locatie(s)
            // Filtert zowel hrefs als API/sponsor URLs — pakt "gelijkaardige panden"
            // ════════════════════════════════════════════════════════════════════
            var searchContext = BuildSearchContext(searchPageUrl);
            var rejectedUrls = new List<(string Url, string Reason)>();

            if (searchContext.AllowedPostalCodes.Count > 0)
                listingUrls = FilterByPostalCode(listingUrls, searchContext.AllowedPostalCodes, rejectedUrls);

            // Markeer listing-URLs van nieuwbouwzoekopdracht voor latere detail-filtering
            if (searchContext.IsNewBuildSearch)
                foreach (var u in listingUrls)
                    _newBuildSearchListings[u] = true;

            Logger.LogInformation(
                "[Immoweb] ══ URL-collectie klaar ══ " +
                "RawHrefsFound={Raw} | HrefCandidates={Href} | ApiFound={Api} | SponsorFound={Sponsor} | RegexFound={Regex} | " +
                "CandidateListingUrls={Candidate} | AcceptedListingUrls={Accepted} | RejectedListingUrls={Rejected}" +
                "{NewBuild}",
                rawHrefCount, hrefListingUrls.Count, apiListingUrls.Count, sponsorListingUrls.Count, regexListingUrls.Count,
                candidateCount, listingUrls.Count, rejectedUrls.Count,
                searchContext.IsNewBuildSearch ? " | IsNewBuildSearch=true" : "");

            if (rejectedUrls.Count > 0)
            {
                foreach (var r in rejectedUrls)
                    Logger.LogInformation("[Immoweb] RejectedListingUrl | {Url} | Reason={Reason}", r.Url, r.Reason);
                await WriteRejectedUrlsFileAsync(rejectedUrls, cancellationToken);
            }

            // ── Debug-bestanden wegschrijven ──────────────────────────────────
            await WriteSearchDebugFilesAsync(
                searchPageUrl, htmlContent, bodyText ?? string.Empty,
                captured, allRequests, apiEndpointLines, listingUrls, page, cancellationToken);

            await WriteAcceptedUrlsFileAsync(listingUrls, cancellationToken);
            await AppendPaginationSummaryAsync(searchPageUrl, resultCount, hrefListingUrls.Count,
                apiListingUrls.Count, sponsorListingUrls.Count, listingUrls.Count, cancellationToken);

            if (listingUrls.Count == 0)
                Logger.LogWarning("[Immoweb] 0 listing-URL's gevonden na alle bronnen. Zie debug/search/ voor details.");

            if (SearchDebugMode)
                Logger.LogWarning(
                    "[Immoweb] SearchDebugMode actief — {Count} URL(s) verzameld en teruggegeven voor analyse. " +
                    "Detailpagina's worden overgeslagen door BaseCrawler.",
                    listingUrls.Count);

            // Altijd de gevonden URLs teruggeven — ook in SearchDebugMode.
            // De beslissing om detailpagina's over te slaan ligt in BaseCrawler.
            return listingUrls;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout"))
        {
            Logger.LogWarning("[Immoweb] Timeout bij {Url} (>{Timeout}ms).", searchPageUrl, Settings.PlaywrightTimeoutMs);
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[Immoweb] Fout bij ophalen van {Url}.", searchPageUrl);
            return Enumerable.Empty<string>();
        }
        finally
        {
            if (page is not null)
            {
                var ctx = page.Context;
                await page.CloseAsync();
                await ctx.DisposeAsync();
            }
        }
    }

    // ── Debug screenshot helper ───────────────────────────────────────────────

    private async Task TakeDebugScreenshotAsync(IPage page, string fileName, CancellationToken cancellationToken)
    {
        if (!Settings.Debug.Enabled || !Settings.Debug.SaveScreenshots) return;
        try
        {
            var path = Path.Combine(GetDebugDir(), fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = false });
            Logger.LogInformation("[Immoweb] Screenshot → {Dir}/{File}", Settings.Debug.DebugDirectory, fileName);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Screenshot mislukt ({File}): {Msg}", fileName, ex.Message);
        }
    }

    // ── Cookie consent ────────────────────────────────────────────────────────

    private static readonly string[] ConsentButtonSelectors =
    [
        // Usercentrics standaard knoppen
        "button[data-testid='uc-accept-all-button']",
        "button[data-testid='uc-save-button']",
        "#usercentrics-root button.sc-gsDKAQ",
        // Tekst-gebaseerde selectors (meerdere talen)
        "button:has-text('Alles accepteren')",
        "button:has-text('Tout accepter')",
        "button:has-text('Accept All')",
        "button:has-text('Accept all')",
        "button:has-text('Akkoord')",
        "button:has-text('OK')",
        "button:has-text('Accepter')",
        // Generieke cookie-banner knoppen
        "[id*='cookie'] button:has-text('OK')",
        "[class*='cookie'] button:has-text('Accept')",
        "[id*='consent'] button",
        ".gdpr-banner button",
        "#didomi-notice-agree-button",
        ".didomi-notice-agree-button",
        "#onetrust-accept-btn-handler",
        ".cc-btn.cc-allow"
    ];

    private async Task HandleCookieConsentAsync(
        IPage page,
        int anchorsBefore,
        int scriptsBefore,
        int bodyLenBefore,
        CancellationToken cancellationToken)
    {
        try
        {
            // Snel checken of Usercentrics shadow-root aanwezig is of een consent-element zichtbaar
            var hasUsercentrics = await page.EvaluateAsync<bool>(
                "() => !!document.querySelector('#usercentrics-root') || " +
                "      !!document.querySelector('[id*=\"usercentrics\"]') || " +
                "      !!document.querySelector('[class*=\"uc-\"]')");

            var hasConsentDialog = await page.EvaluateAsync<bool>(
                "() => { " +
                "  const kws = ['cookie','consent','gdpr','privacy']; " +
                "  return Array.from(document.querySelectorAll('div,section,aside,dialog')) " +
                "    .some(el => kws.some(k => (el.id+' '+(el.className||'')).toLowerCase().includes(k)) && el.offsetHeight > 0); " +
                "}");

            if (!hasUsercentrics && !hasConsentDialog)
            {
                Logger.LogDebug("[Immoweb] Geen cookie consent popup gedetecteerd.");
                return;
            }

            Logger.LogWarning(
                "[Immoweb] CookieBannerDetected=true (Usercentrics={UC} | GenericDialog={GD}) — proberen te sluiten.",
                hasUsercentrics, hasConsentDialog);

            // Usercentrics werkt via een shadow DOM — probeer via evaluate eerst
            if (hasUsercentrics)
            {
                var clicked = await page.EvaluateAsync<bool>(@"
                    () => {
                        const root = document.querySelector('#usercentrics-root');
                        if (!root || !root.shadowRoot) return false;
                        const buttons = root.shadowRoot.querySelectorAll('button');
                        for (const btn of buttons) {
                            const txt = btn.innerText?.toLowerCase() || '';
                            if (txt.includes('accept') || txt.includes('akkoord') || txt.includes('ok') ||
                                txt.includes('alles') || txt.includes('tout') || txt.includes('alle')) {
                                btn.click();
                                return true;
                            }
                        }
                        return false;
                    }");

                if (clicked)
                {
                    Logger.LogInformation("[Immoweb] Cookie consent gesloten via Usercentrics shadow DOM.");
                    await page.WaitForTimeoutAsync(2000);
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                        new PageWaitForLoadStateOptions { Timeout = 10000 });

                    await TakeConsentScreenshotAsync(page, anchorsBefore, scriptsBefore, bodyLenBefore, cancellationToken);
                    return;
                }
            }

            // Fallback: knoppen via standaard selectors
            foreach (var selector in ConsentButtonSelectors)
            {
                try
                {
                    var btn = page.Locator(selector).First;
                    var isVisible = await btn.IsVisibleAsync();
                    if (!isVisible) continue;

                    Logger.LogInformation("[Immoweb] Cookie consent knop gevonden via selector: {Selector}", selector);
                    await btn.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                    Logger.LogInformation("[Immoweb] Cookie consent gesloten via knop.");

                    await page.WaitForTimeoutAsync(2000);
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                        new PageWaitForLoadStateOptions { Timeout = 10000 });

                    await TakeConsentScreenshotAsync(page, anchorsBefore, scriptsBefore, bodyLenBefore, cancellationToken);
                    return;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    Logger.LogDebug("[Immoweb] Selector '{Selector}' niet bruikbaar: {Msg}", selector, ex.Message);
                }
            }

            Logger.LogWarning("[Immoweb] Cookie consent popup gevonden maar kon niet gesloten worden. Crawler gaat verder.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogDebug("[Immoweb] Fout bij cookie consent handling: {Msg}", ex.Message);
        }
    }

    private async Task TakeConsentScreenshotAsync(
        IPage page, int anchorsBefore, int scriptsBefore, int bodyLenBefore,
        CancellationToken cancellationToken)
    {
        var anchorsAfter = await page.EvaluateAsync<int>("() => document.querySelectorAll('a').length");
        var scriptsAfter = await page.EvaluateAsync<int>("() => document.querySelectorAll('script').length");
        var bodyAfter = await page.EvaluateAsync<string>("() => document.body?.innerText ?? ''");
        var bodyLenAfter = bodyAfter?.Length ?? 0;

        Logger.LogInformation(
            "[Immoweb] Na consent → BodyLength: {Before}→{After} | <a>: {ABefore}→{AAfter} | <script>: {SBefore}→{SAfter}",
            bodyLenBefore, bodyLenAfter, anchorsBefore, anchorsAfter, scriptsBefore, scriptsAfter);

        // Snelle check: hoeveel listing-URLs zichtbaar na consent (altijd loggen, ongeacht debug-flags)
        try
        {
            var hrefsAfter = await page.EvaluateAsync<string[]>(
                "() => Array.from(document.querySelectorAll('a[href]')).map(a => a.href)");
            var listingCount = hrefsAfter?.Count(h => IsListingHref(h)) ?? 0;
            Logger.LogInformation("[Immoweb] Na consent: {Count} listing-URL's zichtbaar in hrefs.", listingCount);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Href-count na consent mislukt: {Msg}", ex.Message);
        }

        if (!Settings.Debug.Enabled) return;

        try
        {
            var debugDir = GetDebugDir();
            Directory.CreateDirectory(debugDir);

            await TakeDebugScreenshotAsync(page, "search-after-consent.png", cancellationToken);

            if (Settings.Debug.SaveHtml)
            {
                var htmlAfter = await page.ContentAsync();
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, "immoweb-after-consent.html"), htmlAfter, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Na-consent debug-bestanden mislukt: {Msg}", ex.Message);
        }
    }

    private static bool IsListingHref(string href) =>
        href.Contains("/zoekertje/", StringComparison.OrdinalIgnoreCase) ||
        href.Contains("/annonce/", StringComparison.OrdinalIgnoreCase) ||
        href.Contains("/classified/", StringComparison.OrdinalIgnoreCase);

    // ── Resultaatcount extraheren ─────────────────────────────────────────────

    // Titels die Immoweb gebruikt (allemaal gevangen door één patroon):
    //   "Huis en appartement te koop - Beernem (8730) - 105 panden"
    //   "104 HUIS te koop Beernem (8730)"
    //   "38 resultaten"
    private static readonly Regex ResultCountFromEnd = new(
        @"[-–]\s*(\d+)\s+panden?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ResultCountFromStart = new(
        @"^\s*(\d+)\s+(?:HUIS|APPARTEMENT|WONING|VASTGOED|GROND|GARAGE|panden?|resultaten?)",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ResultCountGeneric = new(
        @"(\d+)\s+(?:panden?|résultats?|results?|annonces?|zoekertjes?|properties|listings?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private async Task<int?> TryExtractResultCountAsync(IPage page, string bodyText)
    {
        // Poging 1: paginatitel bevat het aantal (meest betrouwbaar bij Immoweb)
        try
        {
            var pageTitle = await page.TitleAsync();
            if (!string.IsNullOrEmpty(pageTitle))
            {
                Logger.LogInformation("[Immoweb] Paginatitel: '{Title}'", pageTitle.Trim());
                var m = ResultCountFromEnd.Match(pageTitle);
                if (!m.Success) m = ResultCountFromStart.Match(pageTitle);
                if (m.Success && int.TryParse(m.Groups[1].Value, out var c))
                {
                    Logger.LogInformation("[Immoweb] ResultCount via paginatitel: {Count}", c);
                    return c;
                }
            }
        }
        catch (Exception ex) { Logger.LogDebug("[Immoweb] Paginatitel lezen mislukt: {Msg}", ex.Message); }

        // Poging 2: h1-element
        try
        {
            var h1 = await page.EvaluateAsync<string>(@"
                () => {
                    const h1 = document.querySelector('h1');
                    if (h1) return h1.innerText;
                    const el = document.querySelector('[data-count],[aria-label*=""result""]');
                    if (el) return el.innerText || el.getAttribute('data-count');
                    return null;
                }");

            if (!string.IsNullOrEmpty(h1))
            {
                Logger.LogInformation("[Immoweb] H1-tekst: '{Text}'", h1.Trim());
                var m = ResultCountFromEnd.Match(h1);
                if (!m.Success) m = ResultCountFromStart.Match(h1);
                if (!m.Success) m = ResultCountGeneric.Match(h1);
                if (m.Success && int.TryParse(m.Groups[1].Value, out var c))
                {
                    Logger.LogInformation("[Immoweb] ResultCount via h1: {Count}", c);
                    return c;
                }
            }
        }
        catch (Exception ex) { Logger.LogDebug("[Immoweb] H1 lezen mislukt: {Msg}", ex.Message); }

        // Poging 3: body-tekst regex
        if (!string.IsNullOrEmpty(bodyText))
        {
            var m = ResultCountFromEnd.Match(bodyText);
            if (!m.Success) m = ResultCountGeneric.Match(bodyText);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var c))
            {
                Logger.LogInformation("[Immoweb] ResultCount via body-tekst: {Count}", c);
                return c;
            }
        }

        Logger.LogWarning("[Immoweb] ResultCount kon niet bepaald worden.");
        return null;
    }

    // ── Paginering-samenvatting ───────────────────────────────────────────────

    private async Task AppendPaginationSummaryAsync(
        string searchUrl, int? resultCount,
        int hrefCount, int apiCount, int sponsorCount, int uniqueCount,
        CancellationToken cancellationToken)
    {
        try
        {
            var pageMatch = Regex.Match(searchUrl, @"[?&]page=(\d+)", RegexOptions.IgnoreCase);
            var pageNum = pageMatch.Success ? int.Parse(pageMatch.Groups[1].Value) : 1;

            var locationMatch = Regex.Match(searchUrl, @"/te-koop/([^/?]+)/(\d+)", RegexOptions.IgnoreCase);
            var location = locationMatch.Success
                ? $"{locationMatch.Groups[1].Value}/{locationMatch.Groups[2].Value}"
                : "onbekend";

            if (pageNum == 1 && resultCount.HasValue)
                _detectedResultCount = resultCount;

            _pageStats.Add(new PageStat(pageNum, location, hrefCount, apiCount, sponsorCount, uniqueCount));

            var totalUnique = _pageStats.Sum(p => p.Unique);
            var effectiveCount = _detectedResultCount ?? resultCount;
            var resultsPerPage = _pageStats.Count > 0
                ? (int)Math.Max(1, _pageStats.Average(p => Math.Max(p.Href, p.Unique)))
                : 30;
            var estimatedPages = effectiveCount.HasValue
                ? (int)Math.Ceiling(effectiveCount.Value / (double)resultsPerPage)
                : (int?)null;

            // Sla geschat maximum op na pagina 1 (gebruikt voor vroeg afbreken)
            if (pageNum == 1 && estimatedPages.HasValue)
            {
                var srcSettings = GetSourceSettings();
                var cap = srcSettings.SearchDebugMode
                    ? Settings.Debug.MaxPagesInSearchDebugMode
                    : srcSettings.MaxSearchPagesPerLocation;
                _estimatedMaxPages = Math.Min(estimatedPages.Value, cap);
                Logger.LogInformation(
                    "[Immoweb] EstimatedMaxPages vastgesteld op {Est} (gecapped op {Cap}).",
                    _estimatedMaxPages.Value, cap);
            }

            Logger.LogInformation(
                "[Immoweb] Paginering | Pagina {Page}: Href={Href} Unique={Unique} | " +
                "Cumulatief={Total}/{ResultCount} | EstimatedPages={Est}",
                pageNum, hrefCount, uniqueCount, totalUnique,
                effectiveCount?.ToString() ?? "?",
                estimatedPages?.ToString() ?? "?");

            if (!Settings.Debug.Enabled || !Settings.Debug.SavePaginationSummary) return;

            var path = Path.Combine(GetDebugDir(), "immoweb-pagination-summary.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var lines = new List<string>
            {
                "# Paginering-samenvatting Immoweb",
                $"# Bijgewerkt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"# Locatie   : {location}",
                "",
                $"ResultCount    = {effectiveCount?.ToString() ?? "onbekend"}",
                $"ResultsPerPage = ~{resultsPerPage}",
                $"EstimatedPages = {estimatedPages?.ToString() ?? "onbekend"}",
                $"PagesBezocht   = {_pageStats.Count}",
                ""
            };

            foreach (var stat in _pageStats)
            {
                lines.Add(
                    $"Page{stat.PageNum,-3} = {stat.Unique,4} uniek" +
                    $"  (href={stat.Href}, api={stat.Api}, sponsor={stat.Sponsor})");
            }

            lines.Add("");
            lines.Add($"TotalUnique = {totalUnique}");

            await File.WriteAllLinesAsync(path, lines, cancellationToken);
            Logger.LogInformation("[Immoweb] Paginering-samenvatting → {Dir}/immoweb-pagination-summary.txt", Settings.Debug.DebugDirectory);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Paginering-samenvatting schrijven mislukt: {Msg}", ex.Message);
        }
    }

    // ── Samenvoegen en dedupliceren ───────────────────────────────────────────

    private List<string> MergeAndDeduplicate(
        IEnumerable<string> hrefs,
        IEnumerable<string> api,
        IEnumerable<string> sponsored,
        IEnumerable<string> regex)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // ExternalId
        var result = new List<string>();

        void Add(IEnumerable<string> source)
        {
            foreach (var url in source)
            {
                var id = ExtractExternalIdFromUrl(url);
                if (id is null) continue;
                if (seen.Add(id))
                    result.Add(url);
            }
        }

        // Volgorde: hrefs > API (niet-sponsor) > sponsor > regex
        Add(hrefs);
        Add(api);
        Add(sponsored);
        Add(regex);

        return result;
    }

    private static string? ExtractExternalIdFromUrl(string url)
    {
        var m = UrlIdPattern.Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }

    private async Task WriteAcceptedUrlsFileAsync(
        IReadOnlyList<string> urls, CancellationToken cancellationToken)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "debug", "search");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "immoweb-accepted-listing-urls.txt");

            var lines = new List<string>
            {
                $"# Datum     : {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"# Totaal    : {urls.Count} geaccepteerde listing-URL's",
                ""
            };
            lines.AddRange(urls);

            await File.WriteAllLinesAsync(path, lines, cancellationToken);
            Logger.LogInformation("[Immoweb] Geaccepteerde listing-URL's → debug/search/immoweb-accepted-listing-urls.txt ({Count})",
                urls.Count);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Schrijven accepted-urls mislukt: {Msg}", ex.Message);
        }
    }

    // ── Postcode-filter helpers ───────────────────────────────────────────────

    private SearchUrlContext BuildSearchContext(string searchUrl)
    {
        var isNewBuild = searchUrl.Contains("isNewlyBuilt=true", StringComparison.OrdinalIgnoreCase);
        var codes = GetSourceSettings().AllowedLocations
            .Where(l => !string.IsNullOrWhiteSpace(l.PostalCode))
            .Select(l => l.PostalCode!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new SearchUrlContext(codes, isNewBuild);
    }

    private List<string> FilterByPostalCode(
        List<string> urls,
        IReadOnlySet<string> allowedCodes,
        List<(string Url, string Reason)> rejectedOut)
    {
        var accepted = new List<string>();
        foreach (var url in urls)
        {
            var m = PostalCodeInListingUrl.Match(url);
            if (m.Success && allowedCodes.Contains(m.Groups[1].Value))
            {
                accepted.Add(url);
            }
            else
            {
                var reason = m.Success ? "PostalCodeMismatch" : "NoPostalCodeInUrl";
                rejectedOut.Add((url, reason));
            }
        }
        return accepted;
    }

    private async Task WriteRejectedUrlsFileAsync(
        List<(string Url, string Reason)> rejections, CancellationToken cancellationToken)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "debug", "search");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "immoweb-rejected-listing-urls.txt");
            var lines = rejections.Select(r => $"RejectedListingUrl | {r.Url} | Reason={r.Reason}");
            await File.AppendAllLinesAsync(path, lines, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Schrijven rejected-urls mislukt: {Msg}", ex.Message);
        }
    }

    // ── Helpers voor URL-classificatie ────────────────────────────────────────

    private static bool IsInterestingApiUrl(string url)
    {
        if (url.Contains(".js", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("_next/data", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains(".css", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains(".woff", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains(".png", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains(".jpg", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains(".svg", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains("analytics", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains("tracking", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains("segment.io", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains("google", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains("facebook", StringComparison.OrdinalIgnoreCase)) return false;

        return ApiUrlKeywords.Any(k => url.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Fase 3: Bepaalt of een href een echte Immoweb-detailpagina is.
    /// Accepteert: /nl/zoekertje/, /fr/annonce/, /en/classified/ met 6+ cijferig ID.
    /// Weigert: zoeken/, search/, recherche/, advanced-search, map, filters, landingspagina's.
    /// </summary>
    private bool IsClassifiedUrl(string url)
    {
        // ── Expliciete uitsluitingen ──
        var exclusions = new[]
        {
            "/zoeken/", "/search/", "/recherche/",
            "/advanced-search", "/zoek-goedkoop/", "/search-cheap/",
            "/map", "/carte", "/kaart",
            "/favorites", "/favoriten", "/favoris",
            "/mijn-immoweb", "/my-immoweb", "/mon-immoweb",
            "/agent/", "/agence/", "/kantoor/",
            "/nieuwbouwproject/", "/nouveau-projet/", "/new-development/"
        };
        foreach (var ex in exclusions)
            if (url.Contains(ex, StringComparison.OrdinalIgnoreCase)) return false;

        // Moet het listing-pad-segment bevatten
        var hasListingSegment =
            url.Contains("/classified/", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/zoekertje/", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/annonce/", StringComparison.OrdinalIgnoreCase);

        if (!hasListingSegment)
        {
            Logger.LogDebug("[Immoweb] [HREF] ✗ geen listing-segment: {Url}", url);
            return false;
        }

        var matchFull = ClassifiedUrlPattern.IsMatch(url);
        var matchShort = ClassifiedIdOnlyPattern.IsMatch(url);

        if (matchFull || matchShort)
        {
            Logger.LogDebug("[Immoweb] [HREF] ✔ geaccepteerd ({Pattern}): {Url}",
                matchFull ? "volledig-pad" : "kort-id", url);
            return true;
        }

        Logger.LogDebug("[Immoweb] [HREF] ✗ geen numeriek ID gevonden: {Url}", url);
        return false;
    }

    /// <summary>
    /// Fase 2 helper: extraheer expliciete classified-URL's uit een JSON-response body.
    /// Zoekt naar velden met "url", "link", "href" die /zoekertje/, /annonce/ of /classified/ bevatten.
    /// </summary>
    private static IEnumerable<string> ExtractListingUrlsFromJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) yield break;

        JsonNode? root;
        try { root = JsonNode.Parse(body); }
        catch { yield break; }

        if (root is null) yield break;

        foreach (var url in ExtractUrlsFromNode(root))
            yield return url;
    }

    private static IEnumerable<string> ExtractUrlsFromNode(JsonNode node, int depth = 0)
    {
        if (depth > 8) yield break;

        if (node is JsonObject obj)
        {
            foreach (var kvp in obj)
            {
                var key = kvp.Key.ToLowerInvariant();
                if ((key is "url" or "link" or "href" or "detailurl" or "permalink") &&
                    kvp.Value is JsonValue strVal)
                {
                    string? s = null;
                    try { s = strVal.GetValue<string>(); } catch { }
                    if (!string.IsNullOrEmpty(s) &&
                        (s.Contains("/zoekertje/", StringComparison.OrdinalIgnoreCase) ||
                         s.Contains("/annonce/", StringComparison.OrdinalIgnoreCase) ||
                         s.Contains("/classified/", StringComparison.OrdinalIgnoreCase)))
                    {
                        yield return s.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? s
                            : "https://www.immoweb.be" + s;
                    }
                }

                if (kvp.Value is not null)
                    foreach (var u in ExtractUrlsFromNode(kvp.Value, depth + 1))
                        yield return u;
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
                if (item is not null)
                    foreach (var u in ExtractUrlsFromNode(item, depth + 1))
                        yield return u;
        }
    }

    /// <summary>
    /// Fase 4 fallback: scan volledige HTML-tekst op classified-URL patronen via regex.
    /// </summary>
    private static IEnumerable<string> ScanHtmlForClassifiedUrls(string html)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in HtmlClassifiedUrlPattern.Matches(html))
        {
            var raw = m.Value.TrimEnd('"', '\'', '<', '>', ' ', '\t');
            var url = raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? raw
                : "https://www.immoweb.be" + raw;

            if (seen.Add(url))
                yield return url;
        }
    }

    // ── IDs extraheren uit een JSON-response body ──────────────────────────────

    private static IEnumerable<long> TryExtractClassifiedIds(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) yield break;

        JsonNode? root;
        try { root = JsonNode.Parse(body); }
        catch { yield break; }

        if (root is null) yield break;

        foreach (var id in ExtractIdsFromNode(root))
            yield return id;
    }

    private static IEnumerable<long> ExtractIdsFromNode(JsonNode node, int depth = 0)
    {
        if (depth > 8) yield break;

        if (node is JsonObject obj)
        {
            // "id" op een object dat ook "type" of "propertyType" heeft = waarschijnlijk classified
            var hasId = obj.TryGetPropertyValue("id", out var idNode);
            var looksLikeClassified =
                obj.ContainsKey("type") || obj.ContainsKey("propertyType") ||
                obj.ContainsKey("property") || obj.ContainsKey("transaction") ||
                obj.ContainsKey("classified");

            if (hasId && looksLikeClassified)
            {
                long? id = null;
                try { id = idNode?.GetValue<long?>(); } catch { /* type mismatch */ }
                if (id.HasValue && id.Value > 100_000)
                {
                    yield return id.Value;
                    yield break; // niet dieper — vermijdt dubbele IDs uit child-objecten
                }
            }

            foreach (var child in obj)
                if (child.Value is not null)
                    foreach (var id in ExtractIdsFromNode(child.Value, depth + 1))
                        yield return id;
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
                if (item is not null)
                    foreach (var id in ExtractIdsFromNode(item, depth + 1))
                        yield return id;
        }
    }

    // ── Fase 3: Href-scan ─────────────────────────────────────────────────────

    private async Task<(List<string> Urls, int RawCount)> ScanHrefsAsync(IPage page)
    {
        var result = new List<string>();

        var allHrefs = await page.EvaluateAsync<string[]>(
            "() => Array.from(document.querySelectorAll('a[href]')).map(a => a.href).filter(h => h.length > 0)");

        if (allHrefs is null) return (result, 0);

        var rawCount = allHrefs.Length;
        var accepted = 0;
        var rejected = 0;

        foreach (var raw in allHrefs.Distinct())
        {
            var href = raw.Trim();
            if (string.IsNullOrEmpty(href)) continue;

            if (IsClassifiedUrl(href))
            {
                if (!result.Contains(href))
                {
                    result.Add(href);
                    accepted++;
                }
            }
            else
            {
                rejected++;
            }
        }

        Logger.LogInformation(
            "[Immoweb] Href-scan: totaal={Total} | listing-kandidaten={Acc} | niet-listing={Rej}",
            rawCount, accepted, rejected);

        return (result, rawCount);
    }

    // ── Debug-bestanden (zoekpagina) ──────────────────────────────────────────

    private async Task WriteSearchDebugFilesAsync(
        string sourceUrl,
        string htmlContent,
        string bodyText,
        ConcurrentBag<CapturedResponse> captured,
        ConcurrentBag<string> allRequests,
        List<string> apiEndpointLines,
        List<string> foundListingUrls,
        IPage page,
        CancellationToken cancellationToken)
    {
        if (!Settings.Debug.Enabled) return;

        try
        {
            var debugDir = GetDebugDir();
            var networkDir = Path.Combine(debugDir, "network-responses");
            Directory.CreateDirectory(networkDir);

            var header = new[]
            {
                $"# Zoekpagina : {sourceUrl}",
                $"# Datum      : {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"# Werkelijke URL: {page.Url}",
                $"# Paginatitel: {await page.TitleAsync()}",
                ""
            };

            if (Settings.Debug.SaveHtml)
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, "immoweb-search.html"), htmlContent, cancellationToken);

            if (Settings.Debug.SaveScreenshots)
            {
                var screenshotPath = Path.Combine(debugDir, "immoweb-search.png");
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = false });
                Logger.LogInformation("[Immoweb] Screenshot → {Path}", screenshotPath);
            }

            if (Settings.Debug.SaveBodyText)
            {
                var allHrefs = await page.EvaluateAsync<string[]>(
                    "() => Array.from(document.querySelectorAll('a[href]')).map(a => a.href)");
                await File.WriteAllLinesAsync(
                    Path.Combine(debugDir, "immoweb-hrefs.txt"),
                    header.Concat(allHrefs ?? []), cancellationToken);
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, "immoweb-bodytext.txt"),
                    string.Join("\n", header) + "\n" + bodyText, cancellationToken);
            }

            if (Settings.Debug.SaveNetworkResponses)
            {
                await File.WriteAllLinesAsync(
                    Path.Combine(debugDir, "immoweb-network-requests.txt"),
                    header.Concat(allRequests.OrderBy(r => r)), cancellationToken);

                await File.WriteAllLinesAsync(
                    Path.Combine(debugDir, "immoweb-network-responses-summary.txt"),
                    header
                        .Append($"# JSON-responses: {captured.Count}")
                        .Append(new string('-', 120))
                        .Append("METHOD  STATUS  IDS   URLS  CONTENT-TYPE                        URL")
                        .Concat(apiEndpointLines),
                    cancellationToken);

                var candidateLines = header
                    .Append($"# Gevonden listing-URL's: {foundListingUrls.Count}")
                    .Append("")
                    .Concat(foundListingUrls)
                    .Append("")
                    .Append("# Kandidaat classified-IDs uit JSON:")
                    .Concat(captured.SelectMany(r =>
                        TryExtractClassifiedIds(r.Body).Select(id => $"  {id}  [uit: {r.Url}]")));
                await File.WriteAllLinesAsync(
                    Path.Combine(debugDir, "immoweb-json-candidates.txt"), candidateLines, cancellationToken);

                var fileIndex = 0;
                foreach (var resp in captured.OrderBy(r => r.Url))
                {
                    var safeName = Regex.Replace(resp.Url, @"[^a-zA-Z0-9._-]", "_");
                    if (safeName.Length > 80) safeName = safeName[^80..];
                    var fileName = $"{++fileIndex:00}_{resp.Status}_{safeName}.json";
                    await File.WriteAllTextAsync(
                        Path.Combine(networkDir, fileName), resp.Body, cancellationToken);
                }

                Logger.LogInformation("[Immoweb] Network-responses → {Dir}/network-responses/ ({Count} bestanden)",
                    Settings.Debug.DebugDirectory, captured.Count);
            }

            Logger.LogInformation("[Immoweb] Debugbestanden → {Dir}/", Settings.Debug.DebugDirectory);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Debug-bestanden schrijven mislukt: {Error}", ex.Message);
        }
    }

    // ── Detail-pagina parsing ──────────────────────────────────────────────────

    protected override async Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken)
    {
        IPage? page = null;
        try
        {
            page = await _browser.NewPageAsync(Settings.UserAgent);

            await page.GotoAsync(listingUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = Settings.PlaywrightTimeoutMs
            });

            // Stap 1: window.classified (meest betrouwbaar)
            var classifiedJson = await page.EvaluateAsync<string>(
                "() => typeof window.classified !== 'undefined' ? JSON.stringify(window.classified) : null");

            if (!string.IsNullOrEmpty(classifiedJson))
            {
                var dto = ParseClassifiedJson(classifiedJson, listingUrl);
                if (dto is not null)
                {
                    if (ApplyNewBuildFilter(dto, listingUrl) is null) return null;
                    Logger.LogInformation("[Immoweb] window.classified gevonden.");
                    LogParsedListing(dto);
                    await HandleProjectGroupAsync(dto, classifiedJson, cancellationToken);
                    await WriteParserDebugFileAsync(dto, classifiedJson, cancellationToken);
                    return dto;
                }
            }

            // Stap 2: __NEXT_DATA__
            var nextDataJson = await page.EvaluateAsync<string>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); return el ? el.textContent : null; }");

            if (!string.IsNullOrEmpty(nextDataJson))
            {
                var dto = ParseNextDataJson(nextDataJson, listingUrl);
                if (dto is not null)
                {
                    if (ApplyNewBuildFilter(dto, listingUrl) is null) return null;
                    Logger.LogInformation("[Immoweb] __NEXT_DATA__ gevonden.");
                    LogParsedListing(dto);
                    await HandleProjectGroupAsync(dto, nextDataJson, cancellationToken);
                    await WriteParserDebugFileAsync(dto, nextDataJson, cancellationToken);
                    return dto;
                }

                Logger.LogDebug("[Immoweb] __NEXT_DATA__ aanwezig maar classified-data niet gevonden erin.");
            }

            // Stap 3: Fallback — script-tags scannen op classified ID
            var classifiedId = ExtractIdFromUrl(listingUrl);
            Logger.LogWarning(
                "[Immoweb] window.classified en __NEXT_DATA__ niet bruikbaar. Fallback naar script-scan (ID={Id}).",
                classifiedId ?? "onbekend");

            var htmlContent = await page.ContentAsync();
            var scripts = await page.EvaluateAsync<string[]>(@"
() => Array.from(document.querySelectorAll('script'))
    .map(s => s.textContent || '')
    .filter(t => t.length > 0)");

            ListingDto? fallbackDto = null;
            if (!string.IsNullOrEmpty(classifiedId) && scripts is { Length: > 0 })
                fallbackDto = TryParseFromScripts(scripts, classifiedId, listingUrl);

            if (fallbackDto is not null)
            {
                if (ApplyNewBuildFilter(fallbackDto, listingUrl) is null)
                {
                    await WriteDetailDebugFilesAsync(listingUrl, htmlContent, scripts, cancellationToken);
                    return null;
                }
                Logger.LogInformation("[Immoweb] Script-fallback gelukt voor ID {Id}.", classifiedId);
                LogParsedListing(fallbackDto);
                var fallbackRaw = scripts?.FirstOrDefault(s => s.Contains(classifiedId ?? "")) ?? "";
                await HandleProjectGroupAsync(fallbackDto, fallbackRaw, cancellationToken);
                await WriteParserDebugFileAsync(fallbackDto, fallbackRaw, cancellationToken);
            }
            else
            {
                Logger.LogWarning("[Immoweb] Geen data gevonden op {Url}. Zie debug/immoweb-detail.*", listingUrl);
            }

            // Debug-bestanden altijd schrijven bij mislukking (of als ID gevonden maar data niet)
            await WriteDetailDebugFilesAsync(listingUrl, htmlContent, scripts, cancellationToken);

            return fallbackDto;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout"))
        {
            Logger.LogWarning("[Immoweb] Timeout bij {Url}.", listingUrl);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[Immoweb] Fout bij {Url}.", listingUrl);
            return null;
        }
        finally
        {
            if (page is not null)
            {
                var ctx = page.Context;
                await page.CloseAsync();
                await ctx.DisposeAsync();
            }
        }
    }

    private ListingDto? ApplyNewBuildFilter(ListingDto dto, string listingUrl)
    {
        if (!_newBuildSearchListings.ContainsKey(listingUrl)) return dto;
        if (dto.IsNewBuild == true) return dto;
        Logger.LogInformation(
            "[Immoweb] ListingSkipped | {Url} | Reason=NotNewBuild (zoekopdracht: isNewlyBuilt=true, listing: IsNewBuild=false).",
            listingUrl);
        return null;
    }

    private void LogParsedListing(ListingDto dto)
    {
        var isProject = IsProjectGroup(dto, dto.Url ?? "");
        var features = AnalyzeDescription(dto.Description);

        Logger.LogInformation(
            "[Immoweb] ══ Listing geparsed ══\n" +
            "  ExternalId      : {ExternalId}\n" +
            "  Url             : {Url}\n" +
            "  IsProjectGroup  : {IsProject}\n" +
            "  PropertyType    : {RawType} / {RawSubType}\n" +
            "  Transaction     : {Transaction}\n" +
            "  PostalCode/City : {PostalCode} {City}\n" +
            "  Street          : {Street} {HouseNumber}\n" +
            "  Floor/Unit      : {Floor} / {Unit}\n" +
            "  AskingPrice     : {Price}\n" +
            "  LivingArea      : {Area} m²\n" +
            "  LandArea        : {Land} m²\n" +
            "  Bedrooms        : {Bedrooms}\n" +
            "  Bathrooms       : {Bathrooms}\n" +
            "  GarageCount     : {Garage}\n" +
            "  ConstructionYear: {Year}\n" +
            "  EPCScore        : {EpcScore} kWh/m²jaar\n" +
            "  EPCLabel        : {EpcLabel}\n" +
            "  IsNewBuild      : {IsNewBuild} (bron: {IsNewBuildSource})\n" +
            "  Description     : {DescLen} tekens\n" +
            "  EnergyFeatures  : {Features}\n" +
            "  RawJson         : {HasJson}",
            dto.ExternalId ?? "?",
            dto.Url ?? "?",
            isProject,
            dto.PropertyTypeRaw ?? "?", dto.PropertySubTypeRaw ?? "?",
            dto.TransactionTypeRaw ?? "?",
            dto.PostalCode ?? "?", dto.City ?? "?",
            dto.Street ?? "?", dto.HouseNumber ?? "?",
            dto.Floor?.ToString() ?? "(onbekend)",
            dto.UnitNumber ?? "(onbekend)",
            dto.AskingPrice.HasValue ? $"€{dto.AskingPrice.Value:N0}" : "?",
            dto.LivingArea?.ToString("N0") ?? "?",
            dto.LandArea?.ToString("N0") ?? "?",
            dto.Bedrooms?.ToString() ?? "?",
            dto.Bathrooms?.ToString() ?? "?",
            dto.GarageCount?.ToString() ?? "?",
            dto.ConstructionYear?.ToString() ?? "?",
            dto.EPCScore?.ToString("N0") ?? "?",
            dto.EPCLabelRaw ?? "?",
            dto.IsNewBuild == true ? "JA" : "nee",
            dto.IsNewBuildSource ?? "geen",
            dto.Description?.Length.ToString() ?? "0",
            features.Count > 0 ? string.Join(", ", features) : "geen",
            string.IsNullOrEmpty(dto.RawJson) ? "NEE" : $"JA ({dto.RawJson.Length:N0} bytes)");
    }

    // ── ProjectGroup detectie & debug ─────────────────────────────────────────

    private static bool IsProjectGroup(ListingDto dto, string url) =>
        url.Contains("/nieuwbouwproject-", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("/nouveau-projet-", StringComparison.OrdinalIgnoreCase) ||
        url.Contains("/new-development-", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(dto.PropertyTypeRaw, "HOUSE_GROUP", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(dto.PropertyTypeRaw, "APARTMENT_GROUP", StringComparison.OrdinalIgnoreCase);

    private static (string? ProjectName, int? SoldPercentage, List<ProjectGroupUnitDto> Units)
        ParseProjectGroupUnits(string json, string projectId)
    {
        try
        {
            var root = JsonNode.Parse(json);
            if (root is null) return (null, null, []);

            // window.classified: cluster zit direct op root
            // __NEXT_DATA__: cluster zit dieper
            var cluster = root["cluster"]
                       ?? root["props"]?["pageProps"]?["classified"]?["cluster"]
                       ?? root["listing"]?["cluster"];

            if (cluster is null) return (null, null, []);

            var projectName = cluster["projectInfo"]?["projectName"]?.GetValue<string>();
            var soldPct = TryGetInt(cluster["projectInfo"]?["soldPercentage"]);

            var units = new List<ProjectGroupUnitDto>();
            if (cluster["units"] is not JsonArray unitsArray) return (projectName, soldPct, units);

            foreach (var unitGroup in unitsArray)
            {
                if (unitGroup is null) continue;
                var groupType = unitGroup["type"]?.GetValue<string>();
                if (unitGroup["items"] is not JsonArray items) continue;

                foreach (var item in items)
                {
                    if (item is null) continue;
                    var subType = item["subtype"]?.GetValue<string>();
                    units.Add(new ProjectGroupUnitDto
                    {
                        ParentProjectId = projectId,
                        ParentProjectName = projectName,
                        UnitId = item["id"]?.GetValue<long?>()?.ToString() ?? string.Empty,
                        RawGroupType = groupType,
                        RawSubType = subType,
                        MappedPropertyType = MapUnitPropertyType(groupType),
                        MappedPropertySubType = MapUnitPropertySubType(subType ?? groupType),
                        SaleStatus = MapSaleStatus(item["saleStatus"]?.GetValue<string>()),
                        Price = TryGetDecimal(item["price"]),
                        BedroomCount = TryGetInt(item["bedroomCount"]),
                        Surface = TryGetDecimal(item["surface"]),
                        Floor = TryGetInt(item["floor"]),
                        Phase = item["realEstateProjectPhase"]?.GetValue<string>()
                    });
                }
            }

            return (projectName, soldPct, units);
        }
        catch
        {
            return (null, null, []);
        }
    }

    private static SaleStatus MapSaleStatus(string? raw) => raw?.ToUpperInvariant() switch
    {
        "AVAILABLE" => SaleStatus.Available,
        "SOLD" or "SOLD_OUT" => SaleStatus.Sold,
        "OPTION" => SaleStatus.Option,
        "RESERVED" => SaleStatus.Reserved,
        _ => SaleStatus.Unknown
    };

    private static PropertyType MapUnitPropertyType(string? rawGroupType) => rawGroupType?.ToUpperInvariant() switch
    {
        "HOUSE" => PropertyType.House,
        "APARTMENT" => PropertyType.Apartment,
        "COMMERCIAL" => PropertyType.CommercialProperty,
        _ => PropertyType.Unknown
    };

    private static PropertySubType MapUnitPropertySubType(string? rawSubType) => rawSubType?.ToUpperInvariant() switch
    {
        "HOUSE" => PropertySubType.DetachedHouse,
        "APARTMENT" => PropertySubType.Apartment,
        "PENTHOUSE" => PropertySubType.Penthouse,
        "STUDIO" => PropertySubType.Studio,
        "DUPLEX" => PropertySubType.Duplex,
        "TRIPLEX" => PropertySubType.Triplex,
        "GROUND_FLOOR" => PropertySubType.GroundFloorFlat,
        "COMMERCIAL_PREMISES" => PropertySubType.CommercialGround,
        "OFFICE" => PropertySubType.Office,
        "VILLA" => PropertySubType.Villa,
        "BUNGALOW" => PropertySubType.Bungalow,
        _ => PropertySubType.Unknown
    };

    private async Task HandleProjectGroupAsync(
        ListingDto dto, string rawJson, CancellationToken cancellationToken)
    {
        if (!IsProjectGroup(dto, dto.Url ?? "")) return;

        var (projectName, soldPct, units) = ParseProjectGroupUnits(rawJson, dto.ExternalId ?? "");

        // Unitstatistieken berekenen uit geparsede units
        var unitsTotal     = units.Count;
        var unitsSold      = units.Count(u => u.SaleStatus == SaleStatus.Sold);
        var unitsAvailable = units.Count(u => u.SaleStatus == SaleStatus.Available);
        var unitsOption    = units.Count(u => u.SaleStatus == SaleStatus.Option);
        var unitsReserved  = units.Count(u => u.SaleStatus == SaleStatus.Reserved);

        // Developer info zit al in dto (geparsed in ParseClassifiedJson via customers[0])
        Logger.LogInformation(
            "[Immoweb] ProjectGroupDetected=true | ExternalId={Id} | Type={Type} | " +
            "ProjectName={Name} | SoldPct={Pct}% | " +
            "UnitsTotal={Total} | UnitsSold={Sold} | UnitsAvailable={Avail} | UnitsOption={Opt} | UnitsReserved={Res}",
            dto.ExternalId,
            dto.PropertyTypeRaw ?? "?",
            projectName ?? "?",
            soldPct?.ToString() ?? "?",
            unitsTotal, unitsSold, unitsAvailable, unitsOption, unitsReserved);

        if (dto.DeveloperName is not null)
            Logger.LogInformation(
                "[Immoweb] Developer/Makelaar | Name={Name} | Website={Web} | Phone={Phone}",
                dto.DeveloperName, dto.DeveloperWebsite ?? "?", dto.DeveloperPhone ?? "?");

        if (units.Count > 0)
        {
            _pendingProjectUnits[dto.ExternalId ?? ""] = units;

            Logger.LogInformation("[Immoweb] Eerste {N} units:", Math.Min(units.Count, 5));
            foreach (var unit in units.Take(5))
                Logger.LogInformation(
                    "[Immoweb]   Unit={Id} | {SubType} ({GroupType}) | Status={Status} | Prijs={Price} | Kamers={Beds} | Opp={Surface}m²",
                    unit.UnitId,
                    unit.RawSubType ?? "?",
                    unit.RawGroupType ?? "?",
                    unit.SaleStatus,
                    unit.Price.HasValue ? $"€{unit.Price.Value:N0}" : "?",
                    unit.BedroomCount?.ToString() ?? "?",
                    unit.Surface?.ToString("N0") ?? "?");
        }

        if (!Settings.Debug.Enabled) return;

        try
        {
            var debugDir = Path.Combine(AppContext.BaseDirectory, "debug", "project-units");
            Directory.CreateDirectory(debugDir);

            var output = new
            {
                projectId = dto.ExternalId,
                projectName,
                soldPercentage = soldPct,
                crawledAt = DateTime.UtcNow,
                statistics = new
                {
                    unitsTotal,
                    unitsSold,
                    unitsAvailable,
                    unitsOption,
                    unitsReserved
                },
                developer = dto.DeveloperName is null ? null : new
                {
                    name = dto.DeveloperName,
                    website = dto.DeveloperWebsite,
                    phone = dto.DeveloperPhone
                },
                units = units.Select(u => new
                {
                    unitId = u.UnitId,
                    groupType = u.RawGroupType,
                    subType = u.RawSubType,
                    propertyType = u.MappedPropertyType.ToString(),
                    propertySubType = u.MappedPropertySubType.ToString(),
                    saleStatus = u.SaleStatus.ToString(),
                    price = u.Price,
                    bedroomCount = u.BedroomCount,
                    surface = u.Surface,
                    floor = u.Floor,
                    phase = u.Phase
                }).ToList()
            };

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var path = Path.Combine(debugDir, $"{dto.ExternalId}-units.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(output, opts), cancellationToken);
            Logger.LogInformation("[Immoweb] ProjectGroup units → debug/project-units/{Id}-units.json ({Count} units)",
                dto.ExternalId, units.Count);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] ProjectGroup debug-bestand schrijven mislukt: {Msg}", ex.Message);
        }
    }

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
                "[Immoweb] [DRYRUN] Project zou worden opgeslagen: {ExternalId} | {Units} units te verwerken",
                normalized.ExternalId, units.Count);
            await ListingService.UpsertProjectUnitsAsync(0, normalized, units, dryRun: true, Settings.MissingListingThreshold, cancellationToken);
            return;
        }

        if (!assetId.HasValue) return;

        var saveResult = await ListingService.UpsertProjectUnitsAsync(
            assetId.Value, normalized, units, dryRun: false, Settings.MissingListingThreshold, cancellationToken);

        var soldPct = saveResult.UnitsFound > 0
            ? Math.Round((decimal)saveResult.SoldUnits / saveResult.UnitsFound * 100, 1)
            : 0m;

        Logger.LogInformation(
            "[Immoweb] ProjectGroupSaved | ProjectAssetId={AssetId} | ProjectExternalId={ExternalId} | ProjectName={Name} | " +
            "UnitsFound={Found} | UnitsCreated={Created} | UnitsUpdated={Updated} | " +
            "HouseUnits={H} | ApartmentUnits={A} | CommercialUnits={C} | " +
            "SoldUnits={Sold} | AvailableUnits={Avail} | ReservedUnits={Res} | OptionUnits={Opt} | UnknownUnits={Unk} | " +
            "SoldPct={SoldPct}% | AvgPrice={AvgPrice} | AvgPricePerSqm={AvgPpSqm} | AvgArea={AvgArea}m²",
            assetId.Value,
            normalized.ExternalId,
            units.FirstOrDefault()?.ParentProjectName ?? "?",
            saveResult.UnitsFound,
            saveResult.UnitsCreated,
            saveResult.UnitsUpdated,
            saveResult.HouseUnits,
            saveResult.ApartmentUnits,
            saveResult.CommercialUnits,
            saveResult.SoldUnits,
            saveResult.AvailableUnits,
            saveResult.ReservedUnits,
            saveResult.OptionUnits,
            saveResult.UnknownUnits,
            soldPct,
            saveResult.AveragePrice.HasValue ? $"€{saveResult.AveragePrice.Value:N0}" : "?",
            saveResult.AveragePricePerSqm.HasValue ? $"€{saveResult.AveragePricePerSqm.Value:N0}" : "?",
            saveResult.AverageLivingArea?.ToString("N0") ?? "?");

        // KPI JSON schrijven
        try
        {
            var kpiDir = Path.Combine(AppContext.BaseDirectory, "debug", "kpi");
            Directory.CreateDirectory(kpiDir);

            var kpiOutput = new
            {
                projectId = normalized.ExternalId,
                projectName = units.FirstOrDefault()?.ParentProjectName ?? "?",
                crawledAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                unitsTotal = saveResult.UnitsFound,
                unitsSold = saveResult.SoldUnits,
                unitsAvailable = saveResult.AvailableUnits,
                unitsReserved = saveResult.ReservedUnits,
                unitsOption = saveResult.OptionUnits,
                soldPercentage = soldPct,
                minPrice = saveResult.MinPrice,
                maxPrice = saveResult.MaxPrice,
                averagePrice = saveResult.AveragePrice,
                minPricePerSqm = saveResult.MinPricePerSqm,
                maxPricePerSqm = saveResult.MaxPricePerSqm,
                averagePricePerSqm = saveResult.AveragePricePerSqm,
                minLivingArea = saveResult.MinLivingArea,
                maxLivingArea = saveResult.MaxLivingArea,
                averageLivingArea = saveResult.AverageLivingArea,
                houseCount = saveResult.HouseUnits,
                apartmentCount = saveResult.ApartmentUnits
            };

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var kpiPath = Path.Combine(kpiDir, $"project-{normalized.ExternalId}.json");
            await File.WriteAllTextAsync(kpiPath, JsonSerializer.Serialize(kpiOutput, opts), cancellationToken);
            Logger.LogInformation("[Immoweb] ProjectKPI → debug/kpi/project-{Id}.json", normalized.ExternalId);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] KPI debug-bestand schrijven mislukt: {Msg}", ex.Message);
        }
    }

    private static string? ExtractIdFromUrl(string url)
    {
        var m = UrlIdPattern.Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static ListingDto? TryParseFromScripts(string[] scripts, string classifiedId, string url)
    {
        // Zoek in elk script naar window.classified = {...} of window["classified"] = {...}
        foreach (var scriptText in scripts)
        {
            if (!scriptText.Contains(classifiedId)) continue;

            // Probeer window.classified-toewijzing te vinden
            var idx = scriptText.IndexOf("window.classified", StringComparison.Ordinal);
            if (idx < 0)
                idx = scriptText.IndexOf("\"classified\":", StringComparison.Ordinal);
            if (idx < 0)
                idx = scriptText.IndexOf("'classified':", StringComparison.Ordinal);
            if (idx < 0) continue;

            // Zoek het eerste '{' na de positie
            var braceStart = scriptText.IndexOf('{', idx);
            if (braceStart < 0) continue;

            // Extraheer het JSON-object (tellen van haakjes)
            var jsonStr = ExtractJsonObject(scriptText, braceStart);
            if (string.IsNullOrEmpty(jsonStr)) continue;

            var dto = ParseClassifiedJson(jsonStr, url);
            if (dto?.ExternalId == classifiedId) return dto;
        }

        return null;
    }

    private static string? ExtractJsonObject(string text, int startIndex)
    {
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = startIndex; i < Math.Min(text.Length, startIndex + 500_000); i++)
        {
            var c = text[i];

            if (escape) { escape = false; continue; }
            if (c == '\\' && inString) { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return text.Substring(startIndex, i - startIndex + 1);
            }
        }

        return null;
    }

    private async Task WriteDetailDebugFilesAsync(
        string url,
        string htmlContent,
        string[]? scripts,
        CancellationToken cancellationToken)
    {
        try
        {
            var debugDir = Path.Combine(AppContext.BaseDirectory, "debug");
            Directory.CreateDirectory(debugDir);

            await File.WriteAllTextAsync(
                Path.Combine(debugDir, "immoweb-detail.html"), htmlContent, cancellationToken);

            if (scripts is { Length: > 0 })
            {
                var scriptContent = $"# URL: {url}\n# Scripts: {scripts.Length}\n\n" +
                    string.Join("\n\n=== VOLGENDE SCRIPT ===\n\n", scripts.Select((s, i) =>
                        $"=== SCRIPT {i + 1} ({s.Length} chars) ===\n" + s[..Math.Min(s.Length, 20000)]));

                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, "immoweb-detail-scripts.txt"), scriptContent, cancellationToken);
            }

            Logger.LogInformation("[Immoweb] Debug detail-bestanden → debug/immoweb-detail.html + debug/immoweb-detail-scripts.txt");
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Detail debug-bestanden schrijven mislukt: {Error}", ex.Message);
        }
    }

    // ── JSON-parsers ───────────────────────────────────────────────────────────

    private static ListingDto? ParseClassifiedJson(string json, string url)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch { return null; }

        if (root is null) return null;

        var id = root["id"]?.GetValue<long?>();
        if (!id.HasValue) return null;

        var prop = root["property"];
        var location = prop?["location"];
        var building = prop?["building"];
        var energy = prop?["energy"];

        var rawType = prop?["type"]?.GetValue<string>();
        var rawSubType = prop?["subtype"]?.GetValue<string>();
        var city = location?["locality"]?.GetValue<string>() ?? location?["city"]?.GetValue<string>();
        var condition = building?["condition"]?.GetValue<string>() ?? string.Empty;

        // Prijs: directe verkoopprijs → mainValue (regulier) → minRangeValue (projectgroep) → huurprijs
        var price = TryGetDecimal(root["transaction"]?["sale"]?["price"])
                 ?? TryGetDecimal(root["price"]?["mainValue"])
                 ?? TryGetDecimal(root["price"]?["minRangeValue"])
                 ?? TryGetDecimal(root["transaction"]?["rental"]?["monthlyRent"]);

        // Parkeerplaatsen: som van indoor + outdoor + gesloten box
        var parkingTotal = (TryGetInt(prop?["parkingCountIndoor"]) ?? 0)
                         + (TryGetInt(prop?["parkingCountOutdoor"]) ?? 0)
                         + (TryGetInt(prop?["parkingCountClosedBox"]) ?? 0);

        // Developer/makelaar uit customers[0]
        var customers = root["customers"] as JsonArray;
        var dev = customers is { Count: > 0 } ? customers[0] : null;

        var description = prop?["description"]?.GetValue<string>();
        var features = AnalyzeDescription(description);

        // cluster.projectInfo.projectName — beschikbaar voor ProjectGroups
        var projectName = root["cluster"]?["projectInfo"]?["projectName"]?.GetValue<string>();

        // IsNewBuild detectie – prioriteitsketen
        bool isNewBuild;
        string? isNewBuildSource;
        if (TryGetBool(root?["flags"]?["isNewlyBuilt"]))
        { isNewBuild = true; isNewBuildSource = "flags.isNewlyBuilt"; }
        else if (TryGetBool(root?["flags"]?["isNewRealEstateProject"]))
        { isNewBuild = true; isNewBuildSource = "flags.isNewRealEstateProject"; }
        else if (TryGetBool(prop?["isFirstOccupation"]))
        { isNewBuild = true; isNewBuildSource = "property.isFirstOccupation"; }
        else if (url.Contains("isNewlyBuilt=true", StringComparison.OrdinalIgnoreCase))
        { isNewBuild = true; isNewBuildSource = "searchUrl"; }
        else if (condition.Contains("NEW_CONSTRUCTION", StringComparison.OrdinalIgnoreCase))
        { isNewBuild = true; isNewBuildSource = "condition.NEW_CONSTRUCTION"; }
        else if (!string.IsNullOrEmpty(description))
        {
            var descLower = description.ToLowerInvariant();
            if (descLower.Contains("nieuwbouw") || descLower.Contains("e-peil")
                || descLower.Contains("warmtepomp") || descLower.Contains(" ben ")
                || descLower.Contains("bijna-energieneutraal") || descLower.Contains("bijna energieneutraal"))
            { isNewBuild = true; isNewBuildSource = "descriptionFallback"; }
            else
            { isNewBuild = false; isNewBuildSource = null; }
        }
        else
        { isNewBuild = false; isNewBuildSource = null; }

        return new ListingDto
        {
            ExternalId = id.Value.ToString(),
            Url = url,
            Title = $"{rawType} in {city}",   // resolver verbetert dit in PropertyNormalizer
            ProjectName = projectName,
            PropertyTypeRaw = rawType,
            PropertySubTypeRaw = rawSubType,
            TransactionTypeRaw = root?["transaction"]?["type"]?.GetValue<string>() ?? "FOR_SALE",
            PostalCode = location?["postalCode"]?.GetValue<string>(),
            City = city,
            Street = location?["street"]?.GetValue<string>(),
            HouseNumber = location?["number"]?.GetValue<string>(),
            Floor = TryGetInt(building?["floorNumber"]) ?? TryGetInt(prop?["floor"]),
            UnitNumber = building?["unitNumber"]?.GetValue<string>() ?? prop?["unitNumber"]?.GetValue<string>(),
            Latitude = TryGetDecimal(location?["latitude"]),
            Longitude = TryGetDecimal(location?["longitude"]),
            AskingPrice = price,
            MaxPrice = TryGetDecimal(root?["price"]?["maxRangeValue"]),
            LivingArea = TryGetDecimal(prop?["netHabitableSurface"]) ?? TryGetDecimal(prop?["habitableSurface"]),
            LandArea = TryGetDecimal(prop?["land"]?["surface"]),
            TerraceArea = TryGetDecimal(prop?["terraceSurface"]),
            GardenArea = TryGetDecimal(prop?["gardenSurface"]),
            Bedrooms = TryGetInt(prop?["bedroomCount"]),
            Bathrooms = TryGetInt(prop?["bathroomCount"]),
            ShowerCount = TryGetInt(prop?["showerRoomCount"]),
            ToiletCount = TryGetInt(prop?["toiletCount"]),
            GarageCount = parkingTotal > 0 ? parkingTotal : null,
            ConstructionYear = TryGetInt(building?["constructionYear"]),
            EPCScore = TryGetDecimal(energy?["primaryEnergyConsumptionPerSqm"]),
            EPCLabelRaw = energy?["epcScoreClass"]?.GetValue<string>(),
            IsNewBuild = isNewBuild,
            IsNewBuildSource = isNewBuildSource,
            DeveloperName = dev?["name"]?.GetValue<string>(),
            DeveloperWebsite = dev?["website"]?.GetValue<string>(),
            DeveloperPhone = dev?["phoneNumber"]?.GetValue<string>(),
            EnergyFeatures = features.Count > 0 ? string.Join(",", features) : null,
            Description = description,
            RawJson = json.Length > 50000 ? json[..50000] : json
        };
    }

    private static ListingDto? ParseNextDataJson(string json, string url)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch { return null; }

        var classified = root?["props"]?["pageProps"]?["classified"]
                      ?? root?["props"]?["pageProps"]?["listing"]?["classified"];

        if (classified is null) return null;
        return ParseClassifiedJson(classified.ToJsonString(), url);
    }

    private static decimal? TryGetDecimal(JsonNode? node)
    {
        if (node is null) return null;
        try { return node.GetValue<decimal>(); }
        catch { return null; }
    }

    private static bool TryGetBool(JsonNode? node)
    {
        if (node is null) return false;
        try { return node.GetValue<bool>(); }
        catch { return false; }
    }

    private static int? TryGetInt(JsonNode? node)
    {
        if (node is null) return null;
        try { return node.GetValue<int>(); }
        catch { return null; }
    }

    // ── Beschrijvingsanalyse ───────────────────────────────────────────────────

    private static readonly (string Keyword, string Feature)[] DescriptionFeatureMap =
    [
        ("warmtepomp",             "Warmtepomp"),
        ("heat pump",              "Warmtepomp"),
        ("geothermie",             "Geothermie"),
        ("geothermal",             "Geothermie"),
        ("zonnepanelen",           "Zonnepanelen"),
        ("fotovoltaïsch",          "Zonnepanelen"),
        ("photovoltaï",            "Zonnepanelen"),
        ("vloerverwarming",        "Vloerverwarming"),
        ("ventilatiesysteem d",    "Ventilatie D"),
        ("ventilatie d",           "Ventilatie D"),
        (" wtw ",                  "Ventilatie D"),
        ("bijna-energieneutraal",  "BEN"),
        ("bijna energieneutraal",  "BEN"),
        (" ben ",                  "BEN"),
        ("e20 ",                   "E20"),
        ("epb e20",                "E20"),
        ("e25 ",                   "E25"),
        ("epb e25",                "E25"),
        ("e30 ",                   "E30"),
        ("epb e30",                "E30"),
    ];

    private static List<string> AnalyzeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return [];

        var lower = description.ToLowerInvariant();
        var found = new HashSet<string>();

        foreach (var (keyword, feature) in DescriptionFeatureMap)
            if (lower.Contains(keyword))
                found.Add(feature);

        return [.. found.Order()];
    }

    // ── Parser debug-bestand ──────────────────────────────────────────────────

    private async Task WriteParserDebugFileAsync(
        ListingDto dto, string rawJson, CancellationToken cancellationToken)
    {
        if (!Settings.Debug.Enabled) return;

        try
        {
            JsonNode? root;
            try { root = JsonNode.Parse(rawJson); } catch { root = null; }

            var prop   = root?["property"];
            var price  = root?["price"];
            var energy = prop?["energy"];

            var debugDir = Path.Combine(AppContext.BaseDirectory, "debug", "parser");
            Directory.CreateDirectory(debugDir);

            var output = new
            {
                externalId = dto.ExternalId,
                url = dto.Url,
                crawledAt = DateTime.UtcNow,
                mappedValues = new
                {
                    propertyType = dto.PropertyTypeRaw,
                    propertySubType = dto.PropertySubTypeRaw,
                    transactionType = dto.TransactionTypeRaw,
                    postalCode = dto.PostalCode,
                    city = dto.City,
                    street = dto.Street,
                    houseNumber = dto.HouseNumber,
                    floor = dto.Floor,
                    unitNumber = dto.UnitNumber,
                    askingPrice = dto.AskingPrice,
                    livingArea = dto.LivingArea,
                    landArea = dto.LandArea,
                    bedrooms = dto.Bedrooms,
                    bathrooms = dto.Bathrooms,
                    garageCount = dto.GarageCount,
                    constructionYear = dto.ConstructionYear,
                    epcScore = dto.EPCScore,
                    epcLabel = dto.EPCLabelRaw,
                    isNewBuild = dto.IsNewBuild,
                    isNewBuildSource = dto.IsNewBuildSource
                },
                jsonPathsUsed = new
                {
                    transactionSalePrice = root?["transaction"]?["sale"]?["price"]?.ToString(),
                    priceMainValue       = price?["mainValue"]?.ToString(),
                    priceMinRangeValue   = price?["minRangeValue"]?.ToString(),
                    priceMaxRangeValue   = price?["maxRangeValue"]?.ToString(),
                    netHabitableSurface  = prop?["netHabitableSurface"]?.ToString(),
                    habitableSurface     = prop?["habitableSurface"]?.ToString(),
                    landSurface          = prop?["land"]?["surface"]?.ToString(),
                    bedroomCount         = prop?["bedroomCount"]?.ToString(),
                    bathroomCount        = prop?["bathroomCount"]?.ToString(),
                    showerRoomCount      = prop?["showerRoomCount"]?.ToString(),
                    toiletCount          = prop?["toiletCount"]?.ToString(),
                    parkingCountIndoor   = prop?["parkingCountIndoor"]?.ToString(),
                    parkingCountOutdoor  = prop?["parkingCountOutdoor"]?.ToString(),
                    parkingCountClosedBox = prop?["parkingCountClosedBox"]?.ToString(),
                    gardenSurface        = prop?["gardenSurface"]?.ToString(),
                    terraceSurface       = prop?["terraceSurface"]?.ToString(),
                    constructionYear     = prop?["building"]?["constructionYear"]?.ToString(),
                    floorNumber          = prop?["building"]?["floorNumber"]?.ToString(),
                    epcScore             = energy?["primaryEnergyConsumptionPerSqm"]?.ToString(),
                    epcLabel             = energy?["epcScoreClass"]?.ToString()
                },
                descriptionFeatures = AnalyzeDescription(dto.Description),
                descriptionLength = dto.Description?.Length ?? 0
            };

            var opts = new JsonSerializerOptions { WriteIndented = true };
            var path = Path.Combine(debugDir, $"{dto.ExternalId}-parsed.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(output, opts), cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Parser debug-bestand schrijven mislukt: {Msg}", ex.Message);
        }

        await WriteFieldAvailabilityAsync(dto, rawJson, cancellationToken);
    }

    [GeneratedRegex(@"""(?:id|classifiedId|propertyId)""\s*:\s*(\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex ClassifiedIdPattern();

    // ── Field Availability Report ─────────────────────────────────────────────

    private sealed class FieldEntry
    {
        public bool Found { get; init; }
        public object? Value { get; init; }
        public string? Path { get; init; }
        public string[]? PathsTried { get; init; }
    }

    // Haal classified root op ongeacht wrapper-formaat (window.classified of __NEXT_DATA__)
    private static JsonNode? ExtractClassifiedRoot(JsonNode? root)
        => root?["property"] != null
            ? root
            : root?["props"]?["pageProps"]?["classified"]
              ?? root?["props"]?["pageProps"]?["listing"]?["classified"];

    private static FieldEntry ProbeFirst(params (string Path, object? Value)[] candidates)
    {
        foreach (var (path, value) in candidates)
            if (value is not null)
                return new FieldEntry { Found = true, Value = value, Path = path };
        return new FieldEntry { Found = false, PathsTried = candidates.Select(c => c.Path).ToArray() };
    }

    private static object? V(decimal? v) => v.HasValue ? (object)v.Value : null;
    private static object? V(int? v)     => v.HasValue ? (object)v.Value : null;
    private static object? V(bool? v)    => v.HasValue ? (object)v.Value : null;
    private static object? V(string? v)  => v;

    private static Dictionary<string, FieldEntry> BuildFieldReport(JsonNode? c)
    {
        var prop     = c?["property"];
        var building = prop?["building"];
        var energy   = prop?["energy"];
        var price    = c?["price"];
        var tx       = c?["transaction"];

        var indoor    = TryGetInt(prop?["parkingCountIndoor"])    ?? 0;
        var outdoor   = TryGetInt(prop?["parkingCountOutdoor"])   ?? 0;
        var closedBox = TryGetInt(prop?["parkingCountClosedBox"]) ?? 0;
        var parkingTotal = indoor + outdoor + closedBox;

        return new Dictionary<string, FieldEntry>
        {
            ["askingPrice"] = ProbeFirst(
                ("transaction.sale.price",                           V(TryGetDecimal(tx?["sale"]?["price"]))),
                ("price.mainValue",                                  V(TryGetDecimal(price?["mainValue"]))),
                ("price.minRangeValue",                              V(TryGetDecimal(price?["minRangeValue"]))),
                ("transaction.rental.monthlyRent",                   V(TryGetDecimal(tx?["rental"]?["monthlyRent"])))),

            ["pricePerSqm"] = ProbeFirst(
                ("transaction.sale.pricePerSqm",                     V(TryGetDecimal(tx?["sale"]?["pricePerSqm"])))),

            ["livingArea"] = ProbeFirst(
                ("property.netHabitableSurface",                     V(TryGetDecimal(prop?["netHabitableSurface"]))),
                ("property.habitableSurface",                        V(TryGetDecimal(prop?["habitableSurface"])))),

            ["landArea"] = ProbeFirst(
                ("property.land.surface",                            V(TryGetDecimal(prop?["land"]?["surface"])))),

            ["bedrooms"] = ProbeFirst(
                ("property.bedroomCount",                            V(TryGetInt(prop?["bedroomCount"])))),

            ["bathrooms"] = ProbeFirst(
                ("property.bathroomCount",                           V(TryGetInt(prop?["bathroomCount"])))),

            ["showerCount"] = ProbeFirst(
                ("property.showerRoomCount",                         V(TryGetInt(prop?["showerRoomCount"])))),

            ["toiletCount"] = ProbeFirst(
                ("property.toiletCount",                             V(TryGetInt(prop?["toiletCount"])))),

            ["floor"] = ProbeFirst(
                ("property.building.floorNumber",                    V(TryGetInt(building?["floorNumber"]))),
                ("property.floor",                                   V(TryGetInt(prop?["floor"])))),

            ["constructionYear"] = ProbeFirst(
                ("property.building.constructionYear",               V(TryGetInt(building?["constructionYear"])))),

            ["garageCount"] = parkingTotal > 0
                ? new FieldEntry { Found = true, Value = parkingTotal, Path = "SUM(property.parkingCountIndoor+Outdoor+ClosedBox)" }
                : new FieldEntry { Found = false, PathsTried = ["property.parkingCountIndoor", "property.parkingCountOutdoor", "property.parkingCountClosedBox"] },

            ["epcScore"] = ProbeFirst(
                ("property.energy.primaryEnergyConsumptionPerSqm",   V(TryGetDecimal(energy?["primaryEnergyConsumptionPerSqm"])))),

            ["epcLabel"] = ProbeFirst(
                ("property.energy.epcScoreClass",                    V(energy?["epcScoreClass"]?.GetValue<string>()))),

            ["epcLevel"] = ProbeFirst(
                ("property.energy.eLevel",                           V(energy?["eLevel"]?.GetValue<string>()))),

            ["heatingType"] = ProbeFirst(
                ("property.energy.heatingType",                      V(energy?["heatingType"]?.GetValue<string>()))),

            ["isLowEnergy"] = ProbeFirst(
                ("flags.isLowEnergy",                                V(c?["flags"]?["isLowEnergy"]?.GetValue<bool?>()))),

            ["isPassiveHouse"] = ProbeFirst(
                ("flags.isPassiveHouse",                             V(c?["flags"]?["isPassiveHouse"]?.GetValue<bool?>()))),

            ["terraceArea"] = ProbeFirst(
                ("property.terraceSurface",                          V(TryGetDecimal(prop?["terraceSurface"])))),

            ["hasTerrace"] = ProbeFirst(
                ("property.hasTerrace",                              V(prop?["hasTerrace"]?.GetValue<bool?>()))),

            ["gardenArea"] = ProbeFirst(
                ("property.gardenSurface",                           V(TryGetDecimal(prop?["gardenSurface"])))),

            ["hasGarden"] = ProbeFirst(
                ("property.hasGarden",                               V(prop?["hasGarden"]?.GetValue<bool?>()))),
        };
    }

    private static Dictionary<string, object?> BuildProjectFieldReport(
        JsonNode? root, IReadOnlyList<ProjectGroupUnitDto> units)
    {
        var classified  = ExtractClassifiedRoot(root);
        var cluster     = classified?["cluster"]
                       ?? root?["props"]?["pageProps"]?["classified"]?["cluster"];
        var projectInfo = cluster?["projectInfo"];

        return new Dictionary<string, object?>
        {
            ["projectName"] = new FieldEntry
            {
                Found = projectInfo?["projectName"] != null,
                Value = V(projectInfo?["projectName"]?.GetValue<string>()),
                Path  = "cluster.projectInfo.projectName"
            },
            ["soldPercentage"] = new FieldEntry
            {
                Found = projectInfo?["soldPercentage"] != null,
                Value = V(TryGetInt(projectInfo?["soldPercentage"])),
                Path  = "cluster.projectInfo.soldPercentage"
            },
            ["unitsDetected"] = new FieldEntry
            {
                Found = units.Count > 0,
                Value = units.Count > 0 ? (object)units.Count : null,
                Path  = "cluster.units[].items[]"
            },
            ["unitFieldSummary"] = units.Count == 0 ? null : (object)new
            {
                saleStatus   = new { found = units.Count(u => u.SaleStatus != SaleStatus.Unknown), total = units.Count, path = "cluster.units[].items[].saleStatus" },
                price        = new { found = units.Count(u => u.Price.HasValue),        total = units.Count, path = "cluster.units[].items[].price" },
                surface      = new { found = units.Count(u => u.Surface.HasValue),      total = units.Count, path = "cluster.units[].items[].surface" },
                bedroomCount = new { found = units.Count(u => u.BedroomCount.HasValue), total = units.Count, path = "cluster.units[].items[].bedroomCount" },
                floor        = new { found = units.Count(u => u.Floor.HasValue),        total = units.Count, path = "cluster.units[].items[].floor" },
            }
        };
    }

    private async Task WriteFieldAvailabilityAsync(
        ListingDto dto, string rawJson, CancellationToken ct)
    {
        if (!Settings.Debug.Enabled) return;
        try
        {
            JsonNode? root;
            try { root = JsonNode.Parse(rawJson); } catch { root = null; }

            var classified = ExtractClassifiedRoot(root);
            var fields     = BuildFieldReport(classified);

            // In-memory summary bijwerken
            foreach (var (name, entry) in fields)
            {
                _fieldSummary.TryGetValue(name, out var cur);
                _fieldSummary[name] = entry.Found
                    ? (cur.Found + 1, cur.Missing)
                    : (cur.Found, cur.Missing + 1);
            }

            // Project units (reeds gevuld door HandleProjectGroupAsync)
            _pendingProjectUnits.TryGetValue(dto.ExternalId ?? string.Empty, out var units);
            var isProjectGroup = units is { Count: > 0 }
                || (dto.PropertyTypeRaw?.Contains("GROUP", StringComparison.OrdinalIgnoreCase) ?? false);

            var report = new
            {
                externalId     = dto.ExternalId,
                type           = dto.PropertyTypeRaw ?? "Unknown",
                isProjectGroup,
                fields,
                projectFields  = isProjectGroup && units is { Count: > 0 }
                    ? BuildProjectFieldReport(root, units)
                    : null
            };

            var opts = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var debugDir = Path.Combine(AppContext.BaseDirectory, "debug", "parser");
            Directory.CreateDirectory(debugDir);

            var listingPath = Path.Combine(debugDir, $"{dto.ExternalId}-field-availability.json");
            await File.WriteAllTextAsync(listingPath, JsonSerializer.Serialize(report, opts), ct);

            // Samenvattingsbestand — running total, overschreven na elke listing
            var summary = _fieldSummary.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)new { found = kvp.Value.Found, missing = kvp.Value.Missing });
            var summaryPath = Path.Combine(debugDir, "field-availability-summary.json");
            await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary, opts), ct);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Immoweb] Field-availability bestand schrijven mislukt: {Msg}", ex.Message);
        }
    }
}

// ── Value object voor onderschepte responses ───────────────────────────────────
internal record CapturedResponse(string Url, string Method, int Status, string ContentType, string Body);
