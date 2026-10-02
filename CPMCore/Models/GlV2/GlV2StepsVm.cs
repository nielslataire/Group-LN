namespace CPMCore.Models.GlV2;

/// <summary>Weergave van <see cref="GlV2StepsVm"/> (design-handoff punt 27, "Stappenplan als één
/// component"): <c>Lijn</c> (27c, horizontaal met subtekst — een wijzigingsopdracht- of
/// verkooptraject), <c>Chips</c> (27b, compacte kopbalk-pillen — een budgetflow), <c>Balk</c> (27d,
/// smalle statusbalk voor gsm/kaart), <c>Verticaal</c> (27e/27g, zijpaneel met uitleg per stap).</summary>
public enum GlV2StepsVariant { Lijn, Chips, Balk, Verticaal }

/// <summary>27f "size": <c>Md</c> desktop (standaard), <c>Sm</c> in kaarten/zijpanelen/tablet.</summary>
public enum GlV2StepsSize { Md, Sm }

/// <summary>
/// Status van één stap (27f). <c>Auto</c> (standaard) laat <see cref="GlV2StepsResolver"/> de status
/// afleiden uit <see cref="GlV2StepsVm.Current"/>: vóór de huidige stap → <c>Done</c>, de huidige stap
/// zelf → <c>Current</c>, erna → <c>Todo</c> (of <c>Error</c>/<c>Warning</c> zodra die stap
/// <see cref="GlV2StepItemVm.Errors"/>/<see cref="GlV2StepItemVm.Warnings"/> draagt). Zet de overige
/// waarden enkel expliciet voor wat het verloop zelf niet kan afleiden: <c>Wacht</c> (bv. "1 van 2
/// getekend") en <c>Uit</c> (niet van toepassing voor dit traject, bv. een geweigerde wijzigings-
/// opdracht waarvan "Uitvoering"/"Gefactureerd"/"Betaald" nooit meer aan de beurt komen).
/// </summary>
public enum GlV2StepState { Auto, Done, Current, Todo, Wacht, Warning, Error, Uit }

/// <summary>Kleur van de statuspil vóór de balk (enkel <see cref="GlV2StepsVariant.Balk"/>), los van
/// de status van de individuele stappen — bv. "VERZONDEN" (Wacht) terwijl stap 3 zelf gewoon "Done" is.</summary>
public enum GlV2StepsStatusTone { Ok, Wacht, Error }

/// <summary>Eén stap (27f "steps[]"). Zonder <see cref="State"/> wordt de status afgeleid uit
/// <see cref="GlV2StepsVm.Current"/>; met <see cref="Errors"/>/<see cref="Warnings"/> kleurt en telt
/// een niet-huidige stap automatisch mee (zie <see cref="GlV2StepState"/>).</summary>
public sealed class GlV2StepItemVm
{
    public string Label { get; set; } = "";
    public GlV2StepState State { get; set; } = GlV2StepState.Auto;
    public int Errors { get; set; }
    public int Warnings { get; set; }

    /// <summary>Regel onder het label — datum/wie (27c) of een korte toelichting (27e/27g). Enkel
    /// getoond wanneer <see cref="GlV2StepsVm.ShowSub"/> ook aanstaat.</summary>
    public string? Sub { get; set; }

    /// <summary>Doel-URL voor deze stap. Gezet: de stap is een echte link (CPMCore is een MVC-app
    /// zonder client-side routing, dus "naar een stap springen" is hier gewoon navigeren — geen
    /// aparte <c>onStep</c>-callback nodig). Leeg + <see cref="GlV2StepsVm.Clickable"/>: de stap
    /// rendert als knop die het <c>gl-v2-steps:step</c>-event afvuurt (zie DESIGN.md "Stappenplan"),
    /// voor een toekomstige in-paginawizard die zelf de stapwissel afhandelt. Leeg +
    /// niet-klikbaar: louter weergave (bv. 27c's status van een wijzigingsopdracht).</summary>
    public string? Href { get; set; }
}

