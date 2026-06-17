using System.Globalization;
using System.Text.Json.Nodes;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Enums;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

/// <summary>
/// Parseert Zimmo nieuwbouwproject-pagina's uit __NEXT_DATA__.
/// Extracteert projectnaam, developer, locatie, units en vraagrijs-range.
/// </summary>
public static class ZimmoProjectParser
{
    // Mogelijke root-keys voor projectdata in props.pageProps
    private static readonly string[] ProjectRootKeys =
    [
        "project",
        "projectDetails",
        "projectData",
        "newConstruction",
        "development",
    ];

    /// <summary>
    /// Probeert een ListingDto te bouwen voor een Zimmo nieuwbouwproject.
    /// </summary>
    public static ListingDto? TryParseNextData(string json, string url, ILogger logger)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (Exception ex)
        {
            logger.LogDebug("[ZimmoProject] JSON parse fout: {Msg}", ex.Message);
            return null;
        }

        var pageProps = root?["props"]?["pageProps"];
        if (pageProps is null) return null;

        if (pageProps is JsonObject obj)
            logger.LogDebug("[ZimmoProject] pageProps keys: {Keys}", string.Join(", ", obj.Select(k => k.Key)));

        foreach (var key in ProjectRootKeys)
        {
            var node = pageProps[key];
            if (node is null) continue;

            logger.LogDebug("[ZimmoProject] Project-data gevonden onder pageProps.{Key}", key);
            var dto = ParseProjectNode(node, url, $"pageProps.{key}", json, logger);
            if (dto is not null) return dto;
        }

        // Sommige sites zetten projecten ook onder listing-keys maar met category=PROJECT
        var listingKeys = new[] { "propertyDetails", "listing", "listingDetails" };
        foreach (var key in listingKeys)
        {
            var node = pageProps[key];
            if (node is null) continue;
            var category = node["category"]?.GetValue<string>() ?? node["type"]?.GetValue<string>() ?? "";
            if (!category.Contains("PROJECT", StringComparison.OrdinalIgnoreCase)) continue;

            logger.LogDebug("[ZimmoProject] Project herkend via category=PROJECT onder pageProps.{Key}", key);
            return ParseProjectNode(node, url, $"pageProps.{key}[PROJECT]", json, logger);
        }

