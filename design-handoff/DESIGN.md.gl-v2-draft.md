<!-- DRAFT — not the live DESIGN.md. This is the working design-system record for the gl-v2 layout
     pilot (see PLAN in C:\Users\niels\.claude\plans\async-juggling-metcalfe.md and the branch
     `layout-experiment`). Follows the same canonical DESIGN.md spec the real file uses, kept
     current as decisions are made, so that on adoption day this can become the real DESIGN.md
     directly instead of re-deriving everything from a fresh code scan. Do not treat this as
     authoritative for the shipped app — the real, current DESIGN.md at the repo root still
     governs every page except the gl-v2-toggled ones. -->

---
name: CPM — gl-v2 (pilot)
description: Floating-card icon-rail shell for CPM, explored from a Claude Design wireframe bundle — not yet adopted
colors:
  bg: "#F2F5EF"
  surface: "#ffffff"
  ink: "#2C3B2A"
  muted: "#5a6b58"
  muted-soft: "#4A5B48"
  primary: "#00532D"
  primary-tint: "#D6E5CC"
  accent-sage: "#7A9E6E"
  gold: "#C9A96E"
typography:
  display:
    fontFamily: "Playfair Display, 'Times New Roman', Georgia, serif"
    fontSize: "19px"
    fontWeight: 500
    lineHeight: 1.25
    letterSpacing: "normal"
  body:
    fontFamily: "Avenir, 'Open Sans', -apple-system, 'Segoe UI', Helvetica, sans-serif"
    fontSize: "15px"
    fontWeight: 500
    lineHeight: "normal"
    letterSpacing: "normal"
  label:
    fontFamily: "Avenir, 'Open Sans', -apple-system, 'Segoe UI', Helvetica, sans-serif"
    fontSize: "10.5px"
    fontWeight: 500
    lineHeight: 1
    letterSpacing: "normal"
rounded:
  sm: "8px"
  md: "12px"
  lg: "16px"
  pill: "999px"
spacing:
  xs: "6px"
  sm: "10px"
  md: "14px"
  lg: "18px"
  xl: "22px"
components:
  rail-item:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    size: "48px"
  rail-item-active:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.md}"
    size: "48px"
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.surface}"
    rounded: "{rounded.sm}"
    height: "36px"
    padding: "0 16px"
  button-sage:
    backgroundColor: "{colors.accent-sage}"
    textColor: "{colors.surface}"
    rounded: "{rounded.sm}"
    height: "36px"
    padding: "0 16px"
  flyout-panel:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.lg}"
    width: "262px"
  mobile-menu-item:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "10px"
    padding: "13px 10px"
  mobile-menu-item-expanded:
    backgroundColor: "{colors.primary-tint}"
    textColor: "{colors.primary}"
    rounded: "10px"
    padding: "13px 10px"
---

# Design System: CPM — gl-v2 (pilot)

## Overview

**Creative North Star: "The Elevated Rail"**

A departure from the current shipped CPM system, sourced from a Claude Design wireframe
exploration (`design-handoff/CRM Menu Wireframes.dc.html`, options 2a/2b/2c/3a/3b/3e/4a). Where
the current system is near-flat with a solid full-height green sidebar, gl-v2 is composed of
distinct floating white cards — an icon-only rail, a body card, hover/click flyout panels — all
resting on a warm sage-tinted page (`#F2F5EF`), each with its own soft ambient shadow. A serif
display face (Playfair Display) marks page titles and panel headers, breaking from the current
system's Poppins-only rule; body and label text stay a humanist sans (Avenir/Open Sans). This
pairing is drawn from the public groupln.be site's own brand voice, brought into the admin tool
for the first time.

This is a **pilot**, not the shipped default — see the plan for scope, toggle mechanism
(`gl_v2_preview` cookie via `LayoutPreviewController`), and what's deliberately still out of
scope (flyout pin-to-push, tablet inner-menu variants).

**Decided against:** the mockup's bottom-nav pattern (options 3a/3b's phone-width bottom bar) will
not be built. The topbar's quick-actions slot (see `_LayoutV2.cshtml`, filled per-page — e.g.
`IndexV2.cshtml` for Facturen) already surfaces page-level actions, and on phone width those stay
reachable through the hamburger menu. A separate bottom-nav would duplicate that path, so it's out
of scope permanently, not just deferred.

**Key Characteristics:**
- Floating-card identity: every distinct surface (rail, body, flyout, mobile menu) is its own
  white card with a soft shadow on a tinted page, never a flush/edge-to-edge panel.
- Icon-only rail (72px), no persistent text labels — labels surface via tooltip-on-hover and the
  flyout panel's own heading.
- Serif display type for anything that names a page or panel ("Dashboard", "Facturen - BCO",
  "Menu"); everything else stays the humanist sans.
- 8 / 12 / 16px radius scale, one step up from the current system's 7 / 14 / 16px scale.

## Colors

A single deep green (`#00532D`) over a warm sage-tinted page, distinct from the current system's
paler `#f5f5f8` — the tint itself carries more of the "grounded" identity than any one accent does.

### Primary
- **Deep Green** (`#00532D`): rail-item active state, primary buttons, flyout "current" link,
  expanded mobile-menu-item background text color, mobile topbar background.

### Secondary
- **Sage** (`#7A9E6E`): the "Boeken facturen"-class secondary action button — a muted, lower-
  emphasis green sibling to Primary, never used for a destructive or negative action.

### Tertiary
- **Gold** (`#C9A96E`): reserved for the active-tab underline pattern from the mockup's dashboard
  option (2a) — not yet built on any page in this pilot (no page with a tab strip has been
  implemented). Do not use it for anything else until a tabbed screen actually needs it.

### Neutral
- **Ink** (`#2C3B2A`): default text.
- **Muted** (`#5a6b58`) / **Muted Soft** (`#4A5B48`): secondary text, breadcrumb, icon-muted state.
- **Page Background** (`#F2F5EF`): the sage-tinted page behind every card.
- **Surface** (`#ffffff`): every card, flyout, and panel.
- **Primary Tint** (`#D6E5CC`): hover/active fill for flyout links, expanded mobile-menu items,
  topbar-icon chip background, avatar background.
- **Hairline** (`rgba(44,59,42,.12)`) / **Hairline Soft** (`rgba(44,59,42,.08)`): dividers.
- **Dashed** (`rgba(44,59,42,.18)`): the wireframe-placeholder dashed-border treatment, carried
  into real UI for genuinely-unresolved slots (e.g. the mobile topbar's search-icon stub, which
  is intentionally not wired to real search yet).

### Named Rules
**The Floating-Card Rule.** Every distinct navigational surface (rail, body, flyout, mobile menu
panel) is its own white, shadowed card — never a flush panel sharing an edge with the page
background. If two things visually touch with no gap and no shadow break between them, they are
the same card, not two.

## Typography

**Display Font:** Playfair Display (with 'Times New Roman', Georgia, serif fallback)
**Body/Label Font:** Avenir (with 'Open Sans', -apple-system, 'Segoe UI', Helvetica, sans-serif)

**Character:** The serif is reserved for naming something — a page title, a panel heading, a
company name inside a flyout title. It never appears in a button, a nav item label, or body copy.
Everything that isn't naming something stays in the sans.

### Hierarchy
- **Display** (500, 19px, 1.25): topbar page title, mobile-menu panel title, flyout panel title
  (18px there — see Named Rule below).
