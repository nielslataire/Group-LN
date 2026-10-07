using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BOCore;
using BOCore.Budget;

namespace FacadeCore
{
    /// <summary>
    /// Referentieprojecten voor de nacalculatie in de budgetwizard: werkelijke kost per activiteit van afgewerkte projecten
    /// (uit de app of uit Excel), en de vergelijking per activiteit voor een budgetversie (stap 6).
    /// </summary>
    public interface IBudgetReferentieProjectService
    {
        Task<List<BudgetReferentieProjectBO>> GetAlleAsync();
        Task<BudgetReferentieProjectBO> GetAsync(int id);

        /// <summary>Snapshot van een project in de app: per activiteit de inkomende facturen (werkelijke kost) als die er zijn,
        /// anders de gecontracteerde bedragen. Eenheden en GBA uit de units/het laatste budget van dat project.</summary>
        Task<Response> ImportUitProjectAsync(int projectId, string naam, DateTime? datum, string opmerking);

        /// <summary>Excel (.xlsx) met kolommen Activiteit (of ActivityId) en Bedrag; zie <see cref="MaakSjabloon"/>.
        /// Rijen die niet aan een activiteit te koppelen zijn, komen als waarschuwing terug.</summary>
        Task<Response> ImportExcelAsync(Stream xlsx, string naam, DateTime? datum, int aantalEenheden, decimal? oppervlakteGBA, string opmerking);

        /// <summary>Leeg invulsjabloon: alle activiteiten per lot met een kolom Bedrag.</summary>
        byte[] MaakSjabloon();

        Task<Response> DeleteAsync(int id);

        Task<List<int>> GetReferentieIdsVoorVersieAsync(int budgetVersieId);
        Task<Response> SetReferentiesVoorVersieAsync(int budgetVersieId, IEnumerable<int> referentieProjectIds);

        /// <summary>Per activiteit de referentieprijs (per eenheid en per m²) uit de gekozen referentieprojecten van de versie,
        /// geïndexeerd naar de huidige index van de versie. Leeg zonder gekozen referenties.</summary>
        Task<Dictionary<int, NacalcReferentieBO>> BerekenReferentieAsync(int budgetVersieId);
    }
}
