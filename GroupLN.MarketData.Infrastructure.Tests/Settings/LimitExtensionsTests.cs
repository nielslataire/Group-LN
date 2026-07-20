using GroupLN.MarketData.Core.Settings;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Settings;

public class LimitExtensionsTests
{
    // ── IsUnlimited ───────────────────────────────────────────────────────────

    [Fact] public void IsUnlimited_Zero_ReturnsTrue()     => Assert.True(0.IsUnlimited());
    [Fact] public void IsUnlimited_Negative_ReturnsTrue() => Assert.True((-1).IsUnlimited());
    [Fact] public void IsUnlimited_One_ReturnsFalse()     => Assert.False(1.IsUnlimited());
    [Fact] public void IsUnlimited_Ten_ReturnsFalse()     => Assert.False(10.IsUnlimited());

    // ── ToEffectiveMax ────────────────────────────────────────────────────────

    [Fact] public void ToEffectiveMax_Zero_ReturnsIntMaxValue()     => Assert.Equal(int.MaxValue, 0.ToEffectiveMax());
    [Fact] public void ToEffectiveMax_Negative_ReturnsIntMaxValue() => Assert.Equal(int.MaxValue, (-5).ToEffectiveMax());
    [Fact] public void ToEffectiveMax_One_ReturnsOne()              => Assert.Equal(1, 1.ToEffectiveMax());
    [Fact] public void ToEffectiveMax_Ten_ReturnsTen()              => Assert.Equal(10, 10.ToEffectiveMax());

    // ── WithLimit ─────────────────────────────────────────────────────────────

    [Fact]
    public void WithLimit_Zero_ReturnsAllItems()
    {
        var items = Enumerable.Range(1, 100);
        Assert.Equal(100, items.WithLimit(0).Count());
    }

    [Fact]
    public void WithLimit_Negative_ReturnsAllItems()
    {
        var items = Enumerable.Range(1, 50);
        Assert.Equal(50, items.WithLimit(-1).Count());
    }

    [Fact]
    public void WithLimit_One_ReturnsExactlyOne()
    {
        var items = Enumerable.Range(1, 100);
        Assert.Single(items.WithLimit(1));
    }

    [Fact]
    public void WithLimit_Ten_ReturnsExactlyTen()
    {
        var items = Enumerable.Range(1, 100);
        Assert.Equal(10, items.WithLimit(10).Count());
    }

    [Fact]
    public void WithLimit_LargerThanCollection_ReturnsAllItems()
    {
        var items = new[] { 1, 2, 3 };
        Assert.Equal(3, items.WithLimit(999).Count());
    }

    // ── ToLimitLabel ──────────────────────────────────────────────────────────

    [Fact] public void ToLimitLabel_Zero_ReturnsOnbeperkt()     => Assert.Equal("onbeperkt", 0.ToLimitLabel());
    [Fact] public void ToLimitLabel_Negative_ReturnsOnbeperkt() => Assert.Equal("onbeperkt", (-3).ToLimitLabel());
    [Fact] public void ToLimitLabel_One_ReturnsOne()            => Assert.Equal("1", 1.ToLimitLabel());
    [Fact] public void ToLimitLabel_Ten_ReturnsTen()            => Assert.Equal("10", 10.ToLimitLabel());
}
