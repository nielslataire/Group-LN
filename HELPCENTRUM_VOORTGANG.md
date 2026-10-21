# Helpcentrum (design-handoff punt 41 + 43) — voortgang en overdracht

Laatst bijgewerkt: 13/10/2026. Bron van het ontwerp: `design-handoff/CRM Help.dc.html`
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
- **Nog te schrijven:** de overige ~55 artikels voor de andere topics. Klanten/Leveranciers
  (inclusief Contacten, Klant toevoegen, Contract toevoegen/bewerken) zijn nu gedekt — zie
  12/10/2026 (vervolg 5) en (vervolg 6).
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

## 12/10/2026 (vervolg 5) — Klanten- en Leveranciers-artikels (project-detail)
- 4 nieuwe artikels in `projecten/`, geschreven uit de view/controller-code (geen screenshots —
  geen Playwright-sessie deze keer), per `HELP_SCHRIJFGIDS.md`:
  - `project-klanten-overzicht.md` (treeOrder 5, `screens: projecten-klanten`) — `Projecten/DetailClients`.
  - `project-klant-detail.md` (parent `project-klanten-overzicht`, `screens: projecten-klant-detail`) —
    `Klanten/Detail` (de klantfiche binnen een project).
  - `project-leveranciers-overzicht.md` (treeOrder 6, `screens: projecten-leveranciers`) — `Projecten/DetailContracts`.
  - `project-leverancier-detail.md` (parent `project-leveranciers-overzicht`, `screens: projecten-leverancier-detail`) —
    `Projecten/DetailContract` + `DetailSupplier` (zelfde view, met of zonder contractid).
  `ViewData["HelpScreenId"]` gezet in `ProjectenController.Clients.cs` (DetailClients),
  `ProjectenController.Contracts.cs` (DetailContracts, DetailContract, DetailSupplier) en
  `KlantenController.Core.cs` (Detail) — vlak vóór elke `return View(...)`. Herstart nodig voor het
  "?"-zijpaneel op die 4 schermen (C#-wijziging); de `.md`'s zelf zijn al live.
- Niet gedekt: `Projecten/DetailContacts` (contactpersonen-tak), `Klanten/AddClientAccount` (Klant
  toevoegen), `Projecten/AddContract`/`EditContract` (Leverancier/contract toevoegen/bewerken) — elk
  een apart artikel, nog te schrijven.
- Meerdere `[VRAAG: ...]` toegevoegd (o.a. status verkocht/optie/akte, wat er gebeurt bij een klant
  verwijderen, financiële waarborg vs. bankwaarborg, of "Herinnering sturen" ergens gelogd wordt) —
  nog door Niels te beantwoorden, dan verhuizen naar `HELP_SCHRIJFGIDS.md`.

## 12/10/2026 (vervolg 6) — Contacten, Klant toevoegen, Contract toevoegen/bewerken
- 3 nieuwe artikels in `projecten/`, allemaal kind van hun overzichtsartikel:
  - `project-contactpersonen.md` (kind van `project-klanten-overzicht`, geen `screens`) —
    `Projecten/DetailContacts`/`ContactDetails`/`AddContact`/`EditContact`. Deze vier acties zijn
    **legacy-only** (geen `UseGlV2Layout`-tak, geen V2-view), dus geen `ViewData["HelpScreenId"]`
    gezet: het "?"-zijpaneel bestaat enkel in `_LayoutV2`, op deze pagina's zou het niets doen.
  - `project-klant-toevoegen.md` (kind van `project-klanten-overzicht`, `screens:
    projecten-klant-toevoegen`) — `Klanten/AddClientAccount` (heeft wel een V2).
  - `project-leverancier-toevoegen.md` (kind van `project-leveranciers-overzicht`, `screens:
    projecten-leverancier-toevoegen`) — dekt zowel `Projecten/AddContract` als `EditContract`
    (zelfde formulier, zelfde `screens`-id op beide acties).
  `ViewData["HelpScreenId"]` gezet in `KlantenController.ClientAccount.cs` (AddClientAccount) en
  `ProjectenController.Contracts.cs` (AddContract, EditContract) — vlak vóór hun `return View(...)`.
  Build (`dotnet build CPMCore/CPMCore.csproj -c Debug -o <los pad>`) geeft 0 errors. Herstart nog
  nodig om het zijpaneel op die 2 schermen te zien.
- Nieuwe `[VRAAG: ...]`: of een contactaanvraag automatisch naar de klantenlijst verhuist bij
  aankoop, en of een klantenaccount zonder eenheid kan bestaan.

## 12/10/2026 (vervolg 7) — antwoorden van Niels verwerkt (Klanten/Leveranciers)
- Verwerkt in `HELP_SCHRIJFGIDS.md` (woordenlijst Financieel + nieuwe sectie "Klanten en
  leveranciers") en in de 4 betrokken artikels, alle `[VRAAG: ...]` daar verdwenen:
  - Status eenheid: In optie (tijdelijk, één klant) → Verkocht (na ondertekening compromis) →
    Akte verleden (na het verlijden van de akte).
  - Klant verwijderen is niet omkeerbaar en geblokkeerd zodra de klant al facturatie of
    wijzigingsopdrachten heeft; de gekoppelde eenheid wordt dan terug "Nog geen klant".
  - Contactaanvragen (`DetailContacts`) en de klantenlijst zijn vandaag volledig losse lijsten, geen
    automatische koppeling bij aankoop.
  - Een klantenaccount wordt altijd met minstens één eenheid aangemaakt.
  - Financiële waarborg = percentage ingehouden op elke factuur; bankwaarborg = percentage
    gewaarborgd door een bank met attest, niets ingehouden.
  - "Herinnering sturen"/"Opvragen" zijn pure `mailto:`-snelkoppelingen, zelf nagekeken in de code
    (`DetailContractV2.cshtml`, `ReminderMailto`) — geen enkele controller-actie logt dit; bevestigd
    aan Niels in plaats van aan hem gevraagd.
  - Eén constatering i.p.v. een vraag: Niels verwacht dat je op de klantfiche (`Klanten/Detail`) alle
    eenheden van een klant samen ziet, maar de gl-v2-kaart (`DetailV2.cshtml`) toont vandaag enkel de
    eerste eenheid + een teller "+ N andere", geen klikbare volledige lijst — vastgelegd in de gids
    én in `project-klant-detail.md` als bestaande beperking, geen uitnodiging om het zelf te bouwen.

## 13/10/2026 — Contacten hoort bij Verkoop, niet bij Klanten
- Niels corrigeerde: `project-contactpersonen.md` stond als kind van `project-klanten-overzicht`,
  maar `Projecten/DetailContacts` zit in het linkermenu onder de groep **Verkoop** (samen met **Te
  koop**/`DetailSales`), niet onder **Klanten** — geverifieerd in
  `Views/Shared/GlV2/_ProjectInnerMenuV2.cshtml` regel 121-124 (`verkoopItems`/groep `"verkoop"`).
  De intro van het artikel zei zelfs expliciet (en fout) "via Klanten → Contacten".
- Fix: `parent: project-klanten-overzicht` weg, nu een eigen top-level item (`treeOrder: 7`) in de
  `projecten`-boom i.p.v. een kind van Klanten. `role` Projectleider → Verkoper (zelfde patroon als
  `role: Werfleider` elders). Intro herschreven: "via Verkoop → Contacten", met een expliciete zin
  dat dit bij Verkoop hoort, niet bij Klanten. `project-klanten-overzicht.md` se eigen `related:`
  verwijst niet langer naar dit artikel. Het artikel zelf houdt nog wel een `related:` terug naar
  `project-klanten-overzicht` (het blijft relevant via de FAQ "verschil met de klantenlijst"), dat is
  een gewone kruisverwijzing, geen hiërarchie.
- Geen `screens`/`HelpScreenId` hier (zie vervolg 6: `DetailContacts` is legacy-only), dus geen
  controllerwijziging nodig voor deze fix — enkel de `.md` is aangepast, meteen live.
- "Te koop" (`Projecten/DetailSales`), de andere ingang in dezelfde Verkoop-groep, heeft nog geen
  artikel — nog te schrijven.

## 13/10/2026 — echte, geanonimiseerde screenshots voor alle 7 Klanten/Leveranciers-artikels
- Playwright-opzet in de scratchpad (niet in de repo, zie `DESIGN.md` se nieuwe sectie "Schermafbeeldingen
  maken — herbruikbaar Playwright-recept" voor het volledige, herbruikbare stappenplan): eigen
  automatiseringsprofiel, Niels logde zelf in (Entra ID), daarna de `gl_v2_preview`-cookie gezet via
  `/LayoutPreview/On` en project 68 gevonden (heeft klanten, contracten én contactaanvragen).
- 11 crops gemaakt en met het Read-tool zelf bekeken vóór verplaatsen (geen enkele als volle
  paginascreenshot): `project-klanten-overzicht/1-lijst`, `project-klant-detail/{1-kerncijfers,
  2-gegevens}`, `project-leveranciers-overzicht/1-lijst`, `project-leverancier-detail/{1-kerncijfers,
  2-voorwaarden}`, `project-contactpersonen/{1-kerncijfers,2-lijst}`,
  `project-klant-toevoegen/1-klantenaccount`, `project-leverancier-toevoegen/1-algemeen`. Alle 7
  `.md`'s kregen hun `![...]`-regel(s) en `updated: 2026-10-13`.
- **Privacy-incident, zelf ontdekt vóór publicatie, niet door Niels:** de eerste
  `contacten-2-lijst.png` (legacy `Projecten/DetailContacts`-tabel) stond met **echte namen,
  e-mailadressen en telefoonnummers** (ook dat van Niels als testcontact) nog in de scratch-outputmap
  — de anonimisatie draaide enkel op gl-v2-CSS-klassen, die deze legacy-pagina niet heeft. Bestand
  meteen verwijderd, nooit in de repo of gepubliceerd geweest. Fix: kolompositie-gebaseerde
  anonimisatie (`tr td:nth-child(n)`) toegevoegd voor deze ene legacy-tabel, opnieuw gescreenshot,
  pas daarna manueel geverifieerd en gebruikt. Vastgelegd in `DESIGN.md` als expliciete waarschuwing
  (punt 6 van het Playwright-recept) zodat dit niet herhaald wordt op de volgende legacy-pagina.
- Twee kleinere bugs onderweg gefixt: (1) de login-detectie gebruikte eerst een in-page
  `waitForFunction` die de Microsoft-loginpagina zelf al als "ingelogd" herkende en de browser te
  vroeg sloot — vervangen door `page.url()`-polling vanuit Node; (2) de fake-naamgenerator gaf soms
  "Tom undefined" (signed `>>` i.p.v. unsigned `>>> ` bij de hash-shift, negatieve array-index).
- Niet gedekt: "Te koop" (`DetailSales`), `Klanten/AddClientAccount`/`Projecten/AddContract` tonen
  enkel hun eerste kaart (leeg formulier, geen bestaande data nodig) — de rest van die lange
  formulieren (Eigenaars, Facturatie, Eenheden, Voorwaarden, Loten) heeft nog geen screenshot.

## 13/10/2026 — volledige help voor de budgetflow geschreven
- 14 artikels in topic `budget` (tree): Overzicht (+ Nieuw budget/versie, Afronden & definitief maken), Een budget opmaken met de
  budgetwizard (+ Stap 1 tot 9 in volgorde via `treeOrder`, + Waarschuwingen en fouten). Alle volgens `HELP_SCHRIJFGIDS.md`.
- Geschreven vanuit project 71 (concept-budget) in de draaiende app; screenshots (anoniem: straat en gemeente vervangen) in
  `wwwroot/img/help/budget/<slug>/`. Bron voor de meldingen: DESIGN.md "Budgetflow punt 39k".
- Bewust NIET beschreven: PDF afdrukken en Excel exporteren bij stap 9 (Niels werkt dat nog af). Alleen vermeld als "in opbouw".
- Help-koppeling: `ViewData["HelpScreenId"]` = `budget-index` (BudgetIndex) en `budget-stap-1` t/m `budget-stap-9` (`PrepareWizardV2`);
  de artikels verwijzen ernaar via `screens:`. Controllerwijziging, herstart nodig voor het "?"-paneel.
- Nog in te vullen als `[VRAAG: ...]` in de artikels (zichtbaar in de help tot beantwoord): actief budget, peildatum/S-index/I-index,
  aan te bouwen buren, afkortingen in stap 2, berlinerwanden/secanspalen/onderschoeiingen, wat Overnemen doet (stap 1) en Referentie
  overnemen (stap 6), referentieprojecten beheren, prijscodes (teken P, algemene codes), Voorstel/Markt overnemen en Doorzetten naar units
  (stap 8), Versies vergelijken, Niet-definitief maken (welke status), budget verwijderen, decennale/Breyne/straight loan.
- Bekend, niet opgelost: op de pagina Budgetten wordt de versiekaart rechts afgeknipt (rij-actie "Open versie" deels buiten de kaart).

## 13/10/2026 (vervolg) — antwoorden budget verwerkt
- Verwerkt: actief budget = gebruikt voor de nacalculatie; peildatum/S-index (lonen)/I-index (materialen); Overnemen (stap 1) = standaardprijs terug;
  Referentie overnemen (stap 6) = bedrag van de geselecteerde referentieprojecten; Voorstel/Markt overnemen (stap 8); Niet-definitief maken = terug
  naar Afgerond; aan te bouwen buren, berlinerwanden/secanspalen/onderschoeiingen, decennale (Wet Peeters), Wet Breyne, straight loan.
- Doorzetten naar units nagekeken in de code (`VerkoopVoorstelService.DoorzettenNaarUnitsAsync`): schrijft grondwaarde (incl. tweede zone) en
  bouwwaarde van de aan een eenheid gekoppelde verkooplijnen naar de Units; verkochte units blijven ongemoeid; fout als geen enkele lijn gekoppeld is.
  Het zet NIET de vraagprijzen op de eenheden.
- Open: Niels zegt dat een niet-actief budget verwijderd kan worden, maar er is geen verwijderactie of knop (controller en views) gevonden.
  Artikel `budget-overzicht` heeft hierover nog een `[VRAAG]`.
- Fix: versiekaart op de pagina Budgetten werd afgeknipt (kaarten stonden vanaf 1200px twee naast elkaar, de tabel heeft ~640px nodig).
  `BudgetIndexV2.cshtml`: `repeat(auto-fill, minmax(min(100%, 660px), 1fr))` + `overflow-x:auto` op de tabelwrap. Gebouwd (0 fouten), nog niet in de
  browser bekeken (app stond uit): herstart nodig.
- "Versies vergelijken" (BudgetVergelijken) heeft nog de oude (niet-gl-v2) vormgeving.

## 14/10/2026 — Budgetten: Nieuw budget (39l), Vergelijken (39m) en Vergelijkingspagina (39n)
- Nieuw artikel `budget/budgetten-vergelijken.md` (parent `budget-overzicht`, treeOrder 3, `screens: budget-vergelijken`); `budget-nieuw.md` herschreven voor de nieuwe modal
  (startpunt, overnemen per stap, eenheden, index, meteen actief) en `budget-overzicht.md` bijgewerkt (kolommen Datum en Marge, "stap x van 9" in Totaal, kaart Definitief budget,
  Vergelijken, lege kaart). `budget-stap-resultaat.md` verwijst nu naar de nieuwe vergelijking.
- Help-koppeling: `ViewData["HelpScreenId"] = "budget-vergelijken"` op `BudgetVersiesVergelijken`.
- Geen nieuwe schermafbeeldingen: de pagina's zijn gebouwd en visueel getest met statische voorbeelddata, niet in de draaiende app. De oude schermafbeeldingen van de modal Nieuw budget
  (`budget-nieuw/1-nieuw-budget.png`) en van het overzicht (`budget-overzicht/1-overzicht.png`) zijn niet meer in de artikels opgenomen omdat ze het oude scherm tonen. Nieuwe opnames maken
  met het Playwright-recept in DESIGN.md zodra de pagina's in de app getest zijn.
- Geen [VRAAG] bij dit artikel; de marge-definitie (resultaat / kostprijs) volgt de keuze van Niels van 14/10/2026.

## 14/10/2026 — Budget-PDF opmaken (44d–f)
- `budget/budget-stap-resultaat.md`: nieuwe sectie "Een PDF maken" (PDF intern/bank, opmaakpagina, nieuw tabblad, Download PDF, niet bewaard in Documenten), extra FAQ, oude "nog in opbouw"-zin vervangen. Geen schermafbeeldingen (niet in de draaiende app getest).
- 14/10/2026 (later): budget-stap-resultaat.md en budget-nieuw.md bijgewerkt (PDF bewaard in Documenten › Budgetten, verwijderen met rood bevestigingsvenster). Migratie 099 vóór gebruik.

## 15/10/2026 — helpartikels en publicatie
- Gevonden: een `dotnet publish` nam de foto's (`wwwroot/img/help`) wel mee, maar NIET de teksten (`CPMCore/HelpContent/**/*.md`): `.md` is geen Content.
  Opgelost met `<None Update="HelpContent\**\*.md" CopyToPublishDirectory="PreserveNewest" />` in `CPMCore.csproj` (gecontroleerd: 46 van 46 bestanden in de publish-map).
- Handmatig kopiëren (zonder publish) = de mappen `CPMCore/HelpContent` én `CPMCore/wwwroot/img/help` naar de server, plus de nieuwe build voor de code.
- Op de server worden de artikels maar één keer geladen (geheugen). Na het plaatsen de app herstarten (of recyclen) om wijzigingen te zien.
- Nog uit te voeren op de live DB: `DALCore/Migrations/HelpFeedback.sql` (feedback "Was dit nuttig?" en supportvragen).

## 21/10/2026 — Help bijgewerkt na de controle van Niels
- `budget-stap-activiteiten.md` (correctie: selecteren bij klikken, decimalen, leeg = 100, 0 mag, reden met goudkader en waarschuwingsstaat, reden in PDF intern, extra FAQ), `budget-stap-parameters.md` (Wet Breyne = (bouwkost + erelonen en studies) × 1,1; straight loan gebouw = helft van de looptijd; geen marge op financiering), `budget-stap-resultaat.md` (PDF in drie stappen, Excel via de opmaakpagina, bewaard in Documenten), `budget-nieuw.md` (verwijderen), `budget-overzicht.md` (tegels Gecontracteerd/Gefactureerd/Verwacht verschil), `budget-meldingen.md` ("reden" i.p.v. "opmerking"). Alle `updated: 2026-10-21` en `newsTag: Bijgewerkt`.
- Afbeeldingen staan nu als eerste regel van hun stap (stappenplan punt 3); in `budget-stap-resultaat.md`, `budget-nieuw.md` en `budgetten-vergelijken.md` stonden ze midden in een stap en werden zo niet getoond als stapbeeld.
- **Gedaan (21/10/2026, op het testbudget in project 79):** `budget-stap-activiteiten/4-correctie-reden.png` en `budget-overzicht/5-definitief-tegels.png` toegevoegd (e-mailadres in de kaart vervangen). **Ook vernieuwd:** `budget-stap-activiteiten/1`–`3` (kaart Nacalculatie, tabel per lot, berekeningen) op het testbudget; vroeger: nieuwe opnames van stap 6 (de bestaande `budget-stap-activiteiten/*.png` tonen nog het scherm zonder goudkader en redenveld) en van de tegels in de kaart Definitief budget; vereist de draaiende app met de migraties 107 en 108.
