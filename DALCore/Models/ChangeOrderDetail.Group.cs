#nullable disable
namespace DALCore.Models;

// Migratie 140_ChangeOrderLeveranciersgroepen.sql: de groep (leverancier) waartoe de regel behoort.
public partial class ChangeOrderDetail
{
    public int? GroupId { get; set; }

    public virtual ChangeOrderGroup Group { get; set; }
}
