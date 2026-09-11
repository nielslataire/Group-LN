#nullable disable
namespace DALCore.Models;

/// <summary>M:N-koppeling tussen een dossier en de mijlpalen die het stuurt (bv. een vergunningsdossier met sub-stappen).</summary>
public partial class ProjectDossierMijlpaal
{
    public int Id { get; set; }

    public int ProjectDossierId { get; set; }

    public int MijlpaalId { get; set; }

    public virtual ProjectDossier ProjectDossier { get; set; }

    public virtual Mijlpaal Mijlpaal { get; set; }
}
