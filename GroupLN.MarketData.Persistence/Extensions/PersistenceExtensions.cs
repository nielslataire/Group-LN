using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GroupLN.MarketData.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddMarketDataPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MarketData")
            ?? throw new InvalidOperationException("ConnectionString 'MarketData' is niet geconfigureerd.");

        services.AddDbContext<MarketDataDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(MarketDataDbContext).Assembly.FullName);
                sql.CommandTimeout(120);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sql.UseNetTopologySuite();
            });
        });

        return services;
    }
}
