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
        builder.Property(x => x.MatchScore).HasPrecision(5, 2);

        builder.HasIndex(x => x.ExistingMarketAssetId);
        builder.HasIndex(x => new { x.SourceId, x.ExternalId })
            .HasDatabaseName("IX_MarketAssetMatchCandidate_Source_ExternalId");
        builder.HasIndex(x => x.IsConfirmed);
        builder.HasIndex(x => x.IsRejected);
        builder.HasIndex(x => x.MatchScore);
    }
}
