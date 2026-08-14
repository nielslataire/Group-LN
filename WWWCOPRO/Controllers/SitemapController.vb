Imports System.Data.SqlClient
Imports System.Configuration
Imports System.Text
Imports System.Xml.Linq
Imports BO

Public Class SitemapController
    Inherits System.Web.Mvc.Controller

    Private Const BaseUrl As String = "https://www.groupln.be"
    Private Shared ReadOnly xmlns As XNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9"

    ' GET: /sitemap.xml
    <Route("sitemap.xml", Name:="Sitemap")>
    Function Index() As ActionResult
        Dim urlset As New XElement(xmlns + "urlset")

        ' Statische pagina's
        AddUrl(urlset, "/", "daily", "1.0")
        AddUrl(urlset, "/over-ons", "monthly", "0.6")
        AddUrl(urlset, "/team", "monthly", "0.5")
        AddUrl(urlset, "/contacteer-ons", "monthly", "0.5")
        AddUrl(urlset, "/woonprojecten", "weekly", "0.9")
        AddUrl(urlset, "/commerciele-projecten", "weekly", "0.8")
        AddUrl(urlset, "/realisaties", "monthly", "0.6")
        AddUrl(urlset, "/blog", "weekly", "0.7")
        AddUrl(urlset, "/vacatures", "weekly", "0.6")

        ' Projecten: actieve projecten -> /woonprojecten/{slug}, opgeleverde -> /realisaties/{slug}
        Try
            Dim service = ServiceFactory.GetProjectService()
            Dim response = service.GetProjectsForList(0, 0, Nothing, 0, False)
            If response.Success Then
                For Each p In response.Values
                    If String.IsNullOrWhiteSpace(p.Slug) Then Continue For
                    If p.Status Is Nothing Then Continue For
                    If p.Status.Id = CInt(ProjectStatusType.Stopgezet) Then Continue For

                    ' routes.LowercaseUrls = True genereert intern altijd kleine-letter-URL's —
                    ' de sitemap moet daarom ook kleine letters gebruiken, anders wijkt de
                    ' opgegeven URL af van de canonical zodra de Slug in de database met een
                    ' hoofdletter begint.
                    Dim pSlug As String = p.Slug.ToLowerInvariant()
                    If p.Status.Id = CInt(ProjectStatusType.Opgeleverd) Then
                        Dim lastmod As String = If(p.DeliveryDate.HasValue, p.DeliveryDate.Value.ToString("yyyy-MM-dd"), Nothing)
                        AddUrl(urlset, "/realisaties/" & pSlug, "yearly", "0.4", lastmod)
                    ElseIf p.ProjectType = ProjectType.Commerciëel Then
                        AddUrl(urlset, "/commerciele-projecten/" & pSlug, "weekly", "0.7")
                    Else
                        AddUrl(urlset, "/woonprojecten/" & pSlug, "weekly", "0.7")
                    End If
                Next
            End If
        Catch
            ' sitemap moet blijven werken ook als de projectdata niet beschikbaar is
        End Try

        ' Blogartikels
        Try
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT Slug, Datum FROM BlogArtikel WHERE IsGepubliceerd = 1 ORDER BY Datum DESC", conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim slug As String = reader.GetString(0).ToLowerInvariant()
                        Dim lastmod As String = If(reader.IsDBNull(1), Nothing, reader.GetDateTime(1).ToString("yyyy-MM-dd"))
                        AddUrl(urlset, "/blog/" & slug, "monthly", "0.6", lastmod)
                    End While
                End Using
            End Using
        Catch
            ' sitemap moet blijven werken ook als de blogdata niet beschikbaar is
        End Try

        ' Vacatures
        Try
            Using conn As New SqlConnection(ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString)
                conn.Open()
                Dim cmd As New SqlCommand("SELECT Slug, GewijzigdOp FROM Vacature WHERE IsGepubliceerd = 1 ORDER BY SortOrder", conn)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim slug As String = reader.GetString(0).ToLowerInvariant()
                        Dim lastmod As String = If(reader.IsDBNull(1), Nothing, reader.GetDateTime(1).ToString("yyyy-MM-dd"))
                        AddUrl(urlset, "/vacatures/" & slug, "weekly", "0.6", lastmod)
                    End While
                End Using
            End Using
        Catch
            ' sitemap moet blijven werken ook als de vacature-data niet beschikbaar is
        End Try

        Dim doc As New XDocument(New XDeclaration("1.0", "UTF-8", Nothing), urlset)
        Return Content(doc.Declaration.ToString() & Environment.NewLine & urlset.ToString(), "application/xml", Encoding.UTF8)
    End Function

    Private Sub AddUrl(urlset As XElement, path As String, changefreq As String, priority As String, Optional lastmod As String = Nothing)
        Dim el As New XElement(xmlns + "url",
            New XElement(xmlns + "loc", BaseUrl & path),
            New XElement(xmlns + "changefreq", changefreq),
            New XElement(xmlns + "priority", priority))
        If Not String.IsNullOrEmpty(lastmod) Then
            el.Add(New XElement(xmlns + "lastmod", lastmod))
        End If
        urlset.Add(el)
    End Sub

End Class
