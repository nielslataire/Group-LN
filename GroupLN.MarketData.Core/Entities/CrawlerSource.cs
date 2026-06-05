namespace GroupLN.MarketData.Core.Entities;

public class CrawlerSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int CrawlFrequencyHours { get; set; } = 24;
    public DateTime? LastCrawledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CrawlerRun> Runs { get; set; } = new List<CrawlerRun>();
    public ICollection<MarketProperty> Properties { get; set; } = new List<MarketProperty>();
}
