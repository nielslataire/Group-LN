using System.Globalization;

namespace CPMCore.Models.GlV2;

/// <summary>Eén stap na afleiding — wat de partial per stap moet renderen. CSS-klassen i.p.v. de
/// letterlijke inline-kleuren van de referentiecomponent: de kleur/vorm per status staat in
/// <c>gl-v2-shell.css</c> (<c>.gl-v2-steps-dot.is-error</c> e.d.), niet hier.</summary>
public sealed class GlV2StepsResolvedItem
{
    public int Index { get; init; }
    public string Label { get; init; } = "";
    public string? Sub { get; init; }
    public bool HasSub { get; init; }

    /// <summary>Kleurtoon van <see cref="Sub"/>: "error" · "warning" · "" (standaard, gedempt).</summary>
    public string SubTone { get; init; } = "";

    /// <summary>Basisstatus als CSS-modifierklasse: "is-done" · "is-current" · "is-todo" · "is-wacht"
    /// · "is-warning" · "is-error" · "is-uit". Zelfde klasse op de bol/chip/segment/rij van elke
    /// variant — de vier variant-CSS-blokken delen daardoor één statuspalet.</summary>
    public string StateClass { get; init; } = "is-todo";

    public bool IsCurrent { get; init; }

    /// <summary>De HUIDIGE stap draagt zelf fouten — kleurt vol rood (bol, label, chip, segment),
    /// ongeacht de onderliggende <see cref="StateClass"/> (27f: fout staat boven alles).</summary>
    public bool CurrentHasErrors { get; init; }

    /// <summary>De huidige stap draagt enkel aandachtspunten — enkel een extra gouden/okerring rond
    /// de bol, de kleur zelf verandert niet (anders dan <see cref="CurrentHasErrors"/>).</summary>
    public bool CurrentHasWarnings { get; init; }

    public bool IsCheck { get; init; }

    /// <summary>Cijfer, "!" of "–"; leeg wanneer <see cref="GlV2StepsVm.Numbers"/> uit staat.</summary>
    public string Glyph { get; init; } = "";

    public bool HasConnector { get; init; }
    public bool ConnectorDone { get; init; }

    public bool HasBadge { get; init; }
    /// <summary>Enkel het aantal — voor de ronde, solide badge (lijn/chips).</summary>
    public int BadgeCount { get; init; }
    /// <summary>Volledige tekst ("2 fouten"/"1 aandachtspunt") — voor de zachte pil (verticaal).</summary>
    public string BadgeText { get; init; } = "";
    public bool BadgeIsError { get; init; }

    public bool CanInteract { get; init; }
    public string? Href { get; init; }
    public string Tooltip { get; init; } = "";
}

/// <summary>Resultaat van <see cref="GlV2StepsResolver.Resolve"/>: de stappen plus wat de balk-
/// variant (27d) bovenaan nodig heeft.</summary>
public sealed class GlV2StepsResolved
{
    public IReadOnlyList<GlV2StepsResolvedItem> Items { get; init; } = Array.Empty<GlV2StepsResolvedItem>();

    /// <summary>"Stap 4 van 9 · Dak &amp; Afbraak" — enkel <see cref="GlV2StepsVariant.Balk"/>.</summary>
    public string BalkText { get; init; } = "";
    public bool HasStatus { get; init; }

    /// <summary>Of er ergens in het traject fouten/aandachtspunten open staan (27d's "2 stappen met fouten").</summary>
    public bool HasIssues { get; init; }
    public string IssueText { get; init; } = "";
    public bool IssuesAreErrors { get; init; }
}

