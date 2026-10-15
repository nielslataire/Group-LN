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

    /// <summary>
    /// Uploadt één foto-origineel (max 50 MB, max 10.000 px lange zijde) naar <c>POST /api/assets/photo</c>. De Storage API
    /// draait de foto volgens EXIF, verwijdert EXIF, bewaart een master-JPEG (korte zijde max 5000 px, kwaliteit 92) en maakt de
    /// WebP-varianten (breedtes 320-2560 + de vierkante 447/800 + 'pictures'). <paramref name="baseName"/> = bestandsnaam zonder extensie.
    /// </summary>
    Task<PhotoUploadResult> UploadPhotoAsync(Stream content, string originalFileName, string? contentType, string baseName, CancellationToken ct = default);

    /// <summary>Verwijdert alle varianten van een foto (master-JPEG + alle WebP's, ook de vierkante 447/800); best-effort.</summary>
    Task<bool> DeletePhotoAsync(string fileName, CancellationToken ct = default);
}

/// <summary>Resultaat van <see cref="IAssetStorageClient.UploadPhotoAsync"/>. Width/Height = afmetingen van het (georiënteerde) origineel.</summary>
public sealed record PhotoUploadResult(bool Success, string? FileName, int Width, int Height, string? Error);
