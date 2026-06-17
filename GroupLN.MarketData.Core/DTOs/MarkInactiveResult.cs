namespace GroupLN.MarketData.Core.DTOs;

public record MarkInactiveResult(
    int ListingsMissing,
    int ListingsDeactivated,
    int AssetsLikelySold,
    int AssetsSoldConfirmed,
    int AssetsIsActiveSetFalse)
{
    public static readonly MarkInactiveResult Empty = new(0, 0, 0, 0, 0);
}
