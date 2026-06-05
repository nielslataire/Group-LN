using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Crawlers.Base;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

// Niet actief in fase 1 â€” structuur klaar voor activatie
public class ImmovlanCrawler : BaseCrawler
{
    public ImmovlanCrawler(
        IMarketListingService listingService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        ILogger<ImmovlanCrawler> logger)
        : base(listingService, normalizer, settings, logger) { }

    public override string SourceName => "Immovlan";

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        Logger.LogInformation("[Immovlan] Crawler niet actief in fase 1.");
        return Task.FromResult(Enumerable.Empty<string>());
    }

    protected override Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
        => Task.FromResult(Enumerable.Empty<string>());

    protected override Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken)
        => Task.FromResult<ListingDto?>(null);
}

