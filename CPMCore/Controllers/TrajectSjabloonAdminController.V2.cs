namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 56d/56e): enkel de keuze voor de V2-views; alle acties blijven ongewijzigd. ?classic=true houdt de klassieke pagina bereikbaar.
public partial class TrajectSjabloonAdminController
{
    private bool IsV2 => ViewData["UseGlV2Layout"] as bool? == true;
}
