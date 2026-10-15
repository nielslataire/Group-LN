Imports System.Globalization
Imports System.Linq
Imports System.Web
Imports System.Web.Mvc

' ViewModel voor Views/Projects/DetailV4 (design "Project Detail v4 Split").
'
' Alles wat de pagina toont zit in dit model: per blok een Visible-vlag + de teksten en
' beeld-URL's. De view bevat zelf geen projectspecifieke tekst. Het model is bewust een
' platte POCO-boom zodat het later als JSON uit CPMCore (blokconfiguratie per project)
' gedeserialiseerd kan worden. Zie ProjectPageV4Provider voor de plek waar het model
' opgebouwd wordt.

Public Class ProjectPageV4Model
    Public Sub New()
        Media = New List(Of V4Media)
        Lots = New List(Of V4Lot)
        Intro = New V4IntroBlock
        Story = New V4StoryBlock
        Architecture = New V4ArchitectureBlock
        Homes = New V4HomesBlock
        Location = New V4LocationBlock
        BuildPhase = New V4BuildPhaseBlock
        Buy = New V4BuyBlock
        Faq = New V4FaqBlock
        Contact = New V4ContactBlock
        Comfort = New V4ComfortBlock
        Documents = New V4DocumentsBlock
    End Sub

    ' ── Identiteit / SEO ──
    Public Property ProjectId As Integer
    Public Property Slug As String
    Public Property Name As String
    Public Property Deelgemeente As String
    Public Property Gemeente As String
    Public Property Provincie As String
    Public Property Street As String
    ' Straatnaam zonder huisnummer (label rechtsboven op de foto)
    Public Property StreetName As String
    Public Property Postcode As String
    Public Property Lat As Double
    Public Property Lng As Double
    Public Property MetaTitle As String
    Public Property MetaDescription As String
    Public Property CanonicalUrl As String
    Public Property OgImageUrl As String

    ' ── Linkerkolom: beelden die crossfaden per blok ──
    ' Key wordt door een blok (MediaKey) en door de lot-renders ("lot-{Id}") gebruikt.
    Public Property Media As List(Of V4Media)

    ' Overschrijft de automatisch berekende hero-tekst (badge / regel / prijs) indien ingevuld.
    Public Property HeroBadgeOverride As String
    Public Property HeroLineOverride As String
    Public Property HeroPriceOverride As String

    ' ── Woningen (gedeeld door kaart, prijslijst, calculator, formulier, JSON-LD) ──
    Public Property Lots As List(Of V4Lot)

    ' Volgorde van de sorteerbare blokken (sleutels: story, architecture, homes, location, buildphase, buy, faq).
    ' De intro staat altijd bovenaan, het contactformulier altijd onderaan.
    Public Property BlockOrder As New List(Of String) From {"story", "architecture", "comfort", "homes", "documents", "location", "buildphase", "buy", "faq"}

    ' ── Blokken ──
    Public Property Intro As V4IntroBlock
    Public Property Story As V4StoryBlock
    Public Property Architecture As V4ArchitectureBlock
    Public Property Homes As V4HomesBlock
    Public Property Location As V4LocationBlock
    Public Property BuildPhase As V4BuildPhaseBlock
    Public Property Buy As V4BuyBlock
    Public Property Faq As V4FaqBlock
    Public Property Contact As V4ContactBlock
    Public Property Comfort As V4ComfortBlock
    Public Property Documents As V4DocumentsBlock

    ' Mobiele CTA-balk (< 1000px)
    Public Property ShowMobileBar As Boolean = True
    Public Property Phone As String = "+32 (0)9 216 49 50"
    Public Property PhoneHref As String = "+3292164950"

    ' Endpoints voor de formulieren. Leeg = formulier toont enkel de bevestiging (geen verzending).
    ' Brochure-document van het project (ProjectDoc.IsBrochure): de aanvraag loopt via Projects/SendBrochure (zelfde mail als op de gewone pagina)
    Public Property BrochureDocId As Integer
    Public Property ContactPostUrl As String
    Public Property BrochurePostUrl As String
    Public Property UpdatesPostUrl As String

    ' ── Afgeleide waarden ──
    Public ReadOnly Property AvailableLots As List(Of V4Lot)
        Get
            Return Lots.Where(Function(l) l.IsAvailable).ToList()
        End Get
    End Property

    ' "Vanaf"-prijs voor hero, balk en JSON-LD: de hoofdprijs (eerste afwerking) per beschikbaar lot.
    Public ReadOnly Property MinPrice As Decimal?
        Get
            Dim prices = AvailableLots.Where(Function(l) l.HasPrice).Select(Function(l) l.FromPrice.Value).ToList()
            If prices.Count = 0 Then Return Nothing
            Return prices.Min()
        End Get
    End Property

    ' Laagste prijs over alle afwerkingen heen (prijslijst-samenvatting).
    Public ReadOnly Property MinPriceAnyFinish As Decimal?
        Get
            Dim prices = AvailableLots.SelectMany(Function(l) l.Finishes.Select(Function(f) f.Price)).ToList()
            If prices.Count = 0 Then Return Nothing
            Return prices.Min()
        End Get
    End Property

    Public ReadOnly Property HeroBadge As String
        Get
            If Not String.IsNullOrWhiteSpace(HeroBadgeOverride) Then Return HeroBadgeOverride
            Dim n = AvailableLots.Count
            If n = 0 Then Return "Uitverkocht"
            If n = 1 Then Return "Laatste woning beschikbaar"
            Return "Nog " & n & " van " & Lots.Count & " woningen vrij"
        End Get
    End Property

    Public ReadOnly Property HeroLine As String
        Get
            If Not String.IsNullOrWhiteSpace(HeroLineOverride) Then Return HeroLineOverride
            Dim av = AvailableLots
            If av.Count = 0 Then Return ""
            If av.Count = 1 Then Return av(0).Name & " · " & av(0).Type.ToLowerInvariant() & " · " & av(0).LivingArea & " m²"
            Dim types = String.Join(" & ", av.Select(Function(l) l.Type.ToLowerInvariant()).Distinct())
            Return av.Count & " woningen · " & types & " · " & V4Format.Range(av.Select(Function(l) CDec(l.LivingArea)), " m²")
        End Get
    End Property

    Public ReadOnly Property HeroPrice As String
        Get
            If Not String.IsNullOrWhiteSpace(HeroPriceOverride) Then Return HeroPriceOverride
            If Not MinPrice.HasValue Then Return ""
            Return "Vanaf " & V4Format.Eur(MinPrice.Value)
        End Get
    End Property

    ' Titel van het contactblok volgens de logica uit de handoff (client-side wordt die live bijgewerkt).
    Public ReadOnly Property ContactTitleDefault As String
        Get
            If Not String.IsNullOrWhiteSpace(Contact.CustomTitle) Then Return Contact.CustomTitle
            Dim av = AvailableLots
            If av.Count = 1 Then Return av(0).Name & " kan de jouwe zijn."
            Return Contact.TitleMany
        End Get
    End Property

    Public ReadOnly Property MobileBarLine As String
        Get
            Dim av = AvailableLots
            If av.Count = 0 OrElse Not MinPrice.HasValue Then Return ""
            If av.Count = 1 Then Return av(0).Name & " · vanaf " & V4Format.Eur(MinPrice.Value)
            Return av.Count & " woningen vrij · vanaf " & V4Format.Eur(MinPrice.Value)
        End Get
    End Property
