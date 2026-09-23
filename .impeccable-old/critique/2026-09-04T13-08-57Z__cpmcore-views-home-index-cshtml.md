---
target: dashboard
total_score: 21
max_score: 36
na_heuristics: 9
p0_count: 0
p1_count: 3
target_identity: "file:E:\\TFS\\CPMCore\\Views\\Home\\Index.cshtml"
target_fingerprint: "sha256:68c26c009321ff0398cfe0df9feeff89e3d060d4b08e0fc148ef6f265e6b6436"
target_path: "E:\\TFS\\CPMCore\\Views\\Home\\Index.cshtml"
timestamp: 2026-09-04T13-08-57Z
slug: cpmcore-views-home-index-cshtml
---
# Critique — Standard Dashboard (CPMCore/Views/Home/Index.cshtml)

Method: single-context (degraded — session policy: no sub-agent spawn without explicit user request). Mode: Operate. Detector: clean ([]). No browser overlay (app cannot run locally).

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | Per-project status never shown; filter by an unseen attribute. No loading state, no filtered count |
| 2 | Match System / Real World | 3 | Plain Dutch, domain status names, photo-grid metaphor fits |
| 3 | User Control and Freedom | 3 | Filter resets via "Alle projecten"; aria-pressed correct; nothing destructive |
| 4 | Consistency and Standards | 3 | Shared .card-modern; matches DESIGN.md. Two h2 treatments; card vs filter padding differ |
| 5 | Error Prevention | 3 | Minimal error surface; empty state handled |
| 6 | Recognition Rather Than Recall | 2 | Status invisible on cards; no recent/pinned/on-page search |
| 7 | Flexibility and Efficiency | 1 | No shortcuts, no pinning, no on-page search, fixed sort, filter not persisted |
| 8 | Aesthetic and Minimalist Design | 3 | Clean/on-brand but photo carries no info while dominating; right column near-empty |
| 9 | Error Recovery | n/a | No failable action on this surface |
| 10 | Help and Documentation | 1 | No help, no tooltips, no CoachmarkPageKey, thin empty-state copy |
| Total | | 21 / 36 | Acceptable (58%) |

## Design Specificity Verdict

Category-interchangeable. Photo grid + filter sidebar = Porto "e-commerce products" template, relabelled in Dutch. Nothing in the composition is specific to an internal ops tool for a Belgian residential developer. For an Operate surface it fails the core job: it does not orient a back-office user to what needs attention today. After the last pass removed the empty "Berichtenbox" shell it regressed to a pure project launcher, while HomeController.Index still computes DeedofSaleWarnings / InsuranceWarnings / ProjectInfo and discards them.

Deterministic scan: detect.mjs clean ([]). Agrees with review — no mechanical anti-patterns. The problems are IA/hierarchy/strategy, which the detector cannot see.

## What's Working

1. Responsive grid rework: 1->2->3->4 columns, fixed 4:3 image ratio kills load-shift, gutters normalised across all breakpoints incl. col-xxl.
2. Restrained on-brand execution matching DESIGN.md (green sparingly, soft cards, hairline, Poppins, real empty state with next action).
3. Correct semantics: real buttons, aria-pressed on filters, one h1, lazy images, one keyboard link per card.

## Priority Issues

### [P1] It is a launcher, not a dashboard
Broad back-office team opens this every morning and gets a gallery of all projects; must dig elsewhere for what is urgent. Time-sensitive data (aktes, verzekeringen, project-info) is already fetched by the controller and discarded by the view.
Fix: add a "Vraagt aandacht" region as the primary element (grid becomes secondary), built from existing model data. Decide scope first.
Command: /impeccable shape -> /impeccable layout

### [P1] Project status is invisible on cards
The right column filters by status but an unfiltered card gives no status cue. Users must recall which project is in which state.
Fix: status badge on every card (DESIGN.md badge system) and/or split grid into labelled status sections so the server sort becomes visible structure.
Command: /impeccable colorize + /impeccable layout

### [P1] No hierarchy - photo outranks content
Every card equal weight; 4:3 photo dominates while the project name (the payload for Operate) is small text below. Server "current first" ordering is invisible.
Fix: de-emphasise image, promote name, give "in uitvoering" projects visible primacy via section header or denser/larger treatment.
Command: /impeccable layout

### [P2] Right column under-furnished
~25-33% of viewport for ~6 filter buttons and nothing else; makes the page read as empty.
Fix: move filter to a horizontal chip bar above the grid, or fill the column with the "needs attention" content from P1.
Command: /impeccable layout

### [P2] No accelerators for daily repeat use
PM working on 3 projects re-scans the whole grid every morning. No keyboard nav on filter, no pinning, no on-page search, filter re-defaults every load.
Fix: persist last filter (localStorage), pinning for a "Vastgezet" row, keyboard-navigable filter.
Command: /impeccable harden

## Persona Red Flags

Alex (Power User): no shortcut to a project/filter; full mouse-scan every morning; no pinning/recent/on-page search; filter not persisted; fixed sort. Likely bookmarks project-detail URLs and abandons the dashboard.

Sam (Accessibility): improved post-harden (real buttons, aria-pressed, one h1, focus rings). Remaining: isotope filter reorders DOM with no aria-live announcement (silence for SR users); grid is a div.row not a list (no item count); status conveyed to no one non-visually; card link name is a bare project name.

Wim (Sales Administratie, project-specific): lands on a photo wall of every development when his job is specific clients and aktes; nothing points to "3 aktes te verlijden deze maand" (fetched, discarded); no help on what statuses mean. Treats dashboard as a speed bump.

## Minor Observations

- Two h2 visual treatments in one view (.card-title vs .text-4).
- Project card .card-modern-alt-padding vs filter card plain .card-modern.
- Municipality label has no map-pin icon.
- Long project names wrap unbounded -> uneven rows under isotope fitRows; needs 2-line clamp.
- No CoachmarkPageKey on Index.
- No "X projecten" count anywhere.
- Photo-less mobile cards collapse to a thin whitespace strip.
- Empty-state copy thin; no explanation of why empty or who to ask.

## Questions to Consider

- If a PM only works on 2-3 projects, why show all of them equally every morning?
- What does the dashboard feel like if aktes/verzekeringen/project-info are the first thing you see and the grid is secondary?
- Does "which project do I open" need a whole screen when the sidebar already has Mijn/Alle Projecten?
- What is the one number a Group LN manager wants the instant this loads?
- Is the standard dashboard just missing everything the 870-line Projectleider dashboard already figured out?
