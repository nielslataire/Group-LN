using Microsoft.AspNetCore.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AssetStorageSettings>(builder.Configuration.GetSection(AssetStorageSettings.SectionName));
builder.Services.AddSingleton<AssetSigningHelper>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

var app = builder.Build();
var logger = app.Logger;

AssetStorageSettings settings;
try
{
    settings = app.Services.GetRequiredService<IOptions<AssetStorageSettings>>().Value;
    settings.Validate();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "AssetStorage configuration is invalid.");
    throw;
}

var rootPath = Path.Combine(app.Environment.ContentRootPath, settings.RootPath);
var picturesPath = Path.Combine(rootPath, AssetFolders.Pictures);
var plansPath = Path.Combine(rootPath, AssetFolders.Plans);
var docsPath = Path.Combine(rootPath, AssetFolders.Docs);

Directory.CreateDirectory(rootPath);
Directory.CreateDirectory(picturesPath);
Directory.CreateDirectory(plansPath);
Directory.CreateDirectory(docsPath);

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        if (exception is not null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    });
});

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(picturesPath),
    RequestPath = "/pictures",
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    }
});

app.MapPost("/api/assets/upload", async (HttpRequest request, IOptions<AssetStorageSettings> options, AssetSigningHelper signingHelper, IWebHostEnvironment env, ILoggerFactory loggerFactory) =>
{
    var endpointLogger = loggerFactory.CreateLogger("AssetUpload");
    var localSettings = options.Value;

    if (!TryValidateApiKey(request, localSettings.WriteApiKey))
    {
        return Results.Unauthorized();
    }

    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "Request must be multipart/form-data." });
    }

    var form = await request.ReadFormAsync();
    var folder = form["folder"].ToString().Trim().ToLowerInvariant();

    if (!AssetFolders.IsValid(folder))
    {
        return Results.BadRequest(new { error = "Invalid folder. Allowed values: pictures, plans, docs." });
    }

    var file = form.Files["file"];
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "File is required." });
    }

    if (file.Length > localSettings.MaxUploadBytes)
    {
        return Results.BadRequest(new { error = $"File exceeds max upload size of {localSettings.MaxUploadBytes} bytes." });
    }

    var extension = Path.GetExtension(file.FileName);
    var generatedFileName = $"{Guid.NewGuid():N}{extension}";

    var storageRoot = Path.Combine(env.ContentRootPath, localSettings.RootPath);
    var targetFolderPath = Path.Combine(storageRoot, folder);
    Directory.CreateDirectory(targetFolderPath);

    var targetFilePath = Path.Combine(targetFolderPath, generatedFileName);

    await using (var stream = File.Create(targetFilePath))
    {
        await file.CopyToAsync(stream);
    }

    string? publicUrl = null;
    string? downloadUrl = null;

    if (folder == AssetFolders.Pictures)
    {
        publicUrl = $"/pictures/{generatedFileName}";
    }
    else
    {
        downloadUrl = signingHelper.BuildSignedUrl(folder, generatedFileName, localSettings.SignedUrlExpiryMinutes);
    }

    endpointLogger.LogInformation("Uploaded file {FileName} to folder {Folder}", generatedFileName, folder);

    return Results.Ok(new
    {
        id = generatedFileName,
        folder,
        fileName = generatedFileName,
        publicUrl,
        downloadUrl
    });
});

app.MapPost("/api/assets/{folder}/{fileName}/sign", (HttpRequest request, string folder, string fileName, IOptions<AssetStorageSettings> options, AssetSigningHelper signingHelper) =>
{
    var localSettings = options.Value;

    if (!TryValidateReadOrWriteApiKey(request, localSettings))
    {
        return Results.Unauthorized();
    }

    folder = folder.Trim().ToLowerInvariant();
    if (!AssetFolders.IsPrivate(folder))
    {
        return Results.BadRequest(new { error = "Signed URL is only available for plans and docs." });
    }

    if (!IsSafeFileName(fileName))
    {
        return Results.BadRequest(new { error = "Invalid file name." });
    }

    var signedUrl = signingHelper.BuildSignedUrl(folder, fileName, localSettings.SignedUrlExpiryMinutes);
    return Results.Ok(new { url = signedUrl });
});

