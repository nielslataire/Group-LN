namespace GroupLN.MarketData.Core.DTOs;

public class CrawlerResult
{
    public bool Success { get; set; }
    public int ListingsFound { get; set; }
    public int ListingsCreated { get; set; }
    public int ListingsUpdated { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public string? Message { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
