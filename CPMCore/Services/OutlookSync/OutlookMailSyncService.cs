using System.Text.RegularExpressions;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.OutlookSync;

/// <summary>
/// De mailstap van de Outlook-sync (spec #2, ticket #5): haalt per opt-in gebruiker de nieuwe mails op via <see cref="IGraphIngang"/>, past de
/// in- en uitsluitregels toe (<see cref="OutlookMailRegels"/>) en bewaart alleen metadata en een voorvertoning van maximaal 300 tekens. Geen bijlagen, geen volledige tekst.
/// Enkel mails vanaf het moment van koppelen. Dedup op Graph-id en internet-message-id, ook voor mails die CPM zelf verstuurde.
/// Het opslaan gebeurt door de aanroeper (<see cref="OutlookSyncService"/>), samen met de deltatokens, zodat een mislukte ronde niets half bewaart.
/// </summary>
public class OutlookMailSyncService
{
    public const int VoorvertoningLengte = 300;
    /// <summary>Id van ProjectStatusType.Opgeleverd; een opgeleverd project telt niet als "actief project" van een contact.</summary>
    private const int StatusOpgeleverd = 1;
    private static readonly Regex Adressen = new(@"[^\s;,<>]+@[^\s;,<>]+", RegexOptions.Compiled);

    private readonly cpmRunningContext _db;
    private readonly IGraphIngang _graph;
    private readonly ILogger<OutlookMailSyncService> _logger;
    private MailContext? _context;

    public OutlookMailSyncService(cpmRunningContext db, IGraphIngang graph, ILogger<OutlookMailSyncService> logger)
    {
        _db = db;
        _graph = graph;
        _logger = logger;
    }

    /// <summary>Begin van een nieuwe ronde: de bekende partijen en projecten worden opnieuw ingelezen.</summary>
    public void NieuweRonde() => _context = null;

    /// <summary>Synchroniseert de mail van één gebruiker. Wijzigt <paramref name="rij"/> (deltatokens) en voegt mails toe aan de context, maar slaat zelf niets op.</summary>
    public async Task<(GraphToegang Toegang, string? Fout, int Nieuw)> SynchroniseerAsync(OutlookSyncStatus rij, string? gebruikerSleutel, DateTime nuUtc, CancellationToken ct = default)
    {
        var vanaf = DateTime.SpecifyKind(rij.AangezetOp ?? nuUtc, DateTimeKind.Utc);   // EF geeft Unspecified terug
        var resultaat = await _graph.HaalMailAsync(gebruikerSleutel, rij.DeltaTokensJson, vanaf, ct);
        if (resultaat.Toegang != GraphToegang.Ok) return (resultaat.Toegang, resultaat.Fout, 0);

        var ctx = _context ??= await BouwContextAsync(ct);
        var gebruiker = await LeesGebruikerAsync(rij.UserId, ct);

        var bestaandGraph = (await _db.OutlookMail.Where(m => m.UserId == rij.UserId).Select(m => m.GraphId).ToListAsync(ct)).ToHashSet();
        var bestaandInternet = (await _db.OutlookMail.Where(m => m.UserId == rij.UserId && m.InternetMessageId != null).Select(m => m.InternetMessageId!).ToListAsync(ct)).ToHashSet();

        var nieuw = 0;
        foreach (var mail in resultaat.Nieuw)
        {
            if (mail.OntvangenOpUtc < vanaf) continue;   // enkel mails vanaf het moment van koppelen
            var internetId = Kort(mail.InternetMessageId, 500);   // dedup op de waarde zoals ze bewaard wordt
            if (bestaandGraph.Contains(mail.Id) || (internetId != null && bestaandInternet.Contains(internetId))) continue;

            var beslissing = OutlookMailRegels.Beoordeel(mail, ctx, gebruiker);
            if (!beslissing.Opnemen) continue;

            _db.OutlookMail.Add(new OutlookMail
            {
                UserId = rij.UserId, Richting = mail.Richting, GraphId = mail.Id, InternetMessageId = internetId, ConversationId = Kort(mail.ConversationId, 200),
                Afzender = Kort(mail.VanAdres, 320), AfzenderNaam = Kort(mail.VanNaam, 200), Ontvangers = Kort(string.Join(";", mail.Ontvangers), 2000),
                Onderwerp = Kort(mail.Onderwerp, 500), OntvangenOp = mail.OntvangenOpUtc, Voorvertoning = Kort(mail.Voorvertoning?.Trim(), VoorvertoningLengte),
                WebLink = VeiligeLink(mail.WebLink), ProjectId = beslissing.ProjectId, KlantId = beslissing.KlantId, LeverancierId = beslissing.LeverancierId,
                Status = beslissing.Status, AangemaaktOp = nuUtc
            });
            bestaandGraph.Add(mail.Id);
            if (internetId != null) bestaandInternet.Add(internetId);
            nieuw++;
        }

        // Een mail die naar een andere map verhuisde staat als verwijderd in de oude map maar ook als nieuw in de nieuwe: dat is niet verwijderd.
        var teruggezien = resultaat.Nieuw.Select(n => n.Id).ToHashSet();
        var verwijderd = resultaat.VerwijderdeIds.Where(id => !teruggezien.Contains(id)).ToList();
        if (verwijderd.Count > 0)
        {
            var weg = await _db.OutlookMail.Where(m => m.UserId == rij.UserId && verwijderd.Contains(m.GraphId)).ToListAsync(ct);
            foreach (var m in weg) m.VerwijderdInOutlook = true;
        }

        rij.DeltaTokensJson = resultaat.NieuweTokensJson;
        return (GraphToegang.Ok, null, nieuw);
    }

