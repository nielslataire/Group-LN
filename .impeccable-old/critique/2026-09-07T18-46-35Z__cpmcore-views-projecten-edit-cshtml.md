---
target: projecten/editproject
total_score: 30
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Projecten\\Edit.cshtml"
target_fingerprint: "sha256:16c46cb8e4cc4ca59c460e8821b44ae0fa82749d6cca6d809761b2ce24ed5d3f"
target_path: "C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Projecten\\Edit.cshtml"
timestamp: 2026-09-07T18-46-35Z
slug: cpmcore-views-projecten-edit-cshtml
---
Method: dual-agent (A: af2bd82690c8689a9 · B: a2747b583573eca31)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Good live feedback (slice %, photo preview, tab error badges); the "Laden..." submit-button state depends on a vendor example script that may not actually be wired to this form |
| 2 | Match Between System and Real World | 4 | Natural Dutch domain vocabulary throughout (Aannemer, Werfmelding, Gemeente) |
| 3 | User Control and Freedom | 2 | Cancel silently disarms the dirty-form warning with zero confirmation — a misclick after real edits discards everything |
| 4 | Consistency and Standards | 4 | `.gl-form-section`/`.gl-field-grid`/`.gl-switch-row` reused identically across every tab and both screens |
| 5 | Error Prevention | 2 | Gemeente is submit-blocked; the contract-slices-must-total-100% rule only recolors a badge and never blocks save |
| 6 | Recognition Rather Than Recall | 3 | Labels always above fields; current values pre-populate Select2 as chips |
| 7 | Flexibility and Efficiency | 3 | Arrow/Home/End keyboard tab navigation; `minimumInputLength:3` typeaheads have no visible threshold hint |
| 8 | Aesthetic and Minimalist Design | 3 | Clean per-section, but "Algemeen" — the tab users land on first — bundles 3 full sections vs. 1 for every other tab |
| 9 | Error Recovery | 4 | Tab error-badge + auto-force-tab + scroll-to-first-invalid-field is a genuinely strong recovery pattern |
| 10 | Help and Documentation | 2 | `IssuerCompanyIdBuilder`/`IssuerCompanyIdLandOwner` and other non-obvious fields carry no inline hint |
| **Total** | | **30/40** | **Good** |

## Design Specificity Verdict

**LLM assessment**: Mostly a well-executed generic Bootstrap-admin tab shell wearing CPM's palette, not a shell built around this specific domain. The tab strip, card wrapper, label-above-field grid, and bordered checkbox list are patterns any admin-tool starter kit would produce; Toevoegen.cshtml reusing the identical shell markup verbatim for a one-section form is the clearest evidence of that genericness. Two things redeem it: disciplined reuse of the design tokens (the 40px mist-green topbar page-icon chip is echoed almost 1:1 by every section-head icon chip, tying page chrome to content), and one genuinely domain-specific widget — the live-updating contract-slices "Totaal: X%" badge in the Coördinatie tab that turns green at exactly 100%.

**Deterministic scan**: Clean. 6 of 8 scanned files (Edit.cshtml, _ProjectFormGeneral, _ProjectFormPlanning, _ProjectFormPartners, _ProjectFormDocs, _ProjectFormCoordinationCard) returned zero findings. The detector flagged 2 `broken-image` warnings (Toevoegen.cshtml:86, _ProjectFormMedia.cshtml:29) — both are `<img>` tags with no `src`, hidden via `d-none`, intentionally used as client-side preview targets that JS populates on file-select. Both confirmed false positives; no real broken-image issue exists. One tooling note: the detector's documented exit-code convention (0=clean, 2=findings) didn't hold in practice — both files with a finding still exited 0, so the JSON body, not the exit code, was the reliable signal here.

**Visual overlays**: Not available. No browser automation tool is exposed in this session, so there is no live screenshot or injected overlay to point you to — every finding above comes from reading the Razor markup, the shared JS, and the CSS rules directly, not from a rendered page.

## Overall Impression

