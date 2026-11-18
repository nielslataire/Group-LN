Public Class AboutUsController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /over-ons  (route-vertaling "AboutUs" -> "over-ons", zie RouteTranslations.vb)
    ' De nieuwe "Over ons"-hub. Vervangt de vroegere Index-pagina.
    <Route("AboutUs", Name:="OverOnsHub")>
    Function Index() As ActionResult
        ViewData("HeroHeader") = True
        ViewData("StickyStartAt") = 20
        ViewData("Title") = "Over Group LN | Projectontwikkelaar in Gent en Oost-Vlaanderen"
        ViewData("MetaDescription") = "Group LN ontwikkelt en begeleidt residentiële en commerciële bouwprojecten in Gent en Oost-Vlaanderen, van eerste ontwerp tot definitieve oplevering."
        ViewData("canonical") = "https://www.groupln.be/over-ons"
        Return View()
    End Function

    '
    ' GET: /over-ons-nieuw  — oude preview-URL, permanent doorsturen naar /over-ons
    <Route("over-ons-nieuw")>
    Function OverOnsNieuw() As ActionResult
        Return RedirectToActionPermanent("Index")
    End Function

    '
    ' GET: /projectbegeleiding
    <Route("projectbegeleiding", Name:="Projectbegeleiding")>
    Function Projectmanagement() As ActionResult
        ViewData("HeroHeader") = True
        ViewData("StickyStartAt") = 20
        ViewData("Title") = "Projectbegeleiding | Group LN"
        ViewData("MetaDescription") = "Group LN begeleidt uw bouwproject van ontwerp tot definitieve oplevering: aanbesteding, werfopvolging, budgetbewaking, klantenopvolging en nazorg."
        ViewData("canonical") = "https://www.groupln.be/projectbegeleiding"
        Return View()
    End Function

    '
    ' GET: /grond-of-pand-aanbieden
    <Route("grond-of-pand-aanbieden", Name:="Grondverwerving")>
    Function Grondverwerving() As ActionResult
        ApplyGrondViewData(ViewData)
        ViewBag.ReCaptchaSiteKey = System.Configuration.ConfigurationManager.AppSettings("ReCaptchaV3SiteKey")
        ViewBag.ReCaptchaAction = "contact"
        Return View()
    End Function

    ' Gedeeld met ContactController.GrondAanbieden, die deze pagina opnieuw toont bij een fout in het formulier.
    Friend Shared Sub ApplyGrondViewData(vd As System.Web.Mvc.ViewDataDictionary)
        vd("HeroHeader") = True
        vd("StickyStartAt") = 20
        vd("Title") = "Grond of pand verkopen aan projectontwikkelaar | Group LN"
        vd("MetaDescription") = "Bouwgrond, een oude woning of verouderd pand? Wij onderzoeken de mogelijkheden, doen een concurrentieel voorstel en nemen het volledige traject uit handen."
        vd("canonical") = "https://www.groupln.be/grond-of-pand-aanbieden"
    End Sub

End Class
