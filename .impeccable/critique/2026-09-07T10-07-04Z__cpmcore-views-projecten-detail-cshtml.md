---
target: projecten/detail
total_score: 23
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 3
target_identity: "file:e:\\TFS\\CPMCore\\Views\\Projecten\\Detail.cshtml"
target_fingerprint: "sha256:345e5ef9834dd094664e79a4c68c2a3e7aad47294a88b8115e969dd3db15b047"
target_path: "e:\\TFS\\CPMCore\\Views\\Projecten\\Detail.cshtml"
timestamp: 2026-09-07T10-07-04Z
slug: cpmcore-views-projecten-detail-cshtml
---
# Critique — CPMCore/Views/Projecten/Detail.cshtml (Projecten / Detail)

Method: dual-agent (A: design review · B: detector + static evidence) · Mode: Operate

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | Standaardfoto upload is `fetch → location.reload()` with no progress or confirmation; filter-chip changes aren't announced; no "last updated" on any figure |
| 2 | Match System / Real World | 3 | Strong domain vocabulary, but a sold unit renders in the same alarm Rust (`bg-danger`) as a missing bank guarantee |
| 3 | User Control & Freedom | 3 | Modals cancel, "Alles" chip resets — but attention-panel filter state is lost on refresh (not in URL); photo delete has a confirm, no undo |
| 4 | Consistency & Standards | 2 | Reinvents the signature `gl-pg-bar` as `gl-vb-*`; ships `#3b7ddd` / `#0a3d29`; saturated 1px border on every KPI tile vs the hairline standard; 7 KPI tiles vs the documented 4; BS4 `data-toggle` on a BS5 theme |
| 5 | Error Prevention | 3 | Confirm modal on photo delete; but the photo input has no visible size/type guard and no leave-warning |
| 6 | Recognition vs Recall | 3 | Cards and icons help, but 768–1299px hides every KPI label — icon-recall on the primary on-site device |
| 7 | Flexibility & Efficiency | 2 | The 7 KPI tiles are inert (no href); most sub-areas reachable only via the collapsed inner "Toon Menu"; "recent" lists silently `.Take(5)` with no "5 of 30" |
| 8 | Aesthetic & Minimalist | 2 | 7-tile strip + 4-card row + hero split + inner menu at near-equal weight; 4 bar colours in one card; bordered tiles — loud against a "quiet, one-green, grounded" brief |
| 9 | Error Recovery | 2 | "Upload mislukt" / "Verwijderen mislukt" give no reason and no next step; PNotify error styling is stock red, off-system |
| 10 | Help & Documentation | 1 | Nothing explains "Fysieke vs Financiële voortgang", what counts as an "open punt", or how severe an "Overfacturatie" is; `title` attrs are decorative only |
| Total | | 23/40 | Acceptable |

Applicable maximum: 40 (all ten heuristics scored; none n/a).

## Design Specificity Verdict

Rating: mostly-authored. The content is unmistakably this product; the composition is a stock admin hub.

LLM assessment (A): The "Aandacht vereist" panel (unsigned contracts with "24 dagen open", missing bankwaarborg, over/onderfacturatie with real percentages, overdue werfpunten, insurance gaps — urgency-ranked with deep links), the unit table with finishing-option pricing and "Akte verleden" status, and the budget card's nacalculatie vocabulary (Gecontracteerd, contractuele volwassenheid, besteed) are genuine Belgian-construction-domain modelling, with real field-use touches (40px photo overlay "sized for gloves/outdoors", 16:9-locked photo so card height doesn't jump). But the skeleton — a 7-tile KPI band, a 2-column hero, then a 4-card row of "recent X + link to full X" — is the canonical ops-dashboard detail page, and the newest execution drifts off the documented system.

Deterministic scan (B): `impeccable detect` 3.6.0 → 0 findings, exit 0 (clean), confirmed with `--no-config`. Caveat: the engine runs `.cshtml` through regex matching, not DOM analysis, so exit 0 means "no anti-pattern string matched," not "structure and a11y pass." The static cross-check independently confirmed the colour drift at exact lines: `#3b7ddd` at projecten-custom.css:1238 (`.gl-vb-fys`) and :1295 (`.gl-doc-word`), `#0a3d29` at :1241 (`.gl-vb-budget`), and a bare `#fff` at :1291 where every sibling uses `var(--white, #fff)`. The 6 inline `style=` attributes are all CSS-custom-property pass-throughs (benign); 3 residual Font Awesome icons remain (lines 254, 260, 823 — all pre-existing); no `target="_blank"` is missing `rel="noopener"`. No false positives.

