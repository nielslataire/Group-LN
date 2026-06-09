namespace GroupLN.MarketData.Core.DTOs;

public sealed record DeduplicationSummary(
    int NewAssetsScanned,
    int ProjectCandidatesFound,
    int UnitCandidatesFound,
    int CandidatesSaved
);
