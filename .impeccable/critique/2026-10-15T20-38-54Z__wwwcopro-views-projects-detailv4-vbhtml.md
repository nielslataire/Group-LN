---
target: WWWCOPRO project detail v4 page
total_score: 29
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\WWWCOPRO\\Views\\Projects\\DetailV4.vbhtml"
target_fingerprint: "sha256:7f1d23a9f75c58a7bd9744ae98f950777d217587c4e7928f3588382fd3e0ec39"
target_path: "C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\WWWCOPRO\\Views\\Projects\\DetailV4.vbhtml"
timestamp: 2026-10-15T20-38-54Z
slug: wwwcopro-views-projects-detailv4-vbhtml
---
## Design Critique — WWWCOPRO Project Detail v4

**Method: dual-agent (A: a9742ec86aec06f2a · B: a9e2d1786383e38f0)**. No browser automation available this session; code-read assessment.

### Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | No loading state for the map or the calculator's first paint |
| 2 | Match System / Real World | 4 | Fluent Dutch real-estate vocabulary, natural flow |
| 3 | User Control and Freedom | 3 | No way back to edit the contact form after it submits and hides |
| 4 | Consistency and Standards | 2 | Price numerals in two typefaces; "available" is gold on tabs, off-palette green on map |
| 5 | Error Prevention | 3 | Honeypot, required fields, Belgian-phone regex, format hints |
| 6 | Recognition Rather Than Recall | 3 | Desktop "Grondplan" download is icon-only, hover-title discoverable only |
| 7 | Flexibility and Efficiency | 3 | Deep-linkable anchors, three convergent paths to the same selection |
| 8 | Aesthetic and Minimalist Design | 3 | Clean rhythm, undercut by inconsistent price typography |
| 9 | Error Recovery | 3 | Strong validation UX in modals only, not the main contact form |
| 10 | Help and Documentation | 2 | FAQ exists and is contextual, but not searchable |
| **Total** | | **29/40** | **Good (low end)** |

### Design Specificity Verdict

LLM: genuinely product-specific (convergent map/tabs/table/calculator selection, bespoke amortization calculator), undercut by internal drift in the page's own token system.

Deterministic scan: 152 advisory findings on project-detail-v4.css (113 font-size, 25 color, 14 radius) when scoped directly to that file — most compare against the wrong reference doc (this page intentionally uses its own design-handoff spec), but several color findings (#7CC48B, #CFEBD5, #3B6D11, #9cbf8e) independently corroborate Agent A's P1 map-color finding below.

No visual overlays — no browser tool available this session.

### Priority Issues

**[P1] Dead/contradictory CSS for the project-map "available" color, using off-palette colors.** `.pdv4-zone.is-avail` defined twice; second block overrides first with #7CC48B/#CFEBD5/#8a948a/#3B6D11, none in the token set or DESIGN.md. Map marks "available" in green while lot tabs mark it gold. Fix: delete superseded block, standardize on var(--pdv4-gold). → /impeccable harden

**[P1] Safe-area fix doesn't propagate to a dependent element.** `.pdv4-bar` correctly pads for env(safe-area-inset-bottom); `body:has(.pdv4-bar:not([hidden])) .cc-fab { bottom: 106px; }` is still hardcoded, risking overlap on notched iPhones. Fix: bottom: calc(106px + env(safe-area-inset-bottom)). → /impeccable harden

**[P2] Playfair→Avenir price-typeface switch only half-applied.** .pdv4-priceline__v and .pdv4-summary dd fixed; .pdv4-lotcard__price, .pdv4-media__price, .pdv4-calc__amount (the largest price number on the page) still Playfair, one with tabular-nums on a serif face. → /impeccable polish

**[P2] Validation quality inconsistent between request modals and the main contact form.** Modals share typo-aware checkEmail/checkPhone/checkName; #pdv4ContactForm reimplements weaker, generic-message-only validation. → /impeccable harden

**[P2] Low-contrast text on --pdv4-green-soft.** .pdv4-lotname small and .pdv4-summary dt render ~2.6:1 on white, under WCAG AA 4.5:1. → /impeccable harden

**[P3] Pill-shaped controls contradict DESIGN.md's 3-to-8 Rule.** .pdv4-seg/.pdv4-lottab/.pdv4-togglechip/.pdv4-pill/.pdv4-badge forced to pill radii with !important. → /impeccable document

### Persona Red Flags

Jordan: "In optie"/"Op aanvraag" pills undefined; desktop "Grondplan" is icon-only/hover-title discoverable.
Casey: sticky bottom bar hides exactly when a phone user selects a lot on the map; replacement popup has no tel: action.
Riley: down-payment slider above price silently shows "€ 0/maand" with no explanation; saved-contact cookie wired into modals but never the main contact form.

### Minor Observations

- .pdv4-modal__close safe-area handling only adjusts top, not left/right (relevant in landscape on the new full-screen mobile modals).
- --pdv4-line (0.12) vs documented Hairline (0.10) — confirmed by detector too.
- --pdv4-serif fallback drops DESIGN.md's 'Times New Roman' middle fallback.
- .pdv4-h1/.pdv4-media__name scale to 52-56px, past the documented ~39px Display token.
- applySavedContact() defined but dead code.

### Questions to Consider

1. Has the map/tabs/table convergence been watched on a real phone?
2. Why does the calculator's headline figure still sit in Playfair with tabular-nums bolted on, if the other two price spots just moved to Avenir for that exact reason?
3. Was dropping gold-for-"available" on the map intentional, or unreconciled drift?
