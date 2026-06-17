namespace GroupLN.MarketData.Core.Enums;

public enum AssetLifecycleStatus
{
    Unknown       = 0,
    Available     = 1,
    Reserved      = 2,
    SoldConfirmed = 3,
    LikelySold    = 4,
    Withdrawn     = 5  // Voorlopig niet gebruikt in automatische logica
}
