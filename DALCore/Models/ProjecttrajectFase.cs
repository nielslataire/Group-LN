#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Concrete fase binnen een <see cref="Projecttraject"/>.</summary>
public partial class ProjecttrajectFase
{
    public int Id { get; set; }

    public int ProjecttrajectId { get; set; }

    public int? SjabloonFaseId { get; set; }

    public string Naam { get; set; }

    public string Code { get; set; }

    public int Volgorde { get; set; }

    /// <summary>BOCore.Traject.FaseStatus.</summary>
    public int Status { get; set; }

    public DateOnly? StartGepland { get; set; }

    public DateOnly? StartWerkelijk { get; set; }

    public DateOnly? EindGepland { get; set; }

    public DateOnly? EindWerkelijk { get; set; }

    public bool IsVergrendeld { get; set; }

    public string KleurCode { get; set; }

    public virtual Projecttraject Projecttraject { get; set; }

    public virtual ICollection<Mijlpaal> Mijlpalen { get; set; } = new List<Mijlpaal>();
}
