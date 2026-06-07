#nullable disable
namespace DALCore.Models;
public partial class KostprijsFormulaKoppeling {
    public int Id { get; set; }
    public string Sleutel { get; set; }
    public string Omschrijving { get; set; }
    public int? MateriaalId { get; set; }
    public virtual KostprijsMateriaal Materiaal { get; set; }
}
