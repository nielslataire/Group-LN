using BOCore;

namespace FacadeCore
{
    public interface IProjectVoortgangService
    {
        /// <summary>Lees de laatst opgeslagen voortgang voor een project.</summary>
        GetResponse<ProjectVoortgangBO> GetByProjectId(int projectId);

        /// <summary>Lees opgeslagen voortgang voor meerdere projecten (dashboard-gebruik).</summary>
        Dictionary<int, ProjectVoortgangBO> GetForProjects(IEnumerable<int> projectIds);

        /// <summary>Bereken voortgang on-the-fly zonder op te slaan.</summary>
        GetResponse<ProjectVoortgangBO> Calculate(int projectId);

        /// <summary>Bereken voortgang en sla (upsert) op in de database.</summary>
        GetResponse<ProjectVoortgangBO> CalculateAndSave(int projectId);

        /// <summary>Bereken en sla voortgang op voor alle actieve projecten (niet-opgeleverd).</summary>
        void CalculateAllProjects();
    }
}
