using CPMCore.Services;
using CPMCore.Services.Contacten;
using CPMCore.Services.OutlookSync;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ServiceCore.Tests;

/// <summary>Voorstel en threaderving van de mailregels, en de acties van de gebruiker op een mail (spec #2, ticket #12).</summary>
public class OutlookMailKoppelRegelsTests
{
    private const string Ik = "an@groupln.be";

    private static MailContext Context()
    {
        var ctx = new MailContext();
        ctx.InterneDomeinen.Add("groupln.be");
        ctx.Projecten.Add(new MailProject(1, "Residentie De Linde", null, "Keerstraat", "12", true));
        ctx.Projecten.Add(new MailProject(2, "Villa Cauxyde", null, "Dorpsstraat", "5", true));
        ctx.Contacten["meer@example.com"] = new() { new BekendeContact("klant", 11, null, new[] { 1, 2 }) };
        return ctx;
    }

    private static GraphMail Mail(string van, string onderwerp = "Vraag", string richting = "in", string? conversatie = null, params string[] aan) => new()
    {
        Id = Guid.NewGuid().ToString(), VanAdres = van, Onderwerp = onderwerp, Richting = richting, ConversationId = conversatie,
        Ontvangers = aan.Length == 0 ? new() { Ik } : aan.ToList(), OntvangenOpUtc = new DateTime(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc)
    };

    private static MailGebruiker Gebruiker(Dictionary<string, int>? threads = null, params int[] recent) =>
        new(1, new[] { Ik }, new HashSet<string>(), threads ?? new Dictionary<string, int>(), recent);

    [Fact]
    public void Een_thread_die_al_gekoppeld_is_geeft_zijn_project_door_aan_een_antwoord_van_een_onbekende_afzender()
    {
        var b = OutlookMailRegels.Beoordeel(Mail("nieuw@aannemer.be", "Re: iets", conversatie: "c1"), Context(), Gebruiker(new() { ["c1"] = 2 }));
        Assert.True(b.Opnemen);
        Assert.Equal((2, "nieuw", "thread"), (b.ProjectId, b.Status, b.Reden));
    }

    [Fact]
    public void Een_verzonden_antwoord_in_een_gekoppelde_thread_wordt_gelogd_op_het_project()
    {
        var b = OutlookMailRegels.Beoordeel(Mail(Ik, "Re: iets", "uit", "c1", "nieuw@aannemer.be"), Context(), Gebruiker(new() { ["c1"] = 2 }));
        Assert.True(b.Opnemen);
        Assert.Equal((2, "afgehandeld"), (b.ProjectId, b.Status));
    }

    [Fact]
    public void Een_thread_van_een_ander_gesprek_geeft_niets_door()
        => Assert.False(OutlookMailRegels.Beoordeel(Mail("nieuw@aannemer.be", "Hallo", conversatie: "c2"), Context(), Gebruiker(new() { ["c1"] = 2 })).Opnemen);

    [Fact]
    public void Een_enkel_project_in_het_onderwerp_wint_van_de_thread()
        => Assert.Equal(1, OutlookMailRegels.Beoordeel(Mail("x@y.be", "Keerstraat 12", conversatie: "c1"), Context(), Gebruiker(new() { ["c1"] = 2 })).ProjectId);

    [Fact]
    public void De_thread_wint_van_het_enige_project_van_de_partij_niet_van_een_expliciete_verwijzing_maar_wel_van_een_gok()
    {
        // meer@example.com heeft twee actieve projecten (geen enige), de thread beslist.
        var b = OutlookMailRegels.Beoordeel(Mail("meer@example.com", "Re: ramen", conversatie: "c1"), Context(), Gebruiker(new() { ["c1"] = 2 }));
        Assert.Equal(2, b.ProjectId);
    }

