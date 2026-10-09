#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Werfverslag of oplevering (design-handoff 40i/40l): bundelt de punten van één bezoek.</summary>
public partial class Werfverslag
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    /// <summary>0 = werfverslag, 1 = oplevering.</summary>
    public int VerslagType { get; set; }
    public int Nummer { get; set; } = 1;
    public string Naam { get; set; }
    public DateOnly Datum { get; set; }
    public TimeOnly? Uur { get; set; }
    /// <summary>JSON: [{"name":"…","email":"…"}].</summary>
    public string AanwezigenJson { get; set; }
    public string Weer { get; set; }
    public string Opmerkingen { get; set; }
    public DateTime? VolgendBezoek { get; set; }
    /// <summary>0 = concept, 1 = verstuurd.</summary>
    public int Status { get; set; }
    public int? HernomenVanId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? VerzondenOp { get; set; }
    public string VerzondenDoor { get; set; }

    public virtual Project Project { get; set; }
    public virtual ICollection<WerfverslagPunt> Punten { get; set; } = new List<WerfverslagPunt>();
}

public partial class WerfverslagPunt
{
    public int Id { get; set; }
    public int VerslagId { get; set; }
    public int IssueId { get; set; }
    /// <summary>Tijdens dit bezoek ingegeven (komt als concept binnen tot het verslag afgerond wordt).</summary>
    public bool IsNieuw { get; set; }
    /// <summary>Wat je ter plaatse zag: 0 = blijft open, 1 = opgelost, 2 = afgesloten; null = nog niet nagekeken.</summary>
    public int? TerPlaatse { get; set; }
    public string Opmerking { get; set; }

    public virtual Werfverslag Verslag { get; set; }
    public virtual ConstructionIssue Issue { get; set; }
}
