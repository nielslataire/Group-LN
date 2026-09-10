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
}
