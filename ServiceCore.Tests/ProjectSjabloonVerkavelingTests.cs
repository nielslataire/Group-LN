using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Projectsjabloon voor een verkaveling (migratie 164): de planning van het project zelf (wegenis) komt in de hoofdplanning en de woningen starten na een gekozen bouwstaat.</summary>
public class ProjectSjabloonVerkavelingTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private sealed class TrajectDubbel : ITrajectInstantiationService
    {
        public Task<Projecttraject> Instantiate(BOCore.TrajectInstantiatieBO dto, string? userId) => throw new NotSupportedException();
        public Task<int> SyncMissing(int projecttrajectId, string? userId) => Task.FromResult(0);
        public Task<TrajectSyncPreview?> PreviewSync(int projecttrajectId) => Task.FromResult<TrajectSyncPreview?>(null);
        public Task<int> ApplySync(int projecttrajectId, IReadOnlyCollection<string> keys, string? userId) => Task.FromResult(0);
    }

    private const int ProjectId = 1;

    private static (ProjectSjabloonService Svc, TestDb Db, int SjabloonId) Maak(string? startNa, bool hoofdBezet = false)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Wilgenhoek", StartDateConstruction = new DateOnly(2026, 11, 16) });   // een maandag
        db.Bouwstaat.Add(new Bouwstaat { Code = "WEGENIS_GRONDWERKEN", Naam = "Grondwerken", IsActief = true });
        db.Bouwstaat.Add(new Bouwstaat { Code = "WEGENIS_VERHARDING", Naam = "Verharding", IsActief = true });
        db.SaveChanges();
        var verharding = db.Bouwstaat.Single(b => b.Code == "WEGENIS_VERHARDING").Id;

        var weg = new PlanningSjabloon { Naam = "Verkaveling wegenis", IsActief = true, CreatedDate = DateTime.UtcNow };
        var wb = new PlanningSjabloonBlok { Code = "W_WEGENIS", Naam = "Wegenis", Volgorde = 10 };
        var grond = new PlanningSjabloonTaak { Naam = "Grondwerken", Code = "W_GRONDWERKEN", Duur = 5, Volgorde = 10 };
        var verh = new PlanningSjabloonTaak { Naam = "Verharding", Code = "W_VERHARDING", Duur = 5, Volgorde = 20, BouwstaatId = verharding };
        verh.Voorgangers.Add(new PlanningSjabloonVoorganger { VoorgangerCode = "W_GRONDWERKEN", Type = 0, WachtDagen = 0 });
        wb.Taken.Add(grond); wb.Taken.Add(verh);
        weg.Blokken.Add(wb);

        var wo = new PlanningSjabloon { Naam = "Woning", IsActief = true, CreatedDate = DateTime.UtcNow };
        var ob = new PlanningSjabloonBlok { Code = "B_RUWBOUW", Naam = "Ruwbouw", Volgorde = 10 };
        ob.Taken.Add(new PlanningSjabloonTaak { Naam = "Fundering", Code = "FUNDERING", Duur = 8, Volgorde = 10 });
        wo.Blokken.Add(ob);
        db.PlanningSjabloon.AddRange(weg, wo);
        db.SaveChanges();
        db.ProjectSjabloon.Add(new ProjectSjabloon
        {
            Naam = "Verkaveling met wegenis", IsActief = true, CreatedDate = DateTime.UtcNow,
            PlanningSjabloonProjectId = weg.Id, PlanningSjabloonId = wo.Id, WoningenStartNa = startNa,
        });
        db.Units.Add(new Units { Id = 11, Name = "Woning 1", ProjectId = ProjectId });
        db.Units.Add(new Units { Id = 12, Name = "Woning 2", ProjectId = ProjectId });
        db.SaveChanges();
        var svc = new ProjectSjabloonService(db, new PlanningService(db), new TrajectDubbel(), new SchijfSjabloonService(db));
        return (svc, db, db.ProjectSjabloon.Single().Id);
    }

    [Fact]
    public async Task De_wegenisplanning_komt_in_de_hoofdplanning_en_de_woningen_starten_de_werkdag_na_de_verharding()
    {
        var (svc, db, id) = Maak("WEGENIS_VERHARDING");
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u");
        Assert.True(r.Ok, r.Fout);
        Assert.True(r.Stappen.Any(s => s.Contains("toegepast in de hoofdplanning")), string.Join(" / ", r.Stappen.Concat(r.Opmerkingen)));

        var hoofd = db.BouwPlanning.Single(b => b.IsHoofd);
        Assert.Equal(2, db.PlanningTaak.Count(t => t.PlanningSectie.BouwPlanningId == hoofd.Id));

        // grondwerken 16-20 nov, verharding 23-27 nov: de woningen starten maandag 30 november
        var woningen = db.BouwPlanning.Where(b => !b.IsHoofd).ToList();
        Assert.Equal(2, woningen.Count);
        Assert.All(woningen, b => Assert.Equal(new DateOnly(2026, 11, 30), b.StartDatum));
    }

    [Fact]
    public async Task Zonder_startmoment_starten_de_woningen_op_de_aanvangsdatum_van_de_bouwwerken()
    {
        var (svc, db, id) = Maak(null);
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u");
        Assert.True(r.Ok, r.Fout);
        Assert.All(db.BouwPlanning.Where(b => !b.IsHoofd), b => Assert.Null(b.StartDatum));
    }

    [Fact]
    public async Task Het_voorbeeld_bewaart_niets()
    {
        var (svc, db, id) = Maak("WEGENIS_VERHARDING");
        var r = await svc.Voorbeeld(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 });
        Assert.True(r.Ok, r.Fout);
        Assert.Contains(r.Stappen, s => s.Contains("hoofdplanning"));
        Assert.Empty(db.PlanningTaak);
    }

    [Fact]
    public async Task Een_bouwstaat_die_geen_taak_oplevert_geeft_een_opmerking_en_de_woningen_starten_gewoon()
    {
        var (svc, db, id) = Maak("WEGENIS_GRONDWERKEN");
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u");
        Assert.True(r.Ok, r.Fout);
        Assert.Contains(r.Opmerkingen, o => o.Contains("Geen taak"));
        Assert.All(db.BouwPlanning.Where(b => !b.IsHoofd), b => Assert.Null(b.StartDatum));
    }
}
