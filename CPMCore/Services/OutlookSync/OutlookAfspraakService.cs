using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.OutlookSync;

/// <summary>
/// Koppelt afspraken uit de Outlook-agenda aan projecten (spec #2, ticket #7). Het koppelrecord bevat alleen het event-id, de medewerker en het project; de details van de
/// afspraak blijven live uit Graph komen. CPM stelt een project voor (via een projectverwijzing in de titel of via deelnemers die bekende contactpersonen zijn), maar koppelt
/// nooit zelf: de gebruiker bevestigt altijd.
/// </summary>
public class OutlookAfspraakService
{
    private readonly cpmRunningContext _db;
    private readonly OutlookMailSyncService _context;
    private readonly IGraphIngang _graph;
    private readonly IAdresVoorstel? _adressen;

    public OutlookAfspraakService(cpmRunningContext db, OutlookMailSyncService context, IGraphIngang graph, IAdresVoorstel? adressen = null)
    {
        _adressen = adressen;
        _db = db;
        _context = context;
        _graph = graph;
    }

    private const int MaxUitnodigingen = 25;

    /// <summary>Een mogelijke deelnemer in de keuzelijst. <paramref name="Groep"/> is het kopje waaronder de keuze staat (collega's, project, contacten).</summary>
    public sealed record DeelnemerKeuze(string Email, string Naam, string Sub, string Groep);

    public const string GroepCollegas = "COLLEGA'S", GroepProject = "OP DIT PROJECT", GroepContacten = "CONTACTEN", GroepExtern = "EXTERN E-MAILADRES";