Visual overlays: none — no browser automation is exposed and the ASP.NET app cannot be built or served in this environment.

## Overall Impression

The page earns its keep on domain substance — the triage panel and the sales/nacalculatie vocabulary are things only this product would build. But it asks the eye to hold too much at once (seven coloured chips, a split hero, four cards, a collapsed menu, all near-equal weight), and the cards just added pull away from the "one quiet green" identity with raw blue, a second dark green, and four bar colours in a single control cluster. Biggest opportunity: decide what the hub is for. If it's "see what needs attention, then jump into a sub-area," then the seven most prominent things on the page (the KPI numbers) should be navigable, the strip should shed to what matters on a tablet, and every progress bar should come back onto `gl-pg-bar` and the green/semantic palette.

## What's Working

1. The "Aandacht vereist" panel is the most product-specific thing on the page, and it serves the Operate task directly. Five unrelated data sources fused into one urgency-ranked list where each row carries contextual meta ("24 dagen open") and a deep link. Authored triage, not an activity feed.
2. The unit table is real sales-domain modelling. Inline search over eenheid + klant with a sticky header and a "geen resultaat" row, plus finishing-option pricing ("vanaf € X" with per-option breakdown and a deliberate exclude-from-base rule) — it reflects how these units are actually sold.
3. Disciplined empty states. Every list has a specific, calm Dutch message, and the attention-panel "all clear" (green check + "Niets dat aandacht vraagt") is a designed moment, not a blank card.

## Priority Issues

