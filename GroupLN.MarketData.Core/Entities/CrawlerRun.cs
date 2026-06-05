using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Entities;

public class CrawlerRun
{
    public long Id { get; set; }
    public int SourceId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public CrawlerStatus Status { get; set; }
    public int ListingsFound { get; set; }
    public int ListingsCreated { get; set; }
    public int ListingsUpdated { get; set; }
    public int Errors { get; set; }
    public string? LogMessage { get; set; }

    public CrawlerSource Source { get; set; } = null!;
}
