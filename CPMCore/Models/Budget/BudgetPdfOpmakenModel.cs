namespace CPMCore.Models.Budget
{
    public class BudgetPdfOpmakenModel
    {
        public int BudgetVersieId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public bool Intern { get; set; }
        public string BudgetNaam { get; set; } = "";
        public int Versienummer { get; set; }
    }
}
