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

    /// <summary>Migratie 077: een code die in een budget gebruikt is, kan niet verwijderd worden — wel gearchiveerd (niet meer kiesbaar op stap 8).</summary>
    public bool Gearchiveerd { get; set; }
}
