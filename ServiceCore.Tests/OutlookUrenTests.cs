using CPMCore.Services;
using CPMCore.Services.OutlookSync;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ServiceCore.Tests;

/// <summary>Regie-uren uit Outlook-afspraken goedkeuren (spec #2, ticket #9).</summary>
public class OutlookUrenTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b) => b.UseInMemoryDatabase(_name);
    }

    private static readonly DateOnly Maandag = new(2026, 11, 2);
    private static readonly DateTime Dinsdag = new(2026, 11, 3, 9, 0, 0);
    private static readonly int[] Zichtbaar = { 1, 2, 3 };
    private const string Gebruiker = "entra-an";

    /// <summary>Project 1 = regie, 2 = gemengd, 3 = schijven (geen regie). Gebruiker u1 heeft enkel op project 1 een tarief.</summary>
    private static (OutlookUrenService Uren, TestDb Db, FakeGraphIngang Graph) Bouw()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Users.Add(new Users { Id = 1, UserId = "u1", Voornaam = "An", Familienaam = "A", Email = "an@groupln.be", EntraObjectId = Gebruiker, IsActive = true });
        db.Project.Add(new Project { ProjectId = 1, ProjectName = "Regieproject", ProjectType = 1, StatusId = 2, ContractType = 2 });
        db.Project.Add(new Project { ProjectId = 2, ProjectName = "Gemengd project", ProjectType = 1, StatusId = 2, ContractType = 3 });
        db.Project.Add(new Project { ProjectId = 3, ProjectName = "Schijvenproject", ProjectType = 1, StatusId = 2, ContractType = 1 });
        db.ProjectHourlyRate.Add(new ProjectHourlyRate { ProjectId = 1, UserId = "u1", HourlyRate = 80m });
        db.SaveChanges();
        var graph = new FakeGraphIngang();
        graph.Agenda[Gebruiker] = new();
        return (new OutlookUrenService(db, graph), db, graph);
    }

    private static AgendaAfspraak Afspraak(TestDb db, FakeGraphIngang graph, string id, int? project, int beginUur, int minuten, string titel = "Werfoverleg", Action<AgendaAfspraak>? aanpassen = null)
    {
        var a = new AgendaAfspraak { Id = id, Titel = titel, Start = Dinsdag.Date.AddHours(beginUur), Einde = Dinsdag.Date.AddHours(beginUur).AddMinutes(minuten) };
        aanpassen?.Invoke(a);
        graph.Agenda[Gebruiker].Add(a);
        if (project != null) db.OutlookAfspraak.Add(new OutlookAfspraak { UserId = 1, EventId = id, ProjectId = project.Value, GekoppeldOp = DateTime.UtcNow });
        db.SaveChanges();
        return a;
    }

    private static Task<UrenOverzicht> Overzicht(OutlookUrenService s) => s.OverzichtAsync(1, "u1", Gebruiker, Maandag, Zichtbaar);

    [Theory]
    [InlineData(50, 45)]
    [InlineData(55, 60)]
    [InlineData(60, 60)]
    [InlineData(5, 15)]
    [InlineData(100, 105)]
    public async Task De_duur_wordt_afgerond_op_15_minuten_met_een_minimum_van_15(int minuten, int verwacht)
    {
        var (uren, db, graph) = Bouw();
        Afspraak(db, graph, "a", 1, 9, minuten);

        Assert.Equal(verwacht, Assert.Single((await Overzicht(uren)).Voorstellen).Minuten);
    }

    [Fact]
    public async Task Enkel_gekoppelde_afspraken_op_regie_of_gemengde_projecten_krijgen_een_voorstel()
    {
        var (uren, db, graph) = Bouw();
        Afspraak(db, graph, "regie", 1, 9, 60);
        Afspraak(db, graph, "gemengd", 2, 10, 60);
        Afspraak(db, graph, "schijven", 3, 11, 60);
        Afspraak(db, graph, "ongekoppeld", null, 12, 60);

        Assert.Equal(new[] { "regie", "gemengd" }, (await Overzicht(uren)).Voorstellen.Select(v => v.EventId));
    }

    [Fact]
    public async Task Hele_dag_vrij_prive_en_afgewezen_krijgen_geen_voorstel()
    {
        var (uren, db, graph) = Bouw();
        Afspraak(db, graph, "heledag", 1, 0, 1440, aanpassen: a => a.HeleDag = true);
        Afspraak(db, graph, "vrij", 1, 9, 60, aanpassen: a => a.ToonAls = "free");
        Afspraak(db, graph, "prive", 1, 10, 60, aanpassen: a => a.Prive = true);
        Afspraak(db, graph, "afgewezen", 1, 11, 60, aanpassen: a => a.Afgewezen = true);
        Afspraak(db, graph, "goed", 1, 13, 60);

        Assert.Equal(new[] { "goed" }, (await Overzicht(uren)).Voorstellen.Select(v => v.EventId));
    }

    [Fact]
    public async Task Overlappende_afspraken_krijgen_elk_een_waarschuwing_maar_geen_dubbel_voorstel()
    {
        var (uren, db, graph) = Bouw();
        Afspraak(db, graph, "a", 1, 9, 120, "Overleg A");
        Afspraak(db, graph, "b", 1, 10, 60, "Overleg B");
        Afspraak(db, graph, "c", 1, 14, 60, "Los");

        var lijst = (await Overzicht(uren)).Voorstellen;

        Assert.Equal(new (string, string?)[] { ("a", "Overleg B"), ("b", "Overleg A"), ("c", null) }, lijst.Select(v => (v.EventId, v.Overlap)));
    }

    [Fact]
    public async Task Zonder_tarief_komt_er_een_waarschuwing_maar_goedkeuren_blijft_kunnen()
    {
        var (uren, db, graph) = Bouw();
        Afspraak(db, graph, "a", 1, 9, 60);
        Afspraak(db, graph, "b", 2, 11, 60);

        var lijst = (await Overzicht(uren)).Voorstellen;

        Assert.Equal(new[] { false, true }, lijst.Select(v => v.GeenTarief));
        Assert.Equal(2, await uren.KeurGoedAsync(1, "u1", Gebruiker, Maandag, Zichtbaar, new[] { "a", "b" }));
    }

    [Fact]
    public async Task Goedkeuren_maakt_een_regie_uur_met_event_id_en_nooit_twee_keer_voor_dezelfde_afspraak()
    {
        var (uren, db, graph) = Bouw();
        Afspraak(db, graph, "a", 1, 9, 50, "Werfbezoek Villa");

        Assert.Equal(1, await uren.KeurGoedAsync(1, "u1", Gebruiker, Maandag, Zichtbaar, new[] { "a" }));
        Assert.Equal(0, await uren.KeurGoedAsync(1, "u1", Gebruiker, Maandag, Zichtbaar, new[] { "a" }));

        var uur = db.ProjectRegieUur.Single();
        Assert.Equal((1, "u1", new DateOnly(2026, 11, 3), 0.75m, "Werfbezoek Villa", "a", (int?)null), (uur.ProjectId, uur.UserId, uur.Date, uur.Hours, uur.Description, uur.OutlookEventId, uur.InvoiceId));
        var rij = Assert.Single((await Overzicht(uren)).Voorstellen);
        Assert.True(rij.Goedgekeurd);
    }

    [Fact]
    public async Task Goedkeuren_telt_enkel_wat_de_server_zelf_als_voorstel_ziet()
    {
        var (uren, db, graph) = Bouw();
        Afspraak(db, graph, "prive", 1, 9, 60, aanpassen: a => a.Prive = true);
        Afspraak(db, graph, "schijven", 3, 10, 60);

        Assert.Equal(0, await uren.KeurGoedAsync(1, "u1", Gebruiker, Maandag, Zichtbaar, new[] { "prive", "schijven", "bestaat-niet" }));
        Assert.Empty(db.ProjectRegieUur);
    }

    [Fact]
    public async Task Een_project_buiten_bereik_levert_geen_voorstel()
    {
        var (uren, db, graph) = Bouw();
        Afspraak(db, graph, "a", 1, 9, 60);

        var lijst = await uren.OverzichtAsync(1, "u1", Gebruiker, Maandag, new[] { 2 });

        Assert.Empty(lijst.Voorstellen);
    }

    [Fact]
    public async Task Een_gewijzigde_afspraak_geeft_een_melding_en_bijwerken_zet_het_uur_op_de_nieuwe_duur()
    {
        var (uren, db, graph) = Bouw();
        var a = Afspraak(db, graph, "a", 1, 9, 60);
        await uren.KeurGoedAsync(1, "u1", Gebruiker, Maandag, Zichtbaar, new[] { "a" });

        a.Einde = a.Start.AddMinutes(90);
        var rij = Assert.Single((await Overzicht(uren)).Voorstellen);
        Assert.Equal((true, 90), (rij.Gewijzigd != null, rij.NieuweMinuten ?? 0));

        Assert.Null(await uren.BijwerkenAsync(1, "u1", Gebruiker, rij.UurId!.Value, Zichtbaar));
        Assert.Equal(1.5m, db.ProjectRegieUur.Single().Hours);
        Assert.Null(Assert.Single((await Overzicht(uren)).Voorstellen).Gewijzigd);
    }

    [Fact]
    public async Task Een_gefactureerd_uur_blijft_ongewijzigd_ook_als_de_afspraak_verandert()
    {
        var (uren, db, graph) = Bouw();
        var a = Afspraak(db, graph, "a", 1, 9, 60);
        await uren.KeurGoedAsync(1, "u1", Gebruiker, Maandag, Zichtbaar, new[] { "a" });
        var uur = db.ProjectRegieUur.Single();
        db.Invoices.Add(new Invoices { Id = 1, PublicId = Guid.NewGuid().ToString() });
        uur.InvoiceId = 1;
        db.SaveChanges();

        a.Einde = a.Start.AddMinutes(120);
        var rij = Assert.Single((await Overzicht(uren)).Voorstellen);

        Assert.True(rij.Gefactureerd);
        Assert.Null(rij.Gewijzigd);
        Assert.NotNull(await uren.BijwerkenAsync(1, "u1", Gebruiker, uur.Id, Zichtbaar));
        Assert.Equal(1m, db.ProjectRegieUur.Single().Hours);
    }

    [Fact]
    public async Task Een_verdwenen_afspraak_geeft_een_melding_bij_een_ongefactureerd_uur()
    {
        var (uren, db, graph) = Bouw();
        var a = Afspraak(db, graph, "a", 1, 9, 60);
        await uren.KeurGoedAsync(1, "u1", Gebruiker, Maandag, Zichtbaar, new[] { "a" });

        graph.Agenda[Gebruiker].Remove(a);
        var rij = Assert.Single((await Overzicht(uren)).Voorstellen);

        Assert.NotNull(rij.Gewijzigd);
        Assert.Null(rij.NieuweMinuten);
    }

    [Fact]
    public async Task Zonder_werkende_agenda_is_er_een_foutmelding_en_geen_voorstel()
    {
        var (uren, _, graph) = Bouw();
        graph.Fout = "Geen agenda.";

        var lijst = await Overzicht(uren);

        Assert.Empty(lijst.Voorstellen);
        Assert.NotNull(lijst.Fout);
    }
}
