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
pattern when you touch it, not in a bulk sweep.
- **Markup:** `<span class="gl-row-actions">` wrapping one
  `<button type="button" class="gl-row-action-btn" aria-label="…">` per action,
  each holding one `bx`-icon (`aria-hidden="true"`) at 16px. Reference:
  `ProjectTraject/Index.cshtml` (Mijlpalen-tabel) and `_Timeline.cshtml`.
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
- **Hidden until the row is interacted with.** `.gl-row-actions
  .gl-row-action-btn` and `.gl-traject-mijlpaal .gl-row-action-btn` (i.e. the
  *secondary-action* role specifically, not every use of the class — see
  below) sit at `opacity:0` by default and reveal on `tr:hover` /
  `tr:focus-within` / the button's own `:focus-visible`, plus a
  `@media (hover: none)` fallback that forces them visible on touch (no hover
  to reveal them with there). Two muted icons sitting permanently on every
  row of a long table is more visual noise than signal; revealing them on
  approach removes that noise without removing the affordance.
- **Except when the icon button *is* the primary content, not a secondary
  action** — `_UnitMatrix.cshtml` reuses `gl-row-action-btn` for its
  status-change button, but there the icon *is* the cell's main content (the
  status itself), so it must stay always-visible. That's why the hide-on-rest
  rule above is scoped to `.gl-row-actions`/`.gl-traject-mijlpaal`
  specifically rather than the bare class — check which role a new use case
  is playing before copying the hide-until-hover rule onto it.
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

### Table search + column filter
A DataTable with a custom search box and a column-visibility ("colvis") button
— the search input replaces DataTables' own `.dt-search` (hidden via CSS), the
colvis button is moved out of the default toolbar into a page-chosen container.
Origin: `Projecten/DetailClients.cshtml` (`Clients.cshtml`); current best
version: `ProjectTraject/Index.cshtml` (Mijlpalen-tabel).
- **Layout:** a `d-flex align-items-center gap-2` row holding two children —
  the search `.input-group` (`flex-grow-1`, so it fills all available width)
  and the colvis button's container (`flex-shrink-0`) beside it with a real
  `gap-2`. **Don't** put the colvis container *inside* the same `.input-group`
  as the search box (`Clients.cshtml`'s original approach) — the two controls
  end up visually fused with no breathing room between them, which reads as
  unfinished rather than deliberate.
- **DataTable init:** `layout: { topStart: { buttons: [{ extend: "colvis",
  text: '<i class="bx bx-columns me-2"></i><span>Kolommen</span>', columns:
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
      sidebar — `ProjectTraject/Index.cshtml`, the reference implementation):
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
- **Panels (`gl-form-shell__panel`):** `overflow-y:auto`, 24px padding;
  non-active panels carry `hidden`.
- **Actions (`gl-form-shell__actions`):** a third, always-visible zone rendered
  by `_FormShellActions.cshtml`
  (`FormShellActionsModel { SubmitLabel, SubmitIcon = "bx-save", CancelUrl, CancelLabel = "Annuleren" }`).
  `position: fixed` to the viewport bottom, left-aligned next to the sidebar
  (`left: 300px` / `73px` collapsed / `0` ≤767px), respecting
  `env(safe-area-inset-bottom)`. Buttons use the large app size
  (`btn-px-4 py-3`, `submit-button` / `cancel-button`), not the topbar size.
- **Section (`gl-form-section` + `__body`):** a block inside a panel.
  `gl-form-section__head` is a flex row — a **42px** Mist-Green rounded (≈`md`,
  11px) icon badge + `__title` (700, 1.0625rem) + `__hint` (0.8125rem, muted),
  Hairline under it.
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
  gap-2 mb-3` fallback row at the top of its own content body. A few earlier
  pages (`Projecten/DetailContracts.cshtml`) still use `btn-md` here; migrate to
  `btn-sm` when you touch them, not in a bulk sweep.

### Paginakop (`gl-page-header`)
- **What it replaces:** the ad-hoc title/subtitle/back-button/actions block that
  had drifted into 30+ near-duplicate variants across content pages
  (`font-weight-bold` vs `fw-bold`, `mb-4 mt-0` vs `mb-0`, `btn-group` vs
  `d-flex gap-2`, inconsistent row-wrapper classes). This is the single
  successor pattern; new content pages use it instead of reinventing the row.
- **Structure:** one flex row (`gl-page-header`, `justify-content: space-between`,
  wraps below 576px) with two zones — `gl-page-header__left` (back button +
  title + subtitle, rendered by the shared partial `Views/Shared/_PageHeader.cshtml`
  with `Models.PageHeaderModel { Title, Subtitle?, BackUrl?, BackAriaLabel? }`)
  and `gl-page-header__actions` (page-specific buttons, authored directly by the
  calling page — action content varies too much across pages to generalize
  further; only its position and inter-button gap are standardized).
- **Back button:** 40×40px icon button (`gl-page-header__back`, The Field-Width
  Rule), `bx-chevron-left` at 1.5rem, Muted by default, Mist-Green background +
  Deep Forest Green icon on hover/focus — same interaction language as the
  dashboard icon buttons.
- **Title / subtitle:** plain `<h1>`/`<h5>`, font-size intentionally left to the
  theme's base heading styles (unchanged); `gl-page-header` only standardizes
  spacing (4px between title and subtitle) and the row's own bottom margin (24px).
- **Usage:** an expanding set of content pages use it — e.g.
  `CPMCore/Views/Projecten/AddContract.cshtml` (back + title + subtitle, no
  actions) and `CPMCore/Views/Projecten/DetailContracts.cshtml` (title +
  subtitle + actions, no back). The remaining ad-hoc title rows are migrated
  opportunistically when a view is touched, not in a bulk sweep. Note: a
  `gl-form-shell` form does not use `gl-page-header` — its title lives in the
  `gl-form-section__head`, its actions in `gl-form-shell__actions`.

### Badges
- Solid fills mapped to tokens: `bg-primary` → Deep Forest Green,
  `bg-secondary` → Sage, `bg-info` → Taupe Grey, `bg-accent` → Timber, plus
  standard success/warning/danger/dark/light. White text except warning/light
  (black). Pill radius.

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
