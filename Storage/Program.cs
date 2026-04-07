using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Drawing;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AssetStorageSettings>(builder.Configuration.GetSection(AssetStorageSettings.SectionName));
builder.Services.AddSingleton<AssetSigningHelper>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://groupln.be",
                "https://www.groupln.be",
                "https://cpm.groupln.be",
                "https://bouwenconstructie.be",
                "https://www.bouwenconstructie.be",
                "https://home-estate.be",
                "https://www.home-estate.be")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
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
app.UseCors();

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
    var generatedFileName = IsSafeFileName(file.FileName)
        ? file.FileName
        : $"{Guid.NewGuid():N}{extension}";

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

    if (AssetFolders.IsPictures(folder))
    {
        publicUrl = $"/{folder}/{generatedFileName}";
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

//app.MapPost("/api/assets/docs/generate-thumbnails", (HttpRequest request, IOptions<AssetStorageSettings> options, IWebHostEnvironment env) =>
//{
//    return GenerateAllDocThumbnails(request, options.Value, env, allowQueryApiKey: false);
//});

//app.MapGet("/api/assets/docs/generate-thumbnails", (HttpRequest request, IOptions<AssetStorageSettings> options, IWebHostEnvironment env) =>
//{
//    return GenerateAllDocThumbnails(request, options.Value, env, allowQueryApiKey: true);
//});

app.MapPost("/api/assets/docs/{fileName}/thumbnail", (HttpRequest request, string fileName, IOptions<AssetStorageSettings> options, IWebHostEnvironment env) =>
{
    var localSettings = options.Value;

    if (!TryValidateApiKey(request, localSettings.WriteApiKey))
    {
        return Results.Unauthorized();
    }

    if (!IsSafeFileName(fileName))
    {
        return Results.BadRequest(new { error = "Invalid file name." });
    }

    var docsFolder = Path.Combine(env.ContentRootPath, localSettings.RootPath, AssetFolders.Docs);
    Directory.CreateDirectory(docsFolder);

    if (!TryGenerateDocThumbnail(docsFolder, fileName, out var thumbName))
    {
        return Results.NotFound(new { error = "Source file not found or thumbnail could not be generated." });
    }

    return Results.Ok(new { thumbFileName = thumbName });
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

app.MapGet("/api/assets/plans/{fileName}/page-image", (HttpRequest request, string fileName, int? page, IOptions<AssetStorageSettings> options, IWebHostEnvironment env) =>
{
    var localSettings = options.Value;

    if (!TryValidateReadOrWriteApiKey(request, localSettings))
    {
        return Results.Unauthorized();
    }

    if (!IsSafeFileName(fileName))
    {
        return Results.BadRequest(new { error = "Invalid file name." });
    }

    var pageNumber = page.GetValueOrDefault(1);
    if (pageNumber < 1)
    {
        pageNumber = 1;
    }

    var plansFolder = Path.Combine(env.ContentRootPath, localSettings.RootPath, AssetFolders.Plans);
    Directory.CreateDirectory(plansFolder);

    var sourcePath = Path.Combine(plansFolder, fileName);
    if (!File.Exists(sourcePath))
    {
        return Results.NotFound(new { error = "Source file not found." });
    }

    var extension = Path.GetExtension(fileName);

    try
    {
        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryRenderPdfPageToJpeg(sourcePath, pageNumber, out var jpegBytes))
            {
                return Results.BadRequest(new { error = "Could not render requested PDF page." });
            }

            return Results.File(jpegBytes, "image/jpeg");
        }

        if (IsImageExtension(extension))
        {
            var bytes = File.ReadAllBytes(sourcePath);
            return Results.File(bytes, GetContentTypeForImageExtension(extension));
        }

        return Results.BadRequest(new { error = "File is not a PDF or supported image." });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

//app.Lifetime.ApplicationStarted.Register(() =>
//{
//    try
//    {
//        logger.LogWarning("Startup thumbnail generation trigger fired.");
//        RunStartupDocThumbnailGeneration(app.Environment, settings, logger);
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "Startup thumbnail generation failed with an exception.");
//    }
//});

app.Run();

static void RunStartupDocThumbnailGeneration(IWebHostEnvironment env, AssetStorageSettings localSettings, ILogger logger)
{
    var docsFolder = Path.Combine(env.ContentRootPath, localSettings.RootPath, AssetFolders.Docs);
    Directory.CreateDirectory(docsFolder);

    logger.LogWarning("Startup thumbnail generation scanning docs folder: {DocsFolder}", docsFolder);

    var sourceFiles = Directory
        .EnumerateFiles(docsFolder)
        .Select(Path.GetFileName)
        .Where(static name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith("thumb_", StringComparison.OrdinalIgnoreCase))
        .Cast<string>()
        .ToList();

    var generated = 0;
    foreach (var sourceFileName in sourceFiles)
    {
        if (TryGenerateDocThumbnail(docsFolder, sourceFileName, out _))
        {
            generated++;
        }
    }

    logger.LogWarning("Startup thumbnail generation finished. Docs folder: {DocsFolder}. Docs processed: {DocsProcessed}. Thumbnails generated: {ThumbnailsGenerated}.", docsFolder, sourceFiles.Count, generated);
}

static bool TryValidateApiKeyOrQuery(HttpRequest request, string expectedApiKey)
{
    if (TryValidateApiKey(request, expectedApiKey))
    {
        return true;
    }

    if (request.Query.TryGetValue("apiKey", out var queryApiKeyValues) && queryApiKeyValues.Count > 0)
    {
        return string.Equals(queryApiKeyValues[0], expectedApiKey, StringComparison.Ordinal);
    }

    return false;
}

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

static IResult GenerateAllDocThumbnails(HttpRequest request, AssetStorageSettings localSettings, IWebHostEnvironment env, bool allowQueryApiKey)
{
    var hasValidApiKey = allowQueryApiKey
        ? TryValidateApiKeyOrQuery(request, localSettings.WriteApiKey)
        : TryValidateApiKey(request, localSettings.WriteApiKey);

    if (!hasValidApiKey)
    {
        return Results.Unauthorized();
    }

    var docsFolder = Path.Combine(env.ContentRootPath, localSettings.RootPath, AssetFolders.Docs);
    Directory.CreateDirectory(docsFolder);

    var sourceFiles = Directory
        .EnumerateFiles(docsFolder)
        .Select(Path.GetFileName)
        .Where(static name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith("thumb_", StringComparison.OrdinalIgnoreCase))
        .Cast<string>()
        .ToList();

    var generated = new List<string>();
    foreach (var sourceFileName in sourceFiles)
    {
        if (TryGenerateDocThumbnail(docsFolder, sourceFileName, out var thumbName))
        {
            generated.Add(thumbName);
        }
    }

    return Results.Ok(new
    {
        docsProcessed = sourceFiles.Count,
        thumbnailsGenerated = generated.Count,
        thumbnails = generated
    });
}
static bool TryGenerateDocThumbnail(string docsFolder, string sourceFileName, out string thumbFileName)
{
    thumbFileName = BuildThumbFileName(sourceFileName);

    if (sourceFileName.StartsWith("thumb_", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var sourcePath = Path.Combine(docsFolder, sourceFileName);
    if (!File.Exists(sourcePath))
    {
        return false;
    }

    var thumbPath = Path.Combine(docsFolder, thumbFileName);

    var extension = Path.GetExtension(sourceFileName);
    if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
    {
        if (TryGeneratePdfFirstPageThumbnail(sourcePath, thumbPath))
        {
            return true;
        }

        CreateFallbackThumbnail(thumbPath);
        return true;
    }

    try
    {
        using var image = SixLabors.ImageSharp.Image.Load(sourcePath);
        if (image.Width > 400)
        {
            var targetHeight = (int)Math.Round((double)image.Height * 400 / image.Width);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(400, targetHeight),
                Mode = ResizeMode.Max
            }));
        }

        image.SaveAsJpeg(thumbPath, new JpegEncoder { Quality = 80 });
        return true;
    }
    catch
    {
        CreateFallbackThumbnail(thumbPath);
        return true;
    }
}

static bool TryGeneratePdfFirstPageThumbnail(string sourcePath, string thumbPath)
{
    try
    {
        using var docReader = DocLib.Instance.GetDocReader(sourcePath, new PageDimensions(1200, 1600));
        using var pageReader = docReader.GetPageReader(0);

        var width = pageReader.GetPageWidth();
        var height = pageReader.GetPageHeight();
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var rawBytes = pageReader.GetImage();
        using var pageImage = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(rawBytes, width, height);
        using var flattened = new Image<Rgba32>(pageImage.Width, pageImage.Height, SixLabors.ImageSharp.Color.White);
        flattened.Mutate(ctx => ctx.DrawImage(pageImage, 1f));

        if (flattened.Width > 400)
        {
            var targetHeight = (int)Math.Round((double)flattened.Height * 400 / flattened.Width);
            flattened.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(400, targetHeight),
                Mode = ResizeMode.Max
            }));
        }

        flattened.SaveAsJpeg(thumbPath, new JpegEncoder { Quality = 80 });
        return true;
    }
    catch
    {
        return false;
    }
}

