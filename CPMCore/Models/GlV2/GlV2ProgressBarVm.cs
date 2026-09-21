namespace CPMCore.Models.GlV2;

/// <summary>Kleurtoon voor <see cref="GlV2ProgressBarVm"/> — design-handoff 7c ("STATEN"). De
/// aanroeper bepaalt de toon (bv. "Financieel &gt; 100%" → Over, "Fysiek loopt achter op
/// Financieel" → Behind); het component zelf vergelijkt geen waarden, het tekent enkel.</summary>
public enum GlV2ProgressTone { Primary, Complete, Over, Behind }

/// <summary>4px ("Fijn" — kaarten/tabelrijen) of 8px ("Standaard" — projectdetail/mobiel). Een
/// bewuste keuze per plaatsingscontext, geen breakpoint-gestuurde omschakeling — zie DESIGN.md-draft.</summary>
public enum GlV2ProgressSize { Fine, Standard }

/// <summary>Herbruikbare gl-v2 voortgangsbalk (design-handoff 7c) — projectbreed, niet dashboard-
/// specifiek. Render via <c>@await Html.PartialAsync("GlV2/_ProgressBar", vm)</c>.</summary>
public class GlV2ProgressBarVm
{
    public string Label { get; set; } = "";
    public string ValueLabel { get; set; } = "";

    /// <summary>Ruwe 0-100+ waarde — de balk zelf klemt dit vast op 100% breedte (design-handoff 7c
    /// "Over budget": "balk stopt op 100%, cijfer wordt rood"), ValueLabel mag de echte, ongeklemde
    /// waarde tonen (bv. "110%").</summary>
    public double Pct { get; set; }

    public GlV2ProgressTone Tone { get; set; } = GlV2ProgressTone.Primary;
    public GlV2ProgressSize Size { get; set; } = GlV2ProgressSize.Fine;
    public bool ShowValue { get; set; } = true;

    /// <summary>Compacte varant van de label-rij: enkel <see cref="Label"/> vóór de balk, geen
    /// waarde, op één regel i.p.v. een aparte rij erboven — voor plekken waar de cijfers bewust
    /// wegblijven (bv. de mobiele projectkaart-rij) maar "welke balk is welke" toch duidelijk moet
    /// zijn. Onafhankelijk van <see cref="ShowValue"/>; de twee sluiten elkaar in de praktijk uit
    /// maar de partial staat toe dat de aanroeper dat zelf bepaalt.</summary>
    public bool InlineLabel { get; set; }

    /// <summary>"Met tussenstand" (design-handoff 7c) — optionele goudkleurige streep die toont waar
    /// het project OP DIT MOMENT zou moeten staan volgens planning, los van de echte voortgang.</summary>
    public double? PlanningMarkerPct { get; set; }

    /// <summary>Onbepaald/bezig-laden-staat (design-handoff 7c "Onbepaald") — negeert Pct/Tone, toont
    /// een bewegende gradiëntstreep.</summary>
    public bool Indeterminate { get; set; }
}
