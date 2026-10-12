# Helpcentrum (design-handoff punt 41 + 43) — voortgang en overdracht

Laatst bijgewerkt: 11/10/2026 (avond). Bron van het ontwerp: `design-handoff/CRM Help.dc.html`
(punt 41a–f) + `design-handoff/CRM Paginatypes.dc.html` (punt 43, vooral 43a "Shell" en 43i
"Kennisbank / artikel"). Volledige technische uitleg staat in `DESIGN.md`, sectie "Helpcentrum —
design-handoff punt 41" (met drie "Follow-up"-subsecties) — dit bestand is het overdracht-/
statusoverzicht, DESIGN.md blijft de bron van waarheid per detail.

## Beslissingen van Niels
- Content = **markdown-bestanden in de repo**, geen DB/CMS. `CPMCore/HelpContent/<topic>/<slug>.md`.
- "Vraag stellen aan support" en artikel-feedback ("Was dit nuttig?") = **wél in de database**
  (`HelpFeedback`-tabel, nog uit te voeren SQL — zie onderaan).
- Boomstructuur per topic is **hiërarchisch** (ouder/kind, bv. "1 Algemeen → 1.1 Toevoegen"), niet
  plat-alfabetisch — zie "Nieuw: parent/child-hiërarchie" hieronder.
- Topbar van het Helpcentrum-hoofdscherm (`/Help`) toont **enkel titel "Help" + zoekvak +
  gebruiker**, geen kruimelpad en geen andere knoppen.
- **Staande regel (vastgelegd bovenaan `DESIGN.md`)**: elke nieuwe functie/pagina krijgt ook een
  artikel in het Helpcentrum, op de juiste plek.

## Architectuur in het kort
- `IHelpContentService`/`HelpContentService` — leest alle `.md`'s, cachet in geheugen (2s-
  herlaadvenster in Development, geen herstart nodig voor content-wijzigingen).
- `HelpMarkdownParser` — eigen kleine frontmatter-parser (geen YAML-library), zie DESIGN.md voor
  het volledige veldenoverzicht en het "stappenplan" om een nieuw artikel te schrijven.
- `HelpController` (`/Help`, `/Help/Artikel/{slug}`, `/Help/Search`, `/Help/Feedback`,
  `/Help/Support`) — forceert `UseGlV2Layout=true` (geen legacy-tegenhanger).
- `HelpPanelViewComponent` + `GlV2/_HelpPanel.cshtml` — het "?"-zijpaneel (41d), op élke gl-v2-
  pagina gerenderd, gevoed door `ViewData["HelpScreenId"]` per controller-actie.
- CSS: `gl-v2-help.css` (start/artikel) + `gl-v2/help-panel.css` (zijpaneel + topbar-"?"-knop).
  JS: `gl-v2-help.js` (zoeken, FAQ-accordion, feedback) + `gl-v2-help-panel.js` (paneel-toggle).

## Artikels die vandaag echt geschreven zijn (met echte, geanonimiseerde screenshots)
| Slug | Topic | Ouder | Screens |
|---|---|---|---|
| `account-en-rol` | aan-de-slag | — | — |
| `projecten-algemeen` | projecten | — (treeOrder 1, treeLabel "Overzicht") | `projecten-algemeen` |
| `project-aanmaken` | projecten | `projecten-algemeen` | `projecten-toevoegen`, `projecten-algemeen` |
| `project-detail-overzicht` | projecten | — (treeOrder 2) | `projecten-detail` |
| `project-bewerken` | projecten | `project-detail-overzicht` | `projecten-bewerken` |
| `eerste-budget` | budget | — | — |
| `wijzigingsopdracht-opmaken` | facturatie-wijzigingen | — | `invoices-index` |
| `punt-met-spraakbericht` | punten-verslagen | — | `issues-index` |
| `punten-lijst-overzicht` | punten-verslagen | — | `issues-index` |

Screenshots: Playwright met een eigen automatiseringsprofiel (niet het echte Edge-profiel van
Niels — Edge weigert remote-debugging op het letterlijke default-profielpad), DOM herschreven vóór
elke screenshot om echte klant-/leverancier-/notarisnamen en -adressen te vervangen door fictieve
(consistente mapping, ook over meerdere projecten/screenshots heen). Screenshots zijn uitgesneden
tot één kaart/sectie per stap (`sharp`, dev-only), nooit een volle paginascreenshot naast een
alinea tekst.

## Bugs gevonden en gefixt tijdens het bouwen (geen van deze bestond al — allemaal nieuw ontdekt)
- `HelpController` rendere zonder de preview-cookie onder de oude `_Layout` (geen gl-v2-CSS/rail).
- Dode `else { ... }`-blokken (restant van de JS-only-actiebalk-refactor, geen `@@if` erboven, dus
  pure HTML-tekst) in **zowel** `ToevoegenV2.cshtml` als `EditV2.cshtml` se actiebalk — toonden
  letterlijk "else { ... }" op het scherm.
