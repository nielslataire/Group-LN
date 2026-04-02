namespace FacadeCore;

public interface IContractorPortalDigestService
{
    /// <summary>
    /// Stuurt een dagelijkse digestmail naar elke werfleider met
    /// nieuwe opmerkingen van aannemers en punten die klaar voor controle zijn gezet
    /// since the last 24 hours. Runs at most once per calendar day.
    /// </summary>
    Task RunAsync(CancellationToken ct = default);
}
