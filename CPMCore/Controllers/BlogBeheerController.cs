using BOCore;
using CPMCore.Attributes;
using CPMCore.Models.Instellingen;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.SettingsBlogBeheer)]
public class BlogBeheerController : BaseController
{
    private readonly IBlogArtikelService _blogService;
    private readonly ILogger<BlogBeheerController> _logger;
    private readonly IConfiguration _configuration;
    private readonly cpmRunningContext _db;

    public BlogBeheerController(IBlogArtikelService blogService, ILogger<BlogBeheerController> logger, IConfiguration configuration, cpmRunningContext db)
    {
        _blogService = blogService;
        _logger = logger;
        _configuration = configuration;
        _db = db;
    }

    // ── LIST ────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Index()
    {
        var result = _blogService.GetArtikelen(alleenGepubliceerd: false);
        var vm = new BlogArtikelListVM { Artikelen = result.Values ?? new() };
        return View(vm);
    }

    // ── CREATE ──────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Aanmaken()
    {
        var vm = new BlogArtikelEditVM { Datum = DateTime.Today };
        VulLinkDropdowns(vm);
        return View("Bewerken", vm);
    }

    // ── EDIT ────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Bewerken(int id)
    {
        var result = _blogService.GetArtikelById(id);
        if (result.HasErrors || result.Value == null)
        {
            AddMessage("error", "Artikel niet gevonden.", "Fout");
            return RedirectToAction(nameof(Index));
        }

        var previewBase  = _configuration["Preview:BaseUrl"]?.TrimEnd('/');
        var previewToken = _configuration["Preview:Token"];
        if (!string.IsNullOrEmpty(previewBase) && !string.IsNullOrEmpty(previewToken))
        {
            var slug = result.Value?.Slug;
            if (!string.IsNullOrEmpty(slug))
                ViewBag.PreviewUrl = $"{previewBase}/Blog/{slug}?prev={previewToken}";
        }

        var vm = MapBoToVm(result.Value);
        VulLinkDropdowns(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bewerken(BlogArtikelEditVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var fotoBestand = vm.FotoBestand;
        if (vm.FotoUpload != null && vm.FotoUpload.Length > 0)
        {
            var (uploaded, uploadError) = await UploadNaarStorageAsync(vm.FotoUpload, "pictures");
            if (uploaded != null)
            {
                await DeleteVanStorageAsync(vm.FotoBestand);
                fotoBestand = uploaded;
            }
            else
                AddMessage("warning", $"Foto kon niet opgeslagen worden: {uploadError}", "Let op");
        }

        var bo = new BlogArtikelBO
        {
            ID               = vm.ID,
            Titel            = vm.Titel,
            Slug             = vm.Slug,
            PreviewTekst     = vm.PreviewTekst,
            DetailTitel      = vm.DetailTitel,
            DetailTitelTekst = vm.DetailTitelTekst,
            FotoBestand      = fotoBestand,
            Datum            = vm.Datum,
            IsGepubliceerd   = vm.IsGepubliceerd,
            SortOrder        = vm.SortOrder,
            MetaTitel        = vm.MetaTitel,
            MetaOmschrijving = vm.MetaOmschrijving,
            MetaKeywords     = vm.MetaKeywords,
            GeoRegio         = vm.GeoRegio,
            GeoPlaatsnaam    = vm.GeoPlaatsnaam,
            GeoPositie       = vm.GeoPositie,
            Link1Type        = vm.Link1Type,
            Link1Id          = vm.Link1Id,
            Link2Type        = vm.Link2Type,
            Link2Id          = vm.Link2Id,
            Link3Type        = vm.Link3Type,
            Link3Id          = vm.Link3Id
        };

        var response = _blogService.InsertUpdate(bo);

        if (response.HasErrors)
        {
            foreach (var msg in response.Messages.Where(m => m.Type == MessageType.Error))
                ModelState.AddModelError(string.Empty, msg.Message);
            VulLinkDropdowns(vm);
            return View(vm);
        }

        AddMessage("success", "Artikel opgeslagen.", "Opgeslagen");
        var artikelId = vm.ID != 0 ? vm.ID : response.InsertedId;
        return RedirectToAction(nameof(Bewerken), new { id = artikelId });
    }

    // ── DELETE ARTIKEL ───────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verwijderen(int id)
    {
        var fotos = new List<string?>();
        var artikelResult = _blogService.GetArtikelById(id);
        if (!artikelResult.HasErrors && artikelResult.Value != null)
        {
            fotos.Add(artikelResult.Value.FotoBestand);
            fotos.AddRange(artikelResult.Value.Blokken.Select(b => b.FotoBestand));
        }

        var response = _blogService.DeleteArtikel(id);

        if (response.HasErrors)
            AddMessage("error", "Artikel kon niet verwijderd worden.", "Fout");
        else
        {
            foreach (var foto in fotos)
                await DeleteVanStorageAsync(foto);
            AddMessage("success", "Artikel verwijderd.", "Verwijderd");
        }

        return RedirectToAction(nameof(Index));
    }

    // ── BLOK OPSLAAN ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlokOpslaan(BlogArtikelBlokVM vm)
    {
        var fotoBestand = vm.FotoBestand;
        if (vm.FotoUpload != null && vm.FotoUpload.Length > 0)
        {
            var (uploaded, uploadError) = await UploadNaarStorageAsync(vm.FotoUpload, "pictures");
            if (uploaded != null)
            {
                await DeleteVanStorageAsync(vm.FotoBestand);
                fotoBestand = uploaded;
            }
            else
                AddMessage("warning", $"Blokfoto kon niet opgeslagen worden: {uploadError}", "Let op");
        }

        var bo = new BlogArtikelBlokBO
        {
            ID          = vm.ID,
            ArtikelId   = vm.ArtikelId,
            SortOrder   = vm.SortOrder,
            BlokType    = vm.BlokType,
            Titel       = vm.Titel,
            RijkeTekst  = vm.RijkeTekst,
            FotoBestand = fotoBestand
        };

        var response = _blogService.InsertUpdateBlok(bo);

        if (response.HasErrors)
            AddMessage("error", "Blok kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "Blok opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.ArtikelId });
    }

    // ── BLOK VOLGORDE ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult BlokVolgorde([FromForm] BlokVolgordeVM vm)
    {
        if (vm.ArtikelId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _blogService.UpdateBlokkenVolgorde(vm.ArtikelId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    // ── FAQ OPSLAAN ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult FaqOpslaan(BlogArtikelFaqVM vm)
    {
        var bo = new BlogArtikelFaqBO
        {
            ID        = vm.ID,
            ArtikelId = vm.ArtikelId,
            SortOrder = vm.SortOrder,
            Vraag     = vm.Vraag,
            Antwoord  = vm.Antwoord
        };

        var response = _blogService.InsertUpdateFaq(bo);

        if (response.HasErrors)
            AddMessage("error", "FAQ-item kon niet opgeslagen worden.", "Fout");
        else
            AddMessage("success", "FAQ-item opgeslagen.", "Opgeslagen");

        return RedirectToAction(nameof(Bewerken), new { id = vm.ArtikelId });
    }

    // ── FAQ VOLGORDE ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult FaqVolgorde([FromForm] FaqVolgordeVM vm)
    {
        if (vm.ArtikelId <= 0 || vm.SortedIds == null || !vm.SortedIds.Any())
            return Json(new { success = false });

        var response = _blogService.UpdateFaqVolgorde(vm.ArtikelId, vm.SortedIds);
        return Json(new { success = !response.HasErrors });
    }

    // ── FAQ VERWIJDEREN ──────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult FaqVerwijderen(int faqId, int artikelId)
    {
        var response = _blogService.DeleteFaq(faqId);

        if (response.HasErrors)
            AddMessage("error", "FAQ-item kon niet verwijderd worden.", "Fout");
        else
            AddMessage("success", "FAQ-item verwijderd.", "Verwijderd");

        return RedirectToAction(nameof(Bewerken), new { id = artikelId });
    }

    // ── BLOK VERWIJDEREN ─────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlokVerwijderen(int blokId, int artikelId)
    {
        string? blokFoto = null;
        var artikelResult = _blogService.GetArtikelById(artikelId);
        if (!artikelResult.HasErrors && artikelResult.Value != null)
            blokFoto = artikelResult.Value.Blokken.FirstOrDefault(b => b.ID == blokId)?.FotoBestand;

        var response = _blogService.DeleteBlok(blokId);

        if (response.HasErrors)
            AddMessage("error", "Blok kon niet verwijderd worden.", "Fout");
        else
        {
            await DeleteVanStorageAsync(blokFoto);
            AddMessage("success", "Blok verwijderd.", "Verwijderd");
        }

        return RedirectToAction(nameof(Bewerken), new { id = artikelId });
    }

    // ── PRIVATE HELPERS ──────────────────────────────────────────────────

    private void VulLinkDropdowns(BlogArtikelEditVM vm)
    {
        var artikelen = _blogService.GetArtikelen(alleenGepubliceerd: false);
        vm.ArtikelOpties = (artikelen.Values ?? new())
            .OrderBy(a => a.Titel)
            .Select(a => new SelectListItem(a.Titel, a.ID.ToString()))
            .ToList();

        vm.ProjectOpties = _db.Set<DALCore.Models.Project>()
            .Where(p => p.IsPublished)
            .OrderBy(p => p.ProjectName)
            .Select(p => new SelectListItem(
                p.ProjectName + (p.PostalCode != null && p.PostalCode.Gemeente != null
                    ? " — " + p.PostalCode.Gemeente
                    : ""),
                p.ProjectId.ToString()))
            .ToList();
    }

    private BlogArtikelEditVM MapBoToVm(BlogArtikelBO bo) => new()
    {
        ID               = bo.ID,
        Titel            = bo.Titel,
        Slug             = bo.Slug,
        PreviewTekst     = bo.PreviewTekst,
        DetailTitel      = bo.DetailTitel,
        DetailTitelTekst = bo.DetailTitelTekst,
        FotoBestand      = bo.FotoBestand,
        Datum            = bo.Datum == default ? DateTime.Today : bo.Datum,
        IsGepubliceerd   = bo.IsGepubliceerd,
        SortOrder        = bo.SortOrder,
        MetaTitel        = bo.MetaTitel,
        MetaOmschrijving = bo.MetaOmschrijving,
        MetaKeywords     = bo.MetaKeywords,
        GeoRegio         = bo.GeoRegio,
        GeoPlaatsnaam    = bo.GeoPlaatsnaam,
        GeoPositie       = bo.GeoPositie,
        Link1Type        = bo.Link1Type,
        Link1Id          = bo.Link1Id,
        Link2Type        = bo.Link2Type,
        Link2Id          = bo.Link2Id,
        Link3Type        = bo.Link3Type,
        Link3Id          = bo.Link3Id,
        Blokken  = bo.Blokken.Select(b => new BlogArtikelBlokVM
        {
            ID          = b.ID,
            ArtikelId   = b.ArtikelId,
            SortOrder   = b.SortOrder,
            BlokType    = b.BlokType ?? "tekst",
            Titel       = b.Titel,
            RijkeTekst  = b.RijkeTekst,
            FotoBestand = b.FotoBestand
        }).ToList(),
        FaqItems = bo.FaqItems.Select(f => new BlogArtikelFaqVM
        {
            ID        = f.ID,
            ArtikelId = f.ArtikelId,
            SortOrder = f.SortOrder,
            Vraag     = f.Vraag,
            Antwoord  = f.Antwoord
        }).ToList()
    };

    private async Task DeleteVanStorageAsync(string? fotoBestand)
    {
        if (string.IsNullOrWhiteSpace(fotoBestand))
            return;

        var baseUrl  = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        var writeKey = _configuration["StorageApi:WriteApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
            return;

        if (!fotoBestand.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            return;

        var relativePath = fotoBestand[baseUrl.Length..].TrimStart('/');
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
            _logger.LogWarning(ex, "Kon oude foto niet verwijderen uit storage: {FotoBestand}", fotoBestand);
        }
    }

    private async Task<(string? Url, string? Error)> UploadNaarStorageAsync(IFormFile file, string folder)
    {
        const long MaxBytes = 30 * 1024 * 1024;

        if (file.Length > MaxBytes)
            return (null, $"Foto is te groot ({file.Length / 1024 / 1024} MB). Maximum is 20 MB.");

        var baseUrl  = _configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
        var writeKey = _configuration["StorageApi:WriteApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
        {
            _logger.LogWarning("StorageApi niet geconfigureerd — foto wordt niet opgeslagen.");
            return (null, "Storage API is niet geconfigureerd.");
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Api-Key", writeKey);

            await using var fileStream = file.OpenReadStream();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(folder), "folder");

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
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
            _logger.LogError(ex, "Fout bij uploaden van foto naar Storage API.");
            return (null, "Verbindingsfout met Storage API.");
        }
    }
}
