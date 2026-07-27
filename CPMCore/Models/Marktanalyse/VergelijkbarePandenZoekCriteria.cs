namespace CPMCore.Models.Marktanalyse;

/// <summary>Eén set filtercriteria voor "Vergelijkbare panden" — gebruikt om een zoekactie te
/// loggen, een profiel op te slaan, en om beide terug in te laden als querystring.</summary>
public record VergelijkbarePandenZoekCriteria(
    string ZoekgebiedTab,
    List<int> GemeenteIds,
    string? RondAdresPostcode,
    double? RondAdresLat,
    double? RondAdresLng,
    int RondAdresStraal,
    string Type,
    decimal? Oppervlakte,
    int Tolerantie,
    decimal? PrijsMin,
    decimal? PrijsMax,
    int? Slaapkamers,
    string Status);

/// <summary>Eén rij in de "Laatste zoekacties" / "Opgeslagen profielen"-lijst op de lege staat.</summary>
public class VergelijkbarePandenSnelkoppelingViewModel
{
    public int? Id { get; set; }
    public string Titel { get; set; } = "";
    public string Subtitel { get; set; } = "";
    public int? AantalResultaten { get; set; }
    public string? RelatieveTijd { get; set; }
    public string Url { get; set; } = "";

    /// <summary>Onderliggende criteria — niet getoond in de view, enkel gebruikt door de
    /// controller om de <see cref="Url"/> op te bouwen via Url.Action.</summary>
    public VergelijkbarePandenZoekCriteria? Criteria { get; set; }
}
