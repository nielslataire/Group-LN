using DALCore.Models;

namespace ServiceCore.Traject;

/// <summary>Relatieve streefdatum (migratie 074, design 30e "OF RELATIEF"): een mijlpaal met
/// <see cref="Mijlpaal.RelatiefAnkerMijlpaalId"/> krijgt als <see cref="Mijlpaal.Doeldatum"/> de effectieve
/// datum van haar anker + <see cref="Mijlpaal.RelatiefOffsetDagen"/>. Meerdere passes omdat een anker zelf
/// weer relatief kan zijn; een kring (A na B, B na A) stopt vanzelf en laat de laatste datum staan.</summary>
internal static class MijlpaalRelatief
{
    /// <summary>Herberekent alle relatieve mijlpalen in de lijst; geeft het aantal gewijzigde datums terug.</summary>
    internal static int Herbereken(IReadOnlyCollection<Mijlpaal> mijlpalen)
    {
        var byId = mijlpalen.Where(m => m.Id > 0).ToDictionary(m => m.Id);
        var relatief = mijlpalen.Where(m => m.RelatiefAnkerMijlpaalId.HasValue).ToList();
        if (relatief.Count == 0) return 0;

        int gewijzigd = 0;
        for (int pass = 0; pass < relatief.Count + 1; pass++)
        {
            bool changed = false;
            foreach (var m in relatief)
            {
                if (!byId.TryGetValue(m.RelatiefAnkerMijlpaalId!.Value, out var anker) || ReferenceEquals(anker, m)) continue;
                var basis = anker.WerkelijkeDatum ?? anker.Doeldatum ?? anker.DoeldatumBerekend;
                if (basis is not DateOnly b) continue;
                var nieuw = b.AddDays(m.RelatiefOffsetDagen ?? 0);
                if (m.Doeldatum == nieuw) continue;
                m.Doeldatum = nieuw;
                changed = true;
                gewijzigd++;
            }
            if (!changed) break;
        }
        return gewijzigd;
    }
}
