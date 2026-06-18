using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
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

/// <summary>
/// Zimmo crawler — twee-traps strategie:
///
///   Fase 1 (zoekpagina's):
///     Per .property-item wordt een search-card aangemaakt via DOM-extractie.
///     __NEXT_DATA__ wordt als aanvulling gebruikt (lat/lon, EPC, etc.) als het beschikbaar is.
///     Alle cards worden gecached op ExternalId.
///
///   Fase 2 (detailpagina's):
///     - Gewone listings (huis/appartement): direct uit search-card cache → ListingDto.
///     - Nieuwbouwprojecten: detailpagina openen als OpenProjectDetailPages=true.
///       Bij Cloudflare: fallback naar search-card.
/// </summary>
public class ZimmoCrawler : BaseCrawler
{
    private readonly PlaywrightBrowserService _browser;

    private readonly Dictionary<string, LocationContext> _urlContext      = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ListingDto>       _searchCardCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string>                       _projectExternalIds  = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ProjectGroupUnitDto>> _pendingProjectUnits = new();

    private int _projectDetailPagesOpened;
    private int _projectDetailPagesFailed;
    private int _projectDetailPagesSkipped;
    private int _projectUnitsFound;
    private int _projectDebugSaved;

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

    protected override SourceSettings? GetCurrentSourceSettings() => GetSourceSettings();

    private bool OpenProjectDetailPages         => GetSourceSettings().OpenProjectDetailPages;
    private bool OpenDetailPagesForLooseListings => GetSourceSettings().OpenDetailPagesForLooseListings;
    private int  MaxProjectDetailPagesPerRun    => GetSourceSettings().MaxProjectDetailPagesPerRun;

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

    // Geen rate-limit op listing-loop-niveau (cache-lookups zijn instant).
    // Rate-limit wordt per netwerkoproep toegepast in LoadSearchPageAsync / FetchDetailPageAsync.
    protected override Task ApplyRateLimitAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    // ── Fase 1: zoek-URL's genereren ─────────────────────────────────────────

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        _urlContext.Clear();
        _searchCardCache.Clear();
        _projectExternalIds.Clear();
        _pendingProjectUnits.Clear();
        _projectDetailPagesOpened = 0;
        _projectDetailPagesFailed = 0;
        _projectDetailPagesSkipped = 0;
        _projectUnitsFound = 0;
        _projectDebugSaved = 0;

        var src = GetSourceSettings();
        var locations = src.AllowedLocations.Count > 0
            ? (IReadOnlyList<LocationSettings>)src.AllowedLocations
            : DefaultLocations;
        var maxPages = src.MaxSearchPagesPerLocation > 0 ? src.MaxSearchPagesPerLocation : 5;

        Logger.LogInformation(
            "[Zimmo] {Count} locatie(s) | MaxPagesPerLocatie={Max} | " +
            "OpenProjectDetailPages={ProjDetail} | OpenDetailPagesForLooseListings={LooseDetail}",
            locations.Count, maxPages, OpenProjectDetailPages, OpenDetailPagesForLooseListings);

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

    // ── Fase 1b: paginatie per locatie ────────────────────────────────────────

    protected override async Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
    {
        _urlContext.TryGetValue(searchPageUrl, out var ctx);
        ctx ??= new LocationContext("", "?", null, 5);

        var seenExternalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allUrls         = new List<string>();
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

            if (currentPageUrl is not null && pageNum < ctx.MaxPages)
                await base.ApplyRateLimitAsync(cancellationToken);
        }

        var projectsForCity = allUrls
            .Select(u => ZimmoListingParser.ExtractExternalIdFromUrl(u.Split('?')[0]))
            .Count(id => id is not null && _projectExternalIds.Contains(id!));

        Logger.LogInformation(
            "[Zimmo] {City} ({PostalCode}) — {Count} listing-URL's | " +
            "{Cards} search-cards in cache | {Projects} projecten | {Loose} gewone listings",
            ctx.City, ctx.PostalCode,
            allUrls.Count,
            _searchCardCache.Count,
            projectsForCity,
            _searchCardCache.Count - projectsForCity);

