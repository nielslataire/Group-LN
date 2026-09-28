using System.Text.Json;
using FacadeCore.Signing;

namespace ServiceCore.Signing;

/// <summary>
/// De interne Simple Electronic Signature (ONDERTEKENEN_VOORSTEL.md §4.3, v1). Geen externe partij:
/// het bewijs is wat CPM zelf server-side vaststelt — geverifieerde OTP van deze partij in deze
/// sessie, expliciet aanvaarde akkoordtekst, SHA-256 van het aangeboden document, tijdstip, IP en
/// user-agent — en de append-only audit trail eromheen. Het ondertekeningsblad en het auditrapport
/// worden door <see cref="ISigningDocumentRenderer"/> gemaakt; deze klasse bevestigt enkel het
/// bewijs en benoemt de methode. Nergens "AES", "QES" of "gekwalificeerd": dit is een SES.
/// </summary>
public sealed class InternalSesProvider : ISignatureMethodProvider
{
    public const string ProviderKey = "internal-ses";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Key => ProviderKey;

    public string DisplayName => "Elektronisch ondertekend via CPM (eenvoudige elektronische handtekening)";

    public SignatureCapabilities Capabilities { get; } = new(
        HostedUi: false,
        RequiresOwnOtp: true,
        SupportsWebhooks: false,
        ProducesSignedPdf: false);

    public Task<ProviderStart> StartAsync(CaseStatusView signingCase, CancellationToken ct = default)
        => Task.FromResult(new ProviderStart(true, null, null, null));

    public Task<SignatureEvidence> CompleteAsync(CaseStatusView signingCase, PartyStatusView party, SignRequestEvidence evidence, CancellationToken ct = default)
    {
        // Wat de service al server-side vaststelde wordt hier enkel samengevat en als bewijs
        // teruggegeven; de service schrijft het als event PartySigned. Geen extra controle nodig:
        // de service heeft die al gedaan vóór ze hier komt, en herhaling zou de verantwoordelijkheid
        // versnipperen.
        var data = new
        {
            method = Key,
            verificationId = evidence.VerificationId,
            verificationMethod = evidence.VerificationMethod,
            verifiedAtUtc = evidence.VerifiedAt,
            consentAcceptedAtUtc = evidence.ConsentAcceptedAt,
            consentTextSha256 = SigningCrypto.Sha256Hex(evidence.ConsentText),
            documentSha256 = evidence.DocumentSha256,
            partyVerificationId = party.PartyVerificationId,
        };
        var summary = $"SES; verificatie {evidence.VerificationMethod ?? "geen"}; document {evidence.DocumentSha256[..12]}…";
        return Task.FromResult(new SignatureEvidence(Key, summary, JsonSerializer.Serialize(data, JsonOptions)));
    }

    public Task<ProviderCallbackResult> HandleCallbackAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
        => Task.FromResult(new ProviderCallbackResult(false, null, "De interne SES-methode ontvangt geen callbacks."));

    public Task CancelAsync(CaseStatusView signingCase, CancellationToken ct = default) => Task.CompletedTask;
}
