---
target: projects/dossiers/index
total_score: 28
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:E:\\TFS\\CPMCore\\Views\\ProjectDossiers\\Index.cshtml"
target_fingerprint: "sha256:f0c566af83ae4aa9cf90eaa98d58d56c033b5cdd51730a4eb6f171c5bd3ba325"
target_path: "E:\\TFS\\CPMCore\\Views\\ProjectDossiers\\Index.cshtml"
timestamp: 2026-09-18T12-29-12Z
slug: cpmcore-views-projectdossiers-index-cshtml
---
## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3/4 | No busy/disabled state on any of the 3 modal submit buttons; no live count before bulk-create |
| 2 | Match System / Real World | 3/4 | Tab says "Nutsaanvragen" while every other surface (button, dossier-kind name, both modal titles, section headers) says "Nutsaansluiting" |
| 3 | User Control and Freedom | 3/4 | Cancel/back present everywhere; no undo after a bulk create, only a post-hoc skip report |
| 4 | Consistency and Standards | 3/4 | `gl-mp-modal-section-title` (documented as modal-only) reused as a bare page `<h2>`; accessible-icon pattern applied inconsistently between algemeen list, desktop matrix, and mobile matrix for the same data |
| 5 | Error Prevention | 3/4 | Matrix hides "+" once a cell is filled (good); no pre-submit summary before an irreversible bulk create |
| 6 | Recognition Rather Than Recall | 3/4 | Reuses the app's status icon/color language well; `Aangevraagd` and `InBehandeling` render as the same icon+color, so two statuses are visually indistinguishable |
| 7 | Flexibility and Efficiency | 2/4 | Matrix and "Bulk per eenheid" are disconnected — the matrix already shows exactly which cells are empty, but the bulk modal makes the user re-derive that from a blank checkbox list |
| 8 | Aesthetic and Minimalist Design | 3/4 | Restrained and on-system; the 11-field "Werkstroom" section is the one place density creeps past minimal |
| 9 | Error Recovery | 2/4 | All 3 modals use plain `name="..."` inputs, no `asp-validation-for` — no visible path for a server-side validation failure to land on a specific field |
| 10 | Help and Documentation | 3/4 | No formal help system, but genuinely useful inline guidance exists (bulk modal explains its own skip-logic; nuts modal points to where keuring/overdracht continues) |
| **Total** | | **28/40** | **Good** |

## Design Specificity Verdict

**LLM assessment:** Grounded, not generic. The page encodes real, load-bearing domain knowledge: a two-tier utility model (building-level vs. per-unit connections), a unit × utility-type matrix, Belgian metering identifiers (EAN, meternummer) with unit-keyed prefill, a Netbeheerder company picker, named Belgian telecom operators with a documented legacy-enum migration note, and a bulk-create skip-rule ("unit already has a non-cancelled dossier of this type -> skip") that encodes an actual operational rule of this workflow. This could not be dropped into an unrelated CRUD app unchanged.

**Deterministic scan:** Clean — `impeccable detect --json` returned exit code 0 and `[]` across all 5 scanned markup files (Index.cshtml + `_DossierActionButtons.cshtml` + the 3 modals). No rule-based violations (color tokens, hardcoded values, banned patterns) were found. Worth naming plainly: the detector's clean pass and Assessment A's P1 accessible-name finding are not in tension — a missing `visually-hidden` label inside a link that already has *some* text content is a semantic/context judgment, not a pattern the static detector currently flags. Clean detector output here means "no rule violations," not "no accessibility issues."

**Visual overlays:** Not available this run — no dev server was started (the app requires a live database connection, which this session avoids for safety), so there is nothing to see in a [Human] tab. This is a reported fallback signal, not a skipped step: browser inspection genuinely couldn't run.

## Overall Impression

This is a well-built, domain-faithful page let down by a handful of specific, fixable gaps rather than any structural problem. The strongest thing about it is how convincingly it reuses the app's own visual language (status icons, `gl-doc-item` list rows, the matrix component) — a user of `ProjectTraject` would feel instantly at home here. The biggest opportunity is closing the loop between the matrix (which already shows exactly what's missing) and the bulk-create flow (which throws that context away and makes the user reconstruct it).

## What's Working

1. **The bulk-create result toast** turns a "did that actually create 24 records correctly?" moment into a legible receipt — it names exactly which units were skipped and why, not just a bare count. Best reassurance-design detail on the page.
2. **Cross-feature reuse of the status-icon system.** The dossier matrix, the algemene-aansluitingen list, and the mobile fallback all share the same five-token `gl-mijlpaal-status-icon` language already established on `ProjectTraject` — zero new vocabulary for a returning user.
3. **The mobile matrix-to-card transposition** correctly follows the documented "matrix -> pick-a-row card" pattern instead of a naive table dump, and happens to be more screen-reader-legible than its own desktop counterpart because it prints the utility-type name as visible text.

## Priority Issues

**[P1] Per-unit matrix cell links have no usable accessible name for existing dossiers**
- **Why it matters:** Filled matrix cells render as an `<a>` whose only content is an `aria-hidden` icon and a bare date/dash span. The `title` attribute isn't used for accessible-name computation once there's visible text, so a screen-reader user hears a bare "14/05/24, link" repeated for every filled cell — on a 50-unit project, an indistinguishable wall of links. This directly contradicts your own documented rule for this icon component, and the mobile version of the same data already does it right.
- **Fix:** Add a `visually-hidden` span inside the link carrying "`{unit} — {type}: {status}`" — the same string already computed for `title`.
- **Suggested command:** `/impeccable harden`

