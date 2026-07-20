namespace GroupLN.MarketData.Core.DTOs;

public class LooseListingMatchResult
{
    public int CandidatesEvaluated { get; set; }
    public int Matched { get; set; }
    public int Ambiguous { get; set; }
    public int Unmatched { get; set; }
}
