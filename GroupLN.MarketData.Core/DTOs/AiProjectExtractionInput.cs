namespace GroupLN.MarketData.Core.DTOs;

public class AiProjectExtractionInput
{
    public string SourceName { get; init; } = "";
    public string ExternalId { get; init; } = "";
    public string? Url { get; init; }
    public string? RawTitle { get; init; }
    public string? MetaTitle { get; init; }
    public string? OgTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? BodyText { get; init; }
    public string? Address { get; init; }
    public string? UnitTableText { get; init; }
    public string? Developer { get; init; }
}
