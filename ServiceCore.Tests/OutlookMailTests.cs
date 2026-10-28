using CPMCore.Models.MijnTaken;
using CPMCore.Services;
using CPMCore.Services.MijnTaken;
using CPMCore.Services.OutlookSync;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ServiceCore.Tests;

/// <summary>De in- en uitsluitregels en koppelregels van de mailsync (spec #2, ticket #5), puur.</summary>
public class OutlookMailRegelsTests
{
    private const string Ik = "an@groupln.be";

    private static MailContext Context()
    {
        var ctx = new MailContext();
        ctx.InterneDomeinen.Add("groupln.be");
        ctx.Projecten.Add(new MailProject(1, "Résidence De Linde", "2501-03", "Keerstraat", "12", true, "Vlekkem"));
        ctx.Projecten.Add(new MailProject(4, "Verkaveling Dorp", null, "Molenstraat", null, true, "Vlekkem"));
        ctx.Projecten.Add(new MailProject(2, "Villa Cauxyde", null, "Dorpsstraat", "5", true));
        ctx.Projecten.Add(new MailProject(3, "Oud Project", null, null, null, false));
        ctx.Contacten["klant@example.com"] = new() { new BekendeContact("klant", 10, null, new[] { 1 }) };
        ctx.Contacten["meer@example.com"] = new() { new BekendeContact("klant", 11, null, new[] { 1, 2 }) };
        ctx.Contacten["info@aannemer.be"] = new() { new BekendeContact("leverancier", null, 20, new[] { 2 }) };
        ctx.Contacten["makelaar@immo.be"] = new() { new BekendeContact("makelaar", null, 21, new[] { 2, 3 }) };
        return ctx;
    }

    private static MailGebruiker Gebruiker(params string[] geblokkeerd) => new(1, new[] { Ik }, new HashSet<string>(geblokkeerd));

    private static GraphMail Mail(string van, string onderwerp = "Vraag", string richting = "in", bool overige = false, params string[] aan) => new()
    {
        Id = Guid.NewGuid().ToString(), VanAdres = van, Onderwerp = onderwerp, Richting = richting, IsOverige = overige,
        Ontvangers = aan.Length == 0 ? new() { Ik } : aan.ToList(), OntvangenOpUtc = new DateTime(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc)
    };

    private static MailBeslissing Beoordeel(GraphMail m, params string[] geblokkeerd) => OutlookMailRegels.Beoordeel(m, Context(), Gebruiker(geblokkeerd));

    [Fact]
    public void Bekende_klant_komt_binnen_en_wordt_gekoppeld_aan_zijn_enige_actieve_project()
    {
        var b = Beoordeel(Mail("klant@example.com"));
        Assert.True(b.Opnemen);
        Assert.Equal((1, 10, "nieuw"), (b.ProjectId, b.KlantId, b.Status));
    }

    [Fact]
    public void Bekende_leverancier_krijgt_leverancier_en_project()
    {
        var b = Beoordeel(Mail("info@aannemer.be"));
        Assert.Equal((2, 20), (b.ProjectId, b.LeverancierId));
    }

    [Fact]
    public void Makelaar_telt_enkel_zijn_actieve_projecten_mee()
    {
        var b = Beoordeel(Mail("makelaar@immo.be"));
        Assert.Equal(2, b.ProjectId);   // project 3 is opgeleverd
    }

    [Fact]
    public void Klant_met_meerdere_actieve_projecten_komt_binnen_als_nog_te_koppelen()
    {
        var b = Beoordeel(Mail("meer@example.com"));
        Assert.True(b.Opnemen);
        Assert.Null(b.ProjectId);
        Assert.Equal("te-koppelen", b.Status);
        Assert.Equal(11, b.KlantId);
    }

