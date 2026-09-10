#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>
/// Instantie van een traject voor één project (1:1 met <see cref="Project"/>). Aangemaakt uit een
/// <see cref="TrajectSjabloon"/> en bevat de concrete <see cref="ProjecttrajectFase"/>s en <see cref="Mijlpaal"/>s.
/// </summary>
public partial class Projecttraject
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int? TrajectSjabloonId { get; set; }

    public string Naam { get; set; }

    /// <summary>BOCore.Traject.TrajectStatus.</summary>
    public int Status { get; set; }

    public DateOnly? GestartOp { get; set; }

    public DateOnly? AfgerondOp { get; set; }

    public DateTime? HerberekendOp { get; set; }

    /// <summary>Pipe-gescheiden waarschuwingscodes (zelfde patroon als <c>ProjectVoortgang.Warnings</c>).</summary>
    public string Waarschuwingen { get; set; }

    public DateTime CreatedDate { get; set; }

    public string CreatedByUserId { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string ModifiedByUserId { get; set; }

    public virtual Project Project { get; set; }

    public virtual TrajectSjabloon TrajectSjabloon { get; set; }

    public virtual ICollection<ProjecttrajectFase> Fases { get; set; } = new List<ProjecttrajectFase>();

    public virtual ICollection<Mijlpaal> Mijlpalen { get; set; } = new List<Mijlpaal>();
}
