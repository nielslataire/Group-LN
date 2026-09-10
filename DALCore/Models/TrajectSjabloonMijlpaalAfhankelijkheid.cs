#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Afhankelijkheid tussen twee sjabloon-mijlpalen (bv. finish-to-start).</summary>
public partial class TrajectSjabloonMijlpaalAfhankelijkheid
{
    public int Id { get; set; }

    public int MijlpaalId { get; set; }

    public int VereistMijlpaalId { get; set; }

    /// <summary>BOCore.Traject.AfhankelijkheidType (0 = FinishToStart).</summary>
    public int Type { get; set; }

    public virtual TrajectSjabloonMijlpaal Mijlpaal { get; set; }

    public virtual TrajectSjabloonMijlpaal VereistMijlpaal { get; set; }
}
