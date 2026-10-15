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
    Public Property ComfortTitle As String
    Public Property ComfortSubtitle As String
    Public Property ComfortItems As New List(Of V4TitleText)
    Public Property BuyTitle As String
    Public Property ContactTitle As String
    Public Property ContactText As String
    Public Property BuySteps As New List(Of V4TitleText)
    Public Property FaqItems As New List(Of V4TitleText)      ' Title = vraag, Text = antwoord (HTML)
    ' Ligging
    Public Property LocationTitle As String
    Public Property LocationText As String
    Public Property LocationMunicipality As String
    Public Property LocationLat As Double?
    Public Property LocationLng As Double?
    Public Property GuideItems As New List(Of V4TitleText)
    Public Property Pois As New List(Of V4Poi)
    Public Property TravelTimes As New List(Of V4Travel)
    Public Property Shapes As New Dictionary(Of Integer, V4LotShape)

    Public ReadOnly Property HasAnyContent As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(Location) OrElse Not String.IsNullOrWhiteSpace(Title) OrElse
                   Not String.IsNullOrWhiteSpace(Subtitle) OrElse Not String.IsNullOrWhiteSpace(IntroText) OrElse
                   Kpis.Count > 0 OrElse Not String.IsNullOrWhiteSpace(AerialImageName) OrElse
                   Not String.IsNullOrWhiteSpace(HomesTitle) OrElse Not String.IsNullOrWhiteSpace(HomesIntro) OrElse
                   Not String.IsNullOrWhiteSpace(BlocksJson) OrElse Not String.IsNullOrWhiteSpace(StoryTitle) OrElse Not String.IsNullOrWhiteSpace(StoryText) OrElse
                   StoryItems.Count > 0 OrElse Not String.IsNullOrWhiteSpace(ArchEyebrow) OrElse Not String.IsNullOrWhiteSpace(ArchTitle) OrElse
                   Not String.IsNullOrWhiteSpace(ArchText) OrElse Quotes.Count > 0 OrElse Details.Count > 0 OrElse
                   Not String.IsNullOrWhiteSpace(ComfortTitle) OrElse Not String.IsNullOrWhiteSpace(ComfortSubtitle) OrElse ComfortItems.Count > 0 OrElse
                   Not String.IsNullOrWhiteSpace(BuyTitle) OrElse BuySteps.Count > 0 OrElse FaqItems.Count > 0 OrElse
                   Not String.IsNullOrWhiteSpace(LocationTitle) OrElse Not String.IsNullOrWhiteSpace(LocationText) OrElse LocationLat.HasValue OrElse
                   GuideItems.Count > 0 OrElse Pois.Count > 0 OrElse TravelTimes.Count > 0
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

    ' level 0 = migratie 083, 1 = + projectkaart (084), 2 = + titel/omschrijving woningenblok (085), 3 = + blokken/verhaal/architectuur (086), 4 = + comfort en energie (088), 5 = + vragen en aankoopstappen (089), 6 = + ligging (090)
    Private Shared Function Query(projectId As Integer, level As Integer) As ProjectWebsiteContent
        Dim withMap As Boolean = level >= 1
        Dim sql As String =
            "SELECT Location, Title, Subtitle, IntroText" & If(withMap, ", AerialImageName", "") & If(level >= 2, ", HomesTitle, HomesIntro", "") & If(level >= 3, ", BlocksJson, ShowBrochure, StoryTitle, StoryText, ArchEyebrow, ArchTitle, ArchText", "") & If(level >= 4, ", ComfortTitle, ComfortSubtitle", "") & If(level >= 5, ", BuyTitle", "") & If(level >= 6, ", LocationTitle, LocationText, LocationMunicipality, LocationLat, LocationLng", "") & If(level >= 7, ", ContactTitle, ContactText", "") & " FROM dbo.ProjectWebsite WHERE ProjectId = @id;" &
            "SELECT Title, [Text] FROM dbo.ProjectWebsiteKpi WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
            If(withMap, "SELECT UnitId, Polygon, LabelX, LabelY FROM dbo.ProjectWebsiteLot WHERE ProjectId = @id;", "") &
            If(level >= 3, "SELECT ImageName, [Text] FROM dbo.ProjectWebsiteStoryItem WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
                           "SELECT [Text], Person FROM dbo.ProjectWebsiteQuote WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
                           "SELECT ImageName, Title, [Text] FROM dbo.ProjectWebsiteDetail WHERE ProjectId = @id ORDER BY SortOrder, Id;", "") &
            If(level >= 4, "SELECT Title, [Text] FROM dbo.ProjectWebsiteComfortItem WHERE ProjectId = @id ORDER BY SortOrder, Id;", "") &
            If(level >= 5, "SELECT Title, [Text] FROM dbo.ProjectWebsiteBuyStep WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
                           "SELECT Question, Answer FROM dbo.ProjectWebsiteFaq WHERE ProjectId = @id ORDER BY SortOrder, Id;", "") &
            If(level >= 6, "SELECT Title, [Text] FROM dbo.ProjectWebsiteGuideItem WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
                           "SELECT Category, Name, Lat, Lng, Distance, TimeText FROM dbo.ProjectWebsitePoi WHERE ProjectId = @id ORDER BY SortOrder, Id;" &
                           "SELECT Place, Duration FROM dbo.ProjectWebsiteTravel WHERE ProjectId = @id ORDER BY SortOrder, Id;", "")
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
                        If level >= 4 Then
                            c.ComfortTitle = If(reader.IsDBNull(14), Nothing, reader.GetString(14))
                            c.ComfortSubtitle = If(reader.IsDBNull(15), Nothing, reader.GetString(15))
                        End If
                        If level >= 5 Then c.BuyTitle = If(reader.IsDBNull(16), Nothing, reader.GetString(16))
                        If level >= 6 Then
                            c.LocationTitle = If(reader.IsDBNull(17), Nothing, reader.GetString(17))
                            c.LocationText = If(reader.IsDBNull(18), Nothing, reader.GetString(18))
                            c.LocationMunicipality = If(reader.IsDBNull(19), Nothing, reader.GetString(19))
                            If Not reader.IsDBNull(20) Then c.LocationLat = Convert.ToDouble(reader.GetDecimal(20))
                            If Not reader.IsDBNull(21) Then c.LocationLng = Convert.ToDouble(reader.GetDecimal(21))
                        End If
                        If level >= 7 Then
                            c.ContactTitle = If(reader.IsDBNull(22), Nothing, reader.GetString(22))
                            c.ContactText = If(reader.IsDBNull(23), Nothing, reader.GetString(23))
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
                        If level >= 4 AndAlso reader.NextResult() Then
                            While reader.Read()
                                c.ComfortItems.Add(New V4TitleText With {.Title = reader.GetString(0), .Text = If(reader.IsDBNull(1), "", reader.GetString(1))})
                            End While
                        End If
                        If level >= 5 AndAlso reader.NextResult() Then
                            While reader.Read()
                                c.BuySteps.Add(New V4TitleText With {.Title = reader.GetString(0), .Text = If(reader.IsDBNull(1), "", reader.GetString(1))})
                            End While
                        End If
                        If level >= 5 AndAlso reader.NextResult() Then
                            While reader.Read()
                                c.FaqItems.Add(New V4TitleText With {.Title = reader.GetString(0), .Text = reader.GetString(1)})
                            End While
                        End If
                        If level >= 6 AndAlso reader.NextResult() Then
                            While reader.Read()
                                c.GuideItems.Add(New V4TitleText With {.Title = reader.GetString(0), .Text = If(reader.IsDBNull(1), "", reader.GetString(1))})
                            End While
                        End If
                        If level >= 6 AndAlso reader.NextResult() Then
                            While reader.Read()
                                c.Pois.Add(New V4Poi With {.Category = reader.GetString(0), .Name = reader.GetString(1), .Lat = Convert.ToDouble(reader.GetDecimal(2)), .Lng = Convert.ToDouble(reader.GetDecimal(3)), .Distance = If(reader.IsDBNull(4), "", reader.GetString(4)), .Time = If(reader.IsDBNull(5), "", reader.GetString(5))})
                            End While
                        End If
                        If level >= 6 AndAlso reader.NextResult() Then
                            While reader.Read()
                                c.TravelTimes.Add(New V4Travel With {.Destination = reader.GetString(0), .Duration = reader.GetString(1)})
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
                result = Query(projectId, 7)
            Catch
             Try
                result = Query(projectId, 6)
             Catch
             Try
                result = Query(projectId, 5)
             Catch
               Try
                result = Query(projectId, 4)
               Catch
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
             End Try
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
