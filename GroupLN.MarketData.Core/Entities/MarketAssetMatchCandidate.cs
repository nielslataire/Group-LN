namespace GroupLN.MarketData.Core.Entities;

public class MarketAssetMatchCandidate
{
    public long Id { get; set; }
    public long ExistingMarketAssetId { get; set; }
    public int SourceId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public decimal MatchScore { get; set; }
    public string? MatchReason { get; set; }

    public bool IsConfirmed { get; set; }
    public bool IsRejected { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public MarketAsset ExistingAsset { get; set; } = null!;
}
