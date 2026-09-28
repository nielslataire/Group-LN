using BOCore.Budget;
using DALCore.Models;
using FacadeCore;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Helpers;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services;

/// <summary>
/// Brug tussen de budgetwizard (CPM-database) en de marktdata (MarketData-database):
/// project → postcode → GeoMunicipality → alle nieuwbouw-units (project-units + losse panden)
/// van type appartement/woning in die gemeente, met laatste vraagprijs, verkoopstatus en doorlooptijd.
/// Zelfde status- en datumregels als Gemeenteanalyse (SaleStateHelpers).
/// </summary>
public class MarktReferentieService : IMarktReferentieService
{
    private readonly cpmRunningContext _cpm;
    private readonly MarketDataDbContext _md;

    public MarktReferentieService(cpmRunningContext cpm, MarketDataDbContext md)
    {
        _cpm = cpm;
        _md  = md;
    }

    public async Task<MarktReferentieBO> HaalOpAsync(int projectId, int periodeMaanden = 12)
    {
        var bo = new MarktReferentieBO { PeriodeMaanden = periodeMaanden };

        var project = await _cpm.Project.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => new { p.ProjectId, Postcode = p.PostalCode != null ? p.PostalCode.Postcode : null, Gemeente = p.PostalCode != null ? p.PostalCode.Gemeente : null })
            .FirstOrDefaultAsync();

        if (project is null || string.IsNullOrWhiteSpace(project.Postcode))
        {
            bo.Bron = "Het project heeft geen postcode; vul die in bij de projectgegevens om marktcijfers te tonen.";
            return bo;
        }

        bo.Postcode     = project.Postcode.Trim();
        bo.GemeenteNaam = project.Gemeente;

        // ── Postcode → gemeente in de geo-tabellen van de marktdata ──────────
        var sectie = await _md.GeoMunicipalSections.AsNoTracking()
            .Where(s => s.ZipCode == bo.Postcode)
            .Select(s => new { s.NisCodeMunicipality, s.NameDutch })
            .FirstOrDefaultAsync();

        int? muniId = null;
        if (sectie?.NisCodeMunicipality is { Length: > 0 } nis)
        {
            var muni = await _md.GeoMunicipalities.AsNoTracking()
                .Where(m => m.NisCode == nis)
                .Select(m => new { m.Id, m.NameDutch })
                .FirstOrDefaultAsync();
            if (muni is not null)
            {
                muniId = muni.Id;
                bo.GeoMunicipalityId = muni.Id;
                bo.GemeenteNaam = muni.NameDutch ?? bo.GemeenteNaam;
            }
        }

        var cutoff = periodeMaanden > 0 ? DateTime.UtcNow.AddMonths(-periodeMaanden) : new DateTime(1900, 1, 1);
        var zip    = bo.Postcode;

        // ── Projectgroepen in de gemeente (actief of niet; de units beslissen) ─
        var parentQuery = _md.MarketAssets.AsNoTracking().Where(a => a.IsProjectGroup);
        parentQuery = muniId.HasValue
            ? parentQuery.Where(a => a.GeoMunicipalityId == muniId.Value || (a.GeoMunicipalityId == null && a.PostalCode == zip))
            : parentQuery.Where(a => a.PostalCode == zip);
        var parentIds = await parentQuery.Select(a => a.Id).ToListAsync();

        // ── Units: project-units + losse panden, enkel appartement/woning ────
        var unitQuery = _md.MarketAssets.AsNoTracking()
            .Where(a => !a.IsProjectGroup
                     && (a.PropertyType == PropertyType.Apartment || a.PropertyType == PropertyType.House)
                     && ((a.ParentMarketAssetId.HasValue && parentIds.Contains(a.ParentMarketAssetId.Value))
                         || (a.ParentMarketAssetId == null
                             && (muniId.HasValue
                                 ? (a.GeoMunicipalityId == muniId.Value || (a.GeoMunicipalityId == null && a.PostalCode == zip))
                                 : a.PostalCode == zip))));

        var ruw = await unitQuery
            .Select(a => new
            {
                a.Id, a.PropertyType, a.LivingArea, a.SaleStatus, a.LifecycleStatus, a.IsActive,
                a.FirstSeenAt, a.FirstSoldAt, a.LifecycleStatusUpdatedAt, a.StatusChangedAt, a.ParentMarketAssetId
            })
            .ToListAsync();

        if (ruw.Count == 0)
        {
            bo.Bron = $"Geen nieuwbouw-units in de marktdata voor {bo.GemeenteNaam ?? zip} ({zip}).";
            return bo;
        }

