#nullable disable
using System;

namespace DALCore.Models;

// Migratie 078_BudgetVersieStatus.sql: versiestatus (Concept → Afgerond → Definitief), voortgang en aanpasbare VMSW-factoren.
public partial class BudgetVersie
{
    public const string StatusConcept    = "Concept";
    public const string StatusAfgerond   = "Afgerond";
    public const string StatusDefinitief = "Definitief";

    /// <summary>Laatste opgeslagen wijziging in de wizard (migratie 080); NULL = sinds aanmaak niet gewijzigd.</summary>
    public DateTime? GewijzigdOp { get; set; }

    /// <summary>Bewaarde totale kostprijs voor het overzicht (migratie 080); NULL = nog niet berekend of verouderd.</summary>
    public decimal? TotaalKosten { get; set; }

    public DateTime? VastgezetOp { get; set; }
    public string VastgezetDoor { get; set; }

    /// <summary>Hoogste bereikte wizardstap (1-9).</summary>
    public byte? LaatsteStap { get; set; }

    /// <summary>JSON met de 13 reductiefactoren (BOCore.Budget.VmswFactorenBO); NULL = standaard VMSW.</summary>
    public string VmswFactoren { get; set; }

    /// <summary>Bevestigde aandachtspunten, kommagescheiden sleutels (bv. "decennale").</summary>
    public string WaarschuwingenBevestigd { get; set; }
}
