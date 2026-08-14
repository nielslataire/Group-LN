Imports WWWCOPRO.Controllers
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
        localization.ForCulture("nl").ForController(Of ProjectsController).ForAction("Index").AddTranslation("woonprojecten")
        ' Photos/News-subpagina's zijn buiten gebruik gesteld (niet meer publiek) —
        ' de bijhorende <Route>-attributen zijn verwijderd, dus deze vertalingen ook.
        localization.ForCulture("nl").ForController(Of ReferencesController).ForAction("Index").AddTranslation("realisaties/{id}")
        localization.ForCulture("nl").ForController(Of ContactController).ForAction("Index").AddTranslation("contacteer-ons")
        localization.ForCulture("nl").ForController(Of ContactController).ForAction("Send").AddTranslation("Verzenden")
        localization.ForCulture("nl").ForController(Of AboutUsController).ForAction("Index").AddTranslation("over-ons")
        localization.ForCulture("nl").ForController(Of TeamController).ForAction("Index").AddTranslation("team")
        'localization.ForCulture("nl").ForNamedRoute("ProjectBySlug").AddTranslation("woonprojecten/{slug}") ' route is al woonprojecten/{slug}
        localization.ForCulture("nl").ForNamedRoute("ReferenceBySlug").AddTranslation("realisaties/{slug}")
    End Sub
End Module
