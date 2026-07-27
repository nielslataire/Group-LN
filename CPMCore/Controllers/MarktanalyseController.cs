using BOCore;
using ClosedXML.Excel;
using CPMCore.Helpers;
using CPMCore.Models.Marktanalyse;
using CPMCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.Marktanalyse)]
public class MarktanalyseController : BaseController
{
    private readonly IMarktanalyseService _svc;

    public MarktanalyseController(IMarktanalyseService svc)
    {
        _svc = svc;
    }

    // GET /Marktanalyse/Gemeenteanalyse?geoMunicipalityId=1&geoMunicipalSectionId=5&type=Alles&aanbodtype=Alles&toonGekoppeld=false
    public async Task<IActionResult> Gemeenteanalyse(
        int? geoMunicipalityId     = null,
        int? geoMunicipalSectionId = null,
        string type                = "Alles",
        string aanbodtype          = "Alles",
        bool toonGekoppeld         = false,
        CancellationToken ct       = default)
    {
        SetPageHeader("bx bx-line-chart", "Gemeenteanalyse");

        var locaties = await _svc.GetLocatiesAsync(ct);

        GemeenteAnalyseViewModel vm;
        if (!geoMunicipalityId.HasValue && !geoMunicipalSectionId.HasValue)
        {
            vm = new GemeenteAnalyseViewModel { GeselecteerdType = type, GeselecteerdAanbodtype = aanbodtype, ToonGekoppeld = toonGekoppeld };
        }
        else
        {
            vm = await _svc.GetGemeenteAnalyseAsync(geoMunicipalityId, geoMunicipalSectionId, type, aanbodtype, toonGekoppeld, ct);
        }

        vm.Locaties = locaties;
        return View(vm);
    }

    // GET /Marktanalyse/VergelijkbarePanden
    public async Task<IActionResult> VergelijkbarePanden(
        [FromQuery] List<int> gemeenteIds,
        string? rondAdresPostcode      = null,
        double? rondAdresLat           = null,
        double? rondAdresLng           = null,
        int    rondAdresStraal         = 1000,
        string type                    = "Appartement",
        decimal? oppervlakte           = null,
        int    tolerantie              = 10,
        decimal? prijsMin              = null,
        decimal? prijsMax              = null,
        int?   slaapkamers             = null,
        string status                  = "Alles",
        CancellationToken ct           = default)
    {
        SetPageHeader("bx bx-search-alt", "Vergelijkbare panden");

        var vm = await _svc.GetVergelijkbarePandenAsync(
            gemeenteIds, rondAdresPostcode, rondAdresLat, rondAdresLng, rondAdresStraal,
            type, oppervlakte, tolerantie, prijsMin, prijsMax,
            slaapkamers, status, ct);

        var userId = User.GetCpmUserId();
        if (userId.HasValue)
        {
            if (vm.HeeftZoekparameters)
            {
                var criteria = new VergelijkbarePandenZoekCriteria(
                    vm.ZoekgebiedTab, gemeenteIds, rondAdresPostcode, rondAdresLat, rondAdresLng,
                    rondAdresStraal, type, oppervlakte, tolerantie, prijsMin, prijsMax, slaapkamers, status);
                await _svc.LogZoekActieAsync(userId.Value, criteria, vm.Panden.Count, ct);
            }
            else
            {
                vm.SnelStartPresets = await _svc.GetSnelStartPresetsAsync(userId.Value, 3, ct);
                vm.RecenteZoekActies = await _svc.GetRecenteZoekActiesAsync(userId.Value, 5, ct);
                vm.OpgeslagenProfielen = await _svc.GetOpgeslagenProfielenAsync(userId.Value, 5, ct);

                foreach (var item in vm.SnelStartPresets.Concat(vm.RecenteZoekActies).Concat(vm.OpgeslagenProfielen))
                    item.Url = BuildVergelijkbarePandenUrl(item.Criteria) ?? "#";
            }
        }

        return View(vm);
    }

    private string? BuildVergelijkbarePandenUrl(VergelijkbarePandenZoekCriteria? c)
    {
        if (c is null) return null;

        return Url.Action("VergelijkbarePanden", "Marktanalyse", new
        {
            gemeenteIds       = c.GemeenteIds,
            rondAdresPostcode = c.RondAdresPostcode,
            rondAdresLat      = c.RondAdresLat,
            rondAdresLng      = c.RondAdresLng,
            rondAdresStraal   = c.RondAdresStraal,
            type              = c.Type,
            oppervlakte       = c.Oppervlakte,
            tolerantie        = c.Tolerantie,
            prijsMin          = c.PrijsMin,
            prijsMax          = c.PrijsMax,
            slaapkamers       = c.Slaapkamers,
            status            = c.Status
        });
    }

    public record SaveZoekProfielRequest(
        string Naam,
        string ZoekgebiedTab,
        List<int> GemeenteIds,
        string? RondAdresPostcode,
        double? RondAdresLat,
        double? RondAdresLng,
        int RondAdresStraal,
        string Type,
        decimal? Oppervlakte,
        int Tolerantie,
        decimal? PrijsMin,
        decimal? PrijsMax,
        int? Slaapkamers,
        string Status);

