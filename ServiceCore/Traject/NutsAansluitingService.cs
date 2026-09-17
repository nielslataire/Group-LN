using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject;

public class NutsAansluitingService : INutsAansluitingService
{
    private readonly cpmRunningContext _db;
    public NutsAansluitingService(cpmRunningContext db) { _db = db; }

    public Task<ProjectNutsAansluiting?> GetById(int projectId, int id) =>
        _db.ProjectNutsAansluiting
            .Include(n => n.ProjectDossier)
            .Include(n => n.Unit)
            .Include(n => n.NetbeheerderCompany)
            .FirstOrDefaultAsync(n => n.ProjectDossier.ProjectId == projectId && n.ProjectDossierId == id);

    public Task<List<ProjectNutsAansluiting>> GetForProject(int projectId) =>
        _db.ProjectNutsAansluiting
            .Include(n => n.ProjectDossier)
            .Include(n => n.Unit)
            .Include(n => n.NetbeheerderCompany)
            .Where(n => n.ProjectDossier.ProjectId == projectId)
            .OrderByDescending(n => n.ProjectDossier.CreatedDate)
            .ToListAsync();

    public async Task<ProjectNutsAansluiting> Create(NutsAansluitingUpsertBO dto, string? userId)
    {
        var dossier = new ProjectDossier
        {
            ProjectId = dto.ProjectId,
            UnitId = dto.UnitId,
            DossierKind = (int)DossierKind.NutsAansluiting,
            Titel = (dto.Titel ?? string.Empty).Trim(),
            Referentie = dto.Referentie,
            Status = dto.Status,
            ExterneContactNaam = dto.ExterneContactNaam,
            ExterneContactEmail = dto.ExterneContactEmail,
            AanvraagDatum = dto.AanvraagDatum,
            VerwachteAfhandelingDatum = dto.VerwachteAfhandelingDatum,
            AfgehandeldDatum = dto.AfgehandeldDatum,
            Bedrag = dto.Bedrag,
            Omschrijving = dto.Omschrijving,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };
        var nuts = new ProjectNutsAansluiting();
        ApplyNuts(nuts, dto);
        dossier.NutsAansluiting = nuts;

        _db.ProjectDossier.Add(dossier);
        await _db.SaveChangesAsync();

        _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
        {
            ProjectDossierId = dossier.Id,
            Type = (int)DossierGebeurtenisType.Opmerking,
            Titel = "Nutsaansluitingsdossier aangemaakt",
            UserId = userId,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        foreach (var stap in NutsChecklistDefaults.Stappen)
        {
            _db.ProjectDossierSubstap.Add(new ProjectDossierSubstap
            {
                ProjectDossierId = dossier.Id,
                Code = stap.Code,
                Naam = stap.Naam,
                Volgorde = stap.Volgorde,
                Status = (int)DossierSubstapStatus.NietGestart,
                CreatedDate = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        return nuts;
    }

    public async Task<NutsAansluitingBulkResultBO> CreateBulkForUnits(NutsAansluitingBulkCreateBO dto, string? userId)
    {
        var result = new NutsAansluitingBulkResultBO();
        var units = await _db.Units.AsNoTracking()
            .Where(u => u.ProjectId == dto.ProjectId && dto.UnitIds.Contains(u.Id))
            .ToListAsync();

        foreach (var unit in units)
        {
            bool bestaatAl = await _db.ProjectDossier.AnyAsync(d =>
                d.UnitId == unit.Id && d.DossierKind == (int)DossierKind.NutsAansluiting
                && d.Status != (int)DossierStatus.Geannuleerd
                && d.NutsAansluiting != null && d.NutsAansluiting.NutsType == dto.NutsType);
            if (bestaatAl)
            {
                result.OvergeslagenEenheden.Add(unit.Name);
                continue;
            }

            var titel = string.IsNullOrWhiteSpace(dto.TitelPrefix)
                ? $"{((NutsType)dto.NutsType).GetDisplayName()} — {unit.Name}"
                : $"{dto.TitelPrefix} — {unit.Name}";

            var (ean, meternummer) = PrefillMeterdata(dto.NutsType, unit);

            var perUnit = new NutsAansluitingUpsertBO
            {
                ProjectId = dto.ProjectId,
                UnitId = unit.Id,
                Titel = titel,
                Status = dto.Status,
                ExterneContactNaam = dto.ExterneContactNaam,
                ExterneContactEmail = dto.ExterneContactEmail,
                AanvraagDatum = dto.AanvraagDatum,
                VerwachteAfhandelingDatum = dto.VerwachteAfhandelingDatum,
                Omschrijving = dto.Omschrijving,
                NutsType = dto.NutsType,
                NetbeheerderCompanyId = dto.NetbeheerderCompanyId,
                Ean = ean,
                Meternummer = meternummer
            };
            await Create(perUnit, userId);
            result.AantalAangemaakt++;
        }

        return result;
    }

    private static (string? Ean, string? Meternummer) PrefillMeterdata(int nutsType, Units unit) => (NutsType)nutsType switch
    {
        NutsType.Elektriciteit => (unit.EanElektriciteit, null),
        NutsType.Gas => (unit.EanGas, null),
        NutsType.Water => (null, unit.WatermeterNummer),
        _ => (null, null)
    };

    public async Task<ProjectNutsAansluiting?> Update(int id, NutsAansluitingUpsertBO dto, string? userId)
    {
        var dossier = await _db.ProjectDossier.Include(d => d.NutsAansluiting).FirstOrDefaultAsync(d => d.Id == id);
        if (dossier?.NutsAansluiting == null) return null;

        var oudStatus = dossier.Status;
        dossier.UnitId = dto.UnitId;
        dossier.Titel = (dto.Titel ?? string.Empty).Trim();
        dossier.Referentie = dto.Referentie;
        dossier.Status = dto.Status;
        dossier.ExterneContactNaam = dto.ExterneContactNaam;
        dossier.ExterneContactEmail = dto.ExterneContactEmail;
        dossier.AanvraagDatum = dto.AanvraagDatum;
        dossier.VerwachteAfhandelingDatum = dto.VerwachteAfhandelingDatum;
        dossier.AfgehandeldDatum = dto.AfgehandeldDatum;
        dossier.Bedrag = dto.Bedrag;
        dossier.Omschrijving = dto.Omschrijving;
        dossier.ModifiedByUserId = userId;
        dossier.ModifiedDate = DateTime.UtcNow;

        ApplyNuts(dossier.NutsAansluiting, dto);

        if (dto.Status == (int)DossierStatus.Afgehandeld && dossier.AfgehandeldDatum == null)
            dossier.AfgehandeldDatum = DateOnly.FromDateTime(DateTime.Today);

        await _db.SaveChangesAsync();

        if (oudStatus != dossier.Status)
        {
            _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
            {
                ProjectDossierId = dossier.Id,
                Type = (int)DossierGebeurtenisType.StatusWijziging,
                Titel = $"Status: {(DossierStatus)oudStatus} -> {(DossierStatus)dossier.Status}",
                UserId = userId,
                Datum = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return dossier.NutsAansluiting;
    }

    public async Task<(string? EanGas, string? EanElektriciteit, string? Watermeter)> GetUnitMeterData(int unitId)
    {
        var u = await _db.Units.AsNoTracking()
            .Where(x => x.Id == unitId)
            .Select(x => new { x.EanGas, x.EanElektriciteit, x.WatermeterNummer })
            .FirstOrDefaultAsync();
        return u == null ? (null, null, null) : (u.EanGas, u.EanElektriciteit, u.WatermeterNummer);
    }

    private static void ApplyNuts(ProjectNutsAansluiting nuts, NutsAansluitingUpsertBO dto)
    {
        nuts.UnitId = dto.UnitId;
        nuts.NutsType = dto.NutsType;
        nuts.NetbeheerderCompanyId = dto.NetbeheerderCompanyId;
        nuts.Ean = dto.Ean;
        nuts.Meternummer = dto.Meternummer;
        nuts.GevraagdVermogen = dto.GevraagdVermogen;
        nuts.AanvraagVerstuurdOp = dto.AanvraagVerstuurdOp;
        nuts.AansluitkostRaming = dto.AansluitkostRaming;
        nuts.AansluitkostDefinitief = dto.AansluitkostDefinitief;
    }
}