/// <summary>
/// Herbruikbare gl-v2 voortgangsindicator (design-handoff punt 27, "Stappenplan als één component —
/// de stappenbalken uit 20d, 22d en 22h, en de budgetflow"). Render via
/// <c>@await Html.PartialAsync("GlV2/_StepsV2", new GlV2StepsVm { ... })</c>. Zie DESIGN.md
/// "Stappenplan" voor de volledige contract- en variant-uitleg (27a–27h).
/// </summary>
public sealed class GlV2StepsVm
{
    /// <summary>Verplicht en uniek op de pagina zodra <see cref="Clickable"/> zonder <see cref="GlV2StepItemVm.Href"/>
    /// gebruikt wordt — een toekomstige in-paginawizard luistert op dit id (zie <see cref="GlV2StepItemVm.Href"/>).</summary>
    public string Id { get; set; } = "gl-v2-steps";

    public List<GlV2StepItemVm> Steps { get; set; } = new();

    /// <summary>Index (vanaf 0) van de huidige stap — bepaalt welke stappen als "Done"/"Todo" gelden
    /// wanneer een stap zelf geen expliciete <see cref="GlV2StepItemVm.State"/> draagt.</summary>
    public int Current { get; set; }

    public GlV2StepsVariant Variant { get; set; } = GlV2StepsVariant.Lijn;
    public GlV2StepsSize Size { get; set; } = GlV2StepsSize.Md;

    /// <summary>Uit = louter weergave, geen enkele stap is een link of knop (27c's wijzigingsopdracht-
    /// status: tonen, niet navigeren).</summary>
    public bool Clickable { get; set; } = true;

    /// <summary>Laat een stap ZONDER <see cref="GlV2StepItemVm.Href"/> toch als knop renderen die het
    /// <c>gl-v2-steps:step</c>-event afvuurt (zie <see cref="GlV2StepItemVm.Href"/>) — enkel aanzetten
    /// wanneer de pagina ook echt naar dat event luistert. Standaard uit: anders zou een pagina die
    /// per ongeluk geen <c>Href</c> meegaf een stap krijgen die er klikbaar uitziet maar niets doet.</summary>
    public bool EmitStepEvents { get; set; }

    /// <summary>Stappen verder dan de eerstvolgende open stap zijn niet klikbaar, ook al heeft de stap
    /// zelf een <see cref="GlV2StepItemVm.Href"/> (27f).</summary>
    public bool LockAhead { get; set; }

    /// <summary>Cijfers in de cirkels tonen (standaard) of enkel de statusvorm (vinkje/!/–, 27f).</summary>
    public bool Numbers { get; set; } = true;

    /// <summary>Subtekst onder elke stap tonen (27c/27e) of weglaten voor een compacte rij (27c's derde voorbeeld).</summary>
    public bool ShowSub { get; set; } = true;

    /// <summary>Enkel bij <see cref="GlV2StepsVariant.Balk"/>: statuspil links van de balk (27d), bv.
    /// "VERZONDEN"/"CONCEPT"/"DEFINITIEF" — los van de status van de individuele stappen.</summary>
    public string? StatusLabel { get; set; }
    public GlV2StepsStatusTone StatusTone { get; set; } = GlV2StepsStatusTone.Ok;

    /// <summary>Enkel bij <see cref="GlV2StepsVariant.Balk"/>: de kopregel ("Stap X van Y · Label" +
    /// statuspil/foutenindicator) tonen. Uit = louter de segmentenbalk — voor een compacte
    /// voortgangsstrip in een tabelcel (bv. de "Verloop"-kolom van een lijst), waar labels toch niet
    /// zouden passen; de volledige tekst blijft wel als tooltip op elk segment staan.</summary>
    public bool ShowBalkHead { get; set; } = true;
}
