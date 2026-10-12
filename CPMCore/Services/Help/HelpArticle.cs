using System;
using System.Collections.Generic;

namespace CPMCore.Services.Help;

/// <summary>
/// Een help-artikel, geparset uit één markdown-bestand onder CPMCore/HelpContent/. Puur
/// in-memory (geen EF-entity) — content is docs-as-code, zie DESIGN.md "Helpcentrum".
/// </summary>
public class HelpArticle
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string TopicSlug { get; set; } = "";
    /// <summary>Slug van het bovenliggende artikel binnen hetzelfde topic, of null voor een
    /// hoofdartikel. Eén niveau diep (de boom in 41c toont nooit meer dan ouder+kind) — een
    /// artikel dat naar een onbestaand of verkeerd-topic-slug ouder verwijst valt terug op
    /// hoofdartikel, geen crash.</summary>
    public string ParentSlug { get; set; }
    /// <summary>Sorteervolgorde tussen hoofdartikelen van hetzelfde topic in de boom (41c) — zonder
    /// waarde valt terug op alfabetisch op Title. Losstaand van StartOrder (die bepaalt "Begin
    /// hier" op 41a, een heel andere lijst met een ander doel).</summary>
    public int? TreeOrder { get; set; }
    /// <summary>Kort label voor de boom (41c) als de volledige Title daar te lang/beschrijvend is
    /// (bv. Title "Projecten — het overzicht" → TreeLabel "Overzicht"). Valt terug op Title.</summary>
    public string TreeLabel { get; set; }
    public string Description { get; set; }
    public string Role { get; set; }
    public string Device { get; set; }
    public int Minutes { get; set; }
    public DateOnly? UpdatedOn { get; set; }
    public List<string> Screens { get; set; } = new();
    public List<string> RelatedSlugs { get; set; } = new();
    public List<string> Synonyms { get; set; } = new();
    public string IntroHtml { get; set; } = "";
    public string BeforeHtml { get; set; }
    public string WarningHtml { get; set; }
    public List<HelpArticleStep> Steps { get; set; } = new();
    public List<HelpFaqEntry> Faq { get; set; } = new();

    /// <summary>Gezet op de handvol artikels die in "Begin hier — eerste week" (41a) horen.</summary>
    public int? StartOrder { get; set; }

    /// <summary>"Nieuw" of "Bijgewerkt" — enkel gezet op artikels die in "Nieuw en bijgewerkt" (41a) horen.</summary>
    public string NewsTag { get; set; }

    /// <summary>Platte tekst van titel + stappen + FAQ, voor de zoek-index (41b).</summary>
    public string SearchText { get; set; } = "";
}

public class HelpArticleStep
{
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public string BodyHtml { get; set; } = "";
    public string ImagePath { get; set; }
    public string ImageCaption { get; set; }
}

public class HelpFaqEntry
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
}

public class HelpTopic
{
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}

/// <summary>Resultaatrij voor 41b — artikel, stap-binnen-artikel of scherm, met gemarkeerde tekst.</summary>
public class HelpSearchHit
{
    public string Kind { get; set; } = ""; // "artikel" | "stap" | "scherm"
    public string Title { get; set; } = "";
    public string Path { get; set; } = ""; // bv. "Facturatie & wijzigingen › Wijzigingsopdrachten"
    public string ArticleSlug { get; set; } = "";
    public string Meta { get; set; } = ""; // bv. "5 min"
    public int MatchIndex { get; set; } = -1;
    public int MatchLength { get; set; }
}
