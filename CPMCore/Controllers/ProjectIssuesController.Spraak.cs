using System.Text.Json;
using System.Text.Json.Nodes;
using CPMCore.Helpers;
using CPMCore.Services.PuntenSpraak;
using DALCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPMCore.Controllers;

// Punten inspreken (41c, 40f) en rondgang (41d, 40g/40h): spraak naar tekst via OpenAI, punten herkennen via Anthropic (PuntenSpraakService).
// Inspreken is synchroon in twee stappen (uitschrijven, dan voorstellen) zodat de pagina echte voortgang kan tonen. Een rondgang staat in de database
// en de bestandsopslag (RondgangService, migratie 100) en wordt door een achtergrondtaak verwerkt (RondgangHostedService); hij overleeft dus een herstart
// en blijft 14 dagen staan. Bewaren maakt per voorstel een concept via CreateV2, met de foto's van de rondgang.
public partial class ProjectsIssuesController
{
    private PuntenSpraakService Spraak => HttpContext.RequestServices.GetRequiredService<PuntenSpraakService>();
    private RondgangService Rondgangen => HttpContext.RequestServices.GetRequiredService<RondgangService>();
    private string? CurrentUserId => User.FindFirst(CpmClaims.UserId)?.Value;

    private Task<SpraakContext> LoadSpraakContext(int projectId, int? defaultUnitId) =>
        HttpContext.RequestServices.GetRequiredService<SpraakContextLoader>().LoadAsync(projectId, defaultUnitId);

    private static string ExtensionFor(IFormFile f)
    {
        var n = f.FileName ?? "";
        var ext = Path.GetExtension(n).ToLowerInvariant();
        if (ext is ".webm" or ".mp4" or ".m4a" or ".ogg" or ".wav" or ".mp3" or ".mpeg" or ".mpga") return ext;
        var t = (f.ContentType ?? "").ToLowerInvariant();
        return t.Contains("mp4") ? ".mp4" : t.Contains("ogg") ? ".ogg" : t.Contains("wav") ? ".wav" : t.Contains("mpeg") ? ".mp3" : ".webm";
    }

    private static async Task<byte[]> ReadAll(IFormFile f)
    {
        using var ms = new MemoryStream();
        await f.CopyToAsync(ms);
        return ms.ToArray();
    }

    [HttpGet("SpraakStatusV2")]
    public IActionResult SpraakStatusV2(int projectId) => Json(new { configured = Spraak.Configured });

