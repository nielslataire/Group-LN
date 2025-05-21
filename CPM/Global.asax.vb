Imports System.Globalization
Imports System.Threading
Imports System.Web.Optimization

Public Class MvcApplication
    Inherits System.Web.HttpApplication

    Protected Sub Application_Start()
        ViewEngines.Engines.Add(New MyViewEngine())
        AreaRegistration.RegisterAllAreas()

        ModelBinders.Binders.Add(GetType(Decimal), New DecimalModelBinder())

        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters)
        RouteConfig.RegisterRoutes(RouteTable.Routes)
        BundleConfig.RegisterBundles(BundleTable.Bundles)
    End Sub



End Class
