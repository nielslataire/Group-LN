using System.Text.Json;
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

    public async Task<(bool wasCreated, long assetId)> UpsertListingAsync(
        NormalizedPropertyDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Stap 1: bestaande listing zoeken op SourceId + ExternalId
        var existing = await _context.MarketListings
            .Include(l => l.Snapshots.OrderByDescending(s => s.SnapshotDate).Take(1))
            .Include(l => l.Asset)
            .FirstOrDefaultAsync(l => l.SourceId == dto.SourceId && l.ExternalId == dto.ExternalId, cancellationToken);

        if (existing is not null)
        {
            var oldPrice = existing.AskingPrice;

            existing.Title = dto.Title ?? existing.Title;
            existing.AskingPrice = dto.AskingPrice ?? existing.AskingPrice;
            existing.LastSeenAt = now;
            existing.IsActive = true;
            existing.RemovedAt = null;
            existing.MissingCrawlCount = 0;
            existing.UpdatedAt = now;

            UpdateAssetFromDto(existing.Asset, dto, now);

            var lastSnapshot = existing.Snapshots.MaxBy(s => s.SnapshotDate);

            if (HasRelevantChange(lastSnapshot, dto))
                _context.MarketListingSnapshots.Add(CreateSnapshot(existing.Id, dto, now));

            if (dto.AskingPrice.HasValue && dto.AskingPrice != oldPrice)
            {
                var change = new MarketListingPriceHistory
                {
                    MarketListingId = existing.Id,
                    DetectedAt = now,
                    AskingPrice = dto.AskingPrice.Value,
                    PreviousPrice = oldPrice
                };

                if (oldPrice is decimal prev && prev != 0)
                {
                    change.PriceChangeAmount = dto.AskingPrice.Value - prev;
                    change.PriceChangePercentage = change.PriceChangeAmount / prev * 100m;
                }

                _context.MarketListingPriceHistories.Add(change);
                _logger.LogInformation(
                    "PriceChangeDetected UnitId={ExternalId} OldPrice={Old} NewPrice={New}",
                    dto.ExternalId, oldPrice, dto.AskingPrice.Value);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Listing bijgewerkt: {ExternalId} ({City} {PostalCode}).",
                dto.ExternalId, dto.City, dto.PostalCode);
            return (false, existing.Asset.Id);
        }

        // Stap 2: nieuwe listing — zoek of maak MarketAsset
        var (assetKey, assetKeyStrategy) = BuildAssetKey(dto);
        _logger.LogInformation("AssetKey strategie={Strategy} | key={Key}", assetKeyStrategy, assetKey);

        var matchResult = await _assetMatcher.FindMatchingAssetAsync(dto, cancellationToken);

        MarketAsset asset;

        if (matchResult.AssetMatchType == AssetMatchType.Strong && matchResult.MatchedAssetId.HasValue)
        {
            asset = (await _context.MarketAssets.FindAsync(new object[] { matchResult.MatchedAssetId.Value }, cancellationToken))!;
            UpdateAssetFromDto(asset, dto, now);
            _logger.LogInformation(
                "Nieuwe listing {ExternalId} gekoppeld aan bestaand MarketAsset {AssetId} (Strong, score={Score}).",
                dto.ExternalId, asset.Id, matchResult.MatchScore);
        }
        else
        {
            asset = CreateAsset(dto, now, assetKey);
            _context.MarketAssets.Add(asset);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Nieuw MarketAsset {AssetId} aangemaakt | {City} {PostalCode} | {Type} | match={AssetMatchType} | strategy={Strategy}.",
                asset.Id, dto.City, dto.PostalCode, dto.PropertyType, matchResult.AssetMatchType, assetKeyStrategy);
        }

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

        // Eerste snapshot altijd aanmaken
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
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Nieuwe listing aangemaakt: {ExternalId} ({City} {PostalCode}).",
            dto.ExternalId, dto.City, dto.PostalCode);
        return (true, asset.Id);
    }

    public async Task<ProjectGroupSaveResult> UpsertProjectUnitsAsync(
        long parentAssetId,
        NormalizedPropertyDto projectDto,
        IReadOnlyList<ProjectGroupUnitDto> units,
        bool dryRun,
        int missingThreshold = 1,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        int unitsCreated = 0, unitsUpdated = 0, statusChanges = 0;
        int houseUnits = 0, apartmentUnits = 0, commercialUnits = 0;
        int soldUnits = 0, availableUnits = 0, reservedUnits = 0, optionUnits = 0, unknownUnits = 0;

        var seenUnitIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var unit in units)
        {
            if (string.IsNullOrEmpty(unit.UnitId)) continue;

            seenUnitIds.Add(unit.UnitId);

            switch (unit.MappedPropertyType)
            {
                case PropertyType.House: houseUnits++; break;
                case PropertyType.Apartment: apartmentUnits++; break;
                case PropertyType.CommercialProperty: commercialUnits++; break;
            }

            switch (unit.SaleStatus)
            {
                case SaleStatus.Sold: soldUnits++; break;
                case SaleStatus.Available: availableUnits++; break;
                case SaleStatus.Reserved: reservedUnits++; break;
                case SaleStatus.Option: optionUnits++; break;
                default: unknownUnits++; break;
            }

            if (dryRun)
            {
                _logger.LogInformation(
                    "[DRYRUN] Unit zou worden opgeslagen: {UnitId} | {SubType} | {Status} | {Surface}m² | {Beds} slpk | {Price}",
                    unit.UnitId,
                    unit.RawSubType ?? unit.RawGroupType ?? "?",
                    unit.SaleStatus,
                    unit.Surface?.ToString("N0") ?? "?",
                    unit.BedroomCount?.ToString() ?? "?",
                    unit.Price.HasValue ? $"€{unit.Price.Value:N0}" : "geen prijs");
                continue;
            }

            var existing = await _context.MarketListings
                .Include(l => l.Asset)
                .Include(l => l.Snapshots.OrderByDescending(s => s.SnapshotDate).Take(1))
                .FirstOrDefaultAsync(l => l.SourceId == projectDto.SourceId && l.ExternalId == unit.UnitId, cancellationToken);

            if (existing is not null)
            {
                var oldStatus = existing.Asset.SaleStatus;

                existing.AskingPrice = unit.Price ?? existing.AskingPrice;
                existing.LastSeenAt = now;
                existing.IsActive = true;
                existing.RemovedAt = null;
                existing.MissingCrawlCount = 0;
                existing.UpdatedAt = now;

                existing.Asset.SaleStatus = unit.SaleStatus;
                if (unit.Surface.HasValue) existing.Asset.LivingArea = unit.Surface;
                if (unit.BedroomCount.HasValue) existing.Asset.Bedrooms = unit.BedroomCount;
                existing.Asset.LastSeenAt = now;
                existing.Asset.UpdatedAt = now;

                if (oldStatus != unit.SaleStatus)
                {
                    statusChanges++;
                    existing.Asset.StatusChangedAt = now;

                    if (unit.SaleStatus == SaleStatus.Sold && !existing.Asset.FirstSoldAt.HasValue)
                        existing.Asset.FirstSoldAt = now;

                    _logger.LogInformation(
                        "StatusChangeDetected UnitId={UnitId} {OldStatus} -> {NewStatus}",
                        unit.UnitId, oldStatus?.ToString() ?? "null", unit.SaleStatus);
                }

                var lastSnapshot = existing.Snapshots.MaxBy(s => s.SnapshotDate);
                if (HasUnitRelevantChange(lastSnapshot, unit))
                    _context.MarketListingSnapshots.Add(CreateUnitSnapshot(existing.Id, unit, now, projectDto.IsNewBuild));

                unitsUpdated++;

                _logger.LogInformation(
                    "UnitSaved (Updated) | ParentProjectId={ParentId} | UnitExternalId={UnitId} | " +
                    "{PropertyType}/{SubType} | Status={Status} | Opp={Surface}m² | Kamers={Beds} | Prijs={Price}",
                    parentAssetId, unit.UnitId,
                    unit.MappedPropertyType, unit.RawSubType ?? "?",
                    unit.SaleStatus,
                    unit.Surface?.ToString("N0") ?? "?",
                    unit.BedroomCount?.ToString() ?? "?",
                    unit.Price.HasValue ? $"€{unit.Price.Value:N0}" : "?");
            }
            else
            {
                var assetKey = BuildUnitAssetKey(
                    projectDto.Country ?? "BE",
                    projectDto.PostalCode ?? "0000",
                    projectDto.ExternalId,
                    unit.UnitId);

                var asset = new MarketAsset
                {
                    AssetKey = assetKey,
                    PropertyType = unit.MappedPropertyType,
                    PropertySubType = unit.MappedPropertySubType,
                    TransactionType = projectDto.TransactionType,
                    Country = projectDto.Country ?? "BE",
                    PostalCode = projectDto.PostalCode,
                    City = projectDto.City,
                    Street = projectDto.Street,
                    HouseNumber = projectDto.HouseNumber,
                    Latitude = projectDto.Latitude,
                    Longitude = projectDto.Longitude,
                    LivingArea = unit.Surface,
                    Floor = unit.Floor,
                    Bedrooms = unit.BedroomCount,
                    NewBuild = projectDto.IsNewBuild,
                    IsProjectGroup = false,
                    ParentMarketAssetId = parentAssetId,
                    ProjectExternalId = projectDto.ExternalId,
                    UnitExternalId = unit.UnitId,
                    SaleStatus = unit.SaleStatus,
                    FirstSoldAt = unit.SaleStatus == SaleStatus.Sold ? now : null,
                    StatusChangedAt = null,
                    FirstSeenAt = now,
                    LastSeenAt = now,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.MarketAssets.Add(asset);
                await _context.SaveChangesAsync(cancellationToken);

                var projectName = unit.ParentProjectName ?? "Project";
                var subTypeLabel = unit.RawSubType ?? unit.RawGroupType ?? "Unit";

                var listing = new MarketListing
                {
                    MarketAssetId = asset.Id,
                    SourceId = projectDto.SourceId,
                    ExternalId = unit.UnitId,
                    Url = $"{projectDto.Url}#unit-{unit.UnitId}",
                    Title = $"{projectName} – {subTypeLabel} – Unit {unit.UnitId}",
                    AskingPrice = unit.Price,
                    FirstSeenAt = now,
                    LastSeenAt = now,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.MarketListings.Add(listing);
                await _context.SaveChangesAsync(cancellationToken);

                _context.MarketListingSnapshots.Add(CreateUnitSnapshot(listing.Id, unit, now, projectDto.IsNewBuild));
                unitsCreated++;

                _logger.LogInformation(
                    "UnitSaved (Created) | ParentProjectId={ParentId} | AssetId={AssetId} | UnitExternalId={UnitId} | " +
                    "{PropertyType}/{SubType} | Status={Status} | Opp={Surface}m² | Kamers={Beds} | Prijs={Price}",
                    parentAssetId, asset.Id, unit.UnitId,
                    unit.MappedPropertyType, unit.RawSubType ?? "?",
                    unit.SaleStatus,
                    unit.Surface?.ToString("N0") ?? "?",
                    unit.BedroomCount?.ToString() ?? "?",
                    unit.Price.HasValue ? $"€{unit.Price.Value:N0}" : "?");
            }
        }

        // KPI-aggregaten berekenen op basis van alle aangeboden units
        var pricedUnits = units.Where(u => u.Price.HasValue && u.Price.Value > 0).ToList();
        var areaUnits = units.Where(u => u.Surface.HasValue && u.Surface.Value > 0).ToList();
        var pricedAndArea = pricedUnits.Where(u => u.Surface.HasValue && u.Surface.Value > 0).ToList();

        var minPrice = pricedUnits.Count > 0 ? pricedUnits.Min(u => u.Price!.Value) : (decimal?)null;
        var maxPrice = pricedUnits.Count > 0 ? pricedUnits.Max(u => u.Price!.Value) : (decimal?)null;
        var avgPrice = pricedUnits.Count > 0 ? Math.Round(pricedUnits.Average(u => u.Price!.Value), 2) : (decimal?)null;

        var ppSqms = pricedAndArea.Select(u => u.Price!.Value / u.Surface!.Value).ToList();
        var minPpSqm = ppSqms.Count > 0 ? Math.Round(ppSqms.Min(), 2) : (decimal?)null;
        var maxPpSqm = ppSqms.Count > 0 ? Math.Round(ppSqms.Max(), 2) : (decimal?)null;
        var avgPpSqm = ppSqms.Count > 0 ? Math.Round(ppSqms.Average(), 2) : (decimal?)null;

        var minArea = areaUnits.Count > 0 ? areaUnits.Min(u => u.Surface!.Value) : (decimal?)null;
        var maxArea = areaUnits.Count > 0 ? areaUnits.Max(u => u.Surface!.Value) : (decimal?)null;
        var avgArea = areaUnits.Count > 0 ? Math.Round(areaUnits.Average(u => u.Surface!.Value), 2) : (decimal?)null;

        if (!dryRun)
        {
            if (unitsCreated > 0 || unitsUpdated > 0)
                await _context.SaveChangesAsync(cancellationToken);

            // Verdwenen units bijhouden via threshold
            await MarkMissingProjectUnitsAsync(parentAssetId, projectDto.SourceId, seenUnitIds, missingThreshold, now, cancellationToken);

            // ProjectGroup snapshot bijwerken indien stats gewijzigd
            await UpsertProjectGroupSnapshotAsync(
                parentAssetId, units.Count, soldUnits, availableUnits, reservedUnits, optionUnits, unknownUnits, now, cancellationToken);

            // KPI altijd opslaan per crawl (historisch)
            await InsertProjectGroupKpiAsync(
                parentAssetId,
                units.Count, availableUnits, reservedUnits, soldUnits,
                apartmentUnits, houseUnits,
                minPrice, maxPrice, avgPrice,
                minPpSqm, maxPpSqm, avgPpSqm,
                minArea, maxArea, avgArea,
                now, cancellationToken);
        }

        return new ProjectGroupSaveResult(
            UnitsFound: units.Count,
            UnitsCreated: unitsCreated,
            UnitsUpdated: unitsUpdated,
            HouseUnits: houseUnits,
            ApartmentUnits: apartmentUnits,
            CommercialUnits: commercialUnits,
            SoldUnits: soldUnits,
            AvailableUnits: availableUnits,
            ReservedUnits: reservedUnits,
            OptionUnits: optionUnits,
            UnknownUnits: unknownUnits,
            StatusChanges: statusChanges,
            MinPrice: minPrice,
            MaxPrice: maxPrice,
            AveragePrice: avgPrice,
            MinPricePerSqm: minPpSqm,
            MaxPricePerSqm: maxPpSqm,
            AveragePricePerSqm: avgPpSqm,
            MinLivingArea: minArea,
            MaxLivingArea: maxArea,
            AverageLivingArea: avgArea);
    }

    public async Task MarkInactiveAsync(
        int sourceId,
        IEnumerable<string> activeExternalIds,
        int missingThreshold = 1,
        CancellationToken cancellationToken = default)
    {
        var activeSet = activeExternalIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        var candidates = await _context.MarketListings
            .Where(l => l.SourceId == sourceId && l.IsActive && !activeSet.Contains(l.ExternalId))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0) return;

        int deactivated = 0;
        foreach (var l in candidates)
        {
            l.MissingCrawlCount++;
            l.UpdatedAt = now;

            if (l.MissingCrawlCount >= missingThreshold)
            {
                l.IsActive = false;
                l.RemovedAt = now;
                deactivated++;
                _logger.LogInformation(
                    "ListingDeactivated ExternalId={ExternalId} na {Count} ontbrekende crawls.",
                    l.ExternalId, l.MissingCrawlCount);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "{Missing} listings ontbreken (bron {SourceId}): {Deactivated} gedeactiveerd, {Pending} nog in threshold.",
            candidates.Count, sourceId, deactivated, candidates.Count - deactivated);
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

    // ── Privé helpers ─────────────────────────────────────────────────────────

    private async Task MarkMissingProjectUnitsAsync(
        long parentAssetId,
        int sourceId,
        HashSet<string> seenUnitIds,
        int missingThreshold,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existingUnits = await _context.MarketListings
            .Where(l => l.SourceId == sourceId && l.IsActive
                && _context.MarketAssets
                    .Where(a => a.ParentMarketAssetId == parentAssetId)
                    .Select(a => a.Id)
                    .Contains(l.MarketAssetId))
            .ToListAsync(cancellationToken);

        int deactivated = 0;
        foreach (var unit in existingUnits.Where(u => !seenUnitIds.Contains(u.ExternalId)))
        {
            unit.MissingCrawlCount++;
            unit.UpdatedAt = now;

            if (unit.MissingCrawlCount >= missingThreshold)
            {
                unit.IsActive = false;
                unit.RemovedAt = now;
                deactivated++;
                _logger.LogInformation(
                    "UnitDeactivated ExternalId={ExternalId} (project {ProjectId}) na {Count} ontbrekende crawls.",
                    unit.ExternalId, parentAssetId, unit.MissingCrawlCount);
            }
        }

        if (deactivated > 0 || existingUnits.Any(u => !seenUnitIds.Contains(u.ExternalId)))
            await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertProjectGroupSnapshotAsync(
        long parentAssetId,
        int unitsTotal,
        int unitsSold,
        int unitsAvailable,
        int unitsReserved,
        int unitsOption,
        int unitsUnknown,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var lastSnapshot = await _context.ProjectGroupSnapshots
            .Where(s => s.MarketAssetId == parentAssetId)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastSnapshot is not null
            && lastSnapshot.UnitsTotal == unitsTotal
            && lastSnapshot.UnitsSold == unitsSold
            && lastSnapshot.UnitsAvailable == unitsAvailable
            && lastSnapshot.UnitsReserved == unitsReserved
            && lastSnapshot.UnitsOption == unitsOption
            && lastSnapshot.UnitsUnknown == unitsUnknown)
        {
            return;
        }

        var soldPct = unitsTotal > 0
            ? Math.Round((decimal)unitsSold / unitsTotal * 100, 4)
            : 0m;

        if (lastSnapshot is not null && lastSnapshot.SoldPercentage != soldPct)
        {
            _logger.LogInformation(
                "ProjectSoldPercentageChanged ProjectId={ProjectId} Old={Old}% New={New}%",
                parentAssetId, lastSnapshot.SoldPercentage, soldPct);
        }

        _context.ProjectGroupSnapshots.Add(new ProjectGroupSnapshot
        {
            MarketAssetId = parentAssetId,
            SnapshotDate = now,
            UnitsTotal = unitsTotal,
            UnitsSold = unitsSold,
            UnitsAvailable = unitsAvailable,
            UnitsReserved = unitsReserved,
            UnitsOption = unitsOption,
            UnitsUnknown = unitsUnknown,
            SoldPercentage = soldPct
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task InsertProjectGroupKpiAsync(
        long parentAssetId,
        int unitsTotal, int unitsAvailable, int unitsReserved, int unitsSold,
        int apartmentCount, int houseCount,
        decimal? minPrice, decimal? maxPrice, decimal? avgPrice,
        decimal? minPpSqm, decimal? maxPpSqm, decimal? avgPpSqm,
        decimal? minArea, decimal? maxArea, decimal? avgArea,
        DateTime now, CancellationToken cancellationToken)
    {
        var soldPct = unitsTotal > 0
            ? Math.Round((decimal)unitsSold / unitsTotal * 100, 4)
            : 0m;

        _context.ProjectGroupKpis.Add(new ProjectGroupKpi
        {
            MarketAssetId = parentAssetId,
            SnapshotDate = now,
            UnitsTotal = unitsTotal,
            UnitsAvailable = unitsAvailable,
            UnitsReserved = unitsReserved,
            UnitsSold = unitsSold,
            SoldPercentage = soldPct,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            AveragePrice = avgPrice,
            MinPricePerSqm = minPpSqm,
            MaxPricePerSqm = maxPpSqm,
            AveragePricePerSqm = avgPpSqm,
            MinLivingArea = minArea,
            MaxLivingArea = maxArea,
            AverageLivingArea = avgArea,
            ApartmentCount = apartmentCount,
            HouseCount = houseCount,
            CreatedAt = now
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static bool HasRelevantChange(MarketListingSnapshot? last, NormalizedPropertyDto dto)
    {
        if (last is null) return true;
        return last.AskingPrice != dto.AskingPrice
            || last.LivingArea != dto.LivingArea
            || last.Bedrooms != dto.Bedrooms
            || last.Bathrooms != dto.Bathrooms
            || last.ConstructionYear != dto.ConstructionYear
            || last.EPCScore != dto.EPCScore
            || last.EPCLabel != dto.EPCLabel
            || last.GarageCount != dto.GarageCount
            || last.LandArea != dto.LandArea;
    }

    private static bool HasUnitRelevantChange(MarketListingSnapshot? last, ProjectGroupUnitDto unit)
    {
        if (last is null) return true;
        return last.AskingPrice != unit.Price
            || last.LivingArea != unit.Surface
            || last.Bedrooms != unit.BedroomCount
            || last.SaleStatus != unit.SaleStatus;
    }

    private static void UpdateAssetFromDto(MarketAsset asset, NormalizedPropertyDto dto, DateTime now)
    {
        if (!string.IsNullOrEmpty(dto.PostalCode)) asset.PostalCode = dto.PostalCode;
        if (!string.IsNullOrEmpty(dto.City)) asset.City = dto.City;
        if (!string.IsNullOrEmpty(dto.Street)) asset.Street = dto.Street;
        if (!string.IsNullOrEmpty(dto.HouseNumber)) asset.HouseNumber = dto.HouseNumber;
        if (dto.Floor.HasValue) asset.Floor = dto.Floor;
        if (dto.Latitude.HasValue) asset.Latitude = dto.Latitude;
        if (dto.Longitude.HasValue) asset.Longitude = dto.Longitude;
        if (dto.LivingArea.HasValue) asset.LivingArea = dto.LivingArea;
        if (dto.LandArea.HasValue) asset.LandArea = dto.LandArea;
        if (dto.TerraceArea.HasValue) asset.TerraceArea = dto.TerraceArea;
        if (dto.GardenArea.HasValue) asset.GardenArea = dto.GardenArea;
        if (dto.Bedrooms.HasValue) asset.Bedrooms = dto.Bedrooms;
        if (dto.Bathrooms.HasValue) asset.Bathrooms = dto.Bathrooms;
        if (dto.ShowerCount.HasValue) asset.ShowerCount = dto.ShowerCount;
        if (dto.ToiletCount.HasValue) asset.ToiletCount = dto.ToiletCount;
        if (dto.GarageCount.HasValue) asset.GarageCount = dto.GarageCount;
        if (dto.ConstructionYear.HasValue) asset.ConstructionYear = dto.ConstructionYear;
        if (dto.EPCScore.HasValue) asset.EPCScore = dto.EPCScore;
        if (dto.EPCLabel.HasValue) asset.EPCLabel = dto.EPCLabel;
        if (dto.MaxPrice.HasValue) asset.MaxPrice = dto.MaxPrice;
        if (!string.IsNullOrEmpty(dto.EnergyFeatures)) asset.EnergyFeatures = dto.EnergyFeatures;
        if (!string.IsNullOrEmpty(dto.DeveloperName)) asset.DeveloperName = dto.DeveloperName;
        if (!string.IsNullOrEmpty(dto.DeveloperWebsite)) asset.DeveloperWebsite = dto.DeveloperWebsite;
        if (!string.IsNullOrEmpty(dto.DeveloperPhone)) asset.DeveloperPhone = dto.DeveloperPhone;
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
        Floor = dto.Floor,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        LivingArea = dto.LivingArea,
        LandArea = dto.LandArea,
        TerraceArea = dto.TerraceArea,
        GardenArea = dto.GardenArea,
        Bedrooms = dto.Bedrooms,
        Bathrooms = dto.Bathrooms,
        ShowerCount = dto.ShowerCount,
        ToiletCount = dto.ToiletCount,
        GarageCount = dto.GarageCount,
        ConstructionYear = dto.ConstructionYear,
        EPCScore = dto.EPCScore,
        EPCLabel = dto.EPCLabel,
        MaxPrice = dto.MaxPrice,
        EnergyFeatures = dto.EnergyFeatures,
        DeveloperName = dto.DeveloperName,
        DeveloperWebsite = dto.DeveloperWebsite,
        DeveloperPhone = dto.DeveloperPhone,
        NewBuild = dto.IsNewBuild,
        IsProjectGroup = dto.IsProjectListing || dto.PropertyType == PropertyType.ProjectGroup,
        ProjectExternalId = dto.ProjectExternalId,
        UnitExternalId = dto.UnitExternalId,
        FirstSeenAt = now,
        LastSeenAt = now,
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static (string Key, string Strategy) BuildAssetKey(NormalizedPropertyDto dto)
    {
        var country = S(dto.Country ?? "BE").ToUpperInvariant();
        var postal = S(dto.PostalCode ?? "0000");
        var street = S(dto.Street).ToUpperInvariant();
        var houseNr = S(dto.HouseNumber).ToUpperInvariant();
        var propType = dto.PropertyType.ToString();
        var txType = dto.TransactionType.ToString();
        var extId = S(dto.ExternalId);

        if (dto.IsProjectListing || dto.PropertyType == PropertyType.ProjectGroup)
            return (K(country, postal, propType, txType, extId), "ProjectGroup");

        if (dto.IsProjectUnit || !string.IsNullOrEmpty(dto.ProjectExternalId))
        {
            var projId = S(dto.ProjectExternalId);
            var unitId = S(dto.UnitExternalId ?? extId);
            return (K(country, postal, "ProjectUnit", txType, projId, unitId), "ProjectUnit");
        }

        if (dto.PropertyType == PropertyType.Apartment)
        {
            if (dto.Floor.HasValue || !string.IsNullOrEmpty(dto.UnitNumber))
            {
                var floor = dto.Floor.HasValue ? dto.Floor.Value.ToString() : "X";
                var unit = S(dto.UnitNumber).ToUpperInvariant();
                return (K(country, postal, street, houseNr, propType, txType, floor, unit), "Apartment+Floor");
            }
            return (K(country, postal, street, houseNr, propType, txType, extId), "Apartment+ExternalId");
        }

        if (string.IsNullOrEmpty(dto.Street))
            return (K(country, postal, propType, txType, extId), "NoAddress+ExternalId");

        return (K(country, postal, street, houseNr, propType, txType), "House");

        static string S(string? v) => (v ?? string.Empty).Trim().Replace("|", "_");
        static string K(params string[] parts) => string.Join("|", parts);
    }

    private static string BuildUnitAssetKey(string country, string postalCode, string projectExternalId, string unitExternalId)
    {
        static string S(string? v) => (v ?? string.Empty).Trim().Replace("|", "_");
        return string.Join("|", S(country).ToUpperInvariant(), S(postalCode), "ProjectUnit", "ForSale", S(projectExternalId), S(unitExternalId));
    }

    private static MarketListingSnapshot CreateSnapshot(long listingId, NormalizedPropertyDto dto, DateTime now) => new()
    {
        MarketListingId = listingId,
        SnapshotDate = now,
        AskingPrice = dto.AskingPrice,
        MaxPrice = dto.MaxPrice,
        PricePerSqm = (dto.AskingPrice > 0 && dto.LivingArea > 0)
            ? Math.Round(dto.AskingPrice!.Value / dto.LivingArea!.Value, 2)
            : null,
        LivingArea = dto.LivingArea,
        LandArea = dto.LandArea,
        TerraceArea = dto.TerraceArea,
        GardenArea = dto.GardenArea,
        Floor = dto.Floor,
        Bedrooms = dto.Bedrooms,
        Bathrooms = dto.Bathrooms,
        ShowerCount = dto.ShowerCount,
        ToiletCount = dto.ToiletCount,
        GarageCount = dto.GarageCount,
        ConstructionYear = dto.ConstructionYear,
        EPCScore = dto.EPCScore,
        EPCLabel = dto.EPCLabel,
        IsNewBuild = dto.IsNewBuild,
        EnergyFeatures = dto.EnergyFeatures,
        DescriptionHash = dto.DescriptionHash,
        RawJson = dto.RawJson
    };

    private static MarketListingSnapshot CreateUnitSnapshot(long listingId, ProjectGroupUnitDto unit, DateTime now, bool isNewBuild) => new()
    {
        MarketListingId = listingId,
        SnapshotDate = now,
        AskingPrice = unit.Price,
        PricePerSqm = (unit.Price > 0 && unit.Surface > 0)
            ? Math.Round(unit.Price!.Value / unit.Surface!.Value, 2)
            : null,
        LivingArea = unit.Surface,
        Floor = unit.Floor,
        Bedrooms = unit.BedroomCount,
        IsNewBuild = isNewBuild,
        SaleStatus = unit.SaleStatus,
        RawJson = JsonSerializer.Serialize(unit)
    };
}
