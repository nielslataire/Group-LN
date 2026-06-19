using System.Reflection;
using GroupLN.MarketData.Core.Entities;
using GroupLN.MarketData.Core.Settings;
using GroupLN.MarketData.Infrastructure.Deduplication;
using Xunit;

namespace GroupLN.MarketData.Infrastructure.Tests.Deduplication;

/// <summary>
/// Tests voor de foto-hash scoring in ScoreProjectPair (private static via reflectie).
/// </summary>
public class DeduplicationPhotoScoringTests
{
    private static readonly MethodInfo ScoreMethod = typeof(DeduplicationService)
        .GetMethod("ScoreProjectPair", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ScoreProjectPair method not found via reflection");

    private static (double Score, string Level, object Fields) Invoke(
        string normA, List<string> fpA, List<string> photoContentA, MarketAsset a,
        string normB, List<string> fpB, List<string> photoContentB, MarketAsset b,
        PhotoHashingSettings ph,
        List<long>? perceptualA = null, List<long>? perceptualB = null)
    {
        var result = ScoreMethod.Invoke(null,
        [
            normA, fpA, photoContentA, perceptualA ?? [],
            a,
            normB, fpB, photoContentB, perceptualB ?? [],
            b,
            ph, null   // logger = null
        ])!;
        // ValueTuple<double, string, object>
        var type = result.GetType();
        var score  = (double)type.GetField("Item1")!.GetValue(result)!;
        var level  = (string)type.GetField("Item2")!.GetValue(result)!;
        var fields = type.GetField("Item3")!.GetValue(result)!;
        return (score, level, fields);
    }

    private static MarketAsset MakeAsset(long id, string? postal = null, string? street = null,
        decimal? lat = null, decimal? lon = null) => new()
    {
        Id = id,
        PostalCode = postal,
        Street = street,
        Latitude = lat,
        Longitude = lon,
        ChildUnits = new List<MarketAsset>()
    };

    private static PhotoHashingSettings PhotoEnabled => new()
    {
        EnableProjectPhotoHashing = true,
        PhotoPerceptualHashMaxDistance = 8
    };

    private static PhotoHashingSettings PhotoDisabled => new()
    {
        EnableProjectPhotoHashing = false
    };

    // ── Foto-match combo D ────────────────────────────────────────────────────

    [Fact]
    public void TwoPhotoMatches_SamePostal_SameStreet_LevelsExact()
    {
        var shared = new List<string> { "HASH_A", "HASH_B", "HASH_C" };
        var a = MakeAsset(1, postal: "8730", street: "Kerkstraat");
        var b = MakeAsset(2, postal: "8730", street: "Kerkstraat");

        var (_, level, _) = Invoke(
            "residentie weylerhof", [], shared, a,
            "residentie weylerhof", [], shared, b,
            PhotoEnabled);

        Assert.Equal("Exact", level);
    }

    [Fact]
    public void TwoPhotoMatches_SamePostal_Close_LevelsExact()
    {
        // Same postal + coords within 100m (lat/lon ≈ 0.001 degree ≈ 111m → use 0.0009 ≈ 100m)
        var shared = new List<string> { "HASH_A", "HASH_B" };
        var a = MakeAsset(1, postal: "8730", lat: 51.1000m, lon: 3.3000m);
        var b = MakeAsset(2, postal: "8730", lat: 51.1008m, lon: 3.3000m);  // ~89m north

        var (_, level, _) = Invoke(
            "residentie karmel", [], shared, a,
            "residentie karmel", [], shared, b,
            PhotoEnabled);

        Assert.Equal("Exact", level);
    }

    [Fact]
    public void TwoPhotoMatches_SamePostal_NoStreetNoCoords_LevelsProbable()
    {
        // comboD needs sameStreet OR <=100m — without either, falls back to probCond3
        var shared = new List<string> { "HASH_X", "HASH_Y" };
        var a = MakeAsset(1, postal: "8730");
        var b = MakeAsset(2, postal: "8730");

        var (_, level, _) = Invoke(
            "different name entirely aaa", [], shared, a,
            "different name entirely bbb", [], shared, b,
            PhotoEnabled);

        Assert.Equal("Probable", level);
    }

    [Fact]
    public void OnePhotoMatch_SamePostal_NoOtherSignals_LevelNone()
    {
        // Single photo match = score 0.03 only → not enough for any positive level
        var a = MakeAsset(1, postal: "9000");
        var b = MakeAsset(2, postal: "9000");

        var (_, level, _) = Invoke(
            "aaa bbb ccc", [], ["ONLY_ONE"], a,
            "xxx yyy zzz", [], ["ONLY_ONE", "EXTRA"], b,
            PhotoEnabled);

        Assert.Equal("None", level);
    }

    [Fact]
    public void PhotoHashing_Disabled_PhotoMatchesIgnored()
    {
        // Even with many shared hashes, photo scoring is skipped when disabled
        var shared = new List<string> { "H1", "H2", "H3" };
        var a = MakeAsset(1, postal: "8730");
        var b = MakeAsset(2, postal: "8730");

        var (_, level, _) = Invoke(
            "aaa bbb", [], shared, a,
            "xxx yyy", [], shared, b,
            PhotoDisabled);

        // Score should be low without any other matching signals
        Assert.Equal("None", level);
    }

    // ── Naam-match zonder foto → geen Exact tenzij andere combo ──────────────

    [Fact]
    public void TitleOnlySimilar_NoAddress_NoExact()
    {
        var a = MakeAsset(1, postal: "8730");
        var b = MakeAsset(2, postal: "9000");  // different postal → no samePostal

        var (_, level, _) = Invoke(
            "residentie weylerhof", [], [], a,
            "residentie weylerhof", [], [], b,
            PhotoEnabled);

        // comboA needs samePostal
        Assert.NotEqual("Exact", level);
    }

    [Fact]
    public void IdenticalTitleAndPostal_Close_LevelsExact()
    {
        // comboA: nameSim >= 0.95 + samePostal + <= 50m
        var a = MakeAsset(1, postal: "8730", street: "Kerkstraat", lat: 51.1000m, lon: 3.3000m);
        var b = MakeAsset(2, postal: "8730", street: "Kerkstraat", lat: 51.1003m, lon: 3.3000m); // ~33m

        var (_, level, _) = Invoke(
            "residentie weylerhof", [], [], a,
            "residentie weylerhof", [], [], b,
            PhotoEnabled);

        Assert.Equal("Exact", level);
    }

    // ── Fase-varianten mogen nooit Exact zijn ─────────────────────────────────

    [Fact]
    public void PhaseVariant_NeverExact()
    {
        // "green yard i" vs "green yard ii" — IsPhaseVariant detects the Roman numeral suffix
        // comboD fires (3 shared photos + same postal + same street) but should be downgraded
        var a = MakeAsset(1, postal: "8730", street: "Kerkstraat", lat: 51.1000m, lon: 3.3000m);
        var b = MakeAsset(2, postal: "8730", street: "Kerkstraat", lat: 51.1002m, lon: 3.3000m);

        var shared = new List<string> { "H1", "H2", "H3" };
        var (_, level, _) = Invoke(
            "green yard i", [], shared, a,
            "green yard ii", [], shared, b,
            PhotoEnabled);

        Assert.NotEqual("Exact", level);
    }
}
