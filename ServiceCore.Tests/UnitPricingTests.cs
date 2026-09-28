using ServiceCore.Helpers;
using Xunit;

namespace ServiceCore.Tests;

/// <summary>De rekenregel moet gelijk blijven aan de publieke site (WWWCOPRO Projects/Detail.vbhtml):
/// zonder afwerkingen alle constructieprijzen samen; met afwerkingen elke afwerking als volledig alternatief.</summary>
public class UnitPricingTests
{
    [Fact]
    public void NoOptions_SumsAllConstructionValues()
    {
        var r = UnitPricing.Compute(new (int?, decimal)[] { (null, 400_000m), (null, 215_000m) });
        Assert.False(r.HasOptions);
        Assert.Equal(615_000m, r.From);
        Assert.Equal(615_000m, r.To);
    }

    [Fact]
    public void Empty_IsZero()
    {
        var r = UnitPricing.Compute(new (int?, decimal)[0]);
        Assert.Equal(0m, r.From);
        Assert.Equal(0m, r.To);
        Assert.False(r.HasOptions);
    }

    [Fact]
    public void Options_AreCompleteAlternatives_NotAddedTogether()
    {
        // Afgewerkt = 400k + 215k, Casco = 460k: vanaf = casco, duurste = afgewerkt (niet 1.075k).
        var r = UnitPricing.Compute(new (int?, decimal)[] { (1, 400_000m), (1, 215_000m), (2, 460_000m) });
        Assert.True(r.HasOptions);
        Assert.Equal(460_000m, r.From);
        Assert.Equal(615_000m, r.To);
    }

    [Fact]
    public void SingleOption_FromEqualsTo()
    {
        var r = UnitPricing.Compute(new (int?, decimal)[] { (7, 100m), (7, 50m) });
        Assert.True(r.HasOptions);
        Assert.Equal(150m, r.From);
        Assert.Equal(150m, r.To);
    }

    [Fact]
    public void RowsOutsideOptions_AreIgnored_WhenOptionsExist()
    {
        // Zelfde als de publieke site; in de praktijk komt deze mix niet voor.
        var r = UnitPricing.Compute(new (int?, decimal)[] { (null, 999m), (1, 100m), (2, 300m) });
        Assert.Equal(100m, r.From);
        Assert.Equal(300m, r.To);
    }

    [Fact]
    public void Standard_IsTheDefaultOption_NotTheCheapest()
    {
        var values = new (int?, decimal)[] { (1, 400_000m), (1, 215_000m), (2, 460_000m) };
        var r = UnitPricing.Compute(values, defaultOptionId: 1);
        Assert.Equal(615_000m, r.Standard);
        Assert.Equal(460_000m, r.From);
    }

    [Fact]
    public void Standard_FallsBackToCheapest_WhenNoDefaultKnown()
    {
        var values = new (int?, decimal)[] { (1, 615_000m), (2, 460_000m) };
        Assert.Equal(460_000m, UnitPricing.Compute(values).Standard);
        Assert.Equal(460_000m, UnitPricing.Compute(values, defaultOptionId: 99).Standard);
    }

    [Fact]
    public void Standard_WithoutOptions_IsTheTotal()
    {
        Assert.Equal(615_000m, UnitPricing.Compute(new (int?, decimal)[] { (null, 400_000m), (null, 215_000m) }, 5).Standard);
    }
}
