using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IListingParser
{
    Task<IEnumerable<ListingDto>> ParseListingsAsync(string html, CrawlerSource source);
}
