using System.Net;
using BOCore;
using CPMCore.Helpers;
using CPMCore.Models.Projecten;
using CPMCore.Services;
using CPMCore.Services.Security;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Nodes;

namespace CPMCore.Controllers
{
    /// <summary>Wijzigingsopdracht digitaal ter ondertekening sturen (intern deel). De klant tekent op de publieke
    /// pagina <see cref="SigningController"/>. Actienamen bevatten "ChangeOrder": PermissionResolver koppelt ze aan
    /// ProjectsChangeOrders (GET = lezen, POST = schrijven).</summary>
    public partial class ProjectenController
    {
        private ISigningService SigningSvc => HttpContext.RequestServices.GetRequiredService<ISigningService>();

        private string SigningBaseUrl => (Configuration["App:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

        [HttpGet]
        public async Task<IActionResult> ChangeOrderSignV2(int changeorderid)
        {
            var vm = await BuildChangeOrderSignVm(changeorderid);
            if (vm == null) return NotFound();

            // Deze pagina bestaat enkel in gl-v2; ook wie de oude lay-out gebruikt komt hier via de lijst terecht.
            ViewData["UseGlV2Layout"] = true;
            ViewData["Title"] = "Ter ondertekening sturen";
            ViewData["PageIcon"] = "ph ph-signature";
            ViewData["BackUrl"] = vm.BackUrl;

            var dashboard = new MvcBreadcrumbNode("Index", "Home", "Dashboard");
            var projecten = new MvcBreadcrumbNode("Index", "Projecten", "Projecten") { Parent = dashboard };
            var project = new MvcBreadcrumbNode("Detail", "Projecten", vm.ProjectName) { Parent = projecten, RouteValues = new { projectid = vm.ProjectId } };
            var lijst = new MvcBreadcrumbNode("DetailsChangeOrder", "Projecten", "Wijzigingsopdrachten") { Parent = project, RouteValues = new { projectid = vm.ProjectId } };
            ViewData["BreadcrumbNode"] = lijst;
            return View("ChangeOrderSignV2", vm);
        }

        private async Task<ChangeOrderSignVm?> BuildChangeOrderSignVm(int changeOrderId)
        {
            var co = _projectService.GetChangeOrder(changeOrderId);
            if (!co.Success) return null;
            var bo = co.Values.FirstOrDefault();
            if (bo == null) return null;
            var sales = _projectService.GetSalesSettings(bo.ProjectId);
            var vatPct = sales.Success ? (sales.Values.FirstOrDefault()?.VatPercentage ?? 0m) : 0m;
            var client = _clientService.GetClientAccountById(bo.ClientAccountID);
            var clientBo = client.Success ? client.Values.FirstOrDefault() : null;
            var total = bo.Totaal;
            var vm = new ChangeOrderSignVm
            {
                ChangeOrderId = changeOrderId,
                ProjectId = bo.ProjectId,
                ProjectName = _projectService.GetProjectNameById(bo.ProjectId),
                ClientAccountId = bo.ClientAccountID,
                ClientName = bo.ClientName ?? clientBo?.DisplayName ?? "",
                Description = bo.Description ?? "",
                Date = bo.ChangeOrderDate,
                ExpirationDate = bo.ExpirationDate,
                TotalExcl = total,
                VatAmount = vatPct * total / 100m,
                CanWrite = HttpContext.RequestServices.GetRequiredService<IPermissionService>().HasWrite(PermissionCodes.ProjectsChangeOrders),
                Status = await SigningSvc.GetChangeOrderStatus(changeOrderId),
                ConsentText = SigningSvc.ConsentText,
                BackUrl = Url.Action("DetailsChangeOrder", "Projecten", new { projectid = bo.ProjectId }) ?? "/"
            };
            vm.TotalIncl = vm.TotalExcl + vm.VatAmount;
            var email = !string.IsNullOrWhiteSpace(clientBo?.Email) ? clientBo!.Email : clientBo?.InvoiceEmail;
            vm.SuggestedSigners.Add(new SigningSignerDto { Name = vm.ClientName, Email = email ?? "", Role = "klant", ClientAccountId = bo.ClientAccountID });
            return vm;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderSignSendV2(int changeorderid, List<string>? names, List<string>? emails)
        {
            var signers = new List<SigningSignerDto>();
            names ??= new(); emails ??= new();
            for (var i = 0; i < names.Count; i++)
            {
                var name = (names[i] ?? "").Trim();
                var email = i < emails.Count ? (emails[i] ?? "").Trim() : "";
                if (name == "" && email == "") continue;
                signers.Add(new SigningSignerDto { Name = name, Email = email, Role = "klant" });
            }
            var vm = await BuildChangeOrderSignVm(changeorderid);
            if (vm == null) return NotFound();
            foreach (var s in signers) if (s.ClientAccountId == null && signers.IndexOf(s) == 0) s.ClientAccountId = vm.ClientAccountId;

            // 1) PDF bouwen (zelfde opmaak als de gewone wijzigingsopdracht) en bewaren
            var pdf = await HttpContext.RequestServices.GetRequiredService<ChangeOrderPdfService>().BuildAsync(ControllerContext, changeorderid, pendingDigitalSigning: true);
            if (pdf == null)
            {
                AddMessage("error", "Het PDF van de wijzigingsopdracht kon niet gemaakt worden.", "Fout!");
                return RedirectToAction(nameof(ChangeOrderSignV2), new { changeorderid });
            }
            var stored = await HttpContext.RequestServices.GetRequiredService<DocStorageService>().UploadAsync(pdf.Bytes, pdf.FileName, "application/pdf", "docs");
            if (string.IsNullOrWhiteSpace(stored))
            {
                AddMessage("error", "Het PDF kon niet naar de opslag geüpload worden.", "Fout!");
                return RedirectToAction(nameof(ChangeOrderSignV2), new { changeorderid });
            }
            try { await GenerateDocThumbnailViaStorageAsync(stored); } catch { /* voorbeeld is bijzaak */ }

            // 2) ondertekening aanmaken (document in Contracten, één link per ondertekenaar)
            var res = await SigningSvc.CreateForChangeOrder(changeorderid, new SigningCreateDto
            {
                StoredFilename = stored, OriginalFilename = pdf.FileName, SizeBytes = pdf.Bytes.Length, PdfHash = pdf.Hash,
                Signers = signers, NotifyEmail = User.GetCpmEmail(), UserId = DocUserId, UserName = DocUserName
            });
            if (!res.Ok)
            {
                AddMessage("error", res.Message ?? "Versturen is mislukt.", "Fout!");
                return RedirectToAction(nameof(ChangeOrderSignV2), new { changeorderid });
            }

            // 3) mails met de persoonlijke links (PDF als bijlage: ook wie liever op papier tekent, heeft het document)
            var failed = new List<string>();
            foreach (var issued in res.Issued)
                if (!await SendSigningMail(issued, vm, pdf.Bytes, pdf.FileName, reminder: false)) failed.Add(issued.Email);

            if (failed.Count == 0)
                AddMessage("success", $"Ter ondertekening verstuurd naar {string.Join(", ", res.Issued.Select(i => i.Email))}.", "Gelukt!");
            else
                AddMessage("error", $"De ondertekening is aangemaakt, maar de e-mail naar {string.Join(", ", failed)} kon niet verstuurd worden. Gebruik \"Link opnieuw sturen\".", "Let op");
            return RedirectToAction(nameof(ChangeOrderSignV2), new { changeorderid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeOrderSignResendV2(int changeorderid, int signatureId)
        {
            var vm = await BuildChangeOrderSignVm(changeorderid);
            if (vm == null) return NotFound();
            var status = await SigningSvc.GetChangeOrderStatus(changeorderid);
            if (status == null || status.Signers.All(s => s.SignatureId != signatureId))
            {
                AddMessage("error", "Ondertekenaar niet gevonden.", "Fout!");
                return RedirectToAction(nameof(ChangeOrderSignV2), new { changeorderid });
            }
            var issued = await SigningSvc.ReissueLink(status.ProjectId, signatureId);
            if (issued == null)
            {
                AddMessage("error", "Deze ondertekenaar heeft al getekend.", "Let op");
                return RedirectToAction(nameof(ChangeOrderSignV2), new { changeorderid });
            }
            // Bijlage: het huidige PDF van het document
            byte[]? attachment = null; string attachName = "Wijzigingsopdracht.pdf";
            var file = status.CurrentRevisionId.HasValue ? await DocsSvc.GetRevisionFile(status.ProjectId, status.CurrentRevisionId.Value) : null;
            if (file != null)
            {
                var url = await HttpContext.RequestServices.GetRequiredService<DocStorageService>().GetSignedUrlAsync(file.Filename, "docs");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    try { using var http = new HttpClient(); attachment = await http.GetByteArrayAsync(url); attachName = file.OriginalFilename ?? attachName; } catch { attachment = null; }
                }
            }
            var ok = await SendSigningMail(issued, vm, attachment, attachName, reminder: true);
            AddMessage(ok ? "success" : "error", ok ? $"Nieuwe link verstuurd naar {issued.Email}. De vorige link werkt niet meer." : "De e-mail kon niet verstuurd worden.", ok ? "Gelukt!" : "Fout!");
            return RedirectToAction(nameof(ChangeOrderSignV2), new { changeorderid });
        }

        private async Task<bool> SendSigningMail(SigningIssued issued, ChangeOrderSignVm vm, byte[]? pdf, string pdfName, bool reminder)
        {
            string E(string? s) => WebUtility.HtmlEncode(s ?? "");
            var link = $"{SigningBaseUrl}/ondertekenen/{issued.Token}";
            var euro = System.Globalization.CultureInfo.GetCultureInfo("nl-BE");
            var body =
                $"<p>Beste {E(issued.Name)},</p>" +
                $"<p>{(reminder ? "Hierbij een nieuwe link om" : "Wij vragen u om")} de wijzigingsopdracht <strong>{E(vm.Description)}</strong> voor project <strong>{E(vm.ProjectName)}</strong> digitaal te ondertekenen.</p>" +
                $"<p>Totaal: <strong>{vm.TotalIncl.ToString("C2", euro)}</strong> (incl. btw). Gelieve te ondertekenen tegen {vm.ExpirationDate:dd/MM/yyyy}.</p>" +
                $"<p style=\"margin:22px 0\"><a href=\"{E(link)}\" style=\"background:#00532D;color:#fff;padding:12px 22px;border-radius:8px;text-decoration:none;font-weight:600\">Bekijken en ondertekenen</a></p>" +
                "<p>U bevestigt met een code die u op dit e-mailadres ontvangt. Deze link is persoonlijk: deel hem niet met anderen. " +
                $"De link is geldig tot {issued.ExpiresOn.ToLocalTime():dd/MM/yyyy}.</p>" +
                "<p>Liever op papier? Het PDF hangt aan deze e-mail: u kan het afdrukken, ondertekenen en terugbezorgen.</p>" +
                $"<p>Met vriendelijke groeten,<br/>{E(DocUserName)}</p>";
            try
            {
                var sender = HttpContext.RequestServices.GetRequiredService<IEmailSender>();
                var attachments = pdf == null ? null : new[] { new EmailAttachment(pdfName, pdf, "application/pdf") };
                await sender.SendEmailAsync(issued.Email, (reminder ? "Nieuwe link — " : "Ter ondertekening — ") + "wijzigingsopdracht " + vm.ProjectName, body, attachments);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ondertekeningsmail naar {Email} mislukt.", issued.Email);
                return false;
            }
        }
    }
}
