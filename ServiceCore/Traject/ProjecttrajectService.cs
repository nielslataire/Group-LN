using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject;

public class ProjecttrajectService : IProjecttrajectService
{
    private readonly cpmRunningContext _db;
    public ProjecttrajectService(cpmRunningContext db) { _db = db; }

    public Task<Projecttraject?> GetByProject(int projectId, bool includeDetails = true)
    {
        IQueryable<Projecttraject> q = _db.Projecttraject;
        if (includeDetails)
        {
            q = q.Include(t => t.Fases.OrderBy(f => f.Volgorde))
                 .Include(t => t.Mijlpalen.OrderBy(m => m.Volgorde))
                     .ThenInclude(m => m.ProjecttrajectFase);
            q = q.Include(t => t.Mijlpalen)
                 .ThenInclude(m => m.Unit);
        }
        return q.FirstOrDefaultAsync(t => t.ProjectId == projectId)!;
    }

    public Task<List<int>> GetProjectIdsWithTraject() =>
        _db.Projecttraject.Select(t => t.ProjectId).ToListAsync();

    public async Task<bool> Update(int projecttrajectId, string? naam, int? status, string? userId)
    {
        var t = await _db.Projecttraject.FirstOrDefaultAsync(x => x.Id == projecttrajectId);
        if (t == null) return false;
        if (!string.IsNullOrWhiteSpace(naam)) t.Naam = naam!.Trim();
        if (status.HasValue) t.Status = status.Value;
        t.ModifiedByUserId = userId;
        t.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int projectId, string? userId)
    {
        var t = await _db.Projecttraject.FirstOrDefaultAsync(x => x.ProjectId == projectId);
        if (t == null) return false;
        _db.Projecttraject.Remove(t); // cascade -> fases, mijlpalen, historiek, afhankelijkheden
        await _db.SaveChangesAsync();
        return true;
    }
}
