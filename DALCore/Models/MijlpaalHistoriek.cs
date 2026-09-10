#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Audit-regel voor elke wijziging aan een <see cref="Mijlpaal"/> (spiegelt <c>ConstructionIssueHistory</c>).</summary>
public partial class MijlpaalHistoriek
{
    public int Id { get; set; }

    public int MijlpaalId { get; set; }

    /// <summary>BOCore.Traject.HistoriekActie.</summary>
    public int Actie { get; set; }

    public string UserId { get; set; }

    public DateTime Timestamp { get; set; }

    public string OldValueJson { get; set; }

    public string NewValueJson { get; set; }

    public string Opmerking { get; set; }

    public virtual Mijlpaal Mijlpaal { get; set; }
}
