using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ServiceCore.Signing;

/// <summary>
/// De enige plek in de module die ruwe tokens en OTP-codes aanraakt (ONDERTEKENEN_VOORSTEL.md §6).
/// Uitsluitend .NET-primitieven: <see cref="RandomNumberGenerator"/>, <see cref="SHA256"/>,
/// <see cref="HMACSHA256"/>, <see cref="CryptographicOperations.FixedTimeEquals"/>. Pure functies,
/// geen state, geen logging — zo kan er nooit een geheim in een log belanden.
/// </summary>
public static class SigningCrypto
{
    // ── Tokens (persoonlijke links) ──────────────────────────────────────────────────────────

    /// <summary>Nieuw ruw token: <paramref name="bytes"/> willekeurige bytes als base64url (32 bytes → 43 tekens).</summary>
    public static string GenerateToken(int bytes = 32)
    {
        var buffer = new byte[bytes];
        RandomNumberGenerator.Fill(buffer);
        return ToBase64Url(buffer);
    }

    /// <summary>SHA-256 van het ruwe token, hex in kleine letters. Bij 256 bits entropie is geen salt/pepper nodig.</summary>
    public static string HashToken(string rawToken)
        => Sha256Hex(Encoding.UTF8.GetBytes(rawToken.Trim()));

    /// <summary>Snelle vormcontrole vóór de databaselookup: base64url van 32–64 bytes.</summary>
    public static bool LooksLikeToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (raw.Length is < 40 or > 90) return false;
        foreach (var c in raw)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_')) return false;
        }
        return true;
    }

    // ── OTP ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>Uniforme code van <paramref name="digits"/> cijfers (GetInt32 heeft geen modulo-bias).</summary>
    public static string GenerateOtp(int digits = 6)
    {
        var max = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString("D" + digits, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// HMAC-SHA256 van dossier|partij|aanvraagtijdstip|code. De serversleutel maakt een gelekte
    /// tabel niet offline kraakbaar (10⁶ codes); dossier + partij + tijdstip binden de code aan
    /// precies deze verificatiecyclus, zodat een code van A nooit bij B geldig is.
    /// </summary>
    public static string ComputeOtpHmac(byte[] key, int caseId, int partyId, DateTime requestedAtUtc, string code)
    {
        var payload = Encoding.UTF8.GetBytes(string.Concat(
            caseId.ToString(CultureInfo.InvariantCulture), "|",
            partyId.ToString(CultureInfo.InvariantCulture), "|",
            requestedAtUtc.Ticks.ToString(CultureInfo.InvariantCulture), "|",
            NormalizeOtp(code)));
        return ToHex(HMACSHA256.HashData(key, payload));
    }

    /// <summary>Alleen cijfers overhouden (gebruikers typen soms spaties of streepjes).</summary>
    public static string NormalizeOtp(string? code)
    {
        if (string.IsNullOrEmpty(code)) return string.Empty;
        var sb = new StringBuilder(code.Length);
        foreach (var c in code) if (char.IsAsciiDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    // ── Hashes ──────────────────────────────────────────────────────────────────────────────

    public static string Sha256Hex(byte[] data) => ToHex(SHA256.HashData(data));

    public static string Sha256Hex(string text) => Sha256Hex(Encoding.UTF8.GetBytes(text));

    /// <summary>
    /// Vingerafdruk van een brondocument voor <c>ISigningDocumentSource.ComputeFingerprintAsync</c>:
    /// SHA-256 over de onderdelen, elk met een lengteprefix zodat ("ab","c") en ("a","bc") nooit
    /// dezelfde hash geven en een null-onderdeel verschilt van een lege string. Bedragen/datums
    /// moeten door de aanroeper al cultuuronafhankelijk geformatteerd zijn (Invariant).
    /// </summary>
    public static string ComputeFingerprint(IEnumerable<string?> parts)
    {
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part is null) { sb.Append("-|"); continue; }
            sb.Append(part.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(part).Append('|');
        }
        return Sha256Hex(sb.ToString());
    }

    /// <summary>Hash van een IP/user-agent voor de hash-ketting; de ruwe waarde kan later gescrubd worden zonder de ketting te breken.</summary>
    public static string? HashPii(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Sha256Hex(value.Trim());

    /// <summary>Constante-tijd vergelijking van twee hex-strings; false bij ongelijke lengte of ongeldige hex.</summary>
    public static bool FixedTimeEqualsHex(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a.Length != b.Length) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(a), Convert.FromHexString(b));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // ── Hash-ketting van de audit trail (§3.7) ───────────────────────────────────────────────

    /// <summary>
    /// Canonieke invoer: velden gescheiden door '\n', tijdstip als round-trip ("O"). De ruwe IP en
    /// user-agent zitten er bewust NIET in — enkel hun hashes.
    /// </summary>
    public static string ComputeEventHash(
        string? prevEventHash, int caseId, int? partyId, string eventType, DateTime occurredAtUtc,
        int actorType, int? actorUserId, string? actorLabel, string? ipHash, string? userAgentHash,
        string? documentSha256, string? dataJson)
    {
        var canonical = string.Join('\n',
            prevEventHash ?? string.Empty,
            caseId.ToString(CultureInfo.InvariantCulture),
            partyId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            eventType,
            DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture),
            actorType.ToString(CultureInfo.InvariantCulture),
            actorUserId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            actorLabel ?? string.Empty,
            ipHash ?? string.Empty,
            userAgentHash ?? string.Empty,
            documentSha256 ?? string.Empty,
            dataJson ?? string.Empty);
        return Sha256Hex(canonical);
    }

    // ── Maskeren (§3.4, §3.6) ────────────────────────────────────────────────────────────────

    /// <summary>"jan.peeters@voorbeeld.be" → "j•••@voorbeeld.be".</summary>
    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var at = email.IndexOf('@');
        if (at <= 0) return "•••";
        return string.Concat(email[0].ToString(), "•••", email[at..]);
    }

    /// <summary>"+32 476 12 47 82" → "•••• 47 82" (laatste vier cijfers, per twee).</summary>
    public static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return "••••";
        var last4 = digits[^4..];
        return $"•••• {last4[..2]} {last4[2..]}";
    }

    /// <summary>IPv4 → laatste octet weg; IPv6 → laatste vier groepen weg. Voor weergave in het auditrapport.</summary>
    public static string? MaskIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        if (ip.Contains(':'))
        {
            var groups = ip.Split(':');
            return groups.Length > 4 ? string.Join(':', groups.Take(4)) + ":••••" : "••••";
        }
        var parts = ip.Split('.');
        return parts.Length == 4 ? $"{parts[0]}.{parts[1]}.{parts[2]}.•••" : "•••";
    }

    // ── Encoding ────────────────────────────────────────────────────────────────────────────

    public static string ToBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}
