using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CPMCore.Models;
using CPMCore.Service;
using BOCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
namespace CPMCore.Controllers;

[Authorize]
public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private UserManager<ApplicationUser> _userManager;

    public HomeController(UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var model = new Models.Home.HomeModel();
        var service = ServiceFactory.GetProjectService();
        var response = service.GetProjectsForList(0, (int)ProjectStatusType.Uitvoering, _userManager.GetUserId(User).ToString());
        if ((response.Success))
            model.Projects = response.Values;
        response = service.GetProjectsForList(0, (int)ProjectStatusType.Opgeleverd, _userManager.GetUserId(User).ToString());
        if ((response.Success))
            model.OldProjects = response.Values;
        var service2 = ServiceFactory.GetClientService();
        var response2 = service2.GetClientAccountsByDateDeedofSale();
        var response3 = service.GetStatuses();
        if ((response3.Success))
            model.Statuses = response3.Values;
        if ((response2.Success))
            model.DeedofSaleWarnings = response2.Values;
      if (!User.IsInRole("Admin"))
        {
            var iservice = ServiceFactory.GetInsuranceService();
            var iresponse = iservice.CheckInsurances(_userManager.GetUserId(User).ToString());
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
            var iresponse = service.CheckProjectFinished(_userManager.GetUserId(User).ToString());
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

    //public IActionResult Privacy()
    //{
    //    return View();
    //}

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
