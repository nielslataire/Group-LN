namespace GroupLN.MarketData.Core.Enums;

public enum DeduplicationMatchLevel
{
    Possible = 1,  // score >= 0.60
    Probable = 2,  // score >= 0.80
    Exact    = 3,  // score >= 0.95
}
