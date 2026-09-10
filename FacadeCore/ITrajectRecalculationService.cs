namespace FacadeCore;

/// <summary>
/// Herberekent afgeleide mijlpaal-data (streefdata uit ankers, en status/werkelijke datum uit
/// <c>BronBinding</c>) voor projecttrajecten. Analoog aan <c>ProjectVoortgangService</c>.
/// </summary>
public interface ITrajectRecalculationService
{
    /// <summary>Herberekent het traject van één project. Retourneert het aantal gewijzigde mijlpalen.</summary>
    Task<int> RecalculateProject(int projectId, string? triggeredBy = null);

    /// <summary>Herberekent alle actieve trajecten (project niet Opgeleverd). Retourneert het aantal gewijzigde mijlpalen.</summary>
    Task<int> RecalculateAll(string? triggeredBy = null);

    /// <summary>Project-id's met een traject waarvan het project nog niet is opgeleverd.</summary>
    Task<List<int>> GetActiveProjectIds();
}
