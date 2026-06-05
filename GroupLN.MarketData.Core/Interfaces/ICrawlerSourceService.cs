using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Core.Interfaces;

public interface ICrawlerSourceService
{
    Task<IEnumerable<CrawlerSource>> GetActiveSourcesAsync(CancellationToken cancellationToken = default);

    Task UpdateLastCrawledAsync(int sourceId, CancellationToken cancellationToken = default);

    Task<bool> IsDueCrawlAsync(int sourceId, CancellationToken cancellationToken = default);
}
