---
target: projectleider dashboard
total_score: 29
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 3
target_identity: "file:E:\\TFS\\CPMCore\\Views\\Home\\_DashboardProjectleider.cshtml"
target_fingerprint: "sha256:a3b743a27230e4f4b632bea77349298cad33aac81b60b84f30ec977ab4144ba4"
target_path: "E:\\TFS\\CPMCore\\Views\\Home\\_DashboardProjectleider.cshtml"
timestamp: 2026-09-04T14-04-51Z
slug: cpmcore-views-home-dashboardprojectleider-cshtml
closed: true
---
# Critique — Projectleider Dashboard (CPMCore/Views/Home/_DashboardProjectleider.cshtml)

Method: single-context (degraded). Mode: Operate. Detector: clean ([]). No browser overlay. Post-harden pass (JS now functional, KPIs now real).

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | KPIs real now; accordion/snooze give feedback; search sheets show loading state |
| 2 | Match System / Real World | 4 | Werven, real construction phases, Dutch throughout |
| 3 | User Control and Freedom | 3 | Sheets close via Esc/overlay/button; snooze has no visible undo |
| 4 | Consistency and Standards | 2 | KPI strip identical green regardless of severity vs meldingencentrum's strong red/amber/blue |
| 5 | Error Prevention | 3 | Non-destructive actions; per-submenu empty states handled |
| 6 | Recognition Rather Than Recall | 4 | Status chips + progress bars + days-remaining + icons |
| 7 | Flexibility and Efficiency | 3 | Snelacties accordion strong; quick-search sheets mobile-only, no desktop equivalent |
| 8 | Aesthetic and Minimalist Design | 3 | Dense but organized; KPI icons differentiated by shape not color |
| 9 | Error Recovery | 3 | Clear empty states; failed/slow getJSON search has no .fail() handler |
| 10 | Help and Documentation | 1 | No legend for severity/status meaning; no CoachmarkPageKey |
| Total | | 29 / 40 | Good (72.5%) |

## Design Specificity Verdict

Strongly product-specific (opposite of the standard dashboard). KPI strip, urgency-grouped meldingencentrum with snooze, werf cards with real voortgang bars and delivery countdown, six construction-workflow quick actions, mobile-specific bottom-nav/sheet/iframe pattern for on-site use. Detector clean, agrees. Main gap: two different urgency visual languages on one screen (meldingencentrum red/amber/blue vs flat green KPI strip).

## What's Working

1. Real product mechanics: voortgang bars, delivery countdowns, urgency-grouped meldingen with snooze, workflow-specific quick actions.
2. Recognition over recall done right: chips/bars/icons let a PM assess project health without reading.
3. Deliberate mobile-first flows: bottom nav + sheets + fullscreen modal for on-site punt creation.

## Priority Issues

### [P1] KPI strip doesn't visually differentiate severity
All 4 tiles use identical green bg-primary icon regardless of meaning. No "something's wrong" signal even when Overdue=5.
Fix: color-code icon/accent by severity, matching the meldingencentrum's own language.
Command: /impeccable colorize

### [P1] Severity colors undocumented in DESIGN.md
Red/amber/blue meldingen system and groen/geel/donker status chips used consistently but absent from the design system.
Fix: promote to DESIGN.md as semantic-status tokens.
Command: /impeccable document or /impeccable extract

### [P1] Quick-search is mobile-only
mob-nav-punt/lev/klant wrapped d-lg-none; no desktop equivalent for jump-to-project/leverancier/klant.
Fix: add a compact desktop entry point.
Command: /impeccable adapt

### [P2] No legend for severity or status meaning
Nothing explains ACTIE VEREIST vs OP TE LOSSEN thresholds or status chip colors.
Fix: tooltip/legend, consider first-run coachmark.
Command: /impeccable clarify + /impeccable onboard

### [P2] Urgent meldingen group has no cap
Normal caps at 3 with toon-alles; urgent renders unconditionally - inconsistent disclosure pattern.
Fix: decide intentionally, apply consistently.
Command: /impeccable layout

## Persona Red Flags

Alex (Power User): well served by Snelacties accordion and KPIs; quick-search sheets invisible on desktop; snooze has no undo/visible list.

Sam (Accessibility): much improved post-harden (real buttons, aria-expanded, keyboard TER INFO, focus-visible). Remaining: snooze DOM removal has no aria-live; bottom sheets have no focus trap.

Riley (Stress Tester): zero-projects case handled well everywhere. Leverancier/klant getJSON search has no .fail() handler - can hang silently on network failure.

## Minor Observations

- KPI icons differentiated by shape even without color.
- Three independent urgency signals (Overdue KPI, werf-card countdown color, meldingencentrum severity) share no single threshold.
- Progress bars use --gl-fill-width custom property, consistent with DESIGN.md's signature component.
- Snooze duration hardcoded (1 week), no user control.
- Fullscreen Issues iframe has no loading indicator.
- No CoachmarkPageKey set (same gap as standard dashboard).

## Questions to Consider

- Should the KPI strip borrow the meldingencentrum's red/amber/blue language directly?
- Should severity colors become official DESIGN.md tokens now, before anything else copies the pattern?
- Where should desktop quick-search live - topbar, Snelacties, or new control?
- Should Overdue/countdown-color/meldingen-severity share one urgency definition?
