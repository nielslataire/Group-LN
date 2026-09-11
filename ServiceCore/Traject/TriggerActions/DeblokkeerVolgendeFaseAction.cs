using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>Ontgrendelt (<c>IsVergrendeld = false</c>) de eerstvolgende fase na die van de mijlpaal.</summary>
public class DeblokkeerVolgendeFaseAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.DeblokkeerVolgendeFase;

    private readonly cpmRunningContext _db;
    public DeblokkeerVolgendeFaseAction(cpmRunningContext db) { _db = db; }

    public async Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        if (mijlpaal.ProjecttrajectFaseId is not int faseId)
            return TriggerActieResultaat.Skip("Mijlpaal hoort bij geen enkele fase.");

        var huidige = await _db.ProjecttrajectFase.FirstOrDefaultAsync(f => f.Id == faseId, ct);
        if (huidige == null) return TriggerActieResultaat.Skip("Fase niet gevonden.");

        var volgende = await _db.ProjecttrajectFase
            .Where(f => f.ProjecttrajectId == huidige.ProjecttrajectId && f.Volgorde > huidige.Volgorde)
            .OrderBy(f => f.Volgorde)
            .FirstOrDefaultAsync(ct);

        if (volgende == null)
            return TriggerActieResultaat.Skip("Geen volgende fase (dit is de laatste fase van het traject).");

        if (!volgende.IsVergrendeld)
            return TriggerActieResultaat.Skip($"Fase '{volgende.Naam}' was al ontgrendeld.");

        volgende.IsVergrendeld = false;
        return TriggerActieResultaat.Success($"Fase '{volgende.Naam}' ontgrendeld.");
    }
}
