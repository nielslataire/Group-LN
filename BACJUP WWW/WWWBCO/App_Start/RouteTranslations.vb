Imports Controllers
Imports RouteLocalization.Mvc
Public Module RouteTranslations
    <System.Runtime.CompilerServices.Extension> _
    Public Sub AddRoutesTranslation(ByVal localization As Localization)
        localization.ForCulture("nl").ForNamedRoute("Home").AddTranslation("welkom")
        ' localization.ForCulture("nl").ForNamedRoute("defaultroute").AddTranslation("")
        localization.ForCulture("nl").ForNamedRoute("HomeIndex").AddTranslation("welkom")
        'localization.ForCulture("nl").ForController(Of HomeController).ForAction("Index").AddTranslation("welkom")
        'localization.ForCulture("nl").ForNamedRoute("Projects").AddTranslation("woonprojecten")
        'localization.ForCulture("nl").ForNamedRoute("ProjectById").AddTranslation("woonprojecten/{id}")
        localization.ForCulture("nl").ForController(Of ProjectsController).ForAction("Index").AddTranslation("woonprojecten/{id}")
        localization.ForCulture("nl").ForController(Of ProjectsController).ForAction("Photos").AddTranslation("woonprojecten/{slug}/fotos/")
        localization.ForCulture("nl").ForNamedRoute("ProjectNewsBySlug").AddTranslation("woonprojecten/{slug}/nieuws/")
        localization.ForCulture("nl").ForNamedRoute("ProjectNewsById").AddTranslation("woonprojecten/{id}/nieuws/")
        'localization.ForCulture("nl").ForNamedRoute("ProjectNewsById").AddTranslation("woonprojecten/{id}/nieuws/")
        'localization.ForCulture("nl").ForController(Of ProjectsController).ForAction("News").AddTranslation("woonprojecten/{slug}/nieuws/")
        localization.ForCulture("nl").ForController(Of ReferencesController).ForAction("Index").AddTranslation("realisaties/{id}")
        localization.ForCulture("nl").ForController(Of ContactController).ForAction("Index").AddTranslation("contacteer-ons")
        localization.ForCulture("nl").ForController(Of ContactController).ForAction("Send").AddTranslation("Verzenden")
        localization.ForCulture("nl").ForController(Of ContactController).ForAction("Succes").AddTranslation("Gelukt")
        localization.ForCulture("nl").ForController(Of AboutUsController).ForAction("Index").AddTranslation("over-ons")
        localization.ForCulture("nl").ForController(Of TeamController).ForAction("Index").AddTranslation("ons-team")
        localization.ForCulture("nl").ForNamedRoute("ProjectBySlug").AddTranslation("woonprojecten/{slug}")
        localization.ForCulture("nl").ForNamedRoute("ReferenceBySlug").AddTranslation("realisaties/{slug}")
    End Sub
End Module
