using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Crawlers.Base;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

// ImmoNotaire = notariële verkopen — wekelijkse crawl volstaat
public class ImmoNotaireCrawler : BaseCrawler
{
    public ImmoNotaireCrawler(
        IMarketPropertyService propertyService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        ILogger<ImmoNotaireCrawler> logger)
        : base(propertyService, normalizer, settings, logger) { }

    public override string SourceName => "ImmoNotaire";

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        Logger.LogInformation("[ImmoNotaire] Crawler niet actief in fase 1.");
        return Task.FromResult(Enumerable.Empty<string>());
    }

    protected override Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
        => Task.FromResult(Enumerable.Empty<string>());

    protected override Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken)
        => Task.FromResult<ListingDto?>(null);
}