    [Theory]
    [InlineData("Planning RESIDENCE de linde", 1)]               // hoofdletter- en accentongevoelig
    [InlineData("Planning Residence De Linde", 1)]
    [InlineData("Offerte keerstraat 12", 1)]                      // straat + nummer
    [InlineData("Vraag over 2501-03", 1)]                         // projectcode
    [InlineData("Re: Villa Cauxyde: ramen", 2)]
    [InlineData("Keerstraat Vlekkem", 1)]                         // straat + gemeente
    [InlineData("vlekkem, MOLENSTRAAT: nutsleidingen", 4)]        // andere volgorde, project zonder huisnummer
    public void Projectverwijzing_in_het_onderwerp_neemt_mails_van_onbekende_afzenders_op(string onderwerp, int project)
    {
        var b = Beoordeel(Mail("gemeente@stad.be", onderwerp));
        Assert.True(b.Opnemen);
        Assert.Equal(project, b.ProjectId);
        Assert.Equal("nieuw", b.Status);
    }

    [Theory]
    [InlineData("Keerstraat 123")]    // ander huisnummer
    [InlineData("Keerstraat")]        // geen nummer
    [InlineData("Linde")]             // maar een deel van de projectnaam
    [InlineData("Vlekkem")]           // enkel de gemeente
    [InlineData("Molenstraat")]       // enkel de straat bij een project zonder nummer
    public void Gedeeltelijke_verwijzingen_tellen_niet(string onderwerp)
        => Assert.False(Beoordeel(Mail("gemeente@stad.be", onderwerp)).Opnemen);

    [Fact]
    public void Twee_projecten_in_het_onderwerp_geven_geen_automatische_koppeling()
    {
        var b = Beoordeel(Mail("gemeente@stad.be", "Villa Cauxyde en Keerstraat 12"));
        Assert.True(b.Opnemen);
        Assert.Null(b.ProjectId);
        Assert.Equal("te-koppelen", b.Status);
    }

    [Fact]
    public void Project_in_het_onderwerp_wint_van_het_project_van_de_klant()
        => Assert.Equal(2, Beoordeel(Mail("klant@example.com", "Villa Cauxyde")).ProjectId);

    [Fact]
    public void Onbekende_afzender_zonder_projectverwijzing_blijft_buiten_cpm()
        => Assert.False(Beoordeel(Mail("iemand@random.be", "Hallo")).Opnemen);

    [Fact]
    public void Overige_van_een_onbekende_afzender_blijft_buiten_cpm_ook_met_een_bekende_ontvanger()
        => Assert.False(Beoordeel(Mail("spam@random.be", "Hallo", overige: true, aan: new[] { Ik, "klant@example.com" })).Opnemen);

    [Fact]
    public void Bekende_contactpersoon_overrult_overige()
        => Assert.True(Beoordeel(Mail("info@aannemer.be", "Hallo", overige: true)).Opnemen);

    [Fact]
    public void Expliciete_projectverwijzing_overrult_overige()
        => Assert.True(Beoordeel(Mail("nieuw@aannemer2.be", "Offerte Keerstraat 12", overige: true)).Opnemen);

    [Fact]
    public void Mail_tussen_enkel_interne_collega_s_blijft_buiten_cpm_tenzij_er_een_projectverwijzing_is()
    {
        Assert.False(Beoordeel(Mail("bo@groupln.be", "Lunch?")).Opnemen);
        Assert.True(Beoordeel(Mail("bo@groupln.be", "Keerstraat 12 plannen")).Opnemen);
    }

    [Fact]
    public void Gmail_telt_niet_als_intern()
    {
        var ctx = Context();
        ctx.Contacten["klant@gmail.com"] = new() { new BekendeContact("klant", 12, null, new[] { 1 }) };
        var b = OutlookMailRegels.Beoordeel(Mail("klant@gmail.com", "Hallo", aan: new[] { "collega@gmail.com" }), ctx, Gebruiker());
        Assert.True(b.Opnemen);
    }

