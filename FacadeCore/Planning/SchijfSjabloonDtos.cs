namespace FacadeCore;

/// <summary>Een schijf in de editor van een schijvensjabloon.</summary>
public sealed class SchijfSjabloonSchijfBewerk
{
    /// <summary>Mag {verdieping} bevatten.</summary>
    public string Naam { get; set; } = "";
    public string? PlanningCode { get; set; }
    /// <summary>Vast percentage; leeg = volgt uit het gewicht.</summary>
    public decimal? VastPercentage { get; set; }
    public decimal Gewicht { get; set; } = 1m;
    public bool PerVerdieping { get; set; }
    /// <summary>"kelder" of "dak"; samen met <see cref="VoorwaardeWaarde"/> ("ja", "nee", "hellend", "plat"). Leeg = altijd.</summary>
    public string? VoorwaardeVariabele { get; set; }
    public string? VoorwaardeWaarde { get; set; }
}

/// <summary>Een schijvensjabloon in de editor (Id 0 = nieuw).</summary>
public sealed class SchijfSjabloonBewerk
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public decimal MinimumPercentage { get; set; } = 5m;
    public bool IsActief { get; set; } = true;
    public List<SchijfSjabloonSchijfBewerk> Schijven { get; set; } = new();
}

public sealed class SchijfSjabloonBewerkUitkomst
{
    public bool Ok { get; set; }
    public int Id { get; set; }
    public List<string> Fouten { get; set; } = new();
}

public sealed class SchijfSjabloonBeheerRij
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public int AantalSchijven { get; set; }
    public bool HeeftVerdiepingen { get; set; }
    public bool IsActief { get; set; }
    public DateTime? GewijzigdOp { get; set; }
}

/// <summary>Een sjabloon in de keuzelijst van het scherm van een betalingsgroep.</summary>
public sealed class SchijfSjabloonKeuze
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public bool HeeftVerdiepingen { get; set; }
    public bool HeeftKelder { get; set; }
    public bool HeeftDak { get; set; }
}

/// <summary>Eén schijf uit het uitgerekende voorstel.</summary>
public sealed class SchijfVoorstel
{
    public string Naam { get; set; } = "";
    public decimal Percentage { get; set; }
    public string? PlanningCode { get; set; }
    public int? PlanningVolgnr { get; set; }
    /// <summary>Het percentage was vast ingesteld in het sjabloon (anders volgt het uit een gewicht).</summary>
    public bool IsVast { get; set; }
}

public sealed class SchijfVoorbeeld
{
    public List<SchijfVoorstel> Schijven { get; set; } = new();
    public List<string> Waarschuwingen { get; set; } = new();
    public decimal Totaal { get; set; }
}

public interface ISchijfSjabloonService
{
    Task<List<SchijfSjabloonBeheerRij>> BeheerLijst();

    /// <summary>De actieve sjablonen om uit te kiezen.</summary>
    Task<List<SchijfSjabloonKeuze>> Keuzes();

    Task<SchijfSjabloonBewerk?> Haal(int id);
    Task<SchijfSjabloonBewerkUitkomst> Bewaar(SchijfSjabloonBewerk sjabloon, string? userId);
    Task<string?> Verwijder(int id);
    Task<(int? Id, string? Fout)> Dupliceer(int id, string? userId);
    Task<string?> ZetActief(int id, bool actief, string? userId);

    /// <summary>Rekent het sjabloon uit voor dit aantal verdiepingen. Bewaart niets.</summary>
    Task<SchijfVoorbeeld?> Voorbeeld(int id, int verdiepingen, bool kelder = false, string? dak = null);
}
