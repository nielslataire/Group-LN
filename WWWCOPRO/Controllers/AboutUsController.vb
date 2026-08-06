Public Class AboutUsController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /AboutUs
    <Route("AboutUs")>
    Function Index() As ActionResult
        Return View()
    End Function
End Class