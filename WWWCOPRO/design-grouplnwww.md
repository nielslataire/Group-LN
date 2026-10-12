<!-- WWWCOPRO v2 rebuild — design & architecture doc, specific to this project only.
     Relationship to the other design docs in this repo:
     - WWWCOPRO/DESIGN.md documents the CURRENT, still-LIVE site (Okler/Porto-based "Pine & Brass").
       Keep using that file while patching/editing the live pages — it stays accurate until a page
       is actually migrated below.
     - The repo-root DESIGN.md is for CPMCore (gl-v2 admin shell) — a different app entirely, ignore
       it here.
     - wireframe-handoff/ holds the actual mockups this doc is derived from. This file is the written
       rulebook; the mockups are the pixel source of truth:
         - wireframe-handoff/Home.dc.html — hi-fi, built Home page (open in a browser)
         - wireframe-handoff/WWWCOPRO Wireframes.dc.html — 13 pages, low-fi, sections 1 (shells),
           2 (pages 2a-2m), 3 (component defs, duplicated in the file below)
         - wireframe-handoff/WWWCOPRO Components.dc.html — component spec sheet C01-C29
     The visual identity (Pine & Brass: deep green #00532D + warm gold #C9A96E, Playfair Display +
     Avenir) is NOT changing in v2. This doc is about shedding the Porto/Okler dependency and
     rebuilding the markup/CSS clean — not a redesign. Tokens below are copied from WWWCOPRO/DESIGN.md
     on purpose; if a token needs to change, change it in both files so there is still only one brand. -->

---
name: Group LN — Public Site (v2 rebuild)
description: Architecture & migration doc for rebuilding WWWCOPRO without the Okler/Porto theme — same Pine & Brass brand, clean markup
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
  body:
    fontFamily: "'Avenir', 'Open Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.7
  label:
    fontFamily: "'Avenir', 'Open Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
    fontSize: "11px"
    fontWeight: 700
    lineHeight: 1.4
    letterSpacing: "1.8px"
---

# WWWCOPRO v2 — Rebuild Doc

**Full rationale for the brand itself (why these colors, why Playfair+Avenir, Do's/Don'ts) lives in [WWWCOPRO/DESIGN.md](DESIGN.md) — this file does not repeat it.** What follows is specific to the rebuild: what's changing, what isn't, how it's staged, and the component/page inventory to build against.

## What's changing vs. what isn't

- **Not changing:** the Pine & Brass visual identity — same colors, same Playfair Display / Avenir pairing, same component *look* (buttons, cards, section kickers, the three page shells). Confirmed by the wireframes above, which carry the exact same hex values and fonts as the current live CSS.
- **Changing:** the implementation. The current site is built on a heavily-overridden Okler/Porto Bootstrap admin theme — its grid, its JS bundles, its base component CSS get fought and overridden everywhere. v2 drops that dependency and builds each page from the component spec sheet (C01–C29 below) as hand-rolled, semantic HTML/CSS. No template flourishes to suppress, no fighting an admin theme that was never meant for a marketing site.
- **Decided: no Bootstrap in v2.** No `.container`/`.row`/`.col-*`, no Bootstrap JS. Layout is plain CSS Flexbox/Grid per component (see `Content/v2/*.css`). This also means no jQuery dependency for v2 behaviour — the shell's sticky header, nav overlay, hero search dropdowns and scroll-reveal are all vanilla JS (`Scripts/v2/site.js`).
- **Decided: v2 is built fully offline, not staged behind a preview URL.** It's not deployed and not linked from anywhere live; it only becomes reachable the moment the whole thing gets merged and deployed in one go. `robots.txt` still disallows `/v2/` as a belt-and-suspenders measure, but that's not the real safeguard — not deploying it is.

## Scope boundary with the live site

The live site must keep working, and must stay editable, for the entire duration of this rebuild.

- **Never edit in place.** Everything under `WWWCOPRO/Views/`, `WWWCOPRO/Controllers/`, and the existing `WWWCOPRO/Content/*.css` is the live production path. It stays untouched until the specific page it serves is migrated (see checklist below) — including small content tweaks requested while v2 is in progress; those still go into the old files.
- **New code lives in isolation — built, in this repo, as `Areas/V2/`.** Own Controllers, Views and `Content/v2/`/`Scripts/v2/` CSS/JS tree, registered via `Areas/V2/V2AreaRegistration.vb` under the `/v2/...` URL prefix. Nothing in the old tree references anything in the new tree or vice versa.
- **Scope every v2 rule under one wrapper class** (`.wwwcopro-v2` on `<body>`, see `Content/v2/tokens.css`/`layout.css`) — the same technique used for the CPMCore gl-v2 pilot. This guarantees zero CSS bleed between old and new even while both ship in the same deployed app during the transition.
- **Fully offline until the single merge.** Nothing gets deployed or linked from the live site while v2 is being built — `/v2/...` exists only in this repo/branch, not on any reachable environment. `robots.txt` disallows `/v2/` as an extra safeguard, but the real one is simply: don't deploy it yet.
- **The one-shot cutover.** When the rebuild is done, the plan is a single merge that swaps the live routes over to the new Area's controllers/views in one go, rather than a page-by-page incremental cutover. The old Views/Controllers/Content stay in the repo as the rollback reference until that merge is confirmed good in production, then get removed in a separate cleanup pass (including the Okler/Porto theme assets).
- **Backend logic is out of scope** unless stated otherwise — Controllers/Service classes (contact form handling, newsletter signup, etc.) are reused as-is; this rebuild is the view/markup/CSS layer.

