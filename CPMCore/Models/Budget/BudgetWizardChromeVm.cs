using System;
using System.Collections.Generic;
using CPMCore.Models.GlV2;

namespace CPMCore.Models.Budget
{
    /// <summary>
    /// Alles wat elke gl-v2-wizardpagina (design-handoff punt 39, stap 1-9) deelt: de versie, haar status, het Stappenplan (chips, 27b), de
    /// vorige/volgende-bestemming voor de actiebalk en het projectmenu. De controller bouwt dit met <c>PrepareWizardV2</c> en zet het in
    /// <c>ViewData["Chrome"]</c>; de gedeelde partials <c>V2/_BwStappen</c> en <c>V2/_BwActiebalk</c> renderen het.
    /// </summary>
    public class BudgetWizardChromeVm
    {
        public int VersieId { get; set; }
        public int MasterId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public string MasterNaam { get; set; } = "";
        public int Versienummer { get; set; }
        public string VersieNaam { get; set; } = "";
        public string Status { get; set; } = "Concept";
        public bool IsHuidig { get; set; }
        public DateTime? VastgezetOp { get; set; }
        public string VastgezetDoor { get; set; } = "";

        /// <summary>1-9.</summary>
        public int Step { get; set; }
        public string StepLabel { get; set; } = "";
        public string StepTitle { get; set; } = "";

        public List<GlV2StepItemVm> Steps { get; set; } = new();

        public string PrevUrl { get; set; } = "";
        public string PrevLabel { get; set; } = "";
        public string NextUrl { get; set; } = "";
        public string NextLabel { get; set; } = "";
        /// <summary>Volgende slaat eerst op (formulier/knop) i.p.v. enkel navigeren.</summary>
        public bool NextSaves { get; set; }

        public string OverviewUrl { get; set; } = "";
        public GlV2ProjectMenuVm Menu { get; set; } = new();

        public bool IsLocked => string.Equals(Status, "Definitief", StringComparison.OrdinalIgnoreCase);
        public bool IsAfgerond => string.Equals(Status, "Afgerond", StringComparison.OrdinalIgnoreCase);

        /// <summary>"testindexc · v1" — de versie in titels en kruimels.</summary>
        public string VersieKop => $"{MasterNaam} · v{Versienummer}";

        /// <summary>Badge naast de paginatitel: "testindexc v1 · concept" (39b).</summary>
        public string BadgeTekst => $"{MasterNaam} v{Versienummer} · {Status.ToLowerInvariant()}";

        /// <summary>Open aandachtspunten (som over alle stappen) — voor de resultaatpagina en het overzicht.</summary>
        public int AantalWaarschuwingen { get; set; }
    }
}
