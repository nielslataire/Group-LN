using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject;

public class TrajectSjabloonService : ITrajectSjabloonService
{
    private readonly cpmRunningContext _db;
    public TrajectSjabloonService(cpmRunningContext db) { _db = db; }

    public Task<List<TrajectSjabloon>> GetAll(bool includeInactive = false)
    {
        var q = _db.TrajectSjabloon.Include(s => s.Fases).AsQueryable();
        if (!includeInactive) q = q.Where(s => s.IsActief);
        return q.OrderByDescending(s => s.IsStandaard).ThenBy(s => s.Naam).ToListAsync();
    }

    public Task<TrajectSjabloon?> GetById(int id, bool includeDetails = true)
    {
        IQueryable<TrajectSjabloon> q = _db.TrajectSjabloon;
        if (includeDetails)
            q = q.Include(s => s.Fases.OrderBy(f => f.Volgorde)).ThenInclude(f => f.Mijlpalen.OrderBy(m => m.Volgorde));
        return q.FirstOrDefaultAsync(s => s.Id == id)!;
    }

    public async Task<TrajectSjabloon?> GetStandaardVoorProjectType(int? projectType)
    {
        // 1) exacte match op projecttype, 2) type-onafhankelijk standaardsjabloon, 3) eender welk standaardsjabloon
        return await _db.TrajectSjabloon.Include(s => s.Fases).ThenInclude(f => f.Mijlpalen)
                   .Where(s => s.IsActief && s.IsStandaard && s.ProjectType == projectType)
                   .OrderBy(s => s.Id).FirstOrDefaultAsync()
               ?? await _db.TrajectSjabloon.Include(s => s.Fases).ThenInclude(f => f.Mijlpalen)
                   .Where(s => s.IsActief && s.IsStandaard && s.ProjectType == null)
                   .OrderBy(s => s.Id).FirstOrDefaultAsync()
               ?? await _db.TrajectSjabloon.Include(s => s.Fases).ThenInclude(f => f.Mijlpalen)
                   .Where(s => s.IsActief && s.IsStandaard)
                   .OrderBy(s => s.Id).FirstOrDefaultAsync();
    }

    public async Task<TrajectSjabloon> Upsert(TrajectSjabloonBO dto, string? userId)
    {
        TrajectSjabloon entity;
        if (dto.Id is int id and > 0)
        {
            entity = await _db.TrajectSjabloon
                         .Include(s => s.Fases).ThenInclude(f => f.Mijlpalen)
                         .FirstOrDefaultAsync(s => s.Id == id)
                     ?? throw new InvalidOperationException($"Trajectsjabloon {id} niet gevonden.");
            // kinderen vervangen (replace-strategie: eenvoudig en veilig; instanties matchen op Code)
            foreach (var f in entity.Fases.ToList())
            {
                _db.TrajectSjabloonMijlpaal.RemoveRange(f.Mijlpalen);
                _db.TrajectSjabloonFase.Remove(f);
            }
        }
        else
        {
            entity = new TrajectSjabloon { CreatedByUserId = userId, CreatedDate = DateTime.UtcNow };
            _db.TrajectSjabloon.Add(entity);
        }

        entity.Naam = (dto.Naam ?? string.Empty).Trim();
        entity.ProjectType = dto.ProjectType;
        entity.IsStandaard = dto.IsStandaard;
        entity.IsActief = dto.IsActief;
        entity.Omschrijving = dto.Omschrijving;
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;

        int faseVolgorde = 0;
        foreach (var fb in dto.Fases ?? new())
        {
            var fase = new TrajectSjabloonFase
            {
                Naam = (fb.Naam ?? string.Empty).Trim(),
                Code = (fb.Code ?? string.Empty).Trim(),
                Volgorde = fb.Volgorde == 0 ? (faseVolgorde += 10) : fb.Volgorde,
                KleurCode = fb.KleurCode,
                StandaardProjectStatusId = fb.StandaardProjectStatusId
            };
            int mVolgorde = 0;
            foreach (var mb in fb.Mijlpalen ?? new())
            {
                fase.Mijlpalen.Add(new TrajectSjabloonMijlpaal
                {
                    Naam = (mb.Naam ?? string.Empty).Trim(),
                    Code = (mb.Code ?? string.Empty).Trim(),
                    Volgorde = mb.Volgorde == 0 ? (mVolgorde += 10) : mb.Volgorde,
                    MijlpaalType = mb.MijlpaalType,
                    Scope = mb.Scope,
                    VerantwoordelijkeRol = mb.VerantwoordelijkeRol,
                    DoeldatumAnkerCode = mb.DoeldatumAnkerCode,
                    DoeldatumOffsetDagen = mb.DoeldatumOffsetDagen,
                    IsVerplicht = mb.IsVerplicht,
                    BronBinding = mb.BronBinding,
                    BronParam = mb.BronParam,
                    DossierKind = mb.DossierKind,
                    Omschrijving = mb.Omschrijving
                });
            }
            entity.Fases.Add(fase);
        }

        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> Delete(int id, string? userId)
    {
        var entity = await _db.TrajectSjabloon.FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return false;

        bool inGebruik = await _db.Projecttraject.AnyAsync(t => t.TrajectSjabloonId == id);
        if (inGebruik)
        {
            // niet hard verwijderen zolang er trajecten aan hangen: deactiveren
            entity.IsActief = false;
            entity.ModifiedByUserId = userId;
            entity.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        _db.TrajectSjabloon.Remove(entity); // cascade -> fases, mijlpalen
        await _db.SaveChangesAsync();
        return true;
    }
}
