#nullable disable
using System;

namespace DALCore.Models;

/// <summary>
/// Persoonlijke ondertekenlink (§3.5). Het ruwe token (32 willekeurige bytes, base64url) staat
/// alleen in de verstuurde link; hier enkel <see cref="TokenHash"/> = SHA-256 ervan. Meerdere
/// tokens per ondertekenaar zijn normaal (herinnering, nieuwe link); "nieuwe link" trekt de oude in.
/// Na een geslaagde inwisseling loopt de browser verder op een sessie (<see cref="SessionId"/>) —
/// het token verdwijnt dan uit de URL (§5.2 stap 2).
/// </summary>
public partial class SigningAccessToken
{
    public int Id { get; set; }

    public int SigningPartyId { get; set; }

    /// <summary>SHA-256 van het ruwe token, hex, kleine letters. Uniek.</summary>
    public string TokenHash { get; set; }

    /// <summary>BOCore.SigningTokenPurpose.</summary>
    public int Purpose { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? FirstUsedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public int UseCount { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string RevokedReason { get; set; }

    /// <summary>Sessie die bij de laatste geslaagde inwisseling werd uitgegeven; verificatie en ondertekening moeten deze sessie voorleggen.</summary>
    public Guid? SessionId { get; set; }

    public DateTime? SessionIssuedAt { get; set; }

    public virtual SigningParty SigningParty { get; set; }
}
