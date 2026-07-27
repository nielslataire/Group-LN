using System.Globalization;

namespace CPMCore.Models.Marktanalyse;

public class VergelijkbarePandenViewModel
{
    // ── Filter state ───────────────────────────────────────────────────────────
    public string ZoekgebiedTab { get; set; } = "Gemeenten";
    public List<int> GemeenteIds { get; set; } = new();
    public string? RondAdresPostcode { get; set; }
    /// <summary>Middelpunt gekozen via adreszoekopdracht of door te slepen op de kaart.</summary>
    public double? RondAdresLat { get; set; }
    public double? RondAdresLng { get; set; }
    public int RondAdresStraal { get; set; } = 1000;  // meters
    public string Type { get; set; } = "Appartement";
    public decimal? Oppervlakte { get; set; }
    public int Tolerantie { get; set; } = 10;
    public decimal? PrijsMin { get; set; }
    public decimal? PrijsMax { get; set; }
    public int? Slaapkamers { get; set; }
    public string Status { get; set; } = "Alles";

    // ── Dropdown-data ──────────────────────────────────────────────────────────
    public List<GemeenteGroep> Locaties { get; set; } = new();
    public Dictionary<int, int> AantalPerGemeente { get; set; } = new();

    // ── Resultaten ────────────────────────────────────────────────────────────
    public bool HeeftZoekparameters { get; set; }
    public List<VergelijkbaarPandRij> Panden { get; set; } = new();
    public VergelijkbaarKpiViewModel? Kpi { get; set; }

    // ── Lege staat: snel starten / historiek / opgeslagen profielen ───────────
    public List<VergelijkbarePandenSnelkoppelingViewModel> SnelStartPresets { get; set; } = new();
    public List<VergelijkbarePandenSnelkoppelingViewModel> RecenteZoekActies { get; set; } = new();
    public List<VergelijkbarePandenSnelkoppelingViewModel> OpgeslagenProfielen { get; set; } = new();

    // ── Computed ──────────────────────────────────────────────────────────────
    public bool HeeftResultaten => Panden.Count > 0;
    public decimal? OppervlakteLaag => Oppervlakte.HasValue ? Oppervlakte.Value - Tolerantie : null;
    public decimal? OppervlakteHoog => Oppervlakte.HasValue ? Oppervlakte.Value + Tolerantie : null;

    public string GemeenteDropdownLabel =>
        GemeenteIds.Count == 0
            ? "Alle gemeenten"
            : GemeenteIds.Count == 1
                ? Locaties.FirstOrDefault(l => l.GeoMunicipalityId == GemeenteIds[0])?.GemeenteNaam ?? "1 gemeente"
                : $"{GemeenteIds.Count} gemeenten geselecteerd";

    public bool HeeftRondAdresPunt => RondAdresLat.HasValue && RondAdresLng.HasValue;

    public string RondAdresDropdownLabel =>
        HeeftRondAdresPunt
            ? $"Eigen punt ({RondAdresLat!.Value.ToString("0.000", CultureInfo.InvariantCulture)}, " +
              $"{RondAdresLng!.Value.ToString("0.000", CultureInfo.InvariantCulture)}) · " +
              $"{(RondAdresStraal / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)} km"
            : "Kies een adres...";
}

/// <summary>Resultaat van een geocode-opzoeking, teruggegeven als JSON aan de kaart-UI.</summary>
public record GeocodeAdresResult(double Lat, double Lng, string? Label);

public class VergelijkbaarPandRij
{
    public long AssetId { get; set; }
    public long? ProjectAssetId { get; set; }
    public long? CanonicalProjectId { get; set; }
    public string ProjectNaam { get; set; } = "";
    public string AdresRegel { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public decimal? Oppervlakte { get; set; }
    public int? Slaapkamers { get; set; }
    public decimal? Vraagprijs { get; set; }
    public decimal? PrijsPerM2 { get; set; }
    public string Status { get; set; } = "";
    public bool IsProject { get; set; }
    public decimal? Verkoopgraad { get; set; }
    public string? SourceUrl { get; set; }
}

public class VergelijkbaarKpiViewModel
{
    public int Aantal { get; set; }
    public decimal? GemPrijs { get; set; }
    public decimal? GemPrijsPerM2 { get; set; }
    public decimal? LaagstePrijs { get; set; }
    public decimal? HoogstePrijs { get; set; }
    public decimal? GemOppervlakte { get; set; }
}
