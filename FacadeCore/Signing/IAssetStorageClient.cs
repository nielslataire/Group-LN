namespace FacadeCore.Signing;

/// <summary>
/// Eén client voor de externe Storage API (ONDERTEKENEN_VOORSTEL.md §4.7) — vervangt de privé,
/// gedupliceerde helpers in <c>ProjectenController</c>/<c>ProjectIssuesController</c>. Endpoints:
/// <c>POST /api/assets/upload</c> (multipart: folder + file → { fileName }),
/// <c>POST /api/assets/{folder}/{file}/sign</c> (→ { url }), <c>DELETE /api/assets/{folder}/{file}</c>.
/// Geeft null/false terug bij een niet-geconfigureerde of falende API; nooit exceptions naar de
/// aanroeper, zodat de signingmodule SQL als bewijsbron kan houden en de opslag als spiegel.
/// </summary>
public interface IAssetStorageClient
{
    bool IsConfigured { get; }

    Task<string?> UploadAsync(Stream content, string originalFileName, string? contentType, string folder, CancellationToken ct = default);

    Task<string?> UploadAsync(byte[] content, string originalFileName, string? contentType, string folder, CancellationToken ct = default);

    /// <summary>Tijdelijk ondertekende URL; voor de map "pictures" een rechtstreekse publieke URL (bestaand gedrag).</summary>
    Task<string?> GetSignedUrlAsync(string folder, string fileName, CancellationToken ct = default);

    /// <summary>Synchroon, voor de bestaande Razor/controller-code die geen async pad heeft.</summary>
    string? GetSignedUrl(string folder, string fileName);

    /// <summary>Haalt de bytes op via een ondertekende URL; null bij falen.</summary>
    Task<byte[]?> DownloadAsync(string folder, string fileName, CancellationToken ct = default);

    /// <summary>Best-effort verwijderen (bestaand gedrag: fouten worden geslikt).</summary>
    Task<bool> DeleteAsync(string folder, string fileName, CancellationToken ct = default);
}