End Class

' ───────────────────────── bouwstenen ─────────────────────────

' Een projectfoto uit de opslag (Storage API). HasVariants = WebP-varianten op breedte bestaan (nieuwe upload); anders enkel de
' oude map "pictures". Width/Height = afmetingen van het origineel (voor width/height-attributen, geen layout-verspringing).
Public Class V4Pic
    Public Shared ReadOnly Widths As Integer() = {320, 640, 960, 1280, 1920, 2560}
    Public Property Base As String
    Public Property Name As String
    Public Property Width As Integer
    Public Property Height As Integer
    Public Property HasVariants As Boolean

    Public Function LegacyUrl() As String
        Return Base & "pictures/" & Name
    End Function

    Public Function VariantUrl(w As Integer) As String
        Return Base & "pictures/w" & w & "/" & Name
    End Function

    ' Breedtes die bestaan: nooit groter dan het origineel (onbekend: tot 1280)
    Public Function AvailableWidths() As List(Of Integer)
        If Not HasVariants Then Return New List(Of Integer)
        Dim limit As Integer = If(Width > 0, Width, 1280)
        Return Widths.Where(Function(w) w <= limit).ToList()
    End Function

    ' Grootste beschikbare variant tot maxWidth (bv. voor og:image); anders de oude map
    Public Function BestUrl(maxWidth As Integer) As String
        Dim ok = AvailableWidths().Where(Function(w) w <= maxWidth).ToList()
        If ok.Count = 0 Then Return LegacyUrl()
        Return VariantUrl(ok.Max())
    End Function
