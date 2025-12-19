using System.Collections.Generic;
using CPMCore.Services.Octopus;
namespace CPMCore.Models.Instellingen
{
    public class OctopusRelationSuggestionsVM
    {
        public int IssuerId { get; init; }
        public string IssuerName { get; init; } = string.Empty;
        public IReadOnlyList<OctopusRelationSuggestion> Suggestions { get; init; } = new List<OctopusRelationSuggestion>();
    }
}
