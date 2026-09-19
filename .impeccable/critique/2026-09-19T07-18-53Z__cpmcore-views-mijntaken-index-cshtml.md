---
target: mijntaken
total_score: 19
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 2
target_identity: "file:C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\MijnTaken\\Index.cshtml"
target_fingerprint: "sha256:5988f6514383df1dae8a5e5395d962f351ab884dbc6b81c5dcf95b2fa43b5bbf"
target_path: "C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\MijnTaken\\Index.cshtml"
timestamp: 2026-09-19T07-18-53Z
slug: cpmcore-views-mijntaken-index-cshtml
---
Method: dual-agent (A: af3e345358de7b88b · B: aa7de23a966aad2b0)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | Every action is a full page reload + toast; no in-flight indicator on a surface used on-site over spotty connections |
| 2 | Match System / Real World | 2 | Default status-filter label claims a behavior ("Alle (behalve afgerond/geannuleerd)") the query doesn't actually implement |
| 3 | User Control and Freedom | 2 | Single delete is confirmed with honest copy; bulk status change on N tasks has zero confirmation |
| 4 | Consistency and Standards | 2 | Row actions and two filter selects abandon the page's own `gl-row-action-btn` / `form-control-modern` conventions that the rest of the same file uses correctly |
| 5 | Error Prevention | 1 | `Opslaan` never checks `ModelState.IsValid`; validation rests entirely on an HTML5 `required` attribute |
| 6 | Recognition Rather Than Recall | 3 | "Onbekende gebruiker" fallback and select2 prefill are a deliberate, well-handled edge case |
| 7 | Flexibility and Efficiency | 1 | Text search and priority filter are fully wired server-side and never exposed in the UI; no pagination anywhere |
| 8 | Aesthetic and Minimalist Design | 3 | Restrained, single-accent, well-composed at the component level |
| 9 | Error Recovery | 1 | No `asp-validation-summary`/`asp-validation-for` in the create/edit modal — a rejected save has nowhere to surface |
| 10 | Help and Documentation | 2 | Icon `title` attributes are the only micro-help; empty state gives no next-step guidance |
| **Total** | | **19/40** | **Acceptable (low end)** |

## Design Specificity Verdict

**LLM assessment (Assessment A):** Genuinely authored for CPM in most places — the page correctly reuses `gl-kpi2-*` (real `--kpi-color`/aria-labels), `bulk-bar`/`count-pill`, and a properly-derived 5-state status-icon mapping that follows the Status-Is-Not-Severity Rule (Geannuleerd is muted, never danger-red). That's real component-contract fluency. But it's inconsistent with *itself*: the row actions (Edit/Delete) fall back to bare `btn btn-xs btn-outline-secondary/danger` instead of the documented `gl-row-action-btn`, and the filter-bar selects use Bootstrap 5's `form-select` — a pattern `DESIGN.md` explicitly bans — while the bulk-status and per-row status selects two sections below, in the same file, correctly use `form-control form-control-modern`. The page clearly had a real pass against the design system; two sections just weren't checked against it.

**Deterministic scan (Assessment B):** `impeccable detect --json` against both files returned `[]`, exit code 0 — clean, no pattern-level findings. This isn't a contradiction of Assessment A: the detector catches templated/generic-slop patterns, not cross-file consistency drift (a page using two different select conventions internally) or logic bugs (a filter label that doesn't match its query). Those require reading the code as a whole, which is exactly what Assessment A did. No false positives to report since there were no findings.

**Visual overlays:** Not available. No browser automation tool was exposed to Assessment B, and separately this target is a server-rendered page inside an authenticated, SQL-Server-backed ASP.NET Core session — there is no static/file:// path to render it. This critique is source-only; no live screenshots were taken.

## Overall Impression

The page is competently built and clearly the product of a prior critique pass — several fixes already carry `/impeccable critique P0/P1/P2` comments, and the ones checked here hold up. But two categories of problem remain: a **behavioral bug that undermines the page's core promise** (the default filter doesn't exclude finished/cancelled tasks despite saying it does, so "Mijn taken" silently becomes an unfilterable archive for any returning user), and a **self-inconsistency** where roughly a third of the page's controls quietly skipped the component-contract check the rest of the page passed. Neither is a big rebuild; both are targeted fixes. The single biggest opportunity is closing the gap between what the filter bar promises and what it does — that's the thing a daily user will silently distrust the page for, every single day, without ever filing a bug about it.

## What's Working

1. **The status-icon system correctly resists the "cancelled = red" trap.** `TaakStatusVisual` maps `Geannuleerd` to the muted/dimmed token (`s-3`), not danger — a deliberate, correct read of the Status-Is-Not-Severity Rule where reaching for red would have been the easy default.
2. **The debounced status-select auto-submit is a specifically-reasoned fix.** The comment shows the author understood that a naive `onchange="submit()"` fires on every arrow-key keypress inside a closed `<select>`, and scoped the debounce to exactly the controls where that mattered.
3. **"Onbekende gebruiker" vs. a blank dash.** Distinguishing "nobody assigned" from "assigned to a user that no longer resolves," and feeding that correctly into the select2 prefill instead of leaking a raw GUID, is an edge case most CRUD screens miss entirely.

