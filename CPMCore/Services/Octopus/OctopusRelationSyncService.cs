using CPMCore.Models.Leveranciers;
using DALCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CPMCore.Services.Octopus
{
    public interface IOctopusRelationSyncService
    {
        Task<OctopusRelationSyncResult> SyncRelationsAsync(int issuerId, CancellationToken ct = default);
        Task LinkExistingAsync(int issuerId, bool isSupplier, int candidateId, int? octopusRelationId, CancellationToken ct = default);
    }

    public record OctopusRelationCandidate(int Id, string Name, string VatNumber, string CandidateType);

    public record OctopusRelationSuggestion(string RelationName, string RelationType, string VatNumber, int? OctopusRelationId, IReadOnlyList<OctopusRelationCandidate> Candidates);

    public record OctopusRelationSyncResult(int SuppliersCreated, int SuppliersLinked, int ClientsCreated, int ClientsLinked, int CustomerFlagsUpdated, IReadOnlyList<OctopusRelationSuggestion> Suggestions);

    public class OctopusRelationSyncService : IOctopusRelationSyncService
    {
        private readonly cpmRunningContext _db;
        private readonly IOctopusApiClient _octopusClient;
        private readonly IOctopusTokenManager _tokens;
        private readonly ILogger<OctopusRelationSyncService> _logger;

        public OctopusRelationSyncService(cpmRunningContext db, IOctopusApiClient octopusClient, IOctopusTokenManager tokens, ILogger<OctopusRelationSyncService> logger)
        {
            _db = db;
            _octopusClient = octopusClient;
            _tokens = tokens;
            _logger = logger;
        }

        public async Task<OctopusRelationSyncResult> SyncRelationsAsync(int issuerId, CancellationToken ct = default)
        {
            var issuer = await _db.IssuerCompany.FirstOrDefaultAsync(i => i.Id == issuerId, ct);
            if (issuer == null)
            {
                throw new InvalidOperationException($"Issuer {issuerId} niet gevonden.");
            }

            if (string.IsNullOrWhiteSpace(issuer.OctopusDossierNumber))
            {
                throw new InvalidOperationException("Koppel eerst een Octopus-dossier.");
            }

            var dossierToken = await _tokens.RefreshDossierTokenAsync(issuerId, issuer.OctopusDossierNumber, ct);

            // ⚠️ Octopus rate limit: GET /relations zonder zoekparameters = max 2 calls per dag.
            // Gebruik deze sync dus spaarzaam. Voor frequente opzoekingen: gebruik FindRelationAsync met vatNr/name.
            var relations = await _octopusClient.GetRelationsAsync(dossierToken.Token, issuer.OctopusDossierNumber, ct);

            var suppliersCreated = 0;
            var suppliersLinked = 0;
            var clientsCreated = 0;
            var clientsLinked = 0;
            var customerFlagsUpdated = 0;
            var suggestions = new List<OctopusRelationSuggestion>();

            foreach (var relation in relations.Where(r => r.Client || r.Supplier))
            {
                var normalizedVat = NormalizeVat(relation.VatNr);
                var normalizedName = NormalizeName(relation.Name);
                var octopusRelationId = relation.RelationIdentificationServiceData?.RelationKey?.Id;

                if (relation.Supplier)
                {
                    var supplier = await FindSupplierAsync(issuerId, normalizedVat, normalizedName, ct);

                    if (supplier == null)
                    {
                        var possibleMatches = await FindSimilarSuppliersAsync(normalizedName, normalizedVat, ct);

                        if (possibleMatches.Any())
                        {
                            suggestions.Add(new OctopusRelationSuggestion(relation.Name ?? relation.Firstname ?? "Onbekende relatie", "Leverancier", normalizedVat, octopusRelationId, possibleMatches));
                            continue;
                        }

                        supplier = await CreateSupplierEntityAsync(relation, normalizedVat, ct);
                        _db.CompanyInfo.Add(supplier);
                        await _db.SaveChangesAsync(ct);
                        suppliersCreated++;
                    }

                    suppliersLinked += await EnsureSupplierIssuerLinkAsync(supplier, issuerId, octopusRelationId, ct);

                    if (relation.Client && !supplier.IsCustomer)
                    {
                        supplier.IsCustomer = true;
                        customerFlagsUpdated++;
                    }
                }
                else if (relation.Client)
                {
                    var client = await FindClientAsync(issuerId, normalizedVat, normalizedName, ct);

                    if (client == null)
                    {
                        var possibleMatches = await FindSimilarClientsAsync(normalizedName, normalizedVat, ct);

                        if (possibleMatches.Any())
                        {
                            suggestions.Add(new OctopusRelationSuggestion(relation.Name ?? relation.Firstname ?? "Onbekende relatie", "Klant", normalizedVat, octopusRelationId, possibleMatches));
                            continue;
                        }

                        client = await CreateClientEntityAsync(relation, normalizedVat, ct);
                        _db.ClientAccount.Add(client);
                        await _db.SaveChangesAsync(ct);
                        clientsCreated++;
                    }

                    clientsLinked += await EnsureClientIssuerLinkAsync(client, issuerId, octopusRelationId, ct);
                }
            }

            await _db.SaveChangesAsync(ct);

            return new OctopusRelationSyncResult(suppliersCreated, suppliersLinked, clientsCreated, clientsLinked, customerFlagsUpdated, suggestions);
        }

        public async Task LinkExistingAsync(int issuerId, bool isSupplier, int candidateId, int? octopusRelationId, CancellationToken ct = default)
        {
            if (isSupplier)
            {
                var supplier = await _db.CompanyInfo.FirstOrDefaultAsync(c => c.CompanyId == candidateId, ct)
                    ?? throw new InvalidOperationException("Leverancier niet gevonden.");

                await EnsureSupplierIssuerLinkAsync(supplier, issuerId, octopusRelationId, ct);
            }
            else
            {
                var client = await _db.ClientAccount.FirstOrDefaultAsync(c => c.Id == candidateId, ct)
                    ?? throw new InvalidOperationException("Klant niet gevonden.");

                await EnsureClientIssuerLinkAsync(client, issuerId, octopusRelationId, ct);
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task<int> EnsureSupplierIssuerLinkAsync(CompanyInfo supplier, int issuerId, int? octopusRelationId, CancellationToken ct)
        {
            await _db.Entry(supplier).Collection(s => s.CompanyIssuerCompany).LoadAsync(ct);

            supplier.CompanyIssuerCompany ??= new List<CompanyIssuerCompany>();

            var existingLink = supplier.CompanyIssuerCompany
                .FirstOrDefault(l => l.IssuerCompanyId == issuerId)
                ?? _db.ChangeTracker.Entries<CompanyIssuerCompany>()
                    .FirstOrDefault(e => e.Entity.CompanyId == supplier.CompanyId && e.Entity.IssuerCompanyId == issuerId)?.Entity;

            if (existingLink != null)
            {
                if (octopusRelationId.HasValue && (existingLink.OctopusRelationId ?? 0) <= 0)
                {
                    existingLink.OctopusRelationId = octopusRelationId;
                }

                return 0;
            }

            supplier.CompanyIssuerCompany.Add(new CompanyIssuerCompany
            {
                CompanyId = supplier.CompanyId,
                IssuerCompanyId = issuerId,
                OctopusRelationId = octopusRelationId
            });

            return 1;
        }

        private async Task<int> EnsureClientIssuerLinkAsync(ClientAccount client, int issuerId, int? octopusRelationId, CancellationToken ct)
        {
            await _db.Entry(client).Collection(c => c.ClientAccountIssuerCompany).LoadAsync(ct);

            client.ClientAccountIssuerCompany ??= new List<ClientAccountIssuerCompany>();

            var existingLink = client.ClientAccountIssuerCompany
                .FirstOrDefault(l => l.IssuerCompanyId == issuerId)
                ?? _db.ChangeTracker.Entries<ClientAccountIssuerCompany>()
                    .FirstOrDefault(e => e.Entity.ClientAccountId == client.Id && e.Entity.IssuerCompanyId == issuerId)?.Entity;

            if (existingLink != null)
            {
                if (octopusRelationId.HasValue && (existingLink.OctopusRelationId ?? 0) <= 0)
                {
                    existingLink.OctopusRelationId = octopusRelationId;
                }

                return 0;
            }

            client.ClientAccountIssuerCompany.Add(new ClientAccountIssuerCompany
            {
                ClientAccountId = client.Id,
                IssuerCompanyId = issuerId,
                OctopusRelationId = octopusRelationId
            });

            return 1;
        }

        private async Task<CompanyInfo?> FindSupplierAsync(int issuerId, string normalizedVat, string normalizedName, CancellationToken ct)
        {
            var supplierQuery = _db.CompanyInfo
                .Include(c => c.CompanyIssuerCompany)
                .Where(c => c.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedVat))
            {
                var suppliers = await supplierQuery
                    .Where(c => !string.IsNullOrWhiteSpace(c.Ondernemingsnummer))
                    .ToListAsync(ct);

                var supplier = suppliers.FirstOrDefault(c => NormalizeVat(c.Ondernemingsnummer) == normalizedVat);
                if (supplier != null)
                {
                    return supplier;
                }
            }
            else if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                var suppliers = await supplierQuery
                    .Where(c => !string.IsNullOrWhiteSpace(c.BedrijfsNaam))
                    .ToListAsync(ct);

                var supplier = suppliers.FirstOrDefault(c => NormalizeName(c.BedrijfsNaam) == normalizedName);
                if (supplier != null)
                {
                    return supplier;
                }
            }
            else
            {
                return null;
            }

            var linkedSuppliers = await _db.CompanyInfo
                .Include(c => c.CompanyIssuerCompany)
                .Where(c => c.IsActive && c.CompanyIssuerCompany.Any(link => link.IssuerCompanyId == issuerId))
                .ToListAsync(ct);

            return linkedSuppliers.FirstOrDefault(c =>
                (!string.IsNullOrWhiteSpace(normalizedVat) && NormalizeVat(c.Ondernemingsnummer) == normalizedVat)
                || (!string.IsNullOrWhiteSpace(normalizedName) && NormalizeName(c.BedrijfsNaam) == normalizedName));
        }

        private async Task<ClientAccount?> FindClientAsync(int issuerId, string normalizedVat, string normalizedName, CancellationToken ct)
        {
            var query = _db.ClientAccount
                .Include(c => c.ClientAccountIssuerCompany)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedVat))
            {
                var clients = await query
                    .Where(c => !string.IsNullOrWhiteSpace(c.Vatnumber))
                    .ToListAsync(ct);

                var client = clients.FirstOrDefault(c => NormalizeVat(c.Vatnumber) == normalizedVat);
                if (client != null)
                {
                    return client;
                }
            }
            else if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                var clients = await query
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name) || !string.IsNullOrWhiteSpace(c.CompanyName))
                    .ToListAsync(ct);

                var client = clients.FirstOrDefault(c => NormalizeName(c.Name) == normalizedName || NormalizeName(c.CompanyName) == normalizedName);
                if (client != null)
                {
                    return client;
                }
            }
            else
            {
                return null;
            }

            var linkedClients = await _db.ClientAccount
                .Include(c => c.ClientAccountIssuerCompany)
                .Where(c => c.ClientAccountIssuerCompany.Any(link => link.IssuerCompanyId == issuerId))
                .ToListAsync(ct);

            return linkedClients.FirstOrDefault(c =>
                (!string.IsNullOrWhiteSpace(normalizedVat) && NormalizeVat(c.Vatnumber) == normalizedVat)
                || (!string.IsNullOrWhiteSpace(normalizedName) && (NormalizeName(c.Name) == normalizedName || NormalizeName(c.CompanyName) == normalizedName)));
        }

        private async Task<IReadOnlyList<OctopusRelationCandidate>> FindSimilarSuppliersAsync(string normalizedName, string normalizedVat, CancellationToken ct)
        {
            var supplierMatches = await _db.CompanyInfo
                .Where(c => c.IsActive && !string.IsNullOrEmpty(c.BedrijfsNaam))
                .Select(c => new { c.CompanyId, c.BedrijfsNaam, c.Ondernemingsnummer })
                .ToListAsync(ct);

            return supplierMatches
                .Where(c => IsCloseMatch(normalizedName, NormalizeName(c.BedrijfsNaam), normalizedVat, NormalizeVat(c.Ondernemingsnummer)))
                .Select(c => new OctopusRelationCandidate(c.CompanyId, c.BedrijfsNaam, NormalizeVat(c.Ondernemingsnummer), "Leverancier"))
                .ToList();
        }

        private async Task<IReadOnlyList<OctopusRelationCandidate>> FindSimilarClientsAsync(string normalizedName, string normalizedVat, CancellationToken ct)
        {
            var clientMatches = await _db.ClientAccount
                .Where(c => !string.IsNullOrEmpty(c.Name) || !string.IsNullOrEmpty(c.CompanyName))
                .Select(c => new { c.Id, c.Name, c.CompanyName, c.Vatnumber })
                .ToListAsync(ct);

            return clientMatches
                .Where(c => IsCloseMatch(normalizedName, NormalizeName(c.Name ?? c.CompanyName), normalizedVat, NormalizeVat(c.Vatnumber)))
                .Select(c => new OctopusRelationCandidate(c.Id, c.Name ?? c.CompanyName ?? "", NormalizeVat(c.Vatnumber), "Klant"))
                .ToList();
        }

        private async Task<CompanyInfo> CreateSupplierEntityAsync(OctopusRelation relation, string normalizedVat, CancellationToken ct)
        {
            var postCodeId = await ResolvePostalCodeIdAsync(relation.PostalCode, relation.City, ct);
            var vatStatus = ResolveSupplierVatStatus(relation.VatType);
            var (street, houseNr, busNr) = ParseStreetAndNr(relation.StreetAndNr);

            return new CompanyInfo
            {
                BedrijfsNaam = BuildBedrijfsNaam(relation.Name, relation.Firstname),
                Ondernemingsnummer = normalizedVat,
                VatNumber = normalizedVat,
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
        }

        private async Task<ClientAccount> CreateClientEntityAsync(OctopusRelation relation, string normalizedVat, CancellationToken ct)
        {
            var name = relation.Name ?? relation.Firstname ?? "Nieuwe klant";
            var isIndividual = IsIndividualVatType(relation.VatType) || string.IsNullOrWhiteSpace(normalizedVat);
            var postCodeId = await ResolvePostalCodeIdAsync(relation.PostalCode, relation.City, ct);

            return new ClientAccount
            {
                Name = isIndividual ? name : string.Empty,
                CompanyName = isIndividual ? string.Empty : name,
                Vatnumber = normalizedVat,
                Street = relation.StreetAndNr,
                InvoiceStreet = relation.StreetAndNr,
                PostalCodeId = postCodeId,
                InvoicePostalCodeId = postCodeId,
                InvoiceAddress = false,
                Email = relation.Email,
                InvoiceEmail = relation.Email
            };
        }

        // "Achternaam Voornaam" — geen komma, achternaam eerst
        private static string BuildBedrijfsNaam(string? lastName, string? firstName)
        {
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                return $"{lastName.Trim()} {firstName.Trim()}";
            return (lastName ?? firstName ?? string.Empty).Trim();
        }

        // "Tuinwijk 79/A" → ("Tuinwijk", "79", "A")  |  "Straat 12" → ("Straat", "12", null)
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

        private async Task<int?> ResolvePostalCodeIdAsync(string? postalCode, string? city, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
            {
                return null;
            }

            var normalizedPostal = postalCode.Trim();
            var matches = await _db.PostalCode
                .Where(p => p.Postcode == normalizedPostal)
                .ToListAsync(ct);

            if (matches.Count == 1)
            {
                return matches[0].PostcodeId;
            }

            if (matches.Count > 1 && !string.IsNullOrWhiteSpace(city))
            {
                var normalizedCity = NormalizeName(city);
                var cityMatch = matches.FirstOrDefault(p => NormalizeName(p.Gemeente) == normalizedCity);
                if (cityMatch != null)
                {
                    return cityMatch.PostcodeId;
                }
            }

            return null;
        }

        private static bool IsIndividualVatType(int? vatType)
            => vatType is 7 or 8 or 9;

        private static string ResolveSupplierVatStatus(int? vatType)
        {
            // Octopus vatType waarden:
            // 0  = Onbekend              → btw-plichtig (veilige default)
            // 1  = Belgische btw-plichtige
            // 4  = Intracommunautair     → btw-plichtig (verlegging bij aankoop)
            // 6  = Buiten EU             → btw-plichtig (geen btw op factuur)
            // 7  = Particulier België
            // 8  = Particulier EU
            // 9  = Particulier niet-EU
            // 10 = Niet btw-plichtig

            return vatType switch
            {
                10 => SupplierFormViewModel.VatStatusNietBtwPlichtig,
                7 or 8 or 9 => SupplierFormViewModel.VatStatusParticulier,
                _ => SupplierFormViewModel.VatStatusBtwPlichtig
            };
        }

        private static string NormalizeVat(string? vat)
        {
            if (string.IsNullOrWhiteSpace(vat))
            {
                return string.Empty;
            }

            return Regex.Replace(vat, "\\D", string.Empty);
        }

        private static string NormalizeName(string? name)
        {
            return string.IsNullOrWhiteSpace(name)
                ? string.Empty
                : name.Trim().ToUpperInvariant();
        }

        private static bool IsCloseMatch(string requestedName, string candidateName, string requestedVat, string candidateVat)
        {
            if (!string.IsNullOrWhiteSpace(requestedVat) && requestedVat == candidateVat)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(requestedName) || string.IsNullOrWhiteSpace(candidateName))
            {
                return false;
            }

            if (requestedName == candidateName)
            {
                return true;
            }

            if (requestedName.Contains(candidateName) || candidateName.Contains(requestedName))
            {
                return true;
            }

            return LevenshteinDistance(requestedName, candidateName) <= 2;
        }

        private static int LevenshteinDistance(string source, string target)
        {
            var n = source.Length;
            var m = target.Length;
            var d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}