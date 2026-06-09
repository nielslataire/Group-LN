using System;
using System.Collections.Generic;

namespace ServiceCore.Budget;

public class BudgetFormulaRegistry {
    private record FormuleDef(string Label, Func<BudgetFormulaContext, decimal?> Fn);
    private readonly Dictionary<string, FormuleDef> _defs = new();

    public BudgetFormulaRegistry() {
        // ── Stap 1: Projectgegevens ──────────────────────────────────────────
        Register(
            FormulaSleutels.NacalcRuwbouwBasis,
            "Nacalc basisprijs ruwbouw (€/m²)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.NacalcRuwbouwBasis)
                ? ctx.M(FormulaSleutels.NacalcRuwbouwBasis) * ctx.MIndexFactor(FormulaSleutels.NacalcRuwbouwBasis)
                : (decimal?)null
        );
        Register(
            FormulaSleutels.BovenbouwGevelmetselwerk,
            "Gevelmetselwerk (€/m²)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.BovenbouwGevelmetselwerk)
                ? ctx.M(FormulaSleutels.BovenbouwGevelmetselwerk) * ctx.MIndexFactor(FormulaSleutels.BovenbouwGevelmetselwerk)
                : (decimal?)null
        );
        Register(
            FormulaSleutels.BovenbouwGipsblokken,
            "Gipsblokken (€/m²)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.BovenbouwGipsblokken)
                ? ctx.M(FormulaSleutels.BovenbouwGipsblokken) * ctx.MIndexFactor(FormulaSleutels.BovenbouwGipsblokken)
                : (decimal?)null
        );
        Register(
            FormulaSleutels.BovenbouwTerras,
            "Terras (€/m²)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.BovenbouwTerras)
                ? ctx.M(FormulaSleutels.BovenbouwTerras) * ctx.MIndexFactor(FormulaSleutels.BovenbouwTerras)
                : (decimal?)null
        );

        // ── Stap 2: Onderbouw ────────────────────────────────────────────────────
        Register(
            FormulaSleutels.OnderbouwBerlinerwanden,
            "Berlinerwanden (€/lm)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.OnderbouwBerlinerwanden)
                ? ctx.M(FormulaSleutels.OnderbouwBerlinerwanden) * ctx.MIndexFactor(FormulaSleutels.OnderbouwBerlinerwanden)
                : (decimal?)null
        );
        Register(
            FormulaSleutels.OnderbouwSecanpalen,
            "Secanpalen (€/lm)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.OnderbouwSecanpalen)
                ? ctx.M(FormulaSleutels.OnderbouwSecanpalen) * ctx.MIndexFactor(FormulaSleutels.OnderbouwSecanpalen)
                : (decimal?)null
        );
        Register(
            FormulaSleutels.OnderbouwFunderingen,
            "Funderingen (€/m²)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.OnderbouwFunderingen)
                ? ctx.M(FormulaSleutels.OnderbouwFunderingen) * ctx.MIndexFactor(FormulaSleutels.OnderbouwFunderingen)
                : (decimal?)null
        );
        Register(
            FormulaSleutels.OnderbouwGrondwerken,
            "Grondwerken (€/m³)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.OnderbouwGrondwerken)
                ? ctx.M(FormulaSleutels.OnderbouwGrondwerken) * ctx.MIndexFactor(FormulaSleutels.OnderbouwGrondwerken)
                : (decimal?)null
        );
        Register(
            FormulaSleutels.OnderbouwOnderschoeiingen,
            "Onderschoeiingen (€/m³)",
            ctx => ctx.HeeftMateriaal(FormulaSleutels.OnderbouwOnderschoeiingen)
                ? ctx.M(FormulaSleutels.OnderbouwOnderschoeiingen) * ctx.MIndexFactor(FormulaSleutels.OnderbouwOnderschoeiingen)
                : (decimal?)null
        );

        // Voeg hieronder nieuwe formules toe zodra een nieuw veld gekoppeld wordt.
        // Beschikbare helpers op ctx:
        //   ctx.M("sleutel")              prijs van gekoppeld materiaal
        //   ctx.GewogenIndex              I×0.4 + S×0.4 + 0.2
        //   ctx.AantalEenheden("Woning")  aantal woningen
        //   ctx.TotaleOpp("Woning")       bewoonbare opp per groep
        //   ctx.TotaleOpp()               totale bewoonbare opp
        //   ctx.Pct("naam")               bouwkost % / 100
        //   ctx.F("andere_sleutel")       resultaat van andere formule
        //   ctx.Gegevens.AantalLiften     etc.
    }

    private void Register(string sleutel, string label, Func<BudgetFormulaContext, decimal?> fn)
        => _defs[sleutel] = new FormuleDef(label, fn);

    public decimal? Evaluate(string sleutel, BudgetFormulaContext ctx) {
        if (!_defs.TryGetValue(sleutel, out var def)) return null;
        try { return def.Fn(ctx); }
        catch { return null; }
    }

    public bool HeeftFormule(string sleutel) => _defs.ContainsKey(sleutel);
    public string GetLabel(string sleutel) => _defs.TryGetValue(sleutel, out var d) ? d.Label : sleutel;
    public IEnumerable<string> AlleSleutels => _defs.Keys;
}
