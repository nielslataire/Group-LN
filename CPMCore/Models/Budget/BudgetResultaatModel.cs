using System.Collections.Generic;
using BOCore.Budget;
using DALCore.Models;

namespace CPMCore.Models.Budget
{
    public class BudgetResultaatModel
    {
        public int    BudgetVersieId  { get; set; }
        public int    ProjectId       { get; set; }
        public string ProjectName     { get; set; }
        public string BudgetNaam      { get; set; }
        public int    Versienummer    { get; set; }
        public string VersieLabel     { get; set; }
        public string VersieStatus    { get; set; }
        public int    BudgetMasterId  { get; set; }

        public BudgetResultaatBO Resultaat { get; set; }

        public decimal TotaalBouwAlternatief { get; set; }
        public decimal TotaalBouwNacalc      { get; set; }
        public decimal GewogenFactor         { get; set; }

        public List<BudgetVersie> AndereVersies { get; set; } = new();
    }
}
