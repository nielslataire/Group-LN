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
using System.Text.Json;

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
                    InvoiceSendEmail = x.company.InvoiceSendEmail,
                    Phone = x.company.Phone,
                    Phone2 = x.company.Phone2,
                    LogoPath = x.company.LogoPath,
                    Website = x.company.Website,
                    DefaultPaymentTermId = x.company.DefaultPaymentTermId,
                    DefaultVatTypeId = x.company.DefaultVatTypeId,
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
                    OctopusDossierNumber = x.company.OctopusDossierNumber,
                    OctopusPurchaseJournalKeyPrefix = x.company.OctopusPurchaseJournalKeyPrefix,
                    OctopusCustomFieldsJson = x.company.OctopusCustomFieldsJson,
                    OctopusCustomFieldMappingsJson = x.company.OctopusCustomFieldMappingsJson,
                    OctopusDownloadLinkCustomFieldKeyId = x.company.OctopusDownloadLinkCustomFieldKeyId
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

        public async Task<IReadOnlyList<PaymentTermOptionBO>> GetPaymentTermOptionsAsync(int? issuerId = null, CancellationToken ct = default)
        {
            var terms = _uow.PaymentTerms.GetNoTracking();
            if (issuerId.HasValue)
                terms = terms.Where(x => x.IssuerCompanyId == null || x.IssuerCompanyId == issuerId);
            else
                terms = terms.Where(x => x.IssuerCompanyId == null);

            return await terms
                .OrderBy(x => x.Name)
                .Select(x => new PaymentTermOptionBO { Id = x.Id, Label = x.Name })
                .ToListAsync(ct);
        }
        public async Task SyncOctopusCustomFieldsAsync(int issuerId, IEnumerable<OctopusCustomFieldBO> customFields, CancellationToken ct = default)
        {
            if (issuerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(issuerId));
            }

            var issuer = await _db.IssuerCompany.FirstOrDefaultAsync(i => i.Id == issuerId, ct)
                ?? throw new InvalidOperationException($"Issuer {issuerId} niet gevonden.");

            var list = (customFields ?? Array.Empty<OctopusCustomFieldBO>())
                .Where(f => f != null)
                .Select(f => new OctopusCustomFieldBO
                {
                    CustomFieldKeyId = f.CustomFieldKeyId,
                    TemplateCode = f.TemplateCode,
                    DescriptionNl = f.DescriptionNl,
                    DescriptionFr = f.DescriptionFr,
                    DescriptionEn = f.DescriptionEn,
                    DescriptionDe = f.DescriptionDe,
                    CustomFieldType = f.CustomFieldType
                })
                .ToList();

            issuer.OctopusCustomFieldsJson = JsonSerializer.Serialize(list);

            var validKeys = new HashSet<int>(list.Select(f => f.CustomFieldKeyId));
            var mappings = DeserializeMappings(issuer.OctopusCustomFieldMappingsJson)
                .Where(m => validKeys.Contains(m.CustomFieldKeyId))
                .ToList();
            issuer.OctopusCustomFieldMappingsJson = JsonSerializer.Serialize(mappings);

            if (!issuer.OctopusDownloadLinkCustomFieldKeyId.HasValue || !validKeys.Contains(issuer.OctopusDownloadLinkCustomFieldKeyId.Value))
            {
                issuer.OctopusDownloadLinkCustomFieldKeyId = null;
            }

            await _uow.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<OctopusCustomFieldBO>> ListOctopusCustomFieldsAsync(int issuerId, CancellationToken ct = default)
        {
            if (issuerId <= 0)
            {
                return Array.Empty<OctopusCustomFieldBO>();
            }

            var issuer = await _db.IssuerCompany
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == issuerId, ct);

            if (issuer == null || string.IsNullOrWhiteSpace(issuer.OctopusCustomFieldsJson))
            {
                return Array.Empty<OctopusCustomFieldBO>();
            }

            return JsonSerializer.Deserialize<List<OctopusCustomFieldBO>>(issuer.OctopusCustomFieldsJson)
                   ?? new List<OctopusCustomFieldBO>();
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

        public async Task<IReadOnlyList<PaymentTermBO>> ListPaymentTermsAsync(int issuerId, CancellationToken ct = default)
        {
            return await _db.PaymentTerms
                .AsNoTracking()
                .Where(t => t.IssuerCompanyId == null || t.IssuerCompanyId == issuerId)
                .OrderBy(t => t.Days)
                .Select(t => new PaymentTermBO
                {
                    Id = t.Id,
                    Name = t.Name,
                    Days = t.Days,
                    TermType = (PaymentTermType)t.TermType,
                    DisplayMode = (PaymentTermDisplayMode)t.DisplayMode,
                    DisplayText = t.DisplayText,
                    Description = t.Description,
                    IssuerCompanyId = t.IssuerCompanyId
                })
                .ToListAsync(ct);
        }
        public async Task SyncPaymentTermsAsync(int issuerId, IEnumerable<PaymentTermBO> terms, CancellationToken ct = default)
        {
            if (issuerId <= 0)
                throw new ArgumentOutOfRangeException(nameof(issuerId));

            var existing = await _db.PaymentTerms
                .Where(t => t.IssuerCompanyId == issuerId)
                .ToListAsync(ct);

            var incoming = (terms ?? Array.Empty<PaymentTermBO>())
                .Where(t => t != null)
                .ToList();

            foreach (var term in incoming)
            {
                if (term.Id > 0)
                {
                    var match = existing.FirstOrDefault(t => t.Id == term.Id);
                    if (match == null)
                        continue;

                    if (term.IsDeleted)
                    {
                        _db.PaymentTerms.Remove(match);
                        continue;
                    }

                    match.Name = term.Name?.Trim();
                    match.Days = term.Days;
                    match.Description = term.Description?.Trim();
                    match.TermType = (int)term.TermType;
                    match.DisplayMode = (int)term.DisplayMode;
                    match.DisplayText = term.DisplayText?.Trim();
                }
                else
                {
                    if (term.IsDeleted)
                        continue;

                    _db.PaymentTerms.Add(new PaymentTerms
                    {
                        IssuerCompanyId = issuerId,
                        Name = term.Name?.Trim(),
                        Days = term.Days,
                        Description = term.Description?.Trim(),
                        TermType = (int)term.TermType,
                        DisplayMode = (int)term.DisplayMode,
                        DisplayText = term.DisplayText?.Trim()
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
        }
        public async Task<IReadOnlyList<VatTypeBO>> ListVatTypeAsync(int issuerId, CancellationToken ct = default)
        {
            if (issuerId <= 0)
            {
                return Array.Empty<VatTypeBO>();
            }

            return await _db.Vattype
                .AsNoTracking()
                .Where(t => t.IssuerCompanyId == issuerId)
                .OrderBy(t => t.BasePercentage)
                .ThenBy(t => t.Code)
                .Select(t => new VatTypeBO
                {
                    Id = t.Id,
                    IssuerCompanyId = t.IssuerCompanyId,
                    Code = t.Code,
                    Description = t.Description,
                    Type = t.Type,
                    BasePercentage = t.BasePercentage,
                    DefaultSellBookingAccountNr = t.DefaultSellBookingAccountNr,
                    InvoiceMention = t.InvoiceMention
                })
                .ToListAsync(ct);
        }

        public async Task SyncVatTypesAsync(int issuerId, IEnumerable<VatTypeBO> vatTypes, CancellationToken ct = default)
        {
            if (issuerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(issuerId));
            }

            var incoming = vatTypes?.Where(v => !string.IsNullOrWhiteSpace(v.Code)).ToList()
                           ?? new List<VatTypeBO>();

            var existing = await _db.Vattype
                .Where(v => v.IssuerCompanyId == issuerId)
                .ToListAsync(ct);

            var issuer = await _db.IssuerCompany.FirstOrDefaultAsync(i => i.Id == issuerId, ct);

            var existingByCode = existing
                .GroupBy(v => v.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var code in incoming)
            {
                if (existingByCode.TryGetValue(code.Code, out var entity))
                {
                    entity.Description = code.Description;
                    entity.Type = code.Type;
                    entity.BasePercentage = code.BasePercentage;
                    entity.DefaultSellBookingAccountNr = code.DefaultSellBookingAccountNr;
                    if (code.InvoiceMention != null)
                    {
                        entity.InvoiceMention = code.InvoiceMention;
                    }
                }
                else
                {
                    _uow.VatTypes.Add(new Vattype
                    {
                        IssuerCompanyId = issuerId,
                        Code = code.Code,
                        Description = code.Description,
                        Type = code.Type,
                        BasePercentage = code.BasePercentage,
                        DefaultSellBookingAccountNr = code.DefaultSellBookingAccountNr,
                        InvoiceMention = code.InvoiceMention
                    });
                }
            }

            var incomingCodes = new HashSet<string>(incoming.Select(v => v.Code), StringComparer.OrdinalIgnoreCase);
            foreach (var entity in existing)
            {
                if (!incomingCodes.Contains(entity.Code))
                {
                    if (issuer != null && issuer.DefaultVatTypeId == entity.Id)
                    {
                        issuer.DefaultVatTypeId = null;
                    }
                    _uow.VatTypes.Remove(entity);
                }
            }

            await _uow.SaveChangesAsync(ct);
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
                InvoiceSendEmail = x.InvoiceSendEmail,
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
                DefaultVatTypeId = x.DefaultVatTypeId,
                IsActive = x.IsActive,
                EInvoiceEnabled = x.EinvoiceEnabled,
                PeppolParticipantId = x.PeppolParticipantId,
                UblAttachPdf = x.UblAttachPdf,
                EmailSubjectTemplate = x.EmailSubjectTemplate,
                EmailBodyTemplate = x.EmailBodyTemplate,
                EmailPaidBodyTemplate = x.EmailPaidBodyTemplate,
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
                OctopusDossierNumber = x.OctopusDossierNumber,
                OctopusPurchaseJournalKeyPrefix = x.OctopusPurchaseJournalKeyPrefix,
                OctopusCustomFieldsJson = x.OctopusCustomFieldsJson,
                OctopusCustomFieldMappingsJson = x.OctopusCustomFieldMappingsJson,
                OctopusDownloadLinkCustomFieldKeyId = x.OctopusDownloadLinkCustomFieldKeyId,
                RatePerKm = x.RatePerKm
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
            e.InvoiceSendEmail = bo.InvoiceSendEmail?.Trim();
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
            e.DefaultVatTypeId = bo.DefaultVatTypeId;
            e.IsActive = bo.IsActive;
            e.EinvoiceEnabled = bo.EInvoiceEnabled;
            e.PeppolParticipantId = bo.PeppolParticipantId?.Trim();
            e.UblAttachPdf = bo.UblAttachPdf;
            e.EmailSubjectTemplate = bo.EmailSubjectTemplate;
            e.EmailBodyTemplate = bo.EmailBodyTemplate;
            e.EmailPaidBodyTemplate = bo.EmailPaidBodyTemplate;
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
            e.OctopusPurchaseJournalKeyPrefix = bo.OctopusPurchaseJournalKeyPrefix;
            e.OctopusCustomFieldsJson = bo.OctopusCustomFieldsJson;
            e.OctopusCustomFieldMappingsJson = bo.OctopusCustomFieldMappingsJson;
            e.OctopusDownloadLinkCustomFieldKeyId = bo.OctopusDownloadLinkCustomFieldKeyId;
            return e;
        }
        private static List<OctopusCustomFieldMappingBO> DeserializeMappings(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<OctopusCustomFieldMappingBO>();
            }

            return JsonSerializer.Deserialize<List<OctopusCustomFieldMappingBO>>(json)
                   ?? new List<OctopusCustomFieldMappingBO>();
        }

        // ── Standaard uurtarieven per facturatiebedrijf ───────────────────────

        public async Task<IReadOnlyList<IssuerCompanyUserRateBO>> GetUserRatesAsync(int issuerId, CancellationToken ct = default)
        {
            var rates = await _db.IssuerCompanyUserRate
                .AsNoTracking()
                .Where(r => r.IssuerCompanyId == issuerId)
                .Include(r => r.User)
                .ToListAsync(ct);

            return rates.Select(r => new IssuerCompanyUserRateBO
            {
                Id = r.Id,
                IssuerCompanyId = r.IssuerCompanyId,
                UserId = r.UserId,
                UserFullName = r.User != null ? $"{r.User.Voornaam} {r.User.Familienaam}".Trim() : r.UserId,
                HourlyRate = r.HourlyRate
            }).ToList();
        }

        public async Task SyncUserRatesAsync(int issuerId, IEnumerable<IssuerCompanyUserRateBO> rates, CancellationToken ct = default)
        {
            var existing = await _db.IssuerCompanyUserRate
                .Where(r => r.IssuerCompanyId == issuerId)
                .ToListAsync(ct);

            _db.IssuerCompanyUserRate.RemoveRange(existing);

            foreach (var bo in rates.Where(r => !string.IsNullOrWhiteSpace(r.UserId)))
            {
                _db.IssuerCompanyUserRate.Add(new IssuerCompanyUserRate
                {
                    IssuerCompanyId = issuerId,
                    UserId = bo.UserId,
                    HourlyRate = bo.HourlyRate
                });
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateRatePerKmAsync(int issuerId, decimal? ratePerKm, CancellationToken ct = default)
        {
            var company = await _db.IssuerCompany.FirstOrDefaultAsync(c => c.Id == issuerId, ct);
            if (company == null) return;

            company.RatePerKm = ratePerKm;
            await _db.SaveChangesAsync(ct);
        }
    }
}
