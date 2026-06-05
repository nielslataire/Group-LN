using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GroupLN.MarketData.Infrastructure.Services;

public class MarketAssetMatcherService : IMarketAssetMatcher
{
    private readonly MarketDataDbContext _context;

    public MarketAssetMatcherService(MarketDataDbContext context)
    {
        _context = context;
    }

    public async Task<MarketAssetMatchResult> FindMatchingAssetAsync(
        NormalizedPropertyDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(dto.PostalCode))
            return NoMatch();

        // Kandidaten: zelfde postcode, type en transactietype
        var candidates = await _context.MarketAssets
            .Where(a => a.IsActive
                     && a.PostalCode == dto.PostalCode
                     && a.PropertyType == dto.PropertyType
                     && a.TransactionType == dto.TransactionType)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return NoMatch();

        // Fase 1: Sterke match â€” zelfde straat + huisnummer + oppervlakte Â±5%/Â±10%
        foreach (var candidate in candidates)
        {
            if (IsStrongMatch(dto, candidate))
            {
                return new MarketAssetMatchResult
                {
                    AssetMatchType = AssetMatchType.Strong,
                    MatchedAssetId = candidate.Id,
                    MatchScore = 0.95m,
                    MatchReason = "Zelfde postcode, straat, huisnummer, type en woonoppervlakte (Â±5%)",
                    CandidateAssets = new List<long> { candidate.Id }
                };
            }
        }

        // Fase 2: Middelmatige match â€” zelfde straat, kamers, oppervlakte Â±10%
        var mediumCandidates = candidates.Where(c => IsMediumMatch(dto, c)).Select(c => c.Id).ToList();
        if (mediumCandidates.Count > 0)
        {
            return new MarketAssetMatchResult
            {
                AssetMatchType = AssetMatchType.Medium,
                MatchScore = 0.70m,
                MatchReason = "Zelfde postcode, straat, type en slaapkamers (oppervlakte Â±10%)",
                CandidateAssets = mediumCandidates
            };
        }

        // Fase 3: Zwakke match â€” coÃ¶rdinaten binnen ~100m, zelfde type
        var weakCandidates = candidates.Where(c => IsWeakMatch(dto, c)).Select(c => c.Id).ToList();
        if (weakCandidates.Count > 0)
        {
            return new MarketAssetMatchResult
            {
                AssetMatchType = AssetMatchType.Weak,
                MatchScore = 0.40m,
                MatchReason = "Zelfde postcode, type en coÃ¶rdinaten binnen ~100m",
                CandidateAssets = weakCandidates
            };
        }

        return NoMatch();
    }

    private static bool IsStrongMatch(NormalizedPropertyDto dto, MarketAsset candidate)
    {
        // Appartementen zonder verdieping/unit info: nooit Strong match op adres alleen.
        // Units in hetzelfde gebouw zouden anders foutief worden samengevoegd.
        // Medium/Weak kandidaten gaan naar manuele review.
        if (dto.PropertyType == PropertyType.Apartment && !dto.Floor.HasValue
            && string.IsNullOrEmpty(dto.UnitNumber))
            return false;

        if (dto.PropertyType == PropertyType.ProjectGroup)
            return false;

        if (string.IsNullOrEmpty(dto.Street) || string.IsNullOrEmpty(candidate.Street))
            return false;

        if (!string.Equals(dto.Street.Trim(), candidate.Street.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(dto.HouseNumber) && !string.IsNullOrEmpty(candidate.HouseNumber))
        {
            if (!string.Equals(dto.HouseNumber.Trim(), candidate.HouseNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (dto.LivingArea.HasValue && candidate.LivingArea.HasValue && candidate.LivingArea.Value != 0)
        {
            var diff = Math.Abs(dto.LivingArea.Value - candidate.LivingArea.Value) / candidate.LivingArea.Value;
            if (diff > 0.05m) return false;
        }

        if (dto.LandArea.HasValue && candidate.LandArea.HasValue && candidate.LandArea.Value != 0)
        {
            var diff = Math.Abs(dto.LandArea.Value - candidate.LandArea.Value) / candidate.LandArea.Value;
            if (diff > 0.10m) return false;
        }

        return true;
    }

    private static bool IsMediumMatch(NormalizedPropertyDto dto, MarketAsset candidate)
    {
        if (string.IsNullOrEmpty(dto.Street) || string.IsNullOrEmpty(candidate.Street))
            return false;

        if (!string.Equals(dto.Street.Trim(), candidate.Street.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (dto.Bedrooms.HasValue && candidate.Bedrooms.HasValue && dto.Bedrooms != candidate.Bedrooms)
            return false;

        if (dto.LivingArea.HasValue && candidate.LivingArea.HasValue && candidate.LivingArea.Value != 0)
        {
            var diff = Math.Abs(dto.LivingArea.Value - candidate.LivingArea.Value) / candidate.LivingArea.Value;
            if (diff > 0.10m) return false;
        }

        return true;
    }

    private static bool IsWeakMatch(NormalizedPropertyDto dto, MarketAsset candidate)
    {
        if (!dto.Latitude.HasValue || !dto.Longitude.HasValue
            || !candidate.Latitude.HasValue || !candidate.Longitude.HasValue)
            return false;

        var latDiff = Math.Abs((double)(dto.Latitude.Value - candidate.Latitude.Value));
        var lonDiff = Math.Abs((double)(dto.Longitude.Value - candidate.Longitude.Value));

        // ~0.001 graden â‰ˆ 100 meter
        if (latDiff >= 0.001 || lonDiff >= 0.001)
            return false;

        if (dto.Bedrooms.HasValue && candidate.Bedrooms.HasValue
            && Math.Abs(dto.Bedrooms.Value - candidate.Bedrooms.Value) > 1)
            return false;

        if (dto.LivingArea.HasValue && candidate.LivingArea.HasValue && candidate.LivingArea.Value != 0)
        {
            var diff = Math.Abs(dto.LivingArea.Value - candidate.LivingArea.Value) / candidate.LivingArea.Value;
            if (diff > 0.20m) return false;
        }

        return true;
    }

    private static MarketAssetMatchResult NoMatch() => new() { AssetMatchType = AssetMatchType.None };
}

