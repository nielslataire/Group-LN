#nullable disable
using System;

namespace DALCore.Models;

// Migratie 075_BudgetVerkoopPrijsPerM2.sql: herleidbaarheid van een (manueel opgezochte) prijsreferentie.
public partial class BudgetPrijsReferentie
{
    /// <summary>Datum waarop de referentie gold of opgezocht werd.</summary>
    public DateTime? Datum { get; set; }

    /// <summary>Waar de €/m² vandaan komt, bv. "verkoop Keerstraat lot 3, 06/2026" of "mediaan markt Gent".</summary>
    public string Bron { get; set; }
}
