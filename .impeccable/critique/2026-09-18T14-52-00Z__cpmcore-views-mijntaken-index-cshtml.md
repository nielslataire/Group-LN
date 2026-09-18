---
target: mijn taken
total_score: 16
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 2
target_identity: "file:E:\\TFS\\CPMCore\\Views\\MijnTaken\\Index.cshtml"
target_fingerprint: "sha256:77bcdc6ece91a72ce18880231474c64894786d940f6f7ec8c657d4f700b0f724"
target_path: "E:\\TFS\\CPMCore\\Views\\MijnTaken\\Index.cshtml"
timestamp: 2026-09-18T14-52-00Z
slug: cpmcore-views-mijntaken-index-cshtml
---
## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2/4 | Status "Zet" redirects with zero toast/feedback — Opslaan/Toewijzen/Verwijderen all confirm via toast, this one silently reloads |
| 2 | Match System / Real World | 3/4 | Strong Dutch domain vocabulary, undercut by "Toegewezen aan (gebruikers-id, leeg = mezelf)" exposing a raw internal ID to a human |
| 3 | User Control and Freedom | 1/4 | No edit path for an existing task at all; no undo after delete |
| 4 | Consistency and Standards | 1/4 | Three separate, explicit DESIGN.md violations on one page: raw `<h1>`/`<h5>` page chrome, `form-select` instead of `form-control-modern`, bare `confirm()` |
| 5 | Error Prevention | 2/4 | Delete's only guard is a reflex-clickable browser `confirm()` |
| 6 | Recognition Rather Than Recall | 1/4 | Assigning a task requires recalling a colleague's raw ID string; the table never shows current assignee at all |
| 7 | Flexibility and Efficiency | 1/4 | Every row action was a full-page POST+redirect with no bulk path (now partially addressed — see below) |
| 8 | Aesthetic and Minimalist Design | 2/4 | Sparse in spirit (fits the "post-it" framing) but reads as unstyled defaults, not deliberate restraint |
| 9 | Error Recovery | 1/4 | No validation-failure path anywhere in the view; a failed Opslaan has no branch back into the modal |
| 10 | Help and Documentation | 2/4 | Page is simple enough to mostly not need help, except the assignee ID field, which badly does |
| **Total** | | **16/40** | **Poor** |

## Design Specificity Verdict

**LLM assessment:** The data layer underneath this page is genuinely specific — `TaakHerkomst` encodes that tasks auto-spawn from milestones/dossiers, the "mine" query unions three real organizational ownership paths (assigned to me / my unclaimed role / created by me unassigned), and the cross-project list surfaces real Belgian project names. None of that survives into the view: a bare `<table class="table table-hover">`, unstyled `form-select`s, a native `confirm()`. The one place the page tries to look specific — the `gl-kpi2-strip` KPI tiles — is actually broken (see P0 below). Verdict: specific model, generic skin.

**Deterministic scan:** Clean — `impeccable detect --json` returned exit code 0 and `[]` across `MijnTaken/Index.cshtml` and `_MijnTakenWidget.cshtml`. No rule-based violations. This doesn't mean the page is fine — the detector doesn't catch missing CSS files, wrong class names, or a banned interaction pattern like `confirm()`.

**Visual overlays:** Not available — no dev server (live DB dependency, avoided in this session).

## Overall Impression

This page has a real domain model hiding under a page that looks unfinished. The gap between "the ownership query is smarter than most CRUD apps" and "the KPI strip that's supposed to be the at-a-glance triage widget doesn't even render correctly" is the story here.

## What's Working

1. Correct default sort — earliest-due-first with no-date items pushed to the end.
2. Honest "mine" definition — three-path ownership union reflecting real task-handoff logic.
3. KPI-first framing instinct — leading with Open/Achterstallig counts is the right idea, execution currently broken.

## Priority Issues

**[P0] Delete uses a bare browser `confirm()`**
- Why it matters: irreversible action, zero reassurance, explicit DESIGN.md violation.
- Fix: swap for `_ConfirmModal.cshtml` + `confirmDialog()`.
- Status: fixed in this session (applied the `_TrajectActionButtons.cshtml` pattern while implementing bulk status editing).

**[P0] The KPI strip renders unstyled — wrong stylesheet, wrong class name**
- Why it matters: `.gl-kpi2-*` lives in `projecten-custom.css`, which this page never loads; `gl-kpi2-severe` doesn't exist in CSS at all (only `gl-kpi2-value-severe` does).
- Fix: load `projecten-custom.css` (or promote the KPI-strip component to `custom.css`), correct the modifier class.
- Suggested command: `/impeccable harden`

**[P1] No way to edit an existing task**
- Why it matters: the modal never carries a hidden `Id`, so the controller's update branch is dead code from this view.
- Fix: add a per-row edit trigger that opens the modal pre-filled.
- Suggested command: `/impeccable harden`

**[P1] Page title/chrome is duplicated**
- Why it matters: `SetPageHeader` already puts "Mijn taken" in the topbar; the view repeats it locally, violating "Content Body Has No Page Chrome Rule."
- Fix: remove the local heading block, move "Nieuwe taak" into `@section PageActions`.
- Suggested command: `/impeccable layout`

**[P2] Status and Prioriteit carry no color/icon signal**
- Why it matters: the app already owns a 5-state color system (`gl-mijlpaal-status-icon`) that maps almost 1:1 onto `TaakStatus` — ignored entirely here.
- Fix: reuse existing status-color tokens; tint Prioriteit for Dringend/Hoog.
- Suggested command: `/impeccable colorize`

## Persona Red Flags

**Alex (impatient power user)** — couldn't edit a task at all, every status change or delete was a full-page reload (bulk status change now built, single-task editing still a dead end).

**Jordan (confused first-timer)** — "Toegewezen aan (gebruikers-id, leeg = mezelf)" gives no idea what to type; bare-confirm delete now fixed.

**Casey (mobile/on-site user)** — table has no `.table-responsive` wrapper and no mobile fallback.

## Minor Observations

- "Zet" is terse/informal shorthand for a submit action.
- Delete button is icon-only with no `aria-label`.
- Empty state has no icon or CTA, unlike the app's `gl-mc-empty` convention.
- Only `TaakHerkomst.Trigger` gets the "Automatisch" badge.
- Backend already supports `Prioriteit`/free-text filters the filter bar never exposes.
- Assignment has no user picker and the table never shows the current assignee.

## Questions to Consider

1. If this is meant to feel like "post-it notes," why does every interaction require a full server round-trip instead of feeling instant?
2. Why does the page opened every day get none of the app's existing status-color language while a less-frequently-viewed milestone table does?
3. Is delegating a task to a colleague a real supported workflow, or vestigial?
