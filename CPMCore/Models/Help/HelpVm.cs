using System.Collections.Generic;
using CPMCore.Services.Help;

namespace CPMCore.Models.Help;

// ── 41a — Startpagina ───────────────────────────────────────────────────────────────────────

public class HelpHomeV2Vm
{
    public List<HelpHomeTopicVm> Topics { get; set; } = new();
    public List<HelpStartItemVm> Start { get; set; } = new();
    public List<HelpNewsItemVm> News { get; set; } = new();
    public List<string> PopularTerms { get; set; } = new();
    public int ArticleCount { get; set; }
}

public class HelpHomeTopicVm
{
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Count { get; set; }
    public List<string> TopArticleTitles { get; set; } = new();
    public string FirstArticleSlug { get; set; }
}

public class HelpStartItemVm
{
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Minutes { get; set; } = "";
    public string ArticleSlug { get; set; } = "";
    public bool Highlighted { get; set; }
}

public class HelpNewsItemVm
{
    public string Tag { get; set; } = "";
    public string Title { get; set; } = "";
    public string ArticleSlug { get; set; } = "";
    public string Date { get; set; } = "";
    public bool IsNew { get; set; }
}

// ── 41c — Artikelpagina ─────────────────────────────────────────────────────────────────────

public class HelpArticleV2Vm
{
    public HelpArticle Article { get; set; } = null!;
    public HelpTopic Topic { get; set; } = null!;
    public List<HelpTreeGroupVm> Tree { get; set; } = new();
    public List<HelpTocItemVm> Toc { get; set; } = new();
    public List<HelpRelatedVm> Related { get; set; } = new();
}

public class HelpTreeGroupVm
{
    public string Name { get; set; } = "";
    public bool IsOpen { get; set; }
    public int Count { get; set; }
    /// <summary>Alle artikels van het topic als boom (willekeurig diep). Altijd volledig meegegeven:
    /// elk niveau is openklapbaar in de browser, de server bepaalt enkel wat bij het laden openstaat.</summary>
    public List<HelpTreeNodeVm> Nodes { get; set; } = new();
}

public class HelpTreeNodeVm
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    /// <summary>Dit is het artikel dat nu open staat.</summary>
    public bool IsActive { get; set; }
    /// <summary>Open bij het laden: het actieve artikel zelf (als het kinderen heeft) of een bovenliggend
    /// artikel van het actieve artikel. Alle andere knopen staan dicht.</summary>
    public bool IsOpen { get; set; }
    public List<HelpTreeNodeVm> Children { get; set; } = new();
}

/// <summary>Een knoop plus zijn diepte (0 = direct onder het topic), voor de recursieve partial _HelpTreeNode.</summary>
public class HelpTreeNodeRenderVm
{
    public HelpTreeNodeVm Node { get; set; } = null!;
    public int Depth { get; set; }
}

public class HelpTocItemVm
{
    public string Text { get; set; } = "";
    /// <summary>Id van het blok in de artikeltekst waar dit punt naartoe springt (zonder #).</summary>
    public string TargetId { get; set; } = "";
    public bool IsActive { get; set; }
}

public class HelpRelatedVm
{
    public string TopicName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
}

// ── 41b — Zoeken ────────────────────────────────────────────────────────────────────────────

public class HelpSearchGroupVm
{
    public string Heading { get; set; } = "";
    public List<HelpSearchHit> Items { get; set; } = new();
}

// ── 41d — Contextueel zijpaneel ─────────────────────────────────────────────────────────────

public class HelpPanelVm
{
    public string ScreenTitle { get; set; } = "";
    public List<HelpArticle> Articles { get; set; } = new();
}
