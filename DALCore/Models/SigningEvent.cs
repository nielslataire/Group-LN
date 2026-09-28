#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Audit trail van een ondertekeningsdossier (§3.7). Append-only: een databasetrigger weigert
/// UPDATE/DELETE, en elk event draagt een hash-ketting (<see cref="PrevEventHash"/> →
/// <see cref="EventHash"/>) zodat een verwijderd of gewijzigd event zichtbaar wordt. De ruwe
/// <see cref="Ip"/>/<see cref="UserAgent"/> zitten NIET in de ketting — enkel hun hash — zodat
/// een retentiescrub die ruwe waarden mag nullen zonder de ketting te breken. Nooit tokens of
/// OTP's in <see cref="DataJson"/>.
/// </summary>
public partial class SigningEvent
{
    public long Id { get; set; }

    public int SigningCaseId { get; set; }

    public int? SigningPartyId { get; set; }

    /// <summary>Vaste sleutel, bv. "CaseOpened", "PartySigned" (zie ONDERTEKENEN_VOORSTEL.md §7).</summary>
    public string EventType { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    /// <summary>BOCore.SigningActorType.</summary>
    public int ActorType { get; set; }

    public int? ActorUserId { get; set; }

    /// <summary>Naam/e-mail van de actor op dat moment (snapshot).</summary>
    public string ActorLabel { get; set; }

    public string Ip { get; set; }

    public string IpHash { get; set; }

    public string UserAgent { get; set; }

    public string UserAgentHash { get; set; }

    /// <summary>SHA-256 van het document dat op dat moment "het document" was.</summary>
    public string DocumentSha256 { get; set; }

    /// <summary>Gemaskeerde details als JSON (bestemming, provider-message-id, reden, …).</summary>
    public string DataJson { get; set; }

    public string PrevEventHash { get; set; }

    public string EventHash { get; set; }

    public virtual SigningCase SigningCase { get; set; }

    public virtual SigningParty SigningParty { get; set; }
}
