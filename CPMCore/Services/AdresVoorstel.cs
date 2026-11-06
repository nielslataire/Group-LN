using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace CPMCore.Services;

/// <summary>Een voorgesteld adres bij het intikken: <paramref name="Naam"/> (straat en nummer, of de naam van een plaats) en <paramref name="Adres"/> (rest van het adres).</summary>
public sealed record AdresSuggestie(string Naam, string Adres);

/// <summary>Adressen zoeken bij het intikken, ook adressen die CPM nog niet kent. Seam voor "Ander adres" in Nieuwe afspraak; tests gebruiken een fake.</summary>
public interface IAdresVoorstel
{
    bool IsConfigured { get; }
    /// <summary>Lege lijst als er niets past of de dienst niet bereikbaar is. Gooit nooit.</summary>
    Task<List<AdresSuggestie>> ZoekAsync(string term, CancellationToken ct = default);
}

/// <summary>
/// Google Places API (New), <c>places:autocomplete</c>, beperkt tot België en in het Nederlands. De aanroep gebeurt op de server, dus de sleutel (<c>GoogleMaps:ApiKey</c>) komt nooit in de browser.
/// Het gekozen voorstel wordt als tekst in de afspraak gezet; er is geen aparte Place Details-aanroep nodig. Antwoorden per zoekterm blijven een uur in het geheugen om aanroepen te sparen.
/// </summary>
public sealed class GooglePlacesAdresVoorstel : IAdresVoorstel
{
    public const string Url = "https://places.googleapis.com/v1/places:autocomplete";
    private static readonly ConcurrentDictionary<string, (List<AdresSuggestie> Lijst, DateTime Tot)> Cache = new();
    private readonly HttpClient _http;
    private readonly string? _sleutel;
    private readonly ILogger<GooglePlacesAdresVoorstel> _logger;

    public GooglePlacesAdresVoorstel(HttpClient http, IConfiguration configuration, ILogger<GooglePlacesAdresVoorstel> logger)
    {
        _http = http; _logger = logger;
        _sleutel = configuration["GoogleMaps:ApiKey"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_sleutel);

    public async Task<List<AdresSuggestie>> ZoekAsync(string term, CancellationToken ct = default)
    {
        term = (term ?? "").Trim();
        if (!IsConfigured || term.Length < 3 || term.Length > 200) return new();
        var sleutel = term.ToLowerInvariant();
        if (Cache.TryGetValue(sleutel, out var bekend) && bekend.Tot > DateTime.UtcNow) return bekend.Lijst;
        try
        {
            using var vraag = new HttpRequestMessage(HttpMethod.Post, Url)
            {
                Content = JsonContent.Create(new { input = term, includedRegionCodes = new[] { "be" }, languageCode = "nl" })
            };
            vraag.Headers.Add("X-Goog-Api-Key", _sleutel);
            using var antwoord = await _http.SendAsync(vraag, ct);
            if (!antwoord.IsSuccessStatusCode)
            {
                _logger.LogWarning("Adresvoorstellen ophalen mislukt (status {Status}).", (int)antwoord.StatusCode);
                return new();   // niet onthouden: een tijdelijke fout mag een volgende poging niet blokkeren
            }
            var lijst = Parse(await antwoord.Content.ReadAsStringAsync(ct));
            Cache[sleutel] = (lijst, DateTime.UtcNow.AddHours(1));
            return lijst;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Adresvoorstellen ophalen mislukt.");
            return new();
        }
    }

    /// <summary>Leest <c>suggestions[].placePrediction</c>: hoofdtekst (straat en nummer) en secundaire tekst (postcode, gemeente, land). Zonder structuur valt het terug op de volledige tekst.</summary>
    public static List<AdresSuggestie> Parse(string json)
    {
        var lijst = new List<AdresSuggestie>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("suggestions", out var suggesties) || suggesties.ValueKind != JsonValueKind.Array) return lijst;
        foreach (var s in suggesties.EnumerateArray())
        {
            if (!s.TryGetProperty("placePrediction", out var p)) continue;
            string? Tekst(JsonElement e, string a, string b) => e.TryGetProperty(a, out var x) && x.TryGetProperty(b, out var y) ? y.GetString() : null;
            var hoofd = p.TryGetProperty("structuredFormat", out var sf) ? Tekst(sf, "mainText", "text") : null;
            var rest = sf.ValueKind == JsonValueKind.Object ? Tekst(sf, "secondaryText", "text") : null;
            if (string.IsNullOrWhiteSpace(hoofd)) { hoofd = Tekst(p, "text", "text"); rest = null; }
            if (!string.IsNullOrWhiteSpace(hoofd)) lijst.Add(new AdresSuggestie(hoofd.Trim(), (rest ?? "").Trim()));
        }
        return lijst;
    }
}
