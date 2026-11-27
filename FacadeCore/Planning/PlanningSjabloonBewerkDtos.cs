namespace FacadeCore;

/// <summary>Een planningssjabloon zoals de editor (Instellingen › Planningssjablonen) hem leest en bewaart.</summary>
public sealed class PlanningSjabloonBewerk
{
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public bool IsActief { get; set; } = true;

    /// <summary>Sleutel uit <see cref="PlanningSjabloonReageertOp.Opties"/>; leeg = niets, handmatig toepassen.</summary>
    public string? ReageertOp { get; set; }
    public List<PlanningSjabloonBewerkBlok> Blokken { get; set; } = new();
}

public sealed class PlanningSjabloonBewerkBlok
{
    /// <summary>Leeg = CPM maakt een code uit de naam.</summary>
    public string? Code { get; set; }
    public string Naam { get; set; } = "";

    /// <summary>0 = altijd, 1 = per verdieping herhaald.</summary>
    public int Herhaling { get; set; }
    public bool VolgtVorigeVerdieping { get; set; }

    /// <summary>"kelder" of "dak" (met <see cref="VoorwaardeWaarde"/> "ja", "hellend" of "plat"); leeg = altijd.</summary>
    public string? VoorwaardeVariabele { get; set; }
    public string? VoorwaardeWaarde { get; set; }
    public List<PlanningSjabloonBewerkTaak> Taken { get; set; } = new();
}

public sealed class PlanningSjabloonBewerkTaak
{
    public string? Code { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public int Duur { get; set; } = 1;
    public bool Estafette { get; set; }
    public int EstafetteWacht { get; set; }
    /// <summary>Bv. "laag:eerste,kelder:nee"; leeg = altijd. Zie <c>PlanningSjabloonUitrol.TaakVoorwaarden</c>.</summary>
    public string? Voorwaarde { get; set; }
    /// <summary>De bouwstaat die de taak oplevert (migratie 158).</summary>
    public int? BouwstaatId { get; set; }
    /// <summary>De activiteit (discipline) van de taak (migratie 158).</summary>
    public int? ActivityId { get; set; }
    public List<PlanningSjabloonBewerkVoorganger> Voorgangers { get; set; } = new();
}

public sealed class PlanningSjabloonBewerkVoorganger
{
    /// <summary>Code van een taak of blok in hetzelfde sjabloon.</summary>
    public string Code { get; set; } = "";

    /// <summary>"FS", "SS", "FF" of "SF".</summary>
    public string Type { get; set; } = "FS";
    public int WachtDagen { get; set; }
}

public sealed class PlanningSjabloonBewerkUitkomst
{
    public bool Ok { get; set; }
    public int SjabloonId { get; set; }

    /// <summary>Alles wat de gebruiker moet corrigeren voor het sjabloon bewaard kan worden.</summary>
    public List<string> Fouten { get; set; } = new();
}

/// <summary>Een regel in de lijst van de beheerpagina.</summary>
public sealed class PlanningSjabloonBeheerRij
{
    /// <summary>Duur van de planning van één woning in werkdagen (bij herhaalde blokken met <see cref="Verdiepingen"/> verdiepingen).</summary>
    public int Duur { get; set; }

    /// <summary>Het aantal verdiepingen waarmee <see cref="Duur"/> berekend is; 0 als het sjabloon niets per verdieping herhaalt.</summary>
    public int Verdiepingen { get; set; }
    public string? ReageertOp { get; set; }
    public string ReageertOpTekst { get; set; } = "";

