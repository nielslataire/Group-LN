using CPMCore.Services;
using CPMCore.Services.OutlookSync;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ServiceCore.Tests;

public class OutlookSyncServiceTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name);
    }

    private static readonly DateTime Nu = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);

    private static (OutlookSyncService Svc, TestDb Db, FakeGraphIngang Graph) Create()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Users.AddRange(
            new Users { Id = 1, UserId = "u1", Voornaam = "An", Familienaam = "A", Email = "an@groupln.be", EntraObjectId = "entra-an", IsActive = true },
            new Users { Id = 2, UserId = "u2", Voornaam = "Bo", Familienaam = "B", Email = "bo@groupln.be", IsActive = true },
            new Users { Id = 3, UserId = "u3", Voornaam = "Cy", Familienaam = "C", Email = "cy@groupln.be", IsActive = false });
        db.SaveChanges();
        var graph = new FakeGraphIngang();
        return (new OutlookSyncService(db, graph, NullLogger<OutlookSyncService>.Instance), db, graph);
    }

    [Fact]
    public async Task Standaard_staat_de_koppeling_uit_en_wordt_niet_gesynchroniseerd()
    {
        var (svc, db, graph) = Create();

        var verwerkt = await svc.SynchroniseerAlleAsync(Nu);

        Assert.Equal(0, verwerkt);
        Assert.Empty(graph.ToegangAanvragen);
        Assert.False((await svc.WeergaveVoorAsync(1, Nu)).Aan);
        Assert.Empty(db.OutlookSyncStatus);
    }

    [Fact]
    public async Task Aanzetten_legt_het_moment_vast_en_wacht_op_de_eerste_sync()
    {
        var (svc, _, _) = Create();

        var rij = await svc.ZetAanAsync(1, Nu);

        Assert.True(rij.Aan);
        Assert.Equal(Nu, rij.AangezetOp);
        var w = await svc.WeergaveVoorAsync(1, Nu);
        Assert.True(w.Aan);
        Assert.Equal("wacht", w.Code);
    }

    [Fact]
    public async Task Sync_zoekt_de_gebruiker_via_entra_id_anders_via_mailadres()
    {
        var (svc, _, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        await svc.ZetAanAsync(2, Nu);

        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(5));

        Assert.Equal(new[] { "bo@groupln.be", "entra-an" }, graph.ToegangAanvragen.OrderBy(x => x));
    }

    [Fact]
    public async Task Geslaagde_sync_zet_status_ok_met_laatste_synctijdstip()
    {
        var (svc, _, _) = Create();
        await svc.ZetAanAsync(1, Nu);

        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(5));

        var w = await svc.WeergaveVoorAsync(1, Nu.AddMinutes(6));
        Assert.Equal("ok", w.Code);
        Assert.NotNull(w.LaatsteSyncLokaal);
    }

    [Fact]
    public async Task Gebruiker_buiten_de_groep_krijgt_een_duidelijke_melding()
    {
        var (svc, _, graph) = Create();
        graph.Toegang["entra-an"] = GraphToegang.GeenToegang;
        await svc.ZetAanAsync(1, Nu);

        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(5));

        var w = await svc.WeergaveVoorAsync(1, Nu.AddMinutes(5));
        Assert.Equal("geen-toegang", w.Code);
        Assert.Contains("toegestane groep", w.Melding);
        Assert.Equal("fout", w.Niveau);
    }

    [Fact]
    public async Task Een_fout_stopt_de_koppeling_niet_en_de_volgende_ronde_herstelt_de_status()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.Toegang["entra-an"] = GraphToegang.Fout;

        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(5));
        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(10));
        var rij = db.OutlookSyncStatus.Single();
        Assert.Equal(("fout", 2, true), (rij.Status, rij.FoutenOpRij, rij.Aan));

        graph.Toegang["entra-an"] = GraphToegang.Ok;
        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(15));

        rij = db.OutlookSyncStatus.Single();
        Assert.Equal(("ok", 0, null), (rij.Status, rij.FoutenOpRij, rij.LaatsteFout));
    }

    [Fact]
    public async Task Een_uitzondering_voor_een_gebruiker_houdt_de_andere_gebruikers_niet_tegen()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        await svc.ZetAanAsync(2, Nu);
        graph.ToegangGooit = true;

        var verwerkt = await svc.SynchroniseerAlleAsync(Nu.AddMinutes(5));

        Assert.Equal(2, verwerkt);
        Assert.All(db.OutlookSyncStatus, r => Assert.Equal("fout", r.Status));
    }

    [Fact]
    public async Task Uitval_van_de_sync_is_zichtbaar_als_de_laatste_sync_te_oud_is()
    {
        var (svc, _, _) = Create();
        await svc.ZetAanAsync(1, Nu);
        await svc.SynchroniseerAlleAsync(Nu);

        var w = await svc.WeergaveVoorAsync(1, Nu.AddHours(2));

        Assert.Equal("stil", w.Code);
        Assert.Equal("fout", w.Niveau);
    }

    [Fact]
    public async Task Uitzetten_stopt_de_sync_en_opnieuw_aanzetten_start_een_nieuwe_koppeling()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(5));
        await svc.ZetUitAsync(1, Nu.AddMinutes(6));
        graph.ToegangAanvragen.Clear();

        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(10));
        Assert.Empty(graph.ToegangAanvragen);
        Assert.False((await svc.WeergaveVoorAsync(1, Nu.AddMinutes(10))).Aan);

        var later = Nu.AddDays(1);
        await svc.ZetAanAsync(1, later);
        Assert.Equal(later, db.OutlookSyncStatus.Single().AangezetOp);
    }

    [Fact]
    public async Task Inactieve_gebruikers_worden_niet_gesynchroniseerd()
    {
        var (svc, _, graph) = Create();
        await svc.ZetAanAsync(3, Nu);

        await svc.SynchroniseerAlleAsync(Nu.AddMinutes(5));

        Assert.Empty(graph.ToegangAanvragen);
    }
}
