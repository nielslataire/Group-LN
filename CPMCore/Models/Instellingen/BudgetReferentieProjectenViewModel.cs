using System.Collections.Generic;
using BOCore;
using BOCore.Budget;

namespace CPMCore.Models.Instellingen
{
    /// <summary>Instellingen › Budget › Referentieprojecten (nacalc): afgewerkte projecten met hun werkelijke kost per activiteit.</summary>
    public class BudgetReferentieProjectenViewModel
    {
        public List<BudgetReferentieProjectBO> Referenties { get; set; } = new();
        /// <summary>Projecten in de app waaruit een referentieproject gemaakt kan worden.</summary>
        public List<IdNameBO> Projecten { get; set; } = new();
    }
}
