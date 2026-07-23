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

    // Eenheden (source units — gebruikt als CanonicalUnits leeg is)
    public List<UnitRijViewModel> Units { get; set; } = new();

    // Canonical units (1 rij per unieke unit, meerdere bronnen per rij)
    public List<CanonicalUnitViewModel> CanonicalUnits { get; set; } = new();
    public bool HeeftCanonicalUnits => CanonicalUnits.Count > 0;

    // Navigatie-dropdown: andere projecten in dezelfde postcode
    public List<ProjectNavigatieOptie> AndereProjecten { get; set; } = new();

    // Canonical / bronnen info
    public long? CanonicalProjectId { get; set; }
    public List<BronViewModel> BronnenLijst { get; set; } = new();
    public int AantalBronnen => BronnenLijst.Count;
    public bool HeeftDuplicaten => AantalBronnen > 1;

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
    public string? BronNaam { get; set; }
    public string? OuderProjectNaam { get; set; }
    public bool IsBronActief { get; set; } = true;
}

public class ProjectNavigatieOptie
{
    public long Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Gemeente { get; set; }
    public string Label => string.IsNullOrEmpty(Gemeente) ? Naam : $"{Naam} — {Gemeente}";
}

public class CanonicalUnitViewModel
{
    public long Id { get; set; }
    public string Titel { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public decimal? Oppervlakte { get; set; }
    public int? Slaapkamers { get; set; }
    public decimal? Vraagprijs { get; set; }
    public decimal? PrijsPerM2 { get; set; }
    public string Status { get; set; } = "";
    public int? Verdieping { get; set; }
    public bool IsAmbiguous { get; set; }
    public bool HeeftPrijsConflict { get; set; }
    public bool HeeftStatusConflict { get; set; }
    public bool HeeftOppervlakteConflict { get; set; }
    public string? ConflictSamenvatting { get; set; }
    public List<UnitBronViewModel> Bronnen { get; set; } = new();
    public int AantalBronnen => Bronnen.Count;
}

public class UnitBronViewModel
{
    public long MarketAssetId { get; set; }
    public string BronNaam { get; set; } = "";
    public string? ExternalId { get; set; }
    public string? Url { get; set; }
    public decimal? Vraagprijs { get; set; }
    public decimal? Oppervlakte { get; set; }
    public string? Status { get; set; }
    public string MatchLevel { get; set; } = "";
    public bool IsRepresentatief { get; set; }
    public bool IsLosseAdvertentie { get; set; }
    public bool IsActief { get; set; } = true;
}

public class BronViewModel
{
    public long MarketAssetId { get; set; }
    public string BronNaam { get; set; } = "";
    public string? ExternalId { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public string? DeveloperNaam { get; set; }
    public int UnitCount { get; set; }
    public int AvailableCount { get; set; }
    public int SoldCount { get; set; }
    public string? Adres { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsActief { get; set; } = true;
}