[P1] Off-system colour in the "Voortgang & budget" card (and the doc badges)
- Why it matters: DESIGN.md names `#3b7ddd` and `#0a3d29` as exactly the kind of hexes that break the One Green Rule — and this one card ships both, runs four saturated bar colours in a single cluster (blue / Ochre / green / dark-green), uses the `warning` token as a plain category colour for "Financiële voortgang", and abandons the documented `gl-pg-bar` gradient. The same blue recurs on the Word-doc badge. It is the loudest system violation on the page, and both assessments flagged it independently (A ranked it #1; B pinned the lines).
- Fix: Use the real `gl-pg-bar` component (`#d1d5db → #6b8f80 → #0a5a3b`) for all four bars and drop per-metric colour. If "Budget besteed" over 100% must stand out, switch that one bar to an Ochre/Rust tint as a semantic state. Delete `.gl-vb-fys` / `.gl-vb-budget` custom hex; recolour `.gl-doc-word` to Taupe or Sage; token the bare `#fff` at :1291.
- Suggested command: /impeccable colorize — realign the Voortgang & budget bars and document badges to the One Green Rule + semantic-status palette.

[P1] The KPI strip loses every label 768–1299px — the primary on-site device
- Why it matters: `@media (max-width:1299.98px){ .gl-kpi2-label, .gl-kpi2-sub { display:none } }`. On a site tablet the 7 tiles collapse to an icon + a number; `title` is the only remaining label and it doesn't surface on touch or to most AT. The Field-Width Rule and "on-site tablet use is first-class" are both broken — and the tiles aren't links, so you can't act on them either.
- Fix: Keep the label at all widths (shrink the value or wrap the tile to two lines). Below the breakpoint, drop to the 3–4 tiles that matter on-site (Fysiek, Werkdagen, Open punten, Verkocht) instead of 7 mute discs. Make each tile an `<a>` to its sub-page with a real accessible name.
- Suggested command: /impeccable adapt — make the KPI strip legible and navigable at tablet and phone width.

[P1] Urgency in the "Aandacht vereist" list is colour-only
- Why it matters: `.gl-detail-mc-card .gl-mc-group-hdr { display:none }` removes the "ACTIE VEREIST / OP TE LOSSEN" text and the per-group count. The only surviving urgency signal is `--danger-tint` vs `--warning-tint` — two pale warm tints about one hue apart, unreadable for colourblind users and in outdoor light (WCAG 1.4.1). The category filter chips are not an urgency signal.
- Fix: Keep a compact labelled urgency marker regardless of the chips — a slim "Actie vereist · 3" divider row per group, or an urgency pill on each `gl-mc-item`.
- Suggested command: /impeccable clarify — restore a non-colour urgency signal and the group labels/counts.

[P2] The hub can't actually navigate to most sub-areas
- Why it matters: This is an Operate "jump to a sub-area" surface. Only 5 sub-areas have deep links (from card headers). Klanten, Leveranciers, Punten, Verzekeringen, Betalingen, Wijzigingsopdrachten and Budgetten live only in the `DetailMenu` inner menu, collapsed behind "Toon Menu" on touch. The 7 most prominent elements on the page — the KPI tiles — have no `href`, so "Open punten: 12" can't be clicked through.
- Fix: Make every KPI tile link to its sub-page. Add a visible sub-area nav (chip row or tile grid) to the hub body instead of relying on the collapsed inner menu.
- Suggested command: /impeccable shape — rework the hub's navigation model so the numbers are the routes.

[P2] Sub-40px touch targets across a field-first page
- Why it matters: `gl-mc-filter-chip` is `min-height:32px`; "Bewerken" (`4px 8px`), "Open" (bare `.78rem` text), "Bekijk" (12px) and the "Alle eenheden / documenten / foto's" links are all well under the 40px Field-Width Rule minimum, several sharing an edge. The `gl-mc-body-capped` white fade also sits over the last row's "Bekijk" link, shrinking its tap area.
- Fix: Raise chips and every card action link to a ≥40px hit area; add spacing between adjacent links; lift the fade off the last actionable row.
- Suggested command: /impeccable adapt — enforce 40px touch targets on the project-hub controls.

## Persona Red Flags

Alex (power user): the 7 KPI tiles are display-only — can't click "Open punten 12" to drill in. RecentInvoices / LatestDocs / LatestPictures are silently `.Take(5)` with no "5 of 30". Attention-panel filter state isn't in the URL, so a refresh loses it. `syncHeroColumnHeight` sizes the right column (attention + units) to the left column's rendered height — a project with 20 attention items and 200 units gets a cramped internal scroll dictated by how many roles are filled in "Algemene gegevens".

Sam (accessibility): urgency in the attention list is tint-only; KPI tiles expose meaning only through `title`; `.gl-kpi2-label` (~10px, uppercase) and `.gl-kpi2-sub` (~10.5px) are below comfortable reading size; the unit "Verkocht" badge is `bg-danger` (semantically wrong and alarm-weighted), while "Beschikbaar" (`bg-success`) and "Akte verleden" (`bg-primary`) are two greens competing with the brand green; thumbnail `alt` is the raw filename; filter chips toggle `aria-pressed` but mutate the list via `style.display` with no `aria-live`.

Casey (distracted, one-handed, on-site): on a phone the project name appears nowhere in the page body (topbar title hidden <768px, no `<h1>`) — no orientation. On a tablet the KPI strip is 7 unlabelled discs. Filter chips (32px) and every card link are under 40px. Photo upload gives no feedback. Reaching most sub-areas needs a one-handed open of the collapsed "Toon Menu". The hero right column is a JS-height-computed nested scroll region inside the page scroll.

## Minor Observations

- Off-palette hex all sit in one file: `#3b7ddd` (x2), `#0a3d29`, plus pre-existing `#01532d` — a token pass is overdue.
- Two money formats on the same strip: `FormatMio` → "€3,41 mio" while everything else is `ToString("C0")` → "€ 3.410.000".
- "Werkdagen" tile renders a bare negative number when overdue; the attention panel says "X dagen te laat" — inconsistent.
- The CSS still defines a `gl-detail-media-more` "+N" tile the view never renders (`TotalPictureCount` unused).
- Every `gl-kpi2-tile` carries a saturated 1px border (`var(--kpi-color)`) — off the hairline standard, reads busy.
- `data-toggle` / `data-original-title` (Bootstrap 4) on the "Alle eenheden" link on a Bootstrap 5 theme — tooltip likely dead; should be `data-bs-*`.
- The unit table has no pagination — a 200-unit project = 200 rows inside the hero card's internal scroll.
- 3 residual Font Awesome icons (lines 254, 260, 823) while the rest of the view is now Boxicons — finish the migration.
- Photo upload: `input change → fetch → location.reload()` with no progress; only failure toasts.
- Dead CSS: empty `.gl-vb-item + .gl-vb-item {}` and `.gl-dropdown-menu-start {}` rules.
- Thumbnail `alt="@pic.Name"` is the raw filename (noise for AT).

## Questions to Consider

1. The KPI strip is the first thing the eye lands on and the page's job is to route the user into a sub-area — so why is not one of those seven numbers a link, and if each tile had to earn its place as a navigation target, which three would you keep?
2. A sold unit shows in the same colour as a missing bank guarantee. Is your colour system communicating status or severity — and can it honestly do both on the same screen?
3. The attention-and-units column's height is set by JavaScript measuring the static "Algemene gegevens" column beside it. Which of those two columns does the on-site PM actually open this page for — and why does the other one win the layout?
