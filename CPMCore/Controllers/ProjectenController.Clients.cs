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
    /// <summary>Klanten en contacten van een project. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Clients/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ========== PROJECT DETAIL KLANTEN ==========

        [HttpGet]
        //[Breadcrumb("Klanten")]
        [Breadcrumb("Klanten", FromAction = "Detail")]
        public ActionResult DetailClients(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            SetPageHeader("ph ph-users", "Klanten");
            var _ps = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectCustomers = _ps.HasWrite(PermissionCodes.ProjectsCustomers);
            ViewBag.CanDeleteProjectCustomers = _ps.HasDelete(PermissionCodes.ProjectsCustomers);
            DetailClientsModel model = new DetailClientsModel();
            var service = _clientService;
            var service2 = _projectService;
            var response = service.GetClientAccountsByProjectIdWithUnits(projectid);
            if ((response.Success))
                model.ClientAccounts = response.Values;

            if (model.ClientAccounts.SelectMany(m => m.Units.Where(i => i.Type.GroupId == 1)).Count() > 0)
                model.ClientAccounts = model.ClientAccounts.OrderBy(m => m.Units.Where(a => a.Type.GroupId == 1).Count() > 0 ? m.Units.Where(a => a.Type.GroupId == 1).FirstOrDefault().Name : "", new ServiceCore.Helpers.AlphanumComparator()).ToList();
            model.ProjectId = projectid;
            model.ProjectName = service2.GetProjectNameById(projectid);
            // gl-v2 (DetailClientsV2, design-handoff 12d): wooneenheden/commerciële ruimtes zonder klant
            // ("Nog geen klant"-rij) — GetClientAccountsByProjectIdWithUnits hierboven geeft er per
            // definitie geen terug, dus apart opgehaald via dezelfde dienst als Projecten/DetailV2's
            // eigen Eenheden-tabel.
            var unitsResp = _unitService.GetUnitsWithAttachedByProjectId(projectid);
            if (unitsResp.Success && unitsResp.Values is not null)
            {
                model.AvailableUnits = unitsResp.Values
                    .Select(u => u.Unit)
                    .Where(u => (u.Type.GroupId == 1 || u.Type.GroupId == 4) && u.ClientAccountId == null)
                    .OrderBy(u => u.Name, new ServiceCore.Helpers.AlphanumComparator())
                    .ToList();
            }
            // gl-v2: enkel voor GlV2ProjectMenuVm.IsCoordinationProject (zelfde vlag als Detail's eigen
            // model.Project.IsOnlyCoordinationProject) — het inner menu op deze pagina heeft dezelfde
            // "geen foto's/nieuws/contacten"-uitzondering nodig als op Projecten/DetailV2.
            var projectResponse = service2.GetProjectByID(projectid);
            model.IsCoordinationProject = projectResponse.Success && projectResponse.Value?.IsOnlyCoordinationProject == true;
            //BREADCRUMBS
            // Kruimelpad stopt bij de projectnaam (wáár dit zit) i.p.v. nog een "Klanten"-knoop toe te
            // voegen die de paginatitel herhaalt — zelfde design-handoff punt 13, regel 2-fix als
            // Projecten/Detail hierboven (en Invoices/DetailV2, Projecten/IncommingInvoiceDetailV2):
            // het laatste kruimelitem is nooit de titel zelf. "Klanten" staat dus enkel nog als titel
            // (DetailClientsV2.cshtml zet ViewData["Title"]), niet meer ook als laatste kruimelknoop.
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
            ViewData["BreadcrumbNode"] = projectDetail;
            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Klanten");
            return View(ViewData["UseGlV2Layout"] as bool? == true ? "DetailClientsV2" : "DetailClients", model);
        }


        [HttpGet]
        [Breadcrumb("Contacten", FromAction = "Detail")]
        public ActionResult DetailContacts(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var service = _projectService;
            var model = new DetailContactsModel
            {
                ProjectId = projectid,
                ProjectName = service.GetProjectNameById(projectid)
            };

            var response = service.GetProjectContactRequests(projectid);
            if (response.Success) model.Contacts = response.Values;

            var contacts = model.Contacts ?? new List<ContactRequestBO>();
            model.ContactGroups = BuildContactGroups(contacts);
            model.Stats = BuildContactStats(model.ContactGroups, contacts);

            var lastEmailSentByContact = _emailSendLogService.GetLatestPerContact(projectid);
            foreach (var group in model.ContactGroups)
            {
                var emailKey = (group.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(emailKey) && lastEmailSentByContact.TryGetValue(emailKey, out var lastSent))
                {
                    group.LastEmailSentAt = lastSent.VerzondenOp;
                    group.LastEmailSentBy = lastSent.VerzondenDoorNaam;
                }
            }

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
            var projectContacts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContacts", "Projecten", "Contacten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = projectContacts;
            var _psContacts = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectContacts = _psContacts.HasWrite(PermissionCodes.ProjectsContacts);
            ViewBag.CanDeleteProjectContacts = _psContacts.HasDelete(PermissionCodes.ProjectsContacts);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Contacten");
            return View(model);
        }

        [HttpGet]
        [Breadcrumb("Contactdetails", FromAction = "DetailContacts")]
        public ActionResult ContactDetails(int projectid, string email, string fullname, string phone)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var service = _projectService;
            var response = service.GetProjectContactRequests(projectid);
            var contacts = response.Success ? response.Values : new List<ContactRequestBO>();

            var groupContacts = FilterContactGroup(contacts, email, fullname, phone);
            var groupModel = BuildContactGroup(groupContacts);

            var model = new ContactDetailsModel
            {
                ProjectId = projectid,
                ProjectName = service.GetProjectNameById(projectid),
                Contact = groupModel,
                Requests = groupContacts.OrderByDescending(c => c.CreatedAt).ToList(),
                NewAction = new ContactActionInputModel
                {
                    ProjectId = projectid,
                    Email = groupModel?.Email,
                    Fullname = groupModel?.DisplayName,
                    Phone = groupModel?.Phone,
                    ActionDate = DateTime.Now,
                    ActionTime = DateTime.Now.TimeOfDay
                },
                NewStatus = new ContactStatusInputModel
                {
                    ProjectId = projectid,
                    Email = groupModel?.Email,
                    Fullname = groupModel?.DisplayName,
                    Phone = groupModel?.Phone,
                    StatusDate = DateTime.Now,
                    StatusTime = DateTime.Now.TimeOfDay
                }
            };

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
            var projectContacts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContacts", "Projecten", "Contacten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var contactDetails = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("ContactDetails", "Projecten", "Contactdetails")
            {
                Parent = projectContacts,
                RouteValues = new { projectid = projectid, email, fullname, phone }
            };
            ViewData["BreadcrumbNode"] = contactDetails;
            var _psContactDet = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            ViewBag.CanWriteProjectContacts = _psContactDet.HasWrite(PermissionCodes.ProjectsContacts);

            SetPageHeader("bx bx-building-house", $"{model.ProjectName} - Contactdetails");
            return View(model);
        }
        [HttpGet]
        [Breadcrumb("Contact toevoegen", FromAction = "DetailContacts")]
        public ActionResult AddContact(int projectid)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var service = _projectService;
            var projectName = service.GetProjectNameById(projectid);

            var model = new ContactAddModel
            {
                ProjectId = projectid,
                ProjectName = projectName,
                ContactDate = DateTime.Today
            };

            var Index = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Index", "Projecten", "Projecten")
            {
                Parent = Index,
            };
            var projectDetail = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("Detail", "Projecten", projectName)
            {
                Parent = projectenIndex,
                RouteValues = new { projectid = projectid }
            };
            var projectContacts = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("DetailContacts", "Projecten", "Contacten")
            {
                Parent = projectDetail,
                RouteValues = new { projectid = projectid }
            };
            var addContact = new SmartBreadcrumbs.Nodes.MvcBreadcrumbNode("AddContact", "Projecten", "Contact toevoegen")
            {
                Parent = projectContacts,
                RouteValues = new { projectid = projectid }
            };
            ViewData["BreadcrumbNode"] = addContact;

            SetPageHeader("bx bx-building-house", $"{projectName} - Contact toevoegen");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddContact(ContactAddModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var request = new ContactRequestBO
            {
                ProjectId = model.ProjectId,
                Firstname = NormalizeContactName(model.Firstname),
                Lastname = NormalizeContactName(model.Lastname),
                Fullname = BuildFullname(model.Firstname, model.Lastname),
                Email = model.Email,
                Phone = model.Phone,
                RequestType = "Contact",
                Question = model.Comment,
                CreatedAt = model.ContactDate?.Date ?? DateTime.Now,
                SourceSite = model.ContactMethod,
                Origin = User?.Identity?.Name ?? "Onbekende gebruiker"
            };

            var service = _projectService;
            var response = service.InsertProjectContactRequest(request);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Contact toegevoegd" : "Contact niet toegevoegd", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
        }
        [HttpGet]
        [Breadcrumb("Contact bewerken", FromAction = "DetailContacts")]
        public ActionResult EditContact(int projectid, string email, string fullname, string phone)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            var service = _projectService;
            var response = service.GetProjectContactRequests(projectid);
            var contacts = response.Success ? response.Values : new List<ContactRequestBO>();
            var groupContacts = FilterContactGroup(contacts, email, fullname, phone);
            var latestContact = groupContacts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
            var fullName = GetContactDisplayName(latestContact);

            var model = new ContactEditModel
            {
                ProjectId = projectid,
                Email = email,
                Fullname = fullname ?? fullName,
                Phone = phone ?? latestContact?.Phone,
                Firstname = latestContact?.Firstname,
                Lastname = latestContact?.Lastname,
                NewEmail = latestContact?.Email,
                NewPhone = latestContact?.Phone,
                ContactDate = latestContact?.CreatedAt.Date,
                ContactMethod = latestContact?.SourceSite
            };

            SetPageHeader("bx bx-building-house", $"{service.GetProjectNameById(projectid)} - Contact bewerken");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditContact(ContactEditModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var service = _projectService;
            var updatedValues = new ContactRequestBO
            {
                Firstname = NormalizeContactName(model.Firstname),
                Lastname = NormalizeContactName(model.Lastname),
                Fullname = BuildFullname(model.Firstname, model.Lastname),
                Email = model.NewEmail,
                Phone = model.NewPhone,
                CreatedAt = model.ContactDate?.Date ?? default,
                SourceSite = model.ContactMethod
            };

            var response = service.UpdateContactRequestGroup(model.ProjectId, model.Email, model.Fullname, model.Phone, updatedValues);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Contact bijgewerkt" : "Contact niet bijgewerkt", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteContact(ContactDeleteModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var service = _projectService;
            var response = service.DeleteContactRequestGroup(model.ProjectId, model.Email, model.Fullname, model.Phone);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Contact verwijderd" : "Contact niet verwijderd", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
        }

        [HttpGet]
        public IActionResult ModalSendContactEmail(int projectid, string email, string fullname)
        {
            var templatesResponse = _emailTemplateService.GetAll(alleenActief: true);
            var vm = new SendContactEmailModalVM
            {
                ProjectId = projectid,
                Email = email,
                Fullname = fullname,
                Templates = (templatesResponse.Values ?? new List<EmailTemplateBO>())
                    .Select(t => new SelectListItem(t.Naam, t.ID.ToString()))
                    .ToList()
            };

            return PartialView("Modals/_ModalSendEmail", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendContactEmail(SendContactEmailInputModel model)
        {
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var userId = User.GetCpmUserId();
            if (userId == null)
                return Forbid();

            var templateResponse = _emailTemplateService.GetById(model.TemplateId);
            if (templateResponse.HasErrors || templateResponse.Value == null)
            {
                AddMessage("error", "Template niet gevonden.", "Fout!");
                return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
            }

            var template = templateResponse.Value;
            var projectName = _projectService.GetProjectNameById(model.ProjectId) ?? string.Empty;
            var gemeente = _uow.Projects.GetNoTracking()
                .Where(p => p.ProjectId == model.ProjectId)
                .Select(p => p.PostalCode != null ? p.PostalCode.Gemeente : null)
                .FirstOrDefault() ?? string.Empty;
            var voornaam = (model.Fullname ?? string.Empty).Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;

            string ReplaceTokens(string text) => (text ?? string.Empty)
                .Replace("{Voornaam}", voornaam)
                .Replace("{Naam}", model.Fullname ?? string.Empty)
                .Replace("{ProjectNaam}", projectName)
                .Replace("{Gemeente}", gemeente);

            var subject = ReplaceTokens(template.Onderwerp);
            var body = ReplaceTokens(template.BodyHtml);
            var userName = User.GetCpmDisplayName() ?? User?.Identity?.Name ?? "Onbekende gebruiker";

            if (model.IncludeSignature)
            {
                var signatureResponse = _userSignatureService.GetByUserId(userId.Value);
                var signatureHtml = signatureResponse.Value?.SignatureHtml;
                if (!string.IsNullOrWhiteSpace(signatureHtml))
                {
                    // Belangrijk: de handtekening (mogelijk een volledig Word/Outlook-document
                    // met eigen <html>/<body>) moet als op zichzelf staand fragment opgeschoond
                    // worden vóórdat ze aan de templatetekst geplakt wordt. Zou dit pas gebeuren
                    // op de samengevoegde string, dan zou de "haal de inhoud tussen <body>/</body>
                    // op"-stap de templatetekst die vóór dat ingesloten document staat weggooien.
                    var cleanedSignature = CPMCore.Service.WordHtmlSanitizer.Clean(signatureHtml);
                    body += "<br/><br/>" + cleanedSignature;
                }
            }

            try
            {
                _emailSender.SendEmailAsync(model.Email, subject, body).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kon mail niet versturen naar {Email} voor project {ProjectId}", model.Email, model.ProjectId);
                AddMessage("error", "Mail kon niet verstuurd worden.", "Fout!");
                return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
            }

            _emailSendLogService.Log(new EmailSendLogBO
            {
                ProjectId = model.ProjectId,
                ContactEmail = model.Email,
                ContactNaam = model.Fullname,
                EmailTemplateId = template.ID,
                TemplateNaam = template.Naam,
                Onderwerp = subject,
                VerzondenDoorUserId = userId.Value,
                VerzondenDoorNaam = userName
            });

            AddMessage("success", "Mail verstuurd.", "Geslaagd!");
            return RedirectToAction("DetailContacts", new { projectid = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddContactAction(ContactActionInputModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var actionTime = model.ActionTime == default ? DateTime.Now.TimeOfDay : model.ActionTime;
            var actionDateTime = model.ActionDate.Date.Add(actionTime);
            var userName = User?.Identity?.Name ?? "Onbekende gebruiker";
            var request = new ContactRequestBO
            {
                ProjectId = model.ProjectId,
                Email = model.Email,
                Fullname = model.Fullname,
                Phone = model.Phone,
                RequestType = InternalActionRequestType,
                Subject = model.ActionType,
                Question = model.Comment,
                CreatedAt = actionDateTime,
                SourceSite = InternalSourceSite,
                Origin = userName
            };

            var service = _projectService;
            var response = service.InsertProjectContactRequest(request);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Actie toegevoegd" : "Actie niet toegevoegd", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("ContactDetails", new { projectid = model.ProjectId, email = model.Email, fullname = model.Fullname, phone = model.Phone });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddContactStatus(ContactStatusInputModel model)
        {
            ViewBag.sidebarcollapsed = "sidebar-left-collapsed";
            if (model == null)
                return RedirectToAction("DetailContacts", new { projectid = model?.ProjectId });

            var statusTime = model.StatusTime == default ? DateTime.Now.TimeOfDay : model.StatusTime;
            var statusDateTime = model.StatusDate.Date.Add(statusTime);
            var userName = User?.Identity?.Name ?? "Onbekende gebruiker";
            var request = new ContactRequestBO
            {
                ProjectId = model.ProjectId,
                Email = model.Email,
                Fullname = model.Fullname,
                Phone = model.Phone,
                RequestType = StatusUpdateRequestType,
                Subject = model.Status,
                Question = model.Comment,
                CreatedAt = statusDateTime,
                SourceSite = InternalSourceSite,
                Origin = userName
            };

            var service = _projectService;
            var response = service.InsertProjectContactRequest(request);
            AddMessage(response.Success ? "success" : "error", response.Success ? "Status bijgewerkt" : "Status niet bijgewerkt", response.Success ? "Geslaagd!" : "Fout!");

            return RedirectToAction("ContactDetails", new { projectid = model.ProjectId, email = model.Email, fullname = model.Fullname, phone = model.Phone });
        }

        private const string InternalActionRequestType = "Interne actie";
        private const string StatusUpdateRequestType = "Status update";
        private const string InternalSourceSite = "CPM";
        private const string InternalOrigin = "CPM";

        private static List<ContactGroupModel> BuildContactGroups(List<ContactRequestBO> contacts)
        {
            return contacts
                .GroupBy(BuildContactKey)
                .Select(group => BuildContactGroup(group.ToList()))
                .Where(group => group != null)
                .OrderByDescending(group => group.LatestContactAt)
                .ToList();
        }

        private static ContactStatsModel BuildContactStats(List<ContactGroupModel> groups, List<ContactRequestBO> allContacts)
        {
            var stats = new ContactStatsModel();
            var now = DateTime.Now;
            var groupedContacts = allContacts.GroupBy(BuildContactKey)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.CreatedAt).ToList());

            stats.TotalContacts = groups.Count;
            stats.ActiveContacts = groups.Count(g => string.IsNullOrWhiteSpace(g.LatestStatus) || string.Equals(g.LatestStatus, "Actief", StringComparison.OrdinalIgnoreCase));
            stats.NewContactsWeek = groups.Count(g => GetFirstContactDate(groupedContacts, g.GroupKey) >= now.Date.AddDays(-7));
            stats.NewContactsMonth = groups.Count(g => GetFirstContactDate(groupedContacts, g.GroupKey) >= now.Date.AddDays(-30));

            stats.ConversionRate = stats.TotalContacts > 0
                ? Math.Round((decimal)stats.ActiveContacts / stats.TotalContacts * 100, 2)
                : 0;

            var responseRateData = BuildResponseRate(groups, allContacts);
            stats.ResponseRate = responseRateData.TotalWithInternalAction > 0
                ? Math.Round((decimal)responseRateData.TotalWithResponse / responseRateData.TotalWithInternalAction * 100, 2)
                : 0;

            return stats;
        }

        private static (int TotalWithInternalAction, int TotalWithResponse) BuildResponseRate(List<ContactGroupModel> groups, List<ContactRequestBO> allContacts)
        {
            var groupedContacts = allContacts.GroupBy(BuildContactKey)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).ToList());

            var totalWithInternal = 0;
            var totalWithResponse = 0;

            foreach (var group in groups)
            {
                if (!groupedContacts.TryGetValue(group.GroupKey, out var contactRequests))
                    continue;

                var internalActions = contactRequests
                    .Where(c => IsInternalRequestType(c.RequestType) && string.Equals(c.RequestType, InternalActionRequestType, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();

                if (!internalActions.Any())
                    continue;

                totalWithInternal += 1;
                var latestActionDate = internalActions.First().CreatedAt;

                var responseAfterAction = contactRequests.Any(c => !IsInternalRequestType(c.RequestType) && c.CreatedAt > latestActionDate);
                if (responseAfterAction)
                    totalWithResponse += 1;
            }

            return (totalWithInternal, totalWithResponse);
        }

        private static DateTime GetFirstContactDate(Dictionary<string, List<ContactRequestBO>> groupedContacts, string groupKey)
        {
            if (groupedContacts == null || string.IsNullOrWhiteSpace(groupKey) || !groupedContacts.TryGetValue(groupKey, out var contacts))
                return DateTime.MinValue;

            var firstExternal = contacts.FirstOrDefault(c => !IsInternalRequestType(c.RequestType));
            return firstExternal?.CreatedAt ?? contacts.FirstOrDefault()?.CreatedAt ?? DateTime.MinValue;
        }

        private static ContactGroupModel BuildContactGroup(List<ContactRequestBO> contacts)
        {
            if (contacts == null || contacts.Count == 0)
                return null;

            var latestExternal = contacts
                .Where(c => !IsInternalRequestType(c.RequestType))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            var latestOverall = contacts.OrderByDescending(c => c.CreatedAt).First();
            var referenceContact = latestExternal ?? latestOverall;
            var displayName = GetContactDisplayName(referenceContact);

            var latestStatus = contacts
                .Where(c => string.Equals(c.RequestType, StatusUpdateRequestType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            return new ContactGroupModel
            {
                GroupKey = BuildContactKey(referenceContact),
                DisplayName = displayName,
                Email = referenceContact.Email,
                Phone = referenceContact.Phone,
                LatestContactAt = (latestExternal ?? latestOverall).CreatedAt,
                LatestRequestType = referenceContact.RequestType,
                LatestSourceSite = referenceContact.SourceSite,
                LatestOrigin = referenceContact.Origin,
                TotalRequests = contacts.Count,
                LatestStatus = latestStatus?.Subject,
                LatestStatusComment = latestStatus?.Question,
                LatestStatusAt = latestStatus?.CreatedAt
            };
        }

        private static List<ContactRequestBO> FilterContactGroup(List<ContactRequestBO> contacts, string email, string fullname, string phone)
        {
            var normalizedEmail = NormalizeContactValue(email);
            var normalizedName = NormalizeContactValue(fullname);
            var normalizedPhone = NormalizeContactValue(phone);

            return contacts
                .Where(c => MatchesContactGroup(c, normalizedEmail, normalizedName, normalizedPhone))
                .ToList();
        }

        private static bool MatchesContactGroup(ContactRequestBO contact, string normalizedEmail, string normalizedName, string normalizedPhone)
        {
            var contactEmail = NormalizeContactValue(contact.Email);
            var contactName = NormalizeContactValue(GetContactDisplayName(contact));
            var contactPhone = NormalizeContactValue(contact.Phone);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
                return contactEmail == normalizedEmail;

            return contactName == normalizedName && contactPhone == normalizedPhone;
        }

        private static string BuildContactKey(ContactRequestBO contact)
        {
            var email = NormalizeContactValue(contact?.Email);
            if (!string.IsNullOrWhiteSpace(email))
                return email;
            var name = NormalizeContactValue(GetContactDisplayName(contact));
            var phone = NormalizeContactValue(contact?.Phone);
            return $"{email}|{name}|{phone}";
        }

        private static string GetContactDisplayName(ContactRequestBO contact)
        {
            if (contact == null)
                return "-";

            var fullname = !string.IsNullOrWhiteSpace(contact.Fullname)
                  ? contact.Fullname
                  : $"{contact.Firstname} {contact.Lastname}".Trim();
            if (!string.IsNullOrWhiteSpace(fullname))
                return ToTitleCase(fullname);


            var fallback = $"{contact.Firstname} {contact.Lastname}".Trim();
            return string.IsNullOrWhiteSpace(fallback) ? "-" : fallback;
        }

        private static bool IsInternalRequestType(string requestType)
        {
            return string.Equals(requestType, InternalActionRequestType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(requestType, StatusUpdateRequestType, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeContactValue(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        private static string NormalizeContactName(string value)
          => string.IsNullOrWhiteSpace(value) ? string.Empty : ToTitleCase(value.Trim());

        private static string BuildFullname(string firstname, string lastname)
        {
            var normalizedFirstname = NormalizeContactName(firstname);
            var normalizedLastname = NormalizeContactName(lastname);
            return $"{normalizedFirstname} {normalizedLastname}".Trim();
        }

        private static string ToTitleCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var culture = CultureInfo.CurrentCulture;
            return culture.TextInfo.ToTitleCase(value.ToLower(culture));
        }
    }
}