    /// <summary>Zoekt deelnemers voor een uitnodiging. De collega's (actieve gebruikers, jezelf niet) staan altijd bovenaan, ook zonder zoekterm; daaronder komen de mensen van het gekozen project
    /// (klanten van de eenheden en contactpersonen van de leveranciers met een contract), dan de andere contactpersonen van klanten en leveranciers (vanaf 2 tekens). Is de zoekterm zelf een geldig
    /// e-mailadres dat nog niet in de lijst staat, dan komt het als extern adres onderaan. Geen dubbele adressen.</summary>
    /// <param name="projectId">Het gekozen project; alleen als het in <paramref name="toegestaneProjecten"/> zit, anders genegeerd.</param>
    public async Task<List<DeelnemerKeuze>> ZoekDeelnemersAsync(int userId, string? term, int? projectId = null, IReadOnlyCollection<int>? toegestaneProjecten = null, CancellationToken ct = default)
    {
        term = (term ?? "").Trim();
        var t = term.ToLowerInvariant();
        var resultaat = new List<DeelnemerKeuze>();
        var gezien = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool Voeg(string? email, string? naam, string soort, string groep)
        {
            email = email?.Trim();
            if (string.IsNullOrEmpty(email) || !System.Net.Mail.MailAddress.TryCreate(email, out var a) || a.Address != email || !gezien.Add(email)) return false;
            resultaat.Add(new DeelnemerKeuze(email, string.IsNullOrWhiteSpace(naam) ? email : naam.Trim(), $"{soort} · {email}", groep));
            return true;
        }
        bool Past(string? naam, string? email) => t.Length == 0 || (naam ?? "").ToLowerInvariant().Contains(t) || (email ?? "").ToLowerInvariant().Contains(t);

        var collegas = await _db.Users.AsNoTracking().Where(u => u.IsActive && u.Email != null && u.Id != userId).Select(u => new { u.Voornaam, u.Familienaam, u.Email }).ToListAsync(ct);
        foreach (var u in collegas.Select(u => new { Naam = $"{u.Voornaam} {u.Familienaam}".Trim(), u.Email }).OrderBy(u => u.Naam, StringComparer.CurrentCultureIgnoreCase)
                     .Where(u => Past(u.Naam, u.Email)).Take(30))
            Voeg(u.Email, u.Naam, "Collega", GroepCollegas);

        if (projectId is int pid && toegestaneProjecten != null && toegestaneProjecten.Contains(pid))
        {
            var klantIds = await _db.Units.AsNoTracking().Where(u => u.ProjectId == pid && u.ClientAccountId != null).Select(u => u.ClientAccountId!.Value).Distinct().ToListAsync(ct);
            var klanten = await _db.ClientAccount.AsNoTracking().Where(k => klantIds.Contains(k.Id) && k.Email != null).OrderBy(k => k.Name).Select(k => new { k.Name, k.Email }).ToListAsync(ct);
            foreach (var k in klanten.Where(k => Past(k.Name, k.Email)).Take(10)) Voeg(k.Email, k.Name, "Klant", GroepProject);
            var klantContacten = await _db.ClientContacts.AsNoTracking().Where(c => klantIds.Contains(c.ClientAccountId) && c.Email != null).OrderBy(c => c.Name).Select(c => new { Naam = c.Name, c.Email }).ToListAsync(ct);
            foreach (var c in klantContacten.Where(c => Past(c.Naam, c.Email)).Take(10)) Voeg(c.Email, c.Naam, "Klant", GroepProject);
            var bedrijfIds = await _db.Contract.AsNoTracking().Where(c => c.ProjectId == pid).Select(c => c.CompanyId).Distinct().ToListAsync(ct);
            var leverancierContacten = await _db.CompanyContacts.AsNoTracking().Where(c => bedrijfIds.Contains(c.CompanyId) && c.Email != null).OrderBy(c => c.ContactNaam)
                .Select(c => new { Naam = c.ContactVoornaam + " " + c.ContactNaam, c.Email, c.Functie }).ToListAsync(ct);
            foreach (var c in leverancierContacten.Where(c => Past(c.Naam, c.Email)).Take(15)) Voeg(c.Email, c.Naam, string.IsNullOrWhiteSpace(c.Functie) ? "Contactpersoon" : c.Functie!, GroepProject);
        }

        if (t.Length >= 2)
        {
            var klantContacten = await _db.ClientContacts.AsNoTracking().Where(c => c.Email != null && ((c.Name ?? "").ToLower().Contains(t) || c.Email.ToLower().Contains(t)))
                .OrderBy(c => c.Name).Take(10).Select(c => new { Naam = c.Name, c.Email }).ToListAsync(ct);
            foreach (var c in klantContacten) Voeg(c.Email, c.Naam, "Klant", GroepContacten);
            var klanten = await _db.ClientAccount.AsNoTracking().Where(k => k.Email != null && (k.Name.ToLower().Contains(t) || k.Email.ToLower().Contains(t)))
                .OrderBy(k => k.Name).Take(10).Select(k => new { k.Name, k.Email }).ToListAsync(ct);
            foreach (var k in klanten) Voeg(k.Email, k.Name, "Klant", GroepContacten);
            var leveranciers = await _db.CompanyContacts.AsNoTracking().Where(c => c.Email != null && ((c.ContactVoornaam + " " + c.ContactNaam).ToLower().Contains(t) || c.Email.ToLower().Contains(t)))
                .OrderBy(c => c.ContactNaam).Take(10).Select(c => new { Naam = c.ContactVoornaam + " " + c.ContactNaam, c.Email }).ToListAsync(ct);
            foreach (var c in leveranciers) Voeg(c.Email, c.Naam, "Contactpersoon", GroepContacten);
            Voeg(term, null, "Extern e-mailadres", GroepExtern);   // een getypt adres dat nog niet in de lijst staat
        }
        return resultaat;
    }

    /// <summary>Een voorgestelde plaats voor een afspraak: <paramref name="Naam"/> (bv. het project of de klant), <paramref name="Adres"/> en de soort ("Recent", "Klant", "Leverancier", "Project").</summary>
    public sealed record PlaatsKeuze(string Naam, string Adres, string Soort);