    // POST /Marktanalyse/SaveZoekProfiel — slaat het huidige zoekprofiel op onder een naam.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveZoekProfiel(SaveZoekProfielRequest request, CancellationToken ct = default)
    {
        var userId = User.GetCpmUserId();
        if (userId is null) return Unauthorized(new { success = false, message = "Niet aangemeld." });

        if (string.IsNullOrWhiteSpace(request?.Naam))
            return BadRequest(new { success = false, message = "Geef een naam op voor dit profiel." });

        var criteria = new VergelijkbarePandenZoekCriteria(
            request.ZoekgebiedTab, request.GemeenteIds ?? new(), request.RondAdresPostcode,
            request.RondAdresLat, request.RondAdresLng, request.RondAdresStraal, request.Type,
            request.Oppervlakte, request.Tolerantie, request.PrijsMin, request.PrijsMax,
            request.Slaapkamers, request.Status);

        var id = await _svc.SaveZoekProfielAsync(userId.Value, request.Naam.Trim(), criteria, ct);
        return Json(new { success = true, id });
    }

    // GET /Marktanalyse/ExportVergelijkbarePanden — export van de huidige resultaten als Excel.
    [HttpGet]
    public async Task<IActionResult> ExportVergelijkbarePanden(
        [FromQuery] List<int> gemeenteIds,
        string? rondAdresPostcode = null,
        double? rondAdresLat      = null,
        double? rondAdresLng      = null,
        int    rondAdresStraal    = 1000,
        string type               = "Appartement",
        decimal? oppervlakte      = null,
        int    tolerantie         = 10,
        decimal? prijsMin         = null,
        decimal? prijsMax         = null,
        int?   slaapkamers        = null,
        string status             = "Alles",
        CancellationToken ct      = default)
    {
        var vm = await _svc.GetVergelijkbarePandenAsync(
            gemeenteIds, rondAdresPostcode, rondAdresLat, rondAdresLng, rondAdresStraal,
            type, oppervlakte, tolerantie, prijsMin, prijsMax, slaapkamers, status, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Vergelijkbare panden");

        string[] headers = { "Project", "Adres", "Type", "Oppervlakte (m²)", "Slpk.", "Prijs", "Prijs/m²", "Status", "Verkoopstatus" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0a5a3b");
        headerRange.Style.Font.FontColor = XLColor.White;

        int rij = 2;
        foreach (var p in vm.Panden)
        {
            ws.Cell(rij, 1).Value = p.ProjectNaam;
            ws.Cell(rij, 2).Value = (p.AdresRegel ?? "").Replace('\n', ' ');
            ws.Cell(rij, 3).Value = p.TypeLabel;
            if (p.Oppervlakte.HasValue) ws.Cell(rij, 4).Value = (double)p.Oppervlakte.Value;
            if (p.Slaapkamers.HasValue) ws.Cell(rij, 5).Value = p.Slaapkamers.Value;
            if (p.Vraagprijs.HasValue)
            {
                ws.Cell(rij, 6).Value = (double)p.Vraagprijs.Value;
                ws.Cell(rij, 6).Style.NumberFormat.Format = "€ #,##0";
            }
            if (p.PrijsPerM2.HasValue)
            {
                ws.Cell(rij, 7).Value = (double)p.PrijsPerM2.Value;
                ws.Cell(rij, 7).Style.NumberFormat.Format = "€ #,##0";
            }
            ws.Cell(rij, 8).Value = p.Status;
            ws.Cell(rij, 9).Value = p.IsProject
                ? (p.Verkoopgraad.HasValue ? $"{Math.Round(p.Verkoopgraad.Value)}% verkocht" : "Project")
                : "Losse eenheid";
            rij++;
        }

        ws.Columns().AdjustToContents();
        if (rij > 2)
        {
            ws.Range(1, 1, rij - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(1, 1, rij - 1, headers.Length).Style.Border.InsideBorder  = XLBorderStyleValues.Hair;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var fileName = $"Vergelijkbare_panden_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    // GET /Marktanalyse/GeocodeAdres?query=...
    // Geocodeert een vrij ingevoerd adres naar coördinaten voor de "Rond adres"-kaart.
    public async Task<IActionResult> GeocodeAdres(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Json(new { success = false });

        var result = await _svc.GeocodeAdresAsync(query, ct);
        if (result is null) return Json(new { success = false });

        return Json(new { success = true, lat = result.Lat, lng = result.Lng, label = result.Label });
    }

    // GET /Marktanalyse/TelPandenInStraal?lat=..&lng=..&straal=..
    // Lichtgewicht live telling voor de kaart-preview in de "Rond adres"-dropdown.
    public async Task<IActionResult> TelPandenInStraal(
        double lat,
        double lng,
        int    straal,
        string type          = "Appartement",
        decimal? oppervlakte = null,
        int    tolerantie    = 10,
        decimal? prijsMin    = null,
        decimal? prijsMax    = null,
        int?   slaapkamers   = null,
        string status        = "Alles",
        CancellationToken ct = default)
    {
        var aantal = await _svc.TelPandenInStraalAsync(
            lat, lng, straal, type, oppervlakte, tolerantie, prijsMin, prijsMax, slaapkamers, status, ct);
        return Json(new { aantal });
    }

    // GET /Marktanalyse/Projectdetail/5
    public async Task<IActionResult> Projectdetail(long id, CancellationToken ct = default)
    {
        var vm = await _svc.GetProjectDetailAsync(id, ct);
        if (vm is null) return NotFound();

        SetPageHeader("bx bx-building-house", vm.ProjectNaam);
        return View(vm);
    }
}
