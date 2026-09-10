using DALCore.Models;

namespace FacadeCore;

/// <summary>Beheer van het projecttraject (instantie) per project.</summary>
public interface IProjecttrajectService
{
    /// <summary>Haalt het traject van een project op, inclusief fases en mijlpalen. Null als er nog geen traject is.</summary>
    Task<Projecttraject?> GetByProject(int projectId, bool includeDetails = true);

    /// <summary>Lijst van projectId's die al een traject hebben (voor portfolio-overzichten).</summary>
    Task<List<int>> GetProjectIdsWithTraject();

    /// <summary>Werkt naam/status van het traject bij.</summary>
    Task<bool> Update(int projecttrajectId, string? naam, int? status, string? userId);

    /// <summary>Verwijdert het volledige traject van een project (cascade naar fases/mijlpalen).</summary>
    Task<bool> Delete(int projectId, string? userId);
}
