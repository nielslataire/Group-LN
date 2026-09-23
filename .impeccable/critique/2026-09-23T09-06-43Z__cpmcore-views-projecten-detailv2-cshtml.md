---
target: projecten/detail (incl. algemene gl-v2 layout) — re-run
total_score: 29
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:E:\\TFS\\CPMCore\\Views\\Projecten\\DetailV2.cshtml"
target_fingerprint: "sha256:9f2368eaa9739eafc006eb9d2232194919548b9b96051e582fa1b728915271df"
target_path: "E:\\TFS\\CPMCore\\Views\\Projecten\\DetailV2.cshtml"
timestamp: 2026-09-23T09-06-43Z
slug: cpmcore-views-projecten-detailv2-cshtml
closed: true
---
Method: dual-agent (A: general-purpose design-review sub-agent · B: general-purpose detector/evidence sub-agent)

**Re-run** — same target as the earlier session run (27/40). Since then: badge/card urgent-count unified to one source, empty states added to 5 cards via a shared `.gl-v2-empty-state` component, dead `href="#"` links removed, units table gained a mobile column-collapse, tablet inner-menu gained tap-to-reveal labels, and the "Vraagt actie" list moved from a hard cap to an internal `max-height` scroll.

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Badge/card count now share one source (fixed). Filter-chip active state has only a transient flicker + a live-region string, no persistent visual beyond the chip's own fill. |
| 2 | Match Between System and Real World | 4 | Terminology and ordering are native to how a Belgian PM actually works a project. |
| 3 | User Control and Freedom | 3 | "Alles" chip resets filters, BackUrl returns to the list. No control over the two internal scroll regions — nothing to expand/collapse, only scroll. |
| 4 | Consistency and Standards | 3 | Strong reuse of `.gl-v2-section-card`/`.gl-v2-empty-state`/`.gl-v2-badge`. Docked for a serif/sans inconsistency between card titles and empty-state titles, and chips with no `aria-pressed`. |
| 5 | Error Prevention | 3 | Read-dominant page; the two real inputs (unit search, category filter) can't produce an invalid state. |
| 6 | Recognition Rather Than Recall | 3 | Everything needed for triage is visible without memorization. Docked hard for a total absence of heading hierarchy. |
| 7 | Flexibility and Efficiency of Use | 2 | No keyboard accelerators; the new tablet double-tap-to-navigate is a discoverability fix but a permanent efficiency tax once a user already knows the icons. |
| 8 | Aesthetic and Minimalist Design | 3 | Restrained given the real data volume; the attention card's colored top border does the "this matters most" job well. |
| 9 | Error Recovery / Help Recognizing Errors | 4 | The empty-state pattern is genuinely excellent — five different empty conditions, each specific, each with a working action link. No generic "no data" anywhere. |
| 10 | Help and Documentation | 1 | No contextual help anywhere on the page; domain jargon (ABR, weerverlet) assumes trained-audience knowledge. |
| **Total** | | **29/40** | **Good — up from 27/40. The badge/list unification and empty-state consolidation are real, measurable improvements.** |

## Design Specificity Verdict

**LLM assessment (Assessment A):** Not a template. Copy, data model, and choices are load-bearing Belgian-construction specifics no generic kit would produce unprompted: "Werfmelding ontbreekt," "ABR-verzekering vervallen," "Bankwaarborg ontbreekt," a weerverlet-count explicitly declined in favor of a correct number over a fabricated one. The KPI row and the attention card's category taxonomy (Contracten/Werf/Financieel/Traject) mirror the real object model.

**Deterministic scan (Assessment B):** `impeccable detect --json` across `DetailV2.cshtml`, `_LayoutV2.cshtml`, `_RailPartialV2.cshtml`, `_ProjectInnerMenuV2.cshtml` — **zero findings**, exit 0. Verified as a genuine clean scan, not a silent no-op: the agent ran a synthetic smoke-test `.cshtml` with obvious anti-patterns (Arial, gradient button, icon-only nav) through the same detector and got a real exit-2 finding, confirming the engine does parse this extension.

**Visual overlays:** no browser automation available; both assessments worked from source inspection.

## Overall Impression

Real, measurable progress since the last run (27→29/40). The two structural bugs from before (mismatched counts, hidden blockers behind a cap) are genuinely fixed, and the empty-state work is the strongest piece of craft on the page. What's left is less about this page's own logic and more about two shell-wide gaps surfacing here for the first time: no heading hierarchy anywhere in the normal (non-empty) state, and the new internal scroll region trading a "page too tall" bug for a "silent scroll trap" risk.

## What's Working

1. **The badge/card unification is a correctness fix, not cosmetic.** `actionsRequiredCount = urgentItems.Count` is now the single source of truth for both the topbar badge and the card — eliminating a real trust-breaking bug where two numbers on the same screen disagreed.
2. **The empty-state consolidation is executed cleanly, not just claimed.** The single `.gl-v2-empty-state` definition in `gl-v2-shell.css` is confirmed in place; the old per-page duplicate in `gl-v2-projecten.css` was stripped down to only its genuine page-specific override, not left as silent drift. Each of the five empty states gets a distinct icon, message, and a working action link.
3. **The `href`-omission fix for dead attention-row links is done the right way** — gates both the attribute and the "Bekijk →" affordance together, so a non-actionable item reads as inert text instead of a link that lies.

## Priority Issues