        logger.LogWarning("[ZimmoProject] Geen projectdata gevonden in __NEXT_DATA__ voor {Url}", url);
        return null;
    }

    // ── Kern-parser ────────────────────────────────────────────────────────────

    private static ListingDto? ParseProjectNode(
        JsonNode node, string url, string parseSource, string rawJson, ILogger logger)
    {
        var externalId = Str(node, "id", "projectId", "code", "slug")
                      ?? ZimmoListingParser.ExtractExternalIdFromUrl(url);

        if (string.IsNullOrEmpty(externalId))
        {
            logger.LogWarning("[ZimmoProject] ExternalId niet gevonden via {Source}", parseSource);
            return null;
        }

        var projectName = Str(node, "name", "projectName", "title", "heading");

        // Adres
        var addrNode   = node["address"] ?? node["location"] ?? node["geo"];
        var postalCode = Str(addrNode, "zipCode", "postalCode", "zip", "postal");
        var city       = Str(addrNode, "city", "municipality", "locality");
        var street     = Str(addrNode, "street", "streetName", "streetAddress");
        var houseNumber = Str(addrNode, "number", "houseNumber");

        // Coördinaten
        var coordNode = node["coordinates"] ?? node["geo"] ?? addrNode;
        var latitude  = Dec(coordNode, "lat", "latitude");
        var longitude = Dec(coordNode, "lng", "lon", "longitude");

        // Developer
        var devNode    = node["developer"] ?? node["promoter"] ?? node["agency"];
        var devName    = Str(devNode, "name", "companyName");
        var devPhone   = Str(devNode, "phone", "telephone");
        var devWebsite = Str(devNode, "website", "url");

        // Units voor prijs-range en logging
        var units = ParseUnits(node, externalId, projectName, logger);

        decimal? minPrice = null;
        decimal? maxPrice = null;
        if (units.Count > 0)
        {
            var prices = units.Where(u => u.Price > 0).Select(u => u.Price).ToList();
            if (prices.Count > 0)
            {
                minPrice = prices.Min();
                maxPrice = prices.Max();
            }
        }

        // Fallback: directe prijs op project-node
        var directPrice = Dec(node["price"] ?? node, "amount", "value", "price", "askingPrice");
        minPrice ??= directPrice;

        logger.LogInformation(
            "[ZimmoProject] {Source}: ID={Id} | {City} {PostalCode} | Project={Name} | " +
            "Dev={Dev} | Units={UnitCount} | PrijsRange=€{Min}–€{Max}",
            parseSource, externalId, city ?? "?", postalCode ?? "?",
            projectName ?? "?", devName ?? "?", units.Count,
            minPrice?.ToString("N0") ?? "?", maxPrice?.ToString("N0") ?? "?");

        // Log unittypes
        if (units.Count > 0)
        {
            var stats = units
                .GroupBy(u => u.SaleStatus.ToString())
                .Select(g => $"{g.Key}={g.Count()}");
            logger.LogInformation("[ZimmoProject]   Unit-statussen: {Stats}", string.Join(", ", stats));

            logger.LogInformation("[ZimmoProject]   Eerste {N} units:", Math.Min(5, units.Count));
            foreach (var u in units.Take(5))
                logger.LogInformation(
                    "[ZimmoProject]   Unit ID={Id} | {Type} | Status={Status} | €{Price} | {Area}m²",
                    u.UnitId, u.RawSubType ?? u.RawGroupType ?? "?", u.SaleStatus,
                    u.Price.HasValue ? u.Price.Value.ToString("N0") : "?",
                    u.Surface?.ToString("N0") ?? "?");
        }

        var dto = ZimmoDtoMapper.Build(
            externalId          : externalId,
            url                 : url,
            title               : projectName,
            zimmoPropertyType   : "PROJECT",
            zimmoPropertySubType: null,
            zimmoTransactionType: "FOR_SALE",
            postalCode          : postalCode,
            city                : city,
            street              : street,
            houseNumber         : houseNumber,
            latitude            : latitude,
            longitude           : longitude,
            askingPrice         : minPrice,
            maxPrice            : maxPrice,
            livingArea          : null,
            landArea            : null,
            bedrooms            : null,
            bathrooms           : null,
            epcLabel            : null,
            epcScore            : null,
            isNewConstruction   : true,
            developerName       : devName,
            developerPhone      : devPhone,
            developerWebsite    : devWebsite,
            projectName         : projectName,
            description         : Str(node, "description", "body"),
            rawJson             : rawJson.Length > 50_000 ? rawJson[..50_000] : rawJson);

        return dto;
    }

    // ── Unit-extractie ─────────────────────────────────────────────────────────

    /// <summary>
    /// Extraheert units (appartementen/huizen) uit het project-JSON-knoop.
    /// Probeert meerdere arraynamen.
    /// </summary>
    public static List<ProjectGroupUnitDto> ParseUnits(
        JsonNode node, string projectId, string? parentProjectName, ILogger logger)
    {
        var result = new List<ProjectGroupUnitDto>();
        string[] arrayKeys = ["units", "properties", "listings", "items", "apartments"];

        foreach (var key in arrayKeys)
        {
            if (node[key] is not JsonArray arr) continue;
            if (arr.Count == 0) continue;

            logger.LogDebug("[ZimmoProject] Units gevonden onder '{Key}': {Count}", key, arr.Count);

            foreach (var item in arr)
            {
                if (item is null) continue;
                result.Add(ExtractUnit(item, projectId, parentProjectName));
            }

            return result; // eerste niet-lege array wins
        }

        // Soms zitten units per groep: [{type:"APARTMENT", items:[...]}]
        string[] groupKeys = ["unitGroups", "propertyGroups", "groups"];
        foreach (var gKey in groupKeys)
        {
            if (node[gKey] is not JsonArray groups) continue;
            foreach (var group in groups)
            {
                if (group is null) continue;
                var groupType = group["type"]?.GetValue<string>() ?? group["category"]?.GetValue<string>();
                var items = (group["items"] ?? group["units"] ?? group["properties"]) as JsonArray;
                if (items is null) continue;
                foreach (var item in items)
                {
                    if (item is null) continue;
                    result.Add(ExtractUnit(item, projectId, parentProjectName, groupType));
                }
            }
            if (result.Count > 0) return result;
        }

        return result;
    }

    /// <summary>
    /// Extraheert units uit de volledige __NEXT_DATA__ JSON string.
    /// </summary>
    public static List<ProjectGroupUnitDto> ParseUnitsFromJson(
        string nextDataJson, string projectId, string? parentProjectName, ILogger logger)
    {
        JsonNode? pageProps;
        try { pageProps = JsonNode.Parse(nextDataJson)?["props"]?["pageProps"]; }
        catch { return []; }
        if (pageProps is null) return [];

        foreach (var key in ProjectRootKeys)
        {
            var node = pageProps[key];
            if (node is not null)
                return ParseUnits(node, projectId, parentProjectName, logger);
        }
        return [];
    }

    private static ProjectGroupUnitDto ExtractUnit(
        JsonNode item, string projectId, string? parentProjectName, string? groupTypeOverride = null)
    {
        var id      = Str(item, "id", "unitId", "propertyId", "code") ?? "";
        var type    = groupTypeOverride ?? Str(item, "type", "category", "propertyType");
        var subtype = Str(item, "subtype", "subCategory", "subType");
        var statusRaw = Str(item, "status", "saleStatus", "availability");

        var priceNode = item["price"] ?? item["askingPrice"];
        var price     = Dec(priceNode, "amount", "value") ?? Dec(item, "price", "amount");

        var surfNode  = item["surface"] ?? item["characteristics"] ?? item["features"];
        var surface   = Dec(surfNode, "livable", "livingArea", "net", "habitableSurface")
                     ?? Dec(item, "livingArea", "surface", "area");
        var bedrooms  = Int(item["characteristics"] ?? item["features"] ?? item,
                            "numberOfBedrooms", "bedrooms", "bedroomCount");
        var floor     = Int(item, "floor", "floorNumber", "level");

        return new ProjectGroupUnitDto
        {
            ParentProjectId       = projectId,
            ParentProjectName     = parentProjectName,
            UnitId                = id,
            RawGroupType          = type,
            RawSubType            = subtype,
            MappedPropertyType    = MapUnitType(type),
            MappedPropertySubType = MapUnitSubType(subtype ?? type),
            SaleStatus            = MapUnitStatus(statusRaw),
            Price                 = price,
            Surface               = surface,
            BedroomCount          = bedrooms,
            Floor                 = floor,
        };
    }

    private static PropertyType MapUnitType(string? raw) => raw?.ToUpperInvariant() switch
    {
        "HOUSE"     => PropertyType.House,
        "APARTMENT" => PropertyType.Apartment,
        "COMMERCIAL"=> PropertyType.CommercialProperty,
        _           => PropertyType.Unknown
    };

    private static PropertySubType MapUnitSubType(string? raw) => raw?.ToUpperInvariant() switch
    {
        "HOUSE"      => PropertySubType.DetachedHouse,
        "APARTMENT"  => PropertySubType.Apartment,
        "PENTHOUSE"  => PropertySubType.Penthouse,
        "STUDIO"     => PropertySubType.Studio,
        "DUPLEX"     => PropertySubType.Duplex,
        "TRIPLEX"    => PropertySubType.Triplex,
        _            => PropertySubType.Unknown
    };

    private static SaleStatus MapUnitStatus(string? raw) => raw?.ToUpperInvariant() switch
    {
        "AVAILABLE"  => SaleStatus.Available,
        "FOR_SALE"   => SaleStatus.Available,
        "SOLD"       => SaleStatus.Sold,
        "SOLD_OUT"   => SaleStatus.Sold,
        "OPTION"     => SaleStatus.Option,
        "RESERVED"   => SaleStatus.Reserved,
        _            => SaleStatus.Unknown
    };

    // ── JSON helpers ───────────────────────────────────────────────────────────

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
                    if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
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
}
