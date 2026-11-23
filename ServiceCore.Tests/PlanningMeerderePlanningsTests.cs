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
        db.Units.Add(new Units { Id = 11, Name = "Woning 1", ProjectId = ProjectId, PaymentGroupId = 11 });
        db.Units.Add(new Units { Id = 12, Name = "Woning 2", ProjectId = ProjectId, PaymentGroupId = 12 });
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
        if (bouwdeel != null) velden.Add(("eenheden", new[] { bouwdeel.Value }));
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
    public async Task Het_globale_overzicht_toont_alle_plannings_als_groepen_met_hun_taken_en_een_gezamenlijke_kop()
    {
        var (svc, _, s, hoofdId, woningId) = await MetHoofdEnWoning();
        s = await Doe(svc, s, Taak(woningId, "Ruwbouw", 10, "H:1.1"));

        var g = await svc.Laad(ProjectId, PlanningService.GlobaalId);
        Assert.Equal(PlanningService.GlobaalId, g.ActiefPlanningId);
        Assert.True(g.HeeftPlanning);
        var groepen = g.Rijen.Where(r => r.Niveau == 0).ToList();
        Assert.Equal(new[] { "Hoofdplanning", "Woning 1" }, groepen.Select(r => r.Naam).ToArray());
        Assert.All(groepen, r => Assert.Equal("fase", r.Soort));
        Assert.Contains(g.Rijen, r => r.Soort == "taak" && r.Naam == "Wegenis");
        Assert.Contains(g.Rijen, r => r.Soort == "taak" && r.Naam == "Ruwbouw");
        // De woning wacht op de wegenis: haar groep begint pas daarna, en de kop loopt over alles.
        var hoofd = groepen[0]; var woning = groepen[1];
        Assert.True(string.CompareOrdinal(woning.Start, hoofd.Start) > 0);
        Assert.Equal(woning.Einde, g.Kop.Einde);
        Assert.Equal(hoofd.Start, g.Kop.Start);
        Assert.Equal(2, g.Planningen.Count);
    }

    [Fact]
    public async Task Een_planning_toevoegen_toont_enkel_haar_eigen_rijen_met_eigen_nummering()
    {
        var (svc, _, s, hoofdId, woningId) = await MetHoofdEnWoning();
        Assert.Equal(woningId, s.ActiefPlanningId);
        Assert.Empty(s.Rijen);
        Assert.Equal(2, s.Planningen.Count);
        Assert.Equal("Woning 1", s.Planningen.Single(p => p.Id == woningId).EenhedenTekst);

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
    public async Task Een_voorganger_uit_een_ander_bouwdeel_staat_zichtbaar_in_het_veld_met_de_planningsnaam_ervoor()
    {
        var (svc, db, s, _, woningId) = await MetHoofdEnWoning();
        var u2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Woning 2", 12).Velden }, "u1");
        var woning2 = u2.NieuwePlanningId!.Value;
        s = await Doe(svc, u2.Staat, Taak(woningId, "Ruwbouw", 10));
        s = await Doe(svc, s, Taak(woning2, "Ruwbouw", 10, "Woning 1:1.1"));

        var t2 = await db.PlanningTaak.OrderByDescending(t => t.Id).FirstAsync();
        var staat2 = await svc.Laad(ProjectId, woning2);
        var rij = staat2.Rijen.Single(r => r.Soort == "taak");
        Assert.Equal("Woning 1:1.1", rij.Voorgangers);
        Assert.Equal("2026-11-30", rij.Start);   // woning 1 eindigt vrijdag 27 november

        // Het veld vervangt alles: de estafette weghalen kan, een andere voorganger erbij zetten ook.
        s = await Doe(svc, staat2, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = t2.Id, PlanningId = woning2, Velden = V(("voorgangers", "H:1.1; Woning 1:1.1SS+1d")) });
        Assert.Equal(2, await db.PlanningAfhankelijkheid.CountAsync(l => l.OpvolgerTaakId == t2.Id));
        s = await Doe(svc, s, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = t2.Id, PlanningId = woning2, Velden = V(("voorgangers", "")) });
        Assert.Equal(0, await db.PlanningAfhankelijkheid.CountAsync(l => l.OpvolgerTaakId == t2.Id));
    }

    [Fact]
    public async Task Een_onbekende_planningsnaam_of_taak_geeft_een_duidelijke_fout()
    {
        var (svc, _, s, _, woningId) = await MetHoofdEnWoning();
        var a = await svc.Voer(ProjectId, new PlanningCommando { Type = "taakToevoegen", Versie = s.Versie, PlanningId = woningId, Velden = V(("naam", "X"), ("duur", 1), ("voorgangers", "Villa:1.1")) }, "u1");
        Assert.False(a.Ok);
        Assert.Contains("Er is geen planning", a.Fout);
        var b = await svc.Voer(ProjectId, new PlanningCommando { Type = "taakToevoegen", Versie = s.Versie, PlanningId = woningId, Velden = V(("naam", "X"), ("duur", 1), ("voorgangers", "Woning 1:1.1")) }, "u1");
        Assert.False(b.Ok);
        Assert.Contains("zelf", b.Fout);
    }

    [Fact]
    public async Task Een_planningsnaam_mag_geen_dubbelpunt_bevatten_en_moet_uniek_zijn()
    {
        var (svc, _, s, _, _) = await MetHoofdEnWoning();
        var a = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Woning: 2").Velden }, "u1");
        Assert.False(a.Ok);
        var b = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("woning 1").Velden }, "u1");
        Assert.False(b.Ok);
        Assert.Contains("al een planning", b.Fout);
        var c = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("H").Velden }, "u1");
        Assert.False(c.Ok);
    }

    [Fact]
    public async Task Elke_taak_krijgt_een_estafettevoorstel_per_andere_bouwdeelplanning()
    {
        var (svc, _, s, _, woningId) = await MetHoofdEnWoning();
        var u2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Woning 2", 12).Velden }, "u1");
        var woning2 = u2.NieuwePlanningId!.Value;
        s = await Doe(svc, u2.Staat, Taak(woningId, "Elektriciteit", 4));
        s = await Doe(svc, s, Taak(woning2, "Elektriciteit", 4));

        var rij = (await svc.Laad(ProjectId, woning2)).Rijen.Single(r => r.Soort == "taak");
        Assert.Equal("Woning 1:1.1", Assert.Single(rij.EstafetteVoorstellen).Verwijzing);
        // Elke andere bouwdeelplanning is een keuze, ook de volgende: er is geen vaste volgorde.
        Assert.Equal("Woning 2:1.1", Assert.Single((await svc.Laad(ProjectId, woningId)).Rijen.Single(r => r.Soort == "taak").EstafetteVoorstellen).Verwijzing);
        Assert.Equal("", rij.Voorgangers);
    }

    [Fact]
    public async Task De_hoofdplanning_hernoemen_met_Id_0_werkt_ook_voor_ze_in_de_database_staat()
    {
        var (svc, db) = Maak();
        var s = await Doe(svc, null, new PlanningCommando { Type = "planningWijzig", PlanningId = 0, Velden = V(("naam", "Algemeen"), ("startDatum", "2026-12-01")) });
        Assert.Equal("Algemeen", s.Planningen.Single().Naam);
        Assert.Equal("2026-12-01", s.Planningen.Single().StartDatum);

        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningWijzig", Versie = s.Versie, PlanningId = 0, Velden = V(("eenheden", new[] { 11 })) }, "u1");
        Assert.True(u.Ok, u.Fout);   // de hoofdplanning mag eigen eenheden krijgen (dan geldt ze enkel daarvoor)
        Assert.Equal(new[] { 11 }, u.Staat!.Planningen.Single().EenheidIds.ToArray());
        Assert.True((await db.BouwPlanning.SingleAsync()).IsHoofd);
    }

    [Fact]
    public async Task Een_bouwdeel_kan_maar_één_planning_hebben_en_moet_bij_het_project_horen()
    {
        var (svc, db, s, _, _) = await MetHoofdEnWoning();
        var dubbel = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Nog een", 11).Velden }, "u1");
        Assert.False(dubbel.Ok);
        Assert.Contains("al in een andere planning", dubbel.Fout);

        db.Units.Add(new Units { Id = 99, Name = "Elders", ProjectId = 2 });
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

    [Fact]
    public async Task De_keuzelijst_voor_voorgangers_bevat_alle_plannings_met_de_tekst_om_in_het_veld_te_typen()
    {
        var (svc, _, s, _, woningId) = await MetHoofdEnWoning();
        var u2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Woning 2", 12).Velden }, "u1");
        var woning2 = u2.NieuwePlanningId!.Value;
        s = await Doe(svc, u2.Staat, Taak(woningId, "Ruwbouw", 10));
        s = await Doe(svc, s, Taak(woning2, "Dak", 3));

        var staat = await svc.Laad(ProjectId, woning2);
        Assert.Contains(staat.Keuzes, k => k.Verwijzing == "1.1" && k.Naam == "Dak" && k.IsEigen && !k.IsFase);
        Assert.Contains(staat.Keuzes, k => k.Verwijzing == "H:1.1" && k.Naam == "Wegenis");
        Assert.Contains(staat.Keuzes, k => k.Verwijzing == "Woning 1:1.1" && k.Naam == "Ruwbouw");
        Assert.Contains(staat.Keuzes, k => k.Verwijzing == "Woning 1:1" && k.IsFase);
        // De eigen planning komt eerst, daarna de hoofdplanning.
        Assert.True(staat.Keuzes[0].IsEigen);
        Assert.Equal("Hoofdplanning", staat.Keuzes.First(k => !k.IsEigen).Planning);
    }

    [Fact]
    public async Task Een_mijlpaal_per_woning_staat_in_de_lijst_van_die_woning_en_koppelt_enkel_aan_haar_eigen_planning()
    {
        var (svc, db, s, hoofdId, woning1) = await MetHoofdEnWoning();
        s = await Doe(svc, s, Taak(woning1, "Ruwbouw", 10));
        var u2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "planningToevoegen", Versie = s.Versie, Velden = NieuwePlanning("Woning 2", 12).Velden }, "u1");
        Assert.True(u2.Ok, u2.Fout);
        var woning2 = u2.NieuwePlanningId!.Value;
        s = await Doe(svc, u2.Staat, Taak(woning2, "Ruwbouw", 10));

        var traject = new Projecttraject { ProjectId = ProjectId, Naam = "Traject", Status = 0 };
        db.Projecttraject.Add(traject);
        await db.SaveChangesAsync();
        var m1 = new Mijlpaal { ProjecttrajectId = traject.Id, UnitId = 11, Code = "RUWBOUW_KLAAR", Naam = "Ruwbouw klaar", Status = 0 };
        var m2 = new Mijlpaal { ProjecttrajectId = traject.Id, UnitId = 12, Code = "RUWBOUW_KLAAR", Naam = "Ruwbouw klaar", Status = 0 };
        db.Mijlpaal.AddRange(m1, m2);
        await db.SaveChangesAsync();

        var taak1 = await db.PlanningTaak.SingleAsync(t => t.Naam == "Ruwbouw" && t.PlanningSectie.BouwPlanningId == woning1);
        var taak2 = await db.PlanningTaak.SingleAsync(t => t.Naam == "Ruwbouw" && t.PlanningSectie.BouwPlanningId == woning2);

        // De lijst van Woning 1 toont enkel de mijlpaal van Woning 1, met de naam van de woning; de hoofdplanning toont er geen.
        var lijst1 = (await svc.Laad(ProjectId, woning1)).TrajectMijlpalen;
        var optie = Assert.Single(lijst1);
        Assert.Equal(m1.Id, optie.Id);
        Assert.Equal("Woning 1", optie.EenheidNaam);
        Assert.Empty((await svc.Laad(ProjectId, hoofdId)).TrajectMijlpalen);

        // Een taak van een andere woning wordt geweigerd; de eigen taak lukt.
        var fout = await svc.Koppel(ProjectId, new PlanningKoppelCommando { PlanningId = woning1, Soort = "taak", Id = taak2.Id, MijlpaalId = m1.Id }, "u1");
        Assert.False(fout.Ok);
        Assert.Null((await db.Mijlpaal.AsNoTracking().SingleAsync(x => x.Id == m1.Id)).BronBinding);
        var ok = await svc.Koppel(ProjectId, new PlanningKoppelCommando { PlanningId = woning1, Soort = "taak", Id = taak1.Id, MijlpaalId = m1.Id }, "u1");
        Assert.True(ok.Ok, ok.Fout);
        var vast = await db.Mijlpaal.AsNoTracking().SingleAsync(x => x.Id == m1.Id);
        Assert.Equal((4, taak1.Id), (vast.BronBinding, vast.BronRefId));
    }
}
