using System.Collections.Generic;
using System.Linq;
using BOCore;
using BOCore.Budget;
using DALCore.Models;
using ServiceCore.Budget;
using Xunit;

namespace ServiceCore.Tests;

public class VerkoopVoorstelServiceTests
{
    // Budgetresultaat van € 1.000.000 exclusief grondaankoop:
    //   bouwkost 870.000 + forfaits (infra 50.000, opmeting 10.000) + straight loan grond 5.000 + onvoorzien 65.000
    private static BudgetResultaatBO Resultaat()
    {
        var r = new BudgetResultaatBO { BudgetVersieId = 1, TotaalBouw = 870_000m, Onvoorzien = 65_000m };
        r.Forfaits.Add(new BudgetKostenPostBO { Omschrijving = "Infrastructuur",       Bedrag = 50_000m });
        r.Forfaits.Add(new BudgetKostenPostBO { Omschrijving = "Opmeting + sondering", Bedrag = 10_000m });
        r.Financiering.Add(new BudgetKostenPostBO { Omschrijving = "Straight loan grond (6 mnd)", Bedrag = 5_000m, IsPerc = true });
        return r;
    }

    private static BudgetParams Params(decimal? doel = 0.15m, decimal? grond = 0.10m) => new()
    {
        AankoopprijsGrond        = 400_000m,
        InfrastructuurForfait    = 50_000m,
        OpmetingSonderingForfait = 10_000m,
        DoelMargePerc            = doel,
        GrondMargePerc           = grond
    };

    private static BudgetOppervlaktesBO Rij(string naam, decimal bewoonbaar, decimal grond = 0m, decimal terras = 0m) => new()
    {
        EenheidNaam = naam, BewoonbareOpp = bewoonbaar, Grondopp = grond, TerrasPrefab = terras
    };

    [Fact]
    public void Splitst_kostprijs_in_grond_en_bouw_en_past_marges_toe()
    {
        var bo = VerkoopVoorstelService.Bereken(1, Resultaat(), Params(), new[] { Rij("A", 100m, 300m) });

        Assert.Equal(1_000_000m, Resultaat().TotaalKosten);
        Assert.Equal(465_000m, bo.GrondKost);          // 400.000 + 50.000 + 10.000 + 5.000
        Assert.Equal(935_000m, bo.BouwKost);           // 1.000.000 − 65.000 grondgebonden
        Assert.Equal(1_400_000m, bo.TotaalKostprijs);  // = TotaalKosten + aankoopprijs grond
        Assert.Equal(511_500m, bo.Grondwaarde);        // × 1,10
        Assert.Equal(1_075_250m, bo.Bouwwaarde);       // × 1,15
        Assert.Equal(1_586_750m, bo.TotaalVerkoopwaarde);
        Assert.Equal(186_750m, bo.MargeBedrag);
        Assert.Equal(0.1334m, decimal.Round(bo.MargePerc, 4));
    }

    [Fact]
    public void Verdeelt_bouwwaarde_op_gereduceerde_opp_en_grondwaarde_op_grondopp()
    {
        // A: 100 m² bew. + 20 m² prefab terras (weging 1,0) = 120 gered.; B: 80 m² = 80 gered.
        // Grond: A 300 m², B 100 m².
        var rijen = new[] { Rij("A", 100m, 300m, terras: 20m), Rij("B", 80m, 100m) };
        var bo = VerkoopVoorstelService.Bereken(1, Resultaat(), Params(), rijen);

        Assert.Equal("gereduceerde oppervlakte", bo.VerdeelsleutelBouw);
        Assert.Equal("grondoppervlakte", bo.VerdeelsleutelGrond);

        var a = bo.Eenheden.Single(e => e.EenheidNaam == "A");
        var b = bo.Eenheden.Single(e => e.EenheidNaam == "B");

        Assert.Equal(0.6m, a.AandeelBouw);   // 120 / 200
        Assert.Equal(0.4m, b.AandeelBouw);
        Assert.Equal(0.75m, a.AandeelGrond); // 300 / 400
        Assert.Equal(0.25m, b.AandeelGrond);

        Assert.Equal(645_150m, a.Bouwwaarde);  // 1.075.250 × 0,6
        Assert.Equal(383_625m, a.Grondwaarde); // 511.500 × 0,75
        Assert.Equal(a.Bouwwaarde + a.Grondwaarde, a.MinimumVerkoopprijs);
        Assert.Equal(decimal.Round(a.MinimumVerkoopprijs / 100m, 2), decimal.Round(a.PrijsPerM2Bewoonbaar!.Value, 2));

        // Sommen sluiten op de totalen (afronding per eenheid op 2 decimalen)
        Assert.InRange(bo.Eenheden.Sum(e => e.Bouwwaarde) - bo.Bouwwaarde, -0.05m, 0.05m);
        Assert.InRange(bo.Eenheden.Sum(e => e.Grondwaarde) - bo.Grondwaarde, -0.05m, 0.05m);
        Assert.InRange(bo.Eenheden.Sum(e => e.Kostprijs) - bo.TotaalKostprijs, -0.05m, 0.05m);
    }

    [Fact]
    public void Zonder_grondopp_valt_grondverdeling_terug_op_bouwsleutel_met_waarschuwing()
    {
        var rijen = new[] { Rij("A", 100m), Rij("B", 100m) };
        var bo = VerkoopVoorstelService.Bereken(1, Resultaat(), Params(), rijen);

        Assert.Equal(bo.VerdeelsleutelBouw, bo.VerdeelsleutelGrond);
        Assert.All(bo.Eenheden, e => Assert.Equal(0.5m, e.AandeelGrond));
        Assert.Contains(bo.Waarschuwingen, w => w.Contains("grondoppervlakte"));
    }

    [Fact]
    public void Zonder_marges_is_verkoopwaarde_gelijk_aan_kostprijs()
    {
        var bo = VerkoopVoorstelService.Bereken(1, Resultaat(), Params(doel: null, grond: null), new[] { Rij("A", 100m) });

        Assert.Equal(bo.TotaalKostprijs, bo.TotaalVerkoopwaarde);
        Assert.Equal(0m, bo.MargeBedrag);
        Assert.Contains(bo.Waarschuwingen, w => w.Contains("doelmarge"));
    }

    [Fact]
    public void Zonder_eenheden_geen_verdeling_maar_wel_totalen()
    {
        var bo = VerkoopVoorstelService.Bereken(1, Resultaat(), Params(), new List<BudgetOppervlaktesBO>());

        Assert.False(bo.HeeftEenheden);
        Assert.Equal(1_586_750m, bo.TotaalVerkoopwaarde);
        Assert.Contains(bo.Waarschuwingen, w => w.Contains("Oppervlaktes"));
    }
}
