using FacadeCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace CPMCore.Services;

public class ResendInviteUrlBuilder : IResendInviteUrlBuilder
{
    private readonly byte[] _key;
    private const int ValidDays = 30;

    public ResendInviteUrlBuilder(IConfiguration config)
    {
        var secret = config["App:ResendInviteSecret"] ?? "GroupLN-ResendInvite-Fallback";
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string? Build(string email, int companyId, string appBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(appBaseUrl)) return null;

        var expiresMs = DateTimeOffset.UtcNow.AddDays(ValidDays).ToUnixTimeMilliseconds();
        var sig = Sign(email, companyId, expiresMs);

        return $"{appBaseUrl.TrimEnd('/')}/Uitnodiging/Hernieuw" +
               $"?e={Uri.EscapeDataString(email)}&c={companyId}&x={expiresMs}&s={sig}";
    }

    public bool TryValidate(string email, int companyId, long expiresUtcMs, string signature)
    {
        if (DateTimeOffset.FromUnixTimeMilliseconds(expiresUtcMs) < DateTimeOffset.UtcNow)
            return false;

        try
        {
            var expected = Sign(email, companyId, expiresUtcMs);
            var expectedBytes = FromBase64Url(expected);
            var actualBytes  = FromBase64Url(signature);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch
        {
            return false;
        }
    }

    private string Sign(string email, int companyId, long expiresMs)
    {
        var payload = Encoding.UTF8.GetBytes(
            $"{email.Trim().ToLowerInvariant()}|{companyId}|{expiresMs}");
        return ToBase64Url(HMACSHA256.HashData(_key, payload));
    }

    private static string ToBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] FromBase64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
}
