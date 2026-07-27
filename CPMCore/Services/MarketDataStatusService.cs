using CPMCore.Models.Instellingen;
using GroupLN.MarketData.Core.Enums;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services;

public class MarketDataStatusService : IMarketDataStatusService
{
    private readonly MarketDataDbContext _db;

    public MarketDataStatusService(MarketDataDbContext db)
    {
        _db = db;
    }

    public async Task<MarketDataStatusModel> GetStatusAsync(int recenteRunsAantal = 50, CancellationToken ct = default)
    {
        var sources = await _db.CrawlerSources
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        var statussen = await _db.CrawlerSourceStatuses
            .AsNoTracking()
            .ToDictionaryAsync(s => s.SourceName, ct);

        var sourceVms = sources.Select(source =>
        {
            statussen.TryGetValue(source.Name, out var status);
            return new MarketDataSourceStatusVM
            {
                SourceName = source.Name,
                IsActive = source.IsActive,
                IsRunning = status?.IsRunning ?? false,
                CurrentPhase = status?.CurrentPhase,
                CurrentProgress = status?.CurrentProgress,
                LastAttemptedCrawlAt = status?.LastAttemptedCrawlAt,
                LastSuccessfulCrawlAt = status?.LastSuccessfulCrawlAt,
                LastFailedCrawlAt = status?.LastFailedCrawlAt,
                NextCrawlAt = status?.NextCrawlAt,
                LastDurationSeconds = status?.LastDurationSeconds,
                LastResultFound = status?.LastResultFound,
                LastResultNew = status?.LastResultNew,
                LastResultUpdated = status?.LastResultUpdated,
                LastResultErrors = status?.LastResultErrors,
                LastErrorMessage = status?.LastErrorMessage
            };
        }).ToList();

        var recenteRuns = await _db.CrawlerRuns
            .AsNoTracking()
            .Include(r => r.Source)
            .OrderByDescending(r => r.StartedAt)
            .Take(recenteRunsAantal)
            .Select(r => new MarketDataRunVM
            {
                Id = r.Id,
                SourceName = r.Source.Name,
                StartedAt = r.StartedAt,
                FinishedAt = r.FinishedAt,
                Status = ToStatusLabel(r.Status),
                ListingsFound = r.ListingsFound,
                ListingsCreated = r.ListingsCreated,
                ListingsUpdated = r.ListingsUpdated,
                Errors = r.Errors,
                LogMessage = r.LogMessage
            })
            .ToListAsync(ct);

        return new MarketDataStatusModel
        {
            Sources = sourceVms,
            RecenteRuns = recenteRuns
        };
    }

    private static string ToStatusLabel(CrawlerStatus status) => status switch
    {
        CrawlerStatus.Running => "Bezig",
        CrawlerStatus.Completed => "Voltooid",
        CrawlerStatus.Failed => "Mislukt",
        CrawlerStatus.PartialSuccess => "Gedeeltelijk",
        _ => status.ToString()
    };
}
