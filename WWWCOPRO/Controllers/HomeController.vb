Imports BO

Public Class HomeController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /Home
    <Route("Home/Index", name:="HomeIndex")>
    <Route("Home", name:="Home")>
    <Route("~/", name:="defaultroute")>
    Function Index() As ActionResult
        ' /welkom (en /Home, /Home/Index) zijn allemaal alternatieve routes naar dezelfde
        ' homepage — permanent doorsturen naar de echte root, zodat er maar één indexeerbare
        ' URL voor de homepage bestaat.
        If Request.Url.AbsolutePath <> "/" Then
            Return RedirectPermanent("/")
        End If

        'Dim model As New ProjectModel
        ' Query-logica geëxtraheerd naar Helpers/HomeHeroDataHelper.vb (gedeeld met
        ' Areas/V2/Controllers/HomeController) — zelfde gedrag, niet meer gedupliceerd.
        ViewData("HeroSearchOptions") = HomeHeroDataHelper.BuildHeroSearchOptions()
        ViewData("HomeHeroFeatured") = HomeHeroDataHelper.GetHomeHeroFeatured(Url)
        Return View()
    End Function

End Class