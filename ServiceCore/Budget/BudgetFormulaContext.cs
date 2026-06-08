using System;
using System.Collections.Generic;
using System.Linq;
using BOCore;
using DALCore.Models;

namespace ServiceCore.Budget;

public class BudgetFormulaContext {
    private readonly Dictionary<string, decimal>  _prijzen;
    private readonly Dictionary<string, string>   _namen;
    private readonly Dictionary<string, string>   _indexCodes;
    private readonly Dictionary<string, decimal>  _iRefs;  // I2021-index op referentiedatum materiaal
    private readonly Dictionary<string, decimal>  _sRefs;  // S-index op referentiedatum materiaal
    private readonly Dictionary<string, decimal>  _percentages;
    private readonly List<BudgetOppervlaktes>      _oppervlaktes;
    private readonly BudgetFormulaRegistry        _registry;
    private readonly Dictionary<string, decimal?> _cache = new();
    private readonly HashSet<string>              _bezig = new();

    public BudgetGegevensBO Gegevens { get; }

    // Huidige I2021-waarde uit het budget (bv. 123.50)
    public decimal IHuidig => Gegevens.IIndexHuidig ?? 100m;
    // Huidige S-waarde uit het budget (bv. 119.10)
    public decimal SHuidig => Gegevens.SIndexHuidig ?? 100m;

    // Backward-compat helpers (gebruikt elders in context, bv. GewogenIndex)
    public decimal IFactor {
        get {
            var basis = Gegevens.IIndexStart > 0 ? Gegevens.IIndexStart.Value : 100m;
            return IHuidig / basis;
        }
    }
    public decimal SFactor {
        get {
            var basis = Gegevens.SIndexStart > 0 ? Gegevens.SIndexStart.Value : 100m;
            return SHuidig / basis;
        }
    }

    public decimal GewogenIndex => IFactor * 0.4m + SFactor * 0.4m + 0.2m;

    // I2021- en S-referentiewaarden op de referentiedatum van het materiaal
    public decimal  MIRef(string sleutel) => _iRefs.TryGetValue(sleutel, out var v) ? v : 100m;
    public decimal  MSRef(string sleutel) => _sRefs.TryGetValue(sleutel, out var v) ? v : 100m;

    // Indexatiefactor voor een specifiek materiaal:
    // (I_huidig / I_ref) × 0.40 + (S_huidig / S_ref) × 0.40 + 0.20
    public decimal MIndexFactor(string sleutel) {
        var iRef = MIRef(sleutel);
        var sRef = MSRef(sleutel);
        return (IHuidig / iRef) * 0.4m + (SHuidig / sRef) * 0.4m + 0.2m;
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

    // KmIndexType.Code van het gekoppelde materiaal
    public string MIndexCode(string sleutel) =>
        _indexCodes.TryGetValue(sleutel, out var c) ? c : "GW";

    // Is er een materiaal gekoppeld aan deze sleutel?
    public bool HeeftMateriaal(string sleutel) => _prijzen.ContainsKey(sleutel);

    // Bouwkost percentage op naam
    public decimal Pct(string naam) =>
        _percentages.TryGetValue(naam, out var p) ? p / 100m : 0m;

    // Resultaat van een andere formule (lazy, met circulaire-referentie-detectie)
    public decimal F(string sleutel) {
        if (_cache.TryGetValue(sleutel, out var cached)) return cached ?? 0m;
        if (_bezig.Contains(sleutel)) return 0m;
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
        Dictionary<string, string> indexCodes,
        Dictionary<string, decimal> iRefs,
        Dictionary<string, decimal> sRefs,
        Dictionary<string, decimal> percentages,
        BudgetFormulaRegistry registry)
    {
        Gegevens      = gegevens;
        _oppervlaktes = oppervlaktes;
        _prijzen      = prijzen;
        _namen        = namen;
        _indexCodes   = indexCodes;
        _iRefs        = iRefs;
        _sRefs        = sRefs;
        _percentages  = percentages;
        _registry     = registry;
    }
}
