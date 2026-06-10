using CPMCore.Models.Marktanalyse;

namespace CPMCore.Services;

public interface IMarktanalyseService
{
    Task<List<GemeenteGroep>> GetLocatiesAsync(CancellationToken ct = default);

    Task<GemeenteAnalyseViewModel> GetGemeenteAnalyseAsync(
        int? geoMunicipalityId,
        int? geoMunicipalSectionId,
        string type,
        string aanbodtype = "Alles",
        CancellationToken ct = default);

    Task<ProjectDetailViewModel?> GetProjectDetailAsync(long id, CancellationToken ct = default);
}
