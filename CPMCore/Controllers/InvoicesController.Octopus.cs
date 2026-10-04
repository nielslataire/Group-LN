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
    /// <summary>Octopus-boekhoudkoppeling: hoofdflow issue + verzending, relaties, bijlagen, workflowstatus, nummering/OGM/EPC-QR; geen eigen views. Opgesplitst uit InvoicesController.cs (okt. 2026, structureren) - views in Views/Invoices/Octopus/. Zelfde partial class: alle private velden/services van InvoicesController.cs blijven gewoon bruikbaar.</summary>
    public partial class InvoicesController
    {
        // Hoofdflow voor Octopus-issue + verzending.
        // Volgorde wijzigen? Pas de genummerde stappen hieronder aan.
        private async Task SendInvoiceToOctopusAsync(
            int invoiceId,
            CancellationToken ct,
            bool sendOnly = false,
            bool forceSendStep = false,
            bool forceFinalPdf = false,
            bool skipClassicEmail = false,
            DateOnly? forcedIssueDate = null)
        {
            try
            {
                // 1. Algemene gegevens van de factuur voorbereiden (gestructureerde mededeling, QR-code/public id enz.)
                var context = await BuildOctopusInvoiceContextAsync(invoiceId, ct, forceFinalPdf, forcedIssueDate);
                var progress = GetOctopusWorkflowProgress(context.Invoice.OctopusWorkflowState);

                // 2. Dossiertoken ophalen en valideren
                var dossierToken = await EnsureDossierTokenAsync(context, ct);

                if (progress.SendCompleted && !forceSendStep)
                    return;

                ValidateSendOnlyPreconditions(sendOnly, progress);

                // 3. Relatie ophalen of aanmaken/updaten
                await EnsureOctopusRelationAsync(context, dossierToken, sendOnly, ct, progress);

                // 4. Factuur aanmaken in Octopus API (CreateInvoiceAsync)
                await CreateOctopusInvoiceAsync(context, dossierToken, ct, progress);

                // 5. Document uploaden naar Octopus (UploadInvoiceAttachmentAsync)
                var attachment = await UploadInvoiceAttachmentAsync(context, dossierToken, sendOnly, ct, progress);

                // 6. Factuur verzenden via Octopus (SendInvoiceAsync)
                var sendResponse = await SendInvoiceViaOctopusAsync(context, dossierToken, ct);

                if (context.SkipSendStep)
                    return;

                var emailAttachment = attachment;
                if (sendResponse?.Success == true)
                {
                    if (await UpdateInvoicePublicIdFromOctopusAsync(context, ct))
                    {
                        emailAttachment = await BuildAttachmentAsync(context, ct);
                    }
                }

                // 7. Klassieke mailverzending indien geen Peppol (PDF identiek aan upload)
                var classicSent = false;
                if (!skipClassicEmail)
                {
                    classicSent = await SendClassicInvoiceEmailIfRequiredAsync(context, emailAttachment, ct);
                }

                if (classicSent && !WasSentViaPeppol(sendResponse))
                {
                    await PersistOctopusDeliveryStateAsync(
                        context.Invoice.Id,
                        new OctopusDocumentDeliveryState
                        {
                            DeliveryState = "EMAILED",
                            Comment = "Verzonden via klassieke e-mail",
                            DeliveryDateTime = DateTime.UtcNow
                        },
                        ct);
                }

                // 8. Logboeken en verzendgeschiedenis bijwerken
                await LogOctopusSendAsync(context, sendResponse, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Octopus request failed for invoice {InvoiceId}", invoiceId);
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Octopus gaf een onbekende fout terug."
                    : ex.Message;
                throw new InvalidOperationException($"Octopus gaf een fout terug bij het boeken van de factuur: {message}", ex);
            }

        }

        private sealed class OctopusInvoiceContext
        {
            public OctopusInvoiceContext(
                Invoices invoice,
                IssuerCompany issuer,
                InvoiceDetailBO detail,
                CompanyInfo? company,
                DateOnly finalDate,
                DateOnly? effectiveDueDate,
                int fiscalYear,
                int documentSequenceNr,
                int bookyearId,
                string journalKey,
                int periodNumber,
                string structuredOgm,
                string formattedPublicId,
                bool isIndividual,
                bool skipSendStep,
                string? clientEmail,
                string? clientCc,
                bool forceFinalPdf)
            {
                Invoice = invoice;
                Issuer = issuer;
                Detail = detail;
                Company = company;
                FinalDate = finalDate;
                EffectiveDueDate = effectiveDueDate;
                FiscalYear = fiscalYear;
                DocumentSequenceNr = documentSequenceNr;
                BookyearId = bookyearId;
                JournalKey = journalKey;
                PeriodNumber = periodNumber;
                StructuredOgm = structuredOgm;
                FormattedPublicId = formattedPublicId;
                IsIndividual = isIndividual;

                SkipSendStep = skipSendStep;
                ClientEmail = clientEmail;
                ClientCc = clientCc;
                ForceFinalPdf = forceFinalPdf;
            }

            public Invoices Invoice { get; }
            public IssuerCompany Issuer { get; }
            public InvoiceDetailBO Detail { get; }
            public CompanyInfo? Company { get; }
            public DateOnly FinalDate { get; }
            public DateOnly? EffectiveDueDate { get; }
            public int FiscalYear { get; }
            public int DocumentSequenceNr { get; set; }
            public int BookyearId { get; set; }
            public string JournalKey { get; set; }
            public int PeriodNumber { get; }
            public string StructuredOgm { get; set; }
            public string FormattedPublicId { get; set; }
            public bool IsIndividual { get; }

            public bool SkipSendStep { get; }
            public string? ClientEmail { get; }
            public string? ClientCc { get; }
            public bool ForceFinalPdf { get; }
            public int? OctopusRelationId { get; set; }
            public List<Vattype> VatTypes { get; } = new();
            public string DossierNumber => Issuer.OctopusDossierNumber ?? string.Empty;
        }

        private sealed class OctopusWorkflowProgress
        {
            public bool CreationCompleted { get; set; }
            public bool UploadCompleted { get; set; }
            public bool SendCompleted { get; set; }
        }

        private sealed class OctopusInvoiceAttachment
        {
            public OctopusInvoiceAttachment(byte[] pdfBytes, string fileName, string invoiceNumber)
            {
                PdfBytes = pdfBytes;
                FileName = fileName;
                InvoiceNumber = invoiceNumber;
            }

            public byte[] PdfBytes { get; }
            public string FileName { get; }
            public string InvoiceNumber { get; }
        }

        // Stap 1: laad alle data die nodig is voor Octopus + PDF (incl. status/nummerreeks).
        private async Task<OctopusInvoiceContext> BuildOctopusInvoiceContextAsync(int invoiceId, CancellationToken ct, bool forceFinalPdf, DateOnly? forcedIssueDate = null)
        {
            var invoice = await _db.Invoices
                .Include(i => i.InvoicesDetails)
                .Include(i => i.IssuerCompany)
                .Include(i => i.PostalCode)!
                    .ThenInclude(pc => pc.Country)
                .Include(i => i.ClientIdClientAccountNavigation)!
                    .ThenInclude(c => c.ClientContacts)
                // Migratie 057: "Verzenden naar" (expliciete factuurontvangers van het account) — nodig
                // in BuildInvoiceRecipients hieronder.
                .Include(i => i.ClientIdClientAccountNavigation)!
                    .ThenInclude(c => c.InvoiceRecipients)
                .Include(i => i.ClientIdClientAccountNavigation)!
                    .ThenInclude(c => c.PostalCode)!
                        .ThenInclude(pc => pc.Country)
                .Include(i => i.ClientIdClientAccountNavigation)!
                    .ThenInclude(c => c.InvoicePostalCode)!
                        .ThenInclude(pc => pc.Country)
                .Include(i => i.ClientIdClientContactsNavigation)!
                    .ThenInclude(c => c.PostalCode)!
                        .ThenInclude(pc => pc.Country)
                .Include(i => i.ClientIdClientContactsNavigation)!
                    .ThenInclude(c => c.InvoicePostalCode)!
                        .ThenInclude(pc => pc.Country)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
                ?? throw new InvalidOperationException("Factuur niet gevonden.");

            var issuer = invoice.IssuerCompany
                ?? throw new InvalidOperationException("Factuur heeft geen gekoppeld facturatiebedrijf.");

            CompanyInfo? company = null;
            if (invoice.CompanyId.HasValue)
            {
                company = await _db.CompanyInfo
                    .Include(c => c.PostCode)!
                        .ThenInclude(pc => pc.Country)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CompanyId == invoice.CompanyId.Value, ct);
            }

            if (string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
                throw new InvalidOperationException("Octopus dossiernummer ontbreekt. Vul dit in bij het facturatiebedrijf.");

            var detail = await _invoices.GetDetailAsync(invoiceId, ct)
                  ?? throw new InvalidOperationException("Factuurdetails niet gevonden.");

            var recipientEmails = BuildInvoiceRecipients(
                invoice,
                invoice.ClientIdClientAccountNavigation,
                invoice.ClientIdClientContactsNavigation,
                detail);
            var clientEmail = recipientEmails.FirstOrDefault();
            var clientCc = recipientEmails.Count > 1
                ? string.Join(';', recipientEmails.Skip(1))
                : null;
            var hasCompanyName = !string.IsNullOrWhiteSpace(invoice.ClientIdClientAccountNavigation?.CompanyName)
                || !string.IsNullOrWhiteSpace(invoice.ClientIdClientContactsNavigation?.CompanyName);
            var isIndividual = !invoice.CompanyId.HasValue && !hasCompanyName;
            // Bij nummering van facturen sturen we altijd door naar Octopus.
            // Octopus beslist vervolgens zelf over het kanaal (bv. Peppol bij een geldig btw-nummer).
            var skipSendStep = false;

            var finalDate = forceFinalPdf
                  ? forcedIssueDate ?? (invoice.Date == default ? DateOnly.FromDateTime(DateTime.Today) : invoice.Date)
                  : (invoice.Date == default
                      ? DateOnly.FromDateTime(DateTime.Today)
                      : invoice.Date);

            var effectiveDueDate = await ResolveEffectiveDueDateAsync(invoice, issuer, finalDate, ct, recalculate: forceFinalPdf);

            if (forceFinalPdf && invoice.Date != finalDate)
            {
                invoice.Date = finalDate;
            }

            if (forceFinalPdf && invoice.ExpirationDate != effectiveDueDate)
            {
                invoice.ExpirationDate = effectiveDueDate;
            }

            detail.InvoiceDate = finalDate;

            if (forceFinalPdf && invoice.Date != finalDate)
            {
                invoice.Date = finalDate;
            }

            if (forceFinalPdf && invoice.ExpirationDate != effectiveDueDate)
            {
                invoice.ExpirationDate = effectiveDueDate;
            }
            detail.ExpirationDate = effectiveDueDate;

            var seriesId = await _db.InvoiceSeries
                .Where(s => s.IssuerCompanyId == issuer.Id && s.IsActive)
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Geen actieve nummerreeks voor dit facturatiebedrijf.");

            var sequencesQuery = _db.InvoiceSequence
                 .Include(s => s.Bookyear)!
                 .ThenInclude(b => b.OctopusBookyearPeriods)
                 .Include(s => s.Journal)
                 .AsNoTracking()
                 .Where(s => s.SeriesId == seriesId);

            var sequence = await sequencesQuery
                .FirstOrDefaultAsync(s =>
                    s.Bookyear != null
                    && finalDate >= DateOnly.FromDateTime(s.Bookyear.StartDate)
                    && finalDate <= DateOnly.FromDateTime(s.Bookyear.EndDate), ct);

            if (sequence == null)
            {
                // Enkel legacy-sequenties zonder gekoppeld boekjaar ophalen via kalenderjaar.
                // Sequenties met BookyearId != null worden uitsluitend via de datumrange (pad 1) gevonden.
                var calendarYear = finalDate.Year;
                sequence = await sequencesQuery
                    .FirstOrDefaultAsync(s => s.BookyearId == null && s.FiscalYear == calendarYear, ct);
            }

            if (sequence == null)
            {
                // Onderscheid: bestaat het boekjaar al lokaal (na sync) maar ontbreekt de sequentie,
                // of is het boekjaar nog niet bekend (nooit gesynchroniseerd of niet in Octopus aangemaakt)?
                var bookyearExists = await _db.OctopusBookyears
                    .AnyAsync(b => b.IssuerCompanyId == issuer.Id
                               && b.StartDate <= finalDate.ToDateTime(TimeOnly.MinValue)
                               && b.EndDate >= finalDate.ToDateTime(TimeOnly.MinValue), ct);

                if (bookyearExists)
                    throw new InvalidOperationException(
                        $"Er is een boekjaar voor {finalDate:dd/MM/yyyy} maar er ontbreekt een nummerreeks. " +
                        "Synchroniseer het Octopus-dossier opnieuw zodat de nummerreeks automatisch aangemaakt wordt.");

                throw new InvalidOperationException(
                    $"Geen boekjaar gevonden voor {finalDate:dd/MM/yyyy}. " +
                    "Laat uw boekhouder eerst een nieuw boekjaar aanmaken in Octopus en synchroniseer daarna het dossier.");
            }

            if (sequence.Bookyear == null || sequence.Journal == null)
                throw new InvalidOperationException("Koppel een Octopus-boekjaar en dagboek aan de nummerreeks voor dit boekjaar.");

            // Voor de OGM-gestructureerde mededeling gebruiken we het jaar van de factuurdatum,
            // zodat facturen in een meerjarig boekjaar (bv. okt 2024 – sep 2025) het correcte
            // kalenderjaar in de OGM krijgen in plaats van het startjaar van het boekjaar.
            var fiscalYear = finalDate.Year;

            // Bepaal het hoogste al gebruikte documentnummer voor dit boekjaar + dagboek om dubbels te vermijden.
            // CurrentNumber wordt pas bijgewerkt bij doorboeken, dus ontvangen facturen in hetzelfde boekjaar
            // kunnen een hoger nummer hebben dan CurrentNumber.
            var highestSentNr = await _db.Invoices
                .Where(i => i.IssuerCompanyId == issuer.Id
                         && i.OctopusBookyearId == sequence.Bookyear.BookyearKeyId
                         && i.OctopusJournalKey == sequence.Journal.JournalKey
                         && i.OctopusDocumentSequenceNr != null)
                .MaxAsync(i => (int?)i.OctopusDocumentSequenceNr, ct) ?? 0;
            var lastBookedNr = sequence.Journal.LastBookedDocumentNr ?? 0;
            var nextNumber = Math.Max(Math.Max(sequence.CurrentNumber, highestSentNr), lastBookedNr) + 1;
            var documentSequenceNr = invoice.OctopusDocumentSequenceNr ?? nextNumber;
            var bookyearId = invoice.OctopusBookyearId ?? sequence.Bookyear.BookyearKeyId;
            var journalKey = string.IsNullOrWhiteSpace(invoice.OctopusJournalKey)
                ? sequence.Journal.JournalKey
                : invoice.OctopusJournalKey;

            var matchingPeriod = sequence.Bookyear.OctopusBookyearPeriods
                .FirstOrDefault(p =>
                {
                    var start = DateOnly.FromDateTime(p.StartDate);
                    var end = DateOnly.FromDateTime(p.EndDate);
                    return finalDate >= start && finalDate <= end;
                });

            if (matchingPeriod == null)
                throw new InvalidOperationException(
                    $"Geen boekhoudperiode gevonden voor {finalDate:dd/MM/yyyy}. " +
                    "Synchroniseer het Octopus-dossier om de periodes bij te werken.");

            var periodNumber = matchingPeriod.BookyearPeriodNr;

            var structuredOgm = BuildStructuredOgm(invoice, fiscalYear, documentSequenceNr);

            // Zorg dat gestructureerde mededeling en QR-payload up-to-date zijn voor we Octopus aanspreken
            var updatedQr = BuildEpcQrPayload(
                issuer,
                invoice.BankAccount,
                structuredOgm,
                detail.TotalInclVat);
            var needsSave = false;
            if (!string.IsNullOrWhiteSpace(structuredOgm) && !string.Equals(invoice.StructuredCommOgm, structuredOgm, StringComparison.Ordinal))
            {
                invoice.StructuredCommOgm = structuredOgm;
                needsSave = true;
            }
            if (!string.IsNullOrWhiteSpace(updatedQr) && !string.Equals(invoice.QrEpcPayload, updatedQr, StringComparison.Ordinal))
            {
                invoice.QrEpcPayload = updatedQr;
                detail.QrPayLoad = updatedQr;
                needsSave = true;
            }
            if (needsSave)
            {
                await _db.SaveChangesAsync(ct);
            }

            // Zorg dat de detailweergave dezelfde gegevens heeft als de factuur zelf
            detail.StructuredMessage = string.IsNullOrWhiteSpace(structuredOgm)
                ? detail.StructuredMessage
                : structuredOgm;

            var formattedPublicId = forceFinalPdf || string.IsNullOrWhiteSpace(invoice.PublicId)
                   ? FormatInvoiceNumber(issuer.InvoiceNumberPattern, documentSequenceNr, finalDate)
                   : invoice.PublicId;

            if (forceFinalPdf && !string.Equals(invoice.PublicId, formattedPublicId, StringComparison.Ordinal))
            {
                invoice.PublicId = formattedPublicId;
            }

            if (forceFinalPdf)
            {
                detail.PublicId = formattedPublicId;
            }
            else
            {
                detail.PublicId ??= formattedPublicId;
            }

            return new OctopusInvoiceContext(
                invoice,
                issuer,
                detail,
                company,
                finalDate,
                effectiveDueDate,
                fiscalYear,
                documentSequenceNr,
                bookyearId,
                journalKey,
                periodNumber,
                structuredOgm,
                formattedPublicId,
                isIndividual,
                skipSendStep,
                clientEmail,
                clientCc,
                forceFinalPdf);
        }

        private async Task<DateOnly?> ResolveEffectiveDueDateAsync(Invoices invoice, IssuerCompany issuer, DateOnly invoiceDate, CancellationToken ct, bool recalculate = false)
        {
            if (!recalculate && invoice.ExpirationDate.HasValue)
                return invoice.ExpirationDate.Value;

            var termId = invoice.PaymentTermId ?? issuer.DefaultPaymentTermId;
            if (!termId.HasValue)
                return null;

            var term = await _db.PaymentTerms
                .AsNoTracking()
                .Where(t => t.Id == termId.Value)
                .Select(t => new { t.Days, t.TermType })
                .FirstOrDefaultAsync(ct);

            if (term == null)
                return null;

            var baseDate = ((PaymentTermType)term.TermType) == PaymentTermType.DaysAfterEndOfMonth
                ? new DateOnly(invoiceDate.Year, invoiceDate.Month, DateTime.DaysInMonth(invoiceDate.Year, invoiceDate.Month))
                : invoiceDate;

            return baseDate.AddDays(term.Days);
        }

        private async Task RestoreDraftStateAsync(int invoiceId, byte? statusId, CancellationToken ct)
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
            if (invoice == null)
                return;

            invoice.StatusId = statusId;
            invoice.PublicId = null;
            invoice.StructuredCommOgm = null;
            invoice.QrEpcPayload = null;
            invoice.ExpirationDate = null;
            invoice.SeriesId = null;
            invoice.FiscalYear = null;
            await _db.SaveChangesAsync(ct);
        }

        private static List<string> BuildInvoiceRecipients(
            Invoices invoice,
            ClientAccount? account,
            ClientContacts? contact,
            InvoiceDetailBO detail)
        {
            var recipients = new List<string>();

            if (invoice.ClientType == (int)InvoicePartyType.ClientContact && contact != null)
            {
                if (contact.RequiresDigitalInvoice)
                {
                    AddRecipient(recipients, contact.InvoiceEmail, contact.Email);
                }

                return recipients;
            }

            if (invoice.ClientType == (int)InvoicePartyType.ClientAccount && account != null)
            {
                if (account.RequiresDigitalInvoice)
                {
                    AddRecipient(recipients, account.InvoiceEmail, account.Email);
                }

                if (account.ClientContacts != null)
                {
                    foreach (var accountContact in account.ClientContacts.Where(c => !c.IsCoOwner && c.RequiresDigitalInvoice))
                    {
                        AddRecipient(recipients, accountContact.InvoiceEmail, accountContact.Email);
                    }
                }

                // Migratie 057 — "Verzenden naar" (ClientAccountInvoiceRecipient): de ontvangers die de
                // gebruiker expliciet op het klantenaccount zette (Klanten/EditProjectV2 en
                // AddClientAccountV2, tab/sectie Facturatie). Tot hier werden die enkel BEWAARD en niet
                // gebruikt — een factuur ging dus nooit naar bv. de boekhouder die daar stond. Ze komen
                // nu bovenop de bestaande logica (digitale-factuur-vlag van account/contacten), gededupli-
                // ceerd door AddRecipient. Een ontvanger die enkel naar een contact verwijst (geen eigen
                // e-mail) valt terug op dat contact se factuur-/gewone e-mail.
                if (account.InvoiceRecipients != null)
                {
                    foreach (var storedRecipient in account.InvoiceRecipients.OrderBy(r => r.SortOrder))
                    {
                        if (!string.IsNullOrWhiteSpace(storedRecipient.Email))
                        {
                            AddRecipient(recipients, storedRecipient.Email, null);
                            continue;
                        }

                        var linkedContact = storedRecipient.ClientContactId is int linkedId
                            ? account.ClientContacts?.FirstOrDefault(c => c.Id == linkedId)
                            : null;
                        if (linkedContact != null)
                        {
                            AddRecipient(recipients, linkedContact.InvoiceEmail, linkedContact.Email);
                        }
                    }
                }
            }

            if (recipients.Count == 0 && invoice.ClientType != (int)InvoicePartyType.ClientAccount)
            {
                AddRecipient(recipients, detail.ClientEmail, null);
            }

            return recipients;
        }

        private static void AddRecipient(List<string> recipients, string? invoiceEmail, string? email)
        {
            var candidate = ResolvePreferredEmail(invoiceEmail, email);
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            if (recipients.Any(r => string.Equals(r, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            recipients.Add(candidate);
        }

        private static string? ResolvePreferredEmail(string? invoiceEmail, string? email)
        {
            if (!string.IsNullOrWhiteSpace(invoiceEmail))
            {
                return invoiceEmail.Trim();
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                return email.Trim();
            }

            return null;
        }

        // Stap 2b: extra guardrails als we enkel het verzenddeel willen herhalen.
        private static void ValidateSendOnlyPreconditions(bool sendOnly, OctopusWorkflowProgress progress)
        {
            if (!sendOnly)
                return;

            if (!progress.CreationCompleted)
                throw new InvalidOperationException("Factuur moet eerst in Octopus aangemaakt worden.");

            if (!progress.UploadCompleted)
                throw new InvalidOperationException("Factuur moet eerst als bijlage naar Octopus geüpload worden.");
        }

        // Stap 2: dossiertoken ophalen zodat alle Octopus-calls geauthenticeerd zijn.
        private async Task<OctopusDossierTokenResult> EnsureDossierTokenAsync(OctopusInvoiceContext context, CancellationToken ct)
        {
            return await _octopusTokens.RefreshDossierTokenAsync(context.Issuer.Id, context.DossierNumber, ct);
        }

        // Stap 3: zorg dat de Octopus-relatie bestaat en koppel lokale relation IDs.
        private async Task EnsureOctopusRelationAsync(
            OctopusInvoiceContext context,
            OctopusDossierTokenResult dossierToken,
            bool sendOnly,
            CancellationToken ct,
            OctopusWorkflowProgress progress)
        {
            if (progress.CreationCompleted)
                return;

            if (sendOnly)
                throw new InvalidOperationException("Octopus-factuur is nog niet aangemaakt.");

            var issuerRelationId = await GetIssuerSpecificOctopusRelationIdAsync(
                 context.Issuer.Id,
                 context.Invoice.ClientIdClientAccountNavigation,
                 context.Invoice.ClientIdClientContactsNavigation,
                 context.Company,
                 ct);

            var relationLookups = BuildRelationLookups(
                context.Invoice,
                context.Invoice.ClientIdClientAccountNavigation,
                context.Invoice.ClientIdClientContactsNavigation,
                context.Company,
                issuerRelationId);
            if (!relationLookups.Any())
            {
                throw new InvalidOperationException("Onvoldoende klantgegevens om de Octopus-relatie te bepalen.");
            }

            var relation = await EnsureOctopusRelationAsync(
                dossierToken.Token,
                context.DossierNumber,
                relationLookups,
                BuildRelationRequest(
                    context.Invoice,
                    context.Invoice.ClientIdClientAccountNavigation,
                    context.Invoice.ClientIdClientContactsNavigation,
                    context.Company,
                    issuerRelationId),
                ct);

            var relationId = relation?.RelationIdentificationServiceData?.RelationKey?.Id
                ?? issuerRelationId;

            if (relationId is null or <= 0)
            {
                throw new InvalidOperationException("Octopus-relatie kon niet bepaald worden.");
            }

            context.OctopusRelationId = relationId;
            await UpdateLocalRelationIdsAsync(context, relationId, ct);

            context.VatTypes.Clear();
            var vatTypes = await _db.Vattype
                .Where(v => v.IssuerCompanyId == context.Issuer.Id)
                .AsNoTracking()
                .ToListAsync(ct);
            context.VatTypes.AddRange(vatTypes);
        }

        // Stap 3b: sla relationId per issuer op in de junction table.
        private async Task UpdateLocalRelationIdsAsync(OctopusInvoiceContext context, int? relationId, CancellationToken ct)
        {
            if (!relationId.HasValue || relationId.Value <= 0)
                return;

            await SetIssuerSpecificRelationIdAsync(
                context.Issuer.Id,
                context.Invoice.ClientIdClientAccountNavigation,
                context.Invoice.ClientIdClientContactsNavigation,
                context.Company,
                relationId.Value,
                ct);
        }

        // Stap 4: maak factuur aan in Octopus (booking + lijnen).
        private async Task CreateOctopusInvoiceAsync(
            OctopusInvoiceContext context,
            OctopusDossierTokenResult dossierToken,
            CancellationToken ct,
            OctopusWorkflowProgress progress)
        {
            if (progress.CreationCompleted)
                return;

            var relationId = context.OctopusRelationId;

            if (relationId is null or <= 0)
            {
                throw new InvalidOperationException("Octopus-relatie kon niet bepaald worden.");
            }

            var customFieldValues = BuildInvoiceCustomFieldValues(context.Invoice, context.Issuer, context.FormattedPublicId);

            var payload = new OctopusInvoiceCreateRequest
            {
                BookyearKey = new OctopusBookyearKeyRef { Id = context.BookyearId },
                JournalKey = context.JournalKey,
                DocumentSequenceNr = context.DocumentSequenceNr,
                BookyearPeriodeNr = context.PeriodNumber,
                DocumentDate = context.FinalDate,
                ExpiryDate = context.EffectiveDueDate ?? context.FinalDate,
                CurrencyCode = string.IsNullOrWhiteSpace(context.Invoice.CurrencyCode) ? "EUR" : context.Invoice.CurrencyCode!,
                ExchangeRate = 1m,
                RelationIdentificationServiceData = new OctopusRelationIdentificationServiceData
                {
                    RelationKey = new OctopusRelationKeyRef { Id = relationId ?? 0 },
                    // ClientAccount.Id, ClientContacts.Id en CompanyInfo.CompanyId zijn onafhankelijke
                    // ID-reeksen die overlappen — elke bron krijgt een eigen bereik (zie OctopusExternalRelationIds).
                    ExternalRelationId = ResolveExternalRelationId(context.Invoice)
                },
                Comment = context.Invoice.Text,
                OrderReference = null,
                Reference = context.StructuredOgm,
                FinancialDiscount = 0m,
                CustomFieldValueList = customFieldValues,
                InvoiceLines = context.Invoice.InvoicesDetails.Select(line => new OctopusInvoiceLineRequest
                {
                    ExternProductNr = null,
                    Description = line.Text,
                    Count = 1,
                    Unit = string.Empty,
                    UnitPrice = line.Price ?? 0m,
                    DiscountPercentage = line.DiscountPercent ?? 0m,
                    VatCodeKey = ResolveVatCodeKey(line, context.VatTypes, context.Issuer.DefaultVatTypeId),
                    BookingAccountNr = 0,
                    //CostCentreKey = new OctopusCostCentreKeyRef { Id = 0 },
                    CustomFieldValueList = new List<OctopusCustomFieldValue>(),
                    IntrastatServiceData = null
                }).ToList()
            };

            var created = await _octopusClient.CreateInvoiceAsync(dossierToken.Token, context.DossierNumber, payload, ct);
            if (!created)
            {
                throw new InvalidOperationException("Octopus gaf geen bevestiging terug bij het aanmaken van de factuur.");
            }

            context.Invoice.OctopusBookyearId = context.BookyearId;
            context.Invoice.OctopusJournalKey = context.JournalKey;
            context.Invoice.OctopusDocumentSequenceNr = context.DocumentSequenceNr;
            context.Invoice.OctopusDeliveryState ??= "NONE";
            await PersistOctopusWorkflowStateAsync(context.Invoice, OctopusWorkflowStateCreated, ct);
            UpdateProgressFromState(progress, context.Invoice.OctopusWorkflowState);
        }

        //private OctopusBuySellBookingAndAttachmentRequest BuildBuySellBookingPayload(OctopusInvoiceContext context, int relationId)
        //{
        //    var currency = string.IsNullOrWhiteSpace(context.Invoice.CurrencyCode)
        //        ? "EUR"
        //        : context.Invoice.CurrencyCode!;

        //    var bookingLines = context.Invoice.InvoicesDetails.Select(line =>
        //    {
        //        var baseAmount = line.Price ?? 0m;
        //        var vatPercentage = line.VatPercentage ?? 0m;

        //        return new OctopusBuySellBookingLineServiceData
        //        {
        //            AccountKey = 0,
        //            BaseAmount = baseAmount,
        //            VatCodeKey = ResolveVatCodeKey(line, context.VatTypes, context.Issuer.DefaultVatTypeId),
        //            VatAmount = Math.Round(baseAmount * vatPercentage / 100m, 2, MidpointRounding.AwayFromZero),
        //            Comment = line.Text,
        //            VatRecupPercentage = 0m
        //        };
        //    }).ToList();

        //    return new OctopusBuySellBookingAndAttachmentRequest
        //    {
        //        BuySellBookingServiceData = new OctopusBuySellBookingServiceData
        //        {
        //            BookyearKey = new OctopusBookyearKeyRef { Id = context.BookyearId },
        //            JournalKey = context.JournalKey,
        //            DocumentSequenceNr = context.DocumentSequenceNr,
        //            RelationIdentificationServiceData = new OctopusRelationIdentificationServiceData
        //            {
        //                RelationKey = new OctopusRelationKeyRef { Id = relationId },
        //                ExternalRelationId = context.Invoice.ClientIdClientAccountNavigation?.OctopusRelationId
        //                    ?? context.Invoice.ClientId
        //                    ?? context.Invoice.CompanyId
        //                    ?? relationId
        //            },
        //            BookyearPeriodeNr = context.PeriodNumber,
        //            DocumentDate = context.FinalDate,
        //            ExpiryDate = context.Invoice.ExpirationDate ?? context.FinalDate,
        //            Comment = context.Invoice.Text,
        //            Reference = context.StructuredOgm,
        //            Amount = context.Detail.TotalInclVat,
        //            CurrencyCode = currency,
        //            ExchangeRate = 1m,
        //            BookingLines = bookingLines,
        //            PaymentMethod = 0
        //        }
        //    };
        //}

        // Stap 5: genereer PDF en upload naar Octopus (wordt ook gebruikt voor e-mail).
        private async Task<OctopusInvoiceAttachment> UploadInvoiceAttachmentAsync(
            OctopusInvoiceContext context,
            OctopusDossierTokenResult dossierToken,
            bool sendOnly,
            CancellationToken ct,
            OctopusWorkflowProgress progress)
        {
            if (progress.UploadCompleted && !context.ForceFinalPdf)
            {
                // Upload is reeds gebeurd, maar we hebben nog steeds de PDF-inhoud nodig voor eventuele mailverzending.
                return await BuildAttachmentAsync(context, ct);
            }

            if (sendOnly)
                throw new InvalidOperationException("Octopus-factuurbijlage is nog niet geüpload.");

            var attachment = await BuildAttachmentAsync(context, ct);

            var uploaded = await _octopusClient.UploadInvoiceAttachmentAsync(
                dossierToken.Token,
                context.DossierNumber,
                new OctopusInvoiceAttachmentUploadRequest
                {
                    BookyearId = context.BookyearId,
                    JournalKey = context.JournalKey,
                    DocumentSequenceNumber = context.DocumentSequenceNr,
                    InvoiceNumber = attachment.InvoiceNumber,
                    AttachmentType = "Invoice",
                    FileName = attachment.FileName,
                    Content = attachment.PdfBytes,
                    ContentType = "application/pdf"
                },
                ct);

            if (!uploaded)
            {
                throw new InvalidOperationException("Octopus gaf geen bevestiging terug bij het uploaden van de factuurbijlage.");
            }

            await PersistOctopusWorkflowStateAsync(context.Invoice, OctopusWorkflowStateAttachmentUploaded, ct);
            UpdateProgressFromState(progress, context.Invoice.OctopusWorkflowState);

            return attachment;
        }

        // Stap 5b: bouw de PDF-bijlage (status kan "forced final" zijn tijdens issue).
        private async Task<OctopusInvoiceAttachment> BuildAttachmentAsync(OctopusInvoiceContext context, CancellationToken ct)
        {
            var issuerCompany = await _ics.GetAsync(context.Issuer.Id, ct)
                ?? throw new InvalidOperationException("Facturatiebedrijf niet gevonden voor PDF-opmaak.");

            var vatTypes = context.VatTypes
                .Select(v => new VatTypeBO
                {
                    Id = v.Id,
                    IssuerCompanyId = v.IssuerCompanyId,
                    Code = v.Code,
                    Description = v.Description,
                    Type = v.Type,
                    BasePercentage = v.BasePercentage,
                    DefaultSellBookingAccountNr = v.DefaultSellBookingAccountNr,
                    InvoiceMention = v.InvoiceMention
                })
                .ToList();

            var invoiceDto = context.Detail.ToInvoiceDto(vatTypes);
            invoiceDto.PublicId ??= context.FormattedPublicId;
            if (context.ForceFinalPdf)
            {
                // Zorg dat de PDF nooit als proforma wordt aangemerkt tijdens issue.
                invoiceDto.Status = "Issued";
            }
            if (!string.IsNullOrWhiteSpace(context.StructuredOgm))
            {
                invoiceDto.StructuredMessage = context.StructuredOgm;
            }

            var pdfBytes = await RenderInvoicePdfWithAppendixAsync(context.Invoice.Id, invoiceDto, issuerCompany, ct);
            var invoiceNumber = context.FormattedPublicId;
            var invalidChars = Path.GetInvalidFileNameChars();
            var safePublicId = string.IsNullOrWhiteSpace(context.FormattedPublicId) ? context.Invoice.Id.ToString(CultureInfo.InvariantCulture) : context.FormattedPublicId;
            var sanitizedId = new string((safePublicId ?? string.Empty).Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());
            var fileName = $"factuur {sanitizedId}.pdf";

            return new OctopusInvoiceAttachment(pdfBytes, fileName, invoiceNumber);
        }

        // Stap 6: vraag Octopus om de factuur te verzenden (externe verzending).
        private async Task<OctopusInvoiceSendResponse?> SendInvoiceViaOctopusAsync(
            OctopusInvoiceContext context,
            OctopusDossierTokenResult dossierToken,
            CancellationToken ct)
        {
            if (context.SkipSendStep)
            {
                _logger.LogInformation(
                      "Skipping send for invoice {InvoiceId}: digital delivery not required or no destination email",
                    context.Invoice.Id);
                return null;
            }

            var sendRequest = new OctopusInvoiceSendRequest
            {
                BookyearKey = new OctopusBookyearKeyRef { Id = context.BookyearId },
                Journal = context.JournalKey,
                DocumentSequenceNr = context.DocumentSequenceNr,
                ToDocumentSequenceNr = context.DocumentSequenceNr,
                FromMailAddress = string.IsNullOrWhiteSpace(context.Issuer.InvoiceSendEmail)
                    ? context.Issuer.Email
                    : context.Issuer.InvoiceSendEmail,
                CcMailAddress = null,
                BccMailAddress = null,
                ExcludeOctopusPdf = true,
                ForceUseEmail = false
            };

            var sendResponse = await _octopusClient.SendInvoiceAsync(
                dossierToken.Token,
                context.DossierNumber,
                sendRequest,
                ct);

            if (sendResponse is null || !sendResponse.Success)
            {
                _logger.LogWarning(
                    "Octopus send failed for invoice {InvoiceId} in dossier {DossierNumber}. Continuing with classic send.",
                    context.Invoice.Id,
                    context.DossierNumber);
                return sendResponse;
            }

            var sendDocumentKey = sendResponse?.SendInvoiceStatusList?.FirstOrDefault()?.DocumentKey;
            context.DocumentSequenceNr = sendDocumentKey?.DocumentSequenceNr > 0
                ? sendDocumentKey!.DocumentSequenceNr
                : context.DocumentSequenceNr;

            context.Invoice.OctopusBookyearId = sendDocumentKey?.BookyearKey?.Id ?? context.BookyearId;
            context.Invoice.OctopusJournalKey = string.IsNullOrWhiteSpace(sendDocumentKey?.Journal)
                ? context.JournalKey
                : sendDocumentKey!.Journal;
            context.Invoice.OctopusDocumentSequenceNr = context.DocumentSequenceNr;
            context.Invoice.OctopusDeliveryState ??= "NONE";
            context.Invoice.OctopusDeliveryUpdatedAt = DateTime.UtcNow;
            await PersistOctopusWorkflowStateAsync(context.Invoice, OctopusWorkflowStateSent, ct);

            return sendResponse;
        }

        // Stap 6b: publicId aanpassen op basis van Octopus-nummering (indien beschikbaar).
        private async Task<bool> UpdateInvoicePublicIdFromOctopusAsync(OctopusInvoiceContext context, CancellationToken ct)
        {
            if (context.DocumentSequenceNr <= 0)
                return false;

            var formattedPublicId = FormatInvoiceNumber(
                context.Issuer.InvoiceNumberPattern,
                context.DocumentSequenceNr,
                context.FinalDate);
            var structuredOgm = GenerateStructuredOgm(context.FiscalYear, context.DocumentSequenceNr);

            var updated = false;

            if (!string.Equals(context.Invoice.PublicId, formattedPublicId, StringComparison.Ordinal))
            {
                context.Invoice.PublicId = formattedPublicId;
                updated = true;
            }

            if (!string.Equals(context.Detail.PublicId, formattedPublicId, StringComparison.Ordinal))
            {
                context.Detail.PublicId = formattedPublicId;
                updated = true;
            }

            if (!string.Equals(context.FormattedPublicId, formattedPublicId, StringComparison.Ordinal))
            {
                context.FormattedPublicId = formattedPublicId;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(structuredOgm)
                && !string.Equals(context.Invoice.StructuredCommOgm, structuredOgm, StringComparison.Ordinal))
            {
                context.Invoice.StructuredCommOgm = structuredOgm;
                context.Detail.StructuredMessage = structuredOgm;
                context.StructuredOgm = structuredOgm;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(structuredOgm))
            {
                var updatedQr = BuildEpcQrPayload(
                    context.Issuer,
                    context.Detail.BankAccount,
                    structuredOgm,
                    context.Detail.TotalInclVat);

                if (!string.IsNullOrWhiteSpace(updatedQr)
                    && !string.Equals(context.Invoice.QrEpcPayload, updatedQr, StringComparison.Ordinal))
                {
                    context.Invoice.QrEpcPayload = updatedQr;
                    context.Detail.QrPayLoad = updatedQr;
                    updated = true;
                }
            }

            if (updated)
            {
                await _db.SaveChangesAsync(ct);
            }

            return updated;
        }

        // Stap 7: klassieke e-mailverzending als er geen (of geen succesvolle) digitale route is.
        private async Task<bool> SendClassicInvoiceEmailIfRequiredAsync(
           OctopusInvoiceContext context,
           OctopusInvoiceAttachment attachment,
           CancellationToken ct)
        {
            // Wanneer Peppol niet van toepassing is, kan de Octopus-stroom aangevuld worden met een klassieke mail.
            // Het PDF-bestand is identiek aan de upload naar Octopus.
            if (context.SkipSendStep || string.IsNullOrWhiteSpace(context.ClientEmail))
                return false;

            try
            {
                var issuerBo = await _ics.GetAsync(context.Issuer.Id, ct)
                    ?? throw new InvalidOperationException("Facturatiebedrijf niet gevonden voor e-mailopmaak.");

                var templateModel = BuildEmailTemplateModel(
                    context.Detail,
                    issuerBo,
                    context.Detail.BankAccount,
                    context.Detail.Currency ?? "EUR");

                var subjectTemplate = issuerBo.EmailSubjectTemplate;
                var renderedSubject = !string.IsNullOrWhiteSpace(subjectTemplate)
                    ? _templateInterpolator.Interpolate(subjectTemplate, templateModel).Trim()
                    : string.Empty;
                var mailSubject = !string.IsNullOrWhiteSpace(renderedSubject)
                    ? renderedSubject
                    : $"Factuur {context.FormattedPublicId ?? context.Invoice.Id.ToString(CultureInfo.InvariantCulture)}";

                var bodyTemplate = issuerBo.EmailBodyTemplate;
                if (context.Detail.IsPrepaid && !string.IsNullOrWhiteSpace(issuerBo.EmailPaidBodyTemplate))
                {
                    bodyTemplate = issuerBo.EmailPaidBodyTemplate;
                }
                var renderedBody = !string.IsNullOrWhiteSpace(bodyTemplate)
                    ? _templateInterpolator.Interpolate(bodyTemplate, templateModel).Trim()
                    : string.Empty;
                var mailBody = !string.IsNullOrWhiteSpace(renderedBody)
                    ? renderedBody
                    : BuildDefaultEmailBody(context.Detail, issuerBo, context.Detail.Currency ?? "EUR", context.Detail.BankAccount ?? string.Empty);

                await _emailSender.SendEmailAsync(
                    context.ClientEmail,
                    mailSubject,
                    mailBody,
                    new List<EmailAttachment> { new(attachment.FileName, attachment.PdfBytes, "application/pdf") },
                    context.ClientCc,
                    context.Issuer.InvoiceSendEmail,
                    context.Issuer.InvoiceSendEmail);

                await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
                {
                    InvoiceId = context.Invoice.Id,
                    ToAddress = context.ClientEmail,
                    CcAddress = context.ClientCc,
                    Subject = mailSubject,
                    ProviderId = context.DossierNumber,
                    SentAt = DateTime.UtcNow,
                    Status = "Sent"
                }, ct);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Classic invoice email failed for invoice {InvoiceId}", context.Invoice.Id);
                return false;
            }
        }
        // Stap 8: logging + verzendgeschiedenis bijwerken.
        private async Task LogOctopusBookingAsync(OctopusInvoiceContext context, CancellationToken ct)
        {
            var userName = User?.Identity?.Name ?? "Onbekende gebruiker";

            await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
            {
                InvoiceId = context.Invoice.Id,
                ToAddress = "Octopus",
                CcAddress = userName,
                Subject = $"Boeking aangemaakt in Octopus door {userName}",
                ProviderId = context.DossierNumber,
                SentAt = DateTime.UtcNow,
                Status = "Booked"
            }, ct);
        }

        private async Task LogOctopusSendAsync(
            OctopusInvoiceContext context,
            OctopusInvoiceSendResponse? sendResponse,
            CancellationToken ct)
        {
            var userName = User?.Identity?.Name ?? "Onbekende gebruiker";
            var sentAtUtc = DateTime.UtcNow;

            await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
            {
                InvoiceId = context.Invoice.Id,
                ToAddress = "Octopus",
                CcAddress = userName,
                Subject = $"Doorgestuurd naar Octopus door {userName}",
                ProviderId = context.DossierNumber,
                SentAt = sentAtUtc,
                Status = "Uploaded"
            }, ct);

            if (sendResponse?.SendInvoiceStatusList != null)
            {
                foreach (var status in sendResponse.SendInvoiceStatusList)
                {
                    var method = string.IsNullOrWhiteSpace(status.SendMethod) ? "Octopus" : status.SendMethod!;
                    var subject = $"Octopus verzendstatus ({method})";
                    if (!string.IsNullOrWhiteSpace(status.Comment))
                    {
                        subject = $"{subject}: {Truncate(status.Comment, 120)}";
                    }

                    await _communication.SaveEmailLogAsync(new InvoiceEmailLogBO
                    {
                        InvoiceId = context.Invoice.Id,
                        ToAddress = method,
                        CcAddress = userName,
                        Subject = Truncate(subject, 200),
                        ProviderId = context.DossierNumber,
                        SentAt = sentAtUtc,
                        Status = status.Success ? "Sent" : "Error"
                    }, ct);
                }
            }
        }
        private static OctopusWorkflowProgress GetOctopusWorkflowProgress(string? workflowState)
        {
            var creationCompleted = IsOctopusStepCompleted(workflowState, OctopusWorkflowStateCreated);
            var uploadCompleted = IsOctopusStepCompleted(workflowState, OctopusWorkflowStateAttachmentUploaded);
            var sendCompleted = IsOctopusStepCompleted(workflowState, OctopusWorkflowStateSent);

            return new OctopusWorkflowProgress
            {
                CreationCompleted = creationCompleted,
                UploadCompleted = uploadCompleted,
                SendCompleted = sendCompleted
            };
        }

        private static void UpdateProgressFromState(OctopusWorkflowProgress progress, string? workflowState)
        {
            var updated = GetOctopusWorkflowProgress(workflowState);
            progress.CreationCompleted = updated.CreationCompleted;
            progress.UploadCompleted = updated.UploadCompleted;
            progress.SendCompleted = updated.SendCompleted;
        }

        private static bool IsOctopusStepCompleted(string? currentState, string targetState)
        {
            if (string.IsNullOrWhiteSpace(targetState))
                return false;

            if (!OctopusWorkflowOrder.TryGetValue(targetState, out var targetRank))
                return false;

            if (string.IsNullOrWhiteSpace(currentState))
                return false;

            if (!OctopusWorkflowOrder.TryGetValue(currentState, out var currentRank))
                return false;

            return currentRank >= targetRank;
        }

        // Helper voor stap 7/8: delivery state opslaan voor statusweergave.

        private async Task PersistOctopusWorkflowStateAsync(Invoices invoice, string targetState, CancellationToken ct)
        {
            invoice.OctopusWorkflowState = targetState;
            invoice.OctopusWorkflowUpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        private async Task TryUpdateOctopusDeliveryStateAsync(InvoiceDetailBO detail, CancellationToken ct)
        {
            if (detail == null)
                return;

            if (detail.OctopusDocumentSequenceNr is null or <= 0)
                return;

            if (detail.OctopusBookyearId is null or <= 0)
                return;

            if (string.IsNullOrWhiteSpace(detail.OctopusJournalKey))
                return;

            if (!RefreshableOctopusStates.Contains(detail.OctopusDeliveryState ?? string.Empty))
                return;

            var issuer = await _ics.GetAsync(detail.IssuerCompanyId, ct);
            if (issuer == null || string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
                return;

            try
            {
                var dossierToken = await _octopusTokens.RefreshDossierTokenAsync(issuer.Id, issuer.OctopusDossierNumber, ct);
                var selection = new OctopusDocumentSelectionData
                {
                    BookyearKey = new OctopusBookyearKeyRef { Id = detail.OctopusBookyearId ?? 0 },
                    Journal = detail.OctopusJournalKey,
                    DocumentSequenceNr = detail.OctopusDocumentSequenceNr ?? 0,
                    ToDocumentSequenceNr = detail.OctopusDocumentSequenceNr ?? 0
                };

                var deliveryStates = await _octopusClient.GetInvoiceDeliveryStatesAsync(
                    dossierToken.Token,
                    issuer.OctopusDossierNumber,
                    selection,
                    ct);

                var state = deliveryStates?.FirstOrDefault();
                if (state != null)
                {
                    await PersistOctopusDeliveryStateAsync(detail.Id, state, ct);
                    detail.OctopusDeliveryState = state.DeliveryState;
                    detail.OctopusDeliveryComment = state.Comment;
                    detail.OctopusDeliveryDateTime = state.DeliveryDateTime;
                    detail.OctopusDeliveryUpdatedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Octopus delivery state update failed for invoice {InvoiceId}", detail.Id);
            }
        }

        private async Task PersistOctopusDeliveryStateAsync(int invoiceId, OctopusDocumentDeliveryState state, CancellationToken ct)
        {
            var entity = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
            if (entity == null)
                return;

            entity.OctopusDeliveryState = state.DeliveryState;
            entity.OctopusDeliveryComment = state.Comment;
            entity.OctopusDeliveryDateTime = state.DeliveryDateTime;
            entity.OctopusDeliveryUpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        private async Task<OctopusRelation?> EnsureOctopusRelationAsync(
                  string dossierToken,
                  string dossierNumber,
                  IReadOnlyList<OctopusRelationLookup> lookups,
                  OctopusRelationRequest request,
                  CancellationToken ct)
        {
            OctopusRelation? found = null;
            foreach (var lookup in lookups)
            {
                var relation = await _octopusClient.FindRelationAsync(dossierToken, dossierNumber, lookup, ct);

                if (relation != null)
                {
                    found = relation;
                    break;
                }
            }

            if (found != null)
            {
                request.RelationIdentificationServiceData ??= new OctopusRelationIdentificationData();
                request.RelationIdentificationServiceData.RelationKey ??= found.RelationIdentificationServiceData?.RelationKey;

                if (NeedsRelationUpdate(found, request))
                {
                    return await _octopusClient.UpsertRelationAsync(dossierToken, dossierNumber, request, ct);
                }

                return found;
            }

            return await _octopusClient.UpsertRelationAsync(dossierToken, dossierNumber, request, ct);
        }

        private async Task<int?> GetIssuerSpecificOctopusRelationIdAsync(
        int issuerCompanyId,
        ClientAccount? clientAccount,
        ClientContacts? clientContact,
        CompanyInfo? company,
        CancellationToken ct)
        {
            var connection = _db.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(ct);
            }

            try
            {
                async Task<int?> QueryRelationIdAsync(string table, string entityColumn, int entityId)
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText =
                        $"SELECT TOP 1 OctopusRelationId FROM {table} WHERE {entityColumn} = @entityId AND IssuerCompanyId = @issuerId";

                    var entityParameter = command.CreateParameter();
                    entityParameter.ParameterName = "@entityId";
                    entityParameter.Value = entityId;
                    entityParameter.DbType = DbType.Int32;
                    command.Parameters.Add(entityParameter);

                    var issuerParameter = command.CreateParameter();
                    issuerParameter.ParameterName = "@issuerId";
                    issuerParameter.Value = issuerCompanyId;
                    issuerParameter.DbType = DbType.Int32;
                    command.Parameters.Add(issuerParameter);

                    var result = await command.ExecuteScalarAsync(ct);
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }

                    return null;
                }

                // Elke factuurpartij (account / contact / bedrijf) krijgt zijn eigen Octopus-relatie.
                // Een mede-eigenaar-factuur (ClientContact) mag NIET terugvallen op het account:
                // dan zouden hoofd- en mede-eigenaar dezelfde relatie-id delen en elkaars naam
                // overschrijven bij het boeken.
                if (clientAccount?.Id is int clientAccountId)
                {
                    var relationId = await QueryRelationIdAsync("ClientAccountIssuerCompany", "ClientAccountId", clientAccountId);
                    if (relationId.HasValue)
                    {
                        return relationId;
                    }
                }

                if (clientContact?.Id is int clientContactId)
                {
                    var relationId = await QueryRelationIdAsync("ClientContactIssuerCompany", "ClientContactId", clientContactId);
                    if (relationId.HasValue)
                    {
                        return relationId;
                    }
                }

                if (company?.CompanyId is int companyId)
                {
                    var relationId = await QueryRelationIdAsync("CompanyIssuerCompany", "CompanyId", companyId);
                    if (relationId.HasValue)
                    {
                        return relationId;
                    }
                }

                return null;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task SetIssuerSpecificRelationIdAsync(
            int issuerCompanyId,
            ClientAccount? clientAccount,
            ClientContacts? clientContact,
            CompanyInfo? company,
            int relationId,
            CancellationToken ct)
        {
            var connection = _db.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(ct);
            }

            try
            {
                async Task UpsertRelationIdAsync(string table, string entityColumn, int entityId)
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText =
                        $@"IF EXISTS (SELECT 1 FROM {table} WHERE {entityColumn} = @entityId AND IssuerCompanyId = @issuerId)
BEGIN
    UPDATE {table}
    SET OctopusRelationId = @relationId
    WHERE {entityColumn} = @entityId AND IssuerCompanyId = @issuerId;
END
ELSE
BEGIN
    INSERT INTO {table} ({entityColumn}, IssuerCompanyId, OctopusRelationId)
    VALUES (@entityId, @issuerId, @relationId);
END";

                    var entityParameter = command.CreateParameter();
                    entityParameter.ParameterName = "@entityId";
                    entityParameter.Value = entityId;
                    entityParameter.DbType = DbType.Int32;
                    command.Parameters.Add(entityParameter);

                    var issuerParameter = command.CreateParameter();
                    issuerParameter.ParameterName = "@issuerId";
                    issuerParameter.Value = issuerCompanyId;
                    issuerParameter.DbType = DbType.Int32;
                    command.Parameters.Add(issuerParameter);

                    var relationParameter = command.CreateParameter();
                    relationParameter.ParameterName = "@relationId";
                    relationParameter.Value = relationId;
                    relationParameter.DbType = DbType.Int32;
                    command.Parameters.Add(relationParameter);

                    await command.ExecuteNonQueryAsync(ct);
                }

                // Enkel de eigen partij bijwerken. Een mede-eigenaar-factuur (ClientContact)
                // mag het relatie-id NIET ook op het gedeelde account wegschrijven, anders
                // pikt de hoofdeigenaar-factuur diezelfde relatie op.
                if (clientAccount?.Id is int clientAccountId)
                {
                    await UpsertRelationIdAsync("ClientAccountIssuerCompany", "ClientAccountId", clientAccountId);
                }

                if (clientContact?.Id is int clientContactId)
                {
                    await UpsertRelationIdAsync("ClientContactIssuerCompany", "ClientContactId", clientContactId);
                }

                if (company?.CompanyId is int companyId)
                {
                    await UpsertRelationIdAsync("CompanyIssuerCompany", "CompanyId", companyId);
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        // externalRelationId per factuurpartij. ClientAccount.Id, ClientContacts.Id en
        // CompanyInfo.CompanyId zijn aparte ID-reeksen die overlappen; elke bron krijgt daarom
        // een eigen bereik (zie OctopusExternalRelationIds) zodat Octopus ze niet verwart.
        private static int ResolveExternalRelationId(Invoices invoice)
        {
            if (invoice.ClientType == (int)InvoicePartyType.ClientContact && invoice.ClientId is int contactId)
                return OctopusExternalRelationIds.ClientContactOffset + contactId;

            if (invoice.ClientType == (int)InvoicePartyType.ClientAccount && invoice.ClientId is int accountId)
                return accountId;

            if (invoice.CompanyId is int companyId)
                return OctopusExternalRelationIds.CompanyOffset + companyId;

            return invoice.ClientId ?? 0;
        }

        private static IReadOnlyList<OctopusRelationLookup> BuildRelationLookups(
            Invoices invoice,
            ClientAccount? clientAccount,
            ClientContacts? clientContact = null,
            CompanyInfo? company = null,
            int? issuerRelationId = null)
        {
            var lookups = new List<OctopusRelationLookup>();

            if (issuerRelationId is int issuerSpecificRelationId and > 0)
            {
                lookups.Add(new OctopusRelationLookup { RelationId = issuerSpecificRelationId });
            }

            var vatNumber = FormatVatNumberForOctopus(
                invoice.VatNumber ?? clientAccount?.Vatnumber ?? clientContact?.Vatnumber ?? company?.VatNumber ?? company?.Ondernemingsnummer,
                clientAccount?.InvoicePostalCode?.Country?.LandIsocode
                    ?? clientAccount?.PostalCode?.Country?.LandIsocode
                    ?? clientContact?.InvoicePostalCode?.Country?.LandIsocode
                    ?? clientContact?.PostalCode?.Country?.LandIsocode
                     ?? company?.PostCode?.Country?.LandIsocode
                    ?? invoice.PostalCode?.Country?.LandIsocode
                    ?? company?.LandCode);
            if (!string.IsNullOrWhiteSpace(vatNumber))
            {
                lookups.Add(new OctopusRelationLookup { VatNumber = vatNumber });
            }

            var clientName = SelectClientName(invoice, clientAccount, clientContact, company);

            if (!string.IsNullOrWhiteSpace(clientName))
            {
                lookups.Add(new OctopusRelationLookup { Name = clientName });
            }

            return lookups;
        }


        private static bool NeedsRelationUpdate(OctopusRelation current, OctopusRelationRequest desired)
        {
            static bool Different(string? a, string? b)
                => !string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

            if (desired == null)
                return false;

            if (Different(current.Name, desired.Name)) return true;
            if (Different(current.Firstname, desired.Firstname)) return true;
            if (Different(current.StreetAndNr, desired.StreetAndNr)) return true;
            if (Different(current.PostalCode, desired.PostalCode)) return true;
            if (Different(current.City, desired.City)) return true;
            if (Different(current.Country, desired.Country)) return true;
            if (!string.IsNullOrWhiteSpace(desired.VatNr) && Different(current.VatNr, desired.VatNr)) return true;
            if (desired.VatType.HasValue && current.VatType != desired.VatType) return true;
            if (Different(current.CurrencyCode, desired.CurrencyCode)) return true;
            if (current.Client != desired.Client || current.Supplier != desired.Supplier || current.Active != desired.Active) return true;

            // Een bestaand externalRelationId dat afwijkt van de canonieke (offset-)waarde zetten
            // we recht: relaties die ooit met het rauwe id zijn gepusht migreren zo vanzelf mee bij
            // de volgende boeking, zonder manuele opkuis. (Ontbreekt het veld in de GET-respons van
            // Octopus, dan forceren we geen update — de factuur-payload draagt het juiste id al.)
            var wanted = desired.RelationIdentificationServiceData?.ExternalRelationId;
            var have = current.RelationIdentificationServiceData?.ExternalRelationId;
            if (wanted is int w && w > 0 && have is int h && h != w)
            {
                return true;
            }

            return false;
        }

        private static OctopusRelationRequest BuildRelationRequest(
          Invoices invoice,
          ClientAccount? clientAccount,
          ClientContacts? clientContact,
          CompanyInfo? company,
          int? issuerRelationId)
        {
            var clientName = SelectClientName(invoice, clientAccount, clientContact, company);
            var countryCode = clientAccount?.InvoicePostalCode?.Country?.LandIsocode
                ?? clientAccount?.PostalCode?.Country?.LandIsocode
                ?? clientContact?.InvoicePostalCode?.Country?.LandIsocode
                ?? clientContact?.PostalCode?.Country?.LandIsocode
                ?? company?.PostCode?.Country?.LandIsocode
                ?? invoice.PostalCode?.Country?.LandIsocode
                ?? company?.LandCode;
            var isCompany = !string.IsNullOrWhiteSpace(clientAccount?.CompanyName)
                || (!string.IsNullOrWhiteSpace(clientAccount?.Vatnumber))
                || (!string.IsNullOrWhiteSpace(invoice.VatNumber) && invoice.ClientType != (int)InvoicePartyType.ClientContact)
                || (!string.IsNullOrWhiteSpace(company?.VatNumber) || !string.IsNullOrWhiteSpace(company?.Ondernemingsnummer));

            // Echte voornaam (migratie 057) wint; zonder Forename (niet-gemigreerde accounts) blijft dit
            // exact het bestaande surrogaat: het hele Name-veld als "voornaam" naar Octopus.
            var firstName = clientContact?.Forename
                ?? clientAccount?.Forename
                ?? clientAccount?.Name
                ?? clientName;

            var request = new OctopusRelationRequest
            {
                RelationIdentificationServiceData = new OctopusRelationIdentificationData
                {
                    RelationKey = issuerRelationId.HasValue && issuerRelationId.Value > 0
                        ? new OctopusRelationKey { Id = issuerRelationId.Value }
                        : null,
                    // ClientAccount.Id, ClientContacts.Id en CompanyInfo.CompanyId zijn onafhankelijke
                    // ID-reeksen die overlappen — elke bron krijgt een eigen bereik (zie OctopusExternalRelationIds).
                    ExternalRelationId = ResolveExternalRelationId(invoice)
                },
                Name = clientName,
                Firstname = firstName,
                Client = true,
                Supplier = company != null,
                Active = true,
                StreetAndNr = BuildStreetAndNumber(
                    clientAccount?.InvoiceStreet ?? clientAccount?.Street ?? clientContact?.InvoiceStreet ?? clientContact?.Street,
                    clientAccount?.InvoiceHousenumber ?? clientAccount?.Housenumber ?? clientContact?.InvoiceHousenumber ?? clientContact?.Housenumber,
                    clientAccount?.InvoiceBusnumber ?? clientAccount?.Busnumber ?? clientContact?.InvoiceBusnumber ?? clientContact?.Busnumber ?? company?.Busnummer,
                    invoice.Adress ?? company?.Straat),
                PostalCode = clientAccount?.InvoicePostalCode?.Postcode ?? clientAccount?.PostalCode?.Postcode ?? clientContact?.InvoicePostalCode?.Postcode ?? clientContact?.PostalCode?.Postcode ?? company?.Postcode ?? invoice.PostalCode?.Postcode,
                City = clientAccount?.InvoicePostalCode?.Gemeente ?? clientAccount?.PostalCode?.Gemeente ?? clientContact?.InvoicePostalCode?.Gemeente ?? clientContact?.PostalCode?.Gemeente ?? company?.Gemeente ?? invoice.PostalCode?.Gemeente,
                Country = countryCode,
                VatNr = FormatVatNumberForOctopus(invoice.VatNumber ?? clientAccount?.Vatnumber ?? clientContact?.Vatnumber ?? company?.VatNumber ?? company?.Ondernemingsnummer, countryCode),
                CurrencyCode = "EUR",
                DefaultBookingAccountClient = 0,
                DefaultBookingAccountSupplier = 0,
                SupplierPaymentMethod = 0,
                VatType = DetermineVatType(isCompany, countryCode)
            };

            request.Country ??= "BE";

            return request;
        }

        private static string? SelectClientName(Invoices invoice, ClientAccount? clientAccount, ClientContacts? clientContact = null, CompanyInfo? company = null)
        {
            if (!string.IsNullOrWhiteSpace(clientAccount?.CompanyName))
            {
                return clientAccount.CompanyName;
            }

            if (!string.IsNullOrWhiteSpace(clientContact?.CompanyName))
            {
                return clientContact.CompanyName;
            }

            if (!string.IsNullOrWhiteSpace(company?.BedrijfsNaam))
            {
                return company.BedrijfsNaam;
            }

            // Migratie 057: achternaam + voornaam (zelfde volgorde als ClientAccountBO.DisplayName en
            // InvoiceCommandService.ResolvePartySnapshotAsync) — voordien enkel Name, waardoor een
            // gesplitste klant op de factuur/partij enkel met zijn achternaam verscheen.
            if (!string.IsNullOrWhiteSpace(clientAccount?.Name))
            {
                return string.IsNullOrWhiteSpace(clientAccount.Forename)
                    ? clientAccount.Name
                    : clientAccount.Name.Trim() + " " + clientAccount.Forename.Trim();
            }

            if (!string.IsNullOrWhiteSpace(clientContact?.Name))
            {
                return string.IsNullOrWhiteSpace(clientContact.Forename)
                    ? clientContact.Name
                    : clientContact.Name.Trim() + " " + clientContact.Forename.Trim();
            }

            return invoice.ClientName;
        }

        /// <summary>Achternaam + voornaam (migratie 057), null als er geen achternaam is — voor de
        /// sjabloonvelden/terugvalketens die voordien enkel <c>Name</c> namen.</summary>
        private static string? JoinNameForename(string? name, string? forename)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return string.IsNullOrWhiteSpace(forename) ? name : name.Trim() + " " + forename.Trim();
        }
        private static string? BuildStreetAndNumber(string? street, string? houseNumber, string? busNumber, string? fallback)
        {
            var parts = new[] { street, houseNumber, string.IsNullOrWhiteSpace(busNumber) ? null : busNumber }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var result = string.Join(" ", parts);

            if (string.IsNullOrWhiteSpace(result))
                return fallback;

            return result;
        }

        private static string? NormalizeVatNumber(string? vatNumber)
        {
            if (string.IsNullOrWhiteSpace(vatNumber))
                return vatNumber;

            var trimmed = vatNumber.Trim();
            var cleaned = new string(trimmed.Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? trimmed : cleaned;
        }

        private static string? FormatVatNumberForOctopus(string? vatNumber, string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(vatNumber))
                return null;

            var cleanedDigits = new string(vatNumber.Where(char.IsDigit).ToArray());
            if (cleanedDigits.Length >= 10)
            {
                var digits = cleanedDigits[^10..];
                var prefix = string.IsNullOrWhiteSpace(countryCode) ? "BE" : countryCode.Trim().ToUpperInvariant();
                return $"{prefix}{digits[..4]}.{digits.Substring(4, 3)}.{digits.Substring(7, 3)}";
            }

            return NormalizeVatNumber(vatNumber);
        }

        private static int? DetermineVatType(bool isCompany, string? countryCode)
        {
            var isBelgian = string.Equals(countryCode, "BE", StringComparison.OrdinalIgnoreCase);

            if (isCompany)
                return isBelgian ? 1 : 4;

            return isBelgian ? 7 : 8;
        }

        private static string ResolveVatCodeKey(InvoicesDetails line, IReadOnlyCollection<Vattype> vatTypes, int? defaultVatTypeId)
        {
            const decimal tolerance = 0.001m;

            if (vatTypes == null || vatTypes.Count == 0)
            {
                throw new InvalidOperationException("Geen VAT-types geconfigureerd voor dit facturatiebedrijf.");
            }

            if (!string.IsNullOrWhiteSpace(line.VatCode))
            {
                return line.VatCode.Trim();
            }

            if (line.VatTypeId.HasValue)
            {
                var typeMatch = vatTypes.FirstOrDefault(v => v.Id == line.VatTypeId.Value);
                if (typeMatch != null && !string.IsNullOrWhiteSpace(typeMatch.Code))
                {
                    return typeMatch.Code;
                }
            }

            if (line.VatPercentage.HasValue)
            {
                var match = vatTypes.FirstOrDefault(v => Math.Abs(v.BasePercentage - line.VatPercentage.Value) < tolerance);

                if (match != null && !string.IsNullOrWhiteSpace(match.Code))
                {
                    return match.Code;
                }
            }

            if (defaultVatTypeId.HasValue)
            {
                var defaultMatch = vatTypes.FirstOrDefault(v => v.Id == defaultVatTypeId.Value);
                if (defaultMatch != null && !string.IsNullOrWhiteSpace(defaultMatch.Code))
                {
                    return defaultMatch.Code;
                }
            }

            var firstWithCode = vatTypes.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Code));
            if (firstWithCode != null)
            {
                return firstWithCode.Code;
            }

            throw new InvalidOperationException(
                line.VatPercentage.HasValue
                    ? $"Geen VAT-code gevonden voor percentage {line.VatPercentage:0.##} voor dit facturatiebedrijf."
                    : "Geen VAT-code gevonden voor dit facturatiebedrijf.");
        }

        private static readonly Regex InvoicePatternTokenRegex = new(@"\{(num|date):([^}]+)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly IReadOnlyDictionary<string, Func<DateTime, CultureInfo, string>> InvoiceDateTokenResolvers =
            new Dictionary<string, Func<DateTime, CultureInfo, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["MM"] = static (d, c) => d.ToString("MM", c),
                ["MMM"] = static (d, c) => d.ToString("MMM", c),
                ["MMMM"] = static (d, c) => d.ToString("MMMM", c),
                ["yy"] = static (d, c) => d.ToString("yy", c),
                ["yyyy"] = static (d, c) => d.ToString("yyyy", c),
                ["MM-yyyy"] = static (d, c) => d.ToString("MM-yyyy", c),
            };

        private static string FormatInvoiceNumber(string? pattern, int sequenceNumber, DateOnly invoiceDate)
        {
            var resolvedPattern = string.IsNullOrWhiteSpace(pattern) ? "{num:0000}/{date:yyyy}" : pattern;
            return FormatPattern(resolvedPattern, sequenceNumber, invoiceDate.ToDateTime(TimeOnly.MinValue));
        }

        private static string FormatPattern(string? pattern, int num, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                pattern = "{num:0000}/{date:yyyy}";

            var builder = new StringBuilder();
            var cursor = 0;
            var culture = CultureInfo.CurrentCulture;

            foreach (Match token in InvoicePatternTokenRegex.Matches(pattern))
            {
                builder.Append(pattern, cursor, token.Index - cursor);

                var type = token.Groups[1].Value;
                var format = token.Groups[2].Value?.Trim();

                if (type.Equals("num", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var fmt = string.IsNullOrWhiteSpace(format) ? null : format;
                        builder.Append(fmt is null
                            ? num.ToString(culture)
                            : num.ToString(fmt, culture));
                    }
                    catch (FormatException)
                    {
                        builder.Append(num.ToString(culture));
                    }
                }
                else
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(format) && InvoiceDateTokenResolvers.TryGetValue(format, out var resolver))
                        {
                            builder.Append(resolver(date, culture));
                        }
                        else if (!string.IsNullOrWhiteSpace(format))
                        {
                            builder.Append(date.ToString(format, culture));
                        }
                        else
                        {
                            builder.Append(date.ToString(culture));
                        }
                    }
                    catch (FormatException)
                    {
                        builder.Append(date.ToString("yyyy", culture));
                    }
                }

                cursor = token.Index + token.Length;
            }

            if (cursor < pattern.Length)
                builder.Append(pattern, cursor, pattern.Length - cursor);

            return builder.ToString();
        }

        private static string? BuildStructuredOgm(Invoices invoice, int fiscalYear, int sequenceNumber)
        {
            if (!string.IsNullOrWhiteSpace(invoice.StructuredCommOgm))
            {
                return invoice.StructuredCommOgm.Trim();
            }

            return GenerateStructuredOgm(fiscalYear, sequenceNumber);
        }

        private static string? BuildEpcQrPayload(IssuerCompany issuer, string? bankAccount, string? structuredMessage, decimal amount)
        {
            if (issuer == null || !issuer.EpcQrEnabled)
                return null;
            if (string.IsNullOrWhiteSpace(issuer.EpcBeneficiaryName))
                return null;

            var iban = string.IsNullOrWhiteSpace(bankAccount) ? issuer.EpcIban : bankAccount;
            if (string.IsNullOrWhiteSpace(iban))
                return null;
            if (string.IsNullOrWhiteSpace(structuredMessage))
                return null;
            if (amount <= 0m)
                return null;

            var builder = new StringBuilder();
            builder.AppendLine("BCD");
            builder.AppendLine("001");
            builder.AppendLine("1");
            builder.AppendLine("SCT");
            builder.AppendLine((issuer.EpcBic ?? string.Empty).Trim().ToUpperInvariant());
            builder.AppendLine(TrimEpcValue(issuer.EpcBeneficiaryName, 70));
            builder.AppendLine(NormalizeIban(iban));
            builder.AppendLine($"EUR{amount.ToString("0.00", CultureInfo.InvariantCulture)}");
            builder.AppendLine();
            builder.AppendLine(TrimEpcValue(structuredMessage, 140));
            return builder.ToString();
        }

        private static string NormalizeIban(string iban)
        {
            var sb = new StringBuilder();
            foreach (var ch in iban)
            {
                if (!char.IsWhiteSpace(ch))
                    sb.Append(char.ToUpperInvariant(ch));
            }

            return sb.ToString();
        }

        private static string? FormatBankAccount(string? account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return null;

            var normalized = new string(account.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

            if (normalized.StartsWith("BE", StringComparison.Ordinal) &&
                normalized.Length == 16 &&
                normalized.Skip(2).All(char.IsDigit))
            {
                var checkDigits = normalized.Substring(2, 2);
                var remainder = normalized.Substring(4);
                return $"BE{checkDigits} {remainder.Substring(0, 4)} {remainder.Substring(4, 4)} {remainder.Substring(8, 4)}";
            }

            return account.Trim();
        }

        private static string TrimEpcValue(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned = value
                .Replace("\r", string.Empty)
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Trim();

            return cleaned.Length <= maxLength ? cleaned : cleaned.Substring(0, maxLength);
        }

        private static string? GenerateStructuredOgm(int fiscalYear, int sequenceNumber)
        {
            if (sequenceNumber <= 0)
                return null;

            var yearDigits = Math.Abs(fiscalYear % 10000);
            var numberDigits = Math.Abs(sequenceNumber % 1_000_000);
            var baseDigits = $"{yearDigits:0000}{numberDigits:000000}";
            return FormatBelgianStructuredMessage(baseDigits);
        }
        private static string FormatBookyearLabel(DateTime startDate, DateTime endDate)
            => startDate.Year == endDate.Year
                ? $"{startDate:yyyy}"
                : $"{startDate:yyyy}-{endDate:yyyy}";

        private static string? FormatBelgianStructuredMessage(string baseDigits)
        {
            if (string.IsNullOrWhiteSpace(baseDigits) || baseDigits.Length != 10)
                return null;
            if (!baseDigits.All(char.IsDigit))
                return null;

            var number = long.Parse(baseDigits, CultureInfo.InvariantCulture);
            var checksum = (int)(number % 97);
            if (checksum == 0)
                checksum = 97;

            var combined = $"{baseDigits}{checksum:00}";
            return $"+++{combined[..3]}/{combined.Substring(3, 4)}/{combined[^5..]}+++";
        }


    }
}