    [Fact]
    public void Zonder_zekerheid_komt_de_mail_te_koppelen_met_een_voorstel_uit_de_recentste_koppelingen()
    {
        var b = OutlookMailRegels.Beoordeel(Mail("meer@example.com"), Context(), Gebruiker(recent: new[] { 2, 1 }));
        Assert.True(b.Opnemen);
        Assert.Null(b.ProjectId);
        Assert.Equal(("te-koppelen", 2), (b.Status, b.VoorgesteldProjectId));
    }

    [Fact]
    public void Zonder_recente_koppelingen_is_er_geen_voorstel_en_wordt_er_niet_gegokt()
    {
        var b = OutlookMailRegels.Beoordeel(Mail("meer@example.com"), Context(), Gebruiker());
        Assert.Equal(("te-koppelen", null), (b.Status, b.VoorgesteldProjectId));
        Assert.Null(b.ProjectId);
    }

    [Fact]
    public void Twee_projecten_in_het_onderwerp_geven_een_voorstel_onder_die_twee()
    {
        var b = OutlookMailRegels.Beoordeel(Mail("gemeente@stad.be", "Villa Cauxyde en Keerstraat 12"), Context(), Gebruiker(recent: new[] { 3, 1 }));
        Assert.Equal((null, 1), (b.ProjectId, b.VoorgesteldProjectId));
    }
}

