using System.Collections.Generic;
using System.Linq;
using BOCore;
using ServiceCore.Signing;
using Xunit;

namespace ServiceCore.Tests.Signing;

/// <summary>ALL / ANY / ORDERED (ONDERTEKENEN_VOORSTEL.md §5.2 stap 8, §9.9).</summary>
public class SigningRuleEvaluatorTests
{
    private static PartyRuleState P(int id, SigningPartyStatus s, int order = 0) => new(id, order == 0 ? id : order, s);

    [Fact]
    public void All_InvitesEveryoneAtOpen_AndCompletesOnlyWhenAllSigned()
    {
        var open = SigningRuleEvaluator.Evaluate(SigningRule.All, new[] { P(1, SigningPartyStatus.Pending), P(2, SigningPartyStatus.Pending) });
        Assert.False(open.IsComplete);
        Assert.Equal(new[] { 1, 2 }, open.PartiesToInvite);
        Assert.Empty(open.PartiesToRevoke);

        var oneSigned = SigningRuleEvaluator.Evaluate(SigningRule.All, new[] { P(1, SigningPartyStatus.Signed), P(2, SigningPartyStatus.Opened) });
        Assert.False(oneSigned.IsComplete);
        Assert.Empty(oneSigned.PartiesToInvite);
        Assert.Empty(oneSigned.PartiesToRevoke);

        var both = SigningRuleEvaluator.Evaluate(SigningRule.All, new[] { P(1, SigningPartyStatus.Signed), P(2, SigningPartyStatus.Signed) });
        Assert.True(both.IsComplete);
    }

    [Fact]
    public void All_DeclinedPartyMeansItCanNeverComplete()
    {
        var parties = new[] { P(1, SigningPartyStatus.Signed), P(2, SigningPartyStatus.Declined) };
        Assert.False(SigningRuleEvaluator.CanStillComplete(SigningRule.All, parties));
        Assert.False(SigningRuleEvaluator.CanStillComplete(SigningRule.Ordered, parties));
    }

    [Fact]
    public void Any_FirstSignatureCompletes_AndRevokesTheOthers()
    {
        var open = SigningRuleEvaluator.Evaluate(SigningRule.Any, new[] { P(1, SigningPartyStatus.Pending), P(2, SigningPartyStatus.Pending) });
        Assert.Equal(new[] { 1, 2 }, open.PartiesToInvite);

        var signed = SigningRuleEvaluator.Evaluate(SigningRule.Any, new[] { P(1, SigningPartyStatus.Opened), P(2, SigningPartyStatus.Signed), P(3, SigningPartyStatus.Declined) });
        Assert.True(signed.IsComplete);
        Assert.Equal(new[] { 1 }, signed.PartiesToRevoke);   // 3 is al afgevallen, niet nog eens intrekken
        Assert.Empty(signed.PartiesToInvite);
    }

    [Fact]
    public void Any_CanStillCompleteWhileSomeoneRemains()
    {
        Assert.True(SigningRuleEvaluator.CanStillComplete(SigningRule.Any, new[] { P(1, SigningPartyStatus.Declined), P(2, SigningPartyStatus.Invited) }));
        Assert.False(SigningRuleEvaluator.CanStillComplete(SigningRule.Any, new[] { P(1, SigningPartyStatus.Declined), P(2, SigningPartyStatus.Declined) }));
    }

    [Fact]
    public void Ordered_InvitesOneAtATime_InSortOrder()
    {
        var open = SigningRuleEvaluator.Evaluate(SigningRule.Ordered, new[] { P(10, SigningPartyStatus.Pending, order: 2), P(20, SigningPartyStatus.Pending, order: 1) });
        Assert.Equal(new[] { 20 }, open.PartiesToInvite);   // laagste SortOrder eerst

        var firstSigned = SigningRuleEvaluator.Evaluate(SigningRule.Ordered, new[] { P(10, SigningPartyStatus.Pending, order: 2), P(20, SigningPartyStatus.Signed, order: 1) });
        Assert.False(firstSigned.IsComplete);
        Assert.Equal(new[] { 10 }, firstSigned.PartiesToInvite);

        var waiting = SigningRuleEvaluator.Evaluate(SigningRule.Ordered, new[] { P(10, SigningPartyStatus.Invited, order: 2), P(20, SigningPartyStatus.Signed, order: 1) });
        Assert.Empty(waiting.PartiesToInvite);   // al uitgenodigd, niet nog eens

        var done = SigningRuleEvaluator.Evaluate(SigningRule.Ordered, new[] { P(10, SigningPartyStatus.Signed, order: 2), P(20, SigningPartyStatus.Signed, order: 1) });
        Assert.True(done.IsComplete);
    }

    [Fact]
    public void EmptyPartyList_NeverCompletes()
    {
        foreach (var rule in new[] { SigningRule.All, SigningRule.Any, SigningRule.Ordered })
        {
            var r = SigningRuleEvaluator.Evaluate(rule, new List<PartyRuleState>());
            Assert.False(r.IsComplete);
            Assert.Empty(r.PartiesToInvite);
        }
    }
}
