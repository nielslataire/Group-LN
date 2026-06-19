using GroupLN.MarketData.Core.Entities;

namespace GroupLN.MarketData.Core.Interfaces;

public interface IProjectPhotoHashService
{
    /// <summary>
    /// Download en hash maximaal <c>MaxProjectPhotosPerProject</c> foto's voor een project.
    /// Sla op in ProjectPhotoHash. Fail-safe: fouten worden gelogd, nooit gegooid.
    /// </summary>
    Task UpdateProjectPhotosAsync(
        long marketAssetId,
        int sourceId,
        string externalId,
        IReadOnlyList<string> photoUrls,
        CancellationToken ct);

    /// <summary>
    /// Vergelijk de foto-hashes van twee projecten.
    /// Geeft (contentMatches, perceptualMatches) terug.
    /// </summary>
    Task<(int ContentMatches, int PerceptualMatches)> CompareProjectPhotosAsync(
        long assetId1,
        long assetId2,
        CancellationToken ct);

    /// <summary>
    /// Geeft alle foto-hashes terug voor een project.
    /// </summary>
    Task<List<ProjectPhotoHash>> GetByAssetIdAsync(long assetId, CancellationToken ct);
}
