# Budgetwizard — voortgangsstatus

Doorlopend statusdocument voor de budgetwizard (Projecten › Budgetten, 9 stappen). Hoe het gebouwd is staat in
DEVNOTES.md §3, §5 en §7; dit bestand houdt bij wat af is, wat open staat en welke beslissingen genomen zijn.

**Laatste update:** 2026-10-08 — de volledige budgetflow (overzicht + 9 stappen) staat in gl-v2 volgens design-handoff
punt 39, met versiestatus Concept/Afgerond/Definitief (deel 5). Niets gecommit, niets browser-getest.
**Migraties 075 → 078 uitvoeren vóór de app start** (`_migrations/078_BudgetVersieStatus.sql` is nieuw).

## 13/10/2026 — Parameters: Algemeen-activiteiten en percentages uit Bouwkost % (migraties 091 en 092)

**Uitvoeren: `091_BudgetAlgemeenFormules.sql` en `092_BudgetParamsOpmetingSondering.sql`.** Niet browser-getest.
- **EPB, veiligheidscoördinatie en ventilatieverslaggeving** zijn nu activiteiten in lot 0 Algemeen met een formule `@aantal_wooncomm * @mat_algemeen_<sleutel>` (091 zoekt bestaande activiteiten op naam, anders maakt ze die aan; vereist de koppelingen `algemeen_epb`, `algemeen_vc`, `algemeen_ventilatieverslaggeving`). Het percentage "Veiligheidscoörd. + EPB" en het forfait "Ventilatieverslaggever" zijn van de Parameters-pagina en uit de kostprijs gehaald (anders dubbel).
- **ActivityID's** (opgegeven door Niels): EPB 166, veiligheidscoördinatie 165, ventilatieverslaggeving 278 (formules, migratie 091 gebruikt deze ID's en maakt geen activiteiten meer aan); opmeting 168, sondering 182, tienjarige/decennale 142 (percentageposten op Parameters). daarnaast architect 162, ingenieur 163, projectcoördinatie 277, ABR 142 (zelfde als decennale: samen tellen in het totaalbudget), financiering Wet Breyne 294 en straight loan 295 (gebouw én grond); onvoorzien krijgt bewust geen activiteit (reserve in het totaalbudget). Constanten in `BudgetAlgemeneActiviteit` (FormulaSleutels.cs); de kostenposten van het resultaat dragen nu hun `ActivityId` zodat ze later aan contracten/facturen gekoppeld kunnen worden.
- **Opmeting en sondering** zijn twee aparte percentages op de bouwkost (`OpmetingPerc`, `SonderingPerc`) i.p.v. het forfait "Opmeting + sondering" (kolom blijft bestaan maar telt niet meer mee). Ze blijven grondgebonden in het verkoopvoorstel.
- **Standaardwaarden uit Instellingen › Bouwkost %** (naam/sleutel): Decennale = rij "Wet Peeters (10-jarige)", ABR = "ABR", Opmeting = "Opmeting", Sondering = "Sonderingen"; een leeg of 0-veld op Parameters volgt de standaard (`BudgetBerekeningService.ZoekStandaard`). De decennale-waarschuwing verdwijnt als er een standaard bestaat.
- Twee bestaande tests (`StructuredReferenceServiceTests`, OGM/EPC-betaalstructuur) falen; ze horen niet bij het budget.

## TE DOEN — nog na te kijken door Niels (genoteerd 12/10/2026)

Inhoudelijke controles op de berekeningen (geen bouwwerk, wel nakijken en bijsturen waar nodig):
1. ~~**Nacalculatiebedragen bekijken** (stap 6)~~ — nagegaan door Niels (13/10/2026). Er moeten later nog verbeteringen gebeuren, zie hieronder.
2. ~~**Percentages bij Parameters bekijken** (stap 7): standaardwaarden uit Instellingen › Bouwkost %, de voorstellen per rubriek en de afwijkingswaarschuwing (> 50 % t.o.v. Instellingen).~~ — nagekeken door Niels (14/10/2026).
3. ~~**Alle berekeningen bij Parameters nakijken** (stap 7): bedrag onder elk percentage, totaal parameters en het percentage t.o.v. de bouwkost; financiering (Wet Breyne, straight loan), onvoorzien en forfaits.~~ — nagekeken door Niels (14/10/2026).
4. ~~**Verkoop nakijken** (stap 8): grond- en bouwwaarde, minimumprijs, marges, prijscodes (B-/G-), marktprijs en de vraagprijs per eenheid, incl. garages en poorten.~~ — nagekeken door Niels (14/10/2026).
5. ~~**Resultaat nakijken** (stap 9): kostenoverzicht, per eenheid, bouwkost t.o.v. nacalc, marge en de waarschuwingen/fouten.~~ — nagekeken door Niels (14/10/2026).

## VERBETERINGEN NA NACALCULATIE — later te doen (genoteerd 13/10/2026)

Uit de nacalculatiecontrole van Niels; geen van deze punten is al aangepast:
- **Hellend dak en zolder**: geen beplating rekenen.
- **Oppervlakte plat dak / hellend dak** berekenen uit de andere projectbudgetten en daarmee vergelijken, in plaats van gewoon op de bewoonbare oppervlakte.
- **Dekvloer / PUR / schuimbeton** opsplitsen (nu één activiteit).
- **Tuin**: onderscheid "afgewerkt" of "genivelleerd".
- **Terras / oprit**: onderscheid "afgewerkt" of "onderlaag".
- **Sanitaire installatie**: prijzen nakijken, vooral plaatsing toestellen, verwarming en ventilatie, in functie van nieuwe technieken.
- **Buitenschrijnwerk**: prijzen vergelijken met de oppervlaktes uit andere budgetten, indien beschikbaar.

## 12/10/2026 — Stap 1: herleidbare funderingsoppervlakte + automatisch grondwerk (migratie 089)

`_migrations/089_BudgetGegevensGrondwerk.sql` uitvoeren (BudgetGegevens: FunderingLijnen, DiepteFunderingCm, DiepteOndergrondsCm). Niet browser-getest.
- Oppervlakte funderingen kan nu per type opgebouwd worden (knop "Berekening" naast het veld: omschrijving · aantal · m² per stuk); de som is het veld (read-only zodra er regels zijn), de regels blijven bewaard.
- Grondwerk (m³) is automatisch = oppervlakte funderingen × diepte fundering / 100 + oppervlakte ondergrondse verdiepingen × diepte per verdieping / 100; standaard 40 cm en 300 cm, per versie aanpasbaar. De oppervlakte van de ondergrondse verdiepingen is een eigen veld (totaal van alle ondergrondse verdiepingen, eveneens herleidbaar via een berekening); leeg = aantal verdiepingen × oppervlakte funderingen. Server (`GrondwerkRekenregel`, in `SaveBudgetGegevens`) en JS rekenen hetzelfde. Kolommen `OppOndergronds`/`OndergrondsLijnen` zitten ook in migratie 089.
- Oppervlaktes: een nieuw toegevoegde eenheid respecteert nu de aan/uit-staat van de kolomgroepen.

## 12/10/2026 — Stap 6: formulestrook per activiteit ("Toon berekeningen", 39g)

