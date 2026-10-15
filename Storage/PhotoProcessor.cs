using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

/// <summary>
/// Verwerkt één geüpload origineel tot alle varianten (project-foto's, CPMCore "Foto's &amp; video's"):
///   1. EXIF-oriëntatie toepassen (gsm-foto's staan niet meer gekanteld) en alle EXIF/XMP/IPTC verwijderen
///      (kleiner bestand, geen GPS-locatie online); een ICC-profiel blijft behouden voor correcte kleuren.
///   2. Master-JPEG: korte zijde max 5000 px (nooit vergroten), kwaliteit 92 → map pictures/original.
///   3. WebP-varianten op breedte (320/640/960/1280/1920/2560 — hoogte volgt, niets afgesneden, nooit vergroten,
///      kwaliteit 80) → mappen pictures/w{breedte}. Ze worden van de master afgeleid.
///   4. De bestaande varianten blijven: pictures (past in 1280×960), pictures/800 en pictures/447 (vierkant, bijgesneden).
/// Het geüploade origineel wordt niet bewaard: de aanroeper verwijdert het tijdelijke bestand.
/// Limieten (aanroeper en endpoint): max 50 MB, max 10.000 px aan de lange zijde.
/// </summary>
public static class PhotoProcessor
{
    public const long MaxBytes = 50L * 1024 * 1024;
    public const int MaxLongSide = 10_000;
    public const int MasterShortSide = 5_000;
    public const int MasterJpegQuality = 92;
    public const int WebpQuality = 80;
    public static readonly int[] Widths = { 320, 640, 960, 1280, 1920, 2560 };

    public const string OriginalFolder = "pictures/original";
    public static string WidthFolder(int width) => $"pictures/w{width}";

    public static bool IsWidthFolder(string folder) =>
        folder.StartsWith("pictures/w", StringComparison.Ordinal)
        && int.TryParse(folder.AsSpan("pictures/w".Length), out var w) && Widths.Contains(w);

    public sealed record Result(string FileName, int Width, int Height, int MasterWidth, int MasterHeight, int[] Widths);

    public sealed class PhotoException(string message) : Exception(message);

    public static async Task<Result> ProcessAsync(Stream input, string baseName, string storageRoot, CancellationToken ct)
    {
        ImageInfo info;
        try { info = await Image.IdentifyAsync(input, ct); }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw new PhotoException("Dit bestand is geen geldige afbeelding (jpg, png of webp).");
        }
        // De lange zijde verandert niet door een EXIF-rotatie, dus dit kan vóór het (zware) decoderen.
        var longSide = Math.Max(info.Width, info.Height);
        if (longSide > MaxLongSide)
            throw new PhotoException($"De foto is {longSide:N0} px aan de lange zijde; het maximum is {MaxLongSide:N0} px.");
        input.Position = 0;

