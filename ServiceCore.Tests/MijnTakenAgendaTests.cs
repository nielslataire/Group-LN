using CPMCore.Services;
using CPMCore.Services.MijnTaken;
using CPMCore.Services.OutlookSync;
using Microsoft.Extensions.Logging.Abstractions;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ServiceCore.Tests;

public class MijnTakenAgendaTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name);
    }

    private static readonly DateTime Nu = new(2026, 10, 28, 9, 0, 0); // woensdag

    private static (MijnTakenAssistent Svc, MtContext Ctx, FakeGraphIngang Graph) Create(string? gebruiker = "entra-1")
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        var graph = new FakeGraphIngang();
        var afspraken = new OutlookAfspraakService(db, new OutlookMailSyncService(db, graph, NullLogger<OutlookMailSyncService>.Instance));
        var svc = new MijnTakenAssistent(db, null!, graph, afspraken);
        var ctx = new MtContext { UserId = 1, UserStr = "1", UserCode = "u1", Nu = Nu, AgendaGebruiker = gebruiker };
        return (svc, ctx, graph);
    }

    [Fact]
    public async Task Vandaag_toont_afspraken_van_vandaag_uit_de_graph_ingang()
    {
        var (svc, ctx, graph) = Create();
        graph.Agenda["entra-1"] = new()
        {
            new AgendaAfspraak { Id = "a", Titel = "Werfoverleg", Start = Nu.AddHours(1), Einde = Nu.AddHours(2) },
            new AgendaAfspraak { Id = "b", Titel = "Verlof", Start = Nu.Date, Einde = Nu.Date.AddDays(1), HeleDag = true },
            new AgendaAfspraak { Id = "c", Titel = "Morgen", Start = Nu.AddDays(1), Einde = Nu.AddDays(1).AddHours(1) }
        };

        var vm = await svc.Vandaag(ctx);

        Assert.Null(vm.AgendaFout);
        Assert.Equal(new[] { "Werfoverleg" }, vm.Afspraken.Select(a => a.Titel));
        Assert.Equal(new[] { "Verlof" }, vm.HeleDag.Select(a => a.Titel));
        Assert.Equal("entra-1", graph.AgendaAanvragen.Single().Gebruiker);
    }

    [Fact]
    public async Task Vandaag_geeft_de_fout_van_de_graph_ingang_door()
    {
        var (svc, ctx, graph) = Create();
        graph.Fout = "De app mag je agenda niet lezen.";

        var vm = await svc.Vandaag(ctx);

        Assert.Equal("De app mag je agenda niet lezen.", vm.AgendaFout);
        Assert.Empty(vm.Afspraken);
    }

    [Fact]
    public async Task Agenda_wijzigen_gaat_via_de_graph_ingang()
    {
        var (svc, ctx, graph) = Create();

        var fout = await svc.AgendaWijzig(ctx, "evt-1", "Nieuw", Nu, 45);

        Assert.Null(fout);
        var w = Assert.Single(graph.Wijzigingen);
        Assert.Equal(("entra-1", "evt-1", "Nieuw"), (w.Gebruiker, w.EventId, w.Titel));
        Assert.Equal(Nu.AddMinutes(45), w.Einde);
    }

    [Theory]
    [InlineData("obj-id", "a@b.be", "obj-id")]
    [InlineData(null, "a@b.be", "a@b.be")]
    [InlineData("  ", "a@b.be", "a@b.be")]
    [InlineData(null, null, null)]
    public void Gebruiker_wordt_gevonden_via_entra_object_id_anders_via_mailadres(string? entra, string? mail, string? verwacht)
        => Assert.Equal(verwacht, IGraphIngang.GebruikerSleutel(entra, mail));
}
