using BOCore;
using CPMCore.Models.Instellingen;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsVacatureBeheer)]
public class VacatureBeheerController : BaseController
{
    private readonly IVacatureService _vacatureService;
    private readonly IVacatureSollicitatieService _sollicitatieService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VacatureBeheerController> _logger;

    public VacatureBeheerController(IVacatureService vacatureService, IVacatureSollicitatieService sollicitatieService, IConfiguration configuration, ILogger<VacatureBeheerController> logger)
    {
        _vacatureService = vacatureService;
        _sollicitatieService = sollicitatieService;
        _configuration = configuration;
        _logger = logger;
    }

    // ── LIST ────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Index()
    {
        SetPageHeader("bx bx-briefcase", "Vacatures");

        var result = _vacatureService.GetVacatures(alleenGepubliceerd: false);
        var vm = new VacatureListVM { Vacatures = result.Values ?? new() };
        return View(vm);
    }

    // ── CREATE ──────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Aanmaken()
    {
        SetPageHeader("bx bx-briefcase", "Nieuwe vacature");

        var vm = new VacatureEditVM();
        return View("Bewerken", vm);
    }

    // ── EDIT ────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Bewerken(int id)
    {
        SetPageHeader("bx bx-briefcase", "Vacature bewerken");

        var result = _vacatureService.GetVacatureById(id);
        if (result.HasErrors || result.Value == null)
        {
            AddMessage("error", "Vacature niet gevonden.", "Fout");
            return RedirectToAction(nameof(Index));
        }

        var previewBase  = _configuration["Preview:BaseUrl"]?.TrimEnd('/');
        var previewToken = _configuration["Preview:Token"];
        if (!string.IsNullOrEmpty(previewBase) && !string.IsNullOrEmpty(previewToken))
        {
            var slug = result.Value?.Slug;
            if (!string.IsNullOrEmpty(slug))
                ViewBag.PreviewUrl = $"{previewBase}/vacatures/{slug}?prev={previewToken}";
        }

        var vm = MapBoToVm(result.Value);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bewerken(VacatureEditVM vm)
    {
        if (!ModelState.IsValid)
        {
            SetPageHeader("bx bx-briefcase", vm.ID == 0 ? "Nieuwe vacature" : "Vacature bewerken");
            return View(vm);
        }

        var videoBestand = vm.VideoBestand;
        if (vm.VideoUpload != null && vm.VideoUpload.Length > 0)
        {
            var ext = System.IO.Path.GetExtension(vm.VideoUpload.FileName)?.ToLowerInvariant();
            var webFormaten = new[] { ".mp4", ".m4v", ".webm", ".ogv", ".ogg" };
            if (!webFormaten.Contains(ext))
            {
                AddMessage("warning",
                    "Enkel webvideo-formaten worden ondersteund (MP4/H.264 speelt overal). Exporteer je video als .mp4 en probeer opnieuw.",
                    "Video niet opgeslagen");
            }
            else
            {
                var (uploaded, uploadError) = await UploadNaarStorageAsync(vm.VideoUpload, "videos", 120 * 1024 * 1024);
                if (uploaded != null)
                {
                    await DeleteVanStorageAsync(vm.VideoBestand);
                    videoBestand = uploaded;
                }
                else
                    AddMessage("warning", $"Video kon niet opgeslagen worden: {uploadError}", "Let op");
            }
        }

        var posterBestand = vm.VideoPosterBestand;
        if (vm.PosterUpload != null && vm.PosterUpload.Length > 0)
        {
            var (uploaded, uploadError) = await UploadNaarStorageAsync(vm.PosterUpload, "pictures", 20 * 1024 * 1024);
            if (uploaded != null)
            {
                await DeleteVanStorageAsync(vm.VideoPosterBestand);
                posterBestand = uploaded;
            }
            else
                AddMessage("warning", $"Posterafbeelding kon niet opgeslagen worden: {uploadError}", "Let op");
        }

        var bo = new VacatureBO
        {
            ID                 = vm.ID,
            Titel              = vm.Titel,
            Slug               = vm.Slug,
            Categorie          = vm.Categorie,
            Locatie            = vm.Locatie,
            Dienstverband      = vm.Dienstverband,
            Opleiding          = vm.Opleiding,
            Start              = vm.Start,
            KorteBeschrijving  = vm.KorteBeschrijving,
            Beschrijving       = vm.Beschrijving,
            VideoBestand       = videoBestand,
            VideoPosterBestand = posterBestand,
            IsGepubliceerd     = vm.IsGepubliceerd,
            SortOrder          = vm.SortOrder
        };

        var response = _vacatureService.InsertUpdate(bo);

        if (response.HasErrors)
        {
            foreach (var msg in response.Messages.Where(m => m.Type == MessageType.Error))
                ModelState.AddModelError(string.Empty, msg.Message);
            vm.VideoBestand = videoBestand;
            vm.VideoPosterBestand = posterBestand;
            SetPageHeader("bx bx-briefcase", vm.ID == 0 ? "Nieuwe vacature" : "Vacature bewerken");
            return View(vm);
        }

        AddMessage("success", "Vacature opgeslagen.", "Opgeslagen");
        var vacatureId = vm.ID != 0 ? vm.ID : response.InsertedId;
        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── DELETE ──────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Verwijderen(int id)
    {
        var response = _vacatureService.DeleteVacature(id);

        if (response.HasErrors)
            AddMessage("error", "Vacature kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Vacature verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Index));
    }

    // ── SOLLICITATIES ──────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Sollicitaties(int? vacatureId)
    {
        SetPageHeader("bx bx-mail-send", "Sollicitaties");

        string? vacatureTitel = null;
        if (vacatureId.HasValue)
        {
            var vacatureResult = _vacatureService.GetVacatureById(vacatureId.Value);
            vacatureTitel = vacatureResult.Value?.Titel;
        }

        var result = _sollicitatieService.GetSollicitaties(vacatureId);
        var vm = new VacatureSollicitatieListVM
        {
            Sollicitaties = result.Values ?? new(),
            VacatureId = vacatureId,
            VacatureTitel = vacatureTitel
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult SollicitatieCvDownloaden(int id)
    {
        var result = _sollicitatieService.GetSollicitatieCv(id);
        if (result.HasErrors || result.Value?.CvBestand == null)
        {
            AddMessage("error", "Cv niet gevonden.", "Fout");
            return RedirectToAction(nameof(Sollicitaties));
        }

        // Openen van de sollicitatie markeert ze meteen als gelezen.
        _sollicitatieService.MarkeerGelezen(id, true);

        var cv = result.Value;
        return File(cv.CvBestand, string.IsNullOrWhiteSpace(cv.CvBestandType) ? "application/octet-stream" : cv.CvBestandType, cv.CvBestandsnaam);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SollicitatieVerwijderen(int id, int? vacatureId)
    {
        var response = _sollicitatieService.DeleteSollicitatie(id);

        if (response.HasErrors)
            AddMessage("error", "Sollicitatie kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Sollicitatie verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Sollicitaties), new { vacatureId });
    }

    // ── TAKENPAKKET ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TaakOpslaan(VacatureTaakVM vm)
    {
        var bo = new VacatureTaakBO { ID = vm.ID, VacatureId = vm.VacatureId, SortOrder = vm.SortOrder, Tekst = vm.Tekst };
        var response = _vacatureService.InsertUpdateTaak(bo);

        if (response.HasErrors)
            AddMessage("error", "Taak kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Taak opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.VacatureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TaakVolgorde([FromForm] VacatureVolgordeVM vm)
    {
        if (vm.VacatureId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _vacatureService.UpdateTakenVolgorde(vm.VacatureId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult TaakVerwijderen(int taakId, int vacatureId)
    {
        var response = _vacatureService.DeleteTaak(taakId);

        if (response.HasErrors)
            AddMessage("error", "Taak kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Taak verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── WIE ZOEKEN WE (must-have / mooi meegenomen) ─────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VereisteOpslaan(VacatureVereisteVM vm)
    {
        var bo = new VacatureVereisteBO { ID = vm.ID, VacatureId = vm.VacatureId, SortOrder = vm.SortOrder, Categorie = vm.Categorie, Tekst = vm.Tekst };
        var response = _vacatureService.InsertUpdateVereiste(bo);

        if (response.HasErrors)
            AddMessage("error", "Vereiste kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Vereiste opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.VacatureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VereisteVolgorde([FromForm] VacatureVolgordeVM vm)
    {
        if (vm.VacatureId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _vacatureService.UpdateVereistenVolgorde(vm.VacatureId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VereisteVerwijderen(int vereisteId, int vacatureId)
    {
        var response = _vacatureService.DeleteVereiste(vereisteId);

        if (response.HasErrors)
            AddMessage("error", "Vereiste kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Vereiste verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── WAT BIEDEN WE ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VoordeelOpslaan(VacatureVoordeelVM vm)
    {
        var bo = new VacatureVoordeelBO { ID = vm.ID, VacatureId = vm.VacatureId, SortOrder = vm.SortOrder, Tekst = vm.Tekst };
        var response = _vacatureService.InsertUpdateVoordeel(bo);

        if (response.HasErrors)
            AddMessage("error", "Voordeel kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Voordeel opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.VacatureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VoordeelVolgorde([FromForm] VacatureVolgordeVM vm)
    {
        if (vm.VacatureId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _vacatureService.UpdateVoordelenVolgorde(vm.VacatureId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VoordeelVerwijderen(int voordeelId, int vacatureId)
    {
        var response = _vacatureService.DeleteVoordeel(voordeelId);

        if (response.HasErrors)
            AddMessage("error", "Voordeel kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Voordeel verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── STAPPENLIJST SOLLICITATIE ────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StapOpslaan(VacatureSollicitatieStapVM vm)
    {
        var bo = new VacatureSollicitatieStapBO { ID = vm.ID, VacatureId = vm.VacatureId, SortOrder = vm.SortOrder, Titel = vm.Titel, Tekst = vm.Tekst };
        var response = _vacatureService.InsertUpdateSollicitatieStap(bo);

        if (response.HasErrors)
            AddMessage("error", "Stap kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Stap opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.VacatureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StapVolgorde([FromForm] VacatureVolgordeVM vm)
    {
        if (vm.VacatureId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _vacatureService.UpdateSollicitatieStappenVolgorde(vm.VacatureId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StapVerwijderen(int stapId, int vacatureId)
    {
        var response = _vacatureService.DeleteSollicitatieStap(stapId);

        if (response.HasErrors)
            AddMessage("error", "Stap kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "Stap verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = vacatureId });
    }

    // ── PRIVATE HELPERS ──────────────────────────────────────────────────

    private static VacatureEditVM MapBoToVm(VacatureBO bo) => new()
    {
        ID                = bo.ID,
        Titel             = bo.Titel,
        Slug              = bo.Slug,
        Categorie         = bo.Categorie,
        Locatie           = bo.Locatie,
        Dienstverband     = bo.Dienstverband,
        Opleiding         = bo.Opleiding,
        Start             = bo.Start,
        KorteBeschrijving  = bo.KorteBeschrijving,
        Beschrijving       = bo.Beschrijving,
        VideoBestand       = bo.VideoBestand,
        VideoPosterBestand = bo.VideoPosterBestand,
        IsGepubliceerd     = bo.IsGepubliceerd,
        SortOrder          = bo.SortOrder,
        TaakItems              = bo.TaakItems,
        VereisteItems          = bo.VereisteItems,
        VoordeelItems          = bo.VoordeelItems,
        SollicitatieStapItems  = bo.SollicitatieStapItems
    };

    // ── STORAGE API (upload/verwijder) — zelfde patroon als BlogBeheerController ──

    private async Task<(string? Url, string? Error)> UploadNaarStorageAsync(IFormFile file, string folder, long maxBytes)
    {
        if (file.Length > maxBytes)
            return (null, $"Bestand is te groot ({file.Length / 1024 / 1024} MB). Maximum is {maxBytes / 1024 / 1024} MB.");

        var baseUrl  = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        var writeKey = _configuration["StorageApi:WriteApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
        {
            _logger.LogWarning("StorageApi niet geconfigureerd — bestand wordt niet opgeslagen.");
            return (null, "Storage API is niet geconfigureerd.");
        }

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", writeKey);

            await using var fileStream = file.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(folder), "folder");

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var response = await httpClient.PostAsync($"{baseUrl}/api/assets/upload", content);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Storage upload mislukt: {Status} — {Body}", (int)response.StatusCode, errorBody);
                return (null, $"Upload mislukt (HTTP {(int)response.StatusCode}).");
            }

            var payload = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(payload);
            if (jsonDoc.RootElement.TryGetProperty("publicUrl", out var pub) && pub.GetString() is string pubUrl && !string.IsNullOrEmpty(pubUrl))
                return (baseUrl + pubUrl, null);
            if (jsonDoc.RootElement.TryGetProperty("fileName", out var fn))
                return (fn.GetString(), null);
            return (null, "Storage gaf geen bestandsnaam terug.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij uploaden naar Storage API.");
            return (null, "Verbindingsfout met Storage API.");
        }
    }

    private async Task DeleteVanStorageAsync(string? bestand)
    {
        if (string.IsNullOrWhiteSpace(bestand))
            return;

        var baseUrl  = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        var writeKey = _configuration["StorageApi:WriteApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
            return;

        if (!bestand.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            return;

        var relativePath = bestand[baseUrl.Length..].TrimStart('/');
        var slashIndex = relativePath.IndexOf('/');
        if (slashIndex < 0)
            return;

        var folder   = relativePath[..slashIndex];
        var fileName = relativePath[(slashIndex + 1)..];

        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(fileName) || fileName.Contains('/'))
            return;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", writeKey);
            await httpClient.DeleteAsync($"{baseUrl}/api/assets/{folder}/{Uri.EscapeDataString(fileName)}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kon oud bestand niet verwijderen uit storage: {Bestand}", bestand);
        }
    }
}
