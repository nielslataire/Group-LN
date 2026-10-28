using DALCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.OutlookSync;

public static class OutlookSyncCodes
{
    public const string Wacht = "wacht", Ok = "ok", GeenToegang = "geen-toegang", GeenMailbox = "geen-mailbox", Fout = "fout";
}

/// <summary>Wat de gebruiker (Mijn profiel) en de beheerder zien van de Outlook-koppeling van één gebruiker. Nooit mail- of agenda-inhoud.</summary>
public sealed record OutlookSyncWeergave(bool Aan, string Code, string Label, string Melding, string Niveau, DateTime? LaatsteSyncLokaal, string? TechnischeFout);

/// <summary>
/// Opt-in en synchronisatie van de Outlook-koppeling per gebruiker (spec #2, ticket #6). Standaard uit; aanzetten legt het moment vast (mails komen enkel
/// vanaf dan). Elke ronde controleert per opt-in gebruiker de toegang via <see cref="IGraphIngang"/> en bewaart de uitkomst; een fout stopt de koppeling niet,
/// de volgende ronde probeert opnieuw en herstelt de status. De mail- en agendasync zelf haken later in <see cref="SynchroniseerAsync"/> in.
/// </summary>
public class OutlookSyncService
{
    /// <summary>Hoe vaak de achtergrondservice een ronde draait.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    /// <summary>Na zoveel rondes zonder succesvolle sync (terwijl de status "ok" is) meldt de weergave dat de sync niet meer loopt.</summary>
    private const int StilNaRondes = 4;

    private readonly cpmRunningContext _db;
    private readonly IGraphIngang _graph;
    private readonly OutlookMailSyncService _mail;
    private readonly ILogger<OutlookSyncService> _logger;

    public OutlookSyncService(cpmRunningContext db, IGraphIngang graph, OutlookMailSyncService mail, ILogger<OutlookSyncService> logger)
    {
        _db = db;
        _graph = graph;
        _mail = mail;
        _logger = logger;
    }

    public async Task<OutlookSyncStatus> ZetAanAsync(int userId, DateTime nuUtc, CancellationToken ct = default)
    {
        try
        {
            return await ZetAanZonderHerprobeerAsync(userId, nuUtc, ct);
        }
        catch (DbUpdateException)
        {
            // Twee gelijktijdige aanvragen (bv. dubbelklik) maakten allebei een rij; de unieke index weigerde de tweede. Lees de bestaande rij opnieuw.
            _db.ChangeTracker.Clear();
            return await ZetAanZonderHerprobeerAsync(userId, nuUtc, ct);
        }
    }

    private async Task<OutlookSyncStatus> ZetAanZonderHerprobeerAsync(int userId, DateTime nuUtc, CancellationToken ct)
    {
        var rij = await _db.OutlookSyncStatus.FirstOrDefaultAsync(r => r.UserId == userId, ct);
        if (rij == null)
        {
            rij = new OutlookSyncStatus { UserId = userId };
            _db.OutlookSyncStatus.Add(rij);
        }
        if (!rij.Aan)
        {
            // Opnieuw aanzetten start een nieuwe koppeling: enkel mails vanaf nu, verse deltatokens en een schone status.
            rij.Aan = true; rij.AangezetOp = nuUtc; rij.Status = OutlookSyncCodes.Wacht;
            rij.LaatsteFout = null; rij.FoutenOpRij = 0; rij.DeltaTokensJson = null;
        }
        rij.GewijzigdOp = nuUtc;
        await _db.SaveChangesAsync(ct);
        return rij;
    }

