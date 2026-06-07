#nullable disable
namespace DALCore.Models;
public partial class KostprijsMateriaal {
    public int Id { get; set; }
    public int CategorieId { get; set; }
    public string Naam { get; set; }
    public string Eenheid { get; set; }
    public string Toepassingsgebied { get; set; }
    public int KmIndexTypeId { get; set; }
    public decimal ReferentiePrijs { get; set; }
    public DateTime ReferentieDatum { get; set; }
    public int Volgorde { get; set; }
    public virtual ActivityGroup Categorie { get; set; }
    public virtual KmIndexType IndexType { get; set; }
    public virtual ICollection<ProjectKostprijs> ProjectKostprijzen { get; set; } = new HashSet<ProjectKostprijs>();
}
