using DALCore.Models;

namespace FacadeCore;

/// <summary>Uitkomst van één trigger-actie-uitvoering.</summary>
public sealed record TriggerActieResultaat(bool Geslaagd, bool Overgeslagen, string? Resultaat, string? Fout)
{
    public static TriggerActieResultaat Success(string resultaat) => new(true, false, resultaat, null);
    public static TriggerActieResultaat Skip(string reden) => new(true, true, reden, null);
    public static TriggerActieResultaat Fail(string fout) => new(false, false, null, fout);
}

/// <summary>
/// Eén handler per <c>BOCore.TriggerActie</c>-waarde. Geregistreerd als <c>IEnumerable&lt;ITrajectTriggerAction&gt;</c>
/// zodat de dispatcher de juiste handler kan kiezen (Strategy-registry, analoog aan ISectionRenderer).
/// </summary>
public interface ITrajectTriggerAction
{
    /// <summary>BOCore.TriggerActie die deze handler afhandelt.</summary>
    int Actie { get; }

    Task<TriggerActieResultaat> ExecuteAsync(Mijlpaal mijlpaal, MijlpaalTrigger trigger, Project project, string? triggeredBy, CancellationToken ct = default);
}