    [Theory]
    [InlineData("noreply@aannemer.be")]
    [InlineData("no-reply@aannemer.be")]
    [InlineData("do-not-reply@aannemer.be")]
    [InlineData("newsletter@aannemer.be")]
    [InlineData("mailer-daemon@aannemer.be")]
    public void Nieuwsbrieven_en_no_reply_worden_genegeerd_ook_van_een_bekende_partij(string van)
    {
        var ctx = Context();
        ctx.Contacten[van] = new() { new BekendeContact("leverancier", null, 20, new[] { 2 }) };
        Assert.False(OutlookMailRegels.Beoordeel(Mail(van, "Villa Cauxyde"), ctx, Gebruiker()).Opnemen);
    }

    [Fact]
    public void Geblokkeerde_afzender_wordt_genegeerd_ook_als_hij_bekend_is()
        => Assert.False(Beoordeel(Mail("info@aannemer.be"), "info@aannemer.be").Opnemen);

    [Fact]
    public void Concepten_worden_genegeerd()
    {
        var m = Mail("klant@example.com"); m.IsConcept = true;
        Assert.False(Beoordeel(m).Opnemen);
    }

    [Fact]
    public void Verzonden_mail_naar_een_klant_wordt_gelogd_maar_is_geen_to_do()
    {
        var b = Beoordeel(Mail(Ik, "Antwoord", richting: "uit", aan: new[] { "klant@example.com" }));
        Assert.True(b.Opnemen);
        Assert.Equal((1, 10, "afgehandeld"), (b.ProjectId, b.KlantId, b.Status));
    }

    [Fact]
    public void Eigen_adres_telt_niet_als_bekende_partij()
    {
        var ctx = Context();
        ctx.Contacten[Ik] = new() { new BekendeContact("leverancier", null, 20, new[] { 2 }) };
        Assert.False(OutlookMailRegels.Beoordeel(Mail("iemand@random.be", "Hallo"), ctx, Gebruiker()).Opnemen);
    }
}

