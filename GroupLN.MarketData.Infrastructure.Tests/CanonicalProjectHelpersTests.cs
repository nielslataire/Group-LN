using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Helpers;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests;

public class CanonicalProjectHelpersTests
{
    // ── Test 1 ────────────────────────────────────────────────────────────────
    [Fact]
    public void ResolveLinkedProjectIds_NoCanonicalLinks_ReturnsSingleId()
    {
        var result = CanonicalProjectHelpers.ResolveLinkedProjectIds(42L, []);

        Assert.Equal([42L], result);
    }

    // ── Test 2 ────────────────────────────────────────────────────────────────
    [Fact]
    public void ResolveLinkedProjectIds_NineLinkedAssets_ReturnsAllNineIds()
    {
        var links = Enumerable.Range(1, 9)
            .Select(i => new CanonicalProjectAsset
            {
                Id                 = i,
                CanonicalProjectId = 100,
                MarketAssetId      = i * 10L,
                IsPrimary          = i == 1
            })
            .ToList();

        var result = CanonicalProjectHelpers.ResolveLinkedProjectIds(10L, links);

        Assert.Equal(9, result.Count);
        Assert.Contains(10L, result);
        Assert.Contains(90L, result);
    }

    // ── Test 3 ────────────────────────────────────────────────────────────────
    [Fact]
    public void ComputeUnitStatsByProject_UnitsAcrossNineProjects_TotalIsTwentyEight()
    {
        // Representative project: 3 units; 8 siblings with varying counts (total = 25 + 3 = 28)
        var units = new List<MarketAsset>();
        long[] projectIds = [101, 102, 103, 104, 105, 106, 107, 108, 109];
        int[] unitCounts  = [  3,   4,   3,   4,   3,   3,   2,   4,   2];

        int id = 1;
        for (int p = 0; p < projectIds.Length; p++)
            for (int u = 0; u < unitCounts[p]; u++)
                units.Add(new MarketAsset { Id = id++, ParentMarketAssetId = projectIds[p] });

        var stats = CanonicalProjectHelpers.ComputeUnitStatsByProject(units);

        Assert.Equal(28, stats.Values.Sum(s => s.Total));
    }

    // ── Test 4 ────────────────────────────────────────────────────────────────
    [Fact]
    public void ComputeUnitStatsByProject_AvailableCount_CorrectPerProject()
    {
        var units = new List<MarketAsset>
        {
            new() { Id = 1, ParentMarketAssetId = 10, SaleStatus = SaleStatus.Available },
            new() { Id = 2, ParentMarketAssetId = 10, SaleStatus = SaleStatus.Available },
            new() { Id = 3, ParentMarketAssetId = 10, SaleStatus = SaleStatus.Sold },
            new() { Id = 4, ParentMarketAssetId = 20, SaleStatus = SaleStatus.Available },
            new() { Id = 5, ParentMarketAssetId = 20, SaleStatus = SaleStatus.Sold },
        };

        var stats = CanonicalProjectHelpers.ComputeUnitStatsByProject(units);

        Assert.Equal(2, stats[10].Available);
        Assert.Equal(1, stats[20].Available);
    }

    // ── Test 5 ────────────────────────────────────────────────────────────────
    [Fact]
    public void ComputeUnitStatsByProject_SoldCount_CorrectPerProject()
    {
        var units = new List<MarketAsset>
        {
            new() { Id = 1, ParentMarketAssetId = 10, SaleStatus = SaleStatus.Sold },
            new() { Id = 2, ParentMarketAssetId = 10, SaleStatus = SaleStatus.Sold },
            new() { Id = 3, ParentMarketAssetId = 10, SaleStatus = SaleStatus.Available },
            new() { Id = 4, ParentMarketAssetId = 20, SaleStatus = SaleStatus.Sold },
            new() { Id = 5, ParentMarketAssetId = 20, SaleStatus = SaleStatus.Available },
        };

        var stats = CanonicalProjectHelpers.ComputeUnitStatsByProject(units);

        Assert.Equal(2, stats[10].Sold);
        Assert.Equal(1, stats[20].Sold);
    }
}
