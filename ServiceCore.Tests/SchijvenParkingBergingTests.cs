using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Het schijvensjabloon "Parking of berging" (_migrations/160b): 30 % aanvang, 40 % vloerplaat, 30 % voorlopige oplevering, met en zonder kelder.</summary>
public class SchijvenParkingBergingTests
{
    private class TestDb : DALCore.Models.cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private static SchijfSjabloonSchijfBewerk S(string naam, decimal? vast = null, string? var = null, string? waarde = null, byte regel = 0)
        => new() { Naam = naam, Gewicht = 1, VastPercentage = vast, VoorwaardeVariabele = var, VoorwaardeWaarde = waarde, BouwstaatId = 1, BouwstaatRegel = regel };

    [Theory]
    [InlineData(true, "plat")]
    [InlineData(false, "plat")]
    [InlineData(true, "hellend")]
    public async Task Elke_combinatie_geeft_30_40_30(bool kelder, string dak)
    {
        var svc = new SchijfSjabloonService(new TestDb(Guid.NewGuid().ToString()));
        var u = await svc.Bewaar(new SchijfSjabloonBewerk
        {
            Naam = "Parking of berging", MinimumPercentage = 5,
            Schijven =
            {
                S("Bij aanvang van de werken", vast: 30, regel: 1),
                S("Bij het gieten van de vloerplaat kelder", var: "kelder", waarde: "ja"),
                S("Bij het gieten van de vloerplaat gelijkvloers", var: "kelder", waarde: "nee"),
                S("Bij de voorlopige oplevering (ontvangst van de sleutels)", vast: 30),
            },
        }, "u");
        Assert.True(u.Ok, string.Join("; ", u.Fouten));
        var v = (await svc.Voorbeeld(u.Id, 4, kelder, dak))!;
        Assert.Equal(new[] { 30m, 40m, 30m }, v.Schijven.Select(x => x.Percentage).ToArray());
        Assert.Equal(100m, v.Totaal);
        Assert.Empty(v.Waarschuwingen);
    }
}
