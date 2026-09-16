---
target: views/projects/traject (alle tabbladen)
total_score: 28
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:E:\\TFS\\CPMCore\\Views\\ProjectTraject"
timestamp: 2026-09-16T15-54-33Z
slug: cpmcore-views-projecttraject
---
Method: dual-agent (A: a546ed71ed9f032b6 · B: abc264dac8fdb05cb)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | The status-change modal never previews the automation it's about to trigger (dossier creation, project-status flip, phase unlock) — only the read-only Kalender detail panel discloses that |
| 2 | Match System / Real World | 4 | Domain-correct Dutch terminology throughout, no jargon leakage |
| 3 | User Control and Freedom | 3 | Cancel/Annuleren everywhere; no undo once a status is committed |
| 4 | Consistency and Standards | 4 | Same status-icon set and row-action pattern reused identically across Index, Timeline, UnitMatrix, and re-themed (not reinvented) inside Kalender |
| 5 | Error Prevention | 2 | 11-field upsert modal has no visible client-validation markup; a status change with real side effects has no confirmation step |
| 6 | Recognition Rather Than Recall | 3 | Status icon + hidden label is solid; the "Volgorde" number field has zero explanation of what it does |
| 7 | Flexibility and Efficiency | 3 | Search/colvis/row-level quick actions are real wins; no bulk status-change across multiple mijlpalen |
| 8 | Aesthetic and Minimalist Design | 3 | Restrained overall; the Kalender toolbar exposes ~12 simultaneous interactive targets across two rows |
| 9 | Error Recovery | 2 | No `asp-validation-for`/`aria-describedby` wiring visible in either modal |
| 10 | Help and Documentation | 1 | No tooltip/help text anywhere beyond icon `title` attributes; no coachmark, unlike `Projecten/Detail` |
| **Total** | | **28/40** | **Good** |

## Design Specificity Verdict

**LLM assessment**: This feature is unusually well-anchored in DESIGN.md rather than generic Bootstrap-admin dressing wearing green paint. The status iconography, row-action pattern, sticky-tabbar navigation, colvis/search DataTable convention, and the confirm-modal delete flow are all reused verbatim from documented patterns, and code comments show the authors actively consulting DESIGN.md while building (e.g. `traject.css` explicitly cites the Warm-Grey Rule when fixing an earlier cool-blue color). The Kalender 2.0 widget — four hand-built views instead of a FullCalendar dependency — is genuine bespoke engineering. Where it loses distinctiveness: the compliance is so total the page reads as "correct" rather than memorable. Strip the green and this could be any dense-data admin screen; the personality lives entirely in the inherited color/status vocabulary, not in any composition choice unique to this screen. It's honoring the system well; it isn't pushing it anywhere new.

**Deterministic scan**: Clean. `impeccable detect --json` against `CPMCore/Views/ProjectTraject/` and, separately, `CPMCore/wwwroot/css/traject.css` both returned `[]` at exit 0 — zero primary findings, zero advisories. Assessment B verified this wasn't a scan gap (confirmed `.cshtml` is parsed as HTML with linked CSS correctly resolved in place, confirmed `--no-config` produces the same empty result so no project-level ignore is suppressing anything). No false positives to report, because there were no findings — the automated pattern-matcher's rule set simply doesn't cover what the design review surfaced below (those are judgment calls, not anti-patterns a static scanner catches).

**Visual overlays**: Not available. No browser automation tool is exposed in this session and there's no dev server running, so the browser-injection overlay step was skipped entirely rather than faked — this run is source-level only for visual judgment.

## Overall Impression

A mature, heavily-iterated feature that faithfully executes its own design system — and the deterministic scanner backs that up with a genuinely clean pass. The gap isn't visual polish, it's consequence visibility at the one moment that matters most: marking a mijlpaal "Bereikt" can silently cascade into dossier creation, a project-status change, or unlocking the next phase, and the modal that actually commits that action shows none of it. The single biggest opportunity is closing that gap, not further chrome polish.

## What's Working

- **Cross-tab status language is genuinely unified, not reinvented per view.** The same status-icon mapping and markup pattern appears identically in `Index.cshtml`, `_Timeline.cshtml`, and `_UnitMatrix.cshtml`, with the icon correctly exempted from the hide-until-hover rule specifically where it doubles as the primary interactive element (a deliberately reasoned exception, documented in a `traject.css` comment, not an oversight).
- **Status-Is-Not-Severity survives contact with a real edge case.** An overdue-but-still-open mijlpaal gets Rust applied only to the date text, leaving the status icon's own color/meaning untouched — the system rule holds up under an edge case, not just the happy path.
- **Kalender 2.0 is real custom engineering that stays on-brand.** Four different view geometries (swimlane, Gantt-style timeline, month grid, agenda) all reuse the same four status colors rather than drifting into a library default palette.

