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

    // GET /Marktanalyse/Gemeenteanalyse?geoMunicipalityId=1&geoMunicipalSectionId=5&type=Alles&aanbodtype=Alles
    public async Task<IActionResult> Gemeenteanalyse(
        int? geoMunicipalityId = null,
        int? geoMunicipalSectionId = null,
        string type = "Alles",
        string aanbodtype = "Alles",
        CancellationToken ct = default)
    {
        var locaties = await _svc.GetLocatiesAsync(ct);

        GemeenteAnalyseViewModel vm;
        if (!geoMunicipalityId.HasValue && !geoMunicipalSectionId.HasValue)
        {
            vm = new GemeenteAnalyseViewModel { GeselecteerdType = type, GeselecteerdAanbodtype = aanbodtype };
        }
        else
        {
            vm = await _svc.GetGemeenteAnalyseAsync(geoMunicipalityId, geoMunicipalSectionId, type, aanbodtype, ct);
        }

        vm.Locaties = locaties;
        return View(vm);
    }

    // GET /Marktanalyse/VergelijkbarePanden
    public IActionResult VergelijkbarePanden()
    {
        return View();
    }

    // GET /Marktanalyse/Projectdetail/5
    public async Task<IActionResult> Projectdetail(long id, CancellationToken ct = default)
    {
        var vm = await _svc.GetProjectDetailAsync(id, ct);
        if (vm is null) return NotFound();
        return View(vm);
    }
}
