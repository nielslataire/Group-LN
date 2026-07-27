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

    // ── Zoekhistoriek & opgeslagen profielen ("Vergelijkbare panden") ─────────

    /// <summary>Logt een uitgevoerde zoekopdracht (voor "Laatste zoekacties" en "Snel starten").</summary>
    Task LogZoekActieAsync(int userId, VergelijkbarePandenZoekCriteria criteria, int aantalResultaten, CancellationToken ct = default);

    /// <summary>De meest recente individuele zoekopdrachten van de gebruiker.</summary>
    Task<List<VergelijkbarePandenSnelkoppelingViewModel>> GetRecenteZoekActiesAsync(int userId, int take, CancellationToken ct = default);

    /// <summary>De meest gebruikte criteria-combinaties van de gebruiker (frequentie-gebaseerd).</summary>
    Task<List<VergelijkbarePandenSnelkoppelingViewModel>> GetSnelStartPresetsAsync(int userId, int take, CancellationToken ct = default);

    /// <summary>Slaat het huidige zoekprofiel op onder een naam; retourneert het nieuwe Id.</summary>
    Task<int> SaveZoekProfielAsync(int userId, string naam, VergelijkbarePandenZoekCriteria criteria, CancellationToken ct = default);

    /// <summary>De opgeslagen zoekprofielen van de gebruiker, meest recent eerst.</summary>
    Task<List<VergelijkbarePandenSnelkoppelingViewModel>> GetOpgeslagenProfielenAsync(int userId, int take, CancellationToken ct = default);
}
