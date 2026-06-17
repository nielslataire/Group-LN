using System.Collections.Concurrent;
using System.Text.Json;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Browser;
using GroupLN.MarketData.Infrastructure.Crawlers;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GroupLN.MarketData.Infrastructure.Commands;

/// <summary>
/// Tijdelijke test: opent één Zimmo detailpagina met de volledige NavigationUrl (incl. ?search=...)
/// zoals een gebruiker die vanuit zoekresultaten doorklikt. Geen database-writes.
/// </summary>
public class ZimmoDetailDiscoveryTest
{
    private const string NavigationUrl =
        "https://www.zimmo.be/nl/veerle-2431/te-koop/appartement/LPC9N/" +
        "?search=eyJmaWx0ZXIiOnsic3RhdHVzIjp7ImluIjpbIkZPUl9TQUxFIiwiVEFLRV9PVkVSIl19LCJjYXRlZ29yeSI6eyJpbiI6WyJIT1VTRSIsIkFQQVJUTUVOVCJdfSwibmV3Q29uc3RydWN0aW9uIjp7ImVxIjoiQUxMIn19fQ%3D%3D";

    private const string CanonicalUrl =
        "https://www.zimmo.be/nl/veerle-2431/te-koop/appartement/LPC9N/";

    // Zoekpagina waarvan deze listing afkomstig is (simuleert klik vanuit zoekresultaten)
    private const string SearchRefererUrl =
        "https://www.zimmo.be/nl/zoeken/" +
        "?search=eyJmaWx0ZXIiOnsic3RhdHVzIjp7ImluIjpbIkZPUl9TQUxFIiwiVEFLRV9PVkVSIl19LCJjYXRlZ29yeSI6eyJpbiI6WyJIT1VTRSIsIkFQQVJUTUVOVCJdfSwibmV3Q29uc3RydWN0aW9uIjp7ImVxIjoiQUxMIn19fQ%3D%3D";

    private readonly PlaywrightBrowserService _browser;
    private readonly CrawlerSettings         _settings;
    private readonly ILogger<ZimmoDetailDiscoveryTest> _logger;

    private static readonly JsonSerializerOptions PrettyJson  = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions CompactJson = new() { WriteIndented = false };

    public ZimmoDetailDiscoveryTest(
        PlaywrightBrowserService browser,
        CrawlerSettings settings,
        ILogger<ZimmoDetailDiscoveryTest> logger)
    {
        _browser  = browser;
        _settings = settings;
        _logger   = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var debugDir = Path.Combine(
            AppContext.BaseDirectory,
            _settings.Debug.DebugDirectory,
            "zimmo-detail-test");
        Directory.CreateDirectory(debugDir);

        _logger.LogInformation("[ZimmoDetailTest] Gestart");
        _logger.LogInformation("[ZimmoDetailTest] NavigationUrl : {Nav}", NavigationUrl);
        _logger.LogInformation("[ZimmoDetailTest] CanonicalUrl  : {Can}", CanonicalUrl);
        _logger.LogInformation("[ZimmoDetailTest] DebugDir      : {Dir}", debugDir);

        var pendingResponses = new ConcurrentBag<IResponse>();
        var page = await _browser.NewPageAsync(_settings.UserAgent);

        try
        {
            page.Response += (_, r) =>
            {
                if (r.Url.Contains("zimmo.be", StringComparison.OrdinalIgnoreCase))
                    pendingResponses.Add(r);
            };

            // ── Stap 1: Zoekpagina laden als referer ─────────────────────────
            _logger.LogInformation("[ZimmoDetailTest] Stap 1: Zoekpagina laden als referer...");
            try
            {
                await page.GotoAsync(SearchRefererUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout   = 20_000
                });
                await page.WaitForTimeoutAsync(1500);

                var searchTitle = await page.TitleAsync();
                _logger.LogInformation(
                    "[ZimmoDetailTest] Zoekpagina geladen: '{Title}' | HTTP {Status}",
                    searchTitle, 200);
            }
            catch (PlaywrightException ex)
            {
                _logger.LogWarning("[ZimmoDetailTest] Zoekpagina-fout (niet fataal): {Msg}", ex.Message);
            }

            // ── Stap 2: Detailpagina met NavigationUrl (incl. ?search=...) ──
            _logger.LogInformation("[ZimmoDetailTest] Stap 2: Navigeer naar detailpagina...");
            IResponse? navResponse = null;
            try
            {
                navResponse = await page.GotoAsync(NavigationUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout   = _settings.PlaywrightTimeoutMs
                });
            }
            catch (PlaywrightException ex)
            {
                _logger.LogWarning("[ZimmoDetailTest] GotoAsync fout: {Msg}", ex.Message);
            }

