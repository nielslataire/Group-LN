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
                ? ctx.M(FormulaSleutels.NacalcRuwbouwBasis) * ctx.GewogenIndex
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
