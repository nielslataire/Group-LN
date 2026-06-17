namespace GroupLN.MarketData.Core.DTOs;

public class CanonicalProjectStatisticsDto
{
    public long CanonicalProjectId { get; set; }
    public string CanonicalName { get; set; } = "";
    public int LinkedAssetCount { get; set; }
    public int TotaalUnits { get; set; }
    public int VerkochteUnits { get; set; }
    public int BeschikbareUnits { get; set; }
    public int GereserveerdeUnits { get; set; }
    public decimal Verkoopgraad { get; set; }
    public decimal? GemiddeldePrijs { get; set; }
    public decimal? GemiddeldePrijsPerM2 { get; set; }
}
