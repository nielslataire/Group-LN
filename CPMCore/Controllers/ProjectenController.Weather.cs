using BOCore;
using CPMCore.Documents;
using CPMCore.Helpers;
using CPMCore.Models;
using CPMCore.Models.Invoicing;
using CPMCore.Models.Klanten;
using CPMCore.Models.Leveranciers;
using CPMCore.Models.Projecten;
using FacadeCore;
using DALCore;
using DALCore.Models;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;
using ServiceCore;
using ServiceCore.Budget;
using ServiceCore.Invoicing;
using BOCore.Budget;
using CPMCore.Models.Budget;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ClosedXML.Excel;
using SmartBreadcrumbs.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace CPMCore.Controllers
{
    /// <summary>Weerverlet (bad-weather-days) per project: de kalender, regen/vorst-/wind-dagen en
    /// verlofdagen. Opgesplitst uit het vroegere ProjectenController.cs (okt. 2026, "views/controllers/
    /// models structureren") — views in Views/Projecten/Weather/, model in Models/Projecten/
    /// ProjectWeatherModel.cs. Zelfde partial class, dus alle private velden/services van
    /// ProjectenController.cs (bv. _projectService) blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== WEERVERLET ==========

        [HttpGet]
        //[Breadcrumb("Weerverlet")]
        [Breadcrumb("Weerverlet", FromAction = "Index")]
        public ActionResult Weather()
        {
            SetPageHeader("bx bx-building-house", "Weerverlet");
            var model = new BWDModel();
            var service = _projectService;
            var response = service.GetWheaterstationsSelect();
            if (response.Success)
            {
                model.WeatherStations = response.Values;
            }
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectWeather = _ps.HasWrite(PermissionCodes.ProjectsWeatherDelay);
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "WeatherV2" : "Weather", model);
        }
        [HttpGet]
        public IActionResult GetCalendarBundle(int weatherstationid, int year)
        {
            var service = _projectService;


            var rain = new List<object>();
            var wind = new List<object>();
            var vacation = new List<object>();

            var rainResponse = service.GetBadWeatherDays(weatherstationid, 0);
            if (rainResponse.Success)
            {
                rain = rainResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "Regen/Vorst",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();
            }
            var windResponse = service.GetBadWeatherDays(weatherstationid, 1);
            if (windResponse.Success)
            {
                wind = windResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "Wind",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();
            }
            var vacationResponse = service.GetVacationDays();
            if (vacationResponse.Success)
            {
                vacation = vacationResponse.Values.Select(b => new
                {
                    id = b.Id,
                    title = "verlofdag",
                    year = b.VacationDay.Year,
                    month = b.VacationDay.Month,
                    day = b.VacationDay.Day,
                    color = "#777"
                }).Cast<object>().ToList();
            }
            return Ok(new { rain = rain, wind = wind, vacation = vacation });
        }

        [HttpGet]
        public JsonResult GetBadWeatherDays(int type, int weatherstationid, int year)
        {
            var service = _projectService;
            var response = service.GetBadWeatherDays(weatherstationid, type);

            var rows = new List<object>();
            var vacationRows = new List<object>();

            if (response.Success)
            {
                rows = response.Values.Select(b => new
                {
                    id = b.Id,
                    title = "vorst",
                    year = b.BWDate.Year,
                    month = b.BWDate.Month,
                    day = b.BWDate.Day,
                    color = "#009336"
                }).Cast<object>().ToList();

                var response2 = service.GetVacationDays();
                if (response2.Success)
                {
                    vacationRows = response2.Values.Select(b => new
                    {
                        id = b.Id,
                        title = "verlofdag",
                        year = b.VacationDay.Year,
                        month = b.VacationDay.Month,
                        day = b.VacationDay.Day,
                        color = "#777"
                    }).Cast<object>().ToList();
                }
            }

            var allResults = new List<object>(rows.Count + vacationRows.Count);
            allResults.AddRange(rows);
            allResults.AddRange(vacationRows);


            return new JsonResult(allResults);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // in Core ook geldig
        public int AddBadWeatherDay(DateOnly dag, int weatherstationid, int type)
        {
            var bwd = new BadWeatherDayBO
            {
                BWDate = dag,
                WeatherStationId = weatherstationid,
                Type = type
            };

            var service = _projectService;
            var response = service.InsertUpdateBadWeatherDay(bwd);

            if (!response.Success) return 0;

            var msg = response.Messages.FirstOrDefault()?.Message;
            return int.TryParse(msg, out var id) ? id : 0;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public bool DeleteBadWeatherDay(int id)
        {
            var list = new List<int> { id };
            var service = _projectService;
            var response = service.DeleteBadWeatherDays(list);
            return response.Success;
        }

        [HttpGet]
        public JsonResult GetVacationDays()
        {
            var service = _projectService;
            var response = service.GetVacationDays();

            var rows = new List<object>();
            if (response.Success)
            {
                rows = response.Values.Select(b => new
                {
                    year = b.VacationDay.Year,
                    month = b.VacationDay.Month,
                    day = b.VacationDay.Day
                }).Cast<object>().ToList();
            }

            // Classic MVC:
            return Json(rows);

            // ASP.NET Core alternatief:
            // return new JsonResult(rows);
            // of: return Ok(rows);
        }
    }
}
