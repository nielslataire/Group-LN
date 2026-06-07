#nullable disable
namespace DALCore.Models;
public partial class KmIndexType {
    public int Id { get; set; }
    public string Code { get; set; }
    public string Naam { get; set; }
    public virtual ICollection<KostprijsMateriaal> Materialen { get; set; } = new HashSet<KostprijsMateriaal>();
}
