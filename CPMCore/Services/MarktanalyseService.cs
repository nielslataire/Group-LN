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

    public async Task<List<LocatieOptie>> GetLocatiesAsync(CancellationToken ct = default)
    {
        var projectRows = await _db.MarketAssets
            .Where(a => a.IsProjectGroup && a.IsActive && a.PostalCode != null && a.City != null)
            .Select(a => new { a.PostalCode, a.City })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(ct);

        var losseRows = await _db.MarketAssets
            .Where(a => !a.IsProjectGroup && a.ParentMarketAssetId == null
                     && a.IsActive && a.PostalCode != null && a.City != null)
            .Select(a => new { a.PostalCode, a.City })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(ct);

        // Per (postcode + city case-insensitief) de meest-voorkomende spelling bewaren
        // zodat alle deelgemeenten van dezelfde postcode als aparte entries verschijnen
        return projectRows.Concat(losseRows)
            .Select(r => new { Postcode = r.PostalCode!.Trim(), City = r.City!.Trim() })
            .Where(r => r.Postcode.Length > 0 && r.City.Length > 0)
            .GroupBy(r => new { r.Postcode, CityKey = r.City.ToLowerInvariant() })
            .Select(g => new LocatieOptie
            {
                Postcode = g.Key.Postcode,
                Gemeente = g.GroupBy(r => r.City)
                            .OrderByDescending(cg => cg.Count())
                            .First().Key
            })
            .OrderBy(l => l.Gemeente)
            .ThenBy(l => l.Postcode)
            .ToList();
    }

    public async Task<GemeenteAnalyseViewModel> GetGemeenteAnalyseAsync(
        string? postcode,
        string? gemeente,
        string type,
        string aanbodtype = "Alles",
        CancellationToken ct = default)
    {
        var vm = new GemeenteAnalyseViewModel
        {
            GeselecteerdePostcode = postcode,
            GeselecteerdeGemeente = gemeente,
            GeselecteerdType = type,
            GeselecteerdAanbodtype = aanbodtype
        };

        if (string.IsNullOrWhiteSpace(postcode)) return vm;

        bool loadProjecten = aanbodtype != "Losse eenheden";
        bool loadLosseEenheden = aanbodtype != "Projecten";

        // ── Projectgroepen ophalen ─────────────────────────────────────────────
        List<MarketAsset> projecten = [];
        Dictionary<long, string> projectNamen = new();

        if (loadProjecten)
        {
            var projectQuery = _db.MarketAssets
                .Where(a => a.IsProjectGroup && a.IsActive && a.PostalCode == postcode
                         && (gemeente == null || a.City == gemeente));

            if (type == "Appartement")
                projectQuery = projectQuery.Where(a => a.PropertySubType == PropertySubType.ApartmentGroup);
            else if (type == "Woning")
                projectQuery = projectQuery.Where(a => a.PropertySubType == PropertySubType.HouseGroup);

            var alleProjecten = await projectQuery.AsNoTracking().ToListAsync(ct);

            if (alleProjecten.Count > 0)
            {
                var alleProjectIds = alleProjecten.Select(p => p.Id).ToList();
                var dupIds = await GetDuplicaatProjectIdsAsync(alleProjectIds, ct);
                projecten = alleProjecten.Where(p => !dupIds.Contains(p.Id)).ToList();

                projectNamen = await _db.MarketListings
                    .Where(l => alleProjectIds.Contains(l.MarketAssetId) && l.Title != null)
                    .GroupBy(l => l.MarketAssetId)
                    .Select(g => new
                    {
                        AssetId = g.Key,
                        Naam = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Title).First()
                    })
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.AssetId, x => x.Naam ?? "", ct);
            }
        }

        // GeselecteerdeGemeente komt al van de parameter; enkel invullen als het leeg was
        if (string.IsNullOrEmpty(vm.GeselecteerdeGemeente))
            vm.GeselecteerdeGemeente = projecten.FirstOrDefault()?.City;

        // ── Project units ophalen ──────────────────────────────────────────────
        List<MarketAsset> projectUnits = [];
        if (projecten.Count > 0)
        {
            var projectIds = projecten.Select(p => p.Id).ToList();
            var unitQuery = _db.MarketAssets
                .Where(a => a.ParentMarketAssetId.HasValue && projectIds.Contains(a.ParentMarketAssetId.Value));

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
                .Where(a => !a.IsProjectGroup && a.ParentMarketAssetId == null
                         && a.IsActive && a.PostalCode == postcode
                         && (gemeente == null || a.City == gemeente));

            if (type == "Appartement")
                losseQuery = losseQuery.Where(a => a.PropertyType == PropertyType.Apartment);
            else if (type == "Woning")
                losseQuery = losseQuery.Where(a => a.PropertyType == PropertyType.House);

            losseEenheden = await losseQuery.AsNoTracking().ToListAsync(ct);
        }

        if (projecten.Count == 0 && losseEenheden.Count == 0) return vm;

        vm.GeselecteerdeGemeente ??= losseEenheden.FirstOrDefault()?.City;

        // ── Prijssnapshots ophalen voor project units + losse eenheden ─────────
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

        // ── Losse eenheden: titels en bronnen ophalen ──────────────────────────
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
        var soldCount      = projectUnits.Count(u => u.SaleStatus == SaleStatus.Sold);
        var availableCount = projectUnits.Count(u => u.SaleStatus == SaleStatus.Available);
        var reservedCount  = projectUnits.Count(u =>
            u.SaleStatus == SaleStatus.Reserved || u.SaleStatus == SaleStatus.Option);

        var combinedPrices = alleEenhedenIds
            .Select(id => prijzenPerEenheid.TryGetValue(id, out var s) ? s.AskingPrice : null)
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();

        var combinedPpSqm = alleEenhedenIds
            .Select(id => prijzenPerEenheid.TryGetValue(id, out var s) ? s.PricePerSqm : null)
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();

        var areas = projectUnits
            .Where(u => u.LivingArea.HasValue && u.LivingArea > 0)
            .Select(u => u.LivingArea!.Value)
            .ToList();

        vm.Kpi = new GemeenteKpiViewModel
        {
            ActieveProjecten     = projecten.Count,
            AantalProjectUnits   = projectUnits.Count,
            AantalLosseEenheden  = losseEenheden.Count,
            ActieveUnits         = projectUnits.Count,
            VerkochteUnits       = soldCount,
            BeschikbareUnits     = availableCount,
            GereserveerdeUnits   = reservedCount,
            GemiddeldePrijs      = combinedPrices.Count > 0 ? Math.Round(combinedPrices.Average(), 0) : null,
            GemiddeldePrijsPerM2 = combinedPpSqm.Count > 0 ? Math.Round(combinedPpSqm.Average(), 0) : null,
            GemiddeldeOppervlakte = areas.Count > 0 ? Math.Round(areas.Average(), 0) : null,
            Verkoopgraad = projectUnits.Count > 0
                ? Math.Round((decimal)soldCount / projectUnits.Count * 100, 1)
                : 0m
        };

        // ── Grafieken (gecombineerd: project units + losse eenheden) ───────────
        vm.VraagprijsBuckets = BerekeningVraagprijsBuckets(combinedPrices);
        vm.PrijsPerM2Buckets = BerekeningPrijsPerM2Buckets(combinedPpSqm);

        if (projecten.Count > 0)
            vm.VerkoopgraadPerProject = await BerekeningVerkoopgraadPerProjectAsync(projecten, projectNamen, ct);

        // ── Projectentabel ────────────────────────────────────────────────────
        if (projecten.Count > 0)
            vm.Projecten = BouwProjectenTabel(projecten, projectUnits, prijzenPerEenheid, projectNamen);

        // ── Losse eenheden tabel ──────────────────────────────────────────────
        if (losseEenheden.Count > 0)
            vm.LosseEenheden = BouwLosseEenhedenTabel(losseEenheden, prijzenPerEenheid, losseEenhedenInfo);

        return vm;
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

        // Fallback: bereken uit child units
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
                var pct = uc.Total > 0
                    ? Math.Round((decimal)uc.Sold / uc.Total * 100, 1)
                    : 0m;
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
        Dictionary<long, string> namen)
    {
        return projecten
            .Select(project =>
            {
                var projectUnits = units.Where(u => u.ParentMarketAssetId == project.Id).ToList();

                var soldCount      = projectUnits.Count(u => u.SaleStatus == SaleStatus.Sold);
                var availableCount = projectUnits.Count(u => u.SaleStatus == SaleStatus.Available);

                var unitPrices = projectUnits
                    .Where(u => prijzenPerEenheid.TryGetValue(u.Id, out var s) && s.AskingPrice.HasValue)
                    .Select(u => prijzenPerEenheid[u.Id].AskingPrice!.Value)
                    .ToList();

                var unitPpSqm = projectUnits
                    .Where(u => prijzenPerEenheid.TryGetValue(u.Id, out var s) && s.PricePerSqm.HasValue)
                    .Select(u => prijzenPerEenheid[u.Id].PricePerSqm!.Value)
                    .ToList();

                var apartCount = projectUnits.Count(u => u.PropertyType == PropertyType.Apartment);
                var houseCount = projectUnits.Count(u => u.PropertyType == PropertyType.House);
                var typeLabel  = (apartCount, houseCount) switch
                {
                    ( > 0, 0) => "Appartement",
                    (0, > 0)  => "Woning",
                    ( > 0, > 0) => "Gemengd",
                    _ => "-"
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
                    Postcode    = project.PostalCode,
                    Gemeente    = project.City
                };
            })
            .OrderByDescending(p => p.Verkoopgraad)
            .ToList();
    }

    // ── Losse eenheden tabel ──────────────────────────────────────────────────

    private static List<LosseEenheidRijViewModel> BouwLosseEenhedenTabel(
        List<MarketAsset> losseEenheden,
        Dictionary<long, UnitSnapshot> prijzenPerEenheid,
        Dictionary<long, (string Bron, string? SourceUrl)> info)
    {
        return losseEenheden
            .Select(e =>
            {
                var (bron, sourceUrl) = info.TryGetValue(e.Id, out var i) ? i : ("-", null);

                prijzenPerEenheid.TryGetValue(e.Id, out var snapshot);

                var typeLabel = e.PropertyType switch
                {
                    PropertyType.Apartment => "Appartement",
                    PropertyType.House     => "Woning",
                    _                      => e.PropertyType.ToString()
                };

                var statusLabel = e.SaleStatus is SaleStatus.Sold ? "Verkocht" : "Beschikbaar";

                var adresBase = string.Join(" ",
                    new[] { e.Street, e.HouseNumber }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));

                return new LosseEenheidRijViewModel
                {
                    Id            = e.Id,
                    Adres         = string.IsNullOrWhiteSpace(adresBase) ? null : adresBase,
                    Postcode      = e.PostalCode,
                    Gemeente      = e.City,
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

        // Projectnaam uit meest recente listing
        var projectNaam = await _db.MarketListings
            .Where(l => l.MarketAssetId == id && l.Title != null)
            .OrderByDescending(l => l.LastSeenAt)
            .Select(l => l.Title)
            .FirstOrDefaultAsync(ct) ?? project.AssetKey;

        // Child units ophalen
        var units = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId == id)
            .AsNoTracking()
            .ToListAsync(ct);

        // Prijssnapshots voor units
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

            // Listing info per unit (titel, url)
            var listingInfoPerUnit = unitListings
                .GroupBy(l => l.MarketAssetId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(l => l.LastSeenAt).First());

            // Navigatie-opties: andere projecten in dezelfde postcode
            var andereProjekten = project.PostalCode is not null
                ? await _db.MarketAssets
                    .Where(a => a.IsProjectGroup && a.IsActive && a.PostalCode == project.PostalCode && a.Id != id)
                    .Select(a => new { a.Id, a.City })
                    .AsNoTracking()
                    .ToListAsync(ct)
                : [];

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
                    _                   => "Beschikbaar"
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

            var prices   = unitRows.Where(u => u.Vraagprijs.HasValue).Select(u => u.Vraagprijs!.Value).ToList();
            var ppSqms   = unitRows.Where(u => u.PrijsPerM2.HasValue).Select(u => u.PrijsPerM2!.Value).ToList();
            var areas    = units.Where(u => u.LivingArea is > 0).Select(u => u.LivingArea!.Value).ToList();
            var soldCnt  = units.Count(u => u.SaleStatus == SaleStatus.Sold);
            var availCnt = units.Count(u => u.SaleStatus == SaleStatus.Available);

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
                new() { Id = id, Naam = projectNaam!, Gemeente = project.City }
            };
            foreach (var ap in andereProjekten)
            {
                navOpties.Add(new ProjectNavigatieOptie
                {
                    Id       = ap.Id,
                    Naam     = andereProjektNamen.GetValueOrDefault(ap.Id, ap.Id.ToString()),
                    Gemeente = ap.City
                });
            }

            return new ProjectDetailViewModel
            {
                Id                   = id,
                ProjectNaam          = projectNaam!,
                TypeLabel            = typeLabel2,
                Straat               = project.Street,
                Huisnummer           = project.HouseNumber,
                Postcode             = project.PostalCode,
                Gemeente             = project.City,
                DeveloperNaam        = project.DeveloperName,
                DeveloperWebsite     = project.DeveloperWebsite,
                DeveloperTelefoon    = project.DeveloperPhone,
                TotaalUnits          = units.Count,
                BeschikbareUnits     = availCnt,
                VerkochteUnits       = soldCnt,
                Verkoopgraad         = units.Count > 0 ? Math.Round((decimal)soldCnt / units.Count * 100, 1) : 0m,
                GemiddeldePrijs      = prices.Count > 0 ? Math.Round(prices.Average(), 0) : null,
                GemiddeldePrijsPerM2 = ppSqms.Count > 0 ? Math.Round(ppSqms.Average(), 0) : null,
                GemiddeldeOppervlakte = areas.Count > 0 ? Math.Round(areas.Average(), 0) : null,
                Units                = unitRows,
                AndereProjecten      = navOpties
            };
        }

        // Project zonder units
        return new ProjectDetailViewModel
        {
            Id          = id,
            ProjectNaam = projectNaam!,
            Straat      = project.Street,
            Huisnummer  = project.HouseNumber,
            Postcode    = project.PostalCode,
            Gemeente    = project.City,
            DeveloperNaam     = project.DeveloperName,
            DeveloperWebsite  = project.DeveloperWebsite,
            DeveloperTelefoon = project.DeveloperPhone,
            AndereProjecten   = new List<ProjectNavigatieOptie>
            {
                new() { Id = id, Naam = projectNaam!, Gemeente = project.City }
            }
        };
    }
}
