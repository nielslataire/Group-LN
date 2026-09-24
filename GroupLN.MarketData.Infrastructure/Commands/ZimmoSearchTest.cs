using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Browser;
using GroupLN.MarketData.Infrastructure.Crawlers;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GroupLN.MarketData.Infrastructure.Commands;

/// <summary>
/// Diagnose zonder database: opent per Zimmo-locatie de eerste zoekpagina precies zoals
/// ZimmoCrawler dat doet, en rapporteert HTTP-status, paginatitel, Cloudflare-detectie,
/// aantal listing-links, aantal DOM-cards en aanwezigheid van __NEXT_DATA__.
/// HTML + screenshot worden bewaard in debug/zimmo-search-test/.
///
/// Gebruik:  dotnet GroupLN.MarketData.Worker.dll --zimmo-search-test [--postcode 8000]
/// Draai dit lokaal én op de server: werkt het lokaal wel en op de server niet, dan
/// blokkeert Zimmo/Cloudflare het server-IP. Werkt het nergens, dan is de pagina-opbouw gewijzigd.
/// </summary>
public class ZimmoSearchTest
{
    private readonly PlaywrightBrowserService _browser;
    private readonly CrawlerSettings _settings;
    private readonly ILogger<ZimmoSearchTest> _logger;

    public ZimmoSearchTest(PlaywrightBrowserService browser, CrawlerSettings settings, ILogger<ZimmoSearchTest> logger)
    {
        _browser  = browser;
        _settings = settings;
        _logger   = logger;
    }

    public async Task RunAsync(string? postalCodeFilter, CancellationToken ct = default)
    {
        var debugDir = Path.Combine(AppContext.BaseDirectory, "debug", "zimmo-search-test");
        Directory.CreateDirectory(debugDir);

        _settings.Sources.TryGetValue("Zimmo", out var src);
        var locations = src?.AllowedLocations ?? new List<LocationSettings>();
        if (!string.IsNullOrEmpty(postalCodeFilter))
            locations = locations.Where(l => l.PostalCode == postalCodeFilter).ToList();
        if (locations.Count == 0)
            locations = [new LocationSettings { City = "Brugge", PostalCode = postalCodeFilter ?? "8000" }];

        var timeoutMs = src?.PlaywrightTimeoutMs ?? _settings.PlaywrightTimeoutMs;

        _logger.LogInformation("[ZimmoSearchTest] Gestart | {Count} locatie(s) | Timeout={Timeout}ms | DebugDir={Dir}",
            locations.Count, timeoutMs, debugDir);

        int ok = 0, blocked = 0, empty = 0, failed = 0;

        foreach (var loc in locations)
        {
            ct.ThrowIfCancellationRequested();

            int? placeId = loc.PlaceId;
            if (placeId is null && !string.IsNullOrEmpty(loc.PostalCode)
                && ZimmoSearchUrlBuilder.PlaceIdByPostalCode.TryGetValue(loc.PostalCode, out var id))
                placeId = id;

            var url = ZimmoSearchUrlBuilder.Build(placeId);
            _logger.LogInformation("[ZimmoSearchTest] ── {City} ({Postal}) placeId={PlaceId}", loc.City, loc.PostalCode, placeId?.ToString() ?? "–");
            _logger.LogInformation("[ZimmoSearchTest]    URL: {Url}", url);

            var page = await _browser.NewPageAsync(_settings.UserAgent);
            try
            {
                IResponse? nav = null;
                string gotoError = "";
                try
                {
                    nav = await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = timeoutMs });
                }
                catch (PlaywrightException ex)
                {
                    gotoError = ex.Message.Split('\n')[0];
                }

                await page.WaitForTimeoutAsync(2000);

                var status   = nav?.Status ?? 0;
                var title    = await page.TitleAsync();
                var body     = await page.EvaluateAsync<string>("() => (document.body?.innerText ?? '').slice(0, 10000)") ?? "";
                var links    = await page.EvaluateAsync<int>("() => document.querySelectorAll('.property-item a.property-item_link[href], zimmo-listing article h2 a[href]').length");
                var legacy   = await page.EvaluateAsync<int>("() => document.querySelectorAll('.property-item').length");
                var angular  = await page.EvaluateAsync<int>("() => Array.from(document.querySelectorAll('article')).filter(a => a.querySelector('.zimmo-code')).length");
                var items    = legacy + angular;
                var nextData = await page.EvaluateAsync<int>("() => { const el = document.getElementById('__NEXT_DATA__'); return el ? el.textContent.length : 0; }");
                var cookieBtn = await page.EvaluateAsync<int>("() => document.querySelectorAll('#onetrust-accept-btn-handler, button[id*=\"accept\"], button[class*=\"accept\"]').length");

                var isChallenge =
                    status == 403
                    || title.Contains("Even geduld", StringComparison.OrdinalIgnoreCase)
                    || title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("Beveiliging wordt geverifieerd", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("Checking your browser", StringComparison.OrdinalIgnoreCase);

                var slug = $"{loc.PostalCode}-{(loc.City ?? "x").ToLowerInvariant().Replace(' ', '-')}";
                await File.WriteAllTextAsync(Path.Combine(debugDir, slug + ".html"), await page.ContentAsync(), ct);
                await File.WriteAllTextAsync(Path.Combine(debugDir, slug + ".txt"), body, ct);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(debugDir, slug + ".png"), FullPage = false });

