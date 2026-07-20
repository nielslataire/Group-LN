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
            gemeenteIds, rondAdresPostcode, rondAdresStraal,
            type, oppervlakte, tolerantie, prijsMin, prijsMax,
            slaapkamers, status, ct);
        return View(vm);
    }

    // GET /Marktanalyse/Projectdetail/5
    public async Task<IActionResult> Projectdetail(long id, CancellationToken ct = default)
    {
        var vm = await _svc.GetProjectDetailAsync(id, ct);
        if (vm is null) return NotFound();
        return View(vm);
    }
}
