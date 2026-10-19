using System.Text.Json;
using CPMCore.Helpers;
using DALCore.Models;
using FacadeCore.Signing;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Services.PuntenSpraak
{
    public static class RondgangStatus { public const int Wacht = 0, Bezig = 1, Klaar = 2, Fout = 3; }

    /// <summary>Laadt de lijsten van een project (eenheden, aannemers, open punten) die de AI nodig heeft om een transcript om te zetten naar punten.</summary>
    public class SpraakContextLoader
    {
        private readonly cpmRunningContext _db;
        public SpraakContextLoader(cpmRunningContext db) { _db = db; }

        public async Task<SpraakContext> LoadAsync(int projectId, int? defaultUnitId, CancellationToken ct = default)
        {
            var units = await _db.Units.AsNoTracking().Where(u => u.ProjectId == projectId).OrderBy(u => u.Name).Select(u => new SpraakItem(u.Id, u.Name)).ToListAsync(ct);
            var contractors = (await _db.Contract.AsNoTracking().Where(c => c.ProjectId == projectId)
                .Select(c => new { c.CompanyId, Name = c.Company.BedrijfsNaam }).Distinct().ToListAsync(ct))
                .Select(c => new SpraakItem(c.CompanyId, PuntenWeergave.Aannemer(c.Name))).OrderBy(c => c.Name).ToList();
            var recent = await _db.ConstructionIssue.AsNoTracking().Include(i => i.Unit).Where(i => i.ProjectId == projectId)
                .OrderByDescending(i => i.CreatedDate).Take(300).ToListAsync(ct);
            var bestaand = recent.Where(i => !PuntenWeergave.IsClosed(i.Status))
                .Select(i => new SpraakBestaand(i.PuntNr ?? i.Id, i.Title ?? "", i.Unit?.Name ?? "Algemeen", i.RoomOrZone ?? "")).ToList();
            return new SpraakContext { Units = units, Contractors = contractors, Bestaand = bestaand, DefaultUnitId = defaultUnitId is > 0 ? defaultUnitId : null };
        }
    }

    /// <summary>Rondgangen (opname + foto's) in de database en de bestandsopslag (map "docs"), zodat ze een herstart overleven. Een achtergrondtaak
    /// (<see cref="RondgangHostedService"/>) verwerkt wat nog niet klaar is; bewaarde of verlopen rondgangen (14 dagen) worden opgeruimd.</summary>
    public class RondgangService
    {
        public const string Folder = "docs";
        public const int BewaarDagen = 14;
        private const int MaxPogingen = 2;

        private readonly cpmRunningContext _db;
        private readonly IAssetStorageClient _storage;
        private readonly PuntenSpraakService _spraak;
        private readonly SpraakContextLoader _ctx;
        private readonly ILogger<RondgangService> _log;

        public RondgangService(cpmRunningContext db, IAssetStorageClient storage, PuntenSpraakService spraak, SpraakContextLoader ctx, ILogger<RondgangService> log)
        { _db = db; _storage = storage; _spraak = spraak; _ctx = ctx; _log = log; }

        public async Task<PuntRondgang> CreateAsync(int projectId, string? userId, double duration, int? verslagId, int? unitId,
            byte[] audio, string audioFileName, string audioType, IReadOnlyList<byte[]> photos, IReadOnlyList<double> times, CancellationToken ct = default)
        {
            audioType = (audioType ?? "audio/webm").Split(';')[0].Trim();
            var audioFile = await _storage.UploadAsync(audio, audioFileName, audioType, Folder, ct);
            if (string.IsNullOrWhiteSpace(audioFile)) throw new InvalidOperationException("De opname kon niet opgeslagen worden in de bestandsopslag. Controleer op de server of StorageApi__BaseUrl en StorageApi__WriteApiKey ingesteld zijn (zie het serverlog: \"Storage API upload\").");
            var r = new PuntRondgang
            {
                ProjectId = projectId, UserId = userId, CreatedAt = DateTime.UtcNow, DurationSeconds = duration, VerslagId = verslagId, DefaultUnitId = unitId is > 0 ? unitId : null,
                Status = RondgangStatus.Wacht, Step = "In de wachtrij", AudioFile = audioFile, AudioType = audioType,
            };
            for (var i = 0; i < photos.Count; i++)
            {
                var name = await _storage.UploadAsync(photos[i], $"rondgang-foto-{i + 1}.jpg", "image/jpeg", Folder, ct);
                if (string.IsNullOrWhiteSpace(name))
                {
                    // AssetStorageClient logt zelf al de technische reden (status/exception); hier enkel het
                    // gevolg loggen (welke foto van welke rondgang) — anders is "enkel de eerste foto kwam door"
                    // niet te onderscheiden van een AI-koppelprobleem (zie KoppelFotos) zonder dit regeltje.
                    _log.LogWarning("Rondgang-foto {Index} (van {Totaal}) kon niet opgeslagen worden in de bestandsopslag; overgeslagen.", i + 1, photos.Count);
                    continue;   // een foto die niet lukt laat de rest niet vallen
                }
                r.Fotos.Add(new PuntRondgangFoto { Volgnr = r.Fotos.Count, AtSeconds = i < times.Count ? times[i] : 0, FileName = name });
            }
            _db.PuntRondgang.Add(r);
            await _db.SaveChangesAsync(ct);
            return r;
        }

        public Task<PuntRondgang?> GetAsync(int id, int projectId, string? userId, CancellationToken ct = default) =>
            _db.PuntRondgang.Include(r => r.Fotos).FirstOrDefaultAsync(r => r.Id == id && r.ProjectId == projectId && r.UserId == userId, ct);

        /// <summary>De laatste rondgang van deze gebruiker in dit project die nog niet bewaard is.</summary>
        public Task<PuntRondgang?> LopendAsync(int projectId, string? userId, CancellationToken ct = default) =>
            _db.PuntRondgang.AsNoTracking().Where(r => r.ProjectId == projectId && r.UserId == userId).OrderByDescending(r => r.Id).FirstOrDefaultAsync(ct);

        public Task<byte[]?> AudioAsync(PuntRondgang r, CancellationToken ct = default) => _storage.DownloadAsync(Folder, r.AudioFile, ct);

        public Task<byte[]?> FotoAsync(PuntRondgang r, int volgnr, CancellationToken ct = default)
        {
            var f = r.Fotos.FirstOrDefault(x => x.Volgnr == volgnr);
            return f == null ? Task.FromResult<byte[]?>(null) : _storage.DownloadAsync(Folder, f.FileName, ct);
        }

        public List<PuntVoorstel> Voorstellen(PuntRondgang r)
        {
            if (string.IsNullOrWhiteSpace(r.VoorstellenJson)) return new();
            try { return JsonSerializer.Deserialize<List<PuntVoorstel>>(r.VoorstellenJson) ?? new(); } catch { return new(); }
        }

        /// <summary>Verwijdert de rij en de bestanden (na bewaren, bij weggooien of na 14 dagen).</summary>
        public async Task DeleteAsync(PuntRondgang r, CancellationToken ct = default)
        {
            var files = r.Fotos.Select(f => f.FileName).Append(r.AudioFile).Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
            _db.PuntRondgang.Remove(r);
            await _db.SaveChangesAsync(ct);
            foreach (var f in files) { try { await _storage.DeleteAsync(Folder, f!, ct); } catch { /* best-effort */ } }
        }

        /// <summary>Verwerkt de oudste rondgang die wacht (of waarvan de verwerking bleef hangen, bv. door een herstart). Geeft false als er niets te doen was.</summary>
        public async Task<bool> ProcessNextAsync(CancellationToken ct = default)
        {
            var stale = DateTime.UtcNow.AddMinutes(-15);
            var r = await _db.PuntRondgang.Include(x => x.Fotos)
                .Where(x => x.Status == RondgangStatus.Wacht || (x.Status == RondgangStatus.Bezig && x.StartedAt < stale))
                .OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
            if (r == null) return false;

            if (r.Attempts >= MaxPogingen)
            {
                r.Status = RondgangStatus.Fout; r.Error = "De verwerking is meermaals mislukt."; await _db.SaveChangesAsync(ct);
                return true;
            }
            r.Attempts++; r.Status = RondgangStatus.Bezig; r.StartedAt = DateTime.UtcNow; r.Step = "Uitschrijven"; r.Percent = 10; r.Error = null;
            await _db.SaveChangesAsync(ct);
            try
            {
                var audio = await AudioAsync(r, ct) ?? throw new InvalidOperationException("De opname is niet meer beschikbaar.");
                var ctx = await _ctx.LoadAsync(r.ProjectId, r.DefaultUnitId, ct);
                ctx.PhotoTimes = r.Fotos.OrderBy(f => f.Volgnr).Select(f => f.AtSeconds).ToList();
                var hint = "Werfrondgang. " + string.Join(", ", ctx.Units.Take(40).Select(u => u.Name)) + ". " + string.Join(", ", ctx.Contractors.Take(40).Select(c => c.Name));
                var ext = (r.AudioType ?? "").Contains("mp4") ? ".m4a" : (r.AudioType ?? "").Contains("ogg") ? ".ogg" : ".webm";
                var t = await _spraak.TranscribeAsync(audio, "rondgang" + ext, r.AudioType ?? "audio/webm", true, hint, ct);
                r.Transcript = t.Text; r.Step = "Punten herkennen"; r.Percent = 60; await _db.SaveChangesAsync(ct);
                var list = await _spraak.ExtractAsync(t, ctx, ct);
                KoppelFotos(list, ctx.PhotoTimes);
                r.VoorstellenJson = JsonSerializer.Serialize(list); r.Step = "Klaar"; r.Percent = 100; r.Status = RondgangStatus.Klaar;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Rondgang {Id} verwerken mislukt (poging {N}).", r.Id, r.Attempts);
                r.Status = RondgangStatus.Wacht; r.Step = "Nieuwe poging"; r.Error = ex.Message.Length > 450 ? ex.Message[..450] : ex.Message;
                if (r.Attempts >= MaxPogingen) r.Status = RondgangStatus.Fout;
            }
            await _db.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>Elke foto die de AI aan geen enkel punt koppelde, gaat naar het punt dat vlak ervoor (of anders vlak erna) werd genoemd. Zo heeft een punt
        /// altijd de foto('s) die tijdens het bespreken ervan zijn genomen.
        /// Correctie 17/10/2026: als GEEN enkel voorstel een atSeconds heeft (de AI geeft dat niet altijd consequent voor elk voorstel — gekend risico, zie
        /// "Kwaliteit van de AI-voorstellen… bijstellen" hierboven) viel "best" vroeger op null zodra er meer dan één voorstel was, en verdween de foto
        /// helemaal (ze stond dan wel al in de bestandsopslag/database, maar hing aan geen enkel voorstel en was dus in de review onvindbaar). Eerst
        /// geprobeerd: dan maar altijd het eerste voorstel — bleek fout voor net het geval waar dit vangnet voor bedoeld is (een foto tijdens een LATER punt
        /// genomen moet niet bij punt 1 belanden). Nu i.p.v. daarvan: de voorstellen worden verondersteld in de volgorde te staan waarin ze ingesproken zijn
        /// (zo geeft de AI ze ook terug), dus elk voorstel krijgt een "verondersteld tijdstip" door de voorstellen gelijk te verdelen over de tijdspanne van
        /// de genomen foto's (eerste t/m laatste fototijdstip) — een foto gaat dan naar het voorstel waarvan dat veronderstelde tijdstip het dichtst bij
        /// (en niet na) het echte fototijdstip ligt, net als bij de normale (wel-atSeconds) tak hierboven.</summary>
        public static void KoppelFotos(List<PuntVoorstel> list, IReadOnlyList<double> times)
        {
            if (list.Count == 0 || times.Count == 0) return;
            var metTijd = list.Where(p => p.AtSeconds.HasValue).ToList();
            // Vangnet voor wanneer de AI op GEEN ENKEL voorstel een atSeconds gaf: voorstel i krijgt een
            // verondersteld tijdstip, de voorstellen gelijk verspreid over het bereik van de fototijdstippen —
            // zodat een laat genomen foto ook bij een later voorstel in de lijst terechtkomt, niet bij het eerste.
            List<(PuntVoorstel Punt, double Tijd)>? verondersteld = null;
            if (metTijd.Count == 0)
            {
                var start = times.Min(); var eind = times.Max();
                verondersteld = list.Select((p, idx) => (Punt: p, Tijd: list.Count == 1 ? start : start + (eind - start) * idx / (list.Count - 1))).ToList();
            }
            for (var i = 0; i < times.Count; i++)
            {
                if (list.Any(p => p.PhotoIndexes.Contains(i))) continue;
                PuntVoorstel best;
                if (metTijd.Count > 0)
                {
                    var voor = metTijd.Where(p => p.AtSeconds!.Value <= times[i] + 5).OrderByDescending(p => p.AtSeconds).FirstOrDefault();
                    best = voor ?? metTijd.OrderBy(p => p.AtSeconds).First();
                }
                else
                {
                    var voor = verondersteld!.Where(v => v.Tijd <= times[i] + 5).OrderByDescending(v => v.Tijd).FirstOrDefault();
                    best = (voor.Punt ?? verondersteld!.OrderBy(v => v.Tijd).First().Punt);
                }
                best.PhotoIndexes.Add(i);
            }
        }

        /// <summary>Ruimt rondgangen op die langer dan 14 dagen niet bewaard zijn.</summary>
        public async Task<int> CleanupAsync(CancellationToken ct = default)
        {
            var grens = DateTime.UtcNow.AddDays(-BewaarDagen);
            var oud = await _db.PuntRondgang.Include(r => r.Fotos).Where(r => r.CreatedAt < grens).ToListAsync(ct);
            foreach (var r in oud) await DeleteAsync(r, ct);
            return oud.Count;
        }
    }

    /// <summary>Verwerkt rondgangen op de achtergrond: elke 5 seconden de volgende in de wachtrij, en één keer per uur opruimen.
    /// Na een herstart van de applicatie gaat dit vanzelf verder met wat nog niet klaar was.</summary>
    public class RondgangHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<RondgangHostedService> _log;
        public RondgangHostedService(IServiceScopeFactory scopes, ILogger<RondgangHostedService> log) { _scopes = scopes; _log = log; }

        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stop);
            var nextCleanup = DateTime.UtcNow;
            while (!stop.IsCancellationRequested)
            {
                var busy = false;
                try
                {
                    using var scope = _scopes.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<RondgangService>();
                    busy = await svc.ProcessNextAsync(stop);
                    if (DateTime.UtcNow >= nextCleanup) { var n = await svc.CleanupAsync(stop); if (n > 0) _log.LogInformation("{N} oude rondgang(en) opgeruimd.", n); nextCleanup = DateTime.UtcNow.AddHours(1); }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    // bv. migratie 100 nog niet gedraaid: niet crashen, later opnieuw proberen
                    _log.LogWarning(ex, "Rondgang-achtergrondtaak mislukt.");
                    try { await Task.Delay(TimeSpan.FromMinutes(1), stop); } catch (OperationCanceledException) { break; }
                }
                if (!busy) { try { await Task.Delay(TimeSpan.FromSeconds(5), stop); } catch (OperationCanceledException) { break; } }
            }
        }
    }
}
