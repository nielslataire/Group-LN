#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>
/// Eén planning van een project (migratie 145): de hoofdplanning (<see cref="IsHoofd"/>, zonder bouwdeel) of een planning voor een of meer eenheden
/// (woningen of een gebouw, zie <see cref="BouwPlanningEenheid"/>). Alle plannings van een project worden samen doorgerekend.
/// </summary>
public partial class BouwPlanning
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Naam { get; set; }

    public bool IsHoofd { get; set; }

    /// <summary>Verouderd sinds migratie 148: de eenheden van een planning staan in <see cref="Eenheden"/>. Wordt niet meer gebruikt.</summary>
    public int? PaymentGroupId { get; set; }

    /// <summary>Eerste dag waarop de planning mag starten; null = aanvangsdatum van het project.</summary>
    public DateOnly? StartDatum { get; set; }

    public int Volgorde { get; set; }

    public bool IsActief { get; set; } = true;

    /// <summary>Het planningssjabloon waaruit deze planning gemaakt is (migratie 147); null = met de hand opgebouwd.</summary>
    public int? SjabloonId { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Project Project { get; set; }

    public virtual InvoicingPaymentGroup PaymentGroup { get; set; }

    public virtual ICollection<PlanningSectie> PlanningSectie { get; set; } = new List<PlanningSectie>();

    /// <summary>De eenheden (woningen/gebouwen) die tot deze planning behoren; leeg bij de hoofdplanning.</summary>
    public virtual ICollection<BouwPlanningEenheid> Eenheden { get; set; } = new List<BouwPlanningEenheid>();
}
