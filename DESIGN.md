---
name: CPM — Group LN
description: Grounded, warm-neutral admin system for a Belgian residential property developer
colors:
  forest-green: "#0a5a3b"
  forest-green-deep: "#0f4b40"
  forest-green-bright: "#0f7a52"
  mist-green: "#e8f0eb"
  mint-ice: "#e7fff1"
  sage: "#7A8450"
  timber: "#8B6B4A"
  taupe-grey: "#8A7967"
  ink: "#222222"
  page-grey: "#f5f5f8"
  cloud-grey: "#f8f9fc"
  surface-white: "#ffffff"
  hairline: "#e7e7e7"
  divider-cool: "#e3e7ee"
  slate-900: "#1e293b"
  slate-500: "#94a3b8"
  text-muted: "#8590a5"
  rust: "#b3452f"
  rust-tint: "#f7ece8"
  rust-text: "#8a3420"
  ochre: "#c17d1f"
  ochre-tint: "#faf1df"
  ochre-text: "#7a5210"
  taupe-tint: "#efe9e3"
  taupe-text: "#5f5245"
typography:
  display:
    fontFamily: "Poppins, 'Segoe UI', system-ui, sans-serif"
    fontSize: "1.5rem"
    fontWeight: 700
    lineHeight: 1.3
    letterSpacing: "normal"
  headline:
    fontFamily: "Poppins, 'Segoe UI', system-ui, sans-serif"
    fontSize: "1.4rem"
    fontWeight: 700
    lineHeight: 1.3
    letterSpacing: "normal"
  title:
    fontFamily: "Poppins, 'Segoe UI', system-ui, sans-serif"
    fontSize: "1.1rem"
    fontWeight: 700
    lineHeight: 1.3
    letterSpacing: "normal"
  body:
    fontFamily: "Poppins, 'Segoe UI', system-ui, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.5
    letterSpacing: "normal"
  label:
    fontFamily: "Poppins, 'Segoe UI', system-ui, sans-serif"
    fontSize: "0.78rem"
    fontWeight: 600
    lineHeight: 1.4
    letterSpacing: "normal"
rounded:
  xs: "2px"
  sm: "7px"
  md: "10px"
  lg: "14px"
  xl: "16px"
  pill: "999px"
spacing:
  xs: "6px"
  sm: "10px"
  md: "16px"
  lg: "24px"
  xl: "32px"
components:
  button-primary:
    backgroundColor: "{colors.forest-green}"
    textColor: "{colors.surface-white}"
    rounded: "{rounded.sm}"
    padding: "10px 18px"
  button-primary-hover:
    backgroundColor: "{colors.forest-green-deep}"
    textColor: "{colors.surface-white}"
    rounded: "{rounded.sm}"
    padding: "10px 18px"
  button-outline-primary:
    backgroundColor: "{colors.surface-white}"
    textColor: "{colors.forest-green}"
    rounded: "{rounded.sm}"
    padding: "10px 18px"
  nav-tile:
    backgroundColor: "transparent"
    textColor: "{colors.mint-ice}"
    rounded: "{rounded.lg}"
    padding: "14px 10px"
  nav-tile-active:
    backgroundColor: "{colors.forest-green-bright}"
    textColor: "{colors.surface-white}"
    rounded: "{rounded.lg}"
    padding: "14px 10px"
  card:
    backgroundColor: "{colors.surface-white}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    padding: "24px"
  topbar-page-icon:
    backgroundColor: "{colors.mist-green}"
    textColor: "{colors.forest-green}"
    rounded: "{rounded.md}"
    size: "40px"
  input-modern:
    backgroundColor: "{colors.surface-white}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    height: "46px"
    padding: "6px 17px"
  form-section-icon:
    backgroundColor: "{colors.mist-green}"
    textColor: "{colors.forest-green}"
    rounded: "{rounded.md}"
    size: "42px"
  form-tab-active:
    backgroundColor: "transparent"
    textColor: "{colors.forest-green}"
    rounded: "{rounded.xs}"
    padding: "15px"
  badge-primary:
    backgroundColor: "{colors.forest-green}"
    textColor: "{colors.surface-white}"
    rounded: "{rounded.pill}"
    padding: "4px 9px"
---

# Design System: CPM — Group LN

## Overview

**Creative North Star: "Grounded & Cultivated"**

CPM is the internal operations platform for a Belgian residential property
developer, and its interface is built to feel *rooted* — Flemish land and building
rendered as a deep forest green sitting on warm, low-saturation neutrals. The
identity is carried almost entirely by one colour: a solid dark-green sidebar and
a single green accent that marks what is primary, active, or selected. Everything
else is quiet. The tone is warm and approachable rather than corporate-crisp: an
icon-first navigation with generously rounded tiles, soft green tint surfaces
(`#e8f0eb`) behind icons and hovers, and a white topbar that greets rather than
looms. This is a tool a mixed, non-technical back-office team (project managers,
sales administration, accounting) reaches for all day and — because project
managers and contractors also open it on a phone on-site — it stays legible and
touch-friendly at every width.

Components are **refined and restrained**. The base radius is a modest 7px, state
is shown through subtle green tints rather than motion or heavy depth, and borders
are hairline. Cards are *softly lifted* — they read as distinct objects resting a
millimetre above the pale grey page, never as boxed-in panels with loud drop
shadows. Structural shadow is reserved for things that genuinely float: the
sidebar flyout menus, dropdowns, and modals.

The implementation is a heavily-customised Porto admin theme (`theme.css` →
`modern.css` → `skins/*` → `custom.css`). A formal Group LN brand identity is
expected from the client and may refine or replace parts of this system; until
then this file documents what is actually shipped and the direction confirmed with
the team.

**Key Characteristics:**
- One-colour identity: deep forest green does the work; secondary/accent/info
  appear rarely and only for categorisation.
- Warm neutral ground: greys carry a faint warmth; the page is `#f5f5f8`, not
  pure white or cool grey.
- Icon-first, rounded navigation with a solid dark-green rail and darker-green
  two-column flyout submenus.
- Near-flat surfaces: hairline `#e7e7e7` dividers, soft ambient card lift,
  structural shadow only for overlays.
- Poppins throughout, rem-based scale, weights 300–800.
- Responsive down to a 60px solid-green mobile bar; dense tables that stay usable
  on small screens.

## Colors

A single deep green over warm, desaturated earth neutrals — green signals action
and place, everything else recedes.

### Primary
- **Deep Forest Green** (`#0a5a3b`): the identity colour. Fills the full-height
  sidebar rail, primary buttons, active/selected states, links, focus rings
  (`rgba(10,90,59,.25)`), badges, the topbar page-icon glyph, and the mobile
  topbar. Used on a large surface (the rail) but as an *accent* everywhere in
  content — roughly one green mark per control cluster.
- **Forest Green Deep** (`#0f4b40`): primary-button hover/active and the
  background of the sidebar flyout submenu panels. One step darker, never used
  for text.
- **Forest Green Bright** (`#0f7a52`): the active navigation tile in the sidebar —
  a lighter green block that lifts the current section out of the rail.
- **Mist Green** (`#e8f0eb`): tint surface. Backs the 40px topbar page-icon chip,
  `card-big-info` icon boxes, tab hover/active fills, dropdown-item hover, and
  vertical-tab active rows. The "this relates to the green" wash.
- **Mint Ice** (`#e7fff1`): the only text/icon colour used *on* the green rail —
  a near-white with a green cast for sidebar labels, icons, and the header toggle.

### Secondary
- **Sage** (`#7A8450`): moss green. A categorisation colour for non-primary
  emphasis (`.badge.bg-secondary`, occasional status). Muted enough to never
  compete with the primary.

### Tertiary
- **Timber** (`#8B6B4A`): warm brown, the "accent" token (`--custom-accent`,
  `.badge.bg-accent`). Building-material warmth; used sparingly to distinguish a
  third category.
- **Taupe Grey** (`#8A7967`): the `info` colour — a warm grey-brown for
  informational badges, deliberately not a blue.

### Neutral
- **Ink** (`#222222`): darkest text and `.badge.bg-dark`.
- **Slate 900** (`#1e293b`): topbar page title and userbox header name.
- **Text Muted** (`#8590a5`) / **Slate 500** (`#94a3b8`): secondary text,
  breadcrumb links, meta, captions, muted table cells.
- **Page Grey** (`#f5f5f8`): the application background behind all content.
- **Cloud Grey** (`#f8f9fc`): faint hover fill (userbox toggle) and zebra rows.
- **Surface White** (`#ffffff`): every card, the topbar, menus, inputs.
- **Hairline** (`#e7e7e7`): the default divider/border on cards and sections.
- **Divider Cool** (`#e3e7ee`): the slightly cooler 1px border under the topbar
  and around dropdown menus.

### Semantic Status
Danger/warning/info as warm, earth-family hues — never stock Bootstrap
red/amber/blue — so a severity signal still reads as part of this palette, not
a bolted-on alert library. Each has a solid form (icon, count badge, chip) and
a pale tint + matching darker text form (banner/row backgrounds).
- **Rust** (`#b3452f`) / tint `#f7ece8` / text `#8a3420`: danger — "ACTIE
  VEREIST" meldingen, the melding count badge, the werf-card warning badge.
  A warm brick-red in the same family as Timber, not a cool crimson.
- **Ochre** (`#c17d1f`) / tint `#faf1df` / text `#7a5210`: warning — "OP TE
  LOSSEN" meldingen. Close to the existing amber werf-status chip.
- **Taupe Grey** (`#8A7967`, see Tertiary) / tint `#efe9e3` / text `#5f5245`:
  info — "TER INFO" meldingen. Reuses the info role rather than introducing a
  fourth hue; this replaced a cool blue that had drifted into the
  meldingencentrum outside the documented system.

### Named Rules
**The One Green Rule.** Deep Forest Green is the only brand colour on a content
screen. If a second saturated colour appears, it is a Semantic Status signal
(Rust / Ochre / Taupe Grey) or a deliberate category (Sage / Timber / Taupe),
never decoration. In `custom.css` the primary is referenced ~29× and every
other brand token once — keep that ratio.

**The Warm-Grey Rule.** Neutrals lean warm, never cool-blue. Reach for Page Grey
and Hairline before any `#f1f5f9`-family cool grey; the cool Slate tones are
permitted only for topbar/menu chrome text where they already live.

**The Status-Is-Not-Severity Rule.** Semantic Status colours (Rust / Ochre /
Taupe) mean *something needs attention*, not *a state exists*. A lifecycle
status that happens to be a good or neutral outcome must not borrow an alert
hue: a **sold** unit is `bg-primary` (green — the outcome you want), never
`bg-danger`. Map lifecycle states to Primary / Sage / Ochre / Ink / Dark and
keep Rust for genuine problems. Two greens on one screen (e.g. "Verkocht" and a
green brand mark) is acceptable; an alarm-red "everything is fine" is not.

## Typography

**Display / Body / Label Font:** Poppins (with `'Segoe UI', system-ui, sans-serif`)

**Character:** Poppins is the single voice — geometric, friendly, a little
rounded, which is what keeps the dense admin content feeling approachable rather
than clinical. Weight, not family, creates hierarchy: 700 for anything that is a
heading or a number that matters, 600 for labels and nav, 400 for body. Weights
300 and 800 exist in the loaded face but are rarely used.

