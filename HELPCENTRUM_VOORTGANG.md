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

## 12/10/2026 — bestaande artikels herschreven volgens HELP_SCHRIJFGIDS.md
- Alle 9 artikels herschreven: taaktitels, je-vorm, korte zinnen, geen gedachtestreepjes, knoppen in
  **vet**, genummerde stappen waar de volgorde telt. Slugs/`screens`/`related`/afbeeldingen ongewijzigd.
- Waar een gevolg of regel niet uit de code blijkt, staat `[VRAAG: ...]` in de tekst (ook in 2
  FAQ-antwoorden). Alle open vragen moeten door Niels beantwoord worden; daarna verhuist het antwoord
  naar de woordenlijst/bedrijfslogica in `HELP_SCHRIJFGIDS.md` en verdwijnt de markering.
- Vaste opbouw (taakartikel) is nog niet overal toegepast: `projecten-algemeen`, `project-bewerken` en
  `project-detail-overzicht` zijn schermuitleg en zouden per taak gesplitst kunnen worden.
- Titelwijzigingen: "Projecten — het overzicht" → "Een project zoeken en openen", "Het projectdossier
  lezen" → "De stand van een project bekijken", "Je eerste budget in 9 stappen" → "Een budget opmaken
  met de budgetwizard", enz. Artikels verwijzen elkaar in de tekst bij titel, controleer die verwijzingen.

## 12/10/2026 (vervolg) — antwoorden van Niels verwerkt, projectartikels per taak gesplitst
- Verwerkt in gids + artikels: Gepubliceerd = zichtbaar op de website; Opgeleverd = volledig project
  voorlopig opgeleverd; weerstation berekent weerverletdagen; project verwijderen enkel door een
  beheerder (geen archivering); concept-punt moet door de projectleider goedgekeurd worden, maker krijgt geen
  melding; projectcode = dossiernummer-maand-jaar startdatum (handmatig); herziening definitief budget enkel
  na bedrijfsbeslissingen die het budget beïnvloeden.
- Bevestigd door Niels: de dashboardmelding "punten goed te keuren" voor de projectleider bestaat nog niet (te bouwen). Zodra ze er is: artikel `punt-met-spraakbericht` aanpassen.
- Nieuwe structuur Projecten (boom): Overzicht(+Aanmaken) / De stand bekijken (+Vraagt actie, Traject & eenheden,
  Team/budget/facturen) / Project bewerken (+Bedrijven & werfmelding, Status & planning, Publiceren,
  Partners & documenten, Coördinatie). Bestaande screenshots verhuisd naar de map van hun nieuwe artikel.
- Screenshots gemaakt voor Status & foto, Planning, SEO & website en Coördinatie (Playwright met eigen profiel,
  ingelogd door Niels, enkel gelezen, niets opgeslagen; uitgesneden tot de kaart, geen persoonsgegevens op beeld).
  Veldnamen in de artikels gecorrigeerd naar het echte scherm (Aanvangsdatum, Uitvoeringstermijn, Totale
  grondaandelen, Heeft coördinatie, SEO-titel, Meta-omschrijving).
- Mogelijke UI-bug: op het tabblad Coördinatie staat de schakelaar "Heeft coördinatie" visueel nog uit nadat hij
  aangezet is (checkbox wel true, kaarten verschijnen). Nog te onderzoeken.
- De scripts staan in de scratchpad (niet in de repo). Om opnieuw screenshots te maken: nieuw inlogvenster nodig.

## 12/10/2026 (vervolg 2) — artikel grondaandelen + bugfix Aandelen basisakte
- Nieuw artikel `projecten/grondaandelen-verdelen.md` (basisakte, Aandeel basisakte per eenheid, meldingen op de
  eenhedenlijst, scherm Aandelen basisakte) met 2 anonieme screenshots van project 60. Opslaan wordt niet
  geblokkeerd bij een foute som (Niels bevestigd).
- Bug gevonden tijdens het screenshotten en opgelost: het scherm Aandelen basisakte toonde "1.050" (nl-BE) maar las
  het terug als 1,05 (JS `parseAmount` + `ParseAmountV2`), zodat de som 539,47 i.p.v. 10.000 toonde en opslaan de
  aandelen kapot zou maken. Fix: punt + exact drie cijfers = duizendtalscheiding, enkel voor dit scherm
  (`gl-v2-projecten-landshares.js` + nieuwe `ParseLandshareV2` in `ProjectenController.Units.cs`).
  JS-kant gecontroleerd in de browser (som 10.000, 10,5 %), serverkant bewust niet getest (zou echte data
  overschrijven). Herstart nodig voor de controller.
- Knop "Aandelen verdelen" toegevoegd bovenaan de eenhedenlijst (DetailUnitsV2, enkel bij basisakte en schrijfrecht;
  tablet via het ⋯-menu, gsm via snelle actie "Aandelen"). Herstart nodig, nog niet in de browser bekeken.

## 12/10/2026 (vervolg 3) — helpboom openklapbaar
- Boom links op de artikelpagina is nu volledig openklapbaar (chevrons, willekeurig diep), bij laden enkel het pad naar
  het huidige artikel open. Kolom 340px vanaf 1600px breed. Tooltip met de volledige titel als die is afgeknot.
  Bestanden: `HelpController.BuildTreeNodes`, `HelpVm.cs`, `Views/Help/_HelpTreeNode.cshtml`, `ArtikelV2.cshtml`,
  `gl-v2-help.css`, `gl-v2-help.js`. Gebouwd (0 fouten), nog NIET in de browser bekeken: herstart nodig (views + controller).

## 12/10/2026 (vervolg 4) — "Op deze pagina": meescrollen, actief punt, klikbaar
- Boom: enkel hoofditems vet, 24px inspringing per niveau (ook eerste niveau t.o.v. het onderwerp), boomkolom 500px en
  toc 360px vanaf 1800px (340/240 vanaf 1600px).
- Toc meescrollen: oorzaak gevonden, `.gl-v2-body` heeft `overflow: hidden` in de shell, wat `position: sticky` stil
  uitschakelt. Fix enkel op deze pagina: `.gl-v2-body:has(.gl-v2-help-article-layout) { overflow: clip; }`. Live in de
  browser getest (toc blijft op 20px na 700px scrollen).
- Toc klikbaar + actief punt (41c: vet, groen, groene lijn): `HelpTocItemVm.TargetId`, ids `toc-before`, `toc-steps`,
  `toc-step-N`, `toc-faq` op de blokken, scroll-spy in `gl-v2-help.js` (lijn op 120px, onderaan = laatste punt actief,
  klik scrolt smooth en zet `#` in de url). Gebouwd (0 fouten), nog NIET getest: herstart nodig (view + controller).
