using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;

namespace CPMCore.Services.Help;

/// <summary>
/// Parset één .md-bestand (frontmatter + body) naar een <see cref="HelpArticle"/>. Bewust een
/// eigen, kleine parser i.p.v. een YAML-library: de frontmatter-vorm die de help-artikels nodig
/// hebben is vast en klein (scalars, een paar comma-lijsten, één "|"-blok, één faq-lijst) — een
/// generieke YAML-parser zou hier alleen een extra dependency zijn zonder iets te winnen.
/// </summary>
public static class HelpMarkdownParser
{
    // UseAdvancedExtensions() voor pipe-tables ("| Cijfer | Betekent |" in project-detail-overzicht.md
    // werd anders letterlijk als platte tekst getoond, geen <table>) — neemt meteen de rest van GFM
    // mee (auto-links, strikethrough enz.), allemaal onschadelijk voor de bestaande artikels.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private static readonly Regex ImageLine = new(@"^!\[(.*?)\]\((.*?)\)\s*$", RegexOptions.Compiled);
    private static readonly Regex HeadingLine = new(@"^##\s+(.+?)\s*$", RegexOptions.Compiled);

    public static HelpArticle Parse(string slug, string rawText)
    {
        var lines = rawText.Replace("\r\n", "\n").Split('\n');
        var i = 0;

        var frontmatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var faq = new List<HelpFaqEntry>();

        if (i < lines.Length && lines[i].Trim() == "---")
        {
            i++;
            while (i < lines.Length && lines[i].Trim() != "---")
            {
                var line = lines[i];
                var trimmed = line.TrimStart();
                var colon = trimmed.IndexOf(':');
                if (colon < 0) { i++; continue; }

                var key = trimmed[..colon].Trim();
                var rest = trimmed[(colon + 1)..].Trim();

                if (string.Equals(key, "faq", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    HelpFaqEntry? current = null;
                    while (i < lines.Length)
                    {
                        var faqLine = lines[i];
                        var faqTrimmed = faqLine.TrimStart();
                        if (faqTrimmed.StartsWith("- q:"))
                        {
                            current = new HelpFaqEntry { Question = faqTrimmed[4..].Trim() };
                            faq.Add(current);
                            i++;
                        }
                        else if (faqTrimmed.StartsWith("a:") && current != null)
                        {
                            current.Answer = faqTrimmed[2..].Trim();
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    continue;
                }

                if (rest == "|")
                {
                    i++;
                    var block = new List<string>();
                    while (i < lines.Length && (lines[i].StartsWith("  ") || string.IsNullOrWhiteSpace(lines[i])))
                    {
                        if (lines[i].Trim() == "---") break;
                        block.Add(lines[i].Length >= 2 ? lines[i][2..] : "");
                        i++;
                    }
                    frontmatter[key] = string.Join("\n", block).Trim();
                    continue;
                }

                frontmatter[key] = rest;
                i++;
            }
            i++; // skip closing ---
        }

        var article = new HelpArticle
        {
            Slug = slug,
            Title = Get(frontmatter, "title"),
            TopicSlug = Get(frontmatter, "topic"),
            ParentSlug = Get(frontmatter, "parent", null),
            TreeOrder = int.TryParse(Get(frontmatter, "treeOrder", null), out var to) ? to : null,
            TreeLabel = Get(frontmatter, "treeLabel", null),
            Description = Get(frontmatter, "description", null),
            Role = Get(frontmatter, "role", null),
            Device = Get(frontmatter, "device", null),
            Minutes = int.TryParse(Get(frontmatter, "minutes", "0"), out var m) ? m : 0,
            UpdatedOn = DateOnly.TryParseExact(Get(frontmatter, "updated", null), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null,
            Screens = SplitList(Get(frontmatter, "screens", null)),
            RelatedSlugs = SplitList(Get(frontmatter, "related", null)),
            Synonyms = SplitList(Get(frontmatter, "synonyms", null)),
            Faq = faq,
            StartOrder = int.TryParse(Get(frontmatter, "startOrder", null), out var so) ? so : null,
            NewsTag = Get(frontmatter, "newsTag", null),
        };

        if (frontmatter.TryGetValue("intro", out var introRaw) && !string.IsNullOrWhiteSpace(introRaw))
            article.IntroHtml = Markdown.ToHtml(introRaw, Pipeline);

        if (frontmatter.TryGetValue("before", out var beforeRaw) && !string.IsNullOrWhiteSpace(beforeRaw))
            article.BeforeHtml = Markdown.ToHtml(beforeRaw, Pipeline);

        // ── Body: elke "## Titel" is een stap, behalve "## Let op" (41f #06, waarschuwingskader) ──
        var bodyLines = lines.Skip(i).ToArray();
        var sections = new List<(string Title, List<string> Body)>();
        List<string>? currentBody = null;
        string? currentTitle = null;
        foreach (var line in bodyLines)
        {
            var headingMatch = HeadingLine.Match(line);
            if (headingMatch.Success)
            {
                if (currentTitle != null) sections.Add((currentTitle, currentBody!));
                currentTitle = headingMatch.Groups[1].Value;
                currentBody = new List<string>();
            }
            else
            {
                currentBody?.Add(line);
            }
        }
        if (currentTitle != null) sections.Add((currentTitle, currentBody!));

        var stepNumber = 1;
        var plainTextParts = new List<string> { article.Title };
        plainTextParts.AddRange(article.Synonyms);

        foreach (var (title, bodyRaw) in sections)
        {
            var bodyText = string.Join("\n", bodyRaw).Trim();

            if (string.Equals(title, "Let op", StringComparison.OrdinalIgnoreCase))
            {
                article.WarningHtml = Markdown.ToHtml(bodyText, Pipeline);
                continue;
            }

            string? imagePath = null, imageCaption = null;
            var bodyLinesTrimmed = bodyText.Split('\n');
            if (bodyLinesTrimmed.Length > 0)
            {
                var imgMatch = ImageLine.Match(bodyLinesTrimmed[0].Trim());
                if (imgMatch.Success)
                {
                    imageCaption = imgMatch.Groups[1].Value;
                    imagePath = imgMatch.Groups[2].Value;
                    bodyText = string.Join("\n", bodyLinesTrimmed.Skip(1)).Trim();
                }
            }

            article.Steps.Add(new HelpArticleStep
            {
                Number = stepNumber++,
                Title = title,
                BodyHtml = Markdown.ToHtml(bodyText, Pipeline),
                ImagePath = imagePath,
                ImageCaption = imageCaption,
            });

            plainTextParts.Add(title);
            plainTextParts.Add(StripTags(bodyText));
        }

        foreach (var f in faq)
        {
            plainTextParts.Add(f.Question);
            plainTextParts.Add(f.Answer);
        }

        article.SearchText = string.Join(" ", plainTextParts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return article;
    }

    private static string Get(IDictionary<string, string> dict, string key, string? fallback = "")
        => dict.TryGetValue(key, out var v) ? v : fallback ?? "";

    private static List<string> SplitList(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string StripTags(string html) => Regex.Replace(html, "<.*?>", " ");
}
