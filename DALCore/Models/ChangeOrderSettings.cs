#nullable disable
namespace DALCore.Models;

/// <summary>Migratie 142 — één rij (Id = 1): standaardpercentage voor een schadevergoeding op minwerk.</summary>
public partial class ChangeOrderSettings
{
    public int Id { get; set; } = 1;

    public decimal MinworkDamagePercentage { get; set; } = 25m;
}
