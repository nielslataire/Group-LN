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
}
