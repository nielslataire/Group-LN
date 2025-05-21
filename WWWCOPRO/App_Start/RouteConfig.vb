Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.Web.Mvc
Imports System.Web.Routing
Imports System.Globalization
Imports RouteLocalization.Mvc
Imports RouteLocalization.Mvc.Extensions
Imports RouteLocalization.Mvc.Setup
Imports WWWCOPRO.RouteTranslations


Public Class RouteConfig
    Public Shared Sub RegisterRoutes(ByVal routes As RouteCollection)
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}")
        'routes.MapMvcAttributeRoutes()
        routes.MapMvcAttributeRoutes(Localization.LocalizationDirectRouteProvider)

        Const en As String = "en"
        Dim acceptedCultures As ISet(Of String) = New HashSet(Of String)() From { _
            en, _
            "nl" _
            }

        routes.Localization(Function(configuration)
                                configuration.DefaultCulture = en
                                configuration.AcceptedCultures = acceptedCultures
                                configuration.AttributeRouteProcessing = AttributeRouteProcessing.AddAsNeutralAndDefaultCultureRoute
                                configuration.AddCultureAsRoutePrefix = False
                                configuration.AddTranslationToSimiliarUrls = True
                            End Function).TranslateInitialAttributeRoutes().Translate(Function(localization1)
                                                                                          localization1.AddRoutesTranslation()
                                                                                      End Function)
        CultureSensitiveHttpModule.GetCultureFromHttpContextDelegate = Localization.DetectCultureFromBrowserUserLanguages(acceptedCultures, en)

    ' Dim cultureEN = CultureInfo.GetCultureInfo("en-US")
    ' Dim cultureNL = CultureInfo.GetCultureInfo("nl-BE")


    ' Dim translationTables = New List(Of ControllerTranslation)() From { _
    '     New ControllerTranslation("Home", New List(Of Translation)() From { _
    '         New Translation(cultureEN, "Home"), _
    '         New Translation(cultureNL, "Home") _
    '     }, New List(Of ActionTranslation)() From { _
    '         New ActionTranslation("Home", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Home"), _
    '             New Translation(cultureNL, "Home") _
    '         }) _
    '     }), _
    '     New ControllerTranslation("Projects", New List(Of Translation)() From { _
    '         New Translation(cultureEN, "Projects"), _
    '         New Translation(cultureNL, "Woonprojecten") _
    '     }, New List(Of ActionTranslation)() From { _
    '         New ActionTranslation("Projects", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Projects"), _
    '             New Translation(cultureNL, "Woonprojecten") _
    '         }), _
    '         New ActionTranslation("Detail", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Detail"), _
    '             New Translation(cultureNL, "Detail") _
    '         }), _
    '         New ActionTranslation("News", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "News"), _
    '             New Translation(cultureNL, "Nieuws") _
    '         }), _
    '         New ActionTranslation("Photos", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Photos"), _
    '             New Translation(cultureNL, "fotos") _
    '         }) _
    '     }), _
    '      New ControllerTranslation("References", New List(Of Translation)() From { _
    '         New Translation(cultureEN, "References"), _
    '         New Translation(cultureNL, "Realisaties") _
    '     }, New List(Of ActionTranslation)() From { _
    '         New ActionTranslation("Detail", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Detail"), _
    '             New Translation(cultureNL, "Detail") _
    '         }), _
    '         New ActionTranslation("References", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "References"), _
    '             New Translation(cultureNL, "Realisaties") _
    '         }), _
    '         New ActionTranslation("Photos", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Photos"), _
    '             New Translation(cultureNL, "fotos") _
    '         }) _
    '     }), _
    '     New ControllerTranslation("Contact", New List(Of Translation)() From { _
    '         New Translation(cultureEN, "Contact"), _
    '         New Translation(cultureNL, "Contacteren") _
    '     }, New List(Of ActionTranslation)() From { _
    '         New ActionTranslation("Contact", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Contact"), _
    '             New Translation(cultureNL, "Contacteren") _
    '         }) _
    '     }), _
    '     New ControllerTranslation("AboutUs", New List(Of Translation)() From { _
    '         New Translation(cultureEN, "AboutUs"), _
    '         New Translation(cultureNL, "Over-ons") _
    '     }, New List(Of ActionTranslation)() From { _
    '         New ActionTranslation("AboutUs", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "AboutUs"), _
    '             New Translation(cultureNL, "Over-ons") _
    '         }) _
    '     }), _
    '     New ControllerTranslation("Newsletter", New List(Of Translation)() From { _
    '         New Translation(cultureEN, "Newsletter"), _
    '         New Translation(cultureNL, "Nieuwsbrief") _
    '     }, New List(Of ActionTranslation)() From { _
    '         New ActionTranslation("Newsletter", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Newsletter"), _
    '             New Translation(cultureNL, "Nieuwsbrief") _
    '         }) _
    '     }), _
    '                 New ControllerTranslation("Team", New List(Of Translation)() From { _
    '         New Translation(cultureEN, "Team"), _
    '         New Translation(cultureNL, "Team") _
    '     }, New List(Of ActionTranslation)() From { _
    '         New ActionTranslation("Team", New List(Of Translation)() From { _
    '             New Translation(cultureEN, "Team"), _
    '             New Translation(cultureNL, "Team") _
    '         }) _
    '     }) _
    ' }

    ' routes.Add("ProjectsLocalizedRoute", New TranslatedRoute("{controller}/{id}", New RouteValueDictionary(New With { _
    '    Key .controller = "Projects", _
    '    Key .action = "Index", _
    '    Key .id = "" _
    '}), translationTables, New MvcRouteHandler()))

    ' routes.Add("LocalizedRoute", New TranslatedRoute("{controller}/{action}/{id}", New RouteValueDictionary(New With { _
    '     Key .controller = "Home", _
    '     Key .action = "Index", _
    '     Key .id = "" _
    ' }), translationTables, New MvcRouteHandler()))

    ' routes.Add("LocalizedRoute2", New TranslatedRoute("{controller}/{action}", New RouteValueDictionary(New With { _
    '    Key .controller = "Home", _
    '    Key .action = "Index" _
    '}), translationTables, New MvcRouteHandler()))

    '  routes.MapRoute( _
    '    name:="ProjectsWithoutAction", _
    '    url:="{controller}/{id}", _
    '    defaults:=New With {.controller = "Projects", .action = "Index", .id = UrlParameter.Optional} _
    ')
    '  routes.MapRoute( _
    '    name:="ProjectsWithAction", _
    '    url:="{controller}/{action}/{id}", _
    '    defaults:=New With {.controller = "Projects", .action = "Index", .id = UrlParameter.Optional} _
    ')

    '  routes.MapRoute( _
    '     name:="ProjectsDetail", _
    '     url:="{controller}/{action}/{id}/{slug}", _
    '     defaults:=New With {.controller = "Projects", .action = "Detail", .slug = UrlParameter.Optional} _
    ' )


        routes.MapRoute( _
            name:="Default", _
            url:="{controller}/{action}/{id}", _
            defaults:=New With {.controller = "Home", .action = "Index", .id = UrlParameter.Optional} _
        )

    End Sub


End Class