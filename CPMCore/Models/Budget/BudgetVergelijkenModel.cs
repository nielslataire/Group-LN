using System.Collections.Generic;
using System.Linq;
using BOCore.Budget;
using DALCore.Models;

namespace CPMCore.Models.Budget
{
    public class BudgetVergelijkenModel
    {
        public int    BudgetMasterId  { get; set; }
        public int    ProjectId       { get; set; }
        public string ProjectName     { get; set; }
        public string BudgetNaam      { get; set; }

        public List<BudgetVersie>     AlleVersies            { get; set; } = new();
        public List<int>              GeselecteerdeVersieIds { get; set; } = new();
        public List<BudgetResultaatBO> Resultaten            { get; set; } = new();

        public bool HeeftResultaten => Resultaten != null && Resultaten.Any();
    }
}
