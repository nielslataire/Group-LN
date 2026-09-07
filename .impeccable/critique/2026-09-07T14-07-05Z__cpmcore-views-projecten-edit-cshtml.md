---
target: projecten/editproject
total_score: 19
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 4
target_identity: "file:E:\\TFS\\CPMCore\\Views\\Projecten\\Edit.cshtml"
target_fingerprint: "sha256:e139bc0371612a5c00e0bcc61436a1872c916c9c7f371e277e748d7a7315a46d"
target_path: "E:\\TFS\\CPMCore\\Views\\Projecten\\Edit.cshtml"
timestamp: 2026-09-07T14-07-05Z
slug: cpmcore-views-projecten-edit-cshtml
---
# Critique — projecten/addproject + projecten/editproject

DEGRADED: single-context (no sub-agent spawned — harness policy restricts Task/Agent use to explicit user request; detector run inline)

Targets: CPMCore/Views/Projecten/Toevoegen.cshtml (620) + CPMCore/Views/Projecten/Edit.cshtml (906)
Mode: Operate (data-entry forms)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | No inline validation feedback; only live indicator (slices-total badge) signals validity by colour alone |
| 2 | Match System / Real World | 3 | Domain Dutch is right; same field prompted "coordinatiebedrijf" (Add) vs "facturatiebedrijf" (Edit) |
| 3 | User Control and Freedom | 3 | Cancel + back-chevron present, Cancel honours Referrer; dynamic rows removable; no unsaved-changes guard |
| 4 | Consistency and Standards | 1 | Add vs Edit diverge on date pickers, rich-text editor, coordination UI, card sets, select2 helpers, icons, copy |
| 5 | Error Prevention | 1 | No required markers; ContractType toggle wired to non-existent id in Add; empty "Type project" card; free-text dd/MM/yyyy; slice total not enforced |
| 6 | Recognition Rather Than Recall | 2 | Nothing says which fields are required, ProjectCode format, or that slices cannot be set on Add |
| 7 | Flexibility and Efficiency | 2 | select2 type-ahead only accelerator; no shortcuts, no save-and-add-another, no jump-nav on 6-card form |
| 8 | Aesthetic and Minimalist Design | 2 | Heavy card-big-info chrome, filler descriptions, one empty card, ~100 lines dead JS |
| 9 | Error Recovery | 1 | ValidationSummary renders once at top of card 1; bad field in card 5 has no nearby marker |
| 10 | Help and Documentation | 2 | Some useful text-muted hints; nothing for the dense regie/schijven section |
| Total | | 19/40 | Poor |

## Design Specificity Verdict

LLM: Category-interchangeable. Porto admin-template forms almost untouched — card-big-info panels with oversized icon + filler description, right-aligned horizontal labels, generic "-- Selecteer --" prompts. No connection to the "Grounded & Cultivated" system (no Deep Forest Green beyond default btn-primary, no gl-* components, no gl-page-header). Least-designed surface in the Projecten area, and the most data-critical.

Deterministic scan: impeccable detect -> 1 finding, broken-image at Toevoegen.cshtml:307 (img id=stdFotoPreview src=""). Real: empty src issues a request to the page URL. Row is d-none until a file is picked; render without src. Detector otherwise quiet — does not catch the structural/wiring problems, which are the real story.

Visual overlays: none — app cannot run headless here, no browser tool exposed. Findings from full source reads of both files + controller (SetPageHeader at ProjectenController.cs:329 and :726) + _Layout.cshtml:103-115.

## Overall Impression

One form, maintained twice, drifting apart — plus a coordination feature fully built in Edit and left as dead scaffolding in Add. Biggest opportunity is not visual polish: collapse Add and Edit onto shared partials so a change lands once, and move validation next to the fields.

## What's Working

- Domain language is confident (werfmelding, regie, schijven, EPB-verslaggever, notaris) — correct for the audience.
- select2 async pickers for companies/postcodes/weather stations — type-ahead, allowClear, minimumInputLength 3. The one genuinely efficient interaction.
- Edit's inline hints ("Laat leeg om de huidige foto te behouden", SEO char guidance, current-photo thumbnail before upload) — exactly the reassurance a high-stakes edit screen needs. Add has none.

## Priority Issues

### [P1] Add and Edit are two hand-maintained copies of one form
Every field, select2 init, slice/rate template exists twice with subtle differences. configureSelect() (Edit) vs bindSelect()+inline (Add). Native type=date + yyyy-MM-dd (Edit) vs jQuery datepicker + free-text dd/MM/yyyy (Add). Quill rich-text for CommercialTextNL (Edit) vs plain textarea (Add) — same column, so HTML authored on Edit shows as raw tags on Add. Add is missing SEO, werfmelding, status, project type, land share, DeliveryDateDef, architect/engineer/security/EPB partners, verplichte-documenten checklist. Fix: extract shared partials (_ProjectFormGeneral/Planning/Partners/Coordination/Media) on a shared base VM; Add/Edit become thin compositions; one date control, one rich-text story. Command: /impeccable distill

