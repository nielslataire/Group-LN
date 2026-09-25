using System.Threading.Tasks;
using BOCore.Budget;

namespace FacadeCore
{
    /// <summary>
    /// Verkoopvoorstel per eenheid voor een budgetversie: kostprijs (incl. grond) gesplitst in
    /// grond- en bouwkost, met marges vertaald naar grondwaarde en bouwwaarde en verdeeld over
    /// de eenheden uit BudgetOppervlaktes. Wordt niet gepersisteerd; altijd berekend uit de
    /// actuele budgetgegevens.
    /// </summary>
    public interface IVerkoopVoorstelService
    {
        Task<BudgetVerkoopVoorstelBO> BerekenAsync(int budgetVersieId);

        /// <summary>Voorstel + vastgelegde vraagprijzen op de verkooplijnen → opbrengst en marge (stap 9, vergelijking, PDF/Excel).</summary>
        Task<BudgetVerkoopSamenvattingBO> SamenvattingAsync(int budgetVersieId);

        /// <summary>
        /// Schrijft grond- en bouwwaarde van de verkooplijnen (met gekoppelde Unit) naar de Units van het project.
        /// Verkochte units worden overgeslagen; units met meerdere basis-bouwwaardelijnen ook (manueel).
        /// </summary>
        Task<BOCore.Response> DoorzettenNaarUnitsAsync(int budgetVersieId);
    }
}
