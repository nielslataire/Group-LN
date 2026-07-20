using CPMCore.Models.Marktanalyse;

namespace CPMCore.Services;

public interface IMarktanalyseService
{
    Task<List<GemeenteGroep>> GetLocatiesAsync(CancellationToken ct = default);

    Task<GemeenteAnalyseViewModel> GetGemeenteAnalyseAsync(
        int? geoMunicipalityId,
        int? geoMunicipalSectionId,
        string type,
        string aanbodtype      = "Alles",
        bool toonGekoppeld     = false,
        CancellationToken ct   = default);

    Task<ProjectDetailViewModel?> GetProjectDetailAsync(long id, CancellationToken ct = default);

    Task<VergelijkbarePandenViewModel> GetVergelijkbarePandenAsync(
        List<int> gemeenteIds,
        string? rondAdresPostcode,
        int rondAdresStraal,
        string type,
        decimal? oppervlakte,
        int tolerantie,
        decimal? prijsMin,
        decimal? prijsMax,
        int? slaapkamers,
        string status,
        CancellationToken ct = default);
}
