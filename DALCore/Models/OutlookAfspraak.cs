#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Koppelrecord tussen een afspraak uit de Outlook-agenda van een medewerker en een project (migratie 124). Geen kopie van agenda-inhoud: enkel het event-id.</summary>
public partial class OutlookAfspraak
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>Graph-id van de afspraak (van dit voorkomen, bij een reeks).</summary>
    public string EventId { get; set; }

    public int ProjectId { get; set; }

    /// <summary>UTC.</summary>
    public DateTime GekoppeldOp { get; set; }

    public virtual Users User { get; set; }

    public virtual Project Project { get; set; }
}
