using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;
using BOCore.Budget;
using DALCore.Models;
using ServiceCore.Budget;
using Xunit;

namespace ServiceCore.Tests;

public class VerkoopVoorstelMarktTests
{
    private static BudgetVerkoopVoorstelBO Voorstel(params (string naam, string type, decimal opp)[] eenheden)
    {
        var r = new BudgetResultaatBO { TotaalBouw = 1_000_000m };
        var p = new BudgetParams { AankoopprijsGrond = 200_000m, DoelMargePerc = 0.10m, GrondMargePerc = 0.10m };
        var rijen = eenheden.Select(e => new BudgetOppervlaktesBO { EenheidNaam = e.naam, TypeName = e.type, BewoonbareOpp = e.opp, Grondopp = 100m }).ToList();
        return VerkoopVoorstelService.Bereken(1, r, p, rijen);
    }

    private static MarktReferentieUnitBO Unit(string type, decimal opp, decimal ppm2, bool verkocht, int? doorlooptijd = null) => new()
    {
        PropertyType = type, LivingArea = opp, PrijsPerM2 = ppm2, Prijs = ppm2 * opp,
        IsVerkocht = verkocht, VerkochtOp = verkocht ? DateTime.UtcNow.AddDays(-30) : null, DoorlooptijdDagen = doorlooptijd
    };

    private static MarktReferentieBO Markt(params MarktReferentieUnitBO[] units)
    {
        var m = new MarktReferentieBO { GemeenteNaam = "Brugge", PeriodeMaanden = 12, Bron = "test" };
        m.Units.AddRange(units);
        return m;
    }

    [Fact]
    public void Verkochte_units_van_zelfde_type_en_oppervlakte_hebben_voorrang()
    {
        // 5 verkochte appartementen rond 90 m² à 4.000-4.400 €/m², plus duurdere te-koop-units die niet mogen meetellen
        var markt = Markt(
            Unit("Appartement", 85, 4000, true, 60), Unit("Appartement", 88, 4100, true, 90), Unit("Appartement", 90, 4200, true, 120),
            Unit("Appartement", 95, 4300, true, 45), Unit("Appartement", 100, 4400, true, 200),
            Unit("Appartement", 90, 5500, false), Unit("Appartement", 92, 5600, false), Unit("Appartement", 88, 5700, false),
            Unit("Appartement", 91, 5800, false), Unit("Appartement", 89, 5900, false));

        var bo = Voorstel(("App 1", "Appartement", 90m));
        VerkoopVoorstelService.VerrijkMetMarkt(bo, markt);

        var e = bo.Eenheden.Single();
        Assert.Equal("verkocht", e.MarktBasis);
        Assert.Equal("zelfde type, opp. ±20 %", e.MarktVergelijking);
        Assert.Equal(5, e.MarktAantal);
        Assert.Equal(4200m, e.MarktMediaanPerM2);
        Assert.Equal(4200m * 90m, e.MarktPrijs);
        Assert.Equal(4100m, e.MarktP25PerM2);
        Assert.Equal(4300m, e.MarktP75PerM2);

        Assert.Equal(5, markt.AantalTeKoop);
        Assert.Equal(5, markt.VerkochtInPeriode);
        Assert.Equal(0.4m, markt.AbsorptiePerMaand);
        Assert.Equal(90, markt.MediaanDoorlooptijdDagen);
        Assert.Equal(5700m, markt.MediaanPrijsPerM2TeKoop);
    }

    [Fact]
    public void Valt_terug_op_te_koop_en_dan_op_breder_type_als_verkocht_te_weinig_is()
    {
        var markt = Markt(
            Unit("Appartement", 90, 4000, true),
            Unit("Woning", 150, 3000, false), Unit("Woning", 160, 3100, false), Unit("Woning", 170, 3200, false),
            Unit("Woning", 140, 3300, false), Unit("Woning", 155, 3400, false));

        var bo = Voorstel(("Woning A", "Woning", 150m), ("App B", "Appartement", 90m));
        VerkoopVoorstelService.VerrijkMetMarkt(bo, markt);

        var woning = bo.Eenheden.Single(e => e.EenheidNaam == "Woning A");
        Assert.Equal("te koop", woning.MarktBasis);
        Assert.Equal("zelfde type, opp. ±20 %", woning.MarktVergelijking);
        Assert.Equal(3200m, woning.MarktMediaanPerM2);

        // Appartement: 1 verkocht + 0 te koop van zelfde type → te weinig; alle types (6 units) ≥ 5 te koop → "te koop" op "alle types, opp ±20 %"? nee: opp-band rond 90 bevat enkel de ene verkochte → door naar "alle types": 5 te koop
        var app = bo.Eenheden.Single(e => e.EenheidNaam == "App B");
        Assert.Equal("alle types", app.MarktVergelijking);
        Assert.Equal("te koop", app.MarktBasis);
        Assert.Equal(5, app.MarktAantal);
    }

