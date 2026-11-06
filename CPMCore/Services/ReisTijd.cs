using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace CPMCore.Services;

/// <summary>Reistijd met de auto tussen twee adressen, in minuten (naar boven afgerond op 5). Seam voor de reistijd in "Nieuwe afspraak"; tests gebruiken een fake.</summary>
public interface IReisTijd
{
    bool IsConfigured { get; }
    /// <summary>Null als de reistijd niet berekend kan worden (geen sleutel, adres onbekend, dienst onbereikbaar). Gooit nooit.</summary>
    Task<int?> MinutenAsync(string van, string naar, CancellationToken ct = default);
}

/// <summary>
/// Google Routes API, <c>computeRoutes</c> met <c>TRAFFIC_UNAWARE</c> (de goedkoopste SKU, "Compute Routes Essentials": 10.000 gratis aanroepen per maand, daarna 5 dollar per 1.000).
/// De adressen gaan als tekst mee, dus er is geen aparte geocodering nodig. Per adressenpaar wordt 24 uur onthouden, zodat dezelfde route nooit twee keer gerekend wordt.
/// Sleutel: <c>GoogleMaps:ApiKey</c>.
/// </summary>
public sealed class GoogleRoutesReisTijd : IReisTijd
{
    public const string Url = "https://routes.googleapis.com/directions/v2:computeRoutes";
    private static readonly ConcurrentDictionary<string, (int? Minuten, DateTime Tot)> Cache = new();
    private readonly HttpClient _http;
    private readonly string? _sleutel;
    private readonly ILogger<GoogleRoutesReisTijd> _logger;

    public GoogleRoutesReisTijd(HttpClient http, IConfiguration configuration, ILogger<GoogleRoutesReisTijd> logger)
    {
        _http = http; _logger = logger;
        _sleutel = configuration["GoogleMaps:ApiKey"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_sleutel);

    public async Task<int?> MinutenAsync(string van, string naar, CancellationToken ct = default)
    {
        van = (van ?? "").Trim(); naar = (naar ?? "").Trim();
        if (!IsConfigured || van.Length == 0 || naar.Length == 0 || van.Length > 300 || naar.Length > 300) return null;
        var sleutel = $"{van.ToLowerInvariant()}|{naar.ToLowerInvariant()}";
        if (Cache.TryGetValue(sleutel, out var bekend) && bekend.Tot > DateTime.UtcNow) return bekend.Minuten;
        try
        {
            using var vraag = new HttpRequestMessage(HttpMethod.Post, Url)
            {
                Content = JsonContent.Create(new
                {
                    origin = new { address = van }, destination = new { address = naar },
                    travelMode = "DRIVE", routingPreference = "TRAFFIC_UNAWARE", languageCode = "nl-BE", regionCode = "BE"
                })
            };
            vraag.Headers.Add("X-Goog-Api-Key", _sleutel);
            vraag.Headers.Add("X-Goog-FieldMask", "routes.duration");   // enkel de duur: houdt de aanroep op de goedkoopste SKU
            using var antwoord = await _http.SendAsync(vraag, ct);
            if (!antwoord.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reistijd berekenen mislukt (status {Status}).", (int)antwoord.StatusCode);
                return null;   // niet onthouden: een tijdelijke fout mag een volgende poging niet blokkeren
            }
            var minuten = Parse(await antwoord.Content.ReadAsStringAsync(ct));
            Cache[sleutel] = (minuten, DateTime.UtcNow.AddHours(24));
            return minuten;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Reistijd berekenen mislukt.");
            return null;
        }
    }

    /// <summary>Leest <c>{"routes":[{"duration":"1234s"}]}</c> en rondt naar boven af op 5 minuten. Null zonder route.</summary>
    public static int? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("routes", out var routes) || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0) return null;
        if (!routes[0].TryGetProperty("duration", out var duur) || duur.GetString() is not { } tekst || !tekst.EndsWith('s')) return null;
        if (!double.TryParse(tekst[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconden) || seconden < 0) return null;
        return (int)Math.Ceiling(seconden / 60.0 / 5.0) * 5;
    }
}
