#nullable disable
namespace DALCore.Models;

// Migratie 096_BudgetGrondTweedeZone.sql
public partial class BudgetOppervlaktes
{
    /// <summary>Tweede grondoppervlakte (m²), bv. agrarisch deel van de woning; Grondopp is het eerste deel (woongebied).</summary>
    public decimal GrondoppAgrarisch { get; set; }
}

public partial class BudgetVerkoopLijn
{
    /// <summary>Eigen €/m² voor de tweede grondzone van deze woning.</summary>
    public decimal? GrondAgrPrijsPerM2 { get; set; }

    /// <summary>Waarde van de tweede grondzone (prijs × oppervlakte); Grondwaarde is de eerste zone.</summary>
    public decimal? GrondwaardeAgrarisch { get; set; }
}
