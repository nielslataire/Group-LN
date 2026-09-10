---
name: Group LN — Public Site
description: Deep pine green and a single warm-gold accent over warm off-white — a Belgian residential developer's marketing site
colors:
  deep-green: "#00532D"
  green-hover: "#006638"
  green-dark: "#003D21"
  warm-gold: "#C9A96E"
  gold-hover: "#B8935A"
  olive: "#3D7A4E"
  near-black-green: "#2C3B2A"
  green-mid: "#7A9E6E"
  green-light: "#D6E5CC"
  warm-offwhite: "#F2F5EF"
  text-muted: "#5A6B58"
  hairline: "rgba(0, 83, 45, 0.10)"
  brick-red: "#C0392B"
  brick-red-tint: "#FDF1F0"
  white: "#FFFFFF"
typography:
  display:
    fontFamily: "'Playfair Display', 'Times New Roman', Georgia, serif"
    fontSize: "39px"
    fontWeight: 500
    lineHeight: 1.15
    letterSpacing: "normal"
  headline:
    fontFamily: "'Playfair Display', 'Times New Roman', Georgia, serif"
    fontSize: "34px"
    fontWeight: 500
    lineHeight: 1.2
    letterSpacing: "normal"
  title:
    fontFamily: "'Playfair Display', 'Times New Roman', Georgia, serif"
    fontSize: "19px"
    fontWeight: 600
    lineHeight: 1.3
    letterSpacing: "normal"
  body:
    fontFamily: "'Avenir', 'Open Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.7
    letterSpacing: "normal"
  label:
    fontFamily: "'Avenir', 'Open Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    fontSize: "11px"
    fontWeight: 700
    lineHeight: 1.4
    letterSpacing: "1.8px"
rounded:
  xs: "3px"
  sm: "4px"
  md: "6px"
  lg: "8px"
  round: "50%"
spacing:
  xs: "8px"
  sm: "12px"
  md: "16px"
  lg: "24px"
  xl: "32px"
  xxl: "48px"
  section: "100px"
components:
  button-primary:
    backgroundColor: "{colors.deep-green}"
    textColor: "{colors.white}"
    rounded: "{rounded.md}"
    padding: "16px 28px"
  button-primary-hover:
    backgroundColor: "{colors.green-hover}"
    textColor: "{colors.white}"
    rounded: "{rounded.md}"
    padding: "16px 28px"
  button-outline:
    backgroundColor: "transparent"
    textColor: "{colors.near-black-green}"
    rounded: "{rounded.md}"
    padding: "14px 22px"
  button-outline-hover:
    backgroundColor: "{colors.deep-green}"
    textColor: "{colors.white}"
    rounded: "{rounded.md}"
    padding: "14px 22px"
  button-light:
    backgroundColor: "{colors.white}"
    textColor: "{colors.near-black-green}"
    rounded: "{rounded.sm}"
    padding: "16px 30px"
  button-light-hover:
    backgroundColor: "{colors.warm-gold}"
    textColor: "{colors.near-black-green}"
    rounded: "{rounded.sm}"
    padding: "16px 30px"
  button-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.white}"
    rounded: "{rounded.sm}"
    padding: "16px 24px"
  icon-button-round:
    backgroundColor: "{colors.warm-gold}"
    textColor: "{colors.near-black-green}"
    rounded: "{rounded.round}"
    size: "46px"
  input:
    backgroundColor: "{colors.white}"
    textColor: "{colors.near-black-green}"
    rounded: "{rounded.xs}"
    padding: "12px 16px"
  input-focus:
    backgroundColor: "rgba(0, 83, 45, 0.03)"
    textColor: "{colors.near-black-green}"
    rounded: "{rounded.xs}"
    padding: "12px 16px"
  card:
    backgroundColor: "{colors.white}"
    textColor: "{colors.near-black-green}"
    rounded: "{rounded.xs}"
    padding: "24px"
  section-kicker:
    backgroundColor: "transparent"
    textColor: "{colors.deep-green}"
    typography: "{typography.label}"
    padding: "0"
  nav-link:
    backgroundColor: "transparent"
    textColor: "{colors.white}"
    typography: "{typography.label}"
    padding: "0"
---

# Design System: Group LN — Public Site

## Overview

**Creative North Star: "Pine & Brass"**

