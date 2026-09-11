using BOCore;
using DALCore.Models;
using FacadeCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Maakt een <see cref="ProjectTaak"/> aan (Herkomst = Trigger), gekoppeld aan de mijlpaal. Parameters
/// (optioneel): <c>{"titel":"...","omschrijving":"...","userId":"...","rol":5,"offsetDagen":3}</c> —
/// zonder <c>userId</c>/<c>rol</c> valt terug op <c>Mijlpaal.VerantwoordelijkeUserId</c>/<c>VerantwoordelijkeRol</c>;
/// <c>offsetDagen</c> zet de vervaldatum t.o.v. vandaag (default 7).
/// </summary>
public class MaakTaakAction : ITrajectTriggerAction
{
    public int Actie => (int)TriggerActie.MaakTaak;

    private readonly IProjectTaakService _taken;
    public MaakTaakAction(IProjectTaakService taken) { _taken = taken; }

    public async Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var titel = TriggerParamHelper.GetString(parameters, "titel") ?? $"Opvolgen: {mijlpaal.Naam}";
        var omschrijving = TriggerParamHelper.GetString(parameters, "omschrijving");
        var userId = TriggerParamHelper.GetString(parameters, "userId") ?? mijlpaal.VerantwoordelijkeUserId;
        var rol = TriggerParamHelper.GetInt(parameters, "rol") ?? mijlpaal.VerantwoordelijkeRol;
        var offsetDagen = TriggerParamHelper.GetInt(parameters, "offsetDagen") ?? 7;
        var vervaldatum = DateOnly.FromDateTime(DateTime.Today).AddDays(offsetDagen);

        var taak = await _taken.CreateVanuitTrigger(project.ProjectId, mijlpaal.Id, titel, omschrijving,
            userId, string.IsNullOrWhiteSpace(userId) ? rol : null, vervaldatum, triggeredBy);

        return TriggerActieResultaat.Success($"Taak '{titel}' aangemaakt (Id {taak.Id}).");
    }
}
