using GroupLN.MarketData.Core.DTOs;

namespace GroupLN.MarketData.Core.Interfaces;

public interface ILooseListingMatchingService
{
    /// <summary>
    /// Matcht losse listings (geen ParentMarketAssetId) aan CanonicalUnits van nabijgelegen projecten.
    /// Stelt LinkedCanonicalUnitId in op de MarketAsset en voegt CanonicalUnitAsset-rijen toe.
    /// Verwijdert geen brondata.
    /// </summary>
    Task<LooseListingMatchResult> MatchLooseListingsAsync(CancellationToken cancellationToken = default);
}