### [P1] Add's coordinatieproject flow is broken and half-built
Toevoegen.cshtml:507-605 — ~100 lines of JS drive #coordinationFields/#slicesSection/#regieSection/#slicesContainer/#addSlice/#ratesContainer/#addRate/#slicesTotal — none exist in Add markup. "Type project" card (:317-337) is an empty shell. updateContractTypeVisibility() reads #ContractType but the select renders as #Project_ContractType (Edit fixed this; Add did not). id=IsCoordinationProject is on both the label (:65) and the checkbox (:68) — the checked test binds the label, always false. Net: ticking coordinatieproject on Add does nothing; slices/rates cannot be entered. Fix: ship shared _ProjectFormCoordination on both, delete dead JS, remove empty card, fix duplicate id. Command: /impeccable harden

### [P1] Duplicate page title — the form fights the app topbar header
Both controllers call SetPageHeader(...), rendered by _Layout as topbar icon + topbar-page-title. Both views then render their own h1 fw-bold + h5 + back-chevron (Toevoegen:13-29, Edit:24-40). Title shows twice. Exact pattern removed from Index and Detail this session; these two were missed. Topbar also exposes section PageActions (unused) and there is a _PageHeader partial with gl-page-header__back. Fix: delete the inline h1/h5/chevron block on both; use _PageHeader with BackUrl if an in-page back is wanted; keep Save/Cancel in the sticky bottom bar. Command: /impeccable layout

### [P1] Error recovery: one summary box at the top, fields five cards down
Neither file has a single ValidationMessageFor / asp-validation-for. Only feedback is ValidationSummary at top of card 1, rendered only if ModelState invalid. On a 6-card ~40-field form, a server error on TotalLandShare (card 4) or ContractType (coordination card) leaves the user hunting. No field marked required, so requirements are discovered by failing. Slices "must total 100%" but submit is not blocked — only a badge colour. Fix: inline asp-validation-for spans on every field; mark required (asterisk + aria-required); on submit-with-errors scroll to + focus first invalid field; enforce 100% slice rule client-side with a text message. Command: /impeccable clarify

### [P2] Template scaffolding smothers the form; no product identity
Six full-width card-big-info panels, each with a giant icon and a description restating the fields. Right-aligned labels in a 2-col grid, abandoned by Bootstrap years ago. Narrow col-lg-3 cells crowd date/dropdown inputs on laptop widths. Lots of scrolling, little density, nothing that reads as this company tool. Fix: lighter section headers (gl-* rule + label, no icon column), drop filler descriptions, labels above inputs, group the 8 verplichte-documenten checkboxes into a real 2-col checklist, bring in --primary / gl-page-header vocabulary. Command: /impeccable layout

## Persona Red Flags

Jordan (first-timer): no field marked required -> fills what looks important, submits, red box references fields scrolled past. Ticks "Zuiver coordinatieproject" on Add expecting options; nothing appears. "Type project" card blank. ProjectCode shows only "bv. P-2024-118" with no rule.

Sam (SR + keyboard): label class=control-label wrapping Html.LabelFor = label nested in label, ~15x, invalid; focus-on-click + announcement unreliable. id=IsCoordinationProject twice on Add. ios7-switch keyboard operability unverified. Slice-total validity colour-only. Error summary not focused/announced.

Riley (stress tester): empty card + dead "Schijf toevoegen" -> bug report. "31/12/25" in Add free-text date -> ambiguous parse. Slices 90% + submit -> accepted. Quill HTML from Edit never surfaces on Add plain textarea. Refresh mid-form -> all lost (no draft, no leave guard).

Ingrid (projectassistent, weekly): tab order zig-zags across the label/field grid. No "opslaan en nog een toevoegen". Re-selects same partner companies each time, no recents. Two mental models because Add and Edit do not match.

## Minor Observations

- fas fa-calendar-alt / fas fa-hourglass-half in Add (:193, :202, :213) — FA loaded globally so they render, but rest of these forms are Boxicons.
- Prompt copy drift for CoordinationIssuerCompanyId: "coordinatiebedrijf" (Add) vs "facturatiebedrijf" (Edit).
- script src quill.js inside section PageStyle in Edit (:17-18) — works, wrong section.
- Empty img src="" at Toevoegen.cshtml:307 (detector).
- No beforeunload unsaved-changes guard on either form.
- updateSlicesTotal() has no aria-live — total change silent to SR.
- Edit bundles an 8-checkbox "Verplichte documenten" list inside "Partners & documenten" — deserves its own section.

## Questions to Consider

- If Add and Edit were one set of partials, what would each screen legitimately need that is different beyond the submit label and whether an image exists?
- Does project creation need all six cards up front, or a "minimum viable project" (name, code, location, responsible) with the rest deferred to Edit?
- Should creating a coordination project be its own guided flow rather than a card mid-form?
- What would this form look like using the same gl-* vocabulary as the Detail page it feeds?
