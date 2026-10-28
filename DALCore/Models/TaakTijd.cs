#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Gewerkte tijd op een taak (Mijn taken › timer). Een lopende timer is een rij zonder <see cref="Einde"/>.</summary>
public partial class TaakTijd
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? TaakId { get; set; }

    public int? ProjectId { get; set; }

    /// <summary>Titel op het moment van starten; blijft bewaard als de taak later verwijderd wordt.</summary>
    public string Titel { get; set; }

    public string Context { get; set; }

    public DateTime Start { get; set; }

    public DateTime? Einde { get; set; }

    public virtual Users User { get; set; }

    public virtual ProjectTaak Taak { get; set; }
}
