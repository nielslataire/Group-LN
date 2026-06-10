namespace CPMCore.Models.Marktanalyse;

public class GemeenteAnalyseViewModel
{
    public string? GeselecteerdePostcode { get; set; }
    public string? GeselecteerdeGemeente { get; set; }
    public string GeselecteerdType { get; set; } = "Alles";
    public string GeselecteerdAanbodtype { get; set; } = "Alles";

    public List<LocatieOptie> Locaties { get; set; } = new();
    public GemeenteKpiViewModel? Kpi { get; set; }
    public List<PrijsBucketViewModel> VraagprijsBuckets { get; set; } = new();
    public List<PrijsBucketViewModel> PrijsPerM2Buckets { get; set; } = new();
    public List<ProjectVerkoopgraadViewModel> VerkoopgraadPerProject { get; set; } = new();
    public List<ProjectRijViewModel> Projecten { get; set; } = new();
    public List<LosseEenheidRijViewModel> LosseEenheden { get; set; } = new();

    public bool HeeftData => Kpi != null && (Kpi.ActieveProjecten > 0 || Kpi.AantalLosseEenheden > 0);
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
    public int AantalProjectUnits { get; set; }
    public int AantalLosseEenheden { get; set; }
    public int TotaalAnalyseerbaar => AantalProjectUnits + AantalLosseEenheden;
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

    // Adresgegevens (rechtstreeks uit MarketAsset)
    public string? Straat { get; set; }
    public string? Huisnummer { get; set; }
    public string? Postcode { get; set; }
    public string? Gemeente { get; set; }

    public string AdresRegel =>
        string.Join(", ",
            new[]
            {
                string.Join(" ", new[] { Straat, Huisnummer }.Where(s => !string.IsNullOrWhiteSpace(s))),
                string.Join(" ", new[] { Postcode, Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)))
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public class LosseEenheidRijViewModel
{
    public long Id { get; set; }
    public string? Adres { get; set; }      // straat + huisnummer
    public string? Postcode { get; set; }
    public string? Gemeente { get; set; }
    public string TypeLabel { get; set; } = "";
    public decimal? Oppervlakte { get; set; }
    public int? Slaapkamers { get; set; }
    public decimal? Vraagprijs { get; set; }
    public decimal? PrijsPerM2 { get; set; }
    public string Status { get; set; } = "";
    public string AangeboenDoor { get; set; } = "";  // bron-naam
    public string? SourceUrl { get; set; }
}
