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
    /// <summary>Verzekeringen van een project. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Insurances/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== PROJECT DETAIL INSURANCES ==========

        [HttpGet]
        //[Breadcrumb("Verzekeringen")]
        [Breadcrumb("Verzekeringen", FromAction = "Detail")]
        public IActionResult DetailInsurances(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";

            var model = new DetailInsurancesModel();
            var service = _projectService;
            var response = service.GetProjectInsurances(projectid);

            if (response.Success)
                model.Insurances = response.Values;

            model.ProjectId = projectid;
            model.ProjectName = service.GetProjectNameById(projectid);


            //BREADCRUMBS
            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", model.ProjectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailInsurances", "Projecten", "Verzekeringen")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectInsurances = _ps.HasWrite(PermissionCodes.ProjectsInsurances);
            ViewBag.CanDeleteProjectInsurances = _ps.HasDelete(PermissionCodes.ProjectsInsurances);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Verzekeringen");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditInsurance(ProjectAddInsurancesModel viewmodel)
        {
            var response = new Response();

            if (ModelState.IsValid)
            {
                if (viewmodel?.Insurance == null)
                {
                    response.AddError("Insurance model is leeg.");
                }
                else
                {
                    viewmodel.Insurance.ProjectID = viewmodel.ProjectId;

                    // Nieuwe verzekering: maak de ContractActivity (142) aan op het contract van de
                    // gekozen makelaar; de verzekering hangt daar 1-op-1 aan vast.
                    if (viewmodel.Insurance.Id == 0 && viewmodel.Insurance.ContractActivityID == 0)
                    {
                        if (viewmodel.SelectedBrokerId <= 0)
                        {
                            response.AddError("Kies een makelaar.");
                        }
                        else
                        {
                            var caId = _projectService.CreateInsuranceContractActivity(viewmodel.ProjectId, viewmodel.SelectedBrokerId);
                            if (caId <= 0)
                                response.AddError("De verzekeringsactiviteit kon niet aangemaakt worden.");
                            else
                                viewmodel.Insurance.ContractActivityID = caId;
                        }
                    }

                    if (response.Success)
                    {
                        var service = _insuranceService;
                        response = service.InsertUpdate(viewmodel.Insurance);
                    }
                }
            }

            if (response.Success)
            {
                AddMessage("success", "De verzekering is toegevoegd", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "De verzekering is NIET toegevoegd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            }

            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = viewmodel?.Insurance?.ProjectID ?? viewmodel?.ProjectId });
        }


        [HttpGet]
        public IActionResult ModalDeleteInsurance(int id)
        {
            var viewModel = new InsuranceBO();

            if (id != 0)
            {
                var dservice = _insuranceService;
                viewModel = dservice.GetInsuranceById(id).Value;
            }

            return PartialView("_ModalDeleteInsurance", viewModel);
        }

        [HttpGet]
        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsInsurances)]
        public IActionResult DeleteInsurance(int id, int projectId)
        {
            var response = _insuranceService.Delete(id);
            AddMessage(response.Success ? "success" : "error",
                response.Success ? "De verzekering is verwijderd." : "De verzekering kon niet verwijderd worden.",
                response.Success ? "Geslaagd!" : "Fout!");
            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = projectId });
        }

        [HttpGet]
        public IActionResult ModalEndInsurance(int id)
        {
            var viewModel = new InsuranceBO();

            if (id != 0)
            {
                var dservice = _insuranceService;
                viewModel = dservice.GetInsuranceById(id).Value;

                if (viewModel != null)
                {
                    if (viewModel.Type == InsuranceType.ABR && viewModel.Startdate.HasValue)
                    {
                        var start = viewModel.Startdate.Value;
                        viewModel.Enddate = start.AddMonths(
                            (viewModel.Period ?? 0) + (viewModel.ExtensionPeriod ?? 0) + (viewModel.GuaranteePeriod ?? 0)
                        );
                    }
                    else
                    {
                        viewModel.Enddate = DateOnly.FromDateTime(DateTime.Now);
                    }
                }
            }

            return PartialView("_ModalStopInsurance", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EndInsurance(InsuranceBO viewmodel)
        {
            var response = new Response();

            if (ModelState.IsValid)
            {
                var service = _insuranceService;
                response = service.InsertUpdate(viewmodel);
            }

            if (response.Success)
            {
                AddMessage("success", "De verzekering is beëindigd", "Geslaagd!");
            }
            else
            {
                AddMessage("error", "De verzekering is NIET beëindigd, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            }

            return RedirectToAction("DetailInsurances", "Projecten", new { projectid = viewmodel.ProjectID });
        }

        [HttpGet]
        public IActionResult ModalEditInsurance(int id, int projectid = 0)
        {
            var viewModel = new ProjectAddInsurancesModel();

            if (id != 0)
            {
                var dservice = _insuranceService;
                viewModel.Insurance = dservice.GetInsuranceById(id).Value;
                // zeker dat ProjectId gezet is
                viewModel.ProjectId = viewModel.Insurance?.ProjectID ?? viewModel.ProjectId;
            }
            else
            {
                // Nieuwe verzekering: project komt via de route mee (staat niet in een bestaand record).
                viewModel.ProjectId = projectid;
            }

            var service = _insuranceService;
            var cservice = _companyService;

            var cresponse = cservice.GetCompanyForSelectByActivity(142);
            if (cresponse.Success) viewModel.Brokers = cresponse.Values;

            var response = service.GetInsuranceCompaniesForSelect();
            if (response.Success) viewModel.Companies = response.Values;

            // Zelfde partial als Add
            return PartialView("_ModalEditInsurance", viewModel);
        }

    }
}
