using DALCore.Models;
using BOCore;

namespace FacadeCore;

/// <summary>Instantieert een projecttraject uit een sjabloon en houdt het in lijn met het sjabloon.</summary>
public interface ITrajectInstantiationService
{
    /// <summary>
    /// Maakt een <see cref="Projecttraject"/> voor het project uit het (gekozen of automatisch bepaalde)
    /// sjabloon: kopieert fases en mijlpalen, berekent streefdata uit ankers + offsets.
    /// Gooit als het project al een traject heeft.
    /// </summary>
    Task<Projecttraject> Instantiate(TrajectInstantiatieBO dto, string? userId);

    /// <summary>
    /// Voegt fases/mijlpalen toe die na instantiatie aan het sjabloon zijn toegevoegd, zonder bestaande
    /// (mogelijk handmatig aangepaste) mijlpalen te wijzigen of te verwijderen. Retourneert het aantal toegevoegde mijlpalen.
    /// </summary>
    Task<int> SyncMissing(int projecttrajectId, string? userId);

    /// <summary>Design 30f: toont eerst wat een synchronisatie zou veranderen (nieuw / gewijzigd / niet meer in
    /// sjabloon) zonder iets te schrijven. Null als het traject niet aan een sjabloon hangt.</summary>
    Task<TrajectSyncPreview?> PreviewSync(int projecttrajectId);

    /// <summary>Past enkel de gekozen wijzigingen (sleutels uit <see cref="TrajectSyncItem.Key"/>) toe.
    /// Bereikte mijlpalen worden nooit aangepast; zelf toegevoegde mijlpalen blijven staan. Geeft het aantal
    /// toegepaste wijzigingen terug.</summary>
    Task<int> ApplySync(int projecttrajectId, IReadOnlyCollection<string> keys, string? userId);
}

public static class TrajectSyncSoort
{
    public const string Nieuw = "nieuw";
    public const string Gewijzigd = "gewijzigd";
    public const string Weg = "weg";
}

/// <summary>Eén regel in het sync-voorbeeld. <c>Weg</c>-regels zijn enkel informatief (er wordt nooit iets verwijderd).</summary>
public sealed class TrajectSyncItem
{
    /// <summary>"n:{sjabloonMijlpaalId}" (nieuw) · "g:{mijlpaalId}" (gewijzigd) · "w:{mijlpaalId}" (weg, niet toepasbaar).</summary>
    public string Key { get; set; } = "";
    public string Soort { get; set; } = TrajectSyncSoort.Nieuw;
    public string Naam { get; set; } = "";
    /// <summary>Naam van de fase waarin de mijlpaal thuishoort (voor "Uit sjabloon halen (N)" per fase).</summary>
    public string Fase { get; set; } = "";
    public string Detail { get; set; } = "";
    public bool Toepasbaar => Soort != TrajectSyncSoort.Weg;
}

public sealed class TrajectSyncPreview
{
    public string SjabloonNaam { get; set; } = "";
    public DateTime? SjabloonGewijzigd { get; set; }
    public List<TrajectSyncItem> Items { get; set; } = new();
}
