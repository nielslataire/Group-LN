namespace FacadeCore;

/// <summary>Een rij in de lijst Instellingen › Bouwstaten (migratie 158).</summary>
public sealed class BouwstaatRij
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Naam { get; set; } = "";
    public string? Groep { get; set; }
    public int Volgorde { get; set; }
    /// <summary>0 = geldt voor het hele gebouw, 1 = per eenheid.</summary>
    public byte Bereik { get; set; }
    public bool PerBouwlaag { get; set; }
    public bool IsActief { get; set; }
    /// <summary>Leeg = bedrijfsbreed, anders een eigen bouwstaat van dit project.</summary>
    public int? ProjectId { get; set; }
    /// <summary>Aantal sjabloon- en planningtaken, schijven en fases die ernaar verwijzen; een gebruikte bouwstaat kan niet verwijderd worden.</summary>
    public int GebruiktIn { get; set; }
}

public sealed class BouwstaatBewerk
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Naam { get; set; } = "";
    public string? Groep { get; set; }
    public int Volgorde { get; set; }
    public byte Bereik { get; set; }
    public bool PerBouwlaag { get; set; }
    public bool IsActief { get; set; } = true;
}

public sealed class BouwstaatUitkomst
{
    public bool Ok { get; set; }
    public int Id { get; set; }
    public List<string> Fouten { get; set; } = new();
}

public interface IBouwstaatService
{
    /// <summary>Alle bedrijfsbrede bouwstaten, per groep en volgorde, met het aantal verwijzingen.</summary>
    Task<List<BouwstaatRij>> Lijst();

    Task<BouwstaatBewerk?> Haal(int id);

    /// <summary>Nieuw (Id 0) of bestaand. De code ligt vast na het aanmaken; hoofdletters, cijfers en underscore, uniek.</summary>
    Task<BouwstaatUitkomst> Bewaar(BouwstaatBewerk invoer);

    /// <summary>Verwijdert enkel een bouwstaat die nergens gebruikt wordt; geeft anders een melding terug.</summary>
    Task<string?> Verwijder(int id);

    Task<string?> ZetActief(int id, bool actief);

    /// <summary>Actieve bouwstaten en alle activiteiten (met hun groep) om uit te kiezen in de sjablooneditors.</summary>
    Task<BouwstaatKeuzes> Keuzes();
}

/// <summary>Een keuze in een lijst van de sjablooneditors.</summary>
public sealed class BouwstaatKeuze
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Groep { get; set; }
    public string Code { get; set; } = "";
    public bool IsActief { get; set; } = true;
}

public sealed class ActiviteitKeuze
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    /// <summary>"2 Bovenbouw": het lot en de naam van de activiteitgroep.</summary>
    public string? Groep { get; set; }
}

/// <summary>De keuzelijsten voor bouwstaat en activiteit in de planningssjabloon- en schijvensjabloon-editor.</summary>
public sealed class BouwstaatKeuzes
{
    public List<BouwstaatKeuze> Bouwstaten { get; set; } = new();
    public List<ActiviteitKeuze> Activiteiten { get; set; } = new();
}
