#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Afgevinkte punten (bitmask) van de wekelijkse review van een gebruiker, per week (maandag).</summary>
public partial class TaakWeekReview
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime WeekStart { get; set; }

    public int Afgevinkt { get; set; }

    public virtual Users User { get; set; }
}
