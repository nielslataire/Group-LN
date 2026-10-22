using System.ComponentModel.DataAnnotations;
using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Contacten;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.Contacten;

/// <summary>Contacten van een project met opvolging (design-handoff punt 44, migratie 105). Importeert de oude contactaanvragen (website-formulieren,
/// interne acties, statusupdates uit ContactRequests) en de nieuwe formulieren van de website als contact + tijdlijn, en doet alle wijzigingen.</summary>
public class CrmContactService
{
    private const string InternalAction = "Interne actie";
    private const string StatusUpdate = "Status update";
    private readonly cpmRunningContext _db;

    public CrmContactService(cpmRunningContext db) { _db = db; }

    private static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string NormEmail(string? s) => (s ?? "").Trim().ToLowerInvariant();
    private static string NormPhone(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

    // ───────────────────────── import van de oude aanvragen en nieuwe website-formulieren ─────────────────────────
    /// <summary>Verwerkt elke rij uit ContactRequests die nog niet is overgenomen. Elke aanroep is goedkoop zodra alles is overgenomen.</summary>
    public async Task SyncLegacyAsync(int projectId, CancellationToken ct = default)
    {
        var done = _db.CrmContactEvent.Where(e => e.LegacyRequestId != null && e.Contact.ProjectId == projectId).Select(e => e.LegacyRequestId!.Value);
        var skipped = _db.CrmContactLegacySkip.Where(x => x.ProjectId == projectId).Select(x => x.LegacyRequestId);
        var reqs = await _db.ContactRequests.AsNoTracking()
            .Include(r => r.Unit).Where(r => r.ProjectId == projectId && !done.Contains(r.Id) && !skipped.Contains(r.Id)).OrderBy(r => r.CreatedAt).ThenBy(r => r.Id).ToListAsync(ct);
        if (reqs.Count == 0) return;

        var contacts = await _db.CrmContact.Include(c => c.Units).Where(c => c.ProjectId == projectId).ToListAsync(ct);
        CrmContact? Find(string? email, string? phone)
        {
            var e = NormEmail(email); var p = NormPhone(phone);
            if (e != "") { var c = contacts.FirstOrDefault(x => NormEmail(x.Email) == e); if (c != null) return c; }
            if (p.Length >= 6) return contacts.FirstOrDefault(x => NormPhone(x.Phone) == p);
            return null;
        }
        static (string? first, string? last) Split(ContactRequests r)
        {
            if (!string.IsNullOrWhiteSpace(r.Firstname) || !string.IsNullOrWhiteSpace(r.Lastname)) return (Norm(r.Firstname), Norm(r.Lastname));
            var parts = (r.Fullname ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? (null, null) : parts.Length == 1 ? (null, parts[0]) : (parts[0], parts[1]);
        }
        static DateTime Utc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();

        foreach (var r in reqs)
        {
            var intern = string.Equals(r.RequestType, InternalAction, StringComparison.OrdinalIgnoreCase);
            var status = string.Equals(r.RequestType, StatusUpdate, StringComparison.OrdinalIgnoreCase);
            var c = Find(r.Email, r.Phone);
            if (c == null)
            {
                var (first, last) = Split(r);
                c = new CrmContact
                {
                    ProjectId = projectId, FirstName = first, LastName = last, Email = Norm(r.Email), Phone = Norm(r.Phone),
                    Source = intern || status ? "Telefoon" : ContactenWeergave.Bron(r.SourceSite), Status = ContactenWeergave.Nieuw,
                    CreatedAt = Utc(r.CreatedAt), CreatedBy = Norm(r.Origin), ConsentContact = true, LastActivityAt = Utc(r.CreatedAt),
                };
                _db.CrmContact.Add(c); contacts.Add(c);
            }
            if (!intern && !status && r.UnitId.HasValue && !c.Units.Any(u => u.UnitId == r.UnitId.Value)) c.Units.Add(new CrmContactUnit { UnitId = r.UnitId.Value });
            if (string.IsNullOrWhiteSpace(c.Phone) && !string.IsNullOrWhiteSpace(r.Phone)) c.Phone = Norm(r.Phone);
            if (string.IsNullOrWhiteSpace(c.Email) && !string.IsNullOrWhiteSpace(r.Email)) c.Email = Norm(r.Email);

            var ev = new CrmContactEvent { Contact = c, OccurredAt = Utc(r.CreatedAt), LegacyRequestId = r.Id };
            if (intern)
            {
                var s = (r.Subject ?? "").ToLowerInvariant();
                ev.Kind = s.Contains("beld") ? "call" : s.Contains("mail") ? "mail" : "note"; ev.Title = Norm(r.Subject) ?? "Actie"; ev.Text = Norm(r.Question); ev.UserName = Norm(r.Origin);
                if (c.Status == ContactenWeergave.Nieuw) c.Status = ContactenWeergave.Contact;
            }
            else if (status)
            {
                var s = (r.Subject ?? "").ToLowerInvariant();
                // Actief en Zelf contact opgenomen worden Contact, Niet meer geïnteresseerd wordt Afgevallen
                if (s.Contains("niet meer")) { c.Status = ContactenWeergave.Afgevallen; c.StatusReason = Norm(r.Question); }
                else if (c.Status is ContactenWeergave.Nieuw or ContactenWeergave.Afgevallen) { c.Status = ContactenWeergave.Contact; c.StatusReason = null; }
                ev.Kind = "status"; ev.Title = "Status: " + (Norm(r.Subject) ?? "bijgewerkt"); ev.Text = Norm(r.Question); ev.UserName = Norm(r.Origin);
            }
            else
            {
                var fromUrl = (r.Origin ?? "").TrimStart().StartsWith("http", StringComparison.OrdinalIgnoreCase);   // de website bewaart de pagina waarvan het formulier kwam in Origin
                var website = ContactenWeergave.Bron(r.SourceSite) == "Website" && (string.IsNullOrWhiteSpace(r.Origin) || fromUrl);
                ev.Kind = website ? "form" : "note"; ev.IsAuto = website; ev.UserName = website && !fromUrl ? null : Norm(r.Origin);
                ev.Title = Norm(r.Subject) ?? (r.Unit != null ? $"Formulier \"Meer info {r.Unit.Name}\"" : "Contactaanvraag");
                ev.Text = Norm(r.Question);
                // iemand die afgevallen was en opnieuw schrijft, staat weer als nieuw
                if (c.Status == ContactenWeergave.Afgevallen) { c.Status = ContactenWeergave.Nieuw; c.StatusReason = null; }
            }
            var t = Utc(r.CreatedAt); if (c.LastActivityAt == null || c.LastActivityAt < t) c.LastActivityAt = t;
            _db.CrmContactEvent.Add(ev);
        }
        await _db.SaveChangesAsync(ct);
    }

    // ───────────────────────── lezen ─────────────────────────
    public async Task<List<KeyValuePair<int, string>>> UsersAsync(CancellationToken ct = default) =>
        (await _db.Users.AsNoTracking()
            // enkel interne medewerkers: geen aannemer- of portaalaccounts (UserCompanyAccess) en geen gasten of aannemers uit de uitnodigingen
            .Where(u => u.IsActive && !_db.UserCompanyAccess.Any(a => a.UserId == u.Id)
                && !_db.UserGuestInvitation.Any(i => i.UserId == u.Id && i.UserType != "internal"))
            .OrderBy(u => u.Voornaam).ThenBy(u => u.Familienaam)
            .Select(u => new { u.Id, u.Voornaam, u.Familienaam }).ToListAsync(ct))
        .Select(u => new KeyValuePair<int, string>(u.Id, (u.Voornaam + " " + u.Familienaam).Trim())).ToList();

    public async Task<List<KeyValuePair<int, string>>> UnitsAsync(int projectId, CancellationToken ct = default) =>
        (await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).Select(u => new { u.Id, u.Name }).ToListAsync(ct))
        .Select(u => new KeyValuePair<int, string>(u.Id, u.Name)).ToList();

    /// <summary>De makelaars (bedrijven) die aan het project gekoppeld zijn (tabblad Partners van Project bewerken).</summary>
    public async Task<List<KeyValuePair<int, string>>> BrokersAsync(int projectId, CancellationToken ct = default) =>
        (await _db.ProjectBroker.AsNoTracking().Where(b => b.ProjectId == projectId).Select(b => new { b.CompanyId, b.Company.BedrijfsNaam }).ToListAsync(ct))
        .OrderBy(b => b.BedrijfsNaam).Select(b => new KeyValuePair<int, string>(b.CompanyId, b.BedrijfsNaam)).ToList();

    /// <summary>Wie een contact kan opvolgen: de interne medewerkers (u + id) en de makelaars van het project (c + id).</summary>
    public async Task<List<AssigneeOption>> AssigneesAsync(int projectId, CancellationToken ct = default)
    {
        var list = (await UsersAsync(ct)).Select(u => new AssigneeOption("u" + u.Key, u.Value, "Medewerker")).ToList();
        list.AddRange((await BrokersAsync(projectId, ct)).Select(b => new AssigneeOption("c" + b.Key, b.Value, "Makelaar")));
        return list;
    }

    public async Task SaveProjectBrokersAsync(int projectId, IEnumerable<int> companyIds, CancellationToken ct = default)
    {
        var want = companyIds.Where(x => x > 0).ToHashSet();
        var have = await _db.ProjectBroker.Where(b => b.ProjectId == projectId).ToListAsync(ct);
        _db.ProjectBroker.RemoveRange(have.Where(b => !want.Contains(b.CompanyId)));
        foreach (var id in want.Where(id => !have.Any(b => b.CompanyId == id))) _db.ProjectBroker.Add(new ProjectBroker { ProjectId = projectId, CompanyId = id });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<CrmContactRow>> ListAsync(int projectId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var list = await _db.CrmContact.AsNoTracking().Include(c => c.Units).ThenInclude(u => u.Unit).Where(c => c.ProjectId == projectId).ToListAsync(ct);
        var ids = list.Select(c => c.Id).ToList();
        // laatste gebeurtenis per contact (geen "status"-berichten die enkel een wijziging melden tellen ook mee)
        var last = (await _db.CrmContactEvent.AsNoTracking().Where(e => ids.Contains(e.ContactId))
            .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id).Select(e => new { e.ContactId, e.Title, e.OccurredAt, e.UserName, e.IsAuto }).ToListAsync(ct))
            .GroupBy(e => e.ContactId).ToDictionary(g => g.Key, g => g.First());
        return list.Select(c =>
        {
            last.TryGetValue(c.Id, out var l);
            return new CrmContactRow
            {
                Id = c.Id, Name = ContactenWeergave.Name(c.FirstName, c.PartnerName, c.LastName), Email = c.Email, Phone = c.Phone, Source = c.Source, BrokerName = c.BrokerName, SeenVia = c.SeenVia,
                Status = c.Status, StatusReason = c.StatusReason, UnitNames = c.Units.Select(u => u.Unit?.Name ?? "").Where(n => n != "").OrderBy(n => n).ToList(), UnitIds = c.Units.Select(u => u.UnitId).ToList(),
                Budget = c.Budget, LastTitle = l?.Title, LastAt = l?.OccurredAt, LastBy = l?.UserName, LastAuto = l?.IsAuto ?? false,
                NextText = c.NextActionText, NextDate = c.NextActionDate, AssignedUserId = c.AssignedUserId, AssignedCompanyId = c.AssignedCompanyId, AssignedName = c.AssignedName, CreatedAt = c.CreatedAt,
                ConsentContact = c.ConsentContact, ConsentNewsletter = c.ConsentNewsletter, VisitAt = c.VisitAt,
                Overdue = c.NextActionDate.HasValue && c.NextActionDate.Value < today && ContactenWeergave.IsOpen(c.Status),
            };
        }).OrderBy(r => r.NextDate ?? DateOnly.MaxValue).ThenByDescending(r => r.CreatedAt).ToList();
    }

    public Task<CrmContact?> GetAsync(int projectId, int id, CancellationToken ct = default) =>
        _db.CrmContact.Include(c => c.Units).ThenInclude(u => u.Unit).Include(c => c.Events).FirstOrDefaultAsync(c => c.Id == id && c.ProjectId == projectId, ct);

    /// <summary>Bestaande contacten met dezelfde e-mail of hetzelfde telefoonnummer (dubbel-check bij toevoegen).</summary>
    public async Task<List<CrmContact>> FindDuplicatesAsync(int projectId, string? email, string? phone, int? excludeId = null, CancellationToken ct = default)
    {
        var e = NormEmail(email); var p = NormPhone(phone);
        if (e == "" && p.Length < 6) return new();
        var all = await _db.CrmContact.AsNoTracking().Where(c => c.ProjectId == projectId && (excludeId == null || c.Id != excludeId)).ToListAsync(ct);
        return all.Where(c => (e != "" && NormEmail(c.Email) == e) || (p.Length >= 6 && NormPhone(c.Phone) == p)).ToList();
    }

    // ───────────────────────── schrijven ─────────────────────────
    private CrmContactEvent Ev(CrmContact c, string kind, string? title, string? text, string? user, int? min = null, bool auto = false, DateTime? at = null)
    {
        var ev = new CrmContactEvent { Contact = c, Kind = kind, Title = title, Text = text, UserName = user, DurationMin = min, IsAuto = auto, OccurredAt = at ?? DateTime.UtcNow };
        _db.CrmContactEvent.Add(ev); c.LastActivityAt = ev.OccurredAt;
        return ev;
    }

    public async Task SetStatusAsync(int projectId, IEnumerable<int> ids, int status, string? reason, string user, CancellationToken ct = default)
    {
        var list = await _db.CrmContact.Where(c => c.ProjectId == projectId && ids.Contains(c.Id)).ToListAsync(ct);
        foreach (var c in list)
        {
            if (c.Status == status && string.IsNullOrWhiteSpace(reason)) continue;
            c.Status = status; c.StatusReason = status == ContactenWeergave.Afgevallen ? Norm(reason) : null;
            if (status != ContactenWeergave.Optie) { c.OptionUntil = null; c.OptionUnit = null; }
            if (status != ContactenWeergave.Bezichtiging) { c.VisitAt = null; c.VisitUnit = null; }
            Ev(c, "status", "Status: " + ContactenWeergave.Label(status), c.StatusReason, user);
        }
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Toewijzingssleutel: "u12" = medewerker 12, "c34" = makelaar (bedrijf) 34, leeg = niemand.</summary>
    public static (int? UserId, int? CompanyId) ParseAssignee(string? key)
    {
        key = (key ?? "").Trim();
        if (key.Length < 2 || !int.TryParse(key[1..], out var n) || n <= 0) return (null, null);
        return key[0] == 'u' ? (n, null) : key[0] == 'c' ? (null, n) : (null, null);
    }

    public async Task AssignAsync(int projectId, IEnumerable<int> ids, string? assignee, CancellationToken ct = default)
    {
        var list = await _db.CrmContact.Where(c => c.ProjectId == projectId && ids.Contains(c.Id)).ToListAsync(ct);
        foreach (var c in list) await ApplyAssign(c, assignee, ct);
        await _db.SaveChangesAsync(ct);
    }

    public class EventInput
    {
        public string Kind { get; set; } = "note";
        public string? Text { get; set; }
        public int? DurationMin { get; set; }
        public DateTime? WhenLocal { get; set; }      // bezichtiging: datum + uur
        public DateOnly? Until { get; set; }          // optie: tot wanneer
        public string? Unit { get; set; }             // bezichtiging/optie: voor welke eenheid
        public string? NextText { get; set; }
        public DateOnly? NextDate { get; set; }
        public int? NextUserId { get; set; }
    }

    public async Task<bool> AddEventAsync(int projectId, int id, EventInput i, string user, CancellationToken ct = default)
    {
        var c = await _db.CrmContact.FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);
        if (c == null) return false;
        var kind = i.Kind is "call" or "mail" or "visit" or "option" ? i.Kind : "note";
        string? title = kind switch { "call" => "Gesprek" + (i.DurationMin.HasValue ? " · " + i.DurationMin + " min" : ""), "mail" => "E-mail", "visit" => "Bezichtiging" + (string.IsNullOrWhiteSpace(i.Unit) ? "" : " · " + i.Unit), "option" => "Optie" + (string.IsNullOrWhiteSpace(i.Unit) ? "" : " · " + i.Unit), _ => null };
        DateTime? at = null;
        if (kind == "visit" && i.WhenLocal.HasValue)
        {
            c.VisitAt = DateTime.SpecifyKind(i.WhenLocal.Value, DateTimeKind.Local).ToUniversalTime(); c.VisitUnit = Norm(i.Unit);
            if (c.Status < ContactenWeergave.Bezichtiging) c.Status = ContactenWeergave.Bezichtiging;
            title = "Bezichtiging ingepland " + i.WhenLocal.Value.ToString("dd/MM HH:mm") + (string.IsNullOrWhiteSpace(i.Unit) ? "" : " · " + i.Unit);
        }
        else if (kind == "option")
        {
            c.OptionUntil = i.Until; c.OptionUnit = Norm(i.Unit); c.Status = ContactenWeergave.Optie;
            if (i.Until.HasValue) title = "Optie tot " + i.Until.Value.ToString("dd/MM/yyyy") + (string.IsNullOrWhiteSpace(i.Unit) ? "" : " · " + i.Unit);
        }
        else if (c.Status == ContactenWeergave.Nieuw && kind is "call" or "mail") c.Status = ContactenWeergave.Contact;
        Ev(c, kind, title, Norm(i.Text), user, i.DurationMin, false, at);
        if (!string.IsNullOrWhiteSpace(i.NextText) || i.NextDate.HasValue) await ApplyNext(c, i.NextText, i.NextDate, i.NextUserId, ct);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task ApplyNext(CrmContact c, string? text, DateOnly? date, int? userId, CancellationToken ct)
    {
        c.NextActionText = Norm(text); c.NextActionDate = date; c.NextActionUserId = userId;
        c.NextActionName = null;
        if (userId.HasValue) { var u = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.Voornaam, x.Familienaam }).FirstOrDefaultAsync(ct); c.NextActionName = u == null ? null : (u.Voornaam + " " + u.Familienaam).Trim(); }
    }

