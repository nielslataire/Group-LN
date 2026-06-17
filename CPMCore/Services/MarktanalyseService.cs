using CPMCore.Models.Marktanalyse;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
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
        string aanbodtype = "Alles",
        CancellationToken ct = default)
    {
        var vm = new GemeenteAnalyseViewModel
        {
            GeselecteerdGeoMunicipalityId = geoMunicipalityId,
            GeselecteerdGeoMunicipalSectionId = geoMunicipalSectionId,
            GeselecteerdType = type,
            GeselecteerdAanbodtype = aanbodtype
        };

        if (!geoMunicipalityId.HasValue && !geoMunicipalSectionId.HasValue)
            return vm;

        // Laad display-namen voor de geselecteerde locatie
        await VulDisplayNamenAsync(vm, ct);

        bool loadProjecten = aanbodtype != "Losse eenheden";
        bool loadLosseEenheden = aanbodtype != "Projecten";

        // ── Projectgroepen ophalen ─────────────────────────────────────────────
        List<MarketAsset> projecten = [];
        Dictionary<long, string> projectNamen = new();
        Dictionary<long, List<long>> unitIdsByPrimary = new();

        if (loadProjecten)
        {
            var projectQuery = _db.MarketAssets
                .Where(a => a.IsProjectGroup && a.IsActive);

            projectQuery = ToepassGeoFilter(projectQuery, geoMunicipalityId, geoMunicipalSectionId);

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
                        g => g.CanonicalName ?? listingNamen.GetValueOrDefault(g.PrimaryAssetId, ""));
                    unitIdsByPrimary = canonicalGroepen.ToDictionary(
                        g => g.PrimaryAssetId,
                        g => g.AllAssetIds);
                }
                else
                {
                    // Fallback: origineel gedrag wanneer canonical data nog niet beschikbaar is
                    var dupIds = await GetDuplicaatProjectIdsAsync(alleProjectIds, ct);
                    projecten = alleProjecten.Where(p => !dupIds.Contains(p.Id)).ToList();
                    projectNamen = listingNamen;
                    unitIdsByPrimary = projecten.ToDictionary(p => p.Id, p => new List<long> { p.Id });
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

        // ── Losse eenheden ophalen ─────────────────────────────────────────────
        List<MarketAsset> losseEenheden = [];
        if (loadLosseEenheden)
        {
            var losseQuery = _db.MarketAssets
                .Where(a => !a.IsProjectGroup && a.ParentMarketAssetId == null && a.IsActive);

            losseQuery = ToepassGeoFilter(losseQuery, geoMunicipalityId, geoMunicipalSectionId);

            if (type == "Appartement")
                losseQuery = losseQuery.Where(a => a.PropertyType == PropertyType.Apartment);
            else if (type == "Woning")
                losseQuery = losseQuery.Where(a => a.PropertyType == PropertyType.House);

            losseEenheden = await losseQuery.AsNoTracking().ToListAsync(ct);
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

        // ── KPI berekening ────────────────────────────────────────────────────
        var soldCount          = projectUnits.Count(u => u.SaleStatus == SaleStatus.Sold);
        var availableCount     = projectUnits.Count(u => u.SaleStatus == SaleStatus.Available);
        var reservedCount      = projectUnits.Count(u =>
            u.SaleStatus == SaleStatus.Reserved || u.SaleStatus == SaleStatus.Option);
        var soldConfirmedCount = projectUnits.Count(u => u.LifecycleStatus == AssetLifecycleStatus.SoldConfirmed);
        var likelySoldCount    = projectUnits.Count(u => u.LifecycleStatus == AssetLifecycleStatus.LikelySold);

        var combinedPrices = alleEenhedenIds
            .Select(id => prijzenPerEenheid.TryGetValue(id, out var s) ? s.AskingPrice : null)
            .Where(p => p.HasValue).Select(p => p!.Value).ToList();

        var combinedPpSqm = alleEenhedenIds
            .Select(id => prijzenPerEenheid.TryGetValue(id, out var s) ? s.PricePerSqm : null)
            .Where(p => p.HasValue).Select(p => p!.Value).ToList();

        var areas = projectUnits
            .Where(u => u.LivingArea.HasValue && u.LivingArea > 0)
            .Select(u => u.LivingArea!.Value).ToList();

        vm.Kpi = new GemeenteKpiViewModel
        {
            ActieveProjecten     = projecten.Count,
            AantalProjectUnits   = projectUnits.Count,
            AantalLosseEenheden  = losseEenheden.Count,
            ActieveUnits         = projectUnits.Count,
            VerkochteUnits       = soldCount,
            BeschikbareUnits     = availableCount,
            GereserveerdeUnits   = reservedCount,
            SoldConfirmedCount   = soldConfirmedCount,
            LikelySoldCount      = likelySoldCount,
            GemiddeldePrijs      = combinedPrices.Count > 0 ? Math.Round(combinedPrices.Average(), 0) : null,
            GemiddeldePrijsPerM2 = combinedPpSqm.Count > 0 ? Math.Round(combinedPpSqm.Average(), 0) : null,
            GemiddeldeOppervlakte = areas.Count > 0 ? Math.Round(areas.Average(), 0) : null,
            Verkoopgraad = projectUnits.Count > 0
                ? Math.Round((decimal)soldCount / projectUnits.Count * 100, 1)
                : 0m
        };

        vm.VraagprijsBuckets = BerekeningVraagprijsBuckets(combinedPrices);
        vm.PrijsPerM2Buckets = BerekeningPrijsPerM2Buckets(combinedPpSqm);

        if (projecten.Count > 0)
            vm.VerkoopgraadPerProject = await BerekeningVerkoopgraadPerProjectAsync(projecten, projectNamen, ct);

        if (projecten.Count > 0)
            vm.Projecten = BouwProjectenTabel(projecten, projectUnits, prijzenPerEenheid, projectNamen, officieleLokaties, unitIdsByPrimary);

        if (losseEenheden.Count > 0)
            vm.LosseEenheden = BouwLosseEenhedenTabel(losseEenheden, prijzenPerEenheid, losseEenhedenInfo, officieleLokaties);

        return vm;
    }

    // ── Geo-filter helper ─────────────────────────────────────────────────────

    private static IQueryable<MarketAsset> ToepassGeoFilter(
        IQueryable<MarketAsset> query,
        int? geoMunicipalityId,
        int? geoMunicipalSectionId)
    {
        if (geoMunicipalSectionId.HasValue)
            return query.Where(a => a.GeoMunicipalSectionId == geoMunicipalSectionId.Value);

        if (geoMunicipalityId.HasValue)
            return query.Where(a => a.GeoMunicipalityId == geoMunicipalityId.Value);

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

    private sealed record CanonicalGroupInfo(long PrimaryAssetId, string? CanonicalName, List<long> AllAssetIds);

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
            result.Add(new CanonicalGroupInfo(primary.MarketAssetId, naam, groupIds));
            foreach (var id in groupIds) handled.Add(id);
        }

        // Assets zonder canonical koppeling → singleton
        foreach (var id in knownAssetIds.Where(id => !handled.Contains(id)))
            result.Add(new CanonicalGroupInfo(id, null, [id]));

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

    // ── Verkoopgraad per project ──────────────────────────────────────────────

    private async Task<List<ProjectVerkoopgraadViewModel>> BerekeningVerkoopgraadPerProjectAsync(
        List<MarketAsset> projecten,
        Dictionary<long, string> namen,
        CancellationToken ct)
    {
        var projectIds = projecten.Select(p => p.Id).ToList();

        var latestKpiIds = await _db.ProjectGroupKpis
            .Where(k => projectIds.Contains(k.MarketAssetId))
            .GroupBy(k => k.MarketAssetId)
            .Select(g => g.OrderByDescending(k => k.SnapshotDate).Select(k => k.Id).First())
            .ToListAsync(ct);

        if (latestKpiIds.Count > 0)
        {
            var kpis = await _db.ProjectGroupKpis
                .Where(k => latestKpiIds.Contains(k.Id))
                .AsNoTracking()
                .ToListAsync(ct);

            return kpis
                .Select(k =>
                {
                    var project = projecten.First(p => p.Id == k.MarketAssetId);
                    return new ProjectVerkoopgraadViewModel
                    {
                        ProjectNaam   = namen.GetValueOrDefault(k.MarketAssetId) ?? project.AssetKey,
                        Verkoopgraad  = k.SoldPercentage,
                        VerkochteUnits = k.UnitsSold,
                        TotaalUnits   = k.UnitsTotal
                    };
                })
                .OrderByDescending(p => p.Verkoopgraad)
                .Take(10)
                .ToList();
        }

        var unitCounts = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue && projectIds.Contains(a.ParentMarketAssetId.Value))
            .GroupBy(a => a.ParentMarketAssetId!.Value)
            .Select(g => new
            {
                ProjectId = g.Key,
                Total = g.Count(),
                Sold  = g.Count(a => a.SaleStatus == SaleStatus.Sold)
            })
            .AsNoTracking()
            .ToListAsync(ct);

        return unitCounts
            .Select(uc =>
            {
                var project = projecten.First(p => p.Id == uc.ProjectId);
                var pct = uc.Total > 0 ? Math.Round((decimal)uc.Sold / uc.Total * 100, 1) : 0m;
                return new ProjectVerkoopgraadViewModel
                {
                    ProjectNaam   = namen.GetValueOrDefault(uc.ProjectId) ?? project.AssetKey,
                    Verkoopgraad  = pct,
                    VerkochteUnits = uc.Sold,
                    TotaalUnits   = uc.Total
                };
            })
            .OrderByDescending(p => p.Verkoopgraad)
            .Take(10)
            .ToList();
    }

    // ── Projectentabel ────────────────────────────────────────────────────────

    private static List<ProjectRijViewModel> BouwProjectenTabel(
        List<MarketAsset> projecten,
        List<MarketAsset> units,
        Dictionary<long, UnitSnapshot> prijzenPerEenheid,
        Dictionary<long, string> namen,
        Dictionary<long, OfficieleLokatie> lokaties,
        Dictionary<long, List<long>>? unitIdsByPrimary = null)
    {
        return projecten
            .Select(project =>
            {
                lokaties.TryGetValue(project.Id, out var lok);
                // Gebruik canonical siblings als beschikbaar, anders alleen eigen units
                var siblingIds = unitIdsByPrimary?.TryGetValue(project.Id, out var ids) == true
                    ? ids
                    : (List<long>)[project.Id];
                var projectUnits = units.Where(u => u.ParentMarketAssetId.HasValue && siblingIds.Contains(u.ParentMarketAssetId.Value)).ToList();

                var soldCount      = projectUnits.Count(u => u.SaleStatus == SaleStatus.Sold);
                var availableCount = projectUnits.Count(u => u.SaleStatus == SaleStatus.Available);

                var unitPrices = projectUnits
                    .Where(u => prijzenPerEenheid.TryGetValue(u.Id, out var s) && s.AskingPrice.HasValue)
                    .Select(u => prijzenPerEenheid[u.Id].AskingPrice!.Value).ToList();

                var unitPpSqm = projectUnits
                    .Where(u => prijzenPerEenheid.TryGetValue(u.Id, out var s) && s.PricePerSqm.HasValue)
                    .Select(u => prijzenPerEenheid[u.Id].PricePerSqm!.Value).ToList();

                var apartCount = projectUnits.Count(u => u.PropertyType == PropertyType.Apartment);
                var houseCount = projectUnits.Count(u => u.PropertyType == PropertyType.House);
                var typeLabel  = (apartCount, houseCount) switch
                {
                    ( > 0, 0)   => "Appartement",
                    (0, > 0)    => "Woning",
                    ( > 0, > 0) => "Gemengd",
                    _           => "-"
                };

                var pct = projectUnits.Count > 0
                    ? Math.Round((decimal)soldCount / projectUnits.Count * 100, 1)
                    : 0m;

                return new ProjectRijViewModel
                {
                    Id               = project.Id,
                    ProjectNaam      = namen.GetValueOrDefault(project.Id) ?? project.AssetKey,
                    Ontwikkelaar     = project.DeveloperName ?? "-",
                    TypeLabel        = typeLabel,
                    TotaalUnits      = projectUnits.Count,
                    VerkochteUnits   = soldCount,
                    BeschikbareUnits = availableCount,
                    Verkoopgraad     = pct,
                    GemiddeldePrijs      = unitPrices.Count > 0 ? Math.Round(unitPrices.Average(), 0) : null,
                    GemiddeldePrijsPerM2 = unitPpSqm.Count > 0  ? Math.Round(unitPpSqm.Average(), 0)  : null,
                    Straat      = project.Street,
                    Huisnummer  = project.HouseNumber,
                    Postcode    = lok?.ZipCode ?? project.PostalCode,
                    Gemeente    = lok?.GemeenteNaam ?? project.City
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
        Dictionary<long, OfficieleLokatie> lokaties)
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

                return new LosseEenheidRijViewModel
                {
                    Id            = e.Id,
                    Adres         = string.IsNullOrWhiteSpace(adresBase) ? null : adresBase,
                    Postcode      = lok?.ZipCode ?? e.PostalCode,
                    Gemeente      = lok?.GemeenteNaam ?? e.City,
                    TypeLabel     = typeLabel,
                    Oppervlakte   = e.LivingArea,
                    Slaapkamers   = e.Bedrooms,
                    Vraagprijs    = snapshot?.AskingPrice,
                    PrijsPerM2    = snapshot?.PricePerSqm,
                    Status        = statusLabel,
                    AangeboenDoor = bron,
                    SourceUrl     = sourceUrl
                };
            })
            .OrderBy(e => e.Adres ?? "")
            .ToList();
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

        var units = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId == id)
            .AsNoTracking()
            .ToListAsync(ct);

        // Officiële lokatie voor het project zelf
        var projectLok = (await LaadOfficieleLokaties([project], ct)).GetValueOrDefault(project.Id);

        Dictionary<long, UnitSnapshot> snapshots = new();
        if (units.Count > 0)
        {
            var unitIds = units.Select(u => u.Id).ToList();

            var unitListings = await _db.MarketListings
                .Where(l => unitIds.Contains(l.MarketAssetId) && l.IsActive)
                .Select(l => new { l.Id, l.MarketAssetId, l.Title, l.Url, l.LastSeenAt })
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

                return new UnitRijViewModel
                {
                    Id          = u.Id,
                    Naam        = naam,
                    TypeLabel   = typeLabel,
                    Oppervlakte = u.LivingArea,
                    Slaapkamers = u.Bedrooms,
                    Vraagprijs  = snap?.AskingPrice,
                    PrijsPerM2  = snap?.PricePerSqm,
                    Status      = status,
                    SourceUrl   = listing?.Url is { Length: > 0 } url ? url : null
                };
            }).OrderBy(u => u.Naam).ToList();

            var prices          = unitRows.Where(u => u.Vraagprijs.HasValue).Select(u => u.Vraagprijs!.Value).ToList();
            var ppSqms          = unitRows.Where(u => u.PrijsPerM2.HasValue).Select(u => u.PrijsPerM2!.Value).ToList();
            var areas           = units.Where(u => u.LivingArea is > 0).Select(u => u.LivingArea!.Value).ToList();
            var soldCnt         = units.Count(u => u.SaleStatus == SaleStatus.Sold);
            var availCnt        = units.Count(u => u.SaleStatus == SaleStatus.Available);
            var soldConfirmedCnt = units.Count(u => u.LifecycleStatus == AssetLifecycleStatus.SoldConfirmed);
            var likelySoldCnt    = units.Count(u => u.LifecycleStatus == AssetLifecycleStatus.LikelySold);

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
                TotaalUnits          = units.Count,
                BeschikbareUnits     = availCnt,
                VerkochteUnits       = soldCnt,
                SoldConfirmedCount   = soldConfirmedCnt,
                LikelySoldCount      = likelySoldCnt,
                Verkoopgraad         = units.Count > 0 ? Math.Round((decimal)soldCnt / units.Count * 100, 1) : 0m,
                GemiddeldePrijs      = prices.Count > 0 ? Math.Round(prices.Average(), 0) : null,
                GemiddeldePrijsPerM2 = ppSqms.Count > 0 ? Math.Round(ppSqms.Average(), 0) : null,
                GemiddeldeOppervlakte = areas.Count > 0 ? Math.Round(areas.Average(), 0) : null,
                Units                = unitRows,
                AndereProjecten      = navOpties
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
            }
        };
    }
}
