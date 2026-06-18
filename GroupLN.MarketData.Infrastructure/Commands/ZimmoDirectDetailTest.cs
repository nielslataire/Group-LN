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
/// Gerichte test: opent één Zimmo-pagina DIRECT — geen warmup, geen gedeelde context, geen cookies.
/// Accepteert een willekeurige Zimmo-URL via het --url argument.
/// Inspecteer elke seconde gedurende max 60s bij Cloudflare-blokkering.
/// Detecteert ook nieuwbouwproject-velden.
/// Geen database-writes.
/// </summary>
public class ZimmoDirectDetailTest
{
    private readonly PlaywrightBrowserService _browser;
    private readonly CrawlerSettings          _settings;
    private readonly ILogger<ZimmoDirectDetailTest> _logger;

    private static readonly JsonSerializerOptions PrettyJson  = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions CompactJson = new() { WriteIndented = false };

    public ZimmoDirectDetailTest(
        PlaywrightBrowserService browser,
        CrawlerSettings settings,
        ILogger<ZimmoDirectDetailTest> logger)
    {
        _browser  = browser;
        _settings = settings;
        _logger   = logger;
    }

    public async Task RunAsync(string url, CancellationToken ct = default)
    {
        var slug     = ExtractSlug(url);
        var debugDir = Path.Combine(
            AppContext.BaseDirectory,
            _settings.Debug.DebugDirectory,
            $"zimmo-discovery/zimmo-direct-detail-{slug}");
        Directory.CreateDirectory(debugDir);

        _logger.LogInformation("[DirectDetailTest] ==================================================");
        _logger.LogInformation("[DirectDetailTest] Zimmo Direct Detail Test — {Slug}", slug);
        _logger.LogInformation("[DirectDetailTest] URL      : {Url}", url);
        _logger.LogInformation("[DirectDetailTest] DebugDir : {Dir}", debugDir);
        _logger.LogInformation("[DirectDetailTest] Aanpak   : Ephemeral context, geen warmup, geen cookies");
        _logger.LogInformation("[DirectDetailTest] ==================================================");

        var capturedResponses = new ConcurrentBag<CapturedResponse>();

        var page = await _browser.NewPageAsync(_settings.UserAgent);
        try
        {
            page.Response += (_, r) =>
            {
                if (r.Url.Contains("zimmo.be", StringComparison.OrdinalIgnoreCase))
                {
                    r.Headers.TryGetValue("content-type", out var ctype);
                    capturedResponses.Add(new CapturedResponse(r.Url, r.Status, ctype ?? "", r));
                }
            };

            _logger.LogInformation("[DirectDetailTest] Navigeren naar URL...");
            IResponse? navResponse = null;
            try
            {
                navResponse = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout   = _settings.PlaywrightTimeoutMs
                });
            }
            catch (PlaywrightException ex)
            {
                _logger.LogWarning("[DirectDetailTest] GotoAsync fout: {Msg}", ex.Message);
            }

            await page.WaitForTimeoutAsync(2000);

            var httpStatus = navResponse?.Status ?? 0;
            var finalUrl   = page.Url;
            var pageTitle  = await page.TitleAsync();
            var html       = await page.ContentAsync();
            var bodyText   = await page.EvaluateAsync<string>(
                "() => (document.body?.innerText ?? '').slice(0, 200000)") ?? "";

            var initialSignals = EvaluateSignals(httpStatus, pageTitle, bodyText, html);

            _logger.LogInformation(
                "[DirectDetailTest] Initieel: HTTP {Status} | '{Title}' | {Bytes} bytes | " +
                "CloudflareSignals={CF} | PropertyContent={Prop}",
                httpStatus, pageTitle, html.Length,
                initialSignals.StrongCloudflareSignals.Count,
                initialSignals.PropertySignals.Count);

            _logger.LogInformation("[DirectDetailTest] Body (eerste 500 tekens):\n{Snip}",
                bodyText.Length > 500 ? bodyText[..500] : bodyText);

            var signals    = initialSignals;
            double waitSec = 0;

            bool needsLoop = signals.CloudflareBlocked && !signals.HasRealPropertyContent;