### Hierarchy
- **Display** (700, 1.5rem, 1.3): the topbar page title when there is no
  breadcrumb (e.g. Dashboard) — the largest type most screens show.
- **Headline** (700, 1.4rem, 1.3): section headings inside content, card titles
  on landing/overview pages.
- **Title** (700, 1.05–1.1rem, 1.3): the topbar page title with a breadcrumb;
  sub-section headers; emphasised card headers.
- **Body** (400, 1rem / 16px, 1.5): default text, form values, table cells.
- **Label** (600, 0.78rem, 1.4): breadcrumbs, meta text, form labels, the
  12px/600 sidebar nav labels, table column heads. Not uppercased, not tracked.

### Named Rules
**The Weight-Not-Size Rule.** Build hierarchy by moving between 400 / 600 / 700
Poppins at a small set of sizes. Do not introduce new font sizes, a second
family, uppercase tracking, or italic for emphasis — bump the weight or use Deep
Forest Green.

## Layout

Fixed left sidebar + fixed topbar shell. The sidebar rail is **300px** on desktop
(collapses to **73px** icon-only ≥768px via `sidebar-left-collapsed`); the topbar
is **72px** (`--topbar-height`), white, holding the logo, a 40px rounded
page-icon chip, the page title, a slash-separated breadcrumb, and a right-aligned
userbox. Content sits on a `#f5f5f8` canvas with Bootstrap's grid and container
rhythm.

Spacing rhythm is roughly a 6 / 10 / 16 / 24 / 32 px progression — card interiors
are 24px (`card-big-info` uses 24–26px), topbar element gaps 12–18px, nav tiles
14px vertical. Tables run full-width and dense; `datatable-actions` cells pack
icon buttons with small gaps.

**Responsive:**
- ≥992px: flyout submenus open to the side of the rail as absolutely-positioned
  panels (min 360–430px, up to two columns).
- ≤991px: flyout submenus fall inline beneath their parent, single column.
- ≤767px: topbar becomes a fixed **60px solid Deep Forest Green** bar — hamburger,
  vertically-centred title (icon + breadcrumb hidden), profile photo only.
  Dashboard KPI strip (`.gl-kpi-strip`) is hidden.

### Named Rules
**The Field-Width Rule.** Every screen must stay usable and legible at 360px.
Tables collapse or scroll, controls stay ≥40px tall, the green mobile bar is the
only chrome. On-site phone use is a first-class case, not a fallback.

**`.content-with-menu`'s mobile top-clearance is razor-thin — treat it as
fragile, not fixed.** On any project sub-page with the `DetailMenu` sidebar
(`ProjectTraject`, `DetailClients`, etc.), the vertical budget above
`Shared/DetailMenu.cshtml`'s mobile "Toon Menu" toggle stacks three
unconditional numbers that were never designed together: `.inner-wrapper`'s
`72px` `padding-top` (`custom.css`) + `.content-body`'s own `10px`
`margin-top` (`theme.css`, `html.modern.fixed .content-body`) − `.content-with-menu`'s
own `-20px` `margin-top` (`theme.css`, unconditional) = `62px` of clearance
against a `60px` mobile topbar — **2px of slack, app-wide, on every page
using this shell.** `ProjectTraject/Index.cshtml`'s `ViewBag.ContentBodyClass
= "gl-traject-flush"` (see the Tabstrip breakout-margin bullet above)
zeroed that `10px` unconditionally when it was first added, which ate the
2px and then some — the toggle rendered visibly clipped behind the mobile
topbar, a real regression caught only by testing the phone view directly.
Fixed two ways: `gl-traject-flush`'s cancellation is now scoped to
`≥768px` only (`traject.css`), and `custom.css` independently widens the
shared budget for every page on this shell via
`@media (max-width:767.98px) { .content-with-menu { margin-top: -10px
!important; } }` (`!important` because it has the same specificity as
theme.css's own unconditional rule and source order alone wasn't reliably
winning). **Before touching any of `.inner-wrapper`/`.content-body`/
`.content-with-menu`'s vertical spacing again, re-add up this chain first**
— it is not a coincidence-proof margin, it is a coincidence.

## Elevation & Depth

The system is **near-flat with a soft lift**. Surfaces are separated primarily by
the 1px Hairline border and by the tonal step between white cards and the
`#f5f5f8` page. Primary content cards carry a soft ambient shadow so they read as
resting just above the page — never a hard drop shadow, never a boxed panel.
Structural, obvious shadow is reserved for elements that truly float above the
content plane.

### Shadow Vocabulary
- **Card rest** (`box-shadow: 0 1px 4px rgba(0,0,0,0.08)`): the default soft lift
  for primary content cards and widgets. Pair with a Hairline border.
- **Whisper** (`box-shadow: 0px 0px 37px -36px rgba(0,0,0,0.4)` — the
  `--card-shadow` token): an almost-invisible haze for secondary cards where even
  Card rest is too much.
- **Overlay** (`box-shadow: 0 16px 35px rgba(0,0,0,0.24)` for the sidebar flyout;
  `0 12px 32px rgba(15,23,42,0.12)` for dropdown/userbox menus): structural depth
  for things that float — flyouts, dropdowns, popovers, modals.

### Named Rules
**The Float-Only Rule.** A visible (Overlay-strength) shadow means the element is
literally floating above the page — a menu, a dropdown, a dialog. Resting content
gets Card rest or Whisper or nothing. Never use an Overlay shadow to make a
static card look important; use the Hairline border and, if needed, a Mist Green
header.

## Shapes

Softly rounded, consistent, never sharp and never pill-by-default. The base
radius is **7px** (`--radius`) for cards, buttons, inputs, and menu items.
Navigation is rounder — **14px** tiles in the rail, **16px** flyout panels,
**10px** for the topbar page-icon chip and userbox controls. Tiny elements
(progress bars) use **2px**. Full circles (`pill` / 50%) are only for avatars and
the round icon dots. Borders are 1px Hairline; the sidebar's inner flyout border
is `rgba(231,255,241,0.08)` — a barely-there light line on dark green.

### Named Rules
**The 7-14-16 Rule.** Content chrome (cards, buttons, fields) = 7px. Navigation
chrome (tiles, flyouts) = 14–16px. Don't mix: a button inside the rail is still
7px, a nav tile in content is still 14px.

## Components

**The Shared-CSS-By-Default Rule.** When a pattern documented anywhere in
this file is genuinely a *component* — something another page could
plausibly reuse (a button variant, a row-action pattern, a status icon, a
card shape) rather than something specific to one page's own layout — its
CSS belongs in `custom.css` (loaded on every page) by default, not in a
page- or feature-scoped file (`traject.css`, `projecten-custom.css`,
`budget-wizard.css`, …). This isn't a style preference; it's already caused a
real bug: `gl-row-action-btn` was written straight into `traject.css` when
first built, and DESIGN.md documented it as "the current pattern for new
tables" without noticing that claim was only true on `ProjectTraject` — any
other page reaching for it got nothing, silently, no error. Moved to
`custom.css` once discovered (see Table row actions). Before adding a new
component's CSS, ask: would a page other than this one ever want this? If
the honest answer is "maybe" as much as "no," default to `custom.css`. Only
keep something page-scoped when it's genuinely bound to that page's own
markup/data shape (e.g. `#mp-search-term`-specific selectors, a Kalender-only
layout grid) — a *reusable* shape wrapped around page-specific *content* is
still shared CSS with page-specific markup, not the other way around.

### Buttons
- **Shape:** gently rounded (7px), 1px border matching fill.
- **Primary:** Deep Forest Green fill, white text, ~`10px 18px` padding.
- **Hover / Focus:** fill shifts to Forest Green Deep (`#0f4b40`), or
  `#0f7a52` in some contexts; focus ring `0 0 0 0.2rem rgba(10,90,59,0.25)`.
  Transition ~0.15s on background/color.
- **Outline Primary:** transparent fill, Deep Forest Green text and border;
  inverts to green fill + white text on hover/active.
- **Semantic (`.btn-gl-*`):** e.g. `btn-gl-remove` — soft tinted danger buttons
  (`#fbd0d0` bg, `#dc3545` text) rather than solid fills, matching the restrained
  tone.
- **Disabled:** reduced-opacity look with `cursor: not-allowed` explicitly
  restored on `.btn:disabled` (Bootstrap's `pointer-events:none` is overridden so
  the state reads).

### Cards / Containers
- **Corner Style:** 7px (`--radius`).
- **Background:** Surface White on the Page Grey canvas.
- **Shadow Strategy:** Card rest (`0 1px 4px rgba(0,0,0,.08)`) for primary
  content; Whisper (`--card-shadow`) for secondary; see Elevation.
- **Border:** 1px Hairline (`#e7e7e7`).
- **Internal Padding:** 24px (`card-big-info` 24–26px).
- **`card-big-info` (signature):** a two-zone card — a ~230px left rail (white,
  1px Hairline divider, 24px padding) holding a 40px Mist-Green rounded icon box,
  a 13px/700 green title and 11.5px muted description; the right zone holds the
  form/content, one `form-group row` per field with the label right-aligned
  (`col-*-4 control-label text-md-end`) beside it. Non-standard Bootstrap column
  breakpoints `col-lg-2-5 col-xl-1-5` (rail) / `col-lg-3-5 col-xl-4-5` (content),
  from `theme.css`. Collapses to stacked single-column ≤768px. This is the
  **older** of the two form idioms — see *Formulieren (`gl-form-shell`)* below;
  new forms use the shell, `card-big-info` stays for the existing two-zone
  explanatory supplier/contract forms (`Projecten/AddContract`, `AddUnit`).

### Inputs / Fields
- **Style:** Surface White, 1px Hairline border, 7px radius. Every text field
  carries `form-control form-control-modern`: the modern variant is **46px** tall
  with `6px 17px` padding. Select2 single/multi controls and the
  bootstrap-datepicker are matched to the same height and padding so native and
  enhanced fields align — never restyle them looser.
- **Focus:** green-tinted focus ring consistent with buttons
  (`0 0 0 0.2rem rgba(10,90,59,.25)`).
- **Wrapper — two patterns, one per form idiom:** in a `gl-form-shell` form the
  field sits in `.gl-field` with the **label above** the control (600, 0.8125rem);
  in a `card-big-info` form it sits in `.form-group.row` with the label
  **right-aligned** (`control-label text-md-end`) beside it. Do not mix the two
  within one form.
- **Prefix / suffix:** money, %, date and similar use an `input-group` with a
  `input-group-text` chip (`€`, `%`, calendar glyph). Shared EditorTemplates in
  `Views/Shared/EditorTemplates/` — `Currency`, `CurrencyWithActions`,
  `Percentage`, `Surface`, `Postalcode`, `Date`, `Phone`, `Cellphone` — render
  these; `.Currencymask` inputs are initialised via `CurrencyMask.init(...)`,
  re-run on ajax-added rows.
- **Toggle:** prefer the iOS switch (`.switch.switch-sm.switch-primary` +
  `data-plugin-ios-switch`, init `new ios7Switch(el)`) over `.checkbox-custom`
  for a boolean.
- **Error:** `input.input-validation-error` gets a Rust (`#b3452f`) border + a
  `0 0 0 2px` Rust-tint glow; `<span asp-validation-for>` renders `.text-danger`
  directly under the field.

