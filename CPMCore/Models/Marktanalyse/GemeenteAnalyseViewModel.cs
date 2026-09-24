namespace CPMCore.Models.Marktanalyse;

public class GemeenteAnalyseViewModel
{
    public int? GeselecteerdGeoMunicipalityId { get; set; }
    public int? GeselecteerdGeoMunicipalSectionId { get; set; }
    public string? GeselecteerdeGemeenteNaam { get; set; }
    public string? GeselecteerdeDeelgemeenteNaam { get; set; }
    public string? GeselecteerdePostcode { get; set; }   // alleen voor display
    public string GeselecteerdType { get; set; } = "Alles";
    public string GeselecteerdAanbodtype { get; set; } = "Alles";

    /// <summary>
    /// "Actueel"  = huidig aanbod (actieve projecten met al hun units + actieve losse eenheden).
    /// "Verkocht" = enkel wat in de periode verkocht is, ook uit projecten die niet meer online staan.
    /// "Alles"    = actueel aanbod + alles wat in de periode verkocht is.
    /// </summary>
    public string GeselecteerdAanbod { get; set; } = "Actueel";

    /// <summary>Periode (maanden) voor "verkocht in periode"; 0 = onbeperkt.</summary>
    public int PeriodeMaanden { get; set; } = 12;

    public static readonly string[] AanbodOpties = { "Actueel", "Verkocht", "Alles" };
    public static readonly (int Maanden, string Label)[] PeriodeOpties =
        { (3, "3 mnd"), (6, "6 mnd"), (12, "12 mnd"), (24, "24 mnd"), (0, "Alles") };

    public string PeriodeLabel => PeriodeMaanden > 0 ? $"laatste {PeriodeMaanden} maanden" : "volledige historiek";

    public List<GemeenteGroep> Locaties { get; set; } = new();
    public GemeenteKpiViewModel? Kpi { get; set; }
    public List<PrijsBucketViewModel> VraagprijsBuckets { get; set; } = new();
    public List<PrijsBucketViewModel> PrijsPerM2Buckets { get; set; } = new();
    public List<ProjectVerkoopgraadViewModel> VerkoopgraadPerProject { get; set; } = new();
    public List<ProjectRijViewModel> Projecten { get; set; } = new();
    public List<LosseEenheidRijViewModel> LosseEenheden { get; set; } = new();
    public bool ToonGekoppeld { get; set; } = false;
    public int AantalGekoppeld { get; set; }

    public bool HeeftData => Kpi != null && (Kpi.ActieveProjecten > 0 || Kpi.AantalLosseEenheden > 0);
    public bool HeeftFilter => GeselecteerdGeoMunicipalityId.HasValue || GeselecteerdGeoMunicipalSectionId.HasValue;

    public string FilterLabel =>
        GeselecteerdGeoMunicipalSectionId.HasValue
            ? GeselecteerdeDeelgemeenteNaam is { Length: > 0 } d
                ? $"{d} ({GeselecteerdePostcode})"
                : GeselecteerdeGemeenteNaam ?? ""
            : GeselecteerdGeoMunicipalityId.HasValue
                ? $"{GeselecteerdeGemeenteNaam} (+ deelgemeentes)"
                : "";
}

public class GemeenteGroep
{
    public int GeoMunicipalityId { get; set; }
    public string GemeenteNaam { get; set; } = "";
    public List<LocatieOptie> Secties { get; set; } = new();
}

public class LocatieOptie
{
    public int GeoMunicipalSectionId { get; set; }
    public int GeoMunicipalityId { get; set; }
    public string DeelgemeenteNaam { get; set; } = "";
    public string? ZipCode { get; set; }
    public string Label => string.IsNullOrEmpty(ZipCode)
        ? DeelgemeenteNaam
        : $"{DeelgemeenteNaam} / {ZipCode}";
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
    public int SoldConfirmedCount { get; set; }
    public int LikelySoldCount { get; set; }

    /// <summary>Projecten in de selectie die niet meer online staan (uitverkocht of offline gehaald).</summary>
    public int NietActieveProjecten { get; set; }

    /// <summary>Units met een verkoopdatum binnen de gekozen periode.</summary>
    public int VerkochtInPeriode { get; set; }

    /// <summary>Verkochte units per maand over de gekozen periode; null bij onbeperkte periode.</summary>
    public decimal? AbsorptiePerMaand { get; set; }

    /// <summary>Mediaan van de doorlooptijd (dagen van eerste waarneming tot verkoop).</summary>
    public int? MediaanDoorlooptijdDagen { get; set; }

    /// <summary>Aantal units waarop de mediaan gebaseerd is.</summary>
    public int DoorlooptijdAantal { get; set; }
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

    /// <summary>False als het project niet meer online staat (uitverkocht of offline gehaald).</summary>
    public bool IsActief { get; set; } = true;
    public int? MediaanDoorlooptijdDagen { get; set; }

    public string? Straat { get; set; }
    public string? Huisnummer { get; set; }
    public string? Postcode { get; set; }
    public string? Gemeente { get; set; }

    // Canonical / bronnen info
    public long? CanonicalProjectId { get; set; }
    public int AantalBronnen { get; set; }
    public List<string> BronNamen { get; set; } = new();
    public bool DeveloperConflict { get; set; }
    public bool HeeftDuplicaten => AantalBronnen > 1;

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
    public string? Adres { get; set; }
    public string? Postcode { get; set; }
    public string? Gemeente { get; set; }
    public string TypeLabel { get; set; } = "";
    public decimal? Oppervlakte { get; set; }
    public int? Slaapkamers { get; set; }
    public decimal? Vraagprijs { get; set; }
    public decimal? PrijsPerM2 { get; set; }
    public string Status { get; set; } = "";
    public string AangeboenDoor { get; set; } = "";
    public string? SourceUrl { get; set; }
    public DateTime? VerkochtOp { get; set; }
    public int? DoorlooptijdDagen { get; set; }

    public long? LinkedCanonicalUnitId { get; set; }
    public string? GekoppeldProjectNaam { get; set; }
    public bool IsGekoppeld => LinkedCanonicalUnitId.HasValue;
}
