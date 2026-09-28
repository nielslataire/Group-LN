using System.Text.Json;
using DALCore.Models;
using FacadeCore.Signing;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore.Signing;

/// <summary>
/// De enige schrijver van <c>SigningEvent</c> (ONDERTEKENEN_VOORSTEL.md §4.6, §3.7). Elk event krijgt
/// <c>PrevEventHash</c> = hash van het vorige event van hetzelfde dossier en <c>EventHash</c> over de
/// canonieke velden — ruwe IP/user-agent uitgesloten, hun hash wél meegenomen, zodat een
/// retentiescrub de ketting niet breekt. Schrijft binnen de transactie van de aanroeper wanneer
/// die er een heeft (zelfde DbContext).
/// </summary>
public sealed class SigningEvidenceStore : ISigningEvidenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    private readonly cpmRunningContext _db;

    public SigningEvidenceStore(cpmRunningContext db) { _db = db; }

    public async Task<long> AppendAsync(SigningEventDraft draft, CancellationToken ct = default)
    {
        // Het laatste event van dit dossier — de ketting loopt per dossier, op Id-volgorde.
        var prevHash = await _db.SigningEvent
            .Where(e => e.SigningCaseId == draft.CaseId)
            .OrderByDescending(e => e.Id)
            .Select(e => e.EventHash)
            .FirstOrDefaultAsync(ct);

        var occurredAt = DateTime.UtcNow;
        var dataJson = draft.Data is null ? null : JsonSerializer.Serialize(draft.Data, JsonOptions);
        var ipHash = SigningCrypto.HashPii(draft.Ip);
        var uaHash = SigningCrypto.HashPii(draft.UserAgent);

        var entity = new SigningEvent
        {
            SigningCaseId = draft.CaseId,
            SigningPartyId = draft.PartyId,
            EventType = draft.EventType,
            OccurredAtUtc = occurredAt,
            ActorType = draft.ActorType,
            ActorUserId = draft.ActorUserId,
            ActorLabel = Truncate(draft.ActorLabel, 200),
            Ip = Truncate(draft.Ip, 45),
            IpHash = ipHash,
            UserAgent = Truncate(draft.UserAgent, 500),
            UserAgentHash = uaHash,
            DocumentSha256 = draft.DocumentSha256,
            DataJson = dataJson,
            PrevEventHash = prevHash,
        };
        entity.EventHash = SigningCrypto.ComputeEventHash(
            prevHash, entity.SigningCaseId, entity.SigningPartyId, entity.EventType, entity.OccurredAtUtc,
            entity.ActorType, entity.ActorUserId, entity.ActorLabel, entity.IpHash, entity.UserAgentHash,
            entity.DocumentSha256, entity.DataJson);

        _db.SigningEvent.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<IReadOnlyList<SigningEventView>> ListAsync(int caseId, CancellationToken ct = default)
    {
        var events = await _db.SigningEvent.AsNoTracking()
            .Where(e => e.SigningCaseId == caseId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        return events.Select(e => new SigningEventView(
            e.Id, e.SigningPartyId, e.EventType, e.OccurredAtUtc, e.ActorType, e.ActorLabel,
            SigningCrypto.MaskIp(e.Ip), e.DocumentSha256, e.DataJson, e.EventHash)).ToList();
    }

    public async Task<ChainVerification> VerifyAsync(int caseId, CancellationToken ct = default)
    {
        var events = await _db.SigningEvent.AsNoTracking()
            .Where(e => e.SigningCaseId == caseId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        if (events.Count == 0) return new ChainVerification(true, 0, null, "Geen gebeurtenissen.");

        string? prev = null;
        foreach (var e in events)
        {
            if (!string.Equals(e.PrevEventHash ?? string.Empty, prev ?? string.Empty, StringComparison.Ordinal))
                return new ChainVerification(false, events.Count, e.Id, $"Ketting onderbroken vóór gebeurtenis {e.Id}: vorige hash komt niet overeen.");

            var expected = SigningCrypto.ComputeEventHash(
                e.PrevEventHash, e.SigningCaseId, e.SigningPartyId, e.EventType, e.OccurredAtUtc,
                e.ActorType, e.ActorUserId, e.ActorLabel, e.IpHash, e.UserAgentHash, e.DocumentSha256, e.DataJson);
            if (!SigningCrypto.FixedTimeEqualsHex(expected, e.EventHash))
                return new ChainVerification(false, events.Count, e.Id, $"Gebeurtenis {e.Id} is gewijzigd: hash komt niet overeen.");

            prev = e.EventHash;
        }

        return new ChainVerification(true, events.Count, null, $"{events.Count} gebeurtenissen, ketting intact.");
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
