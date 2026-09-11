using BOCore;
using DALCore.Models;

namespace CPMCore.Models.Traject;

public class DossierIndexVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public List<ProjectDossier> Dossiers { get; set; } = new();
    public List<Units> ProjectUnits { get; set; } = new();
    public DossierFilterBO Filter { get; set; } = new();

    public int AantalOpen => Dossiers.Count(d => d.Status != (int)DossierStatus.Afgehandeld && d.Status != (int)DossierStatus.Geannuleerd);
}

public class DossierDetailsVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public ProjectDossier Dossier { get; set; } = null!;
    public List<Mijlpaal> GekoppeldeMijlpalen { get; set; } = new();
    public List<Mijlpaal> BeschikbareMijlpalen { get; set; } = new();
    public List<Units> ProjectUnits { get; set; } = new();

    /// <summary>Enkel ingevuld wanneer Dossier.DossierKind == NutsAansluiting.</summary>
    public ProjectNutsAansluiting? Nuts { get; set; }
}

/// <summary>Modelvoor de aanmaak-/bewerkmodal van een nutsaansluitingsdossier.</summary>
public class NutsAansluitingModalVm
{
    public int ProjectId { get; set; }
    public List<Units> ProjectUnits { get; set; } = new();

    /// <summary>Ingevuld bij bewerken van een bestaand nutsaansluitingsdossier.</summary>
    public ProjectNutsAansluiting? Bestaand { get; set; }
}
