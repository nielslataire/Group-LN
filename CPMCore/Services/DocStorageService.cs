using System.Net.Http.Headers;
using System.Text.Json;

namespace CPMCore.Services;

/// <summary>Kleine toegang tot de storage-API voor plekken zonder controller (bv. het ondertekende PDF dat de publieke
/// tekenpagina wegschrijft). Zelfde endpoints/sleutels als de upload- en sign-helpers in ProjectenController.</summary>
public class DocStorageService
{
    private readonly IConfiguration _config;
    private readonly ILogger<DocStorageService> _logger;

    public DocStorageService(IConfiguration config, ILogger<DocStorageService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>Uploadt een bestand naar de opgegeven map ("docs") en geeft de opslagnaam terug (of null bij een fout).</summary>
    public async Task<string?> UploadAsync(byte[] data, string originalFileName, string contentType, string folder)
    {
        var baseUrl = _config["StorageApi:BaseUrl"]?.TrimEnd('/');
        var writeKey = _config["StorageApi:WriteApiKey"];
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey)) return null;

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        http.DefaultRequestHeaders.Add("X-Api-Key", writeKey);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(folder), "folder");
        var file = new ByteArrayContent(data);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", originalFileName);

        var response = await http.PostAsync($"{baseUrl}/api/assets/upload", content);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Storage-upload van {File} mislukt: HTTP {Status}", originalFileName, (int)response.StatusCode);
            return null;
        }
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("fileName", out var name) ? name.GetString() : null;
    }

    /// <summary>Tijdelijk geldige leesurl van een opgeslagen bestand, of null.</summary>
    public async Task<string?> GetSignedUrlAsync(string fileName, string folder)
    {
        var safe = Path.GetFileName(fileName ?? string.Empty);
        var baseUrl = _config["StorageApi:BaseUrl"]?.TrimEnd('/');
        var readKey = _config["StorageApi:ReadApiKey"];
        if (string.IsNullOrWhiteSpace(safe) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(readKey)) return null;

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Add("X-Api-Key", readKey);
        var response = await http.PostAsync($"{baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(safe)}/sign", content: null);
        if (!response.IsSuccessStatusCode) return null;
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!json.RootElement.TryGetProperty("url", out var url)) return null;
        var value = url.GetString();
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? value : $"{baseUrl}{value}";
    }
}
