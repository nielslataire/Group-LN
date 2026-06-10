using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace GroupLN.MarketData.Infrastructure.GeoLocation;

public class LocationResolver : ILocationResolver
{
    private static readonly GeometryFactory GeometryFactory =
        new GeometryFactory(new PrecisionModel(), 4326);

    private readonly MarketDataDbContext _context;
    private readonly ILogger<LocationResolver> _logger;

    public LocationResolver(MarketDataDbContext context, ILogger<LocationResolver> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LocationResolutionResult?> ResolveAsync(
        decimal? latitude,
        decimal? longitude,
        string? rawCity,
        string? postalCode,
        CancellationToken cancellationToken = default)
    {
        // Stap 1: spatial resolve via lat/lng
        if (latitude.HasValue && longitude.HasValue)
        {
            var result = await ResolveByCoordinatesAsync(latitude.Value, longitude.Value, cancellationToken);
            if (result is not null)
                return result;

            _logger.LogDebug(
                "Geen polygon-match voor lat={Lat} lng={Lng} — terugvallen op postcode/naam.",
                latitude, longitude);
        }

        // Stap 2: fallback op postcode
        if (!string.IsNullOrWhiteSpace(postalCode))
        {
            var result = await ResolveByPostalCodeAsync(postalCode, rawCity, cancellationToken);
            if (result is not null)
                return result;
        }

        return null;
    }

    private async Task<LocationResolutionResult?> ResolveByCoordinatesAsync(
        decimal latitude, decimal longitude, CancellationToken cancellationToken)
    {
        var point = GeometryFactory.CreatePoint(
            new Coordinate((double)longitude, (double)latitude));

        var section = await _context.GeoMunicipalSections
            .Where(s => s.Boundary.Contains(point))
            .Select(s => new { s.Id, s.NisCodeMunicipality })
            .FirstOrDefaultAsync(cancellationToken);

        if (section is null)
            return null;

        int? municipalityId = null;
        if (!string.IsNullOrEmpty(section.NisCodeMunicipality))
        {
            municipalityId = await _context.GeoMunicipalities
                .Where(m => m.NisCode == section.NisCodeMunicipality)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new LocationResolutionResult
        {
            GeoMunicipalSectionId = section.Id,
            GeoMunicipalityId = municipalityId,
            ResolvedAt = DateTime.UtcNow,
            Source = "AdminVectorPolygon",
            Confidence = 100
        };
    }

    private async Task<LocationResolutionResult?> ResolveByPostalCodeAsync(
        string postalCode, string? rawCity, CancellationToken cancellationToken)
    {
        // Exacte match op postcode + naam
        if (!string.IsNullOrWhiteSpace(rawCity))
        {
            var normalizedCity = rawCity.Trim();
            var sectionByZipAndName = await _context.GeoMunicipalSections
                .Where(s => s.ZipCode == postalCode &&
                            (s.NameDutch != null && EF.Functions.Like(s.NameDutch, normalizedCity) ||
                             s.NameFrench != null && EF.Functions.Like(s.NameFrench, normalizedCity) ||
                             s.NameGerman != null && EF.Functions.Like(s.NameGerman, normalizedCity)))
                .Select(s => new { s.Id, s.NisCodeMunicipality })
                .FirstOrDefaultAsync(cancellationToken);

            if (sectionByZipAndName is not null)
            {
                int? municipalityId = null;
                if (!string.IsNullOrEmpty(sectionByZipAndName.NisCodeMunicipality))
                {
                    municipalityId = await _context.GeoMunicipalities
                        .Where(m => m.NisCode == sectionByZipAndName.NisCodeMunicipality)
                        .Select(m => (int?)m.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                return new LocationResolutionResult
                {
                    GeoMunicipalSectionId = sectionByZipAndName.Id,
                    GeoMunicipalityId = municipalityId,
                    ResolvedAt = DateTime.UtcNow,
                    Source = "PostcodeNaam",
                    Confidence = 60
                };
            }
        }

        // Enkel postcode — kan meerdere deelgemeenten matchen, houd confidence laag
        var sectionsByZip = await _context.GeoMunicipalSections
            .Where(s => s.ZipCode == postalCode)
            .Select(s => new { s.Id, s.NisCodeMunicipality })
            .ToListAsync(cancellationToken);

        if (sectionsByZip.Count == 0)
            return null;

        // Als er exact één deelgemeente is voor deze postcode: betrouwbaar genoeg
        if (sectionsByZip.Count == 1)
        {
            var only = sectionsByZip[0];
            int? municipalityId = null;
            if (!string.IsNullOrEmpty(only.NisCodeMunicipality))
            {
                municipalityId = await _context.GeoMunicipalities
                    .Where(m => m.NisCode == only.NisCodeMunicipality)
                    .Select(m => (int?)m.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return new LocationResolutionResult
            {
                GeoMunicipalSectionId = only.Id,
                GeoMunicipalityId = municipalityId,
                ResolvedAt = DateTime.UtcNow,
                Source = "PostcodeEnkel",
                Confidence = 40
            };
        }

        // Meerdere deelgemeenten voor postcode — alleen gemeente resolven
        var nisCodeMunicipality = sectionsByZip
            .Select(s => s.NisCodeMunicipality)
            .Where(n => !string.IsNullOrEmpty(n))
            .GroupBy(n => n)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        if (nisCodeMunicipality is null)
            return null;

        var municipality = await _context.GeoMunicipalities
            .Where(m => m.NisCode == nisCodeMunicipality)
            .Select(m => new { m.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (municipality is null)
            return null;

        return new LocationResolutionResult
        {
            GeoMunicipalSectionId = null,
            GeoMunicipalityId = municipality.Id,
            ResolvedAt = DateTime.UtcNow,
            Source = "PostcodeAmbiguous",
            Confidence = 20
        };
    }
}
