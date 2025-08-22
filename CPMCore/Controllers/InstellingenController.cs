using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CPMCore.Models;
using CPMCore.Service;
using BOCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CPMCore.Attributes;
using CPMCore.Models.Projecten;
using CPMCore.Models.Home;
namespace CPMCore.Controllers;

[Authorize]
public class InstellingenController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private UserManager<ApplicationUser> _userManager;

    public InstellingenController(UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }


    //VAKANTIEDAGEN
    [HttpGet]
    [Breadcrumb("Vakantiedagen")]
    public ActionResult Vacationdays()
    {
        HomeModel model = new HomeModel();
      
        return View(model);
    }
    [HttpGet]
    public IActionResult GetVacationDays()
    {
        var service = ServiceFactory.GetProjectService();
        var response = service.GetVacationDaysGeneral();

        var rows = response.Success
            ? response.Values.Select(b => new
            {
                id = b.Id,
                title = "verlofdag",
                year = b.VacationDay.Year,
                month = b.VacationDay.Month,
                day = b.VacationDay.Day
            }).ToArray()
            : Array.Empty<object>();

        return Json(rows); // of: return Ok(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public int AddVacationDay(DateOnly dag)
    {
        var day = new VacationDayBO { VacationDay = dag };
        var service = ServiceFactory.GetProjectService();
        var response = service.InsertUpdateVacationDay(day);

        if (!response.Success) return 0;

        var idMsg = response.Messages.FirstOrDefault(m => m.Type == MessageType.Value)?.Message
                    ?? response.Messages.FirstOrDefault()?.Message;

        return int.TryParse(idMsg, out var id) ? id : 0;
    }

    [HttpPost]
    public bool DeleteVacationDay(int id)
    {
        var ids = new List<int> { id };
        var service = ServiceFactory.GetProjectService();
        var response = service.DeleteVacationDays(ids);
        return response.Success;
    }

}
