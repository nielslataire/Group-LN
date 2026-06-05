using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IRealEstateCrawler
{
    string SourceName { get; }

    Task<CrawlerResult> CrawlAsync(CrawlerSource source, CancellationToken cancellationToken);
}
