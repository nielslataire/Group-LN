using BOCore;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;
using System.Diagnostics;
using System.Globalization;

namespace CPMCore.Controllers;

[Authorize]
[CPMCore.Filters.PermissionRead(PermissionCodes.Dashboard)]
public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [DefaultBreadcrumb("Dashboard")]
    public IActionResult Index()
    {
        var model = new Models.Home.HomeModel();
        var service = ServiceFactory.GetProjectService();
        var currentUserCode = User.GetCpmUserCode() ?? string.Empty;

        var response = service.GetProjectsForList(0, 0, currentUserCode);
        if (response.Success)
        {
            var uitvoeringStatusId = (int)ProjectStatusType.Uitvoering;
            var opgeleverdStatusId = (int)ProjectStatusType.Opgeleverd;

            model.Projects = response.Values
                .OrderBy(p => p.Status.Id == uitvoeringStatusId ? 0 : p.Status.Id == opgeleverdStatusId ? 2 : 1)
                .ThenBy(p => p.Status.Name)
                .ThenBy(p => p.Name)
                .ToList();
        }
        var service2 = ServiceFactory.GetClientService();
        var response2 = service2.GetClientAccountsByDateDeedofSale();
        var response3 = service.GetStatuses();
        if ((response3.Success))
        {
            var uitvoeringStatusId = (int)ProjectStatusType.Uitvoering;
            var opgeleverdStatusId = (int)ProjectStatusType.Opgeleverd;

            model.Statuses = response3.Values
                .OrderBy(s => s.Id == uitvoeringStatusId ? 0 : s.Id == opgeleverdStatusId ? 2 : 1)
                .ThenBy(s => s.Name)
                .ToList();
        }
        if ((response2.Success))
            model.DeedofSaleWarnings = response2.Values;
        if (!User.IsInRole("Admin"))
        {
            var iservice = ServiceFactory.GetInsuranceService();
            var iresponse = iservice.CheckInsurances(currentUserCode);
            if ((iresponse.Success))
                model.InsuranceWarnings = iresponse.Values;
        }
        else
        {
            var iservice = ServiceFactory.GetInsuranceService();
            var iresponse = iservice.CheckInsurances();
            if ((iresponse.Success))
                model.InsuranceWarnings = iresponse.Values;
        }
        if (!User.IsInRole("Admin"))
        {
            var iresponse = service.CheckProjectFinished(currentUserCode);
            if ((iresponse.Success))
                model.ProjectInfo = iresponse.Values;
        }
        else
        {
            var iresponse = service.CheckProjectFinished();
            if ((iresponse.Success))
                model.ProjectInfo = iresponse.Values;
        }
        return View(model);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}