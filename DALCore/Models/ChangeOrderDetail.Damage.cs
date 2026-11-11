#nullable disable
namespace DALCore.Models;

// Migratie 142_MinwerkSchadevergoeding.sql — schadevergoeding op een minwerk (design-handoff 28v).
public partial class ChangeOrderDetail
{
    /// <summary>Percentage schadevergoeding op dit minwerk; NULL = geen. Intern: de klant ziet enkel het eindbedrag.</summary>
    public decimal? DamagePercentage { get; set; }

    /// <summary>1 = bouwheer (de leverancier crediteert de volle waarde), 2 = leverancier (crediteert enkel wat de klant terugkrijgt).</summary>
    public byte? DamageGoesTo { get; set; }

    /// <summary>Eenheidsprijs van de waarde van het minwerk vóór de vergoeding (positief); Price is het eindbedrag voor de klant (negatief).</summary>
    public decimal? ValuePrice { get; set; }
}
