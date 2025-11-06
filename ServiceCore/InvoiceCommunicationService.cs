using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOCore;
using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class InvoiceCommunicationService : IInvoiceCommunicationService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public InvoiceCommunicationService(UnitOfWorkCore uow)
        {
            _uow = uow;
            _db = (cpmRunningContext)uow.Context;
        }

        public async Task<IReadOnlyList<InvoiceEmailLogBO>> GetEmailLogsAsync(int invoiceId, CancellationToken ct = default)
        {
            return await _db.InvoiceEmailLog
                .AsNoTracking()
                .Where(log => log.InvoiceId == invoiceId)
                .OrderByDescending(log => log.SentAt)
                .Select(log => new InvoiceEmailLogBO
                {
                    Id = log.Id,
                    InvoiceId = log.InvoiceId,
                    ToAddress = log.ToAddress,
                    CcAddress = log.CcAddress,
                    Subject = log.Subject,
                    ProviderId = log.ProviderId,
                    SentAt = log.SentAt,
                    Status = log.Status
                })
                .ToListAsync(ct);
        }

        public async Task SaveEmailLogAsync(InvoiceEmailLogBO log, CancellationToken ct = default)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            var entity = new InvoiceEmailLog
            {
                InvoiceId = log.InvoiceId,
                ToAddress = log.ToAddress,
                CcAddress = log.CcAddress,
                Subject = log.Subject,
                ProviderId = log.ProviderId,
                SentAt = log.SentAt,
                Status = log.Status
            };

            _uow.InvoiceEmailLogs.Add(entity);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<InvoiceUblBO?> GetInvoiceUblAsync(int invoiceId, CancellationToken ct = default)
        {
            var entity = await _db.InvoiceUbl
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.InvoiceId == invoiceId, ct);

            if (entity == null)
                return null;

            return new InvoiceUblBO
            {
                InvoiceId = entity.InvoiceId,
                UblVersion = entity.UblVersion,
                Profile = entity.Profile,
                XmlContent = entity.XmlContent,
                GeneratedAt = entity.GeneratedAt,
                SentViaPeppol = entity.SentViaPeppol,
                PeppolDocId = entity.PeppolDocId
            };
        }

        public async Task SaveInvoiceUblAsync(InvoiceUblBO ubl, CancellationToken ct = default)
        {
            if (ubl == null) throw new ArgumentNullException(nameof(ubl));

            var entity = await _db.InvoiceUbl
                .FirstOrDefaultAsync(x => x.InvoiceId == ubl.InvoiceId, ct);

            if (entity == null)
            {
                entity = new InvoiceUbl
                {
                    InvoiceId = ubl.InvoiceId
                };
                _uow.InvoiceUbl.Add(entity);
            }

            entity.UblVersion = ubl.UblVersion;
            entity.Profile = ubl.Profile;
            entity.XmlContent = ubl.XmlContent;
            entity.GeneratedAt = ubl.GeneratedAt;
            entity.SentViaPeppol = ubl.SentViaPeppol;
            entity.PeppolDocId = ubl.PeppolDocId;

            await _uow.SaveChangesAsync(ct);
        }
    }
}