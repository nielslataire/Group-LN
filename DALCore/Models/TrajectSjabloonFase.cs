#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Fase binnen een <see cref="TrajectSjabloon"/>.</summary>
public partial class TrajectSjabloonFase
{
    public int Id { get; set; }

    public int TrajectSjabloonId { get; set; }

    public string Naam { get; set; }

    /// <summary>Stabiele sleutel (bv. AANKOOP, VERGUNNING, UITVOERING) — gebruikt bij instantiatie en anker-koppeling.</summary>
    public string Code { get; set; }

    public int Volgorde { get; set; }

    public string KleurCode { get; set; }

    /// <summary>Optionele ProjectStatus (BOCore.ProjectStatusType) die actief wordt bij het betreden van deze fase.</summary>
    public int? StandaardProjectStatusId { get; set; }

    public virtual TrajectSjabloon TrajectSjabloon { get; set; }

    public virtual ICollection<TrajectSjabloonMijlpaal> Mijlpalen { get; set; } = new List<TrajectSjabloonMijlpaal>();
}
