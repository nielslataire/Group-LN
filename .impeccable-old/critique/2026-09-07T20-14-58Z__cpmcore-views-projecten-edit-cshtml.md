---
target: projecten/edit
total_score: 34
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 2
target_identity: "file:C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Projecten\\Edit.cshtml"
target_fingerprint: "sha256:3ab7b96812845d00da2e60e760dd1140caa2121559fe2376af480ac0674afad6"
target_path: "C:\\Users\\niels\\source\\repos\\nielslataire\\Group-LN\\CPMCore\\Views\\Projecten\\Edit.cshtml"
timestamp: 2026-09-07T20-14-58Z
slug: cpmcore-views-projecten-edit-cshtml
---
Method: dual-agent (A: aaa318dc43335c8cd · B: a5867f42acb32415b)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 4 | Tab error badges, live SEO counter, live slices progress bar, live photo preview all give continuous feedback |
| 2 | Match Between System and Real World | 4 | Belgian construction/property vocabulary used correctly and consistently throughout |
| 3 | User Control and Freedom | 3 | Free tab navigation + dirty-form-guarded Cancel, but no way to discard just one tab's changes |
| 4 | Consistency and Standards | 3 | Tokens reused well, but Algemeen's fields are hand-duplicated (not shared) between Toevoegen and the Edit partial — a drift risk |
| 5 | Error Prevention | 3 | Slices-100% gate is proactive (live progress bar); the Gemeente-required gate is purely reactive — no hint exists until Save fails |
| 6 | Recognition Rather Than Recall | 4 | Select2 shows selected partner names; the doc checklist shows real delivered dates instead of making users check elsewhere |
| 7 | Flexibility and Efficiency | 4 | Full keyboard tab navigation (arrows/Home/End), direct tab jump, no forced wizard sequence |
| 8 | Aesthetic and Minimalist Design | 4 | One-accent rule respected; repeated icon-badge section pattern is clean, if a bit monotonous across 7 tabs |
| 9 | Error Recovery | 3 | Strong sighted-user recovery (badge + summary + inline + auto-jump), but the two JS-driven error spans have no `role="alert"`/`aria-live` — silent for screen readers |
| 10 | Help and Documentation | 2 | Only sparse `<small class="text-muted">` hints; nothing explains the two overlapping coordination switches |
| **Total** | | **34/40** | **Good** |

## Design Specificity Verdict

**LLM assessment**: Authored for CPM, not a generic form template. The evidence is domain vocabulary a form-generator wouldn't produce — Werfmelding(sdatum/dossier), EPB-verslaggever, Postinterventiedossier, KMI weerstation, puur coördinatieproject vs. heeft coördinatie, a contract-slices total that must hit exactly 100%, and a document checklist that pulls real dates from two different sources (Project's own planning dates for the two opleveringen, latest matching uploaded document for the other five). The engineering comments themselves — the multi-paragraph justification for position:fixed over sticky in custom.css — show reasoning about this app's actual failure modes, not a pasted component.

**Deterministic scan**: Clean across all 9 scanned files. Two broken-image warnings recur (Toevoegen.cshtml:81, _ProjectFormMedia.cshtml:29) — both confirmed false positives, same as the prior run: intentionally-src-less, d-none preview targets that JS fills on file-select, one paired with an explicit "Nog geen standaard foto ingesteld" empty state. The detector's exit-code/JSON mismatch noted last time didn't reproduce this run.

**Visual overlays**: Not available — no browser automation tool is exposed in this session.

## Overall Impression

This has visibly matured since the last pass (30 → 34/40): the tab split, submit-blocking on the slices total, the cancel-confirm, and the fixed action bar all closed real gaps. What's left clusters in one place — the two JS-driven validation gates (Gemeente, Slices) behave inconsistently with each other, and neither is reachable by assistive technology. The Gemeente requirement in particular is invisible until the moment it blocks a save, which is exactly the kind of "gotcha" the tab-badge system was built to prevent everywhere else.

## What's Working

1. **Multi-level error surfacing.** Tab badges combined with auto-tab-switch + scroll + focus on a failed submit turns "where's my error" into a guided jump — well above the internal-tool bar.
2. **Document checklist grounded in real data.** Showing an actual delivered date or "ontbreekt" instead of a bare toggle turns the checklist into something the back-office team can trust at a glance.
3. **Live, color-coded slices total.** Continuous ochre→green feedback means the 100%-total rule is never a submit-time surprise.

