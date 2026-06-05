using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Services;

public class MarketListingService : IMarketListingService
{
    private readonly MarketDataDbContext _context;
    private readonly IMarketAssetMatcher _assetMatcher;
    private readonly ILogger<MarketListingService> _logger;

    public MarketListingService(
        MarketDataDbContext context,
        IMarketAssetMatcher assetMatcher,
        ILogger<MarketListingService> logger)
    {
        _context = context;
        _assetMatcher = assetMatcher;
        _logger = logger;
    }

    public async Task<bool> UpsertListingAsync(NormalizedPropertyDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Stap 1: bestaande listing zoeken op SourceId + ExternalId
        var existing = await _context.MarketListings
            .Include(l => l.Snapshots.OrderByDescending(s => s.SnapshotDate).Take(1))
            .Include(l => l.Asset)
            .FirstOrDefaultAsync(l => l.SourceId == dto.SourceId && l.ExternalId == dto.ExternalId, cancellationToken);

        if (existing is not null)
        {
            // Stap 2: bestaande listing bijwerken
            existing.Title = dto.Title ?? existing.Title;
            existing.AskingPrice = dto.AskingPrice ?? existing.AskingPrice;
            existing.LastSeenAt = now;
            existing.IsActive = true;
            existing.RemovedAt = null;
            existing.UpdatedAt = now;

            // Asset verrijken met recentere data waar zinvol
            UpdateAssetFromDto(existing.Asset, dto, now);

            var lastSnapshot = existing.Snapshots.MaxBy(s => s.SnapshotDate);
            _context.MarketListingSnapshots.Add(CreateSnapshot(existing.Id, dto, now));

            if (dto.AskingPrice.HasValue && lastSnapshot?.AskingPrice != dto.AskingPrice)
            {
                var change = new MarketListingPriceHistory
                {
                    MarketListingId = existing.Id,
                    DetectedAt = now,
                    AskingPrice = dto.AskingPrice.Value,
                    PreviousPrice = lastSnapshot?.AskingPrice
                };

                if (lastSnapshot?.AskingPrice is decimal prev && prev != 0)
                {
                    change.PriceChangeAmount = dto.AskingPrice.Value - prev;
                    change.PriceChangePercentage = change.PriceChangeAmount / prev * 100m;
                }

                _context.MarketListingPriceHistories.Add(change);
                _logger.LogInformation("Prijswijziging voor listing {ExternalId}: {Old} â†’ {New}.",
                    dto.ExternalId, lastSnapshot?.AskingPrice, dto.AskingPrice);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Listing bijgewerkt: {ExternalId} ({City} {PostalCode}).",
                dto.ExternalId, dto.City, dto.PostalCode);
            return false;
        }

        // Stap 3: nieuwe listing â€" zoek of maak MarketAsset
        var (assetKey, assetKeyStrategy) = BuildAssetKey(dto);
        _logger.LogInformation(
            "AssetKey strategie={Strategy} | key={Key}",
            assetKeyStrategy, assetKey);

        var matchResult = await _assetMatcher.FindMatchingAssetAsync(dto, cancellationToken);

        MarketAsset asset;

        if (matchResult.AssetMatchType == AssetMatchType.Strong && matchResult.MatchedAssetId.HasValue)
        {
            // Sterke match: koppel aan bestaand asset
            asset = (await _context.MarketAssets.FindAsync(new object[] { matchResult.MatchedAssetId.Value }, cancellationToken))!;
            UpdateAssetFromDto(asset, dto, now);
            _logger.LogInformation(
                "Nieuwe listing {ExternalId} gekoppeld aan bestaand MarketAsset {AssetId} (Strong, score={Score}).",
                dto.ExternalId, asset.Id, matchResult.MatchScore);
        }
        else
        {
            // Geen sterke match: nieuw asset aanmaken
            asset = CreateAsset(dto, now, assetKey);
            _context.MarketAssets.Add(asset);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Nieuw MarketAsset {AssetId} aangemaakt | {City} {PostalCode} | {Type} | match={AssetMatchType} | strategy={Strategy}.",
                asset.Id, dto.City, dto.PostalCode, dto.PropertyType, matchResult.AssetMatchType, assetKeyStrategy);
        }

