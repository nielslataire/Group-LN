#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Eén OTP-cyclus van een ondertekenaar (§3.6). De code zelf staat nergens: enkel
/// <see cref="CodeHmac"/> = HMAC-SHA256(serversleutel, dossier|partij|verificatie|code), zodat een
/// gelekte tabel niet offline te kraken is en een code van A nooit bij B geldig is (§6.2).
/// </summary>
public partial class SigningVerification
{
    public int Id { get; set; }

    public int SigningPartyId { get; set; }

    /// <summary>Sleutel van de <c>IVerificationMethod</c>: "EmailOtp" / "SmsOtp".</summary>
    public string Method { get; set; }

    /// <summary>Sleutel van het <c>IMessageChannel</c>: "email" / "sms".</summary>
    public string ChannelKey { get; set; }

    /// <summary>Sleutel van de concrete provider: "smtp-o365", later "bird"/"twilio"/"cm".</summary>
    public string ProviderKey { get; set; }

    /// <summary>Gemaskeerde bestemming voor weergave en audit: "j•••@peeters.be", "•••• 47 82".</summary>
    public string DestinationMasked { get; set; }

    /// <summary>Hex, 64 tekens. Wordt na verificatie/verval/blokkering NIET gewist — de HMAC verraadt de code niet.</summary>
    public string CodeHmac { get; set; }

    /// <summary>Sessie waarbinnen de code werd aangevraagd; de controle moet uit dezelfde sessie komen.</summary>
    public Guid? SessionId { get; set; }

    public DateTime RequestedAt { get; set; }

    public string RequestedIp { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    /// <summary>BOCore.SigningVerificationStatus.</summary>
    public int Status { get; set; }

    public string ProviderMessageId { get; set; }

    public string ProviderStatus { get; set; }

    public DateTime? ProviderAcceptedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string VerifiedIp { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public virtual SigningParty SigningParty { get; set; }
}
