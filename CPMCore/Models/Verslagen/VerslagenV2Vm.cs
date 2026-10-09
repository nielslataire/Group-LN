namespace CPMCore.Models.Verslagen;

public class VerslagAanwezigeV2
{
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

/// <summary>Een rij in de verslagenlijst (design-handoff 40i).</summary>
public class VerslagRowV2
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public DateOnly Datum { get; set; }
    public string AanwezigenTekst { get; set; } = "";
    public int Type { get; set; }
    public string TypeLabel { get; set; } = "";
    public int Punten { get; set; }
    public int Status { get; set; }
    public string StatusLabel => Status == 1 ? "Verstuurd" : "Concept";
}

public class VerslagSamenvattingV2
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public DateOnly Datum { get; set; }
    public int Status { get; set; }
    public string AanwezigenTekst { get; set; } = "";
    public string? Weer { get; set; }
    public string? Opmerkingen { get; set; }
    public int NieuweTotaal { get; set; }
    public int NieuweTeKeuren { get; set; }
    public int OpenTotaal { get; set; }
    public int OpenGemeld { get; set; }
    public DateTime? VolgendBezoek { get; set; }
}

public class VerslagIndexV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public List<VerslagRowV2> Rows { get; set; } = new();
    public VerslagSamenvattingV2? Selected { get; set; }
    /// <summary>Volgnummer van het volgende werfbezoek ("Werfbezoek nr. 15 starten").</summary>
    public int VolgendNummer { get; set; } = 1;
    public int? LopendConceptId { get; set; }
    /// <summary>Beheerders mogen ook een verstuurd verslag verwijderen.</summary>
    public bool IsAdmin { get; set; }
}

/// <summary>Een punt in het verslag: openstaand uit het vorige verslag (nakijken ter plaatse) of nieuw tijdens dit bezoek.</summary>
public class VerslagPuntV2
{
    public int IssueId { get; set; }
    public string Nr { get; set; } = "";
    public string Title { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Contractor { get; set; } = "";
    public int Status { get; set; }
    public string StatusLabel { get; set; } = "";
    public string StatusTone { get; set; } = "is-neutral";
    public string? Sinds { get; set; }
    public int? TerPlaatse { get; set; }
    public string? Opmerking { get; set; }
    public bool IsNieuw { get; set; }
    public string DetailUrl { get; set; } = "";
}

public class VerslagSuggestieV2
{
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Org { get; set; }
}

public class VerslagEditV2Vm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public bool CanWrite { get; set; }
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public int Type { get; set; }
    public int Status { get; set; }
    public bool ReadOnly => Status == 1;
    public bool IsAdmin { get; set; }
    /// <summary>Concept: iedereen met schrijfrecht; verstuurd: enkel een beheerder.</summary>
    public bool CanDelete => Status == 0 ? CanWrite : IsAdmin;
    public DateOnly Datum { get; set; }
    public string? Uur { get; set; }
    public string? Weer { get; set; }
    public string? Opmerkingen { get; set; }
    public DateOnly? VolgendDatum { get; set; }
    public string? VolgendUur { get; set; }
    public List<VerslagAanwezigeV2> Aanwezigen { get; set; } = new();
    public List<VerslagSuggestieV2> Suggesties { get; set; } = new();
    public List<VerslagPuntV2> OpenPunten { get; set; } = new();
    public List<VerslagPuntV2> NieuwePunten { get; set; } = new();
    public int? VorigId { get; set; }
    public string? VorigNaam { get; set; }
    public DateOnly? VorigDatum { get; set; }
    /// <summary>Gegevens voor het gedeelde punt-zijpaneel (Bewerken van een nieuw punt).</summary>
    public CPMCore.Models.Issues.PuntenIndexV2Vm Panel { get; set; } = new();
}
