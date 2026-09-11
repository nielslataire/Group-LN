#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Trigger-definitie op een sjabloon-mijlpaal; wordt bij instantiatie/sync gekopieerd naar <see cref="MijlpaalTrigger"/>.</summary>
public partial class TrajectSjabloonMijlpaalTrigger
{
    public int Id { get; set; }

    public int TrajectSjabloonMijlpaalId { get; set; }

    /// <summary>BOCore.TriggerEvent.</summary>
    public int TriggerEvent { get; set; }

    /// <summary>BOCore.TriggerActie.</summary>
    public int TriggerActie { get; set; }

    /// <summary>Enkel voor TriggerEvent = XDagenVoorDoeldatum.</summary>
    public int? OffsetDagen { get; set; }

    /// <summary>Kleine JSON-payload specifiek voor de actie (bv. {"rol":5} of {"vlag":"DocPid"}).</summary>
    public string ActieParametersJson { get; set; }

    /// <summary>Enkel voor ZetProjectStatus/ZetProjectVlag — expliciete opt-in vereist.</summary>
    public bool MagProjectWijzigen { get; set; }

    public bool IsActief { get; set; }

    public string Omschrijving { get; set; }

    public virtual TrajectSjabloonMijlpaal TrajectSjabloonMijlpaal { get; set; }
}
