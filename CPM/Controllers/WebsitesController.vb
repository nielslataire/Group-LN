Imports System.Web.Mvc

Namespace Controllers
    <Authorize>
    Public Class WebsitesController
        Inherits Controllers.ApplicationBaseController

        ' GET: Websitescontroller
        Function Index() As ActionResult
            Return View()
        End Function

        Function BCO() As ActionResult
            ViewData("Message") = "BCO Website"
            ViewBag.Subtitle() = "Bco website"
            Return View()
        End Function
        Function Copro() As ActionResult
            ViewData("Message") = "BCO Website"
            ViewBag.Subtitle() = "Bco website"
            Return View()
        End Function
        Function Verspurten() As ActionResult
            ViewData("Message") = "BCO Website"
            ViewBag.Subtitle() = "Bco website"
            Return View()
        End Function
    End Class
End Namespace