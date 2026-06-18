using System.Globalization;
using System.Text.Json.Nodes;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.GeoLocation;

/// <summary>
/// Geocodeert adressen via de OpenStreetMap Nominatim API.
/// Rate-limit: maximaal 1 request per seconde (Nominatim gebruiksvoorwaarden).
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private const string Provider = "Nominatim";
    private const string BaseUrl  = "https://nominatim.openstreetmap.org/search";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NominatimGeocodingService> _logger;

    // Simpele rate-limit: minimaal 1100ms tussen calls
    private DateTime _lastCallAt = DateTime.MinValue;
    private readonly SemaphoreSlim _rateLock = new(1, 1);

    public NominatimGeocodingService(
        IHttpClientFactory httpClientFactory,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<GeocodingResult?> GeocodeAsync(
        string? street,
        string? houseNumber,
        string postalCode,
        string city,
        string country = "BE",
        CancellationToken ct = default)
    {
        await _rateLock.WaitAsync(ct);
        try
        {
            var elapsed = DateTime.UtcNow - _lastCallAt;
            if (elapsed.TotalMilliseconds < 1100)
                await Task.Delay(TimeSpan.FromMilliseconds(1100 - elapsed.TotalMilliseconds), ct);

            _lastCallAt = DateTime.UtcNow;
        }
        finally
        {
            _rateLock.Release();
        }

        var streetPart = string.Join(" ",
            new[] { houseNumber, street }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var query = new Dictionary<string, string>
        {
            ["format"]       = "json",
            ["addressdetails"] = "0",
            ["limit"]        = "1",
            ["countrycodes"] = country.ToLowerInvariant(),
            ["postalcode"]   = postalCode,
            ["city"]         = city,
        };
        if (!string.IsNullOrEmpty(streetPart))
            query["street"] = streetPart;

        var qs = string.Join("&", query.Select(kv =>
            Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));

        var url = BaseUrl + "?" + qs;

        try
        {
            var client = _httpClientFactory.CreateClient("Nominatim");
            var json   = await client.GetStringAsync(url, ct);

            var arr = JsonNode.Parse(json) as JsonArray;
            if (arr is null || arr.Count == 0)
            {
                _logger.LogDebug("[Nominatim] Geen resultaat voor {Postcode} {City}", postalCode, city);
                return null;
            }

            var hit = arr[0]!;
            if (!decimal.TryParse(hit["lat"]?.GetValue<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
             || !decimal.TryParse(hit["lon"]?.GetValue<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                return null;

            // Confidence: hoog als huisnummer aanwezig
            var confidence = !string.IsNullOrEmpty(houseNumber) ? 85 : 60;

            _logger.LogDebug(
                "[Nominatim] {Street} {HouseNumber}, {Postcode} {City} → {Lat},{Lon} (conf={Conf})",
                street, houseNumber, postalCode, city, lat, lon, confidence);

            return new GeocodingResult(lat, lon, confidence, Provider, json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[Nominatim] Geocoding fout voor {Postcode} {City}", postalCode, city);
            return null;
        }
    }
}