            await page.WaitForTimeoutAsync(2500);

            // ── Stap 3: Pagina-staat lezen ────────────────────────────────────
            var httpStatus = navResponse?.Status ?? 0;
            var pageTitle  = await page.TitleAsync();
            var html       = await page.ContentAsync();
            var bodyText   = await page.EvaluateAsync<string>(
                "() => (document.body?.innerText ?? '').slice(0, 100000)");

            // ── Stap 4: Challenge-detectie (strikt) ──────────────────────────
            bool cloudflareDetected = IsChallengePage(httpStatus, pageTitle, bodyText);
            bool challengeResolved  = !cloudflareDetected;
            double waitSec          = 0;

            if (cloudflareDetected)
            {
                _logger.LogWarning(
                    "[ZimmoDetailTest] CloudflareDetected (HTTP {Status}, titel='{Title}') — wacht max 60s...",
                    httpStatus, pageTitle);

                for (int sec = 0; sec < 60 && !ct.IsCancellationRequested; sec++)
                {
                    await page.WaitForTimeoutAsync(1_000);
                    waitSec = sec + 1;

                    var tit  = await page.TitleAsync();
                    var snip = await page.EvaluateAsync<string>(
                        "() => (document.body?.innerText ?? '').slice(0, 3000)");

                    if (!IsChallengePage(0, tit, snip))
                    {
                        challengeResolved = true;
                        _logger.LogInformation(
                            "[ZimmoDetailTest] Challenge opgelost na {Sec}s", waitSec);

                        pageTitle = tit;
                        html      = await page.ContentAsync();
                        bodyText  = await page.EvaluateAsync<string>(
                            "() => (document.body?.innerText ?? '').slice(0, 100000)");
                        break;
                    }
                }

                if (!challengeResolved)
                    _logger.LogWarning(
                        "[ZimmoDetailTest] Challenge NIET opgelost na {Sec}s.", waitSec);
            }

            _logger.LogInformation(
                "[ZimmoDetailTest] HTTP {Status} | '{Title}' | {Bytes} bytes | " +
                "CloudflareDetected={CF} | ChallengeResolved={Res}",
                httpStatus, pageTitle, html.Length, cloudflareDetected, challengeResolved);

