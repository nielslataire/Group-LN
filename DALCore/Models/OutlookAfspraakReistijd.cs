#nullable disable
using System;

namespace DALCore.Models;

/// <summary>Koppeling tussen een afspraak en haar blok "Reistijd" in de Outlook-agenda (migratie 134). Enkel de event-id's en de lengte; geen agenda-inhoud.</summary>
public partial class OutlookAfspraakReistijd
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>Graph-id van de afspraak.</summary>
    public string EventId { get; set; }

    /// <summary>Graph-id van het blok reistijd dat er vlak voor eindigt.</summary>
    public string BlokEventId { get; set; }

    /// <summary>Lengte van het blok (reistijd + marge) bij het aanmaken.</summary>
    public int Minuten { get; set; }

    /// <summary>UTC.</summary>
    public DateTime GemaaktOp { get; set; }

    public virtual Users User { get; set; }
}
