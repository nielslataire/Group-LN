using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroupLN.MarketData.Persistence;

public class MarketDataDbContext : DbContext
{
    public MarketDataDbContext(DbContextOptions<MarketDataDbContext> options) : base(options) { }

    public DbSet<CrawlerSource> CrawlerSources => Set<CrawlerSource>();
    public DbSet<CrawlerRun> CrawlerRuns => Set<CrawlerRun>();

    public DbSet<MarketAsset> MarketAssets => Set<MarketAsset>();
    public DbSet<MarketListing> MarketListings => Set<MarketListing>();
    public DbSet<MarketListingSnapshot> MarketListingSnapshots => Set<MarketListingSnapshot>();
    public DbSet<MarketListingPriceHistory> MarketListingPriceHistories => Set<MarketListingPriceHistory>();
    public DbSet<MarketAssetMatchCandidate> MarketAssetMatchCandidates => Set<MarketAssetMatchCandidate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketDataDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
