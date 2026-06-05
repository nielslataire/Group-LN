using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
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

    private const string SearchBase = "https://www.immoweb.be/en/search/{0}/for-sale?countries=BE&maxItems=60&orderBy=newest&page={1}{2}";
    private static readonly string[] PropertyTypes = ["house", "apartment"];
    private const int MaxPages = 10;

    // Echte pand-URL eindigt op een numeriek classified-ID
    // Ondersteunt: /en/classified/ | /nl/zoekertje/ | /fr/annonce/
    private static readonly Regex ClassifiedUrlPattern =
        new(@"/(classified|zoekertje|annonce)/[^/]+/[^/]+/[^/]+/\d{4,}/(\d{6,})(\?.*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ClassifiedIdOnlyPattern =
        new(@"/(classified|zoekertje|annonce)/(\d{6,})(\?.*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Classified ID uit detail-URL extraheren (laatste numeriek segment)
    private static readonly Regex UrlIdPattern =
        new(@"/(\d{6,})(?:\?|$)", RegexOptions.Compiled);

    // URL-fragmenten die duiden op een API-call met zoekresultaten
    private static readonly string[] ApiUrlKeywords =
    [
        "search", "classified", "property", "listing", "result",
        "graphql", "/api/", "_next/data"
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

    // ── Zoekpagina-URL's genereren ─────────────────────────────────────────────

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        var postalFilter = string.Empty;
        if (Settings.AllowedPostalCodes.Count > 0)
        {
            postalFilter = string.Concat(Settings.AllowedPostalCodes.Select(pc => $"&postalCodes[]={pc}"));
            Logger.LogInformation("[Immoweb] Postcode-filter: {Postcodes}",
                string.Join(", ", Settings.AllowedPostalCodes));
        }

        var maxPages = Settings.MaxListingsPerRun > 0
            ? Math.Max(1, (int)Math.Ceiling(Settings.MaxListingsPerRun / 60.0) + 1)
            : MaxPages;

        var urls = PropertyTypes
            .SelectMany(type => Enumerable.Range(1, maxPages)
                .Select(page => string.Format(SearchBase, type, page, postalFilter)));

        return Task.FromResult(urls);
    }

    // ── Listing-URL's ophalen van één zoekpagina ───────────────────────────────

    protected override async Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
    {
        IPage? page = null;

        try
        {
            page = await _browser.NewPageAsync(Settings.UserAgent);

            // ── Netwerkonderschepping koppelen VÓÓR navigatie ──────────────────
            var captured = new ConcurrentBag<CapturedResponse>();
            page.Response += async (_, response) =>
            {
                try
                {
                    // Snel pre-filter op URL en content-type
                    var ct = response.Headers.GetValueOrDefault("content-type", "");
                    if (!ct.Contains("json", StringComparison.OrdinalIgnoreCase)) return;

                    var url = response.Url;
                    if (!IsInterestingApiUrl(url)) return;

                    // Body lezen — kan falen als response al weg is
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

            // Extra wachttijd zodat achterliggende API-calls ook afgerond zijn
            await page.WaitForTimeoutAsync(3000);

            // ── Paginadiagnose ─────────────────────────────────────────────────
            var actualUrl = page.Url;
            var title = await page.TitleAsync();
            var anchorCount = await page.EvaluateAsync<int>(
                "() => document.querySelectorAll('a').length");
            var scriptCount = await page.EvaluateAsync<int>(
                "() => document.querySelectorAll('script').length");

            Logger.LogInformation(
                "[Immoweb] Pagina geladen → URL: {Url} | Titel: '{Title}' | <a>: {Anchors} | <script>: {Scripts} | API-responses onderschept: {ApiCount}",
                actualUrl, title, anchorCount, scriptCount, captured.Count);

            // ── Blokkade-detectie ──────────────────────────────────────────────
            var contentLower = (await page.ContentAsync()).ToLowerInvariant();
            foreach (var indicator in BlockIndicators)
                if (contentLower.Contains(indicator))
                    Logger.LogWarning("[Immoweb] ⚠ Mogelijke blokkade: '{Indicator}'", indicator);

            // ── Stap 1: API-responses verwerken (meest betrouwbaar) ───────────
            var listingUrls = new List<string>();
            var apiEndpointLines = new List<string>();

            foreach (var resp in captured.OrderBy(r => r.Url))
            {
                Logger.LogInformation(
                    "[Immoweb] [NETWERK] {Method} {Status} {ContentType,-35} {Url}",
                    resp.Method, resp.Status, resp.ContentType.Split(';')[0].Trim(), resp.Url);

                var ids = TryExtractClassifiedIds(resp.Body).Distinct().ToList();

                var logLine = $"{resp.Method} {resp.Status} | ids={ids.Count,4} | {resp.Url}";
                apiEndpointLines.Add(logLine);

                if (ids.Count > 0)
                {
                    var sample = ids.First();
                    Logger.LogInformation(
                        "[Immoweb] [API FOUND] URL: {Url} | Aantal records: {Count} | Voorbeeld ID: {Id}",
                        resp.Url, ids.Count, sample);

                    foreach (var id in ids)
                    {
                        var url = $"https://www.immoweb.be/en/classified/{id}";
                        if (!listingUrls.Contains(url))
                            listingUrls.Add(url);
                    }
                }
                else
                {
                    Logger.LogDebug("[Immoweb] [NETWERK] Response bevat geen classified-IDs: {Url}", resp.Url);
                }
            }

            Logger.LogInformation(
                "[Immoweb] Netwerkfase klaar: {ApiCount} responses onderschept, {ListingCount} listing-URL's gevonden.",
                captured.Count, listingUrls.Count);

            // ── Stap 2: Fallback naar hrefs als API niets opleverde ────────────
            if (listingUrls.Count == 0)
            {
                Logger.LogWarning("[Immoweb] Geen listings via netwerk — fallback naar href-scan.");
                var hrefUrls = await ScanHrefsAsync(page);
                listingUrls.AddRange(hrefUrls);
            }

            // ── Debug-bestanden wegschrijven ──────────────────────────────────
            var htmlContent = await page.ContentAsync();
            await WriteDebugFilesAsync(
                searchPageUrl, htmlContent, captured, apiEndpointLines, page, cancellationToken);

            if (listingUrls.Count == 0)
                Logger.LogWarning("[Immoweb] 0 listing-URL's gevonden. Zie debug/-map voor details.");

            return listingUrls.Distinct();
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

    // ── Helpers voor URL-classificatie ────────────────────────────────────────

    private static bool IsInterestingApiUrl(string url)
    {
        // Sla static assets over
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

    private static bool IsClassifiedUrl(string url)
    {
        // Expliciet uitsluiten: zoekpagina's en filterlinks
        if (url.Contains("/zoeken/", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains("/search/", StringComparison.OrdinalIgnoreCase)) return false;
        if (url.Contains("/recherche/", StringComparison.OrdinalIgnoreCase)) return false;

        // Moet minstens één van de listing-segmenten bevatten
        if (!url.Contains("/classified/", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("/zoekertje/", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("/annonce/", StringComparison.OrdinalIgnoreCase))
            return false;

        return ClassifiedUrlPattern.IsMatch(url) || ClassifiedIdOnlyPattern.IsMatch(url);
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

    // ── Href-fallback (HTML-scan) ──────────────────────────────────────────────

    private async Task<List<string>> ScanHrefsAsync(IPage page)
    {
        var result = new List<string>();

        var allHrefs = await page.EvaluateAsync<string[]>(
            "() => Array.from(document.querySelectorAll('a[href]')).map(a => a.href).filter(h => h.length > 0)");

        if (allHrefs is null) return result;

        var accepted = 0;
        var ignored = 0;

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
                    Logger.LogDebug("[Immoweb] [HREF] ✔ {Href}", href);
                }
            }
            else
            {
                ignored++;
                Logger.LogDebug("[Immoweb] [HREF] ✗ {Href}", href);
            }
        }

        Logger.LogInformation("[Immoweb] Href-scan: geaccepteerd={Acc} | genegeerd={Ign}", accepted, ignored);
        return result;
    }

    // ── Debug-bestanden ────────────────────────────────────────────────────────

    private async Task WriteDebugFilesAsync(
        string sourceUrl,
        string htmlContent,
        ConcurrentBag<CapturedResponse> captured,
        List<string> apiEndpointLines,
        IPage page,
        CancellationToken cancellationToken)
    {
        try
        {
            var debugDir = Path.Combine(AppContext.BaseDirectory, "debug");
            var networkDir = Path.Combine(debugDir, "network");
            Directory.CreateDirectory(networkDir);

            // HTML-snapshot
            await File.WriteAllTextAsync(
                Path.Combine(debugDir, "immoweb-search.html"), htmlContent, cancellationToken);

            // Samenvatting API-endpoints
            var endpointsPath = Path.Combine(debugDir, "immoweb-api-endpoints.txt");
            var endpointHeader = new[]
            {
                $"# Zoekpagina: {sourceUrl}",
                $"# Datum: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"# Onderschepte JSON-responses: {captured.Count}",
                "",
                "METHOD  STATUS  IDS   URL",
                new string('-', 120)
            };
            await File.WriteAllLinesAsync(endpointsPath,
                endpointHeader.Concat(apiEndpointLines), cancellationToken);
            Logger.LogInformation("[Immoweb] API-endpoints → {Path}", endpointsPath);

            // Individuele response bodies
            var fileIndex = 0;
            foreach (var resp in captured.OrderBy(r => r.Url))
            {
                var safeName = Regex.Replace(resp.Url, @"[^a-zA-Z0-9._-]", "_");
                if (safeName.Length > 80) safeName = safeName[^80..];
                var fileName = $"{++fileIndex:00}_{resp.Status}_{safeName}.json";
                var filePath = Path.Combine(networkDir, fileName);

                await File.WriteAllTextAsync(filePath, resp.Body, cancellationToken);
                Logger.LogDebug("[Immoweb] Response body → debug/network/{File}", fileName);
            }

            // Script-tags
            var scripts = await page.EvaluateAsync<string[]>(@"
() => Array.from(document.querySelectorAll('script'))
    .map((s, i) => `=== SCRIPT ${i+1} (${s.src || 'inline'}) ===\n` +
                   (s.textContent || '').substring(0, 20000))");

            if (scripts is { Length: > 0 })
            {
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, "immoweb-scripts.txt"),
                    $"# {sourceUrl}\n# {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                    string.Join("\n\n", scripts),
                    cancellationToken);
            }

            Logger.LogInformation(
                "[Immoweb] Debug: HTML + {Count} network-responses + scripts weggeschreven naar debug/",
                captured.Count);
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
                    Logger.LogInformation("[Immoweb] window.classified gevonden.");
                    LogParsedListing(dto);
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
                    Logger.LogInformation("[Immoweb] __NEXT_DATA__ gevonden.");
                    LogParsedListing(dto);
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
                Logger.LogInformation("[Immoweb] Script-fallback gelukt voor ID {Id}.", classifiedId);
                LogParsedListing(fallbackDto);
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

    private void LogParsedListing(ListingDto dto)
    {
        Logger.LogInformation(
            "[Immoweb] ══ Listing geparsed ══\n" +
            "  ExternalId      : {ExternalId}\n" +
            "  Url             : {Url}\n" +
            "  Title           : {Title}\n" +
            "  AskingPrice     : {Price}\n" +
            "  PostalCode      : {PostalCode}\n" +
            "  City            : {City}\n" +
            "  Street          : {Street} {HouseNumber}\n" +
            "  Floor/Unit      : {Floor} / {Unit}\n" +
            "  RawPropertyType : {RawType}\n" +
            "  RawSubType      : {RawSubType}\n" +
            "  Transaction     : {Transaction}\n" +
            "  LivingArea      : {Area} m²\n" +
            "  Bedrooms        : {Bedrooms}\n" +
            "  RawJson         : {HasJson}",
            dto.ExternalId ?? "?",
            dto.Url ?? "?",
            dto.Title ?? "?",
            dto.AskingPrice.HasValue ? $"€{dto.AskingPrice.Value:N0}" : "?",
            dto.PostalCode ?? "?",
            dto.City ?? "?",
            dto.Street ?? "?",
            dto.HouseNumber ?? "?",
            dto.Floor?.ToString() ?? "(onbekend)",
            dto.UnitNumber ?? "(onbekend)",
            dto.PropertyTypeRaw ?? "?",
            dto.PropertySubTypeRaw ?? "?",
            dto.TransactionTypeRaw ?? "?",
            dto.LivingArea?.ToString("N0") ?? "?",
            dto.Bedrooms?.ToString() ?? "?",
            string.IsNullOrEmpty(dto.RawJson) ? "NEE" : $"JA ({dto.RawJson.Length:N0} bytes)");
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

        var price = TryGetDecimal(root["transaction"]?["sale"]?["price"])
                 ?? TryGetDecimal(root["price"]?["mainValue"])
                 ?? TryGetDecimal(root["transaction"]?["rental"]?["monthlyRent"]);

        var location = root["property"]?["location"];
        var prop = root["property"];

        var condition = prop?["building"]?["condition"]?.GetValue<string>() ?? string.Empty;
        var rawType = prop?["type"]?.GetValue<string>();
        var rawSubType = prop?["subtype"]?.GetValue<string>();
        var city = location?["locality"]?.GetValue<string>() ?? location?["city"]?.GetValue<string>();

        return new ListingDto
        {
            ExternalId = id.Value.ToString(),
            Url = url,
            Title = $"{rawType} in {city}",
            PropertyTypeRaw = rawType,
            PropertySubTypeRaw = rawSubType,
            TransactionTypeRaw = root["transaction"]?["type"]?.GetValue<string>() ?? "FOR_SALE",
            PostalCode = location?["postalCode"]?.GetValue<string>(),
            City = city,
            Street = location?["street"]?.GetValue<string>(),
            HouseNumber = location?["number"]?.GetValue<string>(),
            Floor = TryGetInt(prop?["building"]?["floorNumber"]) ?? TryGetInt(prop?["floor"]),
            UnitNumber = prop?["building"]?["unitNumber"]?.GetValue<string>()
                      ?? prop?["unitNumber"]?.GetValue<string>(),
            Latitude = TryGetDecimal(location?["latitude"]),
            Longitude = TryGetDecimal(location?["longitude"]),
            AskingPrice = price,
            LivingArea = TryGetDecimal(prop?["netHabitableSurface"]) ?? TryGetDecimal(prop?["habitableSurface"]),
            LandArea = TryGetDecimal(prop?["land"]?["surface"]),
            Bedrooms = TryGetInt(prop?["bedroomCount"]),
            Bathrooms = TryGetInt(prop?["bathroomCount"]),
            ConstructionYear = TryGetInt(prop?["building"]?["constructionYear"]),
            EPCScore = TryGetDecimal(prop?["energy"]?["primaryEnergyConsumptionPerSqm"]),
            EPCLabelRaw = prop?["energy"]?["epcScoreClass"]?.GetValue<string>(),
            IsNewBuild = condition.Contains("NEW_CONSTRUCTION", StringComparison.OrdinalIgnoreCase)
                      || (rawType?.Contains("GROUP", StringComparison.OrdinalIgnoreCase) ?? false),
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

    private static int? TryGetInt(JsonNode? node)
    {
        if (node is null) return null;
        try { return node.GetValue<int>(); }
        catch { return null; }
    }

    [GeneratedRegex(@"""(?:id|classifiedId|propertyId)""\s*:\s*(\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex ClassifiedIdPattern();
}

// ── Value object voor onderschepte responses ───────────────────────────────────
internal record CapturedResponse(string Url, string Method, int Status, string ContentType, string Body);
