Imports System.Data.SqlClient
Imports System.Configuration
Imports WWWCOPRO.Models.Vacatures

Public Class VacaturesController
    Inherits System.Web.Mvc.Controller

    ' GET: /vacatures
    <Route("vacatures", Name:="Vacatures")>
    Function Index() As ActionResult
        ViewData("HeroHeader") = True
        ViewData("Title") = "Vacatures | Group LN"
        ViewData("MetaDescription") = "Werken bij Group LN? Bekijk onze openstaande vacatures in projectontwikkeling, verkoop en marketing in Gent."
        ViewData("canonical") = "https://www.groupln.be/vacatures"

        Dim vacatures = GetGepubliceerdeVacatures()
        Return View(vacatures)
    End Function

    ' GET: /vacatures/{slug}
    <Route("vacatures/{slug}", Name:="VacatureDetail")>
    Function Detail(slug As String) As ActionResult
        Dim vacature = GetVacatureBySlug(slug)
        If vacature Is Nothing Then
            Return HttpNotFound()
        End If

        Dim canonicalSlug = If(vacature.Slug, "").ToLowerInvariant()
        ViewData("Title") = vacature.Titel & " | Group LN"
        ViewData("MetaDescription") = If(Not String.IsNullOrWhiteSpace(vacature.KorteBeschrijving), vacature.KorteBeschrijving, "Bekijk deze vacature bij Group LN.")
        ViewData("canonical") = "https://www.groupln.be/vacatures/" & canonicalSlug

        Return View(vacature)
    End Function

    ' ── private helpers ────────────────────────────────────────────────

    Private Function GetConnectionString() As String
        Return ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString
    End Function

    Private Function GetGepubliceerdeVacatures() As List(Of VacatureModel)
        Dim result As New List(Of VacatureModel)

        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                Dim cmd As New SqlCommand(
                    "SELECT Id, Titel, Slug, Categorie, Locatie, Dienstverband, KorteBeschrijving, SortOrder
                       FROM Vacature
                      WHERE IsGepubliceerd = 1
                      ORDER BY SortOrder, Id", conn)

                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        result.Add(New VacatureModel With {
                            .ID = reader.GetInt32(0),
                            .Titel = reader.GetString(1),
                            .Slug = reader.GetString(2),
                            .Categorie = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                            .Locatie = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                            .Dienstverband = If(reader.IsDBNull(5), Nothing, reader.GetString(5)),
                            .KorteBeschrijving = If(reader.IsDBNull(6), Nothing, reader.GetString(6)),
                            .SortOrder = reader.GetInt32(7)
                        })
                    End While
                End Using
            End Using
        Catch ex As Exception
            LogError("VACATURES INDEX", ex)
            ' retourneer lege lijst zodat de pagina niet crasht
        End Try

        Return result
    End Function

    Private Function GetVacatureBySlug(slug As String) As VacatureModel
        If String.IsNullOrWhiteSpace(slug) Then Return Nothing

        Dim vacature As VacatureModel = Nothing

        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                Dim cmd As New SqlCommand(
                    "SELECT Id, Titel, Slug, Categorie, Locatie, Dienstverband, Opleiding, Start, KorteBeschrijving, Beschrijving, SortOrder
                       FROM Vacature
                      WHERE Slug = @slug AND IsGepubliceerd = 1", conn)
                cmd.Parameters.AddWithValue("@slug", slug)

                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        vacature = New VacatureModel With {
                            .ID = reader.GetInt32(0),
                            .Titel = reader.GetString(1),
                            .Slug = reader.GetString(2),
                            .Categorie = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                            .Locatie = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                            .Dienstverband = If(reader.IsDBNull(5), Nothing, reader.GetString(5)),
                            .Opleiding = If(reader.IsDBNull(6), Nothing, reader.GetString(6)),
                            .Start = If(reader.IsDBNull(7), Nothing, reader.GetString(7)),
                            .KorteBeschrijving = If(reader.IsDBNull(8), Nothing, reader.GetString(8)),
                            .Beschrijving = If(reader.IsDBNull(9), Nothing, reader.GetString(9)),
                            .SortOrder = reader.GetInt32(10)
                        }
                    End If
                End Using

                If vacature IsNot Nothing Then
                    Dim taakCmd As New SqlCommand("SELECT Tekst FROM VacatureTaak WHERE VacatureId = @id ORDER BY SortOrder", conn)
                    taakCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = taakCmd.ExecuteReader()
                        While reader.Read()
                            vacature.Takenpakket.Add(reader.GetString(0))
                        End While
                    End Using

                    Dim vereisteCmd As New SqlCommand("SELECT Categorie, Tekst FROM VacatureVereiste WHERE VacatureId = @id ORDER BY SortOrder", conn)
                    vereisteCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = vereisteCmd.ExecuteReader()
                        While reader.Read()
                            If reader.GetString(0) = "MooiMeegenomen" Then
                                vacature.MooiMeegenomen.Add(reader.GetString(1))
                            Else
                                vacature.MustHaves.Add(reader.GetString(1))
                            End If
                        End While
                    End Using

                    Dim voordeelCmd As New SqlCommand("SELECT Tekst FROM VacatureVoordeel WHERE VacatureId = @id ORDER BY SortOrder", conn)
                    voordeelCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = voordeelCmd.ExecuteReader()
                        While reader.Read()
                            vacature.Voordelen.Add(reader.GetString(0))
                        End While
                    End Using

                    Dim stapCmd As New SqlCommand("SELECT Titel, Tekst FROM VacatureSollicitatieStap WHERE VacatureId = @id ORDER BY SortOrder", conn)
                    stapCmd.Parameters.AddWithValue("@id", vacature.ID)
                    Using reader = stapCmd.ExecuteReader()
                        While reader.Read()
                            vacature.SollicitatieStappen.Add(New VacatureSollicitatieStapModel With {
                                .Titel = If(reader.IsDBNull(0), Nothing, reader.GetString(0)),
                                .Tekst = If(reader.IsDBNull(1), Nothing, reader.GetString(1))
                            })
                        End While
                    End Using
                End If
            End Using
        Catch ex As Exception
            LogError("VACATURE DETAIL | slug=" & slug, ex)
        End Try

        Return vacature
    End Function

    Private Sub LogError(context As String, ex As Exception)
        Try
            System.IO.File.AppendAllText(
                System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/vacature-error.txt"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " | " & context & " | " & ex.GetType().Name & ": " & ex.Message & Environment.NewLine & ex.StackTrace & Environment.NewLine & "---" & Environment.NewLine)
        Catch
        End Try
    End Sub

End Class
