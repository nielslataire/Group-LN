using System;
using System.Collections.Generic;
using System.Linq;
using ServiceCore.Signing;
using Xunit;

namespace ServiceCore.Tests.Signing;

/// <summary>
/// De hash-ketting van de audit trail (ONDERTEKENEN_VOORSTEL.md §3.7), getest op het algoritme dat
/// <c>SigningEvidenceStore</c> gebruikt: elk event hasht over zijn eigen velden én de hash van het
/// vorige. Wijzigen, verwijderen of tussenvoegen wordt zichtbaar; een retentiescrub van de ruwe
/// IP/user-agent niet — die zitten bewust niet in de ketting.
/// </summary>
public class SigningEventChainTests
{
    private sealed record Ev(long Id, string Type, DateTime At, string? Ip, string? IpHash, string? Data, string? Prev, string Hash);

    private static List<Ev> Chain(params (string Type, string? Ip, string? Data)[] items)
    {
        var list = new List<Ev>();
        string? prev = null;
        var at = new DateTime(2026, 9, 27, 9, 0, 0, DateTimeKind.Utc);
        var id = 1L;
        foreach (var (type, ip, data) in items)
        {
            at = at.AddSeconds(1);
            var ipHash = SigningCrypto.HashPii(ip);
            var hash = SigningCrypto.ComputeEventHash(prev, 42, null, type, at, 2, null, "Jan", ipHash, null, null, data);
            list.Add(new Ev(id++, type, at, ip, ipHash, data, prev, hash));
            prev = hash;
        }
        return list;
    }

    private static (bool Valid, long? Broken) Verify(IReadOnlyList<Ev> events)
    {
        string? prev = null;
        foreach (var e in events)
        {
            if ((e.Prev ?? "") != (prev ?? "")) return (false, e.Id);
            var expected = SigningCrypto.ComputeEventHash(e.Prev, 42, null, e.Type, e.At, 2, null, "Jan", e.IpHash, null, null, e.Data);
            if (!SigningCrypto.FixedTimeEqualsHex(expected, e.Hash)) return (false, e.Id);
            prev = e.Hash;
        }
        return (true, null);
    }

    [Fact]
    public void IntactChain_Verifies()
    {
        var chain = Chain(("CaseCreated", "10.0.0.1", "{}"), ("LinkOpened", "10.0.0.1", null), ("PartySigned", "10.0.0.1", "{\"m\":1}"));
        Assert.Equal((true, (long?)null), Verify(chain));
    }

    [Fact]
    public void TamperedData_BreaksChainAtThatEvent()
    {
        var chain = Chain(("CaseCreated", null, "{}"), ("PartySigned", null, "{\"consent\":true}"), ("CaseCompleted", null, null));
        var tampered = chain.Select(e => e.Id == 2 ? e with { Data = "{\"consent\":false}" } : e).ToList();
        Assert.Equal((false, (long?)2), Verify(tampered));
    }

    [Fact]
    public void RemovedEvent_BreaksChainAtTheNextEvent()
    {
        var chain = Chain(("CaseCreated", null, null), ("LinkOpened", null, null), ("PartySigned", null, null));
        var removed = chain.Where(e => e.Id != 2).ToList();
        Assert.Equal((false, (long?)3), Verify(removed));
    }

    [Fact]
    public void ScrubbingRawIp_KeepsChainIntact()
    {
        var chain = Chain(("LinkOpened", "203.0.113.7", null), ("PartySigned", "203.0.113.7", null));
        var scrubbed = chain.Select(e => e with { Ip = null }).ToList();   // IpHash blijft staan, zoals de trigger toelaat
        Assert.Equal((true, (long?)null), Verify(scrubbed));
    }

    [Fact]
    public void ChangingIpHash_BreaksChain()
    {
        var chain = Chain(("LinkOpened", "203.0.113.7", null));
        var changed = chain.Select(e => e with { IpHash = SigningCrypto.HashPii("203.0.113.8") }).ToList();
        Assert.Equal((false, (long?)1), Verify(changed));
    }
}
