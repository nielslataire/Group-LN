namespace CPMCore.Models.GlV2;

/// <summary>De partial rendert twee keer vanuit dezelfde permissie-opbouw (zie Mode) — desktop/tablet
/// horen als eigen kaart NAAST .gl-v2-body te staan (design-handoff 9a/9b: rail | menu-paneel |
/// inhoud-kaart, drie aparte kaarten), gsm hoort net ÍN de paginakaart, boven de inhoud (9c). Eén DOM
/// kan dat niet met CSS alleen herschikken (verschillende ouders: _LayoutV2's ProjectMenu-sectie vs.
/// de pagina se eigen <main>), dus roept Projecten/DetailV2.cshtml de partial twee keer aan, één keer
/// per Mode.</summary>
public enum GlV2ProjectMenuMode { Outer, Phone }

/// <summary>Kop-info voor <see cref="Views.Shared.GlV2._ProjectInnerMenuV2"/> (design-handoff 9,
/// "Inner menu van het projectdossier") — de rest van het menu (groepen/items/rechten) bouwt de
/// partial zelf op via IPermissionService, exact zoals Views/Shared/DetailMenu.cshtml vandaag al
/// doet. Enkel wat de partial niet zelf goedkoop kan opvragen (naam, ondertitel, coördinatieproject-
/// vlag) komt hier binnen — de aanroepende pagina heeft die data toch al geladen.</summary>
public class GlV2ProjectMenuVm
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";
    public string ProjectSubtitle { get; set; } = "";
    public bool IsCoordinationProject { get; set; }
    public GlV2ProjectMenuMode Mode { get; set; } = GlV2ProjectMenuMode.Outer;

    // Design-handoff 12a/12d: een item toont een teller (bv. "Klanten … 2") ná het label, maar enkel
    // wanneer dat item ook de ACTIEVE ingang is — de mockup's eigen conventie, elders in het menu komt
    // geen enkele teller voor (zie de partial se eigen "geen live tellers"-toelichting). Projectwijd
    // herbruikbaar voor élk item, niet enkel "Klanten": gekeyed op het item se exacte label (zelfde
    // Nederlandse tekst als in de partial, bv. "Klanten", "Punten") i.p.v. een aparte property per
    // item — een volgende pagina die bv. een "Punten"-teller wil, vult gewoon
    // ItemCounts["Punten"] = 3 zonder dat deze klasse of de partial ooit opnieuw moet veranderen. Een
    // pagina die geen tellers heeft (de meeste) laat dit gewoon leeg.
    public Dictionary<string, int> ItemCounts { get; set; } = new();
}
