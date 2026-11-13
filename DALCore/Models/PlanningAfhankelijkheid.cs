#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Voorganger naar opvolger tussen twee <see cref="PlanningTaak"/>-en (migratie 144).</summary>
public partial class PlanningAfhankelijkheid
{
    public int Id { get; set; }

    public int VoorgangerTaakId { get; set; }

    public int OpvolgerTaakId { get; set; }

    /// <summary>0 FS (einde naar begin) · 1 SS (begin naar begin) · 2 FF (einde naar einde) · 3 SF (begin naar einde).</summary>
    public byte Type { get; set; }

    /// <summary>Wachttijd in werkdagen, ook negatief (overlap).</summary>
    public int WachtDagen { get; set; }

    public virtual PlanningTaak Voorganger { get; set; }

    public virtual PlanningTaak Opvolger { get; set; }
}
