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
    /// <summary>Documenten van een project (legacy; de gl-v2-versie staat in ProjectenController.DocsV2.cs). Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Docs/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== PROJECT DETAIL DOCS ==========

        [HttpGet]
        [Breadcrumb("Documenten", FromAction = "Detail")]
        //[Breadcrumb("Documenten")]
        public async Task<IActionResult> DetailDocs(int projectid, int? clientaccountid, string? folder = null, string? smart = null, int? unit = null, int? client = null, int? company = null, int? open = null, int? request = null)
        {
            if (ViewData["UseGlV2Layout"] as bool? == true)
                return await DetailDocsV2Get(projectid, clientaccountid, folder, smart, unit, client, company, open, request);

            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            ViewBag.DocWebUrl = Configuration["URL:DocWebUrl"];

            var service = _projectService;
            var cservice = _clientService;
            var model = new DetailDocsModel
            {
                ProjectId = projectid,
                ProjectName = service.GetProjectNameById(projectid),

            };
            var clientsResponse = cservice.GetClientAccountsByProjectIdForSelect(projectid);
            model.Clients = clientsResponse.Success ? clientsResponse.Values : Array.Empty<IdNameBO>();

            // Als er een client is: clientdocs, anders projectdocs
            if (clientaccountid.HasValue && clientaccountid.Value > 0)
            {

                var respClient = service.GetClientDocs(clientaccountid.Value);
                model.ClientAccountId = (int)clientaccountid;
                model.ClientName = cservice.GetClientAccountNameById(model.ClientAccountId);


                if (respClient.Success)
                    model.Docs = respClient.Values;
                else
                    model.Docs = new List<ProjectDocBO>();
            }
            else
            {
                var respProj = service.GetProjectDocs(projectid);
                if (respProj.Success)
                    model.Docs = respProj.Values;
                else
                    model.Docs = new List<ProjectDocBO>();


            }


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
            var lastnode = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailDocs", "Projecten", "Documenten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = lastnode;
            var _psDocs = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectDocs = _psDocs.HasWrite(PermissionCodes.ProjectsDocuments);
            ViewBag.CanDeleteProjectDocs = _psDocs.HasDelete(PermissionCodes.ProjectsDocuments);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Documenten");
            return View(model);
        }

        [HttpGet]
        public IActionResult ModalAddDoc(int id, int? clientaccountid)
        {
            var clientService = _clientService;
            var clientsResponse = clientService.GetClientAccountsByProjectIdForSelect(id);
            var vm = new CPMCore.Models.Projecten.ProjectDocModalVM
            {
                Document = new ProjectDocBO
                {
                    ProjectId = id,
                    ClientAccountId = clientaccountid ?? 0
                },
                Clients = clientsResponse.Success ? clientsResponse.Values : new List<IdNameBO>(),
                Target = clientaccountid.HasValue && clientaccountid.Value > 0 ? "Client" : "Project",
                SelectedClientAccountId = clientaccountid,
                IsEditMode = false
            };
            return PartialView("Modals/_ModalAddDoc", vm);
        }

        [HttpGet]
        public IActionResult ModalEditDoc(int id)
        {
            var projectService = _projectService;
            var response = projectService.GetProjectDoc(id);
            if (!response.Success || response.Value is null)
                return NotFound();

            var doc = response.Value;
            var clientService = _clientService;
            var clientsResponse = clientService.GetClientAccountsByProjectIdForSelect(doc.ProjectId);
            var hasClient = doc.ClientAccountId.HasValue && doc.ClientAccountId.Value > 0;

            var vm = new CPMCore.Models.Projecten.ProjectDocModalVM
            {
                Document = doc,
                Clients = clientsResponse.Success ? clientsResponse.Values : new List<IdNameBO>(),
                Target = hasClient ? "Client" : "Project",
                SelectedClientAccountId = hasClient ? doc.ClientAccountId : null,
                IsEditMode = true
            };

            if (!string.IsNullOrWhiteSpace(doc.Filename))
            {
                vm.DocumentUrl = GetSignedAssetUrlByFileName(doc.Filename, "docs");
                var thumbFileName = BuildDocThumbFileName(doc.Filename);
                vm.ThumbnailUrl = GetSignedAssetUrlByFileName(thumbFileName, "docs") ?? vm.DocumentUrl;
            }

            return PartialView("Modals/_ModalAddDoc", vm);
        }

        [HttpPost]
        public async Task<IActionResult> AddDocument(CPMCore.Models.Projecten.ProjectDocModalVM vm, IFormFile file)
        {
            var model = vm.Document ?? new ProjectDocBO();
            if (!string.Equals(vm.Target, "Client", StringComparison.OrdinalIgnoreCase))
            {
                model.ClientAccountId = null;
            }
            else
            {
                model.ClientAccountId = vm.SelectedClientAccountId;
            }

            if (file == null || file.Length <= 0)
            {
                ModelState.AddModelError("Upload", "U moet een bestand kiezen");
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }
            if (model.ClientAccountId is int i && i <= 0)
                model.ClientAccountId = null;
            var ext = Path.GetExtension(file.FileName) ?? string.Empty;
            var filename = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ext;

            var uploadedFileName = await UploadAssetToStorageAsync(file, "docs");
            if (string.IsNullOrWhiteSpace(uploadedFileName))
            {
                AddMessage("error", "Upload naar storage API is mislukt.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }

            // Zet filename in het BO vóór de service call
            model.Filename = uploadedFileName;

            await GenerateDocThumbnailViaStorageAsync(uploadedFileName);


            var service = _projectService;
            var response = service.InsertUpdateProjectDoc(model);

            var clientIdVal = (model.ClientAccountId is int v && v > 0) ? v : 0;

            if (response.Success)
            {
                AddMessage("success", "Het document is toegevoegd / bijgewerkt", "Gelukt!");
                return clientIdVal > 0
                    ? RedirectToAction("Detail", "Klanten", new { clientid = clientIdVal, projectid = model.ProjectId })
                    : RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }

            AddMessage("error", "Het document is NIET toegevoegd / bijgewerkt, gelieve opnieuw te proberen of contact op te nemen met de administrator", "Fout!");
            return clientIdVal > 0
                ? RedirectToAction("DetailDocs", new { clientaccountid = clientIdVal, projectid = model.ProjectId })
                : RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAllDocThumbnails(int projectid, int? clientaccountid)
        {
            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var writeKey = Configuration["StorageApi:WriteApiKey"];

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(writeKey))
            {
                AddMessage("error", "Storage API is niet correct geconfigureerd.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectid, clientaccountid });
            }

            var endpointCandidates = BuildStorageEndpointCandidates(
                baseUrl,
                "/api/assets/docs/generate-thumbnails",
                "/assets/docs/generate-thumbnails",
                "/docs/generate-thumbnails");

            try
            {
                using var httpClient = CreateStorageHttpClient(writeKey, TimeSpan.FromMinutes(5));

                HttpStatusCode? lastStatusCode = null;
                string lastResponseBody = string.Empty;
                string lastEndpoint = endpointCandidates.FirstOrDefault() ?? baseUrl;

                foreach (var endpoint in endpointCandidates)
                {
                    var response = await httpClient.PostAsync(endpoint, content: null);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (IsLikelyAuthRedirectOrLoginPage(response, responseBody))
                    {
                        _logger.LogError("Bulk doc thumbnail generation hit auth/login page. Endpoint: {Endpoint}. Status: {StatusCode}. BodySnippet: {BodySnippet}",
                            endpoint, (int)response.StatusCode, responseBody.Length > 220 ? responseBody[..220] : responseBody);

                        AddMessage("error", "Storage API call werd omgeleid naar een loginpagina. Controleer `StorageApi:BaseUrl` (moet de storage service URL zijn, niet CPM) en reverse proxy authenticatie.", "Fout!");
                        return RedirectToAction("DetailDocs", new { projectid, clientaccountid });
                    }

                    if (response.IsSuccessStatusCode && IsValidBulkThumbnailResponse(responseBody))
                    {
                        AddMessage("success", "Thumbnail generatie voor alle documenten is gestart/uitgevoerd.", "Gelukt!");
                        return RedirectToAction("DetailDocs", new { projectid, clientaccountid });
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Bulk doc thumbnail generation got a non-storage success response. Endpoint: {Endpoint}. Body: {Body}", endpoint, responseBody);
                    }

                    lastStatusCode = response.StatusCode;
                    lastResponseBody = responseBody;
                    lastEndpoint = endpoint;

                    _logger.LogWarning("Bulk doc thumbnail generation attempt failed. Status: {StatusCode}. Endpoint: {Endpoint}. Body: {Body}",
                        (int)response.StatusCode, endpoint, responseBody);

                    if (response.StatusCode != HttpStatusCode.NotFound)
                    {
                        break;
                    }
                }

                var shortBody = string.IsNullOrWhiteSpace(lastResponseBody)
                    ? "geen foutdetails ontvangen"
                    : lastResponseBody.Length > 220
                        ? $"{lastResponseBody[..220]}..."
                        : lastResponseBody;

                AddMessage("error",
                    $"Thumbnail generatie is mislukt (HTTP {(int)(lastStatusCode ?? HttpStatusCode.InternalServerError)} - {lastStatusCode}). Endpoint: {lastEndpoint}. Details: {shortBody}",
                    "Fout!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling storage bulk thumbnail endpoints. BaseUrl: {BaseUrl}", baseUrl);
                AddMessage("error", $"Thumbnail generatie kon niet worden gestart: {ex.Message}", "Fout!");
            }

            return RedirectToAction("DetailDocs", new { projectid, clientaccountid });
        }



        [HttpPost]
        public IActionResult EditDocument(CPMCore.Models.Projecten.ProjectDocModalVM vm)
        {
            var model = vm.Document ?? new ProjectDocBO();
            if (model.Docid <= 0)
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });

            var service = _projectService;
            var existingResp = service.GetProjectDoc(model.Docid);
            if (!existingResp.Success || existingResp.Value is null)
            {
                AddMessage("error", "Het document kon niet worden gevonden.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
            }

            var existingDoc = existingResp.Value;
            model.ProjectId = existingDoc.ProjectId;
            model.Filename = existingDoc.Filename;

            if (!string.Equals(vm.Target, "Client", StringComparison.OrdinalIgnoreCase))
                model.ClientAccountId = null;
            else
                model.ClientAccountId = vm.SelectedClientAccountId;

            if (model.ClientAccountId is int i && i <= 0)
                model.ClientAccountId = null;

            var response = service.InsertUpdateProjectDoc(model);
            if (response.Success)
            {
                AddMessage("success", "Het document is bijgewerkt.", "Gelukt!");
            }
            else
            {
                AddMessage("error", "Het document kon niet worden bijgewerkt.", "Fout!");
            }

            var clientIdVal = (model.ClientAccountId is int v && v > 0) ? v : 0;
            return clientIdVal > 0
                ? RedirectToAction("DetailDocs", new { clientaccountid = clientIdVal, projectid = model.ProjectId })
                : RedirectToAction("DetailDocs", new { projectid = model.ProjectId });
        }

        [HttpGet]
        public IActionResult ViewDoc(int id)
        {
            var signedUrl = GetSignedAssetUrl(id, "docs");
            if (string.IsNullOrWhiteSpace(signedUrl)) return NotFound();
            return Redirect(signedUrl);
        }

        [HttpGet]
        public IActionResult DownloadDoc(int id, string? asName = null)
        {
            var signedUrl = GetSignedAssetUrl(id, "docs");
            if (string.IsNullOrWhiteSpace(signedUrl)) return NotFound();
            return Redirect(signedUrl);
        }


        [HttpGet]
        public IActionResult ModalDeleteDoc(int id)
        {
            var vm = new ProjectDocBO();
            if (id != 0)
            {
    
                var resp = _projectService.GetProjectDoc(id);
                if (resp.Success && resp.Value is not null)
                    vm = resp.Value;
            }
            return PartialView("_ModalDeleteDoc", vm);
        }

        [CPMCore.Filters.PermissionDelete(PermissionCodes.ProjectsDocuments)]
        public ActionResult DeleteDoc(int id, int projectId)
        {
            if (id == 0 || projectId == 0)
                return RedirectToAction("DetailDocs", new { projectId });

            var resp = _projectService.DeleteProjectDoc(new List<int> { id });
            if (!resp.Success)
            {
                AddMessage("error", "Het document is niet verwijderd.", "Fout!");
                return RedirectToAction("DetailDocs", new { projectId });
            }

            AddMessage("success", "Het document is verwijderd.", "Geslaagd!");
            return RedirectToAction("DetailDocs", new { projectId });
        }

    }
}
