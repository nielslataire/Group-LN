namespace FacadeCore;

/// <summary>Een projectsjabloon in de editor (Id 0 = nieuw).</summary>
public sealed class ProjectSjabloonBewerk
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public int? PlanningSjabloonId { get; set; }
    public int? TrajectSjabloonId { get; set; }
    public int? SchijfSjabloonId { get; set; }
    public int StandaardVerdiepingen { get; set; } = 1;
    public bool StandaardKelder { get; set; }
    /// <summary>"hellend", "plat" of leeg.</summary>
    public string? StandaardDak { get; set; }
    public bool IsActief { get; set; } = true;
}

public sealed class ProjectSjabloonBewerkUitkomst
{
    public bool Ok { get; set; }
    public int Id { get; set; }
    public List<string> Fouten { get; set; } = new();
}

public sealed class ProjectSjabloonBeheerRij
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public string? PlanningNaam { get; set; }
    public string? TrajectNaam { get; set; }
    public string? SchijvenNaam { get; set; }
    public bool IsActief { get; set; }
    public DateTime? GewijzigdOp { get; set; }
}

/// <summary>Een keuze uit een lijst sjablonen (voor de selectievelden van de editor).</summary>
public sealed class SjabloonOptie
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
}

public sealed class ProjectSjabloonOpties
{
    public List<SjabloonOptie> Planning { get; set; } = new();
    public List<SjabloonOptie> Traject { get; set; } = new();
    public List<SjabloonOptie> Schijven { get; set; } = new();
}

/// <summary>Een projectsjabloon om uit te kiezen op de planningpagina, met haar standaardantwoorden.</summary>
public sealed class ProjectSjabloonKeuze
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public int Verdiepingen { get; set; } = 1;
    public bool Kelder { get; set; }
    public string? Dak { get; set; }
}

/// <summary>De antwoorden over het gebouw waaraan het sjabloon zich aanpast.</summary>
public sealed class ProjectSjabloonAntwoorden
{
    public int Verdiepingen { get; set; } = 1;
    public bool Kelder { get; set; }
    /// <summary>"hellend", "plat" of leeg.</summary>
    public string? Dak { get; set; }
    /// <summary>Eén planning voor alle eenheden zonder planning (een gebouw); anders een planning per eenheid (woningen).</summary>
    public bool EenPlanningVoorAlles { get; set; }
}

/// <summary>Wat het toepassen zou doen (voorbeeld) of gedaan heeft (uitkomst), per onderdeel in gewone taal.</summary>
public sealed class ProjectSjabloonResultaat
{
    public bool Ok { get; set; }
    public string? Fout { get; set; }
    public List<string> Stappen { get; set; } = new();
    public List<string> Opmerkingen { get; set; } = new();
    /// <summary>Voorbeeld van de schijven die in een betalingsgroep komen.</summary>
    public List<SchijfVoorstel> Schijven { get; set; } = new();
}

/// <summary>Een voorstel van de AI op basis van een beschrijving van het gebouw. De gebruiker kijkt het na voor er iets gebeurt.</summary>
public sealed class ProjectSjabloonAiVoorstel
{
    public bool Ok { get; set; }
    public string? Fout { get; set; }
    public ProjectSjabloonAntwoorden Antwoorden { get; set; } = new();
    /// <summary>Wat de AI begrepen heeft of niet kon afleiden.</summary>
    public string? Toelichting { get; set; }
}

public interface IProjectSjabloonService
{
    Task<List<ProjectSjabloonBeheerRij>> BeheerLijst();
    Task<List<ProjectSjabloonKeuze>> Keuzes();
    Task<ProjectSjabloonOpties> Opties();
    Task<ProjectSjabloonBewerk?> Haal(int id);
    Task<ProjectSjabloonBewerkUitkomst> Bewaar(ProjectSjabloonBewerk sjabloon, string? userId);
    Task<string?> Verwijder(int id);
    Task<string?> ZetActief(int id, bool actief, string? userId);

    /// <summary>Wat toepassen op dit project zou doen. Bewaart niets.</summary>
    Task<ProjectSjabloonResultaat> Voorbeeld(int projectId, int sjabloonId, ProjectSjabloonAntwoorden antwoorden);

    /// <summary>Past het sjabloon toe en vult aan wat nog leeg is: traject, planningen voor eenheden zonder planning, schijven in betalingsgroepen zonder (gebruikte) schijven.</summary>
    Task<ProjectSjabloonResultaat> Toepassen(int projectId, int sjabloonId, ProjectSjabloonAntwoorden antwoorden, string? userId);
}

public interface IProjectSjabloonAiService
{
    /// <summary>Haalt uit een vrije beschrijving van het gebouw de antwoorden die het sjabloon nodig heeft. Doet enkel een voorstel, bewaart niets.</summary>
    Task<ProjectSjabloonAiVoorstel> Voorstel(string beschrijving, CancellationToken ct = default);
}