- **Body** (500, 15px): mobile-menu item labels, primary button labels (600 weight there).
- **Label** (500, 10.5px, line-height 1): breadcrumb/subtitle text, rail tooltips (600, 12px).

### Named Rules
**The Naming-Only Serif Rule.** Playfair Display marks the name of the thing you're looking at —
a page, a panel, a person's company. It never appears on a control (button, chip, nav item, form
field). Reach for weight in the sans face for everything else.

## Layout

Rail (72px, sticky) + gap (12px) + floating body card, all on a 12px page inset — every edge of
the composition sits 12px off the viewport, unlike the current system's edge-to-edge sidebar.

Body card: topbar (62px, hairline-bottom) + content area (20-22px padding, `#F7F9F5` background,
one shade cooler/lighter than the page's own `#F2F5EF` — a card's content area is not the same
tone as the page behind the card).

**Responsive:**
- **≥768px**: rail + flyouts, as above.
- **<768px** (phone breakpoint — matches the current system's own ≤767px convention): rail and
  flyouts hidden entirely. Topbar switches to solid Primary-green, white text, and shows, left to
  right: a dashed-border logo box (32px), title + single-line subtitle (last breadcrumb crumb
  only, not the full trail), a spacer, a circular search-icon stub (34px, not wired to real
  search yet), and a circular hamburger trigger (34px) — see Mobile Topbar under Components. The
  userbox/avatar is **not** shown in the mobile topbar at all; the profile moved entirely into
  the mobile menu panel's footer (see Mobile Menu Panel). A page can also render a full-width
  "quick actions" bar pinned under the scrollable content, just above the safe-area — see Mobile
  Quick-Actions Bar under Components.

### Named Rules
**The 12px Inset Rule.** The whole rail+body composition sits 12px off every viewport edge at
desktop width. Nothing in gl-v2 is edge-to-edge except the phone breakpoint's body card, which
intentionally loses its radius/shadow and goes flush (see Elevation).

## Elevation & Depth

Floating, not flat. Every card carries a real ambient shadow — the opposite of the current
system's near-flat, hairline-bordered philosophy. Depth communicates hierarchy: flyouts (which
float highest, over the body) get the strongest shadow; the body card itself gets a lighter one;
rail items get the lightest (`shadow-tile`).

### Shadow Vocabulary
- **Tile** (`0 1px 2px rgba(0,83,45,.06), 0 8px 18px -10px rgba(44,59,42,.28)`): rail items at
  rest.
- **Tile Active** (`0 2px 6px rgba(44,59,42,.22), 0 12px 26px -12px rgba(44,59,42,.5)`): the
  active rail item — a visibly heavier lift than the resting tile shadow.
- **Card** (`0 1px 2px rgba(0,83,45,.06), 0 22px 46px -26px rgba(44,59,42,.38)`): the body card,
  and any content card inside it (toolbar card, table card).
- **Flyout** (`0 1px 2px rgba(0,83,45,.06), 0 30px 60px -28px rgba(44,59,42,.45)`): flyout panels
  and the mobile menu — the strongest shadow in the system, reserved for things that float above
  the body card itself.

### Named Rules
**The Shadow-Ranks-Depth Rule.** Shadow strength is not decorative — it's a literal z-order cue.
Flyout > Card > Tile Active > Tile. Never give a resting element a stronger shadow than something
that's meant to visually float above it.

## Shapes

