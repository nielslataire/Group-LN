Imports System.Data.SqlClient
Imports System.Configuration
Imports WWWCOPRO.Models.Blog

Public Class BlogController
    Inherits System.Web.Mvc.Controller

    ' GET: /Blog
    <Route("Blog", Name:="Blog")>
    <Route("Nieuws", Name:="Nieuws")>
    Function Index() As ActionResult
        ' /Nieuws is een oude, alternatieve route naar dezelfde pagina — permanent
        ' doorsturen naar /blog zodat er maar één indexeerbare URL overblijft.
        If Request.Url.AbsolutePath.StartsWith("/Nieuws", StringComparison.OrdinalIgnoreCase) Then
            Return RedirectPermanent("/blog")
        End If

        Dim artikelen = GetGepubliceerdeArtikelen()
        Return View(artikelen)
    End Function

    ' GET: /Blog/{slug}  (ook preview via ?prev=token)
    <Route("Blog/{slug}", Name:="BlogArtikel")>
    <Route("Nieuws/{slug}", Name:="NieuwsArtikel")>
    Function Artikel(slug As String, prev As String) As ActionResult
        If Request.Url.AbsolutePath.StartsWith("/Nieuws", StringComparison.OrdinalIgnoreCase) Then
            Return RedirectPermanent("/blog/" & slug)
        End If

        Dim verwachtToken = ConfigurationManager.AppSettings("PreviewToken")
        Dim isPreview As Boolean = Not String.IsNullOrEmpty(verwachtToken) AndAlso
                                   Not String.IsNullOrEmpty(prev) AndAlso
                                   prev = verwachtToken

        Dim artikelfull = GetArtikelBySlug(slug, inclConcept:=isPreview)
        If artikelfull Is Nothing Then
            Return HttpNotFound()
        End If

        ' Ontdek meer: laad gerelateerde items
        Dim links = New List(Of Tuple(Of String, Integer?)) From {
            Tuple.Create(artikelfull.Link1Type, artikelfull.Link1Id),
            Tuple.Create(artikelfull.Link2Type, artikelfull.Link2Id),
            Tuple.Create(artikelfull.Link3Type, artikelfull.Link3Id)
        }
        For Each link In links
            If Not String.IsNullOrEmpty(link.Item1) AndAlso link.Item2.HasValue Then
                Dim item = LaadOntdekMeerItem(link.Item1, link.Item2.Value)
                If item IsNot Nothing Then artikelfull.OntdekMeer.Add(item)
            End If
        Next

        If isPreview Then
            ViewData("IsVoorvertoning") = True
        End If

        Return View(artikelfull)
    End Function

    ' ── private helpers ────────────────────────────────────────────────

    Private Function GetConnectionString() As String
        Return ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString
    End Function

    Private Function GetGepubliceerdeArtikelen() As List(Of BlogArtikelModel)
        Dim result As New List(Of BlogArtikelModel)

        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                Dim cmd As New SqlCommand(
                    "SELECT a.Id, a.Titel, a.Slug, a.PreviewTekst, a.FotoBestand, a.Datum,
                            CAST(ISNULL((SELECT SUM(LEN(b.RijkeTekst)) FROM BlogArtikelBlok b WHERE b.ArtikelId = a.Id), 0) AS INT) AS TotaalChars
                       FROM BlogArtikel a
                      WHERE a.IsGepubliceerd = 1
                      ORDER BY a.Datum DESC", conn)

                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim totaalChars As Integer = reader.GetInt32(6)
                        result.Add(New BlogArtikelModel With {
                            .ID = reader.GetInt32(0),
                            .Titel = reader.GetString(1),
                            .Slug = reader.GetString(2),
                            .PreviewTekst = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                            .FotoBestand = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                            .Datum = reader.GetDateTime(5),
                            .LeestijdMinuten = If(totaalChars = 0, 0, CInt(Math.Max(1, Math.Round(totaalChars / 1000.0))))
                        })
                    End While
                End Using
            End Using
        Catch ex As Exception
            ' retourneer lege lijst zodat de pagina niet crasht
        End Try

        Return result
    End Function

    Private Function GetArtikelBySlug(slug As String, Optional inclConcept As Boolean = False) As BlogArtikelModel
        Dim artikel As BlogArtikelModel = Nothing

        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()

                Dim whereClause = If(inclConcept, "Slug = @slug", "Slug = @slug AND IsGepubliceerd = 1")
                Dim cmd As New SqlCommand(
                    "SELECT Id, Titel, Slug, PreviewTekst, DetailTitel, DetailTitelTekst, FotoBestand, Datum,
                            MetaTitel, MetaOmschrijving, MetaKeywords, GeoRegio, GeoPlaatsnaam, GeoPositie,
                            Link1Type, Link1Id, Link2Type, Link2Id, Link3Type, Link3Id
                       FROM BlogArtikel
                      WHERE " & whereClause, conn)
                cmd.Parameters.AddWithValue("@slug", slug)

                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        artikel = New BlogArtikelModel With {
                            .ID              = reader.GetInt32(0),
                            .Titel           = reader.GetString(1),
                            .Slug            = reader.GetString(2),
                            .PreviewTekst    = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                            .DetailTitel     = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                            .DetailTitelTekst = If(reader.IsDBNull(5), Nothing, reader.GetString(5)),
                            .FotoBestand     = If(reader.IsDBNull(6), Nothing, reader.GetString(6)),
                            .Datum           = reader.GetDateTime(7),
                            .MetaTitel       = If(reader.IsDBNull(8), Nothing, reader.GetString(8)),
                            .MetaOmschrijving = If(reader.IsDBNull(9), Nothing, reader.GetString(9)),
                            .MetaKeywords    = If(reader.IsDBNull(10), Nothing, reader.GetString(10)),
                            .GeoRegio        = If(reader.IsDBNull(11), Nothing, reader.GetString(11)),
                            .GeoPlaatsnaam   = If(reader.IsDBNull(12), Nothing, reader.GetString(12)),
                            .GeoPositie      = If(reader.IsDBNull(13), Nothing, reader.GetString(13)),
                            .Link1Type       = If(reader.IsDBNull(14), Nothing, reader.GetString(14)),
                            .Link1Id         = If(reader.IsDBNull(15), Nothing, CType(reader.GetInt32(15), Integer?)),
                            .Link2Type       = If(reader.IsDBNull(16), Nothing, reader.GetString(16)),
                            .Link2Id         = If(reader.IsDBNull(17), Nothing, CType(reader.GetInt32(17), Integer?)),
                            .Link3Type       = If(reader.IsDBNull(18), Nothing, reader.GetString(18)),
                            .Link3Id         = If(reader.IsDBNull(19), Nothing, CType(reader.GetInt32(19), Integer?))
                        }
                    End If
                End Using

                If artikel Is Nothing Then Return Nothing

                Dim cmdBlok As New SqlCommand(
                    "SELECT Id, SortOrder, ISNULL(BlokType, 'tekst'), Titel, RijkeTekst, FotoBestand
                       FROM BlogArtikelBlok
                      WHERE ArtikelId = @id
                      ORDER BY SortOrder", conn)
                cmdBlok.Parameters.AddWithValue("@id", artikel.ID)

                Using reader = cmdBlok.ExecuteReader()
                    While reader.Read()
                        artikel.Blokken.Add(New BlogArtikelBlokModel With {
                            .ID          = reader.GetInt32(0),
                            .SortOrder   = reader.GetInt32(1),
                            .BlokType    = reader.GetString(2),
                            .Titel       = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                            .RijkeTekst  = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                            .FotoBestand = If(reader.IsDBNull(5), Nothing, reader.GetString(5))
                        })
                    End While
                End Using

                Dim cmdFaq As New SqlCommand(
                    "SELECT Id, SortOrder, Vraag, Antwoord
                       FROM BlogArtikelFaq
                      WHERE ArtikelId = @id
                      ORDER BY SortOrder", conn)
                cmdFaq.Parameters.AddWithValue("@id", artikel.ID)

                Using reader = cmdFaq.ExecuteReader()
                    While reader.Read()
                        artikel.FaqItems.Add(New BlogArtikelFaqModel With {
                            .ID        = reader.GetInt32(0),
                            .SortOrder = reader.GetInt32(1),
                            .Vraag     = If(reader.IsDBNull(2), Nothing, reader.GetString(2)),
                            .Antwoord  = If(reader.IsDBNull(3), Nothing, reader.GetString(3))
                        })
                    End While
                End Using
            End Using
        Catch ex As Exception
            System.IO.File.AppendAllText(
                System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/blog-error.txt"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " | slug=" & slug & " | " & ex.GetType().Name & ": " & ex.Message & Environment.NewLine & ex.StackTrace & Environment.NewLine & "---" & Environment.NewLine)
        End Try

        Return artikel
    End Function

    Private Function LaadOntdekMeerItem(itemType As String, itemId As Integer) As OntdekMeerItemModel
        Dim imgBase As String = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL")
        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()

                If itemType = "artikel" Then
                    Dim cmd As New SqlCommand(
                        "SELECT Titel, Slug, PreviewTekst, FotoBestand, Datum
                           FROM BlogArtikel
                          WHERE Id = @id AND IsGepubliceerd = 1", conn)
                    cmd.Parameters.AddWithValue("@id", itemId)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            Return New OntdekMeerItemModel With {
                                .ItemType    = "artikel",
                                .Titel       = reader.GetString(0),
                                .Slug        = reader.GetString(1),
                                .PreviewTekst = If(reader.IsDBNull(2), Nothing, reader.GetString(2)),
                                .FotoUrl     = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                                .Datum       = reader.GetDateTime(4)
                            }
                        End If
                    End Using

                ElseIf itemType = "project" Then
                    Dim cmd As New SqlCommand(
                        "SELECT p.ProjectName, ISNULL(p.CommercialTitleNL, p.ProjectName), p.Slug,
                                pic.Name AS PicName, ISNULL(pic.MediaType, 0) AS MediaType,
                                ISNULL(p.Street, '') AS Straat, pc.Gemeente,
                                (SELECT MIN(sub.SlaapCount) FROM (
                                    SELECT u2.Id, SUM(ISNULL(ur.Number,0)) AS SlaapCount
                                    FROM Units u2 LEFT JOIN UnitRooms ur ON ur.UnitId = u2.Id AND ur.Type = 7
                                    WHERE u2.ProjectId = p.ProjectID AND u2.IsOption = 0 AND u2.AttachedUnitId IS NULL AND u2.LinkedUnitId IS NULL
                                    GROUP BY u2.Id HAVING SUM(ISNULL(ur.Number,0)) > 0) sub) AS MinSlaap,
                                (SELECT MAX(sub.SlaapCount) FROM (
                                    SELECT u2.Id, SUM(ISNULL(ur.Number,0)) AS SlaapCount
                                    FROM Units u2 LEFT JOIN UnitRooms ur ON ur.UnitId = u2.Id AND ur.Type = 7
                                    WHERE u2.ProjectId = p.ProjectID AND u2.IsOption = 0 AND u2.AttachedUnitId IS NULL AND u2.LinkedUnitId IS NULL
                                    GROUP BY u2.Id HAVING SUM(ISNULL(ur.Number,0)) > 0) sub) AS MaxSlaap,
                                (SELECT COUNT(*) FROM Units u2 WHERE u2.ProjectId = p.ProjectID AND u2.IsOption = 0) AS AantalEenheden,
                                0 AS _unused,
                                (SELECT MIN(sub.UnitPrice)
                                 FROM (
                                     SELECT ISNULL(u2.LandValue, 0) +
                                            CASE WHEN EXISTS (SELECT 1 FROM UnitFinishingOption ufo WHERE ufo.UnitId = u2.Id)
                                                 THEN ISNULL((
                                                     SELECT MIN(opt.OptTotal) FROM (
                                                         SELECT ufo2.Id, SUM(ISNULL(ucv.Value, 0)) AS OptTotal
                                                         FROM UnitFinishingOption ufo2
                                                         JOIN UnitConstructionValue ucv ON ucv.FinishingOptionId = ufo2.Id
                                                         WHERE ufo2.UnitId = u2.Id
                                                         GROUP BY ufo2.Id
                                                     ) opt), 0)
                                                 ELSE ISNULL((SELECT SUM(ISNULL(ucv.Value, 0)) FROM UnitConstructionValue ucv WHERE ucv.UnitId = u2.Id), 0)
                                            END AS UnitPrice
                                     FROM Units u2
                                     WHERE u2.ProjectId = p.ProjectID AND u2.IsOption = 0 AND u2.ClientAccountId IS NULL
                                       AND u2.TypeId IN (SELECT t.Id FROM UnitTypes t WHERE t.GroupId IN (1, 4))
                                 ) sub
                                 WHERE sub.UnitPrice > 0
                                ) AS VanafPrijs,
                                (SELECT TOP 1 ufo2.Name
                                 FROM Units u2
                                 JOIN UnitFinishingOption ufo2 ON ufo2.UnitId = u2.Id
                                 CROSS APPLY (SELECT SUM(ISNULL(ucv.Value, 0)) AS OptTotal FROM UnitConstructionValue ucv WHERE ucv.FinishingOptionId = ufo2.Id) ocv
                                 WHERE u2.ProjectId = p.ProjectID AND u2.IsOption = 0 AND u2.ClientAccountId IS NULL
                                   AND u2.TypeId IN (SELECT t.Id FROM UnitTypes t WHERE t.GroupId IN (1, 4))
                                 ORDER BY (ISNULL(u2.LandValue, 0) + ocv.OptTotal) ASC
                                ) AS MinOptieNaam
                           FROM Project p
                           LEFT JOIN ProjectPictures pic ON pic.Id = p.DefaultPictureId
                           LEFT JOIN PostalCode pc ON pc.PostcodeID = p.PostalCodeID
                          WHERE p.ProjectID = @id AND p.IsPublished = 1", conn)
                    cmd.Parameters.AddWithValue("@id", itemId)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' col: 0=ProjectName 1=Titel 2=Slug 3=PicName 4=MediaType
                            '       5=Straat 6=Gemeente 7=MinSlaap 8=MaxSlaap
                            '       9=AantalEenheden 10=unused 11=VanafPrijs 12=MinOptieNaam
                            Dim picName As String = If(reader.IsDBNull(3), Nothing, reader.GetString(3))
                            Dim isVideo As Boolean = Not reader.IsDBNull(4) AndAlso reader.GetInt32(4) = 1
                            Dim fotoUrl As String = Nothing
                            Dim videoUrl As String = Nothing
                            If Not String.IsNullOrEmpty(picName) AndAlso Not String.IsNullOrEmpty(imgBase) Then
                                If isVideo Then
                                    videoUrl = imgBase & "videos/" & picName
                                Else
                                    fotoUrl = imgBase & "pictures/447/" & picName
                                End If
                            End If
                            Dim straat As String = If(reader.IsDBNull(5), Nothing, reader.GetString(5))
                            Dim gemeente As String = If(reader.IsDBNull(6), Nothing, reader.GetString(6))
                            Dim adres As String = String.Join(", ", {straat, gemeente}.Where(Function(s) Not String.IsNullOrEmpty(s)))
                            Dim minSlaap As Integer? = If(reader.IsDBNull(7), Nothing, CType(reader.GetInt32(7), Integer?))
                            Dim maxSlaap As Integer? = If(reader.IsDBNull(8), Nothing, CType(reader.GetInt32(8), Integer?))
                            Dim aantalEenheden As Integer = If(reader.IsDBNull(9), 0, reader.GetInt32(9))
                            Dim eenhedenLabel As String = Nothing
                            If aantalEenheden > 0 Then
                                eenhedenLabel = aantalEenheden.ToString() & " " & If(aantalEenheden = 1, "eenheid", "eenheden")
                            End If
                            Return New OntdekMeerItemModel With {
                                .ItemType        = "project",
                                .Titel           = reader.GetString(1),
                                .Slug            = If(reader.IsDBNull(2), Nothing, reader.GetString(2)),
                                .IsVideo         = isVideo,
                                .FotoUrl         = fotoUrl,
                                .VideoUrl        = videoUrl,
                                .Street          = adres,
                                .Gemeente        = gemeente,
                                .MinSlaapkamers  = minSlaap,
                                .MaxSlaapkamers  = maxSlaap,
                                .AantalEenheden  = eenhedenLabel,
                                .VanafPrijs      = If(reader.IsDBNull(11), Nothing, CType(reader.GetDecimal(11), Decimal?)),
                                .IsCasco         = Not reader.IsDBNull(12) AndAlso reader.GetString(12).ToLower().Contains("casco")
                            }
                        End If
                    End Using
                End If
            End Using
        Catch ex As Exception
            System.IO.File.AppendAllText(
                System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/ontdek-error.txt"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " | type=" & itemType & " id=" & itemId & " | " & ex.GetType().Name & ": " & ex.Message & Environment.NewLine & ex.StackTrace & Environment.NewLine & "---" & Environment.NewLine)
        End Try
        Return Nothing
    End Function

End Class
