using System.Text.Json;

namespace FacadeCore;

/// <summary>Eén wijziging aan de bouwplanning, vanuit de pagina. <see cref="Type"/> bepaalt welke velden gelezen worden.</summary>
public sealed class PlanningCommando
{
    /// <summary>taakToevoegen · faseToevoegen · wijzig · verwijder · verplaats · basisVastleggen · verletMeetellen · herstel · planningToevoegen · planningWijzig · planningVerwijder</summary>
    public string Type { get; set; } = "";

    /// <summary>De planning (hoofdplanning of bouwdeel) waarop het commando slaat; leeg of 0 = de hoofdplanning. Bij planningWijzig/planningVerwijder: de planning zelf.</summary>
    public int? PlanningId { get; set; }

    /// <summary>Versie van de planning waarop de pagina zich baseert (conflictdetectie).</summary>
    public int Versie { get; set; }

    /// <summary>"taak" of "fase" (voor wijzig en verplaats).</summary>
    public string? Soort { get; set; }

    public int? Id { get; set; }

    /// <summary>taakToevoegen: fase waarin de taak komt. faseToevoegen: bovenliggende fase (leeg = hoogste niveau).</summary>
    public int? SectieId { get; set; }

    /// <summary>Plaats: direct na deze rij (met <see cref="NaSoort"/>); leeg = achteraan in de fase.</summary>
    public int? NaId { get; set; }

    public string? NaSoort { get; set; }

    /// <summary>Bij wijzig/toevoegen: de velden die gezet worden (naam, duur, voorgangers, voortgang, beperking, beperkingDatum, deadline, werkelijkStart, werkelijkEinde, notitie, verantwoordelijke, teltMee, lotId). Null = leegmaken.</summary>
    public Dictionary<string, JsonElement>? Velden { get; set; }

    /// <summary>faseToevoegen: optionele eerste taak.</summary>
    public string? EersteTaakNaam { get; set; }

    public int? EersteTaakDuur { get; set; }

    /// <summary>faseToevoegen: de fase start na deze voorgangers (bv. "4.2; 5.1FS+2d"); geldt voor haar eerste taak.</summary>
    public string? StartNa { get; set; }

    /// <summary>verwijder: de te verwijderen rijen.</summary>
    public List<PlanningItemRef>? Items { get; set; }

    /// <summary>verplaats: omhoog · omlaag · inspringen · uitspringen.</summary>
    public string? Actie { get; set; }

    /// <summary>herstel: de momentopname die bij de vorige uitkomst hoorde (<see cref="PlanningUitkomst.Undo"/>).</summary>
    public string? Momentopname { get; set; }
}

public sealed class PlanningItemRef
{
    public string Soort { get; set; } = "taak";
    public int Id { get; set; }
}

public sealed class PlanningUitkomst
{
    public bool Ok { get; set; }
    public string? Fout { get; set; }

    /// <summary>Iemand anders wijzigde de planning intussen: de pagina moet herladen.</summary>
    public bool Conflict { get; set; }

    public PlanningStaat? Staat { get; set; }

    /// <summary>planningToevoegen: de nieuwe planning, zodat de pagina erheen kan schakelen.</summary>
    public int? NieuwePlanningId { get; set; }

    /// <summary>Momentopname van de planning vóór deze wijziging, voor "Ongedaan maken".</summary>
    public string? Undo { get; set; }

    /// <summary>Rij-sleutels (t12 / f3) waarvan de datums verschoven zijn door deze wijziging, zonder de rij zelf.</summary>
    public List<string> Verschoven { get; set; } = new();

    /// <summary>Korte zin voor de wijzigingsbalk, bv. "7 andere taken verschoven · einde +4 werkdagen (13/03/2027)".</summary>
    public string? Samenvatting { get; set; }
}

public sealed class PlanningStaat
{
    public int ProjectId { get; set; }
    public int Versie { get; set; }
    public bool HeeftPlanning { get; set; }

    /// <summary>De planning waarvan de rijen en de kop getoond worden.</summary>
    public int ActiefPlanningId { get; set; }

    /// <summary>Alle plannings van het project, de hoofdplanning eerst. Bestaat er nog geen, dan staat hier een virtuele hoofdplanning met Id 0.</summary>
    public List<PlanningInfo> Planningen { get; set; } = new();

    /// <summary>Bouwdelen (betalingsgroepen) van het project, om een planning aan te koppelen.</summary>
    public List<PlanningBouwdeelOptie> Bouwdelen { get; set; } = new();

    public PlanningKop Kop { get; set; } = new();
    public List<PlanningRij> Rijen { get; set; } = new();
    public List<PlanningMelding> Meldingen { get; set; } = new();
    public List<PlanningGebruiker> Gebruikers { get; set; } = new();

    /// <summary>Budgetlots waaraan een fase gekoppeld kan worden (voor de fysieke voortgang).</summary>
    public List<PlanningGebruiker> Lots { get; set; } = new();
    public List<PlanningNietWerkdag> NietWerkdagen { get; set; } = new();

    /// <summary>Mijlpalen uit het Traject van dit project die aan een taak of fase gekoppeld kunnen worden: vrije (handmatige) en die al aan de planning hangen.</summary>
    public List<PlanningMijlpaalOptie> TrajectMijlpalen { get; set; } = new();
}

public sealed class PlanningInfo
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public bool IsHoofd { get; set; }
    public int? BouwdeelId { get; set; }
    public string? BouwdeelNaam { get; set; }

    /// <summary>Ingestelde startdatum van de planning (yyyy-MM-dd); leeg = aanvangsdatum van het project.</summary>
    public string? StartDatum { get; set; }

    public int AantalTaken { get; set; }
    public string? Start { get; set; }
    public string? Einde { get; set; }
}