    public async Task<bool> SetNextActionAsync(int projectId, int id, string? text, DateOnly? date, int? userId, CancellationToken ct = default)
    {
        var c = await _db.CrmContact.FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);
        if (c == null) return false;
        await ApplyNext(c, text, date, userId, ct);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Een bezichtiging of optie verzetten of afsluiten: de gekozen datum vervangt de oude.</summary>
    public async Task<bool> RescheduleAsync(int projectId, int id, DateTime? visitLocal, DateOnly? optionUntil, string user, CancellationToken ct = default)
    {
        var c = await _db.CrmContact.FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);
        if (c == null) return false;
        if (visitLocal.HasValue) { c.VisitAt = DateTime.SpecifyKind(visitLocal.Value, DateTimeKind.Local).ToUniversalTime(); Ev(c, "visit", "Bezichtiging verzet naar " + visitLocal.Value.ToString("dd/MM HH:mm"), null, user); }
        if (optionUntil.HasValue) { c.OptionUntil = optionUntil; Ev(c, "option", "Optie verlengd tot " + optionUntil.Value.ToString("dd/MM/yyyy"), null, user); }
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public class ContactInput
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PartnerName { get; set; }
        [Display(Name = "E-mail"), EmailAddress] public string? Email { get; set; }
        [Display(Name = "Telefoon"), GlV2Phone] public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string Source { get; set; } = "Telefoon";
        public string? BrokerName { get; set; }
        public string? SeenVia { get; set; }
        public DateOnly? ContactDate { get; set; }
        /// <summary>"u12" (medewerker) of "c34" (makelaar), zie ParseAssignee.</summary>
        public string? Assignee { get; set; }
        public int? BrokerCompanyId { get; set; }
        public int? PostcodeId { get; set; }
        public List<int> UnitIds { get; set; } = new();
        public string? Budget { get; set; }
        public string? Financing { get; set; }
        public string? OwnHome { get; set; }
        public string? Wish { get; set; }
        public string? FirstNote { get; set; }
        public string? NextText { get; set; }
        public DateOnly? NextDate { get; set; }
        public bool ConsentContact { get; set; } = true;
        public bool ConsentNewsletter { get; set; }
    }

    public async Task<int> CreateAsync(int projectId, ContactInput i, string user, CancellationToken ct = default)
    {
        var c = new CrmContact
        {
            ProjectId = projectId, CreatedBy = user,
            CreatedAt = i.ContactDate.HasValue ? DateTime.SpecifyKind(i.ContactDate.Value.ToDateTime(TimeOnly.FromDateTime(DateTime.Now)), DateTimeKind.Local).ToUniversalTime() : DateTime.UtcNow,
        };
        Apply(c, i);
        foreach (var u in i.UnitIds.Distinct()) c.Units.Add(new CrmContactUnit { UnitId = u });
        if (i.ConsentContact || i.ConsentNewsletter) c.ConsentAt = DateTime.UtcNow;
        await ApplyLookups(c, i, ct);
        await ApplyAssign(c, i.Assignee, ct);
        c.NextActionText = Norm(i.NextText); c.NextActionDate = i.NextDate; c.NextActionUserId = c.AssignedUserId; c.NextActionName = c.AssignedUserId.HasValue ? c.AssignedName : null;
        _db.CrmContact.Add(c);
        Ev(c, "note", "Eerste contact · " + c.Source.ToLowerInvariant(), Norm(i.FirstNote), user, null, false, c.CreatedAt);
        if (!string.IsNullOrWhiteSpace(c.NextActionText) || c.NextActionDate.HasValue) { }
        await _db.SaveChangesAsync(ct);
        return c.Id;
    }

    private static void Apply(CrmContact c, ContactInput i)
    {
        c.FirstName = Norm(i.FirstName); c.LastName = Norm(i.LastName); c.PartnerName = Norm(i.PartnerName); c.Email = Norm(i.Email); c.Phone = Norm(i.Phone);
        c.Street = Norm(i.Street); c.PostalCode = Norm(i.PostalCode); c.City = Norm(i.City);
        c.Source = ContactenWeergave.Bronnen.Contains(i.Source) ? i.Source : "Telefoon";
        c.SeenVia = Norm(i.SeenVia);
        c.Budget = Norm(i.Budget); c.Financing = Norm(i.Financing); c.OwnHome = Norm(i.OwnHome); c.Wish = Norm(i.Wish);
        c.ConsentContact = i.ConsentContact; c.ConsentNewsletter = i.ConsentNewsletter;
    }

    /// <summary>Makelaar (bedrijf) en gemeente/postcode: de namen komen uit CompanyInfo en PostalCode, zodat het contact later naar een klant kan.</summary>
    private async Task ApplyLookups(CrmContact c, ContactInput i, CancellationToken ct)
    {
        c.BrokerCompanyId = null; c.BrokerName = null;
        if (c.Source == "Makelaar" && i.BrokerCompanyId.HasValue)
        {
            var name = await _db.CompanyInfo.AsNoTracking().Where(x => x.CompanyId == i.BrokerCompanyId).Select(x => x.BedrijfsNaam).FirstOrDefaultAsync(ct);
            if (name != null) { c.BrokerCompanyId = i.BrokerCompanyId; c.BrokerName = name; }
        }
        c.PostcodeId = null;
        if (i.PostcodeId.HasValue && i.PostcodeId > 0)
        {
            var pc = await _db.PostalCode.AsNoTracking().Where(x => x.PostcodeId == i.PostcodeId).Select(x => new { x.Postcode, x.Gemeente }).FirstOrDefaultAsync(ct);
            if (pc != null) { c.PostcodeId = i.PostcodeId; c.PostalCode = pc.Postcode; c.City = pc.Gemeente; }
        }
    }

    private async Task ApplyAssign(CrmContact c, string? assignee, CancellationToken ct)
    {
        c.AssignedUserId = null; c.AssignedCompanyId = null; c.AssignedName = null;
        var (userId, companyId) = ParseAssignee(assignee);
        if (userId.HasValue)
        {
            var u = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.Voornaam, x.Familienaam }).FirstOrDefaultAsync(ct);
            if (u != null) { c.AssignedUserId = userId; c.AssignedName = (u.Voornaam + " " + u.Familienaam).Trim(); }
        }
        else if (companyId.HasValue)
        {
            var ok = await _db.ProjectBroker.AsNoTracking().AnyAsync(x => x.ProjectId == c.ProjectId && x.CompanyId == companyId, ct);
            var name = ok ? await _db.CompanyInfo.AsNoTracking().Where(x => x.CompanyId == companyId).Select(x => x.BedrijfsNaam).FirstOrDefaultAsync(ct) : null;
            if (name != null) { c.AssignedCompanyId = companyId; c.AssignedName = name; }
        }
    }

    public async Task<bool> UpdateAsync(int projectId, int id, ContactInput i, CancellationToken ct = default)
    {
        var c = await _db.CrmContact.Include(x => x.Units).FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);
        if (c == null) return false;
        var wasConsent = c.ConsentContact || c.ConsentNewsletter;
        // "Datum contact": enkel de dag verandert, het uur blijft
        if (i.ContactDate.HasValue)
        {
            var local = ContactenWeergave.Local(c.CreatedAt);
            if (DateOnly.FromDateTime(local) != i.ContactDate.Value)
                c.CreatedAt = DateTime.SpecifyKind(i.ContactDate.Value.ToDateTime(TimeOnly.FromDateTime(local)), DateTimeKind.Local).ToUniversalTime();
        }
        Apply(c, i);
        if (!wasConsent && (i.ConsentContact || i.ConsentNewsletter)) c.ConsentAt = DateTime.UtcNow;
        await ApplyLookups(c, i, ct);
        await ApplyAssign(c, i.Assignee, ct);
        var keep = i.UnitIds.Distinct().ToHashSet();
        foreach (var u in c.Units.Where(u => !keep.Contains(u.UnitId)).ToList()) _db.CrmContactUnit.Remove(u);
        foreach (var u in keep.Where(u => !c.Units.Any(x => x.UnitId == u))) c.Units.Add(new CrmContactUnit { UnitId = u });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> DeleteAsync(int projectId, IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = await _db.CrmContact.Where(c => c.ProjectId == projectId && ids.Contains(c.Id)).ToListAsync(ct);
        // onthoud welke oude aanvragen bij deze contacten hoorden, anders neemt SyncLegacyAsync ze meteen weer over
        var listIds = list.Select(c => c.Id).ToList();
        var legacy = await _db.CrmContactEvent.Where(e => listIds.Contains(e.ContactId) && e.LegacyRequestId != null).Select(e => e.LegacyRequestId!.Value).Distinct().ToListAsync(ct);
        var known = await _db.CrmContactLegacySkip.Where(x => legacy.Contains(x.LegacyRequestId)).Select(x => x.LegacyRequestId).ToListAsync(ct);
        foreach (var id in legacy.Except(known)) _db.CrmContactLegacySkip.Add(new CrmContactLegacySkip { LegacyRequestId = id, ProjectId = projectId, DeletedAt = DateTime.UtcNow });
        _db.CrmContact.RemoveRange(list);
        await _db.SaveChangesAsync(ct);
        return list.Count;
    }

    /// <summary>Na het versturen van een mail via de bestaande mailknop: ook in de tijdlijn.</summary>
    public async Task LogMailAsync(int projectId, string? email, string templateName, string user, CancellationToken ct = default)
    {
        var e = NormEmail(email); if (e == "") return;
        var c = await _db.CrmContact.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Email != null && x.Email.ToLower() == e, ct);
        if (c == null) return;
        if (c.Status == ContactenWeergave.Nieuw) c.Status = ContactenWeergave.Contact;
        Ev(c, "mail", "E-mail · " + templateName, null, user);
        await _db.SaveChangesAsync(ct);
    }

    // ───────────────────────── opgeslagen lijsten en export (44d) ─────────────────────────
    public Task<List<CrmContactList>> ListsAsync(int projectId, CancellationToken ct = default) =>
        _db.CrmContactList.AsNoTracking().Where(l => l.ProjectId == projectId).OrderBy(l => l.Name).ToListAsync(ct);

    public async Task<int> SaveListAsync(int projectId, string name, string? filtersJson, string user, CancellationToken ct = default)
    {
        var l = new CrmContactList { ProjectId = projectId, Name = name.Trim(), FiltersJson = filtersJson, CreatedBy = user, CreatedAt = DateTime.UtcNow };
        _db.CrmContactList.Add(l);
        await _db.SaveChangesAsync(ct);
        return l.Id;
    }

    public async Task<bool> DeleteListAsync(int projectId, int id, CancellationToken ct = default)
    {
        var l = await _db.CrmContactList.FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);
        if (l == null) return false;
        _db.CrmContactList.Remove(l);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Contacten voor een export, mét tijdlijn. Wie geen toestemming heeft om gecontacteerd te worden, wordt niet meegenomen (en geteld); bij een nieuwsbriefexport telt enkel de toestemming voor de nieuwsbrief.</summary>
    public async Task<(List<CrmContact> Contacts, int Skipped)> ForExportAsync(int projectId, IEnumerable<int> ids, bool newsletterOnly = false, CancellationToken ct = default)
    {
        var all = await _db.CrmContact.AsNoTracking().Include(c => c.Units).ThenInclude(u => u.Unit).Include(c => c.Events).Where(c => c.ProjectId == projectId && ids.Contains(c.Id)).ToListAsync(ct);
        var ok = all.Where(c => newsletterOnly ? c.ConsentNewsletter : c.ConsentContact).OrderBy(c => c.CreatedAt).ThenBy(c => c.Id).ToList();   // op contactdatum, oudste eerst
        return (ok, all.Count - ok.Count);
    }

    public async Task LogExportAsync(int projectId, string user, string? listName, string format, int count, int skipped, CancellationToken ct = default)
    {
        _db.CrmExportLog.Add(new CrmExportLog { ProjectId = projectId, UserName = user, ListName = string.IsNullOrWhiteSpace(listName) ? null : listName, Format = format, ContactCount = count, Skipped = skipped, ExportedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }
}
