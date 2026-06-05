using System.Collections.Generic;
using DALCore.Models;

namespace CPMCore.Models.Budget
{
    public class BudgetVerkoopModel
    {
        public int    BudgetVersieId { get; set; }
        public int    ProjectId      { get; set; }
        public string ProjectName    { get; set; }
        public string BudgetNaam     { get; set; }
        public int    Versienummer   { get; set; }
        public string VersieLabel    { get; set; }
        public string VersieStatus   { get; set; }

        public List<BudgetVerkoopLijn>     Lijnen                { get; set; } = new();
        public List<BudgetPrijsReferentie> PrijsReferentiesBouw  { get; set; } = new();
        public List<BudgetPrijsReferentie> PrijsReferentiesGrond { get; set; } = new();
        public List<string>                BeschikbareEenheden   { get; set; } = new();
    }
}
