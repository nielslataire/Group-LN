using System.Net;
using BOCore;
using FacadeCore;
using Microsoft.Extensions.Logging;

namespace CPMCore.Services.Octopus
{
    public interface IOctopusTokenManager
    {
        Task<IReadOnlyList<OctopusDossierItem>> GetDossiersWithRetryAsync(int issuerId, CancellationToken ct = default);
        Task<OctopusDossierTokenResult> RefreshDossierTokenAsync(int issuerId, string dossierNumber, CancellationToken ct = default);
        Task<T> ExecuteWithDossierTokensAsync<T>(int issuerId, string dossierNumber, Func<string, string, Task<T>> action, CancellationToken ct = default);
    }

    public class OctopusTokenManager : IOctopusTokenManager
    {
        private readonly IIssuerCompanyService _issuers;
        private readonly IOctopusApiClient _client;
        private readonly ILogger<OctopusTokenManager> _logger;
        private static readonly TimeSpan AuthenticateTokenLifetime = TimeSpan.FromMinutes(9);

        public OctopusTokenManager(IIssuerCompanyService issuers, IOctopusApiClient client, ILogger<OctopusTokenManager> logger)
        {
            _issuers = issuers;
            _client = client;
            _logger = logger;
        }

        public async Task<IReadOnlyList<OctopusDossierItem>> GetDossiersWithRetryAsync(int issuerId, CancellationToken ct = default)
        {
            var issuer = await EnsureIssuerAsync(issuerId, ct);
            var authenticateToken = await EnsureAuthenticateTokenAsync(issuer, ct);

            try
            {
                return await _client.GetDossiersAsync(authenticateToken, ct);
            }
            catch (HttpRequestException ex) when (IsUnauthorized(ex))
            {
                _logger.LogWarning(ex, "Octopus dossierlijst mislukt wegens verlopen token voor issuer {IssuerId}. Token wordt ververst.", issuerId);
                authenticateToken = await RefreshAuthenticateTokenAsync(issuer, ct);
                return await _client.GetDossiersAsync(authenticateToken, ct);
            }
        }

        public async Task<OctopusDossierTokenResult> RefreshDossierTokenAsync(int issuerId, string dossierNumber, CancellationToken ct = default)
        {
            var issuer = await EnsureIssuerAsync(issuerId, ct);
            var authenticateToken = await RefreshAuthenticateTokenIfNeededAsync(issuer, ct);

            try
            {
                return await RequestAndStoreDossierTokenAsync(issuer, authenticateToken, dossierNumber, ct);
            }
            catch (HttpRequestException ex) when (IsUnauthorized(ex))
            {
                _logger.LogWarning(ex, "Octopus dossiertoken verversen mislukt wegens verlopen token voor issuer {IssuerId}. Tokens worden opnieuw opgehaald.", issuerId);
                authenticateToken = await RefreshAuthenticateTokenAsync(issuer, ct);
                return await RequestAndStoreDossierTokenAsync(issuer, authenticateToken, dossierNumber, ct);
            }
        }

        public async Task<T> ExecuteWithDossierTokensAsync<T>(int issuerId, string dossierNumber, Func<string, string, Task<T>> action, CancellationToken ct = default)
        {
            var issuer = await EnsureIssuerAsync(issuerId, ct);
            var authenticateToken = await RefreshAuthenticateTokenIfNeededAsync(issuer, ct);
            var dossierToken = await EnsureDossierTokenAsync(issuer, authenticateToken, dossierNumber, ct);

            try
            {
                return await action(authenticateToken, dossierToken);
            }
            catch (HttpRequestException ex) when (IsUnauthorized(ex))
            {
                _logger.LogWarning(ex, "Octopus call mislukt met 401 voor issuer {IssuerId}. Tokens worden vernieuwd en actie wordt herhaald.", issuerId);
                authenticateToken = await RefreshAuthenticateTokenAsync(issuer, ct);
                dossierToken = (await RequestAndStoreDossierTokenAsync(issuer, authenticateToken, dossierNumber, ct)).Token;
                return await action(authenticateToken, dossierToken);
            }
        }

        private async Task<IssuerCompanyBO> EnsureIssuerAsync(int issuerId, CancellationToken ct)
        {
            var issuer = await _issuers.GetAsync(issuerId, ct);
            if (issuer == null)
            {
                throw new InvalidOperationException($"Issuer met id {issuerId} kon niet worden gevonden.");
            }

            if (string.IsNullOrWhiteSpace(issuer.OctopusUsername) || string.IsNullOrWhiteSpace(issuer.OctopusPassword))
            {
                throw new InvalidOperationException("Octopus gebruikersnaam en wachtwoord ontbreken.");
            }

            return issuer;
        }

        private async Task<string> EnsureAuthenticateTokenAsync(IssuerCompanyBO issuer, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(issuer.OctopusAuthenticateToken) && issuer.OctopusAuthenticateTokenValidUntil > DateTime.UtcNow)
            {
                return issuer.OctopusAuthenticateToken;
            }

            return await RefreshAuthenticateTokenAsync(issuer, ct);
        }

        private async Task<string> RefreshAuthenticateTokenIfNeededAsync(IssuerCompanyBO issuer, CancellationToken ct)
        {
            var token = await EnsureAuthenticateTokenAsync(issuer, ct);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            return await RefreshAuthenticateTokenAsync(issuer, ct);
        }

        private async Task<string> RefreshAuthenticateTokenAsync(IssuerCompanyBO issuer, CancellationToken ct)
        {
            var token = await _client.AuthenticateAsync(issuer.OctopusUsername!, issuer.OctopusPassword!, ct);
            issuer.OctopusAuthenticateToken = token;
            issuer.OctopusAuthenticateTokenValidUntil = DateTime.UtcNow.Add(AuthenticateTokenLifetime);
            await _issuers.UpdateAsync(issuer, ct);
            return token;
        }

        private async Task<string> EnsureDossierTokenAsync(IssuerCompanyBO issuer, string authenticateToken, string dossierNumber, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(issuer.OctopusDossierToken) && issuer.OctopusDossierTokenValidUntil > DateTime.UtcNow && string.Equals(issuer.OctopusDossierNumber, dossierNumber, StringComparison.OrdinalIgnoreCase))
            {
                return issuer.OctopusDossierToken;
            }

            var result = await RequestAndStoreDossierTokenAsync(issuer, authenticateToken, dossierNumber, ct);
            return result.Token;
        }

        private async Task<OctopusDossierTokenResult> RequestAndStoreDossierTokenAsync(IssuerCompanyBO issuer, string authenticateToken, string dossierNumber, CancellationToken ct)
        {
            var tokenResult = await _client.GetDossierTokenAsync(authenticateToken, dossierNumber, ct);
            issuer.OctopusDossierNumber = dossierNumber;
            issuer.OctopusDossierToken = tokenResult.Token;
            issuer.OctopusDossierTokenValidUntil = tokenResult.ValidUntil;
            await _issuers.UpdateAsync(issuer, ct);
            return tokenResult;
        }

        private static bool IsUnauthorized(HttpRequestException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                   || (ex.Message?.Contains("Invalid or expired token", StringComparison.OrdinalIgnoreCase) ?? false)
                   || (ex.Message?.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }
}
