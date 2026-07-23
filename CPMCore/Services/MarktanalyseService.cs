using CPMCore.Models.Marktanalyse;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Helpers;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services;

public class MarktanalyseService : IMarktanalyseService
{
    private readonly MarketDataDbContext _db;

    public MarktanalyseService(MarketDataDbContext db)
    {
        _db = db;
    }

    private sealed record UnitSnapshot(decimal? AskingPrice, decimal? PricePerSqm);

    /// <summary>
    /// Genormaliseerde unit-statistiek voor KPI/tabel/grafiek-berekeningen.
    /// Komt ofwel uit een gededupliceerde CanonicalUnit (IsFromCanonical=true),
    /// ofwel uit een ruwe bron-MarketAsset (fallback wanneer nog geen canonical
    /// data bestaat voor het project). Zo tellen Gemeenteanalyse en Projectdetail
    /// altijd dezelfde aantallen.
    /// </summary>
    private sealed record ProjectUnitStat(
        SaleStatus?           SaleStatus,
        AssetLifecycleStatus? LifecycleStatus,
        decimal?              Price,
        decimal?              PricePerSqm,
        decimal?              Area,
        PropertyType          PropertyType,
        bool                  IsFromCanonical);

    // ── Locaties (dropdown-bron) ───────────────────────────────────────────────

    public async Task<List<GemeenteGroep>> GetLocatiesAsync(CancellationToken ct = default)
    {
        // Haal alle GeoMunicipalSectionId's op waarvoor actieve assets bestaan
        var activeSectionIds = await _db.MarketAssets
            .AsNoTracking()
            .Where(a => a.IsActive && a.GeoMunicipalSectionId.HasValue)
            .Select(a => a.GeoMunicipalSectionId!.Value)
            .Distinct()
            .ToListAsync(ct);

        if (activeSectionIds.Count == 0)
            return [];

        // Laad de secties (alleen scalaire velden, geen Boundary)
        var sections = await _db.GeoMunicipalSections
            .Where(s => activeSectionIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.NameDutch,
                s.ZipCode,
                s.NisCodeMunicipality
            })
            .AsNoTracking()
            .ToListAsync(ct);

        // Laad de bijbehorende gemeenten
        var nisCodes = sections
            .Select(s => s.NisCodeMunicipality)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

        var municipalities = await _db.GeoMunicipalities
            .Where(m => nisCodes.Contains(m.NisCode))
            .Select(m => new { m.Id, m.NisCode, m.NameDutch })
            .AsNoTracking()
            .ToDictionaryAsync(m => m.NisCode, ct);

