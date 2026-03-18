using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.Security;

public class SecurityService : ISecurityService
{
    private readonly cpmRunningContext _db;

    public SecurityService(cpmRunningContext db)
    {
        _db = db;
    }

    public async Task<bool> UserHasProjectAccess(string userId, int projectId)
    {
        if (await UserIsAdmin(userId))
            return true;

        var internalUserId = await ResolveInternalUserId(userId);
        if (!internalUserId.HasValue)
            return false;

        return await _db.Set<ProjectUserAccess>()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == internalUserId.Value && x.ProjectId == projectId);
    }

    public async Task<List<int>> GetAllowedProjectIds(string userId)
    {
        if (await UserIsAdmin(userId))
        {
            return await _db.Project
                .AsNoTracking()
                .Select(x => x.ProjectId)
                .ToListAsync();
        }

        var internalUserId = await ResolveInternalUserId(userId);
        if (!internalUserId.HasValue)
            return new List<int>();

        return await _db.Set<ProjectUserAccess>()
            .AsNoTracking()
            .Where(x => x.UserId == internalUserId.Value)
            .Select(x => x.ProjectId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<int>> GetUserCompanyIds(string userId)
    {
        var internalUserId = await ResolveInternalUserId(userId);
        if (!internalUserId.HasValue)
            return new List<int>();

        return await _db.Set<UserCompanyAccess>()
            .AsNoTracking()
            .Where(x => x.UserId == internalUserId.Value)
            .Select(x => x.CompanyId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<bool> UserIsAdmin(string userId)
    {
        var internalUserId = await ResolveInternalUserId(userId);
        if (!internalUserId.HasValue)
            return false;

        return await _db.PermissionPerUser
            .AsNoTracking()
            .Where(x => x.UserId == internalUserId.Value)
            .AnyAsync(x => x.PermissionNavigation.PermissionName == "Admin");
    }

    public async Task<bool> CanAccessIssue(string userId, int issueId)
    {
        var scoped = await ApplyIssueScope(_db.ConstructionIssue.AsNoTracking(), userId);
        return await scoped.AnyAsync(x => x.Id == issueId);
    }

    public async Task<bool> CanAccessDocument(string userId, int documentId)
    {
        var scoped = await ApplyDocumentScope(_db.ProjectDocs.AsNoTracking(), userId);
        return await scoped.AnyAsync(x => x.Id == documentId);
    }

    public async Task<IQueryable<ConstructionIssue>> ApplyIssueScope(IQueryable<ConstructionIssue> query, string userId)
    {
        if (await UserIsAdmin(userId))
            return query;

        var projectIds = await GetAllowedProjectIds(userId);
        if (projectIds.Count == 0)
            return query.Where(_ => false);

        var companyIds = await GetUserCompanyIds(userId);
        query = query.Where(x => projectIds.Contains(x.ProjectId));

        if (companyIds.Count > 0)
        {
            query = query.Where(x => !x.ResponsiblePartyId.HasValue || companyIds.Contains(x.ResponsiblePartyId.Value));
        }

        return query;
    }

    public async Task<IQueryable<ConstructionIssueReport>> ApplyIssueReportScope(IQueryable<ConstructionIssueReport> query, string userId)
    {
        if (await UserIsAdmin(userId))
            return query;

        var projectIds = await GetAllowedProjectIds(userId);
        if (projectIds.Count == 0)
            return query.Where(_ => false);

        var companyIds = await GetUserCompanyIds(userId);
        query = query.Where(x => projectIds.Contains(x.ProjectId));

        if (companyIds.Count > 0)
        {
            query = query.Where(x => !x.ResponsiblePartyId.HasValue || companyIds.Contains(x.ResponsiblePartyId.Value));
        }

        return query;
    }

    public async Task<IQueryable<ProjectDocs>> ApplyDocumentScope(IQueryable<ProjectDocs> query, string userId)
    {
        if (await UserIsAdmin(userId))
            return query;

        var projectIds = await GetAllowedProjectIds(userId);
        if (projectIds.Count == 0)
            return query.Where(_ => false);

        return query.Where(x => projectIds.Contains(x.ProjectId));
    }

    private async Task<int?> ResolveInternalUserId(string userId)
    {
        if (int.TryParse(userId, out var parsedId))
            return parsedId;

        return await _db.Users
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
    }
}