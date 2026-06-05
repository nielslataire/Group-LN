using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Browser;
using GroupLN.MarketData.Infrastructure.Crawlers.Base;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

// Zimmo gebruikt een JavaScript-zware interface â€” Playwright vereist
// TODO: Implementeer na validatie van Zimmo's data structuur
public class ZimmoCrawler : BaseCrawler
{
    private readonly PlaywrightBrowserService _browser;

    public ZimmoCrawler(
        IMarketListingService listingService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        PlaywrightBrowserService browser,
        ILogger<ZimmoCrawler> logger)
        : base(listingService, normalizer, settings, logger)
    {
        _browser = browser;
    }

    public override string SourceName => "Zimmo";

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        // TODO: Implementeer Zimmo zoek-URL structuur
        // Voorbeeld: https://www.zimmo.be/nl/te-koop/?page=1
        Logger.LogInformation("[Zimmo] Crawler nog niet geÃ¯mplementeerd â€” wordt overgeslagen.");
        return Task.FromResult(Enumerable.Empty<string>());
    }

    protected override Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
        => Task.FromResult(Enumerable.Empty<string>());

    protected override Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken)
        => Task.FromResult<ListingDto?>(null);
}

