namespace GroupLN.MarketData.Core.DTOs;

public class PriceChangeDto
{
    public long MarketPropertyId { get; set; }
    public string Url { get; set; } = string.Empty;
    public decimal NewPrice { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal? ChangeAmount { get; set; }
    public decimal? ChangePercentage { get; set; }
    public DateTime DetectedAt { get; set; }
}
