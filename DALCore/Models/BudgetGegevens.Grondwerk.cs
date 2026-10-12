#nullable disable
namespace DALCore.Models;

// Migratie 089_BudgetGegevensGrondwerk.sql: herleidbare funderingsoppervlakte en automatisch grondwerk.
public partial class BudgetGegevens
{
    /// <summary>JSON [{omschrijving, aantal, oppervlakte}]; de som is "Oppervlakte funderingen". NULL/[] = rechtstreeks ingevuld.</summary>
    public string FunderingLijnen { get; set; }

    /// <summary>Totale oppervlakte van alle ondergrondse verdiepingen in m2 (NULL = aantal x oppervlakte funderingen).</summary>
    public decimal? OppOndergronds { get; set; }

    /// <summary>JSON [{omschrijving, aantal, oppervlakte}]; de som is OppOndergronds.</summary>
    public string OndergrondsLijnen { get; set; }

    /// <summary>Diepte gewone fundering in cm (NULL = standaard 40).</summary>
    public decimal? DiepteFunderingCm { get; set; }

    /// <summary>Diepte per ondergrondse verdieping in cm (NULL = standaard 300).</summary>
    public decimal? DiepteOndergrondsCm { get; set; }
}
