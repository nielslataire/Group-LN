using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace CPMCore.Services.Octopus
{
    public interface IOctopusApiClient
    {
        Task<string> AuthenticateAsync(string username, string password, CancellationToken ct = default);

        Task<IReadOnlyList<OctopusDossierItem>> GetDossiersAsync(string authenticateToken, CancellationToken ct = default);

        Task<OctopusDossierTokenResult> GetDossierTokenAsync(string authenticateToken, string dossierNumber, CancellationToken ct = default);
        Task<IReadOnlyList<OctopusBookyearItem>> GetBookyearsAsync(string authenticateToken, string dossierToken, string dossierNumber, CancellationToken ct = default);

        Task<IReadOnlyList<OctopusJournalItem>> GetJournalsAsync(string authenticateToken, string dossierToken, int bookyearKey, string dossierNumber, CancellationToken ct = default);
        Task<IReadOnlyList<OctopusVatCodeItem>> GetVatCodesAsync(string authenticateToken, string dossierToken, string dossierNumber, CancellationToken ct = default);
        Task<OctopusRelation?> FindRelationAsync(string dossierToken, string dossierNumber, OctopusRelationLookup lookup, CancellationToken ct = default);

        Task<OctopusRelation?> UpsertRelationAsync(string dossierToken, string dossierNumber, OctopusRelationRequest payload, CancellationToken ct = default);

        Task CreateInvoiceAsync(string dossierToken, string dossierNumber, OctopusInvoiceCreateRequest payload, CancellationToken ct = default);
    }

    public class OctopusApiClient : IOctopusApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly OctopusOptions _options;
        private readonly ILogger<OctopusApiClient> _logger;

        public OctopusApiClient(HttpClient httpClient, IOptions<OctopusOptions> options, ILogger<OctopusApiClient> logger)
        {
            _httpClient = httpClient;
            _options = options?.Value ?? new OctopusOptions();
            _logger = logger;
        }

        public async Task<string> AuthenticateAsync(
        string username,
        string password,
        CancellationToken ct = default)
        {
            var url = $"{_options.ApiBaseUrl}/authentication";

            var payload = new
            {
                user = username,
                password = password
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };

            // ⚠️ SUPER BELANGRIJK: softwareHouseUuid moet in de HEADER
            request.Headers.Add("softwareHouseUuid", _options.softwareHouseUuid);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new Exception($"Authentication failed: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<OctopusAuthenticationResponse>(cancellationToken: ct);

            return result!.Token;
        }
        public async Task<IReadOnlyList<OctopusDossierItem>> GetDossiersAsync(string authenticateToken, CancellationToken ct = default)
        {
            var url = $"{_options.ApiBaseUrl}/dossiers";
            EnsureUrl(url, "Dossier lijst");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Token", authenticateToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);

            var dossiers = await response.Content.ReadFromJsonAsync<List<OctopusDossierItem>>(cancellationToken: ct)
                ?? new List<OctopusDossierItem>();
            return dossiers;
        }

        public async Task<OctopusDossierTokenResult> GetDossierTokenAsync(string authenticateToken, string dossierNumber, CancellationToken ct = default)
        {
            var url = $"{_options.ApiBaseUrl}/dossiers?dossierId={Uri.EscapeDataString(dossierNumber)}";
            EnsureUrl(url, "Dossier token");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Token", authenticateToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);

            var currenttime = DateTime.UtcNow;
            var body = await response.Content.ReadFromJsonAsync<OctopusDossierTokenResponse>(cancellationToken: ct);
            if (body == null || string.IsNullOrWhiteSpace(body.Dossiertoken))
            {
                throw new InvalidOperationException("Octopus dossier token ontbreekt in het antwoord.");
            }

            return new OctopusDossierTokenResult(body.Dossiertoken!, body.ValidUntil ?? currenttime.AddSeconds(590));
        }

        public async Task<IReadOnlyList<OctopusBookyearItem>> GetBookyearsAsync(string authenticateToken, string dossierToken,string dossierNumber, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/bookyears");
            EnsureUrl(url, "Boekjaren");

            _ = authenticateToken;

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);

            var result = await response.Content.ReadFromJsonAsync<List<OctopusBookyearItem>>(cancellationToken: ct)
                         ?? new List<OctopusBookyearItem>();
            return result;
        }

        public async Task<IReadOnlyList<OctopusJournalItem>> GetJournalsAsync(string authenticateToken, string dossierToken, int bookyearKey, string dossierNumber, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/bookyears/{bookyearKey}/journals");
            EnsureUrl(url, "Journaals");

            _ = authenticateToken;

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);

            var result = await response.Content.ReadFromJsonAsync<List<OctopusJournalItem>>(cancellationToken: ct)
                         ?? new List<OctopusJournalItem>();
            return result;
        }

        public async Task<IReadOnlyList<OctopusVatCodeItem>> GetVatCodesAsync(string authenticateToken, string dossierToken, string dossierNumber, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/vatcodes");
            EnsureUrl(url, "Vatcodes");

            _ = authenticateToken;

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);

            var result = await response.Content.ReadFromJsonAsync<List<OctopusVatCodeItem>>(cancellationToken: ct)
                         ?? new List<OctopusVatCodeItem>();
            return result;
        }

        public async Task<OctopusRelation?> FindRelationAsync(string dossierToken, string dossierNumber, OctopusRelationLookup lookup, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/relations");
            EnsureUrl(url, "Relatie zoeken");

            var query = new Dictionary<string, string?>();

            if (lookup.RelationId.HasValue)
            {
                query["relationId"] = lookup.RelationId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (!string.IsNullOrWhiteSpace(lookup.VatNumber))
            {
                query["vatNr"] = lookup.VatNumber;
            }
            else if (!string.IsNullOrWhiteSpace(lookup.Name))
            {
                query["name"] = lookup.Name;
            }
            else
            {
                return null;
            }

            url = QueryHelpers.AddQueryString(url, query);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            await EnsureSuccessAsync(response, url);

            var relations = await response.Content.ReadFromJsonAsync<List<OctopusRelation>>(cancellationToken: ct)
                           ?? new List<OctopusRelation>();

            return relations.FirstOrDefault();
        }

        public async Task<OctopusRelation?> UpsertRelationAsync(string dossierToken, string dossierNumber, OctopusRelationRequest payload, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/relations");
            EnsureUrl(url, "Relatie opslaan");

            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return payload.ToRelation();
            }

            await EnsureSuccessAsync(response, url);

            var relation = await response.Content.ReadFromJsonAsync<OctopusRelation>(cancellationToken: ct);

            return relation ?? payload.ToRelation();
        }

        public async Task CreateInvoiceAsync(string dossierToken, string dossierNumber, OctopusInvoiceCreateRequest payload, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/invoices");
            EnsureUrl(url, "Factuur aanmaken");

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);
        }


        private static string BuildUrl(string baseUrl, string path)
        {
            baseUrl ??= string.Empty;
            return $"{baseUrl.TrimEnd('/')}/{path}";
        }

        private void EnsureUrl(string url, string context)
        {
            if (string.IsNullOrWhiteSpace(url) || url.EndsWith("/", StringComparison.Ordinal))
            {
                _logger.LogWarning("Octopus {Context} URL ontbreekt of is ongeldig.", context);
                throw new InvalidOperationException($"Octopus {context} URL is niet geconfigureerd.");
            }
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string url)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            var message = string.IsNullOrWhiteSpace(body)
                ? $"Octopus request naar {url} mislukt met status {response.StatusCode}."
                : body;
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }

    public record OctopusAuthenticateResult(string Token, DateTime? ValidUntil);

    public record OctopusDossierTokenResult(string Token, DateTime? ValidUntil);

    public class OctopusDossierItem
    {
        [JsonPropertyName("dossierKey")]
        public OctopusDossierKey? DossierKey { get; set; }

        [JsonPropertyName("dossierDescription")]
        public string? DossierDescription { get; set; }

        [JsonPropertyName("vatNr")]
        public string? VatNumber { get; set; }

        [JsonPropertyName("streetAndNr")]
        public string? StreetAndNumber { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("corporationTypeKey")]
        public OctopusCorporationTypeKey? CorporationTypeKey { get; set; }

        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }

        [JsonIgnore]
        public string? Number => DossierKey?.Id?.ToString(System.Globalization.CultureInfo.InvariantCulture);

        [JsonIgnore]
        public string? Name => DossierDescription;
    }

    public class OctopusDossierKey
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }
    }

    public class OctopusCorporationTypeKey
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }
    }

    public class OctopusBookyearItem
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKey? BookyearKey { get; set; }

        [JsonPropertyName("bookyearDescription")]
        public string? BookyearDescription { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("closed")]
        public bool Closed { get; set; }

        [JsonPropertyName("periods")]
        public List<OctopusBookyearPeriodItem> Periods { get; set; } = new();
    }

    public class OctopusBookyearKey
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class OctopusBookyearPeriodItem
    {
        [JsonPropertyName("bookyearPeriod")]
        public int BookyearPeriod { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }
    }

    public class OctopusJournalItem
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKey? BookyearKey { get; set; }

        [JsonPropertyName("journalKey")]
        public string? JournalKey { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("closed")]
        public bool Closed { get; set; }

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("lastBookedDocumentNr")]
        public int? LastBookedDocumentNr { get; set; }

        [JsonPropertyName("protectedPeriod")]
        public int? ProtectedPeriod { get; set; }

        [JsonPropertyName("closedPeriod")]
        public int? ClosedPeriod { get; set; }

        [JsonPropertyName("insertionType")]
        public int? InsertionType { get; set; }

        [JsonPropertyName("customFieldList")]
        public List<OctopusCustomFieldItem> CustomFieldList { get; set; } = new();

        [JsonPropertyName("customFieldLineList")]
        public List<OctopusCustomFieldItem> CustomFieldLineList { get; set; } = new();
    }

    public class OctopusVatCodeItem
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("basePercentage")]
        public decimal BasePercentage { get; set; }

        [JsonPropertyName("defaultSellBookingAccountNr")]
        public int? DefaultSellBookingAccountNr { get; set; }
    }

    public class OctopusCustomFieldItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class OctopusRelation
    {
        [JsonPropertyName("relationIdentificationServiceData")]
        public OctopusRelationIdentificationData? RelationIdentificationServiceData { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("streetAndNr")]
        public string? StreetAndNr { get; set; }

        [JsonPropertyName("postalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("telephone")]
        public string? Telephone { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("vatNr")]
        public string? VatNr { get; set; }

        [JsonPropertyName("client")]
        public bool Client { get; set; }

        [JsonPropertyName("supplier")]
        public bool Supplier { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }
    }

    public class OctopusRelationRequest : OctopusRelation
    {
        [JsonPropertyName("relationIdentificationServiceData")]
        public new OctopusRelationIdentificationData? RelationIdentificationServiceData { get; set; }

        [JsonPropertyName("defaultBookingAccountClient")]
        public int DefaultBookingAccountClient { get; set; }

        [JsonPropertyName("defaultBookingAccountSupplier")]
        public int DefaultBookingAccountSupplier { get; set; }

        [JsonPropertyName("supplierPaymentMethod")]
        public int SupplierPaymentMethod { get; set; }

        public OctopusRelation ToRelation()
        {
            return new OctopusRelation
            {
                RelationIdentificationServiceData = RelationIdentificationServiceData,
                Name = Name,
                StreetAndNr = StreetAndNr,
                PostalCode = PostalCode,
                City = City,
                Country = Country,
                Telephone = Telephone,
                Email = Email,
                VatNr = VatNr,
                Client = Client,
                Supplier = Supplier,
                Active = Active
            };
        }
    }

    public class OctopusRelationIdentificationData
    {
        [JsonPropertyName("relationKey")]
        public OctopusRelationKey? RelationKey { get; set; }

        [JsonPropertyName("externalRelationId")]
        public int? ExternalRelationId { get; set; }
    }

    public class OctopusRelationKey
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }
    }

    public class OctopusRelationLookup
    {
        public int? RelationId { get; set; }
        public string? Name { get; set; }
        public string? VatNumber { get; set; }
    }


    public class OctopusAuthenticationRequest
    {
        [JsonPropertyName("user")]
        public string User { get; set; } = default!;
        [JsonPropertyName("password")]
        public string Password { get; set; } = default!;
    }

    public class OctopusAuthenticationResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = default!;
    }

    internal class OctopusDossierTokenResponse
    {
        [JsonPropertyName("Dossiertoken")]
        public string? Dossiertoken { get; set; }

        [JsonPropertyName("validUntil")]
        public DateTime? ValidUntil { get; set; }
    }
}
