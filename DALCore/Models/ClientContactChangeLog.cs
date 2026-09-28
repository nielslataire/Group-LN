#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Wijzigingslog van contactgegevens die als verificatiefactor kunnen dienen (§6.3): e-mailadres
/// en gsm-nummer op <c>ClientAccount</c>/<c>ClientContacts</c>. Waarden gemaskeerd, zodat het
/// auditrapport kan tonen dat het gebruikte nummer kort vóór een ondertekening gewijzigd werd,
/// zonder zelf een kopie van persoonsgegevens te worden.
/// </summary>
public partial class ClientContactChangeLog
{
    public int Id { get; set; }

    /// <summary>"ClientAccount" of "ClientContact".</summary>
    public string EntityType { get; set; }

    public int EntityId { get; set; }

    public int ClientAccountId { get; set; }

    /// <summary>"Email" / "Cellphone".</summary>
    public string Field { get; set; }

    public string OldValueMasked { get; set; }

    public string NewValueMasked { get; set; }

    public int? ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; }
}