            if (needsLoop)
            {
                _logger.LogWarning(
                    "[DirectDetailTest] Cloudflare-signalen aanwezig, vastgoedcontent afwezig — " +
                    "wacht max 60s...");

                for (int sec = 0; sec < 60 && !ct.IsCancellationRequested; sec++)
                {
                    await page.WaitForTimeoutAsync(1_000);
                    waitSec = sec + 1;

                    var loopTitle  = await page.TitleAsync();
                    var loopBody   = await page.EvaluateAsync<string>(
                        "() => (document.body?.innerText ?? '').slice(0, 200000)") ?? "";
                    var loopHtml   = await page.ContentAsync();
                    var loopStatus = navResponse?.Status ?? 0;
                    var loopUrl    = page.Url;
                    var readyState = await page.EvaluateAsync<string>(
                        "() => document.readyState") ?? "unknown";

                    var loopSignals = EvaluateSignals(loopStatus, loopTitle, loopBody, loopHtml);

                    _logger.LogInformation(
                        "[DirectDetailTest] t={Sec}s | HTTP {Status} | URL={Url} | " +
                        "Title='{Title}' | readyState={RS} | bodyLen={BL} | " +
                        "CF={CF} | Prop={Prop} | body[0:500]={Snip}",
                        waitSec, loopStatus, loopUrl, loopTitle, readyState,
                        loopBody.Length,
                        loopSignals.StrongCloudflareSignals.Count,
                        loopSignals.PropertySignals.Count,
                        loopBody.Length > 500 ? loopBody[..500] : loopBody);

                    await LogDomSignalsAsync(page, (int)waitSec);

                    if (loopSignals.HasRealPropertyContent || loopSignals.ChallengeResolved)
                    {
                        _logger.LogInformation(
                            "[DirectDetailTest] Vastgoedcontent gevonden / challenge opgelost na {Sec}s!",
                            waitSec);

                        pageTitle = loopTitle;
                        html      = loopHtml;
                        bodyText  = loopBody;
                        signals   = loopSignals;
                        finalUrl  = page.Url;
                        break;
                    }
                }
            }
            else
            {
                await LogDomSignalsAsync(page, 0);
            }

            if (waitSec > 0)
            {
                html      = await page.ContentAsync();
                bodyText  = await page.EvaluateAsync<string>(
                    "() => (document.body?.innerText ?? '').slice(0, 200000)") ?? "";
                pageTitle = await page.TitleAsync();
                finalUrl  = page.Url;
                signals   = EvaluateSignals(httpStatus, pageTitle, bodyText, html);
            }

