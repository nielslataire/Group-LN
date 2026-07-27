using Microsoft.AspNetCore.Mvc;
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
