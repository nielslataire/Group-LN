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
End Class
