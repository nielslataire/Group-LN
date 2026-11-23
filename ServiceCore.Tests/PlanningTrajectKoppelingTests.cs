using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Trajectmijlpalen met dezelfde code als een taak of fase uit het planningssjabloon hangen er automatisch aan.</summary>
public class PlanningTrajectKoppelingTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1;

    private static TestDb Maak(params (string? Code, int? Binding, int? RefId)[] mijlpalen)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling" });
        db.PlanningSectie.Add(new PlanningSectie { Id = 10, ProjectId = ProjectId, Naam = "Ruwbouw", IsActief = true, Code = "RUWBOUW" });
        db.PlanningTaak.Add(new PlanningTaak { Id = 20, PlanningSectieId = 10, Naam = "Dak plaatsen", Code = "DAK_KLAAR" });
        db.Projecttraject.Add(new Projecttraject { Id = 1, ProjectId = ProjectId, Naam = "Traject" });
        var i = 100;
        foreach (var (code, binding, refId) in mijlpalen)
            db.Mijlpaal.Add(new Mijlpaal { Id = i++, ProjecttrajectId = 1, Code = code, Naam = "M" + i, BronBinding = binding, BronRefId = refId });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Een_mijlpaal_met_dezelfde_code_hangt_aan_de_taak_of_de_fase()
    {
        var db = Maak(("dak_klaar", null, null), ("RUWBOUW", null, null), ("ANDERS", null, null));
        var n = await PlanningTrajectKoppeling.KoppelOpCode(db, ProjectId);
        Assert.Equal(2, n);
        var taak = db.Mijlpaal.Single(m => m.Id == 100);
        Assert.Equal(4, taak.BronBinding); Assert.Equal(20, taak.BronRefId);
        var fase = db.Mijlpaal.Single(m => m.Id == 101);
        Assert.Equal(5, fase.BronBinding); Assert.Equal(10, fase.BronRefId);
        Assert.Null(db.Mijlpaal.Single(m => m.Id == 102).BronBinding);   // code komt in de planning niet voor
    }

    [Fact]
    public async Task Een_mijlpaal_die_al_aan_iets_hangt_blijft_ongemoeid_en_een_dubbele_code_wordt_overgeslagen()
    {
        var db = Maak(("DAK_KLAAR", 4, 99), ("DAK_KLAAR", 7, null));      // 1: hangt al aan taak 99; 2: haalt zijn datum elders vandaan
        db.PlanningTaak.Add(new PlanningTaak { Id = 21, PlanningSectieId = 10, Naam = "Dak in woning 2", Code = "DAK_KLAAR" });
        db.SaveChanges();
        Assert.Equal(0, await PlanningTrajectKoppeling.KoppelOpCode(db, ProjectId));
        Assert.Equal(99, db.Mijlpaal.Single(m => m.Id == 100).BronRefId);
        Assert.Equal(7, db.Mijlpaal.Single(m => m.Id == 101).BronBinding);

        // dubbele code in de planning (bv. twee woningen): niet te zeggen welke bedoeld is
        var db2 = Maak(("DAK_KLAAR", null, null));
        db2.PlanningTaak.Add(new PlanningTaak { Id = 21, PlanningSectieId = 10, Naam = "Dak in woning 2", Code = "DAK_KLAAR" });
        db2.SaveChanges();
        Assert.Equal(0, await PlanningTrajectKoppeling.KoppelOpCode(db2, ProjectId));
        Assert.Null(db2.Mijlpaal.Single().BronBinding);
    }

    // ── mijlpalen per eenheid: elke woning heeft een eigen planning met dezelfde codes ──

    private static TestDb MaakVerkaveling()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling" });
        db.Projecttraject.Add(new Projecttraject { Id = 1, ProjectId = ProjectId, Naam = "Traject" });
        for (var w = 1; w <= 3; w++)
        {
            db.Units.Add(new Units { Id = w, ProjectId = ProjectId, Name = "Woning " + w });
            db.BouwPlanning.Add(new BouwPlanning { Id = w, ProjectId = ProjectId, Naam = "Woning " + w, IsActief = true });
            db.BouwPlanningEenheid.Add(new BouwPlanningEenheid { Id = w, BouwPlanningId = w, UnitId = w });
            db.PlanningSectie.Add(new PlanningSectie { Id = 10 * w, ProjectId = ProjectId, Naam = "Ruwbouw " + w, IsActief = true, BouwPlanningId = w });
            db.PlanningTaak.Add(new PlanningTaak { Id = 100 * w, PlanningSectieId = 10 * w, Naam = "Ruwbouw klaar", Code = "RUWBOUW_KLAAR" });
            db.Mijlpaal.Add(new Mijlpaal { Id = 1000 + w, ProjecttrajectId = 1, UnitId = w, Code = "RUWBOUW_KLAAR", Naam = "Ruwbouw klaar woning " + w });
        }
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Een_mijlpaal_per_woning_hangt_aan_de_taak_in_de_planning_van_die_woning()
    {
        var db = MaakVerkaveling();
        Assert.Equal(3, await PlanningTrajectKoppeling.KoppelOpCode(db, ProjectId));
        for (var w = 1; w <= 3; w++)
        {
            var m = db.Mijlpaal.Single(x => x.Id == 1000 + w);
            Assert.Equal(4, m.BronBinding);
            Assert.Equal(100 * w, m.BronRefId);
        }
    }

    [Fact]
    public async Task Een_mijlpaal_van_het_hele_project_blijft_los_als_de_code_in_meerdere_woningen_staat()
    {
        var db = MaakVerkaveling();
        db.Mijlpaal.Add(new Mijlpaal { Id = 2000, ProjecttrajectId = 1, UnitId = null, Code = "RUWBOUW_KLAAR", Naam = "Ruwbouw project" });
        db.SaveChanges();
        await PlanningTrajectKoppeling.KoppelOpCode(db, ProjectId);
        Assert.Null(db.Mijlpaal.Single(x => x.Id == 2000).BronBinding);
        Assert.Equal(3, db.Mijlpaal.Count(x => x.UnitId != null && x.BronBinding == 4));
    }

    [Fact]
    public async Task Een_woning_zonder_planning_blijft_los_zolang_de_code_niet_eenduidig_is()
    {
        var db = MaakVerkaveling();
        db.Units.Add(new Units { Id = 4, ProjectId = ProjectId, Name = "Woning 4" });
        db.Mijlpaal.Add(new Mijlpaal { Id = 1004, ProjecttrajectId = 1, UnitId = 4, Code = "RUWBOUW_KLAAR", Naam = "Ruwbouw klaar woning 4" });
        db.SaveChanges();
        await PlanningTrajectKoppeling.KoppelOpCode(db, ProjectId);
        Assert.Null(db.Mijlpaal.Single(x => x.Id == 1004).BronBinding);
        Assert.Equal(300, db.Mijlpaal.Single(x => x.Id == 1003).BronRefId);
    }

    [Fact]
    public async Task Alle_appartementen_van_een_gebouw_volgen_dezelfde_taak()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Residentie" });
        db.Projecttraject.Add(new Projecttraject { Id = 1, ProjectId = ProjectId, Naam = "Traject" });
        db.BouwPlanning.Add(new BouwPlanning { Id = 1, ProjectId = ProjectId, Naam = "Gebouw A", IsActief = true });
        db.PlanningSectie.Add(new PlanningSectie { Id = 10, ProjectId = ProjectId, Naam = "Ruwbouw", IsActief = true, BouwPlanningId = 1 });
        db.PlanningTaak.Add(new PlanningTaak { Id = 100, PlanningSectieId = 10, Naam = "Ruwbouw klaar", Code = "RUWBOUW_KLAAR" });
        for (var a = 1; a <= 4; a++)
        {
            db.Units.Add(new Units { Id = a, ProjectId = ProjectId, Name = "App " + a });
            db.BouwPlanningEenheid.Add(new BouwPlanningEenheid { Id = a, BouwPlanningId = 1, UnitId = a });
            db.Mijlpaal.Add(new Mijlpaal { Id = 1000 + a, ProjecttrajectId = 1, UnitId = a, Code = "RUWBOUW_KLAAR", Naam = "Ruwbouw app " + a });
        }
        db.SaveChanges();
        Assert.Equal(4, await PlanningTrajectKoppeling.KoppelOpCode(db, ProjectId));
        Assert.All(db.Mijlpaal.ToList(), m => Assert.Equal(100, m.BronRefId));
    }
}