        // Nieuwe listing aanmaken
        var listing = new MarketListing
        {
            MarketAssetId = asset.Id,
            SourceId = dto.SourceId,
            ExternalId = dto.ExternalId,
            Url = dto.Url,
            Title = dto.Title,
            AskingPrice = dto.AskingPrice,
            FirstSeenAt = now,
            LastSeenAt = now,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.MarketListings.Add(listing);
        await _context.SaveChangesAsync(cancellationToken);

        _context.MarketListingSnapshots.Add(CreateSnapshot(listing.Id, dto, now));

        if (dto.AskingPrice.HasValue)
        {
            _context.MarketListingPriceHistories.Add(new MarketListingPriceHistory
            {
                MarketListingId = listing.Id,
                DetectedAt = now,
                AskingPrice = dto.AskingPrice.Value
            });
        }

        // Medium/Weak matches opslaan als kandidaten voor manuele review
        if (matchResult.AssetMatchType is AssetMatchType.Medium or AssetMatchType.Weak)
        {
            foreach (var candidateId in matchResult.CandidateAssets)
            {
                _context.MarketAssetMatchCandidates.Add(new MarketAssetMatchCandidate
                {
                    ExistingMarketAssetId = candidateId,
                    SourceId = dto.SourceId,
                    ExternalId = dto.ExternalId,
                    Url = dto.Url,
                    MatchScore = matchResult.MatchScore,
                    MatchReason = matchResult.MatchReason,
                    CreatedAt = now
                });
            }
            _logger.LogDebug(
                "Listing {ExternalId}: {Count} match-kandidaten opgeslagen ({AssetMatchType}, score={Score}).",
                dto.ExternalId, matchResult.CandidateAssets.Count, matchResult.AssetMatchType, matchResult.MatchScore);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Nieuwe listing aangemaakt: {ExternalId} ({City} {PostalCode}).",
            dto.ExternalId, dto.City, dto.PostalCode);
        return true;
    }

    public async Task MarkInactiveAsync(
        int sourceId, IEnumerable<string> activeExternalIds, CancellationToken cancellationToken = default)
    {
        var activeSet = activeExternalIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        var toDeactivate = await _context.MarketListings
            .Where(l => l.SourceId == sourceId && l.IsActive && !activeSet.Contains(l.ExternalId))
            .ToListAsync(cancellationToken);

        if (toDeactivate.Count == 0) return;

        foreach (var l in toDeactivate)
        {
            l.IsActive = false;
            l.RemovedAt = now;
            l.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{Count} listings gemarkeerd als inactief voor bron {SourceId}.",
            toDeactivate.Count, sourceId);
    }

    public async Task<int> MarkStaleListingsInactiveAsync(
        int sourceId, int afterDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-afterDays);
        var now = DateTime.UtcNow;

        var stale = await _context.MarketListings
            .Where(l => l.SourceId == sourceId && l.IsActive && l.LastSeenAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var l in stale)
        {
            l.IsActive = false;
            l.RemovedAt = now;
            l.UpdatedAt = now;
        }

        if (stale.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return stale.Count;
    }

    private static void UpdateAssetFromDto(MarketAsset asset, NormalizedPropertyDto dto, DateTime now)
    {
        if (!string.IsNullOrEmpty(dto.PostalCode)) asset.PostalCode = dto.PostalCode;
        if (!string.IsNullOrEmpty(dto.City)) asset.City = dto.City;
        if (!string.IsNullOrEmpty(dto.Street)) asset.Street = dto.Street;
        if (!string.IsNullOrEmpty(dto.HouseNumber)) asset.HouseNumber = dto.HouseNumber;
        if (dto.Latitude.HasValue) asset.Latitude = dto.Latitude;
        if (dto.Longitude.HasValue) asset.Longitude = dto.Longitude;
        if (dto.LivingArea.HasValue) asset.LivingArea = dto.LivingArea;
        if (dto.LandArea.HasValue) asset.LandArea = dto.LandArea;
        if (dto.Bedrooms.HasValue) asset.Bedrooms = dto.Bedrooms;
        if (dto.Bathrooms.HasValue) asset.Bathrooms = dto.Bathrooms;
        if (dto.ConstructionYear.HasValue) asset.ConstructionYear = dto.ConstructionYear;
        if (dto.EPCScore.HasValue) asset.EPCScore = dto.EPCScore;
        if (dto.EPCLabel.HasValue) asset.EPCLabel = dto.EPCLabel;
        asset.LastSeenAt = now;
        asset.UpdatedAt = now;
    }

