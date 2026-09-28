#nullable disable
using System;
using System.Collections.Generic;

namespace DALCore.Models;

/// <summary>
/// Eén ondertekenaar binnen een dossier (§3.4). Naam en e-mail zijn snapshots; het gsm-nummer
/// wordt bewust NIET bewaard — enkel de gemaskeerde vorm, het echte nummer wordt op het moment
/// van SMS-verzending uit de bronrecord gelezen (§6.3).
/// </summary>
public partial class SigningParty
{
    public int Id { get; set; }

    public int SigningCaseId { get; set; }

    /// <summary>Volgorde (relevant bij de ORDERED-regel; ook de weergavevolgorde).</summary>
    public int SortOrder { get; set; }

    /// <summary>BOCore.SigningPartyType.</summary>
    public int PartyType { get; set; }

    /// <summary>Id in de brontabel die bij <see cref="PartyType"/> hoort (ClientAccount/ClientContacts/Users).</summary>
    public int? SourceRefId { get; set; }

    public string DisplayName { get; set; }

    public string Email { get; set; }

    /// <summary>Gemaskeerd gsm-nummer voor weergave ("•••• 47 82"); leeg als onbekend.</summary>
    public string PhoneMasked { get; set; }

    /// <summary>Hoedanigheid: "eigenaar", "mede-eigenaar", "namens BV X", "volmachthouder", …</summary>
    public string Capacity { get; set; }

    /// <summary>BOCore.SigningPartyStatus.</summary>
    public int Status { get; set; }

    /// <summary>Uniek verificatie-ID per handtekening, op het ondertekeningsblad.</summary>
    public Guid PartyVerificationId { get; set; }

    public DateTime? InvitedAt { get; set; }

    public DateTime? LastReminderAt { get; set; }

    public int ReminderCount { get; set; }

    public DateTime? FirstOpenedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime? ConsentAcceptedAt { get; set; }

    /// <summary>De akkoordtekst zoals deze persoon ze aanvaardde — kopie per ondertekenaar.</summary>
    public string ConsentTextSnapshot { get; set; }

    public DateTime? SignedAt { get; set; }

    public string SignedIp { get; set; }

    public string SignedUserAgent { get; set; }

    /// <summary>Idempotentiesleutel van de sessie die tekende — een tweede identieke POST wordt als dezelfde behandeld.</summary>
    public string SignIdempotencyKey { get; set; }

    public int? SignatureImageDocumentId { get; set; }

    public DateTime? DeclinedAt { get; set; }

    public string DeclineReason { get; set; }

    /// <summary>Referentie van deze ondertekenaar bij een externe provider (leeg bij SES).</summary>
    public string ProviderPartyRef { get; set; }

    public byte[] RowVersion { get; set; }

    public virtual SigningCase SigningCase { get; set; }

    public virtual SigningDocument SignatureImageDocument { get; set; }

    public virtual ICollection<SigningAccessToken> AccessTokens { get; set; } = new List<SigningAccessToken>();

    public virtual ICollection<SigningVerification> Verifications { get; set; } = new List<SigningVerification>();
}
