namespace GroupLN.MarketData.Core.DTOs;

public class AiProjectExtractionResult
{
    public string? ProjectName { get; init; }
    public int ProjectNameConfidence { get; init; }
    public bool IsMarketingTitle { get; init; }
    public string? Street { get; init; }
    public string? HouseNumber { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Developer { get; init; }
    public int? NumberOfUnits { get; init; }
    public string? ExtractedJson { get; init; }
    public bool FromCache { get; init; }
}
