using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Helpers;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Helpers;

public class SaleStateHelpersTests
{
    [Theory]
    [InlineData(SaleStatus.Sold, AssetLifecycleStatus.Unknown, true)]
    [InlineData(SaleStatus.Available, AssetLifecycleStatus.LikelySold, true)]
    [InlineData(SaleStatus.Available, AssetLifecycleStatus.SoldConfirmed, true)]
    [InlineData(null, AssetLifecycleStatus.LikelySold, true)]
    [InlineData(SaleStatus.Available, AssetLifecycleStatus.Available, false)]
    [InlineData(null, AssetLifecycleStatus.Unknown, false)]
    [InlineData(SaleStatus.Reserved, AssetLifecycleStatus.Reserved, false)]
    public void IsSold_combineert_bronstatus_en_lifecycle(SaleStatus? sale, AssetLifecycleStatus lifecycle, bool expected)
    {
        Assert.Equal(expected, SaleStateHelpers.IsSold(sale, lifecycle));
    }

    [Fact]
    public void Verdwenen_projectunit_telt_niet_meer_als_beschikbaar()
    {
        // Laatste bronstatus was Available, maar de unit is uit de inventaris verdwenen.
        var asset = new MarketAsset
        {
            SaleStatus = SaleStatus.Available,
            LifecycleStatus = AssetLifecycleStatus.LikelySold
        };

        Assert.True(SaleStateHelpers.IsSold(asset));
        Assert.False(SaleStateHelpers.IsAvailable(asset));
        Assert.False(SaleStateHelpers.IsReserved(asset));
    }

    [Fact]
    public void Losse_listing_zonder_bronstatus_is_beschikbaar_zolang_lifecycle_niets_zegt()
    {
        var asset = new MarketAsset { SaleStatus = null, LifecycleStatus = AssetLifecycleStatus.Unknown };

        Assert.True(SaleStateHelpers.IsAvailable(asset));
        Assert.False(SaleStateHelpers.IsSold(asset));
    }

    [Theory]
    [InlineData(SaleStatus.Reserved, AssetLifecycleStatus.Reserved, true)]
    [InlineData(SaleStatus.Option, AssetLifecycleStatus.Reserved, true)]
    [InlineData(SaleStatus.Reserved, AssetLifecycleStatus.LikelySold, false)] // intussen verdwenen → verkocht
    [InlineData(SaleStatus.Available, AssetLifecycleStatus.Available, false)]
    public void IsReserved_enkel_als_niet_verkocht(SaleStatus? sale, AssetLifecycleStatus lifecycle, bool expected)
    {
        Assert.Equal(expected, SaleStateHelpers.IsReserved(sale, lifecycle));
    }

    [Fact]
    public void ComputeUnitStatsByProject_telt_verdwenen_units_als_verkocht()
    {
        var units = new List<MarketAsset>
        {
            new() { ParentMarketAssetId = 1, SaleStatus = SaleStatus.Available, LifecycleStatus = AssetLifecycleStatus.Available },
            new() { ParentMarketAssetId = 1, SaleStatus = SaleStatus.Sold,      LifecycleStatus = AssetLifecycleStatus.SoldConfirmed },
            new() { ParentMarketAssetId = 1, SaleStatus = SaleStatus.Available, LifecycleStatus = AssetLifecycleStatus.LikelySold },
            new() { ParentMarketAssetId = 1, SaleStatus = SaleStatus.Reserved,  LifecycleStatus = AssetLifecycleStatus.Reserved },
        };

        var stats = CanonicalProjectHelpers.ComputeUnitStatsByProject(units)[1];

        Assert.Equal(4, stats.Total);
        Assert.Equal(1, stats.Available);
        Assert.Equal(2, stats.Sold);
    }
}