    /// <summary>Plaatsen om voor te stellen bij "Ander adres". Zonder zoekterm: de plaatsen van je afspraken van de voorbije maand ("Recent"). Vanaf 2 tekens: adressen van projecten, klanten en leveranciers, en vanaf 3 tekens ook nieuwe adressen uit Google Places (label "Nieuw adres").</summary>
    public async Task<List<PlaatsKeuze>> ZoekPlaatsenAsync(string? agendaGebruiker, string? term, IReadOnlyCollection<int> toegestaneProjecten, CancellationToken ct = default)
    {
        var t = (term ?? "").Trim().ToLowerInvariant();
        var lijst = new List<PlaatsKeuze>();
        if (t.Length < 2)
        {
            if (!_graph.IsConfigured || string.IsNullOrWhiteSpace(agendaGebruiker)) return lijst;
            var vandaag = DateTime.Today;
            var agenda = await _graph.GetAgendaAsync(agendaGebruiker, vandaag.AddDays(-30), vandaag, ct);
            return agenda.Afspraken.Where(a => !string.IsNullOrWhiteSpace(a.Plaats) && !a.Online).OrderByDescending(a => a.Start)
                .Select(a => a.Plaats!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).Select(p => new PlaatsKeuze(p, "", "Recent")).ToList();
        }
        string Adres(string? straat, string? nr, string? postcode, string? gemeente) =>
            string.Join(", ", new[] { $"{straat} {nr}".Trim(), $"{postcode} {gemeente}".Trim() }.Where(x => x.Length > 0));

        var projecten = await _db.Project.AsNoTracking().Where(p => toegestaneProjecten.Contains(p.ProjectId) && ((p.ProjectName ?? "").ToLower().Contains(t) || (p.Street ?? "").ToLower().Contains(t)))
            .OrderBy(p => p.ProjectName).Take(4).Select(p => new { p.ProjectName, p.Street, p.Number, Postcode = p.PostalCodeId }).ToListAsync(ct);
        var codeIds = projecten.Select(p => p.Postcode).Where(i => i != null).Select(i => i!.Value).ToList();
        var codes = await _db.PostalCode.AsNoTracking().Where(c => codeIds.Contains(c.PostcodeId)).ToDictionaryAsync(c => c.PostcodeId, ct);
        foreach (var p in projecten)
        {
            codes.TryGetValue(p.Postcode ?? 0, out var pc);
            var adres = Adres(p.Street, p.Number, pc?.Postcode, pc?.Gemeente);
            if (adres.Length > 0) lijst.Add(new PlaatsKeuze(p.ProjectName ?? "", adres, "Project"));
        }
        var klanten = await _db.ClientAccount.AsNoTracking().Where(k => k.Street != null && (k.Name.ToLower().Contains(t) || k.Street.ToLower().Contains(t)))
            .OrderBy(k => k.Name).Take(4).Select(k => new { k.Name, k.Street, Nr = k.Housenumber, Pc = k.PostalCode != null ? k.PostalCode.Postcode : null, Gem = k.PostalCode != null ? k.PostalCode.Gemeente : null }).ToListAsync(ct);
        foreach (var k in klanten) lijst.Add(new PlaatsKeuze(k.Name, Adres(k.Street, k.Nr, k.Pc, k.Gem), "Klant"));
        var bedrijven = await _db.CompanyInfo.AsNoTracking().Where(c => c.Straat != null && (c.BedrijfsNaam.ToLower().Contains(t) || c.Straat.ToLower().Contains(t)))
            .OrderBy(c => c.BedrijfsNaam).Take(4).Select(c => new { c.BedrijfsNaam, c.Straat, c.Huisnummer, c.Postcode, c.Gemeente }).ToListAsync(ct);
        foreach (var c in bedrijven) lijst.Add(new PlaatsKeuze(c.BedrijfsNaam, Adres(c.Straat, c.Huisnummer, c.Postcode, c.Gemeente), "Leverancier"));
        lijst = lijst.Take(6).ToList();
        // Daaronder adressen die CPM nog niet kent (Google Places): zo kan je een volledig nieuw adres opzoeken in hetzelfde vak. Een adres dat al in de lijst staat, komt niet nog eens.
        if (_adressen != null)
            foreach (var a in (await _adressen.ZoekAsync(term ?? "", ct)).Take(5))
            {
                var volledig = a.Adres.Length > 0 ? a.Naam + ", " + a.Adres : a.Naam;
                if (!lijst.Any(k => k.Adres.Length > 0 && volledig.Contains(k.Adres.Split(',')[0], StringComparison.OrdinalIgnoreCase))) lijst.Add(new PlaatsKeuze(a.Naam, a.Adres, "Nieuw adres"));
            }
        return lijst;
    }

    /// <summary>Een bezet stuk van een dag, in minuten sinds middernacht, zodat het formulier kan tonen of een nieuwe afspraak overlapt.</summary>
    public sealed record BezetBlok(int Van, int Tot, string Titel, string? Plaats);

