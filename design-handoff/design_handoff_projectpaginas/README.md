# Handoff: Redesign projectpagina's (WWWCOPRO)

## Overzicht
Redesign van twee pagina's van de Group LN-website (`WWWCOPRO`, ASP.NET MVC / Razor):

- **Projectdetail** → `Views/Projects/Detail.cshtml` (hoofdontwerp: *Project Detail v4 Split*)
- **Woonprojecten-overzicht** → `Views/Projects/Index.cshtml` (ontwerp: *Woonprojecten v2*)

Doel: premium uitstraling, een verhaal met gevoel (sfeer eerst, feiten daarna), sterke SEO/GEO (veel echte tekst, gestructureerde data, antwoordgerichte formulering), een interactieve projectkaart (klikbare percelen op een luchtfoto), een buurtkaart, een prijslijst die schaalt naar veel loten, een maandlastberekening en veel duidelijke CTA's.

## Over de designbestanden
De HTML-bestanden in deze map zijn **designreferenties**: prototypes die het beoogde uitzicht en gedrag tonen. Het is **geen productiecode** om te kopiëren. De opdracht is deze ontwerpen **opnieuw te bouwen in de bestaande WWWCOPRO-omgeving** (Razor-views, bestaande layout `_Layout.cshtml`, bestaande CSS-bestanden zoals `Content/project-detail.css` en `Content/projects-index.css`, bestaande controllers en datamodellen), volgens de patronen die daar al gebruikt worden.

Open de `.dc.html`-bestanden in een browser (naast `support.js`) om het gedrag te bekijken. Alle stijlen staan inline in de prototypes; zet ze in de echte code om naar de bestaande CSS-structuur (BEM-achtige klassen in `project-detail.css`).

