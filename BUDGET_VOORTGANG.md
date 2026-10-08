# Budgetwizard — voortgangsstatus

Doorlopend statusdocument voor de budgetwizard (Projecten › Budgetten, 9 stappen). Hoe het gebouwd is staat in
DEVNOTES.md §3, §5 en §7; dit bestand houdt bij wat af is, wat open staat en welke beslissingen genomen zijn.

**Laatste update:** 2026-10-08 — de volledige budgetflow (overzicht + 9 stappen) staat in gl-v2 volgens design-handoff
punt 39, met versiestatus Concept/Afgerond/Definitief (deel 5). Niets gecommit, niets browser-getest.
**Migraties 075 → 078 uitvoeren vóór de app start** (`_migrations/078_BudgetVersieStatus.sql` is nieuw).

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
