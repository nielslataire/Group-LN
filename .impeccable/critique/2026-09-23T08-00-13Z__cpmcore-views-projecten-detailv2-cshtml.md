---
target: projecten/detail (incl. algemene gl-v2 layout)
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:E:\\TFS\\CPMCore\\Views\\Projecten\\DetailV2.cshtml"
target_fingerprint: "sha256:f89eea3c7815d5c48f63118dd78cbf871c94c3adac335a8db474f37ba0e6d663"
target_path: "E:\\TFS\\CPMCore\\Views\\Projecten\\DetailV2.cshtml"
timestamp: 2026-09-23T08-00-13Z
slug: cpmcore-views-projecten-detailv2-cshtml
closed: true
---
Method: dual-agent (A: general-purpose design-review sub-agent · B: general-purpose detector/evidence sub-agent)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Units-table search filter has no `aria-live` result count, unlike the attention-chip filter which does. |
| 2 | Match Between System and Real World | 4 | Domain terms exact, correctly ordered for a PM's mental model. |
| 3 | User Control and Freedom | 3 | "Vraagt actie" rows with no real destination still render as `<a href="#">` — a dead end disguised as a link. |
| 4 | Consistency and Standards | 3 | Reuses `.gl-v2-section-card`/`.gl-v2-badge` correctly, but the units table skips the mobile-card pattern the app already solved for Invoices. |
| 5 | Error Prevention | 3 | Read-heavy page; the only gap is the dead `href="#"` rows above. |
| 6 | Recognition Rather Than Recall | 2 | Tablet inner-menu (768–1399.98px) is 18 icon-only links labeled only via a hover/focus tooltip — no label mechanism on touch. |
| 7 | Flexibility and Efficiency of Use | 2 | No keyboard shortcuts; units table has search but no sort. |
| 8 | Aesthetic and Minimalist Design | 3 | Individually clean cards, but 8 full sections stack with no progressive disclosure. |
| 9 | Error Recovery | 3 | Empty states are handled well in plain Dutch throughout; no true error states to test further. |
| 10 | Help and Documentation | 1 | No contextual help anywhere (expected for an internal Operate tool, but literally none exists). |
| **Total** | | **27/40** | **Acceptable, bordering Good — the tablet recognition and mobile-table gaps are the ceiling.** |

## Design Specificity Verdict

**LLM assessment (Assessment A):** High. The field vocabulary (Weerstation, ABR-verzekering, Bankwaarborg, Nacalculatie, traject/mijlpalen) and the KPI set (Verkocht, Omzet, Fysieke/Financieel Voortgang, Werkdagen, Open Punten) are deeply Belgian-construction-specific — this could not be dropped into an unrelated product unchanged. The gl-v2 visual language (sage page, floating white cards, serif reserved for names) is correctly and precisely followed, not just claimed: card titles are confirmed sans, sections never go flush. One disclosed gap: the project-photo-as-thumbnail treatment is deferred, so the topbar/inner-menu still show a generic building icon per project — a real, if acknowledged, missed identity cue for a PM juggling many concurrent projects.

**Deterministic scan (Assessment B):** `impeccable detect --json` across `DetailV2.cshtml`, `_LayoutV2.cshtml`, `_RailPartialV2.cshtml`, and `_ProjectInnerMenuV2.cshtml` returned **zero findings**, exit code 0. This was verified as a genuine clean result, not a suppression artifact: no inline `impeccable-disable` comments in any of the four files, a `--no-config` re-run (bypassing DESIGN.md/config context and ignore rules) still returned `[]`, and all four files are substantial (15–42KB), ruling out an empty/misresolved target. This is consistent with Assessment A's "High" specificity verdict — the detector targets generic/template "AI slop" patterns, and none were found.

**Important caveat:** the detector's clean scan does **not** cover most of what Assessment A actually flagged below (dead links, touch-tooltip-only labels, missing mobile table treatment, `alt=""` on real content images, sub-11px muted text) — those are UX/accessibility judgments outside this detector's scope, not something a zero-findings result should be read as clearing.

**Visual overlays:** No browser automation tool is available in this session, so no live-server injection or user-visible overlay was produced. Both assessments worked from direct source inspection (Razor markup, CSS, JS) rather than a rendered screenshot.

## Overall Impression

