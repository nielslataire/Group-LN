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
        int missingThreshold = 1,
        CancellationToken cancellationToken = default);

    // missingThreshold: aantal opeenvolgende ontbrekende crawls voor deactivatie
    Task<MarkInactiveResult> MarkInactiveAsync(
        int sourceId,
        IEnumerable<string> activeExternalIds,
        int missingThreshold = 1,
        CancellationToken cancellationToken = default);

    Task<MarkInactiveResult> MarkStaleListingsInactiveAsync(
        int sourceId,
        int afterDays,
        CancellationToken cancellationToken = default);

    // Geeft de ExternalId terug van een bestaande actieve listing op basis van URL + source.
    // Gebruikt om bij fetch-fouten te voorkomen dat een bestaande listing als 'missing' geteld wordt.
    Task<string?> FindExternalIdByUrlAsync(
        string url,
        int sourceId,
        CancellationToken cancellationToken = default);

    // Geeft de huidige Title van de actieve listing terug voor het gegeven asset.
    // Gebruikt na AI-extractie om de AI-bijgewerkte projectnaam op te halen voor logging.
    Task<string?> GetListingTitleAsync(long assetId, CancellationToken cancellationToken = default);
}
