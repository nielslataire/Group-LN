namespace CPMCore.Models.MijnTaken;

/// <summary>Mijn taken als persoonlijke assistent (design-handoff punt 2): viewmodels voor Vandaag, Week, Inbox en Overzicht.</summary>
public class MtProject
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string Kleur { get; set; } = "";
}

public class MtTaak
{
    public int Id { get; set; }
    public string Titel { get; set; } = "";
    public int? ProjectId { get; set; }
    public string Project { get; set; } = "";
    public string Kleur { get; set; } = "#9aa898";
    public string? Context { get; set; }
    public int? Schatting { get; set; }
    public DateTime? GeplandStart { get; set; }
    public int? GeplandMinuten { get; set; }
    public DateOnly? Vervaldatum { get; set; }
    public int Prioriteit { get; set; }
    public int Status { get; set; }
    public string? WachtOp { get; set; }
    public DateTime? WachtOpDatum { get; set; }
    public string? InboxBron { get; set; }
    public string? InboxMeta { get; set; }
    public DateTime Aangemaakt { get; set; }
}

public class MtBlok
{
    public int? TaakId { get; set; }
    public string Titel { get; set; } = "";
    public string Project { get; set; } = "";
    public string Kleur { get; set; } = "#9aa898";
    public DateTime Start { get; set; }
    public DateTime Einde { get; set; }
    /// <summary>Alleen bij gewerkte tijd: de timer loopt nog.</summary>
    public bool Loopt { get; set; }
}

public class MtDeadline
{
    public int MijlpaalId { get; set; }
    public int ProjectId { get; set; }
    public string Project { get; set; } = "";
    public string Kleur { get; set; } = "";
    public string Naam { get; set; } = "";
    public DateOnly Datum { get; set; }
    public bool HeeftTaak { get; set; }
    public int Dagen { get; set; }
}

public class MtShell
{
    /// <summary>vandaag, week, inbox of overzicht.</summary>
    public string Tab { get; set; } = "vandaag";
    public string Ondertitel { get; set; } = "";
    public int InboxAantal { get; set; }
    /// <summary>De lopende timer, of de laatst gestopte (Pauze) als er nog een taak openstaat.</summary>
    public MtBlok? Timer { get; set; }
    public List<MtProject> Projecten { get; set; } = new();
}

public class MtVandaagVm
{
    public MtShell Shell { get; set; } = new();
    public DateOnly Datum { get; set; }
    public List<MtDeadline> Deadlines { get; set; } = new();
    public List<string> Contexten { get; set; } = new();
    public List<MtTaak> Acties { get; set; } = new();
    public List<MtTaak> Wachten { get; set; } = new();
    public List<MtBlok> Gepland { get; set; } = new();
    public List<MtBlok> Gewerkt { get; set; } = new();
    /// <summary>Afspraken uit de Outlook-agenda van vandaag (met een uur).</summary>
    public List<CPMCore.Services.AgendaAfspraak> Afspraken { get; set; } = new();
    /// <summary>Afspraken van vandaag die de hele dag duren.</summary>
    public List<CPMCore.Services.AgendaAfspraak> HeleDag { get; set; } = new();
    /// <summary>Waarom de agenda ontbreekt (leeg = alles in orde).</summary>
    public string? AgendaFout { get; set; }
    public List<(string Dag, string Titel, string Soort)> DezeWeek { get; set; } = new();
    public int WerkMinuten { get; set; }
    public int GeplandMinuten { get; set; }
    public List<(string Project, string Kleur, int Minuten)> PerProject { get; set; } = new();
    /// <summary>Tijdlijn van-tot (uren).</summary>
    public int UurVan { get; set; } = 6;
    public int UurTot { get; set; } = 22;
}

public class MtWeekDag
{
    public DateOnly Datum { get; set; }
    public List<MtBlok> Gepland { get; set; } = new();
    public List<MtBlok> Gewerkt { get; set; } = new();
    public List<string> Deadlines { get; set; } = new();
    public List<CPMCore.Services.AgendaAfspraak> Afspraken { get; set; } = new();
    public List<CPMCore.Services.AgendaAfspraak> HeleDag { get; set; } = new();
    public int GeplandMinuten { get; set; }
}

