using DALCore.Models;
using FacadeCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Zet <see cref="Project.StatusId"/> (BOCore.ProjectStatusType). Vereist expliciete opt-in per trigger
/// (<c>MagProjectWijzigen</c>). Parameter: <c>{"status":2}</c>.
/// </summary>
public class ZetProjectStatusAction : ITrajectTriggerAction
{
    public int Actie => (int)BOCore.TriggerActie.ZetProjectStatus;

    public Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        if (!trigger.MagProjectWijzigen)
            return Task.FromResult(TriggerActieResultaat.Skip("Trigger mag het project niet wijzigen (MagProjectWijzigen staat uit)."));

        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var status = TriggerParamHelper.GetInt(parameters, "status");
        if (status == null)
            return Task.FromResult(TriggerActieResultaat.Skip("Geen 'status' opgegeven in de trigger-parameters."));

        if (project.StatusId == status)
            return Task.FromResult(TriggerActieResultaat.Skip($"Project.StatusId staat al op {status}."));

        var oud = project.StatusId;
        project.StatusId = status;
        return Task.FromResult(TriggerActieResultaat.Success($"Project.StatusId gewijzigd van {oud} naar {status}."));
    }
}
