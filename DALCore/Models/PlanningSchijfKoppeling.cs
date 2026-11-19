#nullable disable
namespace DALCore.Models;

/// <summary>
/// Koppelt een betalingsschijf per planning aan een taak of fase (migratie 151). De eenheden van die planning bereiken de schijf
/// zodra de taak of fase klaar is. Hoogstens één koppeling per schijf en planning. <see cref="BouwPlanningId"/>, <see cref="TaakId"/>
/// en <see cref="SectieId"/> zijn gewone kolommen zonder foreign key: een verwijderde taak laat een verweesde koppeling achter die genegeerd wordt.
/// </summary>
public partial class PlanningSchijfKoppeling
{
    public int Id { get; set; }

    public int PaymentStageId { get; set; }

    public int BouwPlanningId { get; set; }

    public int? TaakId { get; set; }

    public int? SectieId { get; set; }

    public virtual InvoicingPaymentStages PaymentStage { get; set; }
}