## Priority Issues

**[P0] Required-field signaling is invisible to assistive technology.**
- Why it matters: `.gl-req` asterisks are aria-hidden="true", #PostalSelect carries no aria-required, and the error it reveals on a blocked submit (#postalSelectError) is a plain hidden span with no role="alert"/aria-live. A screen-reader user gets no signal Gemeente is mandatory — before or at the moment of the blocked submit.
- Fix: add aria-required="true" to #PostalSelect; replace aria-hidden="true" on .gl-req with a visually-hidden "(verplicht)" span; add role="alert" to #postalSelectError and #slicesTotalError.
- Suggested command: /impeccable audit

**[P1] The Gemeente gate is reactive-only, unlike the slices gate right next to it.**
- Why it matters: Slices show a live progress bar so the 100% rule is telegraphed continuously. Gemeente has no live indicator at all — a user can fill six tabs and only learn the field was required when Save bounces them back.
- Fix: surface the requirement earlier — a persistent hint under the field, or a light client-side check on blur.
- Suggested command: /impeccable clarify

**[P1] Fixed action bar has no safe-area-inset compensation.**
- Why it matters: .gl-form-shell__actions is position:fixed; bottom:0 with no env(safe-area-inset-bottom), even though the same stylesheet already does this correctly elsewhere. On a notched iPhone, the primary Save button can sit flush against or under the home-indicator gesture bar.
- Fix: padding-bottom: calc(16px + env(safe-area-inset-bottom)) on .gl-form-shell__actions, matching offset on .gl-form-shell's reserved space.
- Suggested command: /impeccable adapt

**[P2] Coordination card's two switches have overlapping, under-explained semantics.**
- Why it matters: "Puur coördinatieproject" and "Heeft coördinatie" sit side by side with one-line hints each, but only "Heeft coördinatie" drives any visible behavior.
- Fix: either give "Puur coördinatieproject" a real visible effect, or add a short inline explanation of how the two relate.
- Suggested command: /impeccable clarify

**[P3] Algemeen's fields are hand-duplicated between Toevoegen and the Edit partial.**
- Why it matters: Name/Projectcode/Land/Gemeente/Verantwoordelijke/Verkoopverantwoordelijke/Standaard-foto exist as two separately-maintained copies, currently in sync but with no structural guarantee they stay that way.
- Fix: extract the shared fields into one partial parameterized by "light create form" vs. "full edit form."
- Suggested command: /impeccable harden

## Persona Red Flags

**Jordan (First-Timer)**: Fills six tabs, clicks Save, gets yanked back to Algemeen for Gemeente with zero forewarning. Can't tell what "Puur coördinatieproject" does versus "Heeft coördinatie."

**Sam (Accessibility-Dependent User)**: Required-ness of Gemeente is undiscoverable via AT until a silent failure. A blocked submit produces no audible signal.

**Casey (Distracted Mobile/On-Site User)**: Fixed Save/Cancel bar risks sitting under the iOS home-indicator gesture bar. The 7-tab strip scrolls horizontally with no fade/chevron affordance.

## Minor Observations

- _FormShellActions.cshtml's own comment still says "(position: sticky)" — stale since custom.css was deliberately switched to fixed.
- .gl-field margin-bottom (18px) and .gl-coord-card padding (20px 22px) sit slightly outside the documented 6/10/16/24/32 spacing scale.
- The SEO description counter turns red past 155 characters but maxlength is 320 — a user can keep typing 165 characters "in the red" with no sense of where Google's actual truncation falls.
- The 84px hardcoded action-bar-height subtraction in .gl-form-shell's height calc is coupled to Bootstrap button padding defined in a different file (theme.css) — a future theme change could silently desync it.

## Questions to Consider

- The Gemeente gate and the Slices-100% gate are near-duplicate JS blocks — now that there are two, is it worth extracting a shared registerSubmitGate() helper before a third form copies the pattern again?
- Is "Puur coördinatieproject" consumed downstream elsewhere, or is it dead weight on this screen?
- Has the mobile path been tested on a real notched device with the keyboard open?
