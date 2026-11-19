using System.Text.Json;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Betalingsschijven gekoppeld aan taken en fases per planning (migratie 151), met "schijf bereikt" voor de eenheden van die planning.</summary>
public class PlanningSchijfTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1;
    private const int Stage1 = 101, Stage2 = 102;

    /// <summary>Eén groep met twee schijven en drie eenheden: 11 en 12 horen bij de planning "Woning 1+2", 13 blijft in de hoofdplanning.</summary>
    private static async Task<(PlanningService Svc, TestDb Db, PlanningStaat Staat, int PlanningId, int TaakId)> Maak(bool eenheid12Verkocht = true)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling", StartDateConstruction = new DateOnly(2026, 11, 16) });
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 5, Name = "Woningen", ProjectId = ProjectId });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = Stage1, GroupId = 5, Name = "Fundering", Percentage = 30 });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = Stage2, GroupId = 5, Name = "Ruwbouw", Percentage = 70 });
        foreach (var id in new[] { 11, 12, 13 })
        {
            var verkocht = id != 12 || eenheid12Verkocht;
            db.Units.Add(new Units { Id = id, Name = "Woning " + id, ProjectId = ProjectId, PaymentGroupId = 5, ClientAccountId = verkocht ? 900 + id : null });
            db.UnitConstructionValue.Add(new UnitConstructionValue { UnitId = id, PaymentGroupId = 5, ValueSold = 100000 });
        }
        db.SaveChanges();
        var svc = new PlanningService(db);

        var s = await Doe(svc, null, new PlanningCommando { Type = "taakToevoegen", PlanningId = 0, Velden = V(("naam", "Wegenis"), ("duur", 3)) });
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = V(("naam", "Woning 1+2"), ("eenheden", new[] { 11, 12 })) }, "u1");
        Assert.True(u.Ok, u.Fout);
        var planningId = u.NieuwePlanningId!.Value;
        s = await Doe(svc, u.Staat, new PlanningCommando { Type = "taakToevoegen", PlanningId = planningId, Velden = V(("naam", "Fundering storten"), ("duur", 4)) });
        s = (await svc.Laad(ProjectId, planningId));
        var taak = s.Rijen.First(r => r.Naam == "Fundering storten");
        return (svc, db, s, planningId, taak.Id);
    }

    private static Dictionary<string, JsonElement> V(params (string Naam, object? Waarde)[] velden)
        => velden.ToDictionary(v => v.Naam, v => JsonSerializer.SerializeToElement(v.Waarde));

    private static async Task<PlanningStaat> Doe(PlanningService svc, PlanningStaat? vorige, PlanningCommando c)
    {
        c.Versie = vorige?.Versie ?? 0;
        var u = await svc.Voer(ProjectId, c, "u1");
        Assert.True(u.Ok, u.Fout);
        return u.Staat!;
    }

    [Fact]
    public async Task Een_schijf_koppelen_toont_ze_bij_de_taak_met_het_aantal_eenheden_van_de_planning()
    {
        var (svc, _, _, pid, taakId) = await Maak();
        var u = await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = taakId, PlanningId = pid, StageId = Stage1 }, "u1");
        Assert.True(u.Ok, u.Fout);
        var rij = u.Staat!.Rijen.First(r => r.Sleutel == "t" + taakId);
        var link = Assert.Single(rij.Schijven);
        Assert.Equal(Stage1, link.StageId);
        Assert.Equal(2, link.AantalEenheden);          // enkel de eenheden van deze planning, niet eenheid 13
        Assert.Equal(0, link.AantalBereikt);
        Assert.False(link.KlaarOmTeMarkeren);          // de taak is nog niet klaar
        Assert.Equal(2, u.Staat.SchijfOpties.Count);
        Assert.Equal("t" + taakId, u.Staat.SchijfOpties.First(o => o.StageId == Stage1).GekoppeldAanSleutel);
    }

    [Fact]
    public async Task Is_de_taak_klaar_dan_kan_de_schijf_voor_de_eenheden_van_de_planning_gemarkeerd_worden()
    {
        var (svc, db, s, pid, taakId) = await Maak();
        await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = taakId, PlanningId = pid, StageId = Stage1 }, "u1");
        s = await Doe(svc, s, new PlanningCommando { Type = "wijzig", PlanningId = pid, Soort = "taak", Id = taakId, Velden = V(("voortgang", 100)) });
        var link = s.Rijen.First(r => r.Sleutel == "t" + taakId).Schijven.Single();
        Assert.True(link.KlaarOmTeMarkeren);
        Assert.Equal(2, link.TeMarkeren);

        var u = await svc.MarkeerSchijf(ProjectId, new PlanningSchijfMarkeerCommando { PlanningId = pid, StageId = Stage1 }, "7");
        Assert.True(u.Ok, u.Fout);
        var na = u.Staat!.Rijen.First(r => r.Sleutel == "t" + taakId).Schijven.Single();
        Assert.True(na.AllesBereikt);
        Assert.False(na.KlaarOmTeMarkeren);
        var gemarkeerd = db.UnitPaymentStageReached.Where(r => r.PaymentStageId == Stage1).Select(r => r.UnitId).OrderBy(x => x).ToList();
        Assert.Equal(new[] { 11, 12 }, gemarkeerd);    // eenheid 13 zit in een andere planning en blijft ongemoeid
        Assert.Equal(7, db.UnitPaymentStageReached.First().ReachedByUserId);
    }

    [Fact]
    public async Task Een_niet_verkochte_eenheid_wordt_overgeslagen()
    {
        var (svc, db, s, pid, taakId) = await Maak(eenheid12Verkocht: false);
        await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = taakId, PlanningId = pid, StageId = Stage1 }, "u1");
        s = await Doe(svc, s, new PlanningCommando { Type = "wijzig", PlanningId = pid, Soort = "taak", Id = taakId, Velden = V(("voortgang", 100)) });
        var link = s.Rijen.First(r => r.Sleutel == "t" + taakId).Schijven.Single();
        Assert.Equal(1, link.TeMarkeren);
        Assert.Equal(1, link.Overgeslagen);
        await svc.MarkeerSchijf(ProjectId, new PlanningSchijfMarkeerCommando { PlanningId = pid, StageId = Stage1 }, "u1");
        Assert.Equal(new[] { 11 }, db.UnitPaymentStageReached.Select(r => r.UnitId).ToArray());
    }

    [Fact]
    public async Task Terugzetten_laat_gefactureerde_eenheden_staan()
    {
        var (svc, db, _, pid, taakId) = await Maak();
        await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = taakId, PlanningId = pid, StageId = Stage1 }, "u1");
        await svc.MarkeerSchijf(ProjectId, new PlanningSchijfMarkeerCommando { PlanningId = pid, StageId = Stage1 }, "u1");
        db.InvoicesDetails.Add(new InvoicesDetails { LineType = "Stages", PaymentStageId = Stage1, UnitId = 11 });
        db.SaveChanges();

        var u = await svc.MarkeerSchijf(ProjectId, new PlanningSchijfMarkeerCommando { PlanningId = pid, StageId = Stage1, Terugzetten = true }, "u1");
        Assert.True(u.Ok, u.Fout);
        Assert.Equal(new[] { 11 }, db.UnitPaymentStageReached.Select(r => r.UnitId).ToArray());   // 12 teruggezet, 11 is gefactureerd
    }

    [Fact]
    public async Task Een_schijf_hangt_per_planning_aan_een_plek_en_ontkoppelen_werkt()
    {
        var (svc, _, s, pid, taakId) = await Maak();
        s = await Doe(svc, s, new PlanningCommando { Type = "taakToevoegen", PlanningId = pid, Velden = V(("naam", "Ruwbouw"), ("duur", 10)) });
        var tweede = (await svc.Laad(ProjectId, pid)).Rijen.First(r => r.Naam == "Ruwbouw").Id;
        await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = taakId, PlanningId = pid, StageId = Stage2 }, "u1");
        var u = await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = tweede, PlanningId = pid, StageId = Stage2 }, "u1");
        Assert.Empty(u.Staat!.Rijen.First(r => r.Sleutel == "t" + taakId).Schijven);                 // verplaatst naar de tweede taak
        Assert.Single(u.Staat.Rijen.First(r => r.Sleutel == "t" + tweede).Schijven);

        u = await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = tweede, PlanningId = pid, StageId = null, VorigeStageId = Stage2 }, "u1");
        Assert.Empty(u.Staat!.Rijen.SelectMany(r => r.Schijven));
    }

    [Fact]
    public async Task De_hoofdplanning_kan_eigen_eenheden_hebben_en_de_groep_komt_uit_de_constructiewaarde()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling", StartDateConstruction = new DateOnly(2026, 11, 16) });
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 5, Name = "Woningen", ProjectId = ProjectId });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = Stage1, GroupId = 5, Name = "Fundering", Percentage = 100 });
        // Alleen de constructiewaarde legt de groep vast (Units.PaymentGroupId blijft leeg, zoals bij de V2-pagina)
        foreach (var id in new[] { 21, 22, 23 })
        {
            db.Units.Add(new Units { Id = id, Name = "Lot " + id, ProjectId = ProjectId, ClientAccountId = 900 + id });
            db.UnitConstructionValue.Add(new UnitConstructionValue { UnitId = id, PaymentGroupId = id == 23 ? null : 5, ValueSold = 100000 });
        }
        db.SaveChanges();
        var svc = new PlanningService(db);
        var s = await Doe(svc, null, new PlanningCommando { Type = "taakToevoegen", PlanningId = 0, Velden = V(("naam", "Ruwbouw"), ("duur", 3)) });
        var hoofd = s.Planningen.First(p => p.IsHoofd).Id;
        var taak = s.Rijen.First(r => r.Naam == "Ruwbouw").Id;

        // Zonder eigen eenheden geldt de hoofdplanning voor alle eenheden zonder planning
        var u = await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = taak, PlanningId = hoofd, StageId = Stage1 }, "u1");
        Assert.True(u.Ok, u.Fout);
        Assert.Equal(2, u.Staat!.Rijen.First(r => r.Sleutel == "t" + taak).Schijven.Single().AantalEenheden);

        // Met eigen eenheden enkel die
        var w = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningWijzig", PlanningId = hoofd, Versie = u.Staat.Versie, Velden = V(("naam", "Hoofdplanning"), ("eenheden", new[] { 21 })) }, "u1");
        Assert.True(w.Ok, w.Fout);
        Assert.Equal(1, w.Staat!.Rijen.First(r => r.Sleutel == "t" + taak).Schijven.Single().AantalEenheden);
    }

    [Fact]
    public async Task Een_schijf_van_een_groep_zonder_eenheden_in_de_planning_wordt_geweigerd()
    {
        var (svc, db, _, pid, taakId) = await Maak();
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 6, Name = "Andere", ProjectId = ProjectId });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = 201, GroupId = 6, Name = "X", Percentage = 100 });
        db.SaveChanges();
        var u = await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = taakId, PlanningId = pid, StageId = 201 }, "u1");
        Assert.False(u.Ok);
        Assert.Contains("hoort niet bij", u.Fout);
    }
}

