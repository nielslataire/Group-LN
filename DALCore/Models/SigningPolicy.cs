#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Beleid per documenttype voor elektronisch ondertekenen (ONDERTEKENEN_VOORSTEL.md §3.1): welke
/// methode, welke regel, of OTP vereist is en via welk kanaal, geldigheden, herinneringen, de
/// standaard-akkoordtekst en retentie. Een dossier kopieert dit beleid bij het aanmaken
/// (snapshot) — een latere wijziging hier raakt lopende dossiers niet.
/// </summary>
public partial class SigningPolicy
{
    public int Id { get; set; }

    /// <summary>Sleutel van het documenttype, bv. "ChangeOrder". Uniek.</summary>
    public string DocumentType { get; set; }

    /// <summary>Weergavenaam, bv. "Wijzigingsopdracht".</summary>
    public string DisplayName { get; set; }

    /// <summary>Sleutel van de <c>ISignatureMethodProvider</c>, bv. "internal-ses".</summary>
    public string SignatureMethod { get; set; }

    /// <summary>BOCore.SigningRule.</summary>
    public int SigningRule { get; set; }

    public bool OtpRequired { get; set; }

    /// <summary>Sleutel van de <c>IVerificationMethod</c>, bv. "EmailOtp" / "SmsOtp".</summary>
    public string VerificationMethod { get; set; }

    public int OtpValiditySeconds { get; set; }

    public int OtpMaxAttempts { get; set; }

    public int LinkValidityDays { get; set; }

    /// <summary>Eerste herinnering na zoveel dagen zonder handtekening; null = geen herinneringen.</summary>
    public int? ReminderAfterDays { get; set; }

    /// <summary>Volgende herinneringen om de zoveel dagen; null = enkel de eerste.</summary>
    public int? ReminderRepeatDays { get; set; }

    public int MaxReminders { get; set; }

    /// <summary>De huidige standaard-akkoordtekst. Wordt per dossier én per ondertekenaar gesnapshot.</summary>
    public string ConsentText { get; set; }

    /// <summary>Na zoveel dagen na voltooiing mogen IP/user-agent gescrubd worden; null = onbeperkt bewaren.</summary>
    public int? RetentionDays { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
