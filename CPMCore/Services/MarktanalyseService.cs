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

    // Interne prijsdata per unit
    private sealed record UnitSnapshot(decimal? AskingPrice, decimal? PricePerSqm);

    public async Task<List<LocatieOptie>> GetLocatiesAsync(CancellationToken ct = default)
    {
        var rows = await _db.MarketAssets
            .Where(a => a.IsProjectGroup && a.IsActive && a.PostalCode != null && a.City != null)
            .Select(a => new { a.PostalCode, a.City })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.PostalCode)
            .Select(g => new LocatieOptie
            {
                Postcode = g.Key!,
                Gemeente = g.First().City!
            })
            .OrderBy(l => l.Gemeente)
            .ThenBy(l => l.Postcode)
            .ToList();
    }

    public async Task<GemeenteAnalyseViewModel> GetGemeenteAnalyseAsync(
        string? postcode,
        string type,
        CancellationToken ct = default)
    {
        var vm = new GemeenteAnalyseViewModel
        {
            GeselecteerdePostcode = postcode,
            GeselecteerdType = type
        };

        if (string.IsNullOrWhiteSpace(postcode))
            return vm;

        // ── Projectgroepen ophalen ────────────────────────────────────────────
        var projectQuery = _db.MarketAssets
            .Where(a => a.IsProjectGroup && a.IsActive && a.PostalCode == postcode);

        if (type == "Appartement")
            projectQuery = projectQuery.Where(a => a.PropertySubType == PropertySubType.ApartmentGroup);
        else if (type == "Woning")
            projectQuery = projectQuery.Where(a => a.PropertySubType == PropertySubType.HouseGroup);

        var projecten = await projectQuery.AsNoTracking().ToListAsync(ct);

        vm.GeselecteerdeGemeente = projecten.FirstOrDefault()?.City;

        if (projecten.Count == 0)
            return vm;

        var projectIds = projecten.Select(p => p.Id).ToHashSet();

        // ── Projectnamen via listings ─────────────────────────────────────────
        var projectNamen = await _db.MarketListings
            .Where(l => projectIds.Contains(l.MarketAssetId) && l.Title != null)
            .GroupBy(l => l.MarketAssetId)
            .Select(g => new
            {
                AssetId = g.Key,
                Naam = g.OrderByDescending(l => l.LastSeenAt).Select(l => l.Title).First()
            })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.AssetId, x => x.Naam ?? "", ct);

        // ── Units ophalen ─────────────────────────────────────────────────────
        var unitQuery = _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue
                     && projectIds.Contains(a.ParentMarketAssetId.Value));

        if (type == "Appartement")
            unitQuery = unitQuery.Where(a => a.PropertyType == PropertyType.Apartment);
        else if (type == "Woning")
            unitQuery = unitQuery.Where(a => a.PropertyType == PropertyType.House);

        var units = await unitQuery.AsNoTracking().ToListAsync(ct);

        if (units.Count == 0)
            return vm;

        var unitIds = units.Select(u => u.Id).ToHashSet();

        // ── Laatste snapshot per unit via listings ────────────────────────────
        var unitListings = await _db.MarketListings
            .Where(l => unitIds.Contains(l.MarketAssetId) && l.IsActive)
            .Select(l => new { l.Id, l.MarketAssetId })
            .AsNoTracking()
            .ToListAsync(ct);

        var listingIds = unitListings.Select(l => l.Id).ToList();
        var listingIdToAssetId = unitListings.ToDictionary(l => l.Id, l => l.MarketAssetId);

        var allSnapshots = await _db.MarketListingSnapshots
            .Where(s => listingIds.Contains(s.MarketListingId))
            .Select(s => new
            {
                s.MarketListingId,
                s.AskingPrice,
                s.PricePerSqm,
                s.SnapshotDate
            })
            .AsNoTracking()
            .ToListAsync(ct);

        // Groepeer in geheugen per asset-ID: neem de meest recente snapshot
        var prijzenPerUnit = allSnapshots
            .Where(s => listingIdToAssetId.ContainsKey(s.MarketListingId))
            .GroupBy(s => listingIdToAssetId[s.MarketListingId])
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(s => s.SnapshotDate).First();
                    return new UnitSnapshot(latest.AskingPrice, latest.PricePerSqm);
                });

        // ── KPI berekening ────────────────────────────────────────────────────
        var soldCount = units.Count(u => u.SaleStatus == SaleStatus.Sold);
        var availableCount = units.Count(u => u.SaleStatus == SaleStatus.Available);
        var reservedCount = units.Count(u =>
            u.SaleStatus == SaleStatus.Reserved || u.SaleStatus == SaleStatus.Option);

        var prices = units
            .Where(u => prijzenPerUnit.TryGetValue(u.Id, out var s) && s.AskingPrice.HasValue)
            .Select(u => prijzenPerUnit[u.Id].AskingPrice!.Value)
            .ToList();

        var ppSqms = units
            .Where(u => prijzenPerUnit.TryGetValue(u.Id, out var s) && s.PricePerSqm.HasValue)
            .Select(u => prijzenPerUnit[u.Id].PricePerSqm!.Value)
            .ToList();

        var areas = units
            .Where(u => u.LivingArea.HasValue && u.LivingArea > 0)
            .Select(u => u.LivingArea!.Value)
            .ToList();

        vm.Kpi = new GemeenteKpiViewModel
        {
            ActieveProjecten = projecten.Count,
            ActieveUnits = units.Count,
            VerkochteUnits = soldCount,
            BeschikbareUnits = availableCount,
            GereserveerdeUnits = reservedCount,
            GemiddeldePrijs = prices.Count > 0 ? Math.Round(prices.Average(), 0) : null,
            GemiddeldePrijsPerM2 = ppSqms.Count > 0 ? Math.Round(ppSqms.Average(), 0) : null,
            GemiddeldeOppervlakte = areas.Count > 0 ? Math.Round(areas.Average(), 0) : null,
            Verkoopgraad = units.Count > 0
                ? Math.Round((decimal)soldCount / units.Count * 100, 1)
                : 0m
        };

        // ── Grafieken ─────────────────────────────────────────────────────────
        vm.VraagprijsBuckets = BerekeningVraagprijsBuckets(prices);
        vm.PrijsPerM2Buckets = BerekeningPrijsPerM2Buckets(ppSqms);
        vm.VerkoopgraadPerProject = await BerekeningVerkoopgraadPerProjectAsync(projecten, projectNamen, ct);

        // ── Projectentabel ────────────────────────────────────────────────────
        vm.Projecten = BouwProjectenTabel(projecten, units, prijzenPerUnit, projectNamen);

        return vm;
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

    // ── Verkoopgraad per project (KPI of fallback) ────────────────────────────

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
                        ProjectNaam = namen.GetValueOrDefault(k.MarketAssetId) ?? project.AssetKey,
                        Verkoopgraad = k.SoldPercentage,
                        VerkochteUnits = k.UnitsSold,
                        TotaalUnits = k.UnitsTotal
                    };
                })
                .OrderByDescending(p => p.Verkoopgraad)
                .Take(10)
                .ToList();
        }

        // Fallback: bereken uit child units
        var unitCounts = await _db.MarketAssets
            .Where(a => a.ParentMarketAssetId.HasValue
                     && projectIds.Contains(a.ParentMarketAssetId.Value))
            .GroupBy(a => a.ParentMarketAssetId!.Value)
            .Select(g => new
            {
                ProjectId = g.Key,
                Total = g.Count(),
                Sold = g.Count(a => a.SaleStatus == SaleStatus.Sold)
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
                    ProjectNaam = namen.GetValueOrDefault(uc.ProjectId) ?? project.AssetKey,
                    Verkoopgraad = pct,
                    VerkochteUnits = uc.Sold,
                    TotaalUnits = uc.Total
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
        Dictionary<long, UnitSnapshot> prijzenPerUnit,
        Dictionary<long, string> namen)
    {
        return projecten
            .Select(project =>
            {
                var projectUnits = units
                    .Where(u => u.ParentMarketAssetId == project.Id)
                    .ToList();

                var soldCount = projectUnits.Count(u => u.SaleStatus == SaleStatus.Sold);
                var availableCount = projectUnits.Count(u => u.SaleStatus == SaleStatus.Available);

                var unitPrices = projectUnits
                    .Where(u => prijzenPerUnit.TryGetValue(u.Id, out var s) && s.AskingPrice.HasValue)
                    .Select(u => prijzenPerUnit[u.Id].AskingPrice!.Value)
                    .ToList();

                var unitPpSqm = projectUnits
                    .Where(u => prijzenPerUnit.TryGetValue(u.Id, out var s) && s.PricePerSqm.HasValue)
                    .Select(u => prijzenPerUnit[u.Id].PricePerSqm!.Value)
                    .ToList();

                var apartCount = projectUnits.Count(u => u.PropertyType == PropertyType.Apartment);
                var houseCount = projectUnits.Count(u => u.PropertyType == PropertyType.House);
                var typeLabel = (apartCount, houseCount) switch
                {
                    ( > 0, 0) => "Appartement",
                    (0, > 0) => "Woning",
                    ( > 0, > 0) => "Gemengd",
                    _ => "-"
                };

                var pct = projectUnits.Count > 0
                    ? Math.Round((decimal)soldCount / projectUnits.Count * 100, 1)
                    : 0m;

                return new ProjectRijViewModel
                {
                    Id = project.Id,
                    ProjectNaam = namen.GetValueOrDefault(project.Id) ?? project.AssetKey,
                    Ontwikkelaar = project.DeveloperName ?? "-",
                    TypeLabel = typeLabel,
                    TotaalUnits = projectUnits.Count,
                    VerkochteUnits = soldCount,
                    BeschikbareUnits = availableCount,
                    Verkoopgraad = pct,
                    GemiddeldePrijs = unitPrices.Count > 0 ? Math.Round(unitPrices.Average(), 0) : null,
                    GemiddeldePrijsPerM2 = unitPpSqm.Count > 0 ? Math.Round(unitPpSqm.Average(), 0) : null
                };
            })
            .OrderByDescending(p => p.Verkoopgraad)
            .ToList();
    }
}
