#nullable disable
namespace DALCore.Models;
public partial class BouwkostPercentage {
    public int Id { get; set; }
    public int GroepId { get; set; }
    public string Naam { get; set; }
    public decimal Percentage { get; set; }
    public int Volgorde { get; set; }
    /// <summary>Niet-null voor vaste systeemrijen die als standaard dienen voor Budget &gt; Parameters
    /// (projectcoordinatie / architect / ingenieur). Deze rijen zijn niet hernoembaar/verwijderbaar.</summary>
    public string Sleutel { get; set; }
    public virtual BouwkostPercentageGroep Groep { get; set; }
}
