namespace GroupLN.MarketData.Core.Entities;

/// <summary>
/// Bijhoudt de crawl-status en planning per bron (Immoweb, Zimmo, ...).
/// Één rij per unieke SourceName. Wordt aangemaakt bij eerste crawl.
/// </summary>
public class CrawlerSourceStatus
{
    public int Id { get; set; }

    /// <summary>Naam van de bron — exact gelijk aan CrawlerSource.Name.</summary>
    public string SourceName { get; set; } = "";

    public DateTime? LastAttemptedCrawlAt { get; set; }
    public DateTime? LastSuccessfulCrawlAt { get; set; }
    public DateTime? LastFailedCrawlAt { get; set; }

    /// <summary>Gepland tijdstip voor de volgende crawl. Wordt gerespecteerd bij app-restart.</summary>
    public DateTime? NextCrawlAt { get; set; }

    public int? LastDurationSeconds { get; set; }
    public int? LastResultFound { get; set; }
    public int? LastResultNew { get; set; }
    public int? LastResultUpdated { get; set; }
    public int? LastResultErrors { get; set; }
    public string? LastErrorMessage { get; set; }

    public bool IsRunning { get; set; }
    public string? CurrentPhase { get; set; }
    public string? CurrentProgress { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
