using CPMCore.Models.Marktanalyse;
using CPMCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

public class MarktanalyseController : Controller
{
    private readonly IMarktanalyseService _svc;

    public MarktanalyseController(IMarktanalyseService svc)
    {
        _svc = svc;
    }

    // GET /Marktanalyse/Gemeenteanalyse?postcode=8000&type=Alles
    public async Task<IActionResult> Gemeenteanalyse(
        string? postcode,
        string type = "Alles",
        CancellationToken ct = default)
    {
        var locaties = await _svc.GetLocatiesAsync(ct);

        GemeenteAnalyseViewModel vm;
        if (string.IsNullOrWhiteSpace(postcode))
        {
            vm = new GemeenteAnalyseViewModel { GeselecteerdType = type };
        }
        else
        {
            vm = await _svc.GetGemeenteAnalyseAsync(postcode, type, ct);
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
    public IActionResult Projectdetail(long id)
    {
        return View(model: id);
    }
}
