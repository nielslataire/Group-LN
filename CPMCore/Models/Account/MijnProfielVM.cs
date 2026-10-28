namespace CPMCore.Models.Account;

/// <summary>Account › Mijn profiel (gl-v2): vier tabbladen, één Opslaan. Velden onder "Toon" zijn enkel weergave en worden niet teruggepost.</summary>
public class MijnProfielVM
{
    /// <summary>profiel, handtekening, meldingen of beveiliging.</summary>
    public string Tab { get; set; } = "profiel";

    // Profiel
    public string? Voornaam { get; set; }
    public string? Familienaam { get; set; }
    public string? Functie { get; set; }
    public string? Gsm { get; set; }
    public string? VastNummer { get; set; }
    /// <summary>Bitmask: ma=1 … zo=64.</summary>
    public int WerkDagen { get; set; } = 31;
    public string WerkVan { get; set; } = "08:00";
    public string WerkTot { get; set; } = "17:00";
    public string? PauzeVan { get; set; } = "12:00";
    public string? PauzeTot { get; set; } = "12:45";
    public bool AfwezigAan { get; set; }
    public DateTime? AfwezigVan { get; set; }
    public DateTime? AfwezigTot { get; set; }
    public int? BackupUserId { get; set; }
    public int AfwezigOpties { get; set; } = 3;

    // Meldingen
    public string? MeldingenJson { get; set; }
    public bool DagSamenvatting { get; set; } = true;
    public string SamenvattingTijd { get; set; } = "07:30";
    public bool StilleUren { get; set; } = true;
    public bool DringendDoorlaten { get; set; } = true;
    public string DeadlineHerinnering { get; set; } = "3d";

    // Handtekening
    public string? SignatureHtml { get; set; }
    /// <summary>"Visual" (laatst bewerkt via Quill) of "Html" (geplakt als ruwe HTML-bron).</summary>
    public string Format { get; set; } = "Visual";

    // Toon (niet gepost)
    public string Email { get; set; } = "";
    public string RolNaam { get; set; } = "";
    public int AantalProjecten { get; set; }
    public string AanmeldMethode { get; set; } = "Microsoft";
    public List<(int Id, string Naam)> Collegas { get; set; } = new();
    /// <summary>Weergave van de Outlook-koppeling (alleen lezen; aan/uit gaat via eigen acties, niet via Opslaan).</summary>
    public CPMCore.Services.OutlookSync.OutlookSyncWeergave? Outlook { get; set; }
}