    /// <summary>Wanneer je op een dag al bezet bent. Hele dag en "vrij" tellen niet mee.</summary>
    public async Task<List<BezetBlok>> BezetOpAsync(string? agendaGebruiker, DateTime dag, CancellationToken ct = default)
    {
        if (!_graph.IsConfigured || string.IsNullOrWhiteSpace(agendaGebruiker)) return new();
        var van = dag.Date;
        var agenda = await _graph.GetAgendaAsync(agendaGebruiker, van, van.AddDays(1), ct);
        return agenda.Afspraken.Where(a => !a.HeleDag && a.ToonAls != "free")
            .Select(a => new BezetBlok((int)Math.Max(0, (a.Start - van).TotalMinutes), (int)Math.Min(1440, (a.Einde - van).TotalMinutes), a.Titel, a.Online ? null : a.Plaats))
            .Where(b => b.Tot > b.Van).OrderBy(b => b.Van).ToList();
    }

    /// <summary>De projecten waar je het laatst een afspraak aan koppelde (de recentste eerst), voor het kopje RECENT in de projectlijst.</summary>
    public async Task<List<int>> RecenteProjectenAsync(int userId, IReadOnlyCollection<int> toegestaneProjecten, int aantal = 3, CancellationToken ct = default)
    {
        var rijen = await _db.OutlookAfspraak.AsNoTracking().Where(a => a.UserId == userId && toegestaneProjecten.Contains(a.ProjectId))
            .OrderByDescending(a => a.GekoppeldOp).Select(a => a.ProjectId).Take(50).ToListAsync(ct);
        return rijen.Distinct().Take(aantal).ToList();
    }

    /// <summary>Maakt een afspraak in de Outlook-agenda van de gebruiker (ticket #8) en koppelt ze, als er een project is gekozen, meteen aan dat project. Zonder project is het een persoonlijke afspraak.
    /// Standaard gaat er geen uitnodiging uit: enkel de opgegeven e-mailadressen komen als deelnemers op de afspraak, waarna Outlook de uitnodigingen stuurt. Later wijzigen in CPM
    /// (<c>MijnTakenAssistent.AgendaWijzig</c>) of in Outlook werkt via het onthouden event-id in beide richtingen door; de agenda zelf blijft de bron.</summary>
    /// <remarks>Mislukt alleen het koppelen, dan komt de afspraak toch met haar event-id terug, samen met een melding: de afspraak bestaat in Outlook en mag niet opnieuw gemaakt worden.</remarks>
    /// <param name="reistijdMinuten">Groter dan 0: een apart blok "Reistijd" (bezet) van zoveel minuten vóór het begin.</param>
    /// <param name="online">Voegt een Teams-vergadering toe.</param>
    /// <param name="herinneringMinuten">Minuten vóór het begin; 0 of negatief = geen herinnering.</param>
    public async Task<(string? EventId, string? Fout)> MaakAsync(int userId, string? agendaGebruiker, int? projectId, IReadOnlyCollection<int> toegestaneProjecten,
        string? titel, DateTime start, int minuten, string? plaats, bool online, string? deelnemers, int herinneringMinuten, string? notitie, int reistijdMinuten = 0, CancellationToken ct = default)
    {
        titel = (titel ?? "").Trim();
        if (titel.Length == 0) return (null, "Geef de afspraak een titel.");
        if (titel.Length > 200) return (null, "De titel is te lang.");
        if (minuten < 5 || minuten > 960) return (null, "Een afspraak duurt tussen 5 minuten en 16 uur.");
        if (projectId.HasValue && !toegestaneProjecten.Contains(projectId.Value)) return (null, "Kies een project uit de lijst, of kies geen project.");
        if (herinneringMinuten > 20160) return (null, "De herinnering kan hoogstens twee weken vooraf.");
        notitie = string.IsNullOrWhiteSpace(notitie) ? null : notitie.Trim();
        if (notitie?.Length > 4000) return (null, "De notitie is te lang.");

        var uitnodigingen = new List<string>();
        foreach (var a in (deelnemers ?? "").Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!System.Net.Mail.MailAddress.TryCreate(a, out var adres) || adres.Address != a) return (null, $"'{a}' is geen geldig e-mailadres.");
            if (!uitnodigingen.Contains(a, StringComparer.OrdinalIgnoreCase)) uitnodigingen.Add(a);
        }
        if (uitnodigingen.Count > MaxUitnodigingen) return (null, $"Je kan hoogstens {MaxUitnodigingen} mensen uitnodigen.");

        var (eventId, fout) = await _graph.MaakAfspraakAsync(agendaGebruiker, new NieuweAfspraak(titel, start, start.AddMinutes(minuten),
            string.IsNullOrWhiteSpace(plaats) ? null : plaats.Trim(), online, uitnodigingen, herinneringMinuten > 0 ? herinneringMinuten : null, notitie), ct);
        if (fout != null || string.IsNullOrEmpty(eventId)) return (null, fout ?? "De afspraak kon niet aangemaakt worden.");

