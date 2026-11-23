using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Planning;

/// <summary>
/// Hangt trajectmijlpalen aan taken en fases van de planning via dezelfde code (bv. RUWBOUW_KLAAR). Een mijlpaal is er één per project;
/// bestaat dezelfde code in meerdere taken of fases (bv. in meerdere plannings), dan is niet te zeggen welke bedoeld is en laten we de mijlpaal met rust.
/// Mijlpalen die al aan iets hangen en mijlpalen die hun datum elders vandaan halen worden nooit aangepast.
/// </summary>
public static class PlanningTrajectKoppeling
{
    public const int BindingTaak = 4, BindingFase = 5;

    /// <returns>Aantal mijlpalen dat gekoppeld werd.</returns>
    public static async Task<int> KoppelOpCode(cpmRunningContext db, int projectId)
    {
        var mijlpalen = await db.Mijlpaal
            .Where(m => m.Projecttraject.ProjectId == projectId && m.Code != null && m.Code != ""
                        && (m.BronBinding == null || m.BronBinding == 0 || ((m.BronBinding == BindingTaak || m.BronBinding == BindingFase) && m.BronRefId == null)))
            .ToListAsync();
        mijlpalen = mijlpalen.Where(m => m.BronBinding is null or 0 || !int.TryParse((m.BronParam ?? "").Trim(), out _)).ToList();
        if (mijlpalen.Count == 0) return 0;

        var secties = await db.PlanningSectie.AsNoTracking().Where(s => s.ProjectId == projectId && s.IsActief && s.Code != null && s.Code != "").Select(s => new { s.Id, s.Code }).ToListAsync();
        var sectieIds = await db.PlanningSectie.AsNoTracking().Where(s => s.ProjectId == projectId && s.IsActief).Select(s => s.Id).ToListAsync();
        var taken = await db.PlanningTaak.AsNoTracking().Where(t => sectieIds.Contains(t.PlanningSectieId) && t.Code != null && t.Code != "").Select(t => new { t.Id, t.Code }).ToListAsync();

        // Een mijlpaal van één eenheid (woning, appartement) zoekt enkel in de planning waartoe die eenheid hoort: daar is de code wel uniek,
        // ook al staat dezelfde code in de planning van elke andere woning. Hoort de eenheid in geen enkele planning, dan geldt de regel voor het hele project.
        var eenheidPlanning = await db.BouwPlanningEenheid.AsNoTracking()
            .Where(e => db.BouwPlanning.Any(p => p.Id == e.BouwPlanningId && p.ProjectId == projectId && p.IsActief))
            .Select(e => new { e.UnitId, e.BouwPlanningId }).ToListAsync();
        var planningVanEenheid = eenheidPlanning.GroupBy(e => e.UnitId).ToDictionary(g => g.Key, g => g.First().BouwPlanningId);
        var sectiePlanning = await db.PlanningSectie.AsNoTracking().Where(s => s.ProjectId == projectId && s.IsActief).Select(s => new { s.Id, s.BouwPlanningId }).ToDictionaryAsync(s => s.Id, s => s.BouwPlanningId);
        var taakSectie = await db.PlanningTaak.AsNoTracking().Where(t => sectieIds.Contains(t.PlanningSectieId)).Select(t => new { t.Id, t.PlanningSectieId }).ToDictionaryAsync(t => t.Id, t => t.PlanningSectieId);

        var aantal = 0;
        foreach (var m in mijlpalen)
        {
            var code = m.Code.Trim();
            var tAlle = taken;
            var sAlle = secties;
            if (m.UnitId is int uid && planningVanEenheid.TryGetValue(uid, out var pid))
            {
                tAlle = taken.Where(x => taakSectie.TryGetValue(x.Id, out var sid) && sectiePlanning.TryGetValue(sid, out var bp) && bp == pid).ToList();
                sAlle = secties.Where(x => sectiePlanning.TryGetValue(x.Id, out var bp) && bp == pid).ToList();
            }
            var t = tAlle.Where(x => string.Equals(x.Code.Trim(), code, StringComparison.OrdinalIgnoreCase)).ToList();
            var f = sAlle.Where(x => string.Equals(x.Code.Trim(), code, StringComparison.OrdinalIgnoreCase)).ToList();
            if (t.Count == 1 && f.Count == 0) { m.BronBinding = BindingTaak; m.BronRefId = t[0].Id; aantal++; }
            else if (f.Count == 1 && t.Count == 0) { m.BronBinding = BindingFase; m.BronRefId = f[0].Id; aantal++; }
        }
        if (aantal > 0) await db.SaveChangesAsync();
        return aantal;
    }
}
