using System.Text.Json;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Bouwstaten (migratie 158): een schijf wacht op alle taken die een bouwstaat opleveren (voltooid of gestart), een mijlpaal met de code van een bouwstaat volgt de laatste leverancier.</summary>
public class BouwstaatKoppelingTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1, StageId = 101;

    private static Dictionary<string, JsonElement> V(params (string Naam, object? Waarde)[] velden)
        => velden.ToDictionary(v => v.Naam, v => JsonSerializer.SerializeToElement(v.Waarde));

    /// <summary>Eén eenheid, één schijf op de bouwstaat TECHNIEKEN_RUW en twee taken (elektriciteit en sanitair) die die bouwstaat opleveren.</summary>
    private static async Task<(PlanningService Svc, TestDb Db, int Elektra, int Sanitair)> Maak(byte regel = 0)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Woning", StartDateConstruction = new DateOnly(2026, 11, 16) });
        var bs = new Bouwstaat { Code = "TECHNIEKEN_RUW", Naam = "Leidingen technieken geplaatst" };
        db.Bouwstaat.Add(bs);
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 5, Name = "Woning", ProjectId = ProjectId });
        db.InvoicingPaymentStages.Add(new InvoicingPaymentStages { Id = StageId, GroupId = 5, Name = "Technieken", Percentage = 10, BouwstaatId = bs.Id, BouwstaatRegel = regel });
        db.Units.Add(new Units { Id = 11, Name = "Woning 1", ProjectId = ProjectId, PaymentGroupId = 5, ClientAccountId = 911 });
        db.UnitConstructionValue.Add(new UnitConstructionValue { UnitId = 11, PaymentGroupId = 5, ValueSold = 100000 });
        db.SaveChanges();

        var svc = new PlanningService(db);
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "taakToevoegen", PlanningId = 0, Versie = 0, Velden = V(("naam", "Leidingen elektriciteit"), ("duur", 5)) }, "u1");
        Assert.True(u.Ok, u.Fout);
        u = await svc.Voer(ProjectId, new PlanningCommando { Type = "taakToevoegen", PlanningId = 0, Versie = u.Staat!.Versie, Velden = V(("naam", "Leidingen sanitair"), ("duur", 8)) }, "u1");
        Assert.True(u.Ok, u.Fout);
        var elektra = db.PlanningTaak.Single(t => t.Naam == "Leidingen elektriciteit");
        var sanitair = db.PlanningTaak.Single(t => t.Naam == "Leidingen sanitair");
        elektra.BouwstaatId = bs.Id; sanitair.BouwstaatId = bs.Id;
        db.SaveChanges();
        return (svc, db, elektra.Id, sanitair.Id);
    }

    private static async Task<PlanningSchijfLink?> Link(PlanningService svc)
        => (await svc.Laad(ProjectId, 0)).Rijen.SelectMany(r => r.Schijven).SingleOrDefault(l => l.StageId == StageId);

    [Fact]
    public async Task Een_schijf_wacht_op_alle_taken_die_de_bouwstaat_opleveren()
    {
        var (svc, db, elektra, sanitair) = await Maak();
        var link = await Link(svc);
        Assert.NotNull(link);                          // de schijf hangt dynamisch aan de taken van de bouwstaat, zonder koppelrij
        Assert.False(link!.KlaarOmTeMarkeren);
        Assert.Empty(db.PlanningSchijfKoppeling);

        db.PlanningTaak.Single(t => t.Id == elektra).IsAfgerond = true; db.SaveChanges();
        Assert.False((await Link(svc))!.KlaarOmTeMarkeren);          // sanitair is nog bezig

        db.PlanningTaak.Single(t => t.Id == sanitair).IsAfgerond = true; db.SaveChanges();
        Assert.True((await Link(svc))!.KlaarOmTeMarkeren);
    }

    [Fact]
    public async Task Met_de_regel_gestart_volstaat_het_dat_alle_taken_begonnen_zijn()
    {
        var (svc, db, elektra, sanitair) = await Maak(regel: 1);
        db.PlanningTaak.Single(t => t.Id == elektra).VoortgangPct = 20; db.SaveChanges();
        Assert.False((await Link(svc))!.KlaarOmTeMarkeren);

        db.PlanningTaak.Single(t => t.Id == sanitair).WerkelijkStart = new DateOnly(2026, 11, 17); db.SaveChanges();
        Assert.True((await Link(svc))!.KlaarOmTeMarkeren);
    }

    [Fact]
    public async Task Een_eigen_koppeling_gaat_voor_op_de_bouwstaat()
    {
        var (svc, db, elektra, _) = await Maak();
        var k = await svc.KoppelSchijf(ProjectId, new PlanningSchijfKoppelCommando { Soort = "taak", Id = elektra, PlanningId = 0, StageId = StageId }, "u1");
        Assert.True(k.Ok, k.Fout);
        db.PlanningTaak.Single(t => t.Id == elektra).IsAfgerond = true; db.SaveChanges();
        Assert.True((await Link(svc))!.KlaarOmTeMarkeren);             // enkel de gekoppelde taak telt, sanitair is niet nodig
    }

    [Fact]
    public async Task Een_mijlpaal_met_de_code_van_een_bouwstaat_volgt_de_taak_die_ze_als_laatste_oplevert()
    {
        var (_, db, elektra, sanitair) = await Maak();
        db.PlanningTaak.Single(t => t.Id == elektra).Einddatum = new DateOnly(2026, 11, 20);
        db.PlanningTaak.Single(t => t.Id == sanitair).Einddatum = new DateOnly(2026, 11, 26);
        db.Projecttraject.Add(new Projecttraject { Id = 1, ProjectId = ProjectId, Naam = "Traject" });
        db.Mijlpaal.Add(new Mijlpaal { Id = 100, ProjecttrajectId = 1, Code = "techNieken_ruw", Naam = "Leidingen klaar" });
        db.SaveChanges();

        Assert.Equal(1, await PlanningTrajectKoppeling.KoppelOpCode(db, ProjectId));
        var m = db.Mijlpaal.Single();
        Assert.Equal(PlanningTrajectKoppeling.BindingTaak, m.BronBinding);
        Assert.Equal(sanitair, m.BronRefId);
    }

    [Fact]
    public async Task De_codekoppeling_laat_een_schijf_met_een_geleverde_bouwstaat_aan_de_dynamische_koppeling()
    {
        var (_, db, elektra, _) = await Maak();
        var stage = db.InvoicingPaymentStages.Single();
        stage.PlanningCode = "LEIDINGEN_ELEKTRA";
        db.PlanningTaak.Single(t => t.Id == elektra).Code = "LEIDINGEN_ELEKTRA";
        db.SaveChanges();
        Assert.Equal(0, await PlanningSchijfAutoKoppeling.KoppelOpCode(db, ProjectId));   // de bouwstaat wordt geleverd: dynamisch
        Assert.Empty(db.PlanningSchijfKoppeling);

        // zonder bouwstaat op de taken valt de schijf terug op de code
        foreach (var t in db.PlanningTaak) t.BouwstaatId = null;
        db.SaveChanges();
        Assert.Equal(1, await PlanningSchijfAutoKoppeling.KoppelOpCode(db, ProjectId));
        Assert.Equal(elektra, db.PlanningSchijfKoppeling.Single().TaakId);
    }

    [Fact]
    public void Het_schijvensjabloon_geeft_de_bouwstaat_en_de_regel_door_aan_het_voorstel()
    {
        var uit = SchijfSjabloonUitrol.Rol(5m, new SchijfInvoer[]
        {
            new("Leidingen", "LEIDINGEN_ELEKTRA", null, 1, false, null, null, 7, 1),
            new("Eind", null, 5, 1, false),
        }, 1);
        var s = uit.Schijven.First(x => x.Naam == "Leidingen");
        Assert.Equal(7, s.BouwstaatId);
        Assert.Equal(1, s.BouwstaatRegel);
        Assert.Null(uit.Schijven.Single(x => x.Naam == "Eind").BouwstaatId);
    }
}
