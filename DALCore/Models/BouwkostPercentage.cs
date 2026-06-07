#nullable disable
namespace DALCore.Models;
public partial class BouwkostPercentage {
    public int Id { get; set; }
    public int GroepId { get; set; }
    public string Naam { get; set; }
    public decimal Percentage { get; set; }
    public int Volgorde { get; set; }
    public virtual BouwkostPercentageGroep Groep { get; set; }
}
