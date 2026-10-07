# Budgetwizard — voortgangsstatus

Doorlopend statusdocument voor de budgetwizard (Projecten › Budgetten, 9 stappen). Hoe het gebouwd is staat in
DEVNOTES.md §3, §5 en §7; dit bestand houdt bij wat af is, wat open staat en welke beslissingen genomen zijn.

**Laatste update:** 2026-10-07 — doorlichting van de volledige flow + eerste reeks fixes (versiebeheer, consistente
kostprijs, stap 7 in procentpunten) én "verkoop manueel per m²" gebouwd (stap 8 herbouwd, referentiebeheer).
Niets gecommit. **Migratie `_migrations/075_BudgetVerkoopPrijsPerM2.sql` uitvoeren vóór de app start** (naast 072/073).
Niets in de browser getest: stap 8 en Instellingen › Prijsreferenties eerst doorlopen.

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
5. **Validatie per stap** (design-handoff 27b/27g: chips met fouten/aandachtspunten) — enkel het poorten-icoon bestaat.
6. **gl-v2-opmaak** van de 9 wizardpagina's en de budget-PDF/Excel (35h).
7. **Versiestatus** blijft altijd "Concept"; geen definitief/vergrendelen.
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
