using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Traject;

public class ProjectDossierService : IProjectDossierService
{
    private readonly cpmRunningContext _db;
    public ProjectDossierService(cpmRunningContext db) { _db = db; }

    private static readonly int StatusAfgehandeld = (int)DossierStatus.Afgehandeld;
    private static readonly int StatusGeannuleerd = (int)DossierStatus.Geannuleerd;

    /// <summary>Standaard checklist voor een Omgevingsvergunning-dossier (Code, Naam, Volgorde).</summary>
    private static readonly (string Code, string Naam, int Volgorde)[] VergunningStappen =
    {
        ("INGEDIEND",           "Ingediend",                              10),
        ("VOLLEDIG_VERKLAARD",  "Volledig en ontvankelijk verklaard",     20),
        ("OPENBAAR_ONDERZOEK",  "Openbaar onderzoek afgerond",            30),
        ("ADVIEZEN",            "Adviezen ontvangen",                     40),
        ("COLLEGEBESLISSING",   "Beslissing college van B&W",             50),
        ("BEROEPSTERMIJN",      "Beroepstermijn verstreken",              60),
        ("DEFINITIEF",          "Vergunning definitief",                  70)
    };

    /// <summary>Bekende sjabloon-mijlpaal-Codes voor de vergunningsstappen -> substap-Code, voor automatisch koppelen.</summary>
    private static readonly Dictionary<string, string> VergunningMijlpaalNaarSubstap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VERGUNNING_INGEDIEND"] = "INGEDIEND",
        ["VERGUNNING_VOLLEDIG"] = "VOLLEDIG_VERKLAARD",
        ["OPENBAAR_ONDERZOEK"] = "OPENBAAR_ONDERZOEK",
        ["VERGUNNING_VERLEEND"] = "COLLEGEBESLISSING",
        ["VERGUNNING_DEFINITIEF"] = "DEFINITIEF"
    };

    public Task<ProjectDossier?> GetById(int projectId, int id) =>
        _db.ProjectDossier
            .Include(d => d.Unit)
            .Include(d => d.NutsAansluiting)
            .Include(d => d.Gebeurtenissen)
            .Include(d => d.Documenten)
            .Include(d => d.Substappen)
            .FirstOrDefaultAsync(d => d.ProjectId == projectId && d.Id == id);

    public async Task<List<ProjectDossier>> Search(int projectId, DossierFilterBO f)
    {
        var q = _db.ProjectDossier.Include(d => d.Unit).Where(d => d.ProjectId == projectId);

        if (f.DossierKind.HasValue) q = q.Where(d => d.DossierKind == f.DossierKind.Value);
        if (f.Status.HasValue) q = q.Where(d => d.Status == f.Status.Value);
        if (f.UnitId.HasValue) q = q.Where(d => d.UnitId == f.UnitId.Value);
        if (!string.IsNullOrWhiteSpace(f.Text))
            q = q.Where(d => d.Titel.Contains(f.Text) || (d.Referentie ?? "").Contains(f.Text) || (d.Omschrijving ?? "").Contains(f.Text));

        return await q.OrderByDescending(d => d.CreatedDate).ToListAsync();
    }

    public async Task<ProjectDossier> Create(DossierUpsertBO dto, string? userId)
    {
        var entity = new ProjectDossier
        {
            ProjectId = dto.ProjectId,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };
        Apply(entity, dto);
        _db.ProjectDossier.Add(entity);
        await _db.SaveChangesAsync();

        _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
        {
            ProjectDossierId = entity.Id,
            Type = (int)DossierGebeurtenisType.Opmerking,
            Titel = "Dossier aangemaakt",
            UserId = userId,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        if (entity.DossierKind == (int)DossierKind.Omgevingsvergunning)
            await SeedVergunningStappenEnKoppelMijlpalen(entity, userId);

        return entity;
    }

    /// <summary>
    /// Seedt de standaard vergunningschecklist en koppelt automatisch de projectniveau-mijlpalen
    /// van dit project (VERGUNNING_INGEDIEND, VERGUNNING_VOLLEDIG, OPENBAAR_ONDERZOEK,
    /// VERGUNNING_VERLEEND, VERGUNNING_DEFINITIEF) die nog aan geen enkel dossier hangen.
    /// </summary>
    private async Task SeedVergunningStappenEnKoppelMijlpalen(ProjectDossier dossier, string? userId)
    {
        foreach (var (code, naam, volgorde) in VergunningStappen)
        {
            _db.ProjectDossierSubstap.Add(new ProjectDossierSubstap
            {
                ProjectDossierId = dossier.Id,
                Code = code,
                Naam = naam,
                Volgorde = volgorde,
                Status = (int)DossierSubstapStatus.NietGestart,
                CreatedDate = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        var kandidaten = await VindOngekoppeldeVergunningMijlpalen(dossier.ProjectId);
        await KoppelKandidatenAanDossier(dossier.Id, kandidaten, userId, "automatisch gekoppeld");
    }

    private Task<List<Mijlpaal>> VindOngekoppeldeVergunningMijlpalen(int projectId)
    {
        var bekendeCodes = VergunningMijlpaalNaarSubstap.Keys.ToList();
        return _db.Mijlpaal
            .Where(m => m.Projecttraject.ProjectId == projectId && m.UnitId == null
                        && m.Code != null && bekendeCodes.Contains(m.Code) && m.DossierId == null)
            .ToListAsync();
    }

    private async Task KoppelKandidatenAanDossier(int dossierId, List<Mijlpaal> kandidaten, string? userId, string reden)
    {
        if (kandidaten.Count == 0) return;

        foreach (var m in kandidaten)
        {
            m.DossierId = dossierId;
            _db.ProjectDossierMijlpaal.Add(new ProjectDossierMijlpaal { ProjectDossierId = dossierId, MijlpaalId = m.Id });
            if (m.BronBinding == null && !string.IsNullOrWhiteSpace(m.Code)
                && VergunningMijlpaalNaarSubstap.TryGetValue(m.Code!, out var substapCode))
            {
                m.BronBinding = (int)ComputedBinding.DossierSubstap;
                m.BronParam = substapCode;
            }
        }

        _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
        {
            ProjectDossierId = dossierId,
            Type = (int)DossierGebeurtenisType.Opmerking,
            Titel = $"{kandidaten.Count} mijlpaal/mijlpalen {reden}",
            Tekst = string.Join(", ", kandidaten.Select(m => m.Naam)),
            UserId = userId,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<int> RelinkVergunningMijlpalen(int projectId, string? userId)
    {
        var dossier = await _db.ProjectDossier
            .Where(d => d.ProjectId == projectId && d.DossierKind == (int)DossierKind.Omgevingsvergunning
                        && d.Status != StatusGeannuleerd)
            .OrderBy(d => d.CreatedDate)
            .FirstOrDefaultAsync();
        if (dossier == null) return 0;

        var kandidaten = await VindOngekoppeldeVergunningMijlpalen(projectId);
        await KoppelKandidatenAanDossier(dossier.Id, kandidaten, userId, "automatisch (her)gekoppeld");
        return kandidaten.Count;
    }

    public async Task<ProjectDossier?> Update(int id, DossierUpsertBO dto, string? userId)
    {
        var entity = await _db.ProjectDossier.FirstOrDefaultAsync(d => d.Id == id);
        if (entity == null) return null;
        var oudStatus = entity.Status;
        Apply(entity, dto);
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (oudStatus != entity.Status)
            await LogStatusWijziging(entity.Id, oudStatus, entity.Status, userId);

        return entity;
    }

    public async Task<bool> ChangeStatus(int projectId, int id, int newStatus, string? userId, string? opmerking = null)
    {
        var entity = await _db.ProjectDossier.FirstOrDefaultAsync(d => d.ProjectId == projectId && d.Id == id);
        if (entity == null) return false;

        var oud = entity.Status;
        entity.Status = newStatus;
        if (newStatus == StatusAfgehandeld && entity.AfgehandeldDatum == null)
            entity.AfgehandeldDatum = DateOnly.FromDateTime(DateTime.Today);
        entity.ModifiedByUserId = userId;
        entity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await LogStatusWijziging(id, oud, newStatus, userId, opmerking);
        return true;
    }

    public async Task<bool> Delete(int projectId, int id, string? userId)
    {
        var entity = await _db.ProjectDossier.FirstOrDefaultAsync(d => d.ProjectId == projectId && d.Id == id);
        if (entity == null) return false;
        _db.ProjectDossier.Remove(entity); // cascade -> gebeurtenissen, documenten, nutsaansluiting, dossier-mijlpalen
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ProjectDossierGebeurtenis> AddGebeurtenis(DossierGebeurtenisBO dto, string? userId)
    {
        var g = new ProjectDossierGebeurtenis
        {
            ProjectDossierId = dto.ProjectDossierId,
            Type = dto.Type,
            Titel = dto.Titel,
            Tekst = dto.Tekst,
            UserId = userId,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        _db.ProjectDossierGebeurtenis.Add(g);
        await _db.SaveChangesAsync();
        return g;
    }

    public Task<List<ProjectDossierGebeurtenis>> GetGebeurtenissen(int projectDossierId) =>
        _db.ProjectDossierGebeurtenis.Where(g => g.ProjectDossierId == projectDossierId)
            .OrderByDescending(g => g.Datum).ToListAsync();

    public async Task<bool> LinkDoc(int projectDossierId, int? projectDocId, string? fileId, string? naam, string? userId)
    {
        if (projectDocId == null && string.IsNullOrWhiteSpace(fileId)) return false;
        _db.ProjectDossierDocument.Add(new ProjectDossierDocument
        {
            ProjectDossierId = projectDossierId,
            ProjectDocId = projectDocId,
            FileId = fileId,
            Naam = naam,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        });
        _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
        {
            ProjectDossierId = projectDossierId,
            Type = (int)DossierGebeurtenisType.DocumentToegevoegd,
            Titel = naam ?? "Document toegevoegd",
            UserId = userId,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LinkMijlpaal(int projectDossierId, int mijlpaalId)
    {
        bool bestaat = await _db.ProjectDossierMijlpaal.AnyAsync(x => x.ProjectDossierId == projectDossierId && x.MijlpaalId == mijlpaalId);
        if (bestaat) return false;
        _db.ProjectDossierMijlpaal.Add(new ProjectDossierMijlpaal { ProjectDossierId = projectDossierId, MijlpaalId = mijlpaalId });

        // Zet ook Mijlpaal.DossierId zodat de Dossier-/DossierSubstap-binding het rechtstreeks kan lezen.
        var mijlpaal = await _db.Mijlpaal.FirstOrDefaultAsync(m => m.Id == mijlpaalId);
        if (mijlpaal != null)
        {
            if (mijlpaal.DossierId == null) mijlpaal.DossierId = projectDossierId;

            // Bij een vergunningsdossier: herkende Code -> automatisch op de bijhorende checklist-stap binden.
            if (mijlpaal.BronBinding == null && !string.IsNullOrWhiteSpace(mijlpaal.Code)
                && VergunningMijlpaalNaarSubstap.TryGetValue(mijlpaal.Code, out var substapCode))
            {
                var dossier = await _db.ProjectDossier.AsNoTracking().Where(d => d.Id == projectDossierId).Select(d => d.DossierKind).FirstOrDefaultAsync();
                if (dossier == (int)DossierKind.Omgevingsvergunning)
                {
                    mijlpaal.BronBinding = (int)ComputedBinding.DossierSubstap;
                    mijlpaal.BronParam = substapCode;
                }
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public Task<List<ProjectDossierSubstap>> GetSubstappen(int projectDossierId) =>
        _db.ProjectDossierSubstap.Where(s => s.ProjectDossierId == projectDossierId)
            .OrderBy(s => s.Volgorde).ToListAsync();

    public async Task<bool> ChangeSubstapStatus(int projectDossierId, int substapId, int status, DateOnly? datum, string? userId)
    {
        var stap = await _db.ProjectDossierSubstap.FirstOrDefaultAsync(s => s.ProjectDossierId == projectDossierId && s.Id == substapId);
        if (stap == null) return false;

        stap.Status = status;
        if (status == (int)DossierSubstapStatus.Afgerond)
            stap.Datum = datum ?? stap.Datum ?? DateOnly.FromDateTime(DateTime.Today);
        stap.ModifiedByUserId = userId;
        stap.ModifiedDate = DateTime.UtcNow;

        _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
        {
            ProjectDossierId = projectDossierId,
            Type = (int)DossierGebeurtenisType.StatusWijziging,
            Titel = $"Stap '{stap.Naam}': {(DossierSubstapStatus)status}",
            UserId = userId,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnlinkMijlpaal(int projectDossierId, int mijlpaalId)
    {
        var link = await _db.ProjectDossierMijlpaal.FirstOrDefaultAsync(x => x.ProjectDossierId == projectDossierId && x.MijlpaalId == mijlpaalId);
        if (link == null) return false;
        _db.ProjectDossierMijlpaal.Remove(link);

        var mijlpaal = await _db.Mijlpaal.FirstOrDefaultAsync(m => m.Id == mijlpaalId && m.DossierId == projectDossierId);
        if (mijlpaal != null) mijlpaal.DossierId = null;

        await _db.SaveChangesAsync();
        return true;
    }

    public Task<int> CountOpen(IEnumerable<int> projectIds) =>
        _db.ProjectDossier.CountAsync(d => projectIds.Contains(d.ProjectId)
            && d.Status != StatusAfgehandeld && d.Status != StatusGeannuleerd);

    private static void Apply(ProjectDossier entity, DossierUpsertBO dto)
    {
        entity.UnitId = dto.UnitId;
        entity.DossierKind = dto.DossierKind;
        entity.Titel = (dto.Titel ?? string.Empty).Trim();
        entity.Referentie = dto.Referentie;
        entity.Status = dto.Status;
        entity.VerantwoordelijkePartijType = dto.VerantwoordelijkePartijType;
        entity.VerantwoordelijkePartijId = dto.VerantwoordelijkePartijId;
        entity.VerantwoordelijkeUserId = dto.VerantwoordelijkeUserId;
        entity.ExterneContactNaam = dto.ExterneContactNaam;
        entity.ExterneContactEmail = dto.ExterneContactEmail;
        entity.AanvraagDatum = dto.AanvraagDatum;
        entity.VerwachteAfhandelingDatum = dto.VerwachteAfhandelingDatum;
        entity.AfgehandeldDatum = dto.AfgehandeldDatum;
        entity.Bedrag = dto.Bedrag;
        entity.Omschrijving = dto.Omschrijving;
    }

    private async Task LogStatusWijziging(int dossierId, int oud, int nieuw, string? userId, string? opmerking = null)
    {
        _db.ProjectDossierGebeurtenis.Add(new ProjectDossierGebeurtenis
        {
            ProjectDossierId = dossierId,
            Type = (int)DossierGebeurtenisType.StatusWijziging,
            Titel = $"Status: {(DossierStatus)oud} -> {(DossierStatus)nieuw}",
            Tekst = opmerking,
            UserId = userId,
            Datum = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
