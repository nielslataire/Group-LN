using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject;

public class ProjectTaakService : IProjectTaakService
{
    private readonly cpmRunningContext _db;
    public ProjectTaakService(cpmRunningContext db) { _db = db; }

    private static readonly int StatusAfgerond = (int)TaakStatus.Afgerond;
    private static readonly int StatusGeannuleerd = (int)TaakStatus.Geannuleerd;

    public Task<ProjectTaak?> GetById(int id) =>
        _db.ProjectTaak.Include(t => t.Project).Include(t => t.Mijlpaal).Include(t => t.ProjectDossier)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<ProjectTaak>> SearchProject(int projectId, TaakFilterBO f)
    {
        var q = _db.ProjectTaak.Where(t => t.ProjectId == projectId);
        q = ApplyFilter(q, f);
        return await q.OrderBy(t => t.Vervaldatum ?? DateOnly.MaxValue).ThenByDescending(t => t.CreatedDate).ToListAsync();
    }

    public async Task<List<ProjectTaak>> SearchMijnTaken(string userId, IEnumerable<int> rollen, IEnumerable<int> zichtbareProjectIds, TaakFilterBO f)
    {
        var projectIds = zichtbareProjectIds.ToList();
        var rolLijst = rollen.ToList();

        var q = _db.ProjectTaak.Include(t => t.Project).AsQueryable()
            .Where(t => t.ProjectId == null || projectIds.Contains(t.ProjectId.Value))
            .Where(t => t.ToegewezenAanUserId == userId
                     || (t.ToegewezenAanUserId == null && t.ToegewezenAanRol != null && rolLijst.Contains(t.ToegewezenAanRol.Value))
                     || (t.ToegewezenAanUserId == null && t.ToegewezenAanRol == null && t.CreatedByUserId == userId));

        q = ApplyFilter(q, f);
        return await q.OrderBy(t => t.Vervaldatum ?? DateOnly.MaxValue).ThenByDescending(t => t.CreatedDate).ToListAsync();
    }

    private static IQueryable<ProjectTaak> ApplyFilter(IQueryable<ProjectTaak> q, TaakFilterBO f)
    {
        if (f == null) return q;
        if (f.Status.HasValue) q = q.Where(t => t.Status == f.Status.Value);
        if (f.Prioriteit.HasValue) q = q.Where(t => t.Prioriteit == f.Prioriteit.Value);
        if (f.ProjectId.HasValue) q = q.Where(t => t.ProjectId == f.ProjectId.Value);
        if (!string.IsNullOrWhiteSpace(f.ToegewezenAanUserId)) q = q.Where(t => t.ToegewezenAanUserId == f.ToegewezenAanUserId);
        if (f.AlleenAchterstallig == true)
        {
            var vandaag = DateOnly.FromDateTime(DateTime.Today);
            q = q.Where(t => t.Vervaldatum != null && t.Vervaldatum.Value < vandaag && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd);
        }
        if (!string.IsNullOrWhiteSpace(f.Text))
            q = q.Where(t => t.Titel.Contains(f.Text) || (t.Omschrijving ?? "").Contains(f.Text));
        return q;
    }

    public async Task<ProjectTaak> Create(TaakUpsertBO dto, string? userId)
    {
        var entity = new ProjectTaak
        {
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };
        Apply(entity, dto);
        _db.ProjectTaak.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<ProjectTaak?> Update(int id, TaakUpsertBO dto, string? userId)
    {
        var entity = await _db.ProjectTaak.FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return null;
        Apply(entity, dto);
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> ChangeStatus(int id, int newStatus, string? userId)
    {
        var entity = await _db.ProjectTaak.FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return false;

        entity.Status = newStatus;
        if (newStatus == StatusAfgerond && entity.AfgewerktOp == null)
            entity.AfgewerktOp = DateTime.UtcNow;
        else if (newStatus != StatusAfgerond)
            entity.AfgewerktOp = null;
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Reassign(int id, string? toegewezenAanUserId, int? toegewezenAanRol, string? userId)
    {
        var entity = await _db.ProjectTaak.FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return false;

        entity.ToegewezenAanUserId = toegewezenAanUserId;
        entity.ToegewezenAanRol = toegewezenAanRol;
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(int id, string? userId)
    {
        var entity = await _db.ProjectTaak.FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null) return false;
        _db.ProjectTaak.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<int> CountOpenVoorGebruiker(string userId) =>
        _db.ProjectTaak.CountAsync(t => t.ToegewezenAanUserId == userId && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd);

    public Task<List<ProjectTaak>> GetTopOpenVoorGebruiker(string userId, int aantal = 5) =>
        _db.ProjectTaak.Include(t => t.Project)
            .Where(t => t.ToegewezenAanUserId == userId && t.Status != StatusAfgerond && t.Status != StatusGeannuleerd)
            .OrderBy(t => t.Vervaldatum ?? DateOnly.MaxValue).ThenByDescending(t => t.Prioriteit)
            .Take(aantal).ToListAsync();

    public async Task<ProjectTaak> CreateVanuitTrigger(int? projectId, int mijlpaalId, string titel, string? omschrijving,
        string? toegewezenAanUserId, int? toegewezenAanRol, DateOnly? vervaldatum, string? triggeredBy)
    {
        var entity = new ProjectTaak
        {
            ProjectId = projectId,
            MijlpaalId = mijlpaalId,
            Titel = titel,
            Omschrijving = omschrijving,
            Status = (int)TaakStatus.Open,
            Prioriteit = (int)TaakPrioriteit.Normaal,
            ToegewezenAanUserId = toegewezenAanUserId,
            ToegewezenAanRol = toegewezenAanRol,
            Vervaldatum = vervaldatum,
            Herkomst = (int)TaakHerkomst.Trigger,
            CreatedByUserId = triggeredBy,
            CreatedDate = DateTime.UtcNow
        };
        _db.ProjectTaak.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    private static void Apply(ProjectTaak entity, TaakUpsertBO dto)
    {
        entity.ProjectId = dto.ProjectId;
        entity.UnitId = dto.UnitId;
        entity.MijlpaalId = dto.MijlpaalId;
        entity.ProjectDossierId = dto.ProjectDossierId;
        entity.ConstructionIssueId = dto.ConstructionIssueId;
        entity.Titel = (dto.Titel ?? string.Empty).Trim();
        entity.Omschrijving = dto.Omschrijving;
        entity.Status = dto.Status;
        entity.Prioriteit = dto.Prioriteit;
        entity.ToegewezenAanUserId = string.IsNullOrWhiteSpace(dto.ToegewezenAanUserId) ? null : dto.ToegewezenAanUserId;
        entity.ToegewezenAanRol = dto.ToegewezenAanRol;
        entity.Vervaldatum = dto.Vervaldatum;
        entity.Herkomst = dto.Herkomst;
    }
}
