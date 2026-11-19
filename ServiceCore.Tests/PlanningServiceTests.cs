using System.Text.Json;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ServiceCore.Planning;
using Xunit;

public class PlanningServiceTests
{
    private class TestDb : cpmRunningContext
    {
        private readonly string _name;
        public TestDb(string name) { _name = name; }
        protected override void OnConfiguring(DbContextOptionsBuilder b)
            => b.UseInMemoryDatabase(_name).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    private const int ProjectId = 1;

    private static (PlanningService Svc, TestDb Db) Maak(int? uitvoeringstermijn = null)
    {
        var db = new TestDb(Guid.NewGuid().ToString());
        db.Project.Add(new Project
        {
            ProjectId = ProjectId,
            ProjectName = "Villa Test",
            StartDateConstruction = new DateOnly(2026, 11, 16), // maandag
            ExecutionDays = uitvoeringstermijn,
        });
        db.SaveChanges();
        return (new PlanningService(db), db);
    }

    private static Dictionary<string, JsonElement> V(params (string Naam, object? Waarde)[] velden)
        => velden.ToDictionary(v => v.Naam, v => JsonSerializer.SerializeToElement(v.Waarde));

    private static PlanningCommando Taak(int versie, int? sectieId, string naam, int duur, string? voor = null)
    {
        var velden = new List<(string, object?)> { ("naam", naam), ("duur", duur) };
        if (voor != null) velden.Add(("voorgangers", voor));
        return new PlanningCommando { Type = "taakToevoegen", Versie = versie, SectieId = sectieId, Velden = V(velden.ToArray()) };
    }

    private static PlanningRij Rij(PlanningStaat s, string wbs) => s.Rijen.Single(r => r.Wbs == wbs);

    private static async Task<(PlanningService Svc, TestDb Db, PlanningStaat Staat)> MetTweeTaken()
    {
        var (svc, db) = Maak();
        var u1 = await svc.Voer(ProjectId, new PlanningCommando { Type = "faseToevoegen", Versie = 0, Velden = V(("naam", "Ruwbouw")) }, "u1");
        Assert.True(u1.Ok, u1.Fout);
        var faseId = u1.Staat!.Rijen.Single().Id;
        var u2 = await svc.Voer(ProjectId, Taak(u1.Staat.Versie, faseId, "Graafwerken", 5), "u1");
        Assert.True(u2.Ok, u2.Fout);
        var u3 = await svc.Voer(ProjectId, Taak(u2.Staat!.Versie, faseId, "Funderingen", 3, "1.1"), "u1");
        Assert.True(u3.Ok, u3.Fout);
        return (svc, db, u3.Staat!);
    }

    [Fact]
    public async Task Een_voorganger_zet_de_opvolger_na_de_voorganger_en_de_kop_klopt()
    {
        var (_, _, s) = await MetTweeTaken();
        var a = Rij(s, "1.1"); var b = Rij(s, "1.2");
        Assert.Equal(("2026-11-16", "2026-11-20"), (a.Start, a.Einde));
        Assert.Equal(("2026-11-23", "2026-11-25"), (b.Start, b.Einde));
        Assert.Equal("1.1", b.Voorgangers);
        Assert.Equal("2026-11-16", s.Kop.Start);
        Assert.Equal("2026-11-25", s.Kop.Einde);
        Assert.Equal(8, s.Kop.DuurWerkdagen);
        Assert.Equal("fase", Rij(s, "1").Soort);
        Assert.Equal("2026-11-25", Rij(s, "1").Einde);
    }

