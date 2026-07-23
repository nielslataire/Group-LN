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
        double? rondAdresLat,
        double? rondAdresLng,
        int rondAdresStraal,
        string type,
        decimal? oppervlakte,
        int tolerantie,
        decimal? prijsMin,
        decimal? prijsMax,
        int? slaapkamers,
        string status,
        CancellationToken ct = default);

    /// <summary>Geocodeert een vrij ingevoerd adres/postcode naar coördinaten (via Nominatim).</summary>
    Task<GeocodeAdresResult?> GeocodeAdresAsync(string adres, CancellationToken ct = default);

    /// <summary>Lichtgewicht live telling van panden binnen een straal — voor de kaart-preview,
    /// zonder de volledige resultatenlijst op te bouwen.</summary>
    Task<int> TelPandenInStraalAsync(
        double lat,
        double lng,
        int straalMeter,
        string type,
        decimal? oppervlakte,
        int tolerantie,
        decimal? prijsMin,
        decimal? prijsMax,
        int? slaapkamers,
        string status,
        CancellationToken ct = default);
}
