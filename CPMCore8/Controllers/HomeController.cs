using Microsoft.AspNetCore.Mvc;
using BOCore;
using System.Security.Claims;

namespace CPMCore8.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new Models.Home.HomeModel();
            var service = Service.ServiceFactory.GetProjectService();
            var response = service.GetProjectsForList(0, (int)ProjectStatusType.Uitvoering, User.FindFirstValue(ClaimTypes.NameIdentifier));
            if ((response.Success))
                model.Projects = response.Values;
            response = service.GetProjectsForList(0, (int)ProjectStatusType.Opgeleverd, User.FindFirstValue(ClaimTypes.NameIdentifier));
            if ((response.Success))
                model.OldProjects = response.Values;
            var service2 = Service.ServiceFactory.GetClientService();
            var response2 = service2.GetClientAccountsByDateDeedofSale();
            if ((response2.Success))
                model.DeedofSaleWarnings = response2.Values;
            if (!User.IsInRole("Admin"))
            {
                var iservice = Service.ServiceFactory.GetInsuranceService();
                var iresponse = iservice.CheckInsurances(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if ((iresponse.Success))
                    model.InsuranceWarnings = iresponse.Values;
            }
            else
            {
                var iservice = Service.ServiceFactory.GetInsuranceService();
                var iresponse = iservice.CheckInsurances();
                if ((iresponse.Success))
                    model.InsuranceWarnings = iresponse.Values;
            }
            if (!User.IsInRole("Admin"))
            {
                var iresponse = service.CheckProjectFinished(User.FindFirstValue(ClaimTypes.NameIdentifier));
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

    }
}
