using BOCore;
using CPMCore.Documents;
using CPMCore.Extensions;
using CPMCore.Models.Invoicing;
using CPMCore.Service;
using CPMCore.Services.Octopus;
using CPMCore.Services.Peppol;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Fluent;
using ServiceCore;
using ServiceCore.Invoicing;
using ServiceCore.Invoicing.Pdf;
using SmartBreadcrumbs.Nodes;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

namespace CPMCore.Controllers
{
    /// <summary>Factuur verzenden (e-mail/Peppol/Octopus) en opnieuw versturen. Opgesplitst uit InvoicesController.cs (okt. 2026, structureren) - views in Views/Invoices/Send/. Zelfde partial class: alle private velden/services van InvoicesController.cs blijven gewoon bruikbaar.</summary>
    public partial class InvoicesController
    {
        //FACTUUR VERZENDEN (GET)
        [HttpGet]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> Send(int id, int? issuerCompanyId = null, string? mode = null, CancellationToken ct = default)
        {
            var detail = await _invoices.GetDetailAsync(id, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return issuerCompanyId.HasValue
                    ? RedirectToAction(nameof(Index), new { issuerCompanyId })
                    : RedirectToAction(nameof(Index));
            }

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null)
            {
                AddMessage("error", "Factuur verstrekker niet gevonden.", "Factuur");
                return issuerCompanyId.HasValue
                    ? RedirectToAction(nameof(Index), new { issuerCompanyId })
                    : RedirectToAction(nameof(Index));
            }

            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(detail.IssuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te versturen.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = detail.IssuerCompanyId });
            }

            var formMode = ParseSendMode(mode);
            var vm = await CreateSendViewModelAsync(detail, issuer, includeDefaults: true, checkPeppol: true, formMode, ct);

            var issuerId = issuerCompanyId ?? issuer.Id;
            if (issuerId > 0)
            {
                await SetIssuerViewBagsAsync(issuerId, ct);
            }
            var companyDisplay = (ViewBag.CompanyName as string) ?? vm.IssuerName;
            var detailTitle = BuildInvoiceDetailBreadcrumbTitle(detail);
            SetSendBreadcrumb(issuerId, companyDisplay, detail.Id, detailTitle);
            SetPageHeader("bx bx-receipt", "Factuur verzenden");
            return View(vm);
        }

        //FACTUUR VERZENDEN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> Send(InvoiceSendVM form, CancellationToken ct = default)
        {
            var submitMode = Request?.Form?["submitMode"].ToString();
            var isCopyRequest = form.IsCopyRequest || string.Equals(submitMode, "copy", StringComparison.OrdinalIgnoreCase);
            form.IsCopyRequest = isCopyRequest;
            if (!ModelState.IsValid)
            {
                // fall through to populate later
            }

            var detail = await _invoices.GetDetailAsync(form.InvoiceId, ct);
            if (detail == null)
            {
                AddMessage("error", "Factuur niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Index));
            }

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null)
            {
                AddMessage("error", "Factuur verstrekker niet gevonden.", "Factuur");
                return RedirectToAction(nameof(Index));
            }

            var writeScopePost = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScopePost.HasAllIssuers && !writeScopePost.AllowedIssuerIds.Contains(detail.IssuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te versturen.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId = detail.IssuerCompanyId });
            }

            var formMode = isCopyRequest ? InvoiceSendFormMode.Copy : InvoiceSendFormMode.Standard;
            if (isCopyRequest)
            {
                form.AttachPdf = true;
                form.AttachUbl = false;
                form.SendToPeppol = false;
            }

            var vm = await CreateSendViewModelAsync(detail, issuer, includeDefaults: false, checkPeppol: true, formMode, ct);
            vm.To = form.To;
            vm.Cc = form.Cc;
            vm.Subject = form.Subject;
            vm.Body = form.Body;
            vm.AttachPdf = form.AttachPdf;
            vm.AttachUbl = form.AttachUbl;
            vm.SendToPeppol = form.SendToPeppol && vm.CanSendViaPeppol;
            vm.IsCopyRequest = isCopyRequest;
            vm.ForcePdfOnly = isCopyRequest;

            await SetIssuerViewBagsAsync(vm.IssuerCompanyId, ct);
            var companyDisplay = (ViewBag.CompanyName as string) ?? vm.IssuerName;
            var detailTitle = BuildInvoiceDetailBreadcrumbTitle(detail);
            SetSendBreadcrumb(vm.IssuerCompanyId, companyDisplay, vm.InvoiceId, detailTitle);
            SetPageHeader("bx bx-receipt", "Factuur verzenden");

            if (!ModelState.IsValid)
                return View(vm);

            if (string.IsNullOrWhiteSpace(vm.To))
            {
                ModelState.AddModelError(nameof(vm.To), "E-mailadres is verplicht.");
                return View(vm);
            }

            if (!vm.AttachPdf && !vm.AttachUbl)
            {
                ModelState.AddModelError(string.Empty, "Selecteer minstens één bijlage.");
                return View(vm);
            }

            var attachments = new List<EmailAttachment>();
            var vatTypes = await _ics.ListVatTypeAsync(detail.IssuerCompanyId, ct);
            var dto = detail.ToInvoiceDto(vatTypes);
            var ublDocument = _ublBuilder.Build(detail, issuer);

            if (vm.AttachPdf)
            {
                var pdfBytes = await RenderInvoicePdfWithAppendixAsync(detail.Id, dto, issuer, ct);
                var pdfName = string.IsNullOrWhiteSpace(dto.PublicId)
                    ? $"Factuur_{dto.Id}.pdf"
                    : $"{dto.PublicId}.pdf";
                attachments.Add(new EmailAttachment(pdfName, pdfBytes, "application/pdf"));
            }

            if (vm.AttachUbl)
            {
                attachments.Add(new EmailAttachment(
                    string.IsNullOrWhiteSpace(ublDocument.FileName) ? $"{dto.PublicId ?? dto.Id.ToString()}_invoice.xml" : ublDocument.FileName,
                    Encoding.UTF8.GetBytes(ublDocument.Xml),
                    "application/xml"));
            }
            var sentAtUtc = DateTime.UtcNow;
            try
            {
                await _emailSender.SendEmailAsync(
                     vm.To,
                     vm.Subject,
                     vm.Body,
                     attachments,
                     vm.Cc,
                     issuer.InvoiceSendEmail,
                     issuer.InvoiceSendEmail);
                await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
                {
                    InvoiceId = detail.Id,
                    ToAddress = vm.To,
                    CcAddress = vm.Cc,
                    Subject = vm.Subject,
                    ProviderId = Guid.NewGuid().ToString(),
                    SentAt = sentAtUtc,
                    Status = "Sent"
                }, ct);
                await _cmd.MarkAsSentAsync(detail.Id, sentAtUtc, ct);
                AddMessage("success", "E-mail verzonden.", "Factuur");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Send invoice email {InvoiceId} failed", detail.Id);
                AddMessage("error", "E-mail kon niet verzonden worden.", "Factuur");
                ModelState.AddModelError(string.Empty, "E-mail kon niet verzonden worden. Probeer opnieuw.");
                return View(vm);
            }

            await _communication.SaveInvoiceUblAsync(new InvoiceUblBO
            {
                InvoiceId = detail.Id,
                XmlContent = ublDocument.Xml,
                UblVersion = ublDocument.UblVersion,
                Profile = ublDocument.Profile,
                GeneratedAt = ublDocument.GeneratedAt,
                SentViaPeppol = false,
                PeppolDocId = null
            }, ct);

            if (vm.SendToPeppol && vm.CanSendViaPeppol)
            {
                var participantId = vm.PeppolParticipantId
                    ?? detail.ClientVatNumber
                    ?? detail.ClientEnterpriseNumber;

                if (!string.IsNullOrWhiteSpace(participantId))
                {
                    var result = await _peppolSender.SendAsync(participantId, ublDocument.Xml, ct);
                    if (result.Success)
                    {
                        await _communication.SaveInvoiceUblAsync(new InvoiceUblBO
                        {
                            InvoiceId = detail.Id,
                            XmlContent = ublDocument.Xml,
                            UblVersion = ublDocument.UblVersion,
                            Profile = ublDocument.Profile,
                            GeneratedAt = ublDocument.GeneratedAt,
                            SentViaPeppol = true,
                            PeppolDocId = result.DocumentId
                        }, ct);
                        AddMessage("success", "Factuur verzonden via Peppol.", "Factuur");
                    }
                    else
                    {
                        AddMessage("warning", string.IsNullOrWhiteSpace(result.Message) ? "Peppol verzending mislukt." : result.Message, "Factuur");
                    }
                }
                else
                {
                    AddMessage("warning", "Geen geldig Peppol-ID beschikbaar.", "Factuur");
                }
            }

            return RedirectToAction(nameof(Send), new { id = detail.Id, issuerCompanyId = vm.IssuerCompanyId, mode = isCopyRequest ? "copy" : null });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CPMCore.Filters.PermissionWrite(PermissionCodes.Invoicing)]
        public async Task<IActionResult> ResendOctopus(int id, int issuerCompanyId, CancellationToken ct = default)
        {
            var writeScope = await ResolveInvoicingIssuerScopeAsync(PermissionAccessType.Write, ct);
            if (!writeScope.HasAllIssuers && !writeScope.AllowedIssuerIds.Contains(issuerCompanyId))
            {
                AddMessage("error", "Je hebt geen rechten om facturen voor dit facturatiebedrijf te versturen.", "Geen toegang");
                return RedirectToAction(nameof(Index), new { issuerCompanyId });
            }

            try
            {
                await SendInvoiceToOctopusAsync(id, ct, sendOnly: true, forceSendStep: true);
                AddMessage("success", "Factuur opnieuw via Octopus verstuurd.", "Factuur");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Octopus resend blocked for invoice {InvoiceId}", id);
                AddMessage("error", ex.Message, "Factuur");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Octopus resend failed for invoice {InvoiceId}", id);
                AddMessage("error", "Factuur kon niet opnieuw via Octopus verzonden worden.", "Factuur");
            }

            return RedirectToAction(nameof(Send), new { id, issuerCompanyId });
        }


        private async Task<InvoiceSendVM> CreateSendViewModelAsync(InvoiceDetailBO detail, IssuerCompanyBO issuer, bool includeDefaults, bool checkPeppol, InvoiceSendFormMode mode, CancellationToken ct)
        {
            var vm = new InvoiceSendVM
            {
                InvoiceId = detail.Id,
                IssuerCompanyId = issuer.Id,
                PublicId = string.IsNullOrWhiteSpace(detail.PublicId) ? null : detail.PublicId,
                InvoiceDate = detail.InvoiceDate,
                ClientName = detail.ClientName ?? string.Empty,
                ClientVatNumber = detail.ClientVatNumber,
                ClientEnterpriseNumber = detail.ClientEnterpriseNumber,
                ClientEmail = detail.ClientEmail,
                RequiresDigitalInvoice = detail.RequiresDigitalInvoice,
                DefaultAttachUbl = detail.AttachUblByDefault,
                IsSupplier = detail.IsSupplier,
                TotalExclVat = RoundCurrency(detail.TotalExclVat),
                TotalVat = RoundCurrency(detail.TotalVat),
                TotalInclVat = RoundCurrency(detail.TotalInclVat),
                IssuerName = issuer.Name ?? string.Empty,
                IssuerEmail = issuer.Email,
                IssuerPeppolEnabled = issuer.PeppolEnabled,
                IssuerPeppolId = issuer.PeppolParticipantId,
                BankAccount = !string.IsNullOrWhiteSpace(detail.BankAccount)
                    ? detail.BankAccount
                    : issuer.DefaultBankAccountIban ?? issuer.EpcIban,
                Currency = !string.IsNullOrWhiteSpace(issuer.DefaultCurrency) ? issuer.DefaultCurrency : "EUR",
                AttachPdf = true,
                AttachUbl = detail.AttachUblByDefault
            };

            vm.IsCopyRequest = mode == InvoiceSendFormMode.Copy;
            vm.ForcePdfOnly = vm.IsCopyRequest;

            if (vm.IsCopyRequest)
            {
                vm.AttachPdf = true;
                vm.AttachUbl = false;
                vm.SendToPeppol = false;
            }

            if (includeDefaults)
            {
                vm.To = detail.ClientEmail?.Trim() ?? string.Empty;
                var templateModel = BuildEmailTemplateModel(detail, issuer, vm.BankAccount, vm.Currency);

                var subjectTemplate = issuer.EmailSubjectTemplate;
                if (!string.IsNullOrWhiteSpace(subjectTemplate))
                {
                    var rendered = _templateInterpolator.Interpolate(subjectTemplate, templateModel).Trim();
                    vm.Subject = !string.IsNullOrWhiteSpace(rendered)
                        ? rendered
                        : $"Factuur {vm.PublicId ?? detail.Id.ToString(CultureInfo.InvariantCulture)}";
                }
                else
                {
                    vm.Subject = $"Factuur {vm.PublicId ?? detail.Id.ToString(CultureInfo.InvariantCulture)}";
                }

                var bodyTemplate = issuer.EmailBodyTemplate;
                if (detail.IsPrepaid && !string.IsNullOrWhiteSpace(issuer.EmailPaidBodyTemplate))
                {
                    bodyTemplate = issuer.EmailPaidBodyTemplate;
                }
                if (!string.IsNullOrWhiteSpace(bodyTemplate))
                {
                    var rendered = _templateInterpolator.Interpolate(bodyTemplate, templateModel).Trim();
                    vm.Body = !string.IsNullOrWhiteSpace(rendered)
                        ? rendered
                        : BuildDefaultEmailBody(detail, issuer, vm.Currency, vm.BankAccount);
                }
                else
                {
                    vm.Body = BuildDefaultEmailBody(detail, issuer, vm.Currency, vm.BankAccount);
                }
                if (vm.IsCopyRequest && !string.IsNullOrWhiteSpace(vm.Subject))
                {
                    vm.Subject = $"Kopie - {vm.Subject}";
                }
            }

            var logs = await _communication.GetEmailLogsAsync(detail.Id, ct);
            vm.EmailLogs = logs
                .Select(log => new InvoiceEmailLogItemVM
                {
                    SentAt = log.SentAt,
                    To = log.ToAddress,
                    Cc = log.CcAddress,
                    Subject = log.Subject,
                    Status = log.Status
                })
                .ToList();
            vm.LastEmailSentAt = logs.FirstOrDefault()?.SentAt;

            var ubl = await _communication.GetInvoiceUblAsync(detail.Id, ct);
            if (ubl != null)
            {
                vm.ExistingUblGeneratedAt = ubl.GeneratedAt;
                vm.ExistingUblSentViaPeppol = ubl.SentViaPeppol;
                vm.ExistingUblDocumentId = ubl.PeppolDocId;
                vm.ExistingUblProfile = ubl.Profile;
                vm.ExistingUblVersion = ubl.UblVersion;
            }

            var octopusProgress = GetOctopusWorkflowProgress(detail.OctopusWorkflowState);
            var hasClientEmail = !string.IsNullOrWhiteSpace(detail.ClientEmail);
            vm.CanRetryOctopusSend = detail.RequiresDigitalInvoice
                && hasClientEmail
                && octopusProgress.CreationCompleted
                && octopusProgress.UploadCompleted;

            if (checkPeppol)
            {
                if (issuer.PeppolEnabled && !string.IsNullOrWhiteSpace(issuer.PeppolParticipantId))
                {
                    var lookupId = detail.ClientVatNumber ?? detail.ClientEnterpriseNumber;
                    if (!string.IsNullOrWhiteSpace(lookupId))
                    {
                        var participant = await _peppolDirectory.FindParticipantAsync(lookupId, ct);
                        if (participant != null)
                        {
                            vm.PeppolAccountFound = true;
                            vm.PeppolParticipantId = participant.ParticipantId;
                            vm.PeppolStatusMessage = $"Peppol-account gevonden: {participant.Name ?? participant.ParticipantId}";
                        }
                        else
                        {
                            vm.PeppolStatusMessage = "Geen Peppol-account gevonden.";
                        }
                    }
                    else
                    {
                        vm.PeppolStatusMessage = "Geen ondernemings- of btw-nummer beschikbaar.";
                    }

                    vm.CanSendViaPeppol = vm.PeppolAccountFound;
                    if (includeDefaults && !vm.IsCopyRequest)
                        vm.SendToPeppol = vm.PeppolAccountFound;
                }
                else
                {
                    vm.PeppolStatusMessage = "Peppol is niet geactiveerd voor dit bedrijf.";
                    vm.CanSendViaPeppol = false;
                }
            }

            return vm;
        }

        private static InvoiceSendFormMode ParseSendMode(string? mode)
        {
            return string.Equals(mode, "copy", StringComparison.OrdinalIgnoreCase)
                ? InvoiceSendFormMode.Copy
                : InvoiceSendFormMode.Standard;
        }

        private async Task TrySendClassicEmailAfterIssueAsync(int invoiceId, CancellationToken ct)
        {
            try
            {
                var postIssueContext = await BuildOctopusInvoiceContextAsync(invoiceId, ct, forceFinalPdf: true);
                var emailAttachment = await BuildAttachmentAsync(postIssueContext, ct);
                var classicSent = await SendClassicInvoiceEmailIfRequiredAsync(postIssueContext, emailAttachment, ct);

                if (classicSent)
                {
                    await PersistOctopusDeliveryStateAsync(
                        postIssueContext.Invoice.Id,
                        new OctopusDocumentDeliveryState
                        {
                            DeliveryState = "EMAILED",
                            Comment = "Verzonden via klassieke e-mail",
                            DeliveryDateTime = DateTime.UtcNow
                        },
                        ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Post-issue classic email failed for invoice {InvoiceId}", invoiceId);
                AddMessage("warning", "Factuur werd uitgegeven, maar klassieke e-mailverzending is mislukt.", "Factuur");
            }
        }


    }
}
