using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Een projectsjabloon met een tweede schijvensjabloon (migratie 160): groepen met enkel parkings en bergingen krijgen dat, alle andere groepen het gewone.</summary>
public class ProjectSjabloonBijgebouwenTests
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

    /// <summary>Groep 5 = appartementen (typegroep 1), groep 6 = parkings en bergingen (typegroep 2), groep 7 = een appartement met een berging erbij. Alle groepen hebben nog geen echte schijven.</summary>
    private static (ProjectSjabloonService Svc, TestDb Db, int SjabloonId) Maak(bool metBijgebouwSjabloon)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Residentie" });
        var hoofd = new SchijfSjabloon { Naam = "Appartementen", IsActief = true, CreatedDate = DateTime.UtcNow };
        hoofd.Schijven.Add(new SchijfSjabloonSchijf { Volgorde = 10, Naam = "Bij het gieten van de vloerplaat", Gewicht = 1 });
        hoofd.Schijven.Add(new SchijfSjabloonSchijf { Volgorde = 20, Naam = "Bij de voorlopige oplevering", VastPercentage = 5 });
        var bij = new SchijfSjabloon { Naam = "Parking of berging", IsActief = true, CreatedDate = DateTime.UtcNow };
        bij.Schijven.Add(new SchijfSjabloonSchijf { Volgorde = 10, Naam = "Bij aanvang van de werken", VastPercentage = 30 });
        bij.Schijven.Add(new SchijfSjabloonSchijf { Volgorde = 20, Naam = "Bij het gieten van de vloerplaat", Gewicht = 1 });
        bij.Schijven.Add(new SchijfSjabloonSchijf { Volgorde = 30, Naam = "Bij de voorlopige oplevering", VastPercentage = 30 });
        db.SchijfSjabloon.AddRange(hoofd, bij);
        db.UnitTypes.Add(new UnitTypes { Id = 1, Name = "Appartement", GroupId = 1 });
        db.UnitTypes.Add(new UnitTypes { Id = 2, Name = "Parking", GroupId = 2 });
        db.UnitTypes.Add(new UnitTypes { Id = 3, Name = "Berging", GroupId = 3 });
        db.SaveChanges();
        db.ProjectSjabloon.Add(new ProjectSjabloon
        {
            Naam = "Appartementsgebouw", IsActief = true, CreatedDate = DateTime.UtcNow,
            SchijfSjabloonId = hoofd.Id, SchijfSjabloonBijgebouwId = metBijgebouwSjabloon ? bij.Id : null,
        });
        db.Units.Add(new Units { Id = 11, Name = "App 1", ProjectId = ProjectId, TypeId = 1, PaymentGroupId = 5 });
        db.Units.Add(new Units { Id = 21, Name = "Parking P01", ProjectId = ProjectId, TypeId = 2, PaymentGroupId = 6 });
        db.Units.Add(new Units { Id = 22, Name = "Berging B01", ProjectId = ProjectId, TypeId = 3, PaymentGroupId = 6 });
        db.Units.Add(new Units { Id = 31, Name = "App 2", ProjectId = ProjectId, TypeId = 1, PaymentGroupId = 7 });
        db.Units.Add(new Units { Id = 32, Name = "Berging B02", ProjectId = ProjectId, TypeId = 3, PaymentGroupId = 7 });
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 5, Name = "Appartementen", ProjectId = ProjectId });
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 6, Name = "Parkings en bergingen", ProjectId = ProjectId });
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 7, Name = "Appartement met berging", ProjectId = ProjectId });
        db.SaveChanges();
        var svc = new ProjectSjabloonService(db, new PlanningService(db), new TrajectDubbel(), new SchijfSjabloonService(db));
        return (svc, db, db.ProjectSjabloon.Single().Id);
    }

    private static decimal[] Percentages(TestDb db, int groep) => db.InvoicingPaymentStages.Where(s => s.GroupId == groep).OrderBy(s => s.Id).Select(s => s.Percentage).ToArray();

    [Fact]
    public async Task Een_groep_met_enkel_parkings_en_bergingen_krijgt_het_schijvensjabloon_voor_bijgebouwen()
    {
        var (svc, db, id) = Maak(metBijgebouwSjabloon: true);
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u");
        Assert.Null(r.Fout);
        Assert.Equal(new[] { 30m, 40m, 30m }, Percentages(db, 6));       // parkings en bergingen: 30 / 40 / 30
        Assert.Equal(new[] { 95m, 5m }, Percentages(db, 5));             // appartementen: het gewone sjabloon
        Assert.Equal(new[] { 95m, 5m }, Percentages(db, 7));             // appartement met een berging erbij telt als gewone groep
        Assert.Contains(r.Stappen, s => s.Contains("parkings en bergingen") && s.Contains("Parkings en bergingen"));
    }

    [Fact]
    public async Task De_groepskoppeling_via_de_bouwwaarde_telt_ook_en_gaat_voor_op_de_oude_koppeling()
    {
        var (svc, db, id) = Maak(metBijgebouwSjabloon: true);
        // zoals in de echte data: Units.PaymentGroupId leeg, de groep hangt aan de bouwwaarde van de eenheid
        foreach (var u in db.Units.ToList()) { db.UnitConstructionValue.Add(new UnitConstructionValue { UnitId = u.Id, PaymentGroupId = u.PaymentGroupId }); u.PaymentGroupId = null; }
        db.SaveChanges();
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u");
        Assert.Null(r.Fout);
        Assert.Equal(new[] { 30m, 40m, 30m }, Percentages(db, 6));
        Assert.Equal(new[] { 95m, 5m }, Percentages(db, 5));
        Assert.Equal(new[] { 95m, 5m }, Percentages(db, 7));
    }

    [Fact]
    public async Task Zonder_bijgebouwsjabloon_krijgen_alle_groepen_het_gewone_schijvensjabloon()
    {
        var (svc, db, id) = Maak(metBijgebouwSjabloon: false);
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u");
        Assert.Null(r.Fout);
        foreach (var g in new[] { 5, 6, 7 }) Assert.Equal(new[] { 95m, 5m }, Percentages(db, g));
    }

    [Fact]
    public async Task Het_voorbeeld_bewaart_niets_maar_vermeldt_de_parkinggroep()
    {
        var (svc, db, id) = Maak(metBijgebouwSjabloon: true);
        var r = await svc.Voorbeeld(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 });
        Assert.Contains(r.Stappen, s => s.Contains("parkings en bergingen"));
        Assert.Empty(db.InvoicingPaymentStages);
    }

    [Fact]
    public async Task Een_bijgebouwsjabloon_dat_niet_op_100_procent_uitkomt_wordt_niet_toegepast_maar_de_andere_groepen_wel()
    {
        var (svc, db, id) = Maak(metBijgebouwSjabloon: true);
        var bij = db.SchijfSjabloon.Single(s => s.Naam == "Parking of berging");
        foreach (var sch in db.SchijfSjabloonSchijf.Where(x => x.SjabloonId == bij.Id && x.VastPercentage != null)) sch.VastPercentage = 60;
        db.SaveChanges();
        var r = await svc.Toepassen(ProjectId, id, new ProjectSjabloonAntwoorden { Verdiepingen = 1 }, "u");
        Assert.Empty(Percentages(db, 6));
        Assert.Equal(new[] { 95m, 5m }, Percentages(db, 5));
        Assert.Contains(r.Opmerkingen, o => o.Contains("Parkings en bergingen"));
    }
}
