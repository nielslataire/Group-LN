using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Services;

public class MarketPropertyService : IMarketPropertyService
{
    private readonly MarketDataDbContext _context;
    private readonly ILogger<MarketPropertyService> _logger;

    public MarketPropertyService(MarketDataDbContext context, ILogger<MarketPropertyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> UpsertPropertyAsync(NormalizedPropertyDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var existing = await _context.MarketProperties
            .Include(p => p.Snapshots.OrderByDescending(s => s.SnapshotDate).Take(1))
            .FirstOrDefaultAsync(
                p => p.SourceId == dto.SourceId && p.ExternalId == dto.ExternalId,
                cancellationToken);

        if (existing is null)
        {
            var property = MapToEntity(dto, now);
            _context.MarketProperties.Add(property);
            await _context.SaveChangesAsync(cancellationToken);

            _context.MarketPropertySnapshots.Add(CreateSnapshot(property.Id, dto, now));

            if (dto.AskingPrice.HasValue)
            {
                _context.MarketPropertyPriceHistories.Add(new MarketPropertyPriceHistory
                {
                    MarketPropertyId = property.Id,
                    DetectedAt = now,
                    AskingPrice = dto.AskingPrice.Value
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Nieuw pand aangemaakt: {ExternalId} ({City} {PostalCode}).",
                dto.ExternalId, dto.City, dto.PostalCode);
            return true;
        }

        // Bestaand pand bijwerken
        existing.Title = dto.Title ?? existing.Title;
        existing.LastSeenAt = now;
        existing.IsActive = true;
        existing.RemovedAt = null;
        existing.UpdatedAt = now;

        // Locatiedata bijwerken indien aangevuld
        if (!string.IsNullOrEmpty(dto.PostalCode)) existing.PostalCode = dto.PostalCode;
        if (!string.IsNullOrEmpty(dto.City)) existing.City = dto.City;
        if (!string.IsNullOrEmpty(dto.Street)) existing.Street = dto.Street;
        if (!string.IsNullOrEmpty(dto.HouseNumber)) existing.HouseNumber = dto.HouseNumber;
        if (dto.Latitude.HasValue) existing.Latitude = dto.Latitude;
        if (dto.Longitude.HasValue) existing.Longitude = dto.Longitude;

        var lastSnapshot = existing.Snapshots.MaxBy(s => s.SnapshotDate);

        _context.MarketPropertySnapshots.Add(CreateSnapshot(existing.Id, dto, now));

        if (dto.AskingPrice.HasValue && lastSnapshot?.AskingPrice != dto.AskingPrice)
        {
            var change = new MarketPropertyPriceHistory
            {
                MarketPropertyId = existing.Id,
                DetectedAt = now,
                AskingPrice = dto.AskingPrice.Value,
                PreviousPrice = lastSnapshot?.AskingPrice
            };

            if (lastSnapshot?.AskingPrice is decimal prev && prev != 0)
            {
                change.PriceChangeAmount = dto.AskingPrice.Value - prev;
                change.PriceChangePercentage = change.PriceChangeAmount / prev * 100m;
            }

            _context.MarketPropertyPriceHistories.Add(change);
            _logger.LogInformation("Prijswijziging gedetecteerd voor {ExternalId}: {Old} → {New}.",
                dto.ExternalId, lastSnapshot?.AskingPrice, dto.AskingPrice);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return false;
    }

    public async Task MarkInactiveAsync(
        int sourceId, IEnumerable<string> activeExternalIds, CancellationToken cancellationToken = default)
    {
        var activeSet = activeExternalIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        var toDeactivate = await _context.MarketProperties
            .Where(p => p.SourceId == sourceId && p.IsActive && !activeSet.Contains(p.ExternalId))
            .ToListAsync(cancellationToken);

        if (toDeactivate.Count == 0) return;

        foreach (var property in toDeactivate)
        {
            property.IsActive = false;
            property.RemovedAt = now;
            property.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{Count} panden gemarkeerd als inactief voor bron {SourceId}.",
            toDeactivate.Count, sourceId);
    }

    public async Task<int> MarkStalePropertiesInactiveAsync(
        int sourceId, int afterDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-afterDays);
        var now = DateTime.UtcNow;

        var stale = await _context.MarketProperties
            .Where(p => p.SourceId == sourceId && p.IsActive && p.LastSeenAt < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var property in stale)
        {
            property.IsActive = false;
            property.RemovedAt = now;
            property.UpdatedAt = now;
        }

        if (stale.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return stale.Count;
    }

    private static MarketProperty MapToEntity(NormalizedPropertyDto dto, DateTime now) => new()
    {
        SourceId = dto.SourceId,
        ExternalId = dto.ExternalId,
        Url = dto.Url,
        Title = dto.Title,
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
        FirstSeenAt = now,
        LastSeenAt = now,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static MarketPropertySnapshot CreateSnapshot(long propertyId, NormalizedPropertyDto dto, DateTime now) => new()
    {
        MarketPropertyId = propertyId,
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
