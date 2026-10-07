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

    // ── gl-v2 (design-handoff 31) ───────────────────────────────────────────────────────────────
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }

    /// <summary>Meest gebruikte netbeheerder per NutsType in dit project (kolomkop "ELEKTRICITEIT · Fluvius", voorinvulling).</summary>
    public Dictionary<int, (int CompanyId, string Naam)> NetbeheerderPerType { get; set; } = new();

    /// <summary>Open (niet-geannuleerde) nutsdossiers per "unitId:nutsType" — voor "wordt overgeslagen" in de bulk-modal (31e).</summary>
    public HashSet<string> BestaandePerEenheidEnType { get; set; } = new();

    public string? StartTab { get; set; }
    public int? FilterKind { get; set; }
    public int? HighlightDossierId { get; set; }
    public Dictionary<int, int> OpenMijlpalen { get; set; } = new();
}

/// <summary>Scherm "Nieuwe nutsaansluiting" / bewerken (design-handoff 31c).</summary>
public class NutsFormV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public ProjectDossier? Dossier { get; set; }
    public ProjectNutsAansluiting? Bestaand { get; set; }
    public bool IsNieuw => Bestaand == null;
    public List<Units> Units { get; set; } = new();
    public int DefaultType { get; set; }
    public int? DefaultUnitId { get; set; }
    public int? DefaultNetbeheerderId { get; set; }
    public string? DefaultNetbeheerderNaam { get; set; }
    public bool HasChecklistOpen { get; set; }
    public int ChecklistTotaal { get; set; }
    public int ChecklistKlaar { get; set; }
}

/// <summary>Modal "Ander dossier" (design-handoff 31f) — nieuw of bewerken.</summary>
public class DossierAnderModalVm
{
    public int ProjectId { get; set; }
    public ProjectDossier? Bestaand { get; set; }
    public bool IsNieuw => Bestaand == null;
    public int DefaultKind { get; set; }
    public List<Units> Units { get; set; } = new();
    public List<Mijlpaal> Mijlpalen { get; set; } = new();
    public int? GekoppeldeMijlpaalId { get; set; }
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

    // ── gl-v2 (design-handoff 31d) ──────────────────────────────────────────────────────────────
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public Dictionary<string, string> Gebruikers { get; set; } = new();
    public string? EenheidKlant { get; set; }
    public List<DossierDocumentV2> Documenten { get; set; } = new();
    public bool CanSeeDocs { get; set; }
    public bool CanUploadDocs { get; set; }
}

/// <summary>Eén document van het documentencentrum dat aan een dossier hangt (design 31d "Documenten").</summary>
public class DossierDocumentV2
{
    public int DocumentId { get; set; }
    public string Naam { get; set; } = "";
    public DateTime? Datum { get; set; }
    public long? Grootte { get; set; }
    public string Extensie { get; set; } = "";
    public int? RevisionId { get; set; }
}

/// <summary>Modelvoor de aanmaak-/bewerkmodal van een nutsaansluitingsdossier.</summary>
public class NutsAansluitingModalVm
{
    public int ProjectId { get; set; }
    public List<Units> ProjectUnits { get; set; } = new();

    /// <summary>Ingevuld bij bewerken van een bestaand nutsaansluitingsdossier.</summary>
    public ProjectNutsAansluiting? Bestaand { get; set; }
}
