using BOCore;
using DALCore.Models;
using ServiceCore.Budget;
using Xunit;

namespace ServiceCore.Tests;

public class VerkoopLijnRekenregelTests
{
    private static BudgetVerkoopLijn Lijn(VerkoopPrijsBron? bron) => new() { EenheidNaam = "A", PrijsBron = (byte?)bron };

    [Fact]
    public void Referentiecode_rekent_bedragen_uit_prijs_per_m2()
    {
        var l = Lijn(VerkoopPrijsBron.Referentiecode);
        l.BouwPrijsPerM2 = 2_500m; l.GrondPrijsPerM2 = 400m; l.ExtraForfait = 5_000m;

        VerkoopLijnRekenregel.Herbereken(l, oppGereduceerd: 120m, grondopp: 300m);

        Assert.Equal(300_000m, l.Bouwwaarde);
        Assert.Equal(120_000m, l.Grondwaarde);
        Assert.Equal(425_000m, l.Vraagprijs);
    }

    [Fact]
    public void Manueel_bedrag_leidt_prijs_per_m2_af_en_houdt_eigen_vraagprijs()
    {
        var l = Lijn(VerkoopPrijsBron.ManueelBedrag);
        l.Bouwwaarde = 330_000m; l.Grondwaarde = 90_000m; l.Vraagprijs = 429_000m;

        VerkoopLijnRekenregel.Herbereken(l, 120m, 300m);

        Assert.Equal(2_750m, l.BouwPrijsPerM2);
        Assert.Equal(300m, l.GrondPrijsPerM2);
        Assert.Equal(429_000m, l.Vraagprijs); // eigen vraagprijs blijft staan
    }

    [Fact]
    public void Voorstel_vult_vraagprijs_als_som_en_leidt_per_m2_af()
    {
        var l = Lijn(VerkoopPrijsBron.Voorstel);
        l.Bouwwaarde = 300_000m; l.Grondwaarde = 120_000m;

        VerkoopLijnRekenregel.Herbereken(l, 100m, 0m);

        Assert.Equal(420_000m, l.Vraagprijs);
        Assert.Equal(3_000m, l.BouwPrijsPerM2);
        Assert.Null(l.GrondPrijsPerM2); // geen grondoppervlakte → niets af te leiden
    }

    [Fact]
    public void Ruil_zet_grondwaarde_op_nul_en_telt_forfait_als_compensatie()
    {
        var l = Lijn(VerkoopPrijsBron.ManueelPerM2);
        l.BouwPrijsPerM2 = 2_000m; l.GrondPrijsPerM2 = 500m; l.IsRuil = true; l.ExtraForfait = 25_000m;

        VerkoopLijnRekenregel.Herbereken(l, 100m, 200m);

        Assert.Equal(0m, l.Grondwaarde);
        Assert.Null(l.GrondPrijsPerM2);
        Assert.Equal(225_000m, l.Vraagprijs);
    }

    [Fact]
    public void Zonder_oppervlakte_blijft_bedrag_bij_per_m2_bron_ongemoeid()
    {
        var l = Lijn(VerkoopPrijsBron.ManueelPerM2);
        l.BouwPrijsPerM2 = 2_000m; l.Bouwwaarde = 123_456m;

        VerkoopLijnRekenregel.Herbereken(l, 0m, 0m);

        Assert.Equal(123_456m, l.Bouwwaarde);
        Assert.Equal(123_456m, l.Vraagprijs);
    }

    [Fact]
    public void Lege_lijn_zonder_bron_blijft_leeg()
    {
        var l = Lijn(null);
        VerkoopLijnRekenregel.Herbereken(l, 100m, 100m);
        Assert.Null(l.Vraagprijs);
        Assert.Null(l.BouwPrijsPerM2);
    }

    [Theory]
    [InlineData(400_000, 420_000, true)]
    [InlineData(420_000, 420_000, false)]
    [InlineData(0, 420_000, false)]
    [InlineData(400_000, 0, false)]
    public void OnderMinimum(decimal vraagprijs, decimal minimum, bool verwacht)
        => Assert.Equal(verwacht, VerkoopLijnRekenregel.OnderMinimum(vraagprijs == 0 ? null : vraagprijs, minimum));
}
