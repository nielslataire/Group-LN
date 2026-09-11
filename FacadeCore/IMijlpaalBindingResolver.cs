using DALCore.Models;

namespace FacadeCore;

/// <summary>Uitkomst van een bron-binding: is de mijlpaal bereikt, en op welke datum.</summary>
public sealed record BindingUitkomst(bool Bereikt, DateOnly? WerkelijkeDatum);

/// <summary>
/// Voorgeladen brondata van één project, zodat de resolver geen queries hoeft te doen.
/// </summary>
public sealed class TrajectBronContext
{
    public required Project Project { get; init; }
    public ProjectVoortgang? Voortgang { get; init; }
    public List<ProjectDocs> Docs { get; init; } = new();
    public List<PlanningTaak> PlanningTaken { get; init; } = new();
    public List<PlanningSectie> PlanningSecties { get; init; } = new();
    public List<InvoicingPaymentStages> PaymentStages { get; init; } = new();
    /// <summary>ClientAccount per Unit.Id (enkel units met een gekoppelde klant).</summary>
    public Dictionary<int, ClientAccount> ClientAccountPerUnit { get; init; } = new();
    /// <summary>Alle ClientAccounts die aan een unit van dit project hangen.</summary>
    public List<ClientAccount> ClientAccounts { get; init; } = new();
    /// <summary>Open werfpunten (niet-afgesloten) per IssuePhase.</summary>
    public Dictionary<int, int> OpenIssuesPerPhase { get; init; } = new();
    public int OpenIssuesTotaal { get; init; }
    public bool HeeftConnectionSettlement { get; init; }
    public DateOnly? LaatsteConnectionSettlement { get; init; }
    /// <summary>Dossiers van dit project, per Id (voor de <c>Dossier</c>-binding).</summary>
    public Dictionary<int, ProjectDossier> DossiersById { get; init; } = new();
    /// <summary>Checklist-stappen per dossier-Id (voor de <c>DossierSubstap</c>-binding).</summary>
    public Dictionary<int, List<ProjectDossierSubstap>> SubstappenPerDossier { get; init; } = new();
}

/// <summary>Leidt de status/werkelijke datum van een mijlpaal af uit bestaande records (read-only).</summary>
public interface IMijlpaalBindingResolver
{
    /// <summary>
    /// Resolveert één mijlpaal tegen zijn <c>BronBinding</c>. Geeft <c>null</c> terug wanneer er geen
    /// (bruikbare) binding is of de bron niet leesbaar is — de mijlpaal blijft dan ongewijzigd.
    /// </summary>
    BindingUitkomst? Resolve(Mijlpaal mijlpaal, TrajectBronContext ctx);
}
