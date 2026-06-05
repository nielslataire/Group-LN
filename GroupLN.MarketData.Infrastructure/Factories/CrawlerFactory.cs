using GroupLN.MarketData.Core.Interfaces;

namespace GroupLN.MarketData.Infrastructure.Factories;

public class CrawlerFactory : ICrawlerFactory
{
    private readonly IEnumerable<IRealEstateCrawler> _crawlers;

    public CrawlerFactory(IEnumerable<IRealEstateCrawler> crawlers)
    {
        _crawlers = crawlers;
    }

    public IRealEstateCrawler? GetCrawler(string sourceName)
        => _crawlers.FirstOrDefault(c =>
            c.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<IRealEstateCrawler> GetAllCrawlers()
        => _crawlers;
}
