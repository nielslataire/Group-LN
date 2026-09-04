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
  form/content. Collapses to stacked single-column ≤768px. Used on
  supplier/contract forms.

### Inputs / Fields
- **Style:** Surface White, 1px border, 7px radius. The `form-control-modern`
  variant is **46px** tall with `6px 17px` padding; Select2 single/multi controls
  are matched to the same height and padding so native and enhanced fields align.
- **Focus:** green-tinted focus ring consistent with buttons.
- **Labels:** Label style (600, 0.78rem), sat directly above the field.

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
