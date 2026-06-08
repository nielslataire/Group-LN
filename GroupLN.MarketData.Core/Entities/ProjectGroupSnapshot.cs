namespace GroupLN.MarketData.Core.Entities;

public class ProjectGroupSnapshot
{
    public long Id { get; set; }
    public long MarketAssetId { get; set; }
    public DateTime SnapshotDate { get; set; }

    public int UnitsTotal { get; set; }
    public int UnitsSold { get; set; }
    public int UnitsAvailable { get; set; }
    public int UnitsReserved { get; set; }
    public int UnitsOption { get; set; }
    public int UnitsUnknown { get; set; }
    public decimal SoldPercentage { get; set; }

    public MarketAsset Asset { get; set; } = null!;
}
