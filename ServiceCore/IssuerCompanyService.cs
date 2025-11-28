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
            var defaultAccounts = _db.IssuerBankAccount
                .AsNoTracking()
                .Where(a => a.IsDefault);

            return await _db.IssuerCompany
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .GroupJoin(
                    defaultAccounts,
                    company => company.Id,
                    account => account.IssuerCompanyId,
                    (company, accounts) => new { company, accounts })
                .SelectMany(x => x.accounts.DefaultIfEmpty(), (x, account) => new { x.company, account })
                .Select(x => new IssuerCompanyBO
                {
                    Id = x.company.Id,
                    Name = x.company.Name,
                    LegalName = x.company.LegalName,
                    VatNumber = x.company.VatNumber,
                    EnterpriseNumber = x.company.EnterpriseNumber,
                    AddressLine1 = x.company.AddressLine1,
                    AddressLine2 = x.company.AddressLine2,
                    PostalCode = x.company.PostalCode,
                    City = x.company.City,
                    CountryCode = x.company.CountryCode,
                    Email = x.company.Email,
                    Phone = x.company.Phone,
                    Phone2 = x.company.Phone2,
                    LogoPath = x.company.LogoPath,
                    Website = x.company.Website,
                    DefaultPaymentTermId = x.company.DefaultPaymentTermId,
                    IsActive = x.company.IsActive,
                    CompanyLegalFormId = x.company.CompanyLegalFormId,
                    CompanyLegalFormName = x.company.CompanyLegalForm != null ? x.company.CompanyLegalForm.Name : null,
                    CompanyLegalFormAbbreviation = x.company.CompanyLegalForm != null ? x.company.CompanyLegalForm.Abbreviation : null,
                    DefaultBankAccountIban = x.account != null ? x.account.Iban : null,
                    DefaultBankAccountBic = x.account != null ? x.account.Bic : null,
                    OctopusUsername = x.company.OctopusUsername,
                    OctopusPassword = x.company.OctopusPassword,
                    OctopusAuthenticateToken = x.company.OctopusAuthenticateToken,
                    OctopusAuthenticateTokenValidUntil = x.company.OctopusAuthenticateTokenValidUntil,
                    OctopusDossierToken = x.company.OctopusDossierToken,
                    OctopusDossierTokenValidUntil = x.company.OctopusDossierTokenValidUntil,
                    OctopusDossierNumber = x.company.OctopusDossierNumber
                })
                .ToListAsync(ct);
        }

        public async Task<IssuerCompanyBO> GetAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.IssuerCompany
                .AsNoTracking()
                .Include(x => x.CompanyLegalForm)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null)
                return null;

            var defaultAccount = await _db.IssuerBankAccount
                .AsNoTracking()
                .Where(a => a.IssuerCompanyId == id && a.IsDefault)
                .FirstOrDefaultAsync(ct);

            return MapToBO(entity, defaultAccount);
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
        public async Task<IReadOnlyList<IssuerListItemBO>> ListActiveIssuersAsync(CancellationToken ct = default)
        {
            return await _db.IssuerCompany
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .Select(i => new IssuerListItemBO
                {
                    Id = i.Id,
                    Name = i.Name,
                    DefaultPaymentTermId = i.DefaultPaymentTermId,
                    DefaultVatTypeId = i.DefaultVatTypeId
                })
                .ToListAsync(ct);
        }

        public async Task<int?> GetFirstActiveIssuerIdAsync(CancellationToken ct = default)
        {
            return await _db.IssuerCompany
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<PaymentTermBO>> ListPaymentTermsAsync(CancellationToken ct = default)
        {
            return await _db.PaymentTerms
                .AsNoTracking()
                .OrderBy(t => t.Days)
                .Select(t => new PaymentTermBO
                {
                    Id = t.Id,
                    Name = t.Name,
                    Days = t.Days
                })
                .ToListAsync(ct);
        }
        public async Task<IReadOnlyList<VatTypeBO>> ListVatTypeAsync(CancellationToken ct = default)
        {
            return await _db.Vattype
                .AsNoTracking()
                .OrderBy(t => t.Vatpercentage)
                .Select(t => new VatTypeBO
                {
                    Id = t.Id,
                    VATPercentage = t.Vatpercentage,
                    VATText = t.Vattext
                })
                .ToListAsync(ct);
        }
        public async Task<IReadOnlyList<CompanyLegalFormBO>> ListLegalFormsAsync(CancellationToken ct = default)
        {
            return await _db.CompanyLegalForm
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .Select(f => new CompanyLegalFormBO
                {
                    Id = f.Id,
                    Name = f.Name,
                    Abbreviation = f.Abbreviation,
                    IsActive = f.IsActive
                })
                .ToListAsync(ct);
        }

        // --------- translators ----------
        private static IssuerCompanyBO MapToBO(IssuerCompany x, IssuerBankAccount? defaultAccount = null)
        {
            var account = defaultAccount ?? x.IssuerBankAccount;

            return new IssuerCompanyBO
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
                Phone2 = x.Phone2,
                LogoPath = x.LogoPath,
                Website = x.Website,
                TemplateKey = x.TemplateKey,
                TemplateJson = x.TemplateJson,
                BrandPrimaryColor = x.BrandPrimaryColor,
                BrandSecondaryColor = x.BrandSecondaryColor,
                FontFamily = x.FontFamily,
                LogoBytes = x.LogoBytes,
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
                FooterLegalText = x.FooterLegalText,
                PeppolEnabled = x.PeppolEnabled,
                CompanyLegalFormId = x.CompanyLegalFormId,
                CompanyLegalFormName = x.CompanyLegalForm?.Name,
                CompanyLegalFormAbbreviation = x.CompanyLegalForm?.Abbreviation,
                DefaultBankAccountIban = account?.Iban,
                DefaultBankAccountBic = account?.Bic,
                OctopusUsername = x.OctopusUsername,
                OctopusPassword = x.OctopusPassword,
                OctopusAuthenticateToken = x.OctopusAuthenticateToken,
                OctopusAuthenticateTokenValidUntil = x.OctopusAuthenticateTokenValidUntil,
                OctopusDossierToken = x.OctopusDossierToken,
                OctopusDossierTokenValidUntil = x.OctopusDossierTokenValidUntil,
                OctopusDossierNumber = x.OctopusDossierNumber
            };
        }

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
            e.Phone2 = bo.Phone2?.Trim();
            e.LogoPath = bo.LogoPath?.Trim();
            e.Website = bo.Website?.Trim();
            e.TemplateKey = bo.TemplateKey?.Trim();
            e.TemplateJson = bo.TemplateJson;
            e.BrandPrimaryColor = bo.BrandPrimaryColor;
            e.BrandSecondaryColor = bo.BrandSecondaryColor;
            e.FontFamily = bo.FontFamily;
            e.LogoBytes = bo.LogoBytes;
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
            e.FooterLegalText = bo.FooterLegalText;
            e.PeppolEnabled = bo.PeppolEnabled;
            e.CompanyLegalFormId = bo.CompanyLegalFormId;
            e.OctopusUsername = bo.OctopusUsername;
            e.OctopusPassword = bo.OctopusPassword;
            e.OctopusAuthenticateToken = bo.OctopusAuthenticateToken;
            e.OctopusAuthenticateTokenValidUntil = bo.OctopusAuthenticateTokenValidUntil;
            e.OctopusDossierToken = bo.OctopusDossierToken;
            e.OctopusDossierTokenValidUntil = bo.OctopusDossierTokenValidUntil;
            e.OctopusDossierNumber = bo.OctopusDossierNumber;
            return e;
        }
    }
}