- FAQ als `## Veelgestelde vragen`-body-heading i.p.v. frontmatter `faq:` — rendert dan als platte
  tekst, geen accordion. Gebeurde **vijf keer** (ook een keer opnieuw nadat het al een keer
  gefixt was elders) — zie DESIGN.md Follow-up voor de volledige lijst.
- Markdown-tabellen renderden als platte "| Cijfer | Betekent |"-tekst (Markdig miste
  `UseAdvancedExtensions()`).
- Anonimisatiescript verving enkel tekstnodes, niet `<input>`-waarden — een formulierveld
  (Projecten/Edit, tab Algemeen) toonde nog de echte projectnaam/straat.

## Nieuw: parent/child-hiërarchie + boom-navigatie (vandaag toegevoegd)
- `HelpArticle.ParentSlug`/`TreeOrder`/`TreeLabel` (frontmatter `parent:`/`treeOrder:`/
  `treeLabel:`) — boom toont hoofdartikel gevolgd door zijn kinderen, ingesprongen, in plaats van
  één platte alfabetische lijst.
- **Open / wacht op herstart:** een ingeklapt topic-item in de boom (elk topic behalve het
  huidige) was niet klikbaar — net gefixt (`HelpTreeGroupVm.FirstArticleSlug`, wijst naar het
  eerste artikel van dat topic). **Controller-wijziging, nog niet geverifieerd na herstart.**
- Verticale centrering van een boom-item (actief/hover) stond niet in het midden van zijn vak —
  gefixt door de fragiele `line-height`-truc te vervangen door echte flexbox-centrering (CSS,
  al live, geen herstart nodig).

## Grote layout-herwerking vandaag (design-handoff punt 43, "Paginatypes")
Niels wees erop dat de layout niet meer overeenkwam met het (ondertussen bijgewerkte) punt 41 en
het nieuwe punt 43 (`CRM Paginatypes.dc.html`, 11 vaste paginatypes — Help-artikel = type 8
"Kennisbank / artikel", 43i). Aangepast:
- Boomkolom/body/toc zijn nu **drie losse witte sectiekaarten** met een echte gap (16px) — niet
  langer één gefuseerde kaart met interne randen (dat was de tussenversie vóór punt 43 bestond).
- Boomkolom 280px breed (was 240), toc is nu `position: sticky`.
- Body-padding 28/40/40, body-tekst 14px (was 13.5px).
- Topbar hoofdscherm: kruimelpad weg (`ViewData["HideBreadcrumb"]`, nieuw, project-wijd
  mechanisme), compact zoekvak toegevoegd (hergebruikt via `wireHelpSearch()` in `gl-v2-help.js`,
  zelfde zoekvak ook op de artikelpagina se topbar).
- "Vraag stellen aan support" verwijderd uit de topbar (Niels se instructie: enkel titel+zoek+
  gebruiker) — blijft bereikbaar via het "?"-zijpaneel en via "Niet gevonden?" in de
  zoekresultaten (nu pas echt gewired, stond er eerst enkel als dode link).

## Nog open / volgende stappen
- **Nog te verifiëren na de volgende herstart:** boom-volgorde (Overzicht vóór Detail),
  "Overzicht"-label, klikbare ingeklapte topic-items.
- **Nog te schrijven:** Klanten- en Leveranciers-artikels (zelfde structuur als Projecten:
  Algemeen/Toevoegen/Detail/Bewerken), en de overige ~55 artikels voor de andere topics.
- **`HelpFeedback`-tabel nog niet aangemaakt op de live DB** — `DALCore/Migrations/
  HelpFeedback.sql` moet nog handmatig uitgevoerd worden (dit project gebruikt geen EF-migraties).
- Geen "Open dit scherm in de app ↗"-knop gebouwd (punt 41c/43i) — er bestaat geen registry die
  een screen-id naar een echte URL vertaalt.
- Geen scroll-spy op "Op deze pagina" (toont de lijst, markeert geen actief item tijdens scrollen).

## Werkafspraken die ik tijdens deze sessie volgde
- Elke C#/.cshtml-wijziging heeft een volledige herstart nodig (Razor-runtime-compilation staat
  niet aan) — markdown-content en CSS/JS zijn wél meteen live.
- Build: `dotnet build CPMCore/CPMCore.csproj -c Debug -o <los pad>` (niet het eigen bin/Debug —
  Visual Studio houdt die DLL's vast zolang de solution open staat); enkel `error CS`/`RZ` telt,
  MSB3021/3027-bestandsvergrendelingsfouten op het eigen `bin/Debug`-pad genegeerd.
- Playwright draait met een eigen automatiseringsprofiel, nooit het echte profiel van Niels — Edge
  weigert remote-debugging op het default-profielpad (anti-hijack-bescherming, niet te omzeilen).
- Screenshots altijd uitsnijden tot de relevante kaart/sectie, nooit een volle paginascreenshot.
