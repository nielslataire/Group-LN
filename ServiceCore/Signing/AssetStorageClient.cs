using System.Net.Http.Headers;
using System.Text.Json;
using FacadeCore.Signing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceCore.Signing;

/// <summary>
/// De ene client voor de externe Storage API (ONDERTEKENEN_VOORSTEL.md §4.7). Zelfde endpoints en
/// hetzelfde gedrag als de privé-helpers die tot nu toe in <c>ProjectenController</c> en
/// <c>ProjectIssuesController</c> gedupliceerd stonden (die delegeren voortaan hierheen), inclusief
/// de uitzondering dat de map "pictures" een rechtstreekse publieke URL heeft. Geregistreerd via
/// <c>AddHttpClient&lt;IAssetStorageClient, AssetStorageClient&gt;</c> — geen <c>new HttpClient()</c>
/// per aanroep meer.
/// </summary>
public sealed class AssetStorageClient : IAssetStorageClient
{
    private readonly HttpClient _http;
    private readonly ILogger<AssetStorageClient> _logger;
    private readonly string? _baseUrl;
    private readonly string? _readKey;
    private readonly string? _writeKey;

    public AssetStorageClient(HttpClient http, IConfiguration configuration, ILogger<AssetStorageClient> logger)
    {
        _http = http;
        _logger = logger;
        _baseUrl = configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        _readKey = configuration["StorageApi:ReadApiKey"];
        _writeKey = configuration["StorageApi:WriteApiKey"];
        if (_http.Timeout == TimeSpan.FromSeconds(100)) _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl) && !string.IsNullOrWhiteSpace(_readKey);

    public async Task<string?> UploadAsync(byte[] content, string originalFileName, string? contentType, string folder, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(content, writable: false);
        return await UploadAsync(stream, originalFileName, contentType, folder, ct);
    }

    public async Task<string?> UploadAsync(Stream content, string originalFileName, string? contentType, string folder, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_writeKey)) return null;
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(folder), "folder");
            var file = new StreamContent(content);
            if (!string.IsNullOrWhiteSpace(contentType))
                file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(file, "file", originalFileName);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/assets/upload") { Content = form };
            request.Headers.Add("X-Api-Key", _writeKey);
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Storage API upload naar map {Folder} faalde: {Status}.", folder, (int)response.StatusCode);
                return null;
            }
            var payload = await response.Content.ReadAsStringAsync(ct);
            using var json = JsonDocument.Parse(payload);
            return json.RootElement.TryGetProperty("fileName", out var name) ? name.GetString() : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Storage API upload naar map {Folder} faalde.", folder);
            return null;
        }
    }

    public string? GetSignedUrl(string folder, string fileName)
        => GetSignedUrlAsync(folder, fileName).GetAwaiter().GetResult();

    public async Task<string?> GetSignedUrlAsync(string folder, string fileName, CancellationToken ct = default)
    {
        var safeFileName = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileName) || string.IsNullOrWhiteSpace(_baseUrl)) return null;

        // Bestaand gedrag (ProjectIssuesController): projectfoto's zijn publiek bereikbaar.
        if (string.Equals(folder, "pictures", StringComparison.OrdinalIgnoreCase))
            return $"{_baseUrl}/pictures/{Uri.EscapeDataString(safeFileName)}";

        if (string.IsNullOrWhiteSpace(_readKey)) return null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(safeFileName)}/sign");
            request.Headers.Add("X-Api-Key", _readKey);
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;
            var payload = await response.Content.ReadAsStringAsync(ct);
            using var json = JsonDocument.Parse(payload);
            if (!json.RootElement.TryGetProperty("url", out var urlElement)) return null;
            var url = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(url)) return null;
            return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : $"{_baseUrl}{url}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Storage API sign-URL voor {Folder}/{File} faalde.", folder, safeFileName);
            return null;
        }
    }

    public async Task<byte[]?> DownloadAsync(string folder, string fileName, CancellationToken ct = default)
    {
        var url = await GetSignedUrlAsync(folder, fileName, ct);
        if (url is null) return null;
        try
        {
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Storage API download van {Folder}/{File} faalde.", folder, fileName);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string folder, string fileName, CancellationToken ct = default)
    {
        var safeFileName = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeFileName) || string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_writeKey)) return false;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(safeFileName)}");
            request.Headers.Add("X-Api-Key", _writeKey);
            using var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Storage API delete van {Folder}/{File} faalde (best-effort).", folder, safeFileName);
            return false;
        }
    }
}
