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
    public DbSet<ProjectGroupSnapshot> ProjectGroupSnapshots => Set<ProjectGroupSnapshot>();
    public DbSet<ProjectGroupKpi> ProjectGroupKpis => Set<ProjectGroupKpi>();
    public DbSet<MarketAreaStatistics> MarketAreaStatistics => Set<MarketAreaStatistics>();

    public DbSet<GeoMunicipality> GeoMunicipalities => Set<GeoMunicipality>();
    public DbSet<GeoMunicipalSection> GeoMunicipalSections => Set<GeoMunicipalSection>();

    public DbSet<CanonicalProject> CanonicalProjects => Set<CanonicalProject>();
    public DbSet<CanonicalProjectAsset> CanonicalProjectAssets => Set<CanonicalProjectAsset>();
    public DbSet<CanonicalUnit> CanonicalUnits => Set<CanonicalUnit>();
    public DbSet<CanonicalUnitAsset> CanonicalUnitAssets => Set<CanonicalUnitAsset>();

    public DbSet<CrawlerSourceStatus> CrawlerSourceStatuses => Set<CrawlerSourceStatus>();

    public DbSet<GeocodingCache> GeocodingCaches => Set<GeocodingCache>();

    public DbSet<ProjectPhotoHash> ProjectPhotoHashes => Set<ProjectPhotoHash>();
    public DbSet<ProjectAiExtractionCache> ProjectAiExtractionCaches => Set<ProjectAiExtractionCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketDataDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
