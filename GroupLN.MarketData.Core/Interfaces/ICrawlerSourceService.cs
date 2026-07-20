using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Core.Interfaces;

public interface ICrawlerSourceService
{
    Task<IEnumerable<CrawlerSource>> GetActiveSourcesAsync(CancellationToken cancellationToken = default);

    Task UpdateLastCrawledAsync(int sourceId, CancellationToken cancellationToken = default);

    Task<bool> IsDueCrawlAsync(int sourceId, CancellationToken cancellationToken = default);

    // ── Per-source scheduling (CrawlerSourceStatus) ───────────────────────────
    Task<CrawlerSourceStatus> GetOrCreateStatusAsync(string sourceName, CancellationToken cancellationToken = default);
    Task MarkCrawlStartedAsync(string sourceName, CancellationToken cancellationToken = default);
    Task MarkCrawlSucceededAsync(string sourceName, CrawlerResult result, DateTime nextCrawlAt, CancellationToken cancellationToken = default);
    Task MarkCrawlFailedAsync(string sourceName, string errorMessage, DateTime nextCrawlAt, CancellationToken cancellationToken = default);
}
