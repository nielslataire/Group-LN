---
target: views/trajectsjabloonadmin/edit & views/trajectsjabloonadmin/index
total_score: 24
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 1
target_identity: "file:E:\\TFS\\CPMCore\\Views\\TrajectSjabloonAdmin"
timestamp: 2026-09-15T16-03-21Z
slug: cpmcore-views-trajectsjabloonadmin
---
# Design Critique: TrajectSjabloonAdmin (Index + Edit)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Dirty-chip, live Controle badge, live streefdatum recompute are good; no feedback during the full-page Opslaan postback |
| 2 | Match System / Real World | 3 | Fluent Dutch domain language and a sentence-style date rule, undercut by a raw JSON field for a stated non-technical audience |
| 3 | User Control and Freedom | 2 | No Cancel/Annuleren action anywhere on the page; no undo for delete |
| 4 | Consistency and Standards | 2 | Bespoke topbar-actions pattern instead of the documented `_FormShellActions` bar; native `confirm()` breaks the crafted visual language; Index duplicates its own header |
| 5 | Error Prevention | 1 | `required` on `#sj-naam` is inert — verified: the field sits outside `#sjabloonForm` with no `form=` association — nothing blocks a blank-name submit |
| 6 | Recognition Rather Than Recall | 3 | Anchor dropdown shows readable labels; keyboard-reorder is documented once in a subtitle, not at point of use |
| 7 | Flexibility and Efficiency of Use | 3 | Full roving-tabindex tree, Alt+Arrow reorder, duplicate actions are genuine accelerators; no bulk move, no save shortcut |
| 8 | Aesthetic and Minimalist Design | 3 | Numbered sections keep a dense form legible; the 6-field "Wat is deze mijlpaal?" row is still busy |
| 9 | Error Recovery | 1 | The one realistic failure mode (blank name) silently discards the entire in-memory fase/mijlpaal tree, confirmed against `TrajectSjabloonAdminController.Opslaan` |
| 10 | Help and Documentation | 3 | The collapsed Parameters-JSON reference table is genuinely task-focused; no onboarding for first-time viewers of ankers/bronbinding |
| **Total** | | **24/40** | **Acceptable — significant improvements needed** |

## Design Specificity Verdict

Both assessments agree: this is genuinely product-specific, not a reskin. The domain vocabulary (fases, mijlpalen, ankers, offsets, bronbindingen, triggers) runs into the actual interaction design — the anchor-chain resolver with cycle detection, the "Telt vanaf [anker] + [N] dagen" sentence-style date rule with a live computed badge, the square-root-scaled timeline that deliberately compresses the multi-year aftercare tail, and a Controle tab that understands this domain's real failure modes rather than shipping a generic "form has errors" banner. Where specificity slips: the raw "Parameters JSON" free-text field is an un-costumed implementation detail leaking through.

Deterministic scan: `impeccable detect --json` against all 5 target files returned zero findings (exit 0, confirmed not a silent failure). No false positives to flag. Visual overlays: not available (authenticated internal app, no accessible credentialed session) — reported as expected fallback, not a gap.

## Overall Impression

The domain modeling and accessibility investment are the real achievements. The single biggest opportunity is also the most urgent: the save path has a verified, complete data-loss bug on the most mundane possible mistake (an empty name field), underneath a page that otherwise goes to real lengths to prevent user error.

## What's Working

- Accessibility care that exceeds the rest of the app's own baseline (page-local AA-compliant tokens, real focus-visible rings, aria-describedby, role/aria-pressed on custom controls).
- The live Controle tab: plain-language, actionable Dutch diagnostics with jump-to-item buttons, refreshed on every edit.
- A fully keyboard-operable master/detail tree: roving tabindex, arrow/home/end, Alt+Arrow reorder as a real non-drag alternative.

## Priority Issues

**[P0] Blank template name silently destroys all unsaved work.** `Edit.cshtml:187` — `required` on `#sj-naam`, but the input sits entirely outside `<form id="sjabloonForm">` (closes at line 177) with no `form=` association. Native validation never fires. `TrajectSjabloonAdminController.Opslaan` (149-153) catches the blank name server-side and redirects to Index, discarding the entire in-memory fase/mijlpaal/trigger tree. Verified against current source.
Fix: block submission client-side when `#sj-naam` is empty; on server validation failure, re-render Edit with the payload rehydrated instead of redirecting to Index. → `/impeccable harden`

**[P1] "Parameters JSON" is a silent-failure trap for a non-technical audience.** Controle validates only JSON syntax, never the actual keys `TriggerParamHelper` reads (already enumerated in `TRIGGER_PARAM_HINTS`). A key typo saves cleanly, reports "Alles in orde," and the trigger silently no-ops at runtime forever.
Fix: promote the reference table into real per-action fields (dropdown + toggle), keep raw JSON only as an escape hatch. → `/impeccable clarify`

**[P2] Destructive actions break the system's own visual language.** Delete-fase, delete-mijlpaal, Dupliceren all use native `confirm()` — the one high-stakes moment where the crafted Overlay-shadow vocabulary opts out entirely.
Fix: in-system confirmation showing exactly what's being lost. → `/impeccable harden`

**[P2] Undocumented action-bar convention with no Cancel.** DESIGN.md's Form-Shell Rule specifies Save/Cancel via `_FormShellActions`; Edit.cshtml uses a bespoke topbar partial instead, with no Annuleren control at all.
Fix: add a Cancel affordance, or formally document this as an accepted second convention. → `/impeccable document`

**[P2] Index.cshtml duplicates its own page title.** `SetPageHeader` already renders icon+title+breadcrumb+description in the topbar; Index.cshtml separately renders a near-identical h1/h5 underneath — the exact pattern `gl-page-header` was built to retire.
Fix: delete the local h1/h5 block. → `/impeccable polish`

## Persona Red Flags

**Riley (stress tester)**: blank-name save destroys the tree; a JSON key typo saves silently and no-ops forever; refresh mid-edit loses all unsaved state; duplicate technical codes are correctly caught.

**Sam (accessibility-dependent)**: color swatches announce raw hex, not color names; drag handles are aria-hidden with no button semantics and the keyboard-reorder alternative is documented once, never at point of use. Real label associations and visible focus rings throughout, above this app's own baseline.

**Alex (power user)**: reordering far down a large tree means either imprecise drag or dozens of Alt+Arrow presses, no jump-to-position; no save shortcut; Dupliceren is a full round-trip with no in-place continuation.

## Minor Observations

- `FASE_PALET` hardcodes hex values in JS duplicating DESIGN.md tokens — drift risk.
- Two input-height conventions (46px meta row vs 31px detail panel) coexist, intentional but could read as inconsistent to a first-time viewer.
- `traject.css` still carries an older unused `.gl-sjabloon-trigger` grid rule from a prior version of this editor.
- Mobile action row omits the dirty-state chip present on desktop.

## Questions to Consider

- What actually happens, today, the moment a project manager forgets to type a template name and clicks Save — is losing an afternoon of work an acceptable cost of one missing field?
- What if "Parameters JSON" didn't require anyone to know JSON at all?
- Does deleting a fase with 12 milestones deserve the same unstyled confirm() as deleting one with zero?
