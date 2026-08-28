Imports System.Web.Mvc

Public Class LegalController
    Inherits System.Web.Mvc.Controller

    ' GET: /privacybeleid
    <Route("privacybeleid", Name:="Privacybeleid")>
    Function Privacybeleid() As ActionResult
        ViewData("Title") = "Privacybeleid | Group LN"
        ViewData("MetaDescription") = "Hoe Group LN BV omgaat met je persoonsgegevens: welke gegevens we verwerken, waarom, hoe lang en welke rechten je hebt."
        ViewData("canonical") = "https://www.groupln.be/privacybeleid"
        Return View()
    End Function

    ' GET: /cookiebeleid
    <Route("cookiebeleid", Name:="Cookiebeleid")>
    Function Cookiebeleid() As ActionResult
        ViewData("Title") = "Cookiebeleid | Group LN"
        ViewData("MetaDescription") = "Welke cookies de website van Group LN gebruikt, waarvoor ze dienen en hoe je je voorkeuren beheert."
        ViewData("canonical") = "https://www.groupln.be/cookiebeleid"
        Return View()
    End Function

    ' GET: /algemene-voorwaarden
    <Route("algemene-voorwaarden", Name:="AlgemeneVoorwaarden")>
    Function AlgemeneVoorwaarden() As ActionResult
        ViewData("Title") = "Algemene voorwaarden | Group LN"
        ViewData("MetaDescription") = "De voorwaarden voor het gebruik van de website van Group LN BV."
        ViewData("canonical") = "https://www.groupln.be/algemene-voorwaarden"
        Return View()
    End Function

End Class
