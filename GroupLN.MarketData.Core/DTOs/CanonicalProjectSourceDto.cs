namespace GroupLN.MarketData.Core.DTOs;

/// <summary>
/// Één bronproject (MarketAsset) dat gekoppeld is aan een CanonicalProject.
/// CPM Core gebruikt dit om alle bronnen van een project te tonen.
/// </summary>
public class CanonicalProjectSourceDto
{
    public long MarketAssetId { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Title { get; set; }

    public string MatchLevel { get; set; } = "";
    public decimal MatchScore { get; set; }
    public string? MatchReason { get; set; }
    public bool IsPrimary { get; set; }

    public DateTime LastSeenAt { get; set; }
    public DateTime? LastCrawledAt { get; set; }

    public int UnitCount { get; set; }
    public decimal? AskingPrice { get; set; }
    public string? DeveloperName { get; set; }
    public string? AiProjectName { get; set; }
    public int AiProjectNameConfidence { get; set; }
}
