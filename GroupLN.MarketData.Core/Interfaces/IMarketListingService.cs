using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IMarketListingService
{
    // Geeft (wasCreated, assetId) terug
    Task<(bool wasCreated, long assetId)> UpsertListingAsync(
        NormalizedPropertyDto property,
        CancellationToken cancellationToken = default);

    // Slaat units van een ProjectGroup op; dryRun=true logt enkel zonder DB-schrijf
    Task<ProjectGroupSaveResult> UpsertProjectUnitsAsync(
        long parentAssetId,
        NormalizedPropertyDto projectDto,
        IReadOnlyList<ProjectGroupUnitDto> units,
        bool dryRun,
        CancellationToken cancellationToken = default);

    Task MarkInactiveAsync(
        int sourceId,
        IEnumerable<string> activeExternalIds,
        CancellationToken cancellationToken = default);

    Task<int> MarkStaleListingsInactiveAsync(
        int sourceId,
        int afterDays,
        CancellationToken cancellationToken = default);
}