        var created = new List<string>();
        try
        {
            using var image = await Image.LoadAsync(input, ct);
            image.Mutate(x => x.AutoOrient());
            image.Metadata.ExifProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;

            var origWidth = image.Width;
            var origHeight = image.Height;

            // Master: korte zijde max 5000 px, nooit vergroten
            var shortSide = Math.Min(origWidth, origHeight);
            if (shortSide > MasterShortSide)
            {
                var scale = (double)MasterShortSide / shortSide;
                image.Mutate(x => x.Resize((int)Math.Round(origWidth * scale), (int)Math.Round(origHeight * scale)));
            }
            var masterWidth = image.Width;
            var masterHeight = image.Height;

            await SaveAsync(image, Path.Combine(storageRoot, OriginalFolder), baseName + ".jpg", created,
                (img, path) => img.SaveAsJpegAsync(path, new JpegEncoder { Quality = MasterJpegQuality }, ct));

            var webp = new WebpEncoder { Quality = WebpQuality };
            var made = new List<int>();

            // Varianten op breedte (hoogte volgt); nooit groter dan de master
            foreach (var w in Widths)
            {
                if (w > masterWidth) continue;
                using var variant = image.Clone(x => x.Resize(w, 0));
                await SaveAsync(variant, Path.Combine(storageRoot, WidthFolder(w)), baseName + ".webp", created,
                    (img, path) => img.SaveAsWebpAsync(path, webp, ct));
                made.Add(w);
            }

            // Bestaande varianten: pictures (past in 1280×960, niet vergroten), pictures/800 en pictures/447 (vierkant, bijgesneden)
            var fit = Math.Min(1.0, Math.Min(1280.0 / masterWidth, 960.0 / masterHeight));
            using (var main = image.Clone(x => x.Resize(Math.Max(1, (int)Math.Round(masterWidth * fit)), Math.Max(1, (int)Math.Round(masterHeight * fit)))))
                await SaveAsync(main, Path.Combine(storageRoot, "pictures"), baseName + ".webp", created, (img, path) => img.SaveAsWebpAsync(path, webp, ct));
            foreach (var size in new[] { 800, 447 })
            {
                using var square = image.Clone(x => x.Resize(new ResizeOptions { Size = new Size(size, size), Mode = ResizeMode.Crop }));
                await SaveAsync(square, Path.Combine(storageRoot, "pictures", size.ToString()), baseName + ".webp", created, (img, path) => img.SaveAsWebpAsync(path, webp, ct));
            }

            return new Result(baseName + ".webp", origWidth, origHeight, masterWidth, masterHeight, made.ToArray());
        }
        catch
        {
            // Alles of niets: bij een fout geen halve set varianten achterlaten
            foreach (var f in created) { try { File.Delete(f); } catch { /* best-effort */ } }
            throw;
        }
    }

    private static async Task SaveAsync(Image img, string folder, string fileName, List<string> created, Func<Image, string, Task> save)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);
        created.Add(path);
        await save(img, path);
    }

    /// <summary>
    /// Maakt voor een bestaande (oudere) foto de ontbrekende breedte-varianten aan, afgeleid van het bestand in "pictures"
    /// (nooit groter dan dat bestand, dus meestal tot 1280). De varianten krijgen dezelfde bestandsnaam en hetzelfde formaat als de bron
    /// (WebP voor .webp, JPEG voor .jpg/.jpeg, PNG voor .png), zodat de naam in de database voor alle mappen klopt.
    /// De EXIF-oriëntatie wordt toegepast en EXIF verwijderd; de teruggegeven afmetingen zijn die van het georiënteerde beeld.
    /// Er is geen master-jpeg: de oorspronkelijke foto is nooit bewaard. Geeft null als de bron niet bestaat of het type niet ondersteund is.
    /// </summary>
    public static async Task<(int Width, int Height, int[] Widths)?> BackfillAsync(string storageRoot, string fileName, CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not (".webp" or ".jpg" or ".jpeg" or ".png")) return null;
        var source = Path.Combine(storageRoot, "pictures", fileName);
        if (!File.Exists(source)) return null;

        using var image = await Image.LoadAsync(source, ct);
        image.Mutate(x => x.AutoOrient());
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;

        var made = new List<int>();
        foreach (var w in Widths)
        {
            if (w > image.Width) continue;
            var folder = Path.Combine(storageRoot, WidthFolder(w));
            Directory.CreateDirectory(folder);
            var target = Path.Combine(folder, fileName);
            if (!File.Exists(target))
            {
                using var variant = image.Clone(x => x.Resize(w, 0));
                switch (ext)
                {
                    case ".webp": await variant.SaveAsWebpAsync(target, new WebpEncoder { Quality = WebpQuality }, ct); break;
                    case ".png": await variant.SaveAsPngAsync(target, ct); break;
                    default: await variant.SaveAsJpegAsync(target, new JpegEncoder { Quality = 82 }, ct); break;
                }
            }
            made.Add(w);
        }
        return (image.Width, image.Height, made.ToArray());
    }

    /// <summary>Verwijdert alle varianten van één foto (volledige bestandsnaam uit de database, bv. "2026….webp" of "2025….jpg"); best-effort.</summary>
    public static int DeleteAll(string storageRoot, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var targets = new List<string>
        {
            Path.Combine(storageRoot, OriginalFolder, baseName + ".jpg"),
            Path.Combine(storageRoot, "pictures", fileName),
            Path.Combine(storageRoot, "pictures", "800", fileName),
            Path.Combine(storageRoot, "pictures", "447", fileName),
        };
        targets.AddRange(Widths.Select(w => Path.Combine(storageRoot, WidthFolder(w), fileName)));
        var deleted = 0;
        foreach (var t in targets)
        {
            try { if (File.Exists(t)) { File.Delete(t); deleted++; } } catch { /* best-effort */ }
        }
        return deleted;
    }

}
