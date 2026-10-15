Imports BO

' Gedeelde data-logica voor de homepage-hero (zoekbalk-opties + uitgelicht project).
' Geëxtraheerd uit de Private methods op WWWCOPRO.Controllers.HomeController (BuildHeroSearchOptions /
' GetHomeHeroFeatured / GetCoordinationOnlyProjectIds) zodat Areas/V2/Controllers/HomeController
' dezelfde query-logica gebruikt in plaats van ze te dupliceren. Geen gedragswijziging: de live
' HomeController roept nu deze Shared methods aan i.p.v. zijn eigen kopie.
Public Class HomeHeroDataHelper

    Public Shared Function GetCoordinationOnlyProjectIds() As HashSet(Of Integer)
        Dim ids As New HashSet(Of Integer)
        Try
            Using conn As New System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString)
                conn.Open()
                Using cmd As New System.Data.SqlClient.SqlCommand("SELECT ProjectID FROM Project WHERE IsOnlyCoordinationProject = 1", conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            ids.Add(reader.GetInt32(0))
                        End While
                    End Using
                End Using
            End Using
        Catch
        End Try
        Return ids
    End Function

    Public Shared Function BuildHeroSearchOptions() As HeroSearchOptionsModel
        Dim model As New HeroSearchOptionsModel
        Try
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectsForList(0,, TrimCommercialText:=True)
            Dim projects As List(Of ProjectBO) = If(response.Success, response.Values, New List(Of ProjectBO))
            projects = projects.Where(Function(p) p.Status.Id <> CInt(ProjectStatusType.Opgeleverd) AndAlso p.Status.Id <> CInt(ProjectStatusType.Stopgezet)).ToList()

            Dim coordinationIds = GetCoordinationOnlyProjectIds()
            projects = projects.Where(Function(p) Not coordinationIds.Contains(p.Id)).ToList()

            If Not projects.Any() Then Return model

            Dim projectIds = projects.Select(Function(p) p.Id).ToList()
            Dim salesResponse = service.GetProjectSalesData(projectIds)
            Dim salesData As List(Of ProjectSalesDataBO) = If(salesResponse.Success, salesResponse.Values, New List(Of ProjectSalesDataBO))
            ' Zie live HomeController: GetSalesSettings(ids) zet Success op False zodra ÉÉN project
            ' geen instellingen-record heeft — .Values blijft wél correct gevuld, .Success bewust negeren.
            Dim settingsResponse = service.GetSalesSettings(projectIds)
            Dim salesSettings As List(Of ProjectSalesSettingsBO) = If(settingsResponse.Values, New List(Of ProjectSalesSettingsBO))

            Dim eligible As New List(Of ProjectBO)
            Dim eligibleSales As New List(Of ProjectSalesDataBO)
            For Each p In projects
                Dim sales = salesData.FirstOrDefault(Function(s) s.ProjectId = p.Id)
                Dim settings = salesSettings.FirstOrDefault(Function(s) s.ProjectId = p.Id)
                Dim saleVisible = If(settings IsNot Nothing, settings.SaleVisible, False)
                Dim beschikbaar = If(sales IsNot Nothing, sales.LivingUnits - sales.LivingUnitsSold, 0)
                If saleVisible AndAlso beschikbaar > 0 Then
                    eligible.Add(p)
                    If sales IsNot Nothing Then eligibleSales.Add(sales)
                End If
            Next

            ' Regio: DB-data heeft inconsistente casing, normaliseren vóór Distinct.
            model.Regios = eligible.Where(Function(p) p.Postalcode IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(p.Postalcode.Gemeente)) _
                                    .Select(Function(p) NormalizeGemeenteCasing(p.Postalcode.Gemeente)) _
                                    .Distinct() _
                                    .OrderBy(Function(g) g) _
                                    .ToList()

            ' Prijs: cumulatieve "tot"-drempels + 1 ondergrens ("vanaf")
            Dim eligiblePrices = eligibleSales.Where(Function(s) s.StartingPrice > 0D).Select(Function(s) s.StartingPrice).ToList()
            Dim thresholds As New List(Of Decimal) From {300000D, 400000D, 500000D, 600000D, 750000D, 1000000D}
            For Each threshold In thresholds
                If eligiblePrices.Any(Function(price) price <= threshold) Then
                    model.PriceBrackets.Add(New HeroPriceBracket With {
                        .MinValue = Nothing,
                        .MaxValue = threshold,
                        .Label = "Tot " & FormatEuro(threshold)
                    })
                End If
            Next
            If eligiblePrices.Any(Function(price) price >= 1000000D) Then
                model.PriceBrackets.Add(New HeroPriceBracket With {
                    .MinValue = 1000000D,
                    .MaxValue = Nothing,
                    .Label = "Vanaf " & FormatEuro(1000000D)
                })
            End If

            ' Eenheidstype
            Dim hasAppartement = eligibleSales.Any(Function(s) s.NumberAppartments > 0)
            Dim hasWoning = eligibleSales.Any(Function(s) s.NumberHouses > 0)
            Dim hasHandelspand = eligibleSales.Any(Function(s) s.NumberCommercial > 0)
            If hasAppartement Then model.UnitCategories.Add(New HeroUnitCategory With {.Key = "Appartement", .Label = "Appartementen"})
            If hasWoning Then model.UnitCategories.Add(New HeroUnitCategory With {.Key = "Woning", .Label = "Woningen"})
            If hasHandelspand Then model.UnitCategories.Add(New HeroUnitCategory With {.Key = "Handelspand", .Label = "Handelspanden"})
            model.ShowTypeField = model.UnitCategories.Count > 1
        Catch
            ' Bij een fout blijft de zoekbalk gewoon met lege/inerte opties werken; homepage crasht niet
        End Try
        Return model
    End Function

    ' Uitgelicht project op de home-hero: geconfigureerd via CPMCore/Instellingen, in de gedeelde
    ' databank. urlHelper komt van de aanroepende controller (werkt ongeacht Area, Url.Content en
    ' Url.RouteUrl zijn niet area-gebonden).
    Public Shared Function GetHomeHeroFeatured(urlHelper As System.Web.Mvc.UrlHelper) As HomeHeroFeaturedModel
        Try
            Dim heroProjectId As Integer = 0
            Dim kicker As String = Nothing
            Dim titel As String = Nothing
            Dim tekst As String = Nothing
            Dim projectTitelOverride As String = Nothing

            Using conn As New System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("testdbSql").ConnectionString)
                conn.Open()
                Using cmd As New System.Data.SqlClient.SqlCommand("SELECT TOP 1 ProjectId, Kicker, Titel, Tekst, ProjectTitelOverride FROM HomeHeroProject", conn)
                    Using reader = cmd.ExecuteReader()
                        If Not reader.Read() Then Return Nothing
                        heroProjectId = reader.GetInt32(0)
                        If Not reader.IsDBNull(1) Then kicker = reader.GetString(1)
                        If Not reader.IsDBNull(2) Then titel = reader.GetString(2)
                        If Not reader.IsDBNull(3) Then tekst = reader.GetString(3)
                        If Not reader.IsDBNull(4) Then projectTitelOverride = reader.GetString(4)
                    End Using
                End Using
            End Using

            Dim service = ServiceFactory.GetProjectService
            Dim projectResponse = service.GetProjectByID(heroProjectId)
            If Not projectResponse.Success OrElse projectResponse.Values Is Nothing OrElse Not projectResponse.Values.Any() Then Return Nothing
            Dim project = projectResponse.Values.First()

            Dim settingsResponse = service.GetSalesSettings(New List(Of Integer) From {heroProjectId})
            Dim settings = If(settingsResponse.Values, New List(Of ProjectSalesSettingsBO)).FirstOrDefault(Function(s) s.ProjectId = heroProjectId)
            Dim saleVisible = If(settings IsNot Nothing, settings.SaleVisible, False)
            If Not saleVisible Then Return Nothing

            ' Zelfde media-logica als het "uitgelicht project"-blok op Views\Projects\Index.vbhtml
            Dim imgBase As String = System.Web.Configuration.WebConfigurationManager.AppSettings("ImageWebURL")
            Dim videoExts As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {".mp4", ".webm", ".mov", ".avi"}
            Dim isVideo = project.DefaultPicture IsNot Nothing AndAlso
                (project.DefaultPicture.MediaType = 1 OrElse videoExts.Contains(System.IO.Path.GetExtension(project.DefaultPicture.Name)))

            ' Breedte-varianten (srcset) voor de foto: zelfde ProjectPictureInfoReader/V4Pic-mechanisme
            ' als Projects/DetailV4, zodat deze foto ook op gsm een gsm-grote download is i.p.v. de
            ' vaste 800px-versie. Geen varianten gevonden (oude upload) -> Pic blijft Nothing en
            ' ImageSrc (de vaste 800px-URL hieronder) is dan gewoon de enige bron, zoals voorheen.
            Dim pic As V4Pic = Nothing
            If project.DefaultPicture IsNot Nothing AndAlso Not isVideo Then
                Try
                    Dim picInfoMap = ProjectPictureInfoReader.Load(heroProjectId)
                    Dim info As ProjectPictureInfo = Nothing
                    If picInfoMap.TryGetValue(project.DefaultPicture.Name, info) AndAlso info.HasVariants Then
                        pic = New V4Pic With {.Base = imgBase, .Name = project.DefaultPicture.Name, .Width = info.Width, .Height = info.Height, .HasVariants = info.HasVariants}
                    End If
                Catch
                End Try
            End If

            Dim model As New HomeHeroFeaturedModel
            model.Kicker = kicker
            model.Titel = titel
            model.Tekst = tekst
            model.ProjectTitel = If(Not String.IsNullOrWhiteSpace(projectTitelOverride), projectTitelOverride, project.Name)
            model.IsVideo = isVideo
            model.ImageSrc = If(project.DefaultPicture IsNot Nothing AndAlso Not isVideo, urlHelper.Content(imgBase & "pictures/800/" & project.DefaultPicture.Name), urlHelper.Content("~/Content/img/no_image.jpg"))
            model.VideoSrc = If(isVideo, urlHelper.Content(imgBase & "videos/" & project.DefaultPicture.Name), "")
            model.Pic = pic
            Dim detailRouteName = If(project.ProjectType = ProjectType.Commerciëel, "CommercieelBySlug", "ProjectBySlug")
            model.DetailUrl = urlHelper.RouteUrl(detailRouteName, New With {.slug = project.Slug})
            Return model
        Catch
            Return Nothing
        End Try
    End Function

    Private Shared Function NormalizeGemeenteCasing(gemeente As String) As String
        Dim lower = gemeente.Trim().ToLowerInvariant()
        Return Char.ToUpperInvariant(lower(0)) & lower.Substring(1)
    End Function

    Private Shared Function FormatEuro(value As Decimal) As String
        ' nl-BE's standaard groepscheidingsteken is een non-breaking space; hier expliciet een punt,
        ' zoals gangbaar op Vlaamse vastgoedsites.
        Dim culture = CType(New System.Globalization.CultureInfo("nl-BE").Clone(), System.Globalization.CultureInfo)
        culture.NumberFormat.NumberGroupSeparator = "."
        Return "€ " & value.ToString("N0", culture)
    End Function

End Class
