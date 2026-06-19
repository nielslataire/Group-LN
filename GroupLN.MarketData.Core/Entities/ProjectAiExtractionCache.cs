namespace GroupLN.MarketData.Core.Entities;

public class ProjectAiExtractionCache
{
    public long Id { get; set; }

    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? Url { get; set; }

    // SHA-256 van de gecombineerde input — voorkomt dubbele API-aanroepen voor identieke input
    public string InputHash { get; set; } = "";
    public string Model { get; set; } = "";

    public string? RawTitle { get; set; }
    public string? ExtractedProjectName { get; set; }
    public int ProjectNameConfidence { get; set; }
    public bool IsMarketingTitle { get; set; }

    public string? ExtractedStreet { get; set; }
    public string? ExtractedHouseNumber { get; set; }
    public string? ExtractedPostalCode { get; set; }
    public string? ExtractedCity { get; set; }
    public string? ExtractedDeveloper { get; set; }
    public int? ExtractedNumberOfUnits { get; set; }

    // Volledig JSON-antwoord van de AI voor auditing
    public string? ExtractedJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
