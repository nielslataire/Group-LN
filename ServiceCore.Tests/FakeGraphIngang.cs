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