            var nextDataJson = await page.EvaluateAsync<string?>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); " +
                "return el ? el.textContent : null; }");

            var jsonLdScripts = await page.EvaluateAsync<string[]>(@"() =>
                Array.from(document.querySelectorAll('script[type=""application/ld+json""]'))
                    .map(s => s.textContent || '').filter(t => t.trim().length > 0)") ?? [];

            var scriptJsonSnippets = await page.EvaluateAsync<string[]>(@"() =>
                Array.from(document.querySelectorAll('script:not([src])'))
                    .map(s => (s.textContent || '').trim())
                    .filter(t => (t.startsWith('{') || t.startsWith('[')) && t.length > 20)
                    .slice(0, 20)") ?? [];

            var possibleJsonKeys = await page.EvaluateAsync<string[]>(@"() => {
                const keys = ['__NEXT_DATA__','__NUXT_DATA__','__INITIAL_STATE__',
                              '__APP_STATE__','__STORE__','__REDUX_STATE__'];
                return keys.filter(k => !!window[k]);
            }") ?? [];

            var geoScripts = await page.EvaluateAsync<int>(@"() =>
                Array.from(document.querySelectorAll('script:not([src])'))
                    .filter(s => {
                        const t = s.textContent || '';
                        return ['latitude','longitude','geo','price','address']
                               .some(k => t.includes(k));
                    }).length");

            // ── Nieuwbouwproject-velden ──────────────────────────────────────────────────
            var projectData = await ExtractProjectDataAsync(page);

            if (projectData is not null)
                _logger.LogInformation(
                    "[DirectDetailTest] Project: naam={Naam} | prijsVanaf=€{Prijs} | " +
                    "units={Units} | beschikbaar={Beschikbaar}%",
                    projectData.ProjectNaam ?? "?",
                    projectData.PrijsVanaf?.ToString("N0") ?? "?",
                    projectData.AantalUnits?.ToString() ?? "?",
                    projectData.BeschikbaarheidsPercentage?.ToString() ?? "?");

            // ── Parsen ────────────────────────────────────────────────────────────────────
            ListingDto? parsedDto = null;
            if (!string.IsNullOrEmpty(nextDataJson))
            {
                _logger.LogInformation(
                    "[DirectDetailTest] __NEXT_DATA__ gevonden ({Len} chars) — ZimmoListingParser...",
                    nextDataJson.Length);
                parsedDto = ZimmoListingParser.TryParseNextData(nextDataJson, url, _logger);
            }
            if (parsedDto is null)
            {
                _logger.LogInformation("[DirectDetailTest] Fallback: ZimmoListingParser.TryParseJsonLd...");
                parsedDto = ZimmoListingParser.TryParseJsonLd(html, url, _logger);
            }

            if (parsedDto is not null)
            {
                parsedDto.NavigationUrl = url;
                parsedDto.CanonicalUrl  = url;
                _logger.LogInformation(
                    "[DirectDetailTest] Geparsed: Prijs=€{P} | {Zip} {City} | {Area}m² | " +
                    "{Beds} slaapk. | lat={Lat} lon={Lon}",
                    parsedDto.AskingPrice?.ToString("N0") ?? "?",
                    parsedDto.PostalCode ?? "?", parsedDto.City ?? "?",
                    parsedDto.LivingArea?.ToString() ?? "?",
                    parsedDto.Bedrooms?.ToString() ?? "?",
                    parsedDto.Latitude?.ToString() ?? "?",
                    parsedDto.Longitude?.ToString() ?? "?");
            }
            else
            {
                _logger.LogWarning("[DirectDetailTest] Geen ListingDto geparsed.");
            }

            if (signals.CloudflareBlocked)
                _logger.LogWarning(
                    "[DirectDetailTest] CONCLUSIE: Detailpagina effectief geblokkeerd. " +
                    "Geen vastgoedcontent gevonden.");
            else if (signals.HasRealPropertyContent)
                _logger.LogInformation(
                    "[DirectDetailTest] CONCLUSIE: Vastgoedcontent gevonden — pagina geladen! " +
                    "Signalen: [{Signals}]",
                    string.Join(", ", signals.PropertySignals));
            else
                _logger.LogInformation(
                    "[DirectDetailTest] CONCLUSIE: Geen Cloudflare-blokkering maar ook " +
                    "geen vastgoedcontent herkend. Controleer debug-bestanden.");

            // ── Network responses ────────────────────────────────────────────────────────
            await page.WaitForTimeoutAsync(300);
            var networkLines = new List<string>();
            foreach (var cap in capturedResponses)
            {
                try
                {
                    string body;
                    try   { body = await cap.Response.TextAsync(); }
                    catch { body = string.Empty; }
                    var line = JsonSerializer.Serialize(new
                    {
                        cap.Url, cap.Status, cap.ContentType,
                        BodyPreview = body.Length > 4000 ? body[..4000] + "…" : body
                    }, CompactJson);
                    networkLines.Add(line);
                }
                catch { /* respons al verlopen */ }
            }

            // ── Debug-bestanden ──────────────────────────────────────────────────────────
            await File.WriteAllTextAsync(
                Path.Combine(debugDir, $"direct-detail-{slug}.html"), html, ct);

            await File.WriteAllTextAsync(
                Path.Combine(debugDir, $"direct-detail-{slug}-body.txt"), bodyText, ct);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path     = Path.Combine(debugDir, $"direct-detail-{slug}.png"),
                FullPage = false
            });

            if (networkLines.Count > 0)
                await File.WriteAllLinesAsync(
                    Path.Combine(debugDir, $"direct-detail-{slug}-network.jsonl"), networkLines, ct);

            if (!string.IsNullOrEmpty(nextDataJson))
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, $"direct-detail-{slug}-next-data.json"), nextDataJson, ct);

            if (jsonLdScripts.Length > 0)
                await File.WriteAllTextAsync(
                    Path.Combine(debugDir, $"direct-detail-{slug}-json-ld.json"),
                    JsonSerializer.Serialize(jsonLdScripts, PrettyJson), ct);

            var summary = new
            {
                Url                          = url,
                Slug                         = slug,
                HttpStatus                   = httpStatus,
                FinalUrl                     = finalUrl,
                Title                        = pageTitle,
                HtmlBytes                    = html.Length,
                BodyLength                   = bodyText.Length,
                CloudflareDetected           = signals.AnyCloudflareSignal,
                CloudflareBlocked            = signals.CloudflareBlocked,
                ChallengeResolved            = signals.ChallengeResolved,
                HasRealPropertyContent       = signals.HasRealPropertyContent,
                PropertySignalsFound         = signals.PropertySignals,
                StrongCloudflareSignalsFound = signals.StrongCloudflareSignals,
                JsonLdCount                  = jsonLdScripts.Length,
                ScriptJsonCount              = scriptJsonSnippets.Length,
                ScriptsWithGeoOrPrice        = geoScripts,
                PossibleJsonKeys             = possibleJsonKeys,
                Project                      = projectData,
                FoundPrice                   = parsedDto?.AskingPrice,
                FoundAddress                 = parsedDto is null ? null
                    : string.Join(" ", new[] { parsedDto.Street, parsedDto.HouseNumber }
                        .Where(s => !string.IsNullOrWhiteSpace(s))),
                FoundPostalCode              = parsedDto?.PostalCode,
                FoundCity                    = parsedDto?.City,
                FoundLivingArea              = parsedDto?.LivingArea,
                FoundBedrooms                = parsedDto?.Bedrooms,
                FoundLatitude                = parsedDto?.Latitude,
                FoundLongitude               = parsedDto?.Longitude,
                SecondsWaited                = waitSec,
                Timestamp                    = DateTime.UtcNow.ToString("O")
            };

            await File.WriteAllTextAsync(
                Path.Combine(debugDir, $"direct-detail-{slug}-summary.json"),
                JsonSerializer.Serialize(summary, PrettyJson), ct);

            _logger.LogInformation(
                "[DirectDetailTest] Debug-bestanden opgeslagen → {Dir}", debugDir);
            _logger.LogInformation("[DirectDetailTest] Gereed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DirectDetailTest] Onverwachte fout");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    // ── Nieuwbouwproject-velden extractie ────────────────────────────────────────────────

    private async Task<ProjectData?> ExtractProjectDataAsync(IPage page)
    {
        try
        {
            var result = await page.EvaluateAsync<JsonElement?>(@"() => {
                try {
                    const nd = window.__NEXT_DATA__;
                    if (!nd) return null;

                    const pp  = nd?.props?.pageProps ?? {};
                    const prop = pp.property ?? pp.listing ?? pp.project
                               ?? pp.data?.property ?? pp.data ?? null;
                    if (!prop) return null;

                    const units = prop.units ?? prop.lots ?? prop.houses
                                ?? prop.residences ?? prop.apartments ?? [];
                    const available = units.filter(u =>
                        u.status === 'available' || u.available === true ||
                        (u.statusLabel ?? '').toLowerCase().includes('beschikbaar'));

                    const prices = units.map(u =>
                        u.price ?? u.askingPrice ?? u.priceFrom).filter(v => v != null && v > 0);
                    const types  = [...new Set(
                        units.map(u => u.type ?? u.propertyType ?? u.typeLabel).filter(Boolean))];
                    const beds   = [...new Set(
                        units.map(u => u.bedrooms ?? u.bedroomsCount).filter(v => v != null))].sort((a,b)=>a-b);
                    const areas  = units.map(u =>
                        u.livingArea ?? u.surface ?? u.area).filter(v => v != null && v > 0);

                    const addr   = prop.address ?? prop.location ?? {};
                    const adresStr = [addr.street, addr.houseNumber, addr.postalCode, addr.city]
                        .filter(Boolean).join(' ').trim();

                    return {
                        projectNaam:
                            prop.name ?? prop.title ?? prop.projectName ?? null,
                        adres:
                            adresStr || null,
                        prijsVanaf:
                            prop.priceFrom ?? prop.minPrice ?? prop.price?.from
                            ?? (prices.length > 0 ? Math.min(...prices) : null),
                        aantalUnits:
                            units.length,
                        beschikbaarheidsPercentage:
                            units.length > 0
                                ? Math.round(available.length / units.length * 100)
                                : null,
                        unitPrijzen:   prices.slice(0, 30),
                        unitTypes:     types,
                        unitSlaapkamers: beds,
                        unitOppervlaktes: areas.slice(0, 30)
                    };
                } catch (e) {
                    return { error: e.message };
                }
            }");

            if (result is null || result.Value.ValueKind == JsonValueKind.Null)
                return null;

            var obj = result.Value;

            if (obj.TryGetProperty("error", out var errProp))
            {
                _logger.LogDebug("[DirectDetailTest] ExtractProjectData JS: {Err}", errProp.GetString());
                return null;
            }

            static decimal? D(JsonElement o, string k) =>
                o.TryGetProperty(k, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : null;
            static int? I(JsonElement o, string k) =>
                o.TryGetProperty(k, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : null;
            static string? S(JsonElement o, string k) =>
                o.TryGetProperty(k, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

            static List<decimal> Ds(JsonElement o, string k) =>
                o.TryGetProperty(k, out var p) && p.ValueKind == JsonValueKind.Array
                    ? p.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Number).Select(e => e.GetDecimal()).ToList()
                    : [];

            static List<string> Ss(JsonElement o, string k) =>
                o.TryGetProperty(k, out var p) && p.ValueKind == JsonValueKind.Array
                    ? p.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList()
                    : [];

            static List<int> Is(JsonElement o, string k) =>
                o.TryGetProperty(k, out var p) && p.ValueKind == JsonValueKind.Array
                    ? p.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Number).Select(e => e.GetInt32()).ToList()
                    : [];

            var data = new ProjectData(
                ProjectNaam:                    S(obj, "projectNaam"),
                Adres:                          S(obj, "adres"),
                PrijsVanaf:                     D(obj, "prijsVanaf"),
                AantalUnits:                    I(obj, "aantalUnits"),
                BeschikbaarheidsPercentage:     I(obj, "beschikbaarheidsPercentage"),
                UnitPrijzen:                    Ds(obj, "unitPrijzen"),
                UnitTypes:                      Ss(obj, "unitTypes"),
                UnitSlaapkamers:                Is(obj, "unitSlaapkamers"),
                UnitOppervlaktes:               Ds(obj, "unitOppervlaktes"));

            // Alleen teruggeven als er iets nuttigs is
            bool hasContent = data.ProjectNaam is not null
                || data.AantalUnits > 0
                || data.PrijsVanaf is not null;

            return hasContent ? data : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[DirectDetailTest] ExtractProjectDataAsync niet-fatale fout");
            return null;
        }
    }

    // ── Per-seconde DOM-signaal logging ──────────────────────────────────────────────────

    private async Task LogDomSignalsAsync(IPage page, int elapsedSec)
    {
        try
        {
            var priceEls = await page.EvaluateAsync<int>(@"() =>
                document.querySelectorAll(
                    '[class*=""price"" i],[class*=""prijs"" i],[data-testid*=""price"" i],' +
                    '[itemprop=""price""],[itemprop=""offers""]').length");

            var addrEls = await page.EvaluateAsync<int>(@"() =>
                document.querySelectorAll(
                    '[class*=""address"" i],[class*=""adres"" i],[itemprop=""address""],' +
                    '[class*=""location"" i],[class*=""locatie"" i]').length");

            var featEls = await page.EvaluateAsync<int>(@"() =>
                document.querySelectorAll(
                    '[class*=""feature"" i],[class*=""kenmerk"" i],[class*=""spec"" i],' +
                    '[class*=""detail"" i],[class*=""surface"" i],[class*=""bedroom"" i]').length");

            var jsonLdTypes = await page.EvaluateAsync<string[]>(@"() =>
                Array.from(document.querySelectorAll('script[type=""application/ld+json""]'))
                    .flatMap(s => {
                        try {
                            const d = JSON.parse(s.textContent || '');
                            const items = Array.isArray(d) ? d : [d];
                            return items.map(i => i['@type']).filter(Boolean);
                        } catch { return []; }
                    })") ?? [];

            _logger.LogInformation(
                "[DirectDetailTest] DOM t={Sec}s: priceEls={P} | addrEls={A} | featEls={F} | " +
                "jsonLdTypes=[{Types}]",
                elapsedSec, priceEls, addrEls, featEls,
                string.Join(", ", jsonLdTypes));
        }
        catch
        {
            // DOM-inspectie is best-effort
        }
    }

    // ── Slug extractie ────────────────────────────────────────────────────────────────────

    private static string ExtractSlug(string url)
    {
        try
        {
            var last = new Uri(url).Segments
                .Select(s => s.Trim('/'))
                .LastOrDefault(s => !string.IsNullOrEmpty(s));
            return last ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    // ── Signaal-evaluatie ────────────────────────────────────────────────────────────────

    private static PageSignals EvaluateSignals(int httpStatus, string title, string body, string html)
    {
        var strongCfSignals = new List<string>();
        var propertySignals = new List<string>();
        var lowerBody       = body.ToLowerInvariant();
        var lowerHtml       = html.ToLowerInvariant();

        // ── Sterke Cloudflare-signalen ─────────────────────────────────────────────────
        if (httpStatus == 403)
            strongCfSignals.Add("HTTP_403");
        if (title.Contains("Even geduld", StringComparison.OrdinalIgnoreCase))
            strongCfSignals.Add("title:Even_geduld");
        if (title.Contains("Just a moment", StringComparison.OrdinalIgnoreCase))
            strongCfSignals.Add("title:Just_a_moment");
        if (lowerBody.Contains("beveiliging wordt geverifieerd"))
            strongCfSignals.Add("body:beveiliging_wordt_geverifieerd");
        if (lowerBody.Contains("checking your browser"))
            strongCfSignals.Add("body:checking_your_browser");
        if (lowerHtml.Contains("cf-challenge"))
            strongCfSignals.Add("html:cf-challenge");
        if (lowerHtml.Contains("challenge-form"))
            strongCfSignals.Add("html:challenge-form");
        if (lowerBody.Contains("cloudflare ray id"))
            strongCfSignals.Add("body:cloudflare_ray_id");

        // ── Vastgoed-signalen: prijs ───────────────────────────────────────────────────
        if (lowerBody.Contains('€'))
            propertySignals.Add("price:€");
        if (lowerBody.Contains("vraagprijs"))
            propertySignals.Add("price:vraagprijs");
        if (lowerBody.Contains("prijs"))
            propertySignals.Add("price:prijs");
        if (lowerBody.Contains("prijs vanaf") || lowerBody.Contains("vanaf"))
            propertySignals.Add("price:vanaf");

        // ── Vastgoed-signalen: kenmerken ───────────────────────────────────────────────
        if (lowerBody.Contains("slaapkamer"))
            propertySignals.Add("prop:slaapkamer");
        if (lowerBody.Contains("m²"))
            propertySignals.Add("prop:m²");
        if (lowerBody.Contains("bewoonbare oppervlakte") || lowerBody.Contains("oppervlakte"))
            propertySignals.Add("prop:oppervlakte");
        if (lowerBody.Contains("garage") || lowerBody.Contains("carport"))
            propertySignals.Add("prop:garage");
        if (lowerBody.Contains("terras") || lowerBody.Contains("tuin"))
            propertySignals.Add("prop:terras_tuin");
        if (lowerBody.Contains("nieuwbouw") || lowerBody.Contains("nieuwbouwproject"))
            propertySignals.Add("type:nieuwbouw");
        if (lowerBody.Contains("appartement") || lowerBody.Contains("woning") || lowerBody.Contains("huis"))
            propertySignals.Add("type:woning_appartement");

        // ── Vastgoed-signalen: structured data ─────────────────────────────────────────
        if (lowerHtml.Contains("\"@type\"") && (
            lowerHtml.Contains("realestate") || lowerHtml.Contains("apartment") ||
            lowerHtml.Contains("residence")  || lowerHtml.Contains("singlefamily") ||
            lowerHtml.Contains("house")))
            propertySignals.Add("ld:realestate_type");

        if (lowerHtml.Contains("schema.org"))
            propertySignals.Add("ld:schema.org");

        foreach (var key in new[] { "\"offer\"", "\"address\"", "\"geo\"",
                                    "\"latitude\"", "\"longitude\"" })
        {
            if (lowerHtml.Contains(key))
                propertySignals.Add($"ld:{key.Trim('"')}");
        }

        bool anyCloudflare    = strongCfSignals.Count > 0;
        bool hasProperty      = propertySignals.Count >= 2;
        bool challengeResolved = hasProperty || (httpStatus == 200 && !anyCloudflare);
        bool cloudflareBlocked = anyCloudflare && !hasProperty;

        return new PageSignals(
            StrongCloudflareSignals : strongCfSignals,
            PropertySignals         : propertySignals,
            AnyCloudflareSignal     : anyCloudflare,
            HasRealPropertyContent  : hasProperty,
            ChallengeResolved       : challengeResolved,
            CloudflareBlocked       : cloudflareBlocked);
    }

    // ── Records ──────────────────────────────────────────────────────────────────────────

    private sealed record ProjectData(
        string?       ProjectNaam,
        string?       Adres,
        decimal?      PrijsVanaf,
        int?          AantalUnits,
        int?          BeschikbaarheidsPercentage,
        List<decimal> UnitPrijzen,
        List<string>  UnitTypes,
        List<int>     UnitSlaapkamers,
        List<decimal> UnitOppervlaktes);

    private sealed record PageSignals(
        List<string> StrongCloudflareSignals,
        List<string> PropertySignals,
        bool AnyCloudflareSignal,
        bool HasRealPropertyContent,
        bool ChallengeResolved,
        bool CloudflareBlocked);

    private sealed record CapturedResponse(
        string    Url,
        int       Status,
        string    ContentType,
        IResponse Response);
}
