---
target: projecten/detail and the main layout — run 3
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:E:\\TFS\\CPMCore\\Views\\Projecten\\DetailV2.cshtml"
target_fingerprint: "sha256:3dcc83312ef0a50fb70c6367eced6a26b60e8c2dcc8fbfba20314817555109d4"
target_path: "E:\\TFS\\CPMCore\\Views\\Projecten\\DetailV2.cshtml"
timestamp: 2026-09-23T09-54-47Z
slug: cpmcore-views-projecten-detailv2-cshtml
closed: true
---
Method: dual-agent (A: general-purpose design-review sub-agent · B: general-purpose detector/evidence sub-agent)

**Third run** — prior scores 27/40 → 29/40. A round of fixes landed since run 2 (h1/h2 heading tree, aria-pressed, scroll-shadow signaling, 44px touch targets on real touch devices, serif/sans documentation, plus separately: the rail's missing Projecten active-state, and title/breadcrumb redundancy fixes on two sibling detail pages). This run verified those fixes directly against the code and did a fresh, unbiased pass rather than re-checking only the old findings — it surfaced a different set of issues, including the same title/breadcrumb redundancy rule catching its own flagship page.

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | `aria-pressed`/live-region filter status is now genuinely good; but "Vraagt actie" links give zero hover/focus feedback that they're clickable. |
| 2 | Match System/Real World | 4 | Domain vocabulary and IA are authentic to how a Belgian PM actually thinks about a project. |
| 3 | User Control and Freedom | 3 | Filter chips reset via "Alles"; units search has no visible clear (×) control. |
| 4 | Consistency and Standards | 2 | Title repeats the breadcrumb's last crumb verbatim — violates the system's own brand-new rule, on its own flagship page. Empty-state `<h2>` sits directly inside a card-title `<h2>`. Attention/row links skip the system's gold focus-ring pattern everything else gets. |
| 5 | Error Prevention | 3 | The "Vraagt actie" panel itself functions as proactive error prevention for the underlying business process. |
| 6 | Recognition Rather Than Recall | 3 | Icons consistently paired with text/tooltips. |
| 7 | Flexibility and Efficiency | 2 | No keyboard shortcuts, no bulk action on the attention list despite it being an obvious triage queue, no column sort on the units table. |
| 8 | Aesthetic and Minimalist Design | 3 | Restrained, purposeful color use undercut by the low-contrast "everything's fine" KPI value. |
| 9 | Error Recovery | 3 | Little applicable error surface; nothing observed is actively broken. |
| 10 | Help and Documentation | 1 | No contextual help anywhere for domain terms a first-timer wouldn't know. |
| **Total** | | **27/40** | **Acceptable — real, verified fixes landed cleanly, but a fresh pass surfaced a different set of issues than run 2 caught.** |

## Design Specificity Verdict

**LLM assessment:** High. Not a reskinned generic admin template — the KPI taxonomy, the "Vraagt actie" category set, and domain terms (ABR-verzekering, Werfmelding, mijlpalen) are native to Belgian residential construction PM work. The team's refusal to fabricate a "Weerverlet" KPI because the real computation isn't cheaply available is a real design-integrity signal, not code hygiene. The gl-v2 visual system is bespoke and consistently authored.

**Deterministic scan:** `impeccable detect --json` across `DetailV2.cshtml`, `_LayoutV2.cshtml`, `_RailPartialV2.cshtml`, `Components/Breadcrumbs/Default.cshtml`, `_ProjectInnerMenuV2.cshtml` — **zero findings**, exit 0. Verified genuine (not a silent no-op): all 5 files confirmed to exist, correct size, and modified as recently as claimed; a single-file rerun on `DetailV2.cshtml` alone also returned `[]`/exit 0. One unrelated pre-existing ignore rule (`side-tab` on `gl-v2-project-inner-menu.css`) is active in project config but doesn't touch any of these 5 markup files.

**Visual overlays:** no browser automation available; both assessments worked from source inspection.

## Overall Impression

The verified fixes hold up under direct re-inspection — the badge/card count unification, the scroll-shadow technique (correctly pinned to the actual surface-white token, not a guessed color), the rail active-state fix, and the aria-pressed/live-region wiring are all real and correctly implemented, not just claimed. But this run's fresh pass (rather than only re-checking old findings) caught something notable: the brand-new "breadcrumb's last item is never the title" rule, written and applied to two sibling detail pages this session, was never checked against this page itself — and this page is the exact pattern the rule exists to catch, on every single project in the app. The heading-hierarchy fix also turns out to have moved the flatness down one level (two `<h2>`s back-to-back on every empty card) rather than resolving it.

## What's Working

1. **The attention-card single-source-of-truth fix is real and holds up.** The topbar badge and the card below now both count `urgentItems` directly — verified in the code, not just asserted.
2. **The scroll-shadow technique is correctly implemented, not just cosmetically present.** Both scroll containers use the layered `background-attachment: local/scroll` gradient trick with the opaque cap color pinned exactly to `--gl-v2-surface` — it only looks right if that color matches the real card background, and it does.
3. **The rail active-state and `aria-pressed` fixes both verified clean** — `IsActive("Projecten", "ProjectTraject", "ProjectDossiers", "ProjectsIssues")` correctly covers the three sibling controllers a project's own tabs live on, and the chip JS toggles `aria-pressed` and the live-region status together correctly on every click.

## Priority Issues

**[P1] Topbar title repeats the breadcrumb's last crumb — on the exact page the rule was just written for.**
- **Why it matters**: `ProjectenController.Detail` sets the breadcrumb's final node to `model.Project.Name`, and `DetailV2.cshtml` sets `ViewData["Title"]` to the identical string. DESIGN.md's own newly-landed "One-Line Topbar Rule" states explicitly that the breadcrumb's last item is never the title itself — and explicitly flagged "no other gl-v2 detail page was audited... worth a project-wide sweep." This is that sweep's first hit, and it's the flagship page, shown on every project.
- **Fix**: Apply the same pattern already used on Invoices/DetailV2 and Projecten/IncommingInvoiceDetailV2 — stop the breadcrumb before the redundant leaf, or give the topbar a distinct title.
- **Suggested command**: `/impeccable harden`

**[P1] The h1/h2 heading fix produced a new flat `<h2>`/`<h2>` sibling pattern on every empty card.**
- **Why it matters**: `.gl-v2-section-card-title` and `.gl-v2-empty-state-title` are both `<h2>`. On Traject/Eenheden/Voortgang/Facturatie/Documenten's empty branches, the card renders `<h2>Traject</h2>` immediately followed by `<h2>Nog geen traject</h2>` — two same-level headings where the second is actually nested content of the first. The whole point of the h1/h2 fix was a real heading tree for screen-reader navigation; this relocates the flatness one level down instead of resolving it.
- **Fix**: `.gl-v2-empty-state-title` should be `<h3>` when nested inside a `.gl-v2-section-card` — visual size/serif stay identical, only the semantic level changes.
- **Suggested command**: `/impeccable harden`

**[P2] `--gl-v2-muted-light` fails WCAG AA contrast and colors real content, not just placeholders.**
- **Why it matters**: `#9aa898` on white is ~2.6:1, failing even the 3:1 large-text threshold. It colors the "OPEN PUNTEN" KPI value when it's 0 (the one purely reassuring number on the page) and every empty field in Algemene gegevens. PRODUCT.md explicitly lists "contrast that holds up outdoors" as a constraint for this exact tablet/field surface.
- **Fix**: Raise the token (DESIGN.md's own disabled-button spec already uses a comparable `#7C8C7A`), or stop reusing this token for real KPI/field content.
- **Suggested command**: `/impeccable harden`

**[P2] No hover or `:focus-visible` treatment on "Vraagt actie" items or Facturatie/Documenten rows.**
- **Why it matters**: These are real links to contracts, insurances, invoices, and documents, with zero CSS for hover/focus — unlike `.gl-v2-flyout-link`/`.gl-v2-userbox-item`/`.gl-v2-menu-item`, which all get the system's documented gold focus ring. Keyboard users tabbing through fall back to the browser default outline, breaking the system's own "same type of action = same UI" rule.
- **Fix**: Add the standard `:focus-visible` gold ring plus a subtle hover background, matching the existing pattern.
- **Suggested command**: `/impeccable harden`

**[P3] Units search has no result-count announcement, unlike the sibling attention filter.**
- **Why it matters**: `initUnitsFilter` hides/shows rows but never updates a live region, while `initAttentionFilters` 60 lines above it in the same file does. A screen-reader user typing into the search gets no feedback that the table just went from 40 rows to 2.
- **Fix**: Add a matching `aria-live="polite"` status element mirroring the attention filter's approach.
- **Suggested command**: `/impeccable polish`

## Persona Red Flags

**Alex (Power User)**: no keyboard shortcuts anywhere. The "Vraagt actie" list — exactly the kind of triage queue a power user wants to bulk-dismiss or snooze — offers no bulk action, one-at-a-time links ported unchanged from the legacy partial. Units table can be searched but not sorted.

**Sam (Accessibility)**: hits the contrast failure directly on the one KPI meant to reassure ("0, niets openstaand") and on every empty field in Algemene gegevens. Tabbing through attention/row links produces the browser default outline instead of gl-v2's gold ring. On an empty new project, heading-navigation lands on "Traject" (h2) immediately followed by "Nog geen traject" (h2) with no structural signal the second is nested.

**Casey (Mobile/Field)**: the 44px touch-target fix for chips/search is real and correctly scoped to touch-only devices. But at phone width the project's inner menu sits behind a tap-to-open sheet stacked above six KPI tiles and the attention card — a PM on-site checking "is anything wrong here" scrolls past a KPI strip before reaching the card that actually answers that question.

## Minor Observations

- Filter chips can total 5 (Alles + up to 4 categories) — right at/over the ≤4-visible-options guideline.
- One inline `style="display:flex;..."` on the "+N meer" media tile — the one escape from the class-based system on an otherwise fully classed page.
- The project name can appear three times simultaneously at ≥1400px (topbar h1, the project-menu panel head, breadcrumb's last crumb) — compounds the P1 title/breadcrumb finding above.
- The units search has no visible clear (×) once text is typed, unlike more polished form patterns elsewhere in gl-v2.

## Questions to Consider

- Now that this rule has caught its own flagship page, should every gl-v2 detail page controller get swept now for the same title/breadcrumb redundancy, rather than waiting for the next report?
- The KPI strip's one purely reassuring value (zero open punten) is the hardest thing on the page to read. If low contrast is effectively reserved for "nothing to worry about here," is that the message actually intended?
- Did the heading-hierarchy fix actually land, or did it just relocate the flatness one level down?
