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

        /// <summary>Bottom-up verkoopvoorstel (kostprijs + marge → grond- en bouwwaarde per eenheid).</summary>
        public BOCore.Budget.BudgetVerkoopVoorstelBO Voorstel { get; set; }

        /// <summary>Units van het project, om een verkooplijn aan een Unit te koppelen (doorzetten).</summary>
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> UnitOptions { get; set; } = new();

        public decimal? VraagprijzenLijnen =>
            Lijnen.Any(l => l.Vraagprijs is > 0m) ? Lijnen.Where(l => l.Vraagprijs is > 0m).Sum(l => l.Vraagprijs.Value) : null;
    }
}