    /// <summary>Enkel http(s)-links van redelijke lengte komen als href op de pagina; een afgekapte link zou stuk zijn, dus dan geen link.</summary>
    private static string? VeiligeLink(string? link) =>
        link is { Length: > 0 and <= 1000 } && Uri.TryCreate(link, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) ? link : null;

    private static string? Kort(string? tekst, int max) => tekst == null ? null : tekst.Length <= max ? tekst : tekst[..max];

    private async Task<MailGebruiker> LeesGebruikerAsync(int userId, CancellationToken ct)
    {
        var email = await _db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).FirstOrDefaultAsync(ct);
        var geblokkeerd = (await _db.OutlookGeblokkeerdeAfzender.AsNoTracking().Where(b => b.UserId == userId).Select(b => b.Email).ToListAsync(ct))
            .Select(e => e.Trim().ToLowerInvariant()).ToHashSet();
        return new MailGebruiker(userId, string.IsNullOrWhiteSpace(email) ? Array.Empty<string>() : new[] { email }, geblokkeerd);
    }

    private static IEnumerable<string> Splits(string? veld) =>
        string.IsNullOrWhiteSpace(veld) ? Enumerable.Empty<string>() : Adressen.Matches(veld).Select(m => m.Value.Trim().ToLowerInvariant());

    /// <summary>Bouwt de bekende partijen (klanten, leveranciers, contactpersonen, makelaars), de projecten en de interne domeinen op.</summary>
    internal async Task<MailContext> BouwContextAsync(CancellationToken ct)
    {
        var ctx = new MailContext();

        foreach (var e in await _db.Users.AsNoTracking().Where(u => u.IsActive && u.Email != null).Select(u => u.Email!).ToListAsync(ct))
            if (OutlookMailRegels.Domein(e) is { } d && !OutlookMailRegels.IsPubliekDomein(d)) ctx.InterneDomeinen.Add(d);

        foreach (var p in await _db.Project.AsNoTracking().Select(p => new { p.ProjectId, p.ProjectName, p.ProjectCode, p.Street, p.Number, p.StatusId }).ToListAsync(ct))
            ctx.Projecten.Add(new MailProject(p.ProjectId, p.ProjectName ?? "", p.ProjectCode, p.Street, p.Number, p.StatusId != StatusOpgeleverd));

        void Voeg(string? veld, BekendeContact partij)
        {
            foreach (var a in Splits(veld))
            {
                if (!ctx.Contacten.TryGetValue(a, out var lijst)) ctx.Contacten[a] = lijst = new();
                lijst.Add(partij);
            }
        }

        // Klanten: het klantenaccount en zijn contactpersonen en mede-eigenaars. Projecten via de eenheden van de klant.
        var klantProjecten = (await _db.Units.AsNoTracking().Where(u => u.ClientAccountId != null).Select(u => new { Klant = u.ClientAccountId!.Value, u.ProjectId }).Distinct().ToListAsync(ct))
            .GroupBy(x => x.Klant).ToDictionary(g => g.Key, g => (IReadOnlyCollection<int>)g.Select(x => x.ProjectId).ToList());
        IReadOnlyCollection<int> ProjectenVanKlant(int id) => klantProjecten.TryGetValue(id, out var l) ? l : Array.Empty<int>();
        foreach (var k in await _db.ClientAccount.AsNoTracking().Select(k => new { k.Id, k.Email, k.InvoiceEmail }).ToListAsync(ct))
        {
            var partij = new BekendeContact("klant", k.Id, null, ProjectenVanKlant(k.Id));
            Voeg(k.Email, partij); Voeg(k.InvoiceEmail, partij);
        }
        foreach (var c in await _db.ClientContacts.AsNoTracking().Select(c => new { c.ClientAccountId, c.Email, c.InvoiceEmail }).ToListAsync(ct))
        {
            var partij = new BekendeContact("klant", c.ClientAccountId, null, ProjectenVanKlant(c.ClientAccountId));
            Voeg(c.Email, partij); Voeg(c.InvoiceEmail, partij);
        }

        // Leveranciers en makelaars: projecten via hun contracten, makelaars ook via ProjectBroker.
        var contractProjecten = (await _db.Contract.AsNoTracking().Select(c => new { c.CompanyId, c.ProjectId }).Distinct().ToListAsync(ct))
            .GroupBy(x => x.CompanyId).ToDictionary(g => g.Key, g => g.Select(x => x.ProjectId).ToList());
        var makelaarProjecten = (await _db.ProjectBroker.AsNoTracking().Select(b => new { b.CompanyId, b.ProjectId }).ToListAsync(ct))
            .GroupBy(x => x.CompanyId).ToDictionary(g => g.Key, g => g.Select(x => x.ProjectId).ToList());
        BekendeContact PartijVanBedrijf(int companyId)
        {
            var projecten = (contractProjecten.TryGetValue(companyId, out var c) ? c : new List<int>())
                .Concat(makelaarProjecten.TryGetValue(companyId, out var m) ? m : new List<int>()).Distinct().ToList();
            return new BekendeContact(makelaarProjecten.ContainsKey(companyId) ? "makelaar" : "leverancier", null, companyId, projecten);
        }
        foreach (var b in await _db.CompanyInfo.AsNoTracking().Select(b => new { b.CompanyId, b.Email }).ToListAsync(ct))
            Voeg(b.Email, PartijVanBedrijf(b.CompanyId));
        foreach (var c in await _db.CompanyContacts.AsNoTracking().Select(c => new { c.CompanyId, c.Email }).ToListAsync(ct))
            Voeg(c.Email, PartijVanBedrijf(c.CompanyId));

        // Contacten uit het CRM (kandidaat-kopers): hangen aan één project.
        foreach (var c in await _db.CrmContact.AsNoTracking().Where(c => c.Email != null).Select(c => new { c.Email, c.ProjectId }).ToListAsync(ct))
            Voeg(c.Email, new BekendeContact("contact", null, null, new[] { c.ProjectId }));

        return ctx;
    }
}
