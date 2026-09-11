#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>Concrete trigger op een mijlpaal-instantie. Zie <see cref="TrajectSjabloonMijlpaalTrigger"/>.</summary>
public partial class MijlpaalTrigger
{
    public int Id { get; set; }

    public int MijlpaalId { get; set; }

    public int? SjabloonTriggerId { get; set; }

    public int TriggerEvent { get; set; }

    public int TriggerActie { get; set; }

    public int? OffsetDagen { get; set; }

    public string ActieParametersJson { get; set; }

    public bool MagProjectWijzigen { get; set; }

    public bool IsActief { get; set; }

    /// <summary>Laatste keer dat deze trigger effectief is afgevuurd (idempotentie — niet opnieuw voor hetzelfde bereikte punt).</summary>
    public DateTime? LaatstGevuurdOp { get; set; }

    public virtual Mijlpaal Mijlpaal { get; set; }

    public virtual ICollection<MijlpaalTriggerRun> Runs { get; set; } = new List<MijlpaalTriggerRun>();
}
