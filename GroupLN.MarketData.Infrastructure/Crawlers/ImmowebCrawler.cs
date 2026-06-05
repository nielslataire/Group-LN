using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Browser;
using GroupLN.MarketData.Infrastructure.Crawlers.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

public class ImmowebCrawler : BaseCrawler
{
    private readonly PlaywrightBrowserService _browser;

    private const string SearchBase = "https://www.immoweb.be/en/search/{0}/for-sale?countries=BE&maxItems=60&orderBy=newest&page={1}{2}";
    private static readonly string[] PropertyTypes = { "house", "apartment" };
    private const int MaxPages = 10;

    public ImmowebCrawler(
        IMarketPropertyService propertyService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        PlaywrightBrowserService browser,
        ILogger<ImmowebCrawler> logger)
        : base(propertyService, normalizer, settings, logger)
    {
        _browser = browser;
    }

    public override string SourceName => "Immoweb";

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        // Postcode-filter in de URL — verlaagt het aantal te verwerken pagina's
        var postalFilter = string.Empty;
        if (Settings.AllowedPostalCodes.Count > 0)
        {
            postalFilter = string.Concat(Settings.AllowedPostalCodes.Select(pc => $"&postalCodes[]={pc}"));
            Logger.LogInformation("[Immoweb] Postcode-filter in zoek-URL: {Postcodes}",
                string.Join(", ", Settings.AllowedPostalCodes));
        }

        // Bij MaxListingsPerRun verminderen we het aantal pagina's (60 per pagina)
        var maxPages = Settings.MaxListingsPerRun > 0
            ? Math.Max(1, (int)Math.Ceiling(Settings.MaxListingsPerRun / 60.0) + 1)
            : MaxPages;

        if (Settings.MaxListingsPerRun > 0)
            Logger.LogInformation("[Immoweb] MaxListingsPerRun={Max} → maximaal {Pages} zoekpagina(s) per type.",
                Settings.MaxListingsPerRun, maxPages);

        var urls = PropertyTypes
            .SelectMany(type => Enumerable.Range(1, maxPages)
                .Select(page => string.Format(SearchBase, type, page, postalFilter)));

