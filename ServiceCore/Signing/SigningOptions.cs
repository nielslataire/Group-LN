namespace ServiceCore.Signing;

/// <summary>
/// Configuratie van de signingmodule (ONDERTEKENEN_VOORSTEL.md §3.9), sectie <c>Signing</c> in
/// user-secrets/appsettings. <see cref="Validate"/> laat de applicatie bij het opstarten falen
/// wanneer de HMAC-sleutel ontbreekt — bewust géén fallback-waarde (contrast:
/// <c>ResendInviteUrlBuilder</c>), een signingmodule mag nooit met een ingebakken sleutel draaien.
/// </summary>
public sealed class SigningOptions
{
    public const string SectionName = "Signing";

    /// <summary>Geheime sleutel voor de OTP-HMAC (§6.2). Minstens 32 tekens; bij voorkeur 32+ willekeurige bytes als base64.</summary>
    public string? OtpHmacKey { get; set; }

    /// <summary>Basis-URL van CPM voor absolute links in mails, bv. "https://cpm.groupln.be". Zonder slash op het einde.</summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Aantal willekeurige bytes per persoonlijke link (§6.1).</summary>
    public int TokenBytes { get; set; } = 32;

    /// <summary>Aantal cijfers in een OTP.</summary>
    public int OtpLength { get; set; } = 6;

    /// <summary>Hoe lang een sessie (na inwisselen van de link) geldig blijft zonder activiteit, en hoe oud een verificatie mag zijn bij het ondertekenen.</summary>
    public int SigningSessionMinutes { get; set; } = 30;

    /// <summary>Map in de Storage API waarin de spiegelkopieën terechtkomen (§9.1).</summary>
    public string StorageFolder { get; set; } = "signing";

    /// <summary>Maximale grootte van een handtekeningafbeelding (PNG) in bytes.</summary>
    public int MaxSignatureImageBytes { get; set; } = 512 * 1024;

    /// <summary>Maximale grootte van een door een beheerder opgeladen getekende PDF (scherm 21b) in bytes.</summary>
    public int MaxUploadedDocumentBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>Geldigheid van een Download-token in dagen (§9.5).</summary>
    public int DownloadLinkDays { get; set; } = 90;

    /// <summary>Afzenderadres voor signing-mails; null = de SMTP-gebruiker.</summary>
    public string? FromEmail { get; set; }

    /// <summary>Sleutel van de SMS-provider die <c>SmsChannel</c> gebruikt (bv. "bird"); leeg = SMS niet beschikbaar.</summary>
    public string? SmsProvider { get; set; }

    /// <summary>
    /// TESTMODUS: is dit gevuld, dan gaan ÁLLE uitgaande signing-mails (uitnodigingen, herinneringen,
    /// verificatiecodes, bevestigingen) naar dit adres in plaats van naar de echte ontvanger; het
    /// oorspronkelijke adres komt in het onderwerp. Zo kan de volledige flow op de testomgeving
    /// doorlopen worden zonder dat een klant iets ontvangt. Hoort LEEG te zijn in productie; de
    /// notifier logt bij het opstarten een waarschuwing wanneer het gevuld is.
    /// </summary>
    public string? TestRecipientOverride { get; set; }

    public bool IsTestMode => !string.IsNullOrWhiteSpace(TestRecipientOverride);

    public byte[] GetOtpHmacKeyBytes()
    {
        Validate();
        var key = OtpHmacKey!;
        try
        {
            var decoded = Convert.FromBase64String(key);
            if (decoded.Length >= 32) return decoded;
        }
        catch (FormatException) { /* geen base64: gebruik de tekst zelf */ }
        return System.Text.Encoding.UTF8.GetBytes(key);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(OtpHmacKey) || OtpHmacKey.Length < 32)
            throw new InvalidOperationException("Signing:OtpHmacKey ontbreekt of is korter dan 32 tekens. De signingmodule start niet zonder een echte, geheime sleutel.");
        if (string.IsNullOrWhiteSpace(PublicBaseUrl))
            throw new InvalidOperationException("Signing:PublicBaseUrl ontbreekt (bv. https://cpm.groupln.be); nodig voor de ondertekenlinks in e-mails.");
        if (TokenBytes < 32) throw new InvalidOperationException("Signing:TokenBytes moet minstens 32 zijn.");
        if (OtpLength is < 6 or > 10) throw new InvalidOperationException("Signing:OtpLength moet tussen 6 en 10 liggen.");
    }
}
