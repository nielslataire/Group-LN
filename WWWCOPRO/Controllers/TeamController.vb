Public Class TeamController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /Team
    <Route("Team")>
    Function Index() As ActionResult
        Return View()
    End Function

End Class