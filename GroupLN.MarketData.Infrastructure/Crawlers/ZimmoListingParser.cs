using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GroupLN.MarketData.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

/// <summary>
/// Parseert Zimmo detailpagina's van reguliere woningen (huis, appartement, …).
/// Probeert meerdere __NEXT_DATA__ root-paths — logging toont welke werkt bij de eerste run.
/// </summary>
public static partial class ZimmoListingParser
{
    // Zimmo URL-patroon: /nl/{gemeente}-{postcode}/te-koop/{type}/{externalId}/
    [GeneratedRegex(@"/te-koop/(?:huis|appartement|studio|duplex|penthouse|villa|grond|garage|gebouw|commercieel|kantoor)/([A-Za-z0-9]+)/?",
        RegexOptions.IgnoreCase)]
    private static partial Regex ExternalIdFromUrlRegex();

    // Mogelijke root-keys voor de listing-data in props.pageProps
    private static readonly string[] ListingRootKeys =
    [
        "propertyDetails",
        "listing",
        "listingDetails",
        "property",
        "singleListing",
        "propertyData",
        "ad",
    ];

    /// <summary>
    /// Probeert een ListingDto te bouwen uit de __NEXT_DATA__ JSON van een detailpagina.
    /// </summary>
    public static ListingDto? TryParseNextData(string json, string url, ILogger logger)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (Exception ex)
        {
            logger.LogDebug("[ZimmoParser] __NEXT_DATA__ JSON parse fout: {Msg}", ex.Message);
            return null;
        }

        var pageProps = root?["props"]?["pageProps"];
        if (pageProps is null)
        {
            logger.LogDebug("[ZimmoParser] props.pageProps niet gevonden in __NEXT_DATA__.");
            return null;
        }

        // Log beschikbare keys voor debug
        if (pageProps is JsonObject obj)
            logger.LogDebug("[ZimmoParser] pageProps keys: {Keys}", string.Join(", ", obj.Select(k => k.Key)));

        // Probeer elke mogelijke root-key
        foreach (var key in ListingRootKeys)
        {
            var node = pageProps[key];
            if (node is null) continue;

            logger.LogDebug("[ZimmoParser] Listing-data gevonden onder pageProps.{Key}", key);
            var dto = ParsePropertyNode(node, url, $"pageProps.{key}", logger);
            if (dto is not null)
            {
                dto.RawJson = json.Length > 50_000 ? json[..50_000] : json;
                return dto;
            }
        }