            // ── Stap 5: Content verzamelen ────────────────────────────────────
            var nextDataJson = await page.EvaluateAsync<string?>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); " +
                "return el ? el.textContent : null; }");

            var jsonLdScripts = await page.EvaluateAsync<string[]>(@"() =>
                Array.from(document.querySelectorAll('script[type=""application/ld+json""]'))
                    .map(s => s.textContent || '').filter(t => t.trim().length > 0)") ?? [];

            var jsonScripts = await page.EvaluateAsync<string[]>(@"() =>
                Array.from(document.querySelectorAll('script:not([src])'))
                    .map(s => (s.textContent || '').trim())
                    .filter(t => (t.startsWith('{') || t.startsWith('[')) && t.length > 20)
                    .slice(0, 20)") ?? [];

            var possibleJsonKeys = await page.EvaluateAsync<string[]>(@"() => {
                const keys = ['__NEXT_DATA__','__NUXT_DATA__','__INITIAL_STATE__',
                              '__APP_STATE__','__STORE__','NUXT_PAGE_DATA','__REDUX_STATE__'];
                return keys.filter(k => !!window[k]);
            }") ?? [];

            // ── Stap 6: Parsen (geen DB-writes) ──────────────────────────────
            ListingDto? parsedDto = null;

            if (!string.IsNullOrEmpty(nextDataJson))
            {
                _logger.LogInformation("[ZimmoDetailTest] __NEXT_DATA__ gevonden ({Len} chars) — parsen...",
                    nextDataJson.Length);
                parsedDto = ZimmoListingParser.TryParseNextData(nextDataJson, CanonicalUrl, _logger);
            }

            if (parsedDto is null && !string.IsNullOrEmpty(html))
            {
                _logger.LogInformation("[ZimmoDetailTest] Fallback naar JSON-LD...");
                parsedDto = ZimmoListingParser.TryParseJsonLd(html, CanonicalUrl, _logger);
            }

            if (parsedDto is not null)
            {
                parsedDto.NavigationUrl = NavigationUrl;
                parsedDto.CanonicalUrl  = CanonicalUrl;
            }

            // ── Stap 7: Network responses ─────────────────────────────────────
            await page.WaitForTimeoutAsync(300);
            var captures = new List<NetworkCapture>();
            foreach (var r in pendingResponses)
            {
                try
                {
                    r.Headers.TryGetValue("content-type", out var ctype);
                    string body;
                    try   { body = await r.TextAsync(); }
                    catch { body = string.Empty; }
                    captures.Add(new NetworkCapture(
                        r.Url, r.Status, ctype ?? "",
                        body.Length > 4000 ? body[..4000] + "…" : body));
                }
                catch { }
            }

            // ── Stap 8: Bestanden opslaan ─────────────────────────────────────
            await File.WriteAllTextAsync(Path.Combine(debugDir, "detail-lpc9n.html"), html, ct);
            _logger.LogInformation("[ZimmoDetailTest] HTML opgeslagen ({Bytes} bytes)", html.Length);

            await File.WriteAllTextAsync(Path.Combine(debugDir, "detail-lpc9n-body.txt"), bodyText, ct);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path     = Path.Combine(debugDir, "detail-lpc9n.png"),
                FullPage = false
            });
            _logger.LogInformation("[ZimmoDetailTest] Screenshot opgeslagen");

            if (!string.IsNullOrEmpty(nextDataJson))
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, "detail-lpc9n-next-data.json"), nextDataJson, ct);

            if (jsonLdScripts.Length > 0)
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, "detail-lpc9n-json-ld.json"),
                    JsonSerializer.Serialize(jsonLdScripts, PrettyJson), ct);

            if (captures.Count > 0)
            {
                var lines = captures.Select(c => JsonSerializer.Serialize(c, CompactJson));
                await File.WriteAllLinesAsync(
                    Path.Combine(debugDir, "detail-lpc9n-network.jsonl"), lines, ct);
                _logger.LogInformation(
                    "[ZimmoDetailTest] Network: {Count} responses opgeslagen", captures.Count);
            }

            // ── Stap 9: Summary ───────────────────────────────────────────────
            var summary = new
            {
                Timestamp            = DateTime.UtcNow.ToString("O"),
                NavigationUrl,
                CanonicalUrl,
                HttpStatus           = httpStatus,
                Title                = pageTitle,
                CloudflareDetected   = cloudflareDetected,
                ChallengeResolved    = challengeResolved,
                ChallengeWaitSeconds = waitSec,
                HtmlBytes            = html.Length,
                HasNextData          = !string.IsNullOrEmpty(nextDataJson),
                JsonLdCount          = jsonLdScripts.Length,
                ScriptJsonCount      = jsonScripts.Length,
                PossibleJsonKeys     = possibleJsonKeys,
                NetworkCaptureCount  = captures.Count,
                FoundPrice           = parsedDto?.AskingPrice,
                FoundAddress         = parsedDto is null ? null
                    : $"{parsedDto.Street} {parsedDto.HouseNumber}".Trim(),
                FoundPostalCode      = parsedDto?.PostalCode,
                FoundCity            = parsedDto?.City,
                FoundLivingArea      = parsedDto?.LivingArea,
                FoundBedrooms        = parsedDto?.Bedrooms,
                FoundLatitude        = parsedDto?.Latitude,
                FoundLongitude       = parsedDto?.Longitude,
            };

            await File.WriteAllTextAsync(
                Path.Combine(debugDir, "detail-lpc9n-summary.json"),
                JsonSerializer.Serialize(summary, PrettyJson), ct);

            _logger.LogInformation("[ZimmoDetailTest] Summary opgeslagen → {Dir}", debugDir);
            _logger.LogInformation(
                "[ZimmoDetailTest] Resultaat: Prijs=€{Price} | {PostalCode} {City} | " +
                "{Area}m² | {Beds} kamers | lat={Lat} lon={Lon}",
                parsedDto?.AskingPrice?.ToString("N0") ?? "?",
                parsedDto?.PostalCode ?? "?",
                parsedDto?.City ?? "?",
                parsedDto?.LivingArea?.ToString("N0") ?? "?",
                parsedDto?.Bedrooms?.ToString() ?? "?",
                parsedDto?.Latitude?.ToString() ?? "?",
                parsedDto?.Longitude?.ToString() ?? "?");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ZimmoDetailTest] Onverwachte fout");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    // Detecteert alleen concrete challenge-signalen.
    // Matcht NIET op brede tekst zoals "Cloudflare" — staat in footer van gewone pagina's.
    private static bool IsChallengePage(int httpStatus, string title, string body) =>
        httpStatus == 403
        || title.Contains("Even geduld", StringComparison.OrdinalIgnoreCase)
        || title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase)
        || body.Contains("Beveiliging wordt geverifieerd", StringComparison.OrdinalIgnoreCase)
        || body.Contains("Checking your browser", StringComparison.OrdinalIgnoreCase);

    private sealed record NetworkCapture(
        string Url,
        int    Status,
        string ContentType,
        string BodyPreview);
}