Onder elke activiteit met een voorstel staat (als "Toon berekeningen" aanstaat) de berekening als chips: `[Oppervlakte funderingen 548,10 m²] × [Eenheidsprijs 100,55 €/m²] × [Index S 1,094] = € 60.290,80 · bron: stap 1 · Gegevens · Formule ›`.
- Formule-activiteiten (Instellingen › Budgetformules): `BudgetActivityFormuleService.BouwStrook` ontleedt de formule in termen en factoren; materiaalprijzen tonen basisprijs én indexfactor apart; de bron komt uit de "tab …"-vermelding van de gebruikte parameters; link "Formule ›".
- Overige voorstellen (ruwbouw, enkelvoudig prijs × hoeveelheid, gevelsluiting, dak): `BudgetActivityService.BouwStrookUitVoorstel`; bron per activiteit uit een kleine lijst (aanvullen waar nodig).
- Grondwerken (activiteit 173) en de formuleparameter `m3_grondwerken` gebruiken nu het automatische grondwerk van stap 1 (oude vaste 0,30/3,50-regel vervangen).
- Niet browser-getest.

## 12/10/2026 — Overige aanpassingen (na de formulestrook)

- **Rijtotalen volgen de formule**: een niet-handmatige activiteitenlijn gebruikt altijd het actuele voorstel/formuleresultaat; het bewaarde bedrag per eenheid telt alleen nog voor `IsManueel`-lijnen (`BudgetActivityService`, alle vijf de plekken). Voorheen bleef een eerder bewaard bedrag staan nadat een materiaalprijs wijzigde.
- **Ramen worden van gevels afgetrokken**: formuleparameter `opp_gevels` = netto (per soort, nooit < 0); nieuwe `opp_gevels_bruto`. Het gevelmetselwerk-voorstel gebruikt ook netto. Een formule die ramen al zelf aftrok, telt nu dubbel — nakijken in Instellingen › Budgetformules.
- **"Toon berekeningen" zweeft** rechtsboven zodra de topbar uit beeld scrolt (`gl-v2-budget.js`, één vaste waarnemer op een ankerplek). Sleutel in localStorage is nog `bw-show-calc`.
- **Stappenplan**: een klik op een chip gaat altijd naar die stap (capture-handler met `data-hrefs`); een kapotte "dirty"-callback op Verkoop (oneindige lus) is weggehaald.
- **Foutvenster bij mislukt opslaan** toont de reden en de open fouten met link; `post()` geeft altijd een leesbare reden terug.
- **Index zonder peildatum**: opzoeken met tolerantie 0,005 (de binder rondt af op 2 decimalen) en bij het opslaan terugzetten op de exacte indexrij.
- **Grondwerken-activiteit en `m3_grondwerken`** gebruiken het automatische grondwerk van stap 1.
- **Afbraak (13/10)**: nieuwe formuleparameter `opp_afbraak` (totaal m² van de afbraakregels op stap 5) en een ingebouwd voorstel voor activiteit 172 (lot 0 Algemeen) = `opp_afbraak` × geïndexeerde prijs van de koppeling `algemeen_afbraak`; een eigen formule (`@opp_afbraak * @mat_algemeen_afbraak`) gaat voor.
- **Open, niet opgelost**: het tabblad Budgetactiviteiten liep eenmalig vast met "Toon berekeningen" aan (na een materiaalprijswijziging); oorzaak niet gevonden, een nieuw venster werkte wel. Een extra beveiliging staat in de formulestrook (max. 24 onderdelen, fouten ingeslikt). Als het terugkomt: noteren wat er net gebeurde (schakelaar, prijs, vernieuwen).

## 12/10/2026 — Garagepoorten per rij (migratie 090)

`_migrations/090_BudgetOppervlaktesPoorten.sql` uitvoeren. Stap 2 heeft in de groep "Parkeren & berging" een kolom **Poorten** (aantal garagepoorten per rij). Ingebouwde garage = poorten op de woning-rij (geen oppervlakte); apart bijgebouwd = ook oppervlakte in "Gar. bgr."; apart verkochte garage = eigen garage-rij (zoals voorheen).
Formuleparameters `aantal_poorten_sectionaal/_kantel` = som van de kolom Poorten, plus 1 per garage-rij met 0 poorten (bestaande budgetten veranderen niet). De poortwaarschuwing op stap 1 kijkt nu ook naar die kolom.

## STAND 09/10/2026 — alles gebouwd en door Niels getest; enkel nog open punten en grotere stappen

De budgetflow in gl-v2 (overzicht + 9 stappen, versiestatus, meldingen 39k, veldstaten 14d, bevestigingsmodals, foutvenster bij mislukt opslaan) werkt volgens Niels.
Niets meer te verifiëren van wat eerder genoteerd stond. Resteert:

**Open punten**
- `confirm()`/`alert()` vervangen door de eigen bevestigingsmodal in: Instellingen › Budget (kostprijsmaterialen, formules, bouwindexen, kostprijs-update) en de klassieke pagina's.
- Trage budgetpagina's: `BudgetActivityService.GetLotGroepenAsync` draait meermaals per pagina (stap 6/9); eventueel databse-indexen. Eerst opnieuw meten.
- Overzicht (39a): tegels Gecontracteerd / Gefactureerd / Verwacht verschil blijven placeholders tot de koppeling hieronder bestaat.
- Infolabel "Marktprijs op …" (39k, stap 8) niet gebouwd: de marktdata heeft geen datum.
- Marge-waarschuwing "5 % t.o.v. definitief" (39j) is geïnterpreteerd als meer dan 5 procentpunt onder de marge van het definitieve budget; bevestigen bij gelegenheid.
- Veldwaarschuwing (14d) staat in de gedeelde stijl maar wordt enkel gebruikt waar een melding bestaat; op andere pagina's per veld koppelen indien gewenst.

**Grotere stappen (in deze volgorde)**
1. Koppeling budget ↔ contracten ↔ facturen (vult de tegels op het overzicht en 35h).
2. Budget-PDF en -Excel (design-handoff 35h).
3. Nacalculatie van het lopende project (menu-item "Nacalculatie", los van het budget).

Nog steeds: niets gecommit op branch `layout-experiment`; migraties 078 en 080 uitvoeren op elke omgeving.

## OVERDRACHT — stand van zaken (08/10/2026, einde sessie)

**Branch `layout-experiment`, niets gecommit** (veel gewijzigde en nieuwe bestanden; commit eerst voor je van pc wisselt). Build slaagt.
**Niets is in een draaiende app/browser getest** (enkel statische headless-renders van stap 9). Eerst doen op de andere pc:
1. Migraties uitvoeren op de DB: `078_BudgetVersieStatus.sql` en `080_BudgetVersieGewijzigd.sql` (079 is van Punten). Daarvóór ook 075–077 als die nog niet liepen.
2. App herstarten/rebuilden en de flow doorlopen met de gl-v2-cookie: overzicht → stap 1…9 → Afronden / Definitief → Niet-definitief.

