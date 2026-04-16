using CPMCore.Models.Coachmarks;
using BOCore;
using FacadeCore;
using System.Collections.Generic;

namespace CPMCore.Services;

/// <summary>
/// Implementatie van ICoachmarkDefinitionProvider die delegeert naar de statische
/// CoachmarkRegistry. ServiceCore kan hierdoor zonder CPMCore-referentie aan de
/// definities geraken.
/// </summary>
public sealed class CoachmarkDefinitionProvider : ICoachmarkDefinitionProvider
{
    public IReadOnlyList<CoachmarkDefinition> GetForPage(string pageKey)
        => CoachmarkRegistry.GetForPage(pageKey);
}