The Group LN public site is built from two materials. A **deep pine green** does almost all the structural work — it fills the site header, the footer, the primary buttons, the links, and the small tracked labels that head every section. Against it sits exactly one accent: a **warm brass-gold**, used sparingly for the things that must be noticed first — a call-to-action over a photograph, the round menu button, a focus ring, a hover state on an otherwise quiet outline. Everything else is warm neutral: pages rest on a green-tinted off-white, never pure white and never cool grey, and text is a near-black green rather than true black. The result should feel like worked timber and aged metal — grounded, warm, and unhurried, the surface of a company that has been building the same kind of thing in the same region for nearly three decades.

The type carries the warmth. **Playfair Display** sets every heading — high-contrast, slightly old-world, confident at large sizes — while **Avenir** handles body copy and the uppercase labels with a clean, humanist calm. Headings are large and set in a medium weight (500), never bold and shouty; the drama comes from scale and the serif's own contrast, not from weight. Body text runs generous and airy (16px, line-height 1.7).

Components lean **warm and tactile**. Buttons carry real vertical padding (14–16px), primary actions over imagery lift on a genuine soft shadow, cards raise slightly on hover, and the recurring round brass icon button (menu, socials) reads as a physical control you press. Depth is used, not avoided — but it is quiet: hairline green borders separate most surfaces, and a heavy shadow is reserved for things that genuinely float (dropdowns, the mobile menu, a hovered project card).

This site is implemented on a heavily-overridden Okler/Porto admin theme. The theme is the substrate, not the identity: its loud components, cool greys, and template flourishes are actively suppressed. This file documents the Group LN system that sits on top.

**Key Characteristics:**
- Two-material identity: deep pine green structure + one warm-gold accent, nothing else saturated.
- Warm neutrals only — green-tinted off-white page (`#F2F5EF`), near-black-green text, hairline green borders. No cool blue-grey.
- Playfair Display headings at medium weight; Avenir body and uppercase tracked labels.
- A small uppercase kicker heads most sections — an established, deliberate convention here.
- Near-flat surfaces with a tactile touch: soft lift on primary CTAs and hovered cards; structural shadow only for floating layers.
- Base corner radius 3px; bespoke marketing components soften to 4–8px; icon buttons and avatars are fully round.
- Solid pine-green header (transparent over the home hero) and a solid pine-green footer.

## Colors

Two greens and one gold do the work; every neutral is warm.

### Primary
- **Deep Green** (`#00532D`): the identity colour. Fills the site header (solid on every page except the home hero, where it is transparent until scroll), the footer, primary buttons, text links, section kickers, and form-field focus borders. Used on large surfaces (header/footer) and as a single accent mark in content.
- **Green Hover** (`#006638`): one step lighter — the hover/active fill for primary green buttons and the colour of hovered links. Never used for resting text.
- **Green Dark** (`#003D21`): one step darker — the footer copyright underbar and the tablet-width fallback background for the hero search dropdowns.

### Secondary
- **Warm Gold** (`#C9A96E`): the single accent. Fills the round menu button, the submit button inside the hero search bar, footer social hovers and the footer ribbon; supplies CTA fills and outline-button hover states over imagery; is the resting colour of every uppercase kicker set on a dark background; and is the keyboard focus-ring colour. Appears rarely and always to direct attention.
- **Gold Hover** (`#B8935A`): the darker press/hover state for gold surfaces.

### Tertiary
- **Olive** (`#3D7A4E`): a mid green defined in the token set for occasional non-primary emphasis. Used sparingly.
- **Green Mid** (`#7A9E6E`) / **Green Light** (`#D6E5CC`): soft green tints available for wash surfaces and faint fills; used lightly.

### Neutral
- **Near-Black Green** (`#2C3B2A`): all body text and every heading colour. Warmer and softer than true black.
- **Warm Off-White** (`#F2F5EF`): the background behind all page content and the page-header band. Green-tinted, never `#fff`, never cool grey.
- **White** (`#FFFFFF`): cards, the hero CTA fill, form fields, dropdown/menu panels.
- **Text Muted** (`#5A6B58`): secondary and supporting copy, captions, form placeholders (at ~60% opacity), meta text.
- **Hairline** (`rgba(0, 83, 45, 0.10)`): the default 1px divider and card border — a barely-there green, not a grey. On dark green surfaces the equivalent is `rgba(255,255,255,0.15–0.3)`.