8 / 12 / 16px radius scale — one notch up from the current system's 7 / 14 / 16px. Rail items and
mobile-menu items use 10-12px (rounder than a plain content-chrome radius, since they're
navigation, matching the current system's own "navigation is rounder" instinct). Flyouts and the
mobile menu panel use the largest radius (16px) or, at phone width, none at all (the body card
goes flush/edge-to-edge below 768px — see Layout).

## Components

### Rail item
Eight defined states (design-handoff reference sheet, 48px tile, real size):
1. **Rest:** white fill, Tile shadow, Ink icon, no label.
2. **Hover:** fill → Hover Tint (`#EDF3E8` — distinct from Primary Tint `#D6E5CC`, a lighter mint
   reserved for hover only), icon → Primary. Tooltip appears after a 150ms delay, with a small
   triangle connector into the tile, not just a floating bubble.
3. **Active (current page):** Primary fill, white icon, Tile Active shadow, **plus a 3px gold
   stripe in the rail's own gutter** just left of the tile (`left:-8px`, 24px tall) — the "where
   you are" indicator. Not a tile border; it reads as a rail-level marker.
4. **Active + hover:** one step darker (`--gl-v2-primary-deep`, `#00401F`).
5. **Pressed:** shadow flips inward (`inset 0 2px 4px rgba(0,0,0,.15)`), icon shrinks 1px (19→18px).
6. **Keyboard focus:** gold outline, 2px offset — `:focus-visible` only, so a mouse click never
   shows it, only Tab.
7. **Badge (counter):** top-right circular badge, small red pill, "9+" past nine — component
   defined (`.gl-v2-rail-badge`) but not wired to a real count on any item yet.
8. **Disabled (no access):** grey fill/icon, no shadow, no hover — component defined
   (`.gl-v2-rail-item.is-disabled`) but not used yet; the rail currently hides no-access items
   entirely rather than showing them greyed out (see Do's and Don'ts).

### Named Rules
**The Flyout-Open-Is-Active Rule.** A trigger tile gets the exact same visual treatment (state 3
above, gold stripe included) for as long as its flyout is open — regardless of whether that item
is the actual current-page route. Two separate classes drive this: `.is-active` (server-rendered,
permanent, the real current page) and `.is-flyout-open` (JS-toggled on open/close, temporary).
Closing a flyout only ever removes `.is-flyout-open`; it must never touch `.is-active`.

### Flyout panel
- **Shape:** 262px wide, 16px radius, Flyout shadow.
- **Trigger:** hover (with a 250ms close-delay on leave) or click — click always ensures open,
  never toggles closed (a real mouse fires hover-then-click on the same interaction, and a
  toggle-on-click would immediately re-close what hover just opened).
- **Close:** outside click, Esc, or the 250ms mouseleave delay.
- **Position:** `position:fixed`, JS-positioned via the trigger's bounding rect — must render as a
  sibling of the rail in the DOM, never a descendant. The rail uses `position:sticky`, which
  creates its own stacking context; a fixed-position descendant of it gets trapped inside that
  context and can render behind unrelated content elsewhere on the page despite its own z-index.
- **Current-selection highlight:** when a flyout lists per-company links (Leveranciers, Klanten,
  Facturatie) and one of them matches the page you're actually on (`issuerCompanyId` query value),
  that link gets `.is-current` (Primary fill, white text) — e.g. opening the Facturatie flyout
  while on the BCO invoices page highlights "BCO" in the list.

### Buttons
Design-handoff optie 4e ("KNOPPEN — DEFAULT · HOVER · ACTIEF · FOCUS · DISABLED"). Five families,
all sharing the same shape (`.gl-v2-btn`: 38px height, 8px radius, `0 16px` padding, 600 12px
sans) — only colors differ per family and per state. `:focus-visible` is identical across every
family: a 3px gold ring (`box-shadow: 0 0 0 3px var(--gl-v2-gold)`), placed after hover/active in
the stylesheet so it wins if both could apply; `:focus-visible` only, so a mouse click never shows
it, only Tab.

- **Primair** (`.gl-v2-btn-primary`) — the one emphasized action on a page (e.g. "+ Nieuwe
  Factuur"). Rest: Primary-green fill. Hover: one step lighter (`#006638`) + a soft green glow
  (`0 8px 18px -10px rgba(0,83,45,.75)`). Active: one step darker (`#003d21`), glow removed.
  Disabled: `#DCE4D7` fill, `#7C8C7A` text — not just a lower-opacity primary green.
- **Secundair** (`.gl-v2-btn-secondary`) — a real but lower-emphasis action sitting next to a
  Primary one (e.g. "Boeken facturen" beside "+ Nieuwe Factuur"), never destructive. Rest: white
  fill, Sage border, `#2F6038` text. Hover: Hover-tint fill, Primary border/text. Active: solid
  `#2F6038` fill and border, white text — the only family whose active state is a *different* fill
  color from its hover, not just a darker version of it. Disabled: white fill, hairline border,
  `#9AA898` text.
- **Tekstknop** (`.gl-v2-btn-text`) — lowest emphasis, e.g. "Annuleren" in a modal footer. Rest:
  transparent, Primary-green text, no border. Hover: `#E4EBDE` fill. Active: Primary-tint fill,
  `#003d21` text. Disabled: transparent, `#9AA898` text.
- **Icoonknop** (`.gl-v2-icon-btn`) — its own square family (38×38, not a `.gl-v2-btn` modifier),
  used for the topbar's Excel/PDF export triggers. Rest: white fill, hairline border, Primary-green
  icon. Hover: Hover-tint fill, Primary border (icon color unchanged — only the container reacts).
  Active: solid Primary fill, white icon — the only state where the icon itself inverts. Disabled:
  hairline-soft border, white fill, `#B6C1B4` icon. Previously used a *dashed* border as an
  intentional "not fully designed yet" marker (same convention as the mobile search stub) — that's
  gone now that this family has a real state set; a dashed border elsewhere in gl-v2 still means
  "acknowledged placeholder," just not here anymore.
- **Verwijderen** (`.gl-v2-btn-danger`) — destructive actions only. Rest: white fill,
  `rgba(139,42,42,.4)` border, `#8B2A2A` text. Hover: `#F8EBEB` fill, solid `#8B2A2A` border. Active:
  solid `#8B2A2A` fill, white text. Disabled: white fill, hairline border, `#9AA898` text — same
  disabled treatment as Secundair/Tekstknop, deliberately not a "greyed-out red" that could still
  read as dangerous.

### Mobile menu panel
- **Header:** Primary-green fill, dashed-border logo placeholder (32px, 8px radius — matches the
  rail/topbar's own dashed-border logo-placeholder convention), serif panel title, circular close
  button (34px) — positioned as the header's last child, deliberately the same on-screen spot as
  the topbar's hamburger trigger (see Mobile Topbar) so open/close reads as one control morphing
  in place, not two unrelated buttons in different corners.
- **Search:** pill-shaped input, hairline border — visually present per the mockup but not yet
  wired to real search (client-side label-filter over the visible menu only).
- **Item:** 13px/10px padding, 10px radius, icon + label + chevron.
- **Item, expanded (has an open sub-group):** Primary-tint background, 3px Primary-green left
  border, Primary-green icon/text, 600 weight — not just a rotated chevron; the whole row commits
  to the "this is open" state.
- **Sub-item:** indented 30px, 9px radius, its own leading icon (every sub-item gets one, not just
  the top-level items — this was a real gap in an earlier pass, since fixed).
- **Footer — profile card:** the whole profile is one tappable card (avatar 36px + name + email,
  ellipsis-truncated + trailing chevron), hairline border, 12px radius, 8px padding — not a bare
  row of text like the first version of this panel had.
- **Footer — actions:** two equal-width buttons below the profile card, "Mijn profiel" and
  "Afmelden", 36px height, 9px radius, hairline border. "Mijn profiel" links to the closest real
  existing page (Mijn handtekening/Account) — the app has no dedicated profile screen, and this
  pilot doesn't invent one just because the mockup's label implies it exists.
- **Footer, overall:** `env(safe-area-inset-bottom)` padding so it clears the home-indicator area
  on notched phones.

### Mobile Topbar
Left to right: dashed-border logo box (32px, 8px radius, white dashed border) → title/subtitle
block → spacer → search-icon stub (34px, full circle, white 1px border) → hamburger trigger (34px,
full circle, white 1px border, same shape/size as the search stub). Both circular icon buttons
share one shape language distinct from the rest of the system's rounded-square icon buttons —
mobile-topbar icon buttons are circles, everywhere else they're 8-16px-radius squares.

### Mobile Quick-Actions Bar
A page-specific, optional bar, only rendered <768px, `position:fixed` to the bottom of the
viewport (option 4c) — Primary-green fill, no page-content-flow placement, no "SNELACTIES" label.
Tiles are bare icon-over-label, no border, no fill (Phosphor icon 21px, label 10px, white on the
green bar); `:hover`/`:focus-visible` get a faint white tint, nothing else distinguishes one tile
from another — 4c itself makes no primary/secondary distinction, so gl-v2 doesn't invent one
either. This is the mobile equivalent of a page's `PageActions` — the two present different-enough
markup (compact topbar button vs. a full tile) that a page defines both, once each, rather than
one shared partial trying to serve both shapes. See `Views/Invoices/IndexV2.cshtml` (2 actions,
canWriteInvoices-gated) and `Views/Home/Index.cshtml` (5 actions, Projectleider-only — see "Real
example" below) for the reference pairing (`@@section PageActions` + `@@section
MobileQuickActions`).

**Fixed height, not measured.** The bar's height is one CSS custom property,
`--gl-v2-qa-bar-h: 64px` (set on `.gl-v2`, covers the icon row only — `env(safe-area-inset-bottom)`
is added separately in every `calc()` that uses it, never baked into the 64px itself, so it doesn't
eat into the icon row's budget on a notched phone). The bar's own `height` and every other rule
that needs to know where the bar ends (content `padding-bottom`, the quick-actions-sheet backdrop,
the sheet itself) all reference this one variable via the same `calc(var(--gl-v2-qa-bar-h) +
env(safe-area-inset-bottom, 0px))`. Earlier attempts guessed a pixel number for the bar's *auto*
height (content-driven, no explicit `height`) and then reused that guess elsewhere — the guess
didn't match the real rendered height, so the sheet/backdrop landed a few px inside the bar instead
of flush above it. Also tried: measuring the real height in JS (`bar.offsetHeight`) and writing it
to the CSS variable at runtime — works, but is strictly more moving parts than just declaring the
height explicitly and letting content clip/center within it (`align-items:center` on the bar; tiles
never come close to 64px of content, so there's nothing to clip in practice).

**Overflow (option 6b).** Generic shell behaviour, not per-page logic: `gl-v2-shell.js` counts the
tiles a page provided and, past 4, hides tile 5+ (`hidden` attribute) and appends one more tile —
"Meer" (`ph-dots-three`) — so the bar never shows more than 4 real actions + this one. Three
states: **rest** (identical to any other tile), **pressed** (pure `:active`, a faint white fill
that disappears the instant the finger/pointer lifts — no JS), **open** (JS-toggled `.is-open`:
the same white fill persists, icon/label swap to `ph-x`/"Sluiten" via two extra
`hidden`-attribute-toggled elements each, and every other tile in the bar drops to `opacity:.45`
via `.gl-v2-mobile-quickactions.has-open-sheet`). Tapping it opens the **quick-actions sheet**: a
bottom sheet (`.gl-v2-quickactions-sheet`, 16px top corners, Flyout shadow, drag handle) titled
"Snelacties op {pageTitle}" with a list of rows built from the hidden overflow tiles — icon, title
(the tile's own label text), an optional subtitle (`data-subtitle` attribute on the source tile,
entirely opt-in — nothing fabricates one), and a trailing chevron. Each row delegates to the
original (hidden) tile via `.click()` rather than re-implementing its `href`/form-submit, so a
disabled source button's row is correctly inert without any extra state-sync code. Esc, backdrop
click, or the sheet's own × close it.

**The bar stays visible while the sheet is open.** The `.gl-v2-quickactions-backdrop` and the sheet
itself both stop their `bottom` edge at the bar (the same `calc(var(--gl-v2-qa-bar-h) + safe-area)`
as the bar's own height) instead of running to the true bottom of the viewport — the backdrop must
NOT cover the bar, or the bar reads as "disappeared" under the dimming layer even though it's still
there in the DOM. Tried raising the bar's own `z-index` above the backdrop's instead: works for
this case but then the bar would also float above the full-screen hamburger menu (`z-index:99999`)
if that ever opened at the same time, which is a worse bug — geometry (stop short of the bar) beats
stacking order (out-z-index the backdrop) here.

**Real example: Projectleider dashboard.** `Views/Home/Index.cshtml` defines
`@@section MobileQuickActions` unconditionally (Razor forbids `@@section` inside `@@if`) but only
emits tiles when `Model.DashboardType == Projectleider`, gated behind
`ViewData["HasMobileQuickActions"]` — see "Empty-bar guard" below for why that flag exists. Five
tiles, ported from the desktop "Snelacties" card + the pre-existing (now gl-v2-only, see below)
`.gl-mob-nav`: **Punt**, **Leverancier** (zoeken), **Klant** (zoeken), **Vastzetten** (Project
vastzetten) visible, **Nieuwe leverancier** (Leverancier toevoegen, `data-subtitle="Nieuw contact
aanmaken"`) overflows into the Meer sheet — 5 tiles is the first real page to exercise the overflow
path. No new click-handling JS: the gl-v2 tiles carry fresh ids (`gl-v2-qa-punt`,
`gl-v2-qa-lev-search`, `gl-v2-qa-klant-search`, `gl-v2-qa-pin`) added onto the *existing* jQuery
selectors in `_DashboardProjectleider.cshtml` that already open the right sheet/panel — converted
from direct `$(selector).on(...)` binding to `$(document).on('click', selector, ...)` delegation in
the same edit, because the gl-v2 tiles render outside `<main>` (in the layout's bar), after that
inline `<script>` block has already run; a direct binding would've found nothing.
Deliberately **not** ported: the 5 project-scoped Snelacties (Contract, Inkomende factuur,
Wijzigingsopdracht, Nacalculatie, Document uploaden) — each needs a project-picker sheet first
(same pattern "Punt" already has via `#gl-punt-sheet`), which doesn't exist yet for the other five.
The old `.gl-mob-nav` bottom nav in `_DashboardProjectleider.cshtml` is now suppressed
(`@@if (!useGlV2Layout)`) so it can't render underneath/alongside the new gl-v2 bar — same
`ViewData["UseGlV2Layout"]` flag `_ViewStart.cshtml` reads to pick the layout in the first place.

**Empty-bar guard.** Because `@@section` must stay unconditionally registered even when a page's
condition makes it render nothing for the current request (the Projectleider-only tiles above; also
`IndexV2.cshtml`'s two tiles behind `canWriteInvoices`), `IsSectionDefined` alone would make
`_LayoutV2.cshtml` show an empty green strip on every dashboard type and for read-only invoice
users. Pages that can go either way set `ViewData["HasMobileQuickActions"]` to the same boolean that
guards their section's content; the layout only renders the bar when both `IsSectionDefined` AND
that flag (defaulting to `true` when a page never sets it, so every other/simpler page keeps working
unchanged) agree there's really something to show.

### Table (Facturen)
Design-handoff optie 4e ("TABEL — RIJSTATEN EN ACTIES IN DE RIJ") + optie 4a (layout/pagination) +
optie 4f (laad-/lege staat + "TABLET — ···-MENU IN DE RIJ"). Reskins the **existing**
`<table>`/DataTables markup in `Views/Invoices/IndexV2.cshtml` — no CSS-grid rewrite; the mockup
renders rows as `display:grid` divs, but that would mean re-implementing DataTables'
sort/search/paging from scratch, so this stays a real `<table>` throughout, styled to look like
the mockup's grid rows (`gl-v2-invoices.css`).

**Tablet (768–1023.98px, optie 4b/4f).** Same range as the userbox's own tablet state in
`gl-v2-shell.css` — one tablet concept for the page, not a Facturen-specific breakpoint. Five
columns hide outright (Type, Datum, Excl. BTW, Project, Verzendwijze — the same five 4b itself
drops), leaving Boeken/Factuurnr/Klant/Totaal/Status/Acties. The Acties column becomes a single
"···" trigger (`.gl-v2-row-menu-trigger`, states: bare/muted at rest, filled Primary-green on
hover, same filled look held via a JS-toggled `.is-menu-open` class while the panel is open — same
`.is-flyout-open` idea the rail flyouts already use) that opens a floating menu
(`.gl-v2-row-menu.is-open`, positioned with `getBoundingClientRect()` exactly like
`gl-v2-shell.js`'s rail-flyout `positionFlyout()`) listing the same actions the desktop/mobile
icon row already has — same `<a>`/`<button>` elements, no duplicated markup, just restyled from
26×26 icon squares into full-width icon+label rows for this one breakpoint. Two things that made
this possible without new markup per action:
  - Every row action now carries a `.gl-v2-row-action-label` span (previously only some did, and
    inconsistently as `visually-hidden`) — a menu row needs visible text for actions like PDF
    Export or Verzenden that were icon-only everywhere else. This is a from-scratch class, not
    Bootstrap's `.visually-hidden`: that utility sets every property with `!important`, which would
    have to be fought with more `!important` to reveal the label inside the open menu — simpler to
    own the hide-technique here and just not fight it.
  - A `.gl-v2-row-menu-divider` div (hidden everywhere except inside an open tablet menu) sits
    right before the Verwijderen link in markup, giving the reference's divider-before-delete
    treatment without needing separate desktop/tablet action lists.
  - Inside the open menu, the Primair/Danger family colors drop their button-chip fill (solid
    green / light-red background) and become plain colored text+icon rows instead — a filled chip
    reads as a button in a horizontal icon row, but as a stray colored box in a vertical menu list;
    the reference's own tablet Verwijderen row is red text+icon on a transparent row, not a red
    button.

**Mobiel (<768px, optie 4c "Mobiel — facturen als kaarten") — genuinely separate markup, not a
reskin.** Unlike the tablet range above, this is **not** the same `<tr>`/`<td>` elements rearranged
with CSS. Two earlier attempts tried exactly that (`grid-template-areas`, then `display:flex` with
`order` on the `<tr>` itself) and both ran into real problems: CSS Grid's `auto`/`1fr` track sizing
either let unbreakable text push the card wider than the viewport, or squeezed the flexible column
down to almost nothing depending on which items shared which track — and even once that was tamed,
table-specific chrome (Bootstrap's row borders/background, DataTables' own layout rows) kept
bleeding through a card that was never really anything but a table row in a costume. The `<table>`
now goes `display:none` entirely below 768px, full stop — no reflow attempt. In its place,
`Views/Invoices/IndexV2.cshtml` has one empty `<div id="gl-v2-mobile-invoice-list">` after the
table, and `renderMobileCards()` (`gl-v2-invoices.js`) builds real `<div class="gl-v2-invoice-card">`
elements into it from the table's own current `<tr>`s — called on every DataTable `"draw"` (init,
search, sort, page), so the card list always mirrors whatever the (hidden) table currently shows
without re-implementing any of that filtering/sorting/paging logic here.

**Checkbox and actions stay single, real elements — the card reaches them, it doesn't duplicate
them.** A second `name="invoiceIds"` checkbox per invoice, or a second copy of the action list,
would be invalid HTML and would double-count a selection (`syncBookButton()`'s `:checked` count).
Instead each card gets a **proxy checkbox** (`.gl-v2-mc-checkbox`, a plain `<button>` — deliberately
*not* carrying the `.invoice-book-checkbox` class, so it never enters `getBookingCheckboxes()`'s own
count) that forwards a click to the real, hidden checkbox and mirrors its checked/disabled state
back (`syncMobileCardCheckboxStates()`, called from `applyBookingRules()` so a cascading "select
everything up to this one" click updates every visible card, not just the one tapped). The "···"
trigger's panel is a **clone** of the real `.gl-v2-row-menu` (`.clone()`, given a fresh id) rather
than a reference to the original — cloning works because every handler that acts on it
(`.deleteInvoice`, `.js-issue-invoice`, the row-menu-open logic itself) is delegated on `$(document)`
by CSS class, not bound to the specific DOM node, so it fires identically on the clone with no new
JS per action.

**No card-in-a-card.** `.gl-v2-table-card` is normally its own white, shadowed card (same as the
toolbar card) — nested white cards inside it would have no contrast. Below 767.98px it goes
transparent/flat instead (`background:transparent; box-shadow:none; border-radius:0`), so the
individual invoice cards sit directly on `.gl-v2-content`'s own `#F7F9F5`, the same depth
relationship every other floating card in gl-v2 has to its background.

**Pagination.** `syncTablePageLength()` (optie 4a, above) assumes a fixed 54px row — cards are
taller and variable, so below 768px it skips that math and sets a flat `MOBILE_PAGE_LENGTH` (15).
No internal scroll container is needed here (an earlier version gave `.table-responsive-md` its own
`overflow-y:auto`, back when the *table* was still the thing rendering on phone) — the page just
scrolls over the card list like an ordinary list, since nothing downstream of it still depends on
the table's own flex-height chain.

### Rij-acties als bottom sheet op telefoon (optie 4j, TYPE 3 "KEUZELIJST/ACTIES")
The "···" panel itself (built above) is a **clone** of the same `.gl-v2-row-menu` the tablet range
already uses as a floating panel — below 768px its *positioning* changes from "floating, JS-placed
near the trigger" to "bottom sheet, pinned by CSS," matching the reference's own split between
"DESKTOP · acties als zwevend menu" and "TABLET & MOBIEL · actiesheet met scrim." This app only
applies the sheet variant at phone width, not tablet — tablet already has a working floating panel
(optie 4f) and nothing asked for it to change.

- **Scrim** — one shared `.gl-v2-row-menu-backdrop` (`rgba(18,28,18,.45)`, `position:fixed;inset:0`),
  not one per card: only one panel is ever open at a time, so a single backdrop element toggled by
  `.is-open` is enough, same economy as the bookyear-select/quick-actions backdrops elsewhere in
  gl-v2. `closeAllRowMenus()` removes `.is-open` from it alongside the panel itself; the existing
  "click outside closes" `document` listener already closes on a backdrop click too, since the
  backdrop matches neither `.gl-v2-row-menu` nor `.gl-v2-row-menu-trigger`.
- **Sheet** — `.gl-v2-row-menu.is-open` gets `left:0;right:0;bottom:0`, `border-radius:18px 18px 0
  0`, a 42×4px drag-handle (`::before`, same dashed-grey token every other sheet in gl-v2 uses), and
  `env(safe-area-inset-bottom)` padding. `positionRowMenu()` (the tablet range's JS positioner)
  explicitly skips itself below 768px — same "just don't reposition, let CSS pin it" pattern
  `positionBookyearPanel()` already uses for the bookyear filter's own mobile sheet — otherwise a
  JS-set inline `top`/`left` would win over the CSS position (inline styles beat stylesheet rules
  without `!important`).
- **Title** — "Factuur {nr}" (serif, per the Naming-Only Serif Rule — this names the specific
  invoice the sheet's actions apply to), added only for the mobile sheet: the tablet floating panel
  has no room/need for one. `renderMobileCards()` prepends it to the cloned panel via `.text()`, not
  string concatenation, so it can't be misread as HTML.
- **Rows** — 44px per the reference ("rijen 44px"), icon 17px, label 13.5px, `padding:13px 10px`,
  `border-radius:10px`. Styled from scratch for this breakpoint (`.gl-v2-row-menu.is-open
  .gl-v2-row-action` with no `#datatable-invoice-list` prefix) rather than reusing the tablet
  block's own menu-row rules: those are deliberately `#id`-scoped to out-specificity the desktop
  bare-icon rule they share an ancestor table with, but the mobile clone lives inside
  `.gl-v2-invoice-card` now, outside the table entirely — nothing else styles `.gl-v2-row-action`
  there, so no specificity fight, no need to carry the id.
- **Not done:** Bootstrap tooltips on the row-action links/status icons inside a cloned or
  `.html()`-copied element don't fire (Bootstrap only wires up elements that exist at page load) —
  accepted as-is, a touch sheet has no hover state for a tooltip to serve anyway.

**Column layout (standard).** The 11 visible columns, in order, are the same set option 4a's own
`display:grid` mockup describes (`grid-template-columns:52px 70px 112px 96px minmax(0,1fr) 112px
106px 66px 116px 92px 158px`): **Boeken** (checkbox) · **Type** · **Factuurnr** · **Datum** ·
**Klant** (the one `grow` column — every other column is `width:"1%"`/content-fit via
`columnDefs`) · **Totaal** (`text-end`) · **Excl. BTW** (`text-end`) · **Status** (centered) ·
**Project** · **Verzendwijze** · **Acties** (`text-end`, not orderable). A 12th column
(**Boekjaar**) exists but is `d-none` — it only feeds the bookyear filter's `column().search()`,
never rendered. This exact column set/order is the reference point for anything else that needs to
represent "the Facturen table" without being the real, live table — see Loading state below, which
mirrors it directly rather than the mockup's own simplified 4-column LADEN demo.
- Row height 54px (`tbody td { height: 54px }`), header row 44px content height. Cell padding:
  10px left/right generally, first column 14px left (matches the toolbar-card/table-card's own
  14px edge inset), Acties 20px right (its icon row needs more breathing room than plain text), and
  **every orderable header** 26px right — not a design choice, a correction: DataTables' own CSS
  reserves `padding-right:30px` on orderable `<th>` cells for its absolutely-positioned sort icon
  (`span.dt-column-order{position:absolute;right:12px;width:12px}`, so it occupies the 12-24px
  zone from the cell's right edge); this file's `thead th` rule carries an `#id` selector, which
  beats DataTables' class-only rule regardless of load order, so without restating that padding
  here it collapsed to this file's own 10px and the icon rendered on top of the text — on
  `text-end` columns (Totaal, Excl. BTW) that reads as "icon before the title" since the
  right-aligned text reaches into the icon's zone. 26px (not DataTables' own 30px) is the
  tightest value that still fully clears the icon without padding every column wider than it
  needs to be.
- Footer/pagination row: padding lives on `.dt-info` (14px left) and `.dt-paging` (14px right)
  directly — matching the table's own edge insets — rather than on the row wrapper around them (an
  earlier attempt put `padding: 0 20px 0 14px` on `.dt-layout-row:last-child`, but that didn't
  visibly move anything, so the padding moved onto the two elements that actually needed it).
  Top/bottom uses `padding: 10px 0` on the row itself rather than a fixed `height: 48px` +
  `align-items: center` — the fixed-height approach left the vertical space above and below the
  pagination buttons at the mercy of how centering happened to round, not guaranteed equal; equal
  padding on both sides is equal by construction.

- **Row, rest:** 54px, hairline-soft bottom border.
- **Row, hover:** Hover-tint background (whole row, `tbody tr:hover td`).
- **Row, geselecteerd:** Primary-tint background + a 3px Primary-green inset stripe on the row's
  first cell only (`tr.is-selected td:first-child` — an inset box-shadow on *every* `td` would give
  every column its own left stripe, not one clean one at the row's edge). This is **not** a separate
  demo state: it's the existing "Boeken"-checkbox selection, now also reflected on the row itself.
  `gl-v2-invoices.js`'s `syncSelectedRows()` toggles `.is-selected` on a row's `<tr>` whenever its
  checkbox's checked state changes (checkbox change handler, table draw, and init all already ran
  `applyBookingRules()` — `syncSelectedRows()` hooks into that same call, no new event wiring).
- **Row, vergrendeld** (`.gl-v2-row-locked`) — defined for parity with the reference (`#FBFBF9`
  background, `#9AA898` text, muted+inert row actions) but **applied nowhere**. The reference's
  "DISABLED" row is a locked invoice; the closest real signal, a checkbox being
  `disabled` (not bookable / already booked), does not mean the invoice itself is locked — most
  invoices aren't bookable via this checkbox yet are completely normal, editable rows. Applying the
  muted treatment there would mislabel ordinary invoices as inaccessible. Same call as the rail's
  badge/disabled states: build the component, don't wire it to a condition that doesn't actually
  mean what the visual implies.
- **Row action buttons** (`.gl-v2-row-action`, 26×26, 7px radius) — bare by default (no border, no
  fill, muted icon, 15px) exactly like the reference's DEFAULT row; the bordered/filled "chip" look
  is the reference's HOVER row, gated per-icon here — `:hover`/`:focus-visible` on the button
  itself only, deliberately **not** a row-wide reveal (hovering one icon must not light up its
  neighbours in the same row; tried row-hover first, wrong reading of the reference). Three
  families, indistinguishable at rest, diverging only once hovered/focused — reflecting the real
  hierarchy of what each action does, not one uniform icon treatment:
  - **Neutral** (default family) — duplicate, details, PDF, print, nummeren/issue, verzenden:
    Hover-tint fill, Primary border/icon on hover.
  - **Primair** (`.gl-v2-row-action-primary`) — bewerken (edit) only: solid Primary fill (one step
    darker, `#006638`, than the standalone Primary button's hover — same idea, this is *already*
    the hover state), white icon. The one action in the row that changes the invoice, so it's the
    one visually promoted.
  - **Danger** (`.gl-v2-row-action-danger`) — verwijderen only: `#F8EBEB` fill, solid `#8B2A2A`
    border/icon — same red vocabulary as `.gl-v2-btn-danger`.
  - **GESELECTEERD rows** get the muted default treatment promoted straight to Primary-colored bare
    icons (no chip) even without hovering — matches the reference's selected-row icons, which are
    green-stroke but still borderless.
  - **VERGRENDELD** (unused, see below) rows stay inert via `pointer-events:none` alone — no extra
    override needed now that there's no row-wide reveal rule to fight.
- **Checkboxes:** built fully self-contained under `.gl-v2`, not as an override of the site-wide
  `.checkbox-custom` component — `_LayoutV2.cshtml` never loads `theme.css`/`custom.css` (only the
  old `_Layout.cshtml` does), so that component's box/checkmark (`label:before`/`:after`
  pseudo-elements) simply doesn't exist on gl-v2 pages; the markup (`.checkbox-custom` div +
  hidden input + empty `<label>`) is unchanged from `Views/Invoices/Index.cshtml`, but every pixel
  of its visual is gl-v2's own (18px, 5px radius, filled Primary-green checked state, white
  Font-Awesome checkmark glyph — same `\f00c` trick `theme.css` uses, just redirected). Checked
  always wins over disabled (declared after it in the stylesheet) — an already-booked invoice's
  checkbox stays visibly green even though it's inert, rather than looking unchecked/muted.
- **Selectie-toolbar** (`.gl-v2-selection-toolbar`) — appears as the table card's own bottom edge
  (inside the card's `overflow:hidden`, so its corners get clipped by the card's own radius for
  free) once ≥1 row is selected: Primary-green fill, white "N facturen geselecteerd" count,
  spacer, action button(s). The reference shows three buttons (Boeken/Verzenden/Verwijderen) as if
  a generic multi-select existed for all three — this table only has **one** real bulk action
  (Boeken, the existing checkbox → `invoice-book-form` flow); Verzenden and Verwijderen are
  per-row-only actions with no bulk endpoint in this app today, so the toolbar ships with Boeken
  alone rather than two buttons that look real but do nothing. The toolbar's button shares the
  `.js-book-invoices-btn` class with the topbar button and the mobile quick-action tile (three
  elements, one class) — `gl-v2-invoices.js` already enables/disables all of them together;
  adding this third instance needed zero new JS for that part. Reaching the *exact* bottom edge of
  `.gl-v2-table-card` needed one more thing: `.gl-v2-table-card` is a flex column, but that alone
  doesn't make its descendants stretch — `.datatables-header-footer-wrapper` and `<form>` were each
  just as tall as their own content, so the toolbar could sit with visible white space beneath it.
  `flex:1;min-height:0` now runs down that whole chain (wrapper → form → `.table-responsive-md`),
  with `.table-responsive-md` the only one that actually grows to soak up the remainder — so the
  toolbar (`flex:none`) is always flush against the card, and any imprecision in
  `syncTablePageLength()`'s row-count math (below) shows up as slack *inside* the table area
  instead of as a gap under the toolbar.
- **Pagination/density (optie 4a):** the table always fills the available screen height — page size
  is computed from real available space, not the other way around. `syncTablePageLength()` measures
  `.gl-v2-table-card`'s rendered height minus the `<thead>` height, the DataTables footer row
  height, and a constant reserved for the selection toolbar (reserved whether or not it's currently
  visible — otherwise the row count would jump by one every time a checkbox is (un)checked), divides
  by the fixed 54px row height, and calls `table.page.len(rows).draw(false)`. Runs once after init
  and again on window resize (150ms debounce); guarded to only redraw when the computed count
  actually changed. The manual page-length dropdown (10/25/50) is gone — `layout.topStart` was
  already overridden to an empty button row, so there was nothing to additionally hide. Footer
  copy is "N van TOTAAL facturen" (via `infoCallback`, not DataTables' default `_START_ tot _END_`
  range string) and pagination buttons are 30×28/7px-radius squares with `‹`/`›` glyphs
  (`language.paginate`) — both matching option 4a exactly rather than DataTables' Bootstrap
  defaults.
- **Icons:** row actions and the Status column both moved off Boxicons/Font Awesome onto Phosphor
  (see the Icons section below for why that's the rule everywhere in gl-v2) — `fs-5` (Bootstrap's
  20px utility) came off with them, sizing is this file's own now (15px row actions, 16px status).
  Status icon/color is a 4-family palette reused from the buttons/row-actions above rather than the
  page's old per-status Bootstrap colors (`#0dcaf0`, `#ffc107`, `#fd7e14`, `#dc3545`, `#6f42c1`,
  `#0d6efd`, …): **muted** (`--gl-v2-muted`) = nothing's happened yet (Draft, Cancelled), **gold**
  (`--gl-v2-gold`) = in progress (Issued, Sent, PartiallyPaid, Generating), **Primary-green** =
  done/succeeded (Paid, Booked), **`#8B2A2A`** (the danger red) = needs attention (Overdue).
  PartiallyPaid reuses Paid's own glyph (`ph-check-circle`) in gold rather than a different icon —
  reads as a step toward the same end state, not an unrelated status. `Generating`'s spinner has no
  Phosphor/Boxicons equivalent of Boxicons' `bx-spin` utility class, so it's `ph-spinner-gap` plus
  a small `.gl-v2-spin` `@@keyframes` rotation defined here. Every status icon (and the separate
  "verzonden via peppol" `ph-planet` indicator next to it) carries a real Bootstrap tooltip
  (`data-bs-toggle="tooltip"`, initialized globally by `common.js` for anything with that attribute
  present at page load) — `r.StatusLabel` (already existed, used for the column's `data-order`) is
  the tooltip text, so no new label had to be invented.
- **Loading state (optie 4f "LADEN"):** visible from first paint with no JS required to *show* it —
  only `gl-v2-invoices.js` adding `.is-ready` to `.gl-v2-table-card` (right after DataTable init +
  the first `syncTablePageLength()`) hides it again. This isn't decorative: without it, the raw,
  un-paginated `<table>` (every row the server sent, unsorted) would flash on screen for however
  long jQuery/DataTables takes to parse and initialize before shrinking down to the real page size.
  Column layout mirrors the real table's 11 columns exactly (see "Column layout" above) rather than
  the mockup's own simplified 4-column LADEN demo — a skeleton that doesn't share the real table's
  column count/order would visibly reflow the instant it's replaced. Fixed 6 placeholder rows (not
  the computed real count) since it's on screen for a fraction of a second at most.
- **Empty states (optie 4f "LEGE STAAT"):** two, for two different real conditions, sharing one
  `.gl-v2-empty-state` treatment (62px tinted icon badge, serif heading, muted description,
  action buttons) — **not** the same message, because they're not the same situation:
  - *Server-side* (`IndexV2.cshtml`'s own `else` branch, `Model` has zero invoices at all): "Geen
    facturen gevonden" / "Er zijn nog geen facturen aangemaakt voor dit bedrijf.", one button
    (+ Nieuwe factuur, `canWriteInvoices`-gated). No "Filters wissen" button — nothing is filtered,
    this company just has no invoices yet.
  - *Client-side* (`DataTable.language.zeroRecords` in `gl-v2-invoices.js`, invoices exist but the
    current search/bookyear filter matches none): same visual shell, but built as an HTML string
    (DataTables renders `zeroRecords` unescaped) with the reference's own copy ("Er zijn geen
    facturen die aan deze filters voldoen. Pas de filters aan of maak een nieuwe factuur.") and
    **two** buttons — Filters wissen (real: clears `#search-term` + resets the bookyear select to
    its default, then redraws) plus + Nieuwe factuur. `config.createUrl`/`config.canWrite` (new
    entries in `window.glV2InvoicesConfig`) carry the Create-URL and write-permission into the
    plain-.js file, same pattern already used for `deleteUrl`/`defaultBookyear`.

### Select / Dropdown
Design-handoff optie 4h ("Dropdowns — gesloten veld, basis, met zoekveld, met 'nieuw item',
telkens ook met icoon"). A generic, reusable component (`gl-v2-shell.css`, not page-specific) built
for the Facturen bookyear filter — the only real instance today — but specified in full per the
reference, same "build the whole system, apply the part that's real" approach used elsewhere in
this pilot. A native `<select>` can't render any of this: the browser draws its own open listbox,
entirely outside CSS reach, so this is a from-scratch trigger + JS-positioned panel
(`gl-v2-invoices.js`) — same shape as the rail flyouts and the table's own tablet ···-menu
(`getBoundingClientRect`-based positioning, outside-click/Esc closes).

- **Trigger** (`.gl-v2-select-trigger`) — four states: **leeg** (placeholder text, muted),
  **ingevuld** (real value, darker/600-weight, icon turns Primary), **hover/focus** (hover: Primary
  border + `#F7F9F5` tint; `:focus-visible`: Primary border + 2.5px gold outline, 2px offset —
  same gold-ring language as buttons, just an outline instead of a box-shadow since this element
  already has a real border to keep), **fout/geblokkeerd** — two distinct disabled-ish states, not
  one: `.is-error` (`#8A3B2A` border/text, `#FBF4F2` fill — a real validation failure) vs `:disabled`
  (hairline border, `#F2F2F0` fill, `#A8B3A6` text — inert, not invalid). Bookyear only ever uses
  leeg/ingevuld/hover/focus — it's an optional filter, never actually invalid or disabled, so
  `.is-error` ships unused (component ready, nothing to wire it to, same call as the rail's
  badge/disabled states).
- **Icon slot** — optional leading icon, "refers to the kind of data, not the action" (the
  reference's own phrase) — bookyear's is `ph-calendar`. Recolors with the trigger's state (muted
  at rest, Primary once filled/open, error/disabled red-ish or muted-light).
- **Panel — three variants**, only the first wired to anything real:
  - **Basis** (`.gl-v2-select-panel`, used by bookyear) — plain option list, no header. Each row
    (`.gl-v2-select-option`) reserves a 15px leading icon slot for a checkmark that's
    `visibility:hidden` unless `.is-selected` (keeps every row's text aligned regardless of which
    one is currently picked, rather than the selected row alone shifting over). Bookyear's list is
    short and fixed (a handful of years) — exactly what this variant is for.
  - **Met zoekveld** (`.gl-v2-select-panel-search`) — adds a bordered search input header + a
    "N RESULTATEN" label above the list. Defined, unused: nothing on this page needs to search a
    dropdown's own options today.
  - **Met zoekveld + nieuw item** (`.gl-v2-select-panel-newitem`) — the search variant plus a fixed
    footer (outside the scrollable option list, `max-height:196px` on the list itself) holding a
    dashed-border "+ Nieuw item toevoegen" button — the reference's own reasoning for the dashed
    border applies here too (a real but placeholder-flavored affordance, same dashed-border
    vocabulary used elsewhere in gl-v2 for "acknowledged, not fully wired yet"). Also unused today.
- **Tablet (768–1023.98px)** — same range as the userbox/table's own tablet states, not a
  component-specific breakpoint. Despite the reference's own "COMPACTER" heading, the actual
  numbers grow (44px trigger/rows, up from 40px/9px×10px) — "compacter" describes the panel as a
  whole next to its surroundings, not the touch targets themselves.
- **Mobiel (<768px) — bottom sheet.** The same panel becomes a `position:fixed` bottom sheet
  (rounded top corners, drag-handle `::before`, a title + "Sluiten" header that only exists for
  this form factor) with a dimming backdrop — `gl-v2-invoices.js`'s `positionBookyearPanel()`
  explicitly skips its `getBoundingClientRect()` math below 768px and lets this CSS pin the panel
  instead. `.gl-v2-select-backdrop`/`.gl-v2-select-sheet-header` are `display:none` outside this
  breakpoint by default — without that base rule they'd render as bare unstyled `<div>`s at
  desktop/tablet widths instead of not existing at all.

### Tekstvelden
Design-handoff optie 4i ("Tekstvelden — label, placeholder, met en zonder icoon, zelfde 40px als
de dropdown"). Generic, reusable component (`gl-v2-shell.css`, not page-specific), same height/
radius language as the Select/Dropdown component above (40px/8px desktop, 44px/10px tablet+phone
— unlike the dropdown, one shared media query, since a text field never becomes a bottom sheet on
phone). Built for the Facturen search field — the only real instance today — same "build the whole
system, apply the part that's real" approach used elsewhere in this pilot.

- **Structure** (`.gl-v2-field`) — an optional `.gl-v2-field-label` (uppercase, 9px), the
  `.gl-v2-field-box` shell (border, icon slot, `.gl-v2-field-input`), and an optional
  `.gl-v2-field-help` line below. All three stack in one `flex-direction:column;gap:5px` wrapper.
- **State without JS.** Most states are pure CSS, no state class needed from the caller: **leeg vs.
  ingevuld** (`:not(:placeholder-shown)` on the input — darker/600 text, icon turns Primary; this
  means every field using this component must always carry a real `placeholder` attribute, or the
  pseudo-class never matches and the field reads as permanently "empty"), **focus**
  (`:focus-within` on the *whole* `.gl-v2-field`, not just the box — the label sits as the box's
  sibling, so a box-only `:focus-within` can never reach it; same gold-ring language as the
  dropdown/buttons, `outline: 2.5px solid gold, offset 2px`), **geblokkeerd**
  (`:has(.gl-v2-field-input:disabled)` on the box — the native `disabled` attribute alone drives
  the whole visual, no companion class to remember). **Fout** stays an explicit `.gl-v2-field.is-
  error` class — there's no native pseudo-class for "this value failed validation", so this one
  state still needs the caller (or a future form's JS) to set it.
- **Icon slot** — same "refers to the kind of data, not the action" rule as the dropdown's icon,
  16px desktop / 17px touch, muted at rest, Primary once filled/focused, error/disabled red-ish or
  muted-light (mirrors the icon-recoloring rules already established for the dropdown/buttons).
- **Clear ("×") button** (`.gl-v2-field-clear`) — shown only once the field has content, via the
  same `:not(:placeholder-shown)` trick (no JS-toggled class to show/hide it, only the click itself
  needs a handler). Used by the Facturen search field: clicking it empties `#search-term`, re-runs
  `table.search("").draw()`, and returns focus to the field (`gl-v2-invoices.js`).
- **Prefix/suffix slots** (`.gl-v2-field-prefix` — plain inline text like `€`; `.gl-v2-field-suffix`
  — a small pill-chip like a currency-code badge) and the **meerregelig** variant
  (`.gl-v2-field-box-textarea`, 104px, top-aligned content, 1.6 line-height) are built per the
  reference but unused today — no field on any gl-v2 page needs a suffix or multiple lines yet.
- **Field group** (`.gl-v2-field-group`) — two `.gl-v2-field`s in a row (e.g. street + number),
  sharing the same 40/44px height as the dropdown so a row mixing a field and a dropdown still
  lines up. `.gl-v2-field-narrow` fixes one member to 92px instead of splitting the row evenly.
  Defined, not applied on any real form yet.
- **Real example: Facturen search field.** `Views/Invoices/IndexV2.cshtml`'s toolbar search
  replaced Bootstrap's `.input-group`/`bx-search` markup with `.gl-v2-field` + `.gl-v2-field-box`
  (icon + input + clear button) — no `.gl-v2-field-label` here, a label above a toolbar search box
  doesn't fit that context the way it would in an actual form. `.gl-v2-toolbar-search` (page CSS,
  `gl-v2-invoices.css`) gives it `flex:1;min-width:0` so it still shares the toolbar row with the
  bookyear dropdown and shrinks correctly on a phone-width screen — same min-width:0 fix the old
  `.input-group` needed before it.

### Icons
Phosphor Regular (`ph ph-*`), not the mockup's hand-drawn custom SVG paths — the mockup's icon
path data isn't recoverable from the static export (live template bindings), and Phosphor is
already loaded app-wide and the documented forward direction for the current system too. Every
gl-v2 icon should be a real Phosphor Regular glyph confirmed to exist, same discipline the current
system's own Icons section documents for its Boxicons→Phosphor migration.

## Do's and Don'ts

### Do:
- **Do** keep every new gl-v2 CSS selector scoped under `.gl-v2` — no bare-element rules, so this
  pilot can never leak onto a page that hasn't opted in.
- **Do** use Phosphor Regular icon classes, confirmed rendering, not hand-drawn SVGs.
- **Do** reserve the serif face for names (page titles, panel headings), never for controls.
- **Do** give click handlers on a hover-openable trigger idempotent "ensure open" behavior, not a
  toggle — see the Flyout panel's Named Rule above.
- **Do** treat a dashed-border placeholder as an honest, disclosed "not wired up yet" signal (the
  mobile search stub) rather than building a fake interactive control with nothing behind it.

### Don't:
- **Don't** nest a `position:fixed` overlay (flyout, mobile menu, backdrop) inside anything with
  `position:sticky` — render it as a sibling instead (see Flyout panel).
- **Don't** rely on the `[hidden]` attribute alone to hide something that also has an explicit
  `display` value in CSS — a plain class selector at equal specificity beats the browser's
  `[hidden]{display:none}` default; guard with `.thing[hidden]{display:none}` explicitly.
- **Don't** wrap `@@section` in an `@@if` in a Razor view — Razor doesn't support conditionally
  registering a section; put the `@@if` inside the section body instead.
- **Don't** give two elements the same `id` just because they do the same thing in two
  presentations (e.g. a desktop topbar button and its mobile quick-action twin) — duplicate ids
  are invalid HTML and `$('#id')`-based JS only ever reaches the first match. Share a class
  instead and target that.
- **Don't** duplicate the current system's Rust/Ochre/Taupe severity tokens or invent a fourth
  status color here — no gl-v2 screen has needed status colors yet; resolve this when one does,
  don't guess ahead of a real need.
- **Don't** set a mobile text input's font below 16px — iOS Safari auto-zooms the whole page on
  focus below that size, which can look like an unrelated layout bug (an oversized field, other
  chrome pushed off-screen) rather than what it actually is. Hit this on the mobile menu's search
  input at 13px.
- **Don't** size a full-screen `position:fixed` overlay with `inset:0` alone and assume that's
  the visible viewport — pair it with `height:100dvh` too. Plain `inset:0` can resolve against the
  browser's large viewport (address bar hidden) rather than what's actually visible with the
  address bar shown, pushing fixed content (the mobile menu footer, in this case) below the fold.
  `dvh` is a pure progressive enhancement — unsupported browsers just ignore the line.
- **Don't** touch the real `DESIGN.md` from this pilot. This draft is the only place gl-v2
  decisions get written down until adoption.