## Priority Issues

**[P0] Default status filter's label lies about its own behavior**
- **Why it matters**: The default option reads "Alle (behalve afgerond/geannuleerd)," but the service only filters on status when one is explicitly selected — on first visit, nothing is excluded. Every task a user has ever completed or cancelled renders alongside active work, contradicting the UI's own promise, every day, for every user, until they manually apply a filter.
- **Fix**: Either make the service apply the "exclude Afgerond/Geannuleerd" default when no status is selected, or change the label to say what actually happens.
- **Suggested command**: `/impeccable harden`

**[P0] No server-side validation path on the create/edit form**
- **Why it matters**: `Opslaan` never checks `ModelState.IsValid` despite `Titel` being `[Required, StringLength(200)]`, and the modal has no `asp-validation-summary`/`asp-validation-for`. A bypass (disabled JS, an over-length title, a stale resubmit) either silently persists bad data or throws with no in-page recovery.
- **Fix**: Check `ModelState.IsValid` in the controller, return the view with validation state on failure, and add `asp-validation-for` spans per the system's documented error convention.
- **Suggested command**: `/impeccable harden`

**[P1] Row actions and filter selects abandon this page's own component contracts, and land under the touch-target minimum**
- **Why it matters**: Edit/Delete use `btn-xs` (≈22-24px, well under the Field-Width Rule's 40px minimum for an on-site tablet/phone surface) instead of `gl-row-action-btn`; the Status/Project filter selects use `form-select` instead of the documented `form-control-modern`, rendering at a visibly different height than every other 46px input on the same page.
- **Fix**: Swap both to the patterns this same file already uses correctly two sections below.
- **Suggested command**: `/impeccable polish`

**[P1] No confirmation on bulk status change**
- **Why it matters**: Single delete is guarded by `confirmDialog` with honest "cannot be undone" copy. Bulk status change on any number of selected tasks is one click, zero confirmation — strictly larger blast radius, strictly less friction than the action the page already treats as dangerous.
- **Fix**: Route the bulk submit through the same `confirmDialog` pattern already wired for delete, with a count in the copy.
- **Suggested command**: `/impeccable harden`

**[P2] Search and priority filter are fully wired server-side and never exposed in the UI**
- **Why it matters**: `TaakFilterBO.Text` (searches Titel + Omschrijving) and `Prioriteit` are both implemented in the service layer; neither has a control in the filter bar. Combined with no pagination anywhere in the query, a user with a real backlog has no way to narrow the list beyond Status/Project/Achterstallig.
- **Fix**: Add the text input and Prioriteit select to the existing filter row — the backend work is already done.
- **Suggested command**: `/impeccable layout`

## Persona Red Flags

**Casey (Distracted Mobile User, on-site tablet/phone)**
- The status-change select and Edit/Delete buttons sit in the last two columns of an 8-column table wrapped only in `.table-responsive` — on a 360px phone, marking a task done (the single most common daily action) is off-screen behind a horizontal scroll by default.
- Edit/Delete buttons are `btn-xs` (≈22-24px), under the 40px minimum this exact surface is supposed to guarantee per the Field-Width Rule.
- The two filter selects render at Bootstrap's default height, not the 46px `form-control-modern` every other input on the page uses — two different control heights within the first screenful.
- Tapping the "Open" KPI tile navigates to a bare index URL — it doesn't apply an Open filter, it silently discards whatever filter Casey had just set, costing extra taps to redo on a small screen.

**Riley (Deliberate Stress Tester, volume/edge cases)**
- Bulk status change on a large selection has zero confirmation — the worst combination of scale and lack of friction on the page.
- The default filter view silently includes every historical finished/cancelled task — for a user with months of history, "Mijn taken" becomes an ever-growing, unfilterable-by-default archive.
- No search box and no pagination — the only lever against volume is three coarse filters; a specific task among fifty looks the same as a needle in the full list.
- Titel and Omschrijving render unclamped — a long description balloons that row's height for every other row on screen, not just the one who wrote it.

## Minor Observations

- `_MijnTakenWidget.cshtml` uses bare `text-danger` for overdue dates — the exact untokened-red problem the Index page's own prior fix addressed for the priority column, left unfixed in the widget reused on dashboards elsewhere.
- The upsert modal has no `aria-labelledby` pointing at its title element; Bootstrap doesn't wire this automatically.
- `TaakStatusIcon`/`TaakStatusVisual` rely on an implicit default arm rather than an explicit case for every enum value — correct today, silently mis-maps if a status is ever added.
- The "Automatisch" badge for trigger-originated tasks uses a bare `bg-light text-dark` instead of any tokened badge/chip convention.

## Questions to Consider

1. If the Status filter's default option promises to exclude Afgerond/Geannuleerd and doesn't, how many other filter defaults across the codebase carry the same label-vs-query gap — is this a one-off, or an audit-worthy class of bug?
2. The page has a fully-built, tested text-search backend sitting behind no UI — was search cut for time, or is it intentionally deferred?
3. Bulk status change can silently set any number of tasks to "Geannuleerd" in one click with no undo — accepted risk, or an oversight nobody's hit yet?
