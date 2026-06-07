using BOCore;
namespace CPMCore.Models;
public class FormulaKoppelingAjaxRequest {
    public string Sleutel    { get; set; }
    public int?   MateriaalId { get; set; }
}
public class KostprijsMaterialenViewModel {
    public List<KmIndexTypeBO>             IndexTypes          { get; set; } = new();
    public List<ActivityGroupBO>           ActivityGroepen     { get; set; } = new();
    public List<KostprijsMateriaalBO>      Materialen          { get; set; } = new();
    public List<BouwkostPercentageGroepBO> PercentageGroepen   { get; set; } = new();
    public List<BouwkostPercentageBO>      Percentages         { get; set; } = new();
    public List<FormulaKoppelingBO>        FormulaKoppelingen  { get; set; } = new();
}