**[P1] No heading hierarchy anywhere on the page — zero `<h1>`, card titles are `<span>`, not headings.**
- **Why it matters**: The topbar title renders as `<p>`, every content-card title (Traject, Eenheden, Algemene gegevens, Voortgang & budget, Facturatie, Documenten, Foto's & media) is a `<span class="gl-v2-section-card-title">`. The *only* real headings on the page are the five empty-state `<h2>`s — which exist exactly when a section has nothing to show. A screen-reader user navigating by heading gets nothing to jump between on a normal, populated page.
- **Fix**: Promote the topbar title to `<h1>` and `.gl-v2-section-card-title` to `<h2>` in the shared shell/card partial — a shell-level fix benefiting every gl-v2 page, not just this one.
- **Suggested command**: `/impeccable harden`

**[P1] Filter chips expose no `aria-pressed` — active category is invisible to assistive tech until after interaction.**
- **Why it matters**: `.js-gl-v2-pd-chip` buttons toggle `.is-active` purely via CSS class; there is no `aria-pressed` set on load or on toggle. A screen-reader user tabbing onto the chip row has no way to know which filter is currently applied without guessing-and-clicking.
- **Fix**: Set `aria-pressed="true/false"` in the chip markup and toggle it alongside `.is-active` in the JS handler.
- **Suggested command**: `/impeccable harden`

**[P2] Two nested, unsignaled scroll containers on one page.**
- **Why it matters**: The attention body (`max-height:400px`) and the units scroll (`max-height:320px`) sit ~600px apart, inside the outer page scroll, with no fade/gradient cue that content continues below. This is the fix for "list pushes the whole page down," but it trades that bug for a navigation trap: a user trying to scroll *past* the card on trackpad/touch instead scrolls *inside* it and may not realize why the page stopped moving.
- **Fix**: Add a bottom-edge gradient mask when `scrollHeight > clientHeight`, or an explicit "toon meer" affordance instead of silent internal scroll.
- **Suggested command**: `/impeccable polish`

**[P2] Touch targets in the attention/units toolbar are under the 44×44px field-use minimum.**
- **Why it matters**: Filter chips (≈20-24px tall) and the units search field (34px) are both meaningfully smaller than 44px, directly against PRODUCT.md's own stated "field-usable" requirement — on the same page that just shipped a careful touch fix for the tablet inner-menu.
- **Fix**: Bump chip vertical padding and the units-search height to clear 44px on touch, or add a larger invisible hit-area.
- **Suggested command**: `/impeccable harden`

**[P2] `.gl-v2-section-card-title` uses the sans face, in tension with DESIGN.md's own stated rule.**
- **Why it matters**: DESIGN.md says panel headings should be serif (Character section and the Do's list), but every content-card title on this page renders sans — while `.gl-v2-empty-state-title`, occupying the exact same role one level down, does get serif. Two headings for the same kind of thing render in two different type families depending on whether the card is empty. Pre-existing shell-wide drift, not introduced by this pass, but nothing currently says whether it's a considered exception or a bug.
- **Fix**: Either promote `.gl-v2-section-card-title` to serif, or add an explicit Named Rule in DESIGN.md carving out card titles as a distinct "dense label" tier.
- **Suggested command**: `/impeccable document`

## Persona Red Flags

**Alex (Power User)**: gets trapped in the attention card's internal scroll with no visual cue it's a sub-scroll — the fastest path through the page (scroll past the noise) now silently stalls. The tablet inner-menu's new two-tap navigation solves Jordan's discovery problem but is a permanent efficiency tax for a user who already knows every icon, with no opt-out. No keyboard fast-path to jump straight to "the thing that's wrong."

**Sam (Accessibility)**: zero heading landmarks in normal state (Priority Issue #1). No `aria-pressed` on filter chips (Priority Issue #2). `.gl-v2-pd-attention-item-text` truncates with ellipsis and no `title` attribute — at 200% zoom a truncated urgent-item description has no way to be recovered, worst exactly on the urgent list. The "0 open issues" good-news value uses a low-contrast muted color, making the reassuring number the hardest one to read.

**Casey (Mobile/Field)**: sub-44px chip and search touch targets directly contradict PRODUCT.md's field-usable principle, on the same page/pass that fixed the tablet menu specifically for touch. Two separate internal scroll regions are harder to operate one-handed on a phone than one continuous page scroll.

## Minor Observations

- `.gl-v2-pd-media-empty`'s dashed placeholder border appears to be dead CSS — nothing in `DetailV2.cshtml` currently renders that class; worth confirming.
- The "OMZET" stat's "geen potentieel" sub-label reads oddly for a fully-sold project — technically correct, could be misread as negative.
- The Facturatie card's row badge is always styled with the danger/red tone regardless of invoice status, which slightly undercuts the "red means urgent" convention the attention card just established two cards above it.
- One inline `style="display:flex;..."` on the "+N meer" media link breaks from the page's otherwise class-based styling discipline.

## Questions to Consider

- If the internal scroll is meant to protect the page from a 44-item list, would "show first 8, expand inline" actually have been closer to right than an unsignaled internal scroll?
- Is the "Vraagt actie" card's total disappearance on a clean project the right call, or does losing a fixed landmark position cost more in predictability than it saves in decluttering?
- Now that empty-state titles and section-card titles sit one visual tier apart in two different type families for the same semantic role, is that worth a DESIGN.md Named Rule, or is it a bug that's been rendering unnoticed?
