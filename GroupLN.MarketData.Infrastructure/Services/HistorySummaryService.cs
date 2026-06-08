using System.Text.Json;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Services;

public class HistorySummaryService : IHistorySummaryService
{
    private readonly MarketDataDbContext _context;
    private readonly ILogger<HistorySummaryService> _logger;

    public HistorySummaryService(MarketDataDbContext context, ILogger<HistorySummaryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task WriteHistorySummaryAsync(DateTime runStartedAt, CancellationToken cancellationToken = default)
    {
        var newProjects = await _context.MarketAssets
            .CountAsync(a => a.IsProjectGroup && a.CreatedAt >= runStartedAt, cancellationToken);

        var newUnits = await _context.MarketAssets
            .CountAsync(a => !a.IsProjectGroup && a.ParentMarketAssetId != null && a.CreatedAt >= runStartedAt, cancellationToken);

        var priceChanges = await _context.MarketListingPriceHistories
            .CountAsync(p => p.DetectedAt >= runStartedAt && p.PreviousPrice != null, cancellationToken);

        var statusChanges = await _context.MarketAssets
            .CountAsync(a => a.StatusChangedAt != null && a.StatusChangedAt >= runStartedAt, cancellationToken);

        var soldUnits = await _context.MarketAssets
            .CountAsync(a => a.FirstSoldAt != null && a.FirstSoldAt >= runStartedAt, cancellationToken);

        // Verwijderde units: listings waarvan het asset een child-unit is
        var removedUnits = await _context.MarketListings
            .Where(l => l.RemovedAt != null && l.RemovedAt >= runStartedAt)
            .Join(_context.MarketAssets,
                l => l.MarketAssetId,
                a => a.Id,
                (l, a) => a)
            .CountAsync(a => !a.IsProjectGroup && a.ParentMarketAssetId != null, cancellationToken);

        // Verwijderde projecten: listings waarvan het asset een projectgroup is
        var removedProjects = await _context.MarketListings
            .Where(l => l.RemovedAt != null && l.RemovedAt >= runStartedAt)
            .Join(_context.MarketAssets,
                l => l.MarketAssetId,
                a => a.Id,
                (l, a) => a)
            .CountAsync(a => a.IsProjectGroup, cancellationToken);

        var summary = new
        {
            crawledAt = runStartedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            newProjects,
            newUnits,
            priceChanges,
            statusChanges,
            soldUnits,
            removedUnits,
            removedProjects
        };

        var dir = Path.Combine(AppContext.BaseDirectory, "debug", "history");
        Directory.CreateDirectory(dir);
        var filename = $"history-summary-{runStartedAt:yyyyMMdd-HHmm}.json";
        var path = Path.Combine(dir, filename);

        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, cancellationToken);

        _logger.LogInformation(
            "HistorySummary geschreven: {Path} | newProjects={NP} | newUnits={NU} | priceChanges={PC} | statusChanges={SC} | soldUnits={SU} | removedUnits={RU} | removedProjects={RP}",
            path, newProjects, newUnits, priceChanges, statusChanges, soldUnits, removedUnits, removedProjects);
    }

    public async Task WriteTopProjectsReportAsync(CancellationToken cancellationToken = default)
    {
        // Meest recente KPI per project via max Id
        var latestKpiIds = await _context.ProjectGroupKpis
            .GroupBy(k => k.MarketAssetId)
            .Select(g => g.Max(k => k.Id))
            .ToListAsync(cancellationToken);

        if (latestKpiIds.Count == 0) return;

        var kpis = await _context.ProjectGroupKpis
            .Where(k => latestKpiIds.Contains(k.Id))
            .Join(_context.MarketAssets,
                k => k.MarketAssetId,
                a => a.Id,
                (k, a) => new
                {
                    k.MarketAssetId,
                    a.ProjectExternalId,
                    a.City,
                    a.PostalCode,
                    a.DeveloperName,
                    k.SnapshotDate,
                    k.UnitsTotal,
                    k.UnitsSold,
                    k.UnitsAvailable,
                    k.UnitsReserved,
                    k.SoldPercentage,
                    k.MinPrice,
                    k.MaxPrice,
                    k.AveragePrice,
                    k.MinPricePerSqm,
                    k.MaxPricePerSqm,
                    k.AveragePricePerSqm,
                    k.MinLivingArea,
                    k.MaxLivingArea,
                    k.AverageLivingArea,
                    k.ApartmentCount,
                    k.HouseCount
                })
            .ToListAsync(cancellationToken);

        static object ToRow(dynamic r) => new
        {
            projectExternalId = r.ProjectExternalId,
            city = r.City,
            postalCode = r.PostalCode,
            developerName = r.DeveloperName,
            snapshotDate = ((DateTime)r.SnapshotDate).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            unitsTotal = r.UnitsTotal,
            unitsSold = r.UnitsSold,
            unitsAvailable = r.UnitsAvailable,
            unitsReserved = r.UnitsReserved,
            soldPercentage = Math.Round((decimal)r.SoldPercentage, 1),
            minPrice = r.MinPrice,
            maxPrice = r.MaxPrice,
            averagePrice = r.AveragePrice,
            minPricePerSqm = r.MinPricePerSqm,
            maxPricePerSqm = r.MaxPricePerSqm,
            averagePricePerSqm = r.AveragePricePerSqm,
            minLivingArea = r.MinLivingArea,
            maxLivingArea = r.MaxLivingArea,
            averageLivingArea = r.AverageLivingArea,
            houseCount = r.HouseCount,
            apartmentCount = r.ApartmentCount
        };

        var report = new
        {
            generatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            totalProjects = kpis.Count,
            byVerkoopgraad = kpis
                .OrderByDescending(k => k.SoldPercentage)
                .ThenByDescending(k => k.UnitsTotal)
                .Select(r => ToRow(r))
                .ToList(),
            byPricePerSqm = kpis
                .Where(k => k.AveragePricePerSqm.HasValue)
                .OrderByDescending(k => k.AveragePricePerSqm)
                .Select(r => ToRow(r))
                .ToList(),
            byProjectSize = kpis
                .OrderByDescending(k => k.UnitsTotal)
                .ThenByDescending(k => k.AveragePricePerSqm)
                .Select(r => ToRow(r))
                .ToList()
        };

        var dir = Path.Combine(AppContext.BaseDirectory, "debug", "kpi");
        Directory.CreateDirectory(dir);
        var reportPath = Path.Combine(dir, "top-projects.json");

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(reportPath, json, cancellationToken);

        _logger.LogInformation(
            "TopProjectsReport geschreven: {Path} | {Count} projecten",
            reportPath, kpis.Count);
    }
}