## Priority Issues

**[P1] Status-change modal hides the consequences it's about to trigger**
- Why it matters: The modal reachable from every tab's row action shows only Status/Datum/Opmerking. The trigger list (dossier creation, project-status change, phase unlock) only renders in the passive Kalender detail panel — a path most status changes never go through. The moment meant to feel like completion instead carries silent risk.
- Fix: Surface the same trigger list inside the status modal itself, scoped to the mijlpaal being edited, before submit.
- Suggested command: /impeccable clarify

**[P1] The one on-site-relevant KPI pill disappears exactly where on-site use matters most**
- Why it matters: `.gl-traject-stats` (Fase/bereikt/achterstallig/binnen-14d) is hidden below 992px — which hides "achterstallig" (overdue count), the single most action-relevant number for a PM checking a project on a jobsite tablet. This runs against the project's own documented Field-Width Rule ("on-site phone use is a first-class case, not a fallback").
- Fix: Collapse to a compact 1-2 item summary at that breakpoint instead of hiding entirely.
- Suggested command: /impeccable adapt

**[P2] Mijlpaal date columns likely don't sort chronologically**
- Why it matters: Streefdatum/Werkelijk render as plain `dd/MM/yyyy` text with no `data-order`, while the table's default sort targets that column. DataTables' default type-sniffing frequently mis-sorts `dd/MM/yyyy` as a string, not a date.
- Fix: Add `data-order="{yyyy-MM-dd}"` to those `<td>`s — the same technique already used correctly on the Status column.
- Suggested command: /impeccable harden

**[P2] 11-field status/upsert modal is one flat ungrouped block**
- Why it matters: Contradicts the chunking discipline (`gl-form-section`) the rest of the app's forms follow — identity fields and scheduling fields sit in the same undivided grid.
- Fix: Split into 2-3 visually separated groups even within the modal's compact footprint.
- Suggested command: /impeccable layout

**[P3] Kalender view-switch claims tablist semantics it doesn't implement**
- Why it matters: Buttons carry `role="tab"` but only get click handlers — no arrow-key roving tabindex — so assistive tech announces behavior the widget doesn't back up.
- Fix: Either wire real tablist keyboard behavior, or drop `role="tab"` for a plain button group (matching `.gl-kal2-filters`, which correctly uses `role="group"`).
- Suggested command: /impeccable audit

## Persona Red Flags

**Casey (Mobile/on-site)**: The stats-pill mobile-hide above is the sharpest hit. The Kalender's 4-button icon+text view-switch has no dedicated mobile compaction — only the filter row gets `overflow-x:auto` at the mobile breakpoint — so the nav+title+view-switch row just flex-wraps, risking a cramped multi-line toolbar at the 360-400px widths this app's own Field-Width Rule treats as a hard floor.

**Sam (Accessibility-dependent)**: The tablist/arrow-key mismatch above, plus: neither modal shows `asp-validation-for`/`aria-describedby` wiring, so a screen-reader user submitting an incomplete field has no obvious DOM path to the error.

**Alex (Power user)**: No bulk status-change despite the table already having full search+colvis infrastructure — updating 10 mijlpalen after a site visit is 10 separate modal round-trips. Also: the Trajectsjabloon template editor elsewhere in this codebase has real drag-to-reorder, but the live per-project upsert modal regresses to a bare "Volgorde" number input — a capability a power user would expect carried over from the template editor.

## Minor Observations

- `data-sortable="false"` on the actions `<th>` isn't a real DataTables option (the actual one is `orderable`) — the actions column likely remains sortable despite the intent.
- `.gl-tm-naam` truncates long mijlpaal names with ellipsis but has no `title` attribute — no way to read the full name without opening the edit modal.
- `--kal2-nogtedoen: #495363` is a new, undocumented cool slate-blue token introduced only for this feature, in tension with DESIGN.md's Warm-Grey Rule (though used for data categorization, which the rule allows more latitude for).
- Empty-phase state in `_Timeline.cshtml` is a bare italic line — noticeably less polished than the full icon+title+hint empty-state pattern used one level up for the whole-page empty state.
- The Kalender agenda/chip views tint the entire row for an overdue item, a heavier treatment than the table view's date-only tint for the same status — not wrong, but an inconsistency in how "urgent" is dosed across tabs.

## Questions to Consider

- If marking a mijlpaal "Bereikt" can auto-create a dossier or flip the project status, why does the read-only Kalender panel disclose that and the modal that actually commits it doesn't?
- Was the "Volgorde" number field on the live editor a deliberate v1 scope cut versus the template editor's drag-reorder, or has the live editor just never caught up?
