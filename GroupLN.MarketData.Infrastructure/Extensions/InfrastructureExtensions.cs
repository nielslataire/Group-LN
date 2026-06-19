using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Browser;
using GroupLN.MarketData.Infrastructure.Commands;
using GroupLN.MarketData.Infrastructure.Crawlers;
using GroupLN.MarketData.Infrastructure.Deduplication;
using GroupLN.MarketData.Infrastructure.Factories;
using GroupLN.MarketData.Infrastructure.GeoLocation;
using GroupLN.MarketData.Infrastructure.Normalizers;
using GroupLN.MarketData.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroupLN.MarketData.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddMarketDataInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Instellingen
        var settings = new CrawlerSettings();
        configuration.GetSection(CrawlerSettings.Section).Bind(settings);
        services.AddSingleton(settings);

        // Anthropic API client (voor AI project extractie)
        services.AddHttpClient("AnthropicClient", client =>
        {
            client.BaseAddress = new Uri("https://api.anthropic.com/");
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            client.DefaultRequestHeaders.Add("anthropic-beta", "messages-2023-12-15");
            client.Timeout = TimeSpan.FromSeconds(settings.AiExtraction.AiExtractionTimeoutSeconds);
        });

        // HTTP client met retry + timeout
        services.AddHttpClient("MarketDataClient", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "nl-BE,nl;q=0.9,fr;q=0.8");
            client.Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = settings.MaxRetryAttempts;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds * 2);
        });

        // Playwright browser service (singleton — één browser-instantie)
        services.AddSingleton<PlaywrightBrowserService>();

        // Normalizer
        services.AddScoped<IPropertyNormalizer, PropertyNormalizer>();

        // Services
        services.AddScoped<IMarketAssetMatcher, MarketAssetMatcherService>();
        services.AddScoped<IMarketListingService, MarketListingService>();
        services.AddScoped<ICrawlerRunService, CrawlerRunService>();
        services.AddScoped<ICrawlerSourceService, CrawlerSourceService>();
        services.AddScoped<IHistorySummaryService, HistorySummaryService>();
        services.AddScoped<IMarketAreaStatisticsService, MarketAreaStatisticsService>();

        // Crawlers — allemaal geregistreerd als IRealEstateCrawler
        services.AddScoped<IRealEstateCrawler, ImmowebCrawler>();
        services.AddScoped<IRealEstateCrawler, ZimmoCrawler>();
        services.AddScoped<IRealEstateCrawler, ImmoscoopCrawler>();
        services.AddScoped<IRealEstateCrawler, ImmovlanCrawler>();
        services.AddScoped<IRealEstateCrawler, RealoCrawler>();
        services.AddScoped<IRealEstateCrawler, BidditCrawler>();
        services.AddScoped<IRealEstateCrawler, ImmoNotaireCrawler>();
        services.AddScoped<IRealEstateCrawler, ZimmoDiscoveryCrawler>();

        // Factory haalt alle crawlers op via IEnumerable<IRealEstateCrawler>
        services.AddScoped<ICrawlerFactory, CrawlerFactory>();

        // Photo hashing + AI extractie
        services.AddScoped<IProjectPhotoHashService, ProjectPhotoHashService>();
        services.AddScoped<IAiProjectExtractionService, AnthropicProjectExtractionService>();

        // Deduplicatie
        services.AddScoped<IDeduplicationService, DeduplicationService>();

        // Geo-locatie
        services.AddScoped<ILocationResolver, LocationResolver>();
        services.AddScoped<IAdminVectorImportService, AdminVectorImportService>();
        services.AddScoped<BackfillMarketAssetLocationsCommand>();

        // Geocoding (adres → coördinaten)
        services.AddHttpClient("Nominatim", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GroupLN-MarketData/1.0");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IGeocodingService, NominatimGeocodingService>();
        services.AddScoped<GeocodingEnrichmentService>();

        // Canonical Projects
        services.AddScoped<ICanonicalProjectService, CanonicalProjectService>();
        services.AddScoped<RebuildCanonicalProjectsCommand>();

        // Database cleanup
        services.AddScoped<ResetMarketDataCommand>();

        // Tijdelijke tests
        services.AddScoped<ZimmoDetailDiscoveryTest>();
        services.AddScoped<ZimmoDirectDetailTest>();

        return services;
    }
}