### Table status indicators (`gl-mijlpaal-status-icon`)
A colored icon replacing a text status badge — same 5-state color mapping,
smaller footprint, and it's the *leading* signal on the row instead of one
more thing to read at the end. New for `ProjectTraject`; the older text-badge
class (`.gl-mijlpaal-status`, see below) is **shared app-wide** (Deadlines,
the Home role dashboards, ProjectDossiers all use it directly) — never rename
or repurpose that class itself, add the icon variant alongside it instead.
- **Markup:** `<span class="gl-mijlpaal-status-icon s-{status}" title="{label}">
  <i class="bx {icon}" aria-hidden="true"></i></span>` plus a
  `<span class="visually-hidden">{label}</span>` sibling — the icon alone has
  no accessible name, and in a DataTable the hidden text is also what keeps
  the column searchable/sortable (pair with `<td data-order="{status}">` for
  correct numeric sort once there's no visible text left to sort by).
  Reference: `ProjectTraject/Index.cshtml`, `_Timeline.cshtml`,
  `_UnitMatrix.cshtml` (there the icon *is* the clickable status-change
  button — see Table row actions below).
- **Icon-per-status mapping** (`MijlpaalStatus`, but the principle — one
  unambiguous glyph per state, colored by the same 5-token system as the
  badge — applies to any status enum): Open `bx-circle`, Bezig `bx-time-five`,
  Bereikt `bx-check-circle`, Niet van toepassing `bx-minus-circle` (dimmed),
  Geblokkeerd `bx-block`. Color from the same tokens as the badge (`--primary`
  bereikt, `--warning` bezig, `--danger` geblokkeerd, muted neutral for the
  rest) — never a second parallel palette for the icon form.
- **A terminal "reached/complete" state can invert instead of just recoloring
  — solid fill, white glyph, and a plainer icon than the other states.**
  `ProjectTraject/Index.cshtml`'s Mijlpalen-tabel specifically (not
  `_Timeline`/`_UnitMatrix`, which keep the outline form) renders Bereikt
  (`s-2`) as a small filled circle (`20px`, down from the shared `30px` —
  a badge reads as a heavier accent than an outline icon at the same size,
  so it's deliberately smaller) in `--kal2-bereikt` with a plain white
  `bx-check`, not `bx-check-circle` — the outline glyph draws its own ring,
  which doubled up visibly against the new solid background. Scope this to
  the specific table via `#datatable-mijlpalen .gl-mijlpaal-status-icon.s-2`
  (never the bare shared rule — `_UnitMatrix`'s icon is *also* its
  click-to-change-status button and needs to stay recognizable as a button,
  not read as "already done and inert"). The completed row becomes the one
  glance-able "done" mark in an otherwise all-outline column, which is the
  point — but it's a table-specific embellishment, not a new app-wide
  status-icon rule.
- **Never reuse a status icon's glyph as an action icon in the same row.**
  `bx-check-circle` was tried as the generic "change status" action button —
  it's *also* the Bereikt status icon, so a completed row showed the same
  glyph twice with two different meanings a few columns apart. Pick an action
  icon with no overlap with the status set it sits next to.
- **Verify the icon actually renders in this app before committing to it.**
  Boxicons is loaded from a CDN "basic" font subset
  (`cdn.boxicons.com/…/basic/boxicons.min.css`, see `_Layout.cshtml`), not the
  full library, and the locally vendored `wwwroot/lib/boxicons` copy is a
  different, incomplete version too — neither is a reliable yes/no oracle.
  `bx-transfer` looked valid (it's referenced in `Projecten/DetailPhotos.cshtml`)
  but didn't render here. The one real signal is: grep for the exact class
  already in live, working use elsewhere in *this* app
  (`grep -rn "bx-{name}\b" CPMCore/Views`) — ideally more than one call site —
  before using a `bx-` icon you haven't seen rendered yourself.

### Table row actions (`gl-row-action-btn`)
Two competing patterns exist for the small icon actions at the end of a table
row (edit, change status, delete). **`gl-row-action-btn` is the current one for
new tables**; `theme.css`'s older `.table .actions a` (bare `<a>` + icon,
`color:#666` → `#333` on hover, no background, no explicit touch size) is the
pre-existing app-wide convention and stays where it already is
(`Projecten/DetailContracts.cshtml` and others) — migrate a table to the new
pattern when you touch it, not in a bulk sweep. Definition lives in
`custom.css` (shared, always loaded) — it was originally written into
`traject.css` only, which made it unusable on any page outside
`ProjectTraject` despite this doc already calling it "the current pattern for
new tables"; moved once discovered, `traject.css` keeps an identical (now
redundant, harmless) copy rather than risk touching that page to remove it.
- **Markup:** `<span class="gl-row-actions">` wrapping one
  `<button type="button" class="gl-row-action-btn" aria-label="…">` per action,
  each holding one icon (`aria-hidden="true"`) at 16px — `bx-*` on
  not-yet-migrated pages, `ph-*` on Phosphor-migrated ones (see Icons); the
  component itself doesn't care which icon font, only that there's exactly
  one glyph inside. Reference: `ProjectTraject/Index.cshtml` (Mijlpalen-tabel,
  Boxicons), `Projecten/Partials/Clients.cshtml` (Phosphor).
- **Size & rest state:** 30×30px, 7px radius (`--radius`), transparent, icon
  in muted gray (`--tsa-muted-aa`) — quiet by design, not the row's focal
  point at rest.
- **Hover/focus:** Mist-Green background, a `#bcd6c7` hairline border, and the
  icon switches to Forest Green Deep (`#0f4b40`) — the extra border matters:
  a background tint alone on a near-white table row read as too small a
  change to register as "hover" (the actual complaint that led to adding it).
  Same "quiet icon, green on interaction" language as `gl-page-header__back`
  and `gl-detail-card-edit`, just the compact 30px table-row variant, not a
  third style. A destructive action adds `.is-danger` for a Rust-tint hover
  instead of green.
- **Inside a `<td class="datatable-actions">` (or a `<th>` reading exactly
  "Acties" — `custom.js`'s `applyActionColumnStyles()` auto-tags that column
  on *every* table, globally, no opt-in), the hover above silently doesn't
  fire — a specificity bug, not a hover-state bug.** That auto-applied
  styling was written for the older bare-`<a>` convention above
  (`.table td.datatable-actions a { background:transparent; color:inherit;
  … }`, plus `i { font-size:1.25rem !important; color:inherit !important; }`)
  and outranks `gl-row-action-btn`'s own rules by pure specificity
  (`.table`+`td`+`.datatable-actions`+`a` beats a bare `.gl-row-action-btn`
  class every time, hover or not — background and icon size stay frozen at
  the non-hover value regardless of what `.gl-row-action-btn:hover` says).
  Fixed with matching-or-higher-specificity re-assertions scoped to
  `.table td.datatable-actions .gl-row-action-btn` (rest, hover,
  `:focus-visible`, `.is-danger:hover`, and an `!important` icon `font-size`
  to beat the other `!important`) in `custom.css`, right after the base
  component rules. **Whenever a `gl-row-action-btn` sits inside a table with
  an "Acties" header, verify its hover state after building it** — this is
  exactly the kind of silent, no-error failure that's easy to ship without
  noticing.
- **Always visible, not hidden until the row is interacted with.** An earlier
  version of this rule hid `.gl-row-actions .gl-row-action-btn` /
  `.gl-traject-mijlpaal .gl-row-action-btn` at `opacity:0` at rest, revealing
  only on `tr:hover`/`tr:focus-within`/`:focus-visible` (plus a
  `@media (hover: none)` touch fallback) — reasoning being that two muted
  icons on every row of a long table was more noise than signal. **Reversed
  by explicit user feedback**: action icons should stay visible so the
  affordance itself is scannable, not just discoverable on approach. Both the
  `custom.css` (canonical, see above) and `traject.css` (redundant, kept in
  sync) copies now simply omit the `opacity:0`/reveal-on-hover rules —
  `gl-row-action-btn` renders at its normal rest state (muted icon,
  transparent background) all the time, hover/focus only changes its color
  per the Hover/focus bullet above. Applies to `Partials/Clients.cshtml`,
  `Partials/Contracts.cshtml` (+ `DetailContracts.cshtml`'s JS-built child
  rows), and `ProjectTraject/Index.cshtml` (Mijlpalen-tabel) /
  `_Timeline.cshtml`.
- `_UnitMatrix.cshtml` reuses `gl-row-action-btn` for its status-change
  button, where the icon *is* the cell's main content (the status itself) —
  always-visible there too, so this exception no longer needs its own scoping
  now that the general rule matches it.
- **Gap between actions:** `.gl-row-actions` uses `gap: 0` (not a few px) —
  with icons always visible (see above), any gap read as too loose for a
  cluster of same-row actions; adjacent 30×30px buttons with 0 gap still read
  as separate targets because each only paints a background on hover.
- **An icon-only `.btn-link` (e.g. `Partials/Contracts.cshtml`'s
  `.supplier-toggle` row-expand caret) needs an explicit
  `text-decoration: none`.** Bootstrap's `.btn-link` defaults to
  `text-decoration: underline`, meant for text links — on an icon-only button
  it rendered as a visible underline beneath the caret glyph. Fixed in
  `projecten-custom.css`'s existing `.supplier-toggle` override (already
  there to swap the stock link-blue for `--primary`). Check any other
  icon-only `.btn-link` usage for the same silent underline.
- **A `.dropdown-toggle` button that gets an explicit Phosphor caret icon
  (`ph-caret-down`) for consistency shows TWO carets** —
  Bootstrap's own `.dropdown-toggle::after` (a border-triangle) still renders
  alongside it, since adding a manual icon doesn't remove Bootstrap's default
  one. Explicitly kill it with `.{scope}::after { display:none !important;
  content:none !important; }` and rotate the manual icon on open via
  `.{scope}[aria-expanded="true"] .{icon-class} { transform: rotate(180deg);
  }` — Bootstrap's dropdown JS manages `aria-expanded` on the toggle
  automatically, no extra JS needed. Fixed on `DetailContracts.cshtml`'s
  "Aannemerslijst afdrukken" button (`.gl-supplier-list-toggle` /
  `.gl-supplier-list-caret` in `projecten-custom.css`); check any other
  `dropdown-toggle` button that carries its own icon for the same doubled
  caret before shipping it.
- **Vertical alignment:** give the table (or its cells) `vertical-align:middle`.
  A status icon/badge and a 30px action button have different intrinsic
  heights; without middle-alignment a row with both looks visibly uneven even
  though neither element itself is wrong.
- **Status badges in the same row (only relevant where the older text badge
  is still used, not the icon form above):** `.badge` is em-relative by
  default (sizes off the surrounding text), which renders noticeably shorter
  than a 30px action button next to it. Give a status badge fixed (non-`em`)
  `font-size` / `padding` — see `.badge.gl-mijlpaal-status` in `traject.css`
  — rather than leaving it to inherit.
- **Row dividers use the Hairline token, never an ad-hoc lighter gray.**
  `#f1f3f7`/`#eef1f4`-style near-white grays read as *no border at all* next to
  white row backgrounds and make a table or list feel like an undifferentiated
  block of text — the opposite of scannable. Every structural row/cell
  separator is `1px solid var(--border, #e7e7e7)` (Hairline), full stop; reach
  for a paler value only for a genuine *fill* (a muted badge/pill background),
  never for a line meant to be seen.

