namespace GroupLN.MarketData.Core.DTOs;

public sealed record TitleResolutionResult(
    string Title,
    string TitleSource,
    string? RawStrongTitle = null,
    string? DetectedProjectName = null
);
