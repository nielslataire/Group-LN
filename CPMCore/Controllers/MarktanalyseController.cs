using BOCore;
using CPMCore.Models.Marktanalyse;
using CPMCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.Marktanalyse)]
public class MarktanalyseController : Controller
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
        var vm = await _svc.GetVergelijkbarePandenAsync(
            gemeenteIds, rondAdresPostcode, rondAdresLat, rondAdresLng, rondAdresStraal,
            type, oppervlakte, tolerantie, prijsMin, prijsMax,
            slaapkamers, status, ct);
        return View(vm);
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
        return View(vm);
    }
}