static bool TryRenderPdfPageToJpeg(string sourcePath, int pageNumber, out byte[] jpegBytes)
{
    jpegBytes = Array.Empty<byte>();

    try
    {
        using var docReader = DocLib.Instance.GetDocReader(sourcePath, new PageDimensions(1600, 2200));

        var pageCount = docReader.GetPageCount();
        if (pageCount <= 0)
        {
            return false;
        }

        var pageIndex = Math.Min(Math.Max(pageNumber - 1, 0), pageCount - 1);

        using var pageReader = docReader.GetPageReader(pageIndex);

        var width = pageReader.GetPageWidth();
        var height = pageReader.GetPageHeight();
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var rawBytes = pageReader.GetImage();
        using var pageImage = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(rawBytes, width, height);
        using var flattened = new Image<Rgba32>(pageImage.Width, pageImage.Height, SixLabors.ImageSharp.Color.White);

        flattened.Mutate(ctx => ctx.DrawImage(pageImage, 1f));

        using var output = new MemoryStream();
        flattened.SaveAsJpeg(output, new JpegEncoder { Quality = 90 });
        jpegBytes = output.ToArray();

        return jpegBytes.Length > 0;
    }
    catch
    {
        return false;
    }
}

static bool IsImageExtension(string? extension)
{
    return string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
        || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
        || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
        || string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase)
        || string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase);
}

