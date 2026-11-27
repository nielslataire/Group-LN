using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Traject.TriggerActions;
using Xunit;

/// <summary>Trigger-mails naar rollen zonder projectkoppeling (Boekhouder, CEO / CFO, Projectontwikkelaar): ze gaan naar de actieve gebruikers met de gebruikersrol Boekhouding of Bestuurder. Gebruikers zonder rol zijn extern en krijgen niets.</summary>
public class TriggerRolMailTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private class Verzender : IEmailSender
    {
        public List<(string Aan, string Onderwerp, string Body)> Verstuurd { get; } = new();
        public Task SendEmailAsync(string toEmail, string subject, string htmlMessage, IEnumerable<EmailAttachment>? attachments = null, string? cc = null, string? bcc = null, string? fromEmail = null)
        { Verstuurd.Add((toEmail, subject, htmlMessage)); return Task.CompletedTask; }
    }

    private static (TestDb Db, Project Project) Maak()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        var project = new Project { ProjectId = 1, ProjectName = "Residentie", AspNetUserId = "pl-1", SalesResponsibleAspNetUserId = "vk-1" };
        db.Project.Add(project);
        db.SecurityRole.AddRange(
            new SecurityRole { RoleId = 1, Name = "Boekhouding", IsActive = true },
            new SecurityRole { RoleId = 2, Name = "Bestuurder", IsActive = true },
            new SecurityRole { RoleId = 3, Name = "Marketing", IsActive = true },
            new SecurityRole { RoleId = 4, Name = "Boekhouding", IsActive = false },
            new SecurityRole { RoleId = 5, Name = "Admin", IsActive = true });                          // niet actieve rol telt niet
        db.Users.AddRange(
            new Users { Id = 1, UserId = "pl-1", Email = "pl@x.be", IsActive = true },
            new Users { Id = 2, UserId = "vk-1", Email = "vk@x.be", IsActive = true },
            new Users { Id = 3, UserId = "b-1", Email = "boek@x.be", IsActive = true },
            new Users { Id = 4, UserId = "b-2", Email = "BOEK@x.be", IsActive = true },                         // dubbel adres (hoofdletters)
            new Users { Id = 5, UserId = "b-3", Email = "oud@x.be", IsActive = false },                         // niet actief
            new Users { Id = 6, UserId = "c-1", Email = "ceo@x.be", IsActive = true },
            new Users { Id = 7, UserId = "c-2", Email = "cfo@x.be", IsActive = true },
            new Users { Id = 8, UserId = "c-3", Email = null, IsActive = true },                                // geen mailadres
            new Users { Id = 9, UserId = "m-1", Email = "marketing@x.be", IsActive = true },                    // andere rol
            new Users { Id = 10, UserId = "e-1", Email = "extern@x.be", IsActive = true });                     // geen rol = extern
        db.Users.Add(new Users { Id = 11, UserId = "a-1", Email = "admin@x.be", IsActive = true });
        db.SecurityUserRole.AddRange(
            new SecurityUserRole { Id = 1, UserId = 3, RoleId = 1 }, new SecurityUserRole { Id = 2, UserId = 4, RoleId = 1 },
            new SecurityUserRole { Id = 3, UserId = 5, RoleId = 1 }, new SecurityUserRole { Id = 4, UserId = 6, RoleId = 2 },
            new SecurityUserRole { Id = 5, UserId = 7, RoleId = 2 }, new SecurityUserRole { Id = 6, UserId = 8, RoleId = 2 },
            new SecurityUserRole { Id = 7, UserId = 9, RoleId = 3 }, new SecurityUserRole { Id = 8, UserId = 1, RoleId = 4 }, new SecurityUserRole { Id = 9, UserId = 11, RoleId = 5 });
        db.SaveChanges();
        return (db, project);
    }

    private static Mijlpaal Mijlpaal(InterneRol rol) => new() { Naam = "Aktefacturen klaarzetten", VerantwoordelijkeRol = (int)rol };
    private static MijlpaalTrigger Trigger() => new() { ActieParametersJson = null };

    [Theory]
    [InlineData(InterneRol.Boekhouder, "boek@x.be")]
    [InlineData(InterneRol.CeoCfo, "admin@x.be,ceo@x.be,cfo@x.be")]
    [InlineData(InterneRol.Projectontwikkelaar, "admin@x.be,ceo@x.be,cfo@x.be")]   // ook een bestuurder
    [InlineData(InterneRol.Projectleider, "pl@x.be")]
    [InlineData(InterneRol.Verkoper, "vk@x.be")]
    public async Task Verwittig_rol_mailt_alle_actieve_gebruikers_van_de_rol(InterneRol rol, string verwacht)
    {
        var (db, project) = Maak();
        var verzender = new Verzender();
        var actie = new VerwittigRolAction(db, verzender);

        var r = await actie.ExecuteAsync(Mijlpaal(rol), Trigger(), project, null);

        Assert.True(r.Geslaagd);
        Assert.False(r.Overgeslagen);
        Assert.Equal(verwacht.Split(','), verzender.Verstuurd.Select(v => v.Aan.ToLowerInvariant()).OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task Extern_heeft_geen_rol_en_wordt_overgeslagen_met_uitleg()
    {
        var (db, project) = Maak();
        var verzender = new Verzender();
        var r = await new VerwittigRolAction(db, verzender).ExecuteAsync(Mijlpaal(InterneRol.Extern), Trigger(), project, null);

        Assert.True(r.Overgeslagen);
        Assert.Empty(verzender.Verstuurd);
        Assert.Contains("Extern", r.Resultaat);
    }

    [Fact]
    public async Task Een_expliciet_mailadres_gaat_voor_op_de_rol()
    {
        var (db, project) = Maak();
        var verzender = new Verzender();
        var trigger = new MijlpaalTrigger { ActieParametersJson = "{\"email\":\"extra@x.be\"}" };
        await new VerwittigRolAction(db, verzender).ExecuteAsync(Mijlpaal(InterneRol.Boekhouder), trigger, project, null);

        Assert.Equal(new[] { "extra@x.be" }, verzender.Verstuurd.Select(v => v.Aan).ToArray());
    }

    [Fact]
    public async Task Herinnering_naar_de_rol_boekhouder_en_bestuurders_werkt_ook()
    {
        var (db, project) = Maak();
        var verzender = new Verzender();
        var actie = new PlanHerinneringAction(db, verzender);

        await actie.ExecuteAsync(Mijlpaal(InterneRol.Boekhouder), Trigger(), project, null);
        await actie.ExecuteAsync(Mijlpaal(InterneRol.CeoCfo), Trigger(), project, null);

        Assert.Equal(4, verzender.Verstuurd.Count);                                          // 1 boekhouder + 3 bestuurders (CEO, CFO, admin)
        Assert.All(verzender.Verstuurd, v => Assert.StartsWith("Herinnering:", v.Onderwerp));
    }

    private static IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["App:BaseUrl"] = "https://cpm.test/" }).Build();

    [Fact]
    public async Task De_mail_zegt_wat_je_moet_doen_en_bevat_eenheid_datum_en_link()
    {
        var (db, project) = Maak();
        db.Units.Add(new Units { Id = 50, Name = "Lot 09", ProjectId = 1 });
        db.SaveChanges();
        var verzender = new Verzender();
        var mijlpaal = new Mijlpaal { Id = 765, Code = "AKTEFACTUREN_BETAALD", Naam = "Aktefacturen betaald", UnitId = 50, VerantwoordelijkeRol = (int)InterneRol.Boekhouder, Doeldatum = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) };
        var trigger = new MijlpaalTrigger { TriggerEvent = (int)TriggerEvent.XDagenVoorDoeldatum };

        await new VerwittigRolAction(db, verzender, Config()).ExecuteAsync(mijlpaal, trigger, project, null);

        var mail = verzender.Verstuurd.First();
        Assert.StartsWith("Actie nodig: Aktefacturen betaald — Lot 09 (Residentie)", mail.Onderwerp);
        Assert.Contains("Wat moet je doen?", mail.Body);
        Assert.Contains("Controleer of de aktefacturen van deze woning betaald zijn", mail.Body);
        Assert.Contains("is morgen", mail.Body);
        Assert.Contains("Lot 09", mail.Body);
        Assert.Contains("https://cpm.test/Projects/1/Traject?hl=765", mail.Body);
        Assert.DoesNotContain("Opmerking", mail.Body);                                      // lege opmerking verdwijnt
    }

    [Fact]
    public async Task De_tekst_van_de_trigger_gaat_voor_op_de_standaardtekst_en_een_onbekende_code_krijgt_een_algemene_zin()
    {
        var (db, project) = Maak();
        var verzender = new Verzender();
        var actie = new VerwittigRolAction(db, verzender, Config());
        var eigen = new MijlpaalTrigger { TriggerEvent = (int)TriggerEvent.BijBereiken, ActieParametersJson = "{\"omschrijving\":\"Bel de notaris.\"}" };
        await actie.ExecuteAsync(new Mijlpaal { Id = 1, Code = "NOTARIELE_AKTE", Naam = "Akte", VerantwoordelijkeRol = (int)InterneRol.Boekhouder }, eigen, project, null);
        await actie.ExecuteAsync(new Mijlpaal { Id = 2, Code = "ONBEKEND_X", Naam = "Iets", VerantwoordelijkeRol = (int)InterneRol.Boekhouder }, new MijlpaalTrigger { TriggerEvent = (int)TriggerEvent.BijOverschrijding }, project, null);

        Assert.Contains("Bel de notaris.", verzender.Verstuurd[0].Body);
        Assert.DoesNotContain("Volg de notariële akte", verzender.Verstuurd[0].Body);
        Assert.Contains("vink ze af zodra ze klaar is", verzender.Verstuurd[1].Body);
        Assert.Contains("niet op tijd afgerond", verzender.Verstuurd[1].Body);
    }
}
