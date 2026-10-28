using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using SuperSimpleBreadcrumbs.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CPMCore.Controllers
{
    [BreadcrumbActionFilter]
    public class BaseController : Controller
    {
        public const string GlV2PreviewCookie = "gl_v2_preview";

        /// <summary>
        /// gl-v2 layout-pilot (design-handoff/): zet ViewData["UseGlV2Layout"] wanneer de
        /// preview-cookie aanwezig is, zodat Views/_ViewStart.cshtml naar _LayoutV2 schakelt.
        /// Zonder cookie (elke gebruiker die de pilot niet expliciet aanzette via
        /// LayoutPreviewController) is dit een no-op en blijft alles exact zoals vandaag.
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Schermen zonder gl-v2-versie blijven in de klassieke shell (zie ClassicLayoutAttribute).
            if (context.ActionDescriptor.EndpointMetadata.OfType<CPMCore.Attributes.ClassicLayoutAttribute>().Any())
                return;

            // Voorkeur van de gebruiker: cookie "1" = gl-v2 aan, "0" = klassiek (blijft ook als de standaard gl-v2 is). Zonder cookie geldt de
            // standaardinstelling appsettings "Layout:GlV2Default" (true = gl-v2 voor iedereen, false = enkel wie het via /LayoutPreview/On aanzette).
            var cookie = Request.Cookies[GlV2PreviewCookie];
            var standaard = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) is Microsoft.Extensions.Configuration.IConfiguration cfg
                && cfg.GetValue<bool>("Layout:GlV2Default");
            if (cookie == "1" || (standaard && cookie != "0"))
            {
                ViewData["UseGlV2Layout"] = true;
            }
        }

        public void AddMessage(string messagetype, string message, string messagetitle)
        {
            TempData["Message"] = message;
            TempData["MessageType"] = messagetype;
            TempData["MessageTitle"] = messagetitle;
        }

        /// <summary>
        /// Vult de header-gegevens die _Layout toont: icoon (boxicons-klasse) en titel in de
        /// topbar, en optioneel kicker/beschrijving voor het titelblok bovenaan de pagina.
        /// </summary>
        public void SetPageHeader(string icon, string title, string? kicker = null, string? description = null)
        {
            ViewData["PageIcon"] = icon;
            ViewData["Title"] = title;
            ViewData["PageKicker"] = kicker;
            ViewData["PageDescription"] = description;
        }
    }
}