### Table column order & alignment
- **A status indicator leads the row**, as the first column, not buried
  mid-row or at the end — it's the fastest thing a user scans a table for.
  When adding one to an existing table, move it, don't just insert it wherever
  is easiest (`ProjectTraject/Index.cshtml`'s Mijlpalen-tabel puts Status
  first; the DataTable's default-sort column index has to move with it).
- **Date, currency, and other numeric columns are right-aligned** — header
  and cells both (`class="text-end"` on the `<th>` and every `<td>`).
  Right-aligned numbers/dates line up on their ones place and scan as a
  column; left-aligned, they don't align with anything and read as prose.
- **No decorative icon riding along inside a date/number cell** (e.g. a
  calculator glyph marking "this date was computed, not entered manually").
  It breaks the column's scannability for a distinction most users don't need
  moment-to-moment; if that distinction matters, say it in the row's detail
  view/tooltip instead of every cell in the column.
- **An icon-only column keeps an accessible header, not a visible one.**
  The Mijlpalen-tabel's status column has no visible `<th>` text (the leading
  icon already says what the column is) — `<th class="text-center"><span
  class="visually-hidden">Status</span></th>`, cells also `text-center` so
  the icon centers under nothing rather than hugging the left edge. Empty a
  header visually only when the column's own content is already
  self-explanatory (an icon, a single glyph) — never to save space on a
  column whose content needs a label to be understood.