End Class

Public Class V4Media
    Public Property Pic As V4Pic
    Public Property Key As String
    Public Property Label As String
    Public Property ImageUrl As String
    Public Property Alt As String
End Class

Public Class V4Quote
    Public Property Text As String
    Public Property Person As String
End Class

Public Class V4Fact
    Public Property Label As String
    Public Property Value As String
End Class

Public Class V4Figure
    Public Property Pic As V4Pic
    Public Property ImageUrl As String
    Public Property Alt As String
    Public Property Text As String
End Class

Public Class V4Material
    Public Property Pic As V4Pic
    Public Property Title As String
    Public Property Text As String
    Public Property ImageUrl As String
    Public Property Alt As String
End Class

Public Class V4TitleText
    Public Property Title As String
    Public Property Text As String
End Class

Public Class V4Poi
    Public Property Category As String
    Public Property Name As String
    Public Property Lat As Double
    Public Property Lng As Double
    Public Property Distance As String
    Public Property Time As String
End Class

Public Class V4PoiCategory
    Public Property Id As String
    Public Property Label As String
    Public Property Icon As String
    Public Property DefaultOn As Boolean
End Class

Public Class V4Travel
    Public Property Destination As String
    Public Property Duration As String
End Class

Public Class V4Milestone
    Public Property DateText As String
    Public Property Title As String
    ' "done" | "now" | "next"
    Public Property Status As String
End Class

Public Class V4Doc
    Public Property Id As Integer
    ' De projectbrochure: opent het brochureformulier (zelfde mail met bijlage) i.p.v. het algemene documentformulier
    Public Property IsBrochure As Boolean
    Public Property Title As String
    Public Property Meta As String
    Public Property Url As String
End Class

Public Class V4Finish
    ' Kort label in de prijslijst ("Afgewerkt" / "Casco") en lang in de calculator.
    Public Property ShortLabel As String
    Public Property Label As String
    Public Property Price As Decimal
    Public Property Description As String
End Class

