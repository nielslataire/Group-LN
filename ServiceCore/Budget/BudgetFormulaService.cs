using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Budget;

public class BudgetFormulaService {
    private readonly UnitOfWorkCore _uow;
    private readonly BudgetFormulaRegistry _registry;
    private readonly BouwIndexService _bouwIndex;

    public BudgetFormulaService(UnitOfWorkCore uow, BudgetFormulaRegistry registry, BouwIndexService bouwIndex) {
        _uow = uow;
        _registry = registry;
        _bouwIndex = bouwIndex;
    }

    public async Task<BudgetFormulaContext> BuildContextAsync(int versieId, BudgetGegevensBO gegevens) {
        var koppelingen = await _uow.FormulaKoppelingen.GetNoTracking()
            .Include(k => k.Materiaal)
                .ThenInclude(m => m.IndexType)
            .ToListAsync();

        var metMateriaal = koppelingen.Where(k => k.Materiaal != null).ToList();

        var prijzen    = metMateriaal.ToDictionary(k => k.Sleutel, k => k.Materiaal.ReferentiePrijs);
        var namen      = metMateriaal.ToDictionary(k => k.Sleutel, k => k.Materiaal.Naam);
        var indexCodes = metMateriaal
            .Where(k => k.Materiaal.IndexType != null)
            .ToDictionary(k => k.Sleutel, k => k.Materiaal.IndexType.Code);

        // Haal per materiaal de historische I2021- en S-index op bij de referentiedatum
        var iRefs = new Dictionary<string, decimal>();
        var sRefs = new Dictionary<string, decimal>();
        foreach (var k in metMateriaal) {
            var refDatum = k.Materiaal.ReferentieDatum;
            var iRef = await _bouwIndex.GetIndexOpDatumAsync("I2021", refDatum);
            var sRef = await _bouwIndex.GetIndexOpDatumAsync("S",     refDatum);
            if (iRef.HasValue) iRefs[k.Sleutel] = iRef.Value;
            if (sRef.HasValue) sRefs[k.Sleutel] = sRef.Value;
        }

        var oppervlaktes = await _uow.BudgetOppervlaktes.GetNoTracking()
            .Where(o => o.BudgetVersieId == versieId)
            .ToListAsync();

        var percentages = await _uow.BouwkostPercentages.GetNoTracking()
            .ToDictionaryAsync(p => p.Naam, p => p.Percentage);

        return new BudgetFormulaContext(gegevens, oppervlaktes, prijzen, namen, indexCodes, iRefs, sRefs, percentages, _registry);
    }

    // Berekent alle bekende formules voor een gegeven context en geeft het resultaat terug als dictionary sleutel→waarde
    public Dictionary<string, FormulaResultaat> BerekenAlle(BudgetFormulaContext ctx) {
        var resultaten = new Dictionary<string, FormulaResultaat>();
        foreach (var sleutel in _registry.AlleSleutels) {
            var waarde = _registry.Evaluate(sleutel, ctx);
            resultaten[sleutel] = new FormulaResultaat {
                Sleutel            = sleutel,
                Label              = _registry.GetLabel(sleutel),
                Waarde             = waarde,
                MNaam              = ctx.HeeftMateriaal(sleutel) ? ctx.MNaam(sleutel) : null,
                MateriaalPrijs     = ctx.HeeftMateriaal(sleutel) ? ctx.M(sleutel) : null,
                MateriaalIndexCode = ctx.HeeftMateriaal(sleutel) ? ctx.MIndexCode(sleutel) : null,
                MateriaalIRef      = ctx.MIRef(sleutel),
                MateriaalSRef      = ctx.MSRef(sleutel)
            };
        }
        return resultaten;
    }
}

public class FormulaResultaat {
    public string   Sleutel            { get; set; }
    public string   Label              { get; set; }
    public decimal? Waarde             { get; set; }
    public string   MNaam              { get; set; }
    public decimal? MateriaalPrijs     { get; set; } // ReferentiePrijs vóór indexering
    public string   MateriaalIndexCode { get; set; } // KmIndexType.Code ("I2021", "S2021", "GW", …)
    public decimal? MateriaalIRef      { get; set; } // I2021-index op ReferentieDatum van het materiaal
    public decimal? MateriaalSRef      { get; set; } // S-index op ReferentieDatum van het materiaal
}