- **A DataTable's default `pageLength` can size itself to the viewport
  instead of a fixed guess.** The Mijlpalen-tabel's `pageLength` used to be a
  flat `25`; `traject.index.js`'s `fitMijlpalenPageLength()` instead measures
  a real rendered row's height and the wrapper's own chrome (toolbar + head +
  info/paging, via `wrapper`'s bounding rect minus `tbody`'s — robust to
  DataTables' exact internal markup) to compute how many rows fit between the
  table and the bottom of the viewport, then sets `page.len()` to that —
  recomputed on resize and on every tab switch (the tab-activation script
  already dispatches `resize` on show/hide, so no extra wiring needed to
  catch the panel becoming visible). Guarded with a `MP_MIN_ROWS` floor and,
  because the estimate can't account for every stray padding elsewhere on the
  page, a self-correcting fallback: if the page still ends up with a vertical
  scrollbar after the computed fit, drop rows one at a time
  (`document.documentElement.scrollHeight > window.innerHeight`) until it's
  gone, capped at a few iterations. Prefer this self-correction over trying
  to hand-derive one more precise gap constant — this session repeatedly
  found that guessing this legacy shell's exact stacked padding/margin by
  hand produces confidently-wrong numbers; measuring and correcting against
  reality is the more reliable default here.

### Table pagination controls
DataTables v2's `bs5` styling integration renders plain Bootstrap
`.pagination`/`.page-item`/`.page-link` markup (`.dt-paging` is only the
outer wrapper) — neither the CDN combined bundle nor the locally-vendored
`dataTables.bootstrap5.css` sets any color on `.page-link`, so it inherits
Bootstrap's own link-blue (`--bs-pagination-color`) untouched. Fixed on the
Mijlpalen-tabel first (`#datatable-mijlpalen_wrapper` in `traject.css`) and
the Klanten-tabel second (`#datatable-clients-list_wrapper` in
`projecten-custom.css`) — same recipe both times, and the same one
`.issue-table-footer` in `custom.css` already used, so it's one language
reused three times, not three inventions:
- **Colors:** default `.page-link` — hairline border, muted gray text (no
  blue anywhere). Hover/focus — Mist-Green background + `#bcd6c7` border +
  Forest-Green-Deep text, the same "quiet control, green on interaction"
  language as `gl-row-action-btn`. Active page — solid Deep Forest Green
  fill, white text. Disabled (prev/next at the ends) — faded muted, no hover.
- **First/Previous/Next/Last as icons, not text — `ph-caret-left`/
  `ph-caret-right` for previous/next, the real `ph-caret-double-left`/
  `ph-caret-double-right` for first/last, both tables now (Klanten-tabel and
  Mijlpalen-tabel).** Boxicons never had a verified-rendering double-chevron
  glyph, so the Mijlpalen-tabel's *original* Boxicons version faked one —
  `bx-chevron-left`/`bx-chevron-right` reused twice each, pulled tight via a
  negative `margin-left` on the second `<i>`. Dropped entirely once that
  table migrated to Phosphor; if you ever see the doubled-icon trick again
  on a not-yet-migrated page, that's what it's standing in for, not a
  considered look — replace it with the real glyph, don't carry the trick
  forward. `language.paginate` accepts HTML either way, so each value pairs
  the icon with a `visually-hidden` span carrying the original word
  ("Vorige", "Volgende", …) — screen readers get real text, sighted users
  get the icon only.
- **Every pagination button gets an explicit `height`, not just padding.** A
  digit ("1") and an icon glyph don't share an intrinsic line-height, so
  sizing purely off Bootstrap's text padding rendered the arrow/first/last
  buttons visibly shorter than the numbered ones next to them. Fixed with a
  flat `height: 36px` + `display:inline-flex; align-items:center;
  justify-content:center;` on every `.page-link` regardless of content type.

### Table search + column filter
A DataTable with a custom search box and a column-visibility ("colvis") button
— the search input replaces DataTables' own `.dt-search` (hidden via CSS), the
colvis button is moved out of the default toolbar into a page-chosen container.
Origin and current reference: `Projecten/DetailClients.cshtml` (`Clients.cshtml`).
- **Layout: colvis sits *inside* the search `.input-group`, right after the
  field, but with a small `8px` gap before it — not Bootstrap's default
  seamless input-group join.** An earlier version of this rule split them
  into a `d-flex gap-2` row (search `.input-group` + colvis in its own
  `flex-shrink-0` container beside it) reasoning that a fully fused look
  "read as unfinished" — briefly the Mijlpalen-tabel's own layout too, since
  it was built from that version of the rule. Overruled on which *box* they
  share (colvis moved back inside the same `.input-group` as the icon span
  and `<input>`, confirmed), but the gap complaint was legitimate too, so
  it's a hybrid: `#search-term + #colvis-container { margin-left: 8px; }`
  (`#mp-search-term + #mp-colvis-container` on the Mijlpalen-tabel) — one
  shared control cluster, not two separate floating boxes, but with
  breathing room between the field and the button. Bootstrap's input-group
  CSS otherwise strips the left border-radius off a `:last-child` button (it
  assumes a seamless join); since there's now a visible gap, that button
  gets its OWN right-side radius back — `border-radius: 0 var(--radius)
  var(--radius) 0` — while the *left* edge stays intentionally square (not
  fully rounded on all four corners): it still reads as "belongs to this
  row" despite the gap, not as a free-floating pill. Reference:
  `#colvis-container` rules in `projecten-custom.css`,
  `#mp-colvis-container` rules in `traject.css`.
- **DataTable init:** `layout: { topStart: { buttons: [{ extend: "colvis",
  text: '<i class="ph ph-columns me-2"></i><span>Kolommen</span>', columns:
  ":not(.noVis)", init: (api, node) => $(node).removeClass("btn-secondary")
  .addClass("btn btn-default") }] } }`, then
  `table.buttons(0, null).containers().appendTo("#<id>-colvis-container")` and
  hide `.dt-search` inside the panel. A column that must never be hideable
  (typically the actions column) gets class `noVis` on its `<th>`.
- **Assets:** the combined DataTables bundle (core + Buttons + ColVis), not
  the bare core build — `https://cdn.datatables.net/v/bs5/…/datatables.min.{js,css}`
  (see the `<script>`/`<link>` tags in `ProjectTraject/Index.cshtml` for the
  exact pinned URL) — the plain `dataTables.min.js` + `dataTables.bootstrap5.js`
  pair used elsewhere has no Buttons/ColVis support at all.

### Mobile adaptation patterns
Three reusable techniques from adapting `ProjectTraject` for phone width
(`/impeccable adapt` — see The Field-Width Rule above; these are the *how*,
that's the *why*).
- **A DataTable that might overflow its column width wraps in
  `.table-responsive`, not the whole page.** The Mijlpalen-tabel (7 columns +
  actions) had no responsive strategy at all — at phone width it would
  either force the entire page to scroll horizontally or get illegibly
  cramped. Fixed by wrapping only the `<table>` element itself in
  `<div class="table-responsive">` *before* `new DataTable(...)` runs —
  DataTables nests its own `#..._wrapper` around whatever currently sits
  where the table is, so the search/colvis row above and the pagination
  below stay full-width and only the data grid scrolls sideways. Same
  contained-horizontal-scroll idea already proven in this codebase by
  `.gl-unit-matrix-wrap` and the Kalender Gantt timeline — don't invent a
  fourth pattern (card-per-row, column-hiding) when this one already fits.
- **Hide a button's label text at a breakpoint with `font-size: 0` on the
  button, not `display:none`/`aria-hidden` on the text.** Kalender 2.0's
  4-button view-switch (Maand/Kwartaal/Jaar/Agenda, icon + bare text, no
  wrapping `<span>` around the label) had no mobile treatment at all — at
  360–400px the buttons simply clipped past the container's `overflow:
  hidden` edge, making "Agenda" physically unreachable. Fixed at ≤767.98px
  by giving the button `font-size: 0` (collapses the bare text node to zero
  visual size without touching the DOM or adding a wrapper element) while
  its `<i>` icon keeps its own explicit `font-size` and stays fully visible
  — unlike `display:none`/`aria-hidden`, the text is still in the
  accessible-name computation, so a screen reader still hears "Maand", just
  nothing is drawn for it. Pair with explicit `order` on the toolbar's flex
  children instead of trusting `flex-wrap` to split nav/title/view-switch
  predictably, and bump touch targets to the `44px` minimum this session's
  `adapt.md` reference calls for (was `34px`, fine for a mouse, tight for a
  thumb).
- **A matrix table (rows × columns, not a simple list) transposes to
  "pick one row, see its columns as a card" on mobile — not to
  cards-per-row.** `_UnitMatrix.cshtml`'s Eenheden-tab is a real matrix
  (eenheid × mijlpaal-kolom); adapt.md's usual "table → cards" advice
  (one card per row, columns become labelled fields inside it) doesn't fit
  a matrix, because the unit a user actually wants to see stays buried
  behind whichever mijlpaal-columns happen to be visible. Instead: a
  `<select>` (mobile-only, `d-md-none`) lists the units, and every unit gets
  its own server-rendered card (`hidden` except the selected one) listing
  *its* mijlpalen as a vertical list — the same clickable
  status-icon-as-button cell markup as the desktop matrix, so status-changing
  stays identical between viewports. The desktop matrix itself just gets
  `d-none d-md-block`; nothing is removed, the two views are alternate
  renders of the same data. A plain `change` listener toggles `hidden` on
  the matching card — no fetch, all units are already in the DOM.

### Formulieren (`gl-form-shell`)
The current pattern for a data-entry form. CSS in `custom.css` ("Formulierschil")
+ `projecten-custom.css` ("Projecten/Toevoegen + Bewerken — formulier"). Reference
views: `CPMCore/Views/Projecten/Toevoegen.cshtml` (short, no tabs) and
`Edit.cshtml` (long, tabbed). The single skeleton doc is
`CPMCore/Views/Projecten/FORMULIER-STRAMIEN.md`.

- **Shell (`gl-form-shell`):** one full-width `card card-modern` whose height is
  `calc(100vh − topbar − …)` so the page body never scrolls — only the active
  panel does. The form is wrapped in
  `Html.BeginForm(… @class = "ecommerce-form gl-project-form", enctype = "multipart/form-data")`.
- **Tabstrip (`gl-form-shell__tabs` / `__tab` / `__tab-badge`):** always sits
  *outside* the card — it is the screen's primary in-page navigation, not a
  widget that happens to live in one — flush under the topbar with **no
  visible gap**, full page width. `role="tablist"`; each `__tab` is `role="tab"`
  with an icon + label. Active tab = Deep Forest Green text + a 3px Ochre
  bottom border (the active-tab marker). `__tab-badge` is a Rust pill counting
  validation errors on that tab. All fields stay in the DOM on every tab — one
  Save submits everything; the show/hide + keyboard script is
  `_ProjectFormTabs.cshtml`, and on a failed POST the server sets
  `data-force-tab` to the first tab carrying an error.
  - **The breakout margin depends on which layout shell the page uses** — the
    two ancestor chains have different vertical chrome, so **never copy one
    page's numbers onto the other shell**:
    - **`.content-body`** (a plain top-level page, no project-detail sidebar —
      `Projecten/Edit.cshtml`, `TrajectSjabloonAdmin/Edit.cshtml`):
      `margin: -50px -40px 24px; padding: 0 40px; height: 54px;` — cancels
      `.content-body`'s own 40px padding-top plus the separate
      `html.modern.fixed .content-body{margin-top:10px}` rule (`theme.css`).
      Mobile: `margin: -10px -15px 16px; padding: 0 15px; height: auto;`.
    - **`.content-with-menu`** (a project sub-page with the `DetailMenu`
      sidebar — `ProjectTraject/Index.cshtml`, the reference implementation;
      `ProjectDossiers/Index.cshtml` reuses the identical markup/CSS, see
      below):
      `margin: -43px -40px 20px; padding: 0 40px; min-height: 54px;` (≥768px
      only — below that `.inner-body` falls back to a third, non-fixed padding
      recipe and the tabs scroll horizontally in-flow instead). This shell
      nests `.content-with-menu` *inside* `.content-body` (`_Layout.cshtml`
      wraps `@RenderBody()` in `<main class="content-body">` unconditionally),
      so the page must *also* neutralize `.content-body`'s own 10px
      margin-top via `html.modern.fixed .content-body.<flush-class>{margin-
      top:0}` scoped through `ViewBag.ContentBodyClass` (set in the
      controller action, consumed by `<main class="content-body
      @ViewBag.ContentBodyClass">` in `_Layout.cshtml`) — never edit the
      shared `.content-body` rule directly, it would shift every other page.
      The remaining -43px cancels `groupln.css`'s `html.modern.fixed
      [.inner-body]{border-top:113px solid transparent;margin-top:-110px}`
      stacked on its ordinary (non-fixed) 40px padding: -110 + 113 + 40 = 43.
      **Any new page reusing `.gl-traject-tabrow` must set
      `ViewBag.ContentBodyClass = "gl-traject-flush"` in its own controller
      action** — it's per-page, not inherited from the CSS class alone.
      `ProjectDossiersController.Index` originally shipped without it: the
      tabrow rendered ~10px lower than `ProjectTraject`'s (the uncancelled
      `.content-body{margin-top:10px}`), which read as "the tabbar isn't
      10px higher, like on the Traject page" — fixed by adding the same
      `ViewBag.ContentBodyClass = "gl-traject-flush"` line there too.
  - **Stays visible while scrolling — via JS, never `position: sticky`.**
    Project-wide requirement: the tabstrip pins under the topbar once the page
    scrolls past it. `position: sticky` was tried twice on both shells
    (`.content-body` *and* `.content-with-menu`) — correct-looking `top` math
    included — and in both cases it silently failed to stick at all, with a
    visible gap between the topbar and the tabstrip at rest that no amount of
    recalculating the breakout margin closed. Treat sticky as **not usable**
    on either shell; the durable fix is a small JS "affix" instead, both
    halves living in `custom.js` (project-wide, runs on every page): one IIFE
    for the bare `.gl-form-shell__tabs` used on `Projecten/Edit.cshtml` and
    `TrajectSjabloonAdmin/Edit.cshtml`, and a second, separate IIFE for the
    `.gl-traject-tabrow` wrapper (`.gl-form-shell__tabs.gl-traject-tabs`
    inside it), which additionally has the `DetailMenu` inner-menu column to
    account for — see below. **This second IIFE originally lived only in
    `traject.index.js` as `initTabrowPin()`, scoped to `ProjectTraject/
    Index.cshtml` specifically** — moved to `custom.js` and generalized to
    `document.querySelectorAll(".gl-traject-tabrow").forEach(...)` when
    `ProjectDossiers/Index.cshtml` shipped the same `.gl-traject-tabrow`
    markup without loading `traject.index.js` (it loads its own `dossiers.js`
    instead, which only handles tab click/keyboard activation, not the
    affix), silently getting no pin/flush-correction behavior at all. Any new
    page using this markup now gets the behavior automatically instead of
    needing its own copy — check `custom.js` first before writing a new one.
    Both IIFEs follow the same recipe: measure the tabstrip's real rendered position with
    `getBoundingClientRect()` and nudge it flush under the topbar via
    `transform` (self-correcting — no need to re-derive the breakout margin by
    hand), then swap to `position: fixed` (class `gl-is-pinned`) the instant a
    **synchronous** `scroll` listener (`requestAnimationFrame`-throttled) says
    the rest position would scroll past the topbar, with a same-sized spacer
    inserted so the page doesn't jump. **Never use `IntersectionObserver`
    for this** — it was tried first and fires asynchronously (~1 frame after
    the real threshold), which read as a visible "flash" before snapping to
    the correct pinned position; the synchronous scroll+rAF version has no
    such delay.
  - **`left`/width of the pinned bar:** `.gl-form-shell__tabs.gl-is-pinned`
    (bare `.content-body` pages) can hardcode `left: 300px` / `73px`
    (`html.sidebar-left-collapsed`), matching `.gl-form-shell__actions`
    exactly — no complication there. `.gl-traject-tabrow.gl-is-pinned`
    (`ProjectTraject`) **cannot** hardcode this: the page has an extra fixed
    `.inner-menu` (DetailMenu) column whose own width/offset shifts across
    `sidebar-left-sm`/`-xs` variants and the collapsed state, too many
    combinations to hardcode reliably. Its `custom.js` IIFE instead reads
    `.inner-body`'s live `getBoundingClientRect()` on every pin and on an
    `html`-class `MutationObserver` (sidebar collapse/inner-menu toggle),
    so it's always correct regardless of sidebar state.
  - **The flush-under-topbar `transform` correction is desktop-only — guard it
    explicitly, don't assume the pin logic's own `mq.matches` check covers
    it.** Both affix scripts read `--topbar-height` to compute the nudge, but
    that variable has no mobile counterpart (the mobile topbar is a separate,
    shorter 60px bar — see Layout below) and the tabstrip has no desktop-style
    breakout margin at that width either. The first version of this fix
    applied `measureRest()`'s transform unconditionally; on
    `ProjectTraject/Index.cshtml` specifically that yanked the tabstrip
    sharply upward on phones, landing it on top of the topbar-actions row
    rendered above it (a real, screenshotted regression, not a hypothetical).
    Fixed by returning out of the transform step before it runs whenever
    `!mq.matches` — the pin/unpin logic already had this guard, the
    measurement step didn't, and that mismatch was enough to break it.
  - **Compact stats sharing the tabstrip row don't fully hide on narrow
    screens — they thin out.** `ProjectTraject/Index.cshtml` puts a small KPI
    strip (`.gl-traject-stats`, e.g. "8/38 bereikt · 1 achterstallig") beside
    the tabs on the same row. Below 992px, only the *secondary* stats
    (`.gl-traject-stat--sec`) disappear; the count that actually drives a
    decision (here: achterstallig) always stays visible, even on a phone.
    Hiding the whole strip was tried first and hid exactly the number a PM on
    a jobsite tablet opens the page to check — a direct conflict with The
    Field-Width Rule ("on-site phone use is a first-class case"). When adding
    stats to a tabstrip row, mark the merely-nice-to-have ones `--sec` and
    keep the one number someone would actually act on.
- **Panels (`gl-form-shell__panel`):** `overflow-y:auto`, 24px padding;
  non-active panels carry `hidden`.
- **Actions (`gl-form-shell__actions`):** a third, always-visible zone rendered
  by `_FormShellActions.cshtml`
  (`FormShellActionsModel { SubmitLabel, SubmitIcon = "bx-save", CancelUrl, CancelLabel = "Annuleren" }`).
  `position: fixed` to the viewport bottom, left-aligned next to the sidebar
  (`left: 300px` / `73px` collapsed / `0` ≤767px), respecting
  `env(safe-area-inset-bottom)`. Buttons use the large app size
  (`btn-px-4 py-3`, `submit-button` / `cancel-button`), not the topbar size.
- **`gl-form-shell__actions` outside a `gl-form-shell` (`.gl-fixed-actions-offset`):**
  `Projecten/AddContract.cshtml` / `EditContract.cshtml` want the same fixed
  Save/Cancel bar but have no tab shell to sit inside — a plain long
  `<form>` instead. `gl-form-shell` normally reserves room for the 84px bar
  itself (see above); without that shell, the page's own content would
  scroll the last fields *behind* the fixed bar. Add class
  `.gl-fixed-actions-offset` directly to the `<form>` — it adds
  `padding-bottom: calc(96px + env(safe-area-inset-bottom, 0px))`, matching
  the bar's own height/safe-area so the last field always clears it. Below
  992px the bar itself stops being fixed (mirrors the phone action-buttons
  pattern: full-width stacked buttons, `position: static`), so
  `.gl-fixed-actions-offset` cancels its own padding-bottom there too — the
  bar no longer needs the reservation once it's back in normal flow.
- **Section (`gl-form-section` + `__body`):** a block inside a panel.
  `gl-form-section__head` is a flex row — a **42px** Mist-Green rounded (≈`md`,
  11px) icon badge + `__title` (700, 1.0625rem) + `__hint` (0.8125rem, muted),
  Hairline under it. Reference: `Projecten/Edit.cshtml`'s "Status &
  publicatie" / "Website-presentatie" / "SEO" sections. **The badge icon
  names the section's own subject** (`bx-flag` for status, `bx-edit-alt` for
  the website-presentation section, `bx-search-alt` for SEO) — same
  matches-the-subject convention as the topbar icon (see "Topbar" above) and
  the tab icons on the same page, not a generic "form section" glyph reused
  everywhere.
- **Field grid (`gl-field-grid`):**
  `display:grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 2px 24px`
  — resolves to 1 / 2 / 3 columns by width on its own.
- **Field (`gl-field`):** flex column, label above the control, 6px gap, 18px
  bottom margin. `gl-field--full` spans the whole grid row (control capped at
  560px); add `gl-field--wide` to let a textarea / rich-text control fill 100%.
  `gl-req` is the Rust `*` after a required label. `gl-field-address` puts street
  (`flex:1`) + number (`gl-nr`, 96px fixed) on one line. `gl-switch-row` lays an
  iOS switch beside its label; `gl-form-note` is a Mist-Green hint box inside a
  section.
- **Shared partials:** `_ProjectFormCoreFields.cshtml` renders Naam / Projectcode
  / Land / Gemeente / Verantwoordelijke as loose `.gl-field`s for callers to drop
  into their own grid; `_ProjectFormStyles.cshtml` is the fixed `<link>` set.
- **Legacy / anti-reference:** `Projecten/AddContact.cshtml` +
  `EditContact.cshtml` still use a bare `card card-modern` + `row g-3` +
  `col-md-6` + `form-label` + plain `form-control`. That is the pattern to
  *replace*, not copy — migrate those views to this section when touched.

**The Form-Shell Rule.** A new data-entry form is a `gl-form-shell` with
`gl-form-section` heads and a `gl-field-grid` of `gl-field`s (label above the
control), and its Save / Cancel come from `_FormShellActions`. `card-big-info`
(labels right of the control) is kept only for the existing two-zone explanatory
supplier/contract forms. Never start a new form on bare `card-modern` +
`row g-3` + `form-label` + `form-control`.

### Modals
- **Always vertically centered.** Every `.modal-dialog` carries
  `modal-dialog-centered` — never Bootstrap's default top-anchored dialog,
  which floats near the top of the viewport and reads off-balance on a tall
  screen. `Views/Shared/_ConfirmModal.cshtml` already does this; several older
  modals across the app still don't. Add it the next time one of those is
  touched, not as a bulk sweep.
- **Confirm dialogs never use the bare browser `confirm()`.** Use
  `Views/Shared/_ConfirmModal.cshtml` (`@await Html.PartialAsync("_ConfirmModal")`,
  once per page, already `modal-dialog-centered`) with `~/js/confirm-modal.js`'s
  `window.confirmDialog(title, bodyHtml, confirmLabel)` → `Promise<boolean>`.
  `bodyHtml` may hold small inline markup (`<b>`) but never unescaped user
  input. Wire the calling form/button with a `data-confirmed` guard so the
  real submit passes through once the promise resolves `true` — see
  `ProjectTraject/_TrajectActionButtons.cshtml` (`gl-delete-traject-form`) or
  `TrajectSjabloonAdmin/Index.cshtml` (`gl-delete-sjabloon-form`) for the exact
  pattern, including how it stays safe when the same form is rendered twice
  (topbar + mobile fallback).
- **A commit modal that triggers side effects discloses them before submit,
  not just in a read-only view elsewhere.** `ProjectTraject`'s mijlpaal
  status-change modal can, depending on which status is picked, silently
  cascade into automation (create a dossier, flip the project status, unlock
  the next fase) configured on that mijlpaal — the modal itself only showed
  Status/Datum/Opmerking, and the trigger list was visible only in the
  *read-only* Kalender detail panel, a path most status changes never go
  through. Fixed via `.gl-mps-triggers` (`Modals/_ModalMijlpaalStatus.cshtml`
  + `traject.index.js`'s `renderMpsTriggers()`): a small Mist-Green panel
  inside the modal body, populated from the same trigger data the Kalender
  already computes, labelled per trigger event ("Bij bereiken", "Bij
  statuswijziging", …). Hidden entirely when the mijlpaal has no configured
  triggers. The general rule: if committing a form can do something beyond
  what the form's own fields describe, say so inside that form, not only in
  a preview screen the user may never open.
- **A modal with more than ~6 fields gets chunked into labelled sections, the
  same instinct as `gl-form-section` on a full page — just compressed for a
  modal's footprint.** `Modals/_ModalMijlpaalUpsert.cshtml`'s 11 fields used
  to sit in one flat `row g-3` with no grouping; split into three
  `.gl-mp-modal-section`s (Mijlpaal / Planning / Verantwoordelijkheid &
  notities — `traject.css`), each just a muted 600/.78rem title
  (`.gl-mp-modal-section-title`, same label typography as everywhere else)
  over a hairline divider between sections. Don't reach for a full
  `gl-form-section` head (icon badge + hint text) inside a modal — that's
  sized for a page, not a dialog; the plain title + divider is the modal-scale
  equivalent.

### Navigation (sidebar)
- **Rail:** solid Deep Forest Green (`#0a5a3b`), fixed full height, 300px /
  73px collapsed.
- **Tiles:** column layout (icon over label), centred, 14px radius, `14px 10px`
  padding, 30px icon, 12px/600 Mint-Ice label, `6px` gap between tiles.
- **Default / hover:** transparent tile, Mint-Ice icon + label, no underline.
- **Active:** Forest Green Bright (`#0f7a52`) fill, near-white text, no left bar,
  no caret — the block itself is the indicator.
- **Flyout submenu:** absolutely positioned to the right of the rail (≥992px),
  Forest Green Deep (`#0f4b40`) panel, 16px radius, Overlay shadow, 1–2 columns,
  min 360–430px; child links 14px/500 Mint-Ice, 8px radius, hover
  `rgba(231,255,241,0.14)`. Falls inline single-column ≤991px.

### Topbar
- **Bar:** 72px, Surface White, 1px Divider-Cool bottom border, no shadow.
- **Page icon:** 40px Mist-Green rounded (10px) chip with a Deep Forest Green
  glyph, left of the title.
- **Title:** Slate-900, 700; 1.05rem with a breadcrumb, 1.5rem (line-height 40px)
  without one.
- **Breadcrumb:** Slate-500, 0.78rem, ` / ` separators (`#cbd5e1`), links hover
  to Deep Forest Green.
- **Userbox:** right-aligned; 10px-radius toggle (hover Cloud Grey), 36px round
  avatar (Deep Forest Green fill, white initials, or photo); dropdown is a
  230px-min white menu, 10px radius, Divider-Cool border, Overlay shadow, items
  7px radius with Mist-Green hover + green text/icon.
- **Mobile (≤767px):** whole bar becomes 60px solid Deep Forest Green; only
  hamburger, centred title, avatar.
- **Page actions (`@section PageActions` → `.topbar-page-actions`):**
  page-specific buttons rendered inside the topbar itself — right-aligned
  (`margin-left:auto`), `gap:10px`, right of the breadcrumb. Distinct from
  `gl-page-header__actions` below, which sits in the content body, not the bar.
  **Always `btn-sm`** — the bar is only 72px tall and the app's default/`btn-md`
  sizing reads oversized against it (reference: `ProjectTraject/Index.cshtml` +
  `_TrajectActionButtons.cshtml`, `TrajectSjabloonAdmin/Edit.cshtml` +
  `_TrajectSjabloonActions.cshtml`). `.topbar-page-actions` is hidden entirely
  below 768px; the calling page repeats the *same* buttons — via the same
  partial, never duplicated markup — in a `d-flex d-md-none justify-content-end
  gap-2 mb-3` fallback row at the top of its own content body.
  `Projecten/DetailContracts.cshtml` used to be the standing example of a page
  that still hadn't migrated to `btn-sm` here — fixed when that page was
  brought in line with DESIGN.md; if you find another one, migrate it the
  same way when you touch it, not in a bulk sweep.

**The Content Body Has No Page Chrome Rule.** A page's title and its
page-level actions never live in the content body — full stop, no exceptions
for "just this once, it fits better here." Where they go depends on what kind
of page it is:
- **An ordinary content page** (a list, a detail view, anything that isn't a
  data-entry form): title + icon come from `SetPageHeader(icon, title)`
  (`BaseController`, sets `ViewData["PageIcon"]`/`["Title"]`, which the topbar
  in `_Layout.cshtml` renders) — never a local `<h1>`. Actions come from
  `@section PageActions` → `.topbar-page-actions` (see above), with the same
  `d-md-none` fallback-row duplication for `<768px`.
- **A form** (`gl-form-shell`): title lives in the `gl-form-section__head`,
  actions in the sticky `gl-form-shell__actions` bottom bar — already the
  established pattern, unchanged by this rule.
- **A subtitle has nowhere to go and is simply dropped, not relocated.** The
  topbar has no subtitle slot (title + breadcrumb only). A subtitle in the
  content body underneath a topbar title would just restate context the
  breadcrumb already gives — see `ProjectTraject/Index.cshtml`'s own comment
  on this exact point, which predates this rule and already got it right by
  instinct: *"de topbar toont icoon + titel al; een lokale h1/h5 zou dat
  woordelijk herhalen."*
- **Reference migration:** `Projecten/DetailClients.cshtml` — was a
  content-body `<h1>`/`<h5>` + `btn-group`; now `SetPageHeader("ph ph-users",
  "Klanten")` in `ProjectenController.DetailClients` + `@section PageActions`
  rendering the shared `Partials/_ClientsActionButtons.cshtml` (also reused
  for the `d-md-none` mobile fallback row) — subtitle ("Beheer de klanten van
  het project") dropped per the rule above, not relocated.

### Paginakop (`gl-page-header`) — superseded, legacy only
**Do not use this for a new page or when migrating one you touch.** It
predates The Content Body Has No Page Chrome Rule above and put title +
subtitle + actions in the content body, which is exactly what that rule now
forbids — `gl-page-header__actions` in particular duplicated
`.topbar-page-actions` badly (default/`btn-md` sizing instead of `btn-sm`,
a second place page actions could live). Still in real use on a handful of
pages (`Projecten/AddContract.cshtml`, `Projecten/Detail.cshtml`,
`Projecten/BudgetIndex.cshtml`, `TrajectSjabloonAdmin/Index.cshtml` — kept
here only so those remain documented, not as guidance to extend), so the
shape is recorded for reference, not as a pattern to reach for:
- **Structure:** one flex row (`gl-page-header`, `justify-content: space-between`,
  wraps below 576px) with two zones — `gl-page-header__left` (back button +
  title + subtitle, rendered by the shared partial `Views/Shared/_PageHeader.cshtml`
  with `Models.PageHeaderModel { Title, Subtitle?, BackUrl?, BackAriaLabel? }`)
  and `gl-page-header__actions` (page-specific buttons, default/`btn-md` size).
- **Back button:** 40×40px icon button (`gl-page-header__back`, The Field-Width
  Rule), `bx-chevron-left` at 1.5rem, Muted by default, Mist-Green background +
  Deep Forest Green icon on hover/focus — same interaction language as the
  dashboard icon buttons.
- **Migrate a page off this when you touch it** (not a bulk sweep, same as
  every other legacy-pattern migration in this doc): move the title to
  `SetPageHeader`, drop the subtitle, move the actions into
  `@section PageActions` (resized to `btn-sm`) + the `d-md-none` mobile
  fallback. `Projecten/DetailClients.cshtml` is the worked example.

### Badges
- Solid fills mapped to tokens: `bg-primary` → Deep Forest Green,
  `bg-secondary` → Sage, `bg-info` → Taupe Grey, `bg-accent` → Timber, plus
  standard success/warning/danger/dark/light. White text except warning/light
  (black). Pill radius.

### Icons — migrating Boxicons → Phosphor (in progress, page by page)
The app is mid-migration from Boxicons to [Phosphor](https://phosphoricons.com/)
— **not a bulk sweep**: a page's icons convert to Phosphor the next time that
page is touched, same discipline as every other legacy-pattern migration in
this doc. Both icon fonts stay loaded (`_Layout.cshtml`) until the migration
is complete, so an unconverted page's `bx-*` classes keep working untouched.
Converted so far: `Projecten/DetailClients.cshtml` + `Partials/Clients.cshtml`
(the first page, treat as the reference) and the whole `ProjectTraject`
feature (`Index.cshtml`, `_Timeline.cshtml`, `_UnitMatrix.cshtml`,
`_Kalender.cshtml`, `_TrajectActionButtons.cshtml`,
`Modals/_ModalMijlpaalStatus.cshtml`, `traject.index.js`,
`ProjectTrajectController.cs`'s `SetPageHeader` call) — the second full page,
and the one that added most of the dictionary entries below. Third:
`Projecten/DetailContracts.cshtml` + `Partials/Contracts.cshtml` (plus its
`ProjectenController.cs`'s `SetPageHeader` call) — the richest table of the
three (up to 5 row actions, expandable child rows built client-side), see
its own callout below for what that surfaced. Shared partials
a migrated page merely *calls* (e.g. `Views/Shared/_ModalTaakQuickAdd.cshtml`,
used by `ProjectTraject` among others) are **not** part of that page's
migration — they stay on Boxicons until a page migration specifically
touches them, same "not a bulk sweep" discipline, just one level removed.
- **Weight: Regular, not Thin.** Phosphor ships six weights (Thin, Light,
  Regular, Bold, Fill, Duotone). Thin's hairline stroke is drawn for large
  display use; most of this app's icons render at 14–18px (row-action
  buttons, table cells, small toolbar buttons), where a hairline stroke loses
  legibility and reads as washed-out next to Poppins 600/700, the weight this
  app's type leans on throughout (The Weight-Not-Size Rule). Regular is the
  closest match to Boxicons' existing stroke weight, so the icon-set swap
  doesn't also silently shift the app's visual weight. Reach for Bold only if
  a specific icon needs more presence than Regular gives it at its actual
  render size — not as a blanket choice.
- **Loading:** the full official web-font bundle via jsDelivr
  (`https://cdn.jsdelivr.net/npm/@phosphor-icons/web@2.1.2/src/regular/style.css`,
  pin the version), **not a subset build.** This app already got burned once
  by Boxicons' CDN "basic" subset silently missing glyphs (see the
  `bx-transfer` incident under Table status indicators below) — Phosphor's
  jsDelivr bundle ships the complete regular-weight set (1,500+ icons,
  verified directly against the downloaded CSS+font before adopting it), so
  that specific failure mode doesn't recur. Markup is two classes:
  `<i class="ph ph-{icon-name}"></i>` (`ph` = weight class for Regular,
  `ph-{name}` = the glyph) — a different weight needs its own additional
  stylesheet link plus its own weight class (`ph-bold`, `ph-thin`, …); this
  app only loads Regular.
- **One glyph per meaning, everywhere — a semantic dictionary, not a
  per-page choice.** The whole point of migrating is that "edit" is always
  the same icon; picking a different reasonable-looking pencil on each page
  defeats it. Confirmed mapping so far (extend this table as new pages
  migrate, don't invent a second glyph for a meaning already listed here):

  | Meaning | Class |
  |---|---|
  | Search | `ph-magnifying-glass` |
  | Edit | `ph-note-pencil` |
  | Delete | `ph-trash` |
  | Add / create | `ph-plus` |
  | Columns (colvis) | `ph-columns` |
  | Print | `ph-printer` |
  | Excel / spreadsheet export | `ph-file-xls` |
  | Pagination previous / next | `ph-caret-left` / `ph-caret-right` |
  | Pagination first / last | `ph-caret-double-left` / `ph-caret-double-right` — a real icon, unlike Boxicons (see Table pagination controls) |
  | People / clients | `ph-users` |
  | Confirm / check | `ph-check` |
  | Status: Open | `ph-circle` |
  | Status: Bezig / in progress | `ph-clock` |
  | Status: Bereikt / done (outline form) | `ph-check-circle` — the filled-circle *table* exception still uses bare `ph-check`, see Table status indicators |
  | Status: Niet van toepassing | `ph-minus-circle` |
  | Status: Geblokkeerd | `ph-prohibit` |
  | Warning / linked-items badge | `ph-warning-circle` |
  | Change status (action, distinct from any status glyph above) | `ph-repeat` |
  | Sync / refresh from source | `ph-arrows-clockwise` — deliberately not `ph-repeat`, which already means "change status" in this feature; don't reuse one action glyph for two different actions in the same feature |
  | Automation trigger indicator | `ph-lightning` |
  | Jump to today | `ph-target` |
  | Filter / options | `ph-funnel` |
  | Task / checklist | `ph-list-checks` |
  | Grid / matrix view | `ph-grid-four` |
  | Timeline / sequence of steps | `ph-flow-arrow` |
  | Flag / milestone | `ph-flag` |
  | List view | `ph-list` |
  | Branch / process start | `ph-git-branch` |
  | Suppliers / contractors (topbar) | `ph-hard-hat` |
- **The topbar page-icon (`SetPageHeader`'s first argument) matches the page's
  own subject, not a generic folder/file glyph — and the empty-state icon on
  that same page matches the topbar icon too, not a separate choice.**
  `ph-users` for Klanten, `ph-hard-hat` for Leveranciers (construction
  subcontractors — deliberately not the more generic `ph-buildings`, since
  this app's own domain is construction, see PRODUCT.md). Established on
  `DetailClients`/`DetailContracts`; carry the same discipline into the next
  page migrated — check `SetPageHeader`'s icon argument for a subject-specific
  choice, don't default to whatever the old `bx-*` mapped to literally.
- **Migrating `DetailContracts` surfaced two real, pre-existing bugs unrelated
  to icons — fix them when you find them mid-migration, they're not optional
  polish.** (1) The Leveranciers-tabel's DataTable `language` object never
  overrode `paginate` at all, so it showed DataTables' hardcoded default
  *English* words ("First"/"Previous"/…) — not stale Dutch, no Dutch ever
  existed there. Added the same icon-based `paginate` block the other two
  tables use. (2) `.js-add-bijbestelling` was a bare `<a href="#">` on this
  page specifically, while its two other usages
  (`Projecten/DetailContract.cshtml`, `EditContract.cshtml`) already use
  `<button type="button">` — the shared click handler
  (`_BijbestellingModal.cshtml`) doesn't care which, so this was a silent
  inconsistency with no functional symptom. Made it a `<button>` here too,
  matching its own established convention elsewhere. Also cleaned up a
  leftover dead CSS rule (`#datatable-clients-list_wrapper .page-link i + i`)
  from before the Klanten-tabel had real double-caret icons — a reminder to
  actually delete a workaround's CSS when removing the workaround, not just
  the markup that used it.
- **A hidden legacy script rewrites icons inside any table's "Acties" column
  — it needs to recognize `ph-*` too, or it silently destroys them.**
  `custom.js` (global, every page) finds every table with a header cell
  reading exactly "Acties" and runs each icon in that column through
  `replaceWithBoxIcon()`: if the icon doesn't already start with `bx`, it
  looks for an `fa-` class to remap to a `bx-*` equivalent, and — critically
  — **falls back to a generic `bx-dots-horizontal-rounded` (three dots) icon
  with no warning if it finds neither.** A freshly-migrated `ph-note-pencil`/
  `ph-trash` matches neither condition, so every Acties-column icon on
  `Projecten/DetailClients.cshtml` silently became three dots the first time
  this was tested live — not a CSS problem, a JS one, and easy to miss
  because nothing errors. Fixed by teaching `replaceWithBoxIcon` that
  `ph`/`ph-*` also counts as "already a real icon, leave it alone" (same
  treatment `bx-*` already got). **Whenever migrating a page whose table has
  an "Acties" column header, verify the row-action icons after
  migrating — this script runs globally and its default assumption (`bx-*`
  is a real icon, other icon fonts aren't) is now half wrong.**

### Progress bars (signature — `gl-pg-bar`)
- 4px tall, 2px radius, `#e9ecef` track. Fill is a fixed left-to-right gradient
  `#d1d5db → #6b8f80 → #0a5a3b` (low → mid → high) clipped by width, so the same
  bar communicates *how far along* and *how good* at once. Standalone
  `gl-pg-bar-laag/midden/hoog` classes give the three solid stops.

### Rol-dashboard componentbibliotheek
All CSS below lives in `CPMCore/wwwroot/css/dashboard-projectleider.css` —
the filename is a historical accident (first built for the Projectleider
dashboard) but every class in it is generic and shared by all three role
dashboards (Projectleider, CeoCfo, Boekhouding) rendered from `Home/Index`.
Treat the file as the dashboard component library, not a Projectleider-only
stylesheet; extend it there rather than forking a per-role copy.

**KPI-strip (`gl-kpi-strip`)**
- Row of `card-featured-left` cards (Bootstrap admin-theme component), one
  icon + number per tile via `widget-summary`/`summary-icon`.
- Established ratio across all three dashboards: 2 neutral tiles
  (`card-featured-primary`, `bg-primary` icon — portfolio-scale counts) + 2
  severity tiles (`card-featured-danger`/`card-featured-warning` border,
  `gl-kpi-icon-danger`/`gl-kpi-icon-warning` icon fill,
  `gl-kpi-amount-danger`/`gl-kpi-amount-warning` text colour). Not a hard
  rule, but breaking it on a new dashboard should be a deliberate choice, not
  an accident.
- Hidden entirely below 768px (`custom.css`, `.gl-kpi-strip { display:none }`
  under `max-width:767.98px`) — mobile keeps the dashboard chrome minimal.

**Aandachtspaneel / meldingencentrum (`gl-mc-*`)**
- Card with three severity-named groups, always in this order: `gl-mc-urgent`
  ("ACTIE VEREIST", `--danger-tint`/`--danger-text`), `gl-mc-normal` ("OP TE
  LOSSEN"/"TE VERWERKEN", `--warning-tint`/`--warning-text`), `gl-mc-info`
  (collapsible, `--info-tint`/`--info-text` — Taupe Grey, deliberately never
  blue). Each `gl-mc-item` is icon + text + a `gl-mc-btn-bekijk` deep link;
  Projectleider's construction-meldingen additionally get a snooze button
  (`gl-mc-btn-snooze`).
- Sticky on desktop (`gl-mc-col`, ≥992px, offset `var(--topbar-height) + 10px`)
  when the panel sits beside a tall scrolling grid (Projectleider, CeoCfo).
  Boekhouding has no grid beside it, so it opts out via the `gl-mc-body-static`
  modifier (removes the artificial `max-height`/scroll and lets the card grow
  with its content instead).
- Empty state: `gl-mc-empty`, a muted check-circle + "niets dat aandacht
  vraagt"-style copy — always show this rather than an empty card body.

**Werf-kaart grid (`gl-werf-*`)**
- Card: fixed 250px photo (`gl-werf-foto`) with a bottom-gradient overlay
  (`gl-werf-overlay`), a status chip top-right (`gl-status-chip` +
  `sc-groen`/`sc-geel`/`sc-rood`/`sc-donker`), an optional warning badge
  bottom-left (`gl-warn-badge`, shown when voortgang flags a warning) and,
  CeoCfo-only, a company badge top-left (`gl-company-chip`, since that grid
  spans every issuer company). Body: name, gemeente, the two
  fysiek/financieel `gl-pg-bar` rows, and a footer with delivery countdown or
  "Opgeleverd op …".
- `.gl-werf-col` is a flex column (not `height:100%` on the card) specifically
  so an optional fixed-height header — the drag/arrange bar below — and the
  card can split a `align-items:stretch`-assigned row height correctly; see
  the comment at the top of that rule before changing either.
- **Rangschikken (drag-to-reorder)** — Projectleider's "Mijn Werven" only.
  `gl-arrange-toggle` switches the grid into arrange mode; each card gets a
  `gl-arrange-bar` (drag grip `gl-drag-grip` + `gl-arrange-btn` up/down
  buttons, both real keyboard-operable controls, not drag-only). Dragging
  uses jQuery UI Sortable with a **cloned** helper appended to `<body>`
  (`.gl-werf-col.ui-sortable-helper`, `z-index:3000`) rather than the
  original node — the original's width comes from Bootstrap column
  percentages and this dashboard's sticky/relative ancestors, which fights a
  naive `position:absolute` drag. `gl-werf-placeholder` marks the drop slot.
  Pin toggle (`gl-pin-toggle`, "vastgezet" projects outside a PM's own
  assignment) is a sibling of the card, not nested inside its `<a>`.

**Snelacties (`gl-snelactie*`, `gl-sa-*`)**
- Two item shapes: `gl-snelactie` (accordion trigger, expands a
  `gl-sa-submenu` of `gl-sa-subitem` deep links — used when the action needs
  a project picked first) and `gl-snelactie-direct` (a plain link/button for
  an action needing no per-project context).
- Projectleider additionally ships a phone-only bottom nav
  (`custom.css`, `.gl-mob-nav`, `d-md-none`) as a thumb-reachable subset of
  the same actions; CeoCfo/Boekhouding rely on the Snelacties card alone
  (`d-none d-md-block` — hidden only below 768px, not below 992px, so tablets
  keep the full action set).

### Projecthub-componentbibliotheek (`Projecten/Detail`)
All CSS lives in `CPMCore/wwwroot/css/projecten-custom.css` (the page also
links `dashboard-projectleider.css` to reuse `gl-mc-*`). The detail page is a
single-project hub: **Operate** mode — scan project state, then jump into a
sub-area. Motion here serves feedback/state only; no page-load choreography.

**Hero-blok — twee rijen van twee kaarten (`gl-detail-hero-row-1/-2`, `gl-detail-hero-cell`)**
- Rij 1: projectfoto (`col-xl-4`) + "Aandacht vereist" (`col-xl-8`). Rij 2:
  "Algemene gegevens" (`col-xl-4`) + "Eenheden & verkoopstatus" (`col-xl-8`).
- **Gelijke hoogte per rij, puur CSS, geen JS-meting.** Rij 1 heeft ≥xl een
  *vaste* hoogte (420px) omdat de foto geen eigen inhoudshoogte heeft en de
  meldingenlijst anders wegrent. Rij 2 laat **"Algemene gegevens" de hoogte
  bepalen** (toont altijd al haar rijen, scrollt nooit); de eenheden-tabel
  staat in een `gl-detail-units-scroll`-wrapper met de scrollzone
  `position:absolute; inset:0`, zodat de tabel géén hoogte aan de flow
  toevoegt en de kaart nooit hoger wordt dan de buurkaart. Bootstrap's
  `align-items:stretch` trekt hem dan naar diezelfde hoogte; de tabel scrollt.
  Dezelfde absolute-uit-de-flow-truc als de projectfoto (`gl-detail-photo`
  `position:absolute; inset:0` met de edit/verwijder-knoppen erbovenop).
- Onder xl stapelen de kaarten op natuurlijke hoogte; de scrollzones vallen
  terug op een gewone `max-height`.
- Een eerdere JS-hoogtesynchronisatie (`syncHeroColumnHeight`) is bewust
  verwijderd — die veroorzaakte telkens "grote witruimte onder een kaart".

**KPI-strip (`gl-kpi2-*`)** — een *aparte* variant van het dashboard
`gl-kpi-strip`, niet dezelfde component.
- 7 tegels (op uitdrukkelijke gebruikerskeuze), flex-wrap met `flex:1 1 150px`.
  Elke tegel is een `<a>` naar het bijhorende onderdeel met een echte
  `aria-label`. De 3 minst dringende tegels krijgen `gl-kpi2-tile-col--sec` en
  vallen weg onder 576px zodat de kern zichtbaar blijft.
- Rand = hairline (`--border`); het *icoon* draagt de kleur — `--primary`
  standaard, `--danger` alleen wanneer de tegel een probleem meldt (Werkdagen
  te laat, Open punten > 0). Geen andere accentkleuren (One Green Rule).
- `gl-kpi2-ring` = donut-icoon via `conic-gradient(var(--kpi-color) calc(var(--pct)*1%), …)`.
  `--pct` is als `@property <number>` geregistreerd zodat de ring bij het
  eerste zien naar zijn waarde veegt (JS zet 'm even op 0 en terug).

**Voortgang & budget-balken (`gl-vb-*`)** — dashboard-`gl-pg-bar` is een
gradient-in-één-balk; dit is een aparte set van vier gelabelde balken in één
kaart. Eén kleur per maatstaf, allemaal **systeemtokens**:
Fysiek = `--primary`, Financieel = `--custom-accent`, Verkocht = `--secondary`,
Budget besteed = `--info`; `gl-vb-over` (budget > 100%) wisselt naar `--warning`
als semantisch signaal. Track `rgba(0,0,0,.07)`, fill `border-radius:999px`.
Bij mount vullen de vier balken links→rechts (`@keyframes` `scaleX(0→1)`,
0,6s ease-out, eenmalig).

**Sleutel/waarde-lijst "Algemene gegevens" (`gl-detail-kv`)**
- `gl-detail-kv-row` = grid `20px 116px 1fr` (icoon | label | waarde), hairline
  tussen de rijen. Icoon `--primary`, label `--gl-detail-text-aa`, waarde
  `--ink`/700. Onder 420px valt de labelkolom weg (`grid-template-columns: 20px 1fr`).

**Kaart-header actielink (`gl-detail-card-header` + `gl-detail-card-edit`)**
- Elke hub-kaart heeft dezelfde header: titel links (klikbaar naar de
  volledige pagina), rechts een pill `gl-detail-card-edit` (bx-icoon + label,
  ≥40px tikgebied, hover = `--lightgreen`). Gebruikt op alle zes de kaarten —
  Bewerken / Facturatieblad / Nacalculatie / Alle documenten / Alle foto's /
  Alle eenheden. Nieuwe kaarten volgen dit, geen ad-hoc `text-muted small`-link.

**Documentrij (`gl-doc-item`) & meldingsrij (`gl-mc-item` op deze pagina)**
- **De hele rij is de link.** `gl-doc-item` en (detail-scoped) `gl-mc-item`
  zijn een `<a href>` i.p.v. een `<div>` met een geneste link; de "Open" / de
  "Bekijk ›" is nog enkel een visueel label (`<span>`). Zo werkt middenklik /
  openen-in-nieuw-tabblad en is de rij toetsenbord-focusbaar (focusring
  `outline-offset:-2px`). Hover: lichte achtergrond (`--lightgreen`) resp.
  `filter:brightness(.97)` op de getinte meldingsrij.
- Bestandstype-badge `gl-doc-badge` (34px, `--radius`): PDF `--danger`,
  Word `--custom-accent`, Excel `--primary`, CAD `--warning`, beeld `--info`.

**Deelpagina-chiprij (`gl-detail-subnav`)** — een quiet wrappende rij pill-links
naar alle deelpagina's, **enkel < 768px** zichtbaar (`@media (min-width:768px){display:none}`),
waar het `DetailMenu` (inner-menu) ingeklapt zit. Samen met `gl-detail-mobile-title`
(projectnaam in de body, ook enkel < 768px) de oriëntatie op de telefoon.

**Beweging** — één geauthoreerd moment (de vier `gl-vb`-balken + de twee
`gl-kpi2`-ringen die bij mount naar hun waarde bewegen); de rest is
≤150ms hover/press-bevestiging op wat aanklikbaar is (KPI-tegels 1px lift,
`gl-detail-thumb-*` foto-knoppen scale 1.09/0.93 + icoon 1.12, mediaminiaturen
1.04). Alles heeft een `prefers-reduced-motion`-pad dat de beweging weglaat maar
kleur/toestand behoudt.

**Coachmark-tour** — `SequenceKey = "Projects.Detail.Redesign.Tour"` in
`CoachmarkRegistry.cs` (PageKey `Projects.Detail`, gezet via
`ViewData["CoachmarkPageKey"]`): 4 stappen — topbar-acties (verplaatst),
klikbare KPI-strip, "Voortgang & budget"-kaart, "Aandacht vereist".

## Do's and Don'ts

### Do:
- **Do** make Deep Forest Green (`#0a5a3b`) the only brand colour on a content
  screen; keep it to ~one mark per control cluster (The One Green Rule).
- **Do** build type hierarchy by weight (400 / 600 / 700 Poppins) at the existing
  sizes, not by new sizes or a second family.
- **Do** separate surfaces with the 1px Hairline (`#e7e7e7`) border and the
  Card-rest shadow (`0 1px 4px rgba(0,0,0,.08)`); reserve Overlay shadows for
  menus, dropdowns, and modals (The Float-Only Rule).
- **Do** use 7px radius for content chrome and 14–16px for navigation chrome
  (The 7-14-16 Rule).
- **Do** back "green-related" affordances — icon chips, tab-active rows, hovers —
  with Mist Green (`#e8f0eb`), and use Mint Ice (`#e7fff1`) as the only text
  colour on the green rail.
- **Do** keep every screen usable at 360px with ≥40px touch targets; on-site
  phone use is first-class (The Field-Width Rule).
- **Do** prefer soft tinted state colours (e.g. `btn-gl-remove`) over solid loud
  fills, in keeping with the restrained tone.
- **Do** use Rust / Ochre / Taupe Grey for danger / warning / info severity —
  never stock Bootstrap red (`#dc3545`-family), amber, or blue.
- **Do** build a new data-entry form as a `gl-form-shell` with
  `gl-form-section` heads and a `gl-field-grid` of `gl-field`s (label above the
  control); render Save / Cancel with `_FormShellActions` (The Form-Shell Rule).

### Don't:
- **Don't** introduce cool blue-greys for neutrals; keep them warm — Page Grey
  and Hairline before any `#f1f5f9`-family grey (The Warm-Grey Rule).
- **Don't** put a visible drop shadow on a resting card to signal importance; use
  the border and, if needed, a Mist-Green header.
- **Don't** add a second brand accent, gradient wash, uppercase tracking, or
  italic for emphasis — reach for weight or green.
- **Don't** give navigation a left active-bar or caret; the Forest-Green-Bright
  tile block is the indicator.
- **Don't** let radius drift — no pill buttons by default, no sharp 0px corners,
  no 4px/12px one-offs outside the documented scale.
- **Don't** restyle Select2 / datepicker / multiselect controls away from the
  46px `form-control-modern` height; native and enhanced fields must stay aligned.
- **Don't** start a new form on a bare `card-modern` + `row g-3` + `col-md-6` +
  `form-label` + plain `form-control` (the `AddContact` / `EditContact` legacy
  pattern); use `gl-form-shell`, and migrate those two views when you touch them.
- **Don't** mix label placement in one form — labels are above the control in a
  `gl-form-shell` form, right-aligned in a `card-big-info` form, never both.
