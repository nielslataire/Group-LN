using Microsoft.AspNetCore.Mvc.Rendering;

namespace CPMCore.Services.Verkoop;

/// <summary>Model van Verkoopinstellingen (45b).</summary>
public class VerkoopInstellingenVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public VerkoopInstellingen Instellingen { get; set; } = new();
    public VerkoopWebsite Website { get; set; } = new();
    public List<SelectListItem> BankAccounts { get; set; } = new();
    public bool MissingBuilder { get; set; }
    /// <summary>Lot dat in het voorbeeld voor de koper getoond wordt (eerste eenheid die nog niet verkocht is).</summary>
    public VerkoopRijVm? Voorbeeld { get; set; }
    public bool RekeningOntbrak { get; set; }
    public string? PublicUrl { get; set; }
}

/// <summary>Een prijscode uit Prijsreferenties verkoop (stap 8 van het budget): €/m² voor grond (G-xx) of bouw (B-xx).</summary>
public record VerkoopCode(int Code, string Label, decimal PerM2, string Omschrijving);

/// <summary>Wat het definitieve budget (stap 8 en 9) over één eenheid weet; enkel gevuld als de eenheid aan een verkooplijn gekoppeld is.</summary>
public class VerkoopBudgetInfo
{
    public int VersieId { get; set; }
    public string EenheidNaam { get; set; } = "";
    public decimal OppGereduceerd { get; set; }
    public decimal Grondopp { get; set; }
    public decimal Kostprijs { get; set; }
    public decimal Minimum { get; set; }
    /// <summary>Doelmarge op de bouw (fractie, 0,18 = 18 %), enkel ter info.</summary>
    public decimal MargePerc { get; set; }
    /// <summary>Makelaarscommissie als fractie (0,025 = 2,5 %).</summary>
    public decimal Commissie { get; set; }
    public int? CodeGrond { get; set; }
    public int? CodeBouw { get; set; }
}

/// <summary>Model van Eenheid berekenen (45c): enkel rekenen en bekijken, er wordt niets bewaard.</summary>
public class VerkoopBerekenVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public List<VerkoopRijVm> Units { get; set; } = new();
    public VerkoopRijVm Unit { get; set; } = new();
    public VerkoopInstellingen Instellingen { get; set; } = new();
    public List<VerkoopCode> CodesGrond { get; set; } = new();
    public List<VerkoopCode> CodesBouw { get; set; } = new();
    public VerkoopBudgetInfo? Budget { get; set; }
}
