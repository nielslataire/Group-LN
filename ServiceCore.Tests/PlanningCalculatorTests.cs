using ServiceCore.Planning;
using Xunit;

public class PlanningCalculatorTests
{
    // Maandag 16/11/2026 is een werkdag; 21/22 november zijn weekend.
    private static readonly DateOnly Ma = new(2026, 11, 16);
    private static DateOnly D(int dag) => new(2026, 11, dag);

    private static PlanTaak T(int id, int duur, params PlanLink[] voorgangers) => new(id, duur, voorgangers);

    private static PlanResultaat Run(IReadOnlyList<PlanTaak> taken, WerkKalender? k = null, bool metVerlet = true)
        => PlanningCalculator.Bereken(taken, Ma, k ?? new WerkKalender(), metVerlet);

    // ── Werkkalender ──

    [Fact]
    public void Kalender_slaat_weekends_over_en_telt_de_startdag_niet_mee()
    {
        var k = new WerkKalender();
        Assert.Equal(D(20), k.Voeg(Ma, 4));          // ma + 4 = vr
        Assert.Equal(D(23), k.Voeg(Ma, 5));          // ma + 5 = volgende ma
        Assert.Equal(Ma, k.Voeg(D(23), -5));
        Assert.Equal(Ma, k.Voeg(Ma, 0));
    }

    [Fact]
    public void Kalender_slaat_feestdagen_en_verlet_over_maar_verlet_kan_uit()
    {
        var k = new WerkKalender(verlof: new[] { D(17) }, verlet: new[] { D(18) });
        Assert.Equal(D(19), k.Voeg(Ma, 1));                       // di (verlof) en wo (verlet) vallen weg
        Assert.Equal(D(18), k.Voeg(Ma, 1, metVerlet: false));     // zonder verlet: wo
        Assert.Equal(3, k.Tel(Ma, D(20), metVerlet: false));      // wo, do, vr (di is verlof)
        Assert.Equal(2, k.Tel(Ma, D(20)));                        // zonder wo (verlet): do, vr
    }

    [Fact]
    public void Kalender_telt_werkdagen_tot_een_datum_zoals_de_oude_BusinessDaysUntil()
    {
        var k = new WerkKalender(verlof: new[] { D(18) });
        Assert.Equal(0, k.Tel(D(20), D(16)));
        Assert.Equal(3, k.Tel(Ma, D(20)));   // di, do, vr (wo is verlof)
    }

    // ── Afhankelijkheden ──

    [Fact]
    public void FS_start_de_werkdag_na_het_einde_van_de_voorganger()
    {
        var r = Run(new[] { T(1, 3), T(2, 2, new PlanLink(1)) });
        Assert.Equal((Ma, D(18)), (r.Taken[1].Start, r.Taken[1].Einde));
        Assert.Equal((D(19), D(20)), (r.Taken[2].Start, r.Taken[2].Einde));
    }

    [Fact]
    public void FS_over_het_weekend()
    {
        var r = Run(new[] { T(1, 5), T(2, 1, new PlanLink(1)) });
        Assert.Equal(D(20), r.Taken[1].Einde);
        Assert.Equal(D(23), r.Taken[2].Start);
    }

    [Fact]
    public void FS_met_wachttijd_en_met_overlap()
    {
        var r = Run(new[] { T(1, 5), T(2, 1, new PlanLink(1, PlanLinkType.FS, 3)), T(3, 1, new PlanLink(1, PlanLinkType.FS, -2)) });
        Assert.Equal(D(26), r.Taken[2].Start);   // vr 20 + 4 werkdagen
        Assert.Equal(D(19), r.Taken[3].Start);   // twee dagen vroeger dan FS+0 (ma 23): do 19
    }

    [Fact]
    public void SS_en_FF()
    {
        var r = Run(new[]
        {
            T(1, 5),
            T(2, 2, new PlanLink(1, PlanLinkType.SS, 2)),
            T(3, 3, new PlanLink(1, PlanLinkType.FF, 0)),
        });
        Assert.Equal(D(18), r.Taken[2].Start);
        Assert.Equal(D(20), r.Taken[3].Einde);
        Assert.Equal(D(18), r.Taken[3].Start);
    }

    [Fact]
    public void Meerdere_voorgangers_de_laatste_bepaalt_de_start()
    {
        var r = Run(new[] { T(1, 2), T(2, 4), T(3, 1, new PlanLink(1), new PlanLink(2)) });
        Assert.Equal(D(20), r.Taken[3].Start);
        Assert.Equal(2, r.Taken[3].BepalendeVoorgangerId);
    }

    [Fact]
    public void Een_mijlpaal_met_duur_nul_start_en_eindigt_dezelfde_dag()
    {
        var r = Run(new[] { T(1, 2), T(2, 0, new PlanLink(1)) });
        Assert.Equal(D(18), r.Taken[2].Start);
        Assert.Equal(D(18), r.Taken[2].Einde);
    }

    [Fact]
    public void Een_verlofdag_verschuift_de_opvolgers()
    {
        var k = new WerkKalender(verlof: new[] { D(17) });
        var r = Run(new[] { T(1, 3), T(2, 1, new PlanLink(1)) }, k);
        Assert.Equal(D(19), r.Taken[1].Einde);
        Assert.Equal(D(20), r.Taken[2].Start);
    }

    [Fact]
    public void Verlet_telt_mee_tenzij_uitgezet()
    {
        var k = new WerkKalender(verlet: new[] { D(17) });
        Assert.Equal(D(19), Run(new[] { T(1, 3) }, k).Taken[1].Einde);
        Assert.Equal(D(18), Run(new[] { T(1, 3) }, k, metVerlet: false).Taken[1].Einde);
    }

