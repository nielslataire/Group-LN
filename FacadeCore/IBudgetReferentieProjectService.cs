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

        /// <summary>Wat een snapshot van dit project zou opleveren (eenheden, GBA, facturen/contracten) — 38c, vóór "Snapshot maken".</summary>
        Task<ReferentieProjectPreviewBO> ProjectPreviewAsync(int projectId);

        /// <summary>Snapshot van een project in de app: per activiteit de inkomende facturen (werkelijke kost) als die er zijn,
        /// anders de gecontracteerde bedragen. Eenheden en GBA uit de units/het laatste budget van dat project.</summary>
        /// <paramref name="oppervlakteGBA"/>/<paramref name="aantalEenheden"/>: eigen invoer als het project geen budget/units heeft; leeg = wat de app weet.
        Task<Response> ImportUitProjectAsync(int projectId, string naam, DateTime? datum, string opmerking, string createdBy = null, decimal? oppervlakteGBA = null, int? aantalEenheden = null);

        /// <summary>Leest een Excel (.xlsx) met kolommen Activiteit (of ActivityId) en Bedrag en koppelt elke regel aan een activiteit
        /// (op id, anders op naam). Niet-gematchte regels komen terug met ActivityId leeg (38d: controle vóór het opslaan).</summary>
        Task<List<ReferentieImportRijBO>> LeesExcelAsync(Stream xlsx);

        /// <summary>Bewaart een referentieproject uit (gecontroleerde) Excel-regels. Regels zonder activiteit tellen mee in het totaal,
        /// niet per activiteit; regels zonder bedrag worden overgeslagen.</summary>
        Task<Response> OpslaanUitRijenAsync(BudgetReferentieProjectBO kop, IReadOnlyList<ReferentieImportRijBO> rijen);

        /// <summary>Excel rechtstreeks inlezen en bewaren (klassieke pagina, zonder controlestap).</summary>
        Task<Response> ImportExcelAsync(Stream xlsx, string naam, DateTime? datum, int aantalEenheden, decimal? oppervlakteGBA, string opmerking, string createdBy = null);

        /// <summary>Leeg invulsjabloon: alle activiteiten per lot met een kolom Bedrag.</summary>
        byte[] MaakSjabloon();

        /// <summary>Alle activiteiten (id, omschrijving, lot) voor de keuzelijst in de controlestap.</summary>
        Task<List<BudgetReferentieLijnBO>> GetActiviteitenAsync();

        Task<Response> DeleteAsync(int id);

        Task<List<int>> GetReferentieIdsVoorVersieAsync(int budgetVersieId);
        Task<Response> SetReferentiesVoorVersieAsync(int budgetVersieId, IEnumerable<int> referentieProjectIds);

        /// <summary>Per activiteit de referentieprijs (per eenheid en per m²) uit de gekozen referentieprojecten van de versie,
        /// geïndexeerd naar de huidige index van de versie. Leeg zonder gekozen referenties.</summary>
        Task<Dictionary<int, NacalcReferentieBO>> BerekenReferentieAsync(int budgetVersieId);

        /// <summary>Huidige (actieve) S- en I2021-index, voor de kolom "Index S / I" (factor t.o.v. de peildatum) in de lijst.</summary>
        Task<(decimal S, decimal I)> HuidigeIndexenAsync();
    }
}