**Wat gebouwd is (deel 5 + opvolging):** volledige budgetflow in gl-v2 (overzicht + 9 stappen), versiestatus Concept/Afgerond/Definitief, hernoemen van budget/versie,
"Laatst gewijzigd" (`GewijzigdOp`) en bewaarde totale kostprijs (`TotaalKosten`, lui herberekend na elke wijziging), tabbar/meldingskaders/dropdowns volgens DESIGN.md,
eigen bevestigingsmodal i.p.v. `confirm()` (budgetflow: `V2/_BwBevestigModal` + `GlV2Budget.bevestig`; Instellingen V2: `GlV2/_BevestigModal` met `data-bv-titel`).
Projectbreed aangepast: dropdown-chevron blijft zichtbaar na keuze (`gl-v2/forms.css`), uitgeschakelde dropdown-stijl, combo `data-allow-new` nu expliciet "true"/"false",
entranceanimatie met fill-mode `backwards` (anders knippen fixed dropdown-panelen af, zie memory).

**Nog te controleren / open:**
- Dropdowns hoofdtype/subtype op Oppervlaktes: oorzaak (animatie-fill-mode) gefixt maar niet in de echte app bevestigd.
- Bevestigingsmodal op Referentieprojecten: script nu inline in de partial; controleren dat hij verschijnt.
- Projectkiezer op Referentieprojecten: "nieuw item"-rij zou weg moeten zijn; zo niet, schermafbeelding nemen.
- Traagheid: opgelost zijn de projectlading (14 includes) en N+1 indexqueries; nog open: `BudgetActivityService.GetLotGroepenAsync` meermaals per pagina (stap 6/9), mogelijk DB-indexen.
- Overige Instellingen › Budget-pagina's (kostprijsmaterialen, formules, bouwindexen, kostprijs-update) en de klassieke pagina's gebruiken nog `confirm()`/`alert()`.
- Placeholders tot de koppeling: tegels Gecontracteerd/Gefactureerd/Verwacht verschil op het overzicht.
- Volgende grote stappen: koppeling budget ↔ contracten/facturen; budget-PDF/Excel (35h); nacalculatie van het lopende project (menu-item "Nacalculatie").
- Werkwijze: DESIGN.md sectie "Budgetflow — design-handoff punt 39" beschrijft componenten, beweging en schermen.

## Gedaan op 2026-10-09 (deel 6: meldingen per stap, design-handoff punt 39k)

Geen nieuwe migratie. `BudgetControleService` berekent alle fouten/waarschuwingen/infomeldingen van de flow, de pagina's tonen ze als veld, rij, kader of label,
het Stappenplan telt ze en fouten blokkeren Afronden. Zie DESIGN.md "Budgetflow — meldingen per stap, punt 39k" voor de volledige tabel. Niet browser-getest.
- Stap 6 heeft nu een **opmerking** per activiteit (bewaard in `BudgetActivityLijnen.Omschrijving`, verschijnt zodra de correctie ≠ 100 % is).
- Bewuste keuzes: "marge 5 % t.o.v. definitief" = meer dan 5 procentpunt onder de marge van het definitieve budget; "geen enkel daktype" = geen plat/hellend/groen-regel; negeren werkt per code.
- Niet gebouwd: het infolabel "Marktprijs op …" (de marktdata heeft geen datum).

## Gedaan op 2026-10-08 (deel 5: budgetflow in gl-v2, design-handoff punt 39)

**Migratie `_migrations/078_BudgetVersieStatus.sql` uitvoeren** (BudgetVersie: VastgezetOp/-Door, LaatsteStap, VmswFactoren,
WaarschuwingenBevestigd). Niets browser-getest — de klassieke pagina's blijven ongewijzigd bereikbaar zonder de gl-v2-cookie.

- **Versiestatus** (keuze Niels): *Concept* → *Afgerond* (blijft bewerkbaar, stap 9 "Afronden") → *Definitief* (alleen-lezen,
  **één per project**; "Afronden & definitief maken"; een eerdere definitieve versie valt terug op Afgerond). Op een definitieve
  versie blokkeert `BudgetVersieVergrendeldFilter` elke POST (behalve status wijzigen/kopiëren/downloaden) en zet de gl-v2-JS alle
  velden uit; "Niet-definitief maken" staat in de vergrendelbalk. `IBudgetService`: `AfrondenVersie`, `MaakDefinitief`,
  `OntgrendelDefinitief`, `IsVergrendeld`, `RegistreerStap`, `Get/SetVmswFactoren`, `BevestigWaarschuwing`.
- **Overzicht 39a** (`BudgetIndexV2`): kaart "Definitief budget" (tegels Gecontracteerd/Gefactureerd/Verwacht verschil zijn
  **placeholders** tot de koppeling budget ↔ contracten/facturen — keuze Niels), per budget een kaart met versies
  (status "Concept · stap x van 9" via `LaatsteStap`), "Maak actief" (`BudgetMasterActiveren`), "Nieuwe versie op basis van de
  huidige" (modal → `BudgetNieuweVersie`), "Nieuw budget" (modal; **leeg of als kopie** van een versie van eender welk project:
  `BudgetMasterAanmakenModel.KopieVanVersieId` → `KopieerVersieInhoud`), "Andere versie definitief maken".
- **Stappen 1–9** (`Budget…V2.cshtml` + partials in `Views/Projecten/Budget/V2/`): gedeelde chrome `BudgetWizardChromeVm`
  (`PrepareWizardV2` in `ProjectenController.BudgetV2.cs`: stappen met fouten/aandachtspunten, vorige/volgende, menu, lock),
  `_BwStappen` (chips, onder 1024px balk + paneel), `_BwActiebalk` met opslagstatus, autosave per veld/rij (`GlV2Budget` in
  `gl-v2-budget.js`), berekeningen uitklapbaar ("Toon berekeningen", onthouden in localStorage).
  Stap 2: **VMSW-reductiefactoren aanpasbaar per versie** (kaart, `BudgetVmswFactorenOpslaan`, keuze Niels); stap 7: autosave via
  `BudgetParamsOpslaan`, waarschuwing "decennale 0 %" wegklikbaar (`BudgetWaarschuwingBevestigen`); stap 8: rij volgens het
  mockup + uitklapbare €/m²-regel (`_BwVerkoopDetail`); stap 9: waarschuwingenlijst met link naar de stap, KPI-tegels,
  kostenoverzicht, per eenheid, bouwkost t.o.v. nacalc, Kopie als nieuwe versie / Afronden / definitief maken.
- **Waarschuwingen per stap** (`BerekenStapWaarschuwingenAsync`): stap 1 poorten zonder type, stap 2 woning zonder grond,
  stap 7 decennale 0 % (tenzij bevestigd), stap 8 vraagprijs onder minimum. Fouten blokkeren "definitief maken", waarschuwingen niet.