app.MapGet("/api/assets/private/{folder}/{fileName}", (string folder, string fileName, long exp, string sig, IOptions<AssetStorageSettings> options, IWebHostEnvironment env, AssetSigningHelper signingHelper) =>
{
    var localSettings = options.Value;

    folder = folder.Trim().ToLowerInvariant();

    if (!AssetFolders.IsPrivate(folder))
    {
        return Results.BadRequest(new { error = "Invalid private folder." });
    }

    if (!IsSafeFileName(fileName))
    {
        return Results.BadRequest(new { error = "Invalid file name." });
    }

    var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
    if (expiry <= DateTimeOffset.UtcNow)
    {
        return Results.Unauthorized();
    }

    if (!signingHelper.IsValidSignature(folder, fileName, exp, sig))
    {
        return Results.Unauthorized();
    }

    var storageRoot = Path.Combine(env.ContentRootPath, localSettings.RootPath);
    var filePath = Path.Combine(storageRoot, folder, fileName);

    if (!File.Exists(filePath))
    {
        return Results.NotFound();
    }

    var provider = new FileExtensionContentTypeProvider();
    var contentType = provider.TryGetContentType(fileName, out var detectedType)
        ? detectedType
        : "application/octet-stream";

    return Results.File(filePath, contentType, enableRangeProcessing: true);
});

app.Run();

static bool TryValidateApiKey(HttpRequest request, string expectedApiKey)
{
    if (!request.Headers.TryGetValue("X-Api-Key", out var providedApiKey))
    {
        return false;
    }

    return string.Equals(providedApiKey.ToString(), expectedApiKey, StringComparison.Ordinal);
}

static bool TryValidateReadOrWriteApiKey(HttpRequest request, AssetStorageSettings settings)
{
    if (!request.Headers.TryGetValue("X-Api-Key", out var providedApiKey))
    {
        return false;
    }

    var key = providedApiKey.ToString();
    return string.Equals(key, settings.ReadApiKey, StringComparison.Ordinal)
           || string.Equals(key, settings.WriteApiKey, StringComparison.Ordinal);
}

static bool IsSafeFileName(string fileName)
{
    if (string.IsNullOrWhiteSpace(fileName))
    {
        return false;
    }

    if (fileName.Contains("/", StringComparison.Ordinal) || fileName.Contains("\\", StringComparison.Ordinal))
    {
        return false;
    }

    return fileName == Path.GetFileName(fileName);
}

sealed class AssetStorageSettings
{
    public const string SectionName = "AssetStorage";
    public string RootPath { get; set; } = "AssetStorage";
    public string ReadApiKey { get; set; } = string.Empty;
    public string WriteApiKey { get; set; } = string.Empty;
    public string SignedUrlSecret { get; set; } = string.Empty;
    public int SignedUrlExpiryMinutes { get; set; } = 5;
    public long MaxUploadBytes { get; set; } = 20 * 1024 * 1024;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RootPath)) throw new InvalidOperationException("AssetStorage:RootPath is required.");
        if (string.IsNullOrWhiteSpace(ReadApiKey)) throw new InvalidOperationException("AssetStorage:ReadApiKey is required.");
        if (string.IsNullOrWhiteSpace(WriteApiKey)) throw new InvalidOperationException("AssetStorage:WriteApiKey is required.");
        if (string.IsNullOrWhiteSpace(SignedUrlSecret)) throw new InvalidOperationException("AssetStorage:SignedUrlSecret is required.");
        if (SignedUrlExpiryMinutes <= 0) throw new InvalidOperationException("AssetStorage:SignedUrlExpiryMinutes must be > 0.");
    }
}

sealed class AssetSigningHelper
{
    private readonly IOptions<AssetStorageSettings> _options;

    public AssetSigningHelper(IOptions<AssetStorageSettings> options)
    {
        _options = options;
    }

    public string BuildSignedUrl(string folder, string fileName, int expiryMinutes)
    {
        var exp = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes).ToUnixTimeSeconds();
        var sig = ComputeSignature(folder, fileName, exp);
        return $"/api/assets/private/{folder}/{Uri.EscapeDataString(fileName)}?exp={exp}&sig={sig}";
    }

    public bool IsValidSignature(string folder, string fileName, long exp, string providedSignature)
    {
        try
        {
            var expected = ComputeSignature(folder, fileName, exp);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(providedSignature));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string ComputeSignature(string folder, string fileName, long exp)
    {
        var settings = _options.Value;
        var payload = $"{folder}{fileName}{exp}";

        using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(settings.SignedUrlSecret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

static class AssetFolders
{
    public const string Pictures = "pictures";
    public const string Plans = "plans";
    public const string Docs = "docs";

    public static bool IsValid(string folder) => folder is Pictures or Plans or Docs;
    public static bool IsPrivate(string folder) => folder is Plans or Docs;
}