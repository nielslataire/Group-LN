using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroupLN.MarketData.Persistence;

public class MarketDataDbContext : DbContext
{
    public MarketDataDbContext(DbContextOptions<MarketDataDbContext> options) : base(options) { }

    public DbSet<CrawlerSource> CrawlerSources => Set<CrawlerSource>();
    public DbSet<CrawlerRun> CrawlerRuns => Set<CrawlerRun>();
    public DbSet<MarketProperty> MarketProperties => Set<MarketProperty>();
    public DbSet<MarketPropertySnapshot> MarketPropertySnapshots => Set<MarketPropertySnapshot>();
    public DbSet<MarketPropertyPriceHistory> MarketPropertyPriceHistories => Set<MarketPropertyPriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketDataDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
