using System;
using System.Linq;
using System.Text;
using ServiceCore.Signing;
using Xunit;

namespace ServiceCore.Tests.Signing;

/// <summary>Tokens, OTP-HMAC, hashes en maskering (ONDERTEKENEN_VOORSTEL.md §6.1, §6.2).</summary>
public class SigningCryptoTests
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("test-key-die-lang-genoeg-is-voor-de-hmac-0123456789");

    [Fact]
    public void GenerateToken_IsBase64UrlOf32Bytes_AndUnique()
    {
        var a = SigningCrypto.GenerateToken();
        var b = SigningCrypto.GenerateToken();
        Assert.Equal(43, a.Length);
        Assert.True(SigningCrypto.LooksLikeToken(a));
        Assert.NotEqual(a, b);
        Assert.DoesNotContain('+', a);
        Assert.DoesNotContain('/', a);
        Assert.DoesNotContain('=', a);
    }

    [Fact]
    public void ComputeFingerprint_IsStable_AndSensitiveToBoundariesAndNulls()
    {
        // Zelfde onderdelen → zelfde vingerafdruk (de bron mag ze op elk moment herberekenen).
        var a = SigningCrypto.ComputeFingerprint(new[] { "12", "Omschrijving", "45.50", null });
        var b = SigningCrypto.ComputeFingerprint(new[] { "12", "Omschrijving", "45.50", null });
        Assert.Equal(a, b);
        Assert.Equal(64, a.Length);

        // Lengteprefix: ("ab","c") ≠ ("a","bc"); null ≠ lege string; volgorde telt.
        Assert.NotEqual(SigningCrypto.ComputeFingerprint(new[] { "ab", "c" }), SigningCrypto.ComputeFingerprint(new[] { "a", "bc" }));
        Assert.NotEqual(SigningCrypto.ComputeFingerprint(new string?[] { null }), SigningCrypto.ComputeFingerprint(new[] { "" }));
        Assert.NotEqual(SigningCrypto.ComputeFingerprint(new[] { "1", "2" }), SigningCrypto.ComputeFingerprint(new[] { "2", "1" }));

        // Eén gewijzigd bedrag → andere vingerafdruk (dat is precies wat SignAsync moet opmerken).
        Assert.NotEqual(a, SigningCrypto.ComputeFingerprint(new[] { "12", "Omschrijving", "45.51", null }));
    }

    [Fact]
    public void HashToken_IsDeterministic_LowercaseHex_AndDiffersFromToken()
    {
        var token = SigningCrypto.GenerateToken();
        var h1 = SigningCrypto.HashToken(token);
        var h2 = SigningCrypto.HashToken(token);
        Assert.Equal(h1, h2);
        Assert.Equal(64, h1.Length);
        Assert.Equal(h1, h1.ToLowerInvariant());
        Assert.NotEqual(token, h1);
        Assert.NotEqual(h1, SigningCrypto.HashToken(SigningCrypto.GenerateToken()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("kort")]
    [InlineData("bevat spaties en te kort")]
    [InlineData("abc$def%ghi&jkl=mno+pqr/stu?vwx#yz0123456789ab")]
    public void LooksLikeToken_RejectsMalformedInput(string raw)
        => Assert.False(SigningCrypto.LooksLikeToken(raw));

    [Fact]
    public void GenerateOtp_HasRequestedLength_AndOnlyDigits()
    {
        for (var i = 0; i < 200; i++)
        {
            var code = SigningCrypto.GenerateOtp(6);
            Assert.Equal(6, code.Length);
            Assert.All(code, c => Assert.True(char.IsDigit(c)));
        }
        Assert.Equal(8, SigningCrypto.GenerateOtp(8).Length);
    }

    [Fact]
    public void OtpHmac_BindsToCasePartyAndRequestTime()
    {
        var at = new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);
        var h = SigningCrypto.ComputeOtpHmac(Key, caseId: 1, partyId: 2, at, "123456");

        Assert.Equal(h, SigningCrypto.ComputeOtpHmac(Key, 1, 2, at, "123456"));
        Assert.Equal(h, SigningCrypto.ComputeOtpHmac(Key, 1, 2, at, "123 456"));          // spaties genormaliseerd
        Assert.NotEqual(h, SigningCrypto.ComputeOtpHmac(Key, 1, 2, at, "123457"));         // andere code
        Assert.NotEqual(h, SigningCrypto.ComputeOtpHmac(Key, 9, 2, at, "123456"));         // ander dossier
        Assert.NotEqual(h, SigningCrypto.ComputeOtpHmac(Key, 1, 9, at, "123456"));         // andere partij
        Assert.NotEqual(h, SigningCrypto.ComputeOtpHmac(Key, 1, 2, at.AddTicks(1), "123456")); // andere cyclus
        Assert.NotEqual(h, SigningCrypto.ComputeOtpHmac(Encoding.UTF8.GetBytes("een-andere-sleutel-van-voldoende-lengte-1234"), 1, 2, at, "123456"));
    }

    [Fact]
    public void FixedTimeEqualsHex_HandlesInvalidAndUnequalInput()
    {
        var a = SigningCrypto.Sha256Hex("x");
        Assert.True(SigningCrypto.FixedTimeEqualsHex(a, a));
        Assert.False(SigningCrypto.FixedTimeEqualsHex(a, SigningCrypto.Sha256Hex("y")));
        Assert.False(SigningCrypto.FixedTimeEqualsHex(a, a[..10]));
        Assert.False(SigningCrypto.FixedTimeEqualsHex(a, "zz" + a[2..]));
        Assert.False(SigningCrypto.FixedTimeEqualsHex(null, a));
    }

    [Theory]
    [InlineData("jan.peeters@voorbeeld.be", "j•••@voorbeeld.be")]
    [InlineData("a@b.be", "a•••@b.be")]
    [InlineData("geen-apenstaart", "•••")]
    [InlineData("", null)]
    public void MaskEmail_KeepsFirstCharAndDomain(string input, string? expected)
        => Assert.Equal(expected, SigningCrypto.MaskEmail(input));

    [Theory]
    [InlineData("+32 476 12 47 82", "•••• 47 82")]
    [InlineData("0476124782", "•••• 47 82")]
    [InlineData("12", "••••")]
    public void MaskPhone_KeepsLastFourDigits(string input, string expected)
        => Assert.Equal(expected, SigningCrypto.MaskPhone(input));

    [Fact]
    public void ComputeEventHash_IgnoresRawIpAndUserAgent_ButNotTheirHashes()
    {
        var at = new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);
        var ipHash = SigningCrypto.HashPii("10.0.0.1");
        var h1 = SigningCrypto.ComputeEventHash(null, 1, 2, "PartySigned", at, 2, null, "Jan", ipHash, null, null, "{}");
        var h2 = SigningCrypto.ComputeEventHash(null, 1, 2, "PartySigned", at, 2, null, "Jan", ipHash, null, null, "{}");
        var h3 = SigningCrypto.ComputeEventHash(null, 1, 2, "PartySigned", at, 2, null, "Jan", SigningCrypto.HashPii("10.0.0.2"), null, null, "{}");
        var h4 = SigningCrypto.ComputeEventHash("abc", 1, 2, "PartySigned", at, 2, null, "Jan", ipHash, null, null, "{}");
        Assert.Equal(h1, h2);
        Assert.NotEqual(h1, h3);
        Assert.NotEqual(h1, h4);
    }

    [Fact]
    public void Sha256Hex_OfBytes_MatchesKnownVector()
    {
        // SHA-256("abc")
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", SigningCrypto.Sha256Hex(Encoding.ASCII.GetBytes("abc")));
    }
}
