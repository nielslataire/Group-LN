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
    /// <summary>Schijvensjabloon voor groepen met enkel parkings en bergingen (leeg = het gewone schijvensjabloon).</summary>
    public int? SchijfSjabloonBijgebouwId { get; set; }
    /// <summary>Planningssjabloon voor het project zelf (hoofdplanning, bv. wegenis). Leeg = geen.</summary>
    public int? PlanningSjabloonProjectId { get; set; }
    /// <summary>Bouwstaatcode waarna de woningen starten (bv. WEGENIS_VERHARDING); leeg = onmiddellijk.</summary>
    public string? WoningenStartNa { get; set; }
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
    /// <summary>De bouwstaat waarna de woningen starten volgens het sjabloon (enkel bij een sjabloon met planning voor het project zelf); anders leeg.</summary>
    public string? WoningenStartNa { get; set; }
    /// <summary>Het planningssjabloon kent gevelafwerkingen (pleister, hout, aluminium): dan vraagt het toepassen welke van toepassing zijn.</summary>
    public bool HeeftGevel { get; set; }
    /// <summary>Het planningssjabloon is voor een gebouw (blokken per bouwlaag): dan staat "Eén planning voor het gebouw" standaard aan en is er geen "woningen per planning".</summary>
    public bool IsGebouw { get; set; }
    /// <summary>"planning 9 fases · traject 7 fases · 14 schijven": wat erin zit, voor de keuzekaart in de modal.</summary>
    public string? Kern { get; set; }
}

/// <summary>De antwoorden over het gebouw waaraan het sjabloon zich aanpast.</summary>
public sealed class ProjectSjabloonAntwoorden
{
    /// <summary>Het aantal bouwlagen boven de grond, het gelijkvloers meegeteld (gelijkvloers + 2 verdiepingen = 3).</summary>
    public int Verdiepingen { get; set; } = 1;
    public bool Kelder { get; set; }
    /// <summary>"hellend", "plat" of leeg.</summary>
    public string? Dak { get; set; }
    /// <summary>De bouwlagen (1 = gelijkvloers, 2 = 1ste verdieping, ...) met terrassen of luifels bij de afdek. Leeg = geen.</summary>
    public List<int> Terrassen { get; set; } = new();
    /// <summary>De gekozen extra gevelafwerkingen naast de stenen gevel ("pleister", "hout", "aluminium"); de stenen gevel is altijd aanwezig. Volgorde: steen, pleister, dan hout of aluminium.</summary>
    public List<string> Gevelafwerkingen { get; set; } = new();
    /// <summary>Eén planning voor alle eenheden zonder planning (een gebouw); anders een planning per eenheid (woningen).</summary>
    public bool EenPlanningVoorAlles { get; set; }
    /// <summary>Samen met <see cref="EenPlanningVoorAlles"/>: de planning komt in de hoofdplanning (enkel als die nog leeg is); anders een aparte planning.</summary>
    public bool InHoofdplanning { get; set; }
    /// <summary>Bij een planning per eenheid: het aantal woningen per planning (2 of meer = een planning per groep, bv. 7 = lot 1 tot 7, lot 8 tot 14). 0 of 1 = een planning per woning.</summary>
    public int GroepGrootte { get; set; }
    /// <summary>Zelf gekozen indeling bij een planning per eenheid: per planning de eenheden (id's) die erin komen. Heeft voorrang op <see cref="GroepGrootte"/>; eenheden zonder planning die in geen groep staan, krijgen elk een eigen planning.</summary>
    public List<List<int>> Groepen { get; set; } = new();
    /// <summary>Eerst alle bestaande plannings van het project verwijderen (en de fases en taken van de hoofdplanning), daarna het sjabloon toepassen. Vraagt het recht Verwijderen op Planning; kan niet ongedaan gemaakt worden.</summary>
    public bool VervangPlanning { get; set; }
    /// <summary>Het startmoment van de woningen: leeg = de standaard van het sjabloon, "GEEN" = meteen op de aanvangsdatum van de bouwwerken, anders de code van een bouwstaat (bv. WEGENIS_VERHARDING).</summary>
    public string? WoningenStartNa { get; set; }
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

