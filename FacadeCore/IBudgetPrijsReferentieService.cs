using BOCore;
using BOCore.Budget;

namespace FacadeCore
{
    /// <summary>Prijsreferentietabel (€/m² per code, type Bouw/Grond) voor de verkooplijnen van de budgetwizard: algemene codes
    /// (Instellingen › Budget) en projectspecifieke codes (stap 8).</summary>
    public interface IBudgetPrijsReferentieService
    {
        /// <summary>Alle referenties, algemeen én projectspecifiek (met projectnaam), gesorteerd op type, project, code.</summary>
        GetResponse<BudgetPrijsReferentieBO> GetAlle();

        /// <summary>Algemene referenties plus die van dit project.</summary>
        GetResponse<BudgetPrijsReferentieBO> GetVoorProject(int projectId);

        /// <summary>Id = 0 maakt aan; code moet uniek zijn binnen type en (project of algemeen).</summary>
        Response InsertUpdate(BudgetPrijsReferentieBO bo);

        Response Delete(int id);
    }
}
