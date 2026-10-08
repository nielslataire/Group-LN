using BOCore;
using BOCore.Budget;

namespace FacadeCore
{
    /// <summary>Prijsreferentietabel (€/m² per code, type Bouw/Grond) voor de verkooplijnen van de budgetwizard: algemene codes
    /// (Instellingen › Budget) en projectspecifieke codes (stap 8).</summary>
    public interface IBudgetPrijsReferentieService
    {
        /// <summary>Alle referenties, algemeen én projectspecifiek (met projectnaam en IsInGebruik), gesorteerd op type, project, code.</summary>
        GetResponse<BudgetPrijsReferentieBO> GetAlle();

        /// <summary>Algemene referenties plus die van dit project; gearchiveerde codes enkel als <paramref name="metGearchiveerd"/>.</summary>
        GetResponse<BudgetPrijsReferentieBO> GetVoorProject(int projectId, bool metGearchiveerd = false);

        /// <summary>Id = 0 maakt aan; code moet uniek zijn binnen type en (project of algemeen).</summary>
        Response InsertUpdate(BudgetPrijsReferentieBO bo);

        /// <summary>Weigert een code die in een verkooplijn gebruikt wordt (archiveren is dan de weg).</summary>
        Response Delete(int id);

        /// <summary>Archiveren of herstellen (migratie 077). Een gearchiveerde code is niet meer kiesbaar op stap 8.</summary>
        Response SetGearchiveerd(int id, bool gearchiveerd);

        /// <summary>Eerstvolgende vrije code voor een type (algemeen + alle projecten), voor het invulvoorstel in het formulier.</summary>
        int VolgendeCode(string prijsType);

        /// <summary>Excel-export van alle codes van een type (38a "Exporteren").</summary>
        byte[] ExportXlsx(string prijsType);
    }
}
