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
            FeatureKey     = "Invoices.Create.QuickCreateClient",       // unieke sleutel
            PageKey        = "Invoices.Create",               // pagina-sleutel
            TargetSelector = "#btn-quick-create-client",                   // CSS-selector
            Title          = "Nieuwe knop klant toevoegen",
            Message        = "Hier kan je een nieuwe klant of leverancier snel aanmaken",
            Placement      = CoachmarkPlacement.Right,
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
    };

    /// <summary>Alle geregistreerde definities.</summary>
    public static IReadOnlyList<CoachmarkDefinition> GetAll() => _definitions;

    /// <summary>Alle definities voor een specifieke pagina.</summary>
    public static IReadOnlyList<CoachmarkDefinition> GetForPage(string pageKey) =>
        _definitions.Where(d => d.PageKey == pageKey).ToList();
}
