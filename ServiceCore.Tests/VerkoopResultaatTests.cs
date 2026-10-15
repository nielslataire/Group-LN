using System;
using System.Collections.Generic;
using BOCore;
using BOCore.Budget;
using DALCore.Models;
using ServiceCore.Budget;
using Xunit;

public class VerkoopResultaatTests
{
    private static VerkoopResultaat Maak()
    {
        var sam = new BudgetVerkoopSamenvattingBO { VraagprijzenLijnen = 2_888_214m, AantalLijnenMetVraagprijs = 8, AantalEenheden = 8 };
        var voorstel = new BudgetVerkoopVoorstelBO { Grondwaarde = 820_000m, Bouwwaarde = 2_068_214m };
        var lijnen = new List<BudgetVerkoopLijn> { new() { Vraagprijs = 2_888_214m, Grondwaarde = 820_000m, Bouwwaarde = 2_068_214m } };
        return VerkoopResultaat.Bereken(sam, voorstel, lijnen, 0.03m, 0m, 2_468_509m);
    }

    [Fact]
    public void Commissie_op_grond_telt_btw_mee_op_bouw_niet()
    {
        var r = Maak();
        Assert.Equal(29_766m, r.GrondKostInclBtw);        // 3 % x 820.000 = 24.600 + 21 % btw
        Assert.Equal(62_046.42m, r.CommissieBouw);        // btw erop is aftrekbaar en telt niet mee
        Assert.Equal(13_029.75m, r.BtwBouwAftrekbaar);
        Assert.Equal(91_812.42m, r.Verkoopkosten);
    }

    [Fact]
    public void Resultaat_en_marges()
    {
        var r = Maak();
        Assert.Equal(327_892.58m, r.Resultaat);
        Assert.Equal(0.133m, System.Math.Round(r.MargeOpKostprijs, 3));
        Assert.Equal(0.114m, System.Math.Round(r.MargeOpVerkoop, 3));
        Assert.Equal(0.170m, System.Math.Round(r.MargeVoorKosten, 3));
    }

    [Fact]
    public void BreakEven_geeft_resultaat_nul()
    {
        var r = Maak();
        Assert.InRange(r.BreakEven, 2_549_000m, 2_550_500m);
        Assert.InRange(r.RuimteTovBreakEven, -0.118m, -0.116m);
    }
}

public class NettoMargeTests
{
    [Fact]
    public void Zonder_verkoopkosten_is_waarde_kost_maal_marge()
    {
        var (g, b) = VerkoopVoorstelService.Waarden(100_000m, 300_000m, 0.10m, 0.15m, 0m, 0m);
        Assert.Equal(110_000m, g);
        Assert.Equal(345_000m, b);
    }

    [Fact]
    public void Marge_is_netto_na_makelaarscommissie_en_overige_kosten()
    {
        decimal grondKost = 100_000m, bouwKost = 300_000m, gm = 0.10m, dm = 0.15m, c = 0.03m, overige = 8_000m;
        var (g, b) = VerkoopVoorstelService.Waarden(grondKost, bouwKost, gm, dm, c, overige);

        // Verkoopkosten op die waarden: commissie op grond incl. 21 % btw, op bouw zonder btw, plus overige
        var kosten = g * c * 1.21m + b * c + overige;
        var netto = g + b - kosten - (grondKost + bouwKost);
        Assert.Equal(grondKost * gm + bouwKost * dm, Math.Round(netto, 0));   // gevraagde marge = effectieve netto marge
    }
}

public class GrondTweedeZoneTests
{
    [Fact]
    public void Rekenregel_telt_tweede_grondzone_met_eigen_prijs_mee_in_de_vraagprijs()
    {
        var l = new BudgetVerkoopLijn { PrijsBron = 3, GrondPrijsPerM2 = 230m, BouwPrijsPerM2 = 2400m, GrondAgrPrijsPerM2 = 40m };
        VerkoopLijnRekenregel.Herbereken(l, oppGereduceerd: 200m, grondopp: 600m, grondoppAgrarisch: 300m);

        Assert.Equal(138_000m, l.Grondwaarde);          // 600 m² × 230
        Assert.Equal(12_000m, l.GrondwaardeAgrarisch);  // 300 m² × 40
        Assert.Equal(480_000m, l.Bouwwaarde);
        Assert.Equal(630_000m, l.Vraagprijs);
    }

    [Fact]
    public void Zonder_tweede_oppervlakte_verdwijnt_de_tweede_zone()
    {
        var l = new BudgetVerkoopLijn { PrijsBron = 3, GrondPrijsPerM2 = 230m, BouwPrijsPerM2 = 2400m, GrondAgrPrijsPerM2 = 40m, GrondwaardeAgrarisch = 12_000m };
        VerkoopLijnRekenregel.Herbereken(l, 200m, 600m, 0m);
        Assert.Null(l.GrondwaardeAgrarisch);
        Assert.Equal(618_000m, l.Vraagprijs);
    }

    [Fact]
    public void Grondkost_wordt_verdeeld_naar_gewogen_oppervlakte()
    {
        var r = new BudgetResultaatBO { TotaalBouw = 1_000_000m };
        r.Grond.Add(new BudgetKostenPostBO { Omschrijving = "Aankoopprijs grond", Bedrag = 200_000m });
        var p = new BudgetParams { AankoopprijsGrond = 200_000m, GrondAgrarischPerc = 0.25m };
        var rijen = new List<BudgetOppervlaktesBO>
        {
            new() { EenheidNaam = "A", TypeName = "Woning", BewoonbareOpp = 150m, Grondopp = 400m, GrondoppAgrarisch = 0m },
            new() { EenheidNaam = "B", TypeName = "Woning", BewoonbareOpp = 150m, Grondopp = 400m, GrondoppAgrarisch = 400m },
        };
        var bo = VerkoopVoorstelService.Bereken(1, r, p, rijen);

        // gewicht A = 400, gewicht B = 400 + 400 × 0,25 = 500  →  A krijgt 4/9 van de grondkost, B 5/9
        Assert.Equal(4m / 9m, bo.Eenheden[0].AandeelGrond, 6);
        Assert.True(bo.Eenheden[1].KostprijsGrond > bo.Eenheden[0].KostprijsGrond);
    }
}
