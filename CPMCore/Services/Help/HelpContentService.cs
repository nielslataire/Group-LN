using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace CPMCore.Services.Help;

/// <summary>
/// Singleton — leest CPMCore/HelpContent/**/*.md bij eerste gebruik en cachet het resultaat in
/// geheugen (zie IHelpContentService). In Development wordt bij elke aanroep gecontroleerd of een
/// bestand gewijzigd is (goedkoop: enkel een mtime-vergelijking), zodat je een artikel kan
/// aanpassen zonder de app te herstarten; in Production blijft de eerste load staan.
/// </summary>
public class HelpContentService : IHelpContentService
{
    // Vaste onderwerpen, in de volgorde van het hoofdmenu/de rail (design-handoff 41a
    // "ONDERWERPEN — ZELFDE VOLGORDE ALS HET MENU") — dit zijn geen artikel-frontmatter-velden,
    // het zijn de vaste rubrieken van de app zelf.
    private static readonly List<HelpTopic> FixedTopics = new()
    {
        new HelpTopic { Slug = "aan-de-slag", Name = "Aan de slag", Icon = "ph-rocket-launch" },
        new HelpTopic { Slug = "projecten", Name = "Projecten", Icon = "ph-buildings" },
        new HelpTopic { Slug = "budget", Name = "Budget", Icon = "ph-chart-bar" },
        new HelpTopic { Slug = "facturatie-wijzigingen", Name = "Facturatie & wijzigingen", Icon = "ph-invoice" },
        new HelpTopic { Slug = "punten-verslagen", Name = "Punten & verslagen", Icon = "ph-map-pin" },
        new HelpTopic { Slug = "traject-dossiers", Name = "Traject & dossiers", Icon = "ph-signpost" },
        new HelpTopic { Slug = "documenten", Name = "Documenten", Icon = "ph-files" },
        new HelpTopic { Slug = "instellingen", Name = "Instellingen", Icon = "ph-gear" },
        new HelpTopic { Slug = "marktanalyse", Name = "Marktanalyse", Icon = "ph-chart-line-up" },
    };

    private readonly IWebHostEnvironment _env;
    private readonly object _lock = new();
    private List<HelpArticle>? _articles;
    private DateTime _loadedAtUtc;

    public HelpContentService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IReadOnlyList<HelpTopic> Topics => FixedTopics;

    public IReadOnlyList<HelpArticle> Articles => EnsureLoaded();

    public HelpArticle? GetBySlug(string slug)
        => EnsureLoaded().FirstOrDefault(a => string.Equals(a.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<HelpArticle> GetByTopic(string topicSlug)
        => EnsureLoaded().Where(a => string.Equals(a.TopicSlug, topicSlug, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<HelpArticle> GetByScreenIds(IEnumerable<string> screenIds)
    {
        var wanted = screenIds.Select(s => s.Trim()).Where(s => s.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0) return Array.Empty<HelpArticle>();
        return EnsureLoaded().Where(a => a.Screens.Any(wanted.Contains)).ToList();
    }

    public IReadOnlyList<HelpSearchHit> Search(string term, int maxResults = 8)
    {
        if (string.IsNullOrWhiteSpace(term)) return Array.Empty<HelpSearchHit>();
        var needle = term.Trim();

        var hits = new List<HelpSearchHit>();

        foreach (var article in EnsureLoaded())
        {
            var titleIndex = article.Title.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            var inSynonyms = article.Synonyms.Any(s => s.Contains(needle, StringComparison.OrdinalIgnoreCase));
            var inBody = titleIndex < 0 && article.SearchText.Contains(needle, StringComparison.OrdinalIgnoreCase);

            if (titleIndex >= 0 || inSynonyms || inBody)
            {
                var topic = FixedTopics.FirstOrDefault(t => t.Slug == article.TopicSlug);
                hits.Add(new HelpSearchHit
                {
                    Kind = "artikel",
                    Title = article.Title,
                    Path = topic?.Name ?? "",
                    ArticleSlug = article.Slug,
                    Meta = article.Minutes > 0 ? $"{article.Minutes} min" : "",
                    MatchIndex = titleIndex >= 0 ? titleIndex : 0,
                    MatchLength = titleIndex >= 0 ? needle.Length : 0,
                });
            }

            foreach (var step in article.Steps)
            {
                var stepIndex = step.Title.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (stepIndex >= 0)
                {
                    hits.Add(new HelpSearchHit
                    {
                        Kind = "stap",
                        Title = $"Stap {step.Number} · {step.Title}",
                        Path = $"in: {article.Title}",
                        ArticleSlug = article.Slug,
                        MatchIndex = stepIndex + 9, // "Stap N · " prefix lengte benaderd
                        MatchLength = needle.Length,
                    });
                }
            }

            foreach (var f in article.Faq)
            {
                var qIndex = f.Question.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (qIndex >= 0)
                {
                    hits.Add(new HelpSearchHit
                    {
                        Kind = "stap",
                        Title = f.Question,
                        Path = "in: Veelgestelde vragen",
                        ArticleSlug = article.Slug,
                        MatchIndex = qIndex,
                        MatchLength = needle.Length,
                    });
                }
            }
        }

        return hits
            .OrderBy(h => h.Kind == "artikel" ? 0 : 1)
            .Take(maxResults)
            .ToList();
    }

    private List<HelpArticle> EnsureLoaded()
    {
        if (_articles != null && (!_env.IsDevelopment() || DateTime.UtcNow - _loadedAtUtc < TimeSpan.FromSeconds(2)))
            return _articles;

        lock (_lock)
        {
            var root = Path.Combine(_env.ContentRootPath, "HelpContent");
            var loaded = new List<HelpArticle>();

            if (Directory.Exists(root))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
                {
                    var slug = Path.GetFileNameWithoutExtension(file);
                    var raw = File.ReadAllText(file);
                    try
                    {
                        loaded.Add(HelpMarkdownParser.Parse(slug, raw));
                    }
                    catch
                    {
                        // Eén kapot artikel mag de rest van het Helpcentrum niet platleggen.
                    }
                }
            }

            _articles = loaded;
            _loadedAtUtc = DateTime.UtcNow;
            return _articles;
        }
    }
}
