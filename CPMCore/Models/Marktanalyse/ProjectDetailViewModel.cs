namespace CPMCore.Models.Marktanalyse;

public class ProjectDetailViewModel
{
    public long Id { get; set; }
    public string ProjectNaam { get; set; } = "";
    public string TypeLabel { get; set; } = "";

    public string? Straat { get; set; }
    public string? Huisnummer { get; set; }
    public string? Postcode { get; set; }
    public string? Gemeente { get; set; }

    public string? DeveloperNaam { get; set; }
    public string? DeveloperWebsite { get; set; }
    public string? DeveloperTelefoon { get; set; }

    // KPI
    public int TotaalUnits { get; set; }
    public int BeschikbareUnits { get; set; }
    public int VerkochteUnits { get; set; }
    public int SoldConfirmedCount { get; set; }
    public int LikelySoldCount { get; set; }
    public decimal Verkoopgraad { get; set; }
    public decimal? GemiddeldePrijs { get; set; }
    public decimal? GemiddeldePrijsPerM2 { get; set; }
    public decimal? GemiddeldeOppervlakte { get; set; }

    // Eenheden
    public List<UnitRijViewModel> Units { get; set; } = new();

    // Navigatie-dropdown: andere projecten in dezelfde postcode
    public List<ProjectNavigatieOptie> AndereProjecten { get; set; } = new();

    public string AdresRegel =>
        string.Join(", ",
            new[]
            {
                string.Join(" ", new[] { Straat, Huisnummer }.Where(s => !string.IsNullOrWhiteSpace(s))),
                string.Join(" ", new[] { Postcode, Gemeente }.Where(s => !string.IsNullOrWhiteSpace(s)))
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string WebsiteLabel =>
        string.IsNullOrWhiteSpace(DeveloperWebsite)
            ? ""
            : DeveloperWebsite
                .Replace("https://", "")
                .Replace("http://", "")
                .TrimEnd('/');
}

public class UnitRijViewModel
{
    public long Id { get; set; }
    public string Naam { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public decimal? Oppervlakte { get; set; }
    public int? Slaapkamers { get; set; }
    public decimal? Vraagprijs { get; set; }
    public decimal? PrijsPerM2 { get; set; }
    public string Status { get; set; } = "";
    public string? SourceUrl { get; set; }
}

public class ProjectNavigatieOptie
{
    public long Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Gemeente { get; set; }
    public string Label => string.IsNullOrEmpty(Gemeente) ? Naam : $"{Naam} — {Gemeente}";
}
