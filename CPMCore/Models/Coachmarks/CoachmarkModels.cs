// Domain models (CoachmarkDefinition, CoachmarkFlow, CoachmarkStepData, CoachmarkPlacement)
// zijn gedefinieerd in BOCore/BO/Coachmarks/CoachmarkBO.vb.
// Dit bestand bevat enkel de request-DTOs die uitsluitend door CoachmarkController gebruikt worden.

namespace CPMCore.Models.Coachmarks;

public sealed class CoachmarkDismissRequest
{
    public required string StateKey { get; init; }
}

public sealed class CoachmarkCompleteRequest
{
    public required string StateKey { get; init; }
}

public sealed class CoachmarkMarkShownRequest
{
    public required string StateKey { get; init; }
}

public sealed class CoachmarkAdvanceStepRequest
{
    public required string StateKey { get; init; }
    public int Step { get; init; }
}
