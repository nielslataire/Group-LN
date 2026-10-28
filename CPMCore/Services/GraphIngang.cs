using System.Collections.Concurrent;
using System.Globalization;
using Azure.Identity;
using FacadeCore;
using Microsoft.Graph;

namespace CPMCore.Services;

/// <summary>Een afspraak uit de Outlook-agenda (lokale tijd).</summary>
public sealed class AgendaAfspraak
{
    /// <summary>Graph-id van de afspraak (van dit voorkomen, bij een reeks).</summary>
    public string Id { get; set; } = "";
    /// <summary>Alleen de organisator kan de tijd van een vergadering wijzigen; een afspraak zonder deelnemers is altijd van jou.</summary>
    public bool IsOrganisator { get; set; } = true;
    public bool HeeftDeelnemers { get; set; }
    public string Titel { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime Einde { get; set; }
    public bool HeleDag { get; set; }
    public string? Plaats { get; set; }
    public bool Online { get; set; }
    public string? Link { get; set; }
    /// <summary>free, tentative, busy, oof of workingElsewhere.</summary>
    public string ToonAls { get; set; } = "busy";
}

public sealed class AgendaResultaat
{
    public bool Gekoppeld { get; set; }
    public string? Fout { get; set; }
    public List<AgendaAfspraak> Afspraken { get; set; } = new();
}

/// <summary>Uitkomst van <see cref="IGraphIngang.ControleerToegangAsync"/>.</summary>
public enum GraphToegang { Ok, GeenToegang, GeenMailbox, Fout }

/// <summary>
/// De ene ingang van CPM naar Microsoft Graph voor mail en agenda. De agenda-service van Mijn taken, het versturen van offertemail en de latere mailsync
/// delen deze afhankelijkheid; tests gebruiken een fake. Een gebruiker wordt aangeduid met <see cref="GebruikerSleutel"/>.
/// </summary>
public interface IGraphIngang
{
    bool IsConfigured { get; }

    /// <param name="gebruiker">Entra-object-id of e-mailadres (UPN) van de mailbox.</param>
    Task<AgendaResultaat> GetAgendaAsync(string? gebruiker, DateTime van, DateTime tot, CancellationToken ct = default);

    /// <summary>Controleert of de app bij de mailbox van deze gebruiker mag (bv. 403 als de gebruiker buiten de toegestane Exchange-groep valt). Gooit nooit; leest geen inhoud.</summary>
    Task<GraphToegang> ControleerToegangAsync(string? gebruiker, CancellationToken ct = default);

    /// <summary>Wijzigt titel en tijd van een afspraak. Geeft een foutmelding terug, of null bij succes.</summary>
    Task<string?> BijwerkenAfspraakAsync(string? gebruiker, string eventId, string? titel, DateTime start, DateTime einde, CancellationToken ct = default);

    /// <summary>True als Graph de mail aanvaardde; false (de aanroeper valt terug op SMTP) bij geen configuratie of een fout.</summary>
    Task<bool> TrySendMailAsync(string fromMailbox, string to, string subject, string html, IEnumerable<EmailAttachment>? attachments, CancellationToken ct = default);

    /// <summary>Hoe Graph een gebruiker vindt: de Entra-object-id, anders het e-mailadres. Null als geen van beide bekend is.</summary>
    static string? GebruikerSleutel(string? entraObjectId, string? email) =>
        !string.IsNullOrWhiteSpace(entraObjectId) ? entraObjectId : (string.IsNullOrWhiteSpace(email) ? null : email);
}

/// <summary>
/// Leest de Office 365-agenda van een gebruiker via Microsoft Graph (<c>/users/{id}/calendarView</c>), met dezelfde Entra-app als het inloggen
/// en de mail (<c>AzureAd:TenantId/ClientId/ClientSecret</c>). Vereist de Graph-toepassingsmachtiging <c>Calendars.Read</c> (lezen) en <c>Calendars.ReadWrite</c> (afspraken wijzigen, <see cref="BijwerkenAfspraakAsync"/>) met beheerderstoestemming.
/// Wees je ervan bewust: als toepassingsmachtiging leest ze in principe ELKE mailbox van de tenant; beperk de app in Exchange Online met een
/// New-ApplicationAccessPolicy tot een groep (zelfde beleid als voor Mail.Send). Tijden komen terug in de tijdzone van de server-Windowsinstelling
/// "Romance Standard Time" (Brussel). Resultaten worden 5 minuten onthouden per mailbox en periode, zodat Vandaag en Week de agenda niet bij elke klik opnieuw ophalen.
/// </summary>
public sealed class GraphIngang : IGraphIngang
{
    private const string Tijdzone = "Romance Standard Time";
    private static readonly TimeSpan Bewaartijd = TimeSpan.FromMinutes(5);
    private readonly GraphServiceClient? _client;
    private readonly ILogger<GraphIngang> _logger;
    private readonly ConcurrentDictionary<string, (DateTime Tot, AgendaResultaat Resultaat)> _cache = new();

