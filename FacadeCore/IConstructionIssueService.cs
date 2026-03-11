using DALCore.Models;
using BOCore;

namespace FacadeCore;

public interface IConstructionIssueService
{
    Task<ConstructionIssue?> GetById(int projectId, int id);
    Task<List<ConstructionIssue>> Search(int projectId, ConstructionIssueFilterBO filters);
    Task<ConstructionIssue> Create(int projectId, ConstructionIssueUpsertBO dto, string? userId);
    Task<ConstructionIssue?> Update(int projectId, int id, ConstructionIssueUpsertBO dto, string? userId);
    Task<bool> ChangeStatus(int projectId, int id, int newStatus, string? optionalComment, string? userId);
    Task<bool> AssignResponsible(int projectId, int id, int responsiblePartyType, int? responsiblePartyId, string? otherName, string? otherEmail, string? userId);
    Task AddHistory(int issueId, int action, string? userId, string? oldValueJson, string? newValueJson, string? comment);
    Task<int> BulkUpdate(int projectId, ConstructionIssueBulkUpdateBO dto, string? userId);
    Task<List<ConstructionIssueMedia>> GetMedia(int projectId, int issueId);
    Task<ConstructionIssueMedia?> AddMedia(int projectId, int issueId, string fileId, int mediaType, string? userId);
    Task<bool> DeleteMedia(int projectId, int issueId, int mediaId, string? userId);
    Task<bool> Delete(int projectId, int issueId, string? userId);
    Task<List<ConstructionIssueNotification>> GetNotifications(int projectId, int issueId);
    Task<List<ConstructionIssueCategory>> GetCategories();
    Task<List<Units>> GetProjectUnits(int projectId);
}