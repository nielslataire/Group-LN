#nullable disable
namespace DALCore.Models;
public partial class ProjectKostprijs {
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int KostprijsMateriaalId { get; set; }
    public string CategorieNaam { get; set; }
    public int? LotNummer { get; set; }
    public string Naam { get; set; }
    public string Eenheid { get; set; }
    public string IndexTypeCode { get; set; }
    public decimal Prijs { get; set; }
    public DateTime ReferentieDatum { get; set; }
    public DateTime SnapshotDatum { get; set; }
    public virtual Project Project { get; set; }
    public virtual KostprijsMateriaal KostprijsMateriaal { get; set; }
}
