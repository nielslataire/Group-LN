using GroupLN.MarketData.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupLN.MarketData.Persistence.Configurations;

public class MarketAssetMatchCandidateConfiguration : IEntityTypeConfiguration<MarketAssetMatchCandidate>
{
    public void Configure(EntityTypeBuilder<MarketAssetMatchCandidate> builder)
    {
        builder.ToTable("MarketAssetMatchCandidate");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.MatchReason).HasMaxLength(500);
        builder.Property(x => x.MatchScore).HasPrecision(5, 4);
        builder.Property(x => x.MatchType).HasMaxLength(20);
        builder.Property(x => x.MatchLevel).HasMaxLength(20);
        builder.Property(x => x.ComparedFieldsJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.ExistingMarketAssetId);
        builder.HasIndex(x => new { x.SourceId, x.ExternalId })
            .HasDatabaseName("IX_MarketAssetMatchCandidate_Source_ExternalId");
        builder.HasIndex(x => x.CandidateMarketAssetId)
            .HasDatabaseName("IX_MarketAssetMatchCandidate_CandidateAssetId");
        // Unieke combinatie voor deduplicatie-candidates (canonical ordering: lowest=Existing)
        builder.HasIndex(x => new { x.ExistingMarketAssetId, x.CandidateMarketAssetId })
            .HasDatabaseName("IX_MarketAssetMatchCandidate_Pair")
            .HasFilter("[CandidateMarketAssetId] IS NOT NULL");
        builder.HasIndex(x => x.IsConfirmed);
        builder.HasIndex(x => x.IsRejected);
        builder.HasIndex(x => x.MatchScore);
        builder.HasIndex(x => x.MatchType).HasDatabaseName("IX_MarketAssetMatchCandidate_MatchType");
        builder.HasIndex(x => x.MatchLevel).HasDatabaseName("IX_MarketAssetMatchCandidate_MatchLevel");
    }
}
