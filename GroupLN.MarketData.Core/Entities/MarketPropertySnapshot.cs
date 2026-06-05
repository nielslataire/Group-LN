using GroupLN.MarketData.Core.Enums;

namespace GroupLN.MarketData.Core.Entities;

public class MarketPropertySnapshot
{
    public long Id { get; set; }
    public long MarketPropertyId { get; set; }
    public DateTime SnapshotDate { get; set; }

    public decimal? AskingPrice { get; set; }

    public decimal? LivingArea { get; set; }
    public decimal? LandArea { get; set; }

    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? GarageCount { get; set; }

    public int? ConstructionYear { get; set; }

    public decimal? EPCScore { get; set; }
    public EPCLabel? EPCLabel { get; set; }

    public bool IsNewBuild { get; set; }

    public string? DescriptionHash { get; set; }
    public string? RawJson { get; set; }

    public MarketProperty Property { get; set; } = null!;
}
