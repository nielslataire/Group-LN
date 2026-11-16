#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>
/// Eén planning van een project (migratie 145): de hoofdplanning (<see cref="IsHoofd"/>, zonder bouwdeel) of de planning van een
/// bouwdeel (woning of gebouw = <see cref="InvoicingPaymentGroup"/>). Alle plannings van een project worden samen doorgerekend.
/// </summary>
public partial class BouwPlanning
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Naam { get; set; }

    public bool IsHoofd { get; set; }

    /// <summary>Het bouwdeel (betalingsgroep) van deze planning; null bij de hoofdplanning.</summary>
    public int? PaymentGroupId { get; set; }

    /// <summary>Eerste dag waarop de planning mag starten; null = aanvangsdatum van het project.</summary>
    public DateOnly? StartDatum { get; set; }

    public int Volgorde { get; set; }

    public bool IsActief { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public virtual Project Project { get; set; }

    public virtual InvoicingPaymentGroup PaymentGroup { get; set; }

    public virtual ICollection<PlanningSectie> PlanningSectie { get; set; } = new List<PlanningSectie>();
}
