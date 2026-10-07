#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

// Migratie 076_BudgetReferentieProjecten.sql: referentieprojecten voor de nacalculatie in de budgetwizard (stap 6).
public partial class BudgetReferentieProject
{
    public int Id { get; set; }

    public string Naam { get; set; }

    /// <summary>Project in de app waaruit de bedragen komen; NULL bij Excel-import.</summary>
    public int? ProjectId { get; set; }

    /// <summary>Peildatum van de bedragen (oplevering / einde uitvoering).</summary>
    public DateTime? Datum { get; set; }

    public int AantalEenheden { get; set; }

    public decimal? OppervlakteGBA { get; set; }

    /// <summary>S-index (lonen) op de peildatum, voor indexatie naar vandaag.</summary>
    public decimal? SIndex { get; set; }

    /// <summary>I2021-index (materialen) op de peildatum.</summary>
    public decimal? IIndex { get; set; }

    /// <summary>"Excel" of "Project".</summary>
    public string Bron { get; set; }

    public string Opmerking { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Project Project { get; set; }

    public virtual ICollection<BudgetReferentieProjectLijn> Lijnen { get; set; } = new List<BudgetReferentieProjectLijn>();
}

public partial class BudgetReferentieProjectLijn
{
    public int Id { get; set; }

    public int ReferentieProjectId { get; set; }

    public int ActivityId { get; set; }

    /// <summary>Werkelijke kost van de activiteit (excl. btw, niet geïndexeerd).</summary>
    public decimal Bedrag { get; set; }

    public string Opmerking { get; set; }

    public virtual BudgetReferentieProject ReferentieProject { get; set; }

    public virtual Activity Activity { get; set; }
}

/// <summary>Welke referentieprojecten een budgetversie gebruikt voor haar nacalc-vergelijking.</summary>
public partial class BudgetVersieNacalcReferentie
{
    public int BudgetVersieId { get; set; }

    public int ReferentieProjectId { get; set; }

    public virtual BudgetVersie BudgetVersie { get; set; }

    public virtual BudgetReferentieProject ReferentieProject { get; set; }
}
