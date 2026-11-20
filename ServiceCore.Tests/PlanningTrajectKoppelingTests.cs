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
}
