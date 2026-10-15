using ServiceCore.Budget;
using Xunit;

public class NotarisTariefTests
{
    // Schaal J, Tariefbesluit (consolidatie 9/2/2026): vast 285, reeksen 7.500/10.000/12.500/15.495/18.595/186.005/249.905/surplus.
    [Fact]
    public void Ereloon_300000_volgt_schaal_J()
        => Assert.Equal(2578.24m, NotarisTarief.Ereloon(300_000m));

    [Fact]
    public void Ereloon_tot_20000_krijgt_vermindering_van_het_vast_gedeelte()
        => Assert.Equal(574.00m, NotarisTarief.Ereloon(15_000m));   // 199 + 7.500 x 2,5 % + 7.500 x 2,5 %

    [Fact]
    public void Btw_komt_erbovenop() => Assert.Equal(3119.67m, NotarisTarief.EerelonInclBtw(300_000m));

    [Fact]
    public void Aankopen_zonder_json_is_een_akte_met_het_totaal()
    {
        var a = NotarisTarief.Aankopen(null, 250_000m);
        Assert.Single(a);
        Assert.Equal(250_000m, a[0].P);
    }

    [Fact]
    public void Aankopen_in_delen_geeft_per_deel_een_akte()
    {
        var a = NotarisTarief.Aankopen("[{\"o\":\"A\",\"p\":100000},{\"o\":\"B\",\"p\":50000},{\"o\":\"leeg\",\"p\":0}]", 0m);
        Assert.Equal(2, a.Count);
    }
}
