using BOCore;
using CPMCore.Services.InvoiceExtraction;
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
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace CPMCore.Services.InvoiceEnrichment
{
    /// <summary>
    /// Orchestreert de volledige verrijkingspipeline na import vanuit Octopus.
    ///
    /// Volgorde:
    ///   1. Laad factuur + bijlagen
    ///   2. Extraheer tekst uit PDF/UBL (PdfPig of Azure)
    ///   3. Leverancierregels raadplegen
    ///   4. Projectmatching (scoring)
    ///   5. Contractmatching
    ///   6. Kostenrekening voorstellen
    ///   7. Duplicaatcontrole
    ///   8. Waarschuwingen aanmaken
    ///   9. Opslaan in IncomingInvoiceEnrichment
    ///  10. Facturstatus updaten
    /// </summary>
    public class InvoiceEnrichmentPipelineService : IInvoiceEnrichmentPipelineService
    {
        private readonly cpmRunningContext _db;
        private readonly IProjectMatchingService _projectMatcher;
        private readonly IContractMatchingService _contractMatcher;
        private readonly IAccountingSuggestionService _accountingSuggester;
        private readonly IAzureInvoiceAnalysisService _azureService;
        private readonly InvoiceExtractionOptions _extractionOptions;
        private readonly ILogger<InvoiceEnrichmentPipelineService> _logger;

        public InvoiceEnrichmentPipelineService(
            cpmRunningContext db,
            IProjectMatchingService projectMatcher,
            IContractMatchingService contractMatcher,
            IAccountingSuggestionService accountingSuggester,
            IAzureInvoiceAnalysisService azureService,
            IOptions<InvoiceExtractionOptions> extractionOptions,
            ILogger<InvoiceEnrichmentPipelineService> logger)
        {
            _db = db;
            _projectMatcher = projectMatcher;
            _contractMatcher = contractMatcher;
            _accountingSuggester = accountingSuggester;
            _azureService = azureService;
            _extractionOptions = extractionOptions.Value;
            _logger = logger;
        }

        // ─── Publieke interface ───────────────────────────────────────────────

        public async Task<InvoiceEnrichmentVm> EnrichAsync(int invoiceId, CancellationToken ct = default)
        {
            var invoice = await LoadInvoiceAsync(invoiceId, ct);
            if (invoice == null)
            {
                _logger.LogWarning("EnrichAsync: factuur {Id} niet gevonden.", invoiceId);
                return null;
            }

            _logger.LogInformation("Verrijkingspipeline starten voor factuur {Id} ({Supplier}).",
                invoiceId, invoice.SupplierName);

            var warnings = new List<(string Code, string Message, string Severity, string Source)>();
            long reasonFlags = 0;

            // ── Stap 1: tekst extraheren ─────────────────────────────────────
            var (combinedText, ublText, pdfText, ocrText, flagsFromExtraction) =
                await ExtractTextAsync(invoice, warnings, ct);
            reasonFlags |= flagsFromExtraction;

            // ── Stap 1b: PDF-factuurlijnen extraheren als UBL-lijnen zwak zijn ─
            var hasWeakUblLines = HasWeakUblLines(invoice);
            List<ProposedInvoiceLine> proposedPdfLines = null;

            if (hasWeakUblLines)
            {
                var pdfAtt = invoice.IncomingInvoiceAttachments
                    .FirstOrDefault(a => a.AttachmentType == "PDF" && a.Content != null);

                if (pdfAtt != null)
                {
                    proposedPdfLines = TryExtractInvoiceLines(pdfAtt.Content, invoice.Price);

                    if (proposedPdfLines?.Count > 0)
                    {
                        reasonFlags |= (long)InvoiceReasonFlags.LinesRecoveredFromPdf;
                        warnings.Add(("PdfLinesAvailable",
                            $"{proposedPdfLines.Count} factuurlijn(en) herkend in PDF — UBL-lijnen zijn generiek. Controleer en neem over.",
                            "Info", "PdfLineExtractor"));

                        _logger.LogInformation(
                            "Factuur {Id}: {Count} PDF-factuurlijn(en) geëxtraheerd ter vervanging van generieke UBL-lijnen.",
                            invoiceId, proposedPdfLines.Count);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Factuur {Id}: UBL-lijnen zijn zwak maar PDF-lijnextractie leverde geen resultaat.",
                            invoiceId);
                    }
                }
            }

            // ── Stap 2: leverancierregels ────────────────────────────────────
            var supplierInfo = await ResolveSupplierInfoAsync(invoice, warnings, ct);

            if (supplierInfo != null)
            {
                reasonFlags |= supplierInfo.ReasonFlags;
            }

            // ── Stap 3: documentclassificatie ────────────────────────────────
            var classification = ClassifyDocument(invoice, pdfText, ublText);

            // ── Stap 4: veldwaarden verrijken + conflicten detecteren ─────────
            var enrichedDueDate = BuildDueDateField(invoice, pdfText, warnings, ref reasonFlags);
            var enrichedInvoiceNr = BuildInvoiceNumberField(invoice, warnings, ref reasonFlags);
            var enrichedDocType = BuildDocTypeField(invoice, classification);

            // ── Stap 5: projectmatching ──────────────────────────────────────
            IReadOnlyList<ProjectMatchResult> projectCandidates = Array.Empty<ProjectMatchResult>();
            if (!string.IsNullOrWhiteSpace(combinedText))
            {
                projectCandidates = await _projectMatcher.FindMatchesAsync(
                    combinedText, invoice.IssuerCompanyId, ct);
            }

            // Leverancierregel kan een default project dicteren
            ProjectMatchResult bestProject = null;
            if (supplierInfo is { DefaultProjectId: int projectId })
            {
                var ruleProject = await _db.Project
                    .AsNoTracking()
                    .Where(p => p.ProjectId == projectId)
                    .Select(p => new { p.ProjectId, p.ProjectName })
                    .FirstOrDefaultAsync(ct);

                if (ruleProject != null)
                {
                    bestProject = new ProjectMatchResult
                    {
                        ProjectId = ruleProject.ProjectId,
                        ProjectName = ruleProject.ProjectName,
                        Score = 100,
                        ConfidenceLevel = "High",
                        MatchReason = "leverancierregel",
                        MatchedFields = "Rule",
                        Source = "RuleEngine"
                    };
                    _logger.LogInformation(
                        "Factuur {Id}: project via leverancierregel → '{Name}'.",
                        invoiceId, ruleProject.ProjectName);
                }
            }

            if (bestProject == null && projectCandidates.Count > 0)
            {
                var bestAllowed = projectCandidates
                    .Where(p => p.AllowedByFilter)
                    .OrderByDescending(p => p.Score)
                    .FirstOrDefault();

                var bestOverall = projectCandidates
                    .OrderByDescending(p => p.Score)
                    .FirstOrDefault();

                if (bestAllowed != null && bestAllowed.Score >= 20)
                {
                    bestProject = bestAllowed;
                }
                else if (bestOverall != null && bestOverall.Score >= 80)
                {
                    bestProject = bestOverall;

                    if (bestOverall.BlockReason == "DifferentIssuerCompany")
                    {
                        warnings.Add(("BuilderMismatch",
                            $"Project '{bestOverall.ProjectName}' matcht sterk maar behoort mogelijks tot een andere bouwheer.",
                            "Warning", "ProjectMatcher"));
                        _logger.LogInformation(
                            "Factuur {Id}: sterke match '{Name}' (score {Score}) geselecteerd — andere bouwheer ({Reason}).",
                            invoiceId, bestOverall.ProjectName, bestOverall.Score, bestOverall.BlockReason);
                    }
                }
                else
                {
                    bestProject = null;
                }

                if (bestProject != null)
                {
                    var allowedCount = projectCandidates.Count(p => p.AllowedByFilter);
                    if (allowedCount > 1)
                    {
                        reasonFlags |= (long)InvoiceReasonFlags.MultipleProjectCandidates;
                        warnings.Add((
                            "MultipleProjectCandidates",
                            $"{allowedCount} toegestane projectkandidaten gevonden — controleer de keuze.",
                            "Warning", "ProjectMatcher"));
                    }

                    if (bestProject.Score < 50)
                    {
                        reasonFlags |= (long)InvoiceReasonFlags.ProjectUncertain;
                        warnings.Add(("ProjectUncertain",
                            $"Projectmatch onzeker (score {bestProject.Score}): {bestProject.MatchReason}.",
                            "Warning", "ProjectMatcher"));
                    }

                    if (bestProject.IsInactiveProject)
                    {
                        reasonFlags |= (long)InvoiceReasonFlags.ProjectUncertain;
                        warnings.Add(("InactiveProject",
                            $"Project '{bestProject.ProjectName}' heeft status '{bestProject.ProjectStatusLabel}' " +
                            $"— controleer vóór bevestiging.",
                            "Warning", "ProjectMatcher"));
                        _logger.LogInformation(
                            "Factuur {Id}: beste projectmatch '{Name}' is inactief (status={Status}).",
                            invoiceId, bestProject.ProjectName, bestProject.ProjectStatusLabel);
                    }
                }
            }

            if (bestProject == null)
            {
                reasonFlags |= (long)InvoiceReasonFlags.GeneralCostSuggested;
            }

            // Vorige koppeling in de factuur zelf meenemen als override
            if (invoice.ProjectId.HasValue && (bestProject == null || invoice.ProjectId != bestProject.ProjectId))
            {
                var linkedProject = await _db.Project
                    .AsNoTracking()
                    .Where(p => p.ProjectId == invoice.ProjectId.Value)
                    .Select(p => new { p.ProjectId, p.ProjectName })
                    .FirstOrDefaultAsync(ct);

                if (linkedProject != null)
                {
                    bestProject = new ProjectMatchResult
                    {
                        ProjectId = linkedProject.ProjectId,
                        ProjectName = linkedProject.ProjectName,
                        Score = 90,
                        ConfidenceLevel = "High",
                        MatchReason = "reeds gekoppeld in factuur",
                        MatchedFields = "Existing",
                        Source = "Existing"
                    };
                }
            }

            // ── Stap 6: contractmatching ─────────────────────────────────────
            IReadOnlyList<ContractMatchResult> contractCandidates = Array.Empty<ContractMatchResult>();
            ContractMatchResult bestContract = null;

            if (bestProject != null)
            {
                contractCandidates = await _contractMatcher.FindMatchesAsync(
                    bestProject.ProjectId,
                    invoice.CompanyId,
                    string.Join(" ", invoice.IncommingInvoiceDetail.Select(d => d.Description)),
                    ct);

                if (contractCandidates.Count > 0)
                {
                    bestContract = contractCandidates[0];
                    if (contractCandidates.Count > 1)
                    {
                        reasonFlags |= (long)InvoiceReasonFlags.MultipleContractCandidates;
                        warnings.Add((
                            "MultipleContractCandidates",
                            $"{contractCandidates.Count} contractkandidaten gevonden — controleer de keuze.",
                            "Warning", "ContractMatcher"));
                    }

                    if (bestContract.Score < 50)
                    {
                        reasonFlags |= (long)InvoiceReasonFlags.ContractUncertain;
                        warnings.Add(("ContractUncertain",
                            $"Contractmatch onzeker (score {bestContract.Score}).",
                            "Warning", "ContractMatcher"));
                    }
                }
            }

            // ── Stap 7: kostenrekening ───────────────────────────────────────
            var isGeneralCost = bestProject == null || supplierInfo?.IsAlwaysGeneralCost == true;
            var allDesc = string.Join(" ",
                invoice.IncommingInvoiceDetail.Select(d => d.Description).Where(d => d != null));
            var accounting = await _accountingSuggester.SuggestAsync(
                invoice.CompanyId,
                bestProject?.ProjectId,
                invoice.IssuerCompanyId,
                allDesc,
                isGeneralCost,
                ct);

            // ── Stap 8: duplicaatcontrole ────────────────────────────────────
            var (isDuplicate, duplicateId) = await CheckDuplicateAsync(invoice, ct);
            if (isDuplicate)
            {
                reasonFlags |= (long)InvoiceReasonFlags.DuplicateSuspected;
                warnings.Add(("DuplicateSuspected",
                    $"Mogelijke dubbele factuur — zie factuur #{duplicateId}.",
                    "Warning", "DuplicateCheck"));
            }

            // ── Stap 9: opslaan ──────────────────────────────────────────────
            var enrichment = await UpsertEnrichmentAsync(
                invoice, classification, supplierInfo, bestProject, projectCandidates,
                bestContract, contractCandidates, accounting, reasonFlags, isDuplicate,
                duplicateId, enrichedDueDate, enrichedInvoiceNr, enrichedDocType,
                hasWeakUblLines, proposedPdfLines, ct);

            await SaveWarningsAsync(invoice.Id, warnings, ct);

            var ublLines = invoice.IncommingInvoiceDetail
                .Select(d => (d.Description, d.Price))
                .ToList();

            // ── Stap 10: auto-fill bij hoge confidence ───────────────────────
            // Status blijft altijd 'Nieuw' na verrijking.
            // Uitzondering: ManualProcessing (leverancier vereist handmatige afhandeling).
            // De statusovergang Nieuw → Te keuren gebeurt pas bij ConfirmEnrichmentAsync.
            if (invoice.StatusId == IncomingInvoiceStatus.New
             || invoice.StatusId == IncomingInvoiceStatus.Enriched)  // backwards compat
            {
                var invoiceEntity = await _db.IncommingInvoices.FindAsync(new object[] { invoice.Id }, ct);
                if (invoiceEntity != null)
                {
                    // Alleen ManualProcessing wordt direct gezet — dat is een andere workflow
                    if (supplierInfo?.RequiresManualReview == true)
                        invoiceEntity.StatusId = IncomingInvoiceStatus.ManualProcessing;

                    invoiceEntity.UpdatedAt = DateTime.UtcNow;

                    // Score ≥ 80 → automatisch invullen, maar NIET als geblokkeerd of inactief
                    if (bestProject?.Score >= 80 && !invoiceEntity.ProjectId.HasValue)
                    {
                        if (bestProject.BlockedByBusinessRule)
                        {
                            _logger.LogInformation(
                                "Factuur {Id}: auto-fill GEBLOKKEERD — project '{Name}' is niet toegestaan ({Reason}).",
                                invoiceId, bestProject.ProjectName, bestProject.BlockReason);
                        }
                        else if (bestProject.IsInactiveProject)
                        {
                            _logger.LogInformation(
                                "Factuur {Id}: auto-fill GEBLOKKEERD — project '{Name}' is inactief (status={Status}).",
                                invoiceId, bestProject.ProjectName, bestProject.ProjectStatusLabel);
                        }
                        else
                        {
                            invoiceEntity.ProjectId = bestProject.ProjectId;
                            _logger.LogInformation(
                                "Factuur {Id}: project automatisch ingevuld (score {Score}): '{Name}'.",
                                invoiceId, bestProject.Score, bestProject.ProjectName);
                        }
                    }

                    if (bestContract?.Score >= 80 && !invoiceEntity.ContractId.HasValue)
                    {
                        invoiceEntity.ContractId = bestContract.ContractId;
                        _logger.LogInformation(
                            "Factuur {Id}: contract automatisch ingevuld op basis van hoge confidence (score {Score}): '{Name}'.",
                            invoiceId, bestContract.Score, bestContract.ContractName);
                    }

                    await _db.SaveChangesAsync(ct);
                }
            }

            _logger.LogInformation(
                "Verrijkingspipeline voltooid voor factuur {Id}: project={Project}, score={Score}, waarschuwingen={Warnings}.",
                invoiceId,
                bestProject?.ProjectName ?? "—", bestProject?.Score ?? 0,
                warnings.Count);

            var (displayLines, displayFromPdf) =
                BuildDisplayLines(ublLines, hasWeakUblLines, proposedPdfLines);

            return MapToVm(enrichment, warnings.Select(w => new InvoiceWarningVm
            {
                Code = w.Code,
                Message = w.Message,
                Severity = w.Severity,
                Source = w.Source
            }).ToList(), displayLines, displayFromPdf);
        }

        public async Task<InvoiceEnrichmentVm> GetEnrichmentAsync(int invoiceId, CancellationToken ct = default)
        {
            var enrichment = await _db.IncomingInvoiceEnrichment
                .Include(e => e.SuggestedProject)
                .Include(e => e.UserConfirmedProject)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IncomingInvoiceId == invoiceId, ct);

            if (enrichment == null) return null;

            var warnings = await _db.IncomingInvoiceWarning
                .AsNoTracking()
                .Where(w => w.IncomingInvoiceId == invoiceId && !w.IsResolved)
                .Select(w => new InvoiceWarningVm
                {
                    Id = w.Id,
                    Code = w.WarningCode,
                    Message = w.WarningMessage,
                    Severity = w.Severity,
                    Source = w.Source,
                    IsResolved = w.IsResolved
                })
                .ToListAsync(ct);

            // Laad invoice-lijnen voor DisplayLines
            var ublLines = await _db.IncommingInvoiceDetail
                .AsNoTracking()
                .Where(d => d.IncommingInvoiceId == invoiceId)
                .Select(d => new { d.Description, d.Price })
                .ToListAsync(ct);

            var vm = MapToVm(enrichment, warnings);

            // Bouw DisplayLines op basis van opgeslagen HasWeakUblLines + ProposedPdfLines
            var (displayLines, displayFromPdf) = BuildDisplayLines(
                ublLines.Select(d => (d.Description, d.Price)).ToList(),
                vm.HasWeakUblLines,
                vm.ProposedPdfLines as IList<ProposedInvoiceLine>
                    ?? vm.ProposedPdfLines.ToList());

            vm.DisplayLines = displayLines;
            vm.DisplayLinesFromPdf = displayFromPdf;

            return vm;
        }

        public async Task ConfirmEnrichmentAsync(ConfirmEnrichmentRequest request, CancellationToken ct = default)
        {
            var invoice = await _db.IncommingInvoices.FindAsync(new object[] { request.IncomingInvoiceId }, ct)
                ?? throw new InvalidOperationException($"Factuur {request.IncomingInvoiceId} niet gevonden.");

            // Project updaten
            if (request.ProjectId.HasValue)
                invoice.ProjectId = request.ProjectId.Value;

            // Contract updaten
            if (request.ContractId.HasValue)
                invoice.ContractId = request.ContractId.Value;

            // Status vooruitschuiven
            invoice.StatusId = IncomingInvoiceStatus.PendingApproval;
            invoice.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                invoice.Notes = request.Notes;

            // Enrichment bijwerken
            var enrichment = await _db.IncomingInvoiceEnrichment
                .FirstOrDefaultAsync(e => e.IncomingInvoiceId == request.IncomingInvoiceId, ct);

            if (enrichment != null)
            {
                enrichment.UserReviewedAt = DateTime.UtcNow;
                enrichment.UserReviewedBy = request.ReviewedBy;
                enrichment.UserConfirmedProjectId = request.ProjectId;
                enrichment.UserConfirmedContractId = request.ContractId;
                enrichment.UserConfirmedAccountingCode = request.AccountingCode;
                enrichment.UserNotes = request.Notes;
                enrichment.ReasonFlags |= (long)InvoiceReasonFlags.UserCorrected;
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Factuur {Id} bevestigd door {User}: project={Project}, contract={Contract}.",
                request.IncomingInvoiceId, request.ReviewedBy, request.ProjectId, request.ContractId);
        }

        public async Task AcceptPdfLinesAsync(AcceptPdfLinesRequest request, CancellationToken ct = default)
        {
            var invoice = await _db.IncommingInvoices
                .Include(i => i.IncommingInvoiceDetail)
                .FirstOrDefaultAsync(i => i.Id == request.IncomingInvoiceId, ct)
                ?? throw new InvalidOperationException($"Factuur {request.IncomingInvoiceId} niet gevonden.");

            // Haal de voorgestelde lijnen op uit het opgeslagen ExtractionResultJson
            var enrichment = await _db.IncomingInvoiceEnrichment
                .FirstOrDefaultAsync(e => e.IncomingInvoiceId == request.IncomingInvoiceId, ct)
                ?? throw new InvalidOperationException("Geen verrijkingsresultaat gevonden voor deze factuur.");

            List<ProposedInvoiceLine> proposed = null;
            if (!string.IsNullOrWhiteSpace(enrichment.ExtractionResultJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(enrichment.ExtractionResultJson);
                    if (doc.RootElement.TryGetProperty("ProposedPdfLines", out var p)
                        && p.ValueKind == System.Text.Json.JsonValueKind.Array)
                        proposed = JsonSerializer.Deserialize<List<ProposedInvoiceLine>>(p.GetRawText());
                }
                catch { }
            }

            if (proposed == null || !proposed.Any())
                throw new InvalidOperationException("Geen PDF-factuurlijnen beschikbaar om over te nemen.");

            // Verwijder bestaande lijnen en vervang door PDF-lijnen
            _db.IncommingInvoiceDetail.RemoveRange(invoice.IncommingInvoiceDetail);

            foreach (var line in proposed)
            {
                _db.IncommingInvoiceDetail.Add(new IncommingInvoiceDetail
                {
                    IncommingInvoiceId = invoice.Id,
                    Description = line.Description,
                    Price       = line.LineTotal,
                    Type        = 0
                });
            }

            // Markeer in enrichment
            enrichment.ReasonFlags |= (long)InvoiceReasonFlags.LinesRecoveredFromPdf
                                    | (long)InvoiceReasonFlags.UserCorrected;
            enrichment.UserReviewedAt = DateTime.UtcNow;
            enrichment.UserReviewedBy = request.AcceptedBy;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Factuur {Id}: {Count} PDF-factuurlijnen overgenomen door {User}.",
                request.IncomingInvoiceId, proposed.Count, request.AcceptedBy);
        }

        public async Task LearnSupplierRuleAsync(LearnSupplierRuleRequest request, CancellationToken ct = default)
        {
            var rule = new InvoiceSupplierRule
            {
                IssuerCompanyId = request.IssuerCompanyId,
                CompanyId = request.CompanyId,
                SupplierVatNumber = request.SupplierVatNumber,
                RuleType = request.RuleType ?? "DefaultProject",
                DefaultProjectId = request.DefaultProjectId,
                DefaultAccountingCode = request.DefaultAccountingCode,
                IsGeneralCost = request.IsGeneralCost,
                RequiresManualReview = request.RequiresManualReview,
                RequiresProject = request.RequiresProject,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy,
                IsActive = true
            };

            _db.InvoiceSupplierRule.Add(rule);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Leverancierregel aangemaakt door {User}: type={Type}, leverancier={Supplier}.",
                request.CreatedBy, request.RuleType, request.SupplierVatNumber ?? request.CompanyId?.ToString());
        }

        public async Task LearnProjectAliasAsync(LearnProjectAliasRequest request, CancellationToken ct = default)
        {
            var existing = await _db.InvoiceProjectAlias
                .FirstOrDefaultAsync(a => a.ProjectId == request.ProjectId
                                       && a.Alias == request.Alias, ct);

            if (existing != null)
            {
                existing.IsActive = true;
                existing.AliasType = request.AliasType;
            }
            else
            {
                _db.InvoiceProjectAlias.Add(new InvoiceProjectAlias
                {
                    ProjectId = request.ProjectId,
                    Alias = request.Alias,
                    AliasType = request.AliasType ?? "Name",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy,
                    IsActive = true
                });
            }

            await _db.SaveChangesAsync(ct);
        }

        // ─── Tekst-extractie ──────────────────────────────────────────────────

        private async Task<(string combined, string ublText, string pdfText, string ocrText, long flags)>
            ExtractTextAsync(
                IncommingInvoices invoice,
                List<(string Code, string Message, string Severity, string Source)> warnings,
                CancellationToken ct)
        {
            long flags = 0;
            var ublParts = new List<string>();
            var pdfParts = new List<string>();
            var ocrParts = new List<string>();

            foreach (var att in invoice.IncomingInvoiceAttachments)
            {
                if (att.Content == null) continue;

                if (att.AttachmentType == "UBL")
                {
                    var ublText = ExtractUblText(att.Content);
                    if (!string.IsNullOrWhiteSpace(ublText))
                        ublParts.Add(ublText);
                }
                else if (att.AttachmentType == "PDF")
                {
                    var threshold = _extractionOptions.ScanDetectionWordThreshold;
                    var isScan = IsScanPdf(att.Content, threshold);

                    if (!isScan)
                    {
                        var text = ExtractPdfText(att.Content);
                        if (!string.IsNullOrWhiteSpace(text))
                            pdfParts.Add(text);
                    }
                    else if (_azureService.IsEnabled)
                    {
                        flags |= (long)InvoiceReasonFlags.OcrUsed;
                        var result = await _azureService.AnalyzeAsync(att.Content, ct);
                        if (result.Success)
                        {
                            // Header-velden + alle regelomschrijvingen meenemen als zoektekst
                            var ocrParts2 = new List<string>
                                { result.VendorName, result.InvoiceId, result.OrderReference };
                            ocrParts2.AddRange(result.Lines.Select(l => l.Description));
                            var ocrText = string.Join(" ", ocrParts2.Where(s => !string.IsNullOrWhiteSpace(s)));
                            if (!string.IsNullOrWhiteSpace(ocrText))
                                ocrParts.Add(ocrText);
                        }
                    }
                    else
                    {
                        flags |= (long)InvoiceReasonFlags.AzureSkippedNoConfig;
                        warnings.Add((
                            "AzureSkippedNoConfig",
                            "Scan-PDF gedetecteerd maar Azure OCR is niet geconfigureerd.",
                            "Info", "Extraction"));
                    }
                }
            }

            if (!pdfParts.Any() && !ocrParts.Any() && !ublParts.Any())
            {
                flags |= (long)InvoiceReasonFlags.PdfTextUnavailable;
                warnings.Add(("PdfTextUnavailable",
                    "Geen tekst beschikbaar uit PDF of UBL voor project-/contractmatching.",
                    "Info", "Extraction"));
            }

            // Opgeslagen factuurlijnen (vanuit Octopus/UBL sync) als extra zoektekst.
            // Dit is de kritieke toevoeging: "werf Wonnegaarde - Otegem" in een lijnomschrijving
            // bereikt zo de projectmatcher ook als het niet in de raw PDF/UBL-bytes staat.
            var detailLines = invoice.IncommingInvoiceDetail
                .Select(d => d.Description)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .ToList();
            if (detailLines.Any())
                pdfParts.Add(string.Join(" ", detailLines));

            // Voeg ook de Octopus-referentievelden toe als zoektekst
            var octopusRef = string.Join(" ",
                new[] { invoice.ExternalId, invoice.SupplierName, invoice.SupplierVatNumber }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

            var ublFull = string.Join(" ", ublParts);
            var pdfFull = string.Join(" ", pdfParts);
            var ocrFull = string.Join(" ", ocrParts);
            var combined = string.Join(" ",
                new[] { ublFull, pdfFull, ocrFull, octopusRef }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

            _logger.LogDebug(
                "Tekst-extractie factuur {Id}: UBL={UblLen} tekens, PDF={PdfLen} tekens, OCR={OcrLen} tekens, " +
                "DetailLijnen={DetailCount}, Combined preview: '{Preview}'",
                invoice.Id, ublFull.Length, pdfFull.Length, ocrFull.Length, detailLines.Count,
                combined.Length > 200 ? combined[..200] + "…" : combined);

            return (combined, ublFull, pdfFull, ocrFull, flags);
        }

        private static string ExtractUblText(byte[] xmlContent)
        {
            try
            {
                var xml = Encoding.UTF8.GetString(xmlContent);
                var doc = XDocument.Parse(xml);

                // Verzamel alle tekst uit relevante UBL-velden
                XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
                XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

                var root = doc.Root;
                if (root == null) return null;

                var parts = new List<string>();

                void AddText(string val)
                {
                    if (!string.IsNullOrWhiteSpace(val)) parts.Add(val.Trim());
                }

                AddText(root.Element(cbc + "ID")?.Value);
                AddText(root.Element(cbc + "BuyerReference")?.Value);
                AddText(root.Element(cac + "OrderReference")?.Element(cbc + "ID")?.Value);
                AddText(root.Element(cac + "ContractDocumentReference")?.Element(cbc + "ID")?.Value);
                AddText(root.Element(cac + "AdditionalDocumentReference")?.Element(cbc + "ID")?.Value);

                // Afleveringsadres
                var delivery = root.Element(cac + "Delivery");
                if (delivery != null)
                {
                    var addr = delivery.Element(cac + "DeliveryLocation")?.Element(cac + "Address");
                    AddText(addr?.Element(cbc + "StreetName")?.Value);
                    AddText(addr?.Element(cbc + "CityName")?.Value);
                    AddText(addr?.Element(cbc + "PostalZone")?.Value);
                }

                // Factuurlijnen omschrijvingen
                var lineElementName = root.Name.LocalName == "CreditNote" ? "CreditNoteLine" : "InvoiceLine";
                foreach (var line in root.Elements(cac + lineElementName))
                {
                    AddText(line.Element(cac + "Item")?.Element(cbc + "Name")?.Value);
                    AddText(line.Element(cac + "Item")?.Element(cbc + "Description")?.Value);
                    AddText(line.Element(cbc + "Note")?.Value);
                }

                // Vrije mededeling / Note op factuurniveau
                foreach (var note in root.Elements(cbc + "Note"))
                    AddText(note.Value);

                return string.Join(" ", parts);
            }
            catch { return null; }
        }

        private static string ExtractPdfText(byte[] pdfBytes)
        {
            try
            {
                using var doc = PdfDocument.Open(pdfBytes);
                var sb = new StringBuilder();
                foreach (var page in doc.GetPages())
                    sb.AppendLine(string.Join(" ", page.GetWords().Select(w => w.Text)));
                return sb.ToString();
            }
            catch { return null; }
        }

        private static bool IsScanPdf(byte[] pdfBytes, int threshold)
        {
            try
            {
                using var doc = PdfDocument.Open(pdfBytes);
                var pages = doc.GetPages().ToList();
                if (pages.Count == 0) return true;
                var totalWords = pages.Sum(p => p.GetWords().Count());
                return totalWords / pages.Count < threshold;
            }
            catch { return true; }
        }

        // ─── Leverancierregels ───────────────────────────────────────────────

        private async Task<SupplierInfoResult?> ResolveSupplierInfoAsync(
            IncommingInvoices invoice,
            List<(string, string, string, string)> warnings,
            CancellationToken ct)
        {
            var rules = await _db.InvoiceSupplierRule
                .Where(r => r.IsActive
                         && (r.CompanyId == null || r.CompanyId == invoice.CompanyId)
                         && (r.IssuerCompanyId == null || r.IssuerCompanyId == invoice.IssuerCompanyId)
                         && (r.SupplierVatNumber == null
                             || r.SupplierVatNumber == invoice.SupplierVatNumber))
                .OrderByDescending(r => r.CompanyId != null)
                .AsNoTracking()
                .ToListAsync(ct);

            if (!rules.Any())
                return null;

            var rule = rules.First();

            long supplierReasonFlags = 0;

            if (rule.RequiresManualReview)
            {
                supplierReasonFlags |= (long)InvoiceReasonFlags.SupplierUncertain;

                warnings.Add(("ManualReviewRequired",
                    "Leverancierregel: altijd manueel te controleren.",
                    "Warning", "RuleEngine"));
            }

            return new SupplierInfoResult
            {
                DefaultAccountingCode = rule.DefaultAccountingCode,
                DefaultProjectId = rule.DefaultProjectId,
                IsAlwaysGeneralCost = rule.IsGeneralCost,
                RequiresManualReview = rule.RequiresManualReview,
                ReasonFlags = supplierReasonFlags
            };
        }

        // ─── Documentclassificatie ───────────────────────────────────────────
        public class SupplierInfoResult
        {
            public string DefaultAccountingCode { get; set; }
            public int? DefaultProjectId { get; set; }
            public bool IsAlwaysGeneralCost { get; set; }
            public bool RequiresManualReview { get; set; }
            public long ReasonFlags { get; set; }
        }
        private static string ClassifyDocument(IncommingInvoices invoice, string pdfText, string ublText)
        {
            // UBL-veld heeft prioriteit
            if (!string.IsNullOrWhiteSpace(invoice.DocumentType))
            {
                return invoice.DocumentType switch
                {
                    "CreditNote" => "CreditNote",
                    "Invoice" => DetectInvoiceSubtype(pdfText ?? ublText ?? string.Empty),
                    _ => "Unknown"
                };
            }

            var text = (pdfText ?? ublText ?? string.Empty).ToLowerInvariant();

            if (text.Contains("creditnota") || text.Contains("credit note") || text.Contains("avoir"))
                return "CreditNote";

            return DetectInvoiceSubtype(text);
        }

        private static string DetectInvoiceSubtype(string text)
        {
            var t = text.ToLowerInvariant();
            if (t.Contains("voorschot") || t.Contains("avance") || t.Contains("acompte") || t.Contains("advance"))
                return "AdvanceInvoice";
            if (t.Contains("eindfactuur") || t.Contains("eindafrekening") || t.Contains("final invoice"))
                return "FinalInvoice";
            if (t.Contains("herinnering") || t.Contains("rappel") || t.Contains("reminder"))
                return "PaymentReminder";
            return "Invoice";
        }

        // ─── Veldverrijking ──────────────────────────────────────────────────

        private static EnrichedFieldValue<DateOnly?> BuildDueDateField(
            IncommingInvoices invoice,
            string pdfText,
            List<(string, string, string, string)> warnings,
            ref long reasonFlags)
        {
            if (!invoice.DueDate.HasValue)
            {
                reasonFlags |= (long)InvoiceReasonFlags.MissingDueDate;
                warnings.Add(("MissingDueDate",
                    "Vervaldatum ontbreekt — niet in UBL en niet teruggevonden in PDF.",
                    "Warning", "Validation"));

                return new EnrichedFieldValue<DateOnly?>
                {
                    Value = null,
                    Source = "None",
                    Confidence = "Low",
                    NeedsReview = true
                };
            }

            // Controleer of UBL en PDF overeenkomen
            var pdfDate = !string.IsNullOrWhiteSpace(pdfText)
                ? ExtractDueDateFromText(pdfText)
                : null;

            if (pdfDate.HasValue && pdfDate != invoice.DueDate && pdfDate != invoice.Date)
            {
                warnings.Add(("DueDateConflict",
                    $"Vervaldatum verschilt: UBL={invoice.DueDate:dd/MM/yyyy}, PDF={pdfDate:dd/MM/yyyy}.",
                    "Warning", "Validation"));
                return new EnrichedFieldValue<DateOnly?>
                {
                    Value = invoice.DueDate,
                    Source = "Ubl",
                    Confidence = "Medium",
                    NeedsReview = true,
                    ConflictNote = $"PDF-datum: {pdfDate:dd/MM/yyyy}"
                };
            }

            return new EnrichedFieldValue<DateOnly?>
            {
                Value = invoice.DueDate,
                Source = "Ubl",
                Confidence = "High",
                NeedsReview = false
            };
        }

        private static EnrichedFieldValue<string> BuildInvoiceNumberField(
            IncommingInvoices invoice,
            List<(string, string, string, string)> warnings,
            ref long reasonFlags)
        {
            if (string.IsNullOrWhiteSpace(invoice.ExternalId))
            {
                reasonFlags |= (long)InvoiceReasonFlags.MissingInvoiceNumber;
                warnings.Add(("MissingInvoiceNumber",
                    "Factuurnummer ontbreekt.",
                    "Warning", "Validation"));
                return new EnrichedFieldValue<string>
                    { Value = null, Source = "None", Confidence = "Low", NeedsReview = true };
            }

            return new EnrichedFieldValue<string>
            {
                Value = invoice.ExternalId,
                Source = "Ubl",
                Confidence = "High",
                NeedsReview = false
            };
        }

        private static EnrichedFieldValue<string> BuildDocTypeField(
            IncommingInvoices invoice,
            string classification)
        {
            return new EnrichedFieldValue<string>
            {
                Value = classification,
                Source = string.IsNullOrWhiteSpace(invoice.DocumentType) ? "PdfText" : "Ubl",
                Confidence = "Medium",
                NeedsReview = false
            };
        }

        // ─── Duplicaatcontrole ───────────────────────────────────────────────

        private async Task<(bool IsDuplicate, int? SuspectId)> CheckDuplicateAsync(
            IncommingInvoices invoice, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(invoice.ExternalId)) return (false, null);

            var suspect = await _db.IncommingInvoices
                .AsNoTracking()
                .Where(i => i.Id != invoice.Id
                         && i.IssuerCompanyId == invoice.IssuerCompanyId
                         && i.ExternalId == invoice.ExternalId
                         && i.SupplierVatNumber == invoice.SupplierVatNumber
                         && i.Date == invoice.Date
                         && i.StatusId != IncomingInvoiceStatus.Duplicate)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync(ct);

            return (suspect.HasValue, suspect);
        }

        // ─── Opslaan ─────────────────────────────────────────────────────────

        private async Task<IncomingInvoiceEnrichment> UpsertEnrichmentAsync(
            IncommingInvoices invoice,
            string classification,
            SupplierInfoResult? supplierInfo,
            ProjectMatchResult? bestProject,
            IReadOnlyList<ProjectMatchResult> allProjects,
            ContractMatchResult? bestContract,
            IReadOnlyList<ContractMatchResult> allContracts,
            AccountingSuggestion? accounting,
            long reasonFlags,
            bool isDuplicate,
            int? duplicateId,
            EnrichedFieldValue<DateOnly?> enrichedDueDate,
            EnrichedFieldValue<string> enrichedInvoiceNr,
            EnrichedFieldValue<string> enrichedDocType,
            bool hasWeakUblLines,
            List<ProposedInvoiceLine> proposedPdfLines,
            CancellationToken ct)
        {
            var enrichment = await _db.IncomingInvoiceEnrichment
                .FirstOrDefaultAsync(e => e.IncomingInvoiceId == invoice.Id, ct);

            var altProjects = allProjects.Skip(1).Take(5).ToList();
            var altContracts = allContracts.Skip(1).Take(5).ToList();

            var extractionJson = JsonSerializer.Serialize(new
            {
                InvoiceNumber = enrichedInvoiceNr,
                DueDate = new
                {
                    enrichedDueDate.Value,
                    enrichedDueDate.Source,
                    enrichedDueDate.Confidence,
                    enrichedDueDate.NeedsReview,
                    enrichedDueDate.ConflictNote
                },
                DocumentType = enrichedDocType,
                SupplierRule = supplierInfo == null ? null : new
                {
                    supplierInfo.DefaultAccountingCode,
                    supplierInfo.DefaultProjectId,
                    supplierInfo.IsAlwaysGeneralCost,
                    supplierInfo.RequiresManualReview,
                    supplierInfo.ReasonFlags
                },
                HasWeakUblLines = hasWeakUblLines,
                ProposedPdfLines = proposedPdfLines,
                GeneratedAt = DateTime.UtcNow
            });

            var suggestedAccountingCode =
                accounting?.AccountingCode ?? supplierInfo?.DefaultAccountingCode;

            var suggestedAccountingDescription =
                accounting?.Description;

            var suggestedAccountingSource =
                accounting?.Source ?? (supplierInfo?.DefaultAccountingCode != null ? "SupplierRule" : null);

            var isGeneralCostSuggested =
                accounting?.IsGeneralCost ?? supplierInfo?.IsAlwaysGeneralCost ?? false;

            if (enrichment == null)
            {
                enrichment = new IncomingInvoiceEnrichment
                {
                    IncomingInvoiceId = invoice.Id
                };

                _db.IncomingInvoiceEnrichment.Add(enrichment);
            }

            enrichment.EnrichedAt = DateTime.UtcNow;
            enrichment.DocumentClassification = classification;

            enrichment.SuggestedProjectId = bestProject?.ProjectId;
            enrichment.SuggestedProjectName = bestProject?.ProjectName;
            enrichment.SuggestedProjectScore = bestProject?.Score ?? 0;
            enrichment.SuggestedProjectConfidence = bestProject?.ConfidenceLevel;
            enrichment.SuggestedProjectMatchReason = bestProject?.MatchReason;
            enrichment.SuggestedProjectMatchedFields = bestProject?.MatchedFields;
            enrichment.AltProjectCandidatesJson = altProjects.Any()
                ? JsonSerializer.Serialize(altProjects)
                : null;

            enrichment.SuggestedContractId = bestContract?.ContractId;
            enrichment.SuggestedContractName = bestContract?.ContractName;
            enrichment.SuggestedContractScore = bestContract?.Score ?? 0;
            enrichment.SuggestedContractConfidence = bestContract?.ConfidenceLevel;
            enrichment.AltContractCandidatesJson = altContracts.Any()
                ? JsonSerializer.Serialize(altContracts)
                : null;

            enrichment.SuggestedAccountingCode = suggestedAccountingCode;
            enrichment.SuggestedAccountingDescription = suggestedAccountingDescription;
            enrichment.SuggestedAccountingSource = suggestedAccountingSource;
            enrichment.IsGeneralCostSuggested = isGeneralCostSuggested;

            enrichment.ReasonFlags = reasonFlags;
            enrichment.IsDuplicateSuspected = isDuplicate;
            enrichment.DuplicateSuspectInvoiceId = duplicateId;
            enrichment.ExtractionResultJson = extractionJson;

            await _db.SaveChangesAsync(ct);

            return enrichment;
        }

        private async Task SaveWarningsAsync(
            int invoiceId,
            List<(string Code, string Message, string Severity, string Source)> warnings,
            CancellationToken ct)
        {
            // Verwijder bestaande onopgeloste waarschuwingen
            var existing = await _db.IncomingInvoiceWarning
                .Where(w => w.IncomingInvoiceId == invoiceId && !w.IsResolved)
                .ToListAsync(ct);
            _db.IncomingInvoiceWarning.RemoveRange(existing);

            foreach (var (code, message, severity, source) in warnings)
            {
                _db.IncomingInvoiceWarning.Add(new IncomingInvoiceWarning
                {
                    IncomingInvoiceId = invoiceId,
                    WarningCode = code,
                    WarningMessage = message,
                    Severity = severity,
                    Source = source,
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (warnings.Any())
                await _db.SaveChangesAsync(ct);
        }

        // ─── Status bepalen ──────────────────────────────────────────────────

        private static string FriendlyBlockReason(string blockReason) => blockReason switch
        {
            "MissingIssuerCompanyRelation" => "geen bouwheer-koppeling op dit project",
            "DifferentIssuerCompany"       => "project hoort bij een andere bouwheer",
            _                              => blockReason ?? "onbekende businessregel"
        };

        private static byte DetermineStatus(
            long reasonFlags,
            SupplierInfoResult? supplierInfo)
        {
            if (supplierInfo?.RequiresManualReview == true)
                return IncomingInvoiceStatus.ManualProcessing;

            // Heeft het warnings die directe aandacht vragen?
            const long criticalFlags =
                (long)InvoiceReasonFlags.DuplicateSuspected
                | (long)InvoiceReasonFlags.SupplierUncertain
                | (long)InvoiceReasonFlags.AmountMismatch
                | (long)InvoiceReasonFlags.BtwnumberMismatch
                | (long)InvoiceReasonFlags.MultipleProjectCandidates;

            if ((reasonFlags & criticalFlags) != 0)
                return IncomingInvoiceStatus.NeedsReview;

            const long reviewFlags =
                (long)InvoiceReasonFlags.MissingDueDate
                | (long)InvoiceReasonFlags.ProjectUncertain
                | (long)InvoiceReasonFlags.ContractUncertain
                | (long)InvoiceReasonFlags.MultipleContractCandidates;

            if ((reasonFlags & reviewFlags) != 0)
                return IncomingInvoiceStatus.NeedsReview;

            return IncomingInvoiceStatus.Enriched;
        }

        // ─── Mapping ─────────────────────────────────────────────────────────

        private static InvoiceEnrichmentVm MapToVm(
            IncomingInvoiceEnrichment e,
            IReadOnlyList<InvoiceWarningVm> warnings,
            IReadOnlyList<DisplayInvoiceLine> displayLines = null,
            bool displayFromPdf = false)
        {
            List<ProjectMatchResult> altProjects = null;
            if (!string.IsNullOrWhiteSpace(e.AltProjectCandidatesJson))
            {
                try { altProjects = JsonSerializer.Deserialize<List<ProjectMatchResult>>(e.AltProjectCandidatesJson); }
                catch { /* ignore */ }
            }

            List<ContractMatchResult> altContracts = null;
            if (!string.IsNullOrWhiteSpace(e.AltContractCandidatesJson))
            {
                try { altContracts = JsonSerializer.Deserialize<List<ContractMatchResult>>(e.AltContractCandidatesJson); }
                catch { /* ignore */ }
            }

            // Lees HasWeakUblLines en ProposedPdfLines uit ExtractionResultJson
            bool hasWeakUblLines = false;
            List<ProposedInvoiceLine> proposedPdfLines = null;
            if (!string.IsNullOrWhiteSpace(e.ExtractionResultJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(e.ExtractionResultJson);
                    if (doc.RootElement.TryGetProperty("HasWeakUblLines", out var wkProp))
                        hasWeakUblLines = wkProp.GetBoolean();
                    if (doc.RootElement.TryGetProperty("ProposedPdfLines", out var linesProp)
                        && linesProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        proposedPdfLines = JsonSerializer.Deserialize<List<ProposedInvoiceLine>>(linesProp.GetRawText());
                    }
                }
                catch { /* defensive */ }
            }

            return new InvoiceEnrichmentVm
            {
                IncomingInvoiceId = e.IncomingInvoiceId,
                EnrichedAt = e.EnrichedAt,
                DocumentClassification = e.DocumentClassification,
                SupplierMatch = new SupplierMatchInfo
                {
                    Iban = e.SupplierIban,
                    Confidence = e.SupplierMatchConfidence,
                    MatchReason = e.SupplierMatchReason
                },
                SuggestedProject = e.SuggestedProjectId.HasValue ? new ProjectMatchResult
                {
                    ProjectId = e.SuggestedProjectId.Value,
                    ProjectName = e.SuggestedProjectName,
                    Score = e.SuggestedProjectScore,
                    ConfidenceLevel = e.SuggestedProjectConfidence,
                    MatchReason = e.SuggestedProjectMatchReason,
                    MatchedFields = e.SuggestedProjectMatchedFields
                } : null,
                AlternativeProjects = altProjects ?? new List<ProjectMatchResult>(),
                SuggestedContract = e.SuggestedContractId.HasValue ? new ContractMatchResult
                {
                    ContractId = e.SuggestedContractId.Value,
                    ContractName = e.SuggestedContractName,
                    Score = e.SuggestedContractScore,
                    ConfidenceLevel = e.SuggestedContractConfidence
                } : null,
                AlternativeContracts = altContracts ?? new List<ContractMatchResult>(),
                AccountingSuggestion = !string.IsNullOrWhiteSpace(e.SuggestedAccountingCode) ? new AccountingSuggestion
                {
                    AccountingCode = e.SuggestedAccountingCode,
                    Description = e.SuggestedAccountingDescription,
                    Source = e.SuggestedAccountingSource,
                    IsGeneralCost = e.IsGeneralCostSuggested,
                    Confidence = e.SuggestedProjectConfidence
                } : null,
                ReasonFlags = e.ReasonFlags,
                IsDuplicateSuspected = e.IsDuplicateSuspected,
                DuplicateSuspectInvoiceId = e.DuplicateSuspectInvoiceId,
                Warnings = warnings,
                UserReviewedAt = e.UserReviewedAt,
                UserReviewedBy = e.UserReviewedBy,
                UserConfirmedProjectId = e.UserConfirmedProjectId,
                UserConfirmedContractId = e.UserConfirmedContractId,
                UserConfirmedAccountingCode = e.UserConfirmedAccountingCode,
                UserNotes = e.UserNotes,
                HasWeakUblLines      = hasWeakUblLines,
                ProposedPdfLines     = proposedPdfLines ?? new List<ProposedInvoiceLine>(),
                DisplayLines         = displayLines ?? Array.Empty<DisplayInvoiceLine>(),
                DisplayLinesFromPdf  = displayFromPdf
            };
        }

        // ─── Laden ───────────────────────────────────────────────────────────

        private async Task<IncommingInvoices> LoadInvoiceAsync(int invoiceId, CancellationToken ct) =>
            await _db.IncommingInvoices
                .Include(i => i.IncomingInvoiceAttachments)
                .Include(i => i.IncommingInvoiceDetail)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        // ─── Datumextractie uit platte tekst ──────────────────────────────────

        private static readonly string[] DueDateKeywords =
            ["vervaldatum", "te betalen voor", "uiterlijk", "due date", "échéance", "verfallsdatum", "vervaldag"];

        private static DateOnly? ExtractDueDateFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var lower = text.ToLowerInvariant();

            foreach (var kw in DueDateKeywords)
            {
                var idx = lower.IndexOf(kw, StringComparison.Ordinal);
                if (idx < 0) continue;

                var snippet = text.Substring(idx, Math.Min(50, text.Length - idx));
                var m = Regex.Match(snippet, @"\b(\d{1,2})[/\-\.](\d{1,2})[/\-\.](\d{2,4})\b");
                if (m.Success)
                {
                    int day = int.Parse(m.Groups[1].Value);
                    int month = int.Parse(m.Groups[2].Value);
                    int year = int.Parse(m.Groups[3].Value);
                    if (year < 100) year += 2000;
                    try { return new DateOnly(year, month, day); }
                    catch { }
                }
            }
            return null;
        }

        // ─── PDF-factuurlijnextractie ─────────────────────────────────────────

        /// <summary>
        /// Detecteert of de UBL-factuurlijnen onvoldoende zijn voor verwerking.
        /// Criteria: geen lijnen, één generieke lijn ("Invoice Line"), of lijn = totaal.
        /// </summary>
        private static bool HasWeakUblLines(IncommingInvoices invoice)
        {
            var lines = invoice.IncommingInvoiceDetail?.ToList();
            if (lines == null || !lines.Any()) return true;
            if (lines.Count > 1) return false;

            var desc = lines[0].Description?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(desc)
                || string.Equals(desc, "Invoice Line", StringComparison.OrdinalIgnoreCase)
                || string.Equals(desc, "InvoiceLine", StringComparison.OrdinalIgnoreCase)
                || string.Equals(desc, "Factuurlijn", StringComparison.OrdinalIgnoreCase))
                return true;

            // Lijn = factuurtotaal → inhoudsloos
            if (lines[0].Price.HasValue && invoice.Price > 0
                && Math.Abs(lines[0].Price.Value - invoice.Price) < 0.05m)
                return true;

            return false;
        }

        /// <summary>
        /// Extraheert factuurregels uit PDF via twee verplichte methoden:
        ///   1. Positie-gebaseerd (PdfPig Y-groepering)
        ///   2. Plain-text fallback (VERPLICHT als methode 1 geen resultaat geeft)
        /// Beide methoden gebruiken tabelcontextdetectie + somvalidatie.
        /// </summary>
        private List<ProposedInvoiceLine> TryExtractInvoiceLines(byte[] pdfBytes, decimal invoiceTotal)
        {
            List<ProposedInvoiceLine> parsed = null;

            // ── Methode 1: positie-gebaseerde lijnreconstructie (PdfPig) ────────
            try
            {
                var rawLines = ExtractPdfLinesRaw(pdfBytes);
                var (tableStart, tableEnd) = DetectTableBounds(rawLines);

                _logger.LogDebug(
                    "PDF tabelcontext: {Total} lijnen, tabel [{Start}..{End}].",
                    rawLines.Count, tableStart, tableEnd);

                var method1 = new List<ProposedInvoiceLine>();
                for (int i = tableStart; i < tableEnd && i < rawLines.Count; i++)
                {
                    var rawLine = rawLines[i];
                    if (IsSkipLine(rawLine)) continue;
                    if (IsMetadataLine(rawLine)) continue;
                    if (!HasAtLeastTwoNumbers(rawLine)) continue;
                    var line = TryParseInvoiceLine(rawLine);
                    if (line != null)
                    {
                        line.Source = "PdfTable";
                        method1.Add(line);
                    }
                }

                if (method1.Any())
                {
                    _logger.LogDebug(
                        "PDF-lijnextractie methode 1 (positie): {Count} lijn(en).", method1.Count);
                    parsed = method1;
                }
                else
                {
                    _logger.LogDebug("PDF-lijnextractie methode 1: geen resultaat.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "PDF-lijnextractie methode 1 mislukt.");
            }

            // ── Methode 2: plain-text fallback (VERPLICHT als methode 1 leeg) ───
            if (parsed == null || !parsed.Any())
            {
                try
                {
                    var method2 = ExtractLinesPlainText(pdfBytes);
                    if (method2.Any())
                    {
                        _logger.LogDebug(
                            "PDF-lijnextractie methode 2 (plain text): {Count} lijn(en).", method2.Count);
                        parsed = method2;
                    }
                    else
                    {
                        _logger.LogDebug("PDF-lijnextractie methode 2: geen resultaat.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "PDF-lijnextractie methode 2 mislukt.");
                }
            }

            if (parsed == null || !parsed.Any()) return null;

            // ── Somvalidatie + secundaire filtering ──────────────────────────────
            parsed = ApplySumValidation(parsed, invoiceTotal);

            return parsed.Any() ? parsed : null;
        }

        /// <summary>
        /// Somvalidatie: als de som significant afwijkt, filter op lijnen met meer structuur.
        /// Verhoog confidence als som klopt.
        /// </summary>
        private List<ProposedInvoiceLine> ApplySumValidation(
            List<ProposedInvoiceLine> parsed, decimal invoiceTotal)
        {
            var sum = parsed.Sum(l => l.LineTotal ?? 0);

            if (invoiceTotal > 0 && Math.Abs(sum - invoiceTotal) > invoiceTotal * 0.10m)
            {
                // Tweede filtering: behoud enkel lijnen met duidelijke tabelstructuur
                var stricter = parsed
                    .Where(l => l.VatRate.HasValue
                             || (l.Quantity.HasValue && l.UnitPrice.HasValue)
                             || (l.Quantity.HasValue && l.Unit != null))
                    .ToList();

                if (stricter.Any())
                {
                    var stricterSum = stricter.Sum(l => l.LineTotal ?? 0);
                    _logger.LogDebug(
                        "Somvalidatie: origineel {Orig:N2} (afwijking {Dev:N2}), na filtering {Strict:N2} (afwijking {DevS:N2}).",
                        sum, Math.Abs(sum - invoiceTotal), stricterSum, Math.Abs(stricterSum - invoiceTotal));

                    if (Math.Abs(stricterSum - invoiceTotal) < Math.Abs(sum - invoiceTotal))
                    {
                        parsed = stricter;
                        sum = stricterSum;
                    }
                }
            }

            if (Math.Abs(sum - invoiceTotal) < 1m)
            {
                foreach (var l in parsed) l.Confidence = "High";
                _logger.LogInformation(
                    "PDF-lijnensom ({Sum:N2}) ≈ factuurtotaal ({Total:N2}) — confidence High.",
                    sum, invoiceTotal);
            }
            else if (invoiceTotal > 0 && Math.Abs(sum - invoiceTotal) > invoiceTotal * 0.10m)
            {
                _logger.LogWarning(
                    "PDF-lijnensom ({Sum:N2}) wijkt >10% af van factuurtotaal ({Total:N2}).",
                    sum, invoiceTotal);
            }

            return parsed;
        }

        /// <summary>
        /// Methode 2: plain-text fallback (VERPLICHT).
        /// Splitst plain-text PDF op newlines, past tabelcontext en metadata-filter toe.
        /// </summary>
        private List<ProposedInvoiceLine> ExtractLinesPlainText(byte[] pdfBytes)
        {
            var allText = ExtractPdfText(pdfBytes);
            if (string.IsNullOrWhiteSpace(allText)) return new List<ProposedInvoiceLine>();

            var allLines = allText
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .ToList();

            var (tableStart, tableEnd) = DetectTableBounds(allLines);
            var parsed = new List<ProposedInvoiceLine>();

            for (int i = tableStart; i < tableEnd && i < allLines.Count; i++)
            {
                var trimmed = allLines[i];
                if (IsSkipLine(trimmed)) continue;
                if (IsMetadataLine(trimmed)) continue;
                if (!HasAtLeastTwoNumbers(trimmed)) continue;

                var line = TryParseInvoiceLine(trimmed);
                if (line != null)
                {
                    line.Source = "PdfText";
                    parsed.Add(line);
                }
            }

            return parsed;
        }

        /// <summary>
        /// Detecteert de grenzen van de factuurlijntabel in een lijst van tekstregels.
        /// Start = regel na tabelheader (beschrijving + aantal/prijs).
        /// Einde = eerste totaal/btw-regel na de start.
        /// Zonder header: gebruik alle regels.
        /// </summary>
        private static (int start, int end) DetectTableBounds(IList<string> lines)
        {
            int headerIdx = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                var lower = lines[i].ToLowerInvariant();
                bool hasDescCol = Regex.IsMatch(lower,
                    @"\b(beschrijving|omschrijving|description|designation|libelle|artikel|item)\b");
                bool hasNumCol = Regex.IsMatch(lower,
                    @"\b(aantal|qty|quantity|hoeveelheid|prijs|price|montant|eenheid|unit|btw|vat|tva)\b");
                if (hasDescCol && hasNumCol)
                {
                    headerIdx = i;
                    break;
                }
            }

            int start = headerIdx >= 0 ? headerIdx + 1 : 0;

            int end = lines.Count;
            for (int i = start; i < lines.Count; i++)
            {
                var lower = lines[i].ToLowerInvariant().Trim();
                if (lower.StartsWith("totaal") || lower.StartsWith("total")
                    || lower.StartsWith("subtotaal") || lower.StartsWith("subtotal")
                    || lower.StartsWith("grand total") || lower.StartsWith("montant total")
                    || lower.StartsWith("btw") || lower.StartsWith("tva")
                    || lower.StartsWith("bedrag") || lower.StartsWith("te betalen"))
                {
                    end = i;
                    break;
                }
            }

            return (start, end);
        }

        /// <summary>
        /// Detecteert metadata-regels die nooit factuurlijnen zijn:
        /// datums, adressen, contactgegevens, referentienummers.
        /// </summary>
        private static bool IsMetadataLine(string line)
        {
            var lower = line.ToLowerInvariant().Trim();

            // Datum-sleutelwoorden
            if (Regex.IsMatch(lower,
                @"\b(datum|documentdatum|vervaldatum|factuurdatum|due date|invoice date|echeance|vervaldag)\b"))
                return true;

            // E-mailadres
            if (Regex.IsMatch(line, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"))
                return true;

            // BTW-nummer
            if (Regex.IsMatch(lower, @"btw[. \-]?(nr|nummer)|tva[. \-]?n[°o]|vat[. \-]?no|be\s*0\d{9}"))
                return true;

            // IBAN
            if (Regex.IsMatch(lower, @"\biban\b"))
                return true;

            // Telefoonnummer
            if (Regex.IsMatch(lower, @"\b(tel|fax|gsm|mob)[. :]"))
                return true;

            // Postcode + stad zonder prijsstructuur (adresregel)
            if (Regex.IsMatch(lower, @"\b\d{4}\s+[a-z]{2,}") && !Regex.IsMatch(lower, @"\d[.,]\d"))
                return true;

            // Referentie/documentnummer sleutelwoorden
            if (Regex.IsMatch(lower,
                @"\b(referentie|klantreferentie|bestelnr|bestelbon|contractnr|documentnr|factuurnr|invoice no|po\s*nr|order no|ref\.?:)\b"))
                return true;

            // Puur jaar (1900–2100) als enig getal → geen bedrag
            if (Regex.IsMatch(lower, @"^\D{0,40}(19|20)\d{2}\s*$") && !Regex.IsMatch(lower, @"\d[.,]\d"))
                return true;

            return false;
        }

        /// <summary>
        /// Reconstrueert tekstregels uit PDF via Y-positie (PdfPig).
        /// Tolerantie 4 punten zodat woorden op dezelfde regel niet gesplitst worden.
        /// </summary>
        private static List<string> ExtractPdfLinesRaw(byte[] pdfBytes)
        {
            var result = new List<string>();
            using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfBytes);
            foreach (var page in doc.GetPages())
            {
                var wordsByLine = page.GetWords()
                    .GroupBy(w => (int)(w.BoundingBox.Bottom / 4) * 4)
                    .OrderByDescending(g => g.Key);

                foreach (var grp in wordsByLine)
                {
                    var lineText = string.Join(" ",
                        grp.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                    if (!string.IsNullOrWhiteSpace(lineText))
                        result.Add(lineText);
                }
            }
            return result;
        }

        // Pre-filter: factuurlijnen bevatten minimaal 2 getallen (bedrag + iets anders)
        private static bool HasAtLeastTwoNumbers(string line)
        {
            var matches = Regex.Matches(line, @"\d[\d\s.,]*\d|\b\d\b");
            return matches.Count >= 2;
        }

        // Skip-filter: NL/FR/EN totaalregels, kolomkoppen en paginanummers
        private static bool IsSkipLine(string line)
        {
            var lower = line.ToLowerInvariant().Trim();
            if (lower.Length < 5) return true;

            if (lower.StartsWith("totaal") || lower.StartsWith("total")
                || lower.StartsWith("subtotaal") || lower.StartsWith("subtotal")
                || lower.StartsWith("grand total") || lower.StartsWith("montant total")
                || lower.StartsWith("bedrag") || lower.StartsWith("te betalen"))
                return true;

            if (lower.StartsWith("btw") || lower.StartsWith("tva")
                || lower.StartsWith("vat ") || lower.StartsWith("tax "))
                return true;

            if (lower is "beschrijving" or "omschrijving" or "description"
                      or "designation" or "libelle" or "artikel" or "item"
                      or "ref" or "reference")
                return true;

            if (lower.Contains("excl") || lower.Contains("incl")
                || lower.Contains("hors tva") || lower.Contains("tvac")
                || lower.Contains("htva"))
                return true;

            if (lower.StartsWith("pagina") || lower.StartsWith("page ")
                || lower.StartsWith("blad "))
                return true;

            if (Regex.IsMatch(lower, @"^\d{1,2}[/\-\.]\d{1,2}[/\-\.]\d{2,4}$"))
                return true;

            return false;
        }

        // Belgische getalnotatie: 1.234,56 / 1 234,56 / 371,46
        private static decimal? ParseBelgian(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();

            if (s.Contains(','))
            {
                var cleaned = Regex.Replace(s, @"[.\s]", "").Replace(",", ".");
                return decimal.TryParse(cleaned,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
            }
            else
            {
                var cleaned = s.Replace(" ", "");
                return decimal.TryParse(cleaned,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
            }
        }

        // Eenheden: negatieve lookahead/lookbehind i.p.v. \b (m² bevat niet-\w teken)
        private static readonly Regex UnitPattern =
            new(@"(?<![a-z0-9])(m²|m2|m³|m3|ml|hl|ltr?|kg|ton|stk?|uur|dag|wk|ls|ft|m)(?![a-z0-9])",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Getal-patroon (Belgisch + internationaal)
        private const string NumPattern =
            @"(\d{1,3}(?:[.\s]\d{3})*(?:,\d{1,5})?|\d+(?:,\d{1,5})?)";

        private static ProposedInvoiceLine TryParseInvoiceLine(string rawLine)
        {
            // Rechts-naar-links: lineTotal → vat% → unitPrice → unit → qty → desc
            var line = rawLine.Trim();

            var endMatch = Regex.Match(line, NumPattern + @"\s*$");
            if (!endMatch.Success) return null;

            var lineTotal = ParseBelgian(endMatch.Groups[1].Value);
            if (!lineTotal.HasValue || lineTotal <= 0) return null;
            var rest = line[..endMatch.Index].TrimEnd();

            // BTW %
            decimal? vatRate = null;
            var vatMatch = Regex.Match(rest, @"(\d+(?:,\d+)?)\s*%\s*$");
            if (vatMatch.Success)
            {
                vatRate = ParseBelgian(vatMatch.Groups[1].Value);
                rest = rest[..vatMatch.Index].TrimEnd();
            }

            // Eenheidsprijs (enkel als BTW aanwezig)
            decimal? unitPrice = null;
            if (vatRate.HasValue)
            {
                var priceMatch = Regex.Match(rest, NumPattern + @"\s*$");
                if (priceMatch.Success)
                {
                    unitPrice = ParseBelgian(priceMatch.Groups[1].Value);
                    rest = rest[..priceMatch.Index].TrimEnd();
                }
            }

            // Eenheid (dicht bij het einde)
            string unit = null;
            var unitMatch = UnitPattern.Match(rest);
            if (unitMatch.Success && unitMatch.Index >= rest.Length - 15)
            {
                unit = unitMatch.Value.Trim();
                rest = rest[..unitMatch.Index].TrimEnd();
            }

            // Hoeveelheid
            decimal? quantity = null;
            var qtyMatch = Regex.Match(rest, NumPattern + @"\s*$");
            if (qtyMatch.Success)
            {
                var qtyVal = ParseBelgian(qtyMatch.Groups[1].Value);
                if (qtyVal is > 0 and < 1_000_000)
                {
                    quantity = qtyVal;
                    rest = rest[..qtyMatch.Index].TrimEnd();
                }
            }

            var desc = rest.TrimEnd(':', ' ', '-').Trim();
            if (string.IsNullOrWhiteSpace(desc) || desc.Length < 4) return null;

            // Minimale structuureis: bij korte beschrijving moet er extra veldstructuur zijn
            // om vals-positieven (adressen, codes) te vermijden.
            bool hasExtraStructure = vatRate.HasValue
                || (quantity.HasValue && unitPrice.HasValue)
                || (quantity.HasValue && unit != null);

            if (!hasExtraStructure && desc.Length < 8)
                return null;

            if (unitPrice == null && quantity is > 0)
                unitPrice = Math.Round(lineTotal.Value / quantity.Value, 4);

            return new ProposedInvoiceLine
            {
                Description = desc,
                Quantity    = quantity,
                Unit        = unit,
                UnitPrice   = unitPrice,
                VatRate     = vatRate,
                LineTotal   = lineTotal,
                Source      = "PdfText",
                Confidence  = "Medium"
            };
        }

        // ─── DisplayLines ─────────────────────────────────────────────────────

        /// <summary>
        /// Bouwt een uniforme DisplayLines-lijst:
        /// - als UBL-lijnen bruikbaar zijn → gebruik die (Source="UBL")
        /// - anders als er PDF-lijnen zijn   → gebruik die (Source="PDF")
        /// </summary>
        private static (IReadOnlyList<DisplayInvoiceLine> lines, bool fromPdf) BuildDisplayLines(
            IList<(string Description, decimal? Price)> ublLines,
            bool hasWeakUbl,
            IList<ProposedInvoiceLine> proposedPdf)
        {
            if (!hasWeakUbl && ublLines.Any())
            {
                var display = ublLines
                    .Select(l => new DisplayInvoiceLine
                    {
                        Description = l.Description,
                        LineTotal   = l.Price,
                        Source      = "UBL"
                    })
                    .ToList();
                return (display, false);
            }

            if (proposedPdf != null && proposedPdf.Any())
            {
                var display = proposedPdf
                    .Select(l => new DisplayInvoiceLine
                    {
                        Description = l.Description,
                        Quantity    = l.Quantity,
                        Unit        = l.Unit,
                        UnitPrice   = l.UnitPrice,
                        VatRate     = l.VatRate,
                        LineTotal   = l.LineTotal,
                        Source      = "PDF"
                    })
                    .ToList();
                return (display, true);
            }

            return (Array.Empty<DisplayInvoiceLine>(), false);
        }
    }
}
