using System.Collections.Generic;
using BOCore;
using BOCore.Budget;

namespace CPMCore.Models.Instellingen
{
    /// <summary>Instellingen › Budget › Prijsreferenties: algemene en projectspecifieke €/m²-codes voor stap 8 van de budgetwizard.</summary>
    public class BudgetPrijsReferentiesViewModel
    {
        public List<BudgetPrijsReferentieBO> Referenties { get; set; } = new();
        /// <summary>Projecten voor de optionele projectkoppeling van een code.</summary>
        public List<IdNameBO> Projecten { get; set; } = new();
    }
}