    private static MarketAsset CreateAsset(NormalizedPropertyDto dto, DateTime now, string assetKey) => new()
    {
        AssetKey = assetKey,
        PropertyType = dto.PropertyType,
        PropertySubType = dto.PropertySubType,
        TransactionType = dto.TransactionType,
        Country = dto.Country,
        PostalCode = dto.PostalCode,
        City = dto.City,
        Street = dto.Street,
        HouseNumber = dto.HouseNumber,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        LivingArea = dto.LivingArea,
        LandArea = dto.LandArea,
        Bedrooms = dto.Bedrooms,
        Bathrooms = dto.Bathrooms,
        ConstructionYear = dto.ConstructionYear,
        EPCScore = dto.EPCScore,
        EPCLabel = dto.EPCLabel,
        NewBuild = dto.IsNewBuild,
        FirstSeenAt = now,
        LastSeenAt = now,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    // AssetKey — type-bewuste strategie voor correcte deduplicatie
    private static (string Key, string Strategy) BuildAssetKey(NormalizedPropertyDto dto)
    {
        var country = S(dto.Country ?? "BE").ToUpperInvariant();
        var postal = S(dto.PostalCode ?? "0000");
        var street = S(dto.Street).ToUpperInvariant();
        var houseNr = S(dto.HouseNumber).ToUpperInvariant();
        var propType = dto.PropertyType.ToString();
        var txType = dto.TransactionType.ToString();
        var extId = S(dto.ExternalId);

        // ── ProjectGroup (APARTMENT_GROUP / HOUSE_GROUP) ──────────────────────
        // Elk project krijgt zijn eigen asset; nooit mergen op basis van adres.
        if (dto.IsProjectListing || dto.PropertyType == Core.Enums.PropertyType.ProjectGroup)
        {
            return (K(country, postal, propType, txType, extId), "ProjectGroup");
        }

        // ── Appartement ───────────────────────────────────────────────────────
        // Als floor/unit beschikbaar: gebruik die voor sleutel (exacte unit-match).
        // Anders: ExternalId als fallback — voorkomt valse sterke matches
        //         tussen verschillende units op hetzelfde adres.
        if (dto.PropertyType == Core.Enums.PropertyType.Apartment)
        {
            if (dto.Floor.HasValue || !string.IsNullOrEmpty(dto.UnitNumber))
            {
                var floor = dto.Floor.HasValue ? dto.Floor.Value.ToString() : "X";
                var unit = S(dto.UnitNumber).ToUpperInvariant();
                return (K(country, postal, street, houseNr, propType, txType, floor, unit), "Apartment+Floor");
            }
            return (K(country, postal, street, houseNr, propType, txType, extId), "Apartment+ExternalId");
        }

        // ── Zonder volledig adres (grond, commercieel zonder straat) ──────────
        if (string.IsNullOrEmpty(dto.Street))
        {
            return (K(country, postal, propType, txType, extId), "NoAddress+ExternalId");
        }

        // ── Standaard: woning, villa, bungalow, etc. ──────────────────────────
        return (K(country, postal, street, houseNr, propType, txType), "House");

        static string S(string? v) => (v ?? string.Empty).Trim().Replace("|", "_");
        static string K(params string[] parts) => string.Join("|", parts);
    }

    private static MarketListingSnapshot CreateSnapshot(long listingId, NormalizedPropertyDto dto, DateTime now) => new()
    {
        MarketListingId = listingId,
        SnapshotDate = now,
        AskingPrice = dto.AskingPrice,
        LivingArea = dto.LivingArea,
        LandArea = dto.LandArea,
        Bedrooms = dto.Bedrooms,
        Bathrooms = dto.Bathrooms,
        GarageCount = dto.GarageCount,
        ConstructionYear = dto.ConstructionYear,
        EPCScore = dto.EPCScore,
        EPCLabel = dto.EPCLabel,
        IsNewBuild = dto.IsNewBuild,
        DescriptionHash = dto.DescriptionHash,
        RawJson = dto.RawJson
    };
}