    [Fact]
    public void Te_weinig_data_geeft_geen_marktprijs_maar_wel_waarschuwing()
    {
        var markt = Markt(Unit("Appartement", 90, 4000, true), Unit("Appartement", 95, 4100, false));
        var bo = Voorstel(("App 1", "Appartement", 90m));
        VerkoopVoorstelService.VerrijkMetMarkt(bo, markt);

        Assert.Equal(0, bo.Eenheden.Single().MarktAantal);
        Assert.Null(bo.Eenheden.Single().MarktPrijs);
        Assert.Null(bo.MarktTotaal);
        Assert.Contains(bo.Waarschuwingen, w => w.Contains("Te weinig"));
    }

    [Fact]
    public void Aanbevolen_vraagprijs_is_markt_maar_nooit_onder_minimum()
    {
        var markt = Markt(Enumerable.Range(0, 6).Select(i => Unit("Appartement", 90, 1000 + i, true)).ToArray()); // markt ver onder kost
        var bo = Voorstel(("App 1", "Appartement", 90m));
        VerkoopVoorstelService.VerrijkMetMarkt(bo, markt);

        var e = bo.Eenheden.Single();
        Assert.True(e.MarktPrijs < e.MinimumVerkoopprijs);
        Assert.True(e.MarktVerschilPerc < 0);
        Assert.Equal(e.MinimumVerkoopprijs, e.AanbevolenVraagprijs);
    }

    [Fact]
    public void Zonder_marktdata_blijft_voorstel_intact()
    {
        var bo = Voorstel(("App 1", "Appartement", 90m));
        var minimum = bo.Eenheden.Single().MinimumVerkoopprijs;
        VerkoopVoorstelService.VerrijkMetMarkt(bo, new MarktReferentieBO { Bron = "geen postcode" });

        Assert.Equal(minimum, bo.Eenheden.Single().MinimumVerkoopprijs);
        Assert.Null(bo.MarktTotaal);
        Assert.Contains(bo.Waarschuwingen, w => w.Contains("geen postcode"));
    }

    [Fact]
    public void Eigen_verkopen_tellen_mee_in_verkocht_pool_maar_niet_in_absorptie()
    {
        var markt = Markt(
            Unit("Appartement", 90, 4000, true, 60), Unit("Appartement", 90, 4100, true, 60),
            Unit("Appartement", 90, 4200, true, 60));
        // 3 eigen verkopen (werkelijke prijzen) → samen met 3 marktverkopen ≥ 5 → basis "verkocht"
        foreach (var ppm2 in new[] { 3800m, 3900m, 4300m })
            markt.Units.Add(new MarktReferentieUnitBO { PropertyType = "Appartement", LivingArea = 90, PrijsPerM2 = ppm2, Prijs = ppm2 * 90, IsVerkocht = true, IsEigenVerkoop = true });

        var bo = Voorstel(("App 1", "Appartement", 90m));
        VerkoopVoorstelService.VerrijkMetMarkt(bo, markt);

        var e = bo.Eenheden.Single();
        Assert.Equal("verkocht", e.MarktBasis);
        Assert.Equal(6, e.MarktAantal);
        Assert.Equal(4050m, e.MarktMediaanPerM2);

        Assert.Equal(3, markt.EigenVerkopen);
        Assert.Equal(3900m, markt.MediaanPrijsPerM2EigenVerkoop);
        Assert.Equal(3, markt.VerkochtInPeriode);            // eigen verkopen niet in absorptie
        Assert.Equal(4100m, markt.MediaanPrijsPerM2Verkocht); // enkel marktverkopen
    }

    [Theory]
    [InlineData("Appartement", "Wooneenheden", "Appartement")]
    [InlineData("Penthouse", "Wooneenheden", "Appartement")]
    [InlineData("Woning", "Wooneenheden", "Woning")]
    [InlineData("Halfopen bebouwing", "Woningen", "Woning")]
    [InlineData("Handelsruimte", "Commercieel", null)]
    public void Classificeert_type_op_naam(string type, string groep, string verwacht)
    {
        Assert.Equal(verwacht, VerkoopVoorstelService.ClassificeerType(type, groep));
    }
}
