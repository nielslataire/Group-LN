using DALCore.Models;
using FacadeCore;

namespace ServiceCore.Traject.TriggerActions;

/// <summary>
/// Zet een bool-vlag op <see cref="Project"/> (bv. DocPid). Vereist expliciete opt-in per trigger
/// (<c>MagProjectWijzigen</c>) — een trigger mag het project alleen wijzigen als dat bewust is aangevinkt.
/// Parameter: <c>{"vlag":"DocPid","waarde":true}</c>.
/// </summary>
public class ZetProjectVlagAction : ITrajectTriggerAction
{
    public int Actie => (int)BOCore.TriggerActie.ZetProjectVlag;

    public Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default)
    {
        if (!trigger.MagProjectWijzigen)
            return Task.FromResult(TriggerActieResultaat.Skip("Trigger mag het project niet wijzigen (MagProjectWijzigen staat uit)."));

        var parameters = TriggerParamHelper.Parse(trigger.ActieParametersJson);
        var vlag = TriggerParamHelper.GetString(parameters, "vlag");
        var waarde = TriggerParamHelper.GetBool(parameters, "waarde") ?? true;

        if (string.IsNullOrWhiteSpace(vlag))
            return Task.FromResult(TriggerActieResultaat.Skip("Geen 'vlag' opgegeven in de trigger-parameters."));

        switch (vlag)
        {
            case "DocPid": project.DocPid = waarde; break;
            case "DocElectricalInspection": project.DocElectricalInspection = waarde; break;
            case "DocWaterInspection": project.DocWaterInspection = waarde; break;
            case "DocSewerInspection": project.DocSewerInspection = waarde; break;
            case "DocFireInspection": project.DocFireInspection = waarde; break;
            case "DocDelivery": project.DocDelivery = waarde; break;
            case "DocDefDelivery": project.DocDefDelivery = waarde; break;
            default:
                return Task.FromResult(TriggerActieResultaat.Skip($"Onbekende projectvlag '{vlag}'."));
        }

        return Task.FromResult(TriggerActieResultaat.Success($"Project.{vlag} = {waarde}."));
    }
}
