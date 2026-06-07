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

    public BudgetFormulaService(UnitOfWorkCore uow, BudgetFormulaRegistry registry) {
        _uow = uow;
        _registry = registry;
    }

    public async Task<BudgetFormulaContext> BuildContextAsync(int versieId, BudgetGegevensBO gegevens) {
        var koppelingen = await _uow.FormulaKoppelingen.GetNoTracking()
            .Include(k => k.Materiaal)
            .ToListAsync();

        var prijzen = koppelingen
            .Where(k => k.Materiaal != null)
            .ToDictionary(k => k.Sleutel, k => k.Materiaal.ReferentiePrijs);

        var namen = koppelingen
            .Where(k => k.Materiaal != null)
            .ToDictionary(k => k.Sleutel, k => k.Materiaal.Naam);

        var oppervlaktes = await _uow.BudgetOppervlaktes.GetNoTracking()
            .Where(o => o.BudgetVersieId == versieId)
            .ToListAsync();

        var percentages = await _uow.BouwkostPercentages.GetNoTracking()
            .ToDictionaryAsync(p => p.Naam, p => p.Percentage);

        return new BudgetFormulaContext(gegevens, oppervlaktes, prijzen, namen, percentages, _registry);
    }

    // Berekent alle bekende formules voor een gegeven context en geeft het resultaat terug als dictionary sleutel→waarde
    public Dictionary<string, FormulaResultaat> BerekenAlle(BudgetFormulaContext ctx) {
        var resultaten = new Dictionary<string, FormulaResultaat>();
        foreach (var sleutel in _registry.AlleSleutels) {
            var waarde = _registry.Evaluate(sleutel, ctx);
            resultaten[sleutel] = new FormulaResultaat {
                Sleutel   = sleutel,
                Label     = _registry.GetLabel(sleutel),
                Waarde    = waarde,
                MNaam     = ctx.HeeftMateriaal(sleutel) ? ctx.MNaam(sleutel) : null
            };
        }
        return resultaten;
    }
}

public class FormulaResultaat {
    public string   Sleutel { get; set; }
    public string   Label   { get; set; }
    public decimal? Waarde  { get; set; }
    public string   MNaam   { get; set; } // naam van gekoppeld materiaal, indien aanwezig
}
