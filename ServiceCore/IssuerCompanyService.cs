using BOCore;
using FacadeCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using ServiceCore.Translators;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ServiceCore
{
    public class IssuerCompanyService : IIssuerCompanyService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public IssuerCompanyService(UnitOfWorkCore uow)
        {
            _uow = uow;
            _db = (cpmRunningContext)_uow.Context;
        }

        public async Task<IReadOnlyList<IssuerCompanyBO>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.IssuerCompany
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .Select(x => new IssuerCompanyBO
                {
                    Id = x.Id,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    VatNumber = x.VatNumber,
                    EnterpriseNumber = x.EnterpriseNumber,
                    AddressLine1 = x.AddressLine1,
                    AddressLine2 = x.AddressLine2,
                    PostalCode = x.PostalCode,
                    City = x.City,
                    CountryCode = x.CountryCode,
                    Email = x.Email,
                    Phone = x.Phone,
                    LogoPath = x.LogoPath,
                    DefaultPaymentTermId = x.DefaultPaymentTermId,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);
        }

        public async Task<IssuerCompanyBO> GetAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.IssuerCompany.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return e == null ? null : MapToBO(e);
        }

        public async Task<int> CreateAsync(IssuerCompanyBO bo, CancellationToken ct = default)
        {
            var e = MapToEntity(bo, new IssuerCompany());
            _uow.IssuerCompanies.Add(e);
            await _uow.SaveChangesAsync(ct);
            return e.Id;
        }

        public async Task UpdateAsync(IssuerCompanyBO bo, CancellationToken ct = default)
        {
            var e = await _db.IssuerCompany.FirstAsync(x => x.Id == bo.Id, ct);
            MapToEntity(bo, e);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task DisableAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.IssuerCompany.FirstAsync(x => x.Id == id, ct);
            e.IsActive = false;
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<PaymentTermOptionBO>> GetPaymentTermOptionsAsync(CancellationToken ct = default)
        {
            return await _uow.PaymentTerms.GetNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new PaymentTermOptionBO { Id = x.Id, Label = x.Name })
                .ToListAsync(ct);
        }

        // --------- translators ----------
        private static IssuerCompanyBO MapToBO(IssuerCompany x) => new IssuerCompanyBO
        {
            Id = x.Id,
            Name = x.Name,
            LegalName = x.LegalName,
            VatNumber = x.VatNumber,
            EnterpriseNumber = x.EnterpriseNumber,
            AddressLine1 = x.AddressLine1,
            AddressLine2 = x.AddressLine2,
            PostalCode = x.PostalCode,
            City = x.City,
            CountryCode = x.CountryCode,
            Email = x.Email,
            Phone = x.Phone,
            LogoPath = x.LogoPath,
            DefaultPaymentTermId = x.DefaultPaymentTermId,
            IsActive = x.IsActive,
            EInvoiceEnabled = x.EinvoiceEnabled,
            PeppolParticipantId = x.PeppolParticipantId,
            UblAttachPdf = x.UblAttachPdf,
            EmailSubjectTemplate = x.EmailSubjectTemplate,
            EmailBodyTemplate = x.EmailBodyTemplate,
            InvoiceFooterHtml = x.InvoiceFooterHtml,
            DefaultLanguage = x.DefaultLanguage,
            DefaultCurrency = x.DefaultCurrency,
            InvoiceNumberPattern = x.InvoiceNumberPattern,
            EpcQrEnabled = x.EpcQrEnabled,
            EpcBeneficiaryName = x.EpcBeneficiaryName,
            EpcIban = x.EpcIban,
            EpcBic = x.EpcBic,
            EpcRemittanceType = x.EpcRemittanceType,
            EpcRemittanceTemplate = x.EpcRemittanceTemplate,
        };

        private static IssuerCompany MapToEntity(IssuerCompanyBO bo, IssuerCompany e)
        {
            e.Name = bo.Name?.Trim();
            e.LegalName = bo.LegalName?.Trim();
            e.VatNumber = bo.VatNumber?.Trim();
            e.EnterpriseNumber = bo.EnterpriseNumber?.Trim();
            e.AddressLine1 = bo.AddressLine1?.Trim();
            e.AddressLine2 = bo.AddressLine2?.Trim();
            e.PostalCode = bo.PostalCode?.Trim();
            e.City = bo.City?.Trim();
            e.CountryCode = bo.CountryCode?.Trim();
            e.Email = bo.Email?.Trim();
            e.Phone = bo.Phone?.Trim();
            e.LogoPath = bo.LogoPath?.Trim();
            e.DefaultPaymentTermId = bo.DefaultPaymentTermId;
            e.IsActive = bo.IsActive;
            e.EinvoiceEnabled = bo.EInvoiceEnabled;
            e.PeppolParticipantId = bo.PeppolParticipantId?.Trim();
            e.UblAttachPdf = bo.UblAttachPdf;
            e.EmailSubjectTemplate = bo.EmailSubjectTemplate;
            e.EmailBodyTemplate = bo.EmailBodyTemplate;
            e.InvoiceFooterHtml = bo.InvoiceFooterHtml;
            e.DefaultLanguage = bo.DefaultLanguage;
            e.DefaultCurrency = bo.DefaultCurrency;
            e.InvoiceNumberPattern = bo.InvoiceNumberPattern;
            e.EpcQrEnabled = bo.EpcQrEnabled;
            e.EpcBeneficiaryName = bo.EpcBeneficiaryName;
            e.EpcIban = bo.EpcIban?.Replace(" ", "").ToUpperInvariant();
            e.EpcBic = bo.EpcBic?.ToUpperInvariant();
            e.EpcRemittanceType = bo.EpcRemittanceType;
            e.EpcRemittanceTemplate = bo.EpcRemittanceTemplate;
            return e;
        }
    }
}
