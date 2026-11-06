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
        var afspraken = new OutlookAfspraakService(db, mail, graph);
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

    // ── Afspraak aanmaken in CPM (ticket #8) ─────────────────────────────────────────────────

    private static Task<(string? EventId, string? Fout)> Maak(OutlookAfspraakService afspraken, int? project, string? titel, DateTime start, int minuten, string? plaats = null,
        bool online = false, string? deelnemers = null, int herinnering = 15, string? notitie = null) =>
        afspraken.MaakAsync(1, "entra-an", project, Zichtbaar, titel, start, minuten, plaats, online, deelnemers, herinnering, notitie);

    [Fact]
    public async Task Een_afspraak_vanuit_cpm_komt_in_outlook_en_is_aan_het_project_gekoppeld()
    {
        var (assistent, afspraken, db, graph) = Bouw();

        var (eventId, fout) = await Maak(afspraken, 2, " Werfoverleg ", Nu.AddHours(3), 60, "Op de werf");

        Assert.Null(fout);
        Assert.Equal("Werfoverleg", graph.Aangemaakt.Single().Afspraak.Titel);
        Assert.Equal("entra-an", graph.Aangemaakt.Single().Gebruiker);
        Assert.Equal(2, db.OutlookAfspraak.Single(k => k.EventId == eventId).ProjectId);

        var vm = await assistent.Vandaag(Ctx());
        var a = vm.Afspraken.Single(x => x.Id == eventId);
        Assert.Equal("Villa Cauxyde", a.ProjectNaam);
        Assert.Equal(Nu.AddHours(4), a.Einde);
    }

    [Fact]
    public async Task Een_afspraak_zonder_project_is_persoonlijk_en_wordt_niet_gekoppeld()
    {
        var (_, afspraken, db, graph) = Bouw();

        var (eventId, fout) = await Maak(afspraken, null, "Tandarts", Nu.AddHours(3), 30);

        Assert.Null(fout);
        Assert.NotNull(eventId);
        Assert.Single(graph.Aangemaakt);
        Assert.Empty(db.OutlookAfspraak);
    }

    [Fact]
    public async Task Online_herinnering_en_notitie_gaan_mee_naar_outlook()
    {
        var (_, afspraken, _, graph) = Bouw();

        await Maak(afspraken, 1, "Overleg", Nu.AddHours(3), 30, online: true, herinnering: 60, notitie: "  Toegangscode 1234  ");
        await Maak(afspraken, 1, "Zonder herinnering", Nu.AddHours(5), 30, herinnering: 0);

        var eerste = graph.Aangemaakt[0].Afspraak;
        Assert.Equal((true, 60, "Toegangscode 1234"), (eerste.Online, eerste.HerinneringMinuten, eerste.Notitie));
        Assert.Null(graph.Aangemaakt[1].Afspraak.HerinneringMinuten);
    }

    [Fact]
    public async Task Met_reistijd_komt_er_een_apart_bezet_blok_vlak_voor_de_afspraak_dat_niet_gekoppeld_wordt()
    {
        var (_, afspraken, db, graph) = Bouw();

        var (eventId, fout) = await afspraken.MaakAsync(1, "entra-an", 2, Zichtbaar, "Werfbezoek", Nu.AddHours(3), 60, "Keerstraat 12", false, null, 15, null, 28);

        Assert.Null(fout);
        Assert.Equal(2, graph.Aangemaakt.Count);
        var blok = graph.Aangemaakt[1].Afspraak;
        Assert.Equal(("Reistijd: Werfbezoek", Nu.AddHours(3).AddMinutes(-28), Nu.AddHours(3), true), (blok.Titel, blok.Start, blok.Einde, blok.Bezet));
        Assert.Equal(eventId, db.OutlookAfspraak.Single().EventId);   // enkel de afspraak is aan het project gekoppeld
    }

    [Fact]
    public async Task Als_je_de_afspraak_verplaatst_gaat_het_blok_reistijd_mee_met_dezelfde_lengte_en_de_nieuwe_titel()
    {
        var (assistent, afspraken, _, graph) = Bouw();
        var (eventId, _) = await afspraken.MaakAsync(1, "entra-an", 2, Zichtbaar, "Werfbezoek", Nu.AddHours(3), 60, "Keerstraat 12", false, null, 15, null, 30);
        var blok = graph.Agenda["entra-an"].Single(a => a.Titel == "Reistijd: Werfbezoek");

        Assert.Null(await assistent.AgendaWijzig(Ctx(), eventId!, "Werfbezoek Villa", Nu.AddHours(5), 90));

        Assert.Equal(("Reistijd: Werfbezoek Villa", Nu.AddHours(4).AddMinutes(30), Nu.AddHours(5)), (blok.Titel, blok.Start, blok.Einde));
        var afspraak = graph.Agenda["entra-an"].Single(a => a.Id == eventId);
        Assert.Equal((Nu.AddHours(5), Nu.AddHours(5).AddMinutes(90)), (afspraak.Start, afspraak.Einde));
    }

    [Fact]
    public async Task Een_afspraak_zonder_blok_reistijd_verplaatst_alleen_zichzelf_ook_als_er_een_ander_blok_op_dezelfde_titel_lijkt()
    {
        var (assistent, afspraken, _, graph) = Bouw();
        var (eventId, _) = await Maak(afspraken, 2, "Overleg", Nu.AddHours(3), 60);
        graph.Agenda["entra-an"].Add(new AgendaAfspraak { Id = "los", Titel = "Reistijd: Overleg", Start = Nu.AddHours(6), Einde = Nu.AddHours(7) });

        Assert.Null(await assistent.AgendaWijzig(Ctx(), eventId!, null, Nu.AddHours(4), 60));

        var los = graph.Agenda["entra-an"].Single(a => a.Id == "los");
        Assert.Equal((Nu.AddHours(6), Nu.AddHours(7)), (los.Start, los.Einde));
    }

    [Fact]
    public async Task Zonder_reistijd_komt_er_geen_extra_blok()
    {
        var (_, afspraken, _, graph) = Bouw();

        await Maak(afspraken, 1, "Overleg", Nu.AddHours(3), 30);

        Assert.Single(graph.Aangemaakt);
    }

    [Theory]
    [InlineData("{\"routes\":[{\"duration\":\"1234s\"}]}", 25)]   // 20,6 min → 25
    [InlineData("{\"routes\":[{\"duration\":\"480s\"}]}", 10)]
    [InlineData("{\"routes\":[{\"duration\":\"0s\"}]}", 0)]
    [InlineData("{}", null)]
    [InlineData("{\"routes\":[]}", null)]
    public void De_reistijd_uit_google_wordt_naar_boven_afgerond_op_vijf_minuten(string json, int? verwacht)
        => Assert.Equal(verwacht, GoogleRoutesReisTijd.Parse(json));

    [Fact]
    public async Task Zonder_deelnemers_gaat_er_geen_uitnodiging_uit()
    {
        var (_, afspraken, _, graph) = Bouw();

        await Maak(afspraken, 1, "Overleg", Nu.AddHours(3), 30);

        Assert.Empty(graph.Aangemaakt.Single().Afspraak.Uitnodigen);
    }

    [Fact]
    public async Task De_gekozen_deelnemers_krijgen_een_uitnodiging_zonder_dubbels()
    {
        var (_, afspraken, _, graph) = Bouw();

        var (_, fout) = await Maak(afspraken, 1, "Overleg", Nu.AddHours(3), 30, deelnemers: "klant@example.com; meer@example.com KLANT@example.com");

        Assert.Null(fout);
        Assert.Equal(new[] { "klant@example.com", "meer@example.com" }, graph.Aangemaakt.Single().Afspraak.Uitnodigen);
    }

    [Fact]
    public async Task Een_ongeldig_adres_wordt_geweigerd_en_maakt_niets_aan()
    {
        var (_, afspraken, _, graph) = Bouw();
        var (eventId, fout) = await Maak(afspraken, 1, "Overleg", Nu.AddHours(3), 30, deelnemers: "geen-adres");

        Assert.NotNull(fout);
        Assert.Null(eventId);
        Assert.Empty(graph.Aangemaakt);
    }

    [Fact]
    public async Task Een_project_buiten_bereik_een_lege_titel_of_een_ongeldige_duur_maken_niets_aan()
    {
        var (_, afspraken, _, graph) = Bouw();

        Assert.NotNull((await Maak(afspraken, 99, "Overleg", Nu, 30)).Fout);
        Assert.NotNull((await Maak(afspraken, 1, "  ", Nu, 30)).Fout);
        Assert.NotNull((await Maak(afspraken, 1, "Overleg", Nu, 2)).Fout);

        Assert.Empty(graph.Aangemaakt);
    }

    [Fact]
    public async Task Als_outlook_weigert_blijft_er_geen_koppeling_achter()
    {
        var (_, afspraken, db, graph) = Bouw();
        graph.AanmaakFout = "De app mag geen afspraken in je agenda maken.";

        var (eventId, fout) = await Maak(afspraken, 1, "Overleg", Nu.AddHours(3), 30);

        Assert.Equal("De app mag geen afspraken in je agenda maken.", fout);
        Assert.Null(eventId);
        Assert.Empty(db.OutlookAfspraak);
    }

    // ── Bezetting van een dag (overlapmelding) en plaatsen ───────────────────────────────────

    [Fact]
    public async Task De_bezetting_van_een_dag_toont_enkel_afspraken_met_een_uur_die_niet_vrij_zijn()
    {
        var (_, afspraken, _, graph) = Bouw();
        var dag = Nu.Date;
        graph.Agenda["entra-an"] = new()
        {
            new AgendaAfspraak { Id = "a", Titel = "Werfbezoek", Start = dag.AddHours(8).AddMinutes(30), Einde = dag.AddHours(10) },
            new AgendaAfspraak { Id = "b", Titel = "Verlof", Start = dag, Einde = dag.AddDays(1), HeleDag = true },
            new AgendaAfspraak { Id = "c", Titel = "Optioneel", Start = dag.AddHours(14), Einde = dag.AddHours(15), ToonAls = "free" }
        };

        var bezet = await afspraken.BezetOpAsync("entra-an", dag);

        var blok = Assert.Single(bezet);
        Assert.Equal(("Werfbezoek", 510, 600), (blok.Titel, blok.Van, blok.Tot));
    }

    private sealed class FakeAdressen : IAdresVoorstel
    {
        public bool IsConfigured => true;
        public List<string> Termen { get; } = new();
        public Task<List<AdresSuggestie>> ZoekAsync(string term, CancellationToken ct = default)
        {
            Termen.Add(term);
            return Task.FromResult(new List<AdresSuggestie> { new("Kerkstraat 8", "9000 Gent, België"), new("Keerstraat 12", "9031 Drongen, België") });
        }
    }

    [Fact]
    public async Task Nieuwe_adressen_komen_onder_de_bekende_met_het_label_nieuw_adres_en_zonder_dubbels()
    {
        var (_, _, db, graph) = Bouw();
        db.Project.Single(p => p.ProjectId == 1).PostalCodeId = 1;
        db.PostalCode.Add(new PostalCode { PostcodeId = 1, Postcode = "9031", Gemeente = "Drongen" });
        db.SaveChanges();
        var fake = new FakeAdressen();
        var afspraken = new OutlookAfspraakService(db, new OutlookMailSyncService(db, graph, NullLogger<OutlookMailSyncService>.Instance), graph, fake);

        var lijst = await afspraken.ZoekPlaatsenAsync("entra-an", "keer", Zichtbaar);

        Assert.Equal(new[] { ("Residentie De Linde", "Project"), ("Kerkstraat 8", "Nieuw adres") }, lijst.Select(k => (k.Naam, k.Soort)));   // Keerstraat 12 Drongen kent CPM al
        Assert.Equal(new[] { "keer" }, fake.Termen);
    }

    [Fact]
    public async Task Zonder_zoekterm_vraagt_cpm_geen_nieuwe_adressen_op()
    {
        var (_, _, db, graph) = Bouw();
        var fake = new FakeAdressen();
        var afspraken = new OutlookAfspraakService(db, new OutlookMailSyncService(db, graph, NullLogger<OutlookMailSyncService>.Instance), graph, fake);

        await afspraken.ZoekPlaatsenAsync("entra-an", "", Zichtbaar);

        Assert.Empty(fake.Termen);
    }

    [Fact]
    public void Google_places_voorstellen_worden_gesplitst_in_straat_en_rest()
    {
        var lijst = GooglePlacesAdresVoorstel.Parse("{\"suggestions\":[{\"placePrediction\":{\"text\":{\"text\":\"Kerkstraat 8, 9000 Gent, België\"},\"structuredFormat\":{\"mainText\":{\"text\":\"Kerkstraat 8\"},\"secondaryText\":{\"text\":\"9000 Gent, België\"}}}},{\"placePrediction\":{\"text\":{\"text\":\"Station Gent\"}}},{\"queryPrediction\":{}}]}");

        Assert.Equal(new[] { new AdresSuggestie("Kerkstraat 8", "9000 Gent, België"), new AdresSuggestie("Station Gent", "") }, lijst);
        Assert.Empty(GooglePlacesAdresVoorstel.Parse("{}"));
    }

    [Fact]
    public async Task Recente_plaatsen_komen_uit_de_afspraken_van_de_voorbije_maand_zonder_online_of_dubbels()
    {
        var (_, afspraken, _, graph) = Bouw();
        var dag = DateTime.Today;
        graph.Agenda["entra-an"] = new()
        {
            new AgendaAfspraak { Id = "a", Titel = "A", Start = dag.AddDays(-2).AddHours(9), Einde = dag.AddDays(-2).AddHours(10), Plaats = "Notaris Van Hulle" },
            new AgendaAfspraak { Id = "b", Titel = "B", Start = dag.AddDays(-5).AddHours(9), Einde = dag.AddDays(-5).AddHours(10), Plaats = "notaris van hulle" },
            new AgendaAfspraak { Id = "c", Titel = "C", Start = dag.AddDays(-3).AddHours(9), Einde = dag.AddDays(-3).AddHours(10), Plaats = "Teams", Online = true }
        };

        var lijst = await afspraken.ZoekPlaatsenAsync("entra-an", "", Zichtbaar);

        var recent = Assert.Single(lijst);
        Assert.Equal(("Notaris Van Hulle", "Recent"), (recent.Naam, recent.Soort));
    }

    // ── Deelnemers zoeken ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Zonder_zoekterm_staan_alleen_de_collega_s_in_de_lijst_en_jezelf_niet()
    {
        var (_, afspraken, _, _) = Bouw();

        var lijst = await afspraken.ZoekDeelnemersAsync(1, null);

        Assert.Equal(new[] { "bo@groupln.be" }, lijst.Select(d => d.Email));
    }

    [Fact]
    public async Task Collega_s_staan_altijd_boven_de_contactpersonen()
    {
        var (_, afspraken, db, _) = Bouw();
        db.Users.Add(new Users { Id = 3, UserId = "u3", Voornaam = "Cas", Familienaam = "C", Email = "cas@example.com", IsActive = true });
        db.SaveChanges();

        var lijst = await afspraken.ZoekDeelnemersAsync(1, "example");

        Assert.Equal(new[] { "cas@example.com", "meer@example.com", "klant@example.com" }, lijst.Select(d => d.Email));
    }

    [Fact]
    public async Task Met_een_project_staan_de_mensen_van_dat_project_onder_de_collega_s_en_boven_de_andere_contacten()
    {
        var (_, afspraken, _, _) = Bouw();

        var zonderTerm = await afspraken.ZoekDeelnemersAsync(1, null, 2, Zichtbaar);
        var metTerm = await afspraken.ZoekDeelnemersAsync(1, "example", 2, Zichtbaar);

        Assert.Equal(new[] { ("bo@groupln.be", OutlookAfspraakService.GroepCollegas), ("meer@example.com", OutlookAfspraakService.GroepProject) }, zonderTerm.Select(d => (d.Email, d.Groep)));
        Assert.Equal(new[] { "meer@example.com", "klant@example.com" }, metTerm.Select(d => d.Email));
        Assert.Equal(OutlookAfspraakService.GroepContacten, metTerm.Last().Groep);
    }

    [Fact]
    public async Task Een_project_buiten_bereik_geeft_geen_projectgroep()
    {
        var (_, afspraken, _, _) = Bouw();

        var lijst = await afspraken.ZoekDeelnemersAsync(1, null, 3, Zichtbaar);

        Assert.Equal(new[] { "bo@groupln.be" }, lijst.Select(d => d.Email));
    }

    [Fact]
    public async Task Een_getypt_adres_wordt_een_extern_adres_en_een_bekend_adres_komt_maar_een_keer()
    {
        var (_, afspraken, _, _) = Bouw();

        var nieuw = await afspraken.ZoekDeelnemersAsync(1, "iemand@buiten.be");
        var bekend = await afspraken.ZoekDeelnemersAsync(1, "klant@example.com");

        Assert.Equal("iemand@buiten.be", nieuw.Last().Email);
        Assert.Contains("Extern", nieuw.Last().Sub);
        Assert.Single(bekend);
    }

    [Fact]
    public async Task Inactieve_gebruikers_en_gebruikers_zonder_adres_staan_niet_in_de_lijst()
    {
        var (_, afspraken, db, _) = Bouw();
        db.Users.Add(new Users { Id = 3, UserId = "u3", Voornaam = "Weg", Familienaam = "W", Email = "weg@groupln.be", IsActive = false });
        db.Users.Add(new Users { Id = 4, UserId = "u4", Voornaam = "Zonder", Familienaam = "Z", IsActive = true });
        db.SaveChanges();

        Assert.Equal(new[] { "bo@groupln.be" }, (await afspraken.ZoekDeelnemersAsync(1, "")).Select(d => d.Email));
    }

    [Fact]
    public async Task Wijzigingen_in_cpm_en_in_outlook_komen_door_en_de_koppeling_blijft()
    {
        var (assistent, afspraken, db, graph) = Bouw();
        var (eventId, _) = await Maak(afspraken, 1, "Overleg", Nu.AddHours(3), 60);

        // CPM → Outlook: de bestaande wijzigactie van Mijn taken schrijft naar dezelfde afspraak.
        Assert.Null(await assistent.AgendaWijzig(Ctx(), eventId!, "Overleg verplaatst", Nu.AddHours(5), 30));
        Assert.Equal(eventId, graph.Wijzigingen.Single().EventId);

        // Outlook → CPM: de afspraak wordt live uit de agenda gelezen, dus een wijziging daar (hier: titel en tijd in de fake-agenda) is meteen zichtbaar, met het project.
        graph.Agenda["entra-an"].Single(x => x.Id == eventId).Titel = "Overleg in Outlook aangepast";
        var vm = await assistent.Vandaag(Ctx());
        var a = vm.Afspraken.Single(x => x.Id == eventId);
        Assert.Equal(("Overleg in Outlook aangepast", "Residentie De Linde", Nu.AddHours(5)), (a.Titel, a.ProjectNaam, a.Start));
        Assert.Single(db.OutlookAfspraak);
    }
}