/// <summary>De mailsync met een fake-Graph en een database, plus de Inbox van Mijn taken.</summary>
public class OutlookMailSyncTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name);
    }

    private static readonly DateTime Nu = new(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc);
    private const string Sleutel = "entra-an";

    private static (OutlookSyncService Svc, TestDb Db, FakeGraphIngang Graph) Create()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Users.AddRange(
            new Users { Id = 1, UserId = "u1", Voornaam = "An", Familienaam = "A", Email = "an@groupln.be", EntraObjectId = Sleutel, IsActive = true },
            new Users { Id = 2, UserId = "u2", Voornaam = "Bo", Familienaam = "B", Email = "bo@groupln.be", IsActive = true });
        db.Project.Add(new Project { ProjectId = 1, ProjectName = "Residentie De Linde", ProjectType = 1, StatusId = 2, Street = "Keerstraat", Number = "12" });
        db.ClientAccount.Add(new ClientAccount { Id = 10, Name = "Familie Peeters", Email = "klant@example.com" });
        db.Units.Add(new Units { ProjectId = 1, ClientAccountId = 10, Name = "Woning 1" });
        db.SaveChanges();
        var graph = new FakeGraphIngang();
        var mail = new OutlookMailSyncService(db, graph, NullLogger<OutlookMailSyncService>.Instance);
        return (new OutlookSyncService(db, graph, mail, NullLogger<OutlookSyncService>.Instance), db, graph);
    }

    private static GraphMail Mail(string id, string van, DateTime? ontvangen = null, string onderwerp = "Vraag", string? internetId = null) => new()
    {
        Id = id, InternetMessageId = internetId ?? "<" + id + "@x>", VanAdres = van, Onderwerp = onderwerp, Ontvangers = new() { "an@groupln.be" },
        OntvangenOpUtc = ontvangen ?? Nu.AddMinutes(1), WebLink = "https://outlook.office.com/mail/" + id, Voorvertoning = "Hallo"
    };

    private static async Task Ronde(OutlookSyncService svc, int minuten) => await svc.SynchroniseerAlleAsync(Nu.AddMinutes(minuten));

    [Fact]
    public async Task Mail_van_een_bekende_klant_komt_in_cpm_met_klant_en_project()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "klant@example.com") };

        await Ronde(svc, 5);

        var m = Assert.Single(db.OutlookMail);
        Assert.Equal((1, 1, 10, "in", "nieuw"), (m.UserId, m.ProjectId, m.KlantId, m.Richting, m.Status));
        Assert.Equal("https://outlook.office.com/mail/m1", m.WebLink);
    }

    [Fact]
    public async Task Ruis_komt_niet_in_cpm()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "iemand@random.be"), Mail("m2", "noreply@x.be", onderwerp: "Keerstraat 12") };

        await Ronde(svc, 5);

        Assert.Empty(db.OutlookMail);
    }

    [Fact]
    public async Task Enkel_mails_vanaf_het_moment_van_koppelen_komen_binnen()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("oud", "klant@example.com", Nu.AddDays(-3)), Mail("nieuw", "klant@example.com") };

        await Ronde(svc, 5);

        Assert.Equal(new[] { "nieuw" }, db.OutlookMail.Select(m => m.GraphId));
        Assert.Equal(Nu, graph.MailAanvragen.Single().Vanaf);
    }

    [Fact]
    public async Task Er_worden_geen_bijlagen_of_volledige_tekst_bewaard_enkel_een_voorvertoning_van_300_tekens()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        var mail = Mail("m1", "klant@example.com");
        mail.Voorvertoning = new string('x', 1000);
        graph.MailsPerGebruiker[Sleutel] = new() { mail };

        await Ronde(svc, 5);

        Assert.Equal(300, db.OutlookMail.Single().Voorvertoning!.Length);
        // De entiteit heeft geen kolom voor een body of bijlagen.
        Assert.DoesNotContain(typeof(OutlookMail).GetProperties(), p => p.Name.Contains("Body", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Bijlage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Dezelfde_mail_wordt_nooit_twee_keer_bewaard()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        // Zelfde internet-message-id onder twee Graph-id's (bv. dezelfde mail in Postvak IN en een submap).
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("a", "klant@example.com", internetId: "<dubbel@x>"), Mail("b", "klant@example.com", internetId: "<dubbel@x>") };

        await Ronde(svc, 5);
        await Ronde(svc, 10);

        Assert.Single(db.OutlookMail);
    }

    [Fact]
    public async Task De_volgende_ronde_gebruikt_het_bewaarde_deltatoken_en_haalt_enkel_nieuwe_mails()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "klant@example.com") };
        await Ronde(svc, 5);

        graph.MailsPerGebruiker[Sleutel].Add(Mail("m2", "klant@example.com"));
        await Ronde(svc, 10);

        Assert.Equal("1", graph.MailAanvragen[1].Tokens);
        Assert.Equal(2, db.OutlookMail.Count());
    }

    [Fact]
    public async Task Een_in_outlook_verwijderde_mail_blijft_met_een_markering()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "klant@example.com") };
        await Ronde(svc, 5);

        graph.VerwijderdeMailIds.Add("m1");
        await Ronde(svc, 10);

        Assert.True(db.OutlookMail.Single().VerwijderdInOutlook);
    }

    [Fact]
    public async Task Een_fout_bij_het_ophalen_laat_de_deltatokens_ongemoeid_en_zet_de_status_op_fout()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "klant@example.com") };
        await Ronde(svc, 5);
        graph.MailFout = "Graph gaf status 500.";

        await Ronde(svc, 10);

        var rij = db.OutlookSyncStatus.Single();
        Assert.Equal(("fout", "1"), (rij.Status, rij.DeltaTokensJson));
        Assert.Single(db.OutlookMail);
    }

    [Fact]
    public async Task Geblokkeerde_afzenders_van_een_gebruiker_worden_overgeslagen()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        db.OutlookGeblokkeerdeAfzender.Add(new OutlookGeblokkeerdeAfzender { UserId = 1, Email = "Klant@Example.com", AangemaaktOp = Nu });
        db.SaveChanges();
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "klant@example.com") };

        await Ronde(svc, 5);

        Assert.Empty(db.OutlookMail);
    }

    [Fact]
    public async Task Een_naar_een_andere_map_verplaatste_mail_is_niet_verwijderd_in_outlook()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "klant@example.com") };
        await Ronde(svc, 5);

        // Graph meldt de oude plaats als verwijderd en dezelfde mail (zelfde vaste id) als nieuw in de nieuwe map.
        graph.VerwijderdeMailIds.Add("m1");
        graph.MailsPerGebruiker[Sleutel].Insert(0, Mail("m0", "iemand@random.be"));   // de fake geeft vanaf het bewaarde aantal terug: dus m1 opnieuw
        await Ronde(svc, 10);

        Assert.False(db.OutlookMail.Single().VerwijderdInOutlook);
    }

    [Fact]
    public async Task Onveilige_links_worden_niet_bewaard()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        var m = Mail("m1", "klant@example.com"); m.WebLink = "javascript:alert(1)";
        graph.MailsPerGebruiker[Sleutel] = new() { m };

        await Ronde(svc, 5);

        Assert.Null(db.OutlookMail.Single().WebLink);
    }

    // ── Inbox ───────────────────────────────────────────────────────────────────────────────

    private static async Task<(MijnTakenAssistent Assistent, MtContext Ctx)> AssistentVoor(TestDb db, FakeGraphIngang graph)
    {
        var assistent = new MijnTakenAssistent(db, null!, graph);
        var ctx = new MtContext { UserId = 1, UserStr = "1", UserCode = "u1", Nu = Nu.AddMinutes(30) };
        ctx.Projecten[1] = new MtProject { Id = 1, Naam = "Residentie De Linde", Kleur = "#000" };
        return (assistent, ctx);
    }

    [Fact]
    public async Task Inbox_toont_mailrijen_en_afgehandeld_haalt_ze_uit_de_inbox_maar_niet_van_het_project()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "klant@example.com", onderwerp: "Vraag over ramen") };
        await Ronde(svc, 5);
        var (assistent, ctx) = await AssistentVoor(db, graph);

        var inbox = await assistent.Inbox(ctx);
        var mail = Assert.Single(inbox.Mails);
        Assert.Equal(("Vraag over ramen", "Residentie De Linde", "Familie Peeters"), (mail.Onderwerp, mail.Project, mail.Klant));
        Assert.Equal(1, inbox.Shell.InboxAantal);

        Assert.True(await assistent.MailAfgehandeld(ctx, mail.Id));

        var daarna = await assistent.Inbox(ctx);
        Assert.Empty(daarna.Mails);
        Assert.Equal(0, daarna.Shell.InboxAantal);
        var rij = db.OutlookMail.Single();
        Assert.Equal((1, "afgehandeld"), (rij.ProjectId, rij.Status));   // blijft op het project
    }

    [Fact]
    public async Task Verzonden_mails_staan_niet_in_de_inbox_of_de_teller_maar_wel_in_de_verzonden_sectie()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        var uit = Mail("u1", "an@groupln.be", onderwerp: "Antwoord"); uit.Richting = "uit"; uit.Ontvangers = new() { "klant@example.com" };
        graph.MailsPerGebruiker[Sleutel] = new() { uit };
        await Ronde(svc, 5);
        var (assistent, ctx) = await AssistentVoor(db, graph);

        var inbox = await assistent.Inbox(ctx);

        Assert.Empty(inbox.Mails);
        Assert.Single(inbox.Verzonden);
        Assert.Equal(0, inbox.Shell.InboxAantal);
    }

    [Fact]
    public async Task Een_mail_van_een_andere_gebruiker_kan_niet_op_afgehandeld_gezet_worden()
    {
        var (svc, db, graph) = Create();
        await svc.ZetAanAsync(1, Nu);
        graph.MailsPerGebruiker[Sleutel] = new() { Mail("m1", "klant@example.com") };
        await Ronde(svc, 5);
        var (assistent, ctx) = await AssistentVoor(db, graph);
        ctx.UserId = 2; ctx.UserStr = "2";

        Assert.False(await assistent.MailAfgehandeld(ctx, db.OutlookMail.Single().Id));
        Assert.Equal("nieuw", db.OutlookMail.Single().Status);
    }
}