Public Class V4Lot
    Public Sub New()
        Finishes = New List(Of V4Finish)
    End Sub
    Public Property Id As Integer
    Public Property Name As String
    Public Property Type As String
    Public Property LivingArea As Integer
    Public Property LandArea As Integer
    Public Property Bedrooms As Integer
    Public Property Bathrooms As Integer
    Public Property GardenOrientation As String
    Public Property EnergyLevel As String
    ' "Beschikbaar" | "Optie" | "Verkocht"
    Public Property Status As String
    Public Property Description As String
    Public Property RenderUrl As String
    ' Heeft een grondplan: de download loopt via hetzelfde formulier/modal als op de gewone projectpagina (Projects/SendPlan)
    Public Property HasPlan As Boolean
    ' Perceelcontour in % van de luchtfoto: "x,y x,y x,y"
    Public Property Polygon As String
    Public Property LabelX As Double
    Public Property LabelY As Double
    ' Zoomfactor bij selectie; 0 = standaard (2,1)
    Public Property Zoom As Double
    Public Property Finishes As List(Of V4Finish)

    Public ReadOnly Property IsAvailable As Boolean
        Get
            Return Status = "Beschikbaar"
        End Get
    End Property

    ' Prijs bekend en zichtbaar (anders "op aanvraag")
    Public ReadOnly Property HasPrice As Boolean
        Get
            Return Finishes.Count > 0
        End Get
    End Property

    Public ReadOnly Property IsSold As Boolean
        Get
            Return Status = "Verkocht"
        End Get
    End Property

    Public ReadOnly Property TypeLabel As String
        Get
            Return Type & " bebouwing"
        End Get
    End Property

    Public ReadOnly Property FromPrice As Decimal?
        Get
            ' Eerste afwerking = hoofdprijs (volledig afgewerkt) zoals in het design
            If Finishes.Count = 0 Then Return Nothing
            Return Finishes(0).Price
        End Get
    End Property
End Class

' ───────────────────────── blokken ─────────────────────────
' Elk blok: Visible + MediaKey (welk linkerbeeld hoort erbij) + inhoud.

Public Class V4IntroBlock
    Public Sub New()
        Visible = True
        MediaKey = "intro"
        Paragraphs = New List(Of String)
        Facts = New List(Of V4Fact)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Eyebrow As String
    Public Property TitleLead As String
    ' Cursief groen tweede deel van de H1
    Public Property TitleEmphasis As String
    Public Property Paragraphs As List(Of String)
    Public Property Facts As List(Of V4Fact)
    Public Property PrimaryCtaLabel As String = "Informatie aanvragen"
    Public Property SecondaryCtaLabel As String = "Bekijk de prijzen"
End Class

Public Class V4StoryBlock
    Public Sub New()
        Visible = True
        MediaKey = "story"
        Figures = New List(Of V4Figure)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Title As String
    Public Property Intro As String
    Public Property Figures As List(Of V4Figure)
    Public Property ShowBrochureCta As Boolean = True
    Public Property BrochureEyebrow As String = "Brochure"
    Public Property BrochureTitle As String = "De brochure met alle materialen, afwerkingen en de volledige beschrijving."
    Public Property BrochureText As String = "We sturen ze meteen naar je mailbox."
    Public Property BrochureButton As String = "Vraag de brochure aan"
    Public Property BrochureSuccess As String = "✓ De brochure is onderweg naar je mailbox."
End Class

Public Class V4ArchitectureBlock
    Public Sub New()
        Visible = True
        MediaKey = "architecture"
        Paragraphs = New List(Of String)
        Materials = New List(Of V4Material)
        Techniques = New List(Of V4TitleText)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Eyebrow As String
    Public Property Title As String
    Public Property Paragraphs As List(Of String)
    Public Property Quotes As New List(Of V4Quote)
    Public Property Materials As List(Of V4Material)
    Public Property ComfortTitle As String
    Public Property ComfortIntro As String
    Public Property Techniques As List(Of V4TitleText)
    Public Property ShowSpecCta As Boolean = True
    Public Property SpecCtaTitle As String
    Public Property SpecCtaText As String
    Public Property SpecCtaLabel As String
    Public Property SpecCtaUrl As String
End Class