/// <summary>
/// Leidt per stap de werkelijke status, cijfer/vinkje, badge en klikbaarheid af (design-handoff punt
/// 27, letterlijke overname van de referentiecomponent se <c>renderVals()</c> — zie DESIGN.md
/// "Stappenplan" voor de regels in woorden). Gescheiden van de partial zodat de afleiding op één
/// plek staat i.p.v. verweven in Razor-markup, en zodat een controller dezelfde samenvatting (bv.
/// <see cref="GlV2StepsResolved.IssueText"/>) ook buiten de view kan herhalen (bv. in een toast).
/// </summary>
public static class GlV2StepsResolver
{
    public static GlV2StepsResolved Resolve(GlV2StepsVm vm)
    {
        var steps = vm.Steps;
        var n = steps.Count;
        var current = vm.Current;

        // 27f "lockAhead": op de eerstvolgende stap ná de huidige die niet al expliciet Done is —
        // vrijwel altijd current+1, behalve wanneer een latere stap zelf al vooraf is afgewerkt.
        var firstOpen = -1;
        for (var i = current + 1; i < n; i++)
        {
            if (steps[i].State != GlV2StepState.Done) { firstOpen = i; break; }
        }

        var stateClasses = new string[n];
        for (var i = 0; i < n; i++)
        {
            var s = steps[i];
            if (s.State != GlV2StepState.Auto) { stateClasses[i] = ClassOf(s.State); continue; }
            if (i < current) { stateClasses[i] = "is-done"; continue; }
            if (i == current) { stateClasses[i] = "is-current"; continue; }
            // Enkel stappen ná de huidige kleuren automatisch rood/goud op hun eigen fouten/
            // aandachtspunten — de huidige stap zelf blijft logisch "current" (de override hieronder
            // regelt zijn kleur apart, zonder zijn status te veranderen).
            stateClasses[i] = s.Errors > 0 ? "is-error" : s.Warnings > 0 ? "is-warning" : "is-todo";
        }

        var items = new List<GlV2StepsResolvedItem>(n);
        for (var i = 0; i < n; i++)
        {
            var s = steps[i];
            var isCur = i == current;
            var st = stateClasses[i];
            var errors = s.Errors;
            var warnings = s.Warnings;

            // 27f "fout staat boven alles": geldt ongeacht de onderliggende status van de huidige stap.
            var currentHasErrors = isCur && errors > 0;
            var currentHasWarnings = !currentHasErrors && isCur && warnings > 0;

            var glyph = (st == "is-error" || st == "is-warning" || currentHasErrors) ? "!"
                : st == "is-uit" ? "–"
                : (i + 1).ToString(CultureInfo.InvariantCulture);
            if (!vm.Numbers && glyph != "!" && glyph != "–") glyph = "";

            var isCheck = st == "is-done";
            var locked = st == "is-uit" || (vm.LockAhead && i > current && st == "is-todo" && i != firstOpen);
            // Een stap is enkel écht interactief met een bestemming: een Href (navigeren) of expliciet
            // EmitStepEvents (een pagina die naar "gl-v2-steps:step" luistert). Zonder één van beide zou
            // Clickable alleen een knop opleveren die er klikbaar uitziet maar niets doet.
            var canInteract = vm.Clickable && !locked && (s.Href is not null || vm.EmitStepEvents);

            var hasConnector = i < n - 1;
            var connectorDone = false;
            if (hasConnector && (st == "is-done" || st == "is-warning" || st == "is-error"))
            {
                var nextIsExplicitDone = steps[i + 1].State == GlV2StepState.Done;
                connectorDone = i < current || nextIsExplicitDone;
            }

            var badgeCount = errors > 0 ? errors : warnings;
            var badgeText = errors > 0
                ? errors + (errors > 1 ? " fouten" : " fout")
                : warnings > 0 ? warnings + (warnings > 1 ? " aandachtspunten" : " aandachtspunt") : "";

            var subTone = (st == "is-error" || currentHasErrors) ? "error" : st == "is-warning" ? "warning" : "";

            var tipBase = st switch
            {
                "is-done" => "afgewerkt",
                "is-wacht" => "in afwachting",
                "is-warning" => "aandachtspunt",
                "is-error" => "bevat fouten",
                "is-uit" => "niet van toepassing",
                _ => "nog te doen"
            };
            var tooltip = s.Label + " — " + (isCur ? "huidige stap" : tipBase) + (badgeText.Length > 0 ? " · " + badgeText : "");

            items.Add(new GlV2StepsResolvedItem
            {
                Index = i,
                Label = s.Label,
                Sub = s.Sub,
                HasSub = vm.ShowSub && !string.IsNullOrEmpty(s.Sub),
                SubTone = subTone,
                StateClass = st,
                IsCurrent = isCur,
                CurrentHasErrors = currentHasErrors,
                CurrentHasWarnings = currentHasWarnings,
                IsCheck = isCheck,
                Glyph = glyph,
                HasConnector = hasConnector,
                ConnectorDone = connectorDone,
                HasBadge = badgeCount > 0,
                BadgeCount = badgeCount,
                BadgeText = badgeText,
                BadgeIsError = errors > 0,
                CanInteract = canInteract,
                Href = canInteract ? s.Href : null,
                Tooltip = tooltip,
            });
        }

        var curLabel = current >= 0 && current < n ? steps[current].Label : "";
        var balkText = n == 0 ? "" : $"Stap {current + 1} van {n} · {curLabel}";

        // "fouten boven aandachtspunten" op dossierniveau (27d) — zelfde discipline als per stap:
        // een stap met zowel fouten als aandachtspunten telt enkel als "fout".
        var errSteps = steps.Count(s => s.Errors > 0);
        var warnSteps = steps.Count(s => s.Warnings > 0 && s.Errors == 0);
        var issueText = errSteps > 0
            ? errSteps + (errSteps > 1 ? " stappen" : " stap") + " met fouten"
            : warnSteps + " aandachtspunt" + (warnSteps > 1 ? "en" : "");

        return new GlV2StepsResolved
        {
            Items = items,
            BalkText = balkText,
            HasStatus = !string.IsNullOrWhiteSpace(vm.StatusLabel),
            HasIssues = errSteps > 0 || warnSteps > 0,
            IssueText = issueText,
            IssuesAreErrors = errSteps > 0,
        };
    }

    private static string ClassOf(GlV2StepState state) => state switch
    {
        GlV2StepState.Done => "is-done",
        GlV2StepState.Current => "is-current",
        GlV2StepState.Todo => "is-todo",
        GlV2StepState.Wacht => "is-wacht",
        GlV2StepState.Warning => "is-warning",
        GlV2StepState.Error => "is-error",
        GlV2StepState.Uit => "is-uit",
        _ => "is-todo",
    };
}
