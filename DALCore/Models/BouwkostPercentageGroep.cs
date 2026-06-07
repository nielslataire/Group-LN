#nullable disable
namespace DALCore.Models;
public partial class BouwkostPercentageGroep {
    public int Id { get; set; }
    public string Naam { get; set; }
    public int Volgorde { get; set; }
    public virtual ICollection<BouwkostPercentage> Percentages { get; set; } = new HashSet<BouwkostPercentage>();
}
