using BOCore;
using DALCore;
using DALCore.Models;
using DALCore.Query;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using ServiceCore.Helpers;
using ServiceCore.Translators;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore
{
    public class PartyLookupService : IPartyLookupService
    {
        private readonly UnitOfWorkCore _uow;
        private readonly cpmRunningContext _db;

        public PartyLookupService(UnitOfWorkCore uow) { _uow = uow; _db = (cpmRunningContext)uow.Context; }

        public async Task<int> GetFirstActiveIssuerIdAsync(CancellationToken ct = default)
               => await _db.IssuerCompany.AsNoTracking()
                     .Where(x => x.IsActive)
                     .OrderBy(x => x.Name)
                     .Select(x => x.Id)
                     .FirstOrDefaultAsync(ct);

        public async Task<IReadOnlyList<(int Id, string Name)>> ListActiveIssuersAsync(CancellationToken ct = default)
            => await _db.IssuerCompany.AsNoTracking()
                  .Where(x => x.IsActive)
                  .OrderBy(x => x.Name)
                  .Select(x => new ValueTuple<int, string>(x.Id, x.Name))
                  .ToListAsync(ct);

        public async Task<IReadOnlyList<PartyLookupItem>> SearchPartiesAsync(string term, int take = 20, CancellationToken ct = default)
        {
            term ??= string.Empty;
            term = term.Trim();
            var like = $"%{term.Replace("%", "[%]").Replace("_", "[_]")}%";

            // 1) EF-vriendelijke projectie naar anonieme types
            var clientsRaw = await _db.ClientAccount.AsNoTracking()
                .Where(x => term == "" || EF.Functions.Like(x.Name, like))
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.CompanyName, x.Salutation })
                .Take(take)
                .ToListAsync(ct);

            var contactsRaw = await _db.ClientContacts.AsNoTracking()
                .Where(x => x.IsCoOwner == true &&
                       (term == ""
                        || EF.Functions.Like(x.Name, like)
                        || EF.Functions.Like(x.Forename, like)
                        || EF.Functions.Like(x.Forename + " " + x.Name, like)))
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Salutation, x.Forename, x.Name, x.CompanyName })
                .Take(take)
                .ToListAsync(ct);

            var suppliersRaw = await _db.CompanyInfo.AsNoTracking()
                .Where(x => term == "" || EF.Functions.Like(x.BedrijfsNaam, like))
                .OrderBy(x => x.BedrijfsNaam)
                .Select(x => new { Id = x.CompanyId, Display = x.BedrijfsNaam })
                .Take(take)
                .ToListAsync(ct);

            // 2) Pas NA ToListAsync omzetten naar jouw record/DTO
            var clients = clientsRaw.Select(x =>
            {
                var company = string.IsNullOrWhiteSpace(x.CompanyName) ? null : x.CompanyName.Trim();
                var baseName = !string.IsNullOrWhiteSpace(x.Name)
                    ? x.Name.Trim()
                    : (company ?? string.Empty);

                var salutation = FormatSalutation(x.Salutation);
                var displayParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(salutation)) displayParts.Add(salutation);
                if (!string.IsNullOrWhiteSpace(baseName)) displayParts.Add(baseName);

                var display = displayParts.Count > 0
                    ? string.Join(" ", displayParts)
                    : (company ?? string.Empty);

                var hint = !string.IsNullOrWhiteSpace(baseName)
                    && company != null
                    && !string.Equals(baseName, company, StringComparison.OrdinalIgnoreCase)
                        ? company
                        : null;

                return new PartyLookupItem(
                    InvoicePartyType.ClientAccount,
                    x.Id,
                    baseName,
                    hint,
                    display);
            });

            var contacts = contactsRaw.Select(x =>
            {
                var company = string.IsNullOrWhiteSpace(x.CompanyName) ? null : x.CompanyName.Trim();

                var personalParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(x.Forename)) personalParts.Add(x.Forename.Trim());
                if (!string.IsNullOrWhiteSpace(x.Name)) personalParts.Add(x.Name.Trim());

                var baseName = personalParts.Count > 0
                    ? string.Join(" ", personalParts)
                    : (company ?? string.Empty);

                var salutation = FormatSalutation(x.Salutation);
                var displayParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(salutation)) displayParts.Add(salutation);
                if (!string.IsNullOrWhiteSpace(baseName)) displayParts.Add(baseName);

                var display = displayParts.Count > 0
                    ? string.Join(" ", displayParts)
                    : (company ?? string.Empty);

                var hint = company != null && personalParts.Count > 0
                    ? company
                    : null;

                return new PartyLookupItem(
                    InvoicePartyType.ClientContact,
                    x.Id,
                    baseName,
                    hint,
                    display);
            });

            var suppliers = suppliersRaw.Select(x =>
                new PartyLookupItem(InvoicePartyType.Supplier, x.Id, x.Display, null, x.Display));


            return clients.Concat(contacts).Concat(suppliers)
                          .OrderBy(x => x.Name)
                          .Take(take)
                          .ToList();
        }
        private static string? FormatSalutation(string? value)
            => SalutationDisplayHelper.ToDisplayString(value);

    }
}
