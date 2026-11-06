#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Regie-uur dat uit een Outlook-afspraak komt (migratie 132). Los bestand omdat <c>ProjectRegieUur.cs</c> gegenereerd is.</summary>
public partial class ProjectRegieUur
{
    /// <summary>Graph-id van de afspraak waaruit dit uur is goedgekeurd; samen met <see cref="UserId"/> de dedup-sleutel. Null voor een uur dat met de hand is ingevoerd.</summary>
    public string OutlookEventId { get; set; }

    /// <summary>Begin en einde (lokale tijd) van de afspraak op het moment van goedkeuren, om een latere wijziging te herkennen.</summary>
    public DateTime? OutlookStart { get; set; }

    public DateTime? OutlookEinde { get; set; }
}
