using System.Net.Http;
using GroupLN.MarketData.Core.DTOs;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Crawlers.Base;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Crawlers;

// Immoscoop is traditioneler HTML â€” kan met HttpClient + HtmlAgilityPack
// TODO: Implementeer na validatie van Immoscoop's HTML structuur
public class ImmoscoopCrawler : BaseCrawler
{
    private readonly HttpClient _httpClient;

    public ImmoscoopCrawler(
        IMarketListingService listingService,
        IPropertyNormalizer normalizer,
        CrawlerSettings settings,
        IHttpClientFactory httpClientFactory,
        ILogger<ImmoscoopCrawler> logger)
        : base(listingService, normalizer, settings, logger)
    {
        _httpClient = httpClientFactory.CreateClient("MarketDataClient");
    }

    public override string SourceName => "Immoscoop";

    protected override Task<IEnumerable<string>> GetSearchPageUrlsAsync(
        CrawlerSource source, CancellationToken cancellationToken)
    {
        // TODO: Implementeer Immoscoop zoek-URL structuur
        Logger.LogInformation("[Immoscoop] Crawler nog niet geÃ¯mplementeerd â€” wordt overgeslagen.");
        return Task.FromResult(Enumerable.Empty<string>());
    }

    protected override Task<IEnumerable<string>> FetchListingUrlsFromSearchPageAsync(
        string searchPageUrl, CancellationToken cancellationToken)
        => Task.FromResult(Enumerable.Empty<string>());

    protected override Task<ListingDto?> FetchAndParseListingAsync(
        string listingUrl, CrawlerSource source, CancellationToken cancellationToken)
        => Task.FromResult<ListingDto?>(null);
}