    /// <summary>Het projectsjabloon dat de AI het best vindt passen (enkel uit de actieve sjablonen); leeg als er geen past.</summary>
    public int? SjabloonId { get; set; }
    /// <summary>Waarom dit sjabloon (of waarom geen).</summary>
    public string? SjabloonReden { get; set; }
    /// <summary>Wat de AI heeft aangenomen omdat het er niet stond (bv. "Geen kelder vermeld: geen kelder aangenomen").</summary>
    public List<string> Aannames { get; set; } = new();
    /// <summary>Vragen die het plan sterk zouden veranderen en die de beschrijving niet beantwoordt.</summary>
    public List<string> Vragen { get; set; } = new();
    /// <summary>Doorgerekend door de planning, niet door de AI: de duur van het voorstel tegenover de uitvoeringstermijn.</summary>
    public string? Controle { get; set; }
}

/// <summary>
/// Wat de AI over het project en de sjablonen mag zien. Bewust anoniem: aantallen en sjabloonstructuur, geen namen van klanten, aannemers of eenheden,
/// geen adressen en geen bedragen.
/// </summary>
public sealed class ProjectSjabloonAiContext
{
    public List<ProjectSjabloonAiKeuze> Keuzes { get; set; } = new();
    public int AantalEenheden { get; set; }
    /// <summary>De uitvoeringstermijn van het project in werkdagen (<c>Project.ExecutionDays</c>); leeg = niet ingevuld.</summary>
    public int? MaxWerkdagen { get; set; }
    public bool HeeftPlanning { get; set; }
    public bool HeeftTraject { get; set; }
    public bool HeeftSchijven { get; set; }
}

public sealed class ProjectSjabloonAiKeuze
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public int Bouwlagen { get; set; } = 1;
    public bool Kelder { get; set; }
    public string? Dak { get; set; }
    /// <summary>Wat het planningssjabloon kent: per bouwlaag herhaald, kelder, dak, terrassen of luifels.</summary>
    public bool HeeftBouwlagen { get; set; }
    public bool HeeftKelder { get; set; }
    public bool HeeftDak { get; set; }
    public bool HeeftTerras { get; set; }
    public bool HeeftGevel { get; set; }
    /// <summary>Korte samenvatting, bv. "11 blokken, 38 taken" en de namen van de blokken.</summary>
    public string? Planning { get; set; }
    public string? Traject { get; set; }
    public string? Schijven { get; set; }
}

/// <summary>Het projectsjabloon dat op het project gekozen is, met de bewaarde antwoorden en wat er al van toegepast is (E1).</summary>
public sealed class ProjectSjabloonProjectKeuze
{
    public int? SjabloonId { get; set; }
    public string? SjabloonNaam { get; set; }
    public ProjectSjabloonAntwoorden Antwoorden { get; set; } = new();
    public bool HeeftTraject { get; set; }
    public bool HeeftPlanning { get; set; }
    public bool HeeftSchijven { get; set; }
}

public interface IProjectSjabloonService
{
    /// <summary>De bewaarde keuze van het project (leeg als er geen is) en wat er al van toegepast is.</summary>
    Task<ProjectSjabloonProjectKeuze> ProjectKeuze(int projectId);

    /// <summary>
    /// Bewaart het gekozen projectsjabloon en de antwoorden op het project (<paramref name="sjabloonId"/> leeg = keuze wissen). Met <paramref name="maakTraject"/> wordt het traject
    /// meteen aangemaakt als het project er nog geen heeft; planning en schijven blijven een bewuste actie (toepassen in het planningvenster).
    /// </summary>
    Task<ProjectSjabloonResultaat> BewaarProjectKeuze(int projectId, int? sjabloonId, ProjectSjabloonAntwoorden? antwoorden, bool maakTraject, string? userId);

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

    /// <summary>De anonieme context voor de AI: de actieve projectsjablonen met hun structuur en enkele aantallen van het project.</summary>
    Task<ProjectSjabloonAiContext> AiContext(int projectId);

    /// <summary>Rekent het voorstel door met de planning (niet de AI): de duur van één bouwdeel tegenover de uitvoeringstermijn. Bewaart niets.</summary>
    Task<string?> Controle(int projectId, int sjabloonId, ProjectSjabloonAntwoorden antwoorden);
}

public interface IProjectSjabloonAiService
{
    /// <summary>
    /// Kiest uit een vrije beschrijving het projectsjabloon dat past en haalt de antwoorden eruit die het sjabloon nodig heeft.
    /// Doet enkel een voorstel, bewaart niets. Zonder context vult de AI enkel de antwoorden in.
    /// </summary>
    Task<ProjectSjabloonAiVoorstel> Voorstel(string beschrijving, ProjectSjabloonAiContext? context = null, CancellationToken ct = default);
}
