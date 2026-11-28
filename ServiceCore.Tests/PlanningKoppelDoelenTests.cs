using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>De keuzelijst "Volgt de planning" in het mijlpaalformulier van het Traject (C4): fases en taken van de planning, per eenheid beperkt tot haar planning.</summary>
public class PlanningKoppelDoelenTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1;

    private static (PlanningService Svc, TestDb Db, int HoofdTaak, int WoningTaak) Maak()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling" });
        db.Units.Add(new Units { Id = 11, Name = "Woning 1", ProjectId = ProjectId });
        db.Units.Add(new Units { Id = 12, Name = "Woning 2", ProjectId = ProjectId });
        var hoofd = new BouwPlanning { ProjectId = ProjectId, Naam = "Hoofdplanning", IsHoofd = true, IsActief = true, Volgorde = 0 };
        var w1 = new BouwPlanning { ProjectId = ProjectId, Naam = "Woning 1", IsHoofd = false, IsActief = true, Volgorde = 10 };
        db.BouwPlanning.AddRange(hoofd, w1); db.SaveChanges();
        db.BouwPlanningEenheid.Add(new BouwPlanningEenheid { BouwPlanningId = w1.Id, UnitId = 11 });
        var s1 = new PlanningSectie { ProjectId = ProjectId, Naam = "Wegenis", Volgorde = 10, IsActief = true, BouwPlanningId = hoofd.Id };
        var s2 = new PlanningSectie { ProjectId = ProjectId, Naam = "Ruwbouw", Volgorde = 10, IsActief = true, BouwPlanningId = w1.Id };
        db.PlanningSectie.AddRange(s1, s2); db.SaveChanges();
        var t1 = new PlanningTaak { PlanningSectieId = s1.Id, Naam = "Verharding", Duur = 5, Volgorde = 10 };
        var t2 = new PlanningTaak { PlanningSectieId = s2.Id, Naam = "Vloerplaat", Duur = 3, Volgorde = 10 };
        db.PlanningTaak.AddRange(t1, t2); db.SaveChanges();
        return (new PlanningService(db), db, t1.Id, t2.Id);
    }

    [Fact]
    public async Task Een_mijlpaal_van_het_hele_project_ziet_alle_fases_en_taken_met_de_naam_van_de_planning()
    {
        var (svc, _, hoofdTaak, woningTaak) = Maak();
        var doelen = await svc.KoppelDoelen(ProjectId, null);
        Assert.Equal(new[] { "Hoofdplanning · Fase · Wegenis", "Hoofdplanning · Wegenis · Verharding", "Woning 1 · Fase · Ruwbouw", "Woning 1 · Ruwbouw · Vloerplaat" }, doelen.Select(d => d.Tekst));
        Assert.Contains(doelen, d => d.Waarde == "taak:" + hoofdTaak);
        Assert.Contains(doelen, d => d.Waarde == "taak:" + woningTaak);
    }

    [Fact]
    public async Task Een_mijlpaal_van_een_woning_ziet_enkel_de_planning_van_die_woning()
    {
        var (svc, _, _, woningTaak) = Maak();
        var doelen = await svc.KoppelDoelen(ProjectId, 11);
        Assert.All(doelen, d => Assert.StartsWith("Woning 1", d.Tekst));
        Assert.Contains(doelen, d => d.Waarde == "taak:" + woningTaak);
        // een woning zonder eigen planning mag overal aan hangen
        Assert.Equal(4, (await svc.KoppelDoelen(ProjectId, 12)).Count);
    }

    [Fact]
    public async Task Een_project_zonder_planning_geeft_geen_doelen()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        Assert.Empty(await new PlanningService(db).KoppelDoelen(ProjectId, null));
    }
}
