namespace GroupLN.MarketData.Core.Interfaces;

public interface ICrawlerFactory
{
    IRealEstateCrawler? GetCrawler(string sourceName);

    IEnumerable<IRealEstateCrawler> GetAllCrawlers();
}