        return Task.FromResult(urls);
    }

    protected override async Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
    {
        var urls = new List<string>();
        IPage? page = null;

        try
        {
            page = await _browser.NewPageAsync(Settings.UserAgent);

            Logger.LogDebug("[Immoweb] Navigeren naar zoekpagina: {Url}", searchPageUrl);

            await page.GotoAsync(searchPageUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = Settings.PlaywrightTimeoutMs
            });

            // Immoweb is een Next.js app — data staat in __NEXT_DATA__
            var json = await page.EvaluateAsync<string>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); return el ? el.textContent : null; }");

            if (!string.IsNullOrEmpty(json))
            {
                var extracted = ExtractUrlsFromNextData(json).ToList();
                urls.AddRange(extracted);
                Logger.LogDebug("[Immoweb] __NEXT_DATA__ gevonden: {Count} listing-ID's geëxtraheerd.", extracted.Count);
            }
            else
            {
                // Fallback: zoek directe links in de HTML
                Logger.LogDebug("[Immoweb] Geen __NEXT_DATA__ — fallback naar anchor tags.");
                var anchors = await page.QuerySelectorAllAsync("a[href*='/classified/']");
                foreach (var anchor in anchors)
                {
                    var href = await anchor.GetAttributeAsync("href");
                    if (!string.IsNullOrEmpty(href) && href.Contains("/classified/"))
                    {
                        var fullUrl = href.StartsWith("http") ? href : $"https://www.immoweb.be{href}";
                        urls.Add(fullUrl);
                    }
                }
                Logger.LogDebug("[Immoweb] Fallback anchor tags: {Count} links gevonden.", urls.Count);
            }

            if (urls.Count == 0)
                Logger.LogWarning("[Immoweb] Geen listings gevonden op {Url}. Mogelijk einde van resultaten of website-wijziging.", searchPageUrl);
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout"))
        {
            Logger.LogWarning("[Immoweb] Timeout bij laden van zoekpagina {Url} (>{Timeout}ms).",
                searchPageUrl, Settings.PlaywrightTimeoutMs);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[Immoweb] Fout bij ophalen van zoekpagina {Url}.", searchPageUrl);
        }
        finally
        {
            if (page is not null)
            {
                var context = page.Context;
                await page.CloseAsync();
                await context.DisposeAsync();
            }
        }

        return urls.Distinct();
    }

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

            // Primaire bron: window.classified (beschikbaar op de meeste Immoweb-pagina's)
            var classifiedJson = await page.EvaluateAsync<string>(
                "() => typeof window.classified !== 'undefined' ? JSON.stringify(window.classified) : null");

            if (!string.IsNullOrEmpty(classifiedJson))
            {
                var dto = ParseClassifiedJson(classifiedJson, listingUrl);
                if (dto is not null)
                {
                    Logger.LogDebug("[Immoweb] Parsed via window.classified: {Id} | {City} | €{Price}",
                        dto.ExternalId, dto.City ?? "?", dto.AskingPrice?.ToString("N0") ?? "?");
                    return dto;
                }
            }

            // Fallback: __NEXT_DATA__
            var nextDataJson = await page.EvaluateAsync<string>(
                "() => { const el = document.getElementById('__NEXT_DATA__'); return el ? el.textContent : null; }");

            if (!string.IsNullOrEmpty(nextDataJson))
            {
                var dto = ParseNextDataJson(nextDataJson, listingUrl);
                if (dto is not null)
                {
                    Logger.LogDebug("[Immoweb] Parsed via __NEXT_DATA__: {Id} | {City}", dto.ExternalId, dto.City ?? "?");
                    return dto;
                }
            }

            Logger.LogWarning("[Immoweb] Geen data gevonden op {Url}. Controleer of de paginastructuur gewijzigd is.", listingUrl);
            return null;
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Timeout"))
        {
            Logger.LogWarning("[Immoweb] Timeout bij laden van detailpagina {Url}.", listingUrl);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[Immoweb] Fout bij parseren van detailpagina {Url}.", listingUrl);
            return null;
        }
        finally
        {
            if (page is not null)
            {
                var context = page.Context;
                await page.CloseAsync();
                await context.DisposeAsync();
            }
        }
    }

    private static IEnumerable<string> ExtractUrlsFromNextData(string json)
    {
        // Immoweb's Next.js structuur (kan veranderen bij updates van hun platform)
        // Aanpassen als de structuur wijzigt
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch { yield break; }

        // Zoek listings in de bekende paden
        var results = root?["props"]?["pageProps"]?["results"]?.AsArray()
                   ?? root?["props"]?["pageProps"]?["listings"]?.AsArray();

        if (results is null) yield break;

        foreach (var item in results)
        {
            var id = item?["id"]?.GetValue<long?>() ?? item?["classified"]?["id"]?.GetValue<long?>();
            if (id.HasValue)
                yield return $"https://www.immoweb.be/en/classified/{id.Value}";
        }
    }

    private static ListingDto? ParseClassifiedJson(string json, string url)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch { return null; }

        if (root is null) return null;

        var id = root["id"]?.GetValue<long?>();
        if (!id.HasValue) return null;

        // Prijs — meerdere mogelijke locaties door Immoweb-versies
        var price = TryGetDecimal(root["transaction"]?["sale"]?["price"])
                 ?? TryGetDecimal(root["price"]?["mainValue"])
                 ?? TryGetDecimal(root["transaction"]?["rental"]?["monthlyRent"]);

        // Locatie
        var location = root["property"]?["location"];
        var postalCode = location?["postalCode"]?.GetValue<string>();
        var city = location?["locality"]?.GetValue<string>()
                ?? location?["city"]?.GetValue<string>();
        var street = location?["street"]?.GetValue<string>();
        var number = location?["number"]?.GetValue<string>();
        var lat = TryGetDecimal(location?["latitude"]);
        var lon = TryGetDecimal(location?["longitude"]);

        // Kenmerken
        var prop = root["property"];
        var living = TryGetDecimal(prop?["netHabitableSurface"])
                  ?? TryGetDecimal(prop?["habitableSurface"]);
        var land = TryGetDecimal(prop?["land"]?["surface"]);
        var bedrooms = prop?["bedroomCount"]?.GetValue<int?>();
        var bathrooms = prop?["bathroomCount"]?.GetValue<int?>();
        var buildYear = prop?["building"]?["constructionYear"]?.GetValue<int?>();
        var epcScore = TryGetDecimal(prop?["energy"]?["primaryEnergyConsumptionPerSqm"]);
        var epcLabel = prop?["energy"]?["epcScoreClass"]?.GetValue<string>();
        var typeRaw = prop?["type"]?.GetValue<string>();
        var subTypeRaw = prop?["subtype"]?.GetValue<string>();
        var condition = prop?["building"]?["condition"]?.GetValue<string>() ?? string.Empty;
        var isNew = condition.Contains("NEW_CONSTRUCTION", StringComparison.OrdinalIgnoreCase);

        var transactionType = root["transaction"]?["type"]?.GetValue<string>() ?? "FOR_SALE";

        return new ListingDto
        {
            ExternalId = id.Value.ToString(),
            Url = url,
            Title = string.IsNullOrEmpty(city) ? typeRaw : $"{typeRaw} in {city}",
            PropertyTypeRaw = typeRaw,
            PropertySubTypeRaw = subTypeRaw,
            TransactionTypeRaw = transactionType,
            PostalCode = postalCode,
            City = city,
            Street = street,
            HouseNumber = number,
            Latitude = lat,
            Longitude = lon,
            AskingPrice = price,
            LivingArea = living,
            LandArea = land,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            ConstructionYear = buildYear,
            EPCScore = epcScore,
            EPCLabelRaw = epcLabel,
            IsNewBuild = isNew,
            RawJson = json.Length > 50000 ? json[..50000] : json  // cap om DB-grootte te beperken
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
}
