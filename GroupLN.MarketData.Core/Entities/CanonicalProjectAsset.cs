namespace GroupLN.MarketData.Core.Entities;

public class CanonicalProjectAsset
{
    public long Id { get; set; }
    public long CanonicalProjectId { get; set; }
    public long MarketAssetId { get; set; }

    // "Exact", "Probable", "Primary" (solo asset), "Manual"
    public string MatchLevel { get; set; } = "";
    public decimal MatchScore { get; set; }
    public string? MatchReason { get; set; }

    // "DeduplicationMatch", "Primary", "Manual"
    public string? Source { get; set; }

    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }

    public CanonicalProject CanonicalProject { get; set; } = null!;
    public MarketAsset MarketAsset { get; set; } = null!;
}
