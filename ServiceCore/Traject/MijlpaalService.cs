using System.Text.Json;
using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Translators;

namespace ServiceCore.Traject;

public class MijlpaalService : IMijlpaalService
{
    private readonly cpmRunningContext _db;
    public MijlpaalService(cpmRunningContext db) { _db = db; }

    private static readonly int StatusBereikt = (int)MijlpaalStatus.Bereikt;
    private static readonly int StatusNvt = (int)MijlpaalStatus.NietVanToepassing;

    public Task<Mijlpaal?> GetById(int projecttrajectId, int id) =>
        _db.Mijlpaal
            .Include(m => m.ProjecttrajectFase)
            .Include(m => m.Unit)
            .Include(m => m.Historiek)
            .FirstOrDefaultAsync(m => m.ProjecttrajectId == projecttrajectId && m.Id == id);

    public async Task<List<Mijlpaal>> Search(int projecttrajectId, MijlpaalFilterBO f)
    {
        var q = _db.Mijlpaal.Include(m => m.ProjecttrajectFase).Include(m => m.Unit)
            .Where(m => m.ProjecttrajectId == projecttrajectId);

        if (f.Status.HasValue) q = q.Where(m => m.Status == f.Status.Value);
        if (f.FaseId.HasValue) q = q.Where(m => m.ProjecttrajectFaseId == f.FaseId.Value);
        if (f.UnitId.HasValue) q = q.Where(m => m.UnitId == f.UnitId.Value);
        if (f.AlleenProjectniveau == true) q = q.Where(m => m.UnitId == null);
        else if (f.AlleenProjectniveau == false) q = q.Where(m => m.UnitId != null);
        if (f.MijlpaalType.HasValue) q = q.Where(m => m.MijlpaalType == f.MijlpaalType.Value);
        if (f.VerantwoordelijkeRol.HasValue) q = q.Where(m => m.VerantwoordelijkeRol == f.VerantwoordelijkeRol.Value);
        if (!string.IsNullOrWhiteSpace(f.VerantwoordelijkeUserId)) q = q.Where(m => m.VerantwoordelijkeUserId == f.VerantwoordelijkeUserId);
        if (f.AlleenVerplicht == true) q = q.Where(m => m.IsVerplicht);
        if (f.Overdue == true)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            q = q.Where(m => m.Status != StatusBereikt && m.Status != StatusNvt
                             && (m.Doeldatum ?? m.DoeldatumBerekend) != null
                             && (m.Doeldatum ?? m.DoeldatumBerekend) < today);
        }
        if (!string.IsNullOrWhiteSpace(f.Text))
            q = q.Where(m => m.Naam.Contains(f.Text) || (m.Opmerking ?? "").Contains(f.Text));

