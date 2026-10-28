using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CPMCore.Services.OutlookSync;

public static class OutlookMailStatus
{
    public const string Nieuw = "nieuw", Afgehandeld = "afgehandeld", TeKoppelen = "te-koppelen";
}

/// <summary>Een bekende partij achter een e-mailadres: klant, leverancier, contactpersoon of makelaar, met de projecten waaraan ze hangt.</summary>
public sealed record BekendeContact(string Soort, int? KlantId, int? LeverancierId, IReadOnlyCollection<int> ProjectIds);

/// <summary>Een project zoals de mailregels het nodig hebben om een onderwerp te herkennen.</summary>
public sealed record MailProject(int Id, string Naam, string? Code, string? Straat, string? Nummer, bool Actief, string? Gemeente = null);

/// <summary>Alles wat de regels over de omgeving moeten weten, één keer opgebouwd per sync-ronde.</summary>
public sealed class MailContext
{
    /// <summary>Kleine letters, per e-mailadres.</summary>
    public Dictionary<string, List<BekendeContact>> Contacten { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<MailProject> Projecten { get; } = new();
    /// <summary>Domeinen van de interne collega's (zonder publieke mailproviders zoals gmail.com).</summary>
    public HashSet<string> InterneDomeinen { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>De gebruiker wiens mailbox gesynchroniseerd wordt.</summary>
/// <param name="ThreadProjecten">Per conversatie het project waaraan de gebruiker die thread al koppelde; een antwoord in zo'n thread erft het project.</param>
/// <param name="RecenteProjecten">Projecten van de recent gekoppelde mails van de gebruiker, nieuwste eerst: de basis voor een voorstel.</param>
public sealed record MailGebruiker(int UserId, IReadOnlyCollection<string> EigenAdressen, ISet<string> GeblokkeerdeAfzenders,
    IDictionary<string, int>? ThreadProjecten = null, IReadOnlyList<int>? RecenteProjecten = null);

public sealed record MailBeslissing(bool Opnemen, string Reden, int? ProjectId, int? KlantId, int? LeverancierId, string Status, int? VoorgesteldProjectId = null);

/// <summary>
/// De in- en uitsluitregels en de koppelregels van de mailsync (spec #2). Puur: geen database, geen Graph. Volgorde:
/// (1) uitsluiten: concept, geblokkeerde afzender, nieuwsbrief of no-reply, enkel interne collega's zonder projectverwijzing;
/// (2) Outlooks "Overige" sluit uit, tenzij de afzender een bekende contactpersoon is of het onderwerp een expliciete projectverwijzing heeft;
/// (3) opnemen bij een bekende klant, leverancier, contactpersoon of makelaar, of een projectnaam, -code, straat + nummer of straat + gemeente in het onderwerp.
/// Koppelen: één project per mail (project in het onderwerp of de gekoppelde thread, anders het enige actieve project van de bekende partijen, anders "Nog te koppelen" met een voorstel), klant en leverancier uit de partijen.
/// </summary>
public static class OutlookMailRegels
{
    private static readonly Regex NoReply = new(@"^(no[-_.]?reply|do[-_.]?not[-_.]?reply|donotreply|mailer-daemon|postmaster|bounces?|newsletters?|nieuwsbrief|mailing|marketing)([-_.+].*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Publieke mailproviders: een adres daar zegt niets over "intern".</summary>
    private static readonly HashSet<string> PubliekeDomeinen = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com", "outlook.com", "outlook.be", "hotmail.com", "hotmail.be", "live.com", "live.be", "msn.com", "yahoo.com", "yahoo.be",
        "icloud.com", "me.com", "telenet.be", "skynet.be", "proximus.be", "belgacom.net", "scarlet.be", "pandora.be", "gmx.com", "gmx.net", "proton.me", "protonmail.com"
    };

    public static bool IsPubliekDomein(string domein) => PubliekeDomeinen.Contains(domein);

    public static string? Domein(string? adres)
    {
        var at = adres?.LastIndexOf('@') ?? -1;
        return at > 0 && at < adres!.Length - 1 ? adres[(at + 1)..].Trim().ToLowerInvariant() : null;
    }

    /// <summary>Hoofdletter-, accent- en leesteken-ongevoelig: "Résidence De-Linde" wordt "residence de linde".</summary>
    public static string Normaliseer(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst)) return "";
        var sb = new StringBuilder(tekst.Length);
        foreach (var c in tekst.Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    /// <summary>De projecten die het onderwerp expliciet noemt: naam, code of straat + nummer (hele woorden).</summary>
    public static List<int> ProjectenInOnderwerp(string? onderwerp, IEnumerable<MailProject> projecten)
    {
        var tekst = " " + Normaliseer(onderwerp) + " ";
        if (tekst.Trim().Length == 0) return new();
        bool Bevat(string? zoek) { var n = Normaliseer(zoek); return n.Length >= 3 && tekst.Contains(" " + n + " ", StringComparison.Ordinal); }

        return projecten.Where(p =>
                (Normaliseer(p.Naam).Length >= 4 && Bevat(p.Naam)) ||
                Bevat(p.Code) ||
                (!string.IsNullOrWhiteSpace(p.Straat) && !string.IsNullOrWhiteSpace(p.Nummer) && Bevat($"{p.Straat} {p.Nummer}")) ||
                // Een verkaveling heeft vaak geen huisnummer: straat én gemeente samen in het onderwerp (in elke volgorde) is een expliciete verwijzing.
                (Bevat(p.Straat) && Bevat(p.Gemeente)))
            .Select(p => p.Id).Distinct().ToList();
    }

    public static MailBeslissing Beoordeel(GraphMail mail, MailContext ctx, MailGebruiker gebruiker)
    {
        static MailBeslissing Overslaan(string reden) => new(false, reden, null, null, null, OutlookMailStatus.TeKoppelen);

        var van = mail.VanAdres?.Trim().ToLowerInvariant();
        var eigen = new HashSet<string>(gebruiker.EigenAdressen.Select(a => a.Trim().ToLowerInvariant()));
        var deelnemers = new List<string>();
        if (!string.IsNullOrEmpty(van)) deelnemers.Add(van);
        deelnemers.AddRange(mail.Ontvangers.Select(a => a.Trim().ToLowerInvariant()).Where(a => a.Length > 0));
        var anderen = deelnemers.Where(a => !eigen.Contains(a)).Distinct().ToList();

        // (1) uitsluiten
        if (mail.IsConcept) return Overslaan("concept");
        if (van != null && gebruiker.GeblokkeerdeAfzenders.Contains(van)) return Overslaan("geblokkeerde afzender");
        if (van != null && NoReply.IsMatch(van.Split('@')[0])) return Overslaan("nieuwsbrief of no-reply");

        var projectRefs = ProjectenInOnderwerp(mail.Onderwerp, ctx.Projecten);
        // Een thread die de gebruiker al aan een project koppelde, telt als projectverwijzing (ook voor een verzonden antwoord of een nieuwe deelnemer).
        int? threadProject = mail.ConversationId != null && gebruiker.ThreadProjecten != null && gebruiker.ThreadProjecten.TryGetValue(mail.ConversationId, out var tp) ? tp : null;
        var verwijzing = projectRefs.Count > 0 || threadProject != null;
        var alleIntern = deelnemers.Count > 0 && deelnemers.All(a => Domein(a) is { } d && ctx.InterneDomeinen.Contains(d));
        if (alleIntern && !verwijzing) return Overslaan("enkel interne collega's");

        // (2) Outlooks "Overige"
        var partijen = anderen.SelectMany(a => ctx.Contacten.TryGetValue(a, out var l) ? l : new()).ToList();
        var afzenderBekend = van != null && !eigen.Contains(van) && ctx.Contacten.ContainsKey(van);
        if (mail.IsOverige && !afzenderBekend && !verwijzing) return Overslaan("Overige van een onbekende afzender");

        // (3) opnemen
        if (partijen.Count == 0 && !verwijzing) return Overslaan("geen bekende partij of projectverwijzing");

        var klantId = partijen.Select(p => p.KlantId).FirstOrDefault(i => i != null);
        var leverancierId = partijen.Select(p => p.LeverancierId).FirstOrDefault(i => i != null);

        // Voorrang: project in het onderwerp of de gekoppelde thread > het enige actieve project van de bekende partijen > "Nog te koppelen" met voorstel.
        int? projectId = null;
        var kandidaten = new List<int>();
        if (projectRefs.Count == 1) projectId = projectRefs[0];
        else if (projectRefs.Count > 1) kandidaten.AddRange(projectRefs);
        else if (threadProject != null) projectId = threadProject;
        else
        {
            var actief = ctx.Projecten.Where(p => p.Actief).Select(p => p.Id).ToHashSet();
            kandidaten.AddRange(partijen.SelectMany(p => p.ProjectIds).Where(actief.Contains).Distinct());
            if (kandidaten.Count == 1) projectId = kandidaten[0];
        }

        // Voorstel (nooit een stille koppeling): van de kandidaten het project waaraan de gebruiker het recentst een mail koppelde.
        int? voorstel = null;
        if (projectId == null && kandidaten.Count > 1 && gebruiker.RecenteProjecten != null)
            voorstel = gebruiker.RecenteProjecten.Where(kandidaten.Contains).Cast<int?>().FirstOrDefault();

        // Verzonden mails zijn geen to-do: ze staan op het project of de klant, niet in de Inbox.
        var status = mail.Richting == "uit" ? OutlookMailStatus.Afgehandeld : projectId != null ? OutlookMailStatus.Nieuw : OutlookMailStatus.TeKoppelen;
        return new MailBeslissing(true, projectRefs.Count > 0 ? "projectverwijzing" : threadProject != null ? "thread" : "bekende partij", projectId, klantId, leverancierId, status, voorstel);
    }
}
