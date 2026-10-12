#nullable disable
namespace DALCore.Models;

// Migratie 090_BudgetOppervlaktesPoorten.sql
public partial class BudgetOppervlaktes
{
    /// <summary>Aantal garagepoorten van deze rij (0 = niet opgegeven; een garage-rij telt dan voor 1).</summary>
    public int Poorten { get; set; }
}
