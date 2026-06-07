using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Budget;

public class BudgetFormulaContext {
    private readonly Dictionary<string, decimal>  _prijzen;
    private readonly Dictionary<string, string>   _namen;
    private readonly Dictionary<string, decimal>  _percentages;
    private readonly List<BudgetOppervlaktes>      _oppervlaktes;
    private readonly BudgetFormulaRegistry        _registry;
    private readonly Dictionary<string, decimal?> _cache = new();
    private readonly HashSet<string>              _bezig = new();

    public BudgetGegevensBO Gegevens { get; }

    public decimal GewogenIndex {
        get {
            var s = Gegevens.SIndexStart > 0 ? (Gegevens.SIndexHuidig ?? 1m) / Gegevens.SIndexStart.Value : 1m;
            var i = Gegevens.IIndexStart > 0 ? (Gegevens.IIndexHuidig ?? 1m) / Gegevens.IIndexStart.Value : 1m;
            return i * 0.4m + s * 0.4m + 0.2m;
        }
    }

    // Aantallen eenheden per UnitGroupType naam (bv. "Woning", "Appartement")
    public int AantalEenheden(string groepNaam) =>
        _oppervlaktes.Count(o => string.Equals(o.EenheidNaam, groepNaam, StringComparison.OrdinalIgnoreCase));

    // Totale bewoonbare oppervlakte per groep
    public decimal TotaleOpp(string groepNaam) =>
        _oppervlaktes
            .Where(o => string.Equals(o.EenheidNaam, groepNaam, StringComparison.OrdinalIgnoreCase))
            .Sum(o => o.BewoonbareOpp);

    // Totale bewoonbare oppervlakte alle eenheden
    public decimal TotaleOpp() => _oppervlaktes.Sum(o => o.BewoonbareOpp);

    // Prijs van een gekoppeld materiaal (via FormulaKoppeling-sleutel)
    public decimal M(string sleutel) =>
        _prijzen.TryGetValue(sleutel, out var p) ? p : 0m;

    // Naam van het gekoppelde materiaal
    public string MNaam(string sleutel) =>
        _namen.TryGetValue(sleutel, out var n) ? n : sleutel;

    // Is er een materiaal gekoppeld aan deze sleutel?
    public bool HeeftMateriaal(string sleutel) => _prijzen.ContainsKey(sleutel);

    // Bouwkost percentage op naam
    public decimal Pct(string naam) =>
        _percentages.TryGetValue(naam, out var p) ? p / 100m : 0m;

    // Resultaat van een andere formule (lazy, met circulaire-referentie-detectie)
    public decimal F(string sleutel) {
        if (_cache.TryGetValue(sleutel, out var cached)) return cached ?? 0m;
        if (_bezig.Contains(sleutel)) return 0m; // circulaire referentie afbreken
        _bezig.Add(sleutel);
        var result = _registry.Evaluate(sleutel, this);
        _bezig.Remove(sleutel);
        _cache[sleutel] = result;
        return result ?? 0m;
    }

    public BudgetFormulaContext(
        BudgetGegevensBO gegevens,
        List<BudgetOppervlaktes> oppervlaktes,
        Dictionary<string, decimal> prijzen,
        Dictionary<string, string> namen,
        Dictionary<string, decimal> percentages,
        BudgetFormulaRegistry registry)
    {
        Gegevens      = gegevens;
        _oppervlaktes = oppervlaktes;
        _prijzen      = prijzen;
        _namen        = namen;
        _percentages  = percentages;
        _registry     = registry;
    }
}
