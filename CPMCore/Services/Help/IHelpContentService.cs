using System.Collections.Generic;

namespace CPMCore.Services.Help;

/// <summary>
/// Leest alle markdown-artikels onder CPMCore/HelpContent/ in geheugen (docs-as-code — zie
/// DESIGN.md "Helpcentrum", design-handoff punt 41). Geen DB, geen admin-CRUD: nieuwe artikels
/// zijn nieuwe .md-bestanden, net als code.
/// </summary>
public interface IHelpContentService
{
    IReadOnlyList<HelpTopic> Topics { get; }
    IReadOnlyList<HelpArticle> Articles { get; }

    HelpArticle? GetBySlug(string slug);
    IReadOnlyList<HelpArticle> GetByTopic(string topicSlug);

    /// <summary>Artikels waarvan Screens één van de gegeven screen-id's bevat (design-handoff 41d).</summary>
    IReadOnlyList<HelpArticle> GetByScreenIds(IEnumerable<string> screenIds);

    /// <summary>Eenvoudige zoek-in-alles over titel/stappen/FAQ/synoniemen (41b).</summary>
    IReadOnlyList<HelpSearchHit> Search(string term, int maxResults = 8);
}