### Semantic Status
- **Brick Red** (`#C0392B`) with tint `#FDF1F0` and border `#F3C6C2`: form validation errors — field messages, invalid-field borders, and the AJAX error banner. A warm brick red in the palette's family, never stock Bootstrap `#dc3545`.

### Named Rules
**The Two-Material Rule.** Deep Green and Warm Gold are the only saturated colours on any screen. Green is structure and default action; gold is the one thing to look at first. A third saturated colour is either a validation error (Brick Red) or a mistake.

**The Warm-Neutral Rule.** Every neutral leans warm and green. Reach for Warm Off-White and the green Hairline before any cool `#f1f5f9`-family grey. The page is never pure white.

**The Gold-Is-Rare Rule.** If gold appears more than about once per viewport of content, it has stopped meaning "look here". Kickers on light backgrounds are green, not gold; gold kickers are only for dark/photographic backgrounds.

## Typography

**Display / Headline Font:** Playfair Display (with `'Times New Roman', Georgia, serif`)
**Body / Label Font:** Avenir (self-hosted, weights 300/400/500/700/900), falling back to `'Open Sans', -apple-system, system sans`

**Character:** Playfair Display is a high-contrast transitional serif — a little classical, quietly premium, and strongest at large sizes where its thick-thin contrast reads. Avenir is a warm geometric humanist sans that keeps body copy and small uppercase labels calm and legible. The pairing is "estate letterpress meets clean signage". Hierarchy comes from **scale and family**, not from heavy weights: headings sit at weight 500, never 700+.

### Hierarchy
- **Display** (Playfair, 500, ~39px, line-height 1.15): the home hero headline and the largest page-hero titles. Scales down to ~30px on phones.
- **Headline** (Playfair, 500, ~34px, line-height 1.2): section headlines across content pages; the footer quote runs a touch larger (~38px). Drops to ~26px on phones.
- **Title** (Playfair, 600, ~19px, line-height 1.3): sub-section headers and card/list-item titles (e.g. the "troeven" items).
- **Body** (Avenir, 400, 16px, line-height 1.7): default paragraph text and form values. Supporting paragraphs use Text Muted at 15–16px.
- **Label** (Avenir, 700, 10–13px, letter-spacing 1–2.6px, UPPERCASE): section kickers, footer column titles, button text (~13px / 1.2px tracking), and nav links. Not italic, never Playfair.

### Named Rules
**The Medium-Weight Heading Rule.** Headings are Playfair at 500. Do not reach for 700/800 or a second family for emphasis — go up in size, or let the serif's own contrast carry it.

**The Uppercase-Label Rule.** Small tracked uppercase text is always Avenir 700. It is the only place tracking is used; body and headings are set at normal spacing.

## Layout

Bootstrap's `.container` and 12-column grid (inherited from the Porto theme) on a Warm Off-White canvas. Content sections run `padding: 100px 0` on desktop, tightening to `64px 0` on phones. Multi-column content grids (the "troeven" / "waarom" blocks) are a 4-up grid with `32px` gaps that collapses 4 → 2 → 1 at 991px and 767px.

**Header:** a fixed, sticky bar. On the home page it is transparent over the hero video and fades to solid Deep Green on scroll (`stickyStartAt` ~175); on every other page it is solid Deep Green from the top. Redesigned header row is `min-height: 70px` (80px on phones); the logo mark is 50px (40px on phones). Left side: logo + brand text + a phone link. Right side: up to four uppercase nav links (hidden ≤991px) and a round brass **menu button** that opens a full navigation overlay.

**Footer:** solid Deep Green, `padding-top: 72px`, opening with a large Playfair quote, then a hairline divider, then a 4-column link grid (`1.3fr 1fr 1fr 1fr`, `32px` gap), then a darker (`#003D21`) copyright underbar. All footer text is white at 68–90% opacity; links lift to full white on hover.

**Responsive:** breakpoints at 991px, 767px, and 576px. Below 991px the desktop nav strip is replaced entirely by the menu overlay. Every interactive target stays ≥44px; the site is expected to work on a phone on-site, not only on a desktop.

### Named Rules
**The Warm-Canvas Rule.** Page background is always Warm Off-White (`#F2F5EF`). Cards and panels are white on top of it; the tonal step plus a hairline border is what separates them.

## Elevation & Depth

