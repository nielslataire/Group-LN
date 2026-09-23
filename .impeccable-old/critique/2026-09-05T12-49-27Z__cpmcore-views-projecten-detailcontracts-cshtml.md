---
target: Projecten/DetailContracts + Projecten/AddContract
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Projecten\\DetailContracts.cshtml"
target_fingerprint: "sha256:90f84b0166ad5d48bd3d0057d9e1ffb589418280b6c5280e86ea9dcbf942ba61"
target_path: "C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Projecten\\DetailContracts.cshtml"
timestamp: 2026-09-05T12-49-27Z
slug: cpmcore-views-projecten-detailcontracts-cshtml
closed: true
---
Method: dual-agent (A: general-purpose design review · B: general-purpose detector/evidence)

# Critique — Projecten/DetailContracts + Projecten/AddContract

Scope: `CPMCore/Views/Projecten/DetailContracts.cshtml` (+ `Partials/Contracts.cshtml`) and `CPMCore/Views/Projecten/AddContract.cshtml` (+ `Partials/_ActivityRow.cshtml`), evaluated as one connected flow — a project's contract list and the form used to add/edit a contract on it.

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Good spinners/dirty-form warnings, but collapsed multi-contract rows show bare `-` with no label |
| 2 | Match System / Real World | 3 | Strong Belgian construction vocabulary, but page says "Leverancier," route/unit is "Contract" |
| 3 | User Control and Freedom | 3 | Cancel/confirm-gating present; destructive confirms are raw `window.confirm()`, stylistically disconnected |
| 4 | Consistency and Standards | 2 | Stock Bootstrap/Porto colors (`.ecommerce-status`, `.text-success/danger`, `btn-outline-success`) instead of DESIGN.md tokens |
| 5 | Error Prevention | 4 | Real strength — conditional fields, disabled-until-relevant, server file validation, focus-first-error |
| 6 | Recognition Rather Than Recall | 3 | Search-by-name/VAT and auto-fill reduce recall; long form has no section anchors |
| 7 | Flexibility and Efficiency | 3 | "Save & add another" and auto-opened search are good; no bulk actions, no persisted export-column prefs |
| 8 | Aesthetic and Minimalist Design | 2 | On-brand `card-big-info` layout, but 3 export affordances crammed in one group; dense ~20-field single page |
| 9 | Error Recovery | 3 | Specific VAT-lookup errors; empty `ValidationSummary` intro reads bare; file input doesn't survive failed POST |
| 10 | Help and Documentation | 1 | Zero inline help for Belgian-jargon toggles (VGM charter, PID attest, Werfmelding) |
| **Total** | | **27/40** | **Acceptable — solid domain plumbing undercut by inconsistent styling and a dense, unstepped form** |

## Design Specificity Verdict

**Partially authored for this product — genuinely specific in its domain plumbing, generic in its status/severity layer.**

**LLM assessment**: Strong specificity evidence — a VAT-lookup-driven "new supplier" modal, "vorig contract" auto-fill of BTW%/betaaltermijn/waarborg, an "Aannemerslijst" PDF export with Belgian-construction-specific columns (VGM charter, Werfmelding, PID attesten), and correct, intentional use of the documented `card-big-info` component. This could not be dropped into a generic CRUD admin unchanged. Against that: the status/severity language on screen is not the shipped design system — `.ecommerce-status.completed/.failed`, `.text-success/.text-danger`, and `btn-outline-success` are unmodified third-party-theme colors, not the Forest-Green/Rust/Ochre tokens DESIGN.md names explicitly ("The One Green Rule," "never stock Bootstrap red/amber/blue"). This is the seam where "generic CRUD admin" leaks through, and it lands exactly where severity should read as part of this system: signed/unsigned status, missing guarantees.

**Deterministic scan**: `impeccable detect --json` on both files returned a clean `[]` — zero mechanical findings, no crash. This is expected, not a contradiction: the detector's pattern-matching doesn't cross-reference actual rendered colors against a project's specific DESIGN.md token map, and can't judge information-hiding UX choices like collapsing a multi-contract row behind an unlabeled chevron. Both of the P1 issues below are semantic/consistency judgments a mechanical scan structurally cannot make on these particular files — exactly the gap dual-assessment critique exists to cover. No false positives to report since there were no findings.

**Visual overlays**: not applicable — browser visualization was correctly skipped. This target is a server-rendered ASP.NET Core MVC view requiring a running app with SQL Server, authentication, and real project/contract route data; no live server was reachable in this environment.

## Overall Impression

The domain modeling is genuinely good — VAT lookup, prior-contract defaults, and conditional guarantee fields show real product thinking, and error-prevention is a standout (score 4/4). But the screen doesn't consistently look like *this* product: it leans on unmodified theme colors for exactly the signals (signed/unsigned, missing guarantee) that matter most, and the add-contract form packs three distinct decisions — pick a supplier, set commercial terms, pick activities — onto one dense, unstepped page. The single biggest opportunity is closing the gap between the backend's real care (data survives failed validation, confirms gate destructive actions) and the frontend's visual/structural follow-through (on-brand color, chunked disclosure).

## What's Working

1. **Belgian-domain-specific supplier onboarding** — the company picker with a sticky "Nieuwe leverancier toevoegen" option, VAT-number lookup that autofills name/address, and "vorig contract" defaults encode real procurement workflow and measurably cut repeat data-entry.
2. **`card-big-info` used exactly as documented** — DESIGN.md names this component for "supplier/contract forms" and both cards implement it correctly (icon rail, title, description, form zone) — concrete evidence of the design system being followed, not improvised.
3. **Real engineering care around data safety** — activities survive a failed server-validation round-trip via re-hydration, a `beforeunload` warning guards unsaved edits, and destructive actions are confirm-gated.

