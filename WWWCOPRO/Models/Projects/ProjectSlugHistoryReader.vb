Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Web

' Zoekt bij een oude projectslug de huidige slug op (tabel dbo.ProjectSlugHistory, migratie 148; CPMCore bewaart er
' de vorige slug in zodra Projecten/Edit hem wijzigt). Enkel aangeroepen wanneer een slug niet (meer) bestaat, dus
' vlak voor een 404. Ontbreekt de tabel of mislukt de query, dan is het antwoord gewoon Nothing: de pagina geeft dan
' zoals voorheen een 404. Kort gecachet zodat een bot die oude of foute links afloopt de database niet belast.
Public Class ProjectSlugHistoryReader
    Private Const CacheMinutes As Integer = 10
    Private Shared ReadOnly Missing As New Object()

    ''' <summary>De huidige slug van het project dat ooit deze slug had, of Nothing.</summary>
    Public Shared Function CurrentSlugFor(oldSlug As String) As String
        If String.IsNullOrWhiteSpace(oldSlug) Then Return Nothing
        Dim key As String = "ProjectSlugHistory:" & oldSlug.ToLowerInvariant()
        Dim cached As Object = HttpRuntime.Cache(key)
        If cached IsNot Nothing Then Return If(ReferenceEquals(cached, Missing), Nothing, CStr(cached))

        Dim result As String = Nothing
        Try
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString)
                conn.Open()
                Using cmd As New SqlCommand("SELECT TOP 1 p.Slug FROM dbo.ProjectSlugHistory h JOIN dbo.Project p ON p.ProjectId = h.ProjectId WHERE h.Slug = @slug AND p.Slug IS NOT NULL AND p.Slug <> ''", conn)
                    cmd.Parameters.AddWithValue("@slug", oldSlug.ToLowerInvariant())
                    Dim o = cmd.ExecuteScalar()
                    If o IsNot Nothing AndAlso Not IsDBNull(o) Then result = CStr(o)
                End Using
            End Using
        Catch
            Return Nothing   ' tabel ontbreekt of database niet bereikbaar: niet cachen, gewoon geen doorverwijzing
        End Try

        HttpRuntime.Cache.Insert(key, If(result, Missing), Nothing, DateTime.UtcNow.AddMinutes(CacheMinutes), System.Web.Caching.Cache.NoSlidingExpiration)
        Return result
    End Function
End Class