        // Laatste gekende vraagprijs per unit (blijft bewaard na verkoop)
        var ids = ruw.Select(r => r.Id).ToList();
        var prijzen = await _md.MarketListings.AsNoTracking()
            .Where(l => ids.Contains(l.MarketAssetId) && l.AskingPrice != null && l.AskingPrice > 0)
            .GroupBy(l => l.MarketAssetId)
            .Select(g => new { AssetId = g.Key, Prijs = g.Max(l => l.AskingPrice) })
            .ToDictionaryAsync(x => x.AssetId, x => x.Prijs);

        foreach (var r in ruw)
        {
            var verkocht    = SaleStateHelpers.IsSold(r.SaleStatus, r.LifecycleStatus);
            var verkochtOp  = SaleStateHelpers.VerkoopDatum(r.SaleStatus, r.LifecycleStatus, r.FirstSoldAt, r.LifecycleStatusUpdatedAt, r.StatusChangedAt);

            // Verkocht buiten de periode telt niet mee; niet-verkocht enkel als het nog online staat.
            if (verkocht && (!verkochtOp.HasValue || verkochtOp.Value < cutoff)) continue;
            if (!verkocht && !r.IsActive) continue;
            if (!verkocht && SaleStateHelpers.IsReserved(r.SaleStatus, r.LifecycleStatus)) continue;

            prijzen.TryGetValue(r.Id, out var prijs);
            var ppm2 = prijs.HasValue && r.LivingArea is > 0 ? Math.Round(prijs.Value / r.LivingArea.Value, 0) : (decimal?)null;

            bo.Units.Add(new MarktReferentieUnitBO
            {
                PropertyType      = r.PropertyType == PropertyType.Apartment ? "Appartement" : "Woning",
                LivingArea        = r.LivingArea,
                Prijs             = prijs,
                PrijsPerM2        = ppm2,
                IsVerkocht        = verkocht,
                VerkochtOp        = verkochtOp,
                DoorlooptijdDagen = SaleStateHelpers.DoorlooptijdDagen(r.FirstSeenAt, verkochtOp),
                IsProjectUnit     = r.ParentMarketAssetId.HasValue
            });
        }

        var eigen = await VoegEigenVerkopenToeAsync(bo, zip);

        bo.Bron = muniId.HasValue
            ? $"Marktdata gemeente {bo.GemeenteNaam} (alle deelgemeenten), {bo.Units.Count - eigen} units"
            : $"Marktdata postcode {zip} (gemeente niet in geo-tabellen gevonden), {bo.Units.Count - eigen} units";
        if (eigen > 0) bo.Bron += $" + {eigen} eigen verkopen";
        return bo;
    }

    /// <summary>
    /// Eigen verkochte Units (CPM) van projecten in dezelfde postcode: de enige bron met werkelijke
    /// verkoopprijzen (grond + basisbouwwaarde zoals verkocht). Ze tellen mee in de "verkocht"-pool
    /// van de marktreferentie; een verkoopdatum is op Units niet bekend, dus geen periodefilter.
    /// </summary>
    private async Task<int> VoegEigenVerkopenToeAsync(MarktReferentieBO bo, string zip)
    {
        var rijen = await _cpm.Units.AsNoTracking()
            .Where(u => u.Project.PostalCode != null && u.Project.PostalCode.Postcode == zip
                     && u.ClientAccountId != null && !u.IsLink && u.Surface > 0)
            .Select(u => new
            {
                u.Id,
                TypeName  = u.Type != null ? u.Type.Name : null,
                GroupName = u.Type != null && u.Type.Group != null ? u.Type.Group.Name : null,
                u.Surface,
                u.LandValueSold,
                u.LandValue,
                u.ConstructionValueSold,
                BasisSold = u.UnitConstructionValue.Where(cv => cv.FinishingOptionId == null).Sum(cv => cv.ValueSold),
                Basis     = u.UnitConstructionValue.Where(cv => cv.FinishingOptionId == null).Sum(cv => cv.Value)
            })
            .ToListAsync();

        var toegevoegd = 0;
        foreach (var r in rijen)
        {
            var type = ServiceCore.Budget.VerkoopVoorstelService.ClassificeerType(r.TypeName, r.GroupName);
            if (type is null) continue;

            var grond = r.LandValueSold ?? r.LandValue ?? 0m;
            var bouw  = r.ConstructionValueSold ?? (r.BasisSold is > 0 ? r.BasisSold.Value : r.Basis ?? 0m);
            var prijs = grond + bouw;
            if (prijs <= 0m || !r.Surface.HasValue || r.Surface.Value <= 0m) continue;

            bo.Units.Add(new MarktReferentieUnitBO
            {
                PropertyType   = type,
                LivingArea     = r.Surface,
                Prijs          = prijs,
                PrijsPerM2     = Math.Round(prijs / r.Surface.Value, 0),
                IsVerkocht     = true,
                IsEigenVerkoop = true,
                IsProjectUnit  = true
            });
            toegevoegd++;
        }
        return toegevoegd;
    }
}
