using System.Collections.Generic;
using BOCore;
using BOCore.Budget;
using DALCore.Models;
using ServiceCore.Budget;
using Xunit;

namespace ServiceCore.Tests;

public class VerkoopSamenvattingTests
{
    private static BudgetVerkoopVoorstelBO Voorstel()
    {
        var r = new BudgetResultaatBO { TotaalBouw = 1_000_000m };
        var p = new BudgetParams { AankoopprijsGrond = 200_000m, DoelMargePerc = 0.10m, GrondMargePerc = 0.10m };
        var rijen = new List<BudgetOppervlaktesBO>
        {
            new() { EenheidNaam = "A", BewoonbareOpp = 100m, Grondopp = 100m },
            new() { EenheidNaam = "B", BewoonbareOpp = 100m, Grondopp = 100m }
        };
        return VerkoopVoorstelService.Bereken(1, r, p, rijen);
    }

    [Fact]
    public void Zonder_vastgelegde_vraagprijzen_is_opbrengst_het_voorstel()
    {
        var v = Voorstel();
        var s = VerkoopVoorstelService.BouwSamenvatting(v, new List<BudgetVerkoopLijn>());

        Assert.Equal(1_200_000m, s.TotaalKostprijsInclGrond);
        Assert.Equal(1_320_000m, s.MinimaleVerkoopwaarde);   // 10 % marge op alles
        Assert.Null(s.VraagprijzenLijnen);
        Assert.Equal(v.TotaalAanbevolenVraagprijs, s.Opbrengst);
        Assert.Equal(120_000m, s.Marge);
        Assert.Equal(0.1m, s.MargePerc);
        Assert.Contains("voorstel", s.OpbrengstBron);
    }

    [Fact]
    public void Vastgelegde_vraagprijzen_gaan_voor_op_het_voorstel()
    {
        var v = Voorstel();
        var lijnen = new List<BudgetVerkoopLijn>
        {
            new() { EenheidNaam = "A", Vraagprijs = 700_000m },
            new() { EenheidNaam = "B", Vraagprijs = 650_000m },
            new() { EenheidNaam = "Garage", Vraagprijs = null }   // zonder prijs telt niet mee
        };
        var s = VerkoopVoorstelService.BouwSamenvatting(v, lijnen);

        Assert.Equal(1_350_000m, s.VraagprijzenLijnen);
        Assert.Equal(2, s.AantalLijnenMetVraagprijs);
        Assert.Equal(1_350_000m, s.Opbrengst);
        Assert.Equal(150_000m, s.Marge);
        Assert.Contains("verkooplijnen (2)", s.OpbrengstBron);
    }

    [Fact]
    public void Marge_kan_negatief_zijn()
    {
        var s = VerkoopVoorstelService.BouwSamenvatting(Voorstel(),
            new List<BudgetVerkoopLijn> { new() { EenheidNaam = "A", Vraagprijs = 500_000m } });

        Assert.True(s.Marge < 0);
        Assert.True(s.MargePerc < 0);
    }
}
