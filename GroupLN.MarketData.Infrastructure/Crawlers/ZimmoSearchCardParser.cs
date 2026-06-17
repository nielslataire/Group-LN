using System.Globalization;
using System.Text.Json.Nodes;
using GroupLN.MarketData.Core.DTOs;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

/// <summary>
/// Parseert listing-cards uit de __NEXT_DATA__ van een Zimmo zoekresultaten-pagina.
/// Primaire databron wanneer OpenDetailPages=false.
/// Geen detailpagina-navigatie nodig — velden die niet in search-cards staan
/// (EPC, beschrijving, developer) blijven null.
/// </summary>
public static class ZimmoSearchCardParser
{
    public static IReadOnlyList<ListingDto> ParseSearchCards(
        string nextDataJson, ILogger logger)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(nextDataJson); }
        catch (Exception ex)
        {
            logger.LogDebug("[ZimmoSearchCard] JSON parse fout: {Msg}", ex.Message);
            return [];
        }

        var pageProps = root?["props"]?["pageProps"];
        if (pageProps is null)
        {
            logger.LogDebug("[ZimmoSearchCard] props.pageProps niet gevonden");
            return [];
        }

        var items = ExtractItems(pageProps, logger);
        if (items.Count == 0)
        {
            logger.LogDebug("[ZimmoSearchCard] Geen items gevonden — beschikbare pageProps keys: {Keys}",
                pageProps is JsonObject obj
                    ? string.Join(", ", obj.Select(k => k.Key))
                    : "(geen object)");
            return [];
        }

        logger.LogInformation("[ZimmoSearchCard] {Count} search-card items gevonden", items.Count);

        var result = new List<ListingDto>();
        foreach (var item in items)
        {
            var dto = TryParseCard(item, logger);
            if (dto is not null) result.Add(dto);
        }

        logger.LogInformation(
            "[ZimmoSearchCard] {Parsed}/{Total} cards geparsed", result.Count, items.Count);
        return result;
    }

    // ── Items ophalen uit __NEXT_DATA__ ───────────────────────────────────────
    // Zimmo wisselt tussen cluster-structuur (map-view) en flat-array (list-view).

    private static IReadOnlyList<JsonNode> ExtractItems(JsonNode pageProps, ILogger logger)
    {
        // 1. clusters[] of result.clusters[] (map-gebaseerde weergave)
        foreach (var clusterPath in new[] { "clusters", "result.clusters" })
        {
            var node = ResolveNode(pageProps, clusterPath);
            if (node is JsonArray arr && arr.Count > 0)
            {
                var clustered = FlattenClusters(arr);
                if (clustered.Count > 0)
                {
                    logger.LogDebug("[ZimmoSearchCard] Pad: {Path} ({Count} clusters)", clusterPath, arr.Count);
                    return clustered;
                }
            }
        }

        // 2. results als array of als object met items/listings sub-key
        var results = pageProps["results"];
        if (results is JsonArray resultsArr && resultsArr.Count > 0)
        {
            logger.LogDebug("[ZimmoSearchCard] Pad: results[]");
            return Compact(resultsArr);
        }
        if (results is JsonObject)
        {
            foreach (var sub in new[] { "items", "listings", "properties", "data", "clusters" })
            {
                if (results[sub] is JsonArray subArr && subArr.Count > 0)
                {
                    if (sub == "clusters")
                    {
                        var flat = FlattenClusters(subArr);
                        if (flat.Count > 0)
                        {
                            logger.LogDebug("[ZimmoSearchCard] Pad: results.clusters");
                            return flat;
                        }
                    }
                    else
                    {
                        logger.LogDebug("[ZimmoSearchCard] Pad: results.{Sub}[]", sub);
                        return Compact(subArr);
                    }
                }
            }
        }

        // 3. Directe arrays op pageProps
        foreach (var key in new[] { "properties", "listings", "searchResults", "items", "initialListings" })
        {
            if (pageProps[key] is JsonArray arr && arr.Count > 0)
            {
                logger.LogDebug("[ZimmoSearchCard] Pad: {Key}[]", key);
                return Compact(arr);
            }
        }

        return [];
    }

    private static List<JsonNode> FlattenClusters(JsonArray clusters)
    {
        var items = new List<JsonNode>();
        foreach (var cluster in clusters)
        {
            if (cluster?["property"] is JsonNode single) items.Add(single);
            if (cluster?["properties"] is JsonArray multi)
                foreach (var p in multi) if (p is not null) items.Add(p);
        }
        return items;
    }

    private static List<JsonNode> Compact(JsonArray arr) =>
        arr.Where(n => n is not null).Select(n => n!).ToList();

    private static JsonNode? ResolveNode(JsonNode root, string dotPath)
    {
        var cur = root;
        foreach (var key in dotPath.Split('.'))
        {
            cur = cur?[key];
            if (cur is null) return null;
        }
        return cur;
    }

    // ── Eén search-card parsen ────────────────────────────────────────────────

    private static ListingDto? TryParseCard(JsonNode item, ILogger logger)
    {
        // ExternalId: voorkeur op basis van permalink (zelfde als URL-pad op zoekpagina)
        var permalink = Str(item, "permalink", "url", "path", "href");
        var idField   = Str(item, "id", "code", "propertyId", "listingId");

        string? canonicalUrl = null;
        string? externalId   = null;

        if (!string.IsNullOrEmpty(permalink))
        {
            var clean = permalink.Split('?')[0].TrimEnd('/');
            if (!clean.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                clean = "https://www.zimmo.be" + clean;
            canonicalUrl = clean + "/";
            externalId   = ZimmoListingParser.ExtractExternalIdFromUrl(canonicalUrl);
        }

        // Fallback: id-veld als het kort genoeg is voor een Zimmo-code (bijv. "LPC9N")
        if (string.IsNullOrEmpty(externalId) && !string.IsNullOrEmpty(idField) && idField.Length <= 20)
            externalId = idField;

        if (string.IsNullOrEmpty(externalId))
        {
            logger.LogDebug("[ZimmoSearchCard] ExternalId niet gevonden, card overgeslagen");
            return null;
        }

        canonicalUrl ??= $"https://www.zimmo.be/nl/onbekend/te-koop/woning/{externalId}/";

        // Prijs
        var priceNode   = item["price"] ?? item["askingPrice"];
        var askingPrice = Dec(priceNode, "amount", "value", "price")
                       ?? Dec(item, "price", "askingPrice", "amount");

        // Adres
        var addrNode    = item["address"] ?? item["location"];
        var postalCode  = Str(addrNode, "zipCode", "postalCode", "zip", "postal");
        var city        = Str(addrNode, "city", "municipality", "locality", "commune");
        var street      = Str(addrNode, "street", "streetName", "streetAddress");
        var houseNumber = Str(addrNode, "number", "houseNumber", "streetNumber", "housenumber");

        // Coördinaten — kunnen null zijn, worden later via geo-resolver bijgevuld
        var coordNode = item["coordinates"] ?? item["geoLocation"] ?? item["geo"]
                     ?? item["location"] ?? addrNode;
        var lat = Dec(coordNode, "lat", "latitude");
        var lng = Dec(coordNode, "lng", "lon", "longitude");

        // Type / transactie
        var propertyType = Str(item, "category", "type", "propertyType", "kind");
        var txType       = Str(item, "transactionType", "status", "saleStatus", "listingType");

        // Kenmerken
        var chars      = item["characteristics"] ?? item["features"] ?? item["specs"] ?? item["attributes"];
        var livingArea = Dec(chars, "livable", "livingArea", "habitableSurface", "usableArea", "net")
                      ?? Dec(item, "livingArea", "habitableSurface", "livable");
        var landArea   = Dec(chars, "terrain", "plotArea", "landArea", "groundArea", "terrainArea")
                      ?? Dec(item, "plotArea", "landArea");
        var bedrooms   = Int(chars ?? item, "numberOfBedrooms", "bedrooms", "bedroomCount", "rooms");
        var bathrooms  = Int(chars ?? item, "numberOfBathrooms", "bathrooms", "bathroomCount");

        // Nieuwbouw
        var isNew = Bool(item, "isNewConstruction", "isNewBuild", "newConstruction", "newBuild");

        // Project / developer (beperkt aanwezig in search-cards)
        var projectNode = item["project"] ?? item["projectInfo"] ?? item["group"];
        var projectName = Str(projectNode, "name", "projectName", "title")
                       ?? Str(item, "projectName");

        var devNode  = item["developer"] ?? item["promoter"] ?? item["agency"] ?? item["agent"];
        var devName  = Str(devNode, "name", "companyName", "agencyName");
        var devPhone = Str(devNode, "phone", "telephone", "phoneNumber");

        var title = Str(item, "title", "name", "heading");

        logger.LogDebug(
            "[ZimmoSearchCard] {Id} | {PostalCode} {City} | €{Price} | {Type} | {Area}m²",
            externalId, postalCode ?? "?", city ?? "?",
            askingPrice?.ToString("N0") ?? "?", propertyType ?? "?",
            livingArea?.ToString() ?? "?");

        return ZimmoDtoMapper.Build(
            externalId          : externalId,
            url                 : canonicalUrl,
            title               : title,
            zimmoPropertyType   : propertyType,
            zimmoPropertySubType: null,
            zimmoTransactionType: txType,
            postalCode          : postalCode,
            city                : city,
            street              : street,
            houseNumber         : houseNumber,
            latitude            : lat,
            longitude           : lng,
            askingPrice         : askingPrice,
            maxPrice            : null,
            livingArea          : livingArea,
            landArea            : landArea,
            bedrooms            : bedrooms,
            bathrooms           : bathrooms,
            epcLabel            : null,
            epcScore            : null,
            isNewConstruction   : isNew,
            developerName       : devName,
            developerPhone      : devPhone,
            developerWebsite    : null,
            projectName         : projectName,
            description         : null,
            rawJson             : null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? Str(JsonNode? node, params string[] keys)
    {
        if (node is null) return null;
        foreach (var k in keys)
        {
            try { var v = node[k]; if (v is not null) return v.GetValue<string>(); }
            catch { }
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
                try { if (int.TryParse(v.GetValue<string>(), out var i)) return i; } catch { }
            }
            catch { }
        }
        return null;
    }

    private static bool? Bool(JsonNode? node, params string[] keys)
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
                    if (s is "true" or "1") return true;
                    if (s is "false" or "0") return false;
                }
                catch { }
            }
            catch { }
        }
        return null;
    }
}
