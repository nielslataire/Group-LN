using Microsoft.AspNetCore.Mvc;

namespace CPMCore.Controllers;

// gl-v2 (design-handoff punt 56a/56b): enkel de keuze voor de V2-views; alle acties blijven ongewijzigd. ?classic=true houdt de klassieke pagina bereikbaar.
public partial class IssueNotificationAdminController
{
    private bool IsV2 => ViewData["UseGlV2Layout"] as bool? == true;
}
