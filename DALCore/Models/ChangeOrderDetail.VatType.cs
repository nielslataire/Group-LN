#nullable disable
namespace DALCore.Models;

// Migratie 073_ChangeOrderDetailVatType.sql: gekozen btw-code (Vattype) van het facturatiebedrijf.
public partial class ChangeOrderDetail
{
    /// <summary>Gekozen btw-code (Vattype.Id); VatPercentage is het daaruit afgeleide percentage.</summary>
    public int? VatTypeId { get; set; }
}
