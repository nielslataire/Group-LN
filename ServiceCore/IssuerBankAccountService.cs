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
    public class IssuerBankAccountService : IIssuerBankAccountService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public IssuerBankAccountService(UnitOfWorkCore uow)
        {
            _uow = uow;
            _db = (cpmRunningContext)_uow.Context;
        }

        public async Task<IReadOnlyList<IssuerBankAccountBO>> ListByIssuerAsync(int issuerCompanyId, CancellationToken ct = default)
        {
            return await _db.IssuerBankAccount.AsNoTracking()
                .Where(x => x.IssuerCompanyId == issuerCompanyId)
                .OrderByDescending(x => x.IsDefault).ThenBy(x => x.DisplayName)
                .Select(x => new IssuerBankAccountBO
                {
                    Id = x.Id,
                    IssuerCompanyId = x.IssuerCompanyId,
                    Iban = x.Iban,
                    Bic = x.Bic,
                    DisplayName = x.DisplayName,
                    IsDefault = x.IsDefault,
                    ValidFrom = x.ValidFrom,
                    ValidTo = x.ValidTo
                })
                .ToListAsync(ct);
        }

        public async Task<IssuerBankAccountBO?> GetAsync(int id, CancellationToken ct = default)
        {
            return await _db.IssuerBankAccount.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new IssuerBankAccountBO
                {
                    Id = x.Id,
                    IssuerCompanyId = x.IssuerCompanyId,
                    Iban = x.Iban,
                    Bic = x.Bic,
                    DisplayName = x.DisplayName,
                    IsDefault = x.IsDefault,
                    ValidFrom = x.ValidFrom,
                    ValidTo = x.ValidTo
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<int> CreateAsync(IssuerBankAccountBO bo, CancellationToken ct = default)
        {
            var e = new IssuerBankAccount
            {
                IssuerCompanyId = bo.IssuerCompanyId,
                Iban = NormalizeIban(bo.Iban),
                Bic = bo.Bic?.Trim().ToUpperInvariant(),
                DisplayName = bo.DisplayName?.Trim(),
                IsDefault = bo.IsDefault,
                ValidFrom = bo.ValidFrom,
                ValidTo = bo.ValidTo
            };

            _uow.IssuerBankAccount.Add(e);
            await _uow.SaveChangesAsync(ct);

            if (e.IsDefault)
                await SetDefaultAsync(e.Id, ct);

            return e.Id;
        }

        public async Task UpdateAsync(IssuerBankAccountBO bo, CancellationToken ct = default)
        {
            var e = await _db.IssuerBankAccount.FirstAsync(x => x.Id == bo.Id, ct);
            e.Iban = NormalizeIban(bo.Iban);
            e.Bic = bo.Bic?.Trim().ToUpperInvariant();
            e.DisplayName = bo.DisplayName?.Trim();
            e.ValidFrom = bo.ValidFrom;
            e.ValidTo = bo.ValidTo;

            // toggling default? respecteer unieke default
            if (bo.IsDefault && !e.IsDefault)
            {
                e.IsDefault = true;
                await _uow.SaveChangesAsync(ct);
                await SetDefaultAsync(e.Id, ct);
                return;
            }

            e.IsDefault = bo.IsDefault;
            await _uow.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.IssuerBankAccount.FirstAsync(x => x.Id == id, ct);
            _uow.IssuerBankAccount.Remove(e);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task SetDefaultAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.IssuerBankAccount.FirstAsync(x => x.Id == id, ct);
            var issuerId = e.IssuerCompanyId;

            await using var tx = await _uow.BeginTransactionAsync(ct);
            try
            {
                // unset andere defaults
                await _db.IssuerBankAccount
                    .Where(x => x.IssuerCompanyId == issuerId && x.IsDefault && x.Id != id)
                    .ExecuteUpdateAsync(up => up.SetProperty(x => x.IsDefault, false), ct);

                // zet deze op default
                e.IsDefault = true;
                await _uow.SaveChangesAsync(ct);

                await _uow.CommitTransactionAsync(_uow.CurrentTransaction!, ct);
            }
            catch
            {
                await _uow.RollbackTransactionAsync(_uow.CurrentTransaction!, ct);
                throw;
            }
        }

        private static string NormalizeIban(string s)
            => (s ?? "").Replace(" ", "").Replace("-", "").ToUpperInvariant();
    }

}
