using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Bouwstaten (migratie 158): bewaren met controle op de code, vaste code na het aanmaken, en verwijderen enkel als niets ernaar verwijst.</summary>
public class BouwstaatServiceTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private static (BouwstaatService svc, string naam) Nieuw()
    {
        var naam = Guid.NewGuid().ToString();
        return (new BouwstaatService(new TestDb(naam)), naam);
    }

    [Fact]
    public async Task Een_nieuwe_bouwstaat_bewaren_en_terugvinden_in_de_lijst()
    {
        var (svc, _) = Nieuw();
        var u = await svc.Bewaar(new BouwstaatBewerk { Code = "opstort_dak", Naam = "Opstort dak", Groep = "Dak", Volgorde = 5, Bereik = 0, PerBouwlaag = true });
        Assert.True(u.Ok, string.Join(" ", u.Fouten));
        var lijst = await svc.Lijst();
        var rij = Assert.Single(lijst);
        Assert.Equal("OPSTORT_DAK", rij.Code);
        Assert.True(rij.PerBouwlaag);
        Assert.Equal(0, rij.GebruiktIn);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1OPSTORT")]
    [InlineData("OP STORT")]
    [InlineData("O")]
    public async Task Een_ongeldige_code_wordt_geweigerd(string code)
    {
        var (svc, _) = Nieuw();
        var u = await svc.Bewaar(new BouwstaatBewerk { Code = code, Naam = "X" });
        Assert.False(u.Ok);
        Assert.NotEmpty(u.Fouten);
    }

    [Fact]
    public async Task Een_dubbele_code_en_een_lege_naam_worden_geweigerd()
    {
        var (svc, _) = Nieuw();
        Assert.True((await svc.Bewaar(new BouwstaatBewerk { Code = "OPSTORT", Naam = "Opstort" })).Ok);
        Assert.False((await svc.Bewaar(new BouwstaatBewerk { Code = "OPSTORT", Naam = "Nog een" })).Ok);
        Assert.False((await svc.Bewaar(new BouwstaatBewerk { Code = "ANDERE", Naam = "  " })).Ok);
    }

    [Fact]
    public async Task De_code_ligt_vast_na_het_aanmaken_maar_de_naam_mag_wijzigen()
    {
        var (svc, _) = Nieuw();
        var id = (await svc.Bewaar(new BouwstaatBewerk { Code = "OPSTORT", Naam = "Opstort" })).Id;
        var u = await svc.Bewaar(new BouwstaatBewerk { Id = id, Code = "IETS_ANDERS", Naam = "Opstort gegoten", Bereik = 1 });
        Assert.True(u.Ok, string.Join(" ", u.Fouten));
        var terug = await svc.Haal(id);
        Assert.Equal("OPSTORT", terug!.Code);
        Assert.Equal("Opstort gegoten", terug.Naam);
        Assert.Equal(1, terug.Bereik);
    }

    [Fact]
    public async Task Een_gebruikte_bouwstaat_kan_enkel_inactief_gezet_worden()
    {
        var naam = Guid.NewGuid().ToString();
        var db = new TestDb(naam);
        var svc = new BouwstaatService(db);
        var id = (await svc.Bewaar(new BouwstaatBewerk { Code = "OPSTORT", Naam = "Opstort" })).Id;
        db.PlanningSectie.Add(new PlanningSectie { Naam = "Ruwbouw", ProjectId = 1, BouwstaatId = id });
        await db.SaveChangesAsync();

        Assert.Equal(1, (await svc.Lijst()).Single().GebruiktIn);
        Assert.NotNull(await svc.Verwijder(id));
        Assert.Null(await svc.ZetActief(id, false));
        Assert.False((await svc.Lijst()).Single().IsActief);
    }

    [Fact]
    public async Task Een_ongebruikte_bouwstaat_kan_verwijderd_worden()
    {
        var (svc, _) = Nieuw();
        var id = (await svc.Bewaar(new BouwstaatBewerk { Code = "OPSTORT", Naam = "Opstort" })).Id;
        Assert.Null(await svc.Verwijder(id));
        Assert.Empty(await svc.Lijst());
    }
}