Near-flat, with a deliberate tactile touch. Most separation is a 1px green Hairline plus the tonal step between a white card and the Warm Off-White page. But this system is not shadow-averse: primary actions and hovered objects get a genuine soft shadow, and floating layers get an obvious one.

### Shadow Vocabulary
- **CTA lift** (`box-shadow: 0 10px 30px rgba(0, 0, 0, 0.28)`): under the white hero CTA sitting on a photograph or video, so it reads as pressable and clearly foremost. Deepens on hover with a `translateY(-2px)`.
- **Card hover** (`box-shadow: 0 6px 24px rgba(0, 0, 0, 0.09)` rising toward `0 16px 40px rgba(0, 0, 0, 0.12)`): content cards are near-flat at rest and lift on hover, paired with a small `translateY`.
- **Overlay** (`box-shadow: 0 16px 40px rgba(0, 0, 0, 0.45)`): structural depth for things that truly float — the hero search dropdowns, the navigation overlay panel, modals.
- **Media gradient**: hero and featured-project sections use a bottom-anchored dark gradient (`linear-gradient(..., rgba(0,20,10,0.6–0.75))`) rather than a box-shadow to seat text over imagery.

### Named Rules
**The Float-Earns-Shadow Rule.** A heavy (Overlay-strength) shadow means the element literally floats above the page — a menu, a dropdown, a dialog. Resting content gets a hairline border and, at most, the soft Card-hover lift on interaction.

## Shapes

Gently squared, warming at the edges. The theme forces a **3px** base radius on buttons, inputs, cards, thumbnails and images (`.btn, button, input, .card, .thumbnail { border-radius: 3px !important }`). Bespoke marketing components soften a step: home-page buttons and the hero search bar use **4px**, section CTAs and outline buttons use **6px**, and floating panels / status banners use **8px**. Icon buttons (the round menu button at 46px, the 38px footer social buttons, the 32px header phone chip) and avatars are fully **round (50%)**. Borders are 1px Hairline on light surfaces and `rgba(255,255,255,0.15–0.3)` on dark.

A recurring silhouette: **columned lists with a thin left rule** — each item gets `border-left: 1px` and `padding-left: 28px`, the first item flush. Used for the "troeven" and "waarom" blocks. Section headers on some pages carry a **trailing hairline rule** that extends from the title to fill the row (`.sectie-kop::after { flex: 1; height: 1px }`).

### Named Rules
**The 3-to-8 Rule.** Corners run 3px (theme base) → 4–6px (bespoke marketing buttons and sections) → 8px (floating panels). Nothing in between drifts to a random one-off value, and nothing that isn't a round icon button or avatar becomes a pill.

## Components

### Buttons
Warm, generously padded, uppercase-labelled (Avenir 700, ~13px, `letter-spacing: 1.2px`). Four variants:
- **Primary (solid green):** Deep Green fill, white text, `16px 28px` padding, 6px radius. Hover → Green Hover fill. Used for the main action on content pages ("Neem contact op").
- **Outline:** transparent, `1.5px solid Near-Black Green`, Near-Black Green text, `14px 22px`, 6px. Hover inverts to a Deep Green fill with white text. Used for secondary links ("Meer over Group LN").
- **Light (on media):** solid white, Near-Black Green text, `16px 30px`, 4px, carries the CTA-lift shadow. Hover → Warm Gold fill (text stays Near-Black Green), `translateY(-2px)`. This is the hero's primary CTA over the video.
- **Ghost (on media):** transparent (`rgba(255,255,255,0.06)`), `1px solid rgba(255,255,255,0.55)`, white text, 4px. Hover raises the fill to `rgba(255,255,255,0.14)` and the border to solid white. The hero's secondary "open the search" trigger — clearly quieter than the white Light button beside it.
- **Round icon button:** 46px circle, Warm Gold fill, Near-Black Green icon strokes, faint `inset 0 0 0 1px rgba(0,0,0,0.12)` edge. The navigation menu toggle and (at 38px) the footer social buttons.
- **Disabled / arrow motion:** links and CTAs carry a trailing `fa-arrow-right` that nudges `translateX(3px)` on hover.

### Cards / Containers
- **Corner Style:** 3px (theme base).
- **Background:** white on the Warm Off-White canvas.
- **Shadow Strategy:** near-flat at rest (hairline border only); some content cards carry a faint resting shadow (`0 8px 32px rgba(0,0,0,0.10)`) and all interactive cards lift on hover (see Elevation).
- **Border:** 1px Hairline (`rgba(0,83,45,0.10)`).
- **Internal Padding:** 24px typical.