    [Fact]
    public async Task Een_langere_duur_verschuift_de_opvolger_en_meldt_dat()
    {
        var (svc, _, s) = await MetTweeTaken();
        var a = Rij(s, "1.1");
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = a.Id, Versie = s.Versie, Velden = V(("duur", 8)) }, "u1");
        Assert.True(u.Ok, u.Fout);
        Assert.Equal("2026-11-26", Rij(u.Staat!, "1.2").Start);   // 16 nov + 8 werkdagen = einde do 25 nov, start do 26 nov
        Assert.Contains("t" + Rij(u.Staat!, "1.2").Id, u.Verschoven);
        Assert.DoesNotContain("t" + a.Id, u.Verschoven);
        Assert.NotNull(u.Samenvatting);
        Assert.Contains("1 andere taak verschoven", u.Samenvatting);
        Assert.Contains("einde +3 werkdagen", u.Samenvatting);
    }

    [Fact]
    public async Task Ongedaan_maken_zet_alles_terug()
    {
        var (svc, _, s) = await MetTweeTaken();
        var a = Rij(s, "1.1");
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = a.Id, Versie = s.Versie, Velden = V(("duur", 8)) }, "u1");
        Assert.NotNull(u.Undo);
        var h = await svc.Voer(ProjectId, new PlanningCommando { Type = "herstel", Versie = u.Staat!.Versie, Momentopname = u.Undo }, "u1");
        Assert.True(h.Ok, h.Fout);
        Assert.Equal(5, Rij(h.Staat!, "1.1").Duur);
        Assert.Equal("2026-11-23", Rij(h.Staat!, "1.2").Start);
        Assert.Equal("1.1", Rij(h.Staat!, "1.2").Voorgangers);
    }

    [Fact]
    public async Task Een_cyclus_wordt_geweigerd()
    {
        var (svc, _, s) = await MetTweeTaken();
        var a = Rij(s, "1.1");
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = a.Id, Versie = s.Versie, Velden = V(("voorgangers", "1.2")) }, "u1");
        Assert.False(u.Ok);
        Assert.Contains("cyclus", u.Fout, StringComparison.OrdinalIgnoreCase);
        // (Dat er niets bewaard blijft, komt van de databasetransactie; de InMemory-provider kent geen rollback en toont dat dus niet.)
    }

    [Fact]
    public async Task Een_onbekende_of_ongeldige_voorganger_geeft_een_duidelijke_fout()
    {
        var (svc, _, s) = await MetTweeTaken();
        var b = Rij(s, "1.2");
        var u1 = await svc.Voer(ProjectId, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = b.Id, Versie = s.Versie, Velden = V(("voorgangers", "9.9")) }, "u1");
        Assert.False(u1.Ok); Assert.Contains("9.9", u1.Fout);
        var u2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = b.Id, Versie = s.Versie, Velden = V(("voorgangers", "abc")) }, "u1");
        Assert.False(u2.Ok); Assert.Contains("geen geldige voorganger", u2.Fout);
    }

    [Fact]
    public async Task Voorgangertype_en_wachttijd_worden_gelezen_en_teruggeschreven()
    {
        var (svc, _, s) = await MetTweeTaken();
        var b = Rij(s, "1.2");
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = b.Id, Versie = s.Versie, Velden = V(("voorgangers", "1.1SS+2d")) }, "u1");
        Assert.True(u.Ok, u.Fout);
        Assert.Equal("1.1SS+2d", Rij(u.Staat!, "1.2").Voorgangers);
        Assert.Equal("2026-11-18", Rij(u.Staat!, "1.2").Start);
    }

    [Fact]
    public async Task Een_verouderde_versie_geeft_een_conflict()
    {
        var (svc, _, s) = await MetTweeTaken();
        var u = await svc.Voer(ProjectId, Taak(s.Versie - 1, Rij(s, "1").Id, "Te laat", 1), "u2");
        Assert.True(u.Conflict);
        Assert.False(u.Ok);
        Assert.NotNull(u.Staat);
    }

    [Fact]
    public async Task Voortgang_100_vinkt_een_taak_af_en_een_fase_telt_gewogen_op_duur()
    {
        var (svc, _, s) = await MetTweeTaken();
        var a = Rij(s, "1.1");   // 5 dagen
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = a.Id, Versie = s.Versie, Velden = V(("voortgang", 100)) }, "u1");
        Assert.True(u.Ok, u.Fout);
        Assert.True(Rij(u.Staat!, "1.1").Afgerond);
        Assert.NotNull(Rij(u.Staat!, "1.1").WerkelijkEinde);
        Assert.Equal(63, Rij(u.Staat!, "1").Voortgang);   // (5*100 + 3*0) / 8 = 62,5 → 63
    }

    [Fact]
    public async Task Verplaatsen_inspringen_en_uitspringen()
    {
        var (svc, _, s) = await MetTweeTaken();
        var f2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "faseToevoegen", Versie = s.Versie, Velden = V(("naam", "Afwerking")) }, "u1");
        Assert.True(f2.Ok, f2.Fout);
        Assert.Equal("Afwerking", Rij(f2.Staat!, "2").Naam);

        // Omlaag: fase 2 staat achter fase 1; omhoog zet ze ervoor
        var omhoog = await svc.Voer(ProjectId, new PlanningCommando { Type = "verplaats", Soort = "fase", Id = Rij(f2.Staat!, "2").Id, Actie = "omhoog", Versie = f2.Staat!.Versie }, "u1");
        Assert.True(omhoog.Ok, omhoog.Fout);
        Assert.Equal("Afwerking", Rij(omhoog.Staat!, "1").Naam);
        Assert.Equal("Ruwbouw", Rij(omhoog.Staat!, "2").Naam);

        // Inspringen: Ruwbouw (2) komt onder Afwerking (1) als deelfase
        var in1 = await svc.Voer(ProjectId, new PlanningCommando { Type = "verplaats", Soort = "fase", Id = Rij(omhoog.Staat!, "2").Id, Actie = "inspringen", Versie = omhoog.Staat!.Versie }, "u1");
        Assert.True(in1.Ok, in1.Fout);
        Assert.Equal("Ruwbouw", Rij(in1.Staat!, "1.1").Naam);
        Assert.Equal("Graafwerken", Rij(in1.Staat!, "1.1.1").Naam);

        // Uitspringen: Ruwbouw is weer een hoofdfase
        var uit = await svc.Voer(ProjectId, new PlanningCommando { Type = "verplaats", Soort = "fase", Id = Rij(in1.Staat!, "1.1").Id, Actie = "uitspringen", Versie = in1.Staat!.Versie }, "u1");
        Assert.True(uit.Ok, uit.Fout);
        Assert.Equal("Ruwbouw", Rij(uit.Staat!, "2").Naam);

        // Een taak mag niet uit haar hoofdfase springen
        var fout = await svc.Voer(ProjectId, new PlanningCommando { Type = "verplaats", Soort = "taak", Id = Rij(uit.Staat!, "2.1").Id, Actie = "uitspringen", Versie = uit.Staat!.Versie }, "u1");
        Assert.False(fout.Ok);
    }

    [Fact]
    public async Task Naar_een_plaats_slepen_verplaatst_een_taak_tussen_fases_en_weigert_onzin()
    {
        var (svc, _, s) = await MetTweeTaken();   // fase 1 (Ruwbouw) met 1.1 Graafwerken en 1.2 Funderingen
        var f2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "faseToevoegen", Versie = s.Versie, EersteTaakNaam = "Dak", EersteTaakDuur = 2, Velden = V(("naam", "Afwerking")) }, "u1");
        Assert.True(f2.Ok, f2.Fout);
        var fase1 = Rij(f2.Staat!, "1").Id; var fase2 = Rij(f2.Staat!, "2").Id;
        var fundering = Rij(f2.Staat!, "1.2").Id; var dak = Rij(f2.Staat!, "2.1").Id;

        // Funderingen vooraan in fase 2
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "verplaats", Soort = "taak", Id = fundering, Actie = "naar", SectieId = fase2, NaSoort = "begin", Versie = f2.Staat!.Versie }, "u1");
        Assert.True(u.Ok, u.Fout);
        Assert.Equal("Funderingen", Rij(u.Staat!, "2.1").Naam);
        Assert.Equal("Dak", Rij(u.Staat!, "2.2").Naam);
        Assert.Single(u.Staat!.Rijen.Where(r => r.Wbs.StartsWith("1.")));

        // Fase 2 vóór fase 1 zetten (hoogste niveau, achteraan = standaard), dan tussen twee taken
        var u2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "verplaats", Soort = "taak", Id = dak, Actie = "naar", SectieId = fase1, NaId = Rij(u.Staat!, "1.1").Id, NaSoort = "taak", Versie = u.Staat!.Versie }, "u1");
        Assert.True(u2.Ok, u2.Fout);
        Assert.Equal("Dak", Rij(u2.Staat!, "1.2").Naam);

        // Een taak kan niet op het hoogste niveau; een fase niet in zichzelf
        var fout1 = await svc.Voer(ProjectId, new PlanningCommando { Type = "verplaats", Soort = "taak", Id = dak, Actie = "naar", Versie = u2.Staat!.Versie }, "u1");
        Assert.False(fout1.Ok);
        var fout2 = await svc.Voer(ProjectId, new PlanningCommando { Type = "verplaats", Soort = "fase", Id = fase1, Actie = "naar", SectieId = fase1, Versie = u2.Staat!.Versie }, "u1");
        Assert.False(fout2.Ok);
    }

    [Fact]
    public async Task Een_fase_verwijderen_neemt_haar_taken_en_hun_afhankelijkheden_mee()
    {
        var (svc, db, s) = await MetTweeTaken();
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "verwijder", Versie = s.Versie, Items = new() { new PlanningItemRef { Soort = "fase", Id = Rij(s, "1").Id } } }, "u1");
        Assert.True(u.Ok, u.Fout);
        Assert.Empty(u.Staat!.Rijen);
        Assert.Empty(db.PlanningTaak);
        Assert.Empty(db.PlanningAfhankelijkheid);
    }

    [Fact]
    public async Task Een_taak_verwijderen_haalt_de_voorganger_bij_de_opvolger_weg()
    {
        var (svc, _, s) = await MetTweeTaken();
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "verwijder", Versie = s.Versie, Items = new() { new PlanningItemRef { Soort = "taak", Id = Rij(s, "1.1").Id } } }, "u1");
        Assert.True(u.Ok, u.Fout);
        Assert.Equal("", Rij(u.Staat!, "1.1").Voorgangers);
        Assert.Equal("Funderingen", Rij(u.Staat!, "1.1").Naam);
    }

    [Fact]
    public async Task De_deadline_komt_uit_aanvangsdatum_en_uitvoeringstermijn_met_marge()
    {
        var (svc, db) = Maak(uitvoeringstermijn: 10);
        var f = await svc.Voer(ProjectId, new PlanningCommando { Type = "faseToevoegen", Versie = 0, Velden = V(("naam", "Ruwbouw")), EersteTaakNaam = "Alles", EersteTaakDuur = 8 }, "u1");
        Assert.True(f.Ok, f.Fout);
        // aanvang ma 16/11 + 10 werkdagen = ma 30/11; de planning eindigt do 25/11 → 3 werkdagen marge
        Assert.Equal("2026-11-30", f.Staat!.Kop.Deadline);
        Assert.Equal("2026-11-25", f.Staat.Kop.Einde);
        Assert.Equal(3, f.Staat.Kop.Marge);
        Assert.Contains(f.Staat.Meldingen, m => m.Titel == "Geen weerstation");
    }

    [Fact]
    public async Task Een_te_late_planning_geeft_een_waarschuwing()
    {
        var (svc, _) = Maak(uitvoeringstermijn: 5);
        var f = await svc.Voer(ProjectId, new PlanningCommando { Type = "faseToevoegen", Versie = 0, Velden = V(("naam", "Ruwbouw")), EersteTaakNaam = "Alles", EersteTaakDuur = 8 }, "u1");
        Assert.True(f.Ok, f.Fout);
        Assert.True(f.Staat!.Kop.Marge < 0);
        Assert.Contains(f.Staat.Meldingen, m => m.Ernst == "danger" && m.Titel.Contains("voorziene oplevering"));
    }

    [Fact]
    public async Task Een_klant_met_afwijkende_termijn_geeft_een_eigen_waarschuwing()
    {
        var (svc, db) = Maak(uitvoeringstermijn: 20);
        db.ClientAccount.Add(new ClientAccount { Id = 5, Name = "Familie Peeters", ExecutionDays = 5 });
        db.Units.Add(new Units { ProjectId = ProjectId, Name = "A1", ClientAccountId = 5 });
        await db.SaveChangesAsync();
        var f = await svc.Voer(ProjectId, new PlanningCommando { Type = "faseToevoegen", Versie = 0, Velden = V(("naam", "Ruwbouw")), EersteTaakNaam = "Alles", EersteTaakDuur = 8 }, "u1");
        Assert.True(f.Ok, f.Fout);
        Assert.Contains(f.Staat!.Meldingen, m => m.Titel.Contains("Familie Peeters"));
        Assert.DoesNotContain(f.Staat.Meldingen, m => m.Ernst == "danger");   // het project zelf is niet te laat
    }

    [Fact]
    public async Task Basisplanning_vastleggen_meet_daarna_de_afwijking()
    {
        var (svc, _, s) = await MetTweeTaken();
        var b = await svc.Voer(ProjectId, new PlanningCommando { Type = "basisVastleggen", Versie = s.Versie }, "u1");
        Assert.True(b.Ok, b.Fout);
        Assert.NotNull(b.Staat!.Kop.BasisOp);
        Assert.Equal(0, b.Staat.Kop.BasisAfwijking);
        var u = await svc.Voer(ProjectId, new PlanningCommando { Type = "wijzig", Soort = "taak", Id = Rij(b.Staat, "1.1").Id, Versie = b.Staat.Versie, Velden = V(("duur", 7)) }, "u1");
        Assert.Equal(2, u.Staat!.Kop.BasisAfwijking);
        Assert.Equal(2, Rij(u.Staat, "1.2").Afwijking);
    }

    [Fact]
    public async Task De_traject_herberekening_gebruikt_de_geplande_einddatum_van_een_gekoppelde_taak()
    {
        var (_, db, s) = await MetTweeTaken();
        var taken = await db.PlanningTaak.ToListAsync();
        var secties = await db.PlanningSectie.ToListAsync();
        var b = taken.Single(t => t.Naam == "Funderingen");
        var m = new Mijlpaal { BronBinding = 4, BronRefId = b.Id };
        Assert.Equal(new DateOnly(2026, 11, 25), ServiceCore.Traject.TrajectRecalculationService.GeplandUitPlanning(m, taken, secties));
        var mFase = new Mijlpaal { BronBinding = 5, BronRefId = secties.Single().Id };
        Assert.Equal(new DateOnly(2026, 11, 25), ServiceCore.Traject.TrajectRecalculationService.GeplandUitPlanning(mFase, taken, secties));
        Assert.Null(ServiceCore.Traject.TrajectRecalculationService.GeplandUitPlanning(new Mijlpaal(), taken, secties));
    }

    // ── Koppeling met een trajectmijlpaal ──

    private static async Task<(Mijlpaal M, Mijlpaal Vreemd)> MaakTraject(TestDb db)
    {
        var traject = new Projecttraject { ProjectId = ProjectId, Naam = "Traject", Status = 0 };
        db.Projecttraject.Add(traject);
        db.Project.Add(new Project { ProjectId = 2, ProjectName = "Ander project" });
        await db.SaveChangesAsync();
        var anderTraject = new Projecttraject { ProjectId = 2, Naam = "Ander traject", Status = 0 };
        db.Projecttraject.Add(anderTraject);
        await db.SaveChangesAsync();
        var m = new Mijlpaal { ProjecttrajectId = traject.Id, Code = "RUWBOUW", Naam = "Ruwbouw klaar", Status = 0 };
        var vreemd = new Mijlpaal { ProjecttrajectId = anderTraject.Id, Code = "X", Naam = "Van een ander project", Status = 0 };
        db.Mijlpaal.AddRange(m, vreemd);
        await db.SaveChangesAsync();
        return (m, vreemd);
    }

    [Fact]
    public async Task Een_vrije_trajectmijlpaal_koppelen_aan_een_taak_en_weer_losmaken()
    {
        var (svc, db, s) = await MetTweeTaken();
        var (m, _) = await MaakTraject(db);
        var taak = await db.PlanningTaak.SingleAsync(t => t.Naam == "Funderingen");

        // De keuzelijst toont de vrije mijlpaal, nog niet gekoppeld.
        var lijst = (await svc.Laad(ProjectId)).TrajectMijlpalen;
        Assert.Equal("Ruwbouw klaar", Assert.Single(lijst).Naam);
        Assert.Null(lijst[0].GekoppeldAanSleutel);

        var u = await svc.Koppel(ProjectId, new PlanningKoppelCommando { Soort = "taak", Id = taak.Id, MijlpaalId = m.Id }, "u1");
        Assert.True(u.Ok, u.Fout);
        var opgeslagen = await db.Mijlpaal.AsNoTracking().SingleAsync(x => x.Id == m.Id);
        Assert.Equal((4, taak.Id), (opgeslagen.BronBinding, opgeslagen.BronRefId));
        Assert.Equal("t" + taak.Id, u.Staat!.TrajectMijlpalen.Single().GekoppeldAanSleutel);
        Assert.Equal("1.2 Funderingen", u.Staat.TrajectMijlpalen.Single().GekoppeldAanTekst);
        Assert.Contains(Rij(u.Staat, "1.2").Mijlpalen, x => x.Id == m.Id);

        var los = await svc.Koppel(ProjectId, new PlanningKoppelCommando { Soort = "taak", Id = taak.Id, MijlpaalId = null, VorigeMijlpaalId = m.Id }, "u1");
        Assert.True(los.Ok, los.Fout);
        var vrij = await db.Mijlpaal.AsNoTracking().SingleAsync(x => x.Id == m.Id);
        Assert.Null(vrij.BronBinding);
        Assert.Null(vrij.BronRefId);
    }

    [Fact]
    public async Task Een_mijlpaal_koppelen_aan_een_fase_gebruikt_binding_5()
    {
        var (svc, db, _) = await MetTweeTaken();
        var (m, _) = await MaakTraject(db);
        var fase = await db.PlanningSectie.SingleAsync();
        var u = await svc.Koppel(ProjectId, new PlanningKoppelCommando { Soort = "fase", Id = fase.Id, MijlpaalId = m.Id }, "u1");
        Assert.True(u.Ok, u.Fout);
        var opgeslagen = await db.Mijlpaal.AsNoTracking().SingleAsync(x => x.Id == m.Id);
        Assert.Equal((5, fase.Id), (opgeslagen.BronBinding, opgeslagen.BronRefId));
    }

    [Fact]
    public async Task Een_mijlpaal_die_al_iets_anders_volgt_wordt_geweigerd_en_blijft_ongewijzigd()
    {
        var (svc, db, _) = await MetTweeTaken();
        var (m, _) = await MaakTraject(db);
        m.BronBinding = 1; // volgt een projectdocument
        await db.SaveChangesAsync();
        var taak = await db.PlanningTaak.FirstAsync();

        Assert.DoesNotContain(m.Id, (await svc.Laad(ProjectId)).TrajectMijlpalen.Select(x => x.Id));
        var u = await svc.Koppel(ProjectId, new PlanningKoppelCommando { Soort = "taak", Id = taak.Id, MijlpaalId = m.Id }, "u1");
        Assert.False(u.Ok);
        Assert.Contains("automatisch", u.Fout);
        Assert.Equal(1, (await db.Mijlpaal.AsNoTracking().SingleAsync(x => x.Id == m.Id)).BronBinding);
    }

    [Fact]
    public async Task Een_mijlpaal_van_een_ander_project_of_een_onbekende_taak_wordt_geweigerd()
    {
        var (svc, db, _) = await MetTweeTaken();
        var (_, vreemd) = await MaakTraject(db);
        var taak = await db.PlanningTaak.FirstAsync();

        var a = await svc.Koppel(ProjectId, new PlanningKoppelCommando { Soort = "taak", Id = taak.Id, MijlpaalId = vreemd.Id }, "u1");
        Assert.False(a.Ok);
        Assert.Null((await db.Mijlpaal.AsNoTracking().SingleAsync(x => x.Id == vreemd.Id)).BronBinding);

        var b = await svc.Koppel(ProjectId, new PlanningKoppelCommando { Soort = "taak", Id = 9999, MijlpaalId = vreemd.Id }, "u1");
        Assert.False(b.Ok);
    }

    [Fact]
    public async Task Losmaken_raakt_een_mijlpaal_niet_die_aan_een_andere_taak_hangt()
    {
        var (svc, db, _) = await MetTweeTaken();
        var (m, _) = await MaakTraject(db);
        var taken = await db.PlanningTaak.OrderBy(t => t.Volgorde).ToListAsync();
        await svc.Koppel(ProjectId, new PlanningKoppelCommando { Soort = "taak", Id = taken[1].Id, MijlpaalId = m.Id }, "u1");

        // "Vorige" verwijst naar de mijlpaal, maar die hangt niet aan taak 1: ze moet blijven staan.
        var u = await svc.Koppel(ProjectId, new PlanningKoppelCommando { Soort = "taak", Id = taken[0].Id, MijlpaalId = null, VorigeMijlpaalId = m.Id }, "u1");
        Assert.True(u.Ok, u.Fout);
        var nog = await db.Mijlpaal.AsNoTracking().SingleAsync(x => x.Id == m.Id);
        Assert.Equal((4, taken[1].Id), (nog.BronBinding, nog.BronRefId));
    }
}