        return allUrls;
    }

    // ── Kern: één zoekpagina laden en cards cachen ───────────────────────────

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
                    Timeout   = EffectivePlaywrightTimeoutMs
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

            // Listing-URL's uit DOM
            var rawHrefs = await page.EvaluateAsync<string[]>(
                "() => Array.from(document.querySelectorAll('.property-item a.property-item_link[href]'))" +
                ".map(a => a.getAttribute('href') || '').filter(h => h.length > 0)") ?? [];

            const string ZimmoBase = "https://www.zimmo.be";
            var listingUrls = rawHrefs
                .Select(h => h.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? h : ZimmoBase + h)
                .GroupBy(u => u.Split('?')[0], StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            Logger.LogInformation(
                "[Zimmo] {City} p{N}: HTTP {Status} | {Count} listing-URL's",
                city, pageNum, httpStatus, listingUrls.Count);

            // ── Search-cards: DOM-extractie (primair) ─────────────────────────
            var domCards    = await ExtractDomCardsAsync(page, city, pageNum);
            var cardsAdded  = 0;
            var projectsAdded = 0;

            foreach (var (extId, dto) in domCards)
            {
                _searchCardCache[extId] = dto;
                cardsAdded++;
                if (IsProjectCard(dto))
                {
                    _projectExternalIds.Add(extId);
                    projectsAdded++;
                }
            }

            // ── Search-cards: __NEXT_DATA__ (aanvulling — overschrijft DOM-cards met rijkere data)
            var nextDataJson = await page.EvaluateAsync<string?>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); " +
                "return el ? el.textContent : null; }");

            if (!string.IsNullOrEmpty(nextDataJson))
            {
                var ndCards = ZimmoSearchCardParser.ParseSearchCards(nextDataJson, Logger);
                foreach (var card in ndCards)
                {
                    if (!string.IsNullOrEmpty(card.ExternalId))
                    {
                        _searchCardCache[card.ExternalId] = card;
                        if (IsProjectCard(card) && _projectExternalIds.Add(card.ExternalId))
                            projectsAdded++;
                    }
                }
                Logger.LogDebug(
                    "[Zimmo] {City} p{N}: {Count} __NEXT_DATA__ cards (aanvulling)",
                    city, pageNum, ndCards.Count);
            }

            Logger.LogInformation(
                "[Zimmo] {City} p{N}: {Dom} DOM-cards gecached | {Projects} projecten | " +
                "{Loose} gewone listings | cache-totaal={Total}",
                city, pageNum, cardsAdded, projectsAdded,
                cardsAdded - projectsAdded, _searchCardCache.Count);

            if (cardsAdded != listingUrls.Count)
                Logger.LogWarning(
                    "[Zimmo] {City} p{N}: MISMATCH — {Urls} listing-URL's vs {Cards} cards gecached " +
                    "(verschil={Diff}). Mogelijke oorzaak: promo-blokken gefilterd of ExternalId ontbreekt.",
                    city, pageNum, listingUrls.Count, cardsAdded, listingUrls.Count - cardsAdded);

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

    // ── DOM-gebaseerde search-card extractie ──────────────────────────────────

    private async Task<IReadOnlyList<(string ExternalId, ListingDto Dto)>> ExtractDomCardsAsync(
        IPage page, string city, int pageNum)
    {
        // data-code op .property-item is de betrouwbaarste bron voor ExternalId.
        // data-project="1" op de save-knop geeft het type direct.
        // Adres staat in .property-item_address met <br> als regelscheider.
        // Opp en slaapkamers staan in .opp-icon/.bedroom-icon spans.
        const string js = """
            () => Array.from(document.querySelectorAll('.property-item[data-code]')).map(item => {
                const code = (item.getAttribute('data-code') || '').trim();
                const link = item.querySelector('a.property-item_link');
                const href = link ? (link.getAttribute('href') || '').trim() : '';

                const saveBtn = item.querySelector('a.property-item_save-property[data-project]');
                const isProject = saveBtn ? saveBtn.getAttribute('data-project') === '1' : false;

                const priceEl   = item.querySelector('.property-item_price');
                const priceText = priceEl ? (priceEl.textContent || '').replace(/\s+/g, ' ').trim() : '';

                const addrEl   = item.querySelector('.property-item_address');
                const addrLines = addrEl
                    ? (addrEl.innerHTML || '')
                        .replace(/<br\s*\/?>/gi, '\n')
                        .replace(/<[^>]+>/g, '')
                        .split('\n')
                        .map(s => s.replace(/\s+/g, ' ').trim())
                        .filter(Boolean)
                    : [];

                const stickerEl   = item.querySelector('.property-item_sticker.__project, .property-item_sticker');
                const stickerText = stickerEl ? (stickerEl.textContent || '').trim() : '';

                const oppEl   = item.querySelector('.opp-icon.property-item_icon, [class*="opp-icon"]');
                const oppText = oppEl ? (oppEl.textContent || '').replace(/\s+/g, ' ').trim() : '';

                const bedEl   = item.querySelector('.bedroom-icon.property-item_icon, [class*="bedroom-icon"]');
                const bedText = bedEl ? (bedEl.textContent || '').replace(/\s+/g, ' ').trim() : '';

                return { code, href, isProject, priceText, addrLines, stickerText, oppText, bedText };
            })
            """;

        JsonElement raw;
        try
        {
            raw = await page.EvaluateAsync<JsonElement>(js);
        }
        catch (Exception ex)
        {
            Logger.LogWarning("[Zimmo] DOM card extractie fout {City} p{N}: {Msg}", city, pageNum, ex.Message);
            return [];
        }

        if (raw.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<(string, ListingDto)>();

        foreach (var item in raw.EnumerateArray())
        {
            static string S(JsonElement el, string key) =>
                el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String
                    ? p.GetString() ?? "" : "";

            static bool B(JsonElement el, string key) =>
                el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.True;

            var code = S(item, "code");
            if (string.IsNullOrEmpty(code)) continue;

            var href = S(item, "href");
            if (string.IsNullOrEmpty(href)) continue;

            if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                href = "https://www.zimmo.be" + href;
            var canonicalUrl = href.Split('?')[0].TrimEnd('/') + "/";

            var isProject   = B(item, "isProject");
            var priceText   = S(item, "priceText");
            var stickerText = S(item, "stickerText");
            var oppText     = S(item, "oppText");
            var bedText     = S(item, "bedText");

            // addrLines: ["Albert Biesbrouckstraat 29", "8800 Roeselare"]
            string[] addrLines = [];
            if (item.TryGetProperty("addrLines", out var addrProp) && addrProp.ValueKind == JsonValueKind.Array)
                addrLines = [.. addrProp.EnumerateArray().Select(a => a.GetString() ?? "").Where(a => a.Length > 0)];

            // Alleen echte te-koop listings — promo-blokken/advertenties overslaan
            if (!IsValidListingUrl(canonicalUrl))
            {
                Logger.LogDebug(
                    "[ZimmoCardFilter] Overgeslagen — geen geldige listing-URL: code={Code} | url={Url}",
                    code, canonicalUrl);
                continue;
            }

            // Type — data-project is leading; URL als bevestiging/aanvulling
            string? zimmoType = null;
            if (isProject || canonicalUrl.Contains("/nieuwbouwproject/", StringComparison.OrdinalIgnoreCase))
                zimmoType = "PROJECT";
            else if (canonicalUrl.Contains("/appartement/", StringComparison.OrdinalIgnoreCase))
                zimmoType = "APARTMENT";
            else if (canonicalUrl.Contains("/woning/",  StringComparison.OrdinalIgnoreCase)
                  || canonicalUrl.Contains("/huis/",    StringComparison.OrdinalIgnoreCase)
                  || canonicalUrl.Contains("/villa/",   StringComparison.OrdinalIgnoreCase))
                zimmoType = "HOUSE";

            var price = ParseCardPrice(priceText);
            var (postalCode, cardCity, street, houseNumber) = ParseStructuredAddress(addrLines);
            var area     = ParseAreaText(oppText);
            var bedrooms = ParseBedroomsText(bedText);

            var dto = ZimmoDtoMapper.Build(
                externalId          : code,
                url                 : canonicalUrl,
                title               : null,
                zimmoPropertyType   : zimmoType,
                zimmoPropertySubType: null,
                zimmoTransactionType: "FOR_SALE",
                postalCode          : postalCode,
                city                : cardCity,
                street              : street,
                houseNumber         : houseNumber,
                latitude            : null,
                longitude           : null,
                askingPrice         : price,
                maxPrice            : null,
                livingArea          : area,
                landArea            : null,
                bedrooms            : bedrooms,
                bathrooms           : null,
                epcLabel            : null,
                epcScore            : null,
                isNewConstruction   : zimmoType == "PROJECT" ? true : null,
                developerName       : null,
                developerPhone      : null,
                developerWebsite    : null,
                projectName         : null,
                description         : null,
                rawJson             : null);

            Logger.LogDebug(
                "[ZimmoDomCard] {Id} | {Type} | {PostalCode} {City} | €{Price} | {Area}m² | {Beds}k | sticker={Sticker}",
                code, zimmoType ?? "onbekend",
                postalCode ?? "?", cardCity ?? "?",
                price?.ToString("N0") ?? "?",
                area?.ToString() ?? "?",
                bedrooms?.ToString() ?? "?",
                stickerText);

            result.Add((code, dto));
        }

        Logger.LogDebug("[Zimmo] DOM extractie: {Count} cards van pagina", result.Count);
        return result;
    }

    // ── Parseer-helpers (statisch) ────────────────────────────────────────────

    private static decimal? ParseCardPrice(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        // Belgisch: € 295.000 of €295.000 of €1.200.000
        var m = Regex.Match(text, @"€\s*([\d\s\.,]+)");
        if (!m.Success) return null;
        // Punt = duizendscheider, komma = decimaalteken in BE
        var raw = m.Groups[1].Value.Trim()
            .Replace(" ", "")
            .Replace(".", "")   // verwijder duizendscheiders
            .Replace(",", "."); // maak decimaalteken
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var p)
            && p > 10_000m && p < 100_000_000m
            ? p : null;
    }

    // Gestructureerd adres: regel 0 = "Straatnaam 29", regel 1 = "8800 Gemeente"
    private static (string? postalCode, string? city, string? street, string? houseNumber) ParseStructuredAddress(
        string[] lines)
    {
        if (lines.Length == 0) return (null, null, null, null);

        string? postalCode = null, cardCity = null, street = null, houseNumber = null;

        // Zoek de regel met postcode (4 cijfers aan het begin of ergens in de tekst)
        string? addrLine = null;
        foreach (var line in lines)
        {
            var m = Regex.Match(line, @"^(\d{4})\s+(.+)$");
            if (m.Success)
            {
                postalCode = m.Groups[1].Value;
                cardCity   = m.Groups[2].Value.Trim();
                break;
            }
        }

        // Straatlijn: de regel die NIET de postcode bevat
        foreach (var line in lines)
        {
            if (!Regex.IsMatch(line, @"^\d{4}\s")) { addrLine = line; break; }
        }

        if (!string.IsNullOrEmpty(addrLine))
        {
            var sm = Regex.Match(addrLine, @"^(.*?)\s+(\d+\w*)\s*$");
            if (sm.Success)
            {
                street      = sm.Groups[1].Value.Trim();
                houseNumber = sm.Groups[2].Value.Trim();
            }
            else
            {
                street = addrLine;
            }
        }

        return (postalCode, cardCity, street, houseNumber);
    }

    private static decimal? ParseAreaText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var m = Regex.Match(text, @"(\d+(?:[,\.]\d+)?)\s*m[²2]");
        if (!m.Success) return null;
        var raw = m.Groups[1].Value.Replace(",", ".");
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var a)
            && a > 5m && a < 100_000m ? a : null;
    }

    private static int? ParseBedroomsText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        // bedText is bijv. " 3" of "-"; enkel het cijfer bewaren
        var m = Regex.Match(text, @"\b(\d+)\b");
        return m.Success && int.TryParse(m.Groups[1].Value, out var b) && b > 0 && b < 20 ? b : null;
    }

    // ── Fase 2: listing parsen ────────────────────────────────────────────────

    protected override async Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken)
    {
        var navigationUrl = listingUrl;
        var canonicalUrl  = listingUrl.Split('?')[0];
        var externalId    = ZimmoListingParser.ExtractExternalIdFromUrl(canonicalUrl);

        var isProject = IsProjectForListing(externalId, canonicalUrl);

        // ── Projectdetailpagina ───────────────────────────────────────────────
        if (isProject && OpenProjectDetailPages)
        {
            if (MaxProjectDetailPagesPerRun > 0 && _projectDetailPagesOpened >= MaxProjectDetailPagesPerRun)
            {
                Logger.LogDebug(
                    "[Zimmo] MaxProjectDetailPagesPerRun={Max} bereikt — overgeslagen: {Url}",
                    MaxProjectDetailPagesPerRun, canonicalUrl);
                _projectDetailPagesSkipped++;
                return GetFromSearchCardCache(externalId, navigationUrl, canonicalUrl);
            }

            // SearchCard-info voor diagnostiek
            _searchCardCache.TryGetValue(externalId ?? "", out var searchCard);
            var projectDetailUrl = BuildProjectDetailUrl(externalId, canonicalUrl);
            _projectDetailPagesOpened++;

            Logger.LogInformation(
                "[Zimmo] ProjectDetail openen ({N}{Max}) | ExternalId={Id} | SearchCard.Nav={Nav} | " +
                "SearchCard.Canonical={Canonical} | BuiltUrl={Built}",
                _projectDetailPagesOpened,
                MaxProjectDetailPagesPerRun > 0 ? $"/{MaxProjectDetailPagesPerRun}" : "",
                externalId ?? "?",
                searchCard?.NavigationUrl ?? canonicalUrl,
                searchCard?.CanonicalUrl  ?? canonicalUrl,
                projectDetailUrl);

            var saveDebug = _projectDebugSaved < 3;
            if (saveDebug) _projectDebugSaved++;

            var (detailDto, failureReason) = await FetchDetailPageAsync(
                projectDetailUrl, projectDetailUrl, cancellationToken,
                projectExternalId: externalId, saveDebugFiles: saveDebug);

            if (detailDto is not null)
            {
                ApplyTitleFallback(detailDto, SourceName);
                return detailDto;
            }

            Logger.LogWarning(
                "[Zimmo] ProjectDetailFailed | Reason={Reason} | ExternalId={Id} | Url={Url}",
                failureReason, externalId ?? "?", projectDetailUrl);
            _projectDetailPagesFailed++;
            return GetFromSearchCardCache(externalId, navigationUrl, canonicalUrl);
        }

        // ── Losse listings met optionele detail-navigatie ─────────────────────
        if (!isProject && OpenDetailPagesForLooseListings)
        {
            var (lDto, _) = await FetchDetailPageAsync(navigationUrl, canonicalUrl, cancellationToken);
            if (lDto is not null) ApplyTitleFallback(lDto, SourceName);
            return lDto;
        }

        // ── Standaard: search-card cache ──────────────────────────────────────
        return GetFromSearchCardCache(externalId, navigationUrl, canonicalUrl);
    }

    private ListingDto? GetFromSearchCardCache(
        string? externalId, string navigationUrl, string canonicalUrl)
    {
        if (externalId is not null && _searchCardCache.TryGetValue(externalId, out var cached))
        {
            cached.NavigationUrl = navigationUrl;
            cached.CanonicalUrl  = canonicalUrl;
            ApplyTitleFallback(cached, SourceName);
            LogParsedListing(cached, "Mapped from search-card");
            return cached;
        }

        Logger.LogDebug(
            "[Zimmo] ExternalId '{Id}' niet in search-card cache — listing overgeslagen.",
            externalId ?? "?");
        return null;
    }

    private static void ApplyTitleFallback(ListingDto dto, string sourceName)
    {
        var t = dto.Title;
        if (!string.IsNullOrWhiteSpace(t) && t != "- -" && t != "?" && t != "-")
            return;

        // 1. Projectnaam
        if (!string.IsNullOrWhiteSpace(dto.ProjectName))
        { dto.Title = dto.ProjectName; return; }

        // 2. Straat + gemeente
        if (!string.IsNullOrWhiteSpace(dto.Street) && !string.IsNullOrWhiteSpace(dto.City))
        {
            dto.Title = string.IsNullOrWhiteSpace(dto.HouseNumber)
                ? $"{dto.Street}, {dto.City}"
                : $"{dto.Street} {dto.HouseNumber}, {dto.City}";
            return;
        }

        // 3. Type te koop in Gemeente (Postcode)
        var typeLabel = dto.PropertyTypeRaw?.ToUpperInvariant() switch
        {
            "HOUSE"     => "Huis",
            "APARTMENT" => "Appartement",
            "PROJECT"   => "Nieuwbouwproject",
            "LAND"      => "Grond",
            "GARAGE"    => "Garage",
            _           => "Vastgoed"
        };
        if (!string.IsNullOrWhiteSpace(dto.City))
        {
            dto.Title = string.IsNullOrWhiteSpace(dto.PostalCode)
                ? $"{typeLabel} te koop in {dto.City}"
                : $"{typeLabel} te koop in {dto.City} ({dto.PostalCode})";
            return;
        }

        // 4. Bron + ExternalId als laatste redmiddel
        dto.Title = $"{sourceName} {dto.ExternalId}";
    }

    private async Task<(ListingDto? Dto, string FailureReason)> FetchDetailPageAsync(
        string navigationUrl, string canonicalUrl, CancellationToken ct,
        string? projectExternalId = null, bool saveDebugFiles = false)
    {
        await base.ApplyRateLimitAsync(ct);

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
                    Timeout   = EffectivePlaywrightTimeoutMs
                });
            }
            catch (PlaywrightException ex)
            {
                Logger.LogWarning("[Zimmo] Detail GotoAsync fout: {Msg}", ex.Message);
            }

            await page.WaitForTimeoutAsync(1500);

            var httpStatus  = navResponse?.Status ?? 0;
            var finalUrl    = page.Url;
            var pageTitle   = await page.TitleAsync();
            var bodyText    = await page.EvaluateAsync<string>(
                "() => (document.body?.innerText ?? '').slice(0, 10000)");
            var bodySnip500 = bodyText.Length > 500 ? bodyText[..500] : bodyText;
            var html        = await page.ContentAsync();
            var htmlBytes   = System.Text.Encoding.UTF8.GetByteCount(html);

            var isCloudflare      = IsChallengePage(httpStatus, pageTitle, bodyText);
            var hasPropertyContent = HasRealPropertyContent(bodyText);

            // Debug-bestanden opslaan voor eerste N projecten
            if (saveDebugFiles && !string.IsNullOrEmpty(projectExternalId))
            {
                var nextDataForDebug = await page.EvaluateAsync<string?>(
                    "() => { const el = document.getElementById('__NEXT_DATA__'); return el ? el.textContent : null; }");
                await WriteProjectDebugAsync(projectExternalId, canonicalUrl, finalUrl,
                    httpStatus, pageTitle, bodyText, html, nextDataForDebug,
                    isCloudflare, hasPropertyContent, page, ct);
            }

            if (isCloudflare && !hasPropertyContent)
            {
                Logger.LogWarning(
                    "[Zimmo] CloudflareBlocked | HTTP={Status} | Title='{Title}' | " +
                    "FinalUrl={FinalUrl} | HtmlBytes={Bytes} | Body500='{Body}'",
                    httpStatus, pageTitle, finalUrl, htmlBytes,
                    bodySnip500.Replace('\n', ' '));
                return (null, "CloudflareBlocked");
            }

            if (isCloudflare && hasPropertyContent)
            {
                Logger.LogWarning(
                    "[Zimmo] CloudflareDetectedButContentPresent — doorgaan met parsen | " +
                    "HTTP={Status} | Title='{Title}' | FinalUrl={FinalUrl}",
                    httpStatus, pageTitle, finalUrl);
            }
            else
            {
                Logger.LogDebug(
                    "[Zimmo] DetailPageLoaded | HTTP={Status} | Title='{Title}' | FinalUrl={FinalUrl} | " +
                    "HtmlBytes={Bytes} | HasContent={HasContent}",
                    httpStatus, pageTitle, finalUrl, htmlBytes, hasPropertyContent);
            }

            // DOM-extractie voor projectpagina's vóór cookie-consent:
            // Na de consent-click herrendert Angular de pagina waardoor selectors tijdelijk verdwijnen.
            // De pre-consent DOM bevat wél alle projectdata → hier evalueren, niet pas na consent.
            var isProjectUrl = IsProjectUrl(canonicalUrl);
            if (isProjectUrl && hasPropertyContent)
            {
                var earlyDomDto = await ParseProjectPageFromDomAsync(page, canonicalUrl, projectExternalId);
                if (earlyDomDto is not null)
                {
                    earlyDomDto.NavigationUrl = navigationUrl;
                    LogParsedListing(earlyDomDto, "DOM (project)");
                    return (earlyDomDto, "Ok");
                }
            }

            await HandleCookieConsentAsync(page);

            // Na cookie-consent kan Zimmo een redirect triggeren. Wachten tot de DOM stabiel is.
            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 8000 });
            }
            catch { /* timeout is oké — doorgaan */ }

            var nextDataJson = await page.EvaluateAsync<string?>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); " +
                "return el ? el.textContent : null; }");


            if (!string.IsNullOrEmpty(nextDataJson))
            {
                ListingDto? dto = null;

                if (isProjectUrl)
                    dto = ZimmoProjectParser.TryParseNextData(nextDataJson, canonicalUrl, Logger);

                dto ??= ZimmoListingParser.TryParseNextData(nextDataJson, canonicalUrl, Logger);

                if (dto is not null)
                {
                    dto.NavigationUrl = navigationUrl;
                    LogParsedListing(dto, isProjectUrl ? "__NEXT_DATA__ (project)" : "__NEXT_DATA__");

                    if (isProjectUrl || dto.PropertyTypeRaw == "PROJECT")
                    {
                        var units = ZimmoProjectParser.ParseUnitsFromJson(
                            nextDataJson, dto.ExternalId, dto.ProjectName, Logger);
                        if (units.Count > 0)
                        {
                            _pendingProjectUnits[dto.ExternalId] = units;
                            Logger.LogInformation(
                                "[Zimmo] ProjectDetail: {Count} units geparsed voor project {Id}",
                                units.Count, dto.ExternalId);
                        }
                        else if (isProjectUrl)
                        {
                            Logger.LogWarning(
                                "[Zimmo] NoProjectUnitsFound | ExternalId={Id} | Url={Url} | " +
                                "nextDataJson-length={Len}",
                                dto.ExternalId, canonicalUrl, nextDataJson.Length);
                        }
                    }

                    return (dto, "Ok");
                }

                // __NEXT_DATA__ aanwezig maar parser kon er niets mee
                Logger.LogWarning(
                    "[Zimmo] PageLoadedButParserFailed | __NEXT_DATA__ aanwezig ({Len} bytes) " +
                    "maar geen dto | ExternalId={Id} | Url={Url} | HasContent={HasContent} | " +
                    "Body500='{Body}'",
                    nextDataJson.Length, projectExternalId ?? "?", canonicalUrl,
                    hasPropertyContent, bodySnip500.Replace('\n', ' '));
            }
            else
            {
                Logger.LogWarning(
                    "[Zimmo] PageLoadedButNoNextData | ExternalId={Id} | Url={Url} | " +
                    "ApiCalls={ApiCalls} | HtmlBytes={Bytes} | HasContent={HasContent}",
                    projectExternalId ?? "?", canonicalUrl,
                    capturedApiUrls.Count, htmlBytes, hasPropertyContent);
            }

            var jsonLdDto = ZimmoListingParser.TryParseJsonLd(html, canonicalUrl, Logger);
            if (jsonLdDto is not null)
            {
                jsonLdDto.NavigationUrl = navigationUrl;
                LogParsedListing(jsonLdDto, "JSON-LD");
                return (jsonLdDto, "Ok");
            }

            if (!saveDebugFiles)
                await WriteDetailDebugAsync(canonicalUrl, html, nextDataJson, page, ct);

            Logger.LogWarning(
                "[Zimmo] ParserFailed | ExternalId={Id} | Url={Url} | " +
                "HasContent={HasContent} | HtmlBytes={Bytes}",
                projectExternalId ?? "?", canonicalUrl, hasPropertyContent, htmlBytes);
            return (null, hasPropertyContent ? "PageLoadedButParserFailed" : "PageLoadedNoContent");
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout"))
        {
            Logger.LogWarning("[Zimmo] Timeout bij detail | ExternalId={Id} | Url={Url}",
                projectExternalId ?? "?", navigationUrl);
            return (null, "Timeout");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[Zimmo] Fout bij detail | ExternalId={Id} | Url={Url}",
                projectExternalId ?? "?", navigationUrl);
            return (null, "Exception");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    // ── AfterPersistAsync: project-units opslaan ──────────────────────────────

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

        _projectUnitsFound += result.UnitsFound;

        Logger.LogInformation(
            "[Zimmo] ProjectGroupSaved | AssetId={AssetId} | ExternalId={Id} | " +
            "Units={Total} (nieuw={New}, bijgewerkt={Upd}) | " +
            "Beschikbaar={Avail} | Verkocht={Sold} | Gereserveerd={Res} | " +
            "TotaalUnitsDezeRun={RunTotal}",
            assetId.Value, normalized.ExternalId,
            result.UnitsFound, result.UnitsCreated, result.UnitsUpdated,
            result.AvailableUnits, result.SoldUnits, result.ReservedUnits,
            _projectUnitsFound);

        if (_projectDetailPagesOpened > 0)
            Logger.LogInformation(
                "[Zimmo] ProjectDetail-samenvatting: geopend={Opened} | geslaagd={Ok} | " +
                "mislukt={Failed} | overgeslagen={Skipped} | units={Units}",
                _projectDetailPagesOpened,
                _projectDetailPagesOpened - _projectDetailPagesFailed,
                _projectDetailPagesFailed,
                _projectDetailPagesSkipped,
                _projectUnitsFound);
    }

    // ── Hulpmethoden ─────────────────────────────────────────────────────────

    private bool IsProjectForListing(string? externalId, string canonicalUrl) =>
        IsProjectUrl(canonicalUrl)
        || (!string.IsNullOrEmpty(externalId) && _projectExternalIds.Contains(externalId));

    private static bool IsProjectCard(ListingDto card) =>
        card.PropertyTypeRaw == "PROJECT"
        || IsProjectUrl(card.CanonicalUrl ?? "");

    private static string BuildProjectDetailUrl(string? externalId, string canonicalUrl)
    {
        if (IsProjectUrl(canonicalUrl)) return canonicalUrl;
        if (string.IsNullOrEmpty(externalId)) return canonicalUrl;

        try
        {
            var segments = new Uri(canonicalUrl).Segments
                .Select(s => s.Trim('/'))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            // ["nl", "mechelen-aan-de-maas-3630", "te-koop", "appartement", "LPC9N"]
            if (segments.Length >= 2 && segments[0] == "nl")
                return $"https://www.zimmo.be/nl/{segments[1]}/te-koop/nieuwbouwproject/{externalId}/";
        }
        catch { }

        return canonicalUrl;
    }

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

    private static bool IsChallengePage(int httpStatus, string title, string body) =>
        httpStatus == 403
        || title.Contains("Even geduld",    StringComparison.OrdinalIgnoreCase)
        || title.Contains("Just a moment",  StringComparison.OrdinalIgnoreCase)
        || body.Contains("Beveiliging wordt geverifieerd", StringComparison.OrdinalIgnoreCase)
        || body.Contains("Checking your browser",          StringComparison.OrdinalIgnoreCase);

    private static bool HasRealPropertyContent(string body) =>
        body.Contains("te koop",      StringComparison.OrdinalIgnoreCase)
        || body.Contains("prijs",     StringComparison.OrdinalIgnoreCase)
        || body.Contains("€",         StringComparison.OrdinalIgnoreCase)
        || body.Contains("m²",        StringComparison.OrdinalIgnoreCase)
        || body.Contains("slaapkamer",StringComparison.OrdinalIgnoreCase)
        || body.Contains("appartement", StringComparison.OrdinalIgnoreCase);

    private static bool IsProjectUrl(string url) =>
        url.Contains("/te-koop/project/",    StringComparison.OrdinalIgnoreCase)
        || url.Contains("/nl/project/",      StringComparison.OrdinalIgnoreCase)
        || url.Contains("/nieuwbouwproject/",StringComparison.OrdinalIgnoreCase);

    private static bool IsValidListingUrl(string url) =>
        url.Contains("/te-koop/huis/",            StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/appartement/",  StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/woning/",        StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/nieuwbouwproject/", StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/villa/",         StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/studio/",        StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/duplex/",        StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/penthouse/",     StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/grond/",         StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/garage/",        StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/gebouw/",        StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/commercieel/",   StringComparison.OrdinalIgnoreCase)
        || url.Contains("/te-koop/kantoor/",       StringComparison.OrdinalIgnoreCase);

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

    // ── DOM-gebaseerde projectpagina-extractie (gebruikt wanneer __NEXT_DATA__ afwezig is) ──

    private async Task<ListingDto?> ParseProjectPageFromDomAsync(
        IPage page, string canonicalUrl, string? externalId)
    {
        const string js = """
            () => {
                // Projectnaam: h1.pand-title (zimmo-code div verwijderd) → meta og:title als fallback
                const h1 = document.querySelector('h1.pand-title');
                let projectName = '';
                if (h1) {
                    const clone = h1.cloneNode(true);
                    const zcode = clone.querySelector('.zimmo-code, p.zimmo-code');
                    if (zcode) zcode.remove();
                    projectName = (clone.textContent || '').replace(/\s+/g, ' ').trim();
                }
                if (!projectName) {
                    const ogTitle = document.querySelector('meta[property="og:title"]');
                    if (ogTitle) {
                        // "Urbisol West nieuwbouw in Beernem vanaf € 275.000 (1004T93) - Kantoor | Zimmo"
                        // Verwijder "(code) - ..." suffix
                        let t = (ogTitle.getAttribute('content') || '')
                            .replace(/\s*\([^)]+\)\s*-.*$/i, '').trim();
                        projectName = t;
                    }
                }

                // Adres: section-features h2.section-title eerste span, anders body text zoeken
                const addrSpan = document.querySelector('section.section-features h2.section-title span:first-child')
                               || document.querySelector('section.section-features h2.section-title span');
                let address = addrSpan ? (addrSpan.textContent || '').replace(/\s+/g, ' ').trim() : '';
                // Fallback: zoek een patroon "Straatnaam 99, 9999 Gemeente" in meta description
                if (!address) {
                    const metaDesc = document.querySelector('meta[name="description"]');
                    if (metaDesc) {
                        const m = (metaDesc.getAttribute('content') || '').match(/\b\d{4}\s+\w[\w\s\-]+/);
                        if (m) address = m[0].trim();
                    }
                }

                // Prijs uit price-box
                const priceEl = document.querySelector('div.price-box span.feature-value');
                const priceText = priceEl ? (priceEl.textContent || '').replace(/\s+/g, ' ').trim() : '';

                // Coördinaten uit zmaps-estate-detail
                const mapEl = document.querySelector('zmaps-estate-detail');
                const lat = mapEl ? mapEl.getAttribute('lat') : null;
                const lon = mapEl ? mapEl.getAttribute('long') : null;

                // Beschrijving
                const descEl = document.querySelector('section.section-description p');
                const description = descEl ? (descEl.textContent || '').replace(/\s+/g, ' ').trim() : '';

                // Units uit de indeling-tabel
                // Kolomvolgorde: [lege][fav][Type][Nr][Prijs][Slpk][Woonopp][Grondopp][Terras][Tuin][Z-code]
                const units = [];
                document.querySelectorAll('section.section-division table.table-striped tr').forEach(tr => {
                    if (tr.querySelector('th')) return;
                    const tds = Array.from(tr.querySelectorAll('td'));
                    if (tds.length < 7) return;
                    const unitType    = (tds[2]?.textContent || '').replace(/\s+/g, ' ').trim();
                    const priceTxt    = (tds[4]?.textContent || '').replace(/\s+/g, ' ').trim();
                    const bedroomsTxt = (tds[5]?.textContent || '').replace(/\s+/g, ' ').trim();
                    const areaTxt     = (tds[6]?.textContent || '').replace(/\s+/g, ' ').trim();
                    const zcode       = tds.length > 10 ? (tds[10]?.textContent || '').replace(/\s+/g, ' ').trim() : '';
                    const sold        = tr.classList.contains('disabled');
                    const href        = tr.querySelector('a[href]')?.getAttribute('href') || '';
                    units.push({ unitType, priceTxt, bedroomsTxt, areaTxt, zcode, sold, href });
                });

                return { projectName, address, priceText, lat, lon, description, units };
            }
            """;

        JsonElement result;
        try { result = await page.EvaluateAsync<JsonElement>(js); }
        catch (Exception ex)
        {
            Logger.LogWarning("[Zimmo] DOM project extractie fout: {Msg}", ex.Message);
            return null;
        }

        static string S(JsonElement el, string key) =>
            el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String
                ? (p.GetString() ?? "") : "";

        var projectName = S(result, "projectName");
        var address     = S(result, "address");
        var priceText   = S(result, "priceText");
        var latStr      = S(result, "lat");
        var lonStr      = S(result, "lon");
        var description = S(result, "description");

        if (string.IsNullOrEmpty(projectName) && string.IsNullOrEmpty(address))
        {
            Logger.LogWarning(
                "[Zimmo] DOM project extractie: geen projectnaam of adres | Url={Url}", canonicalUrl);
            return null;
        }

        // Adres parsen: "Stationsstraat 135, 8730 Beernem"
        string? postalCode = null, city = null, street = null, houseNumber = null;
        if (!string.IsNullOrEmpty(address))
        {
            var addrM = Regex.Match(address, @"^(.*?),\s*(\d{4})\s+(.+)$");
            if (addrM.Success)
            {
                var streetPart = addrM.Groups[1].Value.Trim();
                postalCode = addrM.Groups[2].Value;
                city       = addrM.Groups[3].Value.Trim();
                var sm = Regex.Match(streetPart, @"^(.*?)\s+(\d+\w*)\s*$");
                if (sm.Success) { street = sm.Groups[1].Value.Trim(); houseNumber = sm.Groups[2].Value.Trim(); }
                else              { street = streetPart; }
            }
        }

        var price = ParseCardPrice(priceText);

        decimal? lat = null, lon = null;
        if (!string.IsNullOrEmpty(latStr)
            && decimal.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var latV))
            lat = latV;
        if (!string.IsNullOrEmpty(lonStr)
            && decimal.TryParse(lonStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lonV))
            lon = lonV;

        externalId ??= ZimmoListingParser.ExtractExternalIdFromUrl(canonicalUrl);

        // Units parsen
        var units = new List<ProjectGroupUnitDto>();
        if (result.TryGetProperty("units", out var unitsArr) && unitsArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var u in unitsArr.EnumerateArray())
            {
                var unitType     = S(u, "unitType");
                var unitPriceTxt = S(u, "priceTxt");
                var bedroomsTxt  = S(u, "bedroomsTxt");
                var areaTxt      = S(u, "areaTxt");
                var zcode        = S(u, "zcode");
                var sold         = u.TryGetProperty("sold", out var sp) && sp.ValueKind == JsonValueKind.True;
                var href         = S(u, "href");

                if (string.IsNullOrEmpty(zcode) && string.IsNullOrEmpty(href)) continue;

                var unitId = !string.IsNullOrEmpty(zcode) ? zcode
                    : (ZimmoListingParser.ExtractExternalIdFromUrl(href.Split('?')[0]) ?? href);

                units.Add(new ProjectGroupUnitDto
                {
                    ParentProjectId       = externalId ?? "",
                    ParentProjectName     = projectName,
                    UnitId                = unitId,
                    RawGroupType          = unitType,
                    MappedPropertyType    = MapDomUnitType(unitType),
                    MappedPropertySubType = MapDomUnitSubType(unitType),
                    SaleStatus            = sold ? SaleStatus.Sold : SaleStatus.Available,
                    Price                 = sold ? null : ParseCardPrice(unitPriceTxt),
                    Surface               = ParseAreaText(areaTxt),
                    BedroomCount          = ParseBedroomsText(bedroomsTxt),
                    Floor                 = null,
                });
            }
        }

        var unitsAvailable = units.Count(u => u.SaleStatus == SaleStatus.Available);
        var unitsSold      = units.Count(u => u.SaleStatus == SaleStatus.Sold);

        Logger.LogInformation(
            "[Zimmo] ProjectDetailParsedFromDom | ExternalId={Id} | Name={Name} | Adres={Addr} | " +
            "Prijs=€{Price} | Units={Units} (Available={Avail}, Sold={Sold}) | Lat={Lat} | Lon={Lon}",
            externalId ?? "?", projectName, address,
            price?.ToString("N0") ?? "?", units.Count, unitsAvailable, unitsSold,
            lat?.ToString() ?? "?", lon?.ToString() ?? "?");

        if (units.Count > 0 && externalId is not null)
            _pendingProjectUnits[externalId] = units;

        decimal? maxPrice = null;
        if (units.Count > 0)
        {
            var prices = units.Where(u => u.Price.HasValue).Select(u => u.Price!.Value).ToList();
            if (prices.Count > 0) maxPrice = prices.Max();
        }

        return ZimmoDtoMapper.Build(
            externalId          : externalId ?? "",
            url                 : canonicalUrl,
            title               : projectName,
            zimmoPropertyType   : "PROJECT",
            zimmoPropertySubType: null,
            zimmoTransactionType: "FOR_SALE",
            postalCode          : postalCode,
            city                : city,
            street              : street,
            houseNumber         : houseNumber,
            latitude            : lat,
            longitude           : lon,
            askingPrice         : price,
            maxPrice            : maxPrice,
            livingArea          : null,
            landArea            : null,
            bedrooms            : null,
            bathrooms           : null,
            epcLabel            : null,
            epcScore            : null,
            isNewConstruction   : true,
            developerName       : null,
            developerPhone      : null,
            developerWebsite    : null,
            projectName         : projectName,
            description         : description,
            rawJson             : null);
    }

    private static PropertyType MapDomUnitType(string raw) => raw.ToUpperInvariant() switch
    {
        "APPARTEMENT" or "APARTMENT" => PropertyType.Apartment,
        "HUIS"        or "HOUSE"     => PropertyType.House,
        _                            => PropertyType.Unknown,
    };

    private static PropertySubType MapDomUnitSubType(string raw) => raw.ToUpperInvariant() switch
    {
        "APPARTEMENT" or "APARTMENT" => PropertySubType.Apartment,
        "HUIS"        or "HOUSE"     => PropertySubType.DetachedHouse,
        "STUDIO"                     => PropertySubType.Studio,
        "PENTHOUSE"                  => PropertySubType.Penthouse,
        _                            => PropertySubType.Unknown,
    };

    private async Task WriteProjectDebugAsync(
        string externalId, string canonicalUrl, string finalUrl,
        int httpStatus, string pageTitle, string bodyText, string html,
        string? nextDataJson, bool isCloudflare, bool hasPropertyContent,
        IPage page, CancellationToken ct)
    {
        try
        {
            var dir  = Path.Combine(AppContext.BaseDirectory, "debug", "zimmo-project-details");
            Directory.CreateDirectory(dir);
            var safe = Regex.Replace(externalId, @"[^A-Za-z0-9_\-]", "_");

            await File.WriteAllTextAsync(Path.Combine(dir, $"{safe}.html"), html, ct);
            await File.WriteAllTextAsync(Path.Combine(dir, $"{safe}-body.txt"),
                bodyText.Length > 50_000 ? bodyText[..50_000] : bodyText, ct);
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(dir, $"{safe}.png"), FullPage = false
            });

            var summary = JsonSerializer.Serialize(new
            {
                ExternalId         = externalId,
                CanonicalUrl       = canonicalUrl,
                FinalUrl           = finalUrl,
                HttpStatus         = httpStatus,
                Title              = pageTitle,
                HtmlBytes          = System.Text.Encoding.UTF8.GetByteCount(html),
                BodyFirst500       = bodyText.Length > 500 ? bodyText[..500] : bodyText,
                CloudflareDetected = isCloudflare,
                HasPropertyContent = hasPropertyContent,
                NextDataPresent    = !string.IsNullOrEmpty(nextDataJson),
                NextDataBytes      = nextDataJson?.Length ?? 0
            }, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(dir, $"{safe}-summary.json"), summary, ct);

            if (!string.IsNullOrEmpty(nextDataJson))
                await File.WriteAllTextAsync(Path.Combine(dir, $"{safe}-next-data.json"), nextDataJson, ct);

            Logger.LogInformation(
                "[Zimmo] ProjectDebug opgeslagen in debug/zimmo-project-details/{Safe}.*",
                safe);
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[Zimmo] WriteProjectDebugAsync fout: {Msg}", ex.Message);
        }
    }

    private async Task WriteDetailDebugAsync(
        string url, string html, string? nextDataJson, IPage page, CancellationToken ct)
    {
        try
        {
            var dir  = Path.Combine(AppContext.BaseDirectory, Settings.Debug.DebugDirectory, "zimmo-detail");
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
