Imports System.Data.SqlClient
Imports System.Web.Mvc

''' <summary>
''' Anonieme telling van de cookiebanner-uitkomst (getoond/aanvaard/geweigerd), voor
''' CPMCore/Instellingen — zie migratie 033_CookieConsentEvents. Los van Google Analytics,
''' want die laadt zelf pas ná toestemming en kan dus nooit "geweigerd" of "geen keuze" meten.
''' Schrijft rechtstreeks in de gedeelde databank via SQL, buiten de legacy BO-laag om —
''' zelfde stijl als HomeController.GetHomeHeroFeatured(). Best-effort: een fout hier mag
''' de bezoekerservaring nooit verstoren, dus alles wordt stil opgeslokt.
''' </summary>
Public Class CookieConsentController
    Inherits System.Web.Mvc.Controller

    Private Shared ReadOnly AllowedEventTypes As String() = {"Shown", "Accepted", "Rejected"}

    <HttpPost>
    <Route("cookie-consent-event", Name:="CookieConsentEvent")>
    Function LogEvent(model As CookieConsentEventModel) As ActionResult
        Try
            Dim eventType = If(model?.EventType, "")
            Dim normalized = AllowedEventTypes.FirstOrDefault(Function(t) t.Equals(eventType, StringComparison.OrdinalIgnoreCase))
            If normalized Is Nothing Then Return New HttpStatusCodeResult(204)

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

End Class

Public Class CookieConsentEventModel
    Public Property EventType As String
End Class
