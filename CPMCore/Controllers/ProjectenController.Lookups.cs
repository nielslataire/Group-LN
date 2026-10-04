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
    /// <summary>Bankwaarborg-document van een contract plus de gedeelde AJAX-opzoekingen (landen, postcodes, bedrijven, contacten, weerstations) die meerdere schermen gebruiken; geen eigen views. Opgesplitst uit ProjectenController.cs (okt. 2026, "views/controllers/models structureren") — views in Views/Projecten/Lookups/. Zelfde partial class: alle private velden/services van ProjectenController.cs blijven gewoon bruikbaar.</summary>
    public partial class ProjectenController
    {
        // ── Bankwaarborg-document ─────────────────────────────────────────────────
        private static readonly string[] _guaranteeDocExtensions = { ".pdf", ".jpg", ".jpeg" };

        private static bool ValidateGuaranteeDoc(IFormFile file, out string error)
        {
            error = null;
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (!_guaranteeDocExtensions.Contains(ext))
            {
                error = "Het waarborgdocument moet een pdf, jpg of jpeg zijn.";
                return false;
            }
            if (file.Length > 10 * 1024 * 1024)
            {
                error = "Het waarborgdocument mag maximaal 10 MB groot zijn.";
                return false;
            }
            return true;
        }

        [HttpGet]
        [CPMCore.Filters.PermissionRead(PermissionCodes.ProjectsSuppliers)]
        public IActionResult GuaranteeDoc(int contractid)
        {
            var contract = _projectService.GetContract(contractid);
            var fileName = contract.Success ? contract.Value?.GuaranteeDocFilename : null;
            if (string.IsNullOrWhiteSpace(fileName))
                return NotFound();

            var signedUrl = GetSignedAssetUrlByFileName(fileName, "guarantees");
            if (string.IsNullOrWhiteSpace(signedUrl))
                return NotFound();
            return Redirect(signedUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsSuppliers)]
        public async Task<IActionResult> RemoveGuaranteeDoc(int contractid, int projectid)
        {
            var response = _projectService.GetContract(contractid);
            if (response.Success && response.Value != null)
            {
                var bo = response.Value;
                var oldFile = bo.GuaranteeDocFilename;
                bo.GuaranteeDocFilename = null;
                bo.GuaranteeDocUploadedAt = null;
                var saved = _projectService.InsertUpdateProjectContract(bo);
                if (saved.Success)
                    await DeleteGuaranteeDocFromStorageAsync(oldFile);
                AddMessage(saved.Success ? "success" : "error",
                    saved.Success ? "Het waarborgdocument is verwijderd." : "Het waarborgdocument kon niet verwijderd worden.",
                    saved.Success ? "Geslaagd!" : "Fout!");
            }
            return RedirectToAction(nameof(EditContract), new { projectid, contractid });
        }

        // Best-effort verwijdering van een waarborgdocument uit de storage.
        private async Task DeleteGuaranteeDocFromStorageAsync(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            var baseUrl = Configuration["StorageApi:BaseUrl"]?.TrimEnd('/');
            var writeKey = Configuration["StorageApi:WriteApiKey"];
            if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(writeKey))
                await DeleteStorageFileAsync(baseUrl, writeKey, "guarantees", fileName);
        }

        [HttpPost]
        public JsonResult GetCountryIsoCode(int countryid)
        {
            var pservice = _countryService;
            var presponse = pservice.GetCountryById(countryid);
            var country = presponse.Success ? presponse.Values.FirstOrDefault() : null;
            return Json(country?.ISOCode ?? string.Empty);
        }
        [HttpPost]
        public async Task<JsonResult> GetPostcodesByCountry(string term, int countryId)
        {
            var pservice = _postalcodeService;
            var presponse = await pservice.GetPostalcodeByCountryAndSearchstring(countryId, term ?? string.Empty);

            var list = new List<SelectBO>();
            if (presponse.Success && presponse.Values is not null)
            {
                foreach (var selectedPostalcode in presponse.Values)
                {
                    list.Add(new SelectBO
                    {
                        id = selectedPostalcode.PostcodeId ?? 0,
                        text = $"{selectedPostalcode.Postcode} - {selectedPostalcode.Gemeente}"
                    });
                }
            }

            return Json(list);
        }
        [HttpPost]
        public JsonResult GetCompanys(string term, bool activeOnly = false)
        {
            var pservice = _companyService;
            var presponse = pservice.GetCompanyForSearchList(term, activeOnly);
            var iList = new List<SelectBO>();

            if (presponse.Success)
            {
                iList = presponse.Values;
            }

            return Json(iList);
        }
        [HttpPost]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsSuppliers)]
        public JsonResult GetCompanyContacts(int companyid)
        {
            var contacts = GetSiteManagersForCompany(companyid)
                .Select(x => new SelectBO { id = x.ID, text = x.Display })
                .ToList();
            return Json(contacts);
        }
        [HttpPost]
        public JsonResult GetCompanyContractDefaults(int companyid)
        {
            var last = _db.Contract
                .Where(c => c.CompanyId == companyid)
                .OrderByDescending(c => c.Id)
                .Select(c => new
                {
                    c.VatPercentage,
                    c.PaymentTerm,
                    c.GuaranteeType,
                    c.GuaranteePercentage
                })
                .FirstOrDefault();

            if (last == null)
            {
                return Json(new { found = false });
            }

            return Json(new
            {
                found = true,
                vatPercentage = last.VatPercentage,
                paymentTerm = last.PaymentTerm,
                guaranteeType = last.GuaranteeType,
                guaranteePercentage = last.GuaranteePercentage
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.ProjectsSuppliers)]
        public async Task<JsonResult> AddCompanyContactQuick(int companyId, string name, string email, string phone)
        {
            if (companyId <= 0 || string.IsNullOrWhiteSpace(name))
            {
                Response.StatusCode = 400;
                return Json(new { success = false, error = "Naam is verplicht." });
            }

            var entity = new CompanyContacts
            {
                CompanyId = companyId,
                ContactNaam = name.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Gsm = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
            };

            _db.CompanyContacts.Add(entity);
            await _db.SaveChangesAsync();

            return Json(new { success = true, id = entity.ContactId, text = entity.ContactNaam });
        }
        [HttpPost]
        public PartialViewResult AddContractActivitySelection(int companyId, int activityId, string activityName)
        {
            var alreadyLinked = _db.CompanyInfo
                .Where(c => c.CompanyId == companyId)
                .Any(c => c.Activity.Any(a => a.ActivityId == activityId));

            if (!alreadyLinked)
            {
                _companyService.AddCompanyActivity(companyId, activityId);
            }

            var nContractActivity = new ContractActivityBO();
            var nActivity = new ActivityBO { ID = activityId, Name = activityName };
            nContractActivity.Activity = nActivity;
            ViewData["mode"] = "add";
            return PartialView("_ActivityRow", nContractActivity);
        }
        [HttpPost]
        public JsonResult GetWheaterstations(string term)
        {
            var pservice = _projectService;
            var presponse = pservice.GetWheaterstations(term ?? string.Empty);
            var list = new List<SelectBO>();

            if (presponse.Success && presponse.Values is not null)
            {
                foreach (var station in presponse.Values)
                {
                    list.Add(new SelectBO
                    {
                        id = station.Id,
                        text = station.Name,
                        extra = station.Visible?.ToString()
                    });
                }
            }

            return Json(list);
        }
        public string GetSlugForPostcodeId(int id, string name)
        {
            var city = string.Empty;
            if (id != 0)
            {
                var cityService = _postalcodeService;
                var postalcode = cityService.GetPostalcodeById(id);
                if (postalcode.Success && postalcode.Value is not null)
                {
                    city = postalcode.Value.Gemeente;
                }
            }

            var projectService = _projectService;
            return projectService.GenerateSlug($"{name} {city}".Trim());
        }
        public class Select2DTO
        {
            // Select2 expects objects with 'id' and 'text' fields
            public int id { get; set; }
            public string text { get; set; }
            public string group { get; set; }
        }

    }
}