        return await q.OrderBy(m => m.Volgorde).ThenBy(m => m.Doeldatum ?? m.DoeldatumBerekend).ToListAsync();
    }

    public async Task<Mijlpaal> Create(MijlpaalUpsertBO dto, string? userId)
    {
        var entity = new Mijlpaal
        {
            ProjecttrajectId = dto.ProjecttrajectId,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };
        var r = MijlpaalTranslator.TranslateBOToEntity(entity, dto);
        if (r != ErrorCode.Success)
            throw new InvalidOperationException($"Mijlpaal translation failed: {r}");
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        _db.Mijlpaal.Add(entity);
        await _db.SaveChangesAsync();
        await AddHistory(entity.Id, (int)MijlpaalHistoriekActie.Aangemaakt, userId, null,
            JsonSerializer.Serialize(Snapshot(entity)), "Mijlpaal aangemaakt");
        return entity;
    }

    public async Task<Mijlpaal?> Update(int id, MijlpaalUpsertBO dto, string? userId)
    {
        var entity = await _db.Mijlpaal.FirstOrDefaultAsync(m => m.Id == id);
        if (entity == null) return null;
        var old = JsonSerializer.Serialize(Snapshot(entity));
        var r = MijlpaalTranslator.TranslateBOToEntity(entity, dto);
        if (r != ErrorCode.Success)
            throw new InvalidOperationException($"Mijlpaal translation failed: {r}");
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddHistory(id, (int)MijlpaalHistoriekActie.Bijgewerkt, userId, old,
            JsonSerializer.Serialize(Snapshot(entity)), "Mijlpaal bijgewerkt");
        return entity;
    }

    public async Task<bool> ChangeStatus(int projecttrajectId, MijlpaalStatusChangeBO dto, string? userId)
    {
        var entity = await _db.Mijlpaal.FirstOrDefaultAsync(m => m.ProjecttrajectId == projecttrajectId && m.Id == dto.MijlpaalId);
        if (entity == null) return false;

        var old = entity.Status;
        entity.Status = dto.NieuweStatus;

        if (dto.NieuweStatus == StatusBereikt)
            entity.WerkelijkeDatum = dto.WerkelijkeDatum ?? entity.WerkelijkeDatum ?? DateOnly.FromDateTime(DateTime.Today);
        else if (dto.NieuweStatus == (int)MijlpaalStatus.Open || dto.NieuweStatus == (int)MijlpaalStatus.Bezig)
            entity.WerkelijkeDatum = null;

        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddHistory(entity.Id, (int)MijlpaalHistoriekActie.Statuswijziging, userId,
            old.ToString(), dto.NieuweStatus.ToString(), dto.Opmerking);
        return true;
    }

    public async Task<bool> AssignResponsible(int projecttrajectId, int id, int? rol, int? partijType, int? partijId, string? userId, string? assignedUserId)
    {
        var entity = await _db.Mijlpaal.FirstOrDefaultAsync(m => m.ProjecttrajectId == projecttrajectId && m.Id == id);
        if (entity == null) return false;
        entity.VerantwoordelijkeRol = rol;
        entity.VerantwoordelijkePartijType = partijType;
        entity.VerantwoordelijkePartijId = partijId;
        entity.VerantwoordelijkeUserId = assignedUserId;
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await AddHistory(id, (int)MijlpaalHistoriekActie.VerantwoordelijkeToegewezen, userId, null,
            JsonSerializer.Serialize(new { rol, partijType, partijId, assignedUserId }), "Verantwoordelijke toegewezen");
        return true;
    }

    public async Task<int> BulkUpdate(int projecttrajectId, MijlpaalBulkUpdateBO dto, string? userId)
    {
        var ids = dto.MijlpaalIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0) return 0;

        var items = await _db.Mijlpaal.Where(m => m.ProjecttrajectId == projecttrajectId && ids.Contains(m.Id)).ToListAsync();
        foreach (var m in items)
        {
            var old = JsonSerializer.Serialize(Snapshot(m));
            if (dto.Status.HasValue)
            {
                m.Status = dto.Status.Value;
                if (dto.Status.Value == StatusBereikt && m.WerkelijkeDatum == null)
                    m.WerkelijkeDatum = DateOnly.FromDateTime(DateTime.Today);
            }
            if (dto.Doeldatum.HasValue) m.Doeldatum = dto.Doeldatum;
            if (dto.VerantwoordelijkeRol.HasValue) m.VerantwoordelijkeRol = dto.VerantwoordelijkeRol;
            if (dto.VerantwoordelijkeUserId != null) m.VerantwoordelijkeUserId = dto.VerantwoordelijkeUserId;
            if (dto.ProjecttrajectFaseId.HasValue) m.ProjecttrajectFaseId = dto.ProjecttrajectFaseId;
            m.ModifiedByUserId = userId;
            m.ModifiedDate = DateTime.UtcNow;
            await AddHistory(m.Id, (int)MijlpaalHistoriekActie.Bijgewerkt, userId, old,
                JsonSerializer.Serialize(Snapshot(m)), "Bulk-bijwerking");
        }
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<bool> Delete(int projecttrajectId, int id, string? userId)
    {
        var entity = await _db.Mijlpaal.FirstOrDefaultAsync(m => m.ProjecttrajectId == projecttrajectId && m.Id == id);
        if (entity == null) return false;

        // FK_ProjectDossierMijlpaal_Mijlpaal is NO ACTION (dubbel cascade-pad via ProjectDossier
        // vermeden) — koppelingen dus zelf opruimen vóór het verwijderen van de mijlpaal.
        var koppelingen = await _db.ProjectDossierMijlpaal.Where(x => x.MijlpaalId == id).ToListAsync();
        if (koppelingen.Count > 0) _db.ProjectDossierMijlpaal.RemoveRange(koppelingen);

        _db.Mijlpaal.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<List<MijlpaalHistoriek>> GetHistoriek(int mijlpaalId) =>
        _db.MijlpaalHistoriek.Where(h => h.MijlpaalId == mijlpaalId).OrderByDescending(h => h.Timestamp).ToListAsync();

    public async Task AddHistory(int mijlpaalId, int actie, string? userId, string? oldValueJson, string? newValueJson, string? comment)
    {
        _db.MijlpaalHistoriek.Add(new MijlpaalHistoriek
        {
            MijlpaalId = mijlpaalId,
            Actie = actie,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            Opmerking = comment
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<Mijlpaal>> SearchPortfolio(IEnumerable<int> projectIds, MijlpaalFilterBO f)
    {
        var ids = projectIds.ToList();
        var q = _db.Mijlpaal
            .Include(m => m.ProjecttrajectFase)
            .Include(m => m.Unit)
            .Include(m => m.Projecttraject).ThenInclude(t => t.Project)
            .Where(m => ids.Contains(m.Projecttraject.ProjectId));

        if (f.ProjectId.HasValue) q = q.Where(m => m.Projecttraject.ProjectId == f.ProjectId.Value);
        if (f.Status.HasValue) q = q.Where(m => m.Status == f.Status.Value);
        if (f.FaseId.HasValue) q = q.Where(m => m.ProjecttrajectFaseId == f.FaseId.Value);
        if (f.UnitId.HasValue) q = q.Where(m => m.UnitId == f.UnitId.Value);
        if (f.AlleenProjectniveau == true) q = q.Where(m => m.UnitId == null);
        else if (f.AlleenProjectniveau == false) q = q.Where(m => m.UnitId != null);
        if (f.MijlpaalType.HasValue) q = q.Where(m => m.MijlpaalType == f.MijlpaalType.Value);
        if (f.VerantwoordelijkeRol.HasValue) q = q.Where(m => m.VerantwoordelijkeRol == f.VerantwoordelijkeRol.Value);
        if (!string.IsNullOrWhiteSpace(f.VerantwoordelijkeUserId)) q = q.Where(m => m.VerantwoordelijkeUserId == f.VerantwoordelijkeUserId);
        if (f.AlleenVerplicht == true) q = q.Where(m => m.IsVerplicht);
        if (f.Overdue == true)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            q = q.Where(m => m.Status != StatusBereikt && m.Status != StatusNvt
                             && (m.Doeldatum ?? m.DoeldatumBerekend) != null
                             && (m.Doeldatum ?? m.DoeldatumBerekend) < today);
        }
        if (!string.IsNullOrWhiteSpace(f.Text))
            q = q.Where(m => m.Naam.Contains(f.Text) || (m.Opmerking ?? "").Contains(f.Text));

        return await q.OrderBy(m => m.Doeldatum ?? m.DoeldatumBerekend ?? DateOnly.MaxValue).ThenBy(m => m.Volgorde).ToListAsync();
    }

    public Task<int> CountOverdue(IEnumerable<int> projectIds)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var ids = projectIds.ToList();
        return _db.Mijlpaal
            .Where(m => ids.Contains(m.Projecttraject.ProjectId)
                        && m.Status != StatusBereikt && m.Status != StatusNvt
                        && (m.Doeldatum ?? m.DoeldatumBerekend) != null
                        && (m.Doeldatum ?? m.DoeldatumBerekend) < today)
            .CountAsync();
    }

    private static object Snapshot(Mijlpaal m) => new
    {
        m.Naam,
        m.Code,
        m.ProjecttrajectFaseId,
        m.UnitId,
        m.MijlpaalType,
        m.Status,
        m.Doeldatum,
        m.DoeldatumBerekend,
        m.WerkelijkeDatum,
        m.VerantwoordelijkeRol,
        m.VerantwoordelijkePartijType,
        m.VerantwoordelijkePartijId,
        m.VerantwoordelijkeUserId,
        m.IsVerplicht,
        m.Volgorde,
        m.Opmerking
    };
}