    public GraphIngang(IConfiguration configuration, ILogger<GraphIngang> logger)
    {
        _logger = logger;
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogWarning("Graph niet beschikbaar: AzureAd:TenantId/ClientId/ClientSecret ontbreken.");
            return;
        }
        _client = new GraphServiceClient(new ClientSecretCredential(tenantId, clientId, clientSecret), new[] { "https://graph.microsoft.com/.default" });
    }

    public bool IsConfigured => _client != null;

    /// <param name="gebruiker">Entra-object-id of e-mailadres (UPN) van de mailbox.</param>
    public async Task<AgendaResultaat> GetAgendaAsync(string? gebruiker, DateTime van, DateTime tot, CancellationToken ct = default)
    {
        if (_client == null) return new AgendaResultaat { Gekoppeld = false, Fout = "De Outlook-agenda is niet gekoppeld: de Entra-instellingen ontbreken." };
        if (string.IsNullOrWhiteSpace(gebruiker)) return new AgendaResultaat { Gekoppeld = false, Fout = "Er is geen e-mailadres of Entra-account bij je gebruiker bekend." };

        var sleutel = $"{gebruiker}|{van:yyyyMMddHH}|{tot:yyyyMMddHH}";
        if (_cache.TryGetValue(sleutel, out var bewaard) && bewaard.Tot > DateTime.UtcNow) return bewaard.Resultaat;

        var resultaat = new AgendaResultaat { Gekoppeld = true };
        try
        {
            var opties = new List<QueryOption>
            {
                new("startDateTime", van.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)),
                new("endDateTime", tot.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture))
            };
            var pagina = await _client.Users[gebruiker].CalendarView.Request(opties)
                .Header("Prefer", $"outlook.timezone=\"{Tijdzone}\"")
                .Select("id,subject,start,end,isAllDay,isCancelled,location,showAs,isOnlineMeeting,webLink,isOrganizer,attendees")
                .Top(100).OrderBy("start/dateTime").GetAsync(ct);

