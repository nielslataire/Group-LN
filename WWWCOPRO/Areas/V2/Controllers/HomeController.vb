Namespace Areas.V2.Controllers

    Public Class HomeController
        Inherits System.Web.Mvc.Controller

        ' GET: /v2/Home
        ' v2-rebuild van de homepage, los van WWWCOPRO.Controllers.HomeController (die blijft de
        ' live site bedienen, onaangeroerd). Zelfde query-logica, nu gedeeld via
        ' Helpers/HomeHeroDataHelper.vb — geen duplicatie van de BuildHeroSearchOptions /
        ' GetHomeHeroFeatured-logica meer.
        Function Index() As ActionResult
            ViewData("HeroSearchOptions") = HomeHeroDataHelper.BuildHeroSearchOptions()
            ViewData("HomeHeroFeatured") = HomeHeroDataHelper.GetHomeHeroFeatured(Url)
            Return View()
        End Function

    End Class

End Namespace