- **Na eerste test (08/10)**: inner-menu-CSS + telefoonmenu op alle V2-pagina's; `v@(…)` Razor-fout in de versielijst; **hernoemen** van budget en versie vanuit het overzicht (potlood); **Laatst gewijzigd** is nu echt: kolom `BudgetVersie.GewijzigdOp` (**migratie `080_BudgetVersieGewijzigd.sql`**), bijgewerkt door `BudgetVersieVergrendeldFilter` na elke geslaagde schrijvende actie (voorheen toonde het de aanmaakdatum); overzicht krijgt `GlV2FullHeightBody`.
- **Traagheid**: elke budgetpagina laadde het volledige project (14 includes incl. documenten en foto's) enkel voor de naam, en `BuildContextAsync` deed 2 indexqueries per materiaal (N+1, ook per versie in het overzicht). Nu `GetProjectNameById` en een per-request cache in `BouwIndexService`. Eerstvolgende kandidaten: het overzicht rekent per versie de volledige kostprijs uit; `BudgetActivityService.GetLotGroepenAsync` wordt meermaals per pagina uitgevoerd.
- Documentatie: DESIGN.md "Budgetflow — design-handoff punt 39".

## Gedaan op 2026-10-08 (deel 4: beheerpagina's in gl-v2, design-handoff punt 38)

**Migratie `_migrations/077_BudgetReferentiesBeheer.sql` uitvoeren** (na 075/076). Niets browser-getest.

- **Prijsreferenties verkoop** (`BudgetPrijsReferentiesV2.cshtml`, 38a/38b): tabbar Bouw/Grond, toevoegen-kaart met volgende
  vrije code, lijst met zoeken/projectfilter/"ouder dan 12 maanden", actualiteit-badge, **bewerken per rij**, export .xlsx,
  **archiveren** i.p.v. verwijderen als een code in een verkooplijn gebruikt wordt (`Gearchiveerd`, niet meer kiesbaar op stap 8).
  Codes heten nu `B-01`/`G-07` (`CodeLabel`).
- **Referentieprojecten** (`BudgetReferentieProjectenV2.cshtml`, 38c): Excel-kaart met uploadvak en sjabloon, project-kaart met
  **preview** (eenheden, GBA, facturen/contracten, status) vóór de snapshot, lijst met index-factor S/I en uitklapbare bedragen.
- **Controle na Excel-import** (`BudgetReferentieImportControleV2.cshtml`, 38d, enkel gl-v2): niet-gematchte regels bovenaan met
  keuzelijst per regel; opslaan mag met open regels — die worden bewaard met `ActivityId NULL` (tellen in het totaal, niet per
  activiteit; stap 6 negeert ze). `ExcelNaam`/`Match` bewaren hoe een regel binnenkwam. De klassieke pagina importeert nog direct.
- **Snapshot uit de app telt geen "Meerwerk voor klant" (factuurtype 3)** meer mee — dezelfde splitsing als Gefactureerd /
  Meerwerken klanten op Contracts/Recalculation. Snapshots van vóór 08/10 opnieuw maken. Op stap 6 staat de referentie
  geïndexeerd (peildatum → huidige index van de versie): daarom wijkt het bedrag daar af van de lijst in Instellingen.
- **Correctie (09/10)**: eerder stond hier een "bug" over `type="number"` en de nl-BE-binder ("2150.5" → 21505). Dat klopt niet: `FlexibleDecimalModelBinder` kent geen duizendtalscheiding en valt terug op invariant, dus "2150.5" wordt 2150,5. De prijsvelden zijn wel tekstvelden met komma (consistent met de gl-v2-velden), maar dat was geen bugfix. Let wel: de binder rondt op 2 decimalen af (3-decimaal-velden gaan daarom via JSON).
- Documentatie: DESIGN.md "Instellingen/Prijsreferenties verkoop en Referentieprojecten — punt 38".

## Gedaan op 2026-10-07 (deel 3: nacalculatie in stap 6)

Gebouwd zodat Niels er zijn design-handoff op kan maken. **Migratie `_migrations/076_BudgetReferentieProjecten.sql` uitvoeren**
(drie nieuwe tabellen). Niets browser-getest.

- **Referentieprojecten** (`BudgetReferentieProject` + `…Lijn`): een afgewerkt project met zijn werkelijke kost per activiteit op een
  peildatum, met S- en I-index van die datum (via `BouwIndexService.GetIndexOpDatumAsync`). Twee bronnen:
  - *Uit de app*: snapshot per activiteit van de inkomende facturen (`IncommingInvoiceDetail` via `ActId` of de contractactiviteit);
    zonder facturen de contractbedragen. Eenheden = woon-/commerciële units van het project, GBA uit de recentste budgetversie.
  - *Uit Excel* (projecten van vóór de app): kolommen `ActivityId · Activiteit · Bedrag`; matching op id, anders op genormaliseerde
    naam; niet-herkende rijen komen als waarschuwing terug. Sjabloon downloadbaar (alle activiteiten per lot).
  Beheer: **Instellingen › Budget › Referentieprojecten (nacalc)** (`BudgetReferentieProjecten`), service
  `IBudgetReferentieProjectService`/`BudgetReferentieProjectService`, 5 tests in `NacalcReferentieTests`.
- **Keuze per budgetversie** (`BudgetVersieNacalcReferentie`): op stap 6 een paneel "Nacalculatie — vergelijken met afgewerkte
  projecten" met chips per referentieproject + Toepassen. `KopieerVersieInhoud` kopieert de keuze mee.
- **Vergelijking in stap 6** (`BudgetActivityLijnen.cshtml`, `_ActivityLijnRow.cshtml`): kolommen *Referentie (nacalc)* en *Verschil*
  tussen "€ basis" en "Correctie". Referentie per activiteit = Σ werkelijke kost × gewogen indexfactor (40 % I + 40 % S + 20 %) ÷
  Σ eenheden van de gekozen projecten, × eenheden van dit budget (per m² GBA in de tooltip; min–max bij meerdere projecten).
  Verschil = gecorrigeerd budget t.o.v. referentie, live; per lot en totaal ook. Knop per rij "referentie overnemen" zet de
  correctie-% zodat gecorrigeerd = referentie. KPI-tegel "Referentie nacalc". Bij opslaan wordt `NacalcPrijsPerEenheid` gevuld met
  de geïndexeerde referentie per eenheid → de KPI "Alt. vs nacalc" op stap 9 leeft weer (daar niet meer × gewogen factor; de
  bewaarde nacalc is al op de huidige index — `NacalcGeindexeerd` in de BO idem).
- Oude stub `ImportNacalcVanProject` en `BeschikbareProjecten` verwijderd.
- Nog open: Excel-import van *meerdere* projecten in één bestand; referentie per m² als basis kiezen i.p.v. per eenheid;
  activiteiten die in het referentieproject wél en in dit budget géén bedrag hebben zichtbaar maken (nu enkel "n zonder referentie").

## Gedaan op 2026-10-07 (deel 2: verkoop per m²)

- **Fout stap 7 → 8** ("Nullable object must have a value", `BudgetVerkoop.cshtml`): de voorsteltabel las `MarktVerschilPerc.Value`
  terwijl die leeg is voor een eenheid met minimumverkoopprijs 0 (geen oppervlakte-aandeel). Beide navigaties (tab bovenaan en
  knop onderaan) liepen op dezelfde GET-fout. Nu enkel tonen wanneer er een waarde is.
- **Model** (migratie 075): `BudgetVerkoopLijnen.BouwPrijsPerM2`, `GrondPrijsPerM2`, `PrijsBron` (enum `BOCore.VerkoopPrijsBron`:
  1 voorstel, 2 markt, 3 referentiecode, 4 manueel €/m², 5 manueel bedrag); `BudgetPrijsReferentie.Datum`, `Bron`.
  Partials `DALCore/Models/BudgetVerkoopLijn.PrijsPerM2.cs` en `BudgetPrijsReferentie.Bron.cs`; `KopieerVersieInhoud` neemt ze mee.
- **Rekenregel** `ServiceCore.Budget.VerkoopLijnRekenregel` (puur, 9 tests in `VerkoopLijnRekenregelTests`): bouw = €/m² × gereduceerde
  opp., grond = €/m² × grondopp. (bron code/€/m²), anders €/m² afgeleid uit het bedrag; vraagprijs = grond + bouw + forfait, behalve
  een eigen vraagprijs bij "manueel bedrag"; ruil ⇒ grondwaarde 0. `SaveBudgetVerkoop` past dezelfde regel server-side toe.
- **Stap 8 herbouwd** (`BudgetVerkoop.cshtml`, `_VerkoopRij.cshtml`): kolommen Opp. tuin/terras/dakterras weg (zitten in stap 2);
  per lijn Bron-badge, Grond (code → €/m² → bedrag), Bouw (code → €/m² → bedrag), Ruil, Extra forfait, Vraagprijs met waarschuwing
  onder het minimum; per lijn knoppen "voorstel" en "markt" overnemen; bovenaan "Voorstel overnemen" / "Markt overnemen" voor alles;
  totaalrij + samenvatting (vraagprijzen vs. minimale verkoopwaarde, aantal lijnen onder minimum). Oppervlaktes per eenheid komen via
  `EenhedenInfo` (naam uit stap 2) naar het JS; een vrij ingevulde naam die niet in stap 2 staat, krijgt geen €/m²-berekening.
- **Prijsreferenties**: `IBudgetPrijsReferentieService`/`BudgetPrijsReferentieService` (uniek per type + code + project); beheerpagina
  Instellingen › Budget › **Prijsreferenties verkoop** (`BudgetPrijsReferenties`, tabs Bouw/Grond, inline bewerken, optioneel project);
  op stap 8 een rij per tabel om een **projectcode** toe te voegen/verwijderen (algemene codes alleen in Instellingen).
  Legenda toont nu ook datum en bron; projectcodes dragen "P".
- Doorzetten naar units ongewijzigd (grond-/bouwwaarde per lijn).
- **Opslaan gaf "Verbindingsfout"**: `SaveBudgetVerkoop` bond de JSON rechtstreeks op de EF-entiteit; System.Text.Json bouwt dan de
  navigatiegraaf (Unit → … → CompanyInfo) en struikelt over `postCode`/`PostCode` → 500 vóór de actie. Nu via `VerkoopLijnDto`
  (ProjectenController.cs). Regel: JSON-posts nooit op entiteiten binden.

## Gedaan op 2026-10-07 (deel 1)

- **Versies kopiëren via één helper** `BudgetWizardService.KopieerVersieInhoud(bron, doel)` (ook op `IBudgetService`): gegevens
  (élk veld), oppervlaktes, sanitair, gevel-/dakelementen, activiteitenlijnen, parameters en verkooplijnen. Gebruikt door
  "Nieuwe versie" (kopieerde voordien enkel stap 1–5) en "Herstel als nieuwe versie" (vergat 6 gegevensvelden: veluxen,
  trapzalen, toegangspoorten, aan te bouwen buren, bouwheer, onderschoeiingen). Nieuwe kolommen op een budgettabel horen in
  die helper.
- **Aankoopprijs grond is een kostenpost** (`BudgetResultaatBO.Grond`, groep "Grond" op stap 9, in de versievergelijking, PDF en
  Excel). "Totale kostprijs" op stap 9 = "Kostprijs incl. grond" op stap 8. `VerkoopVoorstelService` haalt die post er weer uit
  voor de bouwkost, zodat de grond niet dubbel telt (unit-test `Grondpost_in_resultaat_telt_niet_dubbel`).
- **Stap 9 toont één bouwkost**: de KPI "Alt. vs nacalc" gebruikt nu dezelfde gecorrigeerde bouwkost als de kostprijs.
- **Projectcoördinatie-%**: enkel een lege waarde (0) krijgt de standaard uit Instellingen; een bewust ingevulde 5,25 % wordt niet
  meer overschreven.
- **Stap 7 volledig in procentpunten**: veiligheid/EPB, decennale, ABR, onvoorzien, Wet Breyne en beide straight loans worden nu
  als 2,00 ingevoerd (bewaard als 0,02), net als projectcoördinatie/architect/ingenieur/marges. Bestaande waarden worden correct
  getoond (fractie × 100).

## Open punten uit de doorlichting (2026-10-07)

1. **Nacalc** — beslissing Niels (2026-10-07): **houden, in stap 6 (Activiteiten)**, niet als aparte view. Twee aparte dingen:
   - *Nacalc in het budget* = per lot/activiteit vergelijken met **voorgaande, uitgevoerde projecten**: ligt een lot te hoog of te laag
     t.o.v. wat vorige projecten werkelijk kostten, en dan meteen corrigeren via de bestaande kolom **Correctie** (`Correctiefactor`).
     Stap 6 krijgt dus naast "€ basis" een kolom "Referentie (nacalc)" + verschil %, gevuld uit referentieprojecten: afgewerkte projecten
     uit de app én **Excel-import** (projecten van vóór de app). Velden bestaan al: `NacalcPrijsPerEenheid`, `NacalcBronProjectId`;
     `ImportNacalcVanProject` is nog een stub. KPI "Alt. vs nacalc" op stap 9 blijft.
   - Het menu-item **Nacalculatie** in de projecthub (handoff menu-wireframes) = de nacalculatie van het lopende project zelf
     (contracten/facturen, punt 4) — staat los van het budget; komt ná de gl-v2-opmaak.
   Vereiste voor beide: **versiestatus definitief/vergrendelen** (punt 7) — akkoord Niels.
   **Design-handoff**: voor de budgetflow bestaat enkel de schil — punt 27 (stappenplan: 27b chips, 27g/27h tablet "Budget V1"), de
   menu-items en 35h (budget-PDF A3). De inhoud van de 9 stappen (tabellen, velden, stap 6 nacalc, stap 8 verkoop) is nog niet ontworpen.
2. ~~**Verkoop manueel per m²**~~ — gebouwd (deel 2 hierboven). Nog open: optie "mediaan markt als referentiecode bewaren";
   stap 8 in gl-v2-opmaak (punt 6); vraagprijs naar `Units` (aparte kolom) als de verkoopmodule ze nodig heeft.
3. **Dode invoer**: gevelelement "Afbraak" en de sanitairtotalen komen in geen vaste formule voor (geen `@opp_afbraak`).
4. **Budget ↔ contracten ↔ facturen**: het wizardbudget staat los van het oude `ProjectBudget` (prijs per activiteit) dat de
   contracten en de projectdetail-KPI gebruiken. Nodig voor 35h (Budget A3: besteld/gefactureerd/verbruik) en voor "% van budget".
5. ~~**Validatie per stap**~~ — gebouwd (deel 5): vier controles; uitbreiden per stap kan in `BerekenStapWaarschuwingenAsync`.
6. ~~**gl-v2-opmaak** van de 9 wizardpagina's~~ — gebouwd (deel 5). Nog open: de budget-PDF/Excel (35h).
7. ~~**Versiestatus**~~ — gebouwd (deel 5): Concept/Afgerond/Definitief, migratie 078.
8. Klein: GET-acties maken een parameterrij aan; AJAX-POSTs zonder antiforgery; "Per eenheid" deelt door alle rijen incl. garages;
   geen tests voor `BudgetBerekeningService`/`BudgetActivityService`.
9. DEVNOTES "Te doen" is deels verouderd: de voorstel-badges voor gevelmetselwerk en gipswerken bestaan al op stap 1.

## Voorstel: verkoopprijzen manueel per m² (gebouwd op 2026-10-07 — bewaard als ontwerpnotitie)

Uitgangspunt (Niels, 2026-10-07): naast het rekenkundige voorstel altijd een manuele instelling kunnen doen, per unit een prijs
per m²; prijsreferentietabellen blijven nuttig als de referenties manueel opgezocht worden.

**Model (stap 8, per verkooplijn)** — bestaande kolommen hergebruiken, twee erbij:
- `Bouwwaarde`/`Grondwaarde`/`Vraagprijs` blijven de vastgelegde bedragen.
- Nieuw `BouwPrijsPerM2` en `GrondPrijsPerM2` (nullable): de manueel ingestelde €/m². Leeg = afgeleid uit het bedrag.
- Nieuw `PrijsBron` (byte): 1 = voorstel (kost + marge), 2 = markt, 3 = referentiecode, 4 = manueel per m², 5 = manueel bedrag.
- `CodeBouw`/`CodeGrond` krijgen betekenis: ze verwijzen naar `BudgetPrijsReferentie.Code`; kiezen = €/m² van die code overnemen.
- Oppervlaktebasis: bouw op **gereduceerde oppervlakte** (zelfde weging als het voorstel; `OppTuin/OppTerras/OppDakterras` vervallen,
  die zitten al in de oppervlaktesrij), grond op `Grondopp`. `ExtraForfait` blijft een vast bedrag bovenop (bv. ruilvergoeding, aansluitingen);
  `IsRuil` blijft (unit tegen grondruil: grondwaarde 0, forfait als compensatie).

**Rekenregel per lijn**
```
bouwwaarde = BouwPrijsPerM2 × gereduceerde opp   (bron 3/4)   of overgenomen bedrag (bron 1/2/5)
grondwaarde = GrondPrijsPerM2 × grondopp          (bron 3/4)   of overgenomen bedrag
vraagprijs  = bouwwaarde + grondwaarde + extraForfait   (tenzij bron 5: manueel bedrag, dan €/m² afgeleid)
```
Wijzigt de gebruiker het bedrag, dan wordt €/m² herrekend en de bron "manueel bedrag"; wijzigt hij €/m², dan het omgekeerde.

**Scherm stap 8**
- Eén tabel per eenheid met: voorstel (kost + marge), markt, en de vastgelegde prijs, elk als €/m² én bedrag. De kolommen "Code"
  worden een keuzelijst uit de referentietabel (met €/m² ernaast); kiezen vult €/m² in.
- Knoppen "Voorstel overnemen", "Markt overnemen", "Code toepassen" per lijn en voor alle lijnen; bron-badge per lijn.
- Onderaan: totaal vraagprijs vs. minimale verkoopwaarde (marge in € en %), en een waarschuwing per lijn onder het minimum.

**Prijsreferentietabellen (Instellingen + per project)**
- `BudgetPrijsReferentie` bestaat al (type Bouw/Grond, code, €/m², omschrijving, optioneel projectId). Beheer ontbreekt: een pagina
  onder Instellingen › Budget (algemene codes) en een blokje op stap 8 (projectspecifieke codes), met datum en bron (bv. "verkoop
  Keerstraat lot 3, 06/2026") zodat een manueel opgezochte referentie herleidbaar blijft.
- Optioneel: "Uit markt" — de mediaan €/m² van de marktreferentie als referentiecode bewaren met datum.

**Doorzetten naar units** blijft: grond-/bouwwaarde per lijn → `Units.LandValue` / basis-`UnitConstructionValue`. Vraagprijs komt
daar nog niet; dat wordt een aparte kolom op Units als de verkoopmodule ze nodig heeft.

**Migratie** (additief): `BudgetVerkoopLijn` + `BouwPrijsPerM2 DECIMAL(18,2) NULL`, `GrondPrijsPerM2 DECIMAL(18,2) NULL`, `PrijsBron TINYINT NULL`;
`BudgetPrijsReferentie` + `Datum DATE NULL`, `Bron NVARCHAR(200) NULL`. `KopieerVersieInhoud` meenemen.

**Volgorde**: (1) model + rekenregel + unit-tests, (2) stap 8 herbouwen (liefst meteen in gl-v2), (3) referentiebeheer, (4) doorzetten uitbreiden.


## 13/10 - Grondkost + percentagebasis
- Parameters, kaart Grondkost: aankoopprijs grond, registratierechten % (standaard uit Bouwkost %, rij 'Registratierechten', anders 12 %; per budget aanpasbaar, kolom RegistratierechtenPerc, migratie 093), notariskosten (ereloon volgens schaaltarief + 21 % btw, ServiceCore/Budget/NotarisTarief.cs), totale grondkost live. Posten 'Registratierechten' en 'Notariskosten (ereloon)' staan in de groep Grond en tellen mee in GrondKost van het verkoopvoorstel. Aktekosten/uitgaven zitten er niet in.
- Percentagebasis: EPB (166), veiligheidscoordinatie (165) en ventilatieverslaggeving (278) staan wel in TotaalBouw/totaalbudget maar niet meer in de basis waarop erelonen, verzekeringen, Wet Breyne, straight loan gebouw en onvoorzien rekenen (BudgetAlgemeneActiviteit.BuitenPercentageBasis).
- Migratie 093 uitvoeren vóór gebruik.
- Publiciteit is nu een percentage van de totale verkoopprijs (PubliciteitPerc, standaard 0,04 % uit Bouwkost % rij 'publiciteit', migratie 093). Het forfait telt niet meer mee. Omdat de verkoopprijs zelf van de kost afhangt, wordt het bedrag algebraïsch opgelost: P = f*V0 / (1 - f*(1+doelmarge)).
- Migratie 093 zet ook de standaardrijen 'registratierechten' (12 %) en 'publiciteit' (0,04 %) in Bouwkost % (groep 'Overige kosten').
- Notariskosten herwerkt volgens het Tariefbesluit (consolidatie 9/2/2026): verkoop uit de hand = schaal J (vast 285, -86 tot 20.000; reeksen 2,5/2,5/1,75/1,71/1,14/0,57/0,20/0,20 %) + 21 % btw, per akte. Daarbovenop aktekosten per akte (standaard 1.500, per budget aanpasbaar).
- Aankoop in delen: Parameters › Grondkost heeft een lijst Aankopen (JSON in BudgetParams.GrondAankopenJson), elk deel = één akte met eigen notariskosten; AankoopprijsGrond blijft het totaal. Migratie 094 vóór gebruik (093 ook).
- Aktekosten per akte (standaard 1.500, excl. btw) worden met 21 % btw als kost gerekend, naast het ereloon (dat zelf ook incl. btw is).
- Getalvelden van de budgetwizard (_BwNum, 2 decimalen, niet alleen-lezen) gebruiken nu het projectbrede .Currencymask (AutoNumeric, currency.js) i.p.v. een eigen opmaak. currency.js zet bij FormData-verzending (autosave) het kale getal; programmatische waarden gaan via GlV2Budget.zet().
- Grondkost-kaart herbouwd naar de mock: tabel Aankopen (omschrijving, datum akte, prijs, dupliceren, verwijderen, nieuwe aankoop + Toevoegen), velden registratierechten/aktekosten/notaris-ereloon, rechts het berekeningspaneel met totale grondkost. Datum per aankoop zit in GrondAankopenJson.
- Parameters › kaart Verkoopkosten (migratie 095): makelaarscommissie % (standaard uit Bouwkost % 'makelaarscommissie', 3 %), btw op commissie 21 % vast, overige verkoopkosten (forfait per project). Resultaat › kaart "Verkoop t.o.v. kostprijs" (ServiceCore/Budget/VerkoopResultaat.cs): verkoopwaarde (vastgelegde vraagprijzen) - verkoopkosten (btw op commissie telt enkel op het grondaandeel mee) - kostprijs = resultaat, met marges, resultaat per eenheid en break-even. Nog niet in PDF/Excel/versievergelijking en niet in het verkoopvoorstel van stap 8.
- Marges zijn netto: Min. prijs per eenheid (stap 8) bevat de verkoopkosten. Waarde = (kost x (1 + marge) + aandeel overige) / (1 - commissie x btw-factor), grond met 21 % btw op de commissie, bouw zonder (VerkoopVoorstelService.Waarden). Publiciteit rekent mee op die verkoopprijs.
- Resultaat/Excel/PDF gebruiken dezelfde herberekende verkooplijnen als stap 8 (HerberekendeLijnenAsync); stap 8 bewaart zelf een lijn die bij het openen afwijkt van de database.
- OPEN VRAAG: grond in twee stukken per woning (woongebied + agrarisch) met verschillende waarde; ontwerp voorgesteld aan Niels, nog niet gebouwd.
- Grond in twee zones per woning (migratie 096): stap 2 heeft een kolom "Grond 2 m²" (BudgetOppervlaktes.GrondoppAgrarisch); Parameters › Verkoop heeft "Waarde 2e grondzone" (standaard 20 %) enkel voor de verdeling van de grondkost (gewicht = zone 1 + zone 2 × %); stap 8 heeft per woning een blok "Grond 2" met eigen €/m² (volgt anders zone 1 × %); Grondwaarde = zone 1, GrondwaardeAgrarisch = zone 2, vraagprijs = som. Nog niet in Excel/PDF en niet in de formule opp_grondopp.
- Budgetten-overzicht (39a) bijgewerkt: definitief budget bovenaan op volle breedte (slotje, naam · versie · totaal, marge % en €, vastzet-regel, drie kengetallen, knoppen rechts); budgetkaarten met kolommen Versie/Naam/Datum/Status/Totaal/Marge; in Totaal staat "stap x van 9" zolang niet alle stappen doorlopen zijn; marge op kostprijs (resultaat / totale kostprijs) bij definitief, afgerond en concept dat stap 9 bereikte (geheugen-cache per versie en wijzigingstijdstip); lege gestreepte kaart "Nieuw budget"; "Maak actief" bestond al.
- BUG opgelost 14/10/2026: de nieuwe percentagekolommen van BudgetParams (092-096) hadden in de DbContext geen kolomtype, waardoor EF ze als decimal(18,2) wegschreef: 2,5 % werd 3 %, 0,04 % werd 0 (en dus telkens de standaard). Nu decimal(9,6) in cpmRunningContext. Eerder opgeslagen afgeronde waarden moeten eenmalig opnieuw ingevuld worden.
- 39a/39l/39m/39n (14/10/2026): topbar Vergelijken + Nieuw budget, modal Nieuw budget (startpunt, overnemen per stap, eenheden, index, meteen actief; `KopieerVersieInhoud` met stappen, eenheden uit het project), modal Vergelijken en de vergelijkingspagina (`BudgetVergelijkService`, Excel-export), help geschreven; zie DESIGN.md 'Update 14/10/2026'. Niet browser-getest, enkel statisch gerenderd met voorbeelddata.
- 44d/44e/44f (14/10/2026): PDF intern/bank in stap 9 opent nu de volle pagina `BudgetPdfOpmaken` (view `BudgetPdfOpmakenV2`, JS `gl-v2-budget-pdf.js`, CSS hergebruikt `.gl-v2-vsprep` uit gl-v2-projecten-verslagen.css): hoofdstukken (tijdgestuurd), PDF opvragen via `DownloadBudgetPdfV2` (headers X-Pagina-Aantal / X-Bestandsnaam), opent in nieuw tabblad, klaar-pagina met Download PDF + Terug naar stap 9. Bank: "Financiering, onvoorzien, publiciteit" doorgestreept. NIET gebouwd: bewaren in Documenten › Budgetten. Niet browser-getest.
- 14/10/2026: Vergelijken-modal: native selects (Basis, Vergelijken op) vervangen door GlV2 `_SelectList` (`setItems` voor Basis). Versie/budget verwijderen op BudgetIndexV2: `ProjectenController.BudgetVersieDelete` (rechtencheck `HasDelete(ProjectsDetail)`, nooit een definitieve versie, laatste versie neemt het budget mee, actief budget/huidige versie gaan over op een ander), knop + bevestigingsmodal; kindrijen via DB-cascade. Build enkel gecompileerd (kopie mislukt omdat de app draait), niet browser-getest.
- 14/10/2026 (later): verwijdermodal = danger-bevestiging met body; PDF opmaken bewaart in Documenten › Budgetten (`BudgetPdfMaken` POST, migratie 099 `DocumentMapBudgetten`). Gecompileerd, niet getest.
- 14/10/2026 (later): Excel-export met verkoopkosten/resultaat en 2e grondzone; formule `opp_grondopp` = zone 1 + zone 2; Nieuw-budgetmodal: keuzelijsten openden niet (label rond radio) — opgelost; help-schermafbeeldingen voor PDF, vergelijken en verwijderen geplaatst. Opname Nieuw-budgetmodal geplaatst (keuzelijsten openen nu correct).

---
## OVERDRACHT 14/10/2026 — stand van zaken om op een andere pc verder te werken
**Let op: er is NIETS gecommit** (branch `layout-experiment`, ±176 gewijzigde/nieuwe bestanden). Commit en push eerst op deze pc, anders staat het werk niet op de andere pc.
Ook `bin/obj` van de lopende app blokkeert een build zolang de app draait: stop de app voor een volledige build.

### Migraties (hand-run op de live DB, idempotent, volgorde)
078, 080, 089, 090, 091, 092, 093, 094, 095, 096 zijn op de live DB uitgevoerd (bevestigd door Niels). **099_DocumentMapBudgetten.sql** (map "Budgetten" in Documenten): door Niels uitgevoerd (punt 1 en 2 "gedaan"), controleer dit op de andere DB indien die verschilt. 097/098/100 horen bij andere features (Punten/Werfverslagen).

### Wat deze sessie is gebouwd (allemaal gecompileerd; browsergetest = alleen via Playwright-opnames)
- **Stap 9 PDF intern/bank → volle opmaakpagina (design 44d/e/f):** `BudgetPdfOpmaken` (GET, view `Views/Projecten/Budget/BudgetPdfOpmakenV2.cshtml`, JS `wwwroot/js/gl-v2-budget-pdf.js`, CSS `.gl-v2-vsprep` uit `gl-v2-projecten-verslagen.css`), `BudgetPdfMaken` (POST, antiforgery via data-token) rendert, bewaart in Documenten › Budgetten (`BewaarBudgetPdfAsync`, geen koppelingen = nooit publiek) en geeft headers `X-Pagina-Aantal`, `X-Bestandsnaam`, `X-Bewaard`. Oude GET `DownloadBudgetPdfV2` blijft bestaan.
- **Budget/versie verwijderen** (overzicht): `BudgetVersieDelete` (naam bevat "Delete" → permissiefilter; ook expliciete `HasDelete(ProjectsDetail)`), nooit definitief, laatste versie neemt budget mee, actief/huidig gaat over. Rode type-1 bevestigingsmodal met tekst.
- **Keuzelijsten:** Vergelijken-modal (Basis, Vergelijken op) nu `GlV2/_SelectList`; Nieuw-budgetmodal: lijsten/vinkjes buiten het radio-`<label>` gezet (lijst sloot meteen).
- **Kopie:** aantal veluxen niet mee zonder "Dak & afbraak"; activiteitenlijnen zonder voorstel (hoeveelheid 0) zonder handmatige prijs worden op 0 gezet (`BudgetActivityService`, oorzaak: oude prijs bleef staan).
- **Excel-export:** verkoopwaarde, verkoopkosten, netto opbrengst, resultaat + marge op kostprijs (zelfde cijfers als stap 9, via `VerkoopResultaat.Bereken`), tabblad Oppervlaktes met grond zone 1/2/totaal. Formule `opp_grondopp` = zone 1 + zone 2.
- **Docs:** DESIGN.md (blokken "Update 14/10/2026 …"), help `budget-nieuw.md`, `budget-stap-resultaat.md`, `budgetten-vergelijken.md` met schermafbeeldingen in `wwwroot/img/help/budget/…`, HELPCENTRUM_VOORTGANG.md.

### Nog te doen
1. **Excel-export testen** door Niels (stap 9 › Excel, budget met verkoop): kostenoverzicht en tabblad Oppervlaktes vergelijken met stap 9.
2. **Kopie testen** (Niels): nieuw budget als kopie zonder Dak & afbraak → dakactiviteiten op 0.
3. Browsertest van de rest van de nieuwe functies (Vergelijken/Nieuw budget/PDF-flow/verwijderen met en zonder verwijderrecht); alleen via Playwright-opnames gezien, nooit handmatig doorlopen.
4. **Koppeling budget ↔ contracten/facturen/nacalculatie** (grootste openstaande brok): tegels Gecontracteerd/Gefactureerd/Verwacht verschil op de budgetindex zijn placeholders ("nog niet gekoppeld"). Eerst beslissen op welk niveau vergeleken wordt (per activiteit/lot); bijkomende nacalc-verbeteringen.
5. Vastloper op de pagina Budgetactiviteiten (nooit gereproduceerd).
6. Screenshot-recept: zie DESIGN.md "Schermafbeeldingen maken — herbruikbaar Playwright-recept"; script staat in de scratchpad van de sessie (niet in de repo): niet Escape drukken in een open modal, `waitUntil: 'load'` i.p.v. `networkidle`, radio's klikken via hun label.
- 16/10/2026: Excel-export via dezelfde volle opmaakpagina als de PDF (`BudgetExcelOpmaken`/`BudgetExcelMaken`, bewaard in Documenten; fouten worden zichtbaar met technische melding). Niets gecompileerd tegen de draaiende app; Excel nog te testen — als de oorzaak van 'doet niets' een serverfout was, staat die nu op de foutpagina.
- 16/10/2026: koppeling budget ↔ contracten/facturen (stap 3): tegels op de budgetindex gevuld (enkel activiteiten; gecontracteerd = contract + bijbestellingen, gefactureerd = incl. meerwerk excl. meerwerk klant; verwacht verschil = begroot − max(contract, factuur) per activiteit, anders budget). Gecompileerd, niet getest. Nog te doen binnen dit thema: koppeling per activiteit/lot op stap 9 of een eigen overzicht, grond en andere posten (aparte bronnen).

---
## IDEE / VOLGENDE GROTE FEATURE (genoteerd 16/10/2026): Projectresultaat — nacalculatie voor het volledige project
**Wens van Niels:** de Nacalculatie is nu enkel voor de bouwkost (activiteiten). Op termijn moet ze het volledige project dekken, met de nadruk op de bouwkost, zodat hij op het einde van een project het totale echte rendement kent.
**Nog niet gestart, geen code.** Dit is een voorstel (bijgewerkt na overleg) en wacht op antwoorden en op de keuze om het op te pakken.

**Stand van zaken (uit de code, niet getest):** de pagina Nacalculatie (gl-v2, `RecalculationV2`) neemt automatisch het definitieve budget als "Begroot" per activiteit (`GetBegrootRegelsV2`), zonder definitief budget valt ze terug op het oude per-activiteit-budget; de oude pagina gebruikt het nieuwe budget niet. De tegels op de budgetindex (Gecontracteerd/Gefactureerd/Verwacht verschil) zijn al gevuld, enkel voor activiteiten.
Er komt bewust géén vergelijking per activiteit/lot op stap 9: een budget wordt gemaakt vóór de nacalculatie (beslissing Niels 16/10/2026).

**Wat er al is:** budgetgroepen volgen dezelfde activiteitenstructuur als contracten en facturen (ook erelonen, verzekeringen, aansluitingen); facturen zonder contract kunnen aan een activiteit hangen (`IncommingInvoiceDetail.ActId`); opbrengst: verkoopfacturen aan klanten, betalingsschijven, meerwerken voor klanten.
**Wat ontbreekt:** werkelijke grondkost (aankoop, akte/notaris, registratierechten), financiering (rente/bankkosten), verkoopkosten (werkelijke makelaarscommissie), onvoorzien en publiciteit; alles staat nu alleen als budgetcijfer.

**Voorstel in drie fases (pagina "Projectresultaat"):**
1. Wat al kan: per budgetgroep budget tegenover werkelijke kost (bouw, erelonen, verzekeringen, aansluitingen) + opbrengst uit de verkoopfacturen → een eerste rendement.
2. Handmatig invoeren van werkelijke grondkost, financiering en verkoopkosten, zolang er geen bron is.
3. Die posten automatisch koppelen uit hun eigen bronnen (bankfacturen, akten …).
**Rendement:** zoals stap 9, resultaat gedeeld door totale kostprijs, zodat budget en werkelijkheid vergelijkbaar zijn.

**Open vragen aan Niels vóór de bouw:**
- Telt de opbrengst per gefactureerd of per betaald bedrag?
- Rendement ook per eenheid, of enkel voor het hele project?
- Grondkost invullen per project, of per aankoop zoals in de grondkaart van het budget?
