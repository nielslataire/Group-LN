Imports System.Data.SqlClient
Imports System.Text.RegularExpressions
Imports System.Web.Mvc

''' <summary>
''' Anonieme telling van de cookiebanner-uitkomst (getoond/aanvaard/geweigerd/verlaten), voor
''' CPMCore/Instellingen — zie migraties 033_CookieConsentEvents en 059_CookieConsentEventAbandoned.
''' Los van Google Analytics, want die laadt zelf pas ná toestemming en kan dus nooit "geweigerd"
''' of "verlaten" meten. Schrijft rechtstreeks in de gedeelde databank via SQL, buiten de legacy
''' BO-laag om — zelfde stijl als HomeController.GetHomeHeroFeatured(). Best-effort: een fout hier
''' mag de bezoekerservaring nooit verstoren, dus alles wordt stil opgeslokt.
''' Bots en crawlers die JavaScript uitvoeren (Googlebot, previewbots, monitoring, headless
''' browsers) krijgen de banner ook te zien maar klikken nooit; die worden hier op user-agent
''' overgeslagen zodat ze de telling niet vervuilen.
''' </summary>
Public Class CookieConsentController
    Inherits System.Web.Mvc.Controller

    Private Shared ReadOnly AllowedEventTypes As String() = {"Shown", "Accepted", "Rejected", "Abandoned"}

    ' Bewust breed: liever een zeldzame echte bezoeker overslaan dan honderden bots meetellen.
    ' Bekende valse positieven (bv. het telefoonmerk "Cubot") zijn verwaarloosbaar.
    Private Shared ReadOnly BotUserAgent As New Regex(
        "bot|crawl|spider|slurp|headless|lighthouse|pagespeed|gtmetrix|pingdom|uptime|monitor|" &
        "facebookexternalhit|preview|embedly|quora link|python|java/|curl|wget|httpclient|okhttp|" &
        "phantomjs|puppeteer|playwright|selenium|scrapy|go-http-client|libwww|ahrefs|semrush|mj12",
        RegexOptions.IgnoreCase Or RegexOptions.Compiled)

    <HttpPost>
    <Route("cookie-consent-event", Name:="CookieConsentEvent")>
    Function LogEvent(model As CookieConsentEventModel) As ActionResult
        Try
            Dim eventType = If(model?.EventType, "")
            Dim normalized = AllowedEventTypes.FirstOrDefault(Function(t) t.Equals(eventType, StringComparison.OrdinalIgnoreCase))
            If normalized Is Nothing Then Return New HttpStatusCodeResult(204)
            If IsBot(Request.UserAgent) Then Return New HttpStatusCodeResult(204)

            Using conn As New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString)
                conn.Open()
                Using cmd As New SqlCommand("INSERT INTO dbo.CookieConsentEvent (EventType) VALUES (@EventType)", conn)
                    cmd.Parameters.AddWithValue("@EventType", normalized)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch
            ' Stil negeren: dit is een best-effort teller, geen kritiek pad.
        End Try
        Return New HttpStatusCodeResult(204)
    End Function

    ''' <summary>Geen of een lege user-agent telt ook als bot: echte browsers sturen er altijd een.</summary>
    Private Shared Function IsBot(userAgent As String) As Boolean
        If String.IsNullOrWhiteSpace(userAgent) Then Return True
        Return BotUserAgent.IsMatch(userAgent)
    End Function

End Class

Public Class CookieConsentEventModel
    Public Property EventType As String
End Class
