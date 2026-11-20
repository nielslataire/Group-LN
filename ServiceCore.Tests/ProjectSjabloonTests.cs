using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Projectsjablonen: beheer en toepassen (vult enkel aan wat nog leeg is).</summary>
public class ProjectSjabloonTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    /// <summary>Maakt enkel een leeg traject aan; de echte opbouw uit een sjabloon is getest bij de trajecttests.</summary>
    private sealed class TrajectDubbel : ITrajectInstantiationService
    {
        private readonly cpmRunningContext _db;
        public TrajectDubbel(cpmRunningContext db) { _db = db; }
        public async Task<Projecttraject> Instantiate(BOCore.TrajectInstantiatieBO dto, string? userId)
        {
            var t = new Projecttraject { ProjectId = dto.ProjectId, Naam = "Traject", TrajectSjabloonId = dto.TrajectSjabloonId };
            _db.Projecttraject.Add(t);
            await _db.SaveChangesAsync();
            return t;
        }
        public Task<int> SyncMissing(int projecttrajectId, string? userId) => Task.FromResult(0);
        public Task<TrajectSyncPreview?> PreviewSync(int projecttrajectId) => Task.FromResult<TrajectSyncPreview?>(null);
        public Task<int> ApplySync(int projecttrajectId, IReadOnlyCollection<string> keys, string? userId) => Task.FromResult(0);
    }

    private const int ProjectId = 1;

    private static (ProjectSjabloonService Svc, TestDb Db, int SjabloonId) Maak(bool metTraject = true)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling", StartDateConstruction = new DateOnly(2026, 11, 16) });
        // planningssjabloon met een taak die de code VLOER draagt
        var ps = new PlanningSjabloon { Naam = "Woning", IsActief = true, CreatedDate = DateTime.UtcNow };
        var blok = new PlanningSjabloonBlok { Naam = "Ruwbouw", Volgorde = 10 };
        blok.Taken.Add(new PlanningSjabloonTaak { Naam = "Vloerplaat gieten", Code = "VLOER", Duur = 3, Volgorde = 10 });
        ps.Blokken.Add(blok);
        db.PlanningSjabloon.Add(ps);
        db.TrajectSjabloon.Add(new TrajectSjabloon { Id = 50, Naam = "Standaardtraject", IsActief = true });
        var ss = new SchijfSjabloon { Naam = "Woning schijven", IsActief = true, CreatedDate = DateTime.UtcNow };
        ss.Schijven.Add(new SchijfSjabloonSchijf { Volgorde = 10, Naam = "Bij het gieten van de vloerplaat", PlanningCode = "VLOER", Gewicht = 1 });
        ss.Schijven.Add(new SchijfSjabloonSchijf { Volgorde = 20, Naam = "Bij de voorlopige oplevering", VastPercentage = 5 });
        db.SchijfSjabloon.Add(ss);
        db.SaveChanges();
        db.ProjectSjabloon.Add(new ProjectSjabloon { Naam = "Rijwoning", IsActief = true, CreatedDate = DateTime.UtcNow, PlanningSjabloonId = ps.Id, TrajectSjabloonId = metTraject ? 50 : null, SchijfSjabloonId = ss.Id });
        // twee eenheden zonder planning, één groep met de standaardschijf
        db.Units.Add(new Units { Id = 11, Name = "Woning 1", ProjectId = ProjectId });
        db.Units.Add(new Units { Id = 12, Name = "Woning 2", ProjectId = ProjectId });
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 5, Name = "Woningen", ProjectId = ProjectId });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = 101, GroupId = 5, Name = "Bij de voorlopige oplevering (ontvangst van de sleutels)", Percentage = 100 });
        db.UnitConstructionValue.Add(new UnitConstructionValue { UnitId = 11, PaymentGroupId = 5, ValueSold = 1 });
        db.UnitConstructionValue.Add(new UnitConstructionValue { UnitId = 12, PaymentGroupId = 5, ValueSold = 1 });
        db.SaveChanges();
        var sjabloonId = db.ProjectSjabloon.Single().Id;
        var schijven = new SchijfSjabloonService(db);
        var svc = new ProjectSjabloonService(db, new PlanningService(db), new TrajectDubbel(db), schijven);
        return (svc, db, sjabloonId);
    }

    [Fact]
    public async Task Het_voorbeeld_beschrijft_wat_er_zou_gebeuren_en_bewaart_niets()
    {
        var (svc, db, id) = Maak();
        var r = await svc.Voorbeeld(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 });
        Assert.True(r.Ok, r.Fout);
        Assert.Contains(r.Stappen, s => s.StartsWith("Planning") && s.Contains("Woning 1") && s.Contains("Woning 2"));
        Assert.Contains(r.Stappen, s => s.StartsWith("Traject"));
        Assert.Contains(r.Stappen, s => s.StartsWith("Schijven"));
        Assert.Equal(new[] { 95m, 5m }, r.Schijven.Select(x => x.Percentage).ToArray());
        Assert.Empty(db.BouwPlanning);
        Assert.Empty(db.Projecttraject);
        Assert.Single(db.InvoicingPaymentStages);       // de standaardschijf staat er nog
    }

    [Fact]
    public async Task Toepassen_maakt_planning_per_eenheid_traject_en_schijven_en_hangt_de_schijf_aan_de_taak()
    {
        var (svc, db, id) = Maak();
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u1");
        Assert.True(r.Ok, r.Fout);
        Assert.Equal(2, db.BouwPlanning.Count(b => !b.IsHoofd));
        Assert.Single(db.Projecttraject);
        var stages = db.InvoicingPaymentStages.OrderBy(s => s.Id).ToList();
        Assert.Equal(2, stages.Count);
        Assert.Equal(new[] { 95m, 5m }, stages.Select(s => s.Percentage).ToArray());
        Assert.Equal("VLOER", stages[0].PlanningCode);
        // elke planning (woning) heeft de schijf aan haar eigen taak met code VLOER hangen
        var koppelingen = db.PlanningSchijfKoppeling.Where(k => k.PaymentStageId == stages[0].Id).ToList();
        Assert.Equal(2, koppelingen.Count);
        Assert.Equal(2, koppelingen.Select(k => k.BouwPlanningId).Distinct().Count());
        Assert.All(koppelingen, k => Assert.NotNull(k.TaakId));
    }

    [Fact]
    public async Task Een_gebouw_krijgt_een_planning_voor_alle_eenheden()
    {
        var (svc, db, id) = Maak();
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 3, EenPlanningVoorAlles = true }, "u1");
        Assert.True(r.Ok, r.Fout);
        var plan = Assert.Single(db.BouwPlanning.Where(b => !b.IsHoofd).ToList());
        Assert.Equal(2, db.BouwPlanningEenheid.Count(e => e.BouwPlanningId == plan.Id));
    }

    [Fact]
    public async Task Wat_al_bestaat_blijft_ongemoeid()
    {
        var (svc, db, id) = Maak();
        // traject bestaat al, een schijf van de groep is gefactureerd, woning 1 heeft al een planning
        db.Projecttraject.Add(new Projecttraject { ProjectId = ProjectId, Naam = "Bestaand" });
        db.InvoicesDetails.Add(new InvoicesDetails { LineType = "Stages", PaymentStageId = 101, UnitId = 11 });
        var bp = new BouwPlanning { ProjectId = ProjectId, Naam = "Woning 1", IsActief = true, IsHoofd = false };
        db.BouwPlanning.Add(bp); db.SaveChanges();
        db.BouwPlanningEenheid.Add(new BouwPlanningEenheid { BouwPlanningId = bp.Id, UnitId = 11 }); db.SaveChanges();

        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u1");
        Assert.True(r.Ok, r.Fout);
        Assert.Single(db.Projecttraject);                                    // geen tweede traject
        Assert.Single(db.InvoicingPaymentStages);                            // gefactureerde groep blijft zoals ze was
        Assert.Equal(100m, db.InvoicingPaymentStages.Single().Percentage);
        Assert.Equal(1, db.BouwPlanning.Count(b => !b.IsHoofd && b.Naam == "Woning 2"));   // enkel woning 2 kreeg een planning
        Assert.Contains(r.Opmerkingen, o => o.Contains("traject"));
        Assert.Contains(r.Opmerkingen, o => o.Contains("ongemoeid"));
    }

    [Fact]
    public async Task Beheer_bewaren_valideren_en_verwijderen()
    {
        var (svc, db, id) = Maak();
        var fout = await svc.Bewaar(new ProjectSjabloonBewerk { Naam = "" }, "u1");
        Assert.False(fout.Ok);
        Assert.Contains(fout.Fouten, f => f.Contains("naam"));
        Assert.Contains(fout.Fouten, f => f.Contains("minstens één sjabloon"));

        var bestaand = await svc.Haal(id);
        Assert.Equal("Rijwoning", bestaand!.Naam);
        Assert.Contains((await svc.Bewaar(new ProjectSjabloonBewerk { Naam = "Rijwoning", SchijfSjabloonId = bestaand.SchijfSjabloonId }, "u1")).Fouten, f => f.Contains("bestaat al"));

        var nieuw = await svc.Bewaar(new ProjectSjabloonBewerk { Naam = "Appartementsgebouw", SchijfSjabloonId = bestaand.SchijfSjabloonId, StandaardVerdiepingen = 4, StandaardDak = "Plat" }, "u1");
        Assert.True(nieuw.Ok, string.Join(" ", nieuw.Fouten));
        Assert.Equal("plat", (await svc.Haal(nieuw.Id))!.StandaardDak);
        Assert.Equal(2, (await svc.Keuzes()).Count);
        Assert.Null(await svc.ZetActief(nieuw.Id, false, "u1"));
        Assert.Single(await svc.Keuzes());
        Assert.Null(await svc.Verwijder(nieuw.Id));
        Assert.Null(await svc.Haal(nieuw.Id));
    }

    [Fact]
    public void Een_schijf_met_voorwaarde_komt_enkel_voor_bij_dat_gebouw()
    {
        var invoer = new[]
        {
            new SchijfInvoer("Ruwbouw", null, null, 1m, false),
            new SchijfInvoer("Kelder gieten", null, null, 1m, false, "kelder", "ja"),
            new SchijfInvoer("Dakstoel", null, null, 1m, false, "dak", "hellend"),
            new SchijfInvoer("Oplevering", null, 5m, 1m, false),
        };
        Assert.Equal(new[] { "Ruwbouw", "Oplevering" }, SchijfSjabloonUitrol.Rol(5m, invoer, 1).Schijven.Select(s => s.Naam).ToArray());
        Assert.Equal(new[] { "Ruwbouw", "Kelder gieten", "Dakstoel", "Oplevering" }, SchijfSjabloonUitrol.Rol(5m, invoer, 1, kelder: true, dak: "hellend").Schijven.Select(s => s.Naam).ToArray());
        Assert.Equal(new[] { "Ruwbouw", "Oplevering" }, SchijfSjabloonUitrol.Rol(5m, invoer, 1, kelder: false, dak: "plat").Schijven.Select(s => s.Naam).ToArray());
    }
}
