namespace GroupLN.MarketData.Core.DTOs;

public record RebuildCanonicalResult(
    int ProjectGroupsLoaded,
    int CanonicalProjectsCreated,
    int CanonicalProjectsUpdated,
    int AssetsLinked,
    int PossibleMatchesSkipped);
