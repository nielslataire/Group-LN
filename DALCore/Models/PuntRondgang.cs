#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Een rondgang (opname + foto's) die op de server verwerkt wordt (Punten, design-handoff 41d). Migratie 100.</summary>
public partial class PuntRondgang
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public double DurationSeconds { get; set; }
    public int? VerslagId { get; set; }
    public int? DefaultUnitId { get; set; }
    /// <summary>0 = wacht, 1 = bezig, 2 = klaar, 3 = fout.</summary>
    public int Status { get; set; }
    public string Step { get; set; }
    public int Percent { get; set; }
    public int Attempts { get; set; }
    public DateTime? StartedAt { get; set; }
    public string AudioFile { get; set; }
    public string AudioType { get; set; }
    public string Transcript { get; set; }
    /// <summary>JSON: de voorgestelde punten.</summary>
    public string VoorstellenJson { get; set; }
    public string Error { get; set; }

    public virtual Project Project { get; set; }
    public virtual ICollection<PuntRondgangFoto> Fotos { get; set; } = new List<PuntRondgangFoto>();
}

public partial class PuntRondgangFoto
{
    public int Id { get; set; }
    public int RondgangId { get; set; }
    public int Volgnr { get; set; }
    public double AtSeconds { get; set; }
    public string FileName { get; set; }

    public virtual PuntRondgang Rondgang { get; set; }
}