**[P1] The tab-row stats strip and the Nutsaanvragen tab badge show different, unlabelled scopes**
- **Why it matters:** The stats strip counts open dossiers across *every* `DossierKind` (permits, sales, insurance, etc.), but it sits directly beside the Nutsaanvragen tab, which carries its own narrower count badge. On the default tab, "X totaal · Y nog open" reads as if it's about utility requests — it's actually project-wide. A PM using this number to judge utility-connection health is being misled by adjacent, differently-scoped counts with no label distinguishing them.
- **Fix:** Either scope the stats strip to the active tab's data, or label it explicitly ("project-breed") so scope doesn't have to be inferred.
- **Suggested command:** `/impeccable clarify`

**[P2] Werkstroom date fields are out of chronological order**
- **Why it matters:** The busiest modal's densest section places "Afgehandeld op" (the terminal state) *before* "Aanvraagdatum" (the first step), undermining the one thing that section is organized around: a timeline.
- **Fix:** Reorder to Aanvraagdatum -> Verwachte offertedatum -> Offertedatum -> Datum goedkeuring -> Datum uitvoering gevraagd -> Datum uitgevoerd -> Afgehandeld op.
- **Suggested command:** `/impeccable layout`

**[P2] No busy-state or pre-commit count on any of the three modal submit buttons**
- **Why it matters:** All three submits are plain buttons with no disable/spinner wiring. Riskiest on the bulk modal: nothing tells the user how many dossiers "Aanmaken" is about to create before they click, and nothing stops a double-click or slow connection from producing a confusing double round-trip. The good itemized result toast only fires after the fact.
- **Fix:** Disable+spin the submit button on click (worth adding once, reusably, to `custom.js`), and add a live "Dit maakt dossiers aan voor N eenheden" line above the bulk modal's footer, driven off the checked boxes.
- **Suggested command:** `/impeccable harden`

**[P3] "Alle dossiers" kind-filter offers 10 same-weight choices in one row**
- **Why it matters:** "Alle" plus all 9 `DossierKind` values render as flat pills with no grouping — past a single scannable decision point, and the only navigation aid on that tab besides free-text search.
- **Fix:** Fold into a labelled `<select>` or colvis-style dropdown, reserving pills for a curated subset (2-3 most common kinds) plus an "overig" catch-all.
- **Suggested command:** `/impeccable distill`

## Persona Red Flags

**Alex (impatient power user, bulk actions)**
- Clicking "+" on one empty matrix cell opens the entire 22-field/3-section nuts modal — Alex wanted to log "gas requested for Unit 12" in two fields.
- The matrix already shows Alex exactly which unit×type cells are empty; "Bulk per eenheid" throws that away and starts from a blank checkbox list — he has to re-derive "who's missing gas" by eye a second time.
- The bulk modal's "Alles" button checks every unit with no indication of which already have this type — Alex must trust the server's silent skip-logic rather than seeing it.
- The 10-pill kind filter has no type-ahead or shortcut, forcing mouse-hunting through nine buttons.

**Sam (keyboard/screen-reader, needs real accessible names & 4.5:1 contrast)**
- The desktop matrix's filled-cell links announce only a date or "—" — for Sam, the primary visual feature of the tab is functionally unusable via screen reader.
- The "Algemene aansluitingen" row's status icon carries its status only via a `title` attribute on a non-focusable `<span>` — not reliably exposed in the accessible-name chain, so status there is effectively sighted-only.
- The mobile fallback is accidentally more accessible than desktop (prints the type name as text) — Sam's experience of this feature literally changes with viewport width, not by design.
- None of the three modals use `asp-validation-for`, so a server-side validation failure has no field-level error for her AT to announce.

## Minor Observations

- `.gl-dossier-kind-nav .nav-link` hardcodes `#f1f3f7`/`#4a5566`/`#0a5a3b` instead of the `var(--border, …)`/`var(--tsa-muted-aa, …)` token pattern used everywhere else in the same file.
- "Alle dossiers" tab has no count badge while the narrower Nutsaanvragen tab does — not wrong, but an inconsistent signal about which tab's number is worth glancing at.
- `NutsIcon`'s `ph-plug` fallback is only reachable for `NutsType.Overig` — worth a one-line comment noting that, the way the Proximus/Telenet/Telecom sharing already is.
- Controller actions (`Opslaan`, `NutsOpslaan`, `NutsBulkAanmaken`) don't visibly check `ModelState.IsValid` before hitting the service layer — combined with the missing `asp-validation-for` wiring, a bad payload's failure mode isn't traceable from the view code alone.

## Questions to Consider

1. Should the matrix's "+" really open the full utility-dossier form, or would a two-field "quick log" (rest added later from Details) match how a PM actually fills a cell?
2. The matrix already renders exactly which unit×type cells are empty — why does "Bulk per eenheid" make the user reconstruct that from a blank checkbox list instead of selecting the empty cells directly?
3. Now that `Aangevraagd` and `InBehandeling` render identically everywhere on this page, is that distinction still earning its keep as two separate statuses?
