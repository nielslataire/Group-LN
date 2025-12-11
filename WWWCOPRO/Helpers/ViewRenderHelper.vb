Imports System.Web.Mvc
Imports System.IO

Public Class ViewRenderHelper
    Public Shared Function RenderViewToString(controllerContext As ControllerContext,
                                              viewName As String,
                                              model As Object) As String

        Dim viewEngineResult = ViewEngines.Engines.FindView(controllerContext, viewName, Nothing)

        If viewEngineResult.View Is Nothing Then
            Throw New FileNotFoundException("View " & viewName & " not found.")
        End If

        controllerContext.Controller.ViewData.Model = model

        Using sw As New StringWriter()
            Dim ctx As New ViewContext(controllerContext, viewEngineResult.View,
                                       controllerContext.Controller.ViewData,
                                       controllerContext.Controller.TempData,
                                       sw)

            viewEngineResult.View.Render(ctx, sw)
            Return sw.ToString()
        End Using
    End Function
End Class