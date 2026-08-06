' Note: For instructions on enabling IIS6 or IIS7 classic mode, 
' visit http://go.microsoft.com/?LinkId=9394802
Imports System.Web.Http
Imports System.Web.Optimization
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web.Mvc
Imports System.Web
Imports System.Web.Routing
Imports System.Globalization
Imports System.Web.Hosting

Public Class MvcApplication
    Inherits System.Web.HttpApplication



    Sub Application_Start()
        'Dim cultureinfo As CultureInfo = New CultureInfo("nl-BE")
        'System.Threading.Thread.CurrentThread.CurrentUICulture = cultureinfo
        'System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture(cultureinfo.Name)
        AreaRegistration.RegisterAllAreas()

        WebApiConfig.Register(GlobalConfiguration.Configuration)
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters)
        RouteConfig.RegisterRoutes(RouteTable.Routes)
        BundleConfig.RegisterBundles(BundleTable.Bundles)
    End Sub

    Sub Application_Error(sender As Object, e As EventArgs)
        Try
            Dim ex = Server.GetLastError()
            If ex Is Nothing Then Return
            Dim folder = HostingEnvironment.MapPath("~/App_Data/")
            If Not System.IO.Directory.Exists(folder) Then System.IO.Directory.CreateDirectory(folder)
            Dim logFile = System.IO.Path.Combine(folder, "app-error.txt")
            Dim url = If(Request IsNot Nothing, Request.Url?.ToString(), "(onbekend)")
            System.IO.File.AppendAllText(logFile,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & " | url=" & url & " | " & ex.GetType().Name & ": " & ex.Message & Environment.NewLine & ex.ToString() & Environment.NewLine & "---" & Environment.NewLine)
        Catch
        End Try
    End Sub



End Class