This page is a strong, domain-authentic build that correctly follows its own gl-v2 design system down to specific rules (Floating-Card, Naming-Only-Serif). The gap isn't taste — it's that lessons the app already learned and shipped elsewhere in gl-v2 (the Invoices mobile-card pattern, the dashboard's snooze/urgency pattern) weren't reused here, and the one section built specifically to grab attention ("Vraagt actie") doesn't yet out-rank the neutral stat tiles around it or the dead-end links inside it. The biggest single opportunity: this is the project manager's main working screen, used on-site on tablets per PRODUCT.md's own stated requirement — and the tablet breakpoint is exactly where the design currently breaks (hover-only tooltips on a touch device).

## What's Working

1. **Disclosed, honest deviations from the legacy screen.** In-file commentary shows the team actively correcting semantic sloppiness (e.g. a "Getoond vs. Gefactureerd" labeling fix, a `GrossTotal`/`Balance` correction) rather than carrying it forward — real design discipline, not just a reskin.
2. **Genuine empty-state coverage.** Every card that could plausibly be empty (traject, units, invoices, documents) has a real, specific Dutch empty message, not a blank card.
3. **Precise adherence to gl-v2's own documented rules.** Every section is its own shadowed `.gl-v2-section-card` on the tinted content background, never flush; the serif face is correctly confined to names (card titles like "Traject" stay sans) — the system's own rules are followed exactly, not just gestured at.

## Priority Issues

**[P1] Tablet inner-menu is unreadable on first use — on the exact device class this app is built for.**
- **Why it matters**: `GlV2/_ProjectInnerMenuV2.cshtml`'s tablet mode (768–1399.98px) renders 18 items as bare icons, labeled only by a `:hover`/`:focus-visible` CSS tooltip. PRODUCT.md is explicit that this breakpoint range is used on-site on tablets, and touch has no hover state — a PM tapping an unfamiliar icon commits to navigating before seeing any label.
- **Fix**: Make first touch open the label (second tap/short-press to navigate), or drop to the phone-mode sheet pattern (which already shows visible text) at this breakpoint instead of the desktop hover pattern.
- **Suggested command**: `/impeccable clarify`

**[P1] Units table has no mobile treatment, though the app already solved this exact problem elsewhere.**
- **Why it matters**: The 6-column units table has zero responsive handling. DESIGN.md documents in detail that the same problem (a DataTable on a 375px phone) was tried twice with CSS reflow and failed both times for Invoices before a real card-markup rebuild fixed it — that lesson isn't reused here, so on phone the table will overflow or compress into unreadable slivers.
- **Fix**: Reuse the Invoices card-list pattern below 768px, or at minimum cut to 3 columns (Eenheid/Status/Prijs) with the rest folded into a secondary line, matching `.gl-v2-pd-units-sub`'s existing pattern.
- **Suggested command**: `/impeccable layout`

**[P2] Dead-end rows disguised as links, in the one section meant to drive action.**
- **Why it matters**: Every "Vraagt actie" item renders as `<a href="@(item.BekijkUrl ?? "#")">`. Four warning types never set `BekijkUrl`, so those rows carry a live link affordance (hover/click) that goes nowhere — silent failure, and in the section most likely to be opened mid-crisis.
- **Fix**: Render items with no `BekijkUrl` as a non-interactive row (no href, no hover state) instead of a dead `#` link.
- **Suggested command**: `/impeccable harden`

**[P2] The "Vraagt actie" card doesn't visually out-rank the six neutral stat tiles above it.**
- **Why it matters**: Its icon chip uses the same size/register as every other card icon on the page — nothing signals "this is the urgent one" until the eye reads its contents, undercutting the card's whole purpose.
- **Fix**: Give the card a distinct header/border treatment (warning-tinted top border or header background) when it actually contains items, not just the same muted icon-chip color reused elsewhere on the page.
- **Suggested command**: `/impeccable clarify`

**[P3] DESIGN.md's documented body font has drifted from what actually ships.**
- **Why it matters**: DESIGN.md specifies Avenir/Open Sans; the shipped code loads and uses IBM Plex Sans (a disclosed, commented substitution in the code, but never carried back into DESIGN.md). Cosmetically minor, but it means the doc can no longer be trusted at face value for this token — the next page built off it alone will load the wrong font.
- **Fix**: Update DESIGN.md's typography block to name IBM Plex Sans (or revert the code) so the two agree again.
- **Suggested command**: `/impeccable document`

## Persona Red Flags

**Alex (Power User)**: No keyboard shortcuts anywhere on the page. The units table offers only a single freetext filter across name+client, no click-to-sort on Status or Prijs. The "Vraagt actie" card has no bulk action or dismiss/snooze even though the app's own Projectleider dashboard already has a snooze pattern for the same kind of urgency list — Alex learns one interaction model on the dashboard and gets a strictly weaker one here.

**Sam (Accessibility-Dependent User)**: All photo/video thumbnails in the media grid render with `alt=""` — real project photos, not decorative chrome, so a screen-reader user gets zero information about what's pictured. Several stat/meta labels run 9–11.5px in a muted grey-green on light backgrounds, which combined with PRODUCT.md's own "contrast that holds up outdoors" requirement for tablet use on-site is worth an explicit contrast-ratio check before shipping, not just a code-review nod.

**Casey (Distracted Mobile User)**: Filter chips are roughly 24–26px effective height, under the 44×44pt touch-target minimum, with up to 5 in a row a PM might tap on-site. No `MobileQuickActions` section is defined on this page at all, even though the shell already has that pattern (used on Invoices and the dashboard) — on phone, Casey scrolls the full 8-card stack for any task with no shortcut bar.

## Minor Observations

- The units-table search filter has no `aria-live` result-count announcement, unlike the attention-chip filter on the same page, which does — an inconsistent accessibility treatment within one screen.
- The traject progress bar's "overdue" segment uses a very pale pink against green on a thin 6px bar — likely hard to distinguish for red/green color-vision deficiency; the numeric stat tile above it only partially compensates.
- The media-grid empty state still uses a dashed-border placeholder — worth confirming this is a deliberately disclosed "not built yet" per the system's own dashed-border convention, not a stray leftover.
- The KPI row's "OMZET" tile shows a raw currency value with no bar/track, breaking the pattern the eye just learned from the two adjacent voortgang tiles.

## Questions to Consider

- If "Vraagt actie" is genuinely why a PM opens this page mid-crisis, should it render *above* the KPI strip rather than below it?
- The tablet breakpoint's hover-only tooltips were carried over wholesale from the desktop pattern — was this range tested on an actual touchscreen tablet, or only in a resized desktop browser?
- Invoices already paid the cost of solving "table on a phone" the hard way — was reusing that pattern here simply out of scope for this pass, or an oversight?
