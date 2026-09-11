#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Audit-regel per uitvoering van een <see cref="MijlpaalTrigger"/> (spiegelt IssueNotificationRun).</summary>
public partial class MijlpaalTriggerRun
{
    public int Id { get; set; }

    public int MijlpaalTriggerId { get; set; }

    public int MijlpaalId { get; set; }

    public DateTime Uitgevoerd { get; set; }

    /// <summary>BOCore.TriggerRunStatus.</summary>
    public int Status { get; set; }

    public string Resultaat { get; set; }

    public string Fout { get; set; }

    public virtual MijlpaalTrigger MijlpaalTrigger { get; set; }
}
