using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Browser;
using GroupLN.MarketData.Infrastructure.Crawlers;
using GroupLN.MarketData.Infrastructure.Factories;
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
        services.AddScoped<IMarketPropertyService, MarketPropertyService>();
        services.AddScoped<ICrawlerRunService, CrawlerRunService>();
        services.AddScoped<ICrawlerSourceService, CrawlerSourceService>();

        // Crawlers — allemaal geregistreerd als IRealEstateCrawler
        services.AddScoped<IRealEstateCrawler, ImmowebCrawler>();
        services.AddScoped<IRealEstateCrawler, ZimmoCrawler>();
        services.AddScoped<IRealEstateCrawler, ImmoscoopCrawler>();
        services.AddScoped<IRealEstateCrawler, ImmovlanCrawler>();
        services.AddScoped<IRealEstateCrawler, RealoCrawler>();
        services.AddScoped<IRealEstateCrawler, BidditCrawler>();
        services.AddScoped<IRealEstateCrawler, ImmoNotaireCrawler>();

        // Factory haalt alle crawlers op via IEnumerable<IRealEstateCrawler>
        services.AddScoped<ICrawlerFactory, CrawlerFactory>();

        return services;
    }
}
