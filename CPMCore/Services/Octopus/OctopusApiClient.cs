using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Globalization;

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
        Task<IReadOnlyList<OctopusCustomFieldResponse>> GetCustomFieldsAsync(string dossierToken, string dossierNumber, CancellationToken ct = default);
        Task<OctopusRelation?> FindRelationAsync(string dossierToken, string dossierNumber, OctopusRelationLookup lookup, CancellationToken ct = default);
        Task<IReadOnlyList<OctopusRelation>> GetRelationsAsync(string dossierToken, string dossierNumber, CancellationToken ct = default);
        Task<OctopusRelation?> UpsertRelationAsync(string dossierToken, string dossierNumber, OctopusRelationRequest payload, CancellationToken ct = default);

        Task<bool> CreateInvoiceAsync(string dossierToken, string dossierNumber, OctopusInvoiceCreateRequest payload, CancellationToken ct = default);
        Task<bool> CreateBuySellBookingAsync(string dossierToken, string dossierNumber, OctopusBuySellBookingAndAttachmentRequest payload, CancellationToken ct = default);
        Task<bool> UploadInvoiceAttachmentAsync(string dossierToken, string dossierNumber, OctopusInvoiceAttachmentUploadRequest payload, CancellationToken ct = default);
        Task<OctopusInvoiceBookResponse> BookInvoiceAsync(
          string dossierToken,
          string dossierNumber,
          int bookyearId,
          string journalKey,
          int toDocumentSequenceNr,
          bool attachUbl = false,
          CancellationToken ct = default);

        Task<OctopusInvoiceSendResponse> SendInvoiceAsync(string dossierToken, string dossierNumber, OctopusInvoiceSendRequest payload, CancellationToken ct = default);

        Task<IReadOnlyList<OctopusDocumentDeliveryState>> GetInvoiceDeliveryStatesAsync(string dossierToken, string dossierNumber, OctopusDocumentSelectionData payload, CancellationToken ct = default);

        // ─── Aankoopfacturen via buysellbookings/modified ─────────────────────────
        // GET /dossiers/{dossierId}/buysellbookings/modified
        // bookyearId = -1 haalt alle boekjaren op. modifiedSince = 1980-01-01 voor initiële sync.
        // Max 48 calls/dag — gebruik incrementele sync via modifiedSince.
        Task<IReadOnlyList<OctopusBuySellBookingItem>> GetPurchaseInvoicesAsync(
            string dossierToken,
            string dossierNumber,
            int bookyearId,
            string journalKey,
            DateTime modifiedSince,
            CancellationToken ct = default);

        // ─── Booking File Sync API (https://service.inaras.be/octopus-filesync-api/v1) ──
        // GET /dossiers/{dossierId}/bookingfiles  — gewijzigde bestandslijst
        Task<IReadOnlyList<OctopusBookingFileItem>> GetBookingFilesAsync(
            string dossierToken,
            string dossierNumber,
            DateTime modifiedSince,
            int? bookyearId = null,
            string journal = null,
            int? documentSequenceNr = null,
            CancellationToken ct = default);

        // POST /dossiers/{dossierId}/bookingfiles  — download bijlage (octet-stream)
        Task<OctopusPurchaseInvoiceAttachment> DownloadBookingFileAsync(
            string dossierToken,
            string dossierNumber,
            OctopusBookingFileItem fileItem,
            CancellationToken ct = default);
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
        public async Task<IReadOnlyList<OctopusCustomFieldResponse>> GetCustomFieldsAsync(string dossierToken, string dossierNumber, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/customfields");
            EnsureUrl(url, "Custom fields");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);

            var result = await response.Content.ReadFromJsonAsync<List<OctopusCustomFieldResponse>>(cancellationToken: ct)
                ?? new List<OctopusCustomFieldResponse>();

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
        public async Task<IReadOnlyList<OctopusRelation>> GetRelationsAsync(string dossierToken, string dossierNumber, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/relations");
            EnsureUrl(url, "Relaties ophalen");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Array.Empty<OctopusRelation>();
            }

            await EnsureSuccessAsync(response, url);

            var relations = await response.Content.ReadFromJsonAsync<List<OctopusRelation>>(cancellationToken: ct)
                           ?? new List<OctopusRelation>();

            return relations;
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

        public async Task<bool> CreateInvoiceAsync(string dossierToken, string dossierNumber, OctopusInvoiceCreateRequest payload, CancellationToken ct = default)
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
            return response.StatusCode == HttpStatusCode.OK
                || response.StatusCode == HttpStatusCode.Created
                || response.StatusCode == HttpStatusCode.NoContent;
        }

        public async Task<OctopusInvoiceBookResponse> BookInvoiceAsync(
                   string dossierToken,
                   string dossierNumber,
                   int bookyearId,
                   string journalKey,
                   int toDocumentSequenceNr,
                   bool attachUbl = false,
                   CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/invoices/book");
            EnsureUrl(url, "Factuur doorboeken");

            var query = new Dictionary<string, string?>
            {
                ["bookyearId"] = bookyearId.ToString(CultureInfo.InvariantCulture),
                ["journalKey"] = journalKey,
                ["toDocumentSeqNr"] = toDocumentSequenceNr.ToString(CultureInfo.InvariantCulture),
                ["attachUbl"] = attachUbl ? "true" : "false"
            };

            url = QueryHelpers.AddQueryString(url, query);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureBookingSuccessAsync(response, url);

            var result = await response.Content.ReadFromJsonAsync<OctopusInvoiceBookResponse>(cancellationToken: ct);
            return result ?? new OctopusInvoiceBookResponse();
        }


        public async Task<bool> CreateBuySellBookingAsync(string dossierToken, string dossierNumber, OctopusBuySellBookingAndAttachmentRequest payload, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/buysellbookings");
            EnsureUrl(url, "Boeking aanmaken");

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };

            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);
            return response.StatusCode == HttpStatusCode.OK
                || response.StatusCode == HttpStatusCode.Created
                || response.StatusCode == HttpStatusCode.NoContent;
        }
        public async Task<bool> UploadInvoiceAttachmentAsync(string dossierToken, string dossierNumber, OctopusInvoiceAttachmentUploadRequest payload, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(payload);

            if (string.IsNullOrWhiteSpace(payload.JournalKey))
                throw new ArgumentException("JournalKey is verplicht voor het uploaden van een factuurbijlage.", nameof(payload));

            if (payload.BookyearId <= 0)
                throw new ArgumentException("BookyearId is verplicht voor het uploaden van een factuurbijlage.", nameof(payload));

            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/invoices/attachments/upload");
            EnsureUrl(url, "Factuur bijlage uploaden");

            var query = new Dictionary<string, string?>
            {
                ["bookyearId"] = payload.BookyearId.ToString(CultureInfo.InvariantCulture),
                ["journal"] = payload.JournalKey,
                ["documentSeqNr"] = payload.DocumentSequenceNumber.ToString(CultureInfo.InvariantCulture)
            };

            url = QueryHelpers.AddQueryString(url, query);

            // Octopus verwacht raw binary als application/octet-stream (geen multipart form).
            var fileContent = new ByteArrayContent(payload.Content ?? Array.Empty<byte>());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = fileContent
            };
            request.Headers.Add("dossierToken", dossierToken);
            request.Headers.Add("fileName", string.IsNullOrWhiteSpace(payload.FileName) ? "invoice.pdf" : payload.FileName);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);
            return response.StatusCode == HttpStatusCode.NoContent
                || response.StatusCode == HttpStatusCode.OK;
        }

        public async Task<OctopusInvoiceSendResponse> SendInvoiceAsync(string dossierToken, string dossierNumber, OctopusInvoiceSendRequest payload, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/invoices/send");
            EnsureUrl(url, "Factuur verzenden");

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);

            var sendResponse = await response.Content.ReadFromJsonAsync<OctopusInvoiceSendResponse>(cancellationToken: ct)
                ?? new OctopusInvoiceSendResponse { Success = true };

            return sendResponse;
        }

        public async Task<IReadOnlyList<OctopusDocumentDeliveryState>> GetInvoiceDeliveryStatesAsync(string dossierToken, string dossierNumber, OctopusDocumentSelectionData payload, CancellationToken ct = default)
        {
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/invoices/send/report/deliverystate");
            EnsureUrl(url, "Factuur afleverstatus");

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);
            await EnsureSuccessAsync(response, url);

            var result = await response.Content.ReadFromJsonAsync<List<OctopusDocumentDeliveryState>>(cancellationToken: ct)
                         ?? new List<OctopusDocumentDeliveryState>();

            return result;
        }

        // ─── Aankoopfacturen ──────────────────────────────────────────────────────

        public async Task<IReadOnlyList<OctopusBuySellBookingItem>> GetPurchaseInvoicesAsync(
            string dossierToken,
            string dossierNumber,
            int bookyearId,
            string journalKey,
            DateTime modifiedSince,
            CancellationToken ct = default)
        {
            // GET /dossiers/{dossierId}/buysellbookings/modified
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/buysellbookings/modified");
            EnsureUrl(url, "Aankoopfacturen ophalen");

            var query = new Dictionary<string, string?>
            {
                ["bookyearId"] = bookyearId.ToString(CultureInfo.InvariantCulture),
                ["modifiedTimeStamp"] = modifiedSince.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
            };

            if (!string.IsNullOrWhiteSpace(journalKey))
                query["journalKey"] = journalKey;

            url = QueryHelpers.AddQueryString(url, query);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Array.Empty<OctopusBuySellBookingItem>();

            await EnsureSuccessAsync(response, url);

            var result = await response.Content.ReadFromJsonAsync<List<OctopusBuySellBookingItem>>(cancellationToken: ct)
                         ?? new List<OctopusBuySellBookingItem>();
            return result;
        }

        public async Task<OctopusPurchaseInvoiceAttachment> GetPurchaseInvoiceAttachmentAsync(
            string dossierToken,
            string dossierNumber,
            int documentSequenceNr,
            CancellationToken ct = default)
        {
            // TODO: Vervang het endpoint-pad door het juiste Octopus REST-pad voor bijlagen bij aankoopfacturen.
            //       Vermoedelijk: dossiers/{dossierNumber}/purchaseinvoices/{documentSequenceNr}/attachment
            var url = BuildUrl(_options.ApiBaseUrl, $"dossiers/{dossierNumber}/purchaseinvoices/{documentSequenceNr}/attachment");
            EnsureUrl(url, "Aankoopfactuur bijlage ophalen");

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            await EnsureSuccessAsync(response, url);

            var content = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"invoice_{documentSequenceNr}.pdf";

            return new OctopusPurchaseInvoiceAttachment
            {
                FileName = fileName,
                ContentType = contentType,
                Content = content
            };
        }

        // ─── Booking File Sync API ────────────────────────────────────────────────

        public async Task<IReadOnlyList<OctopusBookingFileItem>> GetBookingFilesAsync(
            string dossierToken,
            string dossierNumber,
            DateTime modifiedSince,
            int? bookyearId = null,
            string journal = null,
            int? documentSequenceNr = null,
            CancellationToken ct = default)
        {
            var url = BuildUrl(_options.FileSyncApiBaseUrl, $"dossiers/{dossierNumber}/bookingfiles");
            EnsureUrl(url, "Booking bestandslijst ophalen");

            var query = new Dictionary<string, string?>
            {
                ["modifiedTimeStamp"] = modifiedSince.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            };
            if (bookyearId.HasValue)
                query["bookyearId"] = bookyearId.Value.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(journal))
                query["journal"] = journal;
            if (documentSequenceNr.HasValue)
                query["documentSequenceNr"] = documentSequenceNr.Value.ToString(CultureInfo.InvariantCulture);

            url = QueryHelpers.AddQueryString(url, query);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Array.Empty<OctopusBookingFileItem>();

            await EnsureSuccessAsync(response, url);

            var result = await response.Content.ReadFromJsonAsync<List<OctopusBookingFileItem>>(cancellationToken: ct)
                         ?? new List<OctopusBookingFileItem>();
            return result;
        }

        public async Task<OctopusPurchaseInvoiceAttachment> DownloadBookingFileAsync(
            string dossierToken,
            string dossierNumber,
            OctopusBookingFileItem fileItem,
            CancellationToken ct = default)
        {
            var url = BuildUrl(_options.FileSyncApiBaseUrl, $"dossiers/{dossierNumber}/bookingfiles");
            EnsureUrl(url, "Booking bestand downloaden");

            // POST met het exacte SyncFileKeyBooking object dat de GET teruggaf
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(fileItem)
            };
            request.Headers.Add("dossierToken", dossierToken);

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            await EnsureSuccessAsync(response, url);

            var content = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = fileItem.FileName ?? "bijlage.pdf";

            return new OctopusPurchaseInvoiceAttachment
            {
                FileName = fileName,
                ContentType = contentType,
                Content = content
            };
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

        private static async Task EnsureBookingSuccessAsync(HttpResponseMessage response, string url)
        {
            if (response.IsSuccessStatusCode)
                return;

            var apiMessage = await TryReadErrorMessageAsync(response);

            var contextMessage = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Ongeldige of ontbrekende parameters bij het doorboeken.",
                HttpStatusCode.Unauthorized => "Ongeldig of verlopen token. Meld u opnieuw aan bij Octopus.",
                HttpStatusCode.Forbidden => "U hebt geen recht om verkoopfacturen door te boeken in Octopus.",
                _ => null
            };

            string message;
            if (apiMessage != null && contextMessage != null)
                message = $"{contextMessage} Details: {apiMessage}";
            else
                message = apiMessage ?? contextMessage ?? $"Octopus doorboeken mislukt met status {(int)response.StatusCode}.";

            throw new HttpRequestException(message, null, response.StatusCode);
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string url)
        {
            if (response.IsSuccessStatusCode)
                return;

            var message = await TryReadErrorMessageAsync(response)
                ?? $"Octopus request mislukt met status {(int)response.StatusCode}.";

            throw new HttpRequestException(message, null, response.StatusCode);
        }

        // Octopus geeft bij fouten een ErrorResult JSON terug: { errorCode, errorMessage, technicalInfo }
        private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                var error = System.Text.Json.JsonSerializer.Deserialize<OctopusErrorResult>(body);
                if (!string.IsNullOrWhiteSpace(error?.ErrorMessage))
                    return error.ErrorMessage;
            }
            catch
            {
                // Geen geldige JSON — geef ruwe body terug
            }

            return body;
        }

        // ─── Purchase invoice response models (inkomende facturen) ───────────────
        // TODO: Pas deze klassen aan zodra het exacte Octopus-responseschema bekend is.

        private class OctopusErrorResult
        {
            [JsonPropertyName("errorCode")]
            public int? ErrorCode { get; set; }

            [JsonPropertyName("errorMessage")]
            public string? ErrorMessage { get; set; }

            [JsonPropertyName("technicalInfo")]
            public string? TechnicalInfo { get; set; }
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

    public class OctopusCustomFieldResponse
    {
        [JsonPropertyName("customFieldKey")]
        public OctopusCustomFieldKey? CustomFieldKey { get; set; }

        [JsonPropertyName("description")]
        public OctopusCustomFieldDescription? Description { get; set; }

        [JsonPropertyName("customFieldType")]
        public int CustomFieldType { get; set; }

        [JsonPropertyName("templateCode")]
        public string? TemplateCode { get; set; }
    }

    public class OctopusCustomFieldKey
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class OctopusCustomFieldDescription
    {
        [JsonPropertyName("description_NL")]
        public string? DescriptionNl { get; set; }

        [JsonPropertyName("description_FR")]
        public string? DescriptionFr { get; set; }

        [JsonPropertyName("description_EN")]
        public string? DescriptionEn { get; set; }

        [JsonPropertyName("description_DE")]
        public string? DescriptionDe { get; set; }
    }

    public class OctopusRelation
    {
        [JsonPropertyName("relationIdentificationServiceData")]
        public OctopusRelationIdentificationData? RelationIdentificationServiceData { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("streetAndNr")]
        public string? StreetAndNr { get; set; }
        [JsonPropertyName("firstName")]
        public string? Firstname { get; set; }
        [JsonPropertyName("defaultBookingAccountClient")]
        public int? DefaultBookingAccountClient { get; set; }

        [JsonPropertyName("defaultBookingAccountSupplier")]
        public int? DefaultBookingAccountSupplier { get; set; }

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
        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("ibanAccountNr")]
        public string? IbanAccountNr { get; set; }

        [JsonPropertyName("bicCode")]
        public string? BicCode { get; set; }

        [JsonPropertyName("vatNr")]
        public string? VatNr { get; set; }
        [JsonPropertyName("vatType")]
        public int? VatType { get; set; }
        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("contactPerson")]
        public string? Contactperson { get; set; }

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

        [JsonPropertyName("vatType")]
        public new int? VatType { get; set; }

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
                Firstname = Firstname,
                PostalCode = PostalCode,
                City = City,
                Country = Country,
                Telephone = Telephone,
                Email = Email,
                VatNr = VatNr,
                VatType = VatType,
                CurrencyCode = CurrencyCode,
                Contactperson = Contactperson,
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

        // validUntil staat niet in de Octopus swagger spec — wordt nooit gevuld door de API.
        // De caller gebruikt DateTime.UtcNow.AddSeconds(590) als fallback.
        [JsonPropertyName("validUntil")]
        public DateTime? ValidUntil { get; set; }
    }

    // ─── BuySellBookings response models (GET /buysellbookings/modified) ────────

    /// <summary>
    /// Octopus BuySellBookingServiceData — response van GET /dossiers/{id}/buysellbookings/modified.
    /// Bevat zowel aankoop- als verkoopboekingen; filter op journalKey om enkel aankopen te bewaren.
    /// </summary>
    public class OctopusBuySellBookingItem
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKey? BookyearKey { get; set; }

        [JsonPropertyName("journalKey")]
        public string? JournalKey { get; set; }

        /// <summary>Document number identification — deel van de unieke sleutel.</summary>
        [JsonPropertyName("documentSequenceNr")]
        public int DocumentSequenceNr { get; set; }

        [JsonPropertyName("relationIdentificationServiceData")]
        public OctopusRelationIdentificationData? RelationIdentificationServiceData { get; set; }

        [JsonPropertyName("bookyearPeriodeNr")]
        public int BookyearPeriodeNr { get; set; }

        [JsonPropertyName("documentDate")]
        public DateOnly DocumentDate { get; set; }

        [JsonPropertyName("expiryDate")]
        public DateOnly ExpiryDate { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        [JsonPropertyName("orderReference")]
        public string? OrderReference { get; set; }

        /// <summary>Factuurnummer van de leverancier.</summary>
        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        /// <summary>Totaal boekingsbedrag (incl. BTW).</summary>
        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchangeRate")]
        public double ExchangeRate { get; set; }

        [JsonPropertyName("bookingLines")]
        public List<OctopusBuySellBookingLineItem>? BookingLines { get; set; }

        /// <summary>0=Unknown,1=Bank Transfer,2=Cash,3=Creditcard,4=Domiciliëring,11=For approval,12=Blocked…</summary>
        [JsonPropertyName("paymentMethod")]
        public int PaymentMethod { get; set; }

        // ── Computed helpers ──────────────────────────────────────────────────
        [JsonIgnore]
        public string ExternalId => $"{BookyearKey?.Id}_{JournalKey}_{DocumentSequenceNr}";

        [JsonIgnore]
        public int? SupplierRelationKey => RelationIdentificationServiceData?.RelationKey?.Id;

        [JsonIgnore]
        public decimal TotalAmountDecimal => (decimal)Amount;

        [JsonIgnore]
        public decimal VatAmountDecimal =>
            (decimal)(BookingLines?.Sum(l => l.VatAmount) ?? 0);

        [JsonIgnore]
        public decimal NetAmountDecimal =>
            (decimal)(BookingLines?.Sum(l => l.BaseAmount) ?? 0);

        /// <summary>Leveranciersfactuurnummer: Reference indien gevuld, anders OrderReference.</summary>
        [JsonIgnore]
        public string InvoiceNumber =>
            !string.IsNullOrWhiteSpace(Reference) ? Reference :
            !string.IsNullOrWhiteSpace(OrderReference) ? OrderReference :
            DocumentSequenceNr.ToString(CultureInfo.InvariantCulture);
    }

    public class OctopusBuySellBookingLineItem
    {
        [JsonPropertyName("accountKey")]
        public int AccountKey { get; set; }

        [JsonPropertyName("baseAmount")]
        public double BaseAmount { get; set; }

        [JsonPropertyName("vatCodeKey")]
        public string? VatCodeKey { get; set; }

        [JsonPropertyName("vatAmount")]
        public double VatAmount { get; set; }

        [JsonPropertyName("comment")]
        public string? Comment { get; set; }
    }

    public class OctopusPurchaseInvoiceAttachment
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }

    // ─── Booking File Sync API models ─────────────────────────────────────────

    /// <summary>
    /// Bestandsverwijzing vanuit GET /bookingfiles of als request body voor POST /bookingfiles.
    /// Gebruik het object exact zoals je het van de GET ontvangen hebt als POST body voor download.
    /// </summary>
    public class OctopusBookingFileItem
    {
        [JsonPropertyName("documentKey")]
        public OctopusBookingDocumentKey? DocumentKey { get; set; }

        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        /// <summary>Submap-pad binnen het Octopus-archief (meesturen bij POST download).</summary>
        [JsonPropertyName("fileSubDirectoryList")]
        public List<string>? FileSubDirectoryList { get; set; }
    }

    public class OctopusBookingDocumentKey
    {
        [JsonPropertyName("bookyearKey")]
        public OctopusBookyearKey? BookyearKey { get; set; }

        /// <summary>Journaaltype (integer, intern Octopus-veld).</summary>
        [JsonPropertyName("journalType")]
        public int JournalType { get; set; }

        /// <summary>Interne journaalvolgnummer (niet hetzelfde als journalKey string).</summary>
        [JsonPropertyName("journalSequenceNr")]
        public int JournalSequenceNr { get; set; }

        [JsonPropertyName("documentSequenceNr")]
        public int DocumentSequenceNr { get; set; }
    }
}
