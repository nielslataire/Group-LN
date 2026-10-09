Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Web

' Leest de website-inhoud van een project (door CPMCore beheerd: tabellen ProjectWebsite en
' ProjectWebsiteKpi, zie _migrations/083_ProjectWebsite.sql) voor de publieke projectpagina.
'
' Snel op de publieke site:
'  - één round-trip: beide SELECT's (kop + kerncijfers) in één commando, op de primaire sleutel
'    resp. de index (ProjectId, SortOrder);
'  - kort in het geheugen gecachet (HttpRuntime.Cache) zodat een drukke pagina de database niet raakt;
'  - ontbreekt de tabel of de rij, dan geeft de lezer Nothing terug en valt de pagina terug op de
'    bestaande projectgegevens (de site mag nooit stuk gaan door ontbrekende website-inhoud).
Public Class ProjectWebsiteContent
    Public Property Location As String
    Public Property Title As String
    Public Property Subtitle As String
    Public Property IntroText As String
    Public Property Kpis As New List(Of V4Fact)
    ' Projectkaart: gekozen foto + omtrek per eenheid (UnitId -> omtrek)
    Public Property AerialImageName As String
    ' Titel/omschrijving van het blok "De woningen"
    Public Property HomesTitle As String
    Public Property HomesIntro As String
    ' Blokken (volgorde + zichtbaarheid), verhaal en architectuur
    Public Property BlocksJson As String
    Public Property ShowBrochure As Boolean
    Public Property StoryTitle As String
    Public Property StoryText As String
    Public Property StoryItems As New List(Of V4Figure)      ' ImageUrl = bestandsnaam
    Public Property ArchEyebrow As String
    Public Property ArchTitle As String
    Public Property ArchText As String
    Public Property Quotes As New List(Of V4Quote)
    Public Property Details As New List(Of V4Material)       ' ImageUrl = bestandsnaam
    Public Property Shapes As New Dictionary(Of Integer, V4LotShape)

    Public ReadOnly Property HasAnyContent As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(Location) OrElse Not String.IsNullOrWhiteSpace(Title) OrElse
                   Not String.IsNullOrWhiteSpace(Subtitle) OrElse Not String.IsNullOrWhiteSpace(IntroText) OrElse
                   Kpis.Count > 0 OrElse Not String.IsNullOrWhiteSpace(AerialImageName) OrElse
                   Not String.IsNullOrWhiteSpace(HomesTitle) OrElse Not String.IsNullOrWhiteSpace(HomesIntro) OrElse
                   Not String.IsNullOrWhiteSpace(BlocksJson) OrElse Not String.IsNullOrWhiteSpace(StoryTitle) OrElse Not String.IsNullOrWhiteSpace(StoryText) OrElse
                   StoryItems.Count > 0 OrElse Not String.IsNullOrWhiteSpace(ArchEyebrow) OrElse Not String.IsNullOrWhiteSpace(ArchTitle) OrElse
                   Not String.IsNullOrWhiteSpace(ArchText) OrElse Quotes.Count > 0 OrElse Details.Count > 0
        End Get
    End Property
End Class

Public Class V4LotShape
    Public Property Polygon As String
    Public Property LabelX As Double
    Public Property LabelY As Double
End Class