## Priority Issues

**[P1] Off-brand semantic colors throughout both views**
Why it matters: DESIGN.md states this explicitly (One Green Rule; never stock Bootstrap red/amber/blue), and it's violated exactly where severity should read as part of this system — signed/unsigned status, missing guarantee.
Fix: recolor `.ecommerce-status` variants and `text-success`/`text-danger` usages in this flow through the Rust/Forest-Green tokens; swap `btn-outline-success` → `btn-outline-primary`.
Suggested command: `/impeccable colorize`

**[P1] Collapsed multi-contract rows hide nearly all data behind an unlabeled chevron**
Why it matters: a company with >1 contract shows `-` for BTW%, Betaaltermijn, Korting contant, Waarborg, Verstuurd, and follow-up icons, revealed only by an unlabeled `bx-chevron-right`. A PM scanning for unsigned contracts or missing guarantees will systematically miss exactly the companies with the most to track.
Fix: surface an aggregate summary in the collapsed row (e.g. "2 contracten, 1 niet getekend"); make the expand control a labeled, larger-target affordance.
Suggested command: `/impeccable clarify`

**[P2] AddContract is one long ungrouped page; activity picker has no search on an unbounded list**
Why it matters: 4 of 8 cognitive-load checklist items fail — "Algemene info" alone has 9 field-rows, and the activity checklist renders every remaining catalog activity as a flat, unfiltered checkbox list. This is exactly the on-site/tablet complexity PRODUCT.md flags as first-class, and it's where errors and abandonment concentrate.
Fix: split into a lightweight accordion/step grouping (general info → follow-up → activities); add a search box above the activity checklist.
Suggested command: `/impeccable layout`

**[P2] Guarantee-document upload fails only after a full-page POST, with no client pre-check or file persistence**
Why it matters: file-type/size is validated server-side only, and the file input can't retain its selection after a rejected postback — a disproportionate recovery cost for a format mistake, especially attaching a photographed bank guarantee from a tablet on-site.
Fix: add client-side type/size validation with an inline message before submit.
Suggested command: `/impeccable harden`

**[P3] No inline help for Belgian-jargon toggles**
Why it matters: VGM charter, PID attest, and Werfmelding ship with no tooltip/glossary — fine for an experienced PM, a real gap for anyone newer to the domain.
Fix: add a small `?` affordance or `title`/`aria-describedby` micro-copy on each opvolging toggle.
Suggested command: `/impeccable document`

## Persona Red Flags

**Sam (Accessibility)**: Table action icons (`bx-edit`, `bx-trash`, `bx-eye`, `bx-plus`) carry only a `title` attribute — no `aria-label`, no visible text — while the follow-up icon cluster in the *same view* correctly uses `aria-label` + `<title>` on its SVGs. Screen-reader users get a materially worse experience on the row's primary actions than its status icons, on the same page. The `.ecommerce-status` dot conveying signed/unsigned relies on color + hover-only `title`, while the guarantee-missing icon two columns over does the accessible thing — two different accessibility standards for the same category of signal, one screen.

**Riley (Stress Tester)**: Switching company after activities are entered triggers a raw `window.confirm()`, jarring against the rest of the styled UI, with no visual cue distinguishing "OK discards data" from routine confirmation. Deleting an activity row uses a generic confirm that doesn't name which activity is being removed — in a fast cleanup pass, Riley can't be certain what they just deleted from the dialog alone.

**Casey (Mobile/Tablet — explicit persona per PRODUCT.md)**: Paired field rows only stack to single-column below 768px; on a ~1024px landscape tablet, VAT%/payment term and cash-discount/percentage still render cramped side-by-side, at the width DESIGN.md's own Field-Width Rule commits to keeping legible and touch-usable. The activity checklist uses plain native checkboxes with default tap areas, well under the ≥40px touch target DESIGN.md mandates for on-site use. Table action icons in DetailContracts are packed tightly with no button boundary — a real mis-tap risk on-site, where hitting "delete" instead of "edit" has real consequences on a system of record.

## Minor Observations

- The info-alert copy "Gebruik Enter om snel toe te voegen" doesn't correspond to any visible Enter-to-add behavior — reads as stale/borrowed copy.
- Page H1 says "Leveranciers," the CTA says "+ Leverancier toevoegen," but the underlying unit and route are "Contract" (`AddContract`) — see heuristic 2.
- `DetailContracts.cshtml` runs two overlapping export mechanisms (custom buttons that programmatically click DataTables' own hidden buttons, plus a separate `colvis` button) — functional but redundant plumbing.
- `Html.ValidationSummary(false, "", ...)` passes an empty message string, so server errors render as a bare bullet list with no introductory sentence.
- The empty state ("Nog geen leveranciers toegevoegd") uses `btn-default` for its CTA, while the same action elsewhere on the page uses `btn-primary` — inconsistent button semantics for an identical action.

## Questions to Consider

1. If a company can have multiple contracts, why does the list default to hiding almost every meaningful column the moment there's more than one — shouldn't the multi-contract case get *more* visibility, not less?
2. DESIGN.md explicitly bans stock Bootstrap red/green/amber in favor of Rust/Ochre/Forest-Green — was that rule ever meant to reach third-party-theme leftovers like `.ecommerce-status` and `.text-danger`, or does "brand color" only mean the buttons and nav that got explicitly restyled?
3. Given PMs use this on-site on tablets, has anyone tried filling in this ~20-field contract form and attaching a photographed bank guarantee standing on a construction site — where a rejected upload means re-scrolling the whole form and re-selecting the file?
4. Is "Contract toevoegen" really one screen's worth of work, or is it three tasks wearing one page — pick a supplier, set the commercial terms, pick activities — that got laid out side-by-side because nobody decided which comes first?