    /// <summary>Stap 1 van inspreken (en het microfoontje bij de titel): de opname naar tekst.</summary>
    [HttpPost("SpraakTranscribeV2")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> SpraakTranscribeV2(int projectId, IFormFile? audio, int? unitId)
    {
        if (!Spraak.Configured) return Json(new { ok = false, error = "Inspreken is nog niet ingesteld. Vraag een beheerder om de sleutels voor spraak en AI toe te voegen." });
        if (audio == null || audio.Length == 0) return Json(new { ok = false, error = "Er is niets opgenomen." });
        if (audio.Length > Spraak.MaxAudioBytes) return Json(new { ok = false, error = "De opname is te lang. Spreek korter of gebruik Rondgang." });
        try
        {
            var ctx = await LoadSpraakContext(projectId, unitId);
            var hint = "Werfbezoek. " + string.Join(", ", ctx.Units.Take(40).Select(u => u.Name)) + ". " + string.Join(", ", ctx.Contractors.Take(40).Select(c => c.Name));
            var t = await Spraak.TranscribeAsync(await ReadAll(audio), "opname" + ExtensionFor(audio), audio.ContentType ?? "audio/webm", false, hint, HttpContext.RequestAborted);
            return Json(new { ok = true, text = t.Text });
        }
        catch (Exception ex) { return Json(new { ok = false, error = ex.Message }); }
    }

    /// <summary>Stap 2 van inspreken: het transcript wordt voorgestelde punten (met eenheid, zone en aannemer uit de lijsten van het project).</summary>
    [HttpPost("SpraakVoorstelV2")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SpraakVoorstelV2(int projectId, string? text, int? unitId)
    {
        if (!Spraak.Configured) return Json(new { ok = false, error = "Inspreken is nog niet ingesteld." });
        if (string.IsNullOrWhiteSpace(text)) return Json(new { ok = false, error = "Er is geen tekst herkend." });
        try
        {
            var ctx = await LoadSpraakContext(projectId, unitId);
            var list = await Spraak.ExtractAsync(new SpraakTranscript(text.Trim(), new()), ctx, HttpContext.RequestAborted);
            return Json(new { ok = true, proposals = list.Select(ToJson) });
        }
        catch (Exception ex) { return Json(new { ok = false, error = ex.Message }); }
    }

    private static object ToJson(PuntVoorstel p) => new
    {
        title = PuntenWeergave.Titel(p.Title), unitId = p.UnitId, unitName = p.UnitName, zone = string.IsNullOrWhiteSpace(p.Zone) ? "" : PuntenWeergave.Zone(p.Zone),
        contractorId = p.ContractorId, contractorName = p.ContractorName, description = p.Description, priority = p.Priority,
        existingNr = p.ExistingNr, atSeconds = p.AtSeconds, photoIndexes = p.PhotoIndexes,
    };

    // ── rondgang ──

    [HttpPost("RondgangStartV2")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(120 * 1024 * 1024)]
    public async Task<IActionResult> RondgangStartV2(int projectId, IFormFile? audio, List<IFormFile>? photos, string? photoTimes, double duration, int? verslagId, int? unitId)
    {
        if (!Spraak.Configured) return Json(new { ok = false, error = "Rondgang is nog niet ingesteld. Vraag een beheerder om de sleutels voor spraak en AI toe te voegen." });
        if (audio == null || audio.Length == 0) return Json(new { ok = false, error = "Er is niets opgenomen." });
        if (audio.Length > Spraak.MaxAudioBytes) return Json(new { ok = false, error = "De opname is te groot om te verwerken. Splits de rondgang in twee delen." });
        try
        {
            var shots = new List<byte[]>();
            foreach (var ph in photos ?? new()) if (ph.Length > 0) shots.Add(await ReadAll(ph));
            var times = (photoTimes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0).ToList();
            var r = await Rondgangen.CreateAsync(projectId, CurrentUserId, duration, verslagId, unitId, await ReadAll(audio), "rondgang" + ExtensionFor(audio),
                string.IsNullOrWhiteSpace(audio.ContentType) ? "audio/webm" : audio.ContentType, shots, times);
            return Json(new { ok = true, id = r.Id });
        }
        catch (Exception ex) { return Json(new { ok = false, error = ex.Message }); }
    }

    private object RondgangJson(PuntRondgang r)
    {
        var klaar = r.Status == RondgangStatus.Klaar;
        return new
        {
            ok = true, id = r.Id, state = r.Status switch { RondgangStatus.Klaar => "klaar", RondgangStatus.Fout => "fout", _ => "bezig" },
            step = r.Step, percent = r.Percent, duration = r.DurationSeconds, error = r.Status == RondgangStatus.Fout ? (r.Error ?? "De verwerking is mislukt.") : null,
            transcript = klaar ? r.Transcript : "", photoCount = r.Fotos.Count, verslagId = r.VerslagId,
            proposals = klaar ? Rondgangen.Voorstellen(r).Select(ToJson) : Enumerable.Empty<object>(),
        };
    }

    [HttpGet("RondgangStatusV2")]
    public async Task<IActionResult> RondgangStatusV2(int projectId, int id)
    {
        var r = await Rondgangen.GetAsync(id, projectId, CurrentUserId);
        if (r == null) return Json(new { ok = false, gone = true, error = "Deze rondgang is niet meer beschikbaar." });
        return Json(RondgangJson(r));
    }

    /// <summary>De laatste niet-bewaarde rondgang van de gebruiker in dit project (voor de balk bovenaan de lijst), ook na het sluiten van de app of op een ander toestel.</summary>
    [HttpGet("RondgangLopendV2")]
    public async Task<IActionResult> RondgangLopendV2(int projectId)
    {
        try
        {
            var l = await Rondgangen.LopendAsync(projectId, CurrentUserId);
            if (l == null) return Json(new { ok = true, none = true });
            var r = await Rondgangen.GetAsync(l.Id, projectId, CurrentUserId);
            return Json(r == null ? new { ok = true, none = true } : RondgangJson(r));
        }
        catch (Exception ex)
        {
            // bv. migratie 100 nog niet gedraaid: de lijst mag daar niet onder lijden
            HttpContext.RequestServices.GetRequiredService<ILogger<ProjectsIssuesController>>().LogWarning(ex, "Lopende rondgang ophalen mislukt.");
            return Json(new { ok = true, none = true });
        }
    }

    [HttpGet("RondgangFotoV2")]
    public async Task<IActionResult> RondgangFotoV2(int projectId, int id, int i)
    {
        var r = await Rondgangen.GetAsync(id, projectId, CurrentUserId);
        var bytes = r == null ? null : await Rondgangen.FotoAsync(r, i);
        return bytes == null ? NotFound() : File(bytes, "image/jpeg");
    }

    [HttpGet("RondgangAudioV2")]
    public async Task<IActionResult> RondgangAudioV2(int projectId, int id)
    {
        var r = await Rondgangen.GetAsync(id, projectId, CurrentUserId);
        var bytes = r == null ? null : await Rondgangen.AudioAsync(r);
        return bytes == null ? NotFound() : File(bytes, string.IsNullOrWhiteSpace(r!.AudioType) ? "audio/webm" : r.AudioType, enableRangeProcessing: true);
    }

    [HttpPost("RondgangVerwijderV2")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RondgangVerwijderV2(int projectId, int id)
    {
        var r = await Rondgangen.GetAsync(id, projectId, CurrentUserId);
        if (r != null) await Rondgangen.DeleteAsync(r);
        return Json(new { ok = true });
    }

    // ── bewaren ──

    private sealed class BewaarItem
    {
        public string? Title { get; set; }
        public int? UnitId { get; set; }
        public string? Zone { get; set; }
        public int? ContractorId { get; set; }
        public string? Description { get; set; }
        public int? Priority { get; set; }
        public List<int>? PhotoIndexes { get; set; }
    }

    /// <summary>Bewaart de aangevinkte voorstellen als concept. Met een rondgang-id krijgen ze hun foto's; met een verslag-id komen ze in dat open verslag.</summary>
    [HttpPost("SpraakBewaarV2")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SpraakBewaarV2(int projectId, int? jobId, int? verslagId, string? items)
    {
        List<BewaarItem>? list;
        try { list = JsonSerializer.Deserialize<List<BewaarItem>>(items ?? "[]", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return Json(new { ok = false, error = "Ongeldige gegevens." }); }
        if (list == null || list.Count == 0) return Json(new { ok = false, error = "Er zijn geen punten gekozen." });
        var job = jobId.HasValue ? await Rondgangen.GetAsync(jobId.Value, projectId, CurrentUserId) : null;

        var ids = new List<int>(); var fouten = new List<string>();
        foreach (var it in list.Take(60))
        {
            var files = new List<IFormFile>();
            foreach (var i in (it.PhotoIndexes ?? new()).Distinct())
            {
                var bytes = job == null ? null : await Rondgangen.FotoAsync(job, i);
                if (bytes != null) files.Add(new FormFile(new MemoryStream(bytes), 0, bytes.Length, "mediaFiles", $"foto-{i + 1}.jpg") { Headers = new HeaderDictionary(), ContentType = "image/jpeg" });
            }
            var r = await CreateV2(projectId, it.Title, it.UnitId, it.Zone, it.ContractorId, it.Description, null, null, null, it.Priority, null, files, null, null, null, null, false);
            var node = r is JsonResult jr ? JsonNode.Parse(JsonSerializer.Serialize(jr.Value)) : null;
            if (node?["ok"]?.GetValue<bool>() == true && node["id"] != null) ids.Add(node["id"]!.GetValue<int>());
            else fouten.Add(node?["error"]?.GetValue<string>() ?? "onbekende fout");
        }

        verslagId ??= job?.VerslagId;
        if (verslagId.HasValue && ids.Count > 0)
        {
            var verslag = await _db.Werfverslag.FirstOrDefaultAsync(v => v.Id == verslagId && v.ProjectId == projectId && v.Status == 0);
            if (verslag != null)
            {
                foreach (var id in ids) _db.WerfverslagPunt.Add(new WerfverslagPunt { VerslagId = verslag.Id, IssueId = id, IsNieuw = true });
                await _db.SaveChangesAsync();
            }
        }
        if (job != null && fouten.Count == 0) await Rondgangen.DeleteAsync(job);
        return Json(new { ok = ids.Count > 0, count = ids.Count, errors = fouten });
    }
}
