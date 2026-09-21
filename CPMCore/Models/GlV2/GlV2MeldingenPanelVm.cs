namespace CPMCore.Models.GlV2;

/// <summary>Eén meldingsregel binnen een <see cref="GlV2MeldingGroupVm"/> (design-handoff 7b).
/// ProjectId/Category/Text samen zijn de melding-identiteit voor snooze (zie
/// CPMCore.Models.MeldingKeyHelper) — dezelfde drie stukken moeten dus altijd meegegeven worden,
/// ook al staan ze niet allemaal zichtbaar in de UI.</summary>
public class GlV2MeldingItemVm
{
    public string Text { get; set; } = "";
    public string Meta { get; set; } = "";
    public string IconClass { get; set; } = "ph-info";
    public string BekijkUrl { get; set; } = "#";
    public bool CanSnooze { get; set; }
    public int ProjectId { get; set; }
    public string Category { get; set; } = "";
}

/// <summary>Eén dringendheidsgroep ("ACTIE VEREIST" / "OP TE LOSSEN" / "TER INFO") — design-handoff
/// 7b: "gegroepeerd op dringendheid, niet op datum; de groepsbalk draagt de kleur, de rijen blijven
/// wit".</summary>
public class GlV2MeldingGroupVm
{
    public string Title { get; set; } = "";
    public string Accent { get; set; } = "";
    public string Bg { get; set; } = "";
    public List<GlV2MeldingItemVm> Items { get; set; } = new();
}

/// <summary>Een momenteel gesnoozede melding, getoond onderaan het paneel met "Nu tonen" (design-
/// handoff 7b) i.p.v. in haar eigen groep.</summary>
public class GlV2SnoozedItemVm
{
    public string Text { get; set; } = "";
    public string MeldingKey { get; set; } = "";
    public DateTime Until { get; set; }
    public string UntilLabel { get; set; } = "";
}

/// <summary>Herbruikbare gl-v2 meldingenscherm-inhoud (design-handoff 7b) — desktop toont dit als
/// vast paneel in de dashboardkolom, tablet als popover onder een belletje in de topbar, mobiel als
/// volledig scherm. Render via <c>@await Html.PartialAsync("GlV2/_MeldingenPaneel", vm)</c>.</summary>
public class GlV2MeldingenPanelVm
{
    public string Id { get; set; } = "glV2Meldingen";
    public List<GlV2MeldingGroupVm> Groups { get; set; } = new();
    public List<GlV2SnoozedItemVm> Snoozed { get; set; } = new();

    /// <summary>Badge-aantal — design-handoff 7b: "de teller telt alleen de eerste twee" (actie
    /// vereist + op te lossen, NIET ter info). Expliciet door de aanroeper gezet i.p.v. hier
    /// positioneel af te leiden uit Groups[0]/[1], zodat de volgorde van Groups geen stilzwijgende
    /// aanname wordt.</summary>
    public int BadgeCount { get; set; }
}