## Fidelity
**High-fidelity.** Kleuren, typografie, spacing, teksten en interacties zijn definitief bedoeld. Bouw ze zo getrouw mogelijk na. Projectdata (prijzen, casco-prijzen, POI's, reistijden, tijdlijn) is **fictief**: haal ze uit het bestaande projectmodel / CMS.

---

## Design tokens

### Kleuren
| Token | Hex | Gebruik |
|---|---|---|
| Groen primair | `#00532D` | header, knoppen, accenten, groene blokken |
| Groen hover | `#006638` | hover primaire knop |
| Groen donker | `#003D21` | footer |
| Groen medium | `#3D7A4E` | slaapkamer-bolletjes |
| Groen licht | `#7A9E6E` | type-label onder lotnaam, eyebrow-kleine labels |
| Groen pastel | `#D6E5CC` | voortgangsbalk achtergrond |
| Achtergrond pagina | `#F2F5EF` | body, afwisselende secties |
| Wit | `#FFFFFF` | afwisselende secties, kaarten |
| Rij verkocht / zacht vlak | `#F5F7F2` / `#F7F9F5` | verkochte rijen, disclaimers |
| Tekst | `#2C3B2A` | hoofdtekst, titels |
| Tekst muted | `#5a6b58` | bodytekst, labels |
| Tekst disabled | `#8a948a` / `#9aa39a` | verkochte loten |
| Goud | `#C9A96E` | hamburger, accent-CTA op groen, beschikbare percelen |
| Goud hover | `#B8935A` | |
| Pill beschikbaar | bg `#EAF3DE`, tekst `#3B6D11` | status "Beschikbaar" |
| Pill verkocht | bg `#ECEEEA`, tekst `#6b736a` | status "Verkocht" |
| Lijnen | `rgba(0,83,45,0.08 / 0.12 / 0.15 / 0.25)` | scheidingslijnen, borders |

Op groene vlakken: tekst `#fff`, secundaire tekst `rgba(255,255,255,0.8–0.85)`, lijnen `rgba(255,255,255,0.2)`.

### Typografie
- **Display/serif:** `Playfair Display` (Google Fonts; 400, 500, 600, italic 400). Gebruik voor H1/H2/H3, prijzen, grote cijfers.
- **Sans:** `Avenir` (lokaal: `Content/fonts/Avenir-Book.woff2` 400, `Avenir-Heavy.woff2` 700), fallback `'Open Sans', sans-serif`.

| Rol | Font | Grootte | Gewicht | Overig |
|---|---|---|---|---|
| H1 | Playfair | `clamp(38px,4vw,64px)` | 500 | line-height 1.05, `text-wrap:balance`, tweede helft italic 400 in `#00532D` |
| H2 sectie | Playfair | `clamp(30px,3vw,44px)` | 500 | line-height 1.12 |
| H3 | Playfair | 20–30px | 500 | |
| Eyebrow | Avenir | 11px | 700 | uppercase, letter-spacing 1.8px, `#00532D` (of `#C9A96E` op groen) |
| Body groot | Avenir | 17–18px | 400 | line-height 1.75–1.8, `#5a6b58`, max-width ~600px |
| Body | Avenir | 15–16px | 400 | line-height 1.75–1.8 |
| Label klein | Avenir | 12–13px | 400/700 | |
| Knoptekst | Avenir | 12px | 700 | uppercase, letter-spacing 1px |
| Prijs groot | Playfair | 19–28px | 400 | `font-variant-numeric: tabular-nums` |
| Maandlast | Playfair | `clamp(44px,4.4vw,64px)` | 400 | tabular-nums |

### Spacing / radius / schaduw
- Sectie-padding: `clamp(48px,6vw,96px)` verticaal × `clamp(24px,4vw,80px)` horizontaal.
- Gaps: 8, 10, 12, 16, 18, 20, 24, 28, 32, 40, 48, 56px.
- Radius: knoppen 4px; kaarten/blokken 6px; pills/chips 20–22px; bottom sheet 16px boven.
- Schaduwen: CTA op foto `0 10px 30px rgba(0,0,0,0.28)`; kaartje op projectkaart `0 12px 36px rgba(0,0,0,0.25)`; bottom sheet `0 -10px 40px rgba(0,0,0,0.25)`; mobiele balk `0 -6px 20px rgba(0,0,0,0.08)`.
- Hit targets minimaal 44px (knoppen 48px).

### Knoppen
- **Primair:** bg `#00532D`, tekst wit, min-height 48px, padding 0 20px, radius 4px, hover `#006638`.
- **Outline:** bg wit, border 1.5px `#2C3B2A`, tekst `#2C3B2A`; hover bg `#00532D`, tekst wit.
- **Goud (op groen):** bg `#C9A96E`, tekst `#2C3B2A`; hover `#B8935A`.
- **Wit op foto:** bg wit, tekst `#2C3B2A`, schaduw; hover `#C9A96E`.

---

## Scherm 1 — Projectdetail (`Project Detail v4 Split.dc.html`)

### Globale layout
1. **Header** (sticky, `top:0`, z-index boven alles, hoogte 72px, bg `#00532D`): logo + "GROUP LN / PROJECTONTWIKKELING" links; rechts nav (Woonprojecten actief in `#C9A96E`, Grond aanbieden, Blog, Contact: 13px, 700, uppercase) + gouden ronde hamburger (44px). De header blijft **altijd zichtbaar**.
2. **Split-body** (desktop ≥ 1000px): CSS grid `minmax(0,1fr) minmax(0,1fr)`.
   - **Links `<aside>`**: `position:sticky; top:72px; height:calc(100vh - 72px)`. Beeld dat crossfadet (opacity 0.6s) naargelang de sectie die rechts in beeld is. Verloop-overlay: `linear-gradient(180deg, rgba(0,20,10,.35) 0%, transparent 25%, transparent 50%, rgba(0,20,10,.82) 100%)`. Linksboven het label van de huidige sectie (11px uppercase wit). Onderaan: badge (wit, groen bolletje), projectnaam (Playfair `clamp(34px,3.6vw,54px)`), daaronder een lijn + regel (lot/type/opp.) + prijs + witte knop "Informatie aanvragen" → `#contact`.
   - **Rechts `<main>`**: alle secties onder elkaar, met afwisselende achtergrond (`#F2F5EF` / `#fff`).
3. **Footer** (onder de split, volle breedte, bg `#003D21`): slogan "Bouwen aan plekken waar mensen graag thuiskomen.", 4 kolommen (logo, Projecten, Group LN, Gegevens: Klaverdries 53, 9031 Drongen, +32 (0)9 216 49 50, info@groupln.be), bottom bar © + juridische links. Komt in beeld als je tot het einde scrollt.

**< 1000px (tablet/gsm):** één kolom; de aside wordt een gewone afbeelding bovenaan (`position:relative; height:70vh`) en wisselt niet meer mee. **< 640px:** gsm-specifieke varianten (zie verder).

### Beeldwissel links (desktop)
Elke sectie rechts heeft een `data-media`-index. Een IntersectionObserver met `rootMargin: '-45% 0px -45% 0px'` bepaalt welke sectie actief is → het overeenkomstige beeld krijgt `opacity:1`. Mapping:

| idx | Sectie | Beeld |
|---|---|---|
| 0 | Intro | Hoofdbeeld gevel bij avondlicht |
| 1 | Verhaal | Sfeer interieur |
| 7 | Architectuur | Gevel, detail materialen |
| 2 | Woningen | Render; **als een lot geselecteerd is: render van dat lot** (idx 8–11) |
| 3 | Ligging | Luchtfoto / velden |
| 4 | Bouwfase | Werffoto |
| 5 | Kopen + Vragen | Interieur leefruimte |
| 6 | Contact | Portret team |

### Secties (rechterkolom, in volgorde)

**1. Intro** (`data-media=0`, bg pagina)
- Eyebrow: "Vlekkem · Erpe-Mere · Oost-Vlaanderen"
- H1: "Vier nieuwbouwwoningen, *waar de rust begint.*"
- Twee alinea's, antwoordgericht (GEO): wie / wat / waar / door wie / wat is beschikbaar / prijs / oplevering. Zie prototype voor de exacte tekst.
- Kerncijfers als `<dl>` in een grid `repeat(auto-fit,minmax(150px,1fr))`, elk item met border-top: Beschikbaar, Vanaf, Oplevering, Bewoonbaar, Percelen, E-peil.
- **CTA's:** primair "Informatie aanvragen" (`#contact`) + outline "Bekijk de prijzen" (`#prijslijst`).

**2. Verhaal** (`id="verhaal"`, `data-media=1`, bg wit)
- H2 over de **volle kolombreedte**: "Waar het dorp ophoudt en de velden beginnen."
- Eén inleidende alinea (max-width 600px).
- Twee `<figure>`'s onder elkaar (gap 48px): afbeelding 3:2, radius 4px, met één korte zin als `<figcaption>`. **Geen uren/dagdelen, geen verspringende collage.**
- **CTA-blok brochure** (bg `#00532D`, radius 6px): eyebrow "Brochure" (goud), H3 "Alle plannen, materialen en prijzen in één document.", e-mailveld + gouden knop "Stuur mij de brochure". Na versturen: "✓ De brochure is onderweg naar je mailbox."

**3. Architectuur** (`id="architectuur"`, `data-media=7`, bg pagina)
- Eyebrow "Architectuur door TARCH", H2 "Strak en doordacht. Warm waar je het aanraakt.", 2 alinea's.
- Blockquote (border-left 1px, Playfair italic 21px) + bron.
- **Materialen:** grid `repeat(auto-fit,minmax(min(100%,210px),1fr))`, gap 32px × 20px. Op een breed scherm staan ze dus **naast elkaar**. Per item: foto 4:5, H3 21px, tekst 15px. Items: Lichte gevelsteen, Houten lamellen, Grote raampartijen.
- **Comfort en energie:** H3 + intro + `<dl>`-grid `minmax(220px,1fr)`: Warmtepomp, Vloerverwarming, Ventilatie D, Zonnepanelen, Isolatie, Regenwater.
- **CTA-rij** (wit kaartje met border): "Alle technische details" + tekst + outline-knop "⤓ Lastenboek".

**4. Woningen** (`id="woningen"`, `data-media=2`, bg pagina) — zie *Interactieve projectkaart* hieronder, gevolgd door **Prijslijst**, **Maandlast** en **Documenten**.

**5. Ligging** (`id="ligging"`, `data-media=3`, bg wit)
- H2 "Ligging", intro-alinea, buurtgids-grid (4 blokken: Dorpsleven, Natuur, Onderwijs, Bereikbaarheid; H3 20px + tekst 15px).
- Laag-chips (toggle; actief = bg `#00532D` wit, inactief = wit met border): Scholen, Winkels, Openbaar vervoer, Groen, Sport, Fietsbereik.
- Leaflet-kaart, hoogte 440px, OSM-tiles met CSS-filter `grayscale(.85) sepia(.12) brightness(1.04) contrast(.92)` op `.leaflet-tile-pane`. Projectpin: rond 52px groen met gouden rand + logo. POI-pins: rond 30px wit, groene rand, met een kort label (S, W, OV, G, Sp). "Fietsbereik" tekent 3 stippelcirkels (5/10/15 min fietsen ≈ 1,3 / 2,6 / 3,9 km). `scrollWheelZoom:false`.
- Reistijd-tegels (grid, bg `#F2F5EF`): Aalst, E40, Gent, Brussel.
- **CTA-rij** (bg `#F2F5EF`): "Wil je weten of Vlekkem bij je past?" + outline "Bel ons" (`tel:`) + primair "Stel je vraag" (scrollt naar het formulier met interesse "Algemene info").

**6. Bouwfase** (`id="bouwfase"`, `data-media=4`)
- H2 "Bouwfase" + groot percentage (Playfair 44px, groen), voortgangsbalk 6px (bg `#D6E5CC`, fill `#00532D`).
- Mijlpalenlijst: ✓ klaar / ● nu (bold, groen) / ○ gepland (grijs) + datum rechts.
- **CTA "Volg de werf"** (wit kaartje): e-mail + "Hou mij op de hoogte" → "✓ Je ontvangt de volgende werfupdate."

**7. Kopen** (`id="kopen"`, `data-media=5`, bg wit)
- H2 "Zo verloopt de aankoop" + 6 stappen (groen bolletje + **titel** — omschrijving): Kennismaking, Plannen & keuze, Reservatie, Compromis & akte, Afwerking kiezen, Sleutels.
- **CTA:** "De eerste stap is een vrijblijvend gesprek." + primair "Vraag een kennismaking" (scrollt naar het formulier, vult het bericht vooraf in).

**8. Vragen** (`id="vragen"`, `data-media=5`)
- Accordion, telkens één item open; vraag in Playfair 19px, teken +/− in groen; `aria-expanded`.
- 6 FAQ's (prijs, oplevering, btw/registratie, afwerking kiezen, energie, bereikbaarheid). Deze teksten worden ook als `FAQPage` JSON-LD uitgestuurd.
- **CTA:** "Staat je vraag er niet bij?" + outline "Stel je vraag".

**9. Contact** (`id="contact"`, `data-media=6`, bg `#00532D`)
- H2 dynamisch (zie *State*): "Lot 3 kan de jouwe zijn." / "Lot 1 of lot 3 — welke wordt de jouwe?" / "Welke woning wordt de jouwe?"
- Intro: "Je ontvangt de brochure, plannen en prijslijst binnen één werkdag. Je contactpersoon belt je enkel als je dat wil."
- Wit formulierkaartje (radius 6px, padding `clamp(20px,2.4vw,32px)`):
  1. Fieldset "Ik heb interesse in": toggle-chips per **beschikbaar** lot + "Algemene info" (multi-select, `aria-pressed`).
  2. Grid 2 kolommen (gsm: 1): **Voornaam**, **Naam**, **E-mail**, **Gsm** (`autocomplete` given-name / family-name / email / tel). Labels 13px/700 boven het veld; inputs min-height 50px, border `rgba(0,83,45,0.22)`, radius 4px.
  3. "Bericht (optioneel)": textarea, kan vooraf ingevuld worden vanuit de calculator of de kennismaking-CTA.
  4. Checkbox toestemming gegevensverwerking.
  5. Volle breedte primaire knop "Verstuur aanvraag" (min-height 54px).
  - Na verzenden: bedankmelding in plaats van het formulier.
- Onder het kaartje: "Liever meteen iemand spreken? Bel +32 (0)9 216 49 50".

### Interactieve projectkaart (in *Woningen*)
- H2 "De woningen" + korte intro.
- **Lot-tabs** boven de kaart (`role="tablist"`): pill per lot met een bolletje (goud = beschikbaar, grijs = verkocht); de actieve tab is groen gevuld.
- **Kaart:** container met aspect-ratio 4:3, min-height 340px, radius 6px, `overflow:hidden`. Daarin een **stage** met de luchtfoto + een SVG-overlay (`viewBox="0 0 100 100"`, `preserveAspectRatio="none"`) met één `<polygon>` per perceel (coördinaten in % van de foto: zie `ZONES` in het prototype; vervangen door de echte perceelcontouren op de echte luchtfoto).
  - Polygoonstijl: beschikbaar fill `#C9A96E` (opacity 0.32), verkocht fill wit (0.18); hover +opacity; geselecteerd: opacity 0.5, witte stroke 3px; de andere loten vervagen (0.08) en hun labels verdwijnen. `vector-effect: non-scaling-stroke`.
  - Labels (HTML boven de SVG): pill met lotnaam + "Beschikbaar/Verkocht" eronder; de labels worden tegengeschaald (`scale(1/k)`) zodat ze bij het inzoomen even groot blijven.
  - Hint (niets geselecteerd): pill bovenaan in het midden: "Klik op een perceel" (tablet/gsm: "Tik op een perceel").
- **Zoom bij selectie:** stage `transform: translate(tx%,ty%) scale(k)`, `transform-origin:0 0`, transition `0.7s cubic-bezier(.2,.7,.2,1)`. k = 2.1 (lot 4: 1.25 omdat dat perceel groot is). Het lot wordt horizontaal gecentreerd en verticaal op ~34% van de hoogte gezet (gsm: 50%), zodat het niet onder het detailkaartje verdwijnt. Klemmen zodat er geen lege rand zichtbaar wordt.
- **Detailkaartje** (tablet + desktop): absoluut onderaan in de kaart (left/right/bottom 12px, max-width 520px, wit, radius 8px, schaduw). Inhoud: type, lotnaam (Playfair 28px), status-pill, ✕-knop (44px); 4 kerncijfers (Woning, Perceel, Slpk, Tuin); prijs + "Grondplan ⤓" (enkel als beschikbaar) + primair "Prijs & documenten" (→ `#prijslijst`) of "Gelijkaardige woning?" bij verkocht.
- **Gsm (< 640px):** hetzelfde kaartje als **bottom sheet** (`position:fixed; bottom:0`, radius 16px boven, handle-streepje, `env(safe-area-inset-bottom)`).
- **Sluiten/uitzoomen:** opnieuw op het lot klikken, ✕, naast een perceel klikken (transparante achtergrond-rect), of Escape.

### Prijslijst (`id="prijslijst"`, binnen *Woningen*)
- Kop: H3 "Prijslijst" + "Prijzen excl. btw, registratie- en notariskosten. Bijgewerkt op …" + segmented filter **Beschikbaar / Alle woningen** (standaard: Alle).
- **Desktop/tablet: compacte tabel** (`role="table"` op div-grid, zodat het tijdens het laden geen lege `<table>` geeft; in Razor mag dit een echte `<table>` zijn). Kolommen: `minmax(0,1fr) 84px 80px 76px 150px 44px`.
  - Kopregel bg `#00532D`, min-height 52px, labels 10px/700 uppercase, letter-spacing 1.4px, `rgba(255,255,255,0.75)`: **Lot · Bewoonbaar · Grond · Slpk · Prijs · Plan**.
  - Rij min-height 64px, padding 12px 20px, border-top `rgba(0,83,45,0.08)`.
    - Lot: naam 15px/700 groen + daaronder het type (12px, `#7A9E6E`), bv. "Halfopen bebouwing".
    - Bewoonbaar / Grond: rechts uitgelijnd, tabular-nums.
    - Slpk: 4 bolletjes van 7px (gevuld `#3D7A4E` = aantal, leeg met border `#9cbf8e`) + getal.
    - Prijs (rechts):
      - **één afwerking** → enkel de prijs (Playfair 19px), **geen afwerkingslabel**;
      - **meerdere afwerkingen** → per regel een klein label ("Afgewerkt" / "Casco", 11px) + prijs (Playfair 16px);
      - daaronder een link "≈ € … / maand" (11px, groen, onderlijnd) → opent de calculator met dit lot;
      - **verkocht** → grijze pill "Verkocht"; de rij krijgt bg `#F5F7F2` en grijze cijfers.
    - Plan: icoonknop 36px (download-pijl, border, `aria-label="Grondplan Lot X downloaden (PDF)"`). Enkel bij beschikbare loten.
  - **Samenvattingsbalk** onder de tabel (`<dl>`, wit, border): Woningen (x beschikbaar), Bewoonbaar (range), Grond (range), Slaapkamers (range), Prijsrange (vanaf …), berekend uit de data.
- **Gsm: kaartjes** per lot: lotnaam + prijs rechts (bij één afwerking), regel met type/slpk/perceel; bij meerdere afwerkingen een mini-lijst; link "≈ € … / maand · bereken"; twee knoppen "⤓ Grondplan" en "⤓ Lastenboek". Verkocht: grijze kaart zonder knoppen.

### Maandlastcalculator (`id="maandlast"`, onder de prijslijst)
Altijd open, als een duidelijk eigen blok (border, radius 6px).
- **Bovenste deel (wit):** H3 "Wat betaal je per maand?" + subtekst. Keuze-chips (`role="radiogroup"`) per beschikbaar lot × afwerking ("Lot 3 · afgewerkt" / "Lot 3 · casco"; bij één afwerking enkel "Lot 2"), met de prijs eronder (`white-space:nowrap`). Sliders: **Eigen inbreng** (0–400.000, stap 5.000, standaard 120.000) en **Rentevoet** (1,5–5,5 %, stap 0,05, standaard 3,2 %). Looptijd-knoppen 15 / 20 / 25 / 30 jaar (standaard 25).
- **Resultaat (bg `#00532D`):** "Geschatte maandlast · Lot 3 · afgewerkt", groot bedrag; rechts "Lening € … / 25 jaar aan 3,20 %". Daaronder de gouden CTA **"Vraag een persoonlijke simulatie"** + "Vrijblijvend · antwoord binnen één werkdag".
- **Disclaimer** (bg `#F7F9F5`, 12px): "Indicatieve berekening, geen kredietaanbod. Excl. btw-, registratie- en notariskosten."
- Formule (annuïteit): `lening = max(0, prijs − inbreng)`, `r = rente/100/12`, `n = jaren×12`, `maand = lening·r / (1 − (1+r)^−n)`.
- Dezelfde parameters sturen ook de "≈ / maand"-bedragen in de prijslijst.
- CTA-actie: scroll naar `#contact` (offset −84px voor de sticky header), duid het lot aan bij "Ik heb interesse in" en vul het bericht in, bv. *"Ik wil graag een persoonlijke simulatie voor Lot 3 (volledig afgewerkt) aan € 567.000, met € 120.000 eigen inbreng over 25 jaar."*

### Documenten (onder de calculator)
Grid `minmax(220px,1fr)`, kaartjes met PDF-icoon, titel, meta (inhoud · grootte) en ⤓: Brochure, Lastenboek volledig afgewerkt, Lastenboek casco, Inplantingsplan.

### Mobiele CTA-balk (< 1000px)
`position:fixed; bottom:0`, wit, border-top + schaduw, safe-area padding. Links "Lot 3 · vanaf € 567.000" (of "3 woningen vrij · vanaf € …") + "Verkaveling Keerstraat"; rechts een belknop 48px (✆, outline) + primair "Info aanvragen". **Verborgen** wanneer de contactsectie actief is of de bottom sheet van de projectkaart open staat. Voorzie onderaan de pagina genoeg padding zodat de balk de footer niet bedekt.

---

## Interacties & gedrag (samenvatting)
- Ankerlinks scrollen soepel; programmatisch scrollen via `window.scrollTo({top: el.offsetTop − 84, behavior:'smooth'})` (geen `scrollIntoView`).
- Beeldcrossfade links: opacity 0.6s ease.
- Kaartzoom: transform 0.7s `cubic-bezier(.2,.7,.2,1)`; polygon fill-opacity 0.25s.
- Hover primaire knop `#006638`; outline-knop vult groen; rijen in de prijslijst hover `#FAFBF8`.
- Escape sluit het geselecteerde lot.
- Formulier: verplicht = voornaam, naam, e-mail, gsm, toestemming. E-mail valideren; gsm in Belgisch formaat (+32 / 04…). Bij een fout: foutmelding onder het veld (rood niet voorzien in het ontwerp → gebruik een bestaande foutstijl of `#B3261E`). Bij succes: bedankmelding.
- Brochure- en werfupdate-formulieren: één e-mailveld, inline bevestiging.

## State (per pagina, client-side)
| State | Type | Doel |
|---|---|---|
| `media` | int | actieve sectie → beeld links |
| `sel` | lotId \| 0 | geselecteerd perceel op de projectkaart |
| `hover` | lotId \| 0 | hover op perceel |
| `listF` | `'vrij' \| 'alle'` | filter prijslijst |
| `calcOpt` | `'lotId\|afwIndex'` | gekozen woning/afwerking in de calculator |
| `inbreng`, `rente`, `jaren` | number | calculatorparameters |
| `layers` | string[] | actieve lagen op de buurtkaart |
| `faq` | int | open FAQ-item |
| `interest` | string[] | gekozen interesse in het formulier |
| `msg` | string | berichtveld (vooraf invulbaar) |
| `sent`, `bro`, `upd` | bool | verzonden-states van de formulieren |
| `narrow` (< 1000px), `phone` (< 640px) | bool | responsive gedrag |

**Contacttitel-logica:** gekozen beschikbare loten in `interest` → 1: "{Lot} kan de jouwe zijn." · >1: "{Lot a}, {lot b} of {lot c} — welke wordt de jouwe?" · 0: als er maar één lot beschikbaar is "{Lot} kan de jouwe zijn.", anders "Welke woning wordt de jouwe?"

## Datamodel (server, Razor-viewmodel)
```
Project { Naam, Slug, Gemeente, Deelgemeente, Provincie, Adres, Postcode,
          Lat, Lng, Architect, Oplevering, EPeil, BouwvoortgangPct,
          Intro (2 alinea's), Verhaal { Titel, Intro, Figuren[{Img, Alt, Tekst}] },
          Materialen[{Titel, Tekst, Img}], Technieken[{Titel, Tekst}],
          Buurtgids[{Titel, Tekst}], Pois[{Cat, Naam, Lat, Lng, Afstand, Tijd}],
          Reistijden[{Naar, Minuten}], Tijdlijn[{Datum, Titel, Status}],
          Faq[{Vraag, Antwoord}], Documenten[{Titel, Meta, Url}],
          LuchtfotoUrl, Loten[] }
Lot     { Id, Naam, Type, Bewoonbaar, Grond, Slaapkamers, Badkamers, Tuin,
          EPeil, Status (Beschikbaar|Optie|Verkocht), RenderUrl, GrondplanUrl,
          PerceelPolygon ("x,y x,y ..." in % van de luchtfoto), LabelX, LabelY,
          Afwerkingen[{ Type (VolledigAfgewerkt|Casco), Prijs }] }
```
Kolom "Prijs" en de chips in de calculator hangen af van het aantal `Afwerkingen` per lot.

## SEO / GEO
- `<title>`: "{Project} — {n} nieuwbouwwoningen in {Deelgemeente} ({Gemeente}) | Group LN"; `<meta description>` met beschikbaarheid + prijs; `<link rel="canonical">`; Open Graph.
- **Één H1**; H2 per sectie; materialen/technieken/buurt als H3 + echte alinea's (geen tekst in afbeeldingen).
- Intro-alinea's zijn antwoordgericht geschreven (wie/wat/waar/prijs/oplevering) zodat AI-zoekmachines ze kunnen citeren. Toon "Bijgewerkt op {datum}" bij de prijslijst.
- **JSON-LD server-side renderen** (niet via JS): `BreadcrumbList`, `Residence`/`ApartmentComplex` met `address` + `geo`, één `Offer` per beschikbaar lot (price, priceCurrency EUR, availability InStock, `itemOffered` SingleFamilyResidence met `floorSize`, `numberOfRooms`), `FAQPage` met dezelfde FAQ-teksten, `Organization` (Group LN).
- Afbeeldingen: beschrijvende `alt`, `loading="lazy"` behalve het hero-beeld, WebP.
- Prijslijst en documenten zijn gewone HTML/links (crawlbaar).

---

## Scherm 2 — Woonprojecten-overzicht (`Woonprojecten v2.dc.html`)
- **Hero (split):** links eyebrow "Woonprojecten · Oost- & West-Vlaanderen", H1 "Nieuwbouw in Vlaanderen, *op mensenmaat.*" (Playfair `clamp(44px,5.6vw,92px)`), alinea over de visie, onderaan een link-rij "Nu in de kijker — {project}" met ronde pijlknop. Rechts een sfeerbeeld over de volle hoogte (`min-height: min(820px, 100vh − 84px)`). **Geen breadcrumbs, geen filterpills.**
- **Nu te koop:** H2 + uitgelicht project (grote kaart: beeld over 2 kolommen + tekstkolom met `<dl>` beschikbaar/oplevering/oppervlakte/vanaf, verkocht-%-badge) + grid van overige projecten te koop (`minmax(420px,1fr)`).
- **Binnenkort** (bg `#00532D`): beeld + tekst + e-mailinschrijving "Houd mij op de hoogte".
- **Waar men al thuis is:** gerealiseerde projecten, beelden 4:5 met een jaarlabel.
- **Wat je in elk project terugvindt:** beeld + 3 principes (zonder nummering).
- **Nieuwbouw per gemeente:** linkgrid (lokale SEO-landingspagina's).
- **FAQ** (accordion + `FAQPage` JSON-LD) en **CTA-duo** (Grond aanbieden / Stuur een bericht).
- Max-width containers 1680px, padding `clamp(20px,4vw,72px)`. **Geen hoofdstuknummers of "Hoofdstuk 01"-labels.** Dat werd expliciet afgewezen.

## Assets
- Logo: `Content/img/logo.png` (bestaat in de repo).
- Fonts: `Content/fonts/Avenir-*.woff2` (repo) + Playfair Display (Google Fonts).
- Placeholderbeelden in het prototype (`about.webp`, `grondverwerving/bouwgrond.webp`) komen uit de repo; alle andere beelden zijn lege slots → vervangen door echte renders/foto's per project: hoofdbeeld, sfeerbeelden, materiaaldetails, **luchtfoto van het terrein**, render per lot, werffoto's, teamportret.
- Kaart: Leaflet 1.9.4 + OpenStreetMap-tiles (controleer de licentie/attributie; overweeg een tile-provider met API-sleutel voor productie).

## Screenshots (`screenshots/`)
Desktop (rechterkolom per sectie, ~1280px breed):
- `desktop-00-beeld-links.png`: sticky beeld links met de projectkaart-overlay
- `desktop-01-intro.png` … `desktop-09-contact.png`: één bestand per sectie
- `desktop-04-woningen-prijslijst-maandlast.png`: projectkaart (niets geselecteerd), prijslijst, calculator, documenten
- `desktop-04b-projectkaart-lot-geselecteerd.png`: ingezoomd op lot 3 met het detailkaartje
- `desktop-10-footer.png`, `index-desktop-hero.png`

Gsm (390px):
- `gsm-01-hero` … `gsm-08-formulier`: hero, intro, verhaal, projectkaart, ingezoomd lot, prijslijst als kaartjes, maandlast, formulier.

Opmerking: lege beeldvlakken zijn plaatshouders. De kaarttegels van OpenStreetMap staan niet op de screenshots (beperking van de capture); in de browser zijn ze wel zichtbaar. Op de gsm-screenshots is de vaste CTA-balk onderaan afgesneden aan de rechterkant.

## Bestanden in deze map
- `Project Detail v4 Split.dc.html` — hoofdontwerp projectdetail (gedrag + alle teksten)
- `Woonprojecten v2.dc.html` — ontwerp overzichtspagina
- `keerstraat.js` — voorbeelddata + Leaflet-helper (POI's, lagen, fietscirkels, JSON-LD-voorbeeld)
- `image-slot.js`, `support.js` — enkel nodig om de prototypes lokaal te openen
- Ouder verkennend werk (enkel ter referentie, niet bouwen): `Project Detail v2`, `v3a Seizoenen`, `v3b Split`, `v3c Statement`

**Simulatie:** in het prototype kan de tweak `beschikbaar` (1/2/3) het aantal vrije loten wijzigen, zodat je ziet hoe de prijslijst, de calculator, de contacttitel en de mobiele balk zich aanpassen.
