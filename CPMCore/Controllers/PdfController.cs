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
using CPMCore.Service;

namespace CPMCore.Controllers
{
    public class PdfController : Controller
    {
        private readonly IConverter _converter;
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITempDataProvider _tempDataProvider;

        // Constructor om de converter service te injecteren
        public PdfController(IConverter converter,
                        IRazorViewEngine razorViewEngine,
                        IWebHostEnvironment webHostEnvironment,
                        ITempDataProvider tempDataProvider)
        {
            _converter = converter;
            _razorViewEngine = razorViewEngine;
            _webHostEnvironment = webHostEnvironment;
            _tempDataProvider = tempDataProvider;
        }

        public IActionResult PrintRecalculation(int projectid, int details)
        {
            // Je view model invullen
            ProjectContractsModel viewmodel = new ProjectContractsModel();

            var pservice = ServiceFactory.GetProjectService();
            var aservice = ServiceFactory.GetActivityService();
            ViewBag.detail = details;
            viewmodel.ProjectId = projectid;
            viewmodel.ProjectName = pservice.GetProjectNameById(projectid);
            // Get Units
            var response = aservice.GetActivityGroups();
            viewmodel.ActivityGroups = response.Values;
            var response2 = pservice.GetProjectContracts(projectid);
            viewmodel.Contracts = response2.Values;
            var response3 = pservice.GetProjectBudget(projectid);
            viewmodel.BudgetActivities = response3.Values;
            var response4 = pservice.GetProjectIncommingInvoicesForRecalculation(projectid);
            viewmodel.IncommingInvoicesActivities = response4.Values;

            // Render de view naar HTML
            var htmlContent = RenderViewToStringAsync("PrintRecalculation", viewmodel).Result;

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
