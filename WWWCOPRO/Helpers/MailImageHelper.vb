Imports System.Collections.Concurrent
Imports System.Configuration
Imports System.IO
Imports System.Net

''' Afbeeldings-URL's voor e-mails. Veel mailclients (o.a. Outlook) tonen geen WebP, dus voor een
''' .webp-foto zoeken we een JPEG die de Storage-API bij de upload bewaart: eerst de mailvariant
''' (pictures/mail/{naam}.jpg, max 640 px), anders de master (pictures/original/{naam}.jpg).
''' Bestaat geen van beide (foto's van vóór de fotopijplijn en nog niet via de Storage-backfill
''' verwerkt), dan valt de URL terug op de gewone foto in "pictures".
Public Class MailImageHelper
    Private Shared ReadOnly ExistsCache As New ConcurrentDictionary(Of String, Tuple(Of Boolean, DateTime))()
    Private Shared ReadOnly CacheDuration As TimeSpan = TimeSpan.FromHours(6)

    Public Shared Function Url(fileName As String) As String
        Dim base As String = ConfigurationManager.AppSettings("ImageWebURL")
        If String.IsNullOrWhiteSpace(base) Then base = "https://cpm.groupln.be/assets/"
        If Not base.EndsWith("/") Then base &= "/"
        If String.IsNullOrWhiteSpace(fileName) Then Return base & "pictures/placeholder.jpg"

        Dim fallback As String = base & "pictures/" & Uri.EscapeDataString(fileName)
        If Not String.Equals(Path.GetExtension(fileName), ".webp", StringComparison.OrdinalIgnoreCase) Then Return fallback

        Dim baseName As String = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(fileName))
        For Each folder In New String() {"pictures/mail/", "pictures/original/"}
            Dim candidate As String = base & folder & baseName & ".jpg"
            If Exists(candidate) Then Return candidate
        Next
        Return fallback
    End Function

    Private Shared Function Exists(url As String) As Boolean
        Dim cached As Tuple(Of Boolean, DateTime) = Nothing
        If ExistsCache.TryGetValue(url, cached) AndAlso DateTime.UtcNow - cached.Item2 < CacheDuration Then Return cached.Item1
        Dim found As Boolean = False
        Try
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
            Dim req = DirectCast(WebRequest.Create(url), HttpWebRequest)
            req.Method = "HEAD"
            req.Timeout = 3000
            Using resp = DirectCast(req.GetResponse(), HttpWebResponse)
                found = (resp.StatusCode = HttpStatusCode.OK)
            End Using
        Catch
            found = False
        End Try
        ExistsCache(url) = Tuple.Create(found, DateTime.UtcNow)
        Return found
    End Function
End Class
