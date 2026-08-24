using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceCore.Budget;
namespace CPMCore.Models;

public class BudgetFormulesViewModel {
    public List<BudgetActivityFormuleInfo> Formules      { get; set; } = new();
    public List<SelectListItem>            Activiteiten  { get; set; } = new();
    public List<SelectListItem>            TestVersies   { get; set; } = new();
}

public class BudgetFormuleOpslaanRequest {
    public int    ActivityId   { get; set; }
    public string Formule      { get; set; }
    public string Omschrijving { get; set; }
    public bool   Actief       { get; set; }
}

public class BudgetFormuleTestRequest {
    public int    VersieId { get; set; }
    public string Formule  { get; set; }
}
