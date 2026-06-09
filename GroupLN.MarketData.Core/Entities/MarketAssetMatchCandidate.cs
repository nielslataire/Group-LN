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

    // Kandidaat-asset (potentiële duplicate); null bij import-time candidates
    public long? CandidateMarketAssetId { get; set; }

    // "Project" of "Unit"
    public string? MatchType { get; set; }
    // "Exact", "Probable" of "Possible"
    public string? MatchLevel { get; set; }

    // JSON met vergelijkingsdetails
    public string? ComparedFieldsJson { get; set; }

    public bool IsConfirmed { get; set; }
    public bool IsRejected { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public MarketAsset ExistingAsset { get; set; } = null!;
}