        string? waarschuwing = null;
        if (reistijdMinuten > 0 && reistijdMinuten <= 600)
        {
            // Een eigen blok "Reistijd" vóór de afspraak, zodat je agenda die tijd als bezet toont. Mislukt het, dan bestaat de afspraak toch en komt er enkel een melding.
            var (blokId, blokFout) = await _graph.MaakAfspraakAsync(agendaGebruiker, new NieuweAfspraak(MijnTaken.MijnTakenAssistent.ReisBlokTitel(titel), start.AddMinutes(-reistijdMinuten), start, null, false, Array.Empty<string>(), null, null, true), ct);
            if (blokFout != null || string.IsNullOrEmpty(blokId)) waarschuwing = "De afspraak staat in Outlook, maar het blok reistijd kon niet gemaakt worden.";
            else
            {
                // Onthouden welk blok bij deze afspraak hoort (migratie 134), ook zonder project: zo gaat het blok mee bij verplaatsen en verwijderen, in CPM en in Outlook.
                _db.OutlookAfspraakReistijd.Add(new OutlookAfspraakReistijd { UserId = userId, EventId = eventId, BlokEventId = blokId, Minuten = reistijdMinuten, GemaaktOp = DateTime.UtcNow });
                await _db.SaveChangesAsync(ct);
            }
        }
        if (!projectId.HasValue) return (eventId, waarschuwing);
        var koppelFout = await KoppelAsync(userId, eventId, projectId.Value, toegestaneProjecten, ct);
        return koppelFout == null ? (eventId, waarschuwing) : (eventId, "De afspraak staat in Outlook, maar kon niet aan het project gekoppeld worden. Koppel ze via de afspraak.");
    }

    /// <summary>Verwijdert een afspraak uit de Outlook-agenda (ticket #16), samen met haar blok Reistijd, of omgekeerd als <paramref name="eventId"/> het blok is: wat gekoppeld is, gaat samen weg.
    /// Is de afspraak aan een project gekoppeld, dan verdwijnt ook dat koppelrecord. Mislukt het verwijderen van de afspraak zelf, dan blijft alles staan (ook de koppelingen). Bestaat iets al niet meer in Outlook, dan telt dat als gelukt.</summary>
    /// <returns>Een foutmelding, of null bij succes.</returns>
    public async Task<string?> VerwijderAsync(int userId, string? agendaGebruiker, string eventId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return "Deze afspraak is onbekend.";
        var link = await _db.OutlookAfspraakReistijd.FirstOrDefaultAsync(r => r.UserId == userId && (r.EventId == eventId || r.BlokEventId == eventId), ct);
        var hoofdId = link?.EventId ?? eventId;

        // Eerst de afspraak, dan het blok: mislukt de afspraak, dan blijft er niets half achter.
        var fout = await _graph.VerwijderAfspraakAsync(agendaGebruiker, hoofdId, ct);
        if (fout != null) return fout;
        string? blokFout = null;
        if (link != null) blokFout = await _graph.VerwijderAfspraakAsync(agendaGebruiker, link.BlokEventId, ct);

        _db.OutlookAfspraakReistijd.RemoveRange(_db.OutlookAfspraakReistijd.Where(r => r.UserId == userId && r.EventId == hoofdId));
        _db.OutlookAfspraak.RemoveRange(_db.OutlookAfspraak.Where(k => k.UserId == userId && k.EventId == hoofdId));
        await _db.SaveChangesAsync(ct);
        return blokFout == null ? null : "De afspraak is verwijderd, maar het blok Reistijd kon niet verwijderd worden. Verwijder het zelf in je agenda.";
    }

    /// <summary>Het blok Reistijd dat bij een afspraak hoort, of null.</summary>
    public async Task<OutlookAfspraakReistijd?> ReistijdAsync(int userId, string eventId, CancellationToken ct = default) =>
        await _db.OutlookAfspraakReistijd.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId && r.EventId == eventId, ct);

    /// <summary>De koppeling waarvan dit event het blok Reistijd is (dus <paramref name="blokEventId"/> is geen gewone afspraak), of null.</summary>
    public async Task<OutlookAfspraakReistijd?> ReistijdVanBlokAsync(int userId, string blokEventId, CancellationToken ct = default) =>
        await _db.OutlookAfspraakReistijd.FirstOrDefaultAsync(r => r.UserId == userId && r.BlokEventId == blokEventId, ct);

    /// <summary>Onthoudt de nieuwe lengte van een blok Reistijd na het vergroten of verkleinen.</summary>
    public async Task ZetReistijdMinutenAsync(OutlookAfspraakReistijd koppeling, int minuten, CancellationToken ct = default)
    {
        if (koppeling.Minuten == minuten) return;
        koppeling.Minuten = minuten;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Houdt de blokken Reistijd bij hun afspraak, ook als je de afspraak in Outlook verplaatst of hernoemt: het blok eindigt op het begin van de afspraak en draagt haar titel.
    /// Gebeurt bij het laden van Vandaag en Week, voor de afspraken die in <paramref name="afspraken"/> staan. Staat het blok niet in die lijst (de afspraak is naar een andere dag of week verhuisd),
    /// dan wordt het op basis van het onthouden id en de lengte verplaatst. Bestaat het blok niet meer, dan verdwijnt de koppeling. Gooit nooit.</summary>
    public async Task SynchroniseReistijdAsync(int userId, string? agendaGebruiker, IReadOnlyCollection<AgendaAfspraak> afspraken, CancellationToken ct = default)
    {
        try
        {
            var ids = afspraken.Select(a => a.Id).Where(i => i != "").ToList();
            if (ids.Count == 0) return;
            var koppelingen = await _db.OutlookAfspraakReistijd.Where(r => r.UserId == userId && ids.Contains(r.EventId)).ToListAsync(ct);
            foreach (var k in koppelingen)
            {
                var hoofd = afspraken.First(a => a.Id == k.EventId);
                var blok = afspraken.FirstOrDefault(a => a.Id == k.BlokEventId);
                var titel = MijnTaken.MijnTakenAssistent.ReisBlokTitel(hoofd.Titel);
                if (blok != null && blok.Einde == hoofd.Start && blok.Titel == titel) continue;
                var duur = blok != null ? blok.Einde - blok.Start : TimeSpan.FromMinutes(k.Minuten);
                var fout = await _graph.BijwerkenAfspraakAsync(agendaGebruiker, k.BlokEventId, titel, hoofd.Start - duur, hoofd.Start, ct);
                if (fout != null && fout.Contains("bestaat niet meer", StringComparison.OrdinalIgnoreCase)) { _db.OutlookAfspraakReistijd.Remove(k); await _db.SaveChangesAsync(ct); }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Het blok bijwerken is een gemak: een fout hier mag het tonen van de agenda nooit blokkeren.
        }
    }

    /// <summary>Vult per afspraak het gekoppelde project en, voor een niet-gekoppelde afspraak, het voorstel in. Overschrijft altijd alle velden, zodat een bewaarde (gecachte) afspraak nooit een oude koppeling toont.</summary>
    /// <param name="zichtbareProjecten">De projecten van de gebruiker (id → naam); alleen daaruit komen een label of voorstel.</param>
    public async Task VerrijkAsync(int userId, string? eigenEmail, IReadOnlyDictionary<int, string> zichtbareProjecten, IEnumerable<AgendaAfspraak> afspraken, CancellationToken ct = default)
    {
        var lijst = afspraken.Where(a => !string.IsNullOrEmpty(a.Id)).ToList();
        if (lijst.Count == 0) return;

        var ids = lijst.Select(a => a.Id).Distinct().ToList();
        var koppelingen = await _db.OutlookAfspraak.AsNoTracking().Where(k => k.UserId == userId && ids.Contains(k.EventId)).ToDictionaryAsync(k => k.EventId, k => k.ProjectId, ct);
        var reisLinks = await _db.OutlookAfspraakReistijd.AsNoTracking().Where(r => r.UserId == userId && (ids.Contains(r.EventId) || ids.Contains(r.BlokEventId))).ToListAsync(ct);

        MailContext? ctx = null;
        foreach (var a in lijst)
        {
            a.KoppelId = null; a.IsReistijd = false;
            var als = reisLinks.FirstOrDefault(r => r.EventId == a.Id);
            var alsBlok = als == null ? reisLinks.FirstOrDefault(r => r.BlokEventId == a.Id) : null;
            if (als != null) a.KoppelId = als.EventId;
            else if (alsBlok != null) { a.KoppelId = alsBlok.EventId; a.IsReistijd = true; }
            a.ProjectId = null; a.ProjectNaam = null; a.VoorstelProjectId = null; a.VoorstelProjectNaam = null;
            if (koppelingen.TryGetValue(a.Id, out var gekoppeld))
            {
                a.ProjectId = gekoppeld;
                a.ProjectNaam = zichtbareProjecten.TryGetValue(gekoppeld, out var naam) ? naam : null;
                continue;
            }
            if (a.HeleDag) continue;   // verlof en feestdagen horen nergens bij

            ctx ??= await _context.BouwContextAsync(ct);
            var voorstel = Voorstel(a, ctx, eigenEmail, zichtbareProjecten.Keys);
            if (voorstel != null && zichtbareProjecten.TryGetValue(voorstel.Value, out var voorstelNaam))
            {
                a.VoorstelProjectId = voorstel;
                a.VoorstelProjectNaam = voorstelNaam;
            }
        }
    }

    /// <summary>Het project dat bij de afspraak past, als er precies één kandidaat is: (1) één project in de titel (naam, code, straat + nummer of straat + gemeente), anders
    /// (2) het enige actieve project van de deelnemers die bekende contactpersonen zijn. Meerdere kandidaten geven geen voorstel: CPM gokt niet.</summary>
    public static int? Voorstel(AgendaAfspraak afspraak, MailContext ctx, string? eigenEmail, IEnumerable<int> zichtbareProjecten)
    {
        var zichtbaar = zichtbareProjecten.ToHashSet();
        var verwijzingen = OutlookMailRegels.ProjectenInOnderwerp(afspraak.Titel, ctx.Projecten.Where(p => zichtbaar.Contains(p.Id)));
        if (verwijzingen.Count == 1) return verwijzingen[0];
        if (verwijzingen.Count > 1) return null;

        var eigen = eigenEmail?.Trim().ToLowerInvariant();
        var actief = ctx.Projecten.Where(p => p.Actief && zichtbaar.Contains(p.Id)).Select(p => p.Id).ToHashSet();
        var kandidaten = afspraak.Deelnemers.Select(d => d.Trim().ToLowerInvariant()).Where(d => d.Length > 0 && d != eigen).Distinct()
            .SelectMany(d => ctx.Contacten.TryGetValue(d, out var partijen) ? partijen : new())
            .SelectMany(p => p.ProjectIds).Where(actief.Contains).Distinct().ToList();
        return kandidaten.Count == 1 ? kandidaten[0] : null;
    }

    /// <summary>Koppelt (of herkoppelt) een afspraak aan een project; de koppeling blijft onthouden.</summary>
    public async Task<string?> KoppelAsync(int userId, string eventId, int projectId, IReadOnlyCollection<int> toegestaneProjecten, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId) || eventId.Length > 400) return "Afspraak niet gevonden.";
        if (!toegestaneProjecten.Contains(projectId)) return "Dat project is niet beschikbaar.";
        try
        {
            await ZetKoppelingAsync(userId, eventId, projectId, ct);
        }
        catch (DbUpdateException)
        {
            // Twee gelijktijdige aanvragen (dubbelklik) maakten allebei een rij; de unieke index weigerde de tweede. Opnieuw lezen en bijwerken.
            _db.ChangeTracker.Clear();
            await ZetKoppelingAsync(userId, eventId, projectId, ct);
        }
        return null;
    }

    private async Task ZetKoppelingAsync(int userId, string eventId, int projectId, CancellationToken ct)
    {
        var rij = await _db.OutlookAfspraak.FirstOrDefaultAsync(k => k.UserId == userId && k.EventId == eventId, ct);
        if (rij == null) _db.OutlookAfspraak.Add(new OutlookAfspraak { UserId = userId, EventId = eventId, ProjectId = projectId, GekoppeldOp = DateTime.UtcNow });
        else { rij.ProjectId = projectId; rij.GekoppeldOp = DateTime.UtcNow; }
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Haalt de koppeling weg; de afspraak zelf blijft in Outlook ongemoeid.</summary>
    public async Task OntkoppelAsync(int userId, string eventId, CancellationToken ct = default)
    {
        var rij = await _db.OutlookAfspraak.FirstOrDefaultAsync(k => k.UserId == userId && k.EventId == eventId, ct);
        if (rij == null) return;
        _db.OutlookAfspraak.Remove(rij);
        await _db.SaveChangesAsync(ct);
    }
}
