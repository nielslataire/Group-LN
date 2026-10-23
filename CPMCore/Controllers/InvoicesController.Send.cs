using BOCore;
using CPMCore.Documents;
using CPMCore.Extensions;
using CPMCore.Models.Invoicing;
using CPMCore.Services;
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
            return SendView(vm);
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
                form.Channel = "email";
                // "Kopie naar mezelf" (gl-v2): de kopie gaat naar het adres van de ingelogde gebruiker.
                var me = CurrentUserEmail();
                if (submitMode == "copy" && Request?.Form?["copyToSelf"].ToString() == "1" && !string.IsNullOrWhiteSpace(me))
                {
                    form.To = me;
                    form.Cc = null;
                }
            }
            // gl-v2 (11g): het gekozen kanaal bepaalt wat er gebeurt. Zonder Channel (oud scherm) blijft het zoals voorheen: altijd e-mail, Peppol via de schakelaar.
            var channel = (form.Channel ?? string.Empty).Trim().ToLowerInvariant();
            var sendEmail = channel != "peppol";
            // Peppol gaat altijd via Octopus: dat doorsturen gebeurt hieronder, niet via de rechtstreekse Peppol-verzender van het oude scherm.
            var viaOctopus = channel == "peppol" || channel == "both";
            if (channel != string.Empty) form.SendToPeppol = false;
            // Peppol alleen: de e-mailvelden zijn dan niet nodig. "Kopie naar mezelf": het adres wordt hieronder door dat van de gebruiker vervangen.
            if (!sendEmail)
            {
                ModelState.Remove(nameof(InvoiceSendVM.To));
                ModelState.Remove(nameof(InvoiceSendVM.Subject));
                ModelState.Remove(nameof(InvoiceSendVM.Body));
            }
            else if (isCopyRequest && Request?.Form?["copyToSelf"].ToString() == "1" && !string.IsNullOrWhiteSpace(CurrentUserEmail()))
                ModelState.Remove(nameof(InvoiceSendVM.To));

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
            vm.Channel = string.IsNullOrEmpty(channel) ? null : channel;

            await SetIssuerViewBagsAsync(vm.IssuerCompanyId, ct);
            var companyDisplay = (ViewBag.CompanyName as string) ?? vm.IssuerName;
            var detailTitle = BuildInvoiceDetailBreadcrumbTitle(detail);
            SetSendBreadcrumb(vm.IssuerCompanyId, companyDisplay, vm.InvoiceId, detailTitle);
            SetPageHeader("bx bx-receipt", "Factuur verzenden");

            if (!ModelState.IsValid)
                return SendView(vm);

            if (sendEmail && string.IsNullOrWhiteSpace(vm.To))
            {
                ModelState.AddModelError(nameof(vm.To), "E-mailadres is verplicht.");
                return SendView(vm);
            }

            if (sendEmail && !vm.AttachPdf && !vm.AttachUbl)
            {
                ModelState.AddModelError(string.Empty, "Selecteer minstens één bijlage.");
                return SendView(vm);
            }

            if (viaOctopus && !vm.CanSendViaOctopus)
            {
                ModelState.AddModelError(string.Empty, vm.OctopusNote ?? "Doorsturen naar Octopus is niet mogelijk. Kies e-mail.");
                return SendView(vm);
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
            if (sendEmail)
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
                return SendView(vm);
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

            if (channel == string.Empty && vm.SendToPeppol && vm.CanSendViaPeppol)
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
                        if (!sendEmail) await _cmd.MarkAsSentAsync(detail.Id, sentAtUtc, ct);
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

            if (viaOctopus)
            {
                // Doorsturen naar je eigen Octopus, dat de factuur via Peppol naar de klant verstuurt (zoals bij het uitgeven). Een eigen e-mail van CPM is hier niet bij:
                // wie ook per mail wil versturen, kiest Beide of E-mail.
                try
                {
                    var progress = GetOctopusWorkflowProgress(detail.OctopusWorkflowState);
                    await SendInvoiceToOctopusAsync(detail.Id, ct,
                        sendOnly: progress.CreationCompleted && progress.UploadCompleted,
                        forceSendStep: progress.SendCompleted,
                        skipClassicEmail: true);
                    if (!sendEmail) await _cmd.MarkAsSentAsync(detail.Id, sentAtUtc, ct);
                    AddMessage("success", "Factuur doorgestuurd naar Octopus. Octopus verstuurt ze via Peppol naar de klant.", "Factuur");
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Octopus send blocked for invoice {InvoiceId}", detail.Id);
                    AddMessage("error", ex.Message, "Factuur");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Octopus send failed for invoice {InvoiceId}", detail.Id);
                    AddMessage("error", "Factuur kon niet naar Octopus doorgestuurd worden.", "Factuur");
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
            vm.ExpirationDate = detail.ExpirationDate;
            vm.StructuredMessage = detail.StructuredMessage;
            vm.CurrentUserEmail = CurrentUserEmail();
            if (string.IsNullOrWhiteSpace(vm.Channel)) vm.Channel = "email";

            var ubl = await _communication.GetInvoiceUblAsync(detail.Id, ct);
            if (ubl != null)
            {
                vm.ExistingUblGeneratedAt = ubl.GeneratedAt;
                vm.ExistingUblSentViaPeppol = ubl.SentViaPeppol;
                vm.ExistingUblDocumentId = ubl.PeppolDocId;
                vm.ExistingUblProfile = ubl.Profile;
                vm.ExistingUblVersion = ubl.UblVersion;
            }

            // Verzendgeschiedenis met kanaal (11g). Het e-maillogboek bevat ook de Octopus-gebeurtenissen (ToAddress zonder @: "Octopus" of de verzendmethode):
            // die horen bij kanaal Peppol, want Octopus verstuurt enkel via Peppol. Echte e-mails (met een adres) zijn kanaal E-mail.
            var history = new List<InvoiceSendHistoryItemVM>();
            foreach (var log in logs)
            {
                var isMail = (log.ToAddress ?? string.Empty).Contains('@');
                var st = log.Status ?? string.Empty;
                string statusText, tone;
                if (isMail) { statusText = string.Equals(st, "Sent", StringComparison.OrdinalIgnoreCase) ? "Verzonden" : st; tone = string.Equals(st, "Sent", StringComparison.OrdinalIgnoreCase) ? "success" : "neutral"; }
                else if (string.Equals(st, "Error", StringComparison.OrdinalIgnoreCase)) { statusText = "Mislukt"; tone = "danger"; }
                else if (string.Equals(st, "Uploaded", StringComparison.OrdinalIgnoreCase)) { statusText = "Doorgestuurd"; tone = "neutral"; }
                else if (string.Equals(st, "Booked", StringComparison.OrdinalIgnoreCase)) { statusText = "Geboekt"; tone = "neutral"; }
                else if (string.Equals(st, "Sent", StringComparison.OrdinalIgnoreCase)) { statusText = "Verzonden"; tone = "success"; }
                else { statusText = st; tone = "neutral"; }
                history.Add(new InvoiceSendHistoryItemVM
                {
                    At = log.SentAt,
                    Recipient = isMail ? log.ToAddress : (log.ToAddress ?? string.Empty).Contains("peppol", StringComparison.OrdinalIgnoreCase) ? "Peppol-netwerk" : "Octopus",
                    Channel = isMail ? "E-mail" : "Peppol",
                    Detail = log.Subject,
                    Status = statusText,
                    Tone = tone
                });
            }
            // De leveringsstatus die Octopus zelf teruggeeft (enkel Peppol; "EMAILED" is de klassieke mail van CPM en staat al als echte e-mailregel in de lijst).
            if (detail.OctopusDeliveryDateTime.HasValue && !string.IsNullOrWhiteSpace(detail.OctopusDeliveryState))
            {
                var state = detail.OctopusDeliveryState!;
                var upper = state.ToUpperInvariant();
                if (upper is not ("EMAILED" or "NONE"))
                {
                    var failed = upper is "ERROR" or "PEPPOL_SEND_FAILED";
                    history.Add(new InvoiceSendHistoryItemVM
                    {
                        At = detail.OctopusDeliveryDateTime.Value,
                        Recipient = "Peppol-netwerk",
                        Channel = "Peppol",
                        Detail = string.IsNullOrWhiteSpace(detail.OctopusDeliveryComment) ? "Octopus verzendstatus: " + state : detail.OctopusDeliveryComment!,
                        Status = failed ? "Mislukt" : upper == "DELIVERED" ? "Afgeleverd" : upper == "PEPPOL_SENDING_IN_PROGRESS" ? "Onderweg" : state,
                        Tone = failed ? "danger" : upper == "DELIVERED" ? "strong" : "neutral"
                    });
                }
            }
            vm.History = history.OrderByDescending(h => h.At).ToList();

            var octopusProgress = GetOctopusWorkflowProgress(detail.OctopusWorkflowState);
            var hasClientEmail = !string.IsNullOrWhiteSpace(detail.ClientEmail);
            vm.CanRetryOctopusSend = detail.RequiresDigitalInvoice
                && hasClientEmail
                && octopusProgress.CreationCompleted
                && octopusProgress.UploadCompleted;

            // Doorsturen naar Octopus: enkel als Octopus gekoppeld is en de factuur er nog niet is, of de verzending mislukt is.
            var deliveryState = (detail.OctopusDeliveryState ?? string.Empty).ToUpperInvariant();
            var octopusFailed = deliveryState is "ERROR" or "PEPPOL_SEND_FAILED"
                || logs.Any(l => !(l.ToAddress ?? string.Empty).Contains('@') && string.Equals(l.Status, "Error", StringComparison.OrdinalIgnoreCase)) && deliveryState is not "DELIVERED";
            if (string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
            {
                vm.CanSendViaOctopus = false;
                vm.OctopusNote = "Octopus is niet gekoppeld voor dit facturatiebedrijf.";
            }
            else if (octopusProgress.SendCompleted && !octopusFailed)
            {
                vm.CanSendViaOctopus = false;
                vm.OctopusNote = "Deze factuur is al naar Octopus verstuurd.";
            }
            else
            {
                vm.CanSendViaOctopus = true;
                vm.OctopusNote = octopusFailed ? "De vorige verzending naar Octopus is mislukt. Je kan ze opnieuw proberen." : null;
            }

            if (checkPeppol)
            {
                // Controle in de openbare Peppol-directory: enkel informatief (Octopus beslist zelf hoe het verstuurt). Los van de Peppol-instelling van het bedrijf.
                var lookupId = detail.ClientVatNumber ?? detail.ClientEnterpriseNumber;
                if (!string.IsNullOrWhiteSpace(lookupId))
                {
                    PeppolParticipant? participant = null;
                    try
                    {
                        participant = await _peppolDirectory.FindParticipantAsync(lookupId, ct);
                        if (participant == null && !string.IsNullOrWhiteSpace(detail.ClientEnterpriseNumber) && detail.ClientEnterpriseNumber != lookupId)
                            participant = await _peppolDirectory.FindParticipantAsync(detail.ClientEnterpriseNumber, ct);
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Peppol-controle mislukt voor factuur {InvoiceId}", detail.Id); }
                    if (participant != null)
                    {
                        vm.PeppolCheck = "found";
                        vm.PeppolParticipantId = participant.ParticipantId;
                        vm.PeppolStatusMessage = $"Peppol-account gevonden: {participant.Name ?? participant.ParticipantId}";
                    }
                    else
                    {
                        vm.PeppolCheck = "notfound";
                        vm.PeppolStatusMessage = "Geen Peppol-account gevonden.";
                    }
                }
                else
                {
                    vm.PeppolStatusMessage = "Geen ondernemings- of btw-nummer beschikbaar.";
                }

                // Oude scherm (rechtstreeks Peppol): enkel als het bedrijf Peppol zelf heeft geactiveerd.
                if (issuer.PeppolEnabled && !string.IsNullOrWhiteSpace(issuer.PeppolParticipantId))
                {
                    vm.PeppolAccountFound = vm.PeppolCheck == "found";
                    vm.CanSendViaPeppol = vm.PeppolAccountFound;
                    if (includeDefaults && !vm.IsCopyRequest) vm.SendToPeppol = vm.PeppolAccountFound;
                }
                else
                {
                    vm.CanSendViaPeppol = false;
                }

                // Standaardkanaal (gl-v2): e-mail; staat de klant op Peppol en kan het naar Octopus, dan beide.
                if (includeDefaults && !vm.IsCopyRequest) vm.Channel = vm.CanSendViaOctopus && vm.PeppolCheck == "found" ? "both" : "email";
            }

            return vm;
        }

        /// <summary>E-mailadres van de ingelogde gebruiker (voor "Kopie naar mezelf").</summary>
        private string? CurrentUserEmail() =>
            User?.FindFirst(ClaimTypes.Email)?.Value
            ?? User?.FindFirst("email")?.Value
            ?? User?.FindFirst("preferred_username")?.Value;

        /// <summary>Factuur verzenden: nieuwe gl-v2-layout (SendV2, 11g) of het oude scherm.</summary>
        private ViewResult SendView(InvoiceSendVM model) => View(ViewData["UseGlV2Layout"] as bool? == true ? "SendV2" : "Send", model);

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
