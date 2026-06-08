namespace GroupLN.MarketData.Core.Entities;

public class ProjectGroupKpi
{
    public long Id { get; set; }
    public long MarketAssetId { get; set; }
    public DateTime SnapshotDate { get; set; }

    public int UnitsTotal { get; set; }
    public int UnitsAvailable { get; set; }
    public int UnitsReserved { get; set; }
    public int UnitsSold { get; set; }

    public decimal SoldPercentage { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? AveragePrice { get; set; }

    public decimal? MinPricePerSqm { get; set; }
    public decimal? MaxPricePerSqm { get; set; }
    public decimal? AveragePricePerSqm { get; set; }

    public decimal? MinLivingArea { get; set; }
    public decimal? MaxLivingArea { get; set; }
    public decimal? AverageLivingArea { get; set; }

    public int ApartmentCount { get; set; }
    public int HouseCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public MarketAsset Asset { get; set; } = null!;
}
