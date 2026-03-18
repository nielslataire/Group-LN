using DALCore.Models;

namespace CPMCore.Services.Security;

public interface ISecurityService
{
    Task<bool> UserHasProjectAccess(string userId, int projectId);
    Task<List<int>> GetAllowedProjectIds(string userId);
    Task<List<int>> GetUserCompanyIds(string userId);
    Task<bool> UserIsAdmin(string userId);
    Task<bool> CanAccessIssue(string userId, int issueId);
    Task<bool> CanAccessDocument(string userId, int documentId);
    Task<IQueryable<ConstructionIssue>> ApplyIssueScope(IQueryable<ConstructionIssue> query, string userId);
    Task<IQueryable<ConstructionIssueReport>> ApplyIssueReportScope(IQueryable<ConstructionIssueReport> query, string userId);
    Task<IQueryable<ProjectDocs>> ApplyDocumentScope(IQueryable<ProjectDocs> query, string userId);
}