Public Class AboutUsController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /over-ons  (route-vertaling "AboutUs" -> "over-ons", zie RouteTranslations.vb)
    ' Bestaande pagina — voorlopig ongewijzigd. Wordt later vervangen door Overzicht().
    <Route("AboutUs")>
    Function Index() As ActionResult
        Return View()
    End Function

    '
    ' GET: /over-ons-nieuw
    ' Nieuwe "Over ons" hoofdpagina (hub). Draait tijdelijk op een eigen slug zodat de
    ' bestaande /over-ons (Index) intact blijft. Bij livegang:
    '  1. RouteTranslations.vb: de vertaling "over-ons" naar deze actie (Overzicht) verleggen
    '  2. Index() + Views/AboutUs/Index.vbhtml verwijderen
    '  3. de <meta robots="noindex"> in Overzicht.vbhtml schrappen
    <Route("over-ons-nieuw", Name:="OverOnsHub")>
    Function Overzicht() As ActionResult
        ViewData("HeroHeader") = True
        ViewData("StickyStartAt") = 20
        ViewData("Title") = "Over Group LN | Projectontwikkelaar in Gent en Oost-Vlaanderen"
        ViewData("MetaDescription") = "Group LN ontwikkelt en begeleidt residentiële en commerciële bouwprojecten in Gent en Oost-Vlaanderen, van eerste ontwerp tot definitieve oplevering."
        Return View()
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
        ViewData("HeroHeader") = True
        ViewData("StickyStartAt") = 20
        ViewData("Title") = "Grond of pand verkopen aan projectontwikkelaar | Group LN"
        ViewData("MetaDescription") = "Bouwgrond, een oude woning of verouderd pand? Wij onderzoeken de mogelijkheden, doen een concurrentieel voorstel en nemen het volledige traject uit handen."
        ViewData("canonical") = "https://www.groupln.be/grond-of-pand-aanbieden"
        Return View()
    End Function

End Class
