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

    public OutlookAfspraakService(cpmRunningContext db, OutlookMailSyncService context)
    {
        _db = db;
        _context = context;
    }

    /// <summary>Vult per afspraak het gekoppelde project en, voor een niet-gekoppelde afspraak, het voorstel in. Overschrijft altijd alle velden, zodat een bewaarde (gecachte) afspraak nooit een oude koppeling toont.</summary>
    /// <param name="zichtbareProjecten">De projecten van de gebruiker (id → naam); alleen daaruit komen een label of voorstel.</param>
    public async Task VerrijkAsync(int userId, string? eigenEmail, IReadOnlyDictionary<int, string> zichtbareProjecten, IEnumerable<AgendaAfspraak> afspraken, CancellationToken ct = default)
    {
        var lijst = afspraken.Where(a => !string.IsNullOrEmpty(a.Id)).ToList();
        if (lijst.Count == 0) return;

        var ids = lijst.Select(a => a.Id).Distinct().ToList();
        var koppelingen = await _db.OutlookAfspraak.AsNoTracking().Where(k => k.UserId == userId && ids.Contains(k.EventId)).ToDictionaryAsync(k => k.EventId, k => k.ProjectId, ct);

        MailContext? ctx = null;
        foreach (var a in lijst)
        {
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