This is a solid, disciplined *reskin* of a generic admin shell, not yet a shell that feels authored for CPM. The mechanics you'd want from an internal tool — consistent structure, one accent color, dense-but-labeled fields, a genuinely strong validation-recovery flow (tab badges + auto-jump + scroll-into-view) — are all there and well executed. What's missing is enforcement consistency (Gemeente blocks submit, an equally important field doesn't) and a couple of trust-eroding gaps around the new topbar Save/Cancel move (no confirm on discard, no visible focus ring on the tab panel). The single biggest opportunity: make the "Algemeen" tab — the one every user sees first, on every visit — as light as the rest of the shell promises, and make the slices-total rule as strict as the Gemeente rule right next to it.

## What's Working

1. **Tab error-badge + auto-force-tab + scroll-to-first-invalid-field.** A failed Save jumps the user to the exact invalid field, switching tabs automatically and scrolling it into view — a real "reassurance at a high-stakes moment" pattern, well above what most internal tools bother with.
2. **Systemic icon-chip echo.** The 40px mist-green topbar page-icon chip is deliberately mirrored by every section-head icon (42px, same tokens) — evidence of actual design-system thinking carried through to content, not just chrome.
3. **Strict One Green Rule discipline.** Rust (danger) is used only for genuine error states — tab badges, invalid-field borders, the required asterisk — across every partial reviewed. No stray accent colors.

## Priority Issues

**[P1] Focus indicator removed on the tab panel's keyboard-landing target.**
- **Why it matters**: `.gl-form-shell__panel:focus { outline: none; }` strips the focus ring from a `tabindex="0"`, `role="tabpanel"` element that keyboard and screen-reader users land on immediately after switching tabs — with nothing put in its place. A keyboard-only user (Sam) loses their place on every tab switch.
- **Fix**: Give the panel a visible focus style (even a subtle one, consistent with the existing green focus-ring token), or move focus to the first focusable field inside the panel instead of suppressing the outline entirely.
- **Suggested command**: `/impeccable audit`

**[P1] Error prevention is inconsistent between two fields that are both "must be right."**
- **Why it matters**: Gemeente is hard-blocked at submit with a real inline error and auto-scroll. The contract-slices-must-total-100% rule (Coördinatie tab) only recolors a badge — nothing stops a save at 60%. A project manager can silently create a broken coordination contract that only surfaces as a problem later, in accounting.
- **Fix**: Apply the same submit-blocking pattern already built for Gemeente to the slices total when coordination is active.
- **Suggested command**: `/impeccable harden`

**[P2] Adjacent Cancel/Save in the topbar, with no confirmation on discard.**
- **Why it matters**: Cancel and Save now sit 10px apart in the topbar as two same-size `btn-sm` buttons, and Cancel explicitly zeroes the dirty-form flag with no confirmation — a misclick after real edits silently discards them. The old bottom-of-form mobile fallback is, ironically, safer than the new "upgraded" desktop placement it's meant to mirror.
- **Fix**: `confirm()` on Cancel only when the form is actually dirty; consider visually de-emphasizing Cancel relative to Save (outline vs. fill already helps, but proximity + equal size still invites the misclick).
- **Suggested command**: `/impeccable harden`

**[P2] SEO meta-description hint promises a limit the field doesn't enforce.**
- **Why it matters**: The field allows 320 characters (`maxlength="320"`) but the hint text says "Max. 155 tekens aanbevolen" with no live counter — a user can't tell they've crossed the recommended length until it's already truncated in a Google result.
- **Fix**: Add a live `x/155` counter, or lower `maxlength` closer to the recommendation.
- **Suggested command**: `/impeccable clarify`

**[P3] "Algemeen" — the default, first-seen tab — is the densest one in the shell.**
- **Why it matters**: It stacks three full sections (Algemene info, Status & publicatie, Standaard foto) while every other tab gets one. This undercuts the "fits on one screen" promise exactly where users land by default, on every single visit.
- **Fix**: Split Status & publicatie / Media into their own tab, or fold them under one clearer combined section with better internal grouping.
- **Suggested command**: `/impeccable layout`

## Persona Red Flags

**Jordan (First-Timer)**: Company/Gemeente typeaheads require `minimumInputLength: 3` with no placeholder telling Jordan to keep typing — 1-2 letters producing nothing reads as broken, not "type more." `IssuerCompanyIdBuilder`/`IssuerCompanyIdLandOwner` on the Partners tab carry zero inline hint despite being non-obvious financial-routing fields, while nearby switches all get explainers. The SEO 320-vs-155 mismatch above is exactly the kind of silent trap Jordan won't notice until someone else points it out.

**Sam (Accessibility-Dependent User)**: `.gl-form-shell__panel:focus{outline:none}` removes the visible focus indicator on the exact element keyboard/screen-reader users land on after every tab switch (WCAG 2.4.7 concern). The fixed-height, internally-scrolling panel combined with browser zoom (a common low-vision accommodation) shrinks an already-small viewport further — worth a real screen-reader/zoom pass this review couldn't perform. Select2's generated combobox markup has historically inconsistent screen-reader support; unverified here.

**Casey (Distracted Mobile/On-Site User)**: Below 768px, Save/Cancel disappear from the topbar entirely and the fallback row sits at the bottom of whichever tab is currently open — on Coördinatie, with several dynamically-added slice/rate rows, Save could be several swipes away, exactly where on-site use matters most per this product's own "field-usable" principle. The 250ms-debounced Select2 typeaheads for Gemeente/company/weerstation are a harder target on a job site with weak connectivity and gloves than a plain `<select>` would be.

## Minor Observations

- Two competing declarations for the same concept in `projecten-custom.css`: `.gl-project-form .gl-form-section { margin-bottom: 16px }` vs. bare `.gl-form-section { margin-bottom: 26px }`. The scoped rule wins by specificity, but it's worth consolidating.
- `TabMap` in Edit.cshtml is a hand-maintained field-prefix-to-tab list; a future field rename that isn't updated here fails silently (no crash, no badge) — worth a code comment flagging it as a maintenance trap.
- `Toevoegen.cshtml`'s single panel is wrapped in `.gl-form-shell__panel` without `role="tabpanel"`/`hidden`, unlike Edit's real tabpanels — the same class is used in two different ARIA contexts, which is fine functionally but worth a comment noting it's deliberate.
- Detector tooling note (not a design issue): its documented exit-code convention didn't hold in this run — treat the JSON body as authoritative, not the process exit code.

## Questions to Consider

- Toevoegen is explicitly "just enough to create it," yet it now inherits the same fixed-height, scroll-container machinery built for a 6-tab edit form — for one short section, does that add complexity (and dead vertical space on a tall monitor) for zero real payoff?
- Was leaving the slices-total unenforced while Gemeente is hard-gated a deliberate product call (drafts can stay incomplete), or did that validation just not make the jump into the new shell?
- Now that Save lives outside the `<form>` via `form="projectEditForm"` on a multipart form, has cross-boundary submit with a file input been checked in older WebKit/Safari, where this pattern has historically been unreliable?
