using FacadeCore.Signing;

namespace ServiceCore.Signing;

/// <summary>
/// Kiest de juiste implementatie op sleutel uit de DI-multi-registraties (het
/// <c>ITrajectTriggerAction</c>-recept, ONDERTEKENEN_VOORSTEL.md §4). Onbekende sleutel = duidelijke
/// fout, geen stille terugval — een dossier dat naar "itsme" verwijst mag nooit per ongeluk via
/// SES lopen.
/// </summary>
public sealed class SigningRegistry
{
    private readonly IReadOnlyDictionary<string, ISigningDocumentSource> _sources;
    private readonly IReadOnlyDictionary<string, ISignatureMethodProvider> _providers;
    private readonly IReadOnlyDictionary<string, IVerificationMethod> _verifications;
    private readonly IReadOnlyDictionary<string, IMessageChannel> _channels;

    public SigningRegistry(
        IEnumerable<ISigningDocumentSource> sources,
        IEnumerable<ISignatureMethodProvider> providers,
        IEnumerable<IVerificationMethod> verifications,
        IEnumerable<IMessageChannel> channels)
    {
        _sources = sources.ToDictionary(s => s.DocumentType, StringComparer.OrdinalIgnoreCase);
        _providers = providers.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);
        _verifications = verifications.ToDictionary(v => v.Key, StringComparer.OrdinalIgnoreCase);
        _channels = channels.ToDictionary(c => c.Key, StringComparer.OrdinalIgnoreCase);
    }

    public ISigningDocumentSource Source(string documentType)
        => _sources.TryGetValue(documentType, out var s) ? s
           : throw new InvalidOperationException($"Geen ISigningDocumentSource geregistreerd voor documenttype '{documentType}'.");

    public ISigningDocumentSource? TrySource(string documentType)
        => _sources.TryGetValue(documentType, out var s) ? s : null;

    public ISignatureMethodProvider Provider(string key)
        => _providers.TryGetValue(key, out var p) ? p
           : throw new InvalidOperationException($"Geen ISignatureMethodProvider geregistreerd met sleutel '{key}'.");

    public IVerificationMethod Verification(string key)
        => _verifications.TryGetValue(key, out var v) ? v
           : throw new InvalidOperationException($"Geen IVerificationMethod geregistreerd met sleutel '{key}'.");

    public IMessageChannel Channel(string key)
        => _channels.TryGetValue(key, out var c) ? c
           : throw new InvalidOperationException($"Geen IMessageChannel geregistreerd met sleutel '{key}'.");

    public IReadOnlyCollection<string> DocumentTypes => _sources.Keys.ToList();
}