    // ── Beperkingen en werkelijke data ──

    [Fact]
    public void Moet_beginnen_op_schuift_niet_mee()
    {
        var vast = new PlanTaak(2, 1, new[] { new PlanLink(1) }, PlanBeperking.MoetBeginnenOp, D(17));
        var r = Run(new[] { T(1, 5), vast });
        Assert.Equal(D(17), r.Taken[2].Start);
    }

    [Fact]
    public void Niet_eerder_beginnen_dan_duwt_naar_later_en_niet_later_dan_waarschuwt_enkel()
    {
        var eerder = new PlanTaak(2, 1, new[] { new PlanLink(1) }, PlanBeperking.NietEerderBeginnenDan, D(25));
        var later = new PlanTaak(3, 1, new[] { new PlanLink(1) }, PlanBeperking.NietLaterBeginnenDan, D(17));
        var r = Run(new[] { T(1, 2), eerder, later });
        Assert.Equal(D(25), r.Taken[2].Start);
        Assert.Equal(D(18), r.Taken[3].Start);   // niet verschoven
        Assert.Contains(r.Taken[3].Waarschuwingen, w => w.Type == PlanWaarschuwingType.BeperkingOverschreden && w.Dagen == 1);
    }

    [Fact]
    public void Moet_eindigen_op_rekent_de_start_terug()
    {
        var t = new PlanTaak(1, 3, Array.Empty<PlanLink>(), PlanBeperking.MoetEindigenOp, D(26));
        var r = Run(new[] { t });
        Assert.Equal(D(24), r.Taken[1].Start);
        Assert.Equal(D(26), r.Taken[1].Einde);
    }

    [Fact]
    public void Werkelijk_einde_overschrijft_en_de_opvolger_rekent_daarvan()
    {
        var klaar = new PlanTaak(1, 3, Array.Empty<PlanLink>(), WerkelijkEinde: D(24), IsAfgerond: true);
        var r = Run(new[] { klaar, T(2, 1, new PlanLink(1)) });
        Assert.Equal(D(24), r.Taken[1].Einde);
        Assert.Equal(D(25), r.Taken[2].Start);
        Assert.False(r.Taken[1].IsKritiek);
    }

    [Fact]
    public void Werkelijke_start_overschrijft_de_planning()
    {
        var t = new PlanTaak(1, 2, Array.Empty<PlanLink>(), WerkelijkStart: D(18));
        var r = Run(new[] { t });
        Assert.Equal((D(18), D(19)), (r.Taken[1].Start, r.Taken[1].Einde));
    }

    // ── Kritiek pad, waarschuwingen, cyclus ──

    [Fact]
    public void Kritiek_pad_en_speling()
    {
        // 1 (3d) → 2 (4d) is het langste pad; 3 (1d) loopt parallel zonder opvolger.
        var r = Run(new[] { T(1, 3), T(2, 4, new PlanLink(1)), T(3, 1) });
        Assert.True(r.Taken[1].IsKritiek);
        Assert.True(r.Taken[2].IsKritiek);
        Assert.False(r.Taken[3].IsKritiek);
        Assert.Equal(0, r.Taken[2].Speling);
        Assert.Equal(r.Taken[2].Einde, r.Einde);
        Assert.True(r.Taken[3].Speling > 0);
    }

    [Fact]
    public void Een_tak_met_een_kortere_opvolger_heeft_speling_tot_de_opvolger()
    {
        // 1 (1d) → 3 (1d) ; 2 (5d) is het langste pad. 3 volgt 1 en 2 niet.
        var r = Run(new[] { T(1, 1), T(2, 5), T(3, 1, new PlanLink(1)) });
        Assert.True(r.Taken[2].IsKritiek);
        Assert.False(r.Taken[1].IsKritiek);
        Assert.Equal(3, r.Taken[1].Speling);   // 1 en 3 kunnen samen 3 werkdagen opschuiven (tot vr 20)
    }

    [Fact]
    public void Deadline_overschreden_geeft_waarschuwing_met_werkdagen_te_laat()
    {
        var t = new PlanTaak(1, 5, Array.Empty<PlanLink>(), Deadline: D(18));
        var w = Run(new[] { t }).Taken[1].Waarschuwingen.Single();
        Assert.Equal(PlanWaarschuwingType.DeadlineOverschreden, w.Type);
        Assert.Equal(2, w.Dagen);
    }

    [Fact]
    public void Taak_zonder_voorganger_buiten_de_eerste_krijgt_een_aandachtspunt()
    {
        var r = Run(new[] { T(1, 2), T(2, 2) });
        Assert.Empty(r.Taken[1].Waarschuwingen);
        Assert.Contains(r.Taken[2].Waarschuwingen, w => w.Type == PlanWaarschuwingType.GeenVoorganger);
    }

    [Fact]
    public void Cyclus_wordt_gemeld_zonder_te_crashen()
    {
        var r = Run(new[] { T(1, 1, new PlanLink(2)), T(2, 1, new PlanLink(1)), T(3, 1) });
        Assert.Equal(new[] { 1, 2 }, r.Cyclus.OrderBy(x => x));
        Assert.True(r.Taken[1].InCyclus);
        Assert.False(r.Taken[3].InCyclus);
    }

    [Fact]
    public void Lege_planning_en_onbekende_voorganger()
    {
        Assert.Empty(Run(Array.Empty<PlanTaak>()).Taken);
        var r = Run(new[] { T(1, 2, new PlanLink(99)) });
        Assert.Equal(Ma, r.Taken[1].Start);
    }
}
