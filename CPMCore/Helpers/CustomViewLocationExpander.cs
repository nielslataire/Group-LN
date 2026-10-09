using Microsoft.AspNetCore.Mvc.Razor;

namespace CPMCore.Helpers
{
    /// <summary>Mappenstructuur van Views/ (okt. 2026, "views/controllers/models structureren",
    /// STRUCTUREREN_VOORTGANG.md). Twee regels, allebei zonder dat een controller zijn
    /// <c>return View(model)</c> / <c>View("Naam")</c> hoeft aan te passen:
    /// <list type="number">
    /// <item><b>Functiemappen</b> (<see cref="FeatureFolders"/>): een paginaview mag in een submap per
    /// functie staan — Views/Projecten/Budget/BudgetIndex.cshtml — met dezelfde naam als de
    /// controller-partial (ProjectenController.Budget.cs). Geldt voor élke controller ({1}); een
    /// nieuwe mapnaam hoort hier erbij. Eén viewnaam mag maar in één functiemap voorkomen.</item>
    /// <item><b>Clusters</b> (<see cref="ControllerFolders"/>): satelliet-controllers wonen in de map
    /// van hun module — UserAdminController in Views/Instellingen/UserAdmin/, ProjectsIssuesController
    /// in Views/Projecten/Issues/. Routes en controllers veranderen niet; enkel waar de views staan.
    /// Die mappen staan bewust NIET in FeatureFolders (ze hebben eigen Index/Details die anders met
    /// die van de module zouden botsen).</item>
    /// </list>
    /// Absolute paden (<c>"~/Views/…"</c>) omzeilen dit en moeten de submap zelf noemen. Volgorde:
    /// cluster, functiemappen, Modals/Partials/Shared, dan de standaardlocaties — Razor cachet een
    /// gevonden view, dus de extra zoekpaden kosten enkel iets bij de eerste aanroep.</summary>
    public class CustomViewLocationExpander : IViewLocationExpander
    {
        private static readonly string[] FeatureFolders =
        {
            // Projecten
            "Core", "Clients", "Units", "Contracts", "IncomingInvoices", "ChangeOrders", "Weather",
            "Media", "Docs", "Insurances", "Sales", "Coordinatie", "Invoicing", "Budget",
            // Instellingen
            "Algemeen", "Facturatie", "Marktdata",
            // Klanten (Core, Lookups), Invoices (Core, Helpers), Leveranciers (Core, Lookups)
            "ClientAccount", "Editor", "Send", "Portal",
        };

        private static readonly Dictionary<string, string> ControllerFolders = new(StringComparer.OrdinalIgnoreCase)
        {
            // Instellingen-hub
            ["UserAdmin"]              = "Instellingen/UserAdmin",
            ["AppRoles"]               = "Instellingen/AppRoles",
            ["EmailTemplateBeheer"]    = "Instellingen/EmailTemplateBeheer",
            ["BlogBeheer"]             = "Instellingen/BlogBeheer",
            ["VacatureBeheer"]         = "Instellingen/VacatureBeheer",
            ["CookieConsentStats"]     = "Instellingen/CookieConsentStats",
            ["HomeHeroProject"]        = "Instellingen/HomeHeroProject",
            ["IssueNotificationAdmin"] = "Instellingen/IssueNotificationAdmin",
            ["TrajectSjabloonAdmin"]   = "Instellingen/TrajectSjabloonAdmin",
            // Projecten-satellieten
            ["ProjectDossiers"]        = "Projecten/Dossiers",
            ["ProjectTraject"]         = "Projecten/Traject",
            ["ProjectsIssues"]         = "Projecten/Issues",
            ["ProjectVerslagen"]       = "Projecten/Verslagen",
            ["Pdf"]                    = "Projecten/Contracts",
            // Ondertekenen (twee implementaties, bewust apart gehouden — zie ONDERTEKENEN_VOORTGANG.md)
            ["Signing"]                = "Ondertekenen/Signing",
            ["Verifieer"]              = "Ondertekenen/Verifieer",
            ["SigningAdmin"]           = "Ondertekenen/SigningAdmin",
            // Werfportaal (aannemers)
            ["ContractorPortal"]       = "Werfportaal/ContractorPortal",
            ["ContractorInvite"]       = "Werfportaal/ContractorInvite",
        };

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            // niets nodig hier
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var customLocations = new List<string>();
            if (context.ControllerName is { } controller && ControllerFolders.TryGetValue(controller, out var clusterFolder))
                customLocations.Add("/Views/" + clusterFolder + "/{0}.cshtml");
            foreach (var folder in FeatureFolders)
                customLocations.Add("/Views/{1}/" + folder + "/{0}.cshtml");
            customLocations.Add("/Views/{1}/Modals/{0}.cshtml");
            customLocations.Add("/Views/{1}/Partials/{0}.cshtml");
            customLocations.Add("/Views/{1}/Shared/{0}.cshtml"); // {1} is de controllernaam

            return customLocations.Concat(viewLocations);
        }
    }
}
