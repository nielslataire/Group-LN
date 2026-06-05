using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IMarketAssetMatcher
{
    Task<MarketAssetMatchResult> FindMatchingAssetAsync(
        NormalizedPropertyDto property,
        CancellationToken cancellationToken = default);
}
