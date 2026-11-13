#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Kopgegevens van de bouwplanning van een project, één rij per project (migratie 144).</summary>
public partial class ProjectPlanning
{
    public int ProjectId { get; set; }

    /// <summary>Telt op bij elke wijziging (conflictdetectie).</summary>
    public int Versie { get; set; } = 1;

    public DateTime? BasisVastgelegdOp { get; set; }

    /// <summary>Uit = simulatie zonder verlet.</summary>
    public bool VerletMeetellen { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string ModifiedByUserId { get; set; }

    public virtual Project Project { get; set; }
}
