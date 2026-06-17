Imports System.Data.SqlClient
Imports System.Configuration
Imports WWWCOPRO.Models.Blog

Public Class BlogController
    Inherits System.Web.Mvc.Controller

    ' GET: /Blog
    <Route("Blog", Name:="Blog")>
    <Route("Nieuws", Name:="Nieuws")>
    Function Index() As ActionResult
        Dim artikelen = GetGepubliceerdeArtikelen()
        Return View(artikelen)
    End Function

    ' GET: /Blog/{slug}  (ook preview via ?prev=token)
    <Route("Blog/{slug}", Name:="BlogArtikel")>
    <Route("Nieuws/{slug}", Name:="NieuwsArtikel")>
    Function Artikel(slug As String, prev As String) As ActionResult
        Dim verwachtToken = ConfigurationManager.AppSettings("PreviewToken")
        Dim isPreview As Boolean = Not String.IsNullOrEmpty(verwachtToken) AndAlso
                                   Not String.IsNullOrEmpty(prev) AndAlso
                                   prev = verwachtToken

        Dim artikelfull = GetArtikelBySlug(slug, inclConcept:=isPreview)
        If artikelfull Is Nothing Then
            Return HttpNotFound()
        End If

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
            ' Log error — retourneer lege lijst zodat de pagina niet crasht
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
                            MetaTitel, MetaOmschrijving, MetaKeywords, GeoRegio, GeoPlaatsnaam, GeoPositie
                       FROM BlogArtikel
                      WHERE " & whereClause, conn)
                cmd.Parameters.AddWithValue("@slug", slug)

                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        artikel = New BlogArtikelModel With {
                            .ID = reader.GetInt32(0),
                            .Titel = reader.GetString(1),
                            .Slug = reader.GetString(2),
                            .PreviewTekst = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                            .DetailTitel = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                            .DetailTitelTekst = If(reader.IsDBNull(5), Nothing, reader.GetString(5)),
                            .FotoBestand = If(reader.IsDBNull(6), Nothing, reader.GetString(6)),
                            .Datum = reader.GetDateTime(7),
                            .MetaTitel = If(reader.IsDBNull(8), Nothing, reader.GetString(8)),
                            .MetaOmschrijving = If(reader.IsDBNull(9), Nothing, reader.GetString(9)),
                            .MetaKeywords = If(reader.IsDBNull(10), Nothing, reader.GetString(10)),
                            .GeoRegio = If(reader.IsDBNull(11), Nothing, reader.GetString(11)),
                            .GeoPlaatsnaam = If(reader.IsDBNull(12), Nothing, reader.GetString(12)),
                            .GeoPositie = If(reader.IsDBNull(13), Nothing, reader.GetString(13))
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
                            .ID       = reader.GetInt32(0),
                            .SortOrder = reader.GetInt32(1),
                            .Vraag    = If(reader.IsDBNull(2), Nothing, reader.GetString(2)),
                            .Antwoord = If(reader.IsDBNull(3), Nothing, reader.GetString(3))
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

End Class
