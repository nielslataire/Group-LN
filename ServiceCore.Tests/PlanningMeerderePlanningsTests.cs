using System.Text.Json;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using ServiceCore.Planning;
using Xunit;

/// <summary>Meerdere plannings per project: hoofdplanning en bouwdelen, samen doorgerekend (migratie 145).</summary>
public class PlanningMeerderePlanningsTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1;

    private static (PlanningService Svc, TestDb Db) Maak()
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project { ProjectId = ProjectId, ProjectName = "Verkaveling", StartDateConstruction = new DateOnly(2026, 11, 16) }); // maandag
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 11, Name = "Woning 1", ProjectId = ProjectId });
        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 12, Name = "Woning 2", ProjectId = ProjectId });
        db.SaveChanges();
        return (new PlanningService(db), db);
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

    private static PlanningCommando Taak(int planningId, string naam, int duur, string? voor = null)
    {
        var velden = new List<(string, object?)> { ("naam", naam), ("duur", duur) };
        if (voor != null) velden.Add(("voorgangers", voor));
        return new PlanningCommando { Type = "taakToevoegen", PlanningId = planningId, Velden = V(velden.ToArray()) };
    }

    private static PlanningCommando NieuwePlanning(string naam, int? bouwdeel = null, string? start = null)
    {
        var velden = new List<(string, object?)> { ("naam", naam) };
        if (bouwdeel != null) velden.Add(("bouwdeelId", bouwdeel));
        if (start != null) velden.Add(("startDatum", start));
        return new PlanningCommando { Type = "planningToevoegen", Velden = V(velden.ToArray()) };
    }

    private static async Task<(PlanningService Svc, TestDb Db, PlanningStaat Staat, int HoofdId, int WoningId)> MetHoofdEnWoning()
    {
        var (svc, db) = Maak();
        var s = await Doe(svc, null, Taak(0, "Wegenis", 5));                                  // hoofdplanning, 1.1 (in een nieuwe "Fase 1")
        var hoofdId = s.ActiefPlanningId;
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Woning 1", 11).Velden }, "u1");
        Assert.True(u.Ok, u.Fout);
        var woningId = u.NieuwePlanningId!.Value;
        return (svc, db, u.Staat!, hoofdId, woningId);
    }

    [Fact]
    public async Task De_eerste_schrijfactie_maakt_een_echte_hoofdplanning_en_zonder_gegevens_is_ze_virtueel()
    {
        var (svc, db) = Maak();
        var leeg = await svc.Laad(ProjectId);
        Assert.Equal(0, leeg.ActiefPlanningId);
        var virtueel = Assert.Single(leeg.Planningen);
        Assert.True(virtueel.IsHoofd);
        Assert.False(await db.BouwPlanning.AnyAsync());

        var s = await Doe(svc, null, Taak(0, "Wegenis", 5));
        var bp = await db.BouwPlanning.SingleAsync();
        Assert.True(bp.IsHoofd);
        Assert.Equal(bp.Id, s.ActiefPlanningId);
        Assert.All(await db.PlanningSectie.ToListAsync(), x => Assert.Equal(bp.Id, x.BouwPlanningId));
    }

    [Fact]
    public async Task Een_planning_toevoegen_toont_enkel_haar_eigen_rijen_met_eigen_nummering()
    {
        var (svc, _, s, hoofdId, woningId) = await MetHoofdEnWoning();
        Assert.Equal(woningId, s.ActiefPlanningId);
        Assert.Empty(s.Rijen);
        Assert.Equal(2, s.Planningen.Count);
        Assert.Equal("Woning 1", s.Planningen.Single(p => p.Id == woningId).BouwdeelNaam);

        s = await Doe(svc, s, Taak(woningId, "Ruwbouw", 10));
        Assert.Equal(new[] { "1", "1.1" }, s.Rijen.Select(r => r.Wbs));
        Assert.Equal("Ruwbouw", s.Rijen.Single(r => r.Soort == "taak").Naam);

        var hoofd = await svc.Laad(ProjectId, hoofdId);
        Assert.Equal("Wegenis", hoofd.Rijen.Single(r => r.Soort == "taak").Naam);
    }

    [Fact]
    public async Task Een_voorganger_in_de_hoofdplanning_duwt_een_taak_van_het_bouwdeel_op_en_blijft_zichtbaar_met_H()
    {
        var (svc, _, s, _, woningId) = await MetHoofdEnWoning();
        s = await Doe(svc, s, Taak(woningId, "Ruwbouw", 10, "H:1.1"));
        var taak = s.Rijen.Single(r => r.Soort == "taak");
        Assert.Equal("2026-11-23", taak.Start);   // wegenis eindigt vrijdag 20 november
        Assert.Equal("H:1.1", taak.Voorgangers);
    }

    [Fact]
    public async Task H_werkt_niet_in_de_hoofdplanning_zelf_en_een_onbekende_taak_wordt_geweigerd()
    {
        var (svc, _, s, hoofdId, woningId) = await MetHoofdEnWoning();
        var a = await svc.Voer(ProjectId, new PlanningCommando { Type = "taakToevoegen", Versie = s.Versie, PlanningId = hoofdId, Velden = V(("naam", "X"), ("duur", 1), ("voorgangers", "H:1.1")) }, "u1");
        Assert.False(a.Ok);
        var b = await svc.Voer(ProjectId, new PlanningCommando { Type = "taakToevoegen", Versie = s.Versie, PlanningId = woningId, Velden = V(("naam", "X"), ("duur", 1), ("voorgangers", "H:9.9")) }, "u1");
        Assert.False(b.Ok);
        Assert.Contains("hoofdplanning", b.Fout);
    }

    [Fact]
    public async Task De_startdatum_van_een_planning_is_een_ondergrens_voor_haar_taken()
    {
        var (svc, db) = Maak();
        var s = await Doe(svc, null, Taak(0, "Wegenis", 2));
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Woning 2", 12, "2026-12-07").Velden }, "u1");
        Assert.True(u.Ok, u.Fout);
        var woningId = u.NieuwePlanningId!.Value;
        s = await Doe(svc, u.Staat, Taak(woningId, "Ruwbouw", 3));
        Assert.Equal("2026-12-07", s.Rijen.Single(r => r.Soort == "taak").Start);

        // Een startdatum vóór de projectstart doet niets: de taak start op de eerste projectdag.
        s = await Doe(svc, s, new PlanningCommando { Type = "planningWijzig", PlanningId = woningId, Velden = V(("startDatum", "2026-01-05")) });
        Assert.Equal("2026-11-16", s.Rijen.Single(r => r.Soort == "taak").Start);
    }

    [Fact]
    public async Task Het_voorgangersveld_van_een_bouwdeel_laat_een_voorganger_uit_een_ander_bouwdeel_staan()
    {
        var (svc, db, s, _, woningId) = await MetHoofdEnWoning();
        var u2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Woning 2", 12).Velden }, "u1");
        var woning2 = u2.NieuwePlanningId!.Value;
        s = await Doe(svc, u2.Staat, Taak(woningId, "Ruwbouw woning 1", 10));
        s = await Doe(svc, s, Taak(woning2, "Ruwbouw woning 2", 10));

        var t1 = await db.PlanningTaak.SingleAsync(t => t.Naam == "Ruwbouw woning 1");
        var t2 = await db.PlanningTaak.SingleAsync(t => t.Naam == "Ruwbouw woning 2");
        db.PlanningAfhankelijkheid.Add(new PlanningAfhankelijkheid { VoorgangerTaakId = t1.Id, OpvolgerTaakId = t2.Id, Type = 0, WachtDagen = 0 });
        await db.SaveChangesAsync();

        // Woning 2 wacht nu op woning 1: start na het einde van woning 1.
        var staat2 = await svc.Laad(ProjectId, woning2);
        Assert.Equal("2026-11-30", staat2.Rijen.Single(r => r.Soort == "taak").Start);

        // Een voorganger in dezelfde planning of de hoofdplanning bewerken laat die koppeling ongemoeid.
        s = await Doe(svc, staat2, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = t2.Id, PlanningId = woning2, Velden = V(("voorgangers", "H:1.1")) });
        Assert.Equal(2, await db.PlanningAfhankelijkheid.CountAsync(l => l.OpvolgerTaakId == t2.Id));
        Assert.Equal("2026-11-30", s.Rijen.Single(r => r.Soort == "taak").Start);
    }

    [Fact]
    public async Task De_hoofdplanning_hernoemen_met_Id_0_werkt_ook_voor_ze_in_de_database_staat_maar_krijgt_geen_bouwdeel()
    {
        var (svc, db) = Maak();
        var s = await Doe(svc, null, new PlanningCommando { Type = "planningWijzig", PlanningId = 0, Velden = V(("naam", "Algemeen"), ("startDatum", "2026-12-01")) });
        Assert.Equal("Algemeen", s.Planningen.Single().Naam);
        Assert.Equal("2026-12-01", s.Planningen.Single().StartDatum);

        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningWijzig", Versie = s.Versie, PlanningId = 0, Velden = V(("bouwdeelId", 11)) }, "u1");
        Assert.False(u.Ok);
        Assert.Contains("hele project", u.Fout);
        Assert.True((await db.BouwPlanning.SingleAsync()).IsHoofd);
    }

    [Fact]
    public async Task Een_bouwdeel_kan_maar_één_planning_hebben_en_moet_bij_het_project_horen()
    {
        var (svc, db, s, _, _) = await MetHoofdEnWoning();
        var dubbel = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Nog een", 11).Velden }, "u1");
        Assert.False(dubbel.Ok);
        Assert.Contains("al een planning", dubbel.Fout);

        db.InvoicingPaymentGroup.Add(new InvoicingPaymentGroup { Id = 99, Name = "Elders", ProjectId = 2 });
        await db.SaveChangesAsync();
        var vreemd = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Vreemd", 99).Velden }, "u1");
        Assert.False(vreemd.Ok);
        Assert.Contains("niet bij dit project", vreemd.Fout);
    }

    [Fact]
    public async Task Een_planning_verwijderen_ruimt_haar_fases_taken_en_koppelingen_op_en_de_hoofdplanning_blijft()
    {
        var (svc, db, s, hoofdId, woningId) = await MetHoofdEnWoning();
        s = await Doe(svc, s, Taak(woningId, "Ruwbouw", 10, "H:1.1"));

        var weg = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningVerwijder", Versie = s.Versie, PlanningId = woningId }, "u1");
        Assert.True(weg.Ok, weg.Fout);
        Assert.Null(weg.Undo);
        Assert.Equal(hoofdId, weg.Staat!.ActiefPlanningId);
        Assert.Single(weg.Staat.Planningen);
        Assert.Equal(1, await db.PlanningTaak.CountAsync());
        Assert.Equal(0, await db.PlanningAfhankelijkheid.CountAsync());

        var hoofd = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningVerwijder", Versie = weg.Staat.Versie, PlanningId = hoofdId }, "u1");
        Assert.False(hoofd.Ok);
        Assert.Contains("hoofdplanning", hoofd.Fout);
    }

    [Fact]
    public async Task Ongedaan_maken_herstelt_de_fases_in_hun_planning()
    {
        var (svc, db, s, _, woningId) = await MetHoofdEnWoning();
        s = await Doe(svc, s, Taak(woningId, "Ruwbouw", 10));
        var taak = await db.PlanningTaak.SingleAsync(t => t.Naam == "Ruwbouw");

        var verwijderd = await svc.Voer(ProjectId, new PlanningCommando
        {
            Type = "verwijder", Versie = s.Versie, PlanningId = woningId,
            Items = new List<PlanningItemRef> { new() { Soort = "taak", Id = taak.Id } },
        }, "u1");
        Assert.True(verwijderd.Ok, verwijderd.Fout);
        Assert.NotNull(verwijderd.Undo);

        var terug = await svc.Voer(ProjectId, new PlanningCommando { Type = "herstel", Versie = verwijderd.Staat!.Versie, PlanningId = woningId, Momentopname = verwijderd.Undo }, "u1");
        Assert.True(terug.Ok, terug.Fout);
        Assert.Equal("Ruwbouw", terug.Staat!.Rijen.Single(r => r.Soort == "taak").Naam);
        Assert.Equal(woningId, terug.Staat.ActiefPlanningId);
    }

    [Fact]
    public async Task Alle_plannings_samen_bepalen_de_doorrekening_over_de_grenzen_heen()
    {
        var (svc, db, s, hoofdId, woningId) = await MetHoofdEnWoning();
        s = await Doe(svc, s, Taak(woningId, "Ruwbouw", 10, "H:1.1"));
        // De hoofdtaak langer maken verschuift de taak in het bouwdeel.
        var wegenis = await db.PlanningTaak.SingleAsync(t => t.Naam == "Wegenis");
        s = await Doe(svc, s, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = wegenis.Id, PlanningId = hoofdId, Velden = V(("duur", 10)) });
        var woning = await svc.Laad(ProjectId, woningId);
        Assert.Equal("2026-11-30", woning.Rijen.Single(r => r.Soort == "taak").Start);
    }
}
