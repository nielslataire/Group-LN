using BOCore;

namespace ServiceCore.Signing;

/// <summary>Toestand van één ondertekenaar zoals de regelmotor ze nodig heeft — bewust los van de entiteit, zodat dit puur en testbaar blijft.</summary>
public sealed record PartyRuleState(int PartyId, int SortOrder, SigningPartyStatus Status);

public sealed record RuleOutcome(
    bool IsComplete,
    /// <summary>Partijen die (nu) een uitnodiging moeten krijgen — bij ALL/ANY iedereen bij het openen, bij ORDERED telkens de volgende.</summary>
    IReadOnlyList<int> PartiesToInvite,
    /// <summary>Partijen wier link ingetrokken moet worden (ANY: de anderen zodra iemand tekende).</summary>
    IReadOnlyList<int> PartiesToRevoke);

/// <summary>
/// De ondertekenregel (ONDERTEKENEN_VOORSTEL.md §5.2 stap 8): ALL, ANY, ORDERED. Pure functie over
/// de partijstatussen; wordt aangeroepen bij het openen van een dossier en na elke handtekening.
/// </summary>
public static class SigningRuleEvaluator
{
    private static bool IsOut(SigningPartyStatus s) => s is SigningPartyStatus.Declined or SigningPartyStatus.Revoked or SigningPartyStatus.Expired;

    private static bool IsSigned(SigningPartyStatus s) => s == SigningPartyStatus.Signed;

    private static bool NotYetInvited(SigningPartyStatus s) => s == SigningPartyStatus.Pending;

    public static RuleOutcome Evaluate(SigningRule rule, IReadOnlyList<PartyRuleState> parties)
    {
        if (parties.Count == 0) return new RuleOutcome(false, Array.Empty<int>(), Array.Empty<int>());
        var ordered = parties.OrderBy(p => p.SortOrder).ThenBy(p => p.PartyId).ToList();

        switch (rule)
        {
            case SigningRule.Any:
            {
                var signed = ordered.Any(p => IsSigned(p.Status));
                if (signed)
                {
                    var revoke = ordered.Where(p => !IsSigned(p.Status) && !IsOut(p.Status)).Select(p => p.PartyId).ToList();
                    return new RuleOutcome(true, Array.Empty<int>(), revoke);
                }
                var invite = ordered.Where(p => NotYetInvited(p.Status)).Select(p => p.PartyId).ToList();
                return new RuleOutcome(false, invite, Array.Empty<int>());
            }

            case SigningRule.Ordered:
            {
                // Iedereen die niet afgevallen is moet tekenen; enkel de eerste die nog niet tekende wordt uitgenodigd.
                var remaining = ordered.Where(p => !IsOut(p.Status)).ToList();
                var complete = remaining.Count > 0 && remaining.All(p => IsSigned(p.Status));
                if (complete) return new RuleOutcome(true, Array.Empty<int>(), Array.Empty<int>());
                var next = remaining.FirstOrDefault(p => !IsSigned(p.Status));
                var invite = new List<int>();
                if (next is not null && NotYetInvited(next.Status)) invite.Add(next.PartyId);
                return new RuleOutcome(false, invite, Array.Empty<int>());
            }

            case SigningRule.All:
            default:
            {
                var remaining = ordered.Where(p => !IsOut(p.Status)).ToList();
                var complete = remaining.Count > 0 && remaining.All(p => IsSigned(p.Status));
                var invite = complete ? new List<int>() : ordered.Where(p => NotYetInvited(p.Status)).Select(p => p.PartyId).ToList();
                return new RuleOutcome(complete, invite, Array.Empty<int>());
            }
        }
    }

    /// <summary>Kan het dossier nog voltooid worden? Bij ALL/ORDERED niet meer zodra iemand weigerde of verliep; bij ANY zolang er nog iemand overblijft.</summary>
    public static bool CanStillComplete(SigningRule rule, IReadOnlyList<PartyRuleState> parties)
    {
        if (parties.Count == 0) return false;
        return rule == SigningRule.Any
            ? parties.Any(p => !IsOut(p.Status))
            : parties.All(p => p.Status is not (SigningPartyStatus.Declined or SigningPartyStatus.Expired));
    }
}
