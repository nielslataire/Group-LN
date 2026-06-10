using GroupLN.MarketData.Core.Interfaces;
using GroupLN.MarketData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GroupLN.MarketData.Infrastructure.Commands;

public class BackfillMarketAssetLocationsCommand
{
    private readonly MarketDataDbContext _context;
    private readonly ILocationResolver _locationResolver;
    private readonly ILogger<BackfillMarketAssetLocationsCommand> _logger;

    public BackfillMarketAssetLocationsCommand(
        MarketDataDbContext context,
        ILocationResolver locationResolver,
        ILogger<BackfillMarketAssetLocationsCommand> logger)
    {
        _context = context;
        _locationResolver = locationResolver;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("BackfillMarketAssetLocations gestart.");

        var assets = await _context.MarketAssets
            .Where(a => a.Latitude != null && a.Longitude != null && a.GeoMunicipalSectionId == null)
            .Select(a => new { a.Id, a.Latitude, a.Longitude, a.City, a.PostalCode })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("{Count} assets te resolven.", assets.Count);

        var resolved = 0;
        var failed = 0;
        var batchSize = 50;

        for (var i = 0; i < assets.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var item = assets[i];

            try
            {
                var result = await _locationResolver.ResolveAsync(
                    item.Latitude, item.Longitude, item.City, item.PostalCode, cancellationToken);

                if (result is not null && result.Resolved)
                {
                    var asset = await _context.MarketAssets.FindAsync(new object[] { item.Id }, cancellationToken);
                    if (asset is null) continue;

                    asset.GeoMunicipalityId = result.GeoMunicipalityId;
                    asset.GeoMunicipalSectionId = result.GeoMunicipalSectionId;
                    asset.LocationResolvedAt = result.ResolvedAt;
                    asset.LocationResolutionSource = result.Source;
                    asset.LocationResolutionConfidence = result.Confidence;

                    resolved++;
                }
                else
                {
                    failed++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Resolve mislukt voor asset {AssetId}.", item.Id);
            }

            if ((i + 1) % batchSize == 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Backfill voortgang: {Done}/{Total} | Resolved: {Resolved} | Mislukt: {Failed}",
                    i + 1, assets.Count, resolved, failed);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "BackfillMarketAssetLocations klaar. Resolved: {Resolved} | Mislukt: {Failed} | Totaal: {Total}",
            resolved, failed, assets.Count);

        return resolved;
    }
}
