using System.Text.Json;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Services;

public class MarketAreaStatisticsService : IMarketAreaStatisticsService
{
    private readonly MarketDataDbContext _context;
    private readonly ILogger<MarketAreaStatisticsService> _logger;

    public MarketAreaStatisticsService(MarketDataDbContext context, ILogger<MarketAreaStatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpdateStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Meest recente KPI per project (via max Id)
        var latestKpiIds = await _context.ProjectGroupKpis
            .GroupBy(k => k.MarketAssetId)
            .Select(g => g.Max(k => k.Id))
            .ToListAsync(cancellationToken);

        if (latestKpiIds.Count == 0)
        {
            _logger.LogInformation("MarketAreaStatistics: geen ProjectGroupKpi records gevonden.");
            return;
        }

        // KPI's samenvoegen met asset-locatie en type
        var rows = await _context.ProjectGroupKpis
            .Where(k => latestKpiIds.Contains(k.Id))
            .Join(
                _context.MarketAssets.Where(a => a.IsProjectGroup && a.IsActive && a.PostalCode != null),
                k => k.MarketAssetId,
                a => a.Id,
                (k, a) => new
                {
                    a.PostalCode,
                    a.City,
                    a.PropertySubType,
                    k.UnitsTotal,
                    k.UnitsAvailable,
                    k.UnitsReserved,
                    k.UnitsSold,
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

        if (rows.Count == 0) return;

        // Bepaal het effectieve PropertyType per project en groepeer
        var stats = rows
            .Select(r => new
            {
                r.PostalCode,
                r.City,
                EffectiveType = ToPropertyType(r.PropertySubType, r.ApartmentCount, r.HouseCount),
                r.UnitsTotal,
                r.UnitsAvailable,
                r.UnitsReserved,
                r.UnitsSold,
                r.MinPrice,
                r.MaxPrice,
                r.AveragePrice,
                r.MinPricePerSqm,
                r.MaxPricePerSqm,
                r.AveragePricePerSqm,
                r.MinLivingArea,
                r.MaxLivingArea,
                r.AverageLivingArea
            })
            .GroupBy(r => (r.PostalCode, r.City, r.EffectiveType))
            .Select(g =>
            {
                var totalUnits = g.Sum(r => r.UnitsTotal);
                var soldUnits  = g.Sum(r => r.UnitsSold);

                return new MarketAreaStatistics
                {
                    PostalCode       = g.Key.PostalCode,
                    City             = g.Key.City,
                    PropertyType     = g.Key.EffectiveType,
                    SnapshotDate     = now,
                    ProjectsTotal    = g.Count(),
                    UnitsTotal       = totalUnits,
                    UnitsAvailable   = g.Sum(r => r.UnitsAvailable),
                    UnitsReserved    = g.Sum(r => r.UnitsReserved),
                    UnitsSold        = soldUnits,
                    SoldPercentage   = totalUnits > 0 ? Math.Round((decimal)soldUnits / totalUnits * 100, 4) : 0m,
                    MinPrice         = SafeMin(g.Select(r => r.MinPrice)),
                    MaxPrice         = SafeMax(g.Select(r => r.MaxPrice)),
                    AveragePrice     = SafeAvg(g.Select(r => r.AveragePrice)),
                    MinPricePerSqm   = SafeMin(g.Select(r => r.MinPricePerSqm)),
                    MaxPricePerSqm   = SafeMax(g.Select(r => r.MaxPricePerSqm)),
                    AveragePricePerSqm = SafeAvg(g.Select(r => r.AveragePricePerSqm)),
                    MinLivingArea    = SafeMin(g.Select(r => r.MinLivingArea)),
                    MaxLivingArea    = SafeMax(g.Select(r => r.MaxLivingArea)),
                    AverageLivingArea = SafeAvg(g.Select(r => r.AverageLivingArea)),
                    CreatedAt        = now
                };
            })
            .ToList();

        _context.MarketAreaStatistics.AddRange(stats);
        await _context.SaveChangesAsync(cancellationToken);

        await WriteDebugFilesAsync(stats, cancellationToken);

        _logger.LogInformation(
            "MarketAreaStatistics bijgewerkt: {Count} gebieden ({Postcodes} postcodes).",
            stats.Count,
            stats.Select(s => s.PostalCode).Distinct().Count());
    }

    private async Task WriteDebugFilesAsync(List<MarketAreaStatistics> stats, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "debug", "market-analysis");
        Directory.CreateDirectory(dir);

        var opts = new JsonSerializerOptions { WriteIndented = true };
        int written = 0;

        foreach (var postalGroup in stats
            .Where(s => !string.IsNullOrEmpty(s.PostalCode))
            .GroupBy(s => s.PostalCode!))
        {
            var postalCode = postalGroup.Key;
            var city = postalGroup.First().City;

            var apartments = postalGroup.FirstOrDefault(s => s.PropertyType == PropertyType.Apartment);
            var houses     = postalGroup.FirstOrDefault(s => s.PropertyType == PropertyType.House);

            var doc = new Dictionary<string, object?>
            {
                ["city"]       = city,
                ["postalCode"] = postalCode,
                ["crawledAt"]  = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            if (apartments is not null) doc["apartments"] = ToSection(apartments);
            if (houses     is not null) doc["houses"]     = ToSection(houses);

            foreach (var other in postalGroup.Where(s =>
                s.PropertyType != PropertyType.Apartment &&
                s.PropertyType != PropertyType.House))
            {
                doc[other.PropertyType.ToString().ToLowerInvariant()] = ToSection(other);
            }

            var path = Path.Combine(dir, $"{postalCode}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(doc, opts), cancellationToken);
            written++;
        }

        _logger.LogInformation(
            "MarketAnalysis debug JSON geschreven: {Count} postcode-bestanden in debug/market-analysis/", written);
    }

    private static object ToSection(MarketAreaStatistics s) => new
    {
        projects           = s.ProjectsTotal,
        units              = s.UnitsTotal,
        unitsSold          = s.UnitsSold,
        unitsAvailable     = s.UnitsAvailable,
        unitsReserved      = s.UnitsReserved,
        soldPercentage     = Math.Round(s.SoldPercentage, 2),
        minPrice           = s.MinPrice,
        maxPrice           = s.MaxPrice,
        averagePrice       = s.AveragePrice,
        minPricePerSqm     = s.MinPricePerSqm,
        maxPricePerSqm     = s.MaxPricePerSqm,
        averagePricePerSqm = s.AveragePricePerSqm,
        minLivingArea      = s.MinLivingArea,
        maxLivingArea      = s.MaxLivingArea,
        averageLivingArea  = s.AverageLivingArea
    };

    private static PropertyType ToPropertyType(PropertySubType subType, int apartmentCount, int houseCount) =>
        subType switch
        {
            PropertySubType.ApartmentGroup => PropertyType.Apartment,
            PropertySubType.HouseGroup     => PropertyType.House,
            _ when (apartmentCount + houseCount) > 0 =>
                apartmentCount >= houseCount ? PropertyType.Apartment : PropertyType.House,
            _ => PropertyType.Unknown
        };

    private static decimal? SafeMin(IEnumerable<decimal?> values)
    {
        var v = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return v.Count > 0 ? v.Min() : (decimal?)null;
    }

    private static decimal? SafeMax(IEnumerable<decimal?> values)
    {
        var v = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return v.Count > 0 ? v.Max() : (decimal?)null;
    }

    private static decimal? SafeAvg(IEnumerable<decimal?> values)
    {
        var v = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
        return v.Count > 0 ? Math.Round(v.Average(), 2) : (decimal?)null;
    }
}
