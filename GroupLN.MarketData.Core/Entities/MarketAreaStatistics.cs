using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Entities;

public class MarketAreaStatistics
{
    public long Id { get; set; }

    public string? PostalCode { get; set; }
    public string? City { get; set; }

    public PropertyType PropertyType { get; set; }

    public DateTime SnapshotDate { get; set; }

    public int ProjectsTotal { get; set; }
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

    public DateTime CreatedAt { get; set; }
}