Public Class V4HomesBlock
    Public Sub New()
        Visible = True
        MediaKey = "homes"
        ShowMap = True
        ShowPriceList = True
        ' Maandlastcalculator ("Wat betaal je per maand?" + "≈ … / maand" in de prijslijst) is voorlopig verborgen; op True zetten om ze terug te tonen
        ShowCalculator = False
        ShowDocuments = False ' vervangen door het eigen blok "Documenten" (V4DocumentsBlock)
        Documents = New List(Of V4Doc)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Title As String
    Public Property Intro As String
    ' Luchtfoto onder de interactieve projectkaart
    Public Property AerialImageUrl As String
    Public Property AerialAlt As String
    Public Property AerialPic As V4Pic
    Public Property ShowMap As Boolean
    Public Property ShowPriceList As Boolean
    ' Prijzen tonen (verkoop zichtbaar); anders toont de lijst enkel beschikbaarheid en "op aanvraag"
    Public Property ShowPrices As Boolean = True
    Public Property PriceListNote As String
    Public Property PriceListUpdated As String
    Public Property ShowCalculator As Boolean
    Public Property CalculatorTitle As String = "Wat betaal je per maand?"
    Public Property CalculatorIntro As String = "Kies een woning en stel de lening in naar jouw situatie."
    Public Property CalculatorDisclaimer As String = "Indicatieve berekening, geen kredietaanbod. Excl. btw-, registratie- en notariskosten."
    Public Property CalculatorDefaultDownPayment As Integer = 120000
    Public Property CalculatorDefaultRate As Double = 3.2
    Public Property CalculatorDefaultYears As Integer = 25
    Public Property ShowDocuments As Boolean
    Public Property Documents As List(Of V4Doc)
    Public Property SpecDocUrl As String
End Class

Public Class V4LocationBlock
    Public Sub New()
        Visible = True
        MediaKey = "location"
        Guide = New List(Of V4TitleText)
        Categories = New List(Of V4PoiCategory)
        Pois = New List(Of V4Poi)
        TravelTimes = New List(Of V4Travel)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Title As String = "Ligging"
    Public Property Intro As String
    Public Property Guide As List(Of V4TitleText)
    Public Property ShowMap As Boolean = True
    Public Property Categories As List(Of V4PoiCategory)
    Public Property Pois As List(Of V4Poi)
    Public Property TravelTimes As List(Of V4Travel)
    Public Property ShowCta As Boolean = True
    Public Property CtaTitle As String
    Public Property CtaText As String
End Class

Public Class V4BuildPhaseBlock
    Public Sub New()
        Visible = True
        MediaKey = "buildphase"
        Milestones = New List(Of V4Milestone)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Title As String = "Bouwfase"
    Public Property ProgressPercent As Integer
    Public Property Milestones As List(Of V4Milestone)
    Public Property ShowUpdatesCta As Boolean = True
    Public Property UpdatesTitle As String = "Volg de werf"
    Public Property UpdatesText As String = "Ontvang maandelijks een korte update met foto's van de bouw."
    Public Property UpdatesButton As String = "Hou mij op de hoogte"
    Public Property UpdatesSuccess As String = "✓ Je ontvangt de volgende werfupdate."
End Class

Public Class V4BuyBlock
    Public Sub New()
        Visible = True
        MediaKey = "buy"
        Steps = New List(Of V4TitleText)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Title As String = "Zo verloopt de aankoop"
    Public Property Steps As List(Of V4TitleText)
    Public Property CtaText As String = "De eerste stap is een vrijblijvend gesprek."
    Public Property CtaLabel As String = "Vraag een kennismaking"
    Public Property CtaPrefilledMessage As String = "Ik wil graag een vrijblijvend kennismakingsgesprek."
End Class

Public Class V4FaqBlock
    Public Sub New()
        Visible = True
        MediaKey = "buy"
        Items = New List(Of V4TitleText)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Title As String = "Vragen"
    ' Title = vraag, Text = antwoord. Dezelfde tekst gaat ook naar de FAQPage JSON-LD.
    Public Property Items As List(Of V4TitleText)
    Public Property CtaText As String = "Staat je vraag er niet bij?"
    Public Property CtaLabel As String = "Stel je vraag"
End Class

