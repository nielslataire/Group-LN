using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GroupLN.MarketData.Infrastructure.Services;

public class CrawlerSourceService : ICrawlerSourceService
{
    private readonly MarketDataDbContext _context;

    public CrawlerSourceService(MarketDataDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CrawlerSource>> GetActiveSourcesAsync(CancellationToken cancellationToken = default)
        => await _context.CrawlerSources
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task UpdateLastCrawledAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        var source = await _context.CrawlerSources.FindAsync(new object[] { sourceId }, cancellationToken);
        if (source is null) return;

        source.LastCrawledAt = DateTime.UtcNow;
        source.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsDueCrawlAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        var source = await _context.CrawlerSources.FindAsync(new object[] { sourceId }, cancellationToken);
        if (source is null || !source.IsActive) return false;
        if (source.LastCrawledAt is null) return true;

        var nextCrawlAt = source.LastCrawledAt.Value.AddHours(source.CrawlFrequencyHours);
        return DateTime.UtcNow >= nextCrawlAt;
    }

    // ── Per-source scheduling ─────────────────────────────────────────────────

    public async Task<CrawlerSourceStatus> GetOrCreateStatusAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        var status = await _context.CrawlerSourceStatuses
            .FirstOrDefaultAsync(s => s.SourceName == sourceName, cancellationToken);

        if (status is null)
        {
            status = new CrawlerSourceStatus
            {
                SourceName = sourceName,
                UpdatedAt  = DateTime.UtcNow
            };
            _context.CrawlerSourceStatuses.Add(status);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return status;
    }

    public async Task MarkCrawlStartedAsync(string sourceName, CancellationToken cancellationToken = default)
    {
        var status = await GetOrCreateStatusAsync(sourceName, cancellationToken);
        status.IsRunning            = true;
        status.LastAttemptedCrawlAt = DateTime.UtcNow;
        status.CurrentPhase         = "Starting";
        status.CurrentProgress      = null;
        status.UpdatedAt            = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCrawlSucceededAsync(
        string sourceName,
        CrawlerResult result,
        DateTime nextCrawlAt,
        CancellationToken cancellationToken = default)
    {
        var status = await GetOrCreateStatusAsync(sourceName, cancellationToken);
        var duration = (int)(DateTime.UtcNow - (status.LastAttemptedCrawlAt ?? DateTime.UtcNow)).TotalSeconds;

        status.IsRunning              = false;
        status.LastSuccessfulCrawlAt  = DateTime.UtcNow;
        status.NextCrawlAt            = nextCrawlAt;
        status.LastDurationSeconds    = duration;
        status.LastResultFound        = result.ListingsFound;
        status.LastResultNew          = result.ListingsCreated;
        status.LastResultUpdated      = result.ListingsUpdated;
        status.LastResultErrors       = result.Errors;
        status.LastErrorMessage       = null;
        status.CurrentPhase           = null;
        status.CurrentProgress        = null;
        status.UpdatedAt              = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCrawlFailedAsync(
        string sourceName,
        string errorMessage,
        DateTime nextCrawlAt,
        CancellationToken cancellationToken = default)
    {
        var status = await GetOrCreateStatusAsync(sourceName, cancellationToken);
        var duration = (int)(DateTime.UtcNow - (status.LastAttemptedCrawlAt ?? DateTime.UtcNow)).TotalSeconds;

        status.IsRunning           = false;
        status.LastFailedCrawlAt   = DateTime.UtcNow;
        status.NextCrawlAt         = nextCrawlAt;
        status.LastDurationSeconds = duration;
        status.LastErrorMessage    = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
        status.CurrentPhase        = null;
        status.CurrentProgress     = null;
        status.UpdatedAt           = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
