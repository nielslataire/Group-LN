using System.Text.Json;
using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Translators;

namespace ServiceCore.Issues;

public class ConstructionIssueService : IConstructionIssueService
{
    private readonly cpmRunningContext _db;
    public ConstructionIssueService(cpmRunningContext db) { _db = db; }

    public Task<List<ConstructionIssueCategory>> GetCategories() => _db.ConstructionIssueCategory.OrderBy(x => x.Name).ToListAsync();
    public Task<List<Units>> GetProjectUnits(int projectId) => _db.Units.Where(x => x.ProjectId == projectId).OrderBy(x => x.Name).ToListAsync();

    public Task<ConstructionIssue?> GetById(int projectId, int id) =>
        _db.ConstructionIssue
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .Include(x => x.ConstructionIssueHistory)
            .Include(x => x.ConstructionIssueMedia)
            .Include(x => x.ConstructionIssueNotification)
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == id);

    public async Task<List<ConstructionIssue>> Search(int projectId, ConstructionIssueFilterBO f)
    {
        var q = _db.ConstructionIssue.Include(x => x.Category).Include(x => x.Unit).Where(x => x.ProjectId == projectId);
        if (f.Status.HasValue) q = q.Where(x => x.Status == f.Status.Value);
        if (f.UnitId.HasValue) q = q.Where(x => x.UnitId == f.UnitId.Value);
        if (f.CategoryId.HasValue) q = q.Where(x => x.CategoryId == f.CategoryId.Value);
        if (f.ResponsiblePartyType.HasValue) q = q.Where(x => x.ResponsiblePartyType == f.ResponsiblePartyType.Value);
        if (f.ResponsiblePartyId.HasValue) q = q.Where(x => x.ResponsiblePartyId == f.ResponsiblePartyId.Value);
        if (f.Priority.HasValue) q = q.Where(x => x.Priority == f.Priority.Value);
        if (f.Phase.HasValue) q = q.Where(x => x.IssuePhase == f.Phase.Value);
        if (f.Type.HasValue) q = q.Where(x => x.IssueType == f.Type.Value);
        if (f.Sent.HasValue)
            q = f.Sent.Value ? q.Where(x => x.LastSentDate != null) : q.Where(x => x.LastSentDate == null);
        if (f.Overdue == true) q = q.Where(x => x.DueDate.HasValue && x.DueDate < DateOnly.FromDateTime(DateTime.Today) && x.Status != (int)ConstructionIssueStatus.Closed);
        if (!string.IsNullOrWhiteSpace(f.Text)) q = q.Where(x => x.Title.Contains(f.Text) || x.LocationText.Contains(f.Text) || (x.Description ?? "").Contains(f.Text));
        return await q.OrderBy(x => x.Status).ThenBy(x => x.DueDate).ThenByDescending(x => x.CreatedDate).ToListAsync();
    }

    public async Task<ConstructionIssue> Create(int projectId, ConstructionIssueUpsertBO dto, string? userId)
    {
        var entity = new ConstructionIssue { ProjectId = projectId, CreatedByUserId = userId, CreatedDate = DateTime.UtcNow };
        var createTranslate = ConstructionIssueTranslator.TranslateBOToEntity(entity, dto);
        if (createTranslate != ErrorCode.Success)
            throw new InvalidOperationException($"ConstructionIssue translation failed: {createTranslate}");
        entity.LastUpdatedByUserId = userId;
        entity.LastUpdatedDate = DateTime.UtcNow;
        _db.ConstructionIssue.Add(entity);
        await _db.SaveChangesAsync();
        await AddHistory(entity.Id, (int)ConstructionIssueHistoryAction.Created, userId, null, JsonSerializer.Serialize(BuildIssueHistorySnapshot(entity)), "Punt aangemaakt");
        return entity;
    }

    public async Task<ConstructionIssue?> Update(int projectId, int id, ConstructionIssueUpsertBO dto, string? userId)
    {
        var entity = await _db.ConstructionIssue.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == id);
        if (entity == null) return null;
        var old = JsonSerializer.Serialize(BuildIssueHistorySnapshot(entity));
        var createTranslate = ConstructionIssueTranslator.TranslateBOToEntity(entity, dto);
        if (createTranslate != ErrorCode.Success)
            throw new InvalidOperationException($"ConstructionIssue translation failed: {createTranslate}");
        entity.LastUpdatedByUserId = userId;
        entity.LastUpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddHistory(id, (int)ConstructionIssueHistoryAction.Updated, userId, old, JsonSerializer.Serialize(BuildIssueHistorySnapshot(entity)), "Punt geupdate");
        return entity;
    }

    public async Task<bool> ChangeStatus(int projectId, int id, int newStatus, string? optionalComment, string? userId)
    {
        var entity = await _db.ConstructionIssue.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == id);
        if (entity == null) return false;
        var old = entity.Status;
        entity.Status = newStatus;
        entity.LastUpdatedByUserId = userId;
        entity.LastUpdatedDate = DateTime.UtcNow;
        if (newStatus == (int)ConstructionIssueStatus.Resolved) entity.ResolvedDate = DateTime.UtcNow;
        if (newStatus == (int)ConstructionIssueStatus.Closed) entity.ClosedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddHistory(id, (int)ConstructionIssueHistoryAction.StatusChanged, userId, old.ToString(), newStatus.ToString(), optionalComment);
        return true;
    }

    public async Task<bool> AssignResponsible(int projectId, int id, int type, int? responsiblePartyId, string? otherName, string? otherEmail, string? userId)
    {
        var entity = await _db.ConstructionIssue.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == id);
        if (entity == null) return false;
        entity.ResponsiblePartyType = type;
        entity.ResponsiblePartyId = responsiblePartyId;
        entity.ResponsibleOtherName = otherName;
        entity.ResponsibleOtherEmail = otherEmail;
        entity.LastUpdatedByUserId = userId;
        entity.LastUpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddHistory(id, (int)ConstructionIssueHistoryAction.Assigned, userId, null, JsonSerializer.Serialize(new { type, responsiblePartyId, otherName, otherEmail }), "Verantwoordelijke geupdate");
        return true;
    }

    public async Task<int> BulkUpdate(int projectId, ConstructionIssueBulkUpdateBO dto, string? userId)
    {
        var ids = dto.IssueIds?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any()) return 0;

        var issues = await _db.ConstructionIssue.Where(x => x.ProjectId == projectId && ids.Contains(x.Id)).ToListAsync();
        foreach (var issue in issues)
        {
            var old = JsonSerializer.Serialize(BuildIssueHistorySnapshot(issue));
            if (dto.ResponsiblePartyType.HasValue) issue.ResponsiblePartyType = dto.ResponsiblePartyType.Value;
            if (dto.ResponsiblePartyType.HasValue || dto.ResponsiblePartyId.HasValue || dto.ResponsibleOtherName != null || dto.ResponsibleOtherEmail != null)
            {
                issue.ResponsiblePartyId = dto.ResponsiblePartyId;
                issue.ResponsibleOtherName = dto.ResponsibleOtherName;
                issue.ResponsibleOtherEmail = dto.ResponsibleOtherEmail;
            }
            if (dto.Status.HasValue) issue.Status = dto.Status.Value;
            if (dto.DueDate.HasValue) issue.DueDate = dto.DueDate;
            if (dto.Priority.HasValue) issue.Priority = dto.Priority.Value;
            if (dto.IssueType.HasValue) issue.IssueType = dto.IssueType.Value;
            if (dto.IssuePhase.HasValue) issue.IssuePhase = dto.IssuePhase.Value;
            if (dto.CategoryId.HasValue) issue.CategoryId = dto.CategoryId.Value;
            issue.LastUpdatedByUserId = userId;
            issue.LastUpdatedDate = DateTime.UtcNow;
            await AddHistory(issue.Id, (int)ConstructionIssueHistoryAction.Updated, userId, old, JsonSerializer.Serialize(BuildIssueHistorySnapshot(issue)), "Bulk update");
        }

        await _db.SaveChangesAsync();
        return issues.Count;
    }

    public async Task<List<ConstructionIssueMedia>> GetMedia(int projectId, int issueId)
    {
        return await _db.ConstructionIssueMedia
            .Where(x => x.IssueId == issueId && x.Issue.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();
    }

    public async Task<ConstructionIssueMedia?> AddMedia(int projectId, int issueId, string fileId, int mediaType, string? userId)
    {
        var exists = await _db.ConstructionIssue.AnyAsync(x => x.ProjectId == projectId && x.Id == issueId);
        if (!exists) return null;

        var entity = new ConstructionIssueMedia
        {
            IssueId = issueId,
            FileId = fileId,
            MediaType = mediaType,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };
        _db.ConstructionIssueMedia.Add(entity);
        await _db.SaveChangesAsync();
        await AddHistory(issueId, (int)ConstructionIssueHistoryAction.AttachmentAdded, userId, null, JsonSerializer.Serialize(BuildMediaHistorySnapshot(entity)), "Bijlage toegevoegd");
        return entity;
    }

    public async Task<bool> DeleteMedia(int projectId, int issueId, int mediaId, string? userId)
    {
        var media = await _db.ConstructionIssueMedia.FirstOrDefaultAsync(x => x.Id == mediaId && x.IssueId == issueId && x.Issue.ProjectId == projectId);
        if (media == null) return false;
        _db.ConstructionIssueMedia.Remove(media);
        await _db.SaveChangesAsync();
        await AddHistory(issueId, (int)ConstructionIssueHistoryAction.AttachmentRemoved, userId, JsonSerializer.Serialize(BuildMediaHistorySnapshot(media)), null, "Bijlage verwijderd");
        return true;
    }

    public async Task<bool> Delete(int projectId, int issueId, string? userId)
    {
        var issue = await _db.ConstructionIssue.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == issueId);
        if (issue == null) return false;

        var reportItems = await _db.ConstructionIssueReportItem.Where(x => x.IssueId == issueId).ToListAsync();
        var notifications = await _db.ConstructionIssueNotification.Where(x => x.IssueId == issueId).ToListAsync();
        var media = await _db.ConstructionIssueMedia.Where(x => x.IssueId == issueId).ToListAsync();
        var history = await _db.ConstructionIssueHistory.Where(x => x.IssueId == issueId).ToListAsync();

        if (reportItems.Count > 0) _db.ConstructionIssueReportItem.RemoveRange(reportItems);
        if (notifications.Count > 0) _db.ConstructionIssueNotification.RemoveRange(notifications);
        if (media.Count > 0) _db.ConstructionIssueMedia.RemoveRange(media);
        if (history.Count > 0) _db.ConstructionIssueHistory.RemoveRange(history);

        _db.ConstructionIssue.Remove(issue);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<List<ConstructionIssueNotification>> GetNotifications(int projectId, int issueId)
    {
        return _db.ConstructionIssueNotification
            .Where(x => x.IssueId == issueId && x.Issue.ProjectId == projectId)
            .OrderByDescending(x => x.SentDate)
            .ToListAsync();
    }

    private static object BuildIssueHistorySnapshot(ConstructionIssue issue)
    {
        return new
        {
            issue.Id,
            issue.ProjectId,
            issue.Title,
            issue.Description,
            issue.LocationText,
            issue.CategoryId,
            issue.BuildingPart,
            issue.RoomOrZone,
            issue.UnitId,
            issue.IssueType,
            issue.IssuePhase,
            issue.Priority,
            issue.Status,
            issue.ResponsiblePartyType,
            issue.ResponsiblePartyId,
            issue.ResponsibleOtherName,
            issue.ResponsibleOtherEmail,
            issue.DueDate,
            issue.ResolvedDate,
            issue.ClosedDate,
            issue.PlanDocumentId,
            issue.PlanPageNumber,
            issue.PlanXnormalized,
            issue.PlanYnormalized,
            issue.LastSentDate
        };
    }

    private static object BuildMediaHistorySnapshot(ConstructionIssueMedia media)
    {
        return new
        {
            media.Id,
            media.IssueId,
            media.FileId,
            media.MediaType,
            media.CreatedByUserId,
            media.CreatedDate
        };
    }

    public async Task AddHistory(int issueId, int action, string? userId, string? oldValueJson, string? newValueJson, string? comment)
    {
        _db.ConstructionIssueHistory.Add(new ConstructionIssueHistory
        {
            IssueId = issueId,
            Action = action,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            Comment = comment
        });
        await _db.SaveChangesAsync();
    }
}