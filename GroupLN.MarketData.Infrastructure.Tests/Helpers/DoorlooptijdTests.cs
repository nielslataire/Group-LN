using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Helpers;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Helpers;

public class DoorlooptijdTests
{
    private static readonly DateTime Start = new(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Bronbevestigde_verkoop_gebruikt_FirstSoldAt()
    {
        var asset = new MarketAsset
        {
            SaleStatus = SaleStatus.Sold,
            LifecycleStatus = AssetLifecycleStatus.SoldConfirmed,
            FirstSeenAt = Start,
            FirstSoldAt = Start.AddDays(45),
            LifecycleStatusUpdatedAt = Start.AddDays(60),
            StatusChangedAt = Start.AddDays(60)
        };

        Assert.Equal(Start.AddDays(45), SaleStateHelpers.VerkoopDatum(asset));
        Assert.Equal(45, SaleStateHelpers.DoorlooptijdDagen(asset));
    }

    [Fact]
    public void Verdwenen_unit_gebruikt_moment_van_lifecycle_wissel()
    {
        var asset = new MarketAsset
        {
            SaleStatus = SaleStatus.Available,
            LifecycleStatus = AssetLifecycleStatus.LikelySold,
            FirstSeenAt = Start,
            FirstSoldAt = null,
            LifecycleStatusUpdatedAt = Start.AddDays(30.5),
            StatusChangedAt = null
        };

        Assert.Equal(Start.AddDays(30.5), SaleStateHelpers.VerkoopDatum(asset));
        Assert.Equal(30, SaleStateHelpers.DoorlooptijdDagen(asset));
    }

    [Fact]
    public void Niet_verkocht_heeft_geen_verkoopdatum_of_doorlooptijd()
    {
        var asset = new MarketAsset
        {
            SaleStatus = SaleStatus.Available,
            LifecycleStatus = AssetLifecycleStatus.Available,
            FirstSeenAt = Start,
            LifecycleStatusUpdatedAt = Start.AddDays(3)
        };

        Assert.Null(SaleStateHelpers.VerkoopDatum(asset));
        Assert.Null(SaleStateHelpers.DoorlooptijdDagen(asset));
    }

    [Fact]
    public void Al_verkocht_bij_eerste_crawl_geeft_geen_doorlooptijd()
    {
        // FirstSoldAt == FirstSeenAt: de echte doorlooptijd is onbekend, 0 zou de mediaan vertekenen.
        var asset = new MarketAsset
        {
            SaleStatus = SaleStatus.Sold,
            LifecycleStatus = AssetLifecycleStatus.SoldConfirmed,
            FirstSeenAt = Start,
            FirstSoldAt = Start
        };

        Assert.NotNull(SaleStateHelpers.VerkoopDatum(asset));
        Assert.Null(SaleStateHelpers.DoorlooptijdDagen(asset));
    }

    [Theory]
    [InlineData(new int[0], null)]
    [InlineData(new[] { 7 }, 7)]
    [InlineData(new[] { 90, 10, 30 }, 30)]
    [InlineData(new[] { 10, 20, 30, 40 }, 25)]
    public void Mediaan_van_doorlooptijden(int[] waarden, int? verwacht)
    {
        Assert.Equal(verwacht, SaleStateHelpers.Mediaan(waarden));
    }
}