        // Groepeer per gemeente
        return sections
            .Where(s => s.NisCodeMunicipality != null && municipalities.ContainsKey(s.NisCodeMunicipality))
            .GroupBy(s => s.NisCodeMunicipality!)
            .Select(g =>
            {
                var muni = municipalities[g.Key];
                return new GemeenteGroep
                {
                    GeoMunicipalityId = muni.Id,
                    GemeenteNaam = muni.NameDutch ?? g.Key,
                    Secties = g
                        .Select(s => new LocatieOptie
                        {
                            GeoMunicipalSectionId = s.Id,
                            GeoMunicipalityId = muni.Id,
                            DeelgemeenteNaam = s.NameDutch ?? "",
                            ZipCode = s.ZipCode
                        })
                        .OrderBy(s => s.ZipCode)
                        .ThenBy(s => s.DeelgemeenteNaam)
                        .ToList()
                };
            })
            .OrderBy(g => g.GemeenteNaam)
            .ToList();
    }

    // ── Gemeenteanalyse ───────────────────────────────────────────────────────

    public async Task<GemeenteAnalyseViewModel> GetGemeenteAnalyseAsync(
        int? geoMunicipalityId,
        int? geoMunicipalSectionId,
        string type,
        string aanbodtype    = "Alles",
        bool toonGekoppeld   = false,
        CancellationToken ct = default)
    {
        var vm = new GemeenteAnalyseViewModel
        {
            GeselecteerdGeoMunicipalityId = geoMunicipalityId,
            GeselecteerdGeoMunicipalSectionId = geoMunicipalSectionId,
            GeselecteerdType = type,
            GeselecteerdAanbodtype = aanbodtype,
            ToonGekoppeld = toonGekoppeld
        };

        if (!geoMunicipalityId.HasValue && !geoMunicipalSectionId.HasValue)
            return vm;

        // Laad display-namen voor de geselecteerde locatie
        await VulDisplayNamenAsync(vm, ct);

        // Pre-laad fallback-postcodes voor assets die nog geen GeoMunicipalSectionId hebben
        var (fallbackZips, fallbackSectionCity) = await LaadGeoFallbackAsync(
            geoMunicipalityId, geoMunicipalSectionId, vm, ct);

        bool loadProjecten = aanbodtype != "Losse eenheden";
        bool loadLosseEenheden = aanbodtype != "Projecten";

        // ── Projectgroepen ophalen ─────────────────────────────────────────────
        List<MarketAsset> projecten = [];
        Dictionary<long, string> projectNamen = new();
        Dictionary<long, List<long>> unitIdsByPrimary = new();
        Dictionary<long, long?> canonicalProjectIdByPrimary = new();

        if (loadProjecten)
        {
            var projectQuery = _db.MarketAssets
                .Where(a => a.IsProjectGroup && a.IsActive);

            projectQuery = ToepassGeoFilter(projectQuery, geoMunicipalityId, geoMunicipalSectionId, fallbackZips, fallbackSectionCity);

            if (type == "Appartement")
                projectQuery = projectQuery.Where(a => a.PropertySubType == PropertySubType.ApartmentGroup);
            else if (type == "Woning")
                projectQuery = projectQuery.Where(a => a.PropertySubType == PropertySubType.HouseGroup);

            var alleProjecten = await projectQuery.AsNoTracking().ToListAsync(ct);

            if (alleProjecten.Count > 0)
            {
                var alleProjectIds = alleProjecten.Select(p => p.Id).ToList();

                var listingNamen = await _db.MarketListings
                    .Where(l => alleProjectIds.Contains(l.MarketAssetId) && l.Title != null)
                    .GroupBy(l => l.MarketAssetId)
                    .Select(g => new { AssetId = g.Key, Naam = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Title).First() })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.AssetId, x => x.Naam ?? "", ct);

                // Canonical groepen oplossen (vervangt GetDuplicaatProjectIdsAsync)
                var canonicalLinks = await _db.CanonicalProjectAssets
                    .Where(a => alleProjectIds.Contains(a.MarketAssetId))
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (canonicalLinks.Count > 0)
                {
                    var cpIds = canonicalLinks.Select(l => l.CanonicalProjectId).Distinct().ToList();
                    var canonicalNamen = await _db.CanonicalProjects
                        .Where(cp => cpIds.Contains(cp.Id) && cp.IsActive)
                        .AsNoTracking()
                        .ToDictionaryAsync(cp => cp.Id, cp => cp.CanonicalName, ct);

                    var canonicalGroepen = BuildCanonicalGroups(canonicalLinks, alleProjectIds, canonicalNamen);
                    projecten = canonicalGroepen
                        .Select(g => alleProjecten.First(p => p.Id == g.PrimaryAssetId))
                        .ToList();
                    projectNamen = canonicalGroepen.ToDictionary(
                        g => g.PrimaryAssetId,
                        g => listingNamen.GetValueOrDefault(g.PrimaryAssetId) is { Length: > 0 } ln
                             ? ln
                             : g.CanonicalName ?? "");
                    unitIdsByPrimary = canonicalGroepen.ToDictionary(
                        g => g.PrimaryAssetId,
                        g => g.AllAssetIds);
                    canonicalProjectIdByPrimary = canonicalGroepen.ToDictionary(
                        g => g.PrimaryAssetId,
                        g => g.CanonicalProjectId);
                }
                else
                {
                    // Fallback: origineel gedrag wanneer canonical data nog niet beschikbaar is
                    var dupIds = await GetDuplicaatProjectIdsAsync(alleProjectIds, ct);
                    projecten = alleProjecten.Where(p => !dupIds.Contains(p.Id)).ToList();
                    projectNamen = listingNamen;
                    unitIdsByPrimary = projecten.ToDictionary(p => p.Id, p => new List<long> { p.Id });
                    canonicalProjectIdByPrimary = projecten.ToDictionary(p => p.Id, p => (long?)null);
                }
            }
        }

        // ── Project units ophalen (over alle canonical siblings) ───────────────
        List<MarketAsset> projectUnits = [];
        if (projecten.Count > 0)
        {
            // Laad units voor alle assets in de canonical groepen (geaggregeerd over siblings)
            var allSiblingIds = unitIdsByPrimary.Values.SelectMany(ids => ids).Distinct().ToList();
            var unitQuery = _db.MarketAssets
                .Where(a => a.ParentMarketAssetId.HasValue && allSiblingIds.Contains(a.ParentMarketAssetId.Value));

            if (type == "Appartement")
                unitQuery = unitQuery.Where(a => a.PropertyType == PropertyType.Apartment);
            else if (type == "Woning")
                unitQuery = unitQuery.Where(a => a.PropertyType == PropertyType.House);

            projectUnits = await unitQuery.AsNoTracking().ToListAsync(ct);
        }

        // ── CanonicalUnits ophalen (gededupliceerde units, zelfde bron als Projectdetail) ─
        Dictionary<long, List<CanonicalUnit>> canonicalUnitsByCanonicalProjectId = new();
        if (projecten.Count > 0)
        {
            var canonicalProjectIdsForUnits = canonicalProjectIdByPrimary.Values
                .Where(v => v.HasValue).Select(v => v!.Value).Distinct().ToList();

            if (canonicalProjectIdsForUnits.Count > 0)
            {
                var cuQuery = _db.CanonicalUnits
                    .Where(cu => canonicalProjectIdsForUnits.Contains(cu.CanonicalProjectId));

                if (type == "Appartement")
                    cuQuery = cuQuery.Where(cu => cu.PropertyType == PropertyType.Apartment);
                else if (type == "Woning")
                    cuQuery = cuQuery.Where(cu => cu.PropertyType == PropertyType.House);

                var canonicalUnitsAll = await cuQuery.AsNoTracking().ToListAsync(ct);
                canonicalUnitsByCanonicalProjectId = canonicalUnitsAll
                    .GroupBy(cu => cu.CanonicalProjectId)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
        }

        // ── Losse eenheden ophalen ─────────────────────────────────────────────
        List<MarketAsset> losseEenheden = [];
        // Ongekoppelde losse eenheden — gebruikt voor alle KPI/grafiek-totalen zodat
        // een gekoppelde listing (al meegeteld via zijn CanonicalUnit in het project)
        // nooit dubbel telt, ongeacht de "toon gekoppeld"-schakelaar in de tabel.
        List<MarketAsset> losseEenhedenUniek = [];
        if (loadLosseEenheden)
        {
            var losseQuery = _db.MarketAssets
                .Where(a => !a.IsProjectGroup && a.ParentMarketAssetId == null && a.IsActive);

            losseQuery = ToepassGeoFilter(losseQuery, geoMunicipalityId, geoMunicipalSectionId, fallbackZips, fallbackSectionCity);

            if (type == "Appartement")
                losseQuery = losseQuery.Where(a => a.PropertyType == PropertyType.Apartment);
            else if (type == "Woning")
                losseQuery = losseQuery.Where(a => a.PropertyType == PropertyType.House);

            var alleLosse = await losseQuery.AsNoTracking().ToListAsync(ct);

            // Toon standaard enkel ongekoppelde losse eenheden
            vm.AantalGekoppeld = alleLosse.Count(a => a.LinkedCanonicalUnitId.HasValue);
            losseEenhedenUniek = alleLosse.Where(a => !a.LinkedCanonicalUnitId.HasValue).ToList();
            losseEenheden = toonGekoppeld ? alleLosse : losseEenhedenUniek;
        }

        if (projecten.Count == 0 && losseEenheden.Count == 0) return vm;

        // ── Officiële locatienamen voor projecten + losse eenheden ─────────────
        var officieleLokaties = await LaadOfficieleLokaties(projecten.Concat(losseEenheden), ct);

        // ── Prijssnapshots ophalen ─────────────────────────────────────────────
        var alleEenhedenIds = projectUnits.Select(u => u.Id)
            .Concat(losseEenheden.Select(u => u.Id))
            .ToList();

        var prijzenPerEenheid = new Dictionary<long, UnitSnapshot>();
        if (alleEenhedenIds.Count > 0)
        {
            var unitListings = await _db.MarketListings
                .Where(l => alleEenhedenIds.Contains(l.MarketAssetId) && l.IsActive)
                .Select(l => new { l.Id, l.MarketAssetId })
                .AsNoTracking()
                .ToListAsync(ct);

            var listingIds = unitListings.Select(l => l.Id).ToList();
            var listingIdToAssetId = unitListings.ToDictionary(l => l.Id, l => l.MarketAssetId);

            var allSnapshots = await _db.MarketListingSnapshots
                .Where(s => listingIds.Contains(s.MarketListingId))
                .Select(s => new { s.MarketListingId, s.AskingPrice, s.PricePerSqm, s.SnapshotDate })
                .AsNoTracking()
                .ToListAsync(ct);

            prijzenPerEenheid = allSnapshots
                .Where(s => listingIdToAssetId.ContainsKey(s.MarketListingId))
                .GroupBy(s => listingIdToAssetId[s.MarketListingId])
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var latest = g.OrderByDescending(s => s.SnapshotDate).First();
                        return new UnitSnapshot(latest.AskingPrice, latest.PricePerSqm);
                    });
        }

        // ── Unit-statistieken per project (canonical-first, ruw als fallback) ──
        // Elk project gebruikt óf zijn gededupliceerde CanonicalUnits, óf — als
        // die nog niet gebouwd zijn voor dit project — de ruwe bron-units over
        // alle canonical siblings. Dit is exact dezelfde voorrangsregel als
        // GetProjectDetailAsync, zodat de aantallen op beide pagina's overeenkomen.
        var unitStatsByProject = new Dictionary<long, List<ProjectUnitStat>>();
        foreach (var project in projecten)
        {
            var cpId = canonicalProjectIdByPrimary.GetValueOrDefault(project.Id);
            if (cpId.HasValue
                && canonicalUnitsByCanonicalProjectId.TryGetValue(cpId.Value, out var cus)
                && cus.Count > 0)
            {
                unitStatsByProject[project.Id] = cus
                    .Select(cu => new ProjectUnitStat(
                        cu.Status, null, cu.Price, cu.PricePerSqm, cu.Area, cu.PropertyType, IsFromCanonical: true))
                    .ToList();
            }
            else
            {
                var siblingIds = unitIdsByPrimary.TryGetValue(project.Id, out var ids)
                    ? ids : new List<long> { project.Id };

                unitStatsByProject[project.Id] = projectUnits
                    .Where(u => u.ParentMarketAssetId.HasValue && siblingIds.Contains(u.ParentMarketAssetId.Value))
                    .Select(u =>
                    {
                        prijzenPerEenheid.TryGetValue(u.Id, out var snap);
                        return new ProjectUnitStat(
                            u.SaleStatus, u.LifecycleStatus, snap?.AskingPrice, snap?.PricePerSqm,
                            u.LivingArea, u.PropertyType, IsFromCanonical: false);
                    })
                    .ToList();
            }
        }
        var allUnitStats = unitStatsByProject.Values.SelectMany(s => s).ToList();

        // Ongekoppelde losse eenheden meetellen als eigen (niet-canonical) units,
        // zodat de KPI-cards het werkelijke, unieke totaal tonen (projecten +
        // losse eenheden, zonder dubbeltelling van reeds gekoppelde listings).
        var looseUnitStats = losseEenhedenUniek
            .Select(e =>
            {
                prijzenPerEenheid.TryGetValue(e.Id, out var snap);
                return new ProjectUnitStat(
                    e.SaleStatus, e.LifecycleStatus, snap?.AskingPrice, snap?.PricePerSqm,
                    e.LivingArea, e.PropertyType, IsFromCanonical: false);
            })
            .ToList();
        var allCombinedStats = allUnitStats.Concat(looseUnitStats).ToList();

        // ── Losse eenheden: bron en URL ophalen ───────────────────────────────
        var losseEenhedenInfo = new Dictionary<long, (string Bron, string? SourceUrl)>();
        if (losseEenheden.Count > 0)
        {
            var losseIds = losseEenheden.Select(u => u.Id).ToList();
            var listings = await _db.MarketListings
                .Where(l => losseIds.Contains(l.MarketAssetId) && l.IsActive)
                .Select(l => new { l.MarketAssetId, SourceName = l.Source.Name, l.LastSeenAt, l.Url })
                .AsNoTracking()
                .ToListAsync(ct);

            losseEenhedenInfo = listings
                .GroupBy(l => l.MarketAssetId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var latest = g.OrderByDescending(l => l.LastSeenAt).First();
                        return (latest.SourceName ?? "-", string.IsNullOrEmpty(latest.Url) ? null : latest.Url);
                    });
        }

        // ── KPI berekening (canonical-first, projecten + losse eenheden samen) ──
        // Alle telkaarten gebruiken hetzelfde gecombineerde, unieke bestand
        // (allCombinedStats) zodat "Beschikbare units", "Verkocht" en de
        // gemiddelden overal exact optellen tot hetzelfde totaal.
        var soldCount      = allCombinedStats.Count(u => u.SaleStatus == SaleStatus.Sold);
        var availableCount = allCombinedStats.Count(u => u.SaleStatus == SaleStatus.Available);
        var reservedCount  = allCombinedStats.Count(u =>
            u.SaleStatus == SaleStatus.Reserved || u.SaleStatus == SaleStatus.Option);
        // CanonicalUnits bewaren geen SoldConfirmed/LikelySold-onderscheid (net als
        // Projectdetail): een verkochte canonical unit telt als "bevestigd verkocht".
        var soldConfirmedCount = allCombinedStats.Count(u =>
            u.LifecycleStatus == AssetLifecycleStatus.SoldConfirmed
            || (u.IsFromCanonical && u.SaleStatus == SaleStatus.Sold));
        var likelySoldCount = allCombinedStats.Count(u => u.LifecycleStatus == AssetLifecycleStatus.LikelySold);

        var combinedPrices = allCombinedStats.Where(u => u.Price.HasValue).Select(u => u.Price!.Value).ToList();
        var combinedPpSqm  = allCombinedStats.Where(u => u.PricePerSqm.HasValue).Select(u => u.PricePerSqm!.Value).ToList();

        var areas = allCombinedStats
            .Where(u => u.Area.HasValue && u.Area > 0)
            .Select(u => u.Area!.Value).ToList();

        vm.Kpi = new GemeenteKpiViewModel
        {
            ActieveProjecten     = projecten.Count,
            AantalProjectUnits   = allUnitStats.Count,
            AantalLosseEenheden  = losseEenhedenUniek.Count,
            ActieveUnits         = allCombinedStats.Count,
            VerkochteUnits       = soldCount,
            BeschikbareUnits     = availableCount,
            GereserveerdeUnits   = reservedCount,
            SoldConfirmedCount   = soldConfirmedCount,
            LikelySoldCount      = likelySoldCount,
            GemiddeldePrijs      = combinedPrices.Count > 0 ? Math.Round(combinedPrices.Average(), 0) : null,
            GemiddeldePrijsPerM2 = combinedPpSqm.Count > 0 ? Math.Round(combinedPpSqm.Average(), 0) : null,
            GemiddeldeOppervlakte = areas.Count > 0 ? Math.Round(areas.Average(), 0) : null,
            Verkoopgraad = allCombinedStats.Count > 0
                ? Math.Round((decimal)soldCount / allCombinedStats.Count * 100, 1)
                : 0m
        };

        vm.VraagprijsBuckets = BerekeningVraagprijsBuckets(combinedPrices);
        vm.PrijsPerM2Buckets = BerekeningPrijsPerM2Buckets(combinedPpSqm);

        if (projecten.Count > 0)
        {
            vm.VerkoopgraadPerProject = projecten
                .Select(p =>
                {
                    var stats = unitStatsByProject.TryGetValue(p.Id, out var s) ? s : [];
                    var sold  = stats.Count(u => u.SaleStatus == SaleStatus.Sold);
                    var total = stats.Count;
                    return new ProjectVerkoopgraadViewModel
                    {
                        ProjectNaam    = projectNamen.GetValueOrDefault(p.Id) ?? p.AssetKey,
                        Verkoopgraad   = total > 0 ? Math.Round((decimal)sold / total * 100, 1) : 0m,
                        VerkochteUnits = sold,
                        TotaalUnits    = total
                    };
                })
                .OrderByDescending(p => p.Verkoopgraad)
                .Take(10)
                .ToList();
        }

        // ── Bron-namen en developer-conflict per canonical groep ─────────────
        var bronnenInfoPerProject = new Dictionary<long, BronnenInfo>();
        if (projecten.Count > 0)
        {
            var allGroupAssetIds = unitIdsByPrimary.Values.SelectMany(ids => ids).Distinct().ToList();

            var sourceRows = await _db.MarketListings
                .Where(l => allGroupAssetIds.Contains(l.MarketAssetId) && l.IsActive)
                .Select(l => new { l.MarketAssetId, SourceName = l.Source.Name })
                .AsNoTracking()
                .ToListAsync(ct);

            var sourcesByAsset = sourceRows
                .GroupBy(x => x.MarketAssetId)
                .ToDictionary(g => g.Key,
                    g => g.Select(x => x.SourceName ?? "").Where(n => n != "").Distinct().ToList());

            // Developer names per asset voor conflict-detectie
            var devNameByAsset = await _db.MarketAssets
                .Where(a => allGroupAssetIds.Contains(a.Id))
                .Select(a => new { a.Id, a.DeveloperName })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.DeveloperName, ct);

            foreach (var p in projecten)
            {
                var siblingIds = unitIdsByPrimary.TryGetValue(p.Id, out var sids) ? sids : new List<long> { p.Id };

                var bronNamen = siblingIds
                    .SelectMany(id => sourcesByAsset.TryGetValue(id, out var names)
                        ? names : Enumerable.Empty<string>())
                    .Distinct().OrderBy(n => n).ToList();

                var uniqueDevNames = siblingIds
                    .Select(id => devNameByAsset.TryGetValue(id, out var dn) ? dn : null)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Distinct().ToList();

                bronnenInfoPerProject[p.Id] = new BronnenInfo(
                    siblingIds.Count, bronNamen, uniqueDevNames.Count > 1,
                    canonicalProjectIdByPrimary.GetValueOrDefault(p.Id));
            }
        }

        if (projecten.Count > 0)
            vm.Projecten = BouwProjectenTabel(projecten, unitStatsByProject, projectNamen, officieleLokaties, bronnenInfoPerProject);

        // ── Projectnaam laden voor gekoppelde losse eenheden ──────────────────
        Dictionary<long, string> gekoppeldProjectNaam = [];
        if (toonGekoppeld)
        {
            var linkedUnitIds = losseEenheden
                .Where(a => a.LinkedCanonicalUnitId.HasValue)
                .Select(a => a.LinkedCanonicalUnitId!.Value)
                .Distinct().ToList();

            if (linkedUnitIds.Count > 0)
            {
                gekoppeldProjectNaam = await _db.CanonicalUnits
                    .Where(cu => linkedUnitIds.Contains(cu.Id))
                    .Select(cu => new { cu.Id, cu.CanonicalProject.CanonicalName })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id, x => x.CanonicalName, ct);
            }
        }

        if (losseEenheden.Count > 0)
            vm.LosseEenheden = BouwLosseEenhedenTabel(losseEenheden, prijzenPerEenheid, losseEenhedenInfo, officieleLokaties, gekoppeldProjectNaam);

        return vm;
    }

    // ── Geo-filter helpers ────────────────────────────────────────────────────

    private async Task<(List<string> FallbackZips, string? SectionCity)> LaadGeoFallbackAsync(
        int? geoMunicipalityId,
        int? geoMunicipalSectionId,
        GemeenteAnalyseViewModel vm,
        CancellationToken ct)
    {
        if (geoMunicipalSectionId.HasValue)
        {
            // Sectie geselecteerd: fallback = exacte postcode + deelgemeentenaam
            var zip = vm.GeselecteerdePostcode;
            var city = vm.GeselecteerdeDeelgemeenteNaam;
            return (string.IsNullOrEmpty(zip) ? [] : [zip], city);
        }

        if (geoMunicipalityId.HasValue)
        {
            // Gemeente geselecteerd: fallback = alle postcodes in die gemeente
            var nisCode = await _db.GeoMunicipalities
                .Where(m => m.Id == geoMunicipalityId.Value)
                .Select(m => m.NisCode)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrEmpty(nisCode)) return ([], null);

            var zips = await _db.GeoMunicipalSections
                .Where(s => s.NisCodeMunicipality == nisCode && s.ZipCode != null)
                .Select(s => s.ZipCode!)
                .Distinct()
                .ToListAsync(ct);

            return (zips, null);
        }

        return ([], null);
    }

    private static IQueryable<MarketAsset> ToepassGeoFilter(
        IQueryable<MarketAsset> query,
        int? geoMunicipalityId,
        int? geoMunicipalSectionId,
        List<string> fallbackZips,
        string? fallbackSectionCity)
    {
        if (geoMunicipalSectionId.HasValue)
        {
            // Primair: exacte GeoMunicipalSectionId
            // Fallback: assets zonder sectie-ID maar met matchende postcode + stad
            if (fallbackZips.Count > 0 && !string.IsNullOrEmpty(fallbackSectionCity))
            {
                var zip  = fallbackZips[0];
                var city = fallbackSectionCity;
                return query.Where(a =>
                    a.GeoMunicipalSectionId == geoMunicipalSectionId.Value
                    || (a.GeoMunicipalSectionId == null
                        && a.PostalCode == zip
                        && EF.Functions.Like(a.City ?? "", city + "%")));
            }
            return query.Where(a => a.GeoMunicipalSectionId == geoMunicipalSectionId.Value);
        }

        if (geoMunicipalityId.HasValue)
        {
            // Primair: GeoMunicipalityId
            // Fallback: assets zonder gemeente-ID maar met postcode in de gemeente
            if (fallbackZips.Count > 0)
            {
                return query.Where(a =>
                    a.GeoMunicipalityId == geoMunicipalityId.Value
                    || (a.GeoMunicipalityId == null
                        && fallbackZips.Contains(a.PostalCode ?? "")));
            }
            return query.Where(a => a.GeoMunicipalityId == geoMunicipalityId.Value);
        }

        return query;
    }

    private async Task VulDisplayNamenAsync(GemeenteAnalyseViewModel vm, CancellationToken ct)
    {
        if (vm.GeselecteerdGeoMunicipalSectionId.HasValue)
        {
            var section = await _db.GeoMunicipalSections
                .Where(s => s.Id == vm.GeselecteerdGeoMunicipalSectionId.Value)
                .Select(s => new { s.NameDutch, s.ZipCode, s.NisCodeMunicipality })
                .FirstOrDefaultAsync(ct);

            if (section is not null)
            {
                vm.GeselecteerdeDeelgemeenteNaam = section.NameDutch;
                vm.GeselecteerdePostcode = section.ZipCode;

                if (!string.IsNullOrEmpty(section.NisCodeMunicipality))
                {
                    vm.GeselecteerdeGemeenteNaam = await _db.GeoMunicipalities
                        .Where(m => m.NisCode == section.NisCodeMunicipality)
                        .Select(m => m.NameDutch)
                        .FirstOrDefaultAsync(ct);
                }
            }
        }
        else if (vm.GeselecteerdGeoMunicipalityId.HasValue)
        {
            vm.GeselecteerdeGemeenteNaam = await _db.GeoMunicipalities
                .Where(m => m.Id == vm.GeselecteerdGeoMunicipalityId.Value)
                .Select(m => m.NameDutch)
                .FirstOrDefaultAsync(ct);
        }
    }

    // ── Officiële lokatie-lookup ──────────────────────────────────────────────

    private sealed record OfficieleLokatie(string? ZipCode, string? GemeenteNaam);

    private async Task<Dictionary<long, OfficieleLokatie>> LaadOfficieleLokaties(
        IEnumerable<MarketAsset> assets, CancellationToken ct)
    {
        var lijst = assets.ToList();

        var sectionIds = lijst
            .Where(a => a.GeoMunicipalSectionId.HasValue)
            .Select(a => a.GeoMunicipalSectionId!.Value)
            .Distinct().ToList();

        var municipalityIds = lijst
            .Where(a => !a.GeoMunicipalSectionId.HasValue && a.GeoMunicipalityId.HasValue)
            .Select(a => a.GeoMunicipalityId!.Value)
            .Distinct().ToList();

        var sections = sectionIds.Count > 0
            ? await _db.GeoMunicipalSections
                .Where(s => sectionIds.Contains(s.Id))
                .Select(s => new { s.Id, s.ZipCode, s.NameDutch })
                .AsNoTracking()
                .ToDictionaryAsync(s => s.Id, s => (s.ZipCode, s.NameDutch), ct)
            : new Dictionary<int, (string? ZipCode, string? NameDutch)>();

        var munis = municipalityIds.Count > 0
            ? await _db.GeoMunicipalities
                .Where(m => municipalityIds.Contains(m.Id))
                .Select(m => new { m.Id, m.NameDutch })
                .AsNoTracking()
                .ToDictionaryAsync(m => m.Id, m => m.NameDutch, ct)
            : new Dictionary<int, string?>();

        return lijst.ToDictionary(
            a => a.Id,
            a =>
            {
                if (a.GeoMunicipalSectionId.HasValue && sections.TryGetValue(a.GeoMunicipalSectionId.Value, out var sec))
                    return new OfficieleLokatie(sec.ZipCode ?? a.PostalCode, sec.NameDutch ?? a.City);
                if (a.GeoMunicipalityId.HasValue && munis.TryGetValue(a.GeoMunicipalityId.Value, out var muniNaam))
                    return new OfficieleLokatie(a.PostalCode, muniNaam ?? a.City);
                return new OfficieleLokatie(a.PostalCode, a.City);
            });
    }

    // ── Duplicaat-suppressie ──────────────────────────────────────────────────

    private async Task<HashSet<long>> GetDuplicaatProjectIdsAsync(List<long> projectIds, CancellationToken ct)
    {
        var ids = await _db.MarketAssetMatchCandidates
            .Where(c => c.MatchType == "Project"
                     && !c.IsRejected
                     && c.CandidateMarketAssetId.HasValue
                     && (c.MatchLevel == "Exact"
                         || (c.MatchLevel == "Probable" && c.MatchScore >= 0.80m))
                     && projectIds.Contains(c.CandidateMarketAssetId!.Value))
            .Select(c => c.CandidateMarketAssetId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    // ── Canonical groepen ─────────────────────────────────────────────────────

    private sealed record CanonicalGroupInfo(long PrimaryAssetId, long? CanonicalProjectId, string? CanonicalName, List<long> AllAssetIds);

    private sealed record BronnenInfo(int AantalBronnen, List<string> BronNamen, bool DeveloperConflict, long? CanonicalProjectId);

    private static List<CanonicalGroupInfo> BuildCanonicalGroups(
        List<CanonicalProjectAsset> links,
        List<long> knownAssetIds,
        Dictionary<long, string> canonicalNamen)
    {
        var result    = new List<CanonicalGroupInfo>();
        var handled   = new HashSet<long>();

        foreach (var cpGroup in links.GroupBy(l => l.CanonicalProjectId))
        {
            var groupIds = cpGroup
                .Select(l => l.MarketAssetId)
                .Where(knownAssetIds.Contains)
                .ToList();

            if (groupIds.Count == 0) continue;

            // Primary: IsIsPrimary === true én in onze geo-set, anders de eerste
            var primary = cpGroup.FirstOrDefault(l => l.IsPrimary && groupIds.Contains(l.MarketAssetId))
                          ?? cpGroup.First(l => groupIds.Contains(l.MarketAssetId));

            canonicalNamen.TryGetValue(cpGroup.Key, out var naam);
            result.Add(new CanonicalGroupInfo(primary.MarketAssetId, cpGroup.Key, naam, groupIds));
            foreach (var id in groupIds) handled.Add(id);
        }

        // Assets zonder canonical koppeling → singleton
        foreach (var id in knownAssetIds.Where(id => !handled.Contains(id)))
            result.Add(new CanonicalGroupInfo(id, null, null, [id]));

        return result;
    }

    // ── Bucket helpers ────────────────────────────────────────────────────────

    private static List<PrijsBucketViewModel> BerekeningVraagprijsBuckets(List<decimal> prices) =>
    [
        new() { Label = "< 250k",    Aantal = prices.Count(p => p < 250_000) },
        new() { Label = "250k–350k", Aantal = prices.Count(p => p >= 250_000 && p < 350_000) },
        new() { Label = "350k–450k", Aantal = prices.Count(p => p >= 350_000 && p < 450_000) },
        new() { Label = "450k–550k", Aantal = prices.Count(p => p >= 450_000 && p < 550_000) },
        new() { Label = "550k+",     Aantal = prices.Count(p => p >= 550_000) }
    ];

    private static List<PrijsBucketViewModel> BerekeningPrijsPerM2Buckets(List<decimal> ppSqms) =>
    [
        new() { Label = "< 2.500",     Aantal = ppSqms.Count(p => p < 2500) },
        new() { Label = "2.500–3.000", Aantal = ppSqms.Count(p => p >= 2500 && p < 3000) },
        new() { Label = "3.000–3.500", Aantal = ppSqms.Count(p => p >= 3000 && p < 3500) },
        new() { Label = "3.500–4.000", Aantal = ppSqms.Count(p => p >= 3500 && p < 4000) },
        new() { Label = "4.000–4.500", Aantal = ppSqms.Count(p => p >= 4000 && p < 4500) },
        new() { Label = "4.500+",      Aantal = ppSqms.Count(p => p >= 4500) }
    ];

    // ── Projectentabel ────────────────────────────────────────────────────────

    private static List<ProjectRijViewModel> BouwProjectenTabel(
        List<MarketAsset> projecten,
        Dictionary<long, List<ProjectUnitStat>> unitStatsByProject,
        Dictionary<long, string> namen,
        Dictionary<long, OfficieleLokatie> lokaties,
        Dictionary<long, BronnenInfo>? bronnenInfo = null)
    {
        return projecten
            .Select(project =>
            {
                lokaties.TryGetValue(project.Id, out var lok);
                BronnenInfo? bron = null;
                bronnenInfo?.TryGetValue(project.Id, out bron);

                var stats = unitStatsByProject.TryGetValue(project.Id, out var s) ? s : [];

                var soldCount      = stats.Count(u => u.SaleStatus == SaleStatus.Sold);
                var availableCount = stats.Count(u => u.SaleStatus == SaleStatus.Available);

                var unitPrices = stats.Where(u => u.Price.HasValue).Select(u => u.Price!.Value).ToList();
                var unitPpSqm  = stats.Where(u => u.PricePerSqm.HasValue).Select(u => u.PricePerSqm!.Value).ToList();

                var apartCount = stats.Count(u => u.PropertyType == PropertyType.Apartment);
                var houseCount = stats.Count(u => u.PropertyType == PropertyType.House);
                var typeLabel  = (apartCount, houseCount) switch
                {
                    ( > 0, 0)   => "Appartement",
                    (0, > 0)    => "Woning",
                    ( > 0, > 0) => "Gemengd",
                    _           => "-"
                };

                var pct = stats.Count > 0
                    ? Math.Round((decimal)soldCount / stats.Count * 100, 1)
                    : 0m;

                return new ProjectRijViewModel
                {
                    Id               = project.Id,
                    ProjectNaam      = namen.GetValueOrDefault(project.Id) ?? project.AssetKey,
                    Ontwikkelaar     = project.DeveloperName ?? "-",
                    TypeLabel        = typeLabel,
                    TotaalUnits      = stats.Count,
                    VerkochteUnits   = soldCount,
                    BeschikbareUnits = availableCount,
                    Verkoopgraad     = pct,
                    GemiddeldePrijs      = unitPrices.Count > 0 ? Math.Round(unitPrices.Average(), 0) : null,
                    GemiddeldePrijsPerM2 = unitPpSqm.Count > 0  ? Math.Round(unitPpSqm.Average(), 0)  : null,
                    Straat      = project.Street,
                    Huisnummer  = project.HouseNumber,
                    Postcode    = lok?.ZipCode ?? project.PostalCode,
                    Gemeente    = lok?.GemeenteNaam ?? project.City,
                    // Canonical / bronnen
                    CanonicalProjectId = bron?.CanonicalProjectId,
                    AantalBronnen      = bron?.AantalBronnen ?? 1,
                    BronNamen          = bron?.BronNamen ?? new List<string>(),
                    DeveloperConflict  = bron?.DeveloperConflict ?? false
                };
            })
            .OrderByDescending(p => p.Verkoopgraad)
            .ToList();
    }

    // ── Losse eenheden tabel ──────────────────────────────────────────────────

    private static List<LosseEenheidRijViewModel> BouwLosseEenhedenTabel(
        List<MarketAsset> losseEenheden,
        Dictionary<long, UnitSnapshot> prijzenPerEenheid,
        Dictionary<long, (string Bron, string? SourceUrl)> info,
        Dictionary<long, OfficieleLokatie> lokaties,
        Dictionary<long, string> gekoppeldProjectNaam)
    {
        return losseEenheden
            .Select(e =>
            {
                var (bron, sourceUrl) = info.TryGetValue(e.Id, out var i) ? i : ("-", null);
                lokaties.TryGetValue(e.Id, out var lok);
                prijzenPerEenheid.TryGetValue(e.Id, out var snapshot);

                var typeLabel = e.PropertyType switch
                {
                    PropertyType.Apartment => "Appartement",
                    PropertyType.House     => "Woning",
                    _                      => e.PropertyType.ToString()
                };

                var statusLabel = e.SaleStatus == SaleStatus.Sold || e.LifecycleStatus == AssetLifecycleStatus.SoldConfirmed
                    ? "Verkocht"
                    : e.LifecycleStatus == AssetLifecycleStatus.LikelySold
                        ? "Vermoedelijk verkocht"
                        : "Beschikbaar";
                var adresBase = string.Join(" ",
                    new[] { e.Street, e.HouseNumber }.Where(s => !string.IsNullOrWhiteSpace(s)));

                string? projectNaam = null;
                if (e.LinkedCanonicalUnitId.HasValue)
                    gekoppeldProjectNaam.TryGetValue(e.LinkedCanonicalUnitId.Value, out projectNaam);

                return new LosseEenheidRijViewModel
                {
                    Id                   = e.Id,
                    Adres                = string.IsNullOrWhiteSpace(adresBase) ? null : adresBase,
                    Postcode             = lok?.ZipCode ?? e.PostalCode,
                    Gemeente             = lok?.GemeenteNaam ?? e.City,
                    TypeLabel            = typeLabel,
                    Oppervlakte          = e.LivingArea,
                    Slaapkamers          = e.Bedrooms,
                    Vraagprijs           = snapshot?.AskingPrice,
                    PrijsPerM2           = snapshot?.PricePerSqm,
                    Status               = statusLabel,
                    AangeboenDoor        = bron,
                    SourceUrl            = sourceUrl,
                    LinkedCanonicalUnitId = e.LinkedCanonicalUnitId,
                    GekoppeldProjectNaam  = projectNaam
                };
            })
            .OrderBy(e => e.IsGekoppeld ? 1 : 0)
            .ThenBy(e => e.Adres ?? "")
            .ToList();
    }

    // ── Vergelijkbare panden ──────────────────────────────────────────────────

    public async Task<VergelijkbarePandenViewModel> GetVergelijkbarePandenAsync(
        List<int> gemeenteIds,
        string? rondAdresPostcode,
        double? rondAdresLat,
        double? rondAdresLng,
        int rondAdresStraal,
        string type,
        decimal? oppervlakte,
        int tolerantie,
        decimal? prijsMin,
        decimal? prijsMax,
        int? slaapkamers,
        string status,
        CancellationToken ct = default)
    {
        tolerantie = Math.Max(0, tolerantie);
        bool heeftPuntCoords = rondAdresLat.HasValue && rondAdresLng.HasValue;
        var vm = new VergelijkbarePandenViewModel
        {
            // Zodra een adres/punt is gekozen, blijft na een (automatische) zoekactie
            // het tabblad "Rond adres" actief in plaats van terug te vallen op
            // de standaard "Gemeenten"-tab.
            ZoekgebiedTab     = heeftPuntCoords || !string.IsNullOrWhiteSpace(rondAdresPostcode) ? "RondAdres" : "Gemeenten",
            GemeenteIds       = gemeenteIds,
            RondAdresPostcode = rondAdresPostcode,
            RondAdresLat      = rondAdresLat,
            RondAdresLng      = rondAdresLng,
            RondAdresStraal   = rondAdresStraal,
            Type              = type,
            Oppervlakte       = oppervlakte,
            Tolerantie        = tolerantie,
            PrijsMin          = prijsMin,
            PrijsMax          = prijsMax,
            Slaapkamers       = slaapkamers,
            Status            = status
        };

        vm.Locaties          = await GetLocatiesAsync(ct);
        vm.AantalPerGemeente = await BerekenAantalPerGemeenteAsync(ct);

        bool useRondAdres = heeftPuntCoords || !string.IsNullOrWhiteSpace(rondAdresPostcode);
        vm.HeeftZoekparameters = oppervlakte.HasValue || gemeenteIds.Count > 0 || useRondAdres
                              || prijsMin.HasValue || prijsMax.HasValue || slaapkamers.HasValue
                              || status != "Alles";
        if (!vm.HeeftZoekparameters) return vm;

        // ── Geo-center voor "Rond adres" ──────────────────────────────────────
        double centerLat = 0, centerLng = 0;
        bool heeftGeoCenter = false;
        decimal minLat = 0, maxLat = 0, minLng = 0, maxLng = 0;

        if (heeftPuntCoords)
        {
            // Middelpunt kwam rechtstreeks van een geocodeerd adres of van slepen op de kaart.
            centerLat = rondAdresLat!.Value;
            centerLng = rondAdresLng!.Value;
            heeftGeoCenter = true;
        }
        else if (useRondAdres)
        {
            // Legacy fallback (bv. oude gebookmarkte links zonder coördinaten):
            // middelpunt afleiden uit reeds gegeocodeerde units met dezelfde postcode.
            var refCoords = await _db.MarketAssets
                .Where(a => a.PostalCode == rondAdresPostcode && a.Latitude.HasValue && a.Longitude.HasValue)
                .Select(a => new { a.Latitude, a.Longitude })
                .Take(50)
                .AsNoTracking()
                .ToListAsync(ct);

            if (refCoords.Count > 0)
            {
                centerLat = (double)refCoords.Average(a => a.Latitude!.Value);
                centerLng = (double)refCoords.Average(a => a.Longitude!.Value);
                heeftGeoCenter = true;
            }
        }

        if (heeftGeoCenter)
        {
            var straalKm = rondAdresStraal / 1000.0;
            var dLat = (decimal)(straalKm / 111.0);
            var dLng = (decimal)(straalKm / (111.0 * Math.Cos(centerLat * Math.PI / 180)));
            minLat = (decimal)centerLat - dLat;
            maxLat = (decimal)centerLat + dLat;
            minLng = (decimal)centerLng - dLng;
            maxLng = (decimal)centerLng + dLng;
        }

        decimal oppMin = oppervlakte.HasValue ? oppervlakte.Value - tolerantie : 0m;
        decimal oppMax = oppervlakte.HasValue ? oppervlakte.Value + tolerantie : decimal.MaxValue;

        // ── Project groups ophalen ────────────────────────────────────────────
        var pgQuery = _db.MarketAssets.Where(a => a.IsProjectGroup && a.IsActive);
        pgQuery = useRondAdres
            ? heeftGeoCenter
                ? pgQuery.Where(a => a.Latitude.HasValue && a.Longitude.HasValue
                                  && a.Latitude >= minLat && a.Latitude <= maxLat
                                  && a.Longitude >= minLng && a.Longitude <= maxLng)
                : pgQuery.Where(a => a.PostalCode == rondAdresPostcode)
            : gemeenteIds.Count > 0
                ? pgQuery.Where(a => a.GeoMunicipalityId.HasValue && gemeenteIds.Contains(a.GeoMunicipalityId.Value))
                : pgQuery;

        if (type == "Appartement") pgQuery = pgQuery.Where(a => a.PropertySubType == PropertySubType.ApartmentGroup);
        else if (type == "Woning") pgQuery = pgQuery.Where(a => a.PropertySubType == PropertySubType.HouseGroup);

        var projectGroups    = await pgQuery.AsNoTracking().ToListAsync(ct);
        var projectGroupIds  = projectGroups.Select(p => p.Id).ToList();
        var alleRijen        = new List<VergelijkbaarPandRij>();

        // ── Project child units ───────────────────────────────────────────────
        if (projectGroupIds.Count > 0)
        {
            var uq = _db.MarketAssets
                .Where(a => a.ParentMarketAssetId.HasValue && projectGroupIds.Contains(a.ParentMarketAssetId.Value));

            if (type == "Appartement") uq = uq.Where(a => a.PropertyType == PropertyType.Apartment);
            else if (type == "Woning") uq = uq.Where(a => a.PropertyType == PropertyType.House);
            if (oppervlakte.HasValue) uq = uq.Where(a => a.LivingArea.HasValue && a.LivingArea >= oppMin && a.LivingArea <= oppMax);
            if (slaapkamers.HasValue) uq = uq.Where(a => a.Bedrooms == slaapkamers.Value);
            if (status == "Beschikbaar") uq = uq.Where(a => a.SaleStatus == SaleStatus.Available || a.SaleStatus == null);
            else if (status == "Verkocht") uq = uq.Where(a => a.SaleStatus == SaleStatus.Sold);

            var projectUnits = await uq.AsNoTracking().ToListAsync(ct);

            // Exacte afstandsfilter post-query voor "Rond adres"
            if (heeftGeoCenter && projectUnits.Count > 0)
            {
                var parentById2 = projectGroups.ToDictionary(p => p.Id);
                projectUnits = projectUnits
                    .Where(u => u.ParentMarketAssetId.HasValue
                             && parentById2.TryGetValue(u.ParentMarketAssetId.Value, out var par)
                             && par.Latitude.HasValue && par.Longitude.HasValue
                             && VpGeoDistance(centerLat, centerLng, (double)par.Latitude.Value, (double)par.Longitude.Value) <= rondAdresStraal)
                    .ToList();
            }

            if (projectUnits.Count > 0)
            {
                var unitIds = projectUnits.Select(u => u.Id).ToList();
                var unitPrices = await _db.MarketListings
                    .Where(l => unitIds.Contains(l.MarketAssetId) && l.IsActive)
                    .GroupBy(l => l.MarketAssetId)
                    .Select(g => new { AssetId = g.Key, Price = g.Max(l => l.AskingPrice) })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.AssetId, x => x.Price, ct);

                var kpiMap = await BerekenProjectVerkoopgraadAsync(projectGroupIds, ct);

                var listingNamen = await _db.MarketListings
                    .Where(l => projectGroupIds.Contains(l.MarketAssetId) && l.Title != null)
                    .GroupBy(l => l.MarketAssetId)
                    .Select(g => new { Id = g.Key, Naam = g.OrderByDescending(l => l.LastSeenAt).First().Title })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id, x => x.Naam ?? "", ct);

                var canonicalLinks = await _db.CanonicalProjectAssets
                    .Where(a => projectGroupIds.Contains(a.MarketAssetId))
                    .AsNoTracking()
                    .ToListAsync(ct);

                var cpNamen = new Dictionary<long, string>();
                var cpIdMap = new Dictionary<long, long>();
                if (canonicalLinks.Count > 0)
                {
                    var cpIds = canonicalLinks.Select(l => l.CanonicalProjectId).Distinct().ToList();
                    var cpData = await _db.CanonicalProjects
                        .Where(cp => cpIds.Contains(cp.Id) && cp.IsActive)
                        .AsNoTracking()
                        .ToDictionaryAsync(cp => cp.Id, cp => cp.CanonicalName ?? "", ct);
                    foreach (var lnk in canonicalLinks)
                        if (cpData.TryGetValue(lnk.CanonicalProjectId, out var n) && !string.IsNullOrEmpty(n))
                        {
                            cpNamen[lnk.MarketAssetId] = n;
                            cpIdMap[lnk.MarketAssetId] = lnk.CanonicalProjectId;
                        }
                }

                var parentLok = await LaadOfficieleLokaties(projectGroups, ct);
                var parentById = projectGroups.ToDictionary(p => p.Id);

                foreach (var unit in projectUnits)
                {
                    if (!unit.ParentMarketAssetId.HasValue) continue;
                    if (!parentById.TryGetValue(unit.ParentMarketAssetId.Value, out var parent)) continue;

                    unitPrices.TryGetValue(unit.Id, out var price);
                    if (prijsMin.HasValue && (price is null || price < prijsMin.Value)) continue;
                    if (prijsMax.HasValue && price.HasValue && price > prijsMax.Value) continue;

                    var ppSqm = price.HasValue && unit.LivingArea is > 0
                        ? Math.Round(price.Value / unit.LivingArea.Value, 0) : (decimal?)null;
                    parentLok.TryGetValue(parent.Id, out var lok);
                    kpiMap.TryGetValue(parent.Id, out var vkGraad);
                    var naam = listingNamen.GetValueOrDefault(parent.Id) is { Length: > 0 } lnVp
                               ? lnVp
                               : cpNamen.GetValueOrDefault(parent.Id) ?? parent.AssetKey;
                    cpIdMap.TryGetValue(parent.Id, out var cpId);

                    alleRijen.Add(new VergelijkbaarPandRij
                    {
                        AssetId            = unit.Id,
                        ProjectAssetId     = parent.Id,
                        CanonicalProjectId = cpId == 0 ? null : cpId,
                        ProjectNaam        = naam,
                        AdresRegel         = FormatAdres(parent.Street, parent.HouseNumber, lok?.ZipCode ?? parent.PostalCode, lok?.GemeenteNaam ?? parent.City),
                        TypeLabel          = MapTypeLabel(unit.PropertyType),
                        Oppervlakte        = unit.LivingArea,
                        Slaapkamers        = unit.Bedrooms,
                        Vraagprijs         = price,
                        PrijsPerM2         = ppSqm,
                        Status             = MapStatusLabel(unit.SaleStatus, unit.LifecycleStatus),
                        IsProject          = true,
                        Verkoopgraad       = vkGraad
                    });
                }
            }
        }

        // ── Losse listings ────────────────────────────────────────────────────
        var lq = _db.MarketAssets.Where(a => !a.IsProjectGroup && a.ParentMarketAssetId == null && a.IsActive);
        lq = useRondAdres
            ? heeftGeoCenter
                ? lq.Where(a => a.Latitude.HasValue && a.Longitude.HasValue
                             && a.Latitude >= minLat && a.Latitude <= maxLat
                             && a.Longitude >= minLng && a.Longitude <= maxLng)
                : lq.Where(a => a.PostalCode == rondAdresPostcode)
            : gemeenteIds.Count > 0
                ? lq.Where(a => a.GeoMunicipalityId.HasValue && gemeenteIds.Contains(a.GeoMunicipalityId.Value))
                : lq;

        if (type == "Appartement") lq = lq.Where(a => a.PropertyType == PropertyType.Apartment);
        else if (type == "Woning")  lq = lq.Where(a => a.PropertyType == PropertyType.House);
        if (oppervlakte.HasValue) lq = lq.Where(a => a.LivingArea.HasValue && a.LivingArea >= oppMin && a.LivingArea <= oppMax);
        if (slaapkamers.HasValue) lq = lq.Where(a => a.Bedrooms == slaapkamers.Value);
        if (status == "Beschikbaar") lq = lq.Where(a => a.SaleStatus == SaleStatus.Available || a.SaleStatus == null);
        else if (status == "Verkocht") lq = lq.Where(a => a.SaleStatus == SaleStatus.Sold);

        var losseEenheden = await lq.AsNoTracking().ToListAsync(ct);
        if (heeftGeoCenter && losseEenheden.Count > 0)
            losseEenheden = losseEenheden
                .Where(a => a.Latitude.HasValue && a.Longitude.HasValue
                         && VpGeoDistance(centerLat, centerLng, (double)a.Latitude.Value, (double)a.Longitude.Value) <= rondAdresStraal)
                .ToList();

        if (losseEenheden.Count > 0)
        {
            var losseIds = losseEenheden.Select(e => e.Id).ToList();
            var losseLst = await _db.MarketListings
                .Where(l => losseIds.Contains(l.MarketAssetId) && l.IsActive)
                .GroupBy(l => l.MarketAssetId)
                .Select(g => new
                {
                    AssetId = g.Key,
                    Price   = g.Max(l => l.AskingPrice),
                    Url     = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Url).FirstOrDefault(),
                    Title   = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Title).FirstOrDefault()
                })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.AssetId, ct);

            var losseLok = await LaadOfficieleLokaties(losseEenheden, ct);

            foreach (var e in losseEenheden)
            {
                losseLst.TryGetValue(e.Id, out var lst);
                var price = lst?.Price;
                if (prijsMin.HasValue && (price is null || price < prijsMin.Value)) continue;
                if (prijsMax.HasValue && price.HasValue && price > prijsMax.Value) continue;

                var ppSqm = price.HasValue && e.LivingArea is > 0
                    ? Math.Round(price.Value / e.LivingArea.Value, 0) : (decimal?)null;
                losseLok.TryGetValue(e.Id, out var lok);
                var adresRegel = FormatAdres(e.Street, e.HouseNumber, lok?.ZipCode ?? e.PostalCode, lok?.GemeenteNaam ?? e.City);
                var projectNaam = lst?.Title ?? adresRegel;

                alleRijen.Add(new VergelijkbaarPandRij
                {
                    AssetId    = e.Id,
                    ProjectNaam = projectNaam,
                    AdresRegel = adresRegel,
                    TypeLabel  = MapTypeLabel(e.PropertyType),
                    Oppervlakte = e.LivingArea,
                    Slaapkamers = e.Bedrooms,
                    Vraagprijs  = price,
                    PrijsPerM2  = ppSqm,
                    Status      = MapStatusLabel(e.SaleStatus, e.LifecycleStatus),
                    IsProject   = false,
                    SourceUrl   = lst?.Url
                });
            }
        }

        vm.Panden = alleRijen.OrderBy(p => p.PrijsPerM2 ?? decimal.MaxValue).ToList();

        // ── KPIs ──────────────────────────────────────────────────────────────
        if (vm.Panden.Count > 0)
        {
            var prijzen = vm.Panden.Where(p => p.Vraagprijs.HasValue).Select(p => p.Vraagprijs!.Value).ToList();
            var ppSqms  = vm.Panden.Where(p => p.PrijsPerM2.HasValue).Select(p => p.PrijsPerM2!.Value).ToList();
            var opps    = vm.Panden.Where(p => p.Oppervlakte.HasValue).Select(p => p.Oppervlakte!.Value).ToList();
            vm.Kpi = new VergelijkbaarKpiViewModel
            {
                Aantal         = vm.Panden.Count,
                GemPrijs       = prijzen.Count > 0 ? Math.Round(prijzen.Average(), 0) : null,
                GemPrijsPerM2  = ppSqms.Count  > 0 ? Math.Round(ppSqms.Average(), 0)  : null,
                LaagstePrijs   = prijzen.Count > 0 ? prijzen.Min() : null,
                HoogstePrijs   = prijzen.Count > 0 ? prijzen.Max() : null,
                GemOppervlakte = opps.Count    > 0 ? Math.Round(opps.Average(), 0)    : null
            };
        }

        return vm;
    }

    // ── Vergelijkbare panden helpers ──────────────────────────────────────────

    private async Task<Dictionary<int, int>> BerekenAantalPerGemeenteAsync(CancellationToken ct) =>
        await _db.MarketAssets
            .Where(a => a.IsActive && !a.IsProjectGroup && a.GeoMunicipalityId.HasValue)
            .GroupBy(a => a.GeoMunicipalityId!.Value)
            .Select(g => new { Id = g.Key, Aantal = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Aantal, ct);

    private async Task<Dictionary<long, decimal>> BerekenProjectVerkoopgraadAsync(
        List<long> projectGroupIds, CancellationToken ct)
    {
        var latestKpiIds = await _db.ProjectGroupKpis
            .Where(k => projectGroupIds.Contains(k.MarketAssetId))
            .GroupBy(k => k.MarketAssetId)
            .Select(g => new { AssetId = g.Key, Id = g.OrderByDescending(k => k.SnapshotDate).Select(k => k.Id).First() })
            .AsNoTracking()
            .ToListAsync(ct);

        if (latestKpiIds.Count > 0)
        {
            var kpiIds = latestKpiIds.Select(x => x.Id).ToList();
            return await _db.ProjectGroupKpis
                .Where(k => kpiIds.Contains(k.Id))
                .AsNoTracking()
                .ToDictionaryAsync(k => k.MarketAssetId, k => k.SoldPercentage, ct);
        }

        var counts = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue && projectGroupIds.Contains(a.ParentMarketAssetId.Value))
            .GroupBy(a => a.ParentMarketAssetId!.Value)
            .Select(g => new { Id = g.Key, Total = g.Count(), Sold = g.Count(a => a.SaleStatus == SaleStatus.Sold) })
            .AsNoTracking()
            .ToListAsync(ct);

        return counts.ToDictionary(
            x => x.Id,
            x => x.Total > 0 ? Math.Round((decimal)x.Sold / x.Total * 100, 1) : 0m);
    }

    private static string FormatAdres(string? straat, string? huisnummer, string? postcode, string? gemeente)
    {
        var regel1 = string.Join(" ", new[] { straat, huisnummer }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var regel2 = string.Join(" ", new[] { postcode, gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrEmpty(regel1) ? regel2 : $"{regel1}\n{regel2}".Trim('\n');
    }

    private static string MapTypeLabel(PropertyType type) => type switch
    {
        PropertyType.Apartment => "Appartement",
        PropertyType.House     => "Woning",
        _                      => type.ToString()
    };

    private static string MapStatusLabel(SaleStatus? saleStatus, AssetLifecycleStatus? lifecycleStatus) =>
        saleStatus == SaleStatus.Sold || lifecycleStatus == AssetLifecycleStatus.SoldConfirmed
            ? "Verkocht"
            : lifecycleStatus == AssetLifecycleStatus.LikelySold
                ? "Vermoedelijk verkocht"
                : "Beschikbaar";

    // ── Geocoding + live straal-telling (kaart-UI "Rond adres") ────────────────

    public async Task<GeocodeAdresResult?> GeocodeAdresAsync(string adres, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(adres)) return null;

        // Nominatim-zoekopdracht bijsturen richting België voor betere adrestreffers.
        var zoekterm = adres.Contains("belgi", StringComparison.OrdinalIgnoreCase) ? adres : $"{adres}, België";
        var result = await ServiceCore.Helpers.GeocodingHelper.GeocodeAsync(zoekterm);
        if (result is null) return null;

        return new GeocodeAdresResult(result.Lat, result.Lon, result.DisplayName);
    }

    public async Task<int> TelPandenInStraalAsync(
        double lat,
        double lng,
        int straalMeter,
        string type,
        decimal? oppervlakte,
        int tolerantie,
        decimal? prijsMin,
        decimal? prijsMax,
        int? slaapkamers,
        string status,
        CancellationToken ct = default)
    {
        tolerantie = Math.Max(0, tolerantie);

        var straalKm = straalMeter / 1000.0;
        var dLat = (decimal)(straalKm / 111.0);
        var dLng = (decimal)(straalKm / (111.0 * Math.Cos(lat * Math.PI / 180)));
        var minLat = (decimal)lat - dLat;
        var maxLat = (decimal)lat + dLat;
        var minLng = (decimal)lng - dLng;
        var maxLng = (decimal)lng + dLng;

        var oppMin = oppervlakte.HasValue ? oppervlakte.Value - tolerantie : 0m;
        var oppMax = oppervlakte.HasValue ? oppervlakte.Value + tolerantie : decimal.MaxValue;

        // ── Projectunits binnen bounding box (exacte Haversine-filter in memory) ─
        var pgInBox = await _db.MarketAssets
            .Where(a => a.IsProjectGroup && a.IsActive
                     && a.Latitude.HasValue && a.Longitude.HasValue
                     && a.Latitude >= minLat && a.Latitude <= maxLat
                     && a.Longitude >= minLng && a.Longitude <= maxLng)
            .Select(a => new { a.Id, a.Latitude, a.Longitude })
            .AsNoTracking()
            .ToListAsync(ct);

        var pgIdsExact = pgInBox
            .Where(a => VpGeoDistance(lat, lng, (double)a.Latitude!.Value, (double)a.Longitude!.Value) <= straalMeter)
            .Select(a => a.Id)
            .ToList();

        int projectCount = 0;
        if (pgIdsExact.Count > 0)
        {
            var uq = _db.MarketAssets
                .Where(a => a.ParentMarketAssetId.HasValue && pgIdsExact.Contains(a.ParentMarketAssetId.Value));

            if (type == "Appartement") uq = uq.Where(a => a.PropertyType == PropertyType.Apartment);
            else if (type == "Woning")  uq = uq.Where(a => a.PropertyType == PropertyType.House);
            if (oppervlakte.HasValue) uq = uq.Where(a => a.LivingArea.HasValue && a.LivingArea >= oppMin && a.LivingArea <= oppMax);
            if (slaapkamers.HasValue) uq = uq.Where(a => a.Bedrooms == slaapkamers.Value);
            if (status == "Beschikbaar") uq = uq.Where(a => a.SaleStatus == SaleStatus.Available || a.SaleStatus == null);
            else if (status == "Verkocht") uq = uq.Where(a => a.SaleStatus == SaleStatus.Sold);

            if (prijsMin.HasValue || prijsMax.HasValue)
                projectCount += await TelBinnenPrijsbereikAsync(uq.Select(a => a.Id), prijsMin, prijsMax, ct);
            else
                projectCount += await uq.CountAsync(ct);
        }

        // ── Losse listings binnen bounding box ────────────────────────────────
        var lq = _db.MarketAssets
            .Where(a => !a.IsProjectGroup && a.ParentMarketAssetId == null && a.IsActive
                     && a.Latitude.HasValue && a.Longitude.HasValue
                     && a.Latitude >= minLat && a.Latitude <= maxLat
                     && a.Longitude >= minLng && a.Longitude <= maxLng);

        if (type == "Appartement") lq = lq.Where(a => a.PropertyType == PropertyType.Apartment);
        else if (type == "Woning")  lq = lq.Where(a => a.PropertyType == PropertyType.House);
        if (oppervlakte.HasValue) lq = lq.Where(a => a.LivingArea.HasValue && a.LivingArea >= oppMin && a.LivingArea <= oppMax);
        if (slaapkamers.HasValue) lq = lq.Where(a => a.Bedrooms == slaapkamers.Value);
        if (status == "Beschikbaar") lq = lq.Where(a => a.SaleStatus == SaleStatus.Available || a.SaleStatus == null);
        else if (status == "Verkocht") lq = lq.Where(a => a.SaleStatus == SaleStatus.Sold);

        var looseInBox = await lq
            .Select(a => new { a.Id, a.Latitude, a.Longitude })
            .AsNoTracking()
            .ToListAsync(ct);

        var looseIdsExact = looseInBox
            .Where(a => VpGeoDistance(lat, lng, (double)a.Latitude!.Value, (double)a.Longitude!.Value) <= straalMeter)
            .Select(a => a.Id)
            .ToList();

        int looseCount = prijsMin.HasValue || prijsMax.HasValue
            ? await TelBinnenPrijsbereikAsync(looseIdsExact, prijsMin, prijsMax, ct)
            : looseIdsExact.Count;

        return projectCount + looseCount;
    }

    private async Task<int> TelBinnenPrijsbereikAsync(
        IQueryable<long> assetIdsQuery, decimal? prijsMin, decimal? prijsMax, CancellationToken ct)
    {
        var assetIds = await assetIdsQuery.ToListAsync(ct);
        return await TelBinnenPrijsbereikAsync(assetIds, prijsMin, prijsMax, ct);
    }

    private async Task<int> TelBinnenPrijsbereikAsync(
        List<long> assetIds, decimal? prijsMin, decimal? prijsMax, CancellationToken ct)
    {
        if (assetIds.Count == 0) return 0;

        var prices = await _db.MarketListings
            .Where(l => assetIds.Contains(l.MarketAssetId) && l.IsActive)
            .GroupBy(l => l.MarketAssetId)
            .Select(g => g.Max(l => l.AskingPrice))
            .ToListAsync(ct);

        return prices.Count(p =>
            (!prijsMin.HasValue || (p.HasValue && p >= prijsMin.Value)) &&
            (!prijsMax.HasValue || (p.HasValue && p <= prijsMax.Value)));
    }

    private static double VpGeoDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6_371_000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ── Projectdetail ─────────────────────────────────────────────────────────

    public async Task<ProjectDetailViewModel?> GetProjectDetailAsync(long id, CancellationToken ct = default)
    {
        var project = await _db.MarketAssets
            .Where(a => a.Id == id && a.IsProjectGroup)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (project is null) return null;

        var projectNaam = await _db.MarketListings
            .Where(l => l.MarketAssetId == id && l.Title != null)
            .OrderByDescending(l => l.LastSeenAt)
            .Select(l => l.Title)
            .FirstOrDefaultAsync(ct) ?? project.AssetKey;

        // ── Canonical bronnen laden ────────────────────────────────────────────
        var canonicalLink = await _db.CanonicalProjectAssets
            .Where(l => l.MarketAssetId == id)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        long? canonicalProjectId = null;
        List<CanonicalProjectAsset> allCanonicalLinks = [];
        List<MarketAsset> linkedProjectAssets = [];
        var linkedProjectSources  = new Dictionary<long, string>();
        var linkedProjectTitles   = new Dictionary<long, string>();
        var linkedProjectUrls     = new Dictionary<long, string?>();
        var linkedProjectLastSeen = new Dictionary<long, DateTime?>();
        var linkedProjectActief   = new Dictionary<long, bool>();

        // Default: query units only for this asset; overridden below if a canonical group is found.
        var linkedProjectIds = CanonicalProjectHelpers.ResolveLinkedProjectIds(id, []);

        if (canonicalLink != null)
        {
            canonicalProjectId = canonicalLink.CanonicalProjectId;

            allCanonicalLinks = await _db.CanonicalProjectAssets
                .Where(l => l.CanonicalProjectId == canonicalLink.CanonicalProjectId)
                .AsNoTracking()
                .ToListAsync(ct);

            linkedProjectIds = CanonicalProjectHelpers.ResolveLinkedProjectIds(id, allCanonicalLinks);

            linkedProjectAssets = await _db.MarketAssets
                .Where(a => linkedProjectIds.Contains(a.Id))
                .AsNoTracking()
                .ToListAsync(ct);

            // Niet filteren op IsActive: we willen prijs/bron altijd tonen, ook als de listing
            // niet meer bevestigd is bij de laatste crawl — de inactieve status wordt apart doorgegeven.
            var linkedListings = await _db.MarketListings
                .Where(l => linkedProjectIds.Contains(l.MarketAssetId))
                .Select(l => new { l.MarketAssetId, SourceName = l.Source.Name, l.Title, l.Url, l.LastSeenAt, l.IsActive })
                .AsNoTracking()
                .ToListAsync(ct);

            var latestListingPerAsset = linkedListings
                .GroupBy(l => l.MarketAssetId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.LastSeenAt).First());

            foreach (var (linkedAssetId, listing) in latestListingPerAsset)
            {
                linkedProjectSources[linkedAssetId]  = listing.SourceName ?? "";
                linkedProjectTitles[linkedAssetId]   = listing.Title ?? "";
                linkedProjectUrls[linkedAssetId]     = listing.Url;
                linkedProjectLastSeen[linkedAssetId] = listing.LastSeenAt;
                linkedProjectActief[linkedAssetId]   = listing.IsActive;
            }
        }

        // FIXED: load units from ALL linked project assets (was: ParentMarketAssetId == id only)
        var units = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue && linkedProjectIds.Contains(a.ParentMarketAssetId.Value))
            .AsNoTracking()
            .ToListAsync(ct);

        // Build BronnenLijst after units are loaded so we have per-source counts.
        var unitStatsByProject = CanonicalProjectHelpers.ComputeUnitStatsByProject(units);

        var bronnenLijst = allCanonicalLinks
            .Select(link =>
            {
                var asset = linkedProjectAssets.FirstOrDefault(a => a.Id == link.MarketAssetId);
                linkedProjectSources.TryGetValue(link.MarketAssetId, out var sourceName);
                linkedProjectUrls.TryGetValue(link.MarketAssetId, out var url);
                linkedProjectTitles.TryGetValue(link.MarketAssetId, out var title);
                linkedProjectLastSeen.TryGetValue(link.MarketAssetId, out var lastSeen);
                linkedProjectActief.TryGetValue(link.MarketAssetId, out var isActief);
                unitStatsByProject.TryGetValue(link.MarketAssetId, out var stats);

                var adres = string.Join(", ",
                    new[]
                    {
                        string.Join(" ", new[] { asset?.Street, asset?.HouseNumber }.Where(s => !string.IsNullOrWhiteSpace(s))),
                        string.Join(" ", new[] { asset?.PostalCode, asset?.City }.Where(s => !string.IsNullOrWhiteSpace(s)))
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));

                return new BronViewModel
                {
                    MarketAssetId  = link.MarketAssetId,
                    BronNaam       = sourceName ?? "",
                    ExternalId     = asset?.AssetKey,
                    Url            = url,
                    Title          = title,
                    IsPrimary      = link.IsPrimary,
                    DeveloperNaam  = asset?.DeveloperName,
                    UnitCount      = stats.Total,
                    AvailableCount = stats.Available,
                    SoldCount      = stats.Sold,
                    Adres          = adres,
                    LastSeenAt     = lastSeen,
                    IsActief       = isActief
                };
            })
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.BronNaam)
            .ToList();

        // Officiële lokatie voor het project zelf
        var projectLok = (await LaadOfficieleLokaties([project], ct)).GetValueOrDefault(project.Id);

        Dictionary<long, UnitSnapshot> snapshots = new();
        if (units.Count > 0)
        {
            var unitIds = units.Select(u => u.Id).ToList();

            // Niet filteren op IsActive: prijs/bron blijven zichtbaar, ook als de listing
            // niet meer bevestigd is bij de laatste crawl (zie IsBronActief op UnitRijViewModel).
            var unitListings = await _db.MarketListings
                .Where(l => unitIds.Contains(l.MarketAssetId))
                .Select(l => new { l.Id, l.MarketAssetId, l.Title, l.Url, l.LastSeenAt, l.IsActive })
                .AsNoTracking()
                .ToListAsync(ct);

            var listingIds = unitListings.Select(l => l.Id).ToList();

            var snapshotRows = await _db.MarketListingSnapshots
                .Where(s => listingIds.Contains(s.MarketListingId))
                .Select(s => new { s.MarketListingId, s.AskingPrice, s.PricePerSqm, s.SnapshotDate })
                .AsNoTracking()
                .ToListAsync(ct);

            var listingIdToAsset = unitListings.ToDictionary(l => l.Id, l => l);

            snapshots = snapshotRows
                .Where(s => listingIdToAsset.ContainsKey(s.MarketListingId))
                .GroupBy(s => listingIdToAsset[s.MarketListingId].MarketAssetId)
                .ToDictionary(
                    g => g.Key,
                    g => { var l = g.OrderByDescending(s => s.SnapshotDate).First(); return new UnitSnapshot(l.AskingPrice, l.PricePerSqm); });

            var listingInfoPerUnit = unitListings
                .GroupBy(l => l.MarketAssetId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.LastSeenAt).First());

            // Andere projecten in dezelfde gemeente (via GeoMunicipality)
            var andereProjektenAssets = project.GeoMunicipalityId.HasValue
                ? await _db.MarketAssets
                    .Where(a => a.IsProjectGroup && a.IsActive
                             && a.GeoMunicipalityId == project.GeoMunicipalityId && a.Id != id)
                    .AsNoTracking()
                    .ToListAsync(ct)
                : project.PostalCode is not null
                    ? await _db.MarketAssets
                        .Where(a => a.IsProjectGroup && a.IsActive && a.PostalCode == project.PostalCode && a.Id != id)
                        .AsNoTracking()
                        .ToListAsync(ct)
                    : new List<MarketAsset>();

            var andereProjekten = andereProjektenAssets.Select(a => new { a.Id, a.City }).ToList();
            var andereProjektenLokaties = andereProjektenAssets.Count > 0
                ? await LaadOfficieleLokaties(andereProjektenAssets, ct)
                : new Dictionary<long, OfficieleLokatie>();

            var andereProjektNamen = andereProjekten.Count > 0
                ? await _db.MarketListings
                    .Where(l => andereProjekten.Select(p => p.Id).Contains(l.MarketAssetId) && l.Title != null)
                    .GroupBy(l => l.MarketAssetId)
                    .Select(g => new { AssetId = g.Key, Naam = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Title).First() })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.AssetId, x => x.Naam ?? "", ct)
                : new Dictionary<long, string>();

            var unitRows = units.Select((u, i) =>
            {
                listingInfoPerUnit.TryGetValue(u.Id, out var listing);
                snapshots.TryGetValue(u.Id, out var snap);

                var naam = listing?.Title is { Length: > 0 } t ? t
                         : u.UnitExternalId is { Length: > 0 } ext ? ext
                         : $"Unit {i + 1}";

                var typeLabel = u.PropertyType switch
                {
                    PropertyType.Apartment => "Appartement",
                    PropertyType.House     => "Woning",
                    _                      => u.PropertyType.ToString()
                };

                var status = u.SaleStatus switch
                {
                    SaleStatus.Sold     => "Verkocht",
                    SaleStatus.Reserved => "Gereserveerd",
                    SaleStatus.Option   => "Optie",
                    _ => u.LifecycleStatus == AssetLifecycleStatus.LikelySold ? "Vermoedelijk verkocht" : "Beschikbaar"
                };

                linkedProjectSources.TryGetValue(u.ParentMarketAssetId ?? 0, out var bronNaam);
                linkedProjectTitles.TryGetValue(u.ParentMarketAssetId ?? 0, out var ouderProjectNaam);

                return new UnitRijViewModel
                {
                    Id               = u.Id,
                    Naam             = naam,
                    TypeLabel        = typeLabel,
                    Oppervlakte      = u.LivingArea,
                    Slaapkamers      = u.Bedrooms,
                    Vraagprijs       = snap?.AskingPrice,
                    PrijsPerM2       = snap?.PricePerSqm,
                    Status           = status,
                    SourceUrl        = listing?.Url is { Length: > 0 } url ? url : null,
                    BronNaam         = bronNaam,
                    OuderProjectNaam = !string.IsNullOrEmpty(ouderProjectNaam) ? ouderProjectNaam : null,
                    IsBronActief     = listing?.IsActive ?? false
                };
            }).OrderBy(u => u.Naam).ToList();

            // ── Canonical units laden (voor deduplicatie-weergave) ─────────────
            List<CanonicalUnitViewModel> canonicalUnitRows = [];
            if (canonicalProjectId.HasValue)
            {
                var canonicalUnits = await _db.CanonicalUnits
                    .Where(u => u.CanonicalProjectId == canonicalProjectId.Value)
                    .Include(u => u.SourceAssets)
                    .AsNoTracking()
                    .ToListAsync(ct);

                if (canonicalUnits.Count > 0)
                {
                    // Prijs per source-unit opzoeken via de snapshot-dictionary
                    var sourceAssetIds = canonicalUnits
                        .SelectMany(u => u.SourceAssets)
                        .Select(a => a.MarketAssetId)
                        .ToList();

                    // Niet filteren op IsActive: prijs/url blijven zichtbaar, ook als de listing
                    // niet meer bevestigd is bij de laatste crawl (zie IsActief op UnitBronViewModel).
                    var sourceListings = await _db.MarketListings
                        .Where(l => sourceAssetIds.Contains(l.MarketAssetId))
                        .Select(l => new { l.MarketAssetId, l.AskingPrice, l.Url, l.IsActive })
                        .AsNoTracking()
                        .ToListAsync(ct);

                    var sourcePriceByAsset = sourceListings
                        .GroupBy(l => l.MarketAssetId)
                        .ToDictionary(g => g.Key, g => g.First().AskingPrice);

                    var sourceUrlByAsset = sourceListings
                        .GroupBy(l => l.MarketAssetId)
                        .ToDictionary(g => g.Key, g => g.First().Url);

                    var sourceActiefByAsset = sourceListings
                        .GroupBy(l => l.MarketAssetId)
                        .ToDictionary(g => g.Key, g => g.First().IsActive);

                    canonicalUnitRows = canonicalUnits
                        .Select(cu =>
                        {
                            var typeLabel = cu.PropertyType switch
                            {
                                PropertyType.Apartment => "Appartement",
                                PropertyType.House     => "Woning",
                                _                      => cu.PropertyType.ToString()
                            };

                            var statusLabel = cu.Status switch
                            {
                                SaleStatus.Sold     => "Verkocht",
                                SaleStatus.Reserved => "Gereserveerd",
                                SaleStatus.Option   => "Optie",
                                _                   => "Beschikbaar"
                            };

                            var bronnen = cu.SourceAssets.Select(sa =>
                            {
                                var sourceAsset = units.FirstOrDefault(u => u.Id == sa.MarketAssetId);
                                sourcePriceByAsset.TryGetValue(sa.MarketAssetId, out var saPrice);
                                sourceUrlByAsset.TryGetValue(sa.MarketAssetId, out var saUrl);
                                sourceActiefByAsset.TryGetValue(sa.MarketAssetId, out var saActief);

                                return new UnitBronViewModel
                                {
                                    MarketAssetId      = sa.MarketAssetId,
                                    BronNaam           = sa.SourceName,
                                    ExternalId         = sa.ExternalId,
                                    Url                = saUrl,
                                    Vraagprijs         = saPrice,
                                    Oppervlakte        = sourceAsset?.LivingArea,
                                    Status             = sourceAsset?.SaleStatus switch
                                    {
                                        SaleStatus.Sold     => "Verkocht",
                                        SaleStatus.Reserved => "Gereserveerd",
                                        SaleStatus.Option   => "Optie",
                                        _                   => "Beschikbaar"
                                    },
                                    MatchLevel         = sa.MatchLevel,
                                    IsRepresentatief   = sa.IsRepresentative,
                                    IsLosseAdvertentie = sa.IsFromLooseListing,
                                    IsActief           = saActief
                                };
                            }).OrderByDescending(b => b.IsRepresentatief).ThenBy(b => b.BronNaam).ToList();

                            return new CanonicalUnitViewModel
                            {
                                Id                       = cu.Id,
                                Titel                    = cu.Title ?? typeLabel,
                                TypeLabel                = typeLabel,
                                Oppervlakte              = cu.Area,
                                Slaapkamers              = cu.Bedrooms,
                                Vraagprijs               = cu.Price,
                                PrijsPerM2               = cu.PricePerSqm,
                                Status                   = statusLabel,
                                Verdieping               = cu.Floor,
                                IsAmbiguous              = cu.IsAmbiguous,
                                HeeftPrijsConflict       = cu.HasPriceConflict,
                                HeeftStatusConflict      = cu.HasStatusConflict,
                                HeeftOppervlakteConflict = cu.HasAreaConflict,
                                ConflictSamenvatting     = cu.ConflictSummary,
                                Bronnen                  = bronnen
                            };
                        })
                        .OrderBy(u => u.Verdieping ?? 999).ThenBy(u => u.Titel).ThenBy(u => u.Oppervlakte).ToList();
                }
            }

            // KPI's berekenen — gebruik canonical units als beschikbaar (vermijdt dubbeltellingen)
            int kpiTotal, kpiAvail, kpiSold, kpiSoldConfirmed, kpiLikelySold;
            List<decimal> kpiPrices, kpiPpSqms, kpiAreas;

            if (canonicalUnitRows.Count > 0)
            {
                kpiTotal         = canonicalUnitRows.Count;
                kpiSold          = canonicalUnitRows.Count(u => u.Status == "Verkocht");
                kpiAvail         = canonicalUnitRows.Count(u => u.Status == "Beschikbaar");
                kpiSoldConfirmed = kpiSold;
                kpiLikelySold    = 0;
                kpiPrices        = canonicalUnitRows.Where(u => u.Vraagprijs.HasValue).Select(u => u.Vraagprijs!.Value).ToList();
                kpiPpSqms        = canonicalUnitRows.Where(u => u.PrijsPerM2.HasValue).Select(u => u.PrijsPerM2!.Value).ToList();
                kpiAreas         = canonicalUnitRows.Where(u => u.Oppervlakte.HasValue).Select(u => u.Oppervlakte!.Value).ToList();
            }
            else
            {
                kpiTotal         = units.Count;
                kpiAvail         = units.Count(u => u.SaleStatus == SaleStatus.Available);
                kpiSold          = units.Count(u => u.SaleStatus == SaleStatus.Sold);
                kpiSoldConfirmed = units.Count(u => u.LifecycleStatus == AssetLifecycleStatus.SoldConfirmed);
                kpiLikelySold    = units.Count(u => u.LifecycleStatus == AssetLifecycleStatus.LikelySold);
                kpiPrices        = unitRows.Where(u => u.Vraagprijs.HasValue).Select(u => u.Vraagprijs!.Value).ToList();
                kpiPpSqms        = unitRows.Where(u => u.PrijsPerM2.HasValue).Select(u => u.PrijsPerM2!.Value).ToList();
                kpiAreas         = units.Where(u => u.LivingArea is > 0).Select(u => u.LivingArea!.Value).ToList();
            }

            var prices          = kpiPrices;
            var ppSqms          = kpiPpSqms;
            var areas           = kpiAreas;
            var soldCnt         = kpiSold;
            var availCnt        = kpiAvail;
            var soldConfirmedCnt = kpiSoldConfirmed;
            var likelySoldCnt    = kpiLikelySold;

            var apartCount = units.Count(u => u.PropertyType == PropertyType.Apartment);
            var houseCount = units.Count(u => u.PropertyType == PropertyType.House);
            var typeLabel2  = (apartCount, houseCount) switch
            {
                ( > 0, 0)   => "Appartement",
                (0, > 0)    => "Woning",
                ( > 0, > 0) => "Gemengd",
                _           => "-"
            };

            var navOpties = new List<ProjectNavigatieOptie>
            {
                new() { Id = id, Naam = projectNaam!, Gemeente = projectLok?.GemeenteNaam ?? project.City }
            };
            foreach (var ap in andereProjekten)
            {
                andereProjektenLokaties.TryGetValue(ap.Id, out var apLok);
                navOpties.Add(new ProjectNavigatieOptie
                {
                    Id       = ap.Id,
                    Naam     = andereProjektNamen.GetValueOrDefault(ap.Id, ap.Id.ToString()),
                    Gemeente = apLok?.GemeenteNaam ?? ap.City
                });
            }

            return new ProjectDetailViewModel
            {
                Id                   = id,
                ProjectNaam          = projectNaam!,
                TypeLabel            = typeLabel2,
                Straat               = project.Street,
                Huisnummer           = project.HouseNumber,
                Postcode             = projectLok?.ZipCode ?? project.PostalCode,
                Gemeente             = projectLok?.GemeenteNaam ?? project.City,
                DeveloperNaam        = project.DeveloperName,
                DeveloperWebsite     = project.DeveloperWebsite,
                DeveloperTelefoon    = project.DeveloperPhone,
                TotaalUnits          = kpiTotal,
                BeschikbareUnits     = availCnt,
                VerkochteUnits       = soldCnt,
                SoldConfirmedCount   = soldConfirmedCnt,
                LikelySoldCount      = likelySoldCnt,
                Verkoopgraad         = kpiTotal > 0 ? Math.Round((decimal)soldCnt / kpiTotal * 100, 1) : 0m,
                GemiddeldePrijs      = prices.Count > 0 ? Math.Round(prices.Average(), 0) : null,
                GemiddeldePrijsPerM2 = ppSqms.Count > 0 ? Math.Round(ppSqms.Average(), 0) : null,
                GemiddeldeOppervlakte = areas.Count > 0 ? Math.Round(areas.Average(), 0) : null,
                Units                = unitRows,
                CanonicalUnits       = canonicalUnitRows,
                AndereProjecten      = navOpties,
                CanonicalProjectId   = canonicalProjectId,
                BronnenLijst         = bronnenLijst
            };
        }

        return new ProjectDetailViewModel
        {
            Id          = id,
            ProjectNaam = projectNaam!,
            Straat      = project.Street,
            Huisnummer  = project.HouseNumber,
            Postcode    = projectLok?.ZipCode ?? project.PostalCode,
            Gemeente    = projectLok?.GemeenteNaam ?? project.City,
            DeveloperNaam     = project.DeveloperName,
            DeveloperWebsite  = project.DeveloperWebsite,
            DeveloperTelefoon = project.DeveloperPhone,
            AndereProjecten   = new List<ProjectNavigatieOptie>
            {
                new() { Id = id, Naam = projectNaam!, Gemeente = projectLok?.GemeenteNaam ?? project.City }
            },
            CanonicalProjectId = canonicalProjectId,
            BronnenLijst       = bronnenLijst
        };
    }
}
