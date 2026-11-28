using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Traject;
using Xunit;

/// <summary>Besluit Niels 28/11/2026: parkings en bergingen krijgen geen eigen mijlpalen per eenheid; ze volgen het traject van hun hoofdeenheid.</summary>
public class TrajectEenhedenTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private static TestDb Maak(params (int Id, string Naam, int TypeId)[] eenheden)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.UnitTypes.Add(new UnitTypes { Id = 1, Name = "Appartement", GroupId = 1 });
        db.UnitTypes.Add(new UnitTypes { Id = 2, Name = "Parking", GroupId = 2 });
        db.UnitTypes.Add(new UnitTypes { Id = 3, Name = "Berging", GroupId = 3 });
        db.UnitTypes.Add(new UnitTypes { Id = 4, Name = "Handelsruimte", GroupId = 4 });
        foreach (var e in eenheden) db.Units.Add(new Units { Id = e.Id, Name = e.Naam, ProjectId = 1, TypeId = e.TypeId });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Parkings_en_bergingen_krijgen_geen_eigen_traject_naast_een_hoofdeenheid()
    {
        var db = Maak((1, "App 1", 1), (2, "Parking 1", 2), (3, "Berging 1", 3), (4, "Handel", 4));
        var ids = await new TrajectInstantiationService(db, null!, null!).EenhedenMetEigenTraject(1);
        Assert.Equal(new[] { 1, 4 }, ids.OrderBy(x => x));
    }

    [Fact]
    public async Task Een_project_met_enkel_parkings_en_bergingen_geeft_ze_wel_een_eigen_traject()
    {
        var db = Maak((2, "Parking 1", 2), (3, "Berging 1", 3));
        var ids = await new TrajectInstantiationService(db, null!, null!).EenhedenMetEigenTraject(1);
        Assert.Equal(new[] { 2, 3 }, ids.OrderBy(x => x));
    }
}
