using System.Security.Cryptography;
using GroupLN.MarketData.Infrastructure.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

#pragma warning disable CA1416

namespace GroupLN.MarketData.Infrastructure.Tests.Services;

public class PhotoHashUtilsTests
{
    // ── NormalizeImageUrl ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(
        "https://files.zimmo.be/listings/12345/images/736x736/photo.jpg",
        "https://files.zimmo.be/listings/12345/images/photo.jpg")]
    [InlineData(
        "https://files.zimmo.be/listings/12345/images/300x300/photo.jpg",
        "https://files.zimmo.be/listings/12345/images/photo.jpg")]
    [InlineData(
        "https://cdn.immowebstatic.be/classifieds/99999/736x736/img.webp",
        "https://cdn.immowebstatic.be/classifieds/99999/img.webp")]
    public void NormalizeImageUrl_StripsResolutionSegment(string input, string expected)
    {
        var result = ProjectPhotoHashService.NormalizeImageUrl(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeImageUrl_NoResolutionSegment_Unchanged()
    {
        const string url = "https://files.zimmo.be/listings/12345/images/photo.jpg";
        Assert.Equal(url, ProjectPhotoHashService.NormalizeImageUrl(url));
    }

    [Fact]
    public void NormalizeImageUrl_Empty_ReturnsEmpty()
    {
        Assert.Equal("", ProjectPhotoHashService.NormalizeImageUrl(""));
    }

    // ── HammingDistance ───────────────────────────────────────────────────────

    [Fact]
    public void HammingDistance_SameValue_IsZero()
    {
        var dist = ProjectPhotoHashService.HammingDistance(0x123456789ABCDEF0L, 0x123456789ABCDEF0L);
        Assert.Equal(0, dist);
    }

    [Fact]
    public void HammingDistance_AllBitsFlipped_Is64()
    {
        // XOR of value with its complement = all 1s → popcount = 64
        long val = unchecked((long)0xAAAAAAAAAAAAAAAAUL);
        long flip = unchecked((long)0x5555555555555555UL);
        var dist = ProjectPhotoHashService.HammingDistance(val, flip);
        Assert.Equal(64, dist);
    }

    [Fact]
    public void HammingDistance_OneBitDifference_Is1()
    {
        var dist = ProjectPhotoHashService.HammingDistance(0L, 1L);
        Assert.Equal(1, dist);
    }

    [Fact]
    public void HammingDistance_WithinThreshold_Detected()
    {
        // Two hashes that differ by exactly 3 bits should be within threshold 8
        long hashA = 0b_0000_0000_0000_0000L;
        long hashB = 0b_0000_0000_0000_0111L; // 3 bits set
        var dist = ProjectPhotoHashService.HammingDistance(hashA, hashB);
        Assert.True(dist <= 8, $"Distance {dist} should be within threshold 8");
    }

    // ── SHA256 content hash ───────────────────────────────────────────────────

    [Fact]
    public void Sha256_SameBytes_ProduceSameHash()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 100, 200 };
        var h1 = Convert.ToHexString(SHA256.HashData(data));
        var h2 = Convert.ToHexString(SHA256.HashData(data));
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Sha256_DifferentBytes_ProduceDifferentHash()
    {
        var h1 = Convert.ToHexString(SHA256.HashData(new byte[] { 1, 2, 3 }));
        var h2 = Convert.ToHexString(SHA256.HashData(new byte[] { 1, 2, 4 }));
        Assert.NotEqual(h1, h2);
    }

    // ── ComputeDHash ──────────────────────────────────────────────────────────

    [Fact]
    public void ComputeDHash_IdenticalImages_ProduceSameHash()
    {
        var img1 = CreateTestImage(50, 50, 128);
        var img2 = CreateTestImage(50, 50, 128);
        Assert.Equal(
            ProjectPhotoHashService.ComputeDHash(img1),
            ProjectPhotoHashService.ComputeDHash(img2));
    }

    [Fact]
    public void ComputeDHash_HammingDistanceOfIdenticalImages_IsZero()
    {
        var img = CreateTestImage(50, 50, 64);
        var hash = ProjectPhotoHashService.ComputeDHash(img);
        Assert.Equal(0, ProjectPhotoHashService.HammingDistance(hash, hash));
    }

    [Fact]
    public void ComputeDHash_VerySimilarImages_SmallHammingDistance()
    {
        // Nearly identical solid-colour images produce same hash even after resize
        var img1 = CreateTestImage(100, 100, 200);
        var img2 = CreateTestImage(100, 100, 200);
        img2.Mutate(x => x.Resize(99, 99).Resize(100, 100));

        var dist = ProjectPhotoHashService.HammingDistance(
            ProjectPhotoHashService.ComputeDHash(img1),
            ProjectPhotoHashService.ComputeDHash(img2));
        Assert.True(dist <= 8, $"Hamming distance {dist} exceeds threshold 8");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Image<L8> CreateTestImage(int w, int h, byte luminance)
        => new(w, h, new L8(luminance));
}
