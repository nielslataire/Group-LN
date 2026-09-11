using BOCore;
using DALCore.Models;
using FacadeCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Maakt een <see cref="ProjectDossier"/> aan en koppelt het aan de mijlpaal. Parameters (optioneel):
/// <c>{"kind":1,"titel":"..."}</c> — kind = BOCore.DossierKind (default Vrij).
/// </summary>
public class MaakDossierAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.MaakDossier;

    private readonly cpmRunningContext _db;
    public MaakDossierAction(cpmRunningContext db) { _db = db; }

    public async Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        if (mijlpaal.DossierId != null)
            return TriggerActieResultaat.Skip($"Mijlpaal is al gekoppeld aan dossier {mijlpaal.DossierId}.");

        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var kind = TriggerParamHelper.GetInt(parameters, "kind") ?? (int)DossierKind.Vrij;
        var titel = TriggerParamHelper.GetString(parameters, "titel") ?? mijlpaal.Naam;

        var dossier = new ProjectDossier
        {
            ProjectId = project.ProjectId,
            UnitId = mijlpaal.UnitId,
            DossierKind = kind,
            Titel = titel,
            Status = (int)DossierStatus.Nieuw,
            CreatedByUserId = triggeredBy,
            CreatedDate = DateTime.UtcNow
        };
        _db.ProjectDossier.Add(dossier);
        await _db.SaveChangesAsync(ct);

        mijlpaal.DossierId = dossier.Id;
        _db.ProjectDossierMijlpaal.Add(new ProjectDossierMijlpaal { ProjectDossierId = dossier.Id, MijlpaalId = mijlpaal.Id });
        _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
        {
            ProjectDossierId = dossier.Id,
            Type = (int)DossierGebeurtenisType.Opmerking,
            Titel = "Automatisch aangemaakt via trigger",
            Tekst = $"Mijlpaal: {mijlpaal.Naam}",
            UserId = triggeredBy,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });

        return TriggerActieResultaat.Success($"Dossier '{titel}' ({(DossierKind)kind}) aangemaakt (Id {dossier.Id}).");
    }
}
