using System.Security.Cryptography;
using System.Text;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.GeoLocation;

/// <summary>
/// Verrijkt een MarketAsset met coördinaten wanneer die ontbreken.
/// Gebruikt GeocodingCache om herhaalde lookups te voorkomen.
/// Geocoding is enrichment — fouten blokkeren nooit de hoofdpipeline.
/// </summary>
public class GeocodingEnrichmentService
{
    private readonly IGeocodingService _geocoder;
    private readonly MarketDataDbContext _db;
    private readonly ILogger<GeocodingEnrichmentService> _logger;

    public GeocodingEnrichmentService(
        IGeocodingService geocoder,
        MarketDataDbContext db,
        ILogger<GeocodingEnrichmentService> logger)
    {
        _geocoder = geocoder;
        _db       = db;
        _logger   = logger;
    }

    /// <summary>
    /// Probeert Latitude/Longitude in te vullen op basis van het adres.
    /// Slaat het resultaat op in GeocodingCache.
    /// Geeft true terug als coördinaten succesvol zijn ingevuld.
    /// </summary>
    public async Task<bool> TryEnrichAsync(MarketAsset asset, CancellationToken ct = default)
    {
        // Alleen verrijken wanneer coördinaten ontbreken
        if (asset.Latitude.HasValue && asset.Longitude.HasValue)
            return false;

        // Minimumvereiste: straat + postcode + stad
        if (string.IsNullOrWhiteSpace(asset.Street)
         || string.IsNullOrWhiteSpace(asset.PostalCode)
         || string.IsNullOrWhiteSpace(asset.City))
            return false;

        var key = BuildAddressKey(asset);

        try
        {
            // Cache-check
            var cached = await _db.GeocodingCaches
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AddressKey == key, ct);

            if (cached is not null)
            {
                if (!cached.Latitude.HasValue || !cached.Longitude.HasValue)
                {
                    _logger.LogInformation(
                        "[GeocodingEnrichment] GeocodingNoResult (cache) | {Street}, {PostalCode} {City}",
                        asset.Street, asset.PostalCode, asset.City);
                    return false;
                }

                asset.Latitude  = cached.Latitude;
                asset.Longitude = cached.Longitude;
                asset.LocationResolutionSource     = "AddressGeocoding";
                asset.LocationResolutionConfidence = cached.Confidence;
                asset.LocationResolvedAt           = DateTime.UtcNow;

                _logger.LogInformation(
                    "[GeocodingEnrichment] GeocodingCacheHit | {Street}, {PostalCode} {City} → {Lat},{Lon}",
                    asset.Street, asset.PostalCode, asset.City,
                    cached.Latitude, cached.Longitude);
                return true;
            }

            // Geocoding uitvoeren
            var result = await _geocoder.GeocodeAsync(
                asset.Street, asset.HouseNumber,
                asset.PostalCode!, asset.City!, asset.Country,
                ct);

            // Opslaan in cache (ook null-resultaat om herhaalde lookups te voorkomen)
            var entry = new GeocodingCache
            {
                AddressKey      = key,
                Street          = asset.Street,
                HouseNumber     = asset.HouseNumber,
                PostalCode      = asset.PostalCode!,
                City            = asset.City!,
                Country         = asset.Country,
                Latitude        = result?.Latitude,
                Longitude       = result?.Longitude,
                Confidence      = result?.Confidence,
                Provider        = result?.Provider,
                RawResponseJson = result?.RawResponseJson,
                CreatedAt       = DateTime.UtcNow
            };

            _db.GeocodingCaches.Add(entry);
            await _db.SaveChangesAsync(ct);

            if (result is null)
            {
                _logger.LogInformation(
                    "[GeocodingEnrichment] GeocodingNoResult | {Street}, {PostalCode} {City}",
                    asset.Street, asset.PostalCode, asset.City);
                return false;
            }

            asset.Latitude  = result.Latitude;
            asset.Longitude = result.Longitude;
            asset.LocationResolutionSource     = "AddressGeocoding";
            asset.LocationResolutionConfidence = result.Confidence;
            asset.LocationResolvedAt           = DateTime.UtcNow;

            _logger.LogInformation(
                "[GeocodingEnrichment] GeocodingSuccess | {Street} {HouseNumber}, {PostalCode} {City} → {Lat},{Lon} (conf={Conf})",
                asset.Street, asset.HouseNumber, asset.PostalCode, asset.City,
                result.Latitude, result.Longitude, result.Confidence);

            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[GeocodingEnrichment] Fout bij geocoding van {Street}, {PostalCode} {City}",
                asset.Street, asset.PostalCode, asset.City);
            return false;
        }
    }

    private static string BuildAddressKey(MarketAsset asset)
    {
        // Deterministische hash op genormaliseerd adres
        var normalized = string.Concat(
            asset.PostalCode?.Trim().ToUpperInvariant() ?? "",
            "|",
            asset.City?.Trim().ToLowerInvariant() ?? "",
            "|",
            asset.Street?.Trim().ToLowerInvariant() ?? "",
            "|",
            asset.HouseNumber?.Trim().ToLowerInvariant() ?? "",
            "|",
            asset.Country.Trim().ToUpperInvariant());

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
