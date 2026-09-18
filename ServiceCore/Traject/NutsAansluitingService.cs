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
            ExterneContactNaam = dto.ExterneContactNaam,
            ExterneContactEmail = dto.ExterneContactEmail,
            // Gespiegeld vanuit AanvraagVerstuurdOp (het enige "aanvraagdatum"-veld dat het
            // formulier nog toont) i.p.v. een apart dto.AanvraagDatum-veld — anders blijft deze
            // generieke kolom (gebruikt door de Alle-dossiers-tabel op Index.cshtml) leeg.
            AanvraagDatum = dto.AanvraagVerstuurdOp,
            VerwachteAfhandelingDatum = dto.VerwachteAfhandelingDatum,
            Bedrag = dto.Bedrag,
            Omschrijving = dto.Omschrijving,
            CreatedByUserId = userId,
            CreatedDate = DateTime.UtcNow
        };
        var nuts = new ProjectNutsAansluiting();
        ApplyNuts(nuts, dto);
        dossier.NutsAansluiting = nuts;
        // Status/AfgehandeldDatum volgen uit de werkstroomdatums i.p.v. een apart handmatig veld —
        // zie NutsChecklistSpiegel.BerekenStatus (gedeeld met ProjectDossierService.ChangeSubstapStatus,
        // anders herhaalt de checklist-knop dezelfde one-way-sync bug als de datums zelf hadden).
        dossier.Status = NutsChecklistSpiegel.BerekenStatus(nuts, dto.Geannuleerd);
        dossier.AfgehandeldDatum = nuts.UitgevoerdOp;

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

        await MirrorNaarChecklist(dossier.Id, nuts);

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
                ExterneContactNaam = dto.ExterneContactNaam,
                ExterneContactEmail = dto.ExterneContactEmail,
                // Create() spiegelt AanvraagDatum vanuit AanvraagVerstuurdOp (zie toelichting
                // daar) — dto.AanvraagDatum hier zou anders stil genegeerd worden.
                AanvraagVerstuurdOp = dto.AanvraagDatum,
                VerwachteAfhandelingDatum = dto.VerwachteAfhandelingDatum,
                Omschrijving = dto.Omschrijving,
                NutsType = dto.NutsType,
                NetbeheerderCompanyId = dto.NetbeheerderCompanyId,
                GevraagdVermogen = dto.GevraagdVermogen,
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
        dossier.ExterneContactNaam = dto.ExterneContactNaam;
        dossier.ExterneContactEmail = dto.ExterneContactEmail;
        dossier.AanvraagDatum = dto.AanvraagVerstuurdOp; // zie toelichting in Create()
        dossier.VerwachteAfhandelingDatum = dto.VerwachteAfhandelingDatum;
        dossier.Bedrag = dto.Bedrag;
        dossier.Omschrijving = dto.Omschrijving;
        dossier.ModifiedByUserId = userId;
        dossier.ModifiedDate = DateTime.UtcNow;

        ApplyNuts(dossier.NutsAansluiting, dto);
        // Status/AfgehandeldDatum volgen uit de werkstroomdatums — zie toelichting in Create()/
        // NutsChecklistSpiegel.BerekenStatus.
        dossier.Status = NutsChecklistSpiegel.BerekenStatus(dossier.NutsAansluiting, dto.Geannuleerd);
        dossier.AfgehandeldDatum = dossier.NutsAansluiting.UitgevoerdOp;

        await _db.SaveChangesAsync();
        await MirrorNaarChecklist(dossier.Id, dossier.NutsAansluiting);

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
        nuts.VerwachteOfferteDatum = dto.VerwachteOfferteDatum;
        nuts.OfferteOntvangenOp = dto.OfferteOntvangenOp;
        nuts.OfferteGoedgekeurdOp = dto.OfferteGoedgekeurdOp;
        nuts.UitvoeringGevraagdOp = dto.UitvoeringGevraagdOp;
        nuts.UitgevoerdOp = dto.UitgevoerdOp;
        nuts.AansluitkostRaming = dto.AansluitkostRaming;
        nuts.AansluitkostDefinitief = dto.AansluitkostDefinitief;
    }

    /// <summary>
    /// Spiegelt de getypeerde werkstroomdatums (rechtstreeks bewerkbaar op het formulier) naar de
    /// bijhorende ProjectDossierSubstap-rijen, zodat de generieke trajectbindingen (DossierSubstap,
    /// AlleGekoppeldeDossierSubstappen, AlleNutsaanvragenSubstap) blijven werken ongeacht of de
    /// gebruiker dit formulier gebruikt of de checklist-knoppen op de dossierpagina. Enkel
    /// vooruit-of-terug op basis van "is de datum ingevuld" — een stap die de gebruiker handmatig op
    /// "Bezig" zette (nog geen datum) blijft onaangeroerd.
    /// </summary>
    private async Task MirrorNaarChecklist(int dossierId, ProjectNutsAansluiting nuts)
    {
        var stappen = await _db.ProjectDossierSubstap
            .Where(s => s.ProjectDossierId == dossierId).ToListAsync();
        if (stappen.Count == 0) return;

        bool gewijzigd = false;
        foreach (var (code, datumVan, _) in NutsChecklistSpiegel.Velden)
        {
            var stap = stappen.FirstOrDefault(s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
            if (stap == null) continue;

            var datum = datumVan(nuts);
            if (datum.HasValue && (stap.Status != (int)DossierSubstapStatus.Afgerond || stap.Datum != datum))
            {
                stap.Status = (int)DossierSubstapStatus.Afgerond;
                stap.Datum = datum;
                stap.ModifiedDate = DateTime.UtcNow;
                gewijzigd = true;
            }
            else if (!datum.HasValue && stap.Status == (int)DossierSubstapStatus.Afgerond)
            {
                stap.Status = (int)DossierSubstapStatus.NietGestart;
                stap.Datum = null;
                stap.ModifiedDate = DateTime.UtcNow;
                gewijzigd = true;
            }
        }

        if (gewijzigd) await _db.SaveChangesAsync();
    }
}