## Page shells

Three shells, shared footer + menu overlay, background always Warm Off-White (`#F2F5EF`):

| Shell | Used by | Header |
|---|---|---|
| **Home** | Home only | Transparent over full-bleed hero video/poster, fades to solid Deep Green on scroll (~175px) |
| **Standard** | Projecten, Project detail, Inschrijving, Blog, Blog artikel, Vacatures, Contact, Legal | Solid Deep Green from top + light "page-header" band (breadcrumb, title, hairline rule) |
| **Hero** | Over ons (hub/Projectbegeleiding/Grondverwerving), Vacatures index | Transparent over a solid-green hero block (no video/photo) |

**Breadcrumb rule:** only shown on pages 3+ levels deep (Home / Section / Page). Top-level pages show none.

## Pages (from the wireframes)

| ID | Page |
|---|---|
| 2a | Home — **hi-fi built**, see `Home.dc.html` |
| 2b | Projecten — overview |
| 2c | Project detail |
| 2d | Project inschrijving |
| 2e | Over ons — hub |
| 2f | Over ons › Projectbegeleiding |
| 2g | Over ons › Grond- & pandverwerving |
| 2h | Blog — overview |
| 2i | Blog artikel |
| 2j | Vacatures — overview |
| 2k | Vacature detail |
| 2l | Contact |
| 2m | Legal (privacy / cookies / voorwaarden) |

## Component inventory (C01–C29)

Full visual spec for each lives in `wireframe-handoff/WWWCOPRO Components.dc.html` — this is the index to build against.

| ID | Component |
|---|---|
| C01 | Button |
| C02 | Section kicker + heading |
| C03 | Sectie-kop |
| C04 | Page header |
| C05 | Breadcrumb |
| C06 | Green hero |
| C07 | Site header |
| C08 | Menu overlay |
| C09 | Footer |
| C10 | Hero zoekbalk + dropdown |
| C11 | Project kaart |
| C12 | Uitgelicht project |
| C13 | Status badges & pills |
| C14 | Info kaart |
| C15 | Eenheden tabel |
| C16 | Foto galerij |
| C17 | Blog kaart |
| C18 | Haarlijn-kolommen |
| C19 | Genummerde rijen |
| C20 | Fase-kolommen + vinkjeslijst |
| C21 | FAQ accordeon |
| C22 | Formulierveld |
| C23 | Contact info paneel |
| C24 | CTA blok |
| C25 | Vacature kaart + pills |
| C26 | Artikel blokken |
| C27 | Contact modal |
| C28 | Cookie consent |
| C29 | Scroll reveal |

## Migration log

Keep this updated as pages get built in `Areas/V2/`. Nothing here is live or linked from the live site — see "Fully offline until the single merge" above.

| Page | Status | Notes |
|---|---|---|
| Home | **Built, real data wired up, matches live 1:1** — `Areas/V2/Controllers/HomeController.vb`, `Areas/V2/Views/Home/Index.vbhtml`, `Content/v2/home.css`, `Helpers/HomeHeroDataHelper.vb` | Rebuilt to mirror the live homepage exactly (sections, copy, order, colors), re-skinned without Porto: video hero (not a static poster) + search bar (C10, real DB options, submits to `Projects/Index`) + "Wie zijn we" about-section (was missing from the first pass) + troeven (C19, no fabricated numbering — the live page doesn't show any) + full-bleed featured-project (C12, real `HomeHeroProject` data — first pass wrongly used a boxed card layout) + CTA (C24, light section with a solid button — first pass wrongly made it a solid-green card). Footer colors corrected: column titles are white not gold, and the bottom bar is the *same* green as the rest of the footer, not a darker one (verified against footer.css's actual `var(--color-primary, #003d21)` — the fallback never applies since `--color-primary` is always defined). |
| All others | Not started | — |

### Shared v2 shell — built
`Areas/V2/Views/Shared/_LayoutV2.vbhtml` + `Content/v2/tokens.css` + `Content/v2/layout.css` + `Scripts/v2/site.js`: header (sticky transparent→solid), nav overlay (C07/C08), footer (C09). No Bootstrap, no jQuery.

### Real data (done)
`BuildHeroSearchOptions()` / `GetHomeHeroFeatured()` / `GetCoordinationOnlyProjectIds()` were extracted out of `WWWCOPRO.Controllers.HomeController` (where they were `Private`) into `WWWCOPRO.HomeHeroDataHelper` (`Helpers/HomeHeroDataHelper.vb`) as `Shared` methods. The live `HomeController` now calls the same helper instead of its own copy — same behaviour, no duplication, and `Areas/V2/Controllers/HomeController.vb` calls the exact same methods. Verified with a full `MvcBuildViews=true` rebuild (0 errors).

### Known gaps (intentional, for this first pass)
- **Cookie consent banner markup** is not yet in `_LayoutV2.vbhtml` (the script no-ops without it — fine while offline, but required before anything here goes live).
- **Nav overlay focus trap**: Escape-to-close and focus-restore-on-close are done; full tab-cycle trapping inside the open panel is not yet.