    /// <summary>Projecten waar het sjabloon al gebruikt is (maximaal een paar namen) en het totaal aantal.</summary>
    public List<string> GebruiktIn { get; set; } = new();
    public int GebruiktAantal { get; set; }
    public DateTime? GewijzigdOp { get; set; }
    public string? GewijzigdDoor { get; set; }
    public int Id { get; set; }
    public string Naam { get; set; } = "";
    public string? Beschrijving { get; set; }
    public bool IsActief { get; set; }
    public int AantalBlokken { get; set; }
    public int AantalTaken { get; set; }
    public bool HeeftVerdiepingen { get; set; }
    public bool HeeftKelder { get; set; }
    public bool HeeftDak { get; set; }
    public bool HeeftTerras { get; set; }
    public bool HeeftGevel { get; set; }
    public bool HeeftEstafette { get; set; }
}

/// <summary>De waarden van "Reageert op": wanneer een sjabloon later automatisch voorgesteld wordt. Nu enkel bewaard en getoond.</summary>
public static class PlanningSjabloonReageertOp
{
    public static readonly (string Sleutel, string Tekst)[] Opties =
    {
        ("", "Niets — handmatig toepassen"),
        ("traject-vergunning", "Traject: bouwvergunning definitief"),
        ("traject-start", "Traject: start der werken"),
        ("type-woning", "Nieuw project van type Woning"),
        ("type-verkaveling", "Nieuw project van type Verkaveling"),
    };

    public static string Tekst(string? sleutel) => Opties.FirstOrDefault(o => o.Sleutel == (sleutel ?? "")).Tekst ?? Opties[0].Tekst;
    public static bool Geldig(string? sleutel) => Opties.Any(o => o.Sleutel == (sleutel ?? ""));
}

/// <summary>Verzoek om een proefdraai van het sjabloon dat in de editor staat (nog niet bewaard).</summary>
public sealed class PlanningSjabloonProefVerzoek
{
    public PlanningSjabloonBewerk Sjabloon { get; set; } = new();
    public int Woningen { get; set; } = 1;
    public int Verdiepingen { get; set; } = 2;

    /// <summary>Proefdraaien met of zonder kelder: taken "alleen met kelder" en "op de grond" wisselen mee.</summary>
    public bool Kelder { get; set; } = true;
}

public sealed class PlanningSjabloonProefBlok
{
    public string Code { get; set; } = "";
    public string Naam { get; set; } = "";

    /// <summary>Eerste werkdag (1 = projectstart) en laatste werkdag van het blok in woning 1.</summary>
    public int Start { get; set; }
    public int Einde { get; set; }
    public int Duur { get; set; }

    /// <summary>Bij een blok met een deelfase per bouwlaag: per bouwlaag het verloop (en "Eenmalig" voor taken zonder bouwlaag), in woning 1.</summary>
    public List<PlanningSjabloonProefDeel> Delen { get; set; } = new();
}

public sealed class PlanningSjabloonProefDeel
{
    public string Naam { get; set; } = "";
    public int Start { get; set; }
    public int Einde { get; set; }
    public int Duur { get; set; }
}

public sealed class PlanningSjabloonProefWoning
{
    public int Start { get; set; }
    public int Einde { get; set; }
}

public sealed class PlanningSjabloonProefTaak
{
    public string Code { get; set; } = "";
    public int Start { get; set; }
    public bool Kritiek { get; set; }
}

public sealed class PlanningSjabloonProefMelding
{
    /// <summary>"rood" blokkeert het opslaan, "goud" is een aandachtspunt.</summary>
    public string Ernst { get; set; } = "goud";
    public string Titel { get; set; } = "";
    public string Tekst { get; set; } = "";
}

public sealed class PlanningSjabloonProefUitkomst
{
    /// <summary>Duur van één woning in werkdagen, en voor alle gekozen woningen samen met de estafette.</summary>
    public int Werkdagen { get; set; }
    public int WerkdagenAlle { get; set; }
    public int Maanden { get; set; }
    public List<PlanningSjabloonProefBlok> Blokken { get; set; } = new();
    public List<PlanningSjabloonProefWoning> Woningen { get; set; } = new();
    public List<PlanningSjabloonProefTaak> Taken { get; set; } = new();
    public List<string> Kritiek { get; set; } = new();
    public List<PlanningSjabloonProefMelding> Meldingen { get; set; } = new();
}
