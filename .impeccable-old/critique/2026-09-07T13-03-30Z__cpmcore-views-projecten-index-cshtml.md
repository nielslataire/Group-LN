---
target: projecten/index
total_score: 15
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 3
target_identity: "file:E:\\TFS\\CPMCore\\Views\\Projecten\\Index.cshtml"
target_fingerprint: "sha256:f8033c926cef01562e81afa177ce855be4e5639f79ed3f58f9c0175a5fefa858"
target_path: "E:\\TFS\\CPMCore\\Views\\Projecten\\Index.cshtml"
timestamp: 2026-09-07T13-03-30Z
slug: cpmcore-views-projecten-index-cshtml
---
# Critique — CPMCore/Views/Projecten/Index.cshtml + _ProjectGridItems.cshtml (Projecten / lijst)

Method: dual-agent (A: design review · B: detector + static evidence) · Mode: Operate

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 1 | Client-side filter to zero matches shows a blank page — no message, no aria-live; the "N van M geladen" counter never updates for a client search |
| 2 | Match System / Real World | 2 | Domain terms are right, but the verkocht/fysiek/financieel bars are an unlabelled group and every lifecycle status flattens to one green pill |
| 3 | User Control & Freedom | 2 | No reset-filters control, no sort (fixed delivery-date order), "Laad meer" is one-way, back = browser only |
| 4 | Consistency & Standards | 1 | Reinvents gl-werf-*; Font Awesome amid Boxicons; stock alert alert-info blue; bg-primary for every status vs the sc-* chip system; unpkg CDN vs local libs; a stray ; in output |
| 5 | Error Prevention | 2 | bx bx-error isn't in the shipped Boxicons bundle -> the physical-progress warning renders blank; new RegExp(userInput) throws on ( or [ |
| 6 | Recognition vs Recall | 2 | Filter controls have no labels, no active-filter chip, no client-side result count |
| 7 | Flexibility & Efficiency | 1 | Filter state is client-only — not URL-persisted, not shareable, lost on refresh; no sort, no saved views; a client search can't see older projects until you hammer "Laad meer" |
| 8 | Aesthetic & Minimalist | 1 | Six-up cards each carry photo + 2 titles + 3 icon rows + 3 gradient bars + badge; JS height equalisation; a visible stray semicolon; two icon families |
| 9 | Error Recovery | 1 | Zero-results = blank void that reads as a crash; unpkg unreachable = grid silently never lays out; "Laad meer" failure only resets the button |
| 10 | Help & Documentation | 2 | The Detail page has a coachmark tour; this list has none, and the three progress bars are never explained as a set |
| Total | | 15/40 | Poor |

Applicable maximum: 40 (all ten heuristics scored; none n/a).

## Design Specificity Verdict

Rating: category-interchangeable. The data on the card is domain-aware; the page is a generic Bootstrap + Isotope masonry.

LLM assessment (A): The card carries gemeente, bouwstart, aantal klanten, and a permission-gated verkocht / fysiek / financieel gl-pg-bar trio with the signature gradient token. But the page is a generic fitRows masonry: libs from unpkg at floating majors, a JS equalizeCardBodies() height hack, a col-lg-2 six-up grid, Font Awesome glyphs in a Boxicons system, a stock alert alert-info empty state, and a status badge that is the same Deep Forest Green for every state. Most damning: the app already ships an authored construction-site project card — gl-werf-* in the dashboard component library (photo + gradient overlay + severity-mapped status chip sc-groen/geel/rood/donker + warning badge + fysiek/financieel bars + delivery countdown), documented in DESIGN.md as the pattern — and this page ignores it to rebuild a weaker parallel.

