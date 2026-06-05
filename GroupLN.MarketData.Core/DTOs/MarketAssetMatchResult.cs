using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.DTOs;

public class MarketAssetMatchResult
{
    public AssetMatchType AssetMatchType { get; set; }
    public long? MatchedAssetId { get; set; }
    public decimal MatchScore { get; set; }
    public string? MatchReason { get; set; }
    public IList<long> CandidateAssets { get; set; } = new List<long>();
}

