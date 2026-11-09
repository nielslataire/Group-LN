using DALCore;
using DALCore.Models;
using FacadeCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceCore.IncomingInvoices
{
    /// <summary>
    /// Controle van de inhoudingsplicht (30bis/30ter) van leveranciers via de RSZ-webservice BillRetainment (REST v2).
    /// Authenticatie: OAuth client credentials met een JWT client assertion (RS256) die met onze private sleutel getekend wordt; het bijhorende
    /// certificaat staat bij het REST-account in Chaman. Config (sectie <c>RszBillRetainment</c>): <c>ClientId</c>, <c>PrivateKeyPem</c> (de PEM-tekst zelf, via omgevingsvariabele of user-secret; heeft voorrang) of <c>PrivateKeyPath</c> (pad naar de PEM
    /// buiten de repo) en <c>UseSimulation</c> (true = testomgeving; standaard true tot de RSZ de productie-integratie toelaat).
    /// Bewijs: per onderneming bewaren we de consultatiereferentie, het BelGov-Trace-Id van het antwoord en het ongewijzigde RSZ-resultaat.
    /// </summary>
    public class RetainmentCheckService : IRetainmentCheckService
    {
        private const string TokenUrl = "https://services.socialsecurity.be/REST/oauth/v5/token";
        private const string ProductionBase = "https://services.socialsecurity.be/REST/billRetainment/v2";
        private const string SimulationBase = "https://services-sim.socialsecurity.be/REST/billRetainment/v2";
        private const string Scope = "scope:rsz-onss:socialdebt:retention:billretainment-rest:post-billretainment";
        private const int MaxPerRequest = 200;

        // Het token is 10 minuten geldig; we hergebruiken het tot er minder dan een minuut over is.
        private static readonly SemaphoreSlim TokenLock = new(1, 1);
        private static string _token;
        private static DateTime _tokenExpiresUtc = DateTime.MinValue;

        private readonly cpmRunningContext _db;
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<RetainmentCheckService> _logger;

        public RetainmentCheckService(UnitOfWorkCore uow, HttpClient http, IConfiguration config, ILogger<RetainmentCheckService> logger)
        {
            _db = (cpmRunningContext)uow.Context;
            _http = http;
            _config = config;
            _logger = logger;
        }

        private string ClientId => _config["RszBillRetainment:ClientId"];
        private string PrivateKeyPath => _config["RszBillRetainment:PrivateKeyPath"];
        private bool UseSimulation => !string.Equals(_config["RszBillRetainment:UseSimulation"], "false", StringComparison.OrdinalIgnoreCase);
        private string EnvironmentName => UseSimulation ? "Simulation" : "Production";

        public bool IsSimulation => UseSimulation;

        /// <summary>De sleutel zelf (PEM-tekst) uit een omgevingsvariabele of user-secret, bv. <c>RszBillRetainment__PrivateKeyPem</c>. Een letterlijke "\n" telt als regeleinde.</summary>
        private string PrivateKeyPem
        {
            get
            {
                var pem = _config["RszBillRetainment:PrivateKeyPem"];
                return string.IsNullOrWhiteSpace(pem) ? null : pem.Replace("\\n", "\n").Trim();
            }
        }

        /// <summary>Ingesteld = Client ID én een sleutel: de PEM-tekst uit de configuratie, of anders een bestand op <c>PrivateKeyPath</c>.</summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId)
            && (PrivateKeyPem != null || (!string.IsNullOrWhiteSpace(PrivateKeyPath) && File.Exists(PrivateKeyPath)));

        // ─── Lezen ────────────────────────────────────────────────────────────────

        public async Task<RetainmentCheckVm> GetLatestForInvoiceAsync(int incomingInvoiceId, CancellationToken ct = default)
        {
            // Een controle die voor deze factuur gebeurde, of de laatste controle van dezelfde onderneming (die werd hergebruikt toen de factuur binnenkwam).
            var vat = await _db.IncommingInvoices.AsNoTracking().Where(i => i.Id == incomingInvoiceId).Select(i => i.SupplierVatNumber).FirstOrDefaultAsync(ct);
            var identifier = NormalizeIdentifier(vat);
            var row = await _db.SupplierRetainmentCheck.AsNoTracking()
                .Where(c => c.IncomingInvoiceId == incomingInvoiceId || (identifier != null && c.EnterpriseIdentifier == identifier && c.ErrorMessage == null))
                .OrderByDescending(c => c.CheckedAt)
                .FirstOrDefaultAsync(ct);
            return row == null ? null : ToVm(row);
        }

        public async Task<RetainmentCheckVm> GetLatestForEnterpriseAsync(string identifier, CancellationToken ct = default)
        {
            var id = NormalizeIdentifier(identifier);
            if (id == null) return null;
            var row = await _db.SupplierRetainmentCheck.AsNoTracking()
                .Where(c => c.EnterpriseIdentifier == id && c.ErrorMessage == null)
                .OrderByDescending(c => c.CheckedAt)
                .FirstOrDefaultAsync(ct);
            return row == null ? null : ToVm(row);
        }

        public async Task<IReadOnlyList<RetainmentCheckVm>> GetHistoryForEnterpriseAsync(string identifier, int take, CancellationToken ct = default)
        {
            var id = NormalizeIdentifier(identifier);
            if (id == null) return Array.Empty<RetainmentCheckVm>();
            var rows = await _db.SupplierRetainmentCheck.AsNoTracking()
                .Where(c => c.EnterpriseIdentifier == id)
                .OrderByDescending(c => c.CheckedAt)
                .Take(take)
                .ToListAsync(ct);
            return rows.Select(ToVm).ToList();
        }

        public async Task<IReadOnlyList<RetainmentCheckVm>> GetOpenObligationsAsync(CancellationToken ct = default)
        {
            var env = EnvironmentName;
            var rows = await _db.SupplierRetainmentCheck.AsNoTracking()
                .Where(c => c.Environment == env && c.ErrorMessage == null
                    && !_db.SupplierRetainmentCheck.Any(n => n.EnterpriseIdentifier == c.EnterpriseIdentifier && n.Environment == env && n.ErrorMessage == null && n.CheckedAt > c.CheckedAt)
                    && (c.FiscalSituation == "obligation" || c.SocialSituation == "obligation"))
                .OrderByDescending(c => c.CheckedAt)
                .ToListAsync(ct);
            return rows.GroupBy(r => r.EnterpriseIdentifier).Select(g => ToVm(g.First())).ToList();
        }

        /// <summary>De laatste geslaagde controle per onderneming in de huidige omgeving.</summary>
        private async Task<Dictionary<string, RetainmentCheckVm>> LatestSuccessfulAsync(IReadOnlyCollection<string> identifiers, CancellationToken ct)
        {
            var env = EnvironmentName;
            var rows = await _db.SupplierRetainmentCheck.AsNoTracking()
                .Where(c => identifiers.Contains(c.EnterpriseIdentifier) && c.Environment == env && c.ErrorMessage == null)
                .OrderByDescending(c => c.CheckedAt)
                .ToListAsync(ct);
            return rows.GroupBy(r => r.EnterpriseIdentifier).ToDictionary(g => g.Key, g => ToVm(g.First()));
        }

        public async Task<IReadOnlyDictionary<string, RetainmentCheckVm>> GetLastChecksAsync(IReadOnlyCollection<string> identifiers, CancellationToken ct = default)
        {
            var env = EnvironmentName;
            var ids = identifiers.Select(NormalizeIdentifier).Where(i => i != null).Distinct().ToList();
            var rows = await _db.SupplierRetainmentCheck.AsNoTracking()
                .Where(c => ids.Contains(c.EnterpriseIdentifier) && c.Environment == env)
                .OrderByDescending(c => c.CheckedAt)
                .Select(c => new SupplierRetainmentCheck { Id = c.Id, EnterpriseIdentifier = c.EnterpriseIdentifier, Environment = c.Environment, CheckedAt = c.CheckedAt, CheckedBy = c.CheckedBy, FiscalSituation = c.FiscalSituation, FiscalWithholdingPercentage = c.FiscalWithholdingPercentage, FiscalValidUntil = c.FiscalValidUntil, SocialSituation = c.SocialSituation, SocialWithholdingPercentage = c.SocialWithholdingPercentage, SocialValidUntil = c.SocialValidUntil, ErrorMessage = c.ErrorMessage })
                .ToListAsync(ct);
            return rows.GroupBy(r => r.EnterpriseIdentifier).ToDictionary(g => g.Key, g => ToVm(g.First()));
        }

        public async Task<RetainmentCheckVm> EnsureCheckedAsync(RetainmentCheckInput input, int? incomingInvoiceId, string checkedBy, CancellationToken ct = default)
        {
            var identifier = NormalizeIdentifier(input?.Identifier);
            if (identifier == null)
                return new RetainmentCheckVm { SupplierName = input?.SupplierName, EnterpriseIdentifier = input?.Identifier, Environment = EnvironmentName, CheckedAt = DateTime.UtcNow, ErrorMessage = "Geen (geldig) btw- of ondernemingsnummer opgegeven." };

            var latest = await LatestSuccessfulAsync(new[] { identifier }, ct);
            if (latest.TryGetValue(identifier, out var existing) && existing.IsStillValid(DateTime.UtcNow))
                return existing;

            var rows = await RunChecksAsync(new[] { input }, checkedBy, batchId: null, incomingInvoiceId, ct);
            return ToVm(rows[0]);
        }

        public async Task<RetainmentBatchVm> EnsureCheckedBatchAsync(IReadOnlyList<RetainmentCheckInput> inputs, string checkedBy, CancellationToken ct = default)
        {
            var distinct = inputs
                .Select(i => new { Input = i, Id = NormalizeIdentifier(i.Identifier) })
                .Where(x => x.Id != null)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();
            if (distinct.Count == 0) return null;

            var latest = await LatestSuccessfulAsync(distinct.Select(x => x.Id).ToList(), ct);
            var now = DateTime.UtcNow;
            var missing = distinct.Where(x => !latest.TryGetValue(x.Id, out var l) || !l.IsStillValid(now)).Select(x => x.Input).ToList();
            return missing.Count == 0 ? null : await CheckBatchAsync(missing, checkedBy, ct);
        }

        public async Task<RetainmentBatchVm> GetBatchAsync(Guid batchId, CancellationToken ct = default)
        {
            var rows = await _db.SupplierRetainmentCheck.AsNoTracking().Where(c => c.BatchId == batchId).OrderBy(c => c.Id).ToListAsync(ct);
            return rows.Count == 0 ? null : ToBatchVm(batchId, rows);
        }

        public async Task<bool> RenameBatchAsync(Guid batchId, string name, CancellationToken ct = default)
        {
            var rows = await _db.SupplierRetainmentCheck.Where(c => c.BatchId == batchId).ToListAsync(ct);
            if (rows.Count == 0) return false;
            var clean = string.IsNullOrWhiteSpace(name) ? null : Truncate(name.Trim(), 200);
            foreach (var r in rows) r.BatchName = clean;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IReadOnlyList<RetainmentBatchSummaryVm>> ListBatchesAsync(int take, CancellationToken ct = default)
        {
            var groups = await _db.SupplierRetainmentCheck.AsNoTracking()
                .Where(c => c.BatchId != null)
                .GroupBy(c => c.BatchId)
                .Select(g => new
                {
                    BatchId = g.Key.Value,
                    CheckedAt = g.Min(c => c.CheckedAt),
                    CheckedBy = g.Min(c => c.CheckedBy),
                    Environment = g.Min(c => c.Environment),
                    Name = g.Min(c => c.BatchName),
                    Total = g.Count(),
                    WithObligation = g.Count(c => c.ErrorMessage == null && (c.FiscalSituation == "obligation" || c.SocialSituation == "obligation")),
                    Failed = g.Count(c => c.ErrorMessage != null)
                })
                .OrderByDescending(g => g.CheckedAt)
                .Take(take)
                .ToListAsync(ct);
            return groups.Select(g => new RetainmentBatchSummaryVm
            {
                BatchId = g.BatchId, Name = g.Name, CheckedAt = g.CheckedAt, CheckedBy = g.CheckedBy, Environment = g.Environment,
                Total = g.Total, WithObligation = g.WithObligation, Failed = g.Failed
            }).ToList();
        }

        // ─── Controleren ──────────────────────────────────────────────────────────

        public async Task<RetainmentCheckVm> CheckInvoiceAsync(int incomingInvoiceId, string checkedBy, CancellationToken ct = default)
        {
            var inv = await _db.IncommingInvoices.AsNoTracking()
                .Where(i => i.Id == incomingInvoiceId)
                .Select(i => new { i.SupplierVatNumber, i.SupplierName })
                .FirstOrDefaultAsync(ct);

            var rows = await RunChecksAsync(new[] { new RetainmentCheckInput { SupplierName = inv?.SupplierName, Identifier = inv?.SupplierVatNumber } },
                checkedBy, batchId: null, incomingInvoiceId, ct);
            return ToVm(rows[0]);
        }

        public async Task<RetainmentBatchVm> CheckBatchAsync(IReadOnlyList<RetainmentCheckInput> inputs, string checkedBy, CancellationToken ct = default, string batchName = null)
        {
            var batchId = Guid.NewGuid();
            var rows = await RunChecksAsync(inputs, checkedBy, batchId, incomingInvoiceId: null, ct, batchName);
            return ToBatchVm(batchId, rows);
        }

        private async Task<List<SupplierRetainmentCheck>> RunChecksAsync(IReadOnlyList<RetainmentCheckInput> inputs, string checkedBy, Guid? batchId, int? incomingInvoiceId, CancellationToken ct, string batchName = null)
        {
            var now = DateTime.UtcNow;
            var rows = new List<SupplierRetainmentCheck>();
            var seen = new HashSet<string>();
            foreach (var input in inputs)
            {
                var identifier = NormalizeIdentifier(input.Identifier);
                if (identifier != null && !seen.Add(identifier)) continue;   // dezelfde onderneming twee keer in de lijst: één controle volstaat

                rows.Add(new SupplierRetainmentCheck
                {
                    IncomingInvoiceId = incomingInvoiceId,
                    BatchId = batchId,
                    BatchName = Truncate(batchName, 200),
                    SupplierName = Truncate(input.SupplierName, 200),
                    EnterpriseIdentifier = Truncate(identifier ?? (input.Identifier ?? string.Empty).Trim(), 50),
                    Environment = EnvironmentName,
                    CheckedAt = now,
                    CheckedBy = Truncate(checkedBy, 200),
                    ErrorMessage = identifier == null ? "Geen (geldig) btw- of ondernemingsnummer opgegeven." : null
                });
            }

            var toQuery = rows.Where(r => r.ErrorMessage == null).ToList();
            if (toQuery.Count > 0 && !IsConfigured)
                foreach (var r in toQuery) r.ErrorMessage = "De koppeling met de RSZ is niet ingesteld.";
            else
                for (var i = 0; i < toQuery.Count; i += MaxPerRequest)
                    await QueryAsync(toQuery.Skip(i).Take(MaxPerRequest).ToList(), ct);

            _db.SupplierRetainmentCheck.AddRange(rows);
            await _db.SaveChangesAsync(ct);
            return rows;
        }

        /// <summary>Eén bevraging (max. 200 ondernemingen) bij de RSZ; zet het resultaat of de foutmelding op elke rij.</summary>
        private async Task QueryAsync(List<SupplierRetainmentCheck> rows, CancellationToken ct)
        {
            string traceId = null;
            try
            {
                var token = await GetTokenAsync(ct);
                using var request = new HttpRequestMessage(HttpMethod.Post, (UseSimulation ? SimulationBase : ProductionBase) + "/retainmentObligations/search")
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { enterpriseIdentifiers = rows.Select(r => r.EnterpriseIdentifier).ToArray() }), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _http.SendAsync(request, ct);
                traceId = response.Headers.TryGetValues("BelGov-Trace-Id", out var t) ? t.FirstOrDefault() : null;
                var body = await response.Content.ReadAsStringAsync(ct);
                foreach (var r in rows) r.TraceId = Truncate(traceId, 100);

                if (!response.IsSuccessStatusCode)
                {
                    // De foutteksten van de RSZ zijn niet voor eindgebruikers: we loggen ze (met trace-id) en tonen een eigen melding.
                    _logger.LogWarning("RSZ BillRetainment gaf {Status} (trace {TraceId}): {Body}", (int)response.StatusCode, traceId, body.Length > 500 ? body[..500] : body);
                    var msg = (int)response.StatusCode == 429
                        ? "De RSZ weigert tijdelijk nieuwe bevragingen (te veel aanvragen). Probeer het over enkele minuten opnieuw."
                        : $"De RSZ gaf een foutmelding terug (HTTP {(int)response.StatusCode}).";
                    foreach (var r in rows) r.ErrorMessage = msg;
                    return;
                }

                using var doc = JsonDocument.Parse(body);
                var items = doc.RootElement.TryGetProperty("items", out var arr) ? arr.EnumerateArray().ToList() : new List<JsonElement>();
                foreach (var row in rows)
                {
                    var item = items.Cast<JsonElement?>().FirstOrDefault(it => Matches(it.Value, row.EnterpriseIdentifier));
                    if (item == null) { row.ErrorMessage = "De RSZ gaf geen resultaat voor dit nummer."; continue; }
                    Apply(row, item.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Controle inhoudingsplicht mislukt (trace {TraceId})", traceId);
                foreach (var r in rows.Where(r => r.ErrorMessage == null && r.RawResponse == null))
                    r.ErrorMessage = "De RSZ gaf geen antwoord. Probeer het later opnieuw.";
            }
        }

        private static bool Matches(JsonElement item, string identifier)
        {
            if (!item.TryGetProperty("enterpriseIdentifiers", out var ids)) return false;
            foreach (var name in new[] { "enterpriseNumber", "vatNumber", "otherId" })
                if (ids.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && NormalizeIdentifier(v.GetString()) == identifier)
                    return true;
            return false;
        }

        private static void Apply(SupplierRetainmentCheck row, JsonElement item)
        {
            row.RawResponse = item.GetRawText();
            if (item.TryGetProperty("enterpriseIdentifiers", out var ids) && ids.TryGetProperty("enterpriseNumber", out var en))
                row.EnterpriseNumber = Truncate(en.GetString(), 20);
            row.RetainmentCheckReference = Truncate(Str(item, "retainmentCheckReference"), 50);

            if (item.TryGetProperty("fiscalSection", out var fiscal))
            {
                row.FiscalSituation = Str(fiscal, "situation");
                row.FiscalWithholdingPercentage = Pct(fiscal, "fiscalWithholdingPercentage");
                row.FiscalValidUntil = Date(fiscal, "nonRetainmentExpirationDate");
                row.FiscalAttestId = fiscal.TryGetProperty("attestId", out var a) && a.TryGetInt64(out var attest) ? attest : null;
            }
            if (item.TryGetProperty("socialSection", out var social))
            {
                row.SocialSituation = Str(social, "situation");
                row.SocialWithholdingPercentage = Pct(social, "socialWithholdingPercentage");
                row.SocialValidUntil = Date(social, "nonRetainmentExpirationDate");
            }
        }

        /// <summary>
        /// BE0123.456.789 / 0123456789 / 123456789 → "0123456789" (ondernemingsnummer); buitenlandse nummers (NL815224953B01, RO34040794) blijven staan, in hoofdletters.
        /// Null als er niets bruikbaars overblijft.
        /// </summary>
        public static string NormalizeIdentifier(string vat)
        {
            if (string.IsNullOrWhiteSpace(vat)) return null;
            var s = new string(vat.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (s.StartsWith("BE")) s = s[2..];
            if (s.Length == 0) return null;
            if (s.All(char.IsDigit) && s.Length == 9) s = "0" + s;
            return s;
        }

        // ─── Token ────────────────────────────────────────────────────────────────

        private async Task<string> GetTokenAsync(CancellationToken ct)
        {
            await TokenLock.WaitAsync(ct);
            try
            {
                if (_token != null && _tokenExpiresUtc > DateTime.UtcNow.AddMinutes(1)) return _token;

                using var response = await _http.PostAsync(TokenUrl, new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                    new KeyValuePair<string, string>("client_assertion", BuildClientAssertion()),
                    new KeyValuePair<string, string>("scope", Scope)
                }), ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(body);
                _token = doc.RootElement.GetProperty("access_token").GetString();
                _tokenExpiresUtc = DateTime.UtcNow.AddSeconds(doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 540);
                return _token;
            }
            finally { TokenLock.Release(); }
        }

        /// <summary>Een met onze private sleutel getekende JWT (RS256): iss en sub zijn het Client ID, aud het token-adres.</summary>
        private string BuildClientAssertion()
        {
            static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var header = B64(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" })));
            var payload = B64(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { jti = Guid.NewGuid().ToString(), iss = ClientId, sub = ClientId, aud = TokenUrl, iat = now, exp = now + 300 })));
            var input = header + "." + payload;

            using var rsa = RSA.Create();
            rsa.ImportFromPem(PrivateKeyPem ?? File.ReadAllText(PrivateKeyPath));
            return input + "." + B64(rsa.SignData(Encoding.UTF8.GetBytes(input), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static string Truncate(string s, int max) => s == null ? null : s.Length <= max ? s : s[..max];
        private static string Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        private static byte? Pct(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.TryGetByte(out var b) ? b : null;
        private static DateTime? Date(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && DateTime.TryParse(v.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : null;

        private static RetainmentBatchVm ToBatchVm(Guid batchId, List<SupplierRetainmentCheck> rows) => new()
        {
            BatchId = batchId,
            Name = rows.Select(r => r.BatchName).FirstOrDefault(n => n != null),
            CheckedAt = rows.Min(r => r.CheckedAt),
            CheckedBy = rows.Select(r => r.CheckedBy).FirstOrDefault(),
            Environment = rows.Select(r => r.Environment).FirstOrDefault(),
            Items = rows.Select(ToVm).ToList()
        };

        private static RetainmentCheckVm ToVm(SupplierRetainmentCheck c) => new()
        {
            Id = c.Id,
            IncomingInvoiceId = c.IncomingInvoiceId,
            BatchId = c.BatchId,
            SupplierName = c.SupplierName,
            EnterpriseIdentifier = c.EnterpriseIdentifier,
            EnterpriseNumber = c.EnterpriseNumber,
            Environment = c.Environment,
            CheckedAt = c.CheckedAt,
            CheckedBy = c.CheckedBy,
            RetainmentCheckReference = c.RetainmentCheckReference,
            TraceId = c.TraceId,
            FiscalSituation = c.FiscalSituation,
            FiscalWithholdingPercentage = c.FiscalWithholdingPercentage,
            FiscalValidUntil = c.FiscalValidUntil,
            FiscalAttestId = c.FiscalAttestId,
            SocialSituation = c.SocialSituation,
            SocialWithholdingPercentage = c.SocialWithholdingPercentage,
            SocialValidUntil = c.SocialValidUntil,
            RawResponse = c.RawResponse,
            ErrorMessage = c.ErrorMessage,
            BatchName = c.BatchName
        };
    }
}