public class OutlookMailActiesTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name);
    }

    private static readonly DateTime Nu = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);
    private static readonly int[] Toegestaan = { 1, 2 };

    private static (OutlookMailActies Acties, OutlookSyncService Sync, TestDb Db, FakeGraphIngang Graph) Create()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Users.AddRange(
            new Users { Id = 1, UserId = "u1", Voornaam = "An", Familienaam = "A", Email = "an@groupln.be", EntraObjectId = "entra-an", IsActive = true },
            new Users { Id = 2, UserId = "u2", Voornaam = "Bo", Familienaam = "B", Email = "bo@groupln.be", IsActive = true });
        db.Project.Add(new Project { ProjectId = 1, ProjectName = "Residentie De Linde", ProjectType = 1, StatusId = 2, Street = "Keerstraat", Number = "12" });
        db.Project.Add(new Project { ProjectId = 2, ProjectName = "Villa Cauxyde", ProjectType = 1, StatusId = 2 });
        db.SaveChanges();
        var graph = new FakeGraphIngang();
        var mailSync = new OutlookMailSyncService(db, graph, NullLogger<OutlookMailSyncService>.Instance);
        var sync = new OutlookSyncService(db, graph, mailSync, NullLogger<OutlookSyncService>.Instance);
        return (new OutlookMailActies(db, mailSync, new CrmContactService(db)), sync, db, graph);
    }

    private static OutlookMail Rij(int userId, string afzender = "gemeente@stad.be", string status = "te-koppelen", int? project = null, string? conversatie = "c1", string graphId = "g1") => new()
    {
        UserId = userId, Richting = "in", GraphId = graphId, InternetMessageId = "<" + graphId + ">", ConversationId = conversatie, Afzender = afzender, AfzenderNaam = "Jan Peeters",
        Onderwerp = "Vraag over nutsleidingen", OntvangenOp = Nu, Status = status, ProjectId = project, AangemaaktOp = Nu
    };

    // ── Koppelen ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Koppelen_zet_het_project_en_haalt_de_mail_uit_nog_te_koppelen()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1)); db.SaveChanges();

        var fout = await acties.KoppelAsync(1, db.OutlookMail.Single().Id, 2, Toegestaan);

        Assert.Null(fout);
        var m = db.OutlookMail.Single();
        Assert.Equal((2, "nieuw", null), (m.ProjectId, m.Status, m.VoorgesteldProjectId));
    }

    [Fact]
    public async Task Een_foute_koppeling_kan_gecorrigeerd_worden_naar_een_ander_project()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1, status: "nieuw", project: 1)); db.SaveChanges();

        await acties.KoppelAsync(1, db.OutlookMail.Single().Id, 2, Toegestaan);

        Assert.Equal(2, db.OutlookMail.Single().ProjectId);
    }

    [Fact]
    public async Task Een_thread_volgt_mee_als_een_mail_gekoppeld_wordt()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.AddRange(Rij(1, graphId: "g1"), Rij(1, graphId: "g2"), Rij(1, graphId: "g3", conversatie: "c9"));
        db.SaveChanges();

        await acties.KoppelAsync(1, db.OutlookMail.First(m => m.GraphId == "g1").Id, 1, Toegestaan);

        Assert.Equal(new int?[] { 1, 1, null }, db.OutlookMail.OrderBy(m => m.GraphId).Select(m => m.ProjectId).ToArray());
    }

    [Fact]
    public async Task Koppelen_aan_een_project_buiten_je_bereik_of_aan_de_mail_van_een_ander_wordt_geweigerd()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1)); db.SaveChanges();
        var id = db.OutlookMail.Single().Id;

        Assert.NotNull(await acties.KoppelAsync(1, id, 99, Toegestaan));
        Assert.NotNull(await acties.KoppelAsync(2, id, 1, Toegestaan));
        Assert.Null(db.OutlookMail.Single().ProjectId);
    }

    // ── Taak maken ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Van_een_mail_een_taak_maken_zet_een_item_in_de_inbox_en_de_mail_op_afgehandeld()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1, status: "nieuw", project: 1)); db.SaveChanges();

        var (taakId, fout) = await acties.MaakTaakAsync(1, "1", db.OutlookMail.Single().Id, Toegestaan);

        Assert.Null(fout);
        var taak = db.ProjectTaak.Single();
        Assert.Equal(taak.Id, taakId);
        Assert.Equal(("Vraag over nutsleidingen", 1, true, "Mail", "van Jan Peeters", "1"), (taak.Titel, taak.ProjectId, taak.IsInbox, taak.InboxBron, taak.InboxMeta, taak.ToegewezenAanUserId));
        var m = db.OutlookMail.Single();
        Assert.Equal(("afgehandeld", 1), (m.Status, m.ProjectId));
    }

    [Fact]
    public async Task Geen_taak_van_andermans_mail()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1)); db.SaveChanges();

        var (taakId, fout) = await acties.MaakTaakAsync(2, "2", db.OutlookMail.Single().Id, Toegestaan);

        Assert.Null(taakId);
        Assert.NotNull(fout);
        Assert.Empty(db.ProjectTaak);
    }

    [Fact]
    public async Task Een_correctie_werkt_door_op_de_hele_conversatie_ook_op_mails_die_cpm_zelf_koppelde()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.AddRange(Rij(1, graphId: "g1", status: "nieuw", project: 1), Rij(1, graphId: "g2", status: "nieuw", project: 1)); db.SaveChanges();

        await acties.KoppelAsync(1, db.OutlookMail.First(m => m.GraphId == "g1").Id, 2, Toegestaan);

        Assert.All(db.OutlookMail, m => Assert.Equal(2, m.ProjectId));
    }

    [Fact]
    public async Task Een_taak_of_contact_op_een_project_buiten_je_bereik_krijgt_dat_project_niet()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1, afzender: "jan@bedrijf.be", status: "nieuw", project: 2)); db.SaveChanges();
        var id = db.OutlookMail.Single().Id;
        var enkelProject1 = new[] { 1 };

        Assert.NotNull(await acties.VoegContactToeAsync(1, id, "An A", enkelProject1));
        Assert.Empty(db.CrmContact);

        var (_, fout) = await acties.MaakTaakAsync(1, "1", id, enkelProject1);
        Assert.Null(fout);
        Assert.Null(db.ProjectTaak.Single().ProjectId);
    }

    [Fact]
    public async Task Een_dubbelklik_op_taak_maken_geeft_geen_tweede_taak()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1, status: "nieuw", project: 1)); db.SaveChanges();
        var id = db.OutlookMail.Single().Id;

        await acties.MaakTaakAsync(1, "1", id, Toegestaan);
        var (taakId, fout) = await acties.MaakTaakAsync(1, "1", id, Toegestaan);

        Assert.Null(taakId);
        Assert.NotNull(fout);
        Assert.Single(db.ProjectTaak);
    }

    [Fact]
    public async Task De_afzender_van_een_verzonden_mail_negeren_kan_niet_en_blokkeert_je_eigen_adres_niet()
    {
        var (acties, _, db, _) = Create();
        var uit = Rij(1, afzender: "an@groupln.be", status: "afgehandeld", project: 1); uit.Richting = "uit";
        db.OutlookMail.Add(uit); db.SaveChanges();

        Assert.NotNull(await acties.NegeerAfzenderAsync(1, db.OutlookMail.Single().Id));
        Assert.Empty(db.OutlookGeblokkeerdeAfzender);
    }

    [Fact]
    public async Task Een_collega_wordt_niet_als_extern_contact_toegevoegd()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1, afzender: "bo@groupln.be", status: "nieuw", project: 1)); db.SaveChanges();

        Assert.NotNull(await acties.VoegContactToeAsync(1, db.OutlookMail.Single().Id, "An A", Toegestaan));
        Assert.Empty(db.CrmContact);
    }

    [Fact]
    public async Task Een_verzonden_antwoord_in_een_gekoppelde_thread_komt_via_de_sync_op_het_project()
    {
        var (_, sync, db, graph) = Create();
        await sync.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker["entra-an"] = new()
        {
            new GraphMail { Id = "in", InternetMessageId = "<in>", ConversationId = "c1", VanAdres = "gemeente@stad.be", Onderwerp = "Keerstraat 12 nutsleidingen", Ontvangers = new() { "an@groupln.be" }, OntvangenOpUtc = Nu.AddMinutes(1) },
            new GraphMail { Id = "uit", InternetMessageId = "<uit>", ConversationId = "c1", Richting = "uit", VanAdres = "an@groupln.be", Onderwerp = "Re: nutsleidingen", Ontvangers = new() { "nieuw@aannemer.be" }, OntvangenOpUtc = Nu.AddMinutes(2) }
        };

        await sync.SynchroniseerAlleAsync(Nu.AddMinutes(5));

        var rijen = db.OutlookMail.OrderBy(m => m.OntvangenOp).ToList();
        Assert.Equal(new[] { ("in", 1, "nieuw"), ("uit", 1, "afgehandeld") }, rijen.Select(m => (m.Richting, m.ProjectId ?? 0, m.Status)).ToArray());
    }

    // ── Afzender negeren ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Een_afzender_negeren_is_blijvend_ook_voor_de_volgende_sync()
    {
        var (acties, sync, db, graph) = Create();
        await sync.ZetAanAsync(1, Nu.AddDays(-1));
        db.OutlookMail.Add(Rij(1, afzender: "Spam@Bedrijf.be", graphId: "oud")); db.SaveChanges();

        Assert.Null(await acties.NegeerAfzenderAsync(1, db.OutlookMail.Single().Id));

        Assert.Equal("afgehandeld", db.OutlookMail.Single().Status);   // de bestaande mail verdwijnt uit de Inbox
        Assert.Equal("spam@bedrijf.be", db.OutlookGeblokkeerdeAfzender.Single().Email);

        // Een nieuwe mail van die afzender, met een projectverwijzing in het onderwerp, komt niet meer binnen.
        graph.MailsPerGebruiker["entra-an"] = new()
        {
            new GraphMail { Id = "nieuw", InternetMessageId = "<nieuw>", VanAdres = "spam@bedrijf.be", Onderwerp = "Keerstraat 12", Ontvangers = new() { "an@groupln.be" }, OntvangenOpUtc = Nu.AddHours(1) }
        };
        await sync.SynchroniseerAlleAsync(Nu.AddHours(2));
        Assert.Single(db.OutlookMail);
    }

    [Fact]
    public async Task Negeren_twee_keer_geeft_geen_dubbele_rij()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.AddRange(Rij(1, afzender: "spam@bedrijf.be", graphId: "a"), Rij(1, afzender: "spam@bedrijf.be", graphId: "b")); db.SaveChanges();

        await acties.NegeerAfzenderAsync(1, db.OutlookMail.First().Id);
        await acties.NegeerAfzenderAsync(1, db.OutlookMail.OrderBy(m => m.Id).Last().Id);

        Assert.Single(db.OutlookGeblokkeerdeAfzender);
        Assert.All(db.OutlookMail, m => Assert.Equal("afgehandeld", m.Status));
    }

    // ── Contact toevoegen ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task De_sync_maakt_nooit_zelf_contacten_aan()
    {
        var (_, sync, db, graph) = Create();
        await sync.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker["entra-an"] = new()
        {
            new GraphMail { Id = "m1", InternetMessageId = "<m1>", VanAdres = "onbekend@bedrijf.be", Onderwerp = "Keerstraat 12", Ontvangers = new() { "an@groupln.be" }, OntvangenOpUtc = Nu.AddMinutes(1) }
        };

        await sync.SynchroniseerAlleAsync(Nu.AddMinutes(5));

        Assert.Single(db.OutlookMail);
        Assert.Empty(db.CrmContact);
    }

    [Fact]
    public async Task Een_onbekende_afzender_toevoegen_als_contact_gebeurt_enkel_op_verzoek_en_zonder_toestemmingen()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1, afzender: "jan@bedrijf.be", status: "nieuw", project: 1)); db.SaveChanges();

        var fout = await acties.VoegContactToeAsync(1, db.OutlookMail.Single().Id, "An A", Toegestaan);

        Assert.Null(fout);
        var c = db.CrmContact.Single();
        Assert.Equal((1, "jan@bedrijf.be", "Jan", "Peeters", "E-mail", false, false), (c.ProjectId, c.Email, c.FirstName, c.LastName, c.Source, c.ConsentContact, c.ConsentNewsletter));
    }

    [Fact]
    public async Task Een_adres_dat_cpm_al_kent_wordt_niet_nog_eens_als_contact_toegevoegd()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1, afzender: "jan@bedrijf.be", status: "nieuw", project: 1)); db.SaveChanges();
        var id = db.OutlookMail.Single().Id;

        await acties.VoegContactToeAsync(1, id, "An A", Toegestaan);
        var fout = await acties.VoegContactToeAsync(1, id, "An A", Toegestaan);

        Assert.Contains("al bekend", fout);
        Assert.Single(db.CrmContact);
    }

    [Fact]
    public async Task Een_contact_toevoegen_vraagt_eerst_een_project()
    {
        var (acties, _, db, _) = Create();
        db.OutlookMail.Add(Rij(1, afzender: "jan@bedrijf.be")); db.SaveChanges();

        Assert.NotNull(await acties.VoegContactToeAsync(1, db.OutlookMail.Single().Id, "An A", Toegestaan));
        Assert.Empty(db.CrmContact);
    }

    [Theory]
    [InlineData("Jan Peeters", "jan@x.be", "Jan", "Peeters")]
    [InlineData("Jan van den Berg", "jan@x.be", "Jan", "van den Berg")]
    [InlineData("Peeters", "jan@x.be", null, "Peeters")]
    [InlineData("jan@x.be", "jan@x.be", null, "jan")]
    [InlineData(null, "info@bedrijf.be", null, "info")]
    public void Weergavenamen_worden_in_voornaam_en_naam_gesplitst(string? weergave, string adres, string? voornaam, string naam)
        => Assert.Equal((voornaam, naam), OutlookMailActies.SplitsNaam(weergave, adres));
}
