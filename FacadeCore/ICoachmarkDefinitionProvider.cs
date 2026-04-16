using BOCore;
using System.Collections.Generic;

namespace FacadeCore
{
    /// <summary>
    /// Levert coachmark-definities aan de service-laag zonder een directe referentie
    /// naar CPMCore te vereisen. De implementatie (CoachmarkDefinitionProvider) bevindt
    /// zich in CPMCore en wikkelt CoachmarkRegistry in.
    /// </summary>
    public interface ICoachmarkDefinitionProvider
    {
        /// <summary>Alle definities voor een specifieke pagina.</summary>
        IReadOnlyList<CoachmarkDefinition> GetForPage(string pageKey);
    }
}
