namespace FacadeCore.Signing;

/// <summary>
/// De daadwerkelijke ondertekenmethode (ONDERTEKENEN_VOORSTEL.md §4.3): v1 = interne SES; later
/// AES/QES via itsme, OK!Sign of een andere Europese provider. Meermaals geregistreerd in DI,
/// gekozen op <see cref="Key"/> (= <c>SigningCase.SignatureMethod</c>). Provider-details blijven
/// hierachter; de publieke pagina vraagt enkel <see cref="Capabilities"/> om te weten wat ze
/// moet tonen.
/// </summary>
public interface ISignatureMethodProvider
{
    /// <summary>Bv. "internal-ses", "itsme", "oksign".</summary>
    string Key { get; }

    /// <summary>Naam zoals ze op het ondertekeningsblad en in het auditrapport verschijnt, bv. "Elektronisch ondertekend via CPM (SES)".</summary>
    string DisplayName { get; }

    SignatureCapabilities Capabilities { get; }

    /// <summary>Bij het aanbieden van een dossier. SES: no-op. Extern: dossier bij de provider aanmaken.</summary>
    Task<ProviderStart> StartAsync(CaseStatusView signingCase, CancellationToken ct = default);

    /// <summary>Bij het definitief ondertekenen door één partij. SES: bevestigt het server-side verzamelde bewijs.</summary>
    Task<SignatureEvidence> CompleteAsync(CaseStatusView signingCase, PartyStatusView party, SignRequestEvidence evidence, CancellationToken ct = default);

    /// <summary>Callback/webhook van een externe provider (ruwe body + headers, zodat de provider zelf de handtekening kan controleren).</summary>
    Task<ProviderCallbackResult> HandleCallbackAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);

    /// <summary>Bij annuleren: SES no-op; extern: dossier bij de provider intrekken.</summary>
    Task CancelAsync(CaseStatusView signingCase, CancellationToken ct = default);
}