Deterministic scan (B): impeccable detect -> 0 findings, exit 0 on both files. B verified this is a real "nothing matched" (a known-bad fixture correctly returned exit 2 with a low-contrast finding) but flags it low-confidence: .cshtml gets reduced/regex handling, not full DOM+CSS analysis. The static cross-check confirmed A's concrete items with line numbers: 3 Font Awesome glyphs (_ProjectGridItems.cshtml:76,83,91); bx bx-error at _ProjectGridItems.cshtml:124 (v2 name, not in the v3 basic bundle -> blank glyph); unpkg.com scripts at Index.cshtml:90-91 with range pins @3 / @4; badge bg-primary is a fixed class (_ProjectGridItems.cshtml:54) — the .status-@statusId class exists only as an Isotope filter selector, no CSS; #search-term and #status-filter-select have no label and no aria-label (zero for= in the file); only the <img> is wrapped in <a href> — the <h4> name is dead; 3x role="progressbar" with valuenow/min/max but no accessible name; and the stray ; at _ProjectGridItems.cshtml:71 renders as literal text under the commercial title for every project that has one.

Visual overlays: none — no browser automation is exposed and the ASP.NET app cannot be built or served here.

## Overall Impression

The one genuinely good decision — routing the single action to @section PageActions and dropping the in-content title, per the Paginakop contract — sits on top of a page that fights its own design system everywhere else. The portfolio is a wall of six-up, near-identical green-tagged cards where nothing pulls the eye toward the project you came for, the status badge communicates nothing scannable, and a filter that matches nothing drops you into a blank white screen that on a flaky site connection reads as "CPM is down." The single biggest move: stop maintaining a second project card. Render the portfolio with gl-werf-* (or a shared partial), inherit its severity-mapped status chips and CSS-only equal heights, and add the empty/no-results/loading states the surface is missing.

## What's Working

1. Paginakop migration done right. No ad-hoc in-content h1/subtitle, one primary action in @section PageActions, breadcrumb-driven topbar title. This is the compliant successor pattern.
2. Real domain progress signalling. The permission-gated gl-pg-bar trio uses the signature gradient token so each bar shows how far and how good at once; has-no-photo gets a genuine mobile badge reflow rather than a broken empty frame.
3. Sensible server pagination. initialLimit 30 / batchSize 12, a live "N van M projecten geladen" counter, graceful appended layout, and the button self-hiding when exhausted.

## Priority Issues

[P1] Every status badge is the same Deep Forest Green
- Why it matters: the badge is the single most useful scanning cue on a portfolio card — what stage is this project at? — and rendering every status identical green throws it away. Badge + three green-tipped bars per card also breaks the One Green Rule, and a lifecycle status rendered in the brand/primary colour blurs the line the new Status-Is-Not-Severity Rule draws. B confirmed bg-primary is hardcoded and the status id only drives the JS filter.
- Fix: map ProjectStatus to the documented lifecycle palette exactly as gl-status-chip sc-groen/geel/rood/donker already does on the werf-kaart (Primary / Sage / Ochre / Ink); keep Rust for genuine problems; hold the card to one green mark.
- Suggested command: /impeccable colorize — the Projecten/Index status badges to the lifecycle palette.

[P1] The page rebuilds a weaker parallel of the app's own gl-werf-* card
- Why it matters: gl-werf-* is documented in DESIGN.md as the construction-site project card (photo + overlay + severity chip + warning badge + fysiek/financieel bars + delivery countdown). gl-project-card is a second implementation that will keep drifting from it — different status colours, different equal-height mechanism, different icon family.
- Fix: render the portfolio with the gl-werf-* component (extract a shared partial if the two contexts differ), inheriting its status-chip mapping, footer countdown, and CSS-only equal heights.
- Suggested command: /impeccable shape — reconcile the portfolio card with the gl-werf component.

[P1] Zero-results, loading, and empty states are missing or off-system
- Why it matters: client-side Isotope filtering to no matches shows a blank page — indistinguishable from an error; the empty state uses stock alert alert-info (blue, banned by the Warm-Grey / Warm-Signal rules) when a designed empty treatment (gl-mc-empty) already exists; an unpkg failure silently yields no layout at all.
- Fix: show a "Geen projecten gevonden voor '...'" panel in the gl-mc-empty treatment when the filtered count is 0; replace alert-info with the same; add an aria-live result count that tracks the client filter; give the empty state a "Project toevoegen" CTA.
- Suggested command: /impeccable onboard — add empty, no-results and loading states to Projecten/Index.

