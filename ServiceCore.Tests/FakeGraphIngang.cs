using CPMCore.Services;
using FacadeCore;

namespace ServiceCore.Tests;

/// <summary>Fake van <see cref="IGraphIngang"/>: geeft vaste afspraken terug per gebruiker en onthoudt wat er gevraagd of verstuurd werd.</summary>
public sealed class FakeGraphIngang : IGraphIngang
{
    public bool IsConfigured { get; set; } = true;
    public string? Fout { get; set; }
    public Dictionary<string, List<AgendaAfspraak>> Agenda { get; } = new();
    public List<(string? Gebruiker, DateTime Van, DateTime Tot)> AgendaAanvragen { get; } = new();
    public List<(string? Gebruiker, string EventId, string? Titel, DateTime Start, DateTime Einde)> Wijzigingen { get; } = new();
    public List<(string Van, string Aan, string Onderwerp)> Mails { get; } = new();

    /// <summary>Uitkomst per gebruiker; standaard Ok. Zet <see cref="ToegangGooit"/> om een onverwachte fout te simuleren.</summary>
    public Dictionary<string, GraphToegang> Toegang { get; } = new();
    public bool ToegangGooit { get; set; }
    public List<string?> ToegangAanvragen { get; } = new();

    public Task<GraphToegang> ControleerToegangAsync(string? gebruiker, CancellationToken ct = default)
    {
        ToegangAanvragen.Add(gebruiker);
        if (ToegangGooit) throw new InvalidOperationException("boem");
        return Task.FromResult(gebruiker != null && Toegang.TryGetValue(gebruiker, out var t) ? t : GraphToegang.Ok);
    }

    /// <summary>Mails per gebruiker (sleutel zoals de sync die opvraagt). Alleen mails vanaf het gevraagde moment komen terug, zoals bij een eerste delta.</summary>
    public Dictionary<string, List<GraphMail>> MailsPerGebruiker { get; } = new();
    public List<string> VerwijderdeMailIds { get; } = new();
    public List<(string? Gebruiker, string? Tokens, DateTime Vanaf)> MailAanvragen { get; } = new();
    public string? MailFout { get; set; }

    public Task<GraphMailResultaat> HaalMailAsync(string? gebruiker, string? deltaTokensJson, DateTime vanafUtc, CancellationToken ct = default)
    {
        MailAanvragen.Add((gebruiker, deltaTokensJson, vanafUtc));
        if (MailFout != null) return Task.FromResult(new GraphMailResultaat { Toegang = GraphToegang.Fout, Fout = MailFout });
        var lijst = gebruiker != null && MailsPerGebruiker.TryGetValue(gebruiker, out var l) ? l : new List<GraphMail>();
        // Zonder token geeft Graph alles vanaf "vanaf"; met token enkel wat nog niet eerder is teruggegeven.
        var eerder = deltaTokensJson == null ? 0 : int.Parse(deltaTokensJson);
        var nieuw = lijst.Skip(eerder).Where(m => m.OntvangenOpUtc >= vanafUtc).ToList();
        return Task.FromResult(new GraphMailResultaat { Nieuw = nieuw, VerwijderdeIds = VerwijderdeMailIds.ToList(), NieuweTokensJson = lijst.Count.ToString() });
    }

    public Task<AgendaResultaat> GetAgendaAsync(string? gebruiker, DateTime van, DateTime tot, CancellationToken ct = default)
    {
        AgendaAanvragen.Add((gebruiker, van, tot));
        if (Fout != null || gebruiker == null || !Agenda.TryGetValue(gebruiker, out var lijst))
            return Task.FromResult(new AgendaResultaat { Gekoppeld = false, Fout = Fout ?? "Geen agenda." });
        return Task.FromResult(new AgendaResultaat { Gekoppeld = true, Afspraken = lijst.Where(a => a.Start < tot && a.Einde > van).ToList() });
    }

    public Task<string?> BijwerkenAfspraakAsync(string? gebruiker, string eventId, string? titel, DateTime start, DateTime einde, CancellationToken ct = default)
    {
        Wijzigingen.Add((gebruiker, eventId, titel, start, einde));
        return Task.FromResult<string?>(null);
    }

    public Task<bool> TrySendMailAsync(string fromMailbox, string to, string subject, string html, IEnumerable<EmailAttachment>? attachments, CancellationToken ct = default)
    {
        Mails.Add((fromMailbox, to, subject));
        return Task.FromResult(true);
    }
}
