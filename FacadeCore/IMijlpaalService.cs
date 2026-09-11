using DALCore.Models;
using BOCore;

namespace FacadeCore;

/// <summary>CRUD + statusbeheer voor mijlpalen binnen een projecttraject (spiegelt IConstructionIssueService).</summary>
public interface IMijlpaalService
{
    Task<Mijlpaal?> GetById(int projecttrajectId, int id);
    Task<List<Mijlpaal>> Search(int projecttrajectId, MijlpaalFilterBO filters);
    Task<Mijlpaal> Create(MijlpaalUpsertBO dto, string? userId);
    Task<Mijlpaal?> Update(int id, MijlpaalUpsertBO dto, string? userId);
    Task<bool> ChangeStatus(int projecttrajectId, MijlpaalStatusChangeBO dto, string? userId);
    Task<bool> AssignResponsible(int projecttrajectId, int id, int? rol, int? partijType, int? partijId, string? userId, string? assignedUserId);
    Task<int> BulkUpdate(int projecttrajectId, MijlpaalBulkUpdateBO dto, string? userId);
    Task<bool> Delete(int projecttrajectId, int id, string? userId);

    Task<List<MijlpaalHistoriek>> GetHistoriek(int mijlpaalId);
    Task AddHistory(int mijlpaalId, int actie, string? userId, string? oldValueJson, string? newValueJson, string? comment);

    /// <summary>Aantal achterstallige (streefdatum verstreken, niet bereikt) mijlpalen over de opgegeven projecten.</summary>
    Task<int> CountOverdue(IEnumerable<int> projectIds);

    /// <summary>Portfolio-brede mijlpalenlijst over meerdere projecten (voor de /Deadlines-pagina).</summary>
    Task<List<Mijlpaal>> SearchPortfolio(IEnumerable<int> projectIds, MijlpaalFilterBO filters);
}
