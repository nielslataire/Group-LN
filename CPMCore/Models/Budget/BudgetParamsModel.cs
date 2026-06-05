using DALCore.Models;

namespace CPMCore.Models.Budget
{
    public class BudgetParamsModel
    {
        public int    BudgetVersieId { get; set; }
        public int    ProjectId      { get; set; }
        public string ProjectName    { get; set; }
        public string BudgetNaam     { get; set; }
        public int    Versienummer   { get; set; }
        public string VersieLabel    { get; set; }
        public string VersieStatus   { get; set; }

        public BudgetParams Params { get; set; } = new();

        public decimal TotaalBouw     { get; set; }
        public int     AantalEenheden { get; set; }
        public int     AantalLiften   { get; set; }
    }
}