            while (pagina != null)
            {
                foreach (var e in pagina)
                {
                    if (e.IsCancelled == true || e.Start?.DateTime == null || e.End?.DateTime == null) continue;
                    resultaat.Afspraken.Add(new AgendaAfspraak
                    {
                        Id = e.Id ?? "", IsOrganisator = e.IsOrganizer != false, HeeftDeelnemers = e.Attendees?.Any() == true,
                        Titel = string.IsNullOrWhiteSpace(e.Subject) ? "(zonder titel)" : e.Subject!,
                        Start = DateTime.Parse(e.Start.DateTime, CultureInfo.InvariantCulture),
                        Einde = DateTime.Parse(e.End.DateTime, CultureInfo.InvariantCulture),
                        HeleDag = e.IsAllDay == true,
                        Plaats = string.IsNullOrWhiteSpace(e.Location?.DisplayName) ? null : e.Location!.DisplayName,
                        Online = e.IsOnlineMeeting == true,
                        Link = e.WebLink,
                        ToonAls = e.ShowAs?.ToString()?.ToLowerInvariant() ?? "busy"
                    });
                }
                pagina = pagina.NextPageRequest != null ? await pagina.NextPageRequest.GetAsync(ct) : null;
            }
        }
        catch (ServiceException ex)
        {
            _logger.LogWarning(ex, "Outlook-agenda ophalen via Graph mislukt voor {Gebruiker} (status {Status}).", gebruiker, (int)ex.StatusCode);
            resultaat.Fout = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized =>
                    "De app mag je agenda niet lezen. Een beheerder moet de Graph-machtiging Calendars.Read (toepassing) toekennen en goedkeuren.",
                System.Net.HttpStatusCode.NotFound => "Voor je account is geen Outlook-mailbox gevonden.",
                _ => "De Outlook-agenda kon niet geladen worden. Probeer het later opnieuw."
            };
            resultaat.Afspraken.Clear();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Outlook-agenda ophalen mislukt voor {Gebruiker}.", gebruiker);
            resultaat.Fout = "De Outlook-agenda kon niet geladen worden. Probeer het later opnieuw.";
            resultaat.Afspraken.Clear();
        }

        // Fouten maar kort onthouden, zodat een herstelde machtiging snel opgemerkt wordt.
        _cache[sleutel] = (DateTime.UtcNow + (resultaat.Fout == null ? Bewaartijd : TimeSpan.FromMinutes(1)), resultaat);
        return resultaat;
    }

    public async Task<GraphToegang> ControleerToegangAsync(string? gebruiker, CancellationToken ct = default)
    {
        if (_client == null || string.IsNullOrWhiteSpace(gebruiker)) return GraphToegang.Fout;
        try
        {
            await _client.Users[gebruiker].Calendar.Request().Select("id").GetAsync(ct);
            return GraphToegang.Ok;
        }
        catch (ServiceException ex)
        {
            _logger.LogWarning(ex, "Toegang tot de mailbox van {Gebruiker} controleren mislukt (status {Status}).", gebruiker, (int)ex.StatusCode);
            return ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized => GraphToegang.GeenToegang,
                System.Net.HttpStatusCode.NotFound => GraphToegang.GeenMailbox,
                _ => GraphToegang.Fout
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Toegang tot de mailbox van {Gebruiker} controleren mislukt.", gebruiker);
            return GraphToegang.Fout;
        }
    }

    /// <summary>Wijzigt titel en tijd van een afspraak in de Outlook-agenda (PATCH op <c>/users/{id}/events/{eventId}</c>). Vereist
    /// <c>Calendars.ReadWrite</c> (toepassing). Bij een vergadering met deelnemers stuurt Outlook zelf een update uit. Geeft een foutmelding terug, of null bij succes.</summary>
    public async Task<string?> BijwerkenAfspraakAsync(string? gebruiker, string eventId, string? titel, DateTime start, DateTime einde, CancellationToken ct = default)
    {
        if (_client == null || string.IsNullOrWhiteSpace(gebruiker)) return "De Outlook-agenda is niet gekoppeld.";
        if (string.IsNullOrWhiteSpace(eventId)) return "Afspraak niet gevonden.";
        if (einde <= start) return "Het einde moet na het begin liggen.";
        try
        {
            var wijziging = new Event
            {
                Start = new DateTimeTimeZone { DateTime = start.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture), TimeZone = Tijdzone },
                End = new DateTimeTimeZone { DateTime = einde.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture), TimeZone = Tijdzone }
            };
            if (!string.IsNullOrWhiteSpace(titel)) wijziging.Subject = titel.Trim();
            await _client.Users[gebruiker].Events[eventId].Request().UpdateAsync(wijziging, ct);
            foreach (var k in _cache.Keys.Where(k => k.StartsWith(gebruiker + "|", StringComparison.Ordinal)).ToList()) _cache.TryRemove(k, out _);
            return null;
        }
        catch (ServiceException ex)
        {
            _logger.LogWarning(ex, "Outlook-afspraak wijzigen mislukt voor {Gebruiker} (status {Status}).", gebruiker, (int)ex.StatusCode);
            return ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized =>
                    "Je mag deze afspraak niet wijzigen. Enkel de organisator past een vergadering aan, en de app heeft de Graph-machtiging Calendars.ReadWrite nodig.",
                System.Net.HttpStatusCode.NotFound => "Deze afspraak bestaat niet meer in Outlook. Laad de pagina opnieuw.",
                _ => "De afspraak kon niet gewijzigd worden. Probeer het later opnieuw."
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Outlook-afspraak wijzigen mislukt voor {Gebruiker}.", gebruiker);
            return "De afspraak kon niet gewijzigd worden. Probeer het later opnieuw.";
        }
    }

    /// <summary>Verstuurt mail echt NAMENS een mailbox (<c>/users/{afzender}/sendMail</c>), zodat ze in "Verzonden items" van die gebruiker belandt. Vereist <c>Mail.Send</c> (toepassing)
    /// met hetzelfde Exchange-toegangsbeleid als de agenda. Eerste gebruik: de offertemail (ProjectenController.ChangeOrderQuoteSendV2).</summary>
    public async Task<bool> TrySendMailAsync(string fromMailbox, string to, string subject, string html, IEnumerable<EmailAttachment>? attachments, CancellationToken ct = default)
    {
        if (_client == null || string.IsNullOrWhiteSpace(fromMailbox)) return false;
        try
        {
            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = html },
                ToRecipients = new List<Recipient> { new() { EmailAddress = new EmailAddress { Address = to } } },
                Attachments = new MessageAttachmentsCollectionPage(),
            };
            foreach (var a in attachments ?? Array.Empty<EmailAttachment>())
                message.Attachments.Add(new FileAttachment { Name = a.FileName, ContentType = a.ContentType, ContentBytes = a.Content });
            await _client.Users[fromMailbox].SendMail(message, true).Request().PostAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mail versturen via Graph namens {Mailbox} mislukt; terugval op SMTP.", fromMailbox);
            return false;
        }
    }
}
