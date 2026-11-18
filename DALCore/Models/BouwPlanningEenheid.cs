#nullable disable
namespace DALCore.Models;

/// <summary>
/// Een eenheid (woning, appartement, gebouw) die tot een planning behoort (migratie 148). Een planning bundelt één of meer eenheden;
/// een eenheid zit in hoogstens één planning. Los van de betalingsgroep: eenheden uit dezelfde groep kunnen elk een eigen planning hebben.
/// </summary>
public partial class BouwPlanningEenheid
{
    public int Id { get; set; }

    public int BouwPlanningId { get; set; }

    public int UnitId { get; set; }

    public virtual BouwPlanning BouwPlanning { get; set; }

    public virtual Units Unit { get; set; }
}