Public Class V4ContactBlock
    Public Sub New()
        Visible = True
        MediaKey = "contact"
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property TitleMany As String = "Welke woning wordt de jouwe?"
    ' Eigen titel uit CPMCore: vast, de titel past zich dan niet meer aan de gekozen woningen aan
    Public Property CustomTitle As String
    Public Property Intro As String = "Je ontvangt de brochure, plannen en prijslijst binnen één werkdag. Je contactpersoon belt je enkel als je dat wil."
    Public Property ConsentText As String = "Ik ga akkoord dat Group LN mijn gegevens gebruikt om mij over dit project te contacteren."
    Public Property SubmitLabel As String = "Verstuur aanvraag"
    Public Property ThanksTitle As String = "Bedankt, we hebben je aanvraag."
    Public Property ThanksText As String = "De documenten zitten binnen één werkdag in je mailbox."
    Public Property GeneralInfoLabel As String = "Algemene info"
End Class

' ───────────────────────── formatteren ─────────────────────────

Public Module V4Format
    Private ReadOnly Nl As CultureInfo = CultureInfo.GetCultureInfo("nl-BE")

    Public Function Inv(value As Double, Optional fmt As String = "0.##") As String
        Return value.ToString(fmt, CultureInfo.InvariantCulture)
    End Function

    Public Function Eur(value As Decimal) As String
        Return "€ " & Math.Round(value, 0).ToString("#,##0", Nl)
    End Function

    Public Function Num(value As Decimal) As String
        Return Math.Round(value, 0).ToString("#,##0", Nl)
    End Function

    Public Function Range(values As IEnumerable(Of Decimal), Optional suffix As String = "") As String
        Dim list = values.ToList()
        If list.Count = 0 Then Return "—"
        Dim lo = list.Min()
        Dim hi = list.Max()
        If lo = hi Then Return Num(lo) & suffix
        Return Num(lo) & "–" & Num(hi) & suffix
    End Function

    ' Annuïteit: lening · r / (1 − (1+r)^−n)
    Public Function MonthlyPayment(price As Decimal, downPayment As Decimal, ratePct As Double, years As Integer) As Decimal
        Dim loan = Math.Max(0D, price - downPayment)
        If loan = 0D Then Return 0D
        Dim r = ratePct / 100.0R / 12.0R
        Dim n = years * 12
        If r = 0 Then Return loan / n
        Return CDec(CDbl(loan) * r / (1 - Math.Pow(1 + r, -n)))
    End Function
End Module

