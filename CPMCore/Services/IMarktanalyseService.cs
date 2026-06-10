using CPMCore.Models.Marktanalyse;

namespace CPMCore.Services;

public interface IMarktanalyseService
{
    Task<List<LocatieOptie>> GetLocatiesAsync(CancellationToken ct = default);

    Task<GemeenteAnalyseViewModel> GetGemeenteAnalyseAsync(
        string? postcode,
        string? gemeente,
        string type,
        string aanbodtype = "Alles",
        CancellationToken ct = default);

    Task<ProjectDetailViewModel?> GetProjectDetailAsync(long id, CancellationToken ct = default);
}
