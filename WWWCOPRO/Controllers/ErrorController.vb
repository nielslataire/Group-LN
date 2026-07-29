Public Class ErrorController
    Inherits System.Web.Mvc.Controller

    <Route("error/404", Name:="Error404")>
    Function NotFound404() As ActionResult
        Response.StatusCode = 404
        Response.TrySkipIisCustomErrors = True
        ViewData("Title") = "Pagina niet gevonden | Group LN"
        Return View("NotFound")
    End Function

    <Route("error/500", Name:="Error500")>
    Function ServerError500() As ActionResult
        Response.StatusCode = 500
        Response.TrySkipIisCustomErrors = True
        ViewData("Title") = "Er ging iets mis | Group LN"
        Return View("ServerError")
    End Function

End Class
