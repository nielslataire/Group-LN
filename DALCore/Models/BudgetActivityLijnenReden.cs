#nullable disable
namespace DALCore.Models;

// Migratie 108_BudgetActivityLijnRedenAuteur.sql
public partial class BudgetActivityLijnen
{
    /// <summary>Wie de reden van de correctie (Omschrijving) voor het laatst wijzigde.</summary>
    public string OmschrijvingDoor { get; set; }

    /// <summary>Wanneer (UTC).</summary>
    public System.DateTime? OmschrijvingOp { get; set; }
}
