using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GroupLN.MarketData.Infrastructure.Services;

public class ProjectPhotoHashService : IProjectPhotoHashService
{
    private const string HashAlgorithmVersion = "SHA256+dHash1";
    private const string PerceptualHashVersion = "dHash1";

    private readonly MarketDataDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CrawlerSettings _settings;
    private readonly ILogger<ProjectPhotoHashService> _logger;

    public ProjectPhotoHashService(
        MarketDataDbContext db,
        IHttpClientFactory httpClientFactory,
        CrawlerSettings settings,
        ILogger<ProjectPhotoHashService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    // ── Publieke interface ────────────────────────────────────────────────────

    public async Task UpdateProjectPhotosAsync(
        long marketAssetId,
        int sourceId,
        string externalId,
        IReadOnlyList<string> photoUrls,
        CancellationToken ct)
    {
        var ph = _settings.PhotoHashing;

        if (!ph.EnableProjectPhotoHashing)
        {
            _logger.LogInformation("[PhotoHash] Skipped — EnableProjectPhotoHashing=false | ExternalId={Id}", externalId);
            return;
        }
        if (!ph.DownloadProjectPhotos)
        {
            _logger.LogInformation("[PhotoHash] SkippedDownload — DownloadProjectPhotos=false | ExternalId={Id}", externalId);
            return;
        }
        if (photoUrls.Count == 0)
        {
            _logger.LogInformation("[PhotoHash] NoProjectPhotosFound | ExternalId={Id}", externalId);
            return;
        }

        // Bronnaam opzoeken (eenmalig per call)
        var sourceName = await _db.CrawlerSources
            .Where(s => s.Id == sourceId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(ct) ?? $"source_{sourceId}";

        // Reeds bekende NormalizedImageUrls voor dit project — geen dubbele downloads
        var existingUrls = await _db.ProjectPhotoHashes
            .Where(h => h.MarketAssetId == marketAssetId)
            .Select(h => h.NormalizedImageUrl)
            .ToHashSetAsync(ct);

        var toProcess = photoUrls
            .Select(u => (Raw: u, Normalized: NormalizeImageUrl(u)))
            .Where(t => !string.IsNullOrEmpty(t.Normalized))
            .DistinctBy(t => t.Normalized, StringComparer.OrdinalIgnoreCase)
            .Take(ph.MaxProjectPhotosPerProject)
            .ToList();

        _logger.LogInformation(
            "[PhotoHash] Start | Source={Source} | ExternalId={Id} | PhotoUrlsInput={Input} | AfterDedup={Dedup}",
            sourceName, externalId, photoUrls.Count, toProcess.Count);

        var now = DateTime.UtcNow;
        var order = 0;
        var cacheHits = 0;
        var downloadAttempted = 0;
        var downloadSucceeded = 0;
        var downloadFailed = 0;
        var contentHashesCreated = 0;
        var perceptualHashesCreated = 0;

        // Verzamel hashes voor debug-bestand
        var debugHashes = _settings.Debug.Enabled
            ? new List<object>()
            : null;

        using var httpClient = _httpClientFactory.CreateClient("MarketDataClient");
        httpClient.Timeout = TimeSpan.FromSeconds(ph.PhotoHashTimeoutSeconds);

        foreach (var (rawUrl, normalizedUrl) in toProcess)
        {
            if (ct.IsCancellationRequested) break;

            // Reeds gecached en recent genoeg?
            if (existingUrls.Contains(normalizedUrl))
            {
                var existing = await _db.ProjectPhotoHashes
                    .FirstOrDefaultAsync(h =>
                        h.MarketAssetId == marketAssetId &&
                        h.NormalizedImageUrl == normalizedUrl, ct);

                if (existing is not null)
                {
                    var age = (now - existing.DownloadedAt).TotalDays;
                    if (age < ph.RehashPhotosAfterDays)
                    {
                        _logger.LogInformation(
                            "[PhotoHash] CacheHit | ExternalId={Id} | ImageUrl={Url} | " +
                            "ContentHash={CH} | PerceptualHash={PH}",
                            externalId, normalizedUrl,
                            existing.ContentHash[..Math.Min(16, existing.ContentHash.Length)],
                            existing.PerceptualHash?.ToString() ?? "(none)");
                        debugHashes?.Add(new
                        {
                            imageUrl = rawUrl, normalizedUrl, source = "cache",
                            contentHash = existing.ContentHash, perceptualHash = existing.PerceptualHash
                        });
                        cacheHits++;
                        order++;
                        continue;
                    }
                }
            }

            try
            {
                downloadAttempted++;
                _logger.LogInformation(
                    "[PhotoHash] Download | ExternalId={Id} | ImageUrl={Url}",
                    externalId, rawUrl);

                var bytes = await DownloadImageAsync(httpClient, rawUrl, ph.PhotoHashMaxBytes, ct);
                if (bytes is null)
                {
                    downloadFailed++;
                    _logger.LogInformation("[PhotoHash] DownloadFailed | ExternalId={Id} | ImageUrl={Url}", externalId, rawUrl);
                    continue;
                }

                downloadSucceeded++;
                var (contentHash, perceptualHash, width, height) = HashImage(bytes);
                contentHashesCreated++;
                if (perceptualHash.HasValue) perceptualHashesCreated++;

                _logger.LogInformation(
                    "[PhotoHash] HashCreated | ExternalId={Id} | ImageUrl={Url} | " +
                    "ContentHash={CH} | PerceptualHash={PH} | Size={W}x{H}",
                    externalId, rawUrl,
                    contentHash[..Math.Min(16, contentHash.Length)],
                    perceptualHash?.ToString() ?? "(none)",
                    width?.ToString() ?? "?", height?.ToString() ?? "?");

                debugHashes?.Add(new
                {
                    imageUrl = rawUrl, normalizedUrl, source = "download",
                    contentHash, perceptualHash, width, height
                });

                var entity = await _db.ProjectPhotoHashes
                    .FirstOrDefaultAsync(h =>
                        h.MarketAssetId == marketAssetId &&
                        h.NormalizedImageUrl == normalizedUrl, ct);

                if (entity is null)
                {
                    entity = new ProjectPhotoHash
                    {
                        MarketAssetId         = marketAssetId,
                        SourceName            = sourceName,
                        ProjectExternalId     = externalId,
                        ImageUrl              = rawUrl,
                        NormalizedImageUrl    = normalizedUrl,
                        ImageOrder            = order,
                        HashAlgorithm         = HashAlgorithmVersion,
                        ContentHash           = contentHash,
                        PerceptualHash        = perceptualHash,
                        PerceptualHashVersion = PerceptualHashVersion,
                        Width                 = width,
                        Height                = height,
                        DownloadedAt          = now,
                        CreatedAt             = now,
                        UpdatedAt             = now
                    };
                    _db.ProjectPhotoHashes.Add(entity);
                }
                else
                {
                    entity.ContentHash         = contentHash;
                    entity.PerceptualHash      = perceptualHash;
                    entity.Width               = width;
                    entity.Height              = height;
                    entity.ImageOrder          = order;
                    entity.DownloadedAt        = now;
                    entity.UpdatedAt           = now;
                }

                await _db.SaveChangesAsync(ct);
                existingUrls.Add(normalizedUrl);
                order++;
            }
            catch (Exception ex)
            {
                downloadFailed++;
                _logger.LogWarning(
                    "[PhotoHash] PhotoHashFailed: {ExternalId} url={Url} err={Err}",
                    externalId, rawUrl, ex.Message);
            }
        }

        _logger.LogInformation(
            "[PhotoHash] Done | ExternalId={Id} | CacheHits={CH} | DownloadAttempted={DA} | " +
            "DownloadSucceeded={DS} | DownloadFailed={DF} | ContentHashes={ContentH} | PerceptualHashes={PH}",
            externalId, cacheHits, downloadAttempted, downloadSucceeded, downloadFailed,
            contentHashesCreated, perceptualHashesCreated);

        if (debugHashes is not null)
        {
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "debug", "photohash");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"{sourceName}-{externalId}.json");
                var debugDoc = new
                {
                    projectId  = marketAssetId,
                    externalId,
                    imageCount = debugHashes.Count,
                    hashes     = debugHashes
                };
                File.WriteAllText(path,
                    JsonSerializer.Serialize(debugDoc, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger.LogDebug("[PhotoHash] Debug-bestand schrijven mislukt voor {Id}: {Err}", externalId, ex.Message);
            }
        }
    }

    public async Task<(int ContentMatches, int PerceptualMatches)> CompareProjectPhotosAsync(
        long assetId1,
        long assetId2,
        CancellationToken ct)
    {
        var hashes1 = await _db.ProjectPhotoHashes
            .Where(h => h.MarketAssetId == assetId1)
            .AsNoTracking()
            .ToListAsync(ct);

        var hashes2 = await _db.ProjectPhotoHashes
            .Where(h => h.MarketAssetId == assetId2)
            .AsNoTracking()
            .ToListAsync(ct);

        if (hashes1.Count == 0 || hashes2.Count == 0) return (0, 0);

        var contentSet1 = hashes1.Select(h => h.ContentHash).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var contentMatches = hashes2.Count(h => contentSet1.Contains(h.ContentHash));

        var threshold = _settings.PhotoHashing.PhotoPerceptualHashMaxDistance;
        var perceptualMatches = 0;
        var matched2 = new HashSet<int>();

        for (int i = 0; i < hashes1.Count; i++)
        {
            if (hashes1[i].PerceptualHash is null) continue;
            for (int j = 0; j < hashes2.Count; j++)
            {
                if (matched2.Contains(j)) continue;
                if (hashes2[j].PerceptualHash is null) continue;
                var dist = HammingDistance(hashes1[i].PerceptualHash!.Value, hashes2[j].PerceptualHash!.Value);
                if (dist <= threshold)
                {
                    perceptualMatches++;
                    matched2.Add(j);
                    break;
                }
            }
        }

        return (contentMatches, perceptualMatches);
    }

    public async Task<List<ProjectPhotoHash>> GetByAssetIdAsync(long assetId, CancellationToken ct)
        => await _db.ProjectPhotoHashes
            .Where(h => h.MarketAssetId == assetId)
            .AsNoTracking()
            .OrderBy(h => h.ImageOrder)
            .ToListAsync(ct);

    // ── Interne hulpmethoden ──────────────────────────────────────────────────

    private static async Task<byte[]?> DownloadImageAsync(
        HttpClient client, string url, int maxBytes, CancellationToken ct)
    {
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > maxBytes)
            {
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                ms.Write(buffer, 0, read);
                if (ms.Length > maxBytes) return null;
            }
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static (string ContentHash, long? PerceptualHash, int? Width, int? Height) HashImage(byte[] bytes)
    {
        var contentHash = Convert.ToHexString(SHA256.HashData(bytes));
        long? perceptualHash = null;
        int? width = null;
        int? height = null;

        try
        {
            using var image = Image.Load<L8>(bytes);
            width  = image.Width;
            height = image.Height;
            perceptualHash = ComputeDHash(image);
        }
        catch
        {
            // Corrupte of niet-ondersteunde afbeelding — ContentHash blijft geldig
        }

        return (contentHash, perceptualHash, width, height);
    }

    /// <summary>
    /// dHash (difference hash): resize naar 9×8 grijswaarden, vergelijk aangrenzende pixels per rij.
    /// Resultaat: 64-bit hash opgeslagen als <see cref="long"/> (signed reinterpretatie van ulong).
    /// </summary>
    internal static long ComputeDHash(Image<L8> image)
    {
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(9, 8),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.Bicubic
        }));

        ulong hash = 0UL;
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
        {
            if (image[x, y].PackedValue > image[x + 1, y].PackedValue)
                hash |= 1UL << (y * 8 + x);
        }

        return (long)hash;
    }

    internal static int HammingDistance(long a, long b)
        => BitOperations.PopCount((ulong)(a ^ b));

    /// <summary>
    /// Normaliseert een image-URL door resolutiecomponenten te verwijderen (bijv. /736x736/).
    /// Zorgt dat dezelfde foto met verschillende resolutie als één URL wordt beschouwd.
    /// </summary>
    internal static string NormalizeImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        // Verwijder /WxH/ resolutie-segment (bijv. /736x736/ of /500x380/)
        return Regex.Replace(url, @"/\d+x\d+(?=/)", "");
    }
}
