using BOCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore.IncomingInvoices
{
    public class IncomingInvoiceService : IIncomingInvoiceService
    {
        private readonly cpmRunningContext _db;
        private readonly ILogger<IncomingInvoiceService> _logger;
        private readonly IOctopusIncomingInvoiceSyncService _syncService;

        public IncomingInvoiceService(
            cpmRunningContext db,
            ILogger<IncomingInvoiceService> logger,
            IOctopusIncomingInvoiceSyncService syncService)
        {
            _db = db;
            _logger = logger;
            _syncService = syncService;
        }

        public async Task<IncomingInvoicePagedResultVm> GetPagedAsync(IncomingInvoiceFilterVm filter, CancellationToken ct = default)
        {
            // IncommingInvoices gebruikt: Date=factuurdatum, Price=totaal, ExternalId=factuurnummer
            var query = _db.IncommingInvoices
                .Include(i => i.IssuerCompany)
                .Include(i => i.Project)
                .Where(i => i.IssuerCompanyId != null) // enkel documentencentrum-records
                .AsNoTracking()
                .AsQueryable();

            if (filter.IssuerCompanyId.HasValue)
                query = query.Where(i => i.IssuerCompanyId == filter.IssuerCompanyId.Value);

            if (!string.IsNullOrWhiteSpace(filter.SupplierName))
                query = query.Where(i => i.SupplierName.Contains(filter.SupplierName));

            if (!string.IsNullOrWhiteSpace(filter.InvoiceNumber))
                query = query.Where(i => i.ExternalId.Contains(filter.InvoiceNumber));

            if (filter.StatusId.HasValue)
                query = query.Where(i => i.StatusId == filter.StatusId.Value);

            if (!string.IsNullOrWhiteSpace(filter.DocumentType))
                query = query.Where(i => i.DocumentType == filter.DocumentType);

            if (filter.DateFrom.HasValue)
                query = query.Where(i => i.Date >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(i => i.Date <= filter.DateTo.Value);

            if (filter.ProjectId.HasValue)
                query = query.Where(i => i.ProjectId == filter.ProjectId.Value);

            if (filter.HasWarnings == true)
                query = query.Where(i => i.Warnings.Any(w => !w.IsResolved));

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(i => new IncomingInvoiceListItemVm
                {
                    Id = i.Id,
                    IssuerCompanyId = i.IssuerCompanyId ?? 0,
                    IssuerCompanyName = i.IssuerCompany != null ? i.IssuerCompany.Name : string.Empty,
                    SupplierName = i.SupplierName,
                    SupplierVatNumber = i.SupplierVatNumber,
                    InvoiceNumber = i.ExternalId,
                    InvoiceDate = i.Date,
                    DueDate = i.DueDate,
                    TotalAmountInclVat = i.Price,
                    CurrencyCode = i.CurrencyCode,
                    StatusId = i.StatusId,
                    StatusLabel = IncomingInvoiceStatus.Label(i.StatusId),
                    StatusBadgeClass = IncomingInvoiceStatus.BadgeClass(i.StatusId),
                    DocumentType = i.DocumentType,
                    Source = i.Source,
                    ProjectName = i.Project != null ? i.Project.ProjectName : null,
                    SuggestedProjectName = i.Enrichment != null && i.ProjectId == null
                        ? i.Enrichment.SuggestedProjectName
                        : null,
                    IsEnriched = i.Enrichment != null,
                    AssignedToName = i.Enrichment != null ? i.Enrichment.UserReviewedBy : null,
                    HasAttachments = i.IncomingInvoiceAttachments.Any(a => a.AttachmentType == "PDF"),
                    FirstPdfAttachmentId = i.IncomingInvoiceAttachments
                        .Where(a => a.AttachmentType == "PDF")
                        .Select(a => (int?)a.Id)
                        .FirstOrDefault(),
                    CreatedAt = i.CreatedAt ?? DateTime.UtcNow,
                    SyncedAt = i.SyncedAt,
                    ReasonFlags = i.Enrichment != null ? i.Enrichment.ReasonFlags : 0,
                    HasWarnings = i.Warnings.Any(w => !w.IsResolved)
                })
                .ToListAsync(ct);

            return new IncomingInvoicePagedResultVm
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<IncomingInvoiceDetailVm> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.IncommingInvoices
                .Include(i => i.IssuerCompany)
                .Include(i => i.Project)
                .Include(i => i.IncomingInvoiceAttachments)
                .Include(i => i.IncommingInvoiceDetail)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (entity == null)
                return null;

            var vm = MapToDetail(entity);

            // Laad verrijkingsresultaat
            var enrichment = await _db.IncomingInvoiceEnrichment
                .Include(e => e.SuggestedProject)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IncomingInvoiceId == id, ct);

            if (enrichment != null)
            {
                var warnings = await _db.IncomingInvoiceWarning
                    .AsNoTracking()
                    .Where(w => w.IncomingInvoiceId == id && !w.IsResolved)
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

                var ublLines = entity.IncommingInvoiceDetail
                    .Select(d => (d.Description, d.Price))
                    .ToList();

                vm.Enrichment = MapEnrichmentToVm(enrichment, warnings, ublLines);
            }

            return vm;
        }

        public async Task LinkToContractAsync(IncomingInvoiceLinkContractRequest request, CancellationToken ct = default)
        {
            var entity = await _db.IncommingInvoices.FindAsync(new object[] { request.IncomingInvoiceId }, ct)
                ?? throw new InvalidOperationException($"IncomingInvoice {request.IncomingInvoiceId} niet gevonden.");

            entity.ContractId = request.ContractId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        public async Task LinkToProjectAsync(IncomingInvoiceLinkProjectRequest request, CancellationToken ct = default)
        {
            var entity = await _db.IncommingInvoices.FindAsync(new object[] { request.IncomingInvoiceId }, ct)
                ?? throw new InvalidOperationException($"IncomingInvoice {request.IncomingInvoiceId} niet gevonden.");

            entity.ProjectId = request.ProjectId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("IncomingInvoice {Id} gekoppeld aan project {ProjectId}.", request.IncomingInvoiceId, request.ProjectId);
        }

        public async Task UpdateStatusAsync(IncomingInvoiceUpdateStatusRequest request, CancellationToken ct = default)
        {
            var entity = await _db.IncommingInvoices.FindAsync(new object[] { request.IncomingInvoiceId }, ct)
                ?? throw new InvalidOperationException($"IncomingInvoice {request.IncomingInvoiceId} niet gevonden.");

            entity.StatusId = request.StatusId;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                entity.Notes = request.Notes;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("IncomingInvoice {Id} status gewijzigd naar {Status}.",
                request.IncomingInvoiceId, IncomingInvoiceStatus.Label(request.StatusId));
        }

        public async Task<byte[]> GetAttachmentContentAsync(int attachmentId, CancellationToken ct = default)
        {
            var attachment = await _db.IncomingInvoiceAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            return attachment?.Content;
        }

        public async Task<IncomingInvoiceSyncResultVm> SyncFromOctopusAsync(int issuerCompanyId, DateTime? modifiedSinceOverride = null, CancellationToken ct = default)
        {
            try
            {
                return await _syncService.SyncAsync(issuerCompanyId, modifiedSinceOverride, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync vanuit Octopus mislukt voor IssuerCompany {Id}.", issuerCompanyId);
                return new IncomingInvoiceSyncResultVm
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    SyncedAt = DateTime.UtcNow
                };
            }
        }

        private static InvoiceEnrichmentVm MapEnrichmentToVm(
            IncomingInvoiceEnrichment e,
            List<InvoiceWarningVm> warnings,
            List<(string Description, decimal? Price)> ublLines = null)
        {
            List<ProjectMatchResult> altProjects = null;
            if (!string.IsNullOrWhiteSpace(e.AltProjectCandidatesJson))
            {
                try { altProjects = System.Text.Json.JsonSerializer.Deserialize<List<ProjectMatchResult>>(e.AltProjectCandidatesJson); }
                catch { /* ignore */ }
            }

            List<ContractMatchResult> altContracts = null;
            if (!string.IsNullOrWhiteSpace(e.AltContractCandidatesJson))
            {
                try { altContracts = System.Text.Json.JsonSerializer.Deserialize<List<ContractMatchResult>>(e.AltContractCandidatesJson); }
                catch { /* ignore */ }
            }

            // Lees HasWeakUblLines en ProposedPdfLines uit opgeslagen ExtractionResultJson
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
                        proposedPdfLines = System.Text.Json.JsonSerializer
                            .Deserialize<List<ProposedInvoiceLine>>(linesProp.GetRawText());
                }
                catch { /* defensive */ }
            }

            // Bouw DisplayLines: UBL als bruikbaar, anders PDF-voorstel
            var (displayLines, displayFromPdf) = BuildDisplayLines(
                ublLines ?? new List<(string, decimal?)>(),
                hasWeakUblLines,
                proposedPdfLines);

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
                    Confidence = "Medium"
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
                HasWeakUblLines     = hasWeakUblLines,
                ProposedPdfLines    = proposedPdfLines ?? new List<ProposedInvoiceLine>(),
                DisplayLines        = displayLines,
                DisplayLinesFromPdf = displayFromPdf
            };
        }

        private static (IReadOnlyList<DisplayInvoiceLine> lines, bool fromPdf) BuildDisplayLines(
            List<(string Description, decimal? Price)> ublLines,
            bool hasWeakUbl,
            List<ProposedInvoiceLine> proposedPdf)
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

        private static IncomingInvoiceDetailVm MapToDetail(IncommingInvoices entity) => new()
        {
            Id = entity.Id,
            IssuerCompanyId = entity.IssuerCompanyId ?? 0,
            IssuerCompanyName = entity.IssuerCompany?.Name ?? string.Empty,
            SupplierName = entity.SupplierName,
            SupplierVatNumber = entity.SupplierVatNumber,
            OctopusSupplierRelationKey = entity.OctopusSupplierRelationKey,
            InvoiceNumber = entity.ExternalId,
            InvoiceDate = entity.Date,
            DueDate = entity.DueDate,
            TotalAmountInclVat = entity.Price,
            VatAmount = entity.VatAmount,
            NetAmount = entity.NetAmount,
            CurrencyCode = entity.CurrencyCode,
            StatusId = entity.StatusId,
            StatusLabel = IncomingInvoiceStatus.Label(entity.StatusId),
            StatusBadgeClass = IncomingInvoiceStatus.BadgeClass(entity.StatusId),
            DocumentType = entity.DocumentType,
            Source = entity.Source,
            ProjectId = entity.ProjectId,
            ProjectName = entity.Project?.ProjectName,
            ContractId = entity.ContractId,
            Notes = entity.Notes,
            OctopusExternalId = entity.OctopusExternalId,
            SyncedAt = entity.SyncedAt,
            SyncError = entity.SyncError,
            OctopusBookyearId = entity.OctopusBookyearId,
            OctopusJournalKey = entity.OctopusJournalKey,
            OctopusDocumentSequenceNr = entity.OctopusDocumentSequenceNr,
            OctopusBookedAt = entity.OctopusBookedAt,
            OctopusBookedBy = entity.OctopusBookedBy,
            OctopusBookingStatus = entity.OctopusBookingStatus,
            CreatedAt = entity.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = entity.UpdatedAt ?? DateTime.UtcNow,
            Attachments = entity.IncomingInvoiceAttachments.Select(a => new IncomingInvoiceAttachmentVm
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                AttachmentType = a.AttachmentType,
                FileSizeBytes = a.FileSizeBytes,
                CreatedAt = a.CreatedAt
            }).ToList(),
            Lines = entity.IncommingInvoiceDetail
                .OrderBy(l => l.Id)
                .Select(l => new IncomingInvoiceLineVm
                {
                    Id = l.Id,
                    Description = l.Description,
                    Price = l.Price
                }).ToList()
        };
    }
}
