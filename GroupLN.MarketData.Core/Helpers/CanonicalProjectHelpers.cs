using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Helpers;

public static class CanonicalProjectHelpers
{
    /// <summary>
    /// Returns the IDs of all MarketAssets to query for units in a canonical group.
    /// Falls back to [assetId] when no canonical links exist.
    /// </summary>
    public static List<long> ResolveLinkedProjectIds(long assetId, IReadOnlyList<CanonicalProjectAsset> allLinks)
    {
        if (allLinks.Count == 0) return [assetId];
        var ids = allLinks.Select(l => l.MarketAssetId).Distinct().ToList();
        return ids.Count > 0 ? ids : [assetId];
    }

    /// <summary>
    /// Groups units by their ParentMarketAssetId and returns per-project unit statistics (Total, Available, Sold).
    /// </summary>
    public static Dictionary<long, (int Total, int Available, int Sold)> ComputeUnitStatsByProject(
        IReadOnlyList<MarketAsset> units)
    {
        return units
            .Where(u => u.ParentMarketAssetId.HasValue)
            .GroupBy(u => u.ParentMarketAssetId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (
                    Total    : g.Count(),
                    Available: g.Count(u => u.SaleStatus == SaleStatus.Available),
                    Sold     : g.Count(u => u.SaleStatus == SaleStatus.Sold)
                ));
    }
}
