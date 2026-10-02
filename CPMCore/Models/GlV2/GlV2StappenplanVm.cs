namespace CPMCore.Models.GlV2
{
    /// <summary>Weergavemodel van het gedeelde gl-v2-stappenplan (Views/Shared/GlV2/_Stappenplan,
    /// design-handoff punt 27, design-handoff/Stappenplan.dc.html). Voorlopig enkel de variant "lijn"
    /// (bolletjes met een verbindingslijn) — de varianten chips/balk/verticaal uit het ontwerp komen
    /// erbij zodra een scherm ze nodig heeft.</summary>
    public class GlV2StappenplanVm
    {
        public List<GlV2StapVm> Steps { get; set; } = new();
        /// <summary>"md" (bol 24px) of "sm" (bol 18px).</summary>
        public string Size { get; set; } = "md";
        /// <summary>Toont het volgnummer in de bol van een stap die nog niet gedaan is.</summary>
        public bool Numbers { get; set; } = true;
        public string? AriaLabel { get; set; }
    }

    public class GlV2StapVm
    {
        public string Label { get; set; } = "";
        /// <summary>done | current | todo | wacht | warning | error | uit.</summary>
        public string State { get; set; } = "todo";
        /// <summary>Kleine regel onder het label (datum, referentie, …).</summary>
        public string? Sub { get; set; }
    }
}
