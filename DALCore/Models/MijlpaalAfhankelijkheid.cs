#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Afhankelijkheid tussen twee concrete mijlpalen.</summary>
public partial class MijlpaalAfhankelijkheid
{
    public int Id { get; set; }

    public int MijlpaalId { get; set; }

    public int VereistMijlpaalId { get; set; }

    /// <summary>BOCore.Traject.AfhankelijkheidType (0 = FinishToStart).</summary>
    public int Type { get; set; }

    public virtual Mijlpaal Mijlpaal { get; set; }

    public virtual Mijlpaal VereistMijlpaal { get; set; }
}
