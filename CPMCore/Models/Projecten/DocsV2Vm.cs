using FacadeCore;

namespace CPMCore.Models.Projecten;

/// <summary>Projecten/DetailDocsV2 (design-handoff 17a-17e).</summary>
public class DocsV2PageVm
{
    public DocOverview Overview { get; set; } = new();
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    /// <summary>Paneel dat bij het laden meteen open moet (na uploaden/bewerken).</summary>
    public int? OpenDocId { get; set; }
    public int? OpenRequestId { get; set; }
    public int? FilterUnitId { get; set; }
    public int? FilterClientId { get; set; }
    public int? FilterCompanyId { get; set; }
    /// <summary>Zichtbare naam van het entiteitsfilter (bv. "Lot 1") — leeg zonder filter.</summary>
    public string? FilterLabel { get; set; }
    /// <summary>Huidige map/slimme lijst in de querystring, voor de links in de rail.</summary>
    public string? Folder { get; set; }
    public string? Smart { get; set; }
}

public class DocPanelVm
{
    public DocDetail Detail { get; set; } = new();
    public string? ThumbUrl { get; set; }
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public List<DocOption> Units { get; set; } = new();
    public List<DocOption> Clients { get; set; } = new();
    public List<DocOption> Companies { get; set; } = new();
}

public class DocRequestPanelVm
{
    public DocRequestDetail Detail { get; set; } = new();
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
}

/// <summary>Herbruikbare documentenkaart (17b): "rechtstreeks / ook via" op een project, eenheid, klant of leverancier.</summary>
public class DocCardVm
{
    public DocCard Card { get; set; } = new();
    public int ProjectId { get; set; }
    public int? UnitId { get; set; }
    public int? ClientAccountId { get; set; }
    public int? CompanyId { get; set; }
    public string ViewAllUrl { get; set; } = "";
    public string ViewAllLabel { get; set; } = "Alle documenten";
    public bool CanWrite { get; set; }
}
