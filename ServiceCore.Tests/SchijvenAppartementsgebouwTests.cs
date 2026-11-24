using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Het startsjabloon schijven voor het appartementsgebouw (_migrations/158g): bewaart, en geeft voor elke combinatie 100 % met minstens 5 % per schijf en de laatste schijf op 5 %.</summary>
public class SchijvenAppartementsgebouwTests
{
    private class TestDb : DALCore.Models.cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private static SchijfSjabloonSchijfBewerk S(string naam, decimal gewicht = 1, bool perVerd = false, string? var = null, string? waarde = null, decimal? vast = null, int? bs = 1, byte regel = 0)
        => new() { Naam = naam, Gewicht = gewicht, PerVerdieping = perVerd, VoorwaardeVariabele = var, VoorwaardeWaarde = waarde, VastPercentage = vast, BouwstaatId = bs, BouwstaatRegel = regel };

    private static SchijfSjabloonBewerk Sjabloon() => new()
    {
        Naam = "Appartementsgebouw", MinimumPercentage = 5,
        Schijven =
        {
            S("Bij aanvang van de werken", regel: 1),
            S("Gieten vloerplaat kelder", var: "kelder", waarde: "ja"),
            S("Gieten vloerplaat gelijkvloers", var: "kelder", waarde: "ja"),
            S("Gieten vloerplaat gelijkvloers", var: "kelder", waarde: "nee"),
            S("Gieten vloerplaat {verdieping}", perVerd: true, var: "laag", waarde: "boven"),
            S("Plaatsen daktimmerwerken", var: "dak", waarde: "hellend"),
            S("Plaatsen dakbedekking (plat dak)", var: "dak", waarde: "plat"),
            S("Plaatsen dakbedekking (hellend dak)", var: "dak", waarde: "hellend"),
            S("Plaatsen buitenschrijnwerk"),
            S("Plaatsen leidingen en technieken", vast: 5),
            S("Pleisterwerken", vast: 5),
            S("Plaatsen chape", vast: 5),
            S("Plaatsen keuken", vast: 5),
            S("Bij de voorlopige oplevering (ontvangst van de sleutels)", vast: 5),
        },
    };

    [Theory]
    [InlineData(4, true, "plat", 13)]
    [InlineData(3, false, "hellend", 12)]
    [InlineData(5, true, "plat", 14)]
    [InlineData(1, false, "plat", 9)]
    public async Task Het_sjabloon_bewaart_en_geeft_in_elke_combinatie_100_procent_met_minstens_5_procent(int bouwlagen, bool kelder, string dak, int aantal)
    {
        var svc = new SchijfSjabloonService(new TestDb(Guid.NewGuid().ToString()));
        var u = await svc.Bewaar(Sjabloon(), "u");
        Assert.True(u.Ok, string.Join("; ", u.Fouten));
        var voorbeeld = (await svc.Voorbeeld(u.Id, bouwlagen, kelder, dak))!;
        Assert.Equal(aantal, voorbeeld.Schijven.Count);
        Assert.Equal(100m, voorbeeld.Totaal);
        Assert.All(voorbeeld.Schijven, x => Assert.True(x.Percentage >= 5m, x.Naam + " " + x.Percentage));
        Assert.Equal(5m, voorbeeld.Schijven[^1].Percentage);                              // de eindafrekening
        Assert.Contains("voorlopige oplevering", voorbeeld.Schijven[^1].Naam);
        Assert.All(voorbeeld.Schijven, x => Assert.Equal(0m, x.Percentage % 5m));          // stappen van 5 %
        Assert.Empty(voorbeeld.Waarschuwingen);
        // Na het buitenschrijnwerk (gesloten ruwbouw) is altijd minstens 75 % bereikt.
        var tot = 0m;
        foreach (var x in voorbeeld.Schijven) { tot += x.Percentage; if (x.Naam.Contains("buitenschrijnwerk")) break; }
        Assert.True(tot >= 75m, "na het buitenschrijnwerk is " + tot + " % bereikt");
    }
}