    public async Task ZetUitAsync(int userId, DateTime nuUtc, CancellationToken ct = default)
    {
        var rij = await _db.OutlookSyncStatus.FirstOrDefaultAsync(r => r.UserId == userId, ct);
        if (rij == null || !rij.Aan) return;
        rij.Aan = false; rij.Status = OutlookSyncCodes.Wacht; rij.LaatsteFout = null; rij.FoutenOpRij = 0; rij.DeltaTokensJson = null;
        rij.GewijzigdOp = nuUtc;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Eén ronde voor alle actieve gebruikers met de koppeling aan. Een mislukte gebruiker houdt de anderen niet tegen. Geeft het aantal verwerkte gebruikers.</summary>
    public async Task<int> SynchroniseerAlleAsync(DateTime nuUtc, CancellationToken ct = default)
    {
        var kandidaten = await (from r in _db.OutlookSyncStatus.AsNoTracking()
                                join u in _db.Users.AsNoTracking() on r.UserId equals u.Id
                                where r.Aan && u.IsActive
                                select new { r.Id, u.EntraObjectId, u.Email }).ToListAsync(ct);
        var verwerkt = 0;
        _mail.NieuweRonde();
        foreach (var k in kandidaten)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                // Per gebruiker opnieuw lezen: heeft die de koppeling ondertussen uitgezet, dan schrijven we niets meer over de gereset rij.
                var rij = await _db.OutlookSyncStatus.FirstOrDefaultAsync(r => r.Id == k.Id && r.Aan, ct);
                if (rij == null) continue;
                await SynchroniseerAsync(rij, IGraphIngang.GebruikerSleutel(k.EntraObjectId, k.Email), nuUtc, ct);
                verwerkt++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outlook-sync opslaan mislukt voor rij {Id}.", k.Id);
                _db.ChangeTracker.Clear();   // een mislukte SaveChanges mag de volgende gebruikers niet meesleuren
            }
        }
        return verwerkt;
    }

    public async Task SynchroniseerAsync(OutlookSyncStatus rij, string? gebruiker, DateTime nuUtc, CancellationToken ct = default)
    {
        GraphToegang toegang;
        string? fout = null;
        try
        {
            toegang = await _graph.ControleerToegangAsync(gebruiker, ct);
            if (toegang == GraphToegang.Ok)
            {
                // Toegang in orde: nu de mails (nieuw sinds de vorige ronde) ophalen, filteren en koppelen.
                var mail = await _mail.SynchroniseerAsync(rij, gebruiker, nuUtc, ct);
                toegang = mail.Toegang;
                fout = mail.Fout;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Outlook-sync voor gebruiker {UserId} mislukt.", rij.UserId);
            toegang = GraphToegang.Fout;
            fout = ex.GetType().Name;
        }

        switch (toegang)
        {
            case GraphToegang.Ok:
                rij.Status = OutlookSyncCodes.Ok; rij.LaatsteSyncOp = nuUtc; rij.LaatsteFout = null; rij.FoutenOpRij = 0;
                break;
            case GraphToegang.GeenToegang:
                rij.Status = OutlookSyncCodes.GeenToegang; rij.LaatsteFout = "Graph weigerde de toegang tot de mailbox (403). Controleer of de app Mail.Read en Calendars.Read heeft en of de gebruiker in de toegestane groep zit."; rij.FoutenOpRij++;
                break;
            case GraphToegang.GeenMailbox:
                rij.Status = OutlookSyncCodes.GeenMailbox; rij.LaatsteFout = "Graph vond geen mailbox voor deze gebruiker (404)."; rij.FoutenOpRij++;
                break;
            default:
                rij.Status = OutlookSyncCodes.Fout;
                rij.LaatsteFout = Kort(fout) ?? (string.IsNullOrWhiteSpace(gebruiker) ? "Geen Entra-account of e-mailadres bekend." : _graph.IsConfigured ? "Graph gaf een fout." : "De Entra-instellingen ontbreken.");
                rij.FoutenOpRij++;
                break;
        }
        rij.GewijzigdOp = nuUtc;
        await _db.SaveChangesAsync(ct);
    }

    private static string? Kort(string? tekst) => tekst is { Length: > 500 } ? tekst[..500] : tekst;

    public async Task<OutlookSyncWeergave> WeergaveVoorAsync(int userId, DateTime nuUtc, CancellationToken ct = default) =>
        Weergave(await _db.OutlookSyncStatus.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId, ct), nuUtc);

    public static OutlookSyncWeergave Weergave(OutlookSyncStatus? rij, DateTime nuUtc)
    {
        if (rij == null || !rij.Aan)
            return new(false, "uit", "Uit", "CPM leest je Outlook niet. Zet de koppeling aan om afspraken en project-gerelateerde mails te volgen.", "neutraal", null, null);

        var laatste = rij.LaatsteSyncOp?.ToLocalTime();
        var wanneer = laatste == null ? "nog niet" : laatste.Value.ToString("dd/MM/yyyy HH:mm");
        switch (rij.Status)
        {
            case OutlookSyncCodes.Ok:
                if (rij.LaatsteSyncOp != null && nuUtc - rij.LaatsteSyncOp.Value > Interval * StilNaRondes)
                    return new(true, "stil", "Sync loopt niet", $"De synchronisatie loopt niet meer (laatste sync {wanneer}). Contacteer een beheerder als dit blijft duren.", "fout", laatste, rij.LaatsteFout);
                return new(true, OutlookSyncCodes.Ok, "Gekoppeld", $"Laatste sync: {wanneer}.", "ok", laatste, null);
            case OutlookSyncCodes.GeenToegang:
                return new(true, rij.Status, "Geen toegang", "Geen toegang: je zit niet in de toegestane groep. Contacteer je beheerder.", "fout", laatste, rij.LaatsteFout);
            case OutlookSyncCodes.GeenMailbox:
                return new(true, rij.Status, "Geen mailbox", "Voor je account is geen Outlook-mailbox gevonden. Contacteer je beheerder.", "fout", laatste, rij.LaatsteFout);
            case OutlookSyncCodes.Fout:
                return new(true, rij.Status, "Fout", $"De laatste synchronisatie mislukte (laatste geslaagde sync: {wanneer}). CPM probeert het automatisch opnieuw.", "waarschuwing", laatste, rij.LaatsteFout);
            default:
                return new(true, OutlookSyncCodes.Wacht, "Wacht op eerste sync", "De koppeling staat aan; de eerste synchronisatie volgt binnen enkele minuten.", "neutraal", null, null);
        }
    }
}
