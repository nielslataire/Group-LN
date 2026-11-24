using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Planning;

/// <summary>
/// Hangt betalingsschijven die uit een schijvensjabloon komen automatisch aan de juiste taak of fase van de planning, via de code van het sjabloon
/// (<c>InvoicingPaymentStages.PlanningCode</c>) en bij een herhaling per verdieping het volgnummer van de verdieping. Een schijf die in die planning al
/// ergens aan hangt, blijft ongemoeid. Komt de code in een planning niet (genoeg) voor, dan blijft de schijf los en koppel je ze met de hand.
/// Heeft de schijf een bouwstaat die in die planning geleverd wordt, dan koppelt de planning ze dynamisch aan alle leverende taken en maken we hier niets aan.
/// </summary>
public static class PlanningSchijfAutoKoppeling
{
    /// <returns>Aantal nieuwe koppelingen.</returns>
    public static async Task<int> KoppelOpCode(cpmRunningContext db, int projectId)
    {
        var groepIds = await db.InvoicingPaymentGroup.AsNoTracking().Where(g => g.ProjectId == projectId).Select(g => g.Id).ToListAsync();
        if (groepIds.Count == 0) return 0;
        var stages = await db.InvoicingPaymentStages.AsNoTracking().Where(s => groepIds.Contains(s.GroupId) && s.PlanningCode != null && s.PlanningCode != "").ToListAsync();
        if (stages.Count == 0) return 0;

        var planningen = await db.BouwPlanning.AsNoTracking().Where(b => b.ProjectId == projectId && b.IsActief).ToListAsync();
        var planningIds = planningen.Select(p => p.Id).ToList();
        var hoofdId = planningen.FirstOrDefault(p => p.IsHoofd)?.Id ?? 0;
        var eenheidRijen = await db.BouwPlanningEenheid.AsNoTracking().Where(e => planningIds.Contains(e.BouwPlanningId)).ToListAsync();
        var hoofdAlgemeen = !eenheidRijen.Any(e => e.BouwPlanningId == hoofdId);

        // Per betalingsgroep: in welke plannings zitten haar eenheden? (groep via de constructiewaarde, de oudere koppeling op de eenheid als terugval)
        var eenheden = await db.Units.AsNoTracking().Where(u => u.ProjectId == projectId)
            .Select(u => new { u.Id, Legacy = u.PaymentGroupId, Cv = u.UnitConstructionValue.Where(c => c.PaymentGroupId != null).Select(c => c.PaymentGroupId.Value).ToList() })
            .ToListAsync();
        var planningPerGroep = new Dictionary<int, HashSet<int>>();
        foreach (var u in eenheden)
        {
            var groepen = u.Cv.Count > 0 ? u.Cv.Distinct().ToList() : (u.Legacy != null ? new List<int> { u.Legacy.Value } : new List<int>());
            var rij = eenheidRijen.FirstOrDefault(e => e.UnitId == u.Id);
            int? planning = rij != null ? rij.BouwPlanningId : (hoofdAlgemeen ? hoofdId : (int?)null);
            if (planning == null) continue;
            foreach (var g in groepen)
            {
                if (!planningPerGroep.TryGetValue(g, out var set)) planningPerGroep[g] = set = new HashSet<int>();
                set.Add(planning.Value);
            }
        }

        var secties = await db.PlanningSectie.AsNoTracking().Where(s => s.ProjectId == projectId && s.IsActief).ToListAsync();
        var sectieIds = secties.Select(s => s.Id).ToList();
        var taken = await db.PlanningTaak.AsNoTracking().Where(t => sectieIds.Contains(t.PlanningSectieId)).ToListAsync();
        var sectiePerId = secties.ToDictionary(s => s.Id);
        int PlanningVan(PlanningSectie s) => s.BouwPlanningId ?? hoofdId;

        // Bouwstaten (migratie 158): heeft de schijf een bouwstaat die in deze planning door taken of fases geleverd wordt, dan hangt de planning ze dynamisch aan al die taken (zie PlanningService.VulSchijven) en maken we hier geen koppeling met één taak.
        var bouwstatenPerPlanning = new Dictionary<int, HashSet<int>>();
        foreach (var s in secties.Where(s => s.BouwstaatId != null)) { var pl = PlanningVan(s); if (!bouwstatenPerPlanning.TryGetValue(pl, out var set)) bouwstatenPerPlanning[pl] = set = new HashSet<int>(); set.Add(s.BouwstaatId!.Value); }
        foreach (var t in taken.Where(t => t.BouwstaatId != null)) { var pl = PlanningVan(sectiePerId[t.PlanningSectieId]); if (!bouwstatenPerPlanning.TryGetValue(pl, out var set)) bouwstatenPerPlanning[pl] = set = new HashSet<int>(); set.Add(t.BouwstaatId!.Value); }

        var stageIds = stages.Select(s => s.Id).ToList();
        var bestaand = await db.PlanningSchijfKoppeling.Where(k => stageIds.Contains(k.PaymentStageId)).ToListAsync();

        var aantal = 0;
        foreach (var stage in stages)
        {
            if (!planningPerGroep.TryGetValue(stage.GroupId, out var plannings)) continue;
            var code = stage.PlanningCode!.Trim();
            var index = Math.Max(1, stage.PlanningVolgnr ?? 1) - 1;
            foreach (var planning in plannings)
            {
                if (bestaand.Any(k => k.PaymentStageId == stage.Id && k.BouwPlanningId == planning)) continue;
                if (stage.BouwstaatId is int bid && bouwstatenPerPlanning.TryGetValue(planning, out var aanwezig) && aanwezig.Contains(bid)) continue;
                var taakDoel = taken
                    .Where(t => string.Equals(t.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase) && sectiePerId.TryGetValue(t.PlanningSectieId, out var s) && PlanningVan(s) == planning)
                    .OrderBy(t => sectiePerId[t.PlanningSectieId].Volgorde).ThenBy(t => t.PlanningSectieId).ThenBy(t => t.Volgorde).ThenBy(t => t.Id)
                    .Skip(index).FirstOrDefault();
                PlanningSectie? sectieDoel = null;
                if (taakDoel == null)
                    sectieDoel = secties
                        .Where(s => string.Equals(s.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase) && PlanningVan(s) == planning)
                        .OrderBy(s => s.Volgorde).ThenBy(s => s.Id).Skip(index).FirstOrDefault();
                if (taakDoel == null && sectieDoel == null) continue;
                var koppeling = new PlanningSchijfKoppeling { PaymentStageId = stage.Id, BouwPlanningId = planning, TaakId = taakDoel?.Id, SectieId = sectieDoel?.Id };
                db.PlanningSchijfKoppeling.Add(koppeling);
                bestaand.Add(koppeling);
                aantal++;
            }
        }
        if (aantal > 0) await db.SaveChangesAsync();
        return aantal;
    }
}
