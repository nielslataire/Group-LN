using System.Text;
using System.Text.Json;
using System.Web;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

/// <summary>
/// Bepaalt welke nieuwbouw-modus Zimmo gebruikt in de zoekfilter.
/// </summary>
public enum ZimmoNewConstructionMode
{
    /// <summary>
    /// Alle nieuwbouw (woningen + appartementen). Standaard voor de crawler.
    /// Produceert <c>"newConstruction":{"eq":"ALL"}</c> met category-filter.
    /// </summary>
    All,

    /// <summary>
    /// Enkel projectlistings (groepen). Verouderd formaat zonder category-filter.
    /// Produceert <c>"newConstruction":{"eq":"PROJECTS"}</c>.
    /// </summary>
    Projects,
}

/// <summary>
/// Bouwt Zimmo zoek-URLs met Base64-encoded JSON filterparameter.
/// Structuur: https://www.zimmo.be/nl/zoeken/?search={base64(json)}#gallery
/// </summary>
public static class ZimmoSearchUrlBuilder
{
    private const string BaseUrl = "https://www.zimmo.be/nl/zoeken/";

    private static readonly string[] DefaultStatuses    = ["FOR_SALE", "TAKE_OVER"];
    private static readonly string[] DefaultCategories  = ["HOUSE", "APARTMENT"];

    /// <summary>
    /// Bekende Zimmo placeId-waarden per postcode.
    /// Zimmo gebruikt interne locatie-IDs in hun zoekfilter-JSON.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> PlaceIdByPostalCode =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["8000"] = 1113, // Brugge
            ["8200"] = 1119, // Sint-Michiels
            ["8730"] = 1108, // Beernem
        };

    private static readonly JsonSerializerOptions CompactOptions = new() { WriteIndented = false };

    // ── Publieke API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Bouwt een volledige Zimmo zoek-URL.
    /// </summary>
    /// <param name="placeId">Zimmo interne locatie-ID. Null = geen locatiefilter.</param>
    /// <param name="newConstruction">Nieuwbouw-modus. Standaard <see cref="ZimmoNewConstructionMode.All"/>.</param>
    /// <param name="statuses">Verkoopstatussen. Standaard FOR_SALE en TAKE_OVER.</param>
    /// <param name="categories">Woningcategorieën. Standaard HOUSE en APARTMENT (alleen bij All-modus).</param>
    public static string Build(
        int? placeId = null,
        ZimmoNewConstructionMode newConstruction = ZimmoNewConstructionMode.All,
        string[]? statuses = null,
        string[]? categories = null)
    {
        statuses   ??= DefaultStatuses;
        categories ??= newConstruction == ZimmoNewConstructionMode.All ? DefaultCategories : [];

        var json    = BuildJson(placeId, newConstruction, statuses, categories);
        var base64  = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var encoded = Uri.EscapeDataString(base64);

        return $"{BaseUrl}?search={encoded}#gallery";
    }

    /// <summary>
    /// Bouwt een Zimmo zoek-URL op basis van een postcode uit de bekende mapping.
    /// Gooit <see cref="KeyNotFoundException"/> als de postcode niet gevonden is.
    /// </summary>
    public static string BuildForPostalCode(
        string postalCode,
        ZimmoNewConstructionMode newConstruction = ZimmoNewConstructionMode.All,
        string[]? statuses = null,
        string[]? categories = null)
    {
        if (!PlaceIdByPostalCode.TryGetValue(postalCode, out var placeId))
            throw new KeyNotFoundException(
                $"Geen Zimmo placeId gevonden voor postcode '{postalCode}'. " +
                "Voeg deze toe aan ZimmoSearchUrlBuilder.PlaceIdByPostalCode.");

        return Build(placeId, newConstruction, statuses, categories);
    }

    /// <summary>
    /// Decodeert de Base64-waarde van een Zimmo search-parameter terug naar JSON.
    /// Handig voor debugging: plak de waarde van de ?search= querystring parameter.
    /// </summary>
    public static string DecodeSearchParam(string urlEncodedBase64)
    {
        var base64 = HttpUtility.UrlDecode(urlEncodedBase64);
        var bytes  = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    // ── JSON-bouw ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Bouwt de compacte JSON-string die Zimmo verwacht als filterparameter.
    /// Veldvolgorde is bepalend voor de Base64-output; mag niet veranderen.
    /// </summary>
    public static string BuildJson(
        int? placeId,
        ZimmoNewConstructionMode newConstruction,
        string[] statuses,
        string[] categories)
    {
        return newConstruction switch
        {
            ZimmoNewConstructionMode.All      => BuildAllModeJson(placeId, statuses, categories),
            ZimmoNewConstructionMode.Projects => BuildProjectsModeJson(placeId, statuses),
            _ => throw new ArgumentOutOfRangeException(nameof(newConstruction))
        };
    }

    // All-modus: status → category → newConstruction(ALL) → placeId?
    private static string BuildAllModeJson(int? placeId, string[] statuses, string[] categories)
    {
        if (placeId.HasValue)
        {
            return JsonSerializer.Serialize(new
            {
                filter = new
                {
                    status          = new { @in = statuses },
                    category        = new { @in = categories },
                    newConstruction = new { eq = "ALL" },
                    placeId         = new { @in = new[] { placeId.Value } }
                }
            }, CompactOptions);
        }

        return JsonSerializer.Serialize(new
        {
            filter = new
            {
                status          = new { @in = statuses },
                category        = new { @in = categories },
                newConstruction = new { eq = "ALL" }
            }
        }, CompactOptions);
    }

    // Projects-modus (legacy): status → newConstruction(PROJECTS) → placeId?
    private static string BuildProjectsModeJson(int? placeId, string[] statuses)
    {
        if (placeId.HasValue)
        {
            return JsonSerializer.Serialize(new
            {
                filter = new
                {
                    status          = new { @in = statuses },
                    newConstruction = new { eq = "PROJECTS" },
                    placeId         = new { @in = new[] { placeId.Value } }
                }
            }, CompactOptions);
        }

        return JsonSerializer.Serialize(new
        {
            filter = new
            {
                status          = new { @in = statuses },
                newConstruction = new { eq = "PROJECTS" }
            }
        }, CompactOptions);
    }
}