public sealed class PlanningBouwdeelOptie
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public int AantalEenheden { get; set; }

    /// <summary>De planning die dit bouwdeel al heeft; leeg = vrij.</summary>
    public int? PlanningId { get; set; }
}

public sealed class PlanningKop
{
    public string? Start { get; set; }
    public string? Einde { get; set; }
    public int DuurWerkdagen { get; set; }
    public string? ProjectStart { get; set; }
    public int? Uitvoeringstermijn { get; set; }
    public string? Deadline { get; set; }

    /// <summary>Werkdagen tussen het planningseinde en de deadline; negatief = te laat.</summary>
    public int? Marge { get; set; }

    public bool GeenStartdatum { get; set; }
    public bool GeenWeerstation { get; set; }
    public int VerletDagen { get; set; }
    public bool VerletMeetellen { get; set; } = true;
    public string? BasisOp { get; set; }
    public int? BasisAfwijking { get; set; }
}

public sealed class PlanningRij
{
    /// <summary>t{id} of f{id}.</summary>
    public string Sleutel { get; set; } = "";
    public string Soort { get; set; } = "taak";
    public int Id { get; set; }
    public string Wbs { get; set; } = "";
    public int Niveau { get; set; }
    public string Naam { get; set; } = "";
    public int? OuderFaseId { get; set; }
    public int Duur { get; set; }
    public bool IsMijlpaal { get; set; }
    public string? Start { get; set; }
    public string? Einde { get; set; }
    public int Voortgang { get; set; }
    public bool Afgerond { get; set; }
    public bool Kritiek { get; set; }
    public int? Speling { get; set; }
    public string Voorgangers { get; set; } = "";

    /// <summary>De voorgangers als gegevens (voor de pijlen in de Gantt).</summary>
    public List<PlanningLinkInfo> VoorgangerLinks { get; set; } = new();
    public string? BepalendeVoorganger { get; set; }
    public byte Beperking { get; set; }
    public string? BeperkingDatum { get; set; }
    public string? Deadline { get; set; }
    public string? WerkelijkStart { get; set; }
    public string? WerkelijkEinde { get; set; }
    public string? Notitie { get; set; }
    public string? VerantwoordelijkeId { get; set; }
    public string? BasisStart { get; set; }
    public string? BasisEinde { get; set; }
    public int? Afwijking { get; set; }
    public bool TeltMee { get; set; }
    public int? LotId { get; set; }
    public string? LotNaam { get; set; }
    public List<PlanningMijlpaalLink> Mijlpalen { get; set; } = new();
    public List<string> Waarschuwingen { get; set; } = new();
}

public sealed class PlanningLinkInfo
{
    public int TaakId { get; set; }

    /// <summary>FS · SS · FF · SF</summary>
    public string Type { get; set; } = "FS";

    public int Wacht { get; set; }
}

public sealed class PlanningMijlpaalLink
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
}

public sealed class PlanningMelding
{
    /// <summary>info · warning · danger</summary>
    public string Ernst { get; set; } = "warning";
    public string Titel { get; set; } = "";
    public string? Tekst { get; set; }
    public string? RijSleutel { get; set; }
}

public sealed class PlanningGebruiker
{
    public string Id { get; set; } = "";
    public string Naam { get; set; } = "";
}

public sealed class PlanningNietWerkdag
{
    public string Datum { get; set; } = "";

    /// <summary>feestdag · verlof · verlet</summary>
    public string Soort { get; set; } = "";
}

/// <summary>Een mijlpaal uit het Traject, als keuze bij "Koppeling met trajectmijlpaal".</summary>
public sealed class PlanningMijlpaalOptie
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";

    /// <summary>De fase van het Traject waarin de mijlpaal staat (voor de weergave).</summary>
    public string? FaseNaam { get; set; }

    /// <summary>Sleutel (t12 / f3) van de taak of fase waaraan de mijlpaal nu hangt; leeg = vrij.</summary>
    public string? GekoppeldAanSleutel { get; set; }

    /// <summary>Nummer en naam van die taak of fase, voor de weergave ("3.2 Dakbedekking").</summary>
    public string? GekoppeldAanTekst { get; set; }
}

/// <summary>Koppelt een trajectmijlpaal aan een taak of fase van de planning, of haalt de koppeling weg.</summary>
public sealed class PlanningKoppelCommando
{
    /// <summary>"taak" of "fase".</summary>
    public string Soort { get; set; } = "taak";

    public int Id { get; set; }

    /// <summary>De planning die de pagina toont, zodat het antwoord dezelfde planning teruggeeft; leeg = de hoofdplanning.</summary>
    public int? PlanningId { get; set; }

    /// <summary>De mijlpaal die gekoppeld wordt; leeg = enkel ontkoppelen.</summary>
    public int? MijlpaalId { get; set; }

    /// <summary>De mijlpaal die tot nu toe in de keuzelijst stond en losgekoppeld wordt (leeg = geen).</summary>
    public int? VorigeMijlpaalId { get; set; }
}

public interface IPlanningService
{
    /// <param name="planningId">De planning die getoond wordt; leeg of onbekend = de hoofdplanning.</param>
    Task<PlanningStaat> Laad(int projectId, int? planningId = null);

    Task<PlanningUitkomst> Voer(int projectId, PlanningCommando commando, string? userId);

    /// <summary>Koppelt een trajectmijlpaal aan een taak of fase (of haalt de koppeling weg). De mijlpaal in het Traject volgt dan de planning.</summary>
    Task<PlanningUitkomst> Koppel(int projectId, PlanningKoppelCommando commando, string? userId);
}
