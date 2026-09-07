---
target: projecten/detail
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 3
target_identity: "file:C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Projecten\\Detail.cshtml"
target_fingerprint: "sha256:ead6d4691d303773861fc8a6ce9569adc0758e5c09f3c0e488caea9388976135"
target_path: "C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Projecten\\Detail.cshtml"
timestamp: 2026-09-05T18-02-05Z
slug: cpmcore-views-projecten-detail-cshtml
---
Method: dual-agent (A: design-review sub-agent · B: detector/browser-evidence sub-agent)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Overdue delivery date is rendered in `text-muted` instead of a danger color — the one moment status visibility inverts itself |
| 2 | Match System / Real World | 4 | Correct Dutch construction/sales vocabulary and nl-BE currency formatting throughout |
| 3 | User Control and Freedom | 3 | Good escape hatches ("Alle eenheden", "Toon Alle", filter reset) undercut by native `confirm()`/`alert()` on photo actions |
| 4 | Consistency and Standards | 2 | Unit-status badges bypass the brand token system entirely; two incompatible destructive-action UX patterns coexist on one page |
| 5 | Error Prevention | 3 | Project delete has a proper confirmation modal; photo delete/upload rely on weaker browser-native dialogs |
| 6 | Recognition Rather Than Recall | 3 | Good label/value pairing, undercut by the KPI strip and Voortgang card silently duplicating the same four numbers |
| 7 | Flexibility and Efficiency | 3 | Category filter chips and deep links serve power users well; no keyboard shortcuts (consistent with rest of app) |
| 8 | Aesthetic and Minimalist Design | 2 | KPI strip (7 tiles) + attention panel + 6-col table + 7-card grid on one screen, well past the app's own 4-tile KPI convention |
| 9 | Error Recovery | 2 | Upload failure surfaces via a blocking native `alert()` with a raw error string, not the app's own toast/inline pattern |
| 10 | Help and Documentation | 2 | No contextual explanation of "Aandacht vereist" categorization or the filter chips — bare but acceptable for an internal tool |
| **Total** | | **27/40** | **Acceptable** |

## Design Specificity Verdict

**LLM assessment:** Not a generic admin-table page in its bones — it reuses the `gl-mc-*` meldingencentrum vocabulary, the `gl-pg-bar` progress gradient, Rust/Ochre/Taupe severity tints, and real Dutch domain vocabulary (loten, akte, bankwaarborg, werkdagen) a template could never fake. The read-order (KPI strip → attention panel → core inventory table → detail cards) deliberately echoes the Home/Projectleider dashboard grammar from DESIGN.md.

But execution is split-personality: the insurance-status card is correctly token-driven, while the page's own centerpiece — the "Eenheden & verkoopstatus" table — renders its status badges through raw, off-brand Bootstrap defaults (see P0 below). Someone skimming the top of the page would call this authored for CPM; someone who reads the table the page is named after finds the brand identity breaks down exactly where it matters most.

**Deterministic scan:** `impeccable detect` ran clean (exit 0, zero findings) across `Detail.cshtml`, `DetailMenu.cshtml`, and `projecten-custom.css`. Notably, the detector did **not** catch the P0 badge-class bug below — a real, verified brand-token violation that the mechanical scanner's rule set doesn't check for (missing `bg-` prefix on a Bootstrap badge class). This is a case of the LLM review catching what the detector missed, not a detector false positive.

**Visual overlays:** not available — no browser automation tool was exposed in this environment, so there is no live-page overlay to point to. All findings below come from a structural read of the Razor markup and CSS, cross-referenced against DESIGN.md's documented tokens.

## Overall Impression

