using CPMCore.Services;
using CPMCore.Services.MijnTaken;
using CPMCore.Services.OutlookSync;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ServiceCore.Tests;

/// <summary>Afspraken uit Outlook aan projecten koppelen (spec #2, ticket #7): voorstel via titel en deelnemers, koppelrecord, label op Vandaag.</summary>
public class OutlookAfspraakTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name);
    }

    private static readonly DateTime Nu = new(2026, 10, 28, 9, 0, 0);
    private static readonly int[] Zichtbaar = { 1, 2 };
    private static readonly Dictionary<int, string> Namen = new() { [1] = "Residentie De Linde", [2] = "Villa Cauxyde" };

    /// <summary>Maakt de assistent zoals de applicatie hem bouwt: met de afspraakservice erbij.</summary>
    private static (MijnTakenAssistent Assistent, OutlookAfspraakService Afspraken, TestDb Db, FakeGraphIngang Graph) Bouw()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Users.Add(new Users { Id = 1, UserId = "u1", Voornaam = "An", Familienaam = "A", Email = "an@groupln.be", EntraObjectId = "entra-an", IsActive = true });
        db.Users.Add(new Users { Id = 2, UserId = "u2", Voornaam = "Bo", Familienaam = "B", Email = "bo@groupln.be", IsActive = true });
        db.Project.Add(new Project { ProjectId = 1, ProjectName = "Residentie De Linde", ProjectType = 1, StatusId = 2, Street = "Keerstraat", Number = "12" });
        db.Project.Add(new Project { ProjectId = 2, ProjectName = "Villa Cauxyde", ProjectType = 1, StatusId = 2 });
        db.Project.Add(new Project { ProjectId = 3, ProjectName = "Oud Project", ProjectType = 1, StatusId = 1 });
        db.ClientAccount.Add(new ClientAccount { Id = 10, Name = "Familie Peeters", Email = "klant@example.com" });
        db.Units.Add(new Units { ProjectId = 1, ClientAccountId = 10, Name = "Woning 1" });
        db.ClientAccount.Add(new ClientAccount { Id = 11, Name = "Familie Janssens", Email = "meer@example.com" });
        db.Units.Add(new Units { ProjectId = 1, ClientAccountId = 11, Name = "Woning 2" });
        db.Units.Add(new Units { ProjectId = 2, ClientAccountId = 11, Name = "Villa" });
        db.SaveChanges();
        var graph = new FakeGraphIngang();
        var mail = new OutlookMailSyncService(db, graph, NullLogger<OutlookMailSyncService>.Instance);
        var afspraken = new OutlookAfspraakService(db, mail);
        return (new MijnTakenAssistent(db, null!, graph, afspraken), afspraken, db, graph);
    }

    private static AgendaAfspraak Afspraak(string id, string titel, params string[] deelnemers) => new()
    {
        Id = id, Titel = titel, Start = Nu.AddHours(1), Einde = Nu.AddHours(2), Deelnemers = deelnemers.ToList()
    };

    private static MailContext Context()
    {
        var ctx = new MailContext();
        ctx.Projecten.Add(new MailProject(1, "Residentie De Linde", null, "Keerstraat", "12", true, "Vlekkem"));
        ctx.Projecten.Add(new MailProject(2, "Villa Cauxyde", null, "Dorpsstraat", "5", true));
        ctx.Projecten.Add(new MailProject(3, "Oud Project", null, null, null, false));
        ctx.Contacten["klant@example.com"] = new() { new BekendeContact("klant", 10, null, new[] { 1 }) };
        ctx.Contacten["meer@example.com"] = new() { new BekendeContact("klant", 11, null, new[] { 1, 2 }) };
        ctx.Contacten["oud@example.com"] = new() { new BekendeContact("klant", 12, null, new[] { 3 }) };
        return ctx;
    }

    // ── Voorstel ─────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Werfvergadering Residentie De Linde", 1)]
    [InlineData("Plaatsbezoek keerstraat 12", 1)]
    [InlineData("Overleg Villa Cauxyde", 2)]
    public void Een_projectverwijzing_in_de_titel_geeft_een_voorstel(string titel, int verwacht)
        => Assert.Equal(verwacht, OutlookAfspraakService.Voorstel(Afspraak("e", titel), Context(), "an@groupln.be", Zichtbaar));

    [Fact]
    public void Een_deelnemer_die_een_bekende_contactpersoon_is_geeft_het_enige_actieve_project()
        => Assert.Equal(1, OutlookAfspraakService.Voorstel(Afspraak("e", "Kennismaking", "an@groupln.be", "klant@example.com"), Context(), "an@groupln.be", Zichtbaar));

    [Fact]
    public void Een_contact_met_meerdere_actieve_projecten_geeft_geen_voorstel_want_cpm_gokt_niet()
        => Assert.Null(OutlookAfspraakService.Voorstel(Afspraak("e", "Overleg", "meer@example.com"), Context(), "an@groupln.be", Zichtbaar));

    [Fact]
    public void Twee_projecten_in_de_titel_geven_geen_voorstel()
        => Assert.Null(OutlookAfspraakService.Voorstel(Afspraak("e", "Villa Cauxyde en Keerstraat 12"), Context(), "an@groupln.be", Zichtbaar));

    [Fact]
    public void De_titel_wint_van_de_deelnemers()
        => Assert.Equal(2, OutlookAfspraakService.Voorstel(Afspraak("e", "Villa Cauxyde", "klant@example.com"), Context(), "an@groupln.be", Zichtbaar));

    [Fact]
    public void Een_opgeleverd_project_van_een_contact_telt_niet_als_actief_project()
        => Assert.Null(OutlookAfspraakService.Voorstel(Afspraak("e", "Overleg", "oud@example.com"), Context(), "an@groupln.be", Zichtbaar));

    [Fact]
    public void Een_project_dat_je_niet_ziet_wordt_nooit_voorgesteld()
        => Assert.Null(OutlookAfspraakService.Voorstel(Afspraak("e", "Overleg Villa Cauxyde"), Context(), "an@groupln.be", new[] { 1 }));

    [Fact]
    public void Het_eigen_adres_en_onbekende_deelnemers_geven_geen_voorstel()
        => Assert.Null(OutlookAfspraakService.Voorstel(Afspraak("e", "Lunch", "an@groupln.be", "iemand@random.be"), Context(), "an@groupln.be", Zichtbaar));

    // ── Koppelrecord ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Een_koppeling_blijft_onthouden_en_komt_terug_als_label()
    {
        var (_, afspraken, db, _) = Bouw();

        Assert.Null(await afspraken.KoppelAsync(1, "evt-1", 2, Zichtbaar));

        var a = Afspraak("evt-1", "Lunch");
        await afspraken.VerrijkAsync(1, "an@groupln.be", Namen, new[] { a });
        Assert.Equal((2, "Villa Cauxyde", null), (a.ProjectId, a.ProjectNaam, a.VoorstelProjectId));
        Assert.Single(db.OutlookAfspraak);
    }

    [Fact]
    public async Task Herkoppelen_wijzigt_de_bestaande_rij()
    {
        var (_, afspraken, db, _) = Bouw();
        await afspraken.KoppelAsync(1, "evt-1", 1, Zichtbaar);
        await afspraken.KoppelAsync(1, "evt-1", 2, Zichtbaar);

        Assert.Equal(2, db.OutlookAfspraak.Single().ProjectId);
    }

    [Fact]
    public async Task Ontkoppelen_haalt_het_label_weg_en_laat_de_afspraak_ongemoeid()
    {
        var (_, afspraken, db, _) = Bouw();
        await afspraken.KoppelAsync(1, "evt-1", 1, Zichtbaar);

        await afspraken.OntkoppelAsync(1, "evt-1");

        var a = Afspraak("evt-1", "Lunch");
        await afspraken.VerrijkAsync(1, "an@groupln.be", Namen, new[] { a });
        Assert.Null(a.ProjectId);
        Assert.Empty(db.OutlookAfspraak);
    }

    [Fact]
    public async Task Een_koppeling_is_van_de_gebruiker_zelf_en_een_project_buiten_bereik_wordt_geweigerd()
    {
        var (_, afspraken, db, _) = Bouw();
        await afspraken.KoppelAsync(1, "evt-1", 1, Zichtbaar);

        var van2 = Afspraak("evt-1", "Lunch");
        await afspraken.VerrijkAsync(2, "bo@groupln.be", Namen, new[] { van2 });
        Assert.Null(van2.ProjectId);

        Assert.NotNull(await afspraken.KoppelAsync(1, "evt-2", 99, Zichtbaar));
        Assert.Single(db.OutlookAfspraak);
    }

    [Fact]
    public async Task Het_koppelrecord_bevat_geen_agenda_inhoud()
    {
        var props = typeof(OutlookAfspraak).GetProperties().Select(p => p.Name).Where(n => n is not ("User" or "Project")).OrderBy(n => n).ToArray();
        await Task.CompletedTask;
        Assert.Equal(new[] { "EventId", "GekoppeldOp", "Id", "ProjectId", "UserId" }, props);
    }

    [Fact]
    public async Task Een_hele_dag_afspraak_krijgt_geen_voorstel()
    {
        var (_, afspraken, _, _) = Bouw();
        var a = Afspraak("evt-1", "Residentie De Linde bouwverlof"); a.HeleDag = true;

        await afspraken.VerrijkAsync(1, "an@groupln.be", Namen, new[] { a });

        Assert.Null(a.VoorstelProjectId);
    }

    // ── Vandaag ──────────────────────────────────────────────────────────────────────────────

    private static MtContext Ctx() => new()
    {
        UserId = 1, UserStr = "1", UserCode = "u1", Nu = Nu, AgendaGebruiker = "entra-an", Email = "an@groupln.be",
        Projecten = { [1] = new() { Id = 1, Naam = "Residentie De Linde" }, [2] = new() { Id = 2, Naam = "Villa Cauxyde" } }, ProjectIds = { 1, 2 }
    };

    [Fact]
    public async Task Vandaag_toont_het_projectlabel_op_een_gekoppelde_afspraak_en_een_voorstel_op_een_andere()
    {
        var (assistent, afspraken, _, graph) = Bouw();
        graph.Agenda["entra-an"] = new()
        {
            Afspraak("gekoppeld", "Lunch"),
            Afspraak("voorstel", "Kennismaking", "klant@example.com"),
            Afspraak("niets", "Tandarts")
        };
        await afspraken.KoppelAsync(1, "gekoppeld", 2, Zichtbaar);

        var vm = await assistent.Vandaag(Ctx());

        var per = vm.Afspraken.ToDictionary(a => a.Id);
        Assert.Equal("Villa Cauxyde", per["gekoppeld"].ProjectNaam);
        Assert.Equal((null, "Residentie De Linde"), (per["voorstel"].ProjectNaam, per["voorstel"].VoorstelProjectNaam));
        Assert.Null(per["niets"].VoorstelProjectId);
    }

    [Fact]
    public async Task Een_voorstel_wordt_nooit_stil_een_koppeling()
    {
        var (assistent, _, db, graph) = Bouw();
        graph.Agenda["entra-an"] = new() { Afspraak("voorstel", "Kennismaking", "klant@example.com") };

        await assistent.Vandaag(Ctx());
        await assistent.Vandaag(Ctx());

        Assert.Empty(db.OutlookAfspraak);
    }
}
