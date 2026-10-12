using System.Linq;
using CPMCore.Models.Help;
using CPMCore.Services.Help;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Gl-v2 contextueel help-zijpaneel (design-handoff punt 41d). Elke gl-v2-pagina rendert dit
/// ongevraagd mee via @@await Component.InvokeAsync("HelpPanel", ViewData["HelpScreenId"]) —
/// zelfde "altijd in de DOM, JS toont/verbergt"-patroon als het meldingenpaneel, maar de
/// "?"-topbarknop die het opent is (bewust, anders dan het belletje) nooit achter een ViewData-gate
/// verstopt: Help moet overal bereikbaar zijn, ook op een pagina zonder eigen screen-id.
/// </summary>
public class HelpPanelViewComponent : ViewComponent
{
    private readonly IHelpContentService _help;
    public HelpPanelViewComponent(IHelpContentService help) => _help = help;

    public IViewComponentResult Invoke(string screenIds)
    {
        var ids = (screenIds ?? "").Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        var articles = ids.Length > 0 ? _help.GetByScreenIds(ids) : System.Array.Empty<HelpArticle>();

        var vm = new HelpPanelVm
        {
            Articles = articles.OrderBy(a => a.Title).ToList(),
        };

        return View(vm);
    }
}
