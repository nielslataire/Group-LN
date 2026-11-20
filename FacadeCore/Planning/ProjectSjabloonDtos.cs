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
    public int? PlanningId { get; set; }
    public int? TrajectId { get; set; }
    public int? SchijvenId { get; set; }
    /// <summary>Kerncijfers per onderdeel voor in de lijst, bv. "2 blokken · 25 wd", "9 mijlpalen", "6 schijven".</summary>
    public string? PlanningKern { get; set; }
    public string? TrajectKern { get; set; }
    public string? SchijvenKern { get; set; }
    /// <summary>Het standaardgebouw in woorden, bv. "2 verd. · hellend dak · kelder".</summary>
    public string StandaardGebouw { get; set; } = "";
    public bool IsActief { get; set; }
    public DateTime? GewijzigdOp { get; set; }
    public string? GewijzigdDoor { get; set; }
}

/// <summary>Wat het projectsjabloon met deze keuzes zou doen, voor de editor (kaarten met kerncijfers en "Zo wordt het toegepast").</summary>
public sealed class ProjectSjabloonOverzicht
{
    public ProjectSjabloonOverzichtPlanning? Planning { get; set; }
    public ProjectSjabloonOverzichtTraject? Traject { get; set; }
    public ProjectSjabloonOverzichtSchijven? Schijven { get; set; }
    /// <summary>Gouden meldingen, bv. schijven die geen taak in de planning vinden.</summary>
    public List<string> Meldingen { get; set; } = new();
}

public sealed class ProjectSjabloonOverzichtPlanning
{
    public string Naam { get; set; } = "";
    public int Blokken { get; set; }
    public int Taken { get; set; }
    public int Werkdagen { get; set; }
    public bool PerVerdieping { get; set; }
    /// <summary>"Ruwbouw 25 wd · Afwerkingen 3 wd" voor het gekozen gebouw.</summary>
    public string Verloop { get; set; } = "";
    public int Fases { get; set; }
}

public sealed class ProjectSjabloonOverzichtTraject
{
    public string Naam { get; set; } = "";
    public int Mijlpalen { get; set; }
    public string? Omschrijving { get; set; }
    /// <summary>Mijlpalen waarvan de code in de planning voorkomt: die volgen de planning.</summary>
    public int Koppelingen { get; set; }
}

public sealed class ProjectSjabloonOverzichtSchijven
{
    public string Naam { get; set; } = "";
    public int Aantal { get; set; }
    public string Vast { get; set; } = "";
    public string Toelichting { get; set; } = "";
    /// <summary>Codes uit het schijvensjabloon die niet in het gekozen planningssjabloon staan.</summary>
    public List<string> CodesNietInPlanning { get; set; } = new();
    public List<SchijfVoorstel> Voorstel { get; set; } = new();
    public decimal Totaal { get; set; }
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
    /// <summary>Samen met <see cref="EenPlanningVoorAlles"/>: de planning komt in de hoofdplanning (enkel als die nog leeg is); anders een aparte planning.</summary>
    public bool InHoofdplanning { get; set; }
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

    /// <summary>Kerncijfers van de gekozen onderdelen en het voorbeeld voor dit gebouw. Bewaart niets.</summary>
    Task<ProjectSjabloonOverzicht> Overzicht(int? planningId, int? trajectId, int? schijfId, int verdiepingen, bool kelder, string? dak);
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
