namespace GroupLN.MarketData.Core.DTOs;

/// <summary>
/// Tijdlijn van prijswijzigingen en statusveranderingen voor een canonical project.
/// Combineert data van alle bronprojecten chronologisch.
/// </summary>
public class CanonicalProjectTimelineDto
{
    public long CanonicalProjectId { get; set; }
    public string CanonicalName { get; set; } = "";
    public List<CanonicalProjectTimelineEventDto> Events { get; set; } = new();
}

public class CanonicalProjectTimelineEventDto
{
    public DateTime Date { get; set; }
    public string SourceName { get; set; } = "";
    public string ExternalId { get; set; } = "";

    /// <summary>"PriceChange", "FirstSeen", "Reactivated", "Removed"</summary>
    public string EventType { get; set; } = "";

    public decimal? Price { get; set; }
    public decimal? PreviousPrice { get; set; }
    public decimal? PriceChangeAmount { get; set; }
    public decimal? PriceChangePercentage { get; set; }
    public string? Description { get; set; }
}
