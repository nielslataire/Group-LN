Imports System.Globalization
Public Class TranslatedRoute
    Inherits Route

    Public Property Controllers() As List(Of ControllerTranslation)
        Get
            Return m_Controllers
        End Get
        Private Set(value As List(Of ControllerTranslation))
            m_Controllers = value
        End Set
    End Property
    Private m_Controllers As List(Of ControllerTranslation)

    Public Sub New(url As String, defaults As RouteValueDictionary, controllers As List(Of ControllerTranslation), routeHandler As IRouteHandler)
        MyBase.New(url, defaults, routeHandler)
        Me.Controllers = controllers
    End Sub

    Public Sub New(url As String, defaults As RouteValueDictionary, controllers As List(Of ControllerTranslation), constraints As RouteValueDictionary, routeHandler As IRouteHandler)
        MyBase.New(url, defaults, constraints, routeHandler)
        Me.Controllers = controllers
    End Sub

    ''' <summary>
    ''' Translate URL to route
    ''' </summary>
    ''' <param name="httpContext"></param>
    ''' <returns></returns>
    Public Overrides Function GetRouteData(httpContext As HttpContextBase) As RouteData
        Dim routeData As RouteData = MyBase.GetRouteData(httpContext)
        If routeData Is Nothing Then
            Return Nothing
        End If

        Dim controllerFromUrl As String = routeData.Values("controller").ToString()
        Dim actionFromUrl As String = routeData.Values("action").ToString()
        Dim controllerTranslation = Me.Controllers.FirstOrDefault(Function(d) d.Translation.Any(Function(rf) rf.TranslatedValue = controllerFromUrl))
        Dim controllerCulture = Me.Controllers.SelectMany(Function(d) d.Translation).FirstOrDefault(Function(f) f.TranslatedValue = controllerFromUrl).CultureInfo
        If controllerTranslation IsNot Nothing Then
            routeData.Values("controller") = controllerTranslation.ControllerName
            Dim actionTranslation = controllerTranslation.ActionTranslations.FirstOrDefault(Function(d) d.Translation.Any(Function(rf) rf.TranslatedValue = actionFromUrl))
            If actionTranslation IsNot Nothing Then


                routeData.Values("action") = actionTranslation.ActionName
            End If
            System.Threading.Thread.CurrentThread.CurrentCulture = controllerCulture
            System.Threading.Thread.CurrentThread.CurrentUICulture = controllerCulture
        End If


        Return routeData
    End Function

    ''' <summary>
    ''' Used in Html helper to create link
    ''' </summary>
    ''' <param name="requestContext"></param>
    ''' <param name="values"></param>
    ''' <returns></returns>
    Public Overrides Function GetVirtualPath(requestContext As RequestContext, values As RouteValueDictionary) As VirtualPathData
        System.Threading.Thread.CurrentThread.CurrentCulture = New CultureInfo("nl-BE")

        Dim requestedController = values("controller")
        Dim requestedAction = values("action")
        Dim controllerTranslation = Me.Controllers.FirstOrDefault(Function(d) d.Translation.Any(Function(rf) rf.TranslatedValue = requestedController))
        Dim actionTranslation = controllerTranslation.ActionTranslations.FirstOrDefault(Function(d) d.Translation.Any(Function(rf) rf.TranslatedValue = requestedAction))
        Dim controllerTranslatedName = controllerTranslation.Translation.FirstOrDefault(Function(d) d.CultureInfo.Equals(System.Threading.Thread.CurrentThread.CurrentCulture)).TranslatedValue
        If controllerTranslatedName IsNot Nothing Then
            values("controller") = controllerTranslatedName
        End If
        Dim actionTranslate = controllerTranslation.ActionTranslations.FirstOrDefault(Function(d) d.Translation.Any(Function(rf) rf.TranslatedValue = requestedAction))
        If actionTranslate IsNot Nothing Then
            Dim actionTranslateName = actionTranslate.Translation.FirstOrDefault(Function(d) d.CultureInfo.Equals(System.Threading.Thread.CurrentThread.CurrentCulture)).TranslatedValue
            If actionTranslateName IsNot Nothing Then
                values("action") = actionTranslateName
            End If
        End If
        Return MyBase.GetVirtualPath(requestContext, values)
    End Function
End Class
