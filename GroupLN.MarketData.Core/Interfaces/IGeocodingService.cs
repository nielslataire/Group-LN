using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

/// <summary>
/// Geocodeert een adres naar coördinaten.
/// Implementaties kunnen Nominatim, Google Maps, HERE, etc. gebruiken.
/// Geocoding is enrichment — null terugsturen is altijd acceptabel.
/// </summary>
public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeAsync(
        string? street,
        string? houseNumber,
        string postalCode,
        string city,
        string country = "BE",
        CancellationToken ct = default);
}
