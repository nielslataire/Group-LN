#nullable disable
namespace DALCore.Models;

/// <summary>Eigen kostenpost bij de kosten koper van een project (Verkoopinstellingen, "Kost toevoegen", migratie 115).</summary>
public partial class ProjectSalesExtraCost
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public string Note { get; set; }
    public int SortOrder { get; set; }
}
