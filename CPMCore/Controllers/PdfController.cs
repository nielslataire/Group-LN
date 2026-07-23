using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using CPMCore.Models.Projecten;
using BOCore;
using DALCore;
using FacadeCore;
using ServiceCore;
using CPMCore.Models;

namespace CPMCore.Controllers
{
    public class PdfController : Controller
    {
        private readonly IConverter _converter;
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IProjectService _projectService;
        private readonly IActivityService _activityService;

        public PdfController(IConverter converter,
                        IRazorViewEngine razorViewEngine,
                        IWebHostEnvironment webHostEnvironment,
                        ITempDataProvider tempDataProvider,
                        IProjectService projectService,
                        IActivityService activityService)
        {
            _converter = converter;
            _razorViewEngine = razorViewEngine;
            _webHostEnvironment = webHostEnvironment;
            _tempDataProvider = tempDataProvider;
            _projectService = projectService;
            _activityService = activityService;
        }

        public async Task<IActionResult> PrintRecalculation(int projectid, int details)
        {
            ProjectContractsModel viewmodel = new ProjectContractsModel();

            ViewBag.detail = details;
            viewmodel.ProjectId = projectid;
            viewmodel.ProjectName = _projectService.GetProjectNameById(projectid);
            var response = _activityService.GetActivityGroups();
            viewmodel.ActivityGroups = response.Values;
            var response2 = _projectService.GetProjectContracts(projectid);
            viewmodel.Contracts = response2.Values;
            var response3 = _projectService.GetProjectBudget(projectid);
            viewmodel.BudgetActivities = response3.Values;
            var response4 = _projectService.GetProjectIncommingInvoicesForRecalculation(projectid);
            viewmodel.IncommingInvoicesActivities = response4.Values;

            // Render de view naar HTML
            var htmlContent = await RenderViewToStringAsync("PrintRecalculation", viewmodel);

            // Maak een nieuwe PdfDocument
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = { ColorMode = ColorMode.Color, Orientation = Orientation.Portrait, PaperSize = PaperKind.A4, },
                Objects = { new ObjectSettings() { HtmlContent = htmlContent, WebSettings = new WebSettings() { DefaultEncoding = "utf-8" }, } }
            };

            // Genereer de PDF
            var pdf = _converter.Convert(doc);

            // Geef de gegenereerde PDF terug als bestand
            return File(pdf, "application/pdf", "nacalculatie.pdf");
        }

        // Helper methode om de view naar een string te renderen
        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            var viewResult = _razorViewEngine.FindView(ControllerContext, viewName, isMainPage: true);

            if (viewResult.Success == false)
                throw new ArgumentNullException($"View {viewName} not found");

            var tempData = new TempDataDictionary(ControllerContext.HttpContext, _tempDataProvider);
            var viewContext = new ViewContext(ControllerContext, viewResult.View, new ViewDataDictionary(new EmptyModelMetadataProvider(), ModelState) { Model = model }, tempData, new StringWriter(), new HtmlHelperOptions());
            await viewResult.View.RenderAsync(viewContext);
            // Verkrijg de HTML als string van de StringWriter
            return viewContext.Writer.ToString();
        }
    }
}
