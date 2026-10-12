using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CPMCore.Helpers;
using CPMCore.Models.Help;
using CPMCore.Services.Help;
using DALCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartBreadcrumbs.Attributes;

namespace CPMCore.Controllers;

/// <summary>
/// Gl-v2 Helpcentrum (design-handoff punt 41). Content komt uit markdown-bestanden
/// (<see cref="IHelpContentService"/>), geen DB — zie DESIGN.md "Helpcentrum". Geen
/// PermissionCode: het helpcentrum is bedoeld voor élke ingelogde gebruiker, ongeacht rol.
/// </summary>
[Authorize]
[Route("Help")]
public class HelpController : BaseController
{
    private readonly IHelpContentService _help;
    private readonly cpmRunningContext _db;

    public HelpController(IHelpContentService help, cpmRunningContext db)
    {
        _help = help;
        _db = db;
    }

    /// <summary>
    /// Het Helpcentrum heeft geen legacy-tegenhanger — enkel gl-v2-views bestaan hier, dus in
    /// tegenstelling tot de rest van de app (waar UseGlV2Layout afhangt van de preview-cookie,
    /// zie BaseController) mag deze pagina nooit onder de oude _Layout belanden: dat zou een
    /// pagina vol gl-v2-klassen tonen zonder de gl-v2-CSS/rail/topbar eromheen — compleet
    /// onopgemaakt, exact het symptoom dat een gebruiker zonder de preview-cookie zou zien.
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        ViewData["UseGlV2Layout"] = true;
    }

    [HttpGet("")]
    [Breadcrumb("Help", FromController = typeof(HomeController), FromAction = nameof(HomeController.Index))]
    public IActionResult Index()
    {
        SetPageHeader("ph ph-question", "Help", "Helpcentrum", $"{_help.Articles.Count} artikels");
        // Design-handoff punt 43a/41a — het hoofdscherm toont enkel titel + zoekvak + gebruiker,
        // geen kruimelpad (dit IS het topniveau van het Helpcentrum, er is niets om naar te kruimelen).
        ViewData["HideBreadcrumb"] = true;

        var topics = _help.Topics.Select(t =>
        {
            var articles = _help.GetByTopic(t.Slug)
                .OrderBy(a => a.StartOrder ?? int.MaxValue).ThenBy(a => a.Title).ToList();
            return new HelpHomeTopicVm
            {
                Slug = t.Slug,
                Name = t.Name,
                Icon = t.Icon,
                Count = articles.Count,
                TopArticleTitles = articles.Take(3).Select(a => a.Title).ToList(),
                FirstArticleSlug = articles.FirstOrDefault()?.Slug,
            };
        }).ToList();

        var start = _help.Articles
            .Where(a => a.StartOrder.HasValue)
            .OrderBy(a => a.StartOrder)
            .Select((a, i) => new HelpStartItemVm
            {
                Number = i + 1,
                Title = a.Title,
                Description = a.Description ?? "",
                Minutes = a.Minutes > 0 ? $"{a.Minutes} min" : "",
                ArticleSlug = a.Slug,
                Highlighted = i < 2,
            })
            .Take(4)
            .ToList();

        var news = _help.Articles
            .Where(a => !string.IsNullOrEmpty(a.NewsTag))
            .OrderByDescending(a => a.UpdatedOn)
            .Take(4)
            .Select(a => new HelpNewsItemVm
            {
                Tag = a.NewsTag,
                Title = a.Title,
                ArticleSlug = a.Slug,
                Date = a.UpdatedOn?.ToString("dd/MM") ?? "",
                IsNew = string.Equals(a.NewsTag, "Nieuw", StringComparison.OrdinalIgnoreCase),
            })
            .ToList();

        var popularTerms = _help.Articles
            .SelectMany(a => a.Synonyms)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        var vm = new HelpHomeV2Vm
        {
            Topics = topics,
            Start = start,
            News = news,
            PopularTerms = popularTerms,
            ArticleCount = _help.Articles.Count,
        };

        return View("IndexV2", vm);
    }

    [HttpGet("Artikel/{slug}")]
    [Breadcrumb("Artikel", FromController = typeof(HelpController), FromAction = nameof(Index))]
    public IActionResult Artikel(string slug)
    {
        var article = _help.GetBySlug(slug);
        if (article == null) return NotFound();

        var topic = _help.Topics.FirstOrDefault(t => t.Slug == article.TopicSlug)
            ?? new HelpTopic { Slug = article.TopicSlug, Name = article.TopicSlug };

        var tree = _help.Topics.Select(t =>
        {
            var topicArticles = _help.GetByTopic(t.Slug);
            var isCurrentTopic = t.Slug == article.TopicSlug;
            var slugs = topicArticles.Select(a => a.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var firstTopLevel = topicArticles
                .Where(a => string.IsNullOrEmpty(a.ParentSlug) || !slugs.Contains(a.ParentSlug))
                .OrderBy(a => a.TreeOrder ?? int.MaxValue).ThenBy(a => a.Title)
                .FirstOrDefault();
            return new HelpTreeGroupVm
            {
                Name = t.Name,
                IsOpen = isCurrentTopic,
                Count = topicArticles.Count,
                Articles = isCurrentTopic
                    ? BuildTreeArticles(topicArticles, article.Slug)
                    : new List<HelpTreeArticleVm>(),
                FirstArticleSlug = firstTopLevel?.Slug,
            };
        }).ToList();

        var toc = new List<HelpTocItemVm>();
        if (!string.IsNullOrWhiteSpace(article.BeforeHtml)) toc.Add(new HelpTocItemVm { Text = "Voor je begint" });
        if (article.Steps.Count > 0) toc.Add(new HelpTocItemVm { Text = "Zo werkt het" });
        toc.AddRange(article.Steps.Select(s => new HelpTocItemVm { Text = $"{s.Number} · {s.Title}" }));
        if (article.Faq.Count > 0) toc.Add(new HelpTocItemVm { Text = "Veelgestelde vragen" });

        var related = article.RelatedSlugs
            .Select(s => _help.GetBySlug(s))
            .Where(a => a != null)
            .Select(a => new HelpRelatedVm
            {
                Slug = a!.Slug,
                Title = a.Title,
                TopicName = _help.Topics.FirstOrDefault(t => t.Slug == a.TopicSlug)?.Name ?? "",
            })
            .ToList();

        var vm = new HelpArticleV2Vm
        {
            Article = article,
            Topic = topic,
            Tree = tree,
            Toc = toc,
            Related = related,
        };

        SetPageHeader("ph ph-question", article.Title, topic.Name);
        return View("ArtikelV2", vm);
    }

    [HttpGet("Search")]
    public IActionResult Search(string term)
    {
        var hits = _help.Search(term ?? "");
        var groups = new List<object>();

        var articleHits = hits.Where(h => h.Kind == "artikel").ToList();
        if (articleHits.Count > 0)
            groups.Add(new { heading = "ARTIKELS", count = articleHits.Count, items = articleHits });

        var stepHits = hits.Where(h => h.Kind == "stap").ToList();
        if (stepHits.Count > 0)
            groups.Add(new { heading = "STAPPEN & VRAGEN", count = stepHits.Count, items = stepHits });

        return Json(new { groups });
    }

    [HttpPost("Feedback")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(string articleSlug, bool helpful, string message, CancellationToken ct = default)
    {
        var userId = User.GetCpmUserId();
        if (userId == null) return Forbid();

        _db.HelpFeedback.Add(new HelpFeedback
        {
            UserId = userId.Value,
            Kind = helpful ? "ArticleHelpful" : "ArticleNotHelpful",
            ArticleSlug = articleSlug,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        return Json(new { ok = true });
    }

    [HttpPost("Support")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Support(string message, string screenId, CancellationToken ct = default)
    {
        var userId = User.GetCpmUserId();
        if (userId == null) return Forbid();
        if (string.IsNullOrWhiteSpace(message)) return BadRequest();

        _db.HelpFeedback.Add(new HelpFeedback
        {
            UserId = userId.Value,
            Kind = "SupportQuestion",
            ScreenId = string.IsNullOrWhiteSpace(screenId) ? null : screenId.Trim(),
            Message = message.Trim(),
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        return Json(new { ok = true });
    }

    /// <summary>Hoofdartikel gevolgd door haar kinderen (41c-boom, één niveau diep). Een artikel
    /// met een <c>parent:</c> die niet in hetzelfde topic bestaat valt terug op hoofdartikel —
    /// nooit een kind dat in de boom nergens onder hangt.</summary>
    private static List<HelpTreeArticleVm> BuildTreeArticles(IReadOnlyList<HelpArticle> topicArticles, string activeSlug)
    {
        var slugs = topicArticles.Select(a => a.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool HasValidParent(HelpArticle a) => !string.IsNullOrEmpty(a.ParentSlug) && slugs.Contains(a.ParentSlug);

        var result = new List<HelpTreeArticleVm>();
        var topLevel = topicArticles.Where(a => !HasValidParent(a))
            .OrderBy(a => a.TreeOrder ?? int.MaxValue).ThenBy(a => a.Title);
        foreach (var top in topLevel)
        {
            result.Add(new HelpTreeArticleVm { Slug = top.Slug, Title = top.TreeLabel ?? top.Title, IsActive = top.Slug == activeSlug });
            var children = topicArticles.Where(a => string.Equals(a.ParentSlug, top.Slug, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.TreeOrder ?? int.MaxValue).ThenBy(a => a.Title);
            foreach (var child in children)
                result.Add(new HelpTreeArticleVm { Slug = child.Slug, Title = child.TreeLabel ?? child.Title, IsActive = child.Slug == activeSlug, IsChild = true });
        }
        return result;
    }
}
