using BOCore;
using CPMCore.Services.Contacten;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.OutlookSync;

/// <summary>
/// De acties van de gebruiker op een mail in de Inbox (spec #2, ticket #12): (her)koppelen aan een project, er een taak van maken, de afzender blijvend negeren
/// en een onbekende afzender als contact toevoegen. Elke actie werkt enkel op een mail van de gebruiker zelf. CPM doet dit nooit uit zichzelf; ook contacten
/// worden nooit automatisch aangemaakt.
/// </summary>
public class OutlookMailActies
{
    private readonly cpmRunningContext _db;
    private readonly OutlookMailSyncService _sync;
    private readonly CrmContactService _crm;

    public OutlookMailActies(cpmRunningContext db, OutlookMailSyncService sync, CrmContactService crm)
    {
        _db = db;
        _sync = sync;
        _crm = crm;
    }

    private Task<OutlookMail?> EigenMail(int userId, int mailId, CancellationToken ct) => _db.OutlookMail.FirstOrDefaultAsync(m => m.Id == mailId && m.UserId == userId, ct);

    /// <summary>Koppelt de mail aan één project (ook om een foute koppeling te corrigeren of een voorstel te bevestigen). De hele conversatie volgt mee: ook mails die CPM eerder zelf aan een
    /// ander project koppelde, anders zouden nieuwe antwoorden de foute koppeling blijven erven.</summary>
    public async Task<string?> KoppelAsync(int userId, int mailId, int projectId, IReadOnlyCollection<int> toegestaneProjecten, CancellationToken ct = default)
    {
        var mail = await EigenMail(userId, mailId, ct);
        if (mail == null) return "Die mail werd niet gevonden.";
        if (!toegestaneProjecten.Contains(projectId)) return "Dat project is niet beschikbaar.";

        mail.ProjectId = projectId;
        mail.VoorgesteldProjectId = null;
        if (mail.Status == OutlookMailStatus.TeKoppelen) mail.Status = OutlookMailStatus.Nieuw;

        if (mail.ConversationId != null)
        {
            var thread = await _db.OutlookMail.Where(m => m.UserId == userId && m.ConversationId == mail.ConversationId && m.Id != mail.Id).ToListAsync(ct);
            foreach (var m in thread)
            {
                m.ProjectId = projectId; m.VoorgesteldProjectId = null;
                if (m.Status == OutlookMailStatus.TeKoppelen) m.Status = OutlookMailStatus.Nieuw;
            }
        }
        await _db.SaveChangesAsync(ct);
        return null;
    }

    /// <summary>Maakt van de mail een item in de Inbox-notities (met het project van de mail) en zet de mail op afgehandeld: een mail wordt pas werk wanneer de gebruiker dat kiest.</summary>
    public async Task<(int? TaakId, string? Fout)> MaakTaakAsync(int userId, string userStr, int mailId, IReadOnlyCollection<int> toegestaneProjecten, CancellationToken ct = default)
    {
        var mail = await EigenMail(userId, mailId, ct);
        if (mail == null) return (null, "Die mail werd niet gevonden.");
        if (mail.Status == OutlookMailStatus.Afgehandeld) return (null, "Deze mail is al afgehandeld.");   // geen dubbele taak bij een dubbelklik

        var titel = string.IsNullOrWhiteSpace(mail.Onderwerp) ? "Mail beantwoorden" : mail.Onderwerp.Trim();
        var wie = string.IsNullOrWhiteSpace(mail.AfzenderNaam) ? mail.Afzender : mail.AfzenderNaam;
        var taak = new ProjectTaak
        {
            Titel = titel.Length <= 200 ? titel : titel[..197] + "…", ProjectId = mail.ProjectId != null && toegestaneProjecten.Contains(mail.ProjectId.Value) ? mail.ProjectId : null, Status = (int)TaakStatus.Open, Prioriteit = (int)TaakPrioriteit.Normaal,
            ToegewezenAanUserId = userStr, Herkomst = (int)TaakHerkomst.Handmatig, IsInbox = true, InboxBron = "Mail", InboxMeta = string.IsNullOrWhiteSpace(wie) ? "uit Outlook" : "van " + wie,
            CreatedByUserId = userStr, CreatedDate = DateTime.UtcNow
        };
        _db.ProjectTaak.Add(taak);
        mail.Status = OutlookMailStatus.Afgehandeld;
        await _db.SaveChangesAsync(ct);
        return (taak.Id, null);
    }

