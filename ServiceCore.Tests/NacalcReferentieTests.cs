using System.Collections.Generic;
using BOCore.Budget;
using ServiceCore.Budget;
using Xunit;

namespace ServiceCore.Tests;

public class NacalcReferentieTests
{
    private static BudgetReferentieProjectBO Ref(int id, int eenheden, decimal? gba, decimal? s, decimal? i, params (int act, decimal bedrag)[] lijnen)
    {
        var r = new BudgetReferentieProjectBO { Id = id, Naam = "P" + id, AantalEenheden = eenheden, OppervlakteGBA = gba, SIndex = s, IIndex = i };
        foreach (var (act, bedrag) in lijnen) r.Lijnen.Add(new BudgetReferentieLijnBO { ActivityId = act, Bedrag = bedrag });
        return r;
    }

    [Fact]
    public void Zonder_indexen_is_de_factor_1_en_deelt_door_de_som_van_de_eenheden()
    {
        var refs = new List<BudgetReferentieProjectBO>
        {
            Ref(1, 10, null, null, null, (100, 500_000m)),   // 50.000 / eenheid
            Ref(2, 20, null, null, null, (100, 800_000m))    // 40.000 / eenheid
        };
        var r = BudgetReferentieProjectService.Bereken(refs, 120m, 130m);

        Assert.Equal(2, r[100].AantalProjecten);
        Assert.Equal(1_300_000m / 30m, r[100].PrijsPerEenheid, 2);   // gewogen: 43.333,33
        Assert.Equal(40_000m, r[100].MinPerEenheid);
        Assert.Equal(50_000m, r[100].MaxPerEenheid);
        Assert.Null(r[100].PrijsPerM2);
    }

    [Fact]
    public void Indexeert_met_de_gewogen_formule_naar_de_huidige_index()
    {
        // S 100 → 110 (+10 %), I 100 → 120 (+20 %): factor = 1,2 × 0,4 + 1,1 × 0,4 + 0,2 = 1,12
        var refs = new List<BudgetReferentieProjectBO> { Ref(1, 10, 1_000m, 100m, 100m, (100, 100_000m)) };
        var r = BudgetReferentieProjectService.Bereken(refs, 110m, 120m);

        Assert.Equal(1.12m, BudgetReferentieProjectService.Factor(refs[0], 110m, 120m));
        Assert.Equal(11_200m, r[100].PrijsPerEenheid);
        Assert.Equal(112m, r[100].PrijsPerM2);
    }

    [Fact]
    public void Per_m2_telt_enkel_projecten_met_oppervlakte_en_activiteit_zonder_bedrag_ontbreekt()
    {
        var refs = new List<BudgetReferentieProjectBO>
        {
            Ref(1, 10, 1_000m, null, null, (100, 100_000m), (200, 50_000m)),
            Ref(2, 10, null,   null, null, (100, 300_000m))
        };
        var r = BudgetReferentieProjectService.Bereken(refs, 120m, 130m);

        Assert.Equal(20_000m, r[100].PrijsPerEenheid);   // 400.000 / 20
        Assert.Equal(100m, r[100].PrijsPerM2);           // enkel project 1: 100.000 / 1.000
        Assert.Equal(1, r[200].AantalProjecten);
        Assert.False(r.ContainsKey(300));
    }

    [Fact]
    public void Project_zonder_eenheden_wordt_overgeslagen()
    {
        var refs = new List<BudgetReferentieProjectBO> { Ref(1, 0, null, null, null, (100, 100_000m)) };
        Assert.Empty(BudgetReferentieProjectService.Bereken(refs, 120m, 130m));
    }

    [Theory]
    [InlineData("  Ruwbouw  ", "ruwbouw")]
    [InlineData("Elektriciteit – algemeen", "elektriciteit algemeen")]
    [InlineData("", "")]
    public void Normaliseer_maakt_activiteitsnamen_vergelijkbaar(string invoer, string verwacht)
        => Assert.Equal(verwacht, BudgetReferentieProjectService.Normaliseer(invoer));
}
