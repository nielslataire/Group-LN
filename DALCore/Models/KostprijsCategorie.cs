#nullable disable
namespace DALCore.Models;
public partial class KostprijsCategorie {
    public int Id { get; set; }
    public string Naam { get; set; }
    public int? LotNummer { get; set; }
    public int Volgorde { get; set; }
}