' Kleine HTML-helpers voor de view (afbeelding met placeholder, URL-resolutie).
Public Module V4Html
    Private ReadOnly AllowedTags As New HashSet(Of String)(New String() {"p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a"}, StringComparer.OrdinalIgnoreCase)

    ' Opgemaakte tekst uit CPMCore (bv. FAQ-antwoorden) veilig tonen: enkel eenvoudige opmaak en links, nooit scripts of attributen.
    ' CPMCore schoont ze al bij het opslaan; dit is een tweede slot voor het geval er toch iets anders in de database staat.
    ' Platte tekst uit CPMCore: een witregel start een nieuwe alinea, een gewone nieuwe regel wordt een regeleinde.
    Public Function Paragraphs(text As String, cssClass As String) As IHtmlString
        If String.IsNullOrWhiteSpace(text) Then Return New HtmlString("")
        Dim sb As New System.Text.StringBuilder()
        Dim parts = System.Text.RegularExpressions.Regex.Split(text.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Trim(), "\n[ \t]*\n+")
        For Each part In parts
            If String.IsNullOrWhiteSpace(part) Then Continue For
            sb.Append("<p class=""").Append(System.Web.HttpUtility.HtmlAttributeEncode(cssClass)).Append(""">")
            sb.Append(System.Web.HttpUtility.HtmlEncode(part.Trim()).Replace(vbLf, "<br />"))
            sb.Append("</p>")
        Next
        Return New HtmlString(sb.ToString())
    End Function

    ' Woorden met een koppelteken ("Sint-Martens-Latem", "half-open") blijven op één regel: elk zo'n woord krijgt white-space: nowrap.
    Public Function NoBreakHyphens(text As String) As IHtmlString
        If String.IsNullOrEmpty(text) Then Return New HtmlString("")
        Dim encoded As String = System.Web.HttpUtility.HtmlEncode(text)
        Return New HtmlString(System.Text.RegularExpressions.Regex.Replace(encoded, "[^\s-]+(?:-[^\s-]+)+", "<span class=""pdv4-nowrap"">$0</span>"))
    End Function

    ' Plaatsnaam met hoofdletter per woord en de rest kleine letters ("ERPE-MERE" / "erpe-mere" -> "Erpe-Mere")
    Public Function ProperName(text As String) As String
        If String.IsNullOrWhiteSpace(text) Then Return text
        Dim chars = text.Trim().ToLower(New System.Globalization.CultureInfo("nl-BE")).ToCharArray()
        Dim startOfWord As Boolean = True
        For i = 0 To chars.Length - 1
            If startOfWord AndAlso Char.IsLetter(chars(i)) Then chars(i) = Char.ToUpper(chars(i), New System.Globalization.CultureInfo("nl-BE"))
            startOfWord = (chars(i) = " "c OrElse chars(i) = "-"c)
        Next
        Return New String(chars)
    End Function

    ' Klinkt de projectnaam als een straatnaam ("Keerstraat", "Dorpslaan")? Dan "aan de Keerstraat", anders "bij Residentie X".
    Public Function IsStreetName(name As String) As Boolean
        If String.IsNullOrWhiteSpace(name) Then Return False
        Return System.Text.RegularExpressions.Regex.IsMatch(name.Trim(), "(straat|laan|weg|plein|dreef|steenweg|lei|kaai|dijk|baan|pad|hof|markt|gracht|park|ring)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) AndAlso Not name.Trim().Contains(" ")
    End Function

    Public Function SafeHtml(html As String) As IHtmlString
        If String.IsNullOrWhiteSpace(html) Then Return New HtmlString("")
        Dim t = System.Text.RegularExpressions.Regex.Replace(html, "<(script|style|iframe|object|embed)\b[^>]*>.*?</\1\s*>", "", System.Text.RegularExpressions.RegexOptions.Singleline Or System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        t = System.Text.RegularExpressions.Regex.Replace(t, "<!--.*?-->", "", System.Text.RegularExpressions.RegexOptions.Singleline)
        t = System.Text.RegularExpressions.Regex.Replace(t, "<(/?)([a-zA-Z][a-zA-Z0-9]*)\b([^>]*)>",
            Function(m As System.Text.RegularExpressions.Match) As String
                Dim tag = m.Groups(2).Value.ToLowerInvariant()
                If Not AllowedTags.Contains(tag) Then Return ""
                If m.Groups(1).Value = "/" Then Return "</" & tag & ">"
                If tag = "a" Then
                    Dim h = System.Text.RegularExpressions.Regex.Match(m.Groups(3).Value, "href\s*=\s*(?:""([^""]*)""|'([^']*)')", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                    Dim url As String = If(h.Success, If(h.Groups(1).Success, h.Groups(1).Value, h.Groups(2).Value), "")
                    url = HttpUtility.HtmlDecode(url).Trim()
                    If url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) OrElse
                       url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) OrElse url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) OrElse
                       (url.StartsWith("/") AndAlso Not url.StartsWith("//")) Then
                        Return "<a href=""" & HttpUtility.HtmlAttributeEncode(url) & """ target=""_blank"" rel=""noopener noreferrer"">"
                    End If
                    Return "<a>"
                End If
                Return "<" & tag & ">"
            End Function)
        Return New HtmlString(t)
    End Function

    ' Platte tekst van opgemaakte tekst (voor de FAQPage-JSON-LD)
    Public Function PlainText(html As String) As String
        If String.IsNullOrWhiteSpace(html) Then Return ""
        Dim t = System.Text.RegularExpressions.Regex.Replace(html, "</(p|li)>|<br\s*/?>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        t = HttpUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(t, "<[^>]+>", ""))
        Return System.Text.RegularExpressions.Regex.Replace(t, "\s+", " ").Trim()
    End Function

    ' "~/..." -> app-relatieve URL; absolute URL's blijven ongewijzigd.
    Public Function ResolveUrl(url As String) As String
        If String.IsNullOrWhiteSpace(url) Then Return ""
        If url.StartsWith("~/") Then Return VirtualPathUtility.ToAbsolute(url)
        Return url
    End Function

    ' <img> als er een URL is, anders een neutrale plaatshouder (zodat de layout nooit breekt).
    ' Met een V4Pic die varianten heeft: srcset op breedte (w) + sizes, en width/height van het origineel zodat de browser de ruimte
    ' reserveert. priority = de belangrijkste foto (hero): niet lazy, fetchpriority="high".
    Public Function Img(url As String, alt As String, Optional lazy As Boolean = True, Optional pic As V4Pic = Nothing,
                        Optional sizes As String = Nothing, Optional priority As Boolean = False) As IHtmlString
        Dim widths As List(Of Integer) = If(pic IsNot Nothing, pic.AvailableWidths(), New List(Of Integer))
        Dim hasSrc As Boolean = Not String.IsNullOrWhiteSpace(url) OrElse widths.Count > 0
        If Not hasSrc Then
            Dim ph As New TagBuilder("div")
            ph.AddCssClass("pdv4-ph")
            ph.Attributes("role") = "img"
            ph.Attributes("aria-label") = If(alt, "")
            Return New HtmlString(ph.ToString(TagRenderMode.Normal))
        End If
        Dim t As New TagBuilder("img")
        If widths.Count > 0 Then
            ' standaard-src: 960 breed (of de grootste kleinere), srcset laat de browser kiezen (ook voor retina)
            Dim def = widths.Where(Function(w) w <= 960).DefaultIfEmpty(widths.Min()).Max()
            t.Attributes("src") = pic.VariantUrl(def)
            t.Attributes("srcset") = String.Join(", ", widths.Select(Function(w) pic.VariantUrl(w) & " " & w & "w"))
            t.Attributes("sizes") = If(sizes, "100vw")
        Else
            t.Attributes("src") = ResolveUrl(url)
        End If
        t.Attributes("alt") = If(alt, "")
        If pic IsNot Nothing AndAlso pic.Width > 0 AndAlso pic.Height > 0 Then
            t.Attributes("width") = pic.Width.ToString()
            t.Attributes("height") = pic.Height.ToString()
        End If
        If priority Then
            t.Attributes("fetchpriority") = "high"
        ElseIf lazy Then
            t.Attributes("loading") = "lazy"
        End If
        t.Attributes("decoding") = "async"
        Return New HtmlString(t.ToString(TagRenderMode.SelfClosing))
    End Function
End Module

' Blok "Comfort en energie": titel, subtitel en puntjes; onderaan het blokje "Alle technische details" wanneer er een projectbrochure is.
Public Class V4ComfortBlock
    Public Sub New()
        Visible = True
        MediaKey = "comfort"
        Items = New List(Of V4TitleText)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Title As String
    Public Property Subtitle As String
    Public Property Items As List(Of V4TitleText)
    ' Projectbrochure (het lastenboek is dezelfde brochure): 0 = geen brochure, blokje "Alle technische details" niet tonen
    Public Property SpecDocId As Integer
    Public Property SpecDocName As String
    Public Property SpecCtaTitle As String = "Alle technische details"
    Public Property SpecCtaText As String = "De projectbrochure beschrijft materialen, technieken en afwerking per woning."
    Public Property SpecCtaLabel As String = "Projectbrochure"
End Class

' Blok "Documenten": alle verkoopdocumenten van het project die niet aan een klant/eenheid hangen.
Public Class V4DocumentsBlock
    Public Sub New()
        Visible = True
        MediaKey = "documents"
        Docs = New List(Of V4Doc)
    End Sub
    Public Property Visible As Boolean
    Public Property MediaKey As String
    Public Property Title As String = "Documenten"
    Public Property Docs As List(Of V4Doc)
End Class