### Inputs / Fields
- **Style:** white fill, 1px border, 3px radius, comfortable padding (~`12px 16px`). Placeholder text is Text Muted at ~60% opacity.
- **Focus:** border shifts to Deep Green and the fill takes a faint green wash (`rgba(0,83,45,0.03)`); **no** glow or box-shadow.
- **Error:** invalid-field border becomes Brick Red (`#C0392B`); the field message is Brick Red 12px; a form-level AJAX failure shows a Brick-Red-on-`#FDF1F0` banner with an 8px radius.

### Navigation
- **Top strip (desktop):** up to four Avenir 700 uppercase links (`~13px`, `letter-spacing: 0.5px`), white, no underline. Hover/active → Warm Gold. Hidden ≤991px.
- **Menu button:** the round brass icon button (above), labelled `aria-label="Menu openen"`, present at every width. It is the *only* nav affordance ≤991px.
- **Navigation overlay:** a full-width Deep Green panel (fixed dropdown ≥992px, full-screen ≤767px) holding a centered/left-aligned list of large (22–35px) white Avenir links plus a contact/address block. Links hover to Warm Gold. It traps focus, restores focus to the menu button on close, and `inert`s the rest of the page while open.

### Section kicker (signature)
A short **UPPERCASE Avenir 700 label** (10–13px, `letter-spacing` 1.8–2.6px) directly above almost every section heading. **Deep Green** on light backgrounds, **Warm Gold** on dark/photographic backgrounds. This is an intentional, site-wide convention here — not a stray eyebrow — and new sections are expected to follow it.

### Scroll reveal (signature motion)
Content blocks tagged `.reveal` start at `opacity: 0; translateY(24px)` and transition to rest as they enter the viewport; `.reveal-slide-right` comes in from `translateX(60px)` instead. Gated behind `html.js-reveal` (so no-JS and no-`IntersectionObserver` clients simply see everything) **and** `@media (prefers-reduced-motion: no-preference)`. Staggered 100ms per sibling, capped at 4 steps. This is the one authored motion moment; everything else is ≤250ms hover/press feedback.

## Do's and Don'ts

### Do:
- **Do** keep Deep Green (`#00532D`) as the structural colour and Warm Gold (`#C9A96E`) as the single accent — about one gold mark per viewport (The Two-Material Rule, The Gold-Is-Rare Rule).
- **Do** put every page on the Warm Off-White canvas (`#F2F5EF`); white is for cards and panels on top of it (The Warm-Canvas Rule).
- **Do** set headings in Playfair Display at weight 500 and build hierarchy by size, not weight (The Medium-Weight Heading Rule).
- **Do** head new sections with the uppercase Avenir-700 kicker — green on light, gold on dark.
- **Do** separate resting surfaces with the 1px green Hairline; reserve heavy shadows for menus, dropdowns and dialogs (The Float-Earns-Shadow Rule).
- **Do** give buttons real vertical padding (14–16px) and let primary CTAs over imagery carry the soft lift shadow and a `translateY(-2px)` hover.
- **Do** use Brick Red (`#C0392B` / tint `#FDF1F0`) for form validation — never stock Bootstrap red.
- **Do** keep corners in the 3 → 6 → 8px range; only round icon buttons and avatars are circular (The 3-to-8 Rule).
- **Do** keep every target ≥44px and every screen usable at 360px — on-site phone use is first-class.

### Don't:
- **Don't** introduce a third saturated colour, a gradient wash on a content surface, or gold as decoration.
- **Don't** use cool blue-grey neutrals (`#f1f5f9` family) or pure white page backgrounds (The Warm-Neutral Rule).
- **Don't** set headings bold (700+), in a second family, or with letter-spacing — tracking belongs only to the uppercase labels.
- **Don't** put a glow/box-shadow on a focused input; the border-shift + faint green wash is the focus treatment.
- **Don't** let the underlying Porto/Okler theme show through — no `section-*-scale-*` bands, revolution-slider chrome, template card shadows, or its cool greys.
- **Don't** give the desktop nav a fifth link; overflow goes into the menu overlay.
- **Don't** animate section entrances outside the `.reveal` system, and always leave the `prefers-reduced-motion` path intact.
