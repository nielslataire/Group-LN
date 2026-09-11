namespace FacadeCore;

/// <summary>
/// Evalueert mijlpaal-triggers en voert ze uit via de geregistreerde <see cref="ITrajectTriggerAction"/>-handlers.
/// Draait mee op de bestaande <c>TrajectHostedService</c>-tick, na de herberekening.
/// </summary>
public interface ITrajectTriggerDispatcher
{
    /// <summary>Evalueert alle triggers van het traject van één project. Retourneert het aantal afgevuurde triggers.</summary>
    Task<int> EvaluateAndDispatchProject(int projectId, string? triggeredBy = null, CancellationToken ct = default);
}