public class MtWeekVm
{
    public MtShell Shell { get; set; } = new();
    public DateOnly Maandag { get; set; }
    public int Weeknummer { get; set; }
    public List<MtWeekDag> Dagen { get; set; } = new();
    public List<MtTaak> Ongepland { get; set; } = new();
    public int CapaciteitMinuten { get; set; }
    public string? AgendaFout { get; set; }
    public int UurVan { get; set; } = 6;
    public int UurTot { get; set; } = 22;
}

/// <summary>Een gesynchroniseerde Outlook-mail in de Inbox (enkel metadata en voorvertoning).</summary>
public class MtMail
{
    public int Id { get; set; }
    public string Afzender { get; set; } = "";
    public string Onderwerp { get; set; } = "";
    public string Voorvertoning { get; set; } = "";
    public DateTime Ontvangen { get; set; }
    public string Richting { get; set; } = "in";
    public string? WebLink { get; set; }
    public int? ProjectId { get; set; }
    public string? Project { get; set; }
    public string? Klant { get; set; }
    public string? Leverancier { get; set; }
    public bool TeKoppelen { get; set; }
    public bool VerwijderdInOutlook { get; set; }
}

public class MtInboxVm
{
    public MtShell Shell { get; set; } = new();
    public List<MtTaak> Items { get; set; } = new();
    /// <summary>Ontvangen mails die nog een reactie vragen (Postvak IN, niet afgehandeld).</summary>
    public List<MtMail> Mails { get; set; } = new();
    /// <summary>Verzonden mails, enkel om terug te vinden (ingeklapt).</summary>
    public List<MtMail> Verzonden { get; set; } = new();
    public List<string> Contexten { get; set; } = new();
}

public class MtOverzichtVm
{
    public MtShell Shell { get; set; } = new();
    public DateOnly Maandag { get; set; }
    public int Weeknummer { get; set; }
    public int GewerktMinuten { get; set; }
    public int GeplandMinuten { get; set; }
    public int Afgerond { get; set; }
    public int FocusMinuten { get; set; }
    public List<(string Naam, string Kleur, int Minuten)> PerProject { get; set; } = new();
    public List<(string Naam, int Minuten)> PerSoort { get; set; } = new();
    public List<(string Dag, int Gepland, int Gewerkt)> PerDag { get; set; } = new();
    public int Afgevinkt { get; set; }
    public List<MtTaak> Focus { get; set; } = new();
    public int WachtenOpen { get; set; }
    public int WachtenTeLaat { get; set; }
}

/// <summary>Deadlines (design-handoff punt 1b): mijlpalen per project met voortgang.</summary>
public class MtMijlpaalRij
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Eenheid { get; set; }
    public string Rol { get; set; } = "—";
    public DateOnly? Datum { get; set; }
    public int Status { get; set; }
    public int Dagen { get; set; }
    public bool Klaar { get; set; }
    public bool Laat { get; set; }
    /// <summary>Datum ver in de toekomst (waarschijnlijk een tikfout).</summary>
    public bool Onwaarschijnlijk { get; set; }
    public bool Mijn { get; set; }
    public bool HeeftTaak { get; set; }
}

public class MtDeadlineProject
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string Kleur { get; set; } = "";
    public List<MtMijlpaalRij> Rijen { get; set; } = new();
    public int Bereikt => Rijen.Count(r => r.Klaar);
    public int Laat => Rijen.Count(r => r.Laat);
    public int Onwaarschijnlijk => Rijen.Count(r => r.Onwaarschijnlijk && !r.Klaar);
}

public class MtDeadlinesVm
{
    public List<MtDeadlineProject> Projecten { get; set; } = new();
    public int Achterstallig { get; set; }
    public int Komende30 { get; set; }
    public int Bereikt { get; set; }
    public int Totaal { get; set; }
    public MtDeadlineProject? NakijkenProject { get; set; }
    public int NakijkenAantal { get; set; }
    public bool KanTaakMaken { get; set; }
}

/// <summary>Invoer voor <c>Views/MijnTaken/Partials/_MtKalItemV2.cshtml</c>: één blok in de dag- of weekkalender.</summary>
public class MtKalItemVm
{
    public CPMCore.Services.MijnTaken.MtFormat.KalItem Item { get; set; } = new();
    public int Px { get; set; }
    public int UurVan { get; set; }
    public bool Compact { get; set; }
}