Public Class ProjectWebsiteReader
    Private Const CacheMinutes As Integer = 5
    Private Shared ReadOnly Missing As New Object()

    ' level 0 = migratie 083, 1 = + projectkaart (084), 2 = + titel/omschrijving woningenblok (085), 3 = + blokken/verhaal/architectuur (086)
    Private Shared Function Query(projectId As Integer, level As Integer) As ProjectWebsiteContent
        Dim withMap As Boolean = level >= 1
        Dim sql As String =
            "SELECT Location, Title, Subtitle, IntroText" & If(withMap, ", AerialImageName", "") & If(level >= 2, ", HomesTitle, HomesIntro", "") & If(level >= 3, ", BlocksJson, ShowBrochure, StoryTitle, StoryText, ArchEyebrow, ArchTitle, ArchText", "") & " FROM dbo.ProjectWebsite WHERE ProjectId = @id;" &
            "SELECT Title, [Text] FROM dbo.ProjectWebsiteKpi WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
            If(withMap, "SELECT UnitId, Polygon, LabelX, LabelY FROM dbo.ProjectWebsiteLot WHERE ProjectId = @id;", "") &
            If(level >= 3, "SELECT ImageName, [Text] FROM dbo.ProjectWebsiteStoryItem WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
                           "SELECT [Text], Person FROM dbo.ProjectWebsiteQuote WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
                           "SELECT ImageName, Title, [Text] FROM dbo.ProjectWebsiteDetail WHERE ProjectId = @id ORDER BY SortOrder, Id;", "")
        Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString)
            conn.Open()
            Using cmd As New SqlCommand(sql, conn)
                cmd.Parameters.AddWithValue("@id", projectId)
                Using reader = cmd.ExecuteReader()
                    Dim c As New ProjectWebsiteContent
                    If reader.Read() Then
                        c.Location = If(reader.IsDBNull(0), Nothing, reader.GetString(0))
                        c.Title = If(reader.IsDBNull(1), Nothing, reader.GetString(1))
                        c.Subtitle = If(reader.IsDBNull(2), Nothing, reader.GetString(2))
                        c.IntroText = If(reader.IsDBNull(3), Nothing, reader.GetString(3))
                        If withMap Then c.AerialImageName = If(reader.IsDBNull(4), Nothing, reader.GetString(4))
                        If level >= 2 Then
                            c.HomesTitle = If(reader.IsDBNull(5), Nothing, reader.GetString(5))
                            c.HomesIntro = If(reader.IsDBNull(6), Nothing, reader.GetString(6))
                        End If
                        If level >= 3 Then
                            c.BlocksJson = If(reader.IsDBNull(7), Nothing, reader.GetString(7))
                            c.ShowBrochure = Not reader.IsDBNull(8) AndAlso reader.GetBoolean(8)
                            c.StoryTitle = If(reader.IsDBNull(9), Nothing, reader.GetString(9))
                            c.StoryText = If(reader.IsDBNull(10), Nothing, reader.GetString(10))
                            c.ArchEyebrow = If(reader.IsDBNull(11), Nothing, reader.GetString(11))
                            c.ArchTitle = If(reader.IsDBNull(12), Nothing, reader.GetString(12))
                            c.ArchText = If(reader.IsDBNull(13), Nothing, reader.GetString(13))
                        End If
                    End If
                    If reader.NextResult() Then
                        While reader.Read()
                            c.Kpis.Add(New V4Fact With {.Label = reader.GetString(0), .Value = reader.GetString(1)})
                        End While
                    End If
                    If withMap AndAlso reader.NextResult() Then
                        While reader.Read()
                            c.Shapes(reader.GetInt32(0)) = New V4LotShape With {
                                .Polygon = reader.GetString(1),
                                .LabelX = Convert.ToDouble(reader.GetDecimal(2)),
                                .LabelY = Convert.ToDouble(reader.GetDecimal(3))}
                        End While
                    End If
                    If level >= 3 Then
                        If reader.NextResult() Then
                            While reader.Read()
                                c.StoryItems.Add(New V4Figure With {.ImageUrl = If(reader.IsDBNull(0), "", reader.GetString(0)), .Text = If(reader.IsDBNull(1), "", reader.GetString(1))})
                            End While
                        End If
                        If reader.NextResult() Then
                            While reader.Read()
                                c.Quotes.Add(New V4Quote With {.Text = reader.GetString(0), .Person = If(reader.IsDBNull(1), "", reader.GetString(1))})
                            End While
                        End If
                        If reader.NextResult() Then
                            While reader.Read()
                                c.Details.Add(New V4Material With {.ImageUrl = If(reader.IsDBNull(0), "", reader.GetString(0)), .Title = reader.GetString(1), .Text = If(reader.IsDBNull(2), "", reader.GetString(2))})
                            End While
                        End If
                    End If
                    Return If(c.HasAnyContent OrElse c.Shapes.Count > 0, c, Nothing)
                End Using
            End Using
        End Using
    End Function

    Public Shared Function Load(projectId As Integer) As ProjectWebsiteContent
        If projectId <= 0 Then Return Nothing

        Dim key As String = "pdv4-web-" & projectId
        Dim cached = HttpRuntime.Cache.Get(key)
        If cached IsNot Nothing Then
            If Object.ReferenceEquals(cached, Missing) Then Return Nothing
            Return DirectCast(cached, ProjectWebsiteContent)
        End If

        Dim result As ProjectWebsiteContent = Nothing
        Try
            ' Eén round-trip: kop, kerncijfers en projectkaart. Bestaat de projectkaart-structuur nog niet
            ' (migratie 084), dan lezen we enkel de kop + kerncijfers.
            Try
                result = Query(projectId, 3)
            Catch
                Try
                    result = Query(projectId, 2)
                Catch
                    Try
                        result = Query(projectId, 1)
                    Catch
                        result = Query(projectId, 0)
                    End Try
                End Try
            End Try
        Catch
            ' tabellen (nog) niet aangemaakt of database even niet bereikbaar: geen website-inhoud, pagina valt terug
            result = Nothing
        End Try

        HttpRuntime.Cache.Insert(key, If(result Is Nothing, Missing, CObj(result)), Nothing,
                                 DateTime.UtcNow.AddMinutes(CacheMinutes), System.Web.Caching.Cache.NoSlidingExpiration)
        Return result
    End Function
End Class
