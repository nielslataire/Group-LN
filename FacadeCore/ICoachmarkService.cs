using BOCore;
using System.Threading;
using System.Threading.Tasks;

namespace FacadeCore
{
    public interface ICoachmarkService
    {
        /// <summary>
        /// Geeft de actieve coachmark-flow terug voor de opgegeven pagina en gebruiker.
        /// Geeft null als er niets te tonen is.
        /// Max. 1 flow tegelijk: sequences krijgen prioriteit over singles.
        /// </summary>
        Task<CoachmarkFlow> GetActiveForPageAsync(string pageKey, int userId, CancellationToken ct = default);

        /// <summary>
        /// Registreert dat een coachmark (of sequence) getoond is.
        /// </summary>
        Task MarkShownAsync(string stateKey, int userId, CancellationToken ct = default);

        /// <summary>
        /// Dismisst een coachmark of volledige sequence.
        /// </summary>
        Task DismissAsync(string stateKey, int userId, CancellationToken ct = default);

        /// <summary>
        /// Markeert een coachmark of sequence als voltooid.
        /// </summary>
        Task CompleteAsync(string stateKey, int userId, CancellationToken ct = default);

        /// <summary>
        /// Zet de huidige stap van een sequence verder.
        /// </summary>
        Task AdvanceStepAsync(string stateKey, int userId, int step, CancellationToken ct = default);
    }
}
