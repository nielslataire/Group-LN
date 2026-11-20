using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Schijven uit een schijvensjabloon hangen automatisch aan de taak of fase met dezelfde code in de planning (per verdieping de juiste).</summary>
public class PlanningSchijfAutoKoppelingTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1;

    /// <summary>Hoofdplanning met een blok per verdieping (twee verdiepingen, elk met een taak met code VLOER) en een taak OPLEVERING; één groep met één eenheid.</summary>
    private static TestDb Maak()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling" });
        db.BouwPlanning.Add(new BouwPlanning { Id = 7, ProjectId = ProjectId, Naam = "Hoofdplanning", IsHoofd = true, IsActief = true });
        db.PlanningSectie.Add(new PlanningSectie { Id = 10, ProjectId = ProjectId, Naam = "Verdieping 1", Volgorde = 10, IsActief = true, BouwPlanningId = 7 });
        db.PlanningSectie.Add(new PlanningSectie { Id = 11, ProjectId = ProjectId, Naam = "Verdieping 2", Volgorde = 20, IsActief = true, BouwPlanningId = 7 });
        db.PlanningSectie.Add(new PlanningSectie { Id = 12, ProjectId = ProjectId, Naam = "Oplevering", Volgorde = 30, IsActief = true, BouwPlanningId = 7, Code = "OPLEVERING" });
        db.PlanningTaak.Add(new PlanningTaak { Id = 20, PlanningSectieId = 10, Naam = "Vloerplaat gieten", Code = "VLOER" });
        db.PlanningTaak.Add(new PlanningTaak { Id = 21, PlanningSectieId = 11, Naam = "Vloerplaat gieten", Code = "vloer" });
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 5, Name = "Woningen", ProjectId = ProjectId });
        db.Units.Add(new Units { Id = 11, Name = "Woning", ProjectId = ProjectId });
        db.UnitConstructionValue.Add(new UnitConstructionValue { UnitId = 11, PaymentGroupId = 5, ValueSold = 1 });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = 101, GroupId = 5, Name = "Vloerplaat 1ste verdieping", Percentage = 10, PlanningCode = "VLOER", PlanningVolgnr = 1 });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = 102, GroupId = 5, Name = "Vloerplaat 2de verdieping", Percentage = 10, PlanningCode = "VLOER", PlanningVolgnr = 2 });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = 103, GroupId = 5, Name = "Oplevering", Percentage = 5, PlanningCode = "OPLEVERING" });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = 104, GroupId = 5, Name = "Zonder code", Percentage = 75 });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = 105, GroupId = 5, Name = "Onbekende code", Percentage = 0, PlanningCode = "BESTAAT_NIET" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Een_schijf_hangt_aan_de_taak_met_dezelfde_code_en_bij_een_herhaling_aan_de_juiste_verdieping()
    {
        var db = Maak();
        Assert.Equal(3, await PlanningSchijfAutoKoppeling.KoppelOpCode(db, ProjectId));
        var k = db.PlanningSchijfKoppeling.ToList();
        Assert.Equal(20, k.Single(x => x.PaymentStageId == 101).TaakId);       // 1ste verdieping
        Assert.Equal(21, k.Single(x => x.PaymentStageId == 102).TaakId);       // 2de verdieping
        var fase = k.Single(x => x.PaymentStageId == 103);                       // code van een fase
        Assert.Null(fase.TaakId); Assert.Equal(12, fase.SectieId);
        Assert.All(k, x => Assert.Equal(7, x.BouwPlanningId));
        Assert.DoesNotContain(k, x => x.PaymentStageId == 104 || x.PaymentStageId == 105);   // zonder of met onbekende code blijft los
    }

    [Fact]
    public async Task Een_tweede_keer_koppelen_en_een_handmatige_koppeling_blijven_ongemoeid()
    {
        var db = Maak();
        db.PlanningSchijfKoppeling.Add(new PlanningSchijfKoppeling { PaymentStageId = 101, BouwPlanningId = 7, TaakId = 21 });   // met de hand aan een andere taak gehangen
        db.SaveChanges();
        Assert.Equal(2, await PlanningSchijfAutoKoppeling.KoppelOpCode(db, ProjectId));
        Assert.Equal(21, db.PlanningSchijfKoppeling.Single(x => x.PaymentStageId == 101).TaakId);
        Assert.Equal(0, await PlanningSchijfAutoKoppeling.KoppelOpCode(db, ProjectId));
        Assert.Equal(3, db.PlanningSchijfKoppeling.Count());
    }

    [Fact]
    public async Task Een_verdieping_die_niet_in_de_planning_staat_geeft_geen_koppeling()
    {
        var db = Maak();
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = 106, GroupId = 5, Name = "Vloerplaat 3de verdieping", Percentage = 10, PlanningCode = "VLOER", PlanningVolgnr = 3 });
        db.SaveChanges();
        await PlanningSchijfAutoKoppeling.KoppelOpCode(db, ProjectId);
        Assert.DoesNotContain(db.PlanningSchijfKoppeling, x => x.PaymentStageId == 106);
    }
}
