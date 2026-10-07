#nullable disable
namespace DALCore.Models;

// Migratie 075_BudgetVerkoopPrijsPerM2.sql: verkoopprijs manueel per m² (stap 8 budgetwizard).
public partial class BudgetVerkoopLijn
{
    /// <summary>Ingestelde €/m² voor de bouwwaarde, op de gereduceerde oppervlakte van de eenheid. NULL = afgeleid uit Bouwwaarde.</summary>
    public decimal? BouwPrijsPerM2 { get; set; }

    /// <summary>Ingestelde €/m² voor de grondwaarde, op de grondoppervlakte van de eenheid. NULL = afgeleid uit Grondwaarde.</summary>
    public decimal? GrondPrijsPerM2 { get; set; }

    /// <summary>Herkomst van de prijs (BOCore.VerkoopPrijsBron): 1 voorstel, 2 markt, 3 referentiecode, 4 manueel €/m², 5 manueel bedrag.</summary>
    public byte? PrijsBron { get; set; }
}
