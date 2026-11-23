using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Schijvensjablonen: vaste percentages eerst, de rest naar gewicht, herhaling per verdieping en de waarschuwing onder het minimum.</summary>
public class SchijfSjabloonTests
{
    private static SchijfInvoer S(string naam, decimal? vast = null, decimal gewicht = 1m, bool perVerdieping = false, string? code = null)
        => new(naam, code, vast, gewicht, perVerdieping);

    [Theory]
    [InlineData(true, new[] { "Opstort gelijkvloers", "Opstort 1ste verdieping", "Opstort 2de verdieping" }, new[] { 1, 2, 3 })]
    [InlineData(false, new[] { "Gieten vloerplaat gelijkvloers", "Opstort 1ste verdieping", "Opstort 2de verdieping" }, new int[] { 1, 1, 2 })]
    public void Een_schijf_op_de_opstort_van_elke_bouwlaag_en_zonder_kelder_op_de_vloerplaat_gelijkvloers(bool kelder, string[] namen, int[] volgnrs)
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new SchijfInvoer[]
        {
            new("Opstort gelijkvloers", "OPSTORT", null, 1, false, "kelder", "ja"),
            new("Gieten vloerplaat gelijkvloers", "VLOERPLAAT_GLVL", null, 1, false, "kelder", "nee"),
            new("Opstort {verdieping}", "OPSTORT", null, 1, true, "laag", "boven"),
        }, 3, kelder);
        Assert.Equal(namen, uit.Schijven.Select(x => x.Naam));
        // Het volgnummer is de plaats van de code in de planning: met kelder staat de opstort ook op het gelijkvloers, zonder kelder niet.
        Assert.Equal(volgnrs, uit.Schijven.Select(x => x.PlanningVolgnr ?? 1));
        Assert.Equal(100m, uit.Totaal);
        // Bij één bouwlaag is er geen schijf boven het gelijkvloers.
        var een = SchijfSjabloonUitrol.Rol(5m, new SchijfInvoer[] { new("Opstort {verdieping}", "OPSTORT", null, 1, true, "laag", "boven"), new("Eind", null, null, 1, false) }, 1);
        Assert.DoesNotContain(een.Schijven, x => x.Naam.StartsWith("Opstort"));
    }

    [Fact]
    public void De_laatste_schijf_is_vast_5_procent_en_de_rest_gaat_naar_gewicht()
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new[]
        {
            S("Uitzetten", gewicht: 1), S("Vloerplaat", gewicht: 4), S("Ruwbouw", gewicht: 5), S("Voorlopige oplevering", vast: 5),
        }, 1);
        // 95 % naar gewicht 1 : 4 : 5 in stappen van 5 %: 10 / 40 / 45
        Assert.Equal(new[] { 10m, 40m, 45m, 5m }, uit.Schijven.Select(x => x.Percentage).ToArray());
        Assert.Equal(100m, uit.Totaal);
        Assert.Empty(uit.Waarschuwingen);
        Assert.True(uit.Schijven[3].IsVast);
        Assert.False(uit.Schijven[0].IsVast);
    }

    [Fact]
    public void De_afrondingsrest_komt_op_de_laatste_schijf_met_een_gewicht_en_het_totaal_blijft_100()
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new[] { S("A"), S("B"), S("C"), S("Oplevering", vast: 5) }, 1);
        Assert.Equal(100m, uit.Totaal);
        // 95 % in stappen van 5 % over drie gelijke schijven: 19 stappen = 7 + 6 + 6 (de grootste afrondingsrest eerst)
        Assert.Equal(35m, uit.Schijven[0].Percentage);
        Assert.Equal(30m, uit.Schijven[1].Percentage);
        Assert.Equal(30m, uit.Schijven[2].Percentage);
        Assert.Equal(5m, uit.Schijven[3].Percentage);
    }

    [Fact]
    public void Een_schijf_per_verdieping_wordt_gelijk_verdeeld_met_de_rest_op_de_laatste_verdieping()
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new[]
        {
            S("Bij het gieten van de vloerplaat {verdieping}", vast: 20, perVerdieping: true, code: "VLOERPLAAT"),
            S("Rest", gewicht: 1), S("Oplevering", vast: 5),
        }, 3);
        var vloer = uit.Schijven.Where(x => x.PlanningCode == "VLOERPLAAT").ToList();
        Assert.Equal(3, vloer.Count);
        Assert.Equal(new[] { 6.67m, 6.67m, 6.66m }, vloer.Select(x => x.Percentage).ToArray());
        Assert.Equal("Bij het gieten van de vloerplaat gelijkvloers", vloer[0].Naam);
        Assert.Equal("Bij het gieten van de vloerplaat 1ste verdieping", vloer[1].Naam);
        Assert.Equal(new int?[] { 1, 2, 3 }, vloer.Select(x => x.PlanningVolgnr).ToArray());
        Assert.Equal(100m, uit.Totaal);
    }

    [Fact]
    public void Een_gewicht_per_verdieping_wordt_over_de_verdiepingen_verdeeld_en_zonder_plaatshouder_komt_de_verdieping_erachter()
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new[] { S("Afdekken", gewicht: 2, perVerdieping: true), S("Rest", gewicht: 2), S("Oplevering", vast: 5) }, 2);
        Assert.Equal("Afdekken gelijkvloers", uit.Schijven[0].Naam);
        Assert.Equal("Afdekken 1ste verdieping", uit.Schijven[1].Naam);
        // gewichten 1 + 1 + 2 over 95 % in stappen van 5 %: 25 / 25 / 45
        Assert.Equal(new[] { 25m, 25m, 45m, 5m }, uit.Schijven.Select(x => x.Percentage).ToArray());
    }

    [Fact]
    public void Bij_een_verdieping_verdwijnt_de_plaatshouder_uit_de_naam()
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new[] { S("Gieten vloerplaat {verdieping}", gewicht: 1, perVerdieping: true), S("Oplevering", vast: 5) }, 1);
        Assert.Equal("Gieten vloerplaat", uit.Schijven[0].Naam);
        Assert.Equal(95m, uit.Schijven[0].Percentage);
    }

    [Fact]
    public void Een_kleine_schijf_krijgt_toch_minstens_5_procent_en_alle_percentages_zijn_veelvouden_van_5()
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new[] { S("Klein", gewicht: 1), S("Groot", gewicht: 30), S("Oplevering", vast: 5) }, 1);
        Assert.Equal(new[] { 5m, 90m, 5m }, uit.Schijven.Select(x => x.Percentage).ToArray());
        Assert.Equal(100m, uit.Totaal);
        Assert.Empty(uit.Waarschuwingen);
    }

    [Fact]
    public void Onder_het_minimum_geeft_een_waarschuwing_geen_blokkade()
    {
        // 20 schijven met een gewicht passen niet in 95 % met stappen van 5 %: één schijf krijgt niets en dat wordt gemeld.
        var invoer = Enumerable.Range(1, 20).Select(i => S("Schijf " + i, gewicht: 1)).Append(S("Oplevering", vast: 5)).ToArray();
        var uit = SchijfSjabloonUitrol.Rol(5m, invoer, 1);
        Assert.Equal(100m, uit.Totaal);
        Assert.Contains(uit.Waarschuwingen, w => w.Contains("minimum"));
    }

    [Fact]
    public void Vaste_percentages_zonder_gewicht_die_geen_100_zijn_geven_een_waarschuwing_over_het_totaal()
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new[] { S("A", vast: 60), S("B", vast: 30) }, 1);
        Assert.Equal(90m, uit.Totaal);
        Assert.Contains(uit.Waarschuwingen, w => w.Contains("100 %"));
    }

    [Theory]
    [InlineData(1, "1ste")]
    [InlineData(2, "2de")]
    [InlineData(3, "3de")]
    [InlineData(8, "8ste")]
    [InlineData(9, "9de")]
    public void Rangtelwoorden_in_het_nederlands(int n, string verwacht) => Assert.Equal(verwacht, SchijfSjabloonUitrol.Rangtelwoord(n));

    // ── service ──

    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private static SchijfSjabloonBewerk Voorbeeld() => new()
    {
        Naam = "Woning",
        Schijven =
        {
            new SchijfSjabloonSchijfBewerk { Naam = "Ruwbouw", Gewicht = 3 },
            new SchijfSjabloonSchijfBewerk { Naam = "Voorlopige oplevering (ontvangst van de sleutels)", VastPercentage = 5 },
        },
    };

    [Fact]
    public async Task Een_sjabloon_bewaren_ophalen_en_uitrekenen()
    {
        var svc = new SchijfSjabloonService(new TestDb(Guid.NewGuid().ToString()));
        var u = await svc.Bewaar(Voorbeeld(), "u1");
        Assert.True(u.Ok, string.Join(" ", u.Fouten));
        var terug = await svc.Haal(u.Id);
        Assert.Equal(2, terug!.Schijven.Count);
        Assert.Equal("Voorlopige oplevering (ontvangst van de sleutels)", terug.Schijven[1].Naam);
        var v = await svc.Voorbeeld(u.Id, 1);
        Assert.Equal(new[] { 95m, 5m }, v!.Schijven.Select(x => x.Percentage).ToArray());
        var keuzes = await svc.Keuzes();
        Assert.Single(keuzes);
    }

    [Fact]
    public async Task Ongeldige_sjablonen_worden_geweigerd()
    {
        var svc = new SchijfSjabloonService(new TestDb(Guid.NewGuid().ToString()));
        var leeg = await svc.Bewaar(new SchijfSjabloonBewerk { Naam = "" }, "u1");
        Assert.False(leeg.Ok);
        Assert.Contains(leeg.Fouten, f => f.Contains("naam"));
        Assert.Contains(leeg.Fouten, f => f.Contains("minstens één schijf"));

        var teVeelVast = Voorbeeld();
        teVeelVast.Schijven[1].VastPercentage = 100;
        Assert.Contains((await svc.Bewaar(teVeelVast, "u1")).Fouten, f => f.Contains("minstens 100"));

        var enkelVast = Voorbeeld();
        enkelVast.Schijven[0].VastPercentage = 50;     // 50 + 5 = 55, en geen enkele schijf met gewicht
        Assert.Contains((await svc.Bewaar(enkelVast, "u1")).Fouten, f => f.Contains("samen 100"));

        Assert.True((await svc.Bewaar(Voorbeeld(), "u1")).Ok);
        Assert.Contains((await svc.Bewaar(Voorbeeld(), "u1")).Fouten, f => f.Contains("bestaat al"));   // dubbele naam
    }

    [Fact]
    public async Task Dupliceren_inactief_zetten_en_verwijderen()
    {
        var svc = new SchijfSjabloonService(new TestDb(Guid.NewGuid().ToString()));
        var id = (await svc.Bewaar(Voorbeeld(), "u1")).Id;
        var (kopie, fout) = await svc.Dupliceer(id, "u1");
        Assert.Null(fout);
        Assert.Equal("Woning (kopie)", (await svc.Haal(kopie!.Value))!.Naam);
        Assert.Single(await svc.Keuzes());                        // de kopie is inactief tot je haar activeert
        Assert.Null(await svc.ZetActief(kopie.Value, true, "u1"));
        Assert.Equal(2, (await svc.Keuzes()).Count);
        Assert.Null(await svc.Verwijder(kopie.Value));
        Assert.Null(await svc.Haal(kopie.Value));
    }
}
