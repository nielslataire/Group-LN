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
                    HasAttachments = i.IncomingInvoiceAttachments.Any(),
                    CreatedAt = i.CreatedAt ?? DateTime.UtcNow,
                    SyncedAt = i.SyncedAt
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

            return MapToDetail(entity);
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
