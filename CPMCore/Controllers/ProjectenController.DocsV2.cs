using System.Net;
using BOCore;
using Microsoft.EntityFrameworkCore;
using CPMCore.Helpers;
using CPMCore.Models.Projecten;
using CPMCore.Services.Security;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers
{
    /// <summary>Projecten/DetailDocsV2 — documenten van een project (design-handoff 17a-17e).
    /// Alle actienamen bevatten "Document": PermissionResolver koppelt die aan ProjectsDocuments
    /// (GET = lezen, POST = schrijven, "Delete"/"Remove" = verwijderen), zonder extra attributen.</summary>
    public partial class ProjectenController
    {
        private IDocumentService DocsSvc => HttpContext.RequestServices.GetRequiredService<IDocumentService>();
        private string? DocUserId => User.FindFirst(CpmClaims.UserId)?.Value;
        private string? DocUserName => User.GetCpmDisplayName();

        private bool DocCanWrite => HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsDocuments);
        private bool DocCanDelete => HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasDelete(PermissionCodes.ProjectsDocuments);

        private IActionResult DocJson(DocResult r)
        {
            if (r.Ok && !string.IsNullOrWhiteSpace(r.Message)) AddMessage("success", r.Message!, "Gelukt!");
            else if (!r.Ok) AddMessage("error", r.Message ?? "Er ging iets mis.", "Fout!");
            return Json(new { ok = r.Ok, message = r.Message, id = r.Id });
        }

        // =====================================================================
        // Pagina
        // =====================================================================

        private async Task<IActionResult> DetailDocsV2Get(int projectid, int? clientaccountid, string? folder, string? smart, int? unit, int? client, int? company, int? open, int? request)
        {
            var svc = DocsSvc;
            var filter = new DocFilter
            {
                Folder = string.IsNullOrWhiteSpace(folder) ? null : folder,
                Smart = string.IsNullOrWhiteSpace(smart) ? null : smart,
                UnitId = unit,
                ClientAccountId = client ?? clientaccountid,
                CompanyId = company
            };
            var overview = await svc.GetOverview(projectid, filter);
            var vm = new DocsV2PageVm
            {
                Overview = overview,
                CanWrite = DocCanWrite,
                CanDelete = DocCanDelete,
                OpenDocId = open,
                OpenRequestId = request,
                FilterUnitId = filter.UnitId,
                FilterClientId = filter.ClientAccountId,
                FilterCompanyId = filter.CompanyId,
                Folder = filter.Folder,
                Smart = filter.Smart
            };
            if (filter.UnitId.HasValue) vm.FilterLabel = overview.Units.FirstOrDefault(u => u.Id == filter.UnitId)?.Name;
            else if (filter.ClientAccountId.HasValue) vm.FilterLabel = overview.Clients.FirstOrDefault(c => c.Id == filter.ClientAccountId)?.Name;
            else if (filter.CompanyId.HasValue) vm.FilterLabel = overview.Companies.FirstOrDefault(c => c.Id == filter.CompanyId)?.Name;

            // Kruimelpad: stopt bij het project (DESIGN.md regel 2 — de pagina zelf staat niet als laatste kruimel).
            var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projectenIndex = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = dashboard };
            var projectDetail = new MvcBreadcrumbNode("Detail", "Projecten", overview.ProjectName) { Parent = projectenIndex, RouteValues = new { projectid } };
            ViewData["BreadcrumbNode"] = projectDetail;

            SetPageHeader("ph ph-folder", "Documenten");
            return View("DetailDocsV2", vm);
        }

        // =====================================================================
        // Panelen (HTML-fragmenten)
        // =====================================================================

        [HttpGet]
        public async Task<IActionResult> DocumentPanelV2(int projectid, int id)
        {
            var svc = DocsSvc;
            var detail = await svc.GetDetail(projectid, id);
            if (detail == null) return NotFound();
            var overview = await svc.GetOverview(projectid, new DocFilter { Folder = "__none__" });
            var vm = new DocPanelVm
            {
                Detail = detail,
                CanWrite = DocCanWrite,
                CanDelete = DocCanDelete,
                Units = overview.Units,
                Clients = overview.Clients,
                Companies = overview.Companies
            };
            var previewRev = detail.Revisions.FirstOrDefault(r => r.Id == detail.PreviewRevisionId);
            if (previewRev != null)
            {
                var file = await svc.GetRevisionFile(projectid, previewRev.Id);
                if (file != null && !string.IsNullOrWhiteSpace(file.Filename))
                    vm.ThumbUrl = GetSignedAssetUrlByFileName(BuildDocThumbFileName(file.Filename), "docs");
            }
            return PartialView("Partials/_DocPanelV2", vm);
        }

        [HttpGet]
        public async Task<IActionResult> DocumentRequestPanelV2(int projectid, int id)
        {
            var detail = await DocsSvc.GetRequestDetail(projectid, id);
            if (detail == null) return NotFound();
            return PartialView("Partials/_DocRequestPanelV2", new DocRequestPanelVm { Detail = detail, CanWrite = DocCanWrite, CanDelete = DocCanDelete });
        }

        [HttpGet]
        public async Task<IActionResult> DocumentCardV2(int projectid, int? unit, int? client, int? company)
        {
            var scope = new DocCardScope { ProjectId = projectid, UnitId = unit, ClientAccountId = client, CompanyId = company };
            var card = await DocsSvc.GetCard(scope);
            string viewAll = unit.HasValue
                ? Url.Action("DetailDocs", "Projecten", new { projectid, unit }) ?? ""
                : client.HasValue
                    ? Url.Action("DetailDocs", "Projecten", new { projectid, client }) ?? ""
                    : Url.Action("DetailDocs", "Projecten", new { projectid, company }) ?? "";
            return PartialView("Partials/_DocumentsCardV2", new DocCardVm
            {
                Card = card, ProjectId = projectid, UnitId = unit, ClientAccountId = client, CompanyId = company,
                ViewAllUrl = viewAll, ViewAllLabel = "Alle documenten", CanWrite = DocCanWrite
            });
        }

        /// <summary>Keuzelijst van de upload-dialoog ("Nieuwe revisie van …"): alle documenten én verwachte documenten van het project.</summary>
        [HttpGet]
        public async Task<IActionResult> DocumentReplaceOptionsV2(int projectid)
        {
            var overview = await DocsSvc.GetOverview(projectid, new DocFilter());
            var items = overview.Groups.SelectMany(g => g.Rows)
                .GroupBy(r => r.Kind + ":" + r.Id).Select(g => g.First())
                .Where(r => r.StatusKey != "notrequired")
                .OrderBy(r => r.Kind == "request" ? 0 : 1).ThenBy(r => r.Name)
                .Select(r => new
                {
                    kind = r.Kind,
                    id = r.Id,
                    name = r.Name,
                    folder = r.FolderName,
                    missing = r.Kind == "request",
                    frozen = r.StatusKey is "signed",
                    units = r.Chips.Where(c => c.Type == "unit").Select(c => c.Label).ToList()
                });
            return Json(items);
        }

        [HttpGet]
        public async Task<IActionResult> DocumentFileV2(int projectid, int revisionId)
        {
            var f = await DocsSvc.GetRevisionFile(projectid, revisionId);
            if (f == null) return NotFound();
            var url = GetSignedAssetUrlByFileName(f.Filename, "docs");
            if (string.IsNullOrWhiteSpace(url)) return NotFound();
            return Redirect(url);
        }

        // =====================================================================
        // Uploaden en revisies
        // =====================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> DocumentUploadV2(int projectid, string? mode, int? documentId, int? requestId, string? name, int? folderId,
            DateOnly? docDate, DateOnly? expiresOn, string? number, string? authoredBy, string? perceel, decimal? amount, string? note,
            List<string>? links, bool shareClients, bool shareSuppliers, byte? status, IFormFile? file)
        {
            if (file == null || file.Length <= 0) return DocJson(DocResult.Fail("Kies eerst een bestand."));
            if (mode != "revision" && string.IsNullOrWhiteSpace(name) && !requestId.HasValue)
                return DocJson(DocResult.Fail("Geef het document een naam."));

            var stored = await UploadAssetToStorageAsync(file, "docs");
            if (string.IsNullOrWhiteSpace(stored)) return DocJson(DocResult.Fail("Upload naar de opslag is mislukt."));
            try { await GenerateDocThumbnailViaStorageAsync(stored); }
            catch (Exception ex) { _logger.LogWarning(ex, "Thumbnail voor {File} kon niet gemaakt worden.", stored); }

            var refs = new List<DocLinkRef>();
            foreach (var l in links ?? new List<string>())
            {
                var parts = (l ?? "").Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var id) && parts[0] is "unit" or "client" or "company")
                    refs.Add(new DocLinkRef { Type = parts[0], Id = id });
            }

            var res = await DocsSvc.Upload(new DocUploadDto
            {
                ProjectId = projectid, Mode = mode ?? "new", DocumentId = documentId, RequestId = requestId, Name = name,
                FolderId = folderId, DocDate = docDate, ExpiresOn = expiresOn, Number = number, AuthoredBy = authoredBy,
                Perceel = perceel, Amount = amount, Note = note, Links = refs, ShareClients = shareClients, ShareSuppliers = shareSuppliers,
                Status = status, UploaderKind = DocumentUploaderKind.Intern, StoredFilename = stored, OriginalFilename = file.FileName,
                SizeBytes = file.Length, UserId = DocUserId, UserName = DocUserName
            });
            return DocJson(res);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentApproveV2(int projectid, int revisionId) =>
            DocJson(await DocsSvc.ApproveRevision(projectid, revisionId, DocUserId, DocUserName));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentRejectV2(int projectid, int revisionId, string? note) =>
            DocJson(await DocsSvc.RejectRevision(projectid, revisionId, note, DocUserId, DocUserName));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentUpdateV2(int projectid, int id, string? name, int? folderId, string? number, string? authoredBy,
            DateOnly? docDate, DateOnly? expiresOn, string? perceel) =>
            DocJson(await DocsSvc.UpdateDocument(projectid, id, new DocUpdateDto
            {
                Name = name ?? "", FolderId = folderId, Number = number, AuthoredBy = authoredBy, DocDate = docDate, ExpiresOn = expiresOn, Perceel = perceel
            }, DocUserId));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentDeleteV2(int projectid, int id) =>
            DocJson(await DocsSvc.DeleteDocument(projectid, id));

        // =====================================================================
        // Koppelen en delen
        // =====================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentLinkAddV2(int projectid, int id, string type, int targetId, bool share) =>
            DocJson(await DocsSvc.AddLink(projectid, id, type, targetId, share));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentLinkRemoveV2(int projectid, int id, int linkId) =>
            DocJson(await DocsSvc.RemoveLink(projectid, id, linkId));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentShareV2(int projectid, int id, string audience, bool share) =>
            DocJson(await DocsSvc.SetShare(projectid, id, audience, share));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentLinkShareV2(int projectid, int id, int linkId, bool share) =>
            DocJson(await DocsSvc.SetLinkShare(projectid, id, linkId, share));

        // =====================================================================
        // Verwachte / aangevraagde documenten
        // =====================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentRequestSaveV2(int projectid, int? id, string? name, int folderId, int? unitId, string? perceel, string? kind,
            int? companyId, int? clientAccountId, string? responsibleName, DateOnly? dueDate, string? dueLabel, int? reminderDays, int? expiryYears,
            bool shareAfter, string? note, bool sendNow)
        {
            var dto = new DocRequestDto
            {
                ProjectId = projectid, Name = name ?? "", FolderId = folderId, UnitId = unitId, Perceel = perceel,
                ResponsibleKind = kind ?? "leverancier", CompanyId = companyId, ClientAccountId = clientAccountId, ResponsibleName = responsibleName,
                DueDate = dueDate, DueLabel = dueLabel, ReminderDaysBefore = reminderDays, ExpiryYears = expiryYears,
                ShareWithBuyerAfterApproval = shareAfter, Note = note, SendNow = sendNow
            };
            DocResult res;
            var svc = DocsSvc;
            var requestId = id ?? 0;
            if (requestId > 0)
            {
                res = await svc.UpdateRequest(projectid, requestId, dto);
                if (res.Ok && sendNow) { await svc.MarkRequested(projectid, requestId, DocUserId, DocUserName); res.Id = requestId; }
            }
            else
            {
                res = await svc.CreateRequest(dto, DocUserId, DocUserName);
                requestId = res.Id ?? 0;
            }
            if (res.Ok && sendNow && requestId > 0)
            {
                var mailNote = await SendDocumentRequestMail(projectid, requestId, false);
                if (mailNote != null) res.Message = (res.Message ?? "") + " " + mailNote;
            }
            res.Id = requestId;
            return DocJson(res);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentRequestRemindV2(int projectid, int id)
        {
            var svc = DocsSvc;
            var res = await svc.MarkReminded(projectid, id);
            if (!res.Ok) return DocJson(res);
            var note = await SendDocumentRequestMail(projectid, id, true);
            res.Message = note ?? "Herinnering geregistreerd.";
            return DocJson(res);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentRequestNotRequiredV2(int projectid, int id, bool notRequired) =>
            DocJson(await DocsSvc.SetRequestNotRequired(projectid, id, notRequired));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentRequestDeleteV2(int projectid, int id) =>
            DocJson(await DocsSvc.DeleteRequest(projectid, id));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentGenerateExpectedV2(int projectid)
        {
            var n = await DocsSvc.GenerateExpected(projectid, DocUserId);
            return DocJson(DocResult.Success(n == 0 ? "Alle verwachte documenten stonden al in de lijst." : $"{n} verwachte document{(n == 1 ? "" : "en")} aangemaakt uit de sjablonen."));
        }

        /// <summary>Stuurt de aanvraag/herinnering per e-mail. Geeft een korte melding terug (of null bij succes zonder opmerking).
        /// Zolang het leveranciersportaal er niet is, vraagt de mail om het bestand als antwoord terug te sturen.</summary>
        private async Task<string?> SendDocumentRequestMail(int projectId, int requestId, bool reminder)
        {
            var svc = DocsSvc;
            var (email, recipient) = await svc.GetRequestRecipient(projectId, requestId);
            if (string.IsNullOrWhiteSpace(email)) return "Er is geen e-mailadres bekend voor de verantwoordelijke: er werd geen mail verstuurd.";
            var req = await svc.GetRequestDetail(projectId, requestId);
            if (req == null) return null;
            var projectName = _projectService.GetProjectNameById(projectId);
            string E(string? s) => WebUtility.HtmlEncode(s ?? "");
            var deadline = req.DueDate.HasValue ? "Gewenst tegen " + req.DueDate.Value.ToString("dd/MM/yyyy") + "." : (req.DueLabel != null ? E(req.DueLabel) + "." : "");
            var body = $"<p>Beste {E(recipient)},</p>" +
                       $"<p>{(reminder ? "Een herinnering:" : "Voor")} project <strong>{E(projectName)}</strong> hebben we het volgende document nodig: <strong>{E(req.Name)}</strong>" +
                       (req.UnitLabel != null ? $" ({E(req.UnitLabel)})" : "") + $". {deadline}</p>" +
                       "<p>Gelieve het bestand als antwoord op deze e-mail te bezorgen. Zodra het leveranciersportaal beschikbaar is, kan u het daar zelf uploaden.</p>" +
                       $"<p>Met vriendelijke groeten,<br/>{E(DocUserName)}</p>";
            try
            {
                var sender = HttpContext.RequestServices.GetRequiredService<IEmailSender>();
                await sender.SendEmailAsync(email, (reminder ? "Herinnering — " : "Documentaanvraag — ") + req.Name + " (" + projectName + ")", body);
                return $"Mail verstuurd naar {email}.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Documentaanvraag-mail naar {Email} mislukt.", email);
                return "De aanvraag is geregistreerd, maar de e-mail kon niet verstuurd worden.";
            }
        }

        // =====================================================================
        // Ondertekening en gunning
        // =====================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentSignSendV2(int projectid, int id, List<string>? names, List<string>? roles, List<int?>? clientIds)
        {
            var list = new List<DocSignatoryDto>();
            names ??= new(); roles ??= new(); clientIds ??= new();
            for (var i = 0; i < names.Count; i++)
                list.Add(new DocSignatoryDto
                {
                    Name = names[i] ?? "",
                    Role = i < roles.Count ? roles[i] : null,
                    ClientAccountId = i < clientIds.Count ? clientIds[i] : null,
                    UserId = (i < roles.Count && roles[i] == "verkoper") ? DocUserId : null
                });
            return DocJson(await DocsSvc.SendForSignature(projectid, id, list));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentSignMarkV2(int projectid, int signatureId, string? method) =>
            DocJson(await DocsSvc.MarkSigned(projectid, signatureId, method ?? "manueel"));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentSignRemindV2(int projectid, int signatureId)
        {
            var res = await DocsSvc.MarkSignatureReminded(projectid, signatureId);
            if (!res.Ok) return DocJson(res);
            var sig = await _db.DocumentSignatures.Where(s => s.Id == signatureId)
                .Select(s => new { s.Name, s.ClientAccountId, Doc = s.Document.Name })
                .FirstOrDefaultAsync();
            var sigEmail = sig?.ClientAccountId != null
                ? await _db.ClientAccount.Where(c => c.Id == sig.ClientAccountId).Select(c => c.Email).FirstOrDefaultAsync()
                : null;
            if (sig != null && !string.IsNullOrWhiteSpace(sigEmail))
            {
                try
                {
                    var sender = HttpContext.RequestServices.GetRequiredService<IEmailSender>();
                    await sender.SendEmailAsync(sigEmail, "Herinnering ondertekening — " + sig.Doc,
                        $"<p>Beste {WebUtility.HtmlEncode(sig.Name)},</p><p>Het document <strong>{WebUtility.HtmlEncode(sig.Doc)}</strong> wacht nog op uw handtekening.</p><p>Met vriendelijke groeten,<br/>{WebUtility.HtmlEncode(DocUserName)}</p>");
                    res.Message = $"Herinnering verstuurd naar {sigEmail}.";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Herinneringsmail ondertekening mislukt.");
                    res.Message = "Herinnering geregistreerd, maar de e-mail kon niet verstuurd worden.";
                }
            }
            else res.Message = "Herinnering geregistreerd (geen e-mailadres bekend om te versturen).";
            return DocJson(res);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentAwardV2(int projectid, int id) =>
            DocJson(await DocsSvc.Award(projectid, id, DocUserId, DocUserName));
    }
}
