using System.Collections.Generic;

namespace FacadeCore;

/// <summary>Eén regel uit een geïmporteerd planningsbestand (MS Project-XML), zoals ze in het voorbeeld en in het importcommando staat.</summary>
public sealed class PlanningImportRij
{
    /// <summary>Het nummer van de regel in het bronbestand; voorgangers verwijzen ernaar.</summary>
    public int Uid { get; set; }

    public string Wbs { get; set; } = "";
    public string Naam { get; set; } = "";

    /// <summary>1 = hoogste niveau.</summary>
    public int Niveau { get; set; } = 1;

    /// <summary>fase · taak · mijlpaal</summary>
    public string Soort { get; set; } = "taak";

    /// <summary>Duur in werkdagen (0 bij een fase of mijlpaal).</summary>
    public int Duur { get; set; }

    /// <summary>Start en einde uit het bestand (yyyy-MM-dd), enkel ter info in het voorbeeld.</summary>
    public string? Start { get; set; }
    public string? Einde { get; set; }
    public int Voortgang { get; set; }

    /// <summary>0 zo vroeg mogelijk · 1 moet beginnen op · 2 niet eerder beginnen dan · 3 niet later beginnen dan · 4 niet eerder eindigen dan · 5 niet later eindigen dan · 6 moet eindigen op.</summary>
    public byte Beperking { get; set; }
    public string? BeperkingDatum { get; set; }
    public string? Deadline { get; set; }
    public string? WerkelijkStart { get; set; }
    public string? WerkelijkEinde { get; set; }
    public string? BasisStart { get; set; }
    public string? BasisEinde { get; set; }
    public string? Notitie { get; set; }
    public List<PlanningImportVoorganger> Voorgangers { get; set; } = new();

    /// <summary>De resources (meestal aannemers) die in het bestand aan deze taak hangen.</summary>
    public List<string> Resources { get; set; } = new();

    /// <summary>ok · waarschuwing · fout (enkel in het voorbeeld)</summary>
    public string Status { get; set; } = "ok";
    public string? Melding { get; set; }
}

public sealed class PlanningImportVoorganger
{
    public int Uid { get; set; }

    /// <summary>FS · SS · FF · SF</summary>
    public string Type { get; set; } = "FS";

    /// <summary>Wachttijd in werkdagen, ook negatief (overlap).</summary>
    public int Wacht { get; set; }
}

/// <summary>Het resultaat van het inlezen van een bestand: het voorbeeld in stap 2 van de import.</summary>
public sealed class PlanningImportBestand
{
    public bool Ok { get; set; }
    public string? Fout { get; set; }
    public string Bestandsnaam { get; set; } = "";
    public string? Projectnaam { get; set; }

    /// <summary>De startdatum van het project in het bestand (yyyy-MM-dd).</summary>
    public string? ProjectStart { get; set; }

    public List<PlanningImportRij> Rijen { get; set; } = new();
    public int AantalTaken { get; set; }
    public int AantalFases { get; set; }
    public int AantalMijlpalen { get; set; }
    public int AantalAfhankelijkheden { get; set; }
    public int AantalAandachtspunten { get; set; }
    public int AantalFouten { get; set; }

    /// <summary>De resources uit het bestand die aan minstens één taak hangen, met het aantal taken (voor de stap "Uitvoerders koppelen").</summary>
    public List<PlanningImportResource> Resources { get; set; } = new();

    /// <summary>Bedrijven waaraan een resource gekoppeld kan worden (ingevuld door de controller; niet door de parser).</summary>
    public List<PlanningUitvoerderOptie> Bedrijven { get; set; } = new();

    /// <summary>Algemene opmerkingen die niet bij één regel horen.</summary>
    public List<string> Meldingen { get; set; } = new();
}

/// <summary>Wat er met een ingelezen bestand moet gebeuren (command <c>importeren</c>).</summary>
public sealed class PlanningImportOpdracht
{
    public List<PlanningImportRij> Rijen { get; set; } = new();

    /// <summary>true = een nieuwe planning (met <see cref="Naam"/> en <see cref="EenheidIds"/>); false = in <see cref="PlanningCommando.PlanningId"/>.</summary>
    public bool Nieuw { get; set; } = true;

    public string? Naam { get; set; }
    public List<int> EenheidIds { get; set; } = new();

    /// <summary>Bij een bestaande planning: "toevoegen" (achteraan) of "vervangen" (de huidige fases en taken verdwijnen).</summary>
    public string Modus { get; set; } = "toevoegen";

    /// <summary>"bestand" = de startdatum uit het bestand overnemen; "nieuw" = <see cref="StartDatum"/> gebruiken; "geen" = de planning niet aanraken.</summary>
    public string StartModus { get; set; } = "bestand";

    public string? StartDatum { get; set; }
    public string? BestandStart { get; set; }

    /// <summary>Resourcenaam uit het bestand → bedrijf (<c>CompanyInfo.CompanyId</c>). Een niet gekoppelde naam komt als tekst in de notitie van de taak.</summary>
    public Dictionary<string, int> Uitvoerders { get; set; } = new();
}

public sealed class PlanningImportResource
{
    public string Naam { get; set; } = "";
    public int AantalTaken { get; set; }
}
