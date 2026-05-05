using BOCore;
using CPMCore.Models.Leveranciers;
using CPMCore.Services.InvoiceEnrichment;
using CPMCore.Services.InvoiceExtraction;
using CPMCore.Services.Octopus;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceCore.IncomingInvoices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace CPMCore.Services.Octopus
{
    /// <summary>
    /// Synchroniseert inkomende facturen vanuit Octopus naar IncommingInvoices.
    ///
    /// Endpoint boekingen:  GET /dossiers/{id}/buysellbookings/modified
    /// Endpoint bijlagen:   GET /dossiers/{id}/bookingfiles  (file sync API)
    ///                      POST /dossiers/{id}/bookingfiles (download)
    ///
    /// Let op: max 48 calls/dag op het buysellbookings-endpoint.
    /// De incrementele aanpak (modifiedSince) houdt het verbruik laag.
    ///
    /// Dagboek-configuratie: per IssuerCompany (OctopusPurchaseJournalKeyPrefix)
    /// of globaal via appsettings.json → Octopus:PurchaseJournalKeyPrefix (standaard "A").
    /// </summary>
    public class OctopusIncomingInvoiceSyncService : IOctopusIncomingInvoiceSyncService
    {
        private readonly cpmRunningContext _db;
        private readonly IOctopusApiClient _octopus;
        private readonly IOctopusTokenManager _tokenManager;
        private readonly IIssuerCompanyService _issuerCompanyService;
        private readonly OctopusOptions _options;
        private readonly InvoiceExtractionOptions _extractionOptions;
        private readonly IAzureInvoiceAnalysisService _azureService;
        private readonly IInvoiceEnrichmentPipelineService _enrichmentPipeline;
        private readonly ILogger<OctopusIncomingInvoiceSyncService> _logger;

        public OctopusIncomingInvoiceSyncService(
            cpmRunningContext db,
            IOctopusApiClient octopus,
            IOctopusTokenManager tokenManager,
            IIssuerCompanyService issuerCompanyService,
            IOptions<OctopusOptions> options,
            IOptions<InvoiceExtractionOptions> extractionOptions,
            IAzureInvoiceAnalysisService azureService,
            IInvoiceEnrichmentPipelineService enrichmentPipeline,
            ILogger<OctopusIncomingInvoiceSyncService> logger)
        {
            _db = db;
            _octopus = octopus;
            _tokenManager = tokenManager;
            _issuerCompanyService = issuerCompanyService;
            _options = options.Value;
            _extractionOptions = extractionOptions.Value;
            _azureService = azureService;
            _enrichmentPipeline = enrichmentPipeline;
            _logger = logger;
        }

        public async Task<IncomingInvoiceSyncResultVm> SyncAsync(int issuerCompanyId, DateTime? modifiedSinceOverride = null, CancellationToken ct = default)
        {
            var result = new IncomingInvoiceSyncResultVm { SyncedAt = DateTime.UtcNow };

            // ── 1. Facturatiebedrijf + dossiernummer ────────────────────────────────────
            var company = await _issuerCompanyService.GetAsync(issuerCompanyId);
            if (company == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Facturatiebedrijf {issuerCompanyId} niet gevonden.";
                return result;
            }

            var dossierNumber = company.OctopusDossierNumber;
            if (string.IsNullOrWhiteSpace(dossierNumber))
            {
                result.Success = false;
                result.ErrorMessage = $"Facturatiebedrijf '{company.Name}' heeft geen Octopus dossiernummer geconfigureerd.";
                return result;
            }

            try
            {
                // ── 2. Bepaal modifiedSince (incrementele sync) ─────────────────────────
                // Prioriteit: expliciete override van de gebruiker → anders max SyncedAt →
                // anders 1 januari van het huidige jaar als eerste sync.
                DateTime modifiedSince;
                if (modifiedSinceOverride.HasValue)
                {
                    modifiedSince = DateTime.SpecifyKind(modifiedSinceOverride.Value, DateTimeKind.Utc);
                }
                else
                {
                    var lastSync = await _db.IncommingInvoices
                        .Where(i => i.IssuerCompanyId == issuerCompanyId
                                 && i.Source == "Octopus"
                                 && i.SyncedAt.HasValue)
                        .MaxAsync(i => (DateTime?)i.SyncedAt, ct);

                    modifiedSince = lastSync
                        ?? new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                }

                _logger.LogInformation(
                    "Octopus sync starten voor '{Company}' (dossier {Dossier}), modifiedSince={Since:u}.",
                    company.Name, dossierNumber, modifiedSince);

                // ── 3. Haal gewijzigde boekingen op via TokenManager ────────────────────
                // bookyearId = -1: alle boekjaren in één call.
                var bookings = await _tokenManager.ExecuteWithDossierTokensAsync(
                    issuerCompanyId,
                    dossierNumber,
                    (authToken, dossierToken) => _octopus.GetPurchaseInvoicesAsync(
                        dossierToken,
                        dossierNumber,
                        bookyearId: -1,
                        journalKey: null,
                        modifiedSince: modifiedSince,
                        ct: ct),
                    ct);

                if (bookings == null || bookings.Count == 0)
                {
                    _logger.LogInformation("Geen gewijzigde boekingen gevonden voor dossier {Dossier}.", dossierNumber);
                }
                else
                {
                    // ── 4. Filter: enkel aankoopjournalen bewaren ───────────────────────────
                    var prefix = !string.IsNullOrWhiteSpace(company.OctopusPurchaseJournalKeyPrefix)
                        ? company.OctopusPurchaseJournalKeyPrefix
                        : _options.PurchaseJournalKeyPrefix;
                    var filtered = string.IsNullOrWhiteSpace(prefix)
                        ? bookings.ToList()
                        : bookings
                            .Where(b => b.JournalKey?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true)
                            .ToList();

                    // ── 4b. Extra documentdatumfilter bij expliciete gebruikersdatum ────────
                    // Het Octopus-endpoint filtert op modifiedTimeStamp (= wanneer de boeking
                    // voor het laastst werd gewijzigd), NIET op de factuurdatum.
                    // Een oude factuur die onlangs in Octopus werd aangepast (bijv. betaald of
                    // gecorrigeerd) heeft een recente modifiedTimeStamp en zou anders mee komen.
                    // Wanneer de gebruiker expliciet een "vanaf datum" invult, filteren we ook
                    // op DocumentDate zodat enkel facturen met die datum of later worden opgehaald.
                    if (modifiedSinceOverride.HasValue)
                    {
                        var minDocDate = DateOnly.FromDateTime(modifiedSinceOverride.Value);
                        var beforeFilter = filtered.Count;
                        filtered = filtered.Where(b => b.DocumentDate >= minDocDate).ToList();
                        var excluded = beforeFilter - filtered.Count;
                        if (excluded > 0)
                            _logger.LogInformation(
                                "{Excluded} boeking(en) uitgesloten omdat factuurdatum vóór {MinDate:dd/MM/yyyy} valt (modifiedTimeStamp was recent).",
                                excluded, minDocDate);
                    }

                    _logger.LogInformation(
                        "Octopus leverde {Total} boekingen, {Filtered} na journaalfilter '{Prefix}*'.",
                        bookings.Count, filtered.Count, prefix);

                    // ── 5. Upsert per boeking ───────────────────────────────────────────────
                    foreach (var booking in filtered)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            var (outcome, _) = await UpsertAsync(issuerCompanyId, dossierNumber, booking, ct);
                            if (outcome == UpsertOutcome.Created) result.NewCount++;
                            else if (outcome == UpsertOutcome.Updated) result.UpdatedCount++;
                            else result.SkippedDuplicates++;
                        }
                        catch (Exception ex)
                        {
                            result.ErrorCount++;
                            _logger.LogWarning(ex, "Fout bij verwerken van boeking {ExternalId}.", booking.ExternalId);
                        }
                    }
                }

                // ── 6. Bijlagen: altijd uitvoeren, ook als er geen nieuwe boekingen zijn.
                // Facturen kunnen bijlagen missen doordat ze eerder manueel verwijderd werden
                // of doordat de file sync API de bestanden pas later beschikbaar stelde.
                var (attachCount, attachErrors) = await SyncAttachmentsAsync(
                    issuerCompanyId, dossierNumber, modifiedSince, ct);
                result.AttachmentCount = attachCount;
                result.AttachmentErrorCount = attachErrors;

                // ── 7. Verrijking voor alle nieuwe facturen zonder enrichment ────────────
                // Facturen zonder UBL-bijlage doorlopen nooit EnrichInvoiceFromUblAsync.
                // Dit blok zorgt dat élke factuur (ook zonder bijlagen) verrijkt wordt
                // zodat leverancier-, project- en rekeningmatching altijd plaatsvinden.
                var unenrichedIds = await _db.IncommingInvoices
                    .Where(i => i.IssuerCompanyId == issuerCompanyId
                             && i.StatusId == IncomingInvoiceStatus.New
                             && i.Source == "Octopus"
                             && !_db.IncomingInvoiceEnrichment.Any(e => e.IncomingInvoiceId == i.Id))
                    .Select(i => i.Id)
                    .ToListAsync(ct);

                if (unenrichedIds.Count > 0)
                {
                    _logger.LogInformation(
                        "{Count} factuur/facturen gevonden zonder verrijking — verrijkingspipeline starten.",
                        unenrichedIds.Count);

                    foreach (var invoiceId in unenrichedIds)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            await _enrichmentPipeline.EnrichAsync(invoiceId, ct);
                            result.EnrichedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "Verrijkingspipeline mislukt voor factuur {Id}.", invoiceId);
                        }
                    }
                }

                result.Success = true;
                _logger.LogInformation(
                    "Octopus sync klaar voor '{Company}': {New} nieuw, {Updated} bijgewerkt, {Skip} overgeslagen, {Att} bijlagen, {Enriched} verrijkt, {Err} fouten.",
                    company.Name, result.NewCount, result.UpdatedCount, result.SkippedDuplicates,
                    result.AttachmentCount, result.EnrichedCount, result.ErrorCount);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Octopus sync mislukt voor IssuerCompany {Id}.", issuerCompanyId);
            }

            return result;
        }

        private async Task<(UpsertOutcome Outcome, int? LocalId)> UpsertAsync(
            int issuerCompanyId,
            string dossierNumber,
            OctopusBuySellBookingItem booking,
            CancellationToken ct)
        {
            var externalId = booking.ExternalId;

            // Zoek bestaand record op OctopusExternalId
            var existing = await _db.IncommingInvoices
                .FirstOrDefaultAsync(i =>
                    i.IssuerCompanyId == issuerCompanyId &&
                    i.OctopusExternalId == externalId, ct);

            if (existing == null)
            {
                // Fallback: zelfde boekjaar + journaal + documentnummer
                existing = await _db.IncommingInvoices
                    .FirstOrDefaultAsync(i =>
                        i.IssuerCompanyId == issuerCompanyId &&
                        i.OctopusBookyearId == booking.BookyearKey!.Id &&
                        i.OctopusJournalKey == booking.JournalKey &&
                        i.OctopusDocumentSequenceNr == booking.DocumentSequenceNr, ct);
            }

            var now = DateTime.UtcNow;

            if (existing != null)
            {
                var changed = false;

                if (existing.OctopusExternalId != externalId)
                { existing.OctopusExternalId = externalId; changed = true; }
                if (existing.Price != booking.TotalAmountDecimal)
                { existing.Price = booking.TotalAmountDecimal; changed = true; }
                if (existing.VatAmount != booking.VatAmountDecimal)
                { existing.VatAmount = booking.VatAmountDecimal; changed = true; }
                if (existing.NetAmount != booking.NetAmountDecimal)
                { existing.NetAmount = booking.NetAmountDecimal; changed = true; }
                // CompanyId invullen als het nog ontbreekt (zoeken via junction table per issuer)
                if (existing.CompanyId == null && booking.SupplierRelationKey.HasValue)
                {
                    var supplier = await _db.CompanyInfo
                        .FirstOrDefaultAsync(c => c.CompanyIssuerCompany.Any(
                            l => l.IssuerCompanyId == issuerCompanyId && l.OctopusRelationId == booking.SupplierRelationKey.Value), ct);
                    if (supplier != null)
                    { existing.CompanyId = supplier.CompanyId; changed = true; }
                }

                if (!changed)
                    return (UpsertOutcome.Skipped, null);

                existing.SyncedAt = now;
                existing.SyncError = null;
                existing.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
                return (UpsertOutcome.Updated, null);
            }

            // ── Nieuw record aanmaken ─────────────────────────────────────────────────
            var (supplierName, supplierVat, supplierCompanyId) = await ResolveSupplierAsync(issuerCompanyId, dossierNumber, booking.SupplierRelationKey, ct);

            var entity = new IncommingInvoices
            {
                IssuerCompanyId = issuerCompanyId,
                CompanyId = supplierCompanyId,
                SupplierName = supplierName,
                SupplierVatNumber = supplierVat,
                OctopusSupplierRelationKey = booking.SupplierRelationKey,
                ExternalId = booking.InvoiceNumber,
                Date = booking.DocumentDate,
                DueDate = booking.ExpiryDate == default ? null : booking.ExpiryDate,
                Price = booking.TotalAmountDecimal,
                VatAmount = booking.VatAmountDecimal,
                NetAmount = booking.NetAmountDecimal,
                CurrencyCode = booking.CurrencyCode ?? "EUR",
                DocumentType = "Invoice",
                Source = "Octopus",
                Notes = booking.Comment,
                OctopusExternalId = externalId,
                OctopusBookyearId = booking.BookyearKey?.Id,
                OctopusJournalKey = booking.JournalKey,
                OctopusDocumentSequenceNr = booking.DocumentSequenceNr,
                OctopusBookingStatus = "BOOKED",
                StatusId = IncomingInvoiceStatus.New,
                SyncedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.IncommingInvoices.Add(entity);
            await _db.SaveChangesAsync(ct);
            return (UpsertOutcome.Created, entity.Id);
        }

        private async Task<(int Count, int ErrorCount)> SyncAttachmentsAsync(
            int issuerCompanyId,
            string dossierNumber,
            DateTime modifiedSince,
            CancellationToken ct)
        {
            var counts = (Count: 0, ErrorCount: 0);

            // Bouw lookup op uit ALLE lokale Octopus-facturen voor dit bedrijf.
            // Sleutel: (OctopusBookyearId, OctopusDocumentSequenceNr) → lokale Id.
            var localInvoices = await _db.IncommingInvoices
                .Where(i => i.IssuerCompanyId == issuerCompanyId
                         && i.Source == "Octopus"
                         && i.OctopusBookyearId.HasValue
                         && i.OctopusDocumentSequenceNr.HasValue)
                .Select(i => new { i.Id, i.OctopusBookyearId, i.OctopusDocumentSequenceNr, i.SyncedAt,
                                   HasAttachment = i.IncomingInvoiceAttachments.Any() })
                .ToListAsync(ct);

            var lookup = new Dictionary<(int BookyearId, int DocSeqNr), int>();
            foreach (var inv in localInvoices)
                lookup.TryAdd((inv.OctopusBookyearId!.Value, inv.OctopusDocumentSequenceNr!.Value), inv.Id);

            if (lookup.Count == 0)
                return counts;

            // Bepaal de modifiedSince voor de file sync:
            // Als er facturen zijn zonder bijlagen, gebruik een vaste verre datum zodat
            // de API ALLE bestanden teruggeeft — ongeacht wanneer ze in Octopus zijn aangemaakt.
            // De modifiedTimeStamp filtert op de wijzigingsdatum van het bestand in Octopus,
            // niet op onze eigen sync-datum. We kunnen dus niet terugrekenen vanuit SyncedAt.
            // Anders: gebruik de normale modifiedSince voor incrementele sync.
            var missingAttachmentCount = localInvoices.Count(i => !i.HasAttachment);
            DateTime fileSyncFrom;
            if (missingAttachmentCount > 0)
            {
                fileSyncFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                _logger.LogInformation(
                    "{Count} facturen zonder bijlagen — file sync start vanaf {From:u} om alle bestanden op te halen.",
                    missingAttachmentCount, fileSyncFrom);
            }
            else
            {
                fileSyncFrom = modifiedSince;
            }

            await _tokenManager.ExecuteWithDossierTokensAsync<int>(
                issuerCompanyId,
                dossierNumber,
                async (authToken, dossierToken) =>
                {
                    // Één call met fileSyncFrom — geen bookyear/journal/docNr filters.
                    IReadOnlyList<OctopusBookingFileItem> allFiles;
                    try
                    {
                        allFiles = await _octopus.GetBookingFilesAsync(
                            dossierToken,
                            dossierNumber,
                            modifiedSince: fileSyncFrom,
                            ct: ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Bestandenlijst ophalen mislukt voor dossier {Dossier}.", dossierNumber);
                        counts.ErrorCount++;
                        return 0;
                    }

                    _logger.LogInformation(
                        "File sync API leverde {Count} bestanden (modifiedSince={Since:u}).",
                        allFiles.Count, fileSyncFrom);

                    foreach (var file in allFiles)
                    {
                        ct.ThrowIfCancellationRequested();

                        var byId = file.DocumentKey?.BookyearKey?.Id;
                        var docSeq = file.DocumentKey?.DocumentSequenceNr;
                        if (byId == null || docSeq == null)
                            continue;

                        if (!lookup.TryGetValue((byId.Value, docSeq.Value), out var localId))
                            continue; // hoort bij een andere boeking (niet in ons documentencentrum)

                        try
                        {
                            var attachment = await _octopus.DownloadBookingFileAsync(
                                dossierToken, dossierNumber, file, ct);

                            if (attachment?.Content == null || attachment.Content.Length == 0)
                                continue;

                            var type = DetectAttachmentType(attachment.FileName, attachment.ContentType);

                            var alreadyExists = await _db.IncomingInvoiceAttachments
                                .AnyAsync(a => a.IncomingInvoiceId == localId
                                            && a.FileName == attachment.FileName, ct);

                            if (alreadyExists) continue;

                            _db.IncomingInvoiceAttachments.Add(new IncomingInvoiceAttachments
                            {
                                IncomingInvoiceId = localId,
                                FileName = attachment.FileName,
                                ContentType = attachment.ContentType,
                                AttachmentType = type,
                                Content = attachment.Content,
                                FileSizeBytes = attachment.Content.Length,
                                CreatedAt = DateTime.UtcNow
                            });
                            counts.Count++;

                            // UBL/XML: embedded documenten extraheren + opslaan + enrichment
                            if (type == "UBL")
                            {
                                var embeddedPdfs = new List<byte[]>();
                                foreach (var (embName, embMime, embContent) in ExtractEmbeddedDocuments(attachment.Content))
                                {
                                    var embExists = await _db.IncomingInvoiceAttachments
                                        .AnyAsync(a => a.IncomingInvoiceId == localId
                                                    && a.FileName == embName, ct);
                                    if (!embExists)
                                    {
                                        _db.IncomingInvoiceAttachments.Add(new IncomingInvoiceAttachments
                                        {
                                            IncomingInvoiceId = localId,
                                            FileName = embName,
                                            ContentType = embMime,
                                            AttachmentType = DetectAttachmentType(embName, embMime),
                                            Content = embContent,
                                            FileSizeBytes = embContent.Length,
                                            CreatedAt = DateTime.UtcNow
                                        });
                                        counts.Count++;
                                        _logger.LogInformation(
                                            "Embedded document '{File}' ({Mime}) geëxtraheerd uit UBL voor factuur {Id}.",
                                            embName, embMime, localId);
                                    }

                                    if (embMime.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                                        embeddedPdfs.Add(embContent);
                                }

                                await EnrichInvoiceFromUblAsync(localId, attachment.Content, embeddedPdfs, ct);
                            }

                            await _db.SaveChangesAsync(ct);
                        }
                        catch (Exception ex)
                        {
                            counts.ErrorCount++;
                            _logger.LogWarning(ex,
                                "Bijlage '{File}' downloaden mislukt (bookyear={By}, doc={Seq}).",
                                file.FileName, byId, docSeq);
                        }
                    }

                    return 0;
                },
                ct);

            return counts;
        }

        /// <summary>
        /// Parseert de UBL XML en vult het factuur-record aan.
        /// Volgorde: UBL → PdfPig (digitale PDF) → Azure Document Intelligence (scan-PDF, optioneel).
        /// Azure wordt enkel gebruikt als de PDF als scan gedetecteerd wordt én Azure geconfigureerd is.
        /// </summary>
        private async Task EnrichInvoiceFromUblAsync(int invoiceId, byte[] xmlContent, IReadOnlyList<byte[]> embeddedPdfs, CancellationToken ct)
        {
            var ubl = ParseUbl(xmlContent);
            if (ubl == null) return;

            var invoice = await _db.IncommingInvoices
                .Include(i => i.IncommingInvoiceDetail)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
            if (invoice == null) return;

            var changed = false;

            // ── Stap 1: pre-verwerk elke embedded PDF eenmalig ────────────────────────────
            // Per PDF: detecteer scan → gebruik PdfPig (digitaal) of Azure (scan).
            // Resultaat wordt hergebruikt door DueDate, project en detaillijnen — Azure wordt
            // dus nooit meerdere keren per PDF aangeroepen.
            var pdfResults = await ClassifyAndExtractPdfsAsync(embeddedPdfs, invoiceId, ct);

            // ── Stap 2: ExternalId ────────────────────────────────────────────────────────
            var resolvedInvoiceId = ubl.InvoiceId
                ?? pdfResults.Select(r => r.InvoiceId).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            if (!string.IsNullOrWhiteSpace(resolvedInvoiceId) &&
                (string.IsNullOrWhiteSpace(invoice.ExternalId) || invoice.ExternalId != resolvedInvoiceId))
            {
                invoice.ExternalId = resolvedInvoiceId;
                changed = true;
            }

            // ── Stap 3: DueDate ───────────────────────────────────────────────────────────
            var ublDueDate = ubl.DueDate;
            var needPdfFallback = !ublDueDate.HasValue || ublDueDate == invoice.Date;

            if (!needPdfFallback && invoice.DueDate != ublDueDate)
            {
                invoice.DueDate = ublDueDate;
                changed = true;
            }
            else if (needPdfFallback)
            {
                var pdfDate = pdfResults
                    .Select(r => r.DueDate)
                    .FirstOrDefault(d => d.HasValue && d != invoice.Date);
                if (pdfDate.HasValue && invoice.DueDate != pdfDate)
                {
                    invoice.DueDate = pdfDate;
                    changed = true;
                    _logger.LogInformation(
                        "Factuur {Id}: vervaldatum {Date} overgenomen uit PDF (UBL had geen geldige vervaldatum).",
                        invoiceId, pdfDate);
                }
            }

            // ── Stap 4: DocumentType ──────────────────────────────────────────────────────
            var resolvedDocType = ubl.DocumentType
                ?? pdfResults.Select(r => r.DocumentType).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            if (!string.IsNullOrWhiteSpace(resolvedDocType) && invoice.DocumentType != resolvedDocType)
            {
                invoice.DocumentType = resolvedDocType;
                changed = true;
            }

            // ── Stap 5: Project koppelen ──────────────────────────────────────────────────
            if (invoice.ProjectId == null)
            {
                var projects = await _db.Project
                    .Where(p => p.ProjectName != null
                             && p.StatusId != (int)ProjectStatusType.Opgeleverd
                             && p.StatusId != (int)ProjectStatusType.Stopgezet)
                    .Select(p => new
                    {
                        p.ProjectId,
                        p.ProjectName,
                        p.Street,
                        Gemeente = p.PostalCode != null ? p.PostalCode.Gemeente : null
                    })
                    .ToListAsync(ct);

                var projectList = projects.Select(p => (p.ProjectId, p.ProjectName));

                // a. UBL OrderReference
                if (!string.IsNullOrWhiteSpace(ubl.OrderReference))
                {
                    var m = FindProjectInText(ubl.OrderReference, projectList);
                    if (m.HasValue)
                    {
                        invoice.ProjectId = m.Value.ProjectId;
                        changed = true;
                        _logger.LogInformation("Factuur {Id} gekoppeld aan '{Name}' via UBL-referentie.", invoiceId, m.Value.ProjectName);
                    }
                }

                // b. PDF-tekst of Azure OrderReference
                if (invoice.ProjectId == null)
                {
                    foreach (var r in pdfResults)
                    {
                        // Azure geeft een gestructureerde OrderReference; PdfPig geeft volledige tekst
                        var searchText = r.OrderReference ?? r.FullText;
                        if (string.IsNullOrWhiteSpace(searchText)) continue;

                        var m = FindProjectInText(searchText, projectList);
                        if (m.HasValue)
                        {
                            invoice.ProjectId = m.Value.ProjectId;
                            changed = true;
                            _logger.LogInformation(
                                "Factuur {Id} gekoppeld aan '{Name}' via {Source}.",
                                invoiceId, m.Value.ProjectName, r.Source);
                            break;
                        }
                    }
                }

                // c. UBL lijnen beschrijvingen
                if (invoice.ProjectId == null && ubl.Lines.Count > 0)
                {
                    var ublLineText = string.Join(" ", ubl.Lines.Select(l => l.Description));
                    var m = FindProjectInText(ublLineText, projectList);
                    if (m.HasValue)
                    {
                        invoice.ProjectId = m.Value.ProjectId;
                        changed = true;
                        _logger.LogInformation("Factuur {Id} gekoppeld aan '{Name}' via UBL-lijnen.", invoiceId, m.Value.ProjectName);
                    }
                }

                // d. Adres: straatnaam + gemeente moeten allebei voorkomen in de PDF-tekst
                if (invoice.ProjectId == null)
                {
                    var allText = string.Join(" ", pdfResults.Select(r => r.FullText).Where(t => t != null));
                    if (!string.IsNullOrWhiteSpace(allText))
                    {
                        foreach (var p in projects)
                        {
                            if (string.IsNullOrWhiteSpace(p.Street) || p.Street.Length < 5) continue;
                            if (string.IsNullOrWhiteSpace(p.Gemeente)) continue;

                            var streetLower  = p.Street.Trim().ToLowerInvariant();
                            var gemeenteLower = p.Gemeente.Trim().ToLowerInvariant();
                            var textLower    = allText.ToLowerInvariant();

                            if (Regex.IsMatch(textLower, $@"\b{Regex.Escape(streetLower)}\b")
                             && Regex.IsMatch(textLower, $@"\b{Regex.Escape(gemeenteLower)}\b"))
                            {
                                invoice.ProjectId = p.ProjectId;
                                changed = true;
                                _logger.LogInformation(
                                    "Factuur {Id} gekoppeld aan '{Name}' via adres ({Street}, {Gemeente}).",
                                    invoiceId, p.ProjectName, p.Street, p.Gemeente);
                                break;
                            }
                        }
                    }
                }
            }

            // ── Stap 6: Detaillijnen ──────────────────────────────────────────────────────
            // Hersync ook als alle bestaande detaillijnen een lege beschrijving hebben
            var hasUsefulDetails = invoice.IncommingInvoiceDetail.Any(d => !string.IsNullOrWhiteSpace(d.Description));
            if (!hasUsefulDetails)
            {
                var ublLines = ubl.Lines;
                var pdfLines = pdfResults
                    .Select(r => r.Lines)
                    .FirstOrDefault(l => l.Count > 0)
                    ?? new List<UblLine>();

                List<UblLine> linesToUse;
                string bron;

                if (ublLines.Count == 1 && pdfLines.Count > 1)
                {
                    var pdfSum = pdfLines.Where(l => l.Amount > 0).Sum(l => l.Amount ?? 0m);
                    var tolerance = invoice.Price > 0 ? invoice.Price * 0.02m : 1m;
                    if (Math.Abs(pdfSum - invoice.Price) <= tolerance)
                    {
                        linesToUse = pdfLines;
                        bron = pdfResults.FirstOrDefault(r => r.Lines.Count > 0)?.Source ?? "PDF";
                        _logger.LogInformation(
                            "Factuur {Id}: {Bron}-lijnen ({Count}) verkozen boven UBL (1 lijn), som={Sum:N2} ≈ totaal={Total:N2}.",
                            invoiceId, bron, pdfLines.Count, pdfSum, invoice.Price);
                    }
                    else { linesToUse = ublLines; bron = "UBL"; }
                }
                else if (ublLines.Count > 0) { linesToUse = ublLines; bron = "UBL"; }
                else                          { linesToUse = pdfLines; bron = pdfResults.FirstOrDefault(r => r.Lines.Count > 0)?.Source ?? "PDF"; }

                if (linesToUse.Count > 0)
                {
                    // Verwijder lege detaillijnen die eventueel uit een vorige sync stammen
                    var emptyDetails = invoice.IncommingInvoiceDetail
                        .Where(d => string.IsNullOrWhiteSpace(d.Description))
                        .ToList();
                    foreach (var empty in emptyDetails)
                        invoice.IncommingInvoiceDetail.Remove(empty);

                    foreach (var line in linesToUse)
                        invoice.IncommingInvoiceDetail.Add(new IncommingInvoiceDetail
                        {
                            IncommingInvoiceId = invoiceId,
                            Description = line.Description ?? string.Empty,
                            Price = line.Amount,
                            Type = 0m
                        });
                    changed = true;
                    _logger.LogInformation(
                        "Factuur {Id}: {Count} detaillijnen geïmporteerd uit {Source}.", invoiceId, linesToUse.Count, bron);
                }
            }

            if (changed)
            {
                invoice.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            // ── Stap 7: verrijkingspipeline (scoring + opslaan) ───────────────────────
            // Fouten in de pipeline mogen de import nooit blokkeren.
            try
            {
                await _enrichmentPipeline.EnrichAsync(invoiceId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Verrijkingspipeline mislukt voor factuur {Id} — import zelf is wel geslaagd.", invoiceId);
            }
        }

        /// <summary>
        /// Verwerkt elke embedded PDF eenmalig: detecteert scan vs. digitaal en kiest
        /// de juiste extractiemethode (PdfPig of Azure). Azure wordt per PDF maximaal één keer
        /// aangeroepen zodat API-kosten minimaal blijven.
        /// </summary>
        private async Task<IReadOnlyList<PdfExtractionResult>> ClassifyAndExtractPdfsAsync(
            IReadOnlyList<byte[]> embeddedPdfs,
            int invoiceId,
            CancellationToken ct)
        {
            var results = new List<PdfExtractionResult>();
            if (embeddedPdfs.Count == 0) return results;

            var threshold = _extractionOptions.ScanDetectionWordThreshold;

            foreach (var pdf in embeddedPdfs)
            {
                var isScan = IsScanPdf(pdf, threshold);

                if (!isScan)
                {
                    // Digitale PDF → PdfPig (lokaal, gratis)
                    _logger.LogDebug("Factuur {Id}: digitale PDF → PdfPig extractie.", invoiceId);
                    results.Add(new PdfExtractionResult
                    {
                        Source   = "PDF",
                        DueDate  = ExtractDueDateFromPdf(pdf),
                        FullText = ExtractPdfText(pdf),
                        Lines    = ExtractDetailLinesFromPdf(pdf)
                    });
                }
                else if (_azureService.IsEnabled)
                {
                    // Scan/foto → Azure Document Intelligence
                    _logger.LogInformation(
                        "Factuur {Id}: scan-PDF gedetecteerd (<{Threshold} woorden/pagina) → Azure Document Intelligence.",
                        invoiceId, threshold);
                    var azureResult = await _azureService.AnalyzeAsync(pdf, ct);
                    if (azureResult.Success)
                    {
                        results.Add(new PdfExtractionResult
                        {
                            Source         = "Azure",
                            InvoiceId      = azureResult.InvoiceId,
                            DueDate        = azureResult.DueDate,
                            DocumentType   = azureResult.DocumentType,
                            OrderReference = azureResult.OrderReference,
                            Lines = azureResult.Lines
                                .Select(l => new UblLine(l.Description, l.Amount))
                                .ToList()
                        });
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Factuur {Id}: Azure analyse mislukt — {Error}.", invoiceId, azureResult.ErrorMessage);
                    }
                }
                else
                {
                    // Scan maar geen Azure → gedeeltelijke info, enkel UBL-data beschikbaar
                    _logger.LogInformation(
                        "Factuur {Id}: scan-PDF gedetecteerd maar Azure is niet geconfigureerd. " +
                        "Vul 'InvoiceExtraction:Azure:Endpoint' en 'ApiKey' in appsettings in om scans te verwerken.",
                        invoiceId);
                }
            }

            return results;
        }

        private sealed record PdfExtractionResult
        {
            public string Source         { get; init; }  // "PDF" of "Azure"
            public string InvoiceId      { get; init; }
            public DateOnly? DueDate     { get; init; }
            public string DocumentType   { get; init; }
            public string OrderReference { get; init; }  // Azure: gestructureerd veld
            public string FullText       { get; init; }  // PdfPig: volledige paginatekst
            public List<UblLine> Lines   { get; init; } = new();
        }

        /// <summary>
        /// Detecteert of een PDF een scan/foto is door het gemiddeld aantal woorden per pagina
        /// te tellen via PdfPig. Onder de drempel = scan.
        /// </summary>
        private static bool IsScanPdf(byte[] pdfBytes, int wordsPerPageThreshold = 10)
        {
            try
            {
                using var doc = PdfDocument.Open(pdfBytes);
                var pages = doc.GetPages().ToList();
                if (pages.Count == 0) return true;
                var totalWords = pages.Sum(p => p.GetWords().Count());
                var avgWordsPerPage = totalWords / pages.Count;
                return avgWordsPerPage < wordsPerPageThreshold;
            }
            catch { return true; }
        }

        private sealed record UblLine(string Description, decimal? Amount);

        private sealed record UblInvoiceData(
            string InvoiceId,
            string DocumentType,
            DateOnly? DueDate,
            string OrderReference,
            List<UblLine> Lines);

        private static UblInvoiceData ParseUbl(byte[] xmlContent)
        {
            string xml;
            try { xml = Encoding.UTF8.GetString(xmlContent); }
            catch { return null; }

            XDocument doc;
            try { doc = XDocument.Parse(xml); }
            catch { return null; }

            XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
            XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

            var root = doc.Root;
            if (root == null) return null;

            // DocumentType bepalen op basis van root-element
            var localName = root.Name.LocalName; // "Invoice" of "CreditNote"
            var documentType = localName == "CreditNote" ? "CreditNote" : "Invoice";

            // cbc:ID = factuurnummer / creditnotanummer
            var invoiceId = root.Element(cbc + "ID")?.Value?.Trim();

            // cbc:DueDate
            DateOnly? dueDate = null;
            var dueDateStr = root.Element(cbc + "DueDate")?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(dueDateStr) &&
                DateOnly.TryParseExact(dueDateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsed))
                dueDate = parsed;

            // Referentie voor project-koppeling: OrderReference of BuyerReference
            var orderRef = root.Element(cac + "OrderReference")?.Element(cbc + "ID")?.Value?.Trim()
                        ?? root.Element(cbc + "BuyerReference")?.Value?.Trim()
                        ?? root.Element(cac + "ContractDocumentReference")?.Element(cbc + "ID")?.Value?.Trim();

            // Detaillijnen: InvoiceLine of CreditNoteLine
            var lineElementName = documentType == "CreditNote" ? "CreditNoteLine" : "InvoiceLine";
            var lines = root.Elements(cac + lineElementName)
                .Select(line =>
                {
                    // Kies de langste zinvolle omschrijving uit alle mogelijke velden.
                    // Sommige UBL-generatoren (bv. Octopus) zetten een placeholder ("x") in cbc:Name.
                    var description = new[]
                        {
                            line.Element(cac + "Item")?.Element(cbc + "Name")?.Value?.Trim(),
                            line.Element(cac + "Item")?.Element(cbc + "Description")?.Value?.Trim(),
                            line.Element(cbc + "Note")?.Value?.Trim()
                        }
                        .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 1)
                        .OrderByDescending(s => s.Length)
                        .FirstOrDefault();
                    var amountStr = line.Element(cbc + "LineExtensionAmount")?.Value?.Trim();
                    decimal? amount = null;
                    if (!string.IsNullOrWhiteSpace(amountStr) &&
                        decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var amt))
                        amount = amt;
                    return new UblLine(description, amount);
                })
                .Where(l => !string.IsNullOrWhiteSpace(l.Description) || l.Amount.HasValue)
                .ToList();

            return new UblInvoiceData(invoiceId, documentType, dueDate, orderRef, lines);
        }

        /// <summary>
        /// Zoekt in <paramref name="text"/> naar een projectnaam via woord-voor-woord matching.
        /// Een match vereist dat minstens één significant woord (≥4 tekens) uit de projectnaam
        /// voorkomt in de tekst, EN dat de volledige projectnaam als aaneengesloten substring
        /// aanwezig is — dit voorkomt false-positives op korte of generieke woorden.
        /// </summary>
        private static (int ProjectId, string ProjectName)? FindProjectInText(
            string text,
            IEnumerable<(int ProjectId, string ProjectName)> projects)
        {
            var textLower = text.ToLowerInvariant();

            foreach (var (id, name) in projects)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var nameLower = name.Trim().ToLowerInvariant();

                // Directe match op de volledige projectnaam met woordgrens aan begin en einde.
                // Voorkomt dat "Woning" matcht op "woningen" of "Nieuwbouw" op "Nieuwbouwproject".
                if (Regex.IsMatch(textLower, $@"\b{Regex.Escape(nameLower)}\b"))
                    return (id, name);

                // Woord-voor-woord: alle significante woorden (≥4 tekens) van de projectnaam
                // moeten als volledig woord (woordgrens) aanwezig zijn in de tekst.
                // Vereist minstens 2 significante woorden om false-positives te vermijden
                // (bv. "woning" matcht anders "woningen", "Brugge" matcht "Afdeling Brugge").
                var nameWords = Regex.Split(nameLower, @"[^a-z0-9]+")
                    .Where(w => w.Length >= 4)
                    .ToList();

                if (nameWords.Count >= 2 && nameWords.All(w => Regex.IsMatch(textLower, $@"\b{Regex.Escape(w)}\b")))
                    return (id, name);
            }

            return null;
        }

        /// <summary>
        /// Extraheert alle tekst uit een PDF via PdfPig. Geeft null terug bij fouten.
        /// </summary>
        private static string ExtractPdfText(byte[] pdfBytes)
        {
            try
            {
                using var doc = PdfDocument.Open(pdfBytes);
                var sb = new StringBuilder();
                foreach (var page in doc.GetPages())
                    sb.AppendLine(page.Text);
                return sb.ToString();
            }
            catch { return null; }
        }

        /// <summary>
        /// Probeert de vervaldatum te extraheren uit de tekst van een PDF via PdfPig.
        /// Zoekt naar patronen als "Vervaldatum: DD/MM/YYYY", "Due date: YYYY-MM-DD", enz.
        /// </summary>
        private static DateOnly? ExtractDueDateFromPdf(byte[] pdfBytes)
        {
            try
            {
                var text = ExtractPdfText(pdfBytes);
                if (string.IsNullOrEmpty(text)) return null;

                // Zoekwoorden voor vervaldatum (NL + FR + EN)
                var keywords = new[]
                {
                    "vervaldatum", "vervaldag", "betalen voor", "te betalen voor",
                    "due date", "payment due", "échéance", "date d'échéance",
                    "betaaldatum", "uiterste betaaldatum"
                };

                // Datumpatronen: DD/MM/YYYY, DD-MM-YYYY, YYYY-MM-DD
                var datePatterns = new[]
                {
                    @"(\d{2})[\/\-](\d{2})[\/\-](\d{4})",  // DD/MM/YYYY of DD-MM-YYYY
                    @"(\d{4})[\/\-](\d{2})[\/\-](\d{2})"   // YYYY-MM-DD
                };

                var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var lower = line.ToLowerInvariant();
                    if (!keywords.Any(k => lower.Contains(k))) continue;

                    // DD/MM/YYYY of DD-MM-YYYY
                    var m = Regex.Match(line, datePatterns[0]);
                    if (m.Success &&
                        int.TryParse(m.Groups[1].Value, out var day) &&
                        int.TryParse(m.Groups[2].Value, out var month) &&
                        int.TryParse(m.Groups[3].Value, out var year))
                    {
                        if (day >= 1 && day <= 31 && month >= 1 && month <= 12 && year >= 2000)
                            return new DateOnly(year, month, day);
                    }

                    // YYYY-MM-DD
                    m = Regex.Match(line, datePatterns[1]);
                    if (m.Success &&
                        int.TryParse(m.Groups[1].Value, out year) &&
                        int.TryParse(m.Groups[2].Value, out month) &&
                        int.TryParse(m.Groups[3].Value, out day))
                    {
                        if (year >= 2000 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
                            return new DateOnly(year, month, day);
                    }
                }
            }
            catch { /* PDF niet leesbaar — geen probleem */ }

            return null;
        }

        /// <summary>
        /// Extraheert factuurdetaillijnen uit een PDF via PdfPig op woordniveau.
        /// Woorden worden gegroepeerd op Y-positie (= visuele tabelrij). Per rij wordt
        /// de beschrijving genomen uit de meest linkse niet-cijferwoorden en het bedrag
        /// uit het meest rechtse decimale getal (= subtotaalkolom).
        /// Dit werkt ook voor PDF-tabellen waarbij tekst en bedragen in aparte kolommen staan.
        /// </summary>
        private static List<UblLine> ExtractDetailLinesFromPdf(byte[] pdfBytes)
        {
            var result = new List<UblLine>();
            try
            {
                var excludeKeywords = new[]
                {
                    "totaal", "subtotaal", "te betalen", "te voldoen", "netto totaal",
                    "btw", "tva", "belasting", "tax", "total", "sous-total", "net total",
                    "amount due", "grand total", "invoice total", "credit total",
                    "korting", "réduction", "discount",
                    "pagina", "page", "datum", "date", "factuur", "invoice",
                    "leverancier", "klant", "client", "supplier"
                };

                // Matcht een op zichzelf staand decimaal getal (Belgisch of internationaal)
                var amountRx = new Regex(
                    @"^-?\d{1,3}(?:[.,]\d{3})*[.,]\d{2}$|^-?\d+[.,]\d{2}$",
                    RegexOptions.Compiled);

                using var doc = PdfDocument.Open(pdfBytes);

                foreach (var page in doc.GetPages())
                {
                    // Groepeer woorden in visuele rijen op basis van Y-positie (tolerantie 4pt).
                    // PdfPig geeft coördinaten met Y = 0 onderaan, stijgend naar boven.
                    const double rowTolerance = 4.0;
                    var rowList = new List<(double Y, List<(string Text, double Left)> Words)>();

                    foreach (var word in page.GetWords().OrderByDescending(w => w.BoundingBox.Bottom))
                    {
                        var y = word.BoundingBox.Bottom;
                        int matchIdx = -1;
                        for (int i = 0; i < rowList.Count; i++)
                        {
                            if (Math.Abs(rowList[i].Y - y) <= rowTolerance) { matchIdx = i; break; }
                        }
                        if (matchIdx >= 0)
                            rowList[matchIdx].Words.Add((word.Text, word.BoundingBox.Left));
                        else
                            rowList.Add((y, new List<(string Text, double Left)> { (word.Text, word.BoundingBox.Left) }));
                    }

                    // Eerste pass: bouw candidaat-rijen
                    var candidateLines = new List<(string Description, decimal Amount, bool HasAmount)>();

                    foreach (var (_, rowWords) in rowList)
                    {
                        // Sorteer woorden van links naar rechts
                        var words = rowWords.OrderBy(w => w.Left).Select(w => w.Text).ToList();
                        if (words.Count < 1) continue;

                        var rowText = string.Join(" ", words);
                        var lower = rowText.ToLowerInvariant();
                        if (excludeKeywords.Any(k => lower.Contains(k))) continue;

                        // Zoek het meest rechtse decimale getal = subtotaalkolom
                        int amountIdx = -1;
                        for (int i = words.Count - 1; i >= 0; i--)
                        {
                            if (amountRx.IsMatch(words[i])) { amountIdx = i; break; }
                        }

                        if (amountIdx < 0)
                        {
                            // Rij zonder bedrag: bewaar als vervolgrij-kandidaat
                            var contWords = words.Where(w => w.Length > 1 && !amountRx.IsMatch(w)).ToList();
                            var contText = string.Join(" ", contWords).Trim();
                            if (!string.IsNullOrWhiteSpace(contText))
                                candidateLines.Add((contText, 0m, false));
                            continue;
                        }

                        // Beschrijving = woorden van links tot het eerste decimale getal (aantalkolom).
                        // Sla voorloopwoorden over die pure integers zijn (aantalkolom, bv. "1")
                        // en sla eentekenwoorden over (koppelstreep, scheidingsteken).
                        var descParts = new List<string>();
                        bool descStarted = false;
                        foreach (var w in words.Take(amountIdx))
                        {
                            if (amountRx.IsMatch(w))
                            {
                                if (descStarted) break;  // eerste decimaal na beschrijving = stop
                                continue;                 // decimaal vóór beschrijving = aantalkolom
                            }
                            if (w.Length <= 1) continue; // eentekenwoorden (koppelstreep, enz.)
                            if (!descStarted && Regex.IsMatch(w, @"^\d+$")) continue; // pure integer vóór beschrijving = aantalkolom
                            descStarted = true;
                            descParts.Add(w);
                        }

                        // Strip alle combinaties van spaties en dubbele punten aan het einde
                        var description = Regex.Replace(string.Join(" ", descParts), @"[\s:]+$", "").Trim();
                        // Niet filteren op beschrijvingslengte hier — de tweede pass voegt
                        // eventuele vervolgrijen (boven of onder) toe voor de lengte-check.

                        // Normaliseer bedrag: Belgisch (1.234,56 → 1234.56) of internationaal
                        var raw = words[amountIdx];
                        var lastComma = raw.LastIndexOf(',');
                        var lastDot   = raw.LastIndexOf('.');
                        var normalized = lastComma > lastDot
                            ? raw.Replace(".", "").Replace(",", ".")
                            : raw.Replace(",", "");

                        if (!decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var amount))
                            continue;
                        if (amount == 0m) continue;

                        candidateLines.Add((description, amount, true));
                    }

                    // Tweede pass: merge vervolgrijen rondom elke bedrag-rij.
                    // Rijen zonder bedrag direct VOOR een bedrag-rij → prefix beschrijving (omloop omhoog).
                    // Rijen zonder bedrag direct NA een bedrag-rij → suffix beschrijving (omloop omlaag).
                    var used = new bool[candidateLines.Count];
                    for (int i = 0; i < candidateLines.Count; i++)
                    {
                        if (!candidateLines[i].HasAmount || used[i]) continue;
                        used[i] = true;

                        var (desc, amt, _) = candidateLines[i];

                        // Zoek aaneengesloten rijen zonder bedrag die deze rij VOORAFGAAN (omloop omhoog)
                        int pre = i - 1;
                        var prefixParts = new List<string>();
                        while (pre >= 0 && !candidateLines[pre].HasAmount && !used[pre])
                        {
                            prefixParts.Insert(0, candidateLines[pre].Description);
                            used[pre] = true;
                            pre--;
                        }
                        if (prefixParts.Count > 0)
                            desc = string.Join(" ", prefixParts) + " " + desc;

                        // Zoek aaneengesloten rijen zonder bedrag die deze rij VOLGEN (omloop omlaag)
                        while (i + 1 < candidateLines.Count && !candidateLines[i + 1].HasAmount && !used[i + 1])
                        {
                            used[i + 1] = true;
                            desc += " " + candidateLines[i + 1].Description;
                            i++;
                        }

                        var finalDesc = desc.Trim();
                        if (finalDesc.Length >= 3)
                            result.Add(new UblLine(finalDesc, amt));
                    }
                }
            }
            catch { /* PDF niet leesbaar — geen probleem */ }

            return result;
        }

        /// <summary>
        /// Parseert een UBL XML document en geeft alle embedded bestanden terug
        /// die zijn opgenomen via cbc:EmbeddedDocumentBinaryObject (base64-gecodeerd).
        /// </summary>
        private static IEnumerable<(string FileName, string ContentType, byte[] Content)> ExtractEmbeddedDocuments(byte[] xmlContent)
        {
            string xml;
            try { xml = Encoding.UTF8.GetString(xmlContent); }
            catch { yield break; }

            XDocument doc;
            try { doc = XDocument.Parse(xml); }
            catch { yield break; }

            // cbc-namespace is consistent in alle UBL versies
            XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

            foreach (var el in doc.Descendants(cbc + "EmbeddedDocumentBinaryObject"))
            {
                var mimeCode = el.Attribute("mimeCode")?.Value?.Trim() ?? "application/octet-stream";
                var filename = el.Attribute("filename")?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(filename))
                    filename = mimeCode.Contains("pdf", StringComparison.OrdinalIgnoreCase) ? "bijlage.pdf" : "bijlage.bin";

                var base64 = el.Value?.Trim();
                if (string.IsNullOrWhiteSpace(base64)) continue;

                byte[] content;
                try { content = Convert.FromBase64String(base64); }
                catch { continue; }

                if (content.Length == 0) continue;

                yield return (filename, mimeCode, content);
            }
        }

        private static string DetectAttachmentType(string fileName, string contentType)
        {
            if (contentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true ||
                fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true)
                return "PDF";

            if (contentType?.Contains("xml", StringComparison.OrdinalIgnoreCase) == true ||
                fileName?.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) == true ||
                fileName?.EndsWith(".ubl", StringComparison.OrdinalIgnoreCase) == true)
                return "UBL";

            return "Other";
        }

        private async Task<(string Name, string VatNumber, int? CompanyId)> ResolveSupplierAsync(
            int issuerCompanyId, string dossierNumber, int? relationKey, CancellationToken ct)
        {
            if (!relationKey.HasValue)
                return ("Onbekende leverancier", null, null);

            // 1. Directe hit via junction table (per issuer)
            var existing = await _db.CompanyInfo
                .FirstOrDefaultAsync(c => c.CompanyIssuerCompany.Any(
                    l => l.IssuerCompanyId == issuerCompanyId && l.OctopusRelationId == relationKey.Value), ct);
            if (existing != null)
                return (existing.BedrijfsNaam ?? $"Relatie #{relationKey.Value}", existing.VatNumber, existing.CompanyId);

            // 2. Onbekende relatie — ophalen uit Octopus
            OctopusRelation relation = null;
            try
            {
                relation = await _tokenManager.ExecuteWithDossierTokensAsync(
                    issuerCompanyId, dossierNumber,
                    (_, dossierToken) => _octopus.FindRelationAsync(
                        dossierToken, dossierNumber,
                        new OctopusRelationLookup { RelationId = relationKey.Value }, ct),
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Octopus relatie {Key} kon niet worden opgehaald.", relationKey.Value);
                return ($"Relatie #{relationKey.Value}", null, null);
            }

            if (relation == null)
                return ($"Relatie #{relationKey.Value}", null, null);

            var vatNr = NormalizeVat(relation.VatNr);
            var name = BuildBedrijfsNaam(relation.Name, relation.Firstname) is { Length: > 0 } n
                ? n
                : $"Relatie #{relationKey.Value}";

            // 3. Probeer te matchen op BTW-nr of naam in CompanyInfo (enkel actieve relaties)
            CompanyInfo matched = null;
            if (!string.IsNullOrWhiteSpace(vatNr))
            {
                var candidates = await _db.CompanyInfo
                    .Where(c => c.IsActive && !string.IsNullOrWhiteSpace(c.Ondernemingsnummer))
                    .ToListAsync(ct);
                matched = candidates.FirstOrDefault(c =>
                    string.Equals(NormalizeVat(c.Ondernemingsnummer), vatNr, StringComparison.OrdinalIgnoreCase));
            }

            if (matched == null && !string.IsNullOrWhiteSpace(name))
            {
                var candidates = await _db.CompanyInfo
                    .Where(c => c.IsActive && !string.IsNullOrWhiteSpace(c.BedrijfsNaam))
                    .ToListAsync(ct);
                matched = candidates.FirstOrDefault(c =>
                    string.Equals(c.BedrijfsNaam?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (matched != null)
            {
                await EnsureSupplierLinkAsync(matched, issuerCompanyId, relationKey.Value, ct);
                await _db.SaveChangesAsync(ct);
                return (matched.BedrijfsNaam ?? name, matched.VatNumber ?? vatNr, matched.CompanyId);
            }

            // 4. Nieuw: leverancier aanmaken in CompanyInfo
            var (street, houseNr, busNr) = ParseStreetAndNr(relation.StreetAndNr);
            var postCodeId = await ResolvePostalCodeIdAsync(relation.PostalCode, relation.City, ct);
            var vatStatus = ResolveSupplierVatStatus(relation.VatType);

            var newSupplier = new CompanyInfo
            {
                BedrijfsNaam = name,
                Ondernemingsnummer = vatNr,
                VatNumber = vatNr,
                VatStatus = vatStatus,
                Straat = street,
                Huisnummer = houseNr,
                Busnummer = busNr,
                Postcode = relation.PostalCode,
                Gemeente = relation.City,
                PostCodeId = postCodeId,
                LandCode = relation.Country,
                Telefoon1 = relation.Telephone,
                Gsm = relation.Mobile,
                Email = relation.Email,
                Bank = relation.IbanAccountNr,
                IsCustomer = relation.Client,
                IsActive = relation.Active
            };

            _db.CompanyInfo.Add(newSupplier);
            await _db.SaveChangesAsync(ct);

            await EnsureSupplierLinkAsync(newSupplier, issuerCompanyId, relationKey.Value, ct);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Nieuwe leverancier '{Name}' (OctopusId={Key}) aangemaakt vanuit Octopus factuurimport.",
                name, relationKey.Value);

            return (name, vatNr, newSupplier.CompanyId);
        }

        private async Task EnsureSupplierLinkAsync(CompanyInfo supplier, int issuerCompanyId, int octopusRelationId, CancellationToken ct)
        {
            await _db.Entry(supplier).Collection(s => s.CompanyIssuerCompany).LoadAsync(ct);
            supplier.CompanyIssuerCompany ??= new List<CompanyIssuerCompany>();

            var link = supplier.CompanyIssuerCompany.FirstOrDefault(l => l.IssuerCompanyId == issuerCompanyId);
            if (link == null)
            {
                supplier.CompanyIssuerCompany.Add(new CompanyIssuerCompany
                {
                    CompanyId = supplier.CompanyId,
                    IssuerCompanyId = issuerCompanyId,
                    OctopusRelationId = octopusRelationId
                });
            }
            else if (!link.OctopusRelationId.HasValue)
            {
                link.OctopusRelationId = octopusRelationId;
            }
        }

        private static string NormalizeVat(string vat)
        {
            if (string.IsNullOrWhiteSpace(vat)) return null;
            return Regex.Replace(vat, @"\D", "");
        }

        private static string BuildBedrijfsNaam(string? lastName, string? firstName)
        {
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                return $"{lastName.Trim()} {firstName.Trim()}";
            return (lastName ?? firstName ?? string.Empty).Trim();
        }

        private static (string Street, string? HouseNr, string? BusNr) ParseStreetAndNr(string? streetAndNr)
        {
            if (string.IsNullOrWhiteSpace(streetAndNr))
                return (string.Empty, null, null);

            var match = Regex.Match(streetAndNr.Trim(), @"^(.*?)\s+(\d+[a-zA-Z]?)(?:[/\s]+(\S+))?$");
            if (match.Success)
            {
                return (
                    match.Groups[1].Value.Trim(),
                    match.Groups[2].Value.Trim(),
                    match.Groups[3].Success ? match.Groups[3].Value.Trim() : null
                );
            }

            return (streetAndNr.Trim(), null, null);
        }

        private static string ResolveSupplierVatStatus(int? vatType) => vatType switch
        {
            10 => SupplierFormViewModel.VatStatusNietBtwPlichtig,
            7 or 8 or 9 => SupplierFormViewModel.VatStatusParticulier,
            _ => SupplierFormViewModel.VatStatusBtwPlichtig
        };

        private async Task<int?> ResolvePostalCodeIdAsync(string? postalCode, string? city, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                return null;

            var matches = await _db.PostalCode
                .Where(p => p.Postcode == postalCode.Trim())
                .ToListAsync(ct);

            if (matches.Count == 1)
                return matches[0].PostcodeId;

            if (matches.Count > 1 && !string.IsNullOrWhiteSpace(city))
            {
                var normalizedCity = city.Trim().ToUpperInvariant();
                var match = matches.FirstOrDefault(p => p.Gemeente?.Trim().ToUpperInvariant() == normalizedCity);
                if (match != null)
                    return match.PostcodeId;
            }

            return null;
        }

        private enum UpsertOutcome { Created, Updated, Skipped }
    }
}
