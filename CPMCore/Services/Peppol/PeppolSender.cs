using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services.Peppol
{
    public class PeppolSender : IPeppolSender
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PeppolSender> _logger;
        private readonly string? _endpoint;

        public PeppolSender(HttpClient httpClient, IConfiguration configuration, ILogger<PeppolSender> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _endpoint = configuration["Peppol:SendEndpoint"];
        }

        public async Task<PeppolSendResult> SendAsync(string participantId, string xmlContent, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(participantId))
                throw new ArgumentException("ParticipantId is required", nameof(participantId));
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("XML content is required", nameof(xmlContent));

            if (string.IsNullOrWhiteSpace(_endpoint))
            {
                var docId = $"STUB-{Guid.NewGuid():N}";
                _logger.LogInformation("Peppol sending stubbed for {Participant} with document {DocumentId}", participantId, docId);
                return new PeppolSendResult(true, docId, "Stub delivery (no endpoint configured)");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = new StringContent(xmlContent, Encoding.UTF8, "application/xml")
                };
                request.Headers.Add("X-Peppol-Participant", participantId);

                var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var reason = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Peppol endpoint {Endpoint} returned {StatusCode}: {Reason}", _endpoint, response.StatusCode, reason);
                    return new PeppolSendResult(false, null, reason);
                }

                var documentId = response.Headers.Contains("X-Document-Id")
                    ? string.Join(",", response.Headers.GetValues("X-Document-Id"))
                    : $"DOC-{Guid.NewGuid():N}";
                return new PeppolSendResult(true, documentId, "Sent via configured endpoint");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Peppol send failed for {Participant}", participantId);
                return new PeppolSendResult(false, null, ex.Message);
            }
        }
    }
}