    /// <summary>Negeert de afzender blijvend: nieuwe mails van dat adres komen niet meer binnen en de openstaande mails van die afzender verdwijnen uit de Inbox (ze blijven op hun project).</summary>
    public async Task<string?> NegeerAfzenderAsync(int userId, int mailId, CancellationToken ct = default)
    {
        var mail = await EigenMail(userId, mailId, ct);
        if (mail == null) return "Die mail werd niet gevonden.";
        if (mail.Richting != "in") return "Je kan enkel de afzender van een ontvangen mail negeren.";   // anders blokkeer je je eigen adres en verdwijnen je verzonden mails
        var adres = mail.Afzender?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(adres)) return "Van deze mail is geen afzender bekend.";

        if (!await _db.OutlookGeblokkeerdeAfzender.AnyAsync(b => b.UserId == userId && b.Email == adres, ct))
            _db.OutlookGeblokkeerdeAfzender.Add(new OutlookGeblokkeerdeAfzender { UserId = userId, Email = adres, AangemaaktOp = DateTime.UtcNow });

        var open = await _db.OutlookMail.Where(m => m.UserId == userId && m.Richting == "in" && m.Afzender == mail.Afzender && m.Status != OutlookMailStatus.Afgehandeld).ToListAsync(ct);
        foreach (var m in open) m.Status = OutlookMailStatus.Afgehandeld;
        await _db.SaveChangesAsync(ct);
        return null;
    }

    /// <summary>Voegt de afzender op uitdrukkelijk verzoek toe als contact van het project van de mail. Een adres dat CPM al kent, wordt geweigerd. Zonder toestemmingen aangevinkt: de persoon gaf die niet.</summary>
    public async Task<string?> VoegContactToeAsync(int userId, int mailId, string gebruikersnaam, IReadOnlyCollection<int> toegestaneProjecten, CancellationToken ct = default)
    {
        var mail = await EigenMail(userId, mailId, ct);
        if (mail == null) return "Die mail werd niet gevonden.";
        if (mail.ProjectId == null) return "Koppel de mail eerst aan een project: een contact hoort bij een project.";
        if (!toegestaneProjecten.Contains(mail.ProjectId.Value)) return "Dat project is niet beschikbaar.";
        var adres = mail.Afzender?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(adres)) return "Van deze mail is geen afzender bekend.";

        var context = await _sync.BouwContextAsync(ct);
        if (context.Contacten.ContainsKey(adres)) return "Dit e-mailadres is al bekend in CPM.";
        if (OutlookMailRegels.Domein(adres) is { } domein && context.InterneDomeinen.Contains(domein)) return "Dit is een collega en geen extern contact.";

        var (voornaam, naam) = SplitsNaam(mail.AfzenderNaam, adres);
        await _crm.CreateAsync(mail.ProjectId.Value, new CrmContactService.ContactInput
        {
            FirstName = voornaam, LastName = naam, Email = adres, Source = "E-mail", SeenVia = "Mail",
            ConsentContact = false, ConsentNewsletter = false,
            FirstNote = string.IsNullOrWhiteSpace(mail.Onderwerp) ? "Toegevoegd vanuit een mail." : "Toegevoegd vanuit de mail \"" + mail.Onderwerp.Trim() + "\"."
        }, gebruikersnaam, ct);
        return null;
    }

    /// <summary>"Jan Peeters" wordt voornaam Jan en naam Peeters; één woord of enkel een adres wordt de naam.</summary>
    public static (string? Voornaam, string? Naam) SplitsNaam(string? weergavenaam, string adres)
    {
        var tekst = string.IsNullOrWhiteSpace(weergavenaam) || weergavenaam.Contains('@') ? "" : weergavenaam.Trim();
        if (tekst.Length == 0) return (null, adres.Split('@')[0]);
        var delen = tekst.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return delen.Length == 1 ? (null, delen[0]) : (delen[0], delen[1]);
    }
}
