using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace GroupLN.MarketData.Infrastructure.GeoLocation;

public class AdminVectorImportService : IAdminVectorImportService
{
    private readonly MarketDataDbContext _context;
    private readonly ILogger<AdminVectorImportService> _logger;

    public AdminVectorImportService(MarketDataDbContext context, ILogger<AdminVectorImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int MunicipalitiesImported, int SectionsImported)> ImportAsync(
        string geoPackagePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(geoPackagePath))
            throw new FileNotFoundException($"GeoPackage niet gevonden: {geoPackagePath}");

        _logger.LogInformation("AdminVector import gestart vanuit: {Path}", geoPackagePath);

        var municipalitiesImported = await ImportMunicipalitiesAsync(geoPackagePath, cancellationToken);
        var sectionsImported = await ImportMunicipalSectionsAsync(geoPackagePath, cancellationToken);

        _logger.LogInformation(
            "AdminVector import klaar. Gemeenten: {M} | Deelgemeenten: {S}",
            municipalitiesImported, sectionsImported);

        return (municipalitiesImported, sectionsImported);
    }

    private async Task<int> ImportMunicipalitiesAsync(string geoPackagePath, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var imported = 0;

        var existing = await _context.GeoMunicipalities
            .ToDictionaryAsync(m => m.NisCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

        using var connection = new SqliteConnection($"Data Source={geoPackagePath};Mode=ReadOnly;");
        await connection.OpenAsync(cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT niscode, nameger, namefre, namedut, shape FROM municipality";

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var batch = new List<GeoMunicipality>();

        while (await reader.ReadAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) break;

            var nisCode = reader.GetString(0)?.Trim() ?? string.Empty;
            var nameGer = reader.IsDBNull(1) ? null : reader.GetString(1);
            var nameFre = reader.IsDBNull(2) ? null : reader.GetString(2);
            var nameDut = reader.IsDBNull(3) ? null : reader.GetString(3);
            var shapeBytes = (byte[])reader.GetValue(4);

            Geometry boundary;
            try
            {
                boundary = GeoPackageBinaryParser.Parse(shapeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geometrie parseren mislukt voor gemeente {NisCode} — overgeslagen.", nisCode);
                continue;
            }

            if (existing.TryGetValue(nisCode, out var entity))
            {
                entity.NameDutch = nameDut;
                entity.NameFrench = nameFre;
                entity.NameGerman = nameGer;
                entity.Boundary = boundary;
            }
            else
            {
                entity = new GeoMunicipality
                {
                    NisCode = nisCode,
                    NameDutch = nameDut,
                    NameFrench = nameFre,
                    NameGerman = nameGer,
                    Boundary = boundary,
                    CreatedAt = now
                };
                _context.GeoMunicipalities.Add(entity);
                existing[nisCode] = entity;
            }

            imported++;

            if (imported % 100 == 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Gemeenten opgeslagen: {Count}", imported);
            }
        }

        if (imported % 100 != 0 || imported == 0)
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Gemeenten geïmporteerd: {Count}", imported);
        return imported;
    }

    private async Task<int> ImportMunicipalSectionsAsync(string geoPackagePath, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var imported = 0;

        var existing = await _context.GeoMunicipalSections
            .ToDictionaryAsync(s => s.PseudoNis, StringComparer.OrdinalIgnoreCase, cancellationToken);

        using var connection = new SqliteConnection($"Data Source={geoPackagePath};Mode=ReadOnly;");
        await connection.OpenAsync(cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pseudonis, zipcode, niscode_municipality, nameger, namefre, namedut, shape FROM municipalsection";

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) break;

            var pseudoNis = reader.GetString(0)?.Trim() ?? string.Empty;
            var zipCode = reader.IsDBNull(1) ? null : reader.GetString(1);
            var nisCodeMunicipality = reader.IsDBNull(2) ? null : reader.GetString(2)?.Trim();
            var nameGer = reader.IsDBNull(3) ? null : reader.GetString(3);
            var nameFre = reader.IsDBNull(4) ? null : reader.GetString(4);
            var nameDut = reader.IsDBNull(5) ? null : reader.GetString(5);
            var shapeBytes = (byte[])reader.GetValue(6);

            Geometry boundary;
            try
            {
                boundary = GeoPackageBinaryParser.Parse(shapeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Geometrie parseren mislukt voor deelgemeente {PseudoNis} — overgeslagen.", pseudoNis);
                continue;
            }

            if (existing.TryGetValue(pseudoNis, out var entity))
            {
                entity.ZipCode = zipCode;
                entity.NisCodeMunicipality = nisCodeMunicipality;
                entity.NameDutch = nameDut;
                entity.NameFrench = nameFre;
                entity.NameGerman = nameGer;
                entity.Boundary = boundary;
            }
            else
            {
                entity = new GeoMunicipalSection
                {
                    PseudoNis = pseudoNis,
                    ZipCode = zipCode,
                    NisCodeMunicipality = nisCodeMunicipality,
                    NameDutch = nameDut,
                    NameFrench = nameFre,
                    NameGerman = nameGer,
                    Boundary = boundary,
                    CreatedAt = now
                };
                _context.GeoMunicipalSections.Add(entity);
                existing[pseudoNis] = entity;
            }

            imported++;

            if (imported % 200 == 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Deelgemeenten opgeslagen: {Count}", imported);
            }
        }

        if (imported % 200 != 0 || imported == 0)
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deelgemeenten geïmporteerd: {Count}", imported);
        return imported;
    }
}

internal static class GeoPackageBinaryParser
{
    // GeoPackage Binary Format: 2-byte magic (0x47 0x50) + 1 version + 1 flags + 4 SRID + envelope + WKB
    private static readonly int[] EnvelopeSizes = { 0, 32, 48, 48, 64 };

    public static Geometry Parse(byte[] bytes)
    {
        if (bytes is null || bytes.Length < 8)
            throw new InvalidDataException("GeoPackage binary te kort.");

        if (bytes[0] != 0x47 || bytes[1] != 0x50)
            throw new InvalidDataException("Ongeldige GeoPackage magic bytes.");

        var flags = bytes[3];
        var envelopeType = (flags >> 1) & 0x07;
        var isEmpty = ((flags >> 4) & 0x01) == 1;

        if (isEmpty)
            return Point.Empty;

        var envelopeSize = envelopeType < EnvelopeSizes.Length ? EnvelopeSizes[envelopeType] : 0;
        var wkbOffset = 8 + envelopeSize;

        if (bytes.Length <= wkbOffset)
            throw new InvalidDataException("GeoPackage binary bevat geen WKB na de header.");

        var wkb = new byte[bytes.Length - wkbOffset];
        Buffer.BlockCopy(bytes, wkbOffset, wkb, 0, wkb.Length);

        var geom = new WKBReader().Read(wkb);
        geom.SRID = 4326;
        return geom;
    }
}