static string GetContentTypeForImageExtension(string? extension)
{
    if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
        return "image/png";
    if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
        return "image/gif";
    if (string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
        return "image/webp";

    return "image/jpeg";
}


static void CreateFallbackThumbnail(string thumbPath)
{
    using var fallback = new Image<Rgba32>(400, 240, SixLabors.ImageSharp.Color.ParseHex("#f4f5f7"));
    fallback.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.ParseHex("#1bb788").WithAlpha(0.20f)));
    fallback.SaveAsJpeg(thumbPath, new JpegEncoder { Quality = 80 });
}

static string BuildThumbFileName(string sourceFileName)
{
    var baseName = Path.GetFileNameWithoutExtension(sourceFileName);
    return $"thumb_{baseName}.jpg";
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
    public const string Pictures447 = "pictures/447";
    public const string Pictures800 = "pictures/800";
    public const string PicturesNews = "pictures/news";
    public const string PicturesNewsOriginal = "pictures/news/original";
    public const string PicturesNews800 = "pictures/news/800";
    public const string Plans = "plans";
    public const string Docs = "docs";

    public static bool IsValid(string folder) =>
        folder is Pictures or Pictures447 or Pictures800
            or PicturesNews or PicturesNewsOriginal or PicturesNews800
            or Plans or Docs;

    public static bool IsPrivate(string folder) => folder is Plans or Docs;
    public static bool IsPictures(string folder) => folder.StartsWith("pictures", StringComparison.Ordinal);
}