[P2] The card is not a unit, and photoless cards are un-openable on mobile
- Why it matters: only the <img> links to Detail — the <h4> name, gemeente and meta rows are dead to pointer and keyboard — and at <768px the has-no-photo rule sets image-frame-wrapper { display:none }, so a project without a photo has no route to its detail page at all on a phone.
- Fix: make the whole card a single <a> (matching the "hele rij is de link" pattern from the detail hub); keep a tappable placeholder on the no-photo card instead of hiding the wrapper.
- Suggested command: /impeccable adapt — make the card a single clickable unit and fix the no-photo mobile dead end.

[P2] Off-system icons, an invalid icon, stock red, and an unpinned third-party CDN
- Why it matters: Font Awesome (fas fa-map-marker-alt/calendar-alt/users, lines 76/83/91) runs alongside Boxicons; bx bx-error (line 124) isn't in the shipped bundle so the physical-progress warning renders blank — the signal is silently lost; text-danger is stock Bootstrap red, not Rust; unpkg.com/isotope-layout@3 and imagesloaded@4 are floating majors from a CDN outside the project convention, fragile on-site.
- Fix: swap the three FA glyphs to bx equivalents, replace bx bx-error with a valid v3 name (bx-error-circle) and text-danger with the Rust token, and vendor Isotope/imagesLoaded locally at a pinned version (or from cdnjs).
- Suggested command: /impeccable harden — Projecten/Index icons and dependencies.

## Persona Red Flags

Alex (power user): filter/status state is client-only — lost on refresh, not shareable, no deep link; no sort control at all (fixed DeliveryDate order); must click "Laad meer" repeatedly before a client-side search can even see older projects; no shortcut to focus #search-term; browser Ctrl-F is unreliable because Isotope leaves filtered-out cards in the DOM.

Sam (accessibility): #search-term and #status-filter-select have no label or aria-label; three role="progressbar" per card with no accessible name -> three identical announcements with no verkocht/fysiek/financieel distinction; the bx bx-error warning is conveyed by title only and renders blank; the zero-results change is not announced; "how good" is encoded by gradient hue alone; heading structure is a flat run of <h4> with no h1-h3; no visible focus style on the photo link; the no_image.jpg placeholder still gets the project name as alt.

Casey (distracted mobile, on-site): a photoless project has no tap target to open it; ~30 tall cards to thumb past with no sticky filter; no-results is a blank screen that looks like a crash on a weak connection; the grid depends on two unpkg scripts that may not load on-site; 4px progress bars are near-invisible in daylight.

## Minor Observations

- Stray literal ; renders under the commercial title (_ProjectGridItems.cshtml:71) for every project with a CommercialTitleNL.
- <hr class="d-md-none my-2"> shows a divider on mobile only — inconsistent chunking across breakpoints.
- Controller still sets ViewData["SubTitle"] / ["SubTitleText"] although the in-content subtitle was removed — dead data.
- Comment cruft ships to production: the "GEEN toolbar meer" HTML comment, plus emoji throughout the inline <script>.
- The Isotope filter matches text() of the whole card, so "5" matches "Aantal klanten: 5", "20" matches "20%" in a bar, "2024" matches a start date — noisy full-text matching behind a "Zoek op projectnaam of gemeente" placeholder.
- new RegExp(query, 'gi') on raw input throws on an unbalanced ( or [ and kills the filter with no recovery.
- Density jumps col-md-4 (3-up) straight to col-lg-2 (6-up) with no 4-up stop; a col-lg-2 card is ~180px wide holding photo + 2 titles + 3 rows + 3 bars.
- equalizeCardBodies binds to resize un-debounced and re-measures every .card-body — a job display:grid; align-items:stretch does for free.

## Questions to Consider

1. You already built gl-werf-* — a construction-site project card with severity-mapped status chips and a delivery countdown — and documented it as the pattern. Why does the actual project portfolio get a weaker, parallel card instead of the one your design system prescribes?
2. If a project manager on-site filters to "In aanbouw" and gets a blank white screen, how many seconds pass before they assume CPM is down and phone the office?
3. Every status badge is the same green. What is that badge telling anyone — and if the honest answer is "nothing you can scan," why is it the most prominent thing on the card after the photo?
