namespace CPMCore.Models.Marktanalyse;

public class GemeenteAnalyseViewModel
{
    public string? GeselecteerdePostcode { get; set; }
    public string? GeselecteerdeGemeente { get; set; }
    public string GeselecteerdType { get; set; } = "Alles";

    public List<LocatieOptie> Locaties { get; set; } = new();
    public GemeenteKpiViewModel? Kpi { get; set; }
    public List<PrijsBucketViewModel> VraagprijsBuckets { get; set; } = new();
    public List<PrijsBucketViewModel> PrijsPerM2Buckets { get; set; } = new();
    public List<ProjectVerkoopgraadViewModel> VerkoopgraadPerProject { get; set; } = new();
    public List<ProjectRijViewModel> Projecten { get; set; } = new();

    public bool HeeftData => Kpi != null && Kpi.ActieveProjecten > 0;
    public bool HeeftFilter => !string.IsNullOrEmpty(GeselecteerdePostcode);
}

public class LocatieOptie
{
    public string Gemeente { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Label => $"{Gemeente} ({Postcode})";
}

public class GemeenteKpiViewModel
{
    public int ActieveProjecten { get; set; }
    public int ActieveUnits { get; set; }
    public int VerkochteUnits { get; set; }
    public int BeschikbareUnits { get; set; }
    public int GereserveerdeUnits { get; set; }
    public decimal? GemiddeldePrijs { get; set; }
    public decimal? GemiddeldePrijsPerM2 { get; set; }
    public decimal? GemiddeldeOppervlakte { get; set; }
    public decimal Verkoopgraad { get; set; }
}

public class PrijsBucketViewModel
{
    public string Label { get; set; } = "";
    public int Aantal { get; set; }
}

public class ProjectVerkoopgraadViewModel
{
    public string ProjectNaam { get; set; } = "";
    public decimal Verkoopgraad { get; set; }
    public int VerkochteUnits { get; set; }
    public int TotaalUnits { get; set; }
}

public class ProjectRijViewModel
{
    public long Id { get; set; }
    public string ProjectNaam { get; set; } = "";
    public string Ontwikkelaar { get; set; } = "-";
    public string TypeLabel { get; set; } = "";
    public int TotaalUnits { get; set; }
    public int VerkochteUnits { get; set; }
    public int BeschikbareUnits { get; set; }
    public decimal Verkoopgraad { get; set; }
    public decimal? GemiddeldePrijs { get; set; }
    public decimal? GemiddeldePrijsPerM2 { get; set; }
}