        logger.LogWarning("[ZimmoParser] Geen listing-data gevonden in __NEXT_DATA__ voor {Url}", url);
        return null;
    }

    /// <summary>
    /// Probeert een ListingDto te bouwen uit JSON-LD op de pagina (fallback).
    /// </summary>
    public static ListingDto? TryParseJsonLd(string html, string url, ILogger logger)
    {
        foreach (Match m in JsonLdRegex().Matches(html))
        {
            var json = m.Groups[1].Value.Trim();
            try
            {
                var node = JsonNode.Parse(json);
                if (node is null) continue;

                var type = node["@type"]?.GetValue<string>() ?? "";
                if (!type.Contains("RealEstate", StringComparison.OrdinalIgnoreCase)
                    && !type.Contains("Product", StringComparison.OrdinalIgnoreCase))
                    continue;

                logger.LogInformation("[ZimmoParser] JSON-LD gevonden: @type={Type}", type);

                var externalId = ExtractExternalIdFromUrl(url);
                if (string.IsNullOrEmpty(externalId)) continue;

                var addrNode = node["address"];
                return ZimmoDtoMapper.Build(
                    externalId       : externalId,
                    url              : url,
                    title            : Str(node, "name"),
                    zimmoPropertyType: null,
                    zimmoPropertySubType: null,
                    zimmoTransactionType: "FOR_SALE",
                    postalCode       : Str(addrNode, "postalCode"),
                    city             : Str(addrNode, "addressLocality", "city"),
                    street           : Str(addrNode, "streetAddress"),
                    houseNumber      : null,
                    latitude         : Dec(node["geo"], "latitude"),
                    longitude        : Dec(node["geo"], "longitude"),
                    askingPrice      : Dec(node["offers"], "price") ?? Dec(node, "price"),
                    maxPrice         : null,
                    livingArea       : null,
                    landArea         : null,
                    bedrooms         : null,
                    bathrooms        : null,
                    epcLabel         : null,
                    epcScore         : null,
                    isNewConstruction: null,
                    developerName    : null,
                    developerPhone   : null,
                    developerWebsite : null,
                    projectName      : null,
                    description      : Str(node, "description"),
                    rawJson          : json);
            }
            catch { /* ongeldig JSON-LD, probeer volgende */ }
        }

        return null;
    }

    // ── Kern-parser ────────────────────────────────────────────────────────────

    private static ListingDto? ParsePropertyNode(
        JsonNode node, string url, string parseSource, ILogger logger)
    {
        var externalId = Str(node, "id", "listingId", "propertyId", "code", "slug")
                      ?? ExtractExternalIdFromUrl(url);

        if (string.IsNullOrEmpty(externalId))
        {
            logger.LogWarning("[ZimmoParser] ExternalId niet gevonden via {Source}", parseSource);
            return null;
        }

        // ── Adres ──────────────────────────────────────────────────────────────
        var addrNode  = node["address"] ?? node["location"] ?? node["geo"];
        var postalCode = Str(addrNode, "zipCode", "postalCode", "zip", "postal");
        var city       = Str(addrNode, "city", "municipality", "locality", "commune");
        var street     = Str(addrNode, "street", "streetName", "streetAddress");
        var houseNumber = Str(addrNode, "number", "houseNumber", "streetNumber", "housenumber");

        // ── Coördinaten ────────────────────────────────────────────────────────
        var coordNode = node["coordinates"] ?? node["geo"] ?? node["geoLocation"] ?? addrNode;
        var latitude   = Dec(coordNode, "lat", "latitude");
        var longitude  = Dec(coordNode, "lng", "lon", "longitude");

        // ── Prijs ──────────────────────────────────────────────────────────────
        var priceNode  = node["price"] ?? node["askingPrice"] ?? node["salePrice"];
        var askingPrice = Dec(priceNode, "amount", "value", "askingPrice", "price")
                       ?? Dec(node, "price", "askingPrice", "amount");

        // ── Kenmerken ──────────────────────────────────────────────────────────
        var chars = node["characteristics"] ?? node["features"] ?? node["attributes"] ?? node["specs"];
        var surface = node["surface"] ?? node["surfaces"] ?? chars;

        var livingArea = Dec(surface, "livable", "livingArea", "habitableSurface", "usableArea", "net")
                      ?? Dec(chars, "livingArea", "habitableSurface", "livableArea", "netSurface");
        var landArea   = Dec(surface, "terrain", "plotArea", "landArea", "groundArea", "terrainArea")
                      ?? Dec(chars, "plotArea", "landArea", "groundSurface");

        var bedrooms  = Int(chars ?? node, "numberOfBedrooms", "bedrooms", "bedroomCount", "rooms");
        var bathrooms = Int(chars ?? node, "numberOfBathrooms", "bathrooms", "bathroomCount");

        // ── EPC ────────────────────────────────────────────────────────────────
        var epcNode  = node["energyPerformance"] ?? node["epc"] ?? node["energy"] ?? chars?["epc"];
        var epcLabel = Str(epcNode, "label", "energyClass", "epcLabel")
                    ?? Str(node, "energyLabel", "epcLabel", "epcClass");
        var epcScore = Dec(epcNode, "score", "value", "consumption", "primaryEnergyConsumption")
                    ?? Dec(node, "primaryEnergyConsumption", "epcScore", "energyScore");

        // ── Type en status ─────────────────────────────────────────────────────
        var propertyType    = Str(node, "category", "type", "propertyType", "kind");
        var propertySubType = Str(node, "subCategory", "subtype", "propertySubType", "subKind");
        var txType          = Str(node, "transactionType", "status", "saleStatus", "listingType");

        // ── Nieuwbouw ──────────────────────────────────────────────────────────
        var isNew = BoolVal(node, "isNewConstruction", "isNewBuild", "isNewlyBuilt", "newBuild");

        // ── Developer ──────────────────────────────────────────────────────────
        var devNode = node["developer"] ?? node["promoter"] ?? node["agency"] ?? node["agent"];
        var devName    = Str(devNode, "name", "companyName", "agencyName");
        var devPhone   = Str(devNode, "phone", "telephone", "phoneNumber");
        var devWebsite = Str(devNode, "website", "url", "siteUrl");

        // ── Titel / omschrijving ───────────────────────────────────────────────
        var title       = Str(node, "title", "name", "heading", "pageTitle");
        var description = Str(node, "description", "body", "text");

        // ── Rapport ────────────────────────────────────────────────────────────
        logger.LogInformation(
            "[ZimmoParser] {Source}: ID={Id} | {PostalCode} {City} | €{Price} | {Type} | " +
            "Opp={Area}m² | Kamers={Beds} | EPC={Epc}",
            parseSource, externalId, postalCode ?? "?", city ?? "?",
            askingPrice?.ToString("N0") ?? "?", propertyType ?? "?",
            livingArea?.ToString() ?? "?", bedrooms?.ToString() ?? "?", epcLabel ?? "?");

        LogMissingFields(logger, parseSource, externalId,
            (postalCode,   "postalCode"),
            (city,         "city"),
            (askingPrice?.ToString(), "askingPrice"),
            (livingArea?.ToString(),  "livingArea"),
            (bedrooms?.ToString(),    "bedrooms"),
            (epcLabel,     "epcLabel"));

        return ZimmoDtoMapper.Build(
            externalId          : externalId,
            url                 : url,
            title               : title,
            zimmoPropertyType   : propertyType,
            zimmoPropertySubType: propertySubType,
            zimmoTransactionType: txType,
            postalCode          : postalCode,
            city                : city,
            street              : street,
            houseNumber         : houseNumber,
            latitude            : latitude,
            longitude           : longitude,
            askingPrice         : askingPrice,
            maxPrice            : null,
            livingArea          : livingArea,
            landArea            : landArea,
            bedrooms            : bedrooms,
            bathrooms           : bathrooms,
            epcLabel            : epcLabel,
            epcScore            : epcScore,
            isNewConstruction   : isNew,
            developerName       : devName,
            developerPhone      : devPhone,
            developerWebsite    : devWebsite,
            projectName         : null,
            description         : description,
            rawJson             : null);  // caller vult rawJson in
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    public static string? ExtractExternalIdFromUrl(string url)
    {
        var m = ExternalIdFromUrlRegex().Match(url);
        if (m.Success) return m.Groups[1].Value;

        // Fallback: laatste pad-segment vóór querystring
        var withoutQuery = url.Split('?')[0].TrimEnd('/');
        var lastSlash    = withoutQuery.LastIndexOf('/');
        if (lastSlash < 0) return null;
        var segment = withoutQuery[(lastSlash + 1)..];
        return string.IsNullOrEmpty(segment) ? null : segment;
    }

    private static string? Str(JsonNode? node, params string[] keys)
    {
        if (node is null) return null;
        foreach (var k in keys)
        {
            try
            {
                var v = node[k];
                if (v is not null) return v.GetValue<string>();
            }
            catch { /* type-mismatch */ }
        }
        return null;
    }

    private static decimal? Dec(JsonNode? node, params string[] keys)
    {
        if (node is null) return null;
        foreach (var k in keys)
        {
            try
            {
                var v = node[k];
                if (v is null) continue;
                try { return v.GetValue<decimal>(); } catch { }
                try
                {
                    var s = v.GetValue<string>();
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                        return d;
                }
                catch { }
            }
            catch { }
        }
        return null;
    }

    private static int? Int(JsonNode? node, params string[] keys)
    {
        if (node is null) return null;
        foreach (var k in keys)
        {
            try
            {
                var v = node[k];
                if (v is null) continue;
                try { return v.GetValue<int>(); } catch { }
                try
                {
                    var s = v.GetValue<string>();
                    if (int.TryParse(s, out var i)) return i;
                }
                catch { }
            }
            catch { }
        }
        return null;
    }

    private static bool? BoolVal(JsonNode? node, params string[] keys)
    {
        if (node is null) return null;
        foreach (var k in keys)
        {
            try
            {
                var v = node[k];
                if (v is null) continue;
                try { return v.GetValue<bool>(); } catch { }
                try
                {
                    var s = v.GetValue<string>()?.ToLowerInvariant();
                    if (s is "true" or "1" or "yes") return true;
                    if (s is "false" or "0" or "no") return false;
                }
                catch { }
            }
            catch { }
        }
        return null;
    }

    private static void LogMissingFields(
        ILogger logger, string source, string externalId,
        params (string? Value, string Name)[] fields)
    {
        var missing = fields.Where(f => f.Value is null).Select(f => f.Name).ToList();
        if (missing.Count > 0)
            logger.LogInformation(
                "[ZimmoParser] {Source} [{Id}] ontbrekende velden: {Fields}",
                source, externalId, string.Join(", ", missing));
    }

    [GeneratedRegex(@"<script[^>]+type=[""']application/ld\+json[""'][^>]*>(.*?)</script>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex JsonLdRegex();
}