The page's architecture is right — it correctly reuses the app's own meldingencentrum/progress-bar/severity-token vocabulary instead of reinventing one, which is the hard part to get right. What's missing is finishing: one real brand-color bug on the page's most-used table, a few pre-existing UX rough edges (muted overdue-date, alert()-based errors) that a redesign pass was a natural place to catch, and a density level that's grown past what the app's own KPI-strip convention (4 tiles) and cognitive-load budget can comfortably hold (7 KPI tiles, 7 cards, duplicated numbers). The single biggest opportunity: fix the badge-class bug (it's a one-line change) and then decide whether this hub wants to be dense-and-complete or trimmed to what a PM actually needs at a glance — right now it's reaching for both.

## What's Working

1. **The attention-panel reuse is a real architectural win.** Pulling unsigned contracts, missing bank-guarantees, progress warnings, insurance warnings, and open issues into the same `gl-mc-urgent`/`gl-mc-normal` grouping used on the Home/Projectleider dashboard means a PM doesn't have to learn a second "what needs me" pattern for project-scoped vs. portfolio-scoped attention — directly serving PRODUCT.md's "one linked system" principle.
2. **Insurance status coloring is correctly token-driven.** The `gl-detail-kpi-value-primary`/`-warning`/`gl-text-danger` mapping off `Enddate` thresholds is exactly what DESIGN.md's Semantic Status system asks for (tint+text pairing, no stock Bootstrap hues) — it's the one status-color implementation on the page that does what the broken unit-status badges should have done.
3. **Empty-state handling is genuinely careful.** Every KPI tile and every card has a defined, on-brand empty state ("Nog geen eenheden…", "Nog geen facturen…") rather than blank space — consistent with DESIGN.md's rule to always show an empty state rather than an empty card body.

## Priority Issues

**[P0] Unit-status badges bypass the entire semantic-status token system**
*Verified against source.* `Detail.cshtml:265`: `<span class="badge badge-@u.StatusVariant">` — missing the `bg-` prefix. `custom.css`'s brand overrides only target compound selectors `.badge.bg-primary/.bg-danger/.bg-warning/.bg-success` (lines 558–599); this markup instead produces `badge-primary`/`badge-danger`/etc., which never match those selectors and fall through to `theme.css`'s stock Bootstrap colors (grey `#CCC` for "Akte verleden" — the *most positive* status; stock red `#d2322d` instead of Rust; stock amber instead of Ochre). `custom.css` even has a comment at line 461 noting this exact failure mode was already fixed elsewhere in the app ("badge.bg-danger/-warning vielen stil terug op Bootstrap-standaard") — it just wasn't caught here, on this page's own centerpiece table.
**Fix:** change to `class="badge bg-@u.StatusVariant"`.
**Suggested command:** `/impeccable polish`

**[P1] Overdue delivery date is visually de-emphasized instead of flagged**
*Verified against source.* `Detail.cshtml:499`: when the delivery date has passed, the copy renders as `<span class="text-muted">Opleverdatum voorbij (...)</span>` — muted grey, *less* visually weighted than the plain on-track "N dagen resterend" case just below it. This is exactly the case the Rust danger token exists for, and it's unused here — a PM scanning the card reads the overdue case as calmer than the normal one.
**Fix:** use `gl-text-danger` (already used elsewhere on this same page) on the overdue branch.
**Suggested command:** `/impeccable polish`

**[P1] New photo/filter controls sit well under the app's own 40px touch-target rule**
`.gl-detail-thumb-edit` is 22×22px, `.gl-detail-thumb-remove` is 18×18px, `.gl-mc-filter-chip` resolves to roughly 22–24px tall — all new this session. DESIGN.md's Field-Width Rule is explicit: "controls stay ≥40px tall... on-site phone use is first-class, not a fallback." This app is used by PMs on tablets/phones on-site per PRODUCT.md.
**Fix:** enlarge hit areas (padding, not just visible size) to 40px minimum, or move photo actions into a properly-sized menu trigger.
**Suggested command:** `/impeccable adapt`

**[P1] Heading semantics break exactly on the highest-priority card**
Every other card uses `<h2 class="card-title">` (Algemene gegevens, Klanten, Voortgang & verkoop, Documenten & contracten, Facturatie, Foto's & media, Verzekeringen) — *verified against source* — but "Aandacht vereist", the section meant to draw attention first, uses a plain `<div class="card-title">` (`Detail.cshtml:157`). A screen-reader user navigating by heading level skips over the one section that's supposed to be most urgent.
**Fix:** promote to `<h2>` to match every sibling card.
**Suggested command:** `/impeccable audit`

**[P2] Two incompatible destructive/error UX patterns on one page**
Project deletion uses the branded modal (`#modaldeleteproject`, magnificPopup). Standard-photo removal uses a raw browser `confirm()`, and upload failure uses a raw `alert()` with a blocking, unstyled dialog and a semi-raw error string. Both are real user-facing moments rendering in completely different visual languages.
**Fix:** route photo remove/upload-failure through the same modal/toast pattern used for project delete.
**Suggested command:** `/impeccable clarify`

**[P2] Duplicated data increases scan cost without adding information**
Fysieke/Financiële voortgang %, open-punten count, and werkdagen-resterend all appear both in the KPI strip and, unchanged, in the "Voortgang & verkoop" card two sections down — the same numbers, re-read, with no differentiation of purpose.
**Fix:** merge into one component, or give each appearance a distinct purpose (e.g., KPI strip = current state, card = trend/detail).
**Suggested command:** `/impeccable distill`

## Persona Red Flags

**Casey (mobile field-user, on-site on phone/tablet):**
- The 18–22px photo edit/remove buttons and ~22px filter chips aren't reliably tappable outdoors or with work gloves — a direct Field-Width Rule violation on controls built this session.
- The KPI strip has no mobile-hide rule (unlike the Home dashboard's KPI strip, which `custom.css` explicitly hides below 768px). On a phone, Casey gets 7 stacked tiles before reaching the attention panel, and each value is `white-space:nowrap` + ellipsis-truncated — a long currency figure risks silently truncating with no way to see the full number.
- *(Lower confidence, needs a live check)* the 7 detail cards use `col-xl-4 col-lg-6 col-md-6 col-sm-12` with no explicit `col-12` fallback below `sm` — worth confirming they render full-width on an actual phone.

**Alex (power user / desk-based PM running many projects):**
- The badge-color bug is worse for Alex specifically — Alex lives in this table daily and will burn real pattern-recognition time on colors that don't match what every other screen in the app has taught them (Rust=danger, Ochre=warning).
- `.gl-mc-filter-chip` defines no `:focus`/`:focus-visible` style at all, while comparable toggles elsewhere in this codebase (`gl-arrange-toggle`, `gl-snelactie`, `gl-pin-toggle`) all have one — a keyboard-driving power user loses visible focus tracking on the newest interactive element on the page.

**Sam (accessibility, nl-BE users outdoors):**
- Same missing `:focus-visible` gap as above.
- Filter chips have no `aria-pressed` state (the codebase's own `gl-arrange-toggle` pattern has one) — a screen-reader user gets no indication which chip is selected, only sighted users following the color change do.
- The broken heading hierarchy (P1 above) directly costs a screen-reader user their normal way of jumping to the most important section first.

## Minor Observations

- `card-body bg-light` appears on every card; no confirmed mapping from Bootstrap's stock light grey to the app's own warm Page Grey token was found in the stylesheets read — flagged at lower confidence since it can't be rendered here, but worth a live check for a Warm-Grey Rule violation.
- This page still uses the legacy ad-hoc header block (`h1.page-head` + `h5` subtitle + `.btn-group`) rather than the documented `gl-page-header`/`_PageHeader.cshtml` component that DESIGN.md calls "the single successor pattern" — notable given this exact page was just redesigned.
- The scrollable units table (`gl-detail-units-body`, 260px max-height) has no sticky `<thead>`, so scrolling loses column context.
- "Aandacht vereist" only implements 2 of the documented 3 severity tiers (no `gl-mc-info`/"TER INFO"), and loses the snooze affordance the same issue records get on the Projectleider Home dashboard.
- KPI-strip applies severity color to "Open punten" when >0 but never to "Werkdagen resterend", arguably the more time-pressured number.

## Questions to Consider

1. What if the KPI strip and the "Voortgang & verkoop" card were merged into one component instead of two that repeat the same four numbers?
2. What if "Aandacht vereist" appeared *above* the KPI strip — is a portfolio-style "state of the world" bar really the right first thing on a page whose purpose is surfacing what needs a decision right now?
3. What if this page's 7-tile KPI strip and 7-card grid were held to the same 4-tile discipline the Home dashboard already enforces?