                var verdict = isChallenge ? "CLOUDFLARE-BLOCKED"
                            : status == 0 && gotoError.Length > 0 ? "NAVIGATIE-FOUT"
                            : items == 0 ? "LEEG (0 property-items)"
                            : "OK";

                if (isChallenge) blocked++;
                else if (status == 0 && gotoError.Length > 0) failed++;
                else if (items == 0) empty++;
                else ok++;

                _logger.LogInformation(
                    "[ZimmoSearchTest]    → {Verdict} | HTTP {Status} | Title='{Title}' | kaarten={Items} (oud={Legacy}, angular={Angular}) | listing-links={Links} | __NEXT_DATA__={NextData} tekens | cookie-knop={Cookie} | bodyText={BodyLen} tekens{GotoErr}",
                    verdict, status, title, items, legacy, angular, links, nextData, cookieBtn, body.Length,
                    gotoError.Length > 0 ? " | GotoAsync: " + gotoError : "");

                if (items == 0 && !isChallenge)
                {
                    var preview = body.Replace("\n", " ").Replace("\r", " ");
                    if (preview.Length > 400) preview = preview[..400];
                    _logger.LogInformation("[ZimmoSearchTest]    Body-preview: {Preview}", preview);
                }

                // Steekproef van de kaartvelden zoals ZimmoCrawler.ExtractDomCardsAsync ze leest (Angular-markup)
                if (angular > 0)
                {
                    var sample = await page.EvaluateAsync<string[]>("""
                        () => Array.from(document.querySelectorAll('article'))
                            .filter(a => a.querySelector('.zimmo-code')).slice(0, 4).map(item => {
                                const clean = s => (s || '').replace(/ /g, ' ').replace(/\s+/g, ' ').trim();
                                const code = clean(item.querySelector('.zimmo-code')?.textContent);
                                const href = item.querySelector('h2 a[href]')?.getAttribute('href') || '';
                                const sticker = clean(item.querySelector('.sticker')?.textContent);
                                const price = clean(item.querySelector('.price .amount')?.textContent);
                                const addr = clean((item.querySelector('address')?.innerHTML || '').replace(/<br\s*\/?>/gi, ' | ').replace(/<[^>]+>/g, ''));
                                let opp = '', bed = '';
                                for (const f of item.querySelectorAll('.features_item')) {
                                    const l = (f.getAttribute('aria-label') || '').toLowerCase();
                                    const v = clean(f.querySelector('.value')?.textContent);
                                    if (l.includes('woonoppervlakte')) opp = v; else if (l.includes('slaapkamer')) bed = v;
                                }
                                return `${code} | ${href} | prijs='${price}' | adres='${addr}' | opp='${opp}' | slpk='${bed}' | sticker='${sticker}'`;
                            })
                        """) ?? [];
                    foreach (var s in sample)
                        _logger.LogInformation("[ZimmoSearchTest]    kaart: {Card}", s);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "[ZimmoSearchTest]    → FOUT: {Msg}", ex.Message);
            }
            finally
            {
                await page.Context.CloseAsync();
            }
        }

        _logger.LogInformation(
            "[ZimmoSearchTest] Klaar | OK={Ok} | Cloudflare-blocked={Blocked} | Leeg={Empty} | Fout={Failed} | Bestanden in {Dir}",
            ok, blocked, empty, failed, debugDir);
    }
}
