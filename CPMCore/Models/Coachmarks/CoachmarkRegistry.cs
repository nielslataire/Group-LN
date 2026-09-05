using BOCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CPMCore.Models.Coachmarks;

/// <summary>
/// Centrale registry van alle coachmark-definities.
///
/// ─ Nieuwe coachmark toevoegen ────────────────────────────────────────────────
/// Voeg een nieuw CoachmarkDefinition object toe aan de _definitions lijst.
/// Gebruik een beschrijvende, hiërarchische FeatureKey (Controller.Page.Feature).
/// Stel altijd een ReleaseDate in en een MaxShowDays passend bij de levensduur.
///
/// ─ Nieuwe sequence toevoegen ─────────────────────────────────────────────────
/// Maak meerdere definities met dezelfde SequenceKey, elk met oplopende StepIndex.
/// De SequenceKey wordt ook gebruikt als StateKey in API-calls.
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public static class CoachmarkRegistry
{
    private static readonly IReadOnlyList<CoachmarkDefinition> _definitions = new List<CoachmarkDefinition>
    {
        // ── Voorbeeld 1: single coachmark ─────────────────────────────────────
        new CoachmarkDefinition
        {
            FeatureKey      = "Projects.Issues.Planning",
            PageKey         = "Projects.Detail.Issues",
            TargetSelector  = "#btnAddPlanning",
            Title           = "Nieuw",
            Message         = "Je kan nu planning toevoegen aan punten.",
            Placement       = CoachmarkPlacement.Bottom,
            ReleaseDate     = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays     = 60
        },
        // eigen test voor + knop in invoices

        new CoachmarkDefinition
        {
            FeatureKey     = "Invoices.Create.CreateRestyling.Step1",       // unieke sleutel
            PageKey        = "Invoices.Create",               // pagina-sleutel
            TargetSelector = ".invoice-step-dot.active",                   // CSS-selector
            Title          = "Stappen factuur opmaken",
            Message        = "Vanaf nu kan je hier je stappen doorlopen voor het opmaken van een factuur",
            Placement      = CoachmarkPlacement.Bottom,
            SequenceKey     = "Invoices.Create.CreateRestyling",
            StepIndex       = 0,
            ReleaseDate    = new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays    = 30
        },
        new CoachmarkDefinition
        {
            FeatureKey     = "Invoices.Create.CreateRestyling.Step2",       // unieke sleutel
            PageKey        = "Invoices.Create",               // pagina-sleutel
            TargetSelector = "#stepNext",                   // CSS-selector
            Title          = "Volgende - Vorige",
            Message        = "Ook hier kan je de stappen doorlopen en op het einde de factuur opslaan",
            Placement      = CoachmarkPlacement.Right,
            SequenceKey     = "Invoices.Create.CreateRestyling",
            StepIndex       = 1,
            ReleaseDate    = new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays    = 30
        },
        new CoachmarkDefinition
        {
            FeatureKey     = "Invoices.Create.CreateRestyling.Step3",       // unieke sleutel
            PageKey        = "Invoices.Create",               // pagina-sleutel
            TargetSelector = "#previewCard",                   // CSS-selector
            Title          = "Preview",
            Message        = "Je krrijgt een preview van de factuur terwijl je deze opmaakt!",
            Placement      = CoachmarkPlacement.Left,
            SequenceKey     = "Invoices.Create.CreateRestyling",
            StepIndex       = 2,
            ReleaseDate    = new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays    = 30
        },
        new CoachmarkDefinition
        {
            FeatureKey     = "Invoices.Create.CreateRestyling.Step4",       // unieke sleutel
            PageKey        = "Invoices.Create",               // pagina-sleutel
            TargetSelector = "#btn-quick-create-client",                   // CSS-selector
            Title          = "Nieuwe knop klant toevoegen",
            Message        = "Hier kan je een nieuwe klant of leverancier snel aanmaken",
            Placement      = CoachmarkPlacement.Right,
            SequenceKey     = "Invoices.Create.CreateRestyling",
            StepIndex       = 3,
            ReleaseDate    = new DateTime(2026, 4, 16, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays    = 30
        },


        // ── Voorbeeld 2: multi-step sequence (2 stappen) ──────────────────────
        new CoachmarkDefinition
        {
            FeatureKey      = "Projects.Issues.Reminders.Step1",
            PageKey         = "Projects.Detail.Issues",
            TargetSelector  = "#btnSendReminder",
            Title           = "Herinneringen versturen",
            Message         = "Klik hier om een herinnering te sturen naar projectleden.",
            Placement       = CoachmarkPlacement.Bottom,
            SequenceKey     = "Projects.Issues.Reminders",
            StepIndex       = 0,
            ReleaseDate     = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays     = 60
        },
        new CoachmarkDefinition
        {
            FeatureKey      = "Projects.Issues.Reminders.Step2",
            PageKey         = "Projects.Detail.Issues",
            TargetSelector  = "#issuesTable",
            Title           = "Individuele herinneringen",
            Message         = "Je kan ook per rij afzonderlijk herinneringen instellen via het actie-menu.",
            Placement       = CoachmarkPlacement.Top,
            SequenceKey     = "Projects.Issues.Reminders",
            StepIndex       = 1,
            ReleaseDate     = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays     = 60
        },

        // ── Projectleider-dashboard: kennismakingstour (4 stappen) ────────────
        // Alle vier de elementen zijn pas sinds deze herwerking écht functioneel
        // (KPI-kleuren, snooze/toggle-JS, desktop leverancier/klant-zoeken,
        // project vastzetten/pinnen).
        new CoachmarkDefinition
        {
            FeatureKey      = "Home.Dashboard.Projectleider.Tour.Step1",
            PageKey         = "Home.Dashboard.Projectleider",
            TargetSelector  = ".gl-kpi-strip",
            Title           = "Je dashboard in één oogopslag",
            Message         = "Actieve projecten en open punten blijven groen; Urgent en Achterstallig springen eruit in kleur zodra ze aandacht vragen.",
            Placement       = CoachmarkPlacement.Bottom,
            SequenceKey     = "Home.Dashboard.Projectleider.Tour",
            StepIndex       = 0,
            ReleaseDate     = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays     = 45
        },
        new CoachmarkDefinition
        {
            FeatureKey      = "Home.Dashboard.Projectleider.Tour.Step2",
            PageKey         = "Home.Dashboard.Projectleider",
            TargetSelector  = ".gl-mc-card",
            Title           = "Meldingen per urgentie",
            Message         = "ACTIE VEREIST vraagt meteen actie, OP TE LOSSEN kan nog even wachten, TER INFO is ter kennisgeving. Snooze een melding met het klokje-icoon.",
            Placement       = CoachmarkPlacement.Right,
            SequenceKey     = "Home.Dashboard.Projectleider.Tour",
            StepIndex       = 1,
            ReleaseDate     = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays     = 45
        },
        new CoachmarkDefinition
        {
            FeatureKey      = "Home.Dashboard.Projectleider.Tour.Step3",
            PageKey         = "Home.Dashboard.Projectleider",
            TargetSelector  = "#gl-snelacties-card",
            Title           = "Snelacties",
            Message         = "Rechtstreeks naar een nieuw contract, punt, factuur of leverancier — inclusief leverancier en klant zoeken.",
            Placement       = CoachmarkPlacement.Left,
            SequenceKey     = "Home.Dashboard.Projectleider.Tour",
            StepIndex       = 2,
            ReleaseDate     = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays     = 45
        },
        new CoachmarkDefinition
        {
            FeatureKey      = "Home.Dashboard.Projectleider.Tour.Step4",
            PageKey         = "Home.Dashboard.Projectleider",
            TargetSelector  = "#mw-add-project",
            Title           = "Project vastzetten",
            Message         = "Sta je een project bij dat niet aan jou is toegewezen? Zet het hier vast — het verschijnt dan naast je eigen projecten in Mijn Werven. Losmaken kan via het pin-icoontje op de kaart.",
            Placement       = CoachmarkPlacement.Bottom,
            SequenceKey     = "Home.Dashboard.Projectleider.Tour",
            StepIndex       = 3,
            ReleaseDate     = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays     = 45
        },
        new CoachmarkDefinition
        {
            FeatureKey      = "Home.Dashboard.Projectleider.Tour.Step5",
            PageKey         = "Home.Dashboard.Projectleider",
            TargetSelector  = "#mw-arrange-toggle",
            Title           = "Rangschikken",
            Message         = "Wil je Mijn Werven in je eigen volgorde zetten? Klik op Rangschikken, sleep een werf aan de greep of gebruik de pijltjes, en klik op Klaar om te bewaren.",
            Placement       = CoachmarkPlacement.Bottom,
            SequenceKey     = "Home.Dashboard.Projectleider.Tour",
            StepIndex       = 4,
            ReleaseDate     = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
            MaxShowDays     = 45
        },
    };

    /// <summary>Alle geregistreerde definities.</summary>
    public static IReadOnlyList<CoachmarkDefinition> GetAll() => _definitions;

    /// <summary>Alle definities voor een specifieke pagina.</summary>
    public static IReadOnlyList<CoachmarkDefinition> GetForPage(string pageKey) =>
        _definitions.Where(d => d.PageKey == pageKey).ToList();
}
