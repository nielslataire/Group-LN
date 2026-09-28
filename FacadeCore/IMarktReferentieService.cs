using System.Threading.Tasks;
using BOCore.Budget;

namespace FacadeCore
{
    /// <summary>
    /// Levert de vergelijkbare units uit de marktdata voor de gemeente van een project.
    /// Geïmplementeerd in CPMCore (enige laag met toegang tot de MarketData-database);
    /// de statistiek (medianen, absorptie, doorlooptijd) gebeurt in ServiceCore.
    /// </summary>
    public interface IMarktReferentieService
    {
        /// <param name="projectId">CPM-project; de postcode ervan bepaalt de gemeente.</param>
        /// <param name="periodeMaanden">Venster voor "verkocht in periode"; 0 = onbeperkt.</param>
        /// <returns>Altijd een object; <see cref="MarktReferentieBO.Bron"/> legt uit als er geen data is.</returns>
        Task<MarktReferentieBO> HaalOpAsync(int projectId, int periodeMaanden = 12);
    }
}
