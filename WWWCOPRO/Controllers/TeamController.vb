Public Class TeamController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /Team
    <Route("Team")>
    Function Index() As ActionResult
        Return View()
    End Function

    ' Oude URL (/ons-team) permanent doorsturen naar de nieuwe, schone /team.
    <Route("ons-team", Name:="TeamLegacy")>
    Function IndexLegacy() As ActionResult
        Return RedirectPermanent("/team")
    End Function

End Class