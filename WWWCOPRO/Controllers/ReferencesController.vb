Imports BO
Public Class ReferencesController
    Inherits System.Web.Mvc.Controller

    '
    ' GET: /References
    <Route("References/{id?}")>
    Function Index(Optional id As Integer = 0) As ActionResult
        If Not id = 0 Then

            Dim model As New ReferenceDetailModel
            Dim service = ServiceFactory.GetProjectService
            Dim response = service.GetProjectByID(id)
            If (response.Success) Then model.Data = response.Values.FirstOrDefault
            If model.Data Is Nothing OrElse model.Data.Id = 0 Then Return HttpNotFound()

            ' Deze route (/References/{id}) is legacy — altijd doorsturen naar de echte,
            ' canonieke plek van dit project: realisaties zodra opgeleverd, anders woonprojecten.
            If model.Data.Status IsNot Nothing AndAlso model.Data.Status.Id = CInt(ProjectStatusType.Opgeleverd) Then
                Return RedirectToRoutePermanent("ReferenceBySlug", New With {.slug = model.Data.Slug})
            Else
                Return RedirectToRoutePermanent("ProjectBySlug", New With {.slug = model.Data.Slug})
            End If
        Else
            Dim model As New ReferencesModel
            Dim service = ServiceFactory.GetProjectService

            Dim response = service.GetProjectsForList(0, 1)
            If (response.Success) Then model.Projects = response.Values
            model.Projects = model.Projects.OrderByDescending(Function(m) m.DeliveryDate).ToList
            Return View(model)
        End If

    End Function
    <Route("References/ReferenceBySlug/{slug}", name:="ReferenceBySlug")>
    Function ReferenceBySlug(slug As String) As ActionResult
        Dim model As New ReferenceDetailModel
        Dim service = ServiceFactory.GetProjectService
        Dim response = service.GetProjectBySlug(slug)
        If (response.Success) Then model.Data = response.Values.FirstOrDefault
        If model.Data Is Nothing OrElse model.Data.Id = 0 Then Return HttpNotFound()

        ' Nog niet opgeleverd? Dan hoort dit project (nog) bij woonprojecten, niet bij realisaties.
        If model.Data.Status Is Nothing OrElse model.Data.Status.Id <> CInt(ProjectStatusType.Opgeleverd) Then
            Return RedirectToRoutePermanent("ProjectBySlug", New With {.slug = slug})
        End If

        ' De oude, niet-vertaalde route (/References/ReferenceBySlug/{slug}) permanent
        ' doorsturen naar de schone canonieke URL /realisaties/{slug}.
        If Request.Url.AbsolutePath.StartsWith("/References/", StringComparison.OrdinalIgnoreCase) Then
            Return RedirectPermanent("/realisaties/" & slug)
        End If

        'sort pictures
        model.Data.Pictures = model.Data.Pictures.OrderByDescending(Function(m) m.DateTimeUploaded).ToList

        'Developer
        Dim companyservice = ServiceFactory.GetCompanyService
        Dim response2 = companyservice.GetCompanyByID(model.Data.Developer.ID)
        If (response.Success) Then model.Developer = response2.Values.FirstOrDefault
        'Builder
        response2 = companyservice.GetCompanyByID(model.Data.Builder.ID)
        If (response.Success) Then model.Builder = response2.Values.FirstOrDefault
        'Architect
        response2 = companyservice.GetCompanyByID(model.Data.Architect.ID)
        If (response.Success) Then model.Architect = response2.Values.FirstOrDefault
        ViewData("Title") = WWWCOPRO.Extensions.BuildProjectSeoTitle(model.Data.Name, model.Data.Postalcode?.Gemeente)
        ViewData("MetaDescription") = WWWCOPRO.Extensions.BuildProjectSeoDescription(model.Data.Name, model.Data.Postalcode?.Gemeente, model.Data.Street, model.Data.CommercialTextNL)
        ViewData("canonical") = "https://www.groupln.be/realisaties/" & If(slug, "").ToLowerInvariant()
        Return View("detail", model)


    End Function
    Function Detail(model As ReferenceDetailModel) As ActionResult
        If model Is Nothing OrElse model.Data Is Nothing OrElse model.Data.Id = 0 Then Return HttpNotFound()

        Return View(model)

    End Function
End Class