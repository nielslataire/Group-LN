Imports System.Web.Mvc

Namespace Areas.V2

    Public Class V2AreaRegistration
        Inherits AreaRegistration

        Public Overrides ReadOnly Property AreaName() As String
            Get
                Return "V2"
            End Get
        End Property

        Public Overrides Sub RegisterArea(ByVal context As AreaRegistrationContext)
            ' Isoliert onder /v2/... — geen enkele bestaande route/controller gebruikt dit prefix,
            ' dus dit kan nooit met de live site botsen. Zie WWWCOPRO/design-grouplnwww.md.
            context.MapRoute(
                "V2_default",
                "v2/{controller}/{action}/{id}",
                New With {.controller = "Home", .action = "Index", .id = UrlParameter.Optional},
                New String() {"WWWCOPRO.Areas.V2.Controllers"}
            )
        End Sub

    End Class

End Namespace
