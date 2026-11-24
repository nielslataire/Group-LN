# Bouwstaten (en activiteiten als discipline): eerste voorstel (24/11/2026)

Uitwerking van stap A uit `PLANNING_ONTWERP_HERZIEN.md`. Dit is een **voorstel om te corrigeren**, nog niets gebouwd. Het komt uit de codes die vandaag in de testsjablonen (project 79) staan: appartementsgebouw, woning, parking en verharding.

## 1. Spelregels van de lijst
- Een **bouwstaat** is een toestand van een eenheid die je kunt bereiken ("ruwbouw 1ste verdieping klaar"). Een taak of fase **levert** een bouwstaat. Een schijf of trajectmijlpaal **verwijst** ernaar.
- Eén bedrijfsbrede lijst met vaste codes. Een project kan er eigen bouwstaten bij maken (bv. `PROJ_...`).
- Bouwlagen zijn geen aparte bouwstaten: de bouwstaten in de groep **Ruwbouw bovenbouw** bestaan per bouwlaag (gelijkvloers, 1ste verdieping, ...). Zo verdwijnen `laag:boven` en het volgnummer `kelder ? i : i-1`: een schijf verwijst gewoon naar "Opstort 1ste verdieping".
- **Bereik:** `G` = geldt voor het hele gebouw (alle appartementen tegelijk), `E` = per eenheid. Bij een woning is alles per eenheid, want de planning hoort bij één woning.
- **Schijf?** zegt of dit in de testsjablonen vandaag al een schijf is. Dat is geen vast gegeven; het sjabloon beslist.

## 2. Voorstel standaardlijst

| Volgorde | Code | Bouwstaat | Groep | Bereik gebouw | Vandaag in sjabloon (code) | Schijf? |
|---|---|---|---|---|---|---|
| 10 | `VOORBEREIDING_KLAAR` | Voorbereidende werken klaar | Voorbereiding | G | taak `AFBRAAK` | ja |
| 20 | `WERF_UITGEZET` | Werf ingericht en uitgezet | Voorbereiding | G | `WERF_UITZETTEN`, `UITZETTEN` | ja (woning) |
| 30 | `KELDER_UITGEGRAVEN` | Kelder uitgegraven | Ruwbouw onderbouw | G | `UITGRAVEN_KELDER` | nee |
| 31 | `FUNDERING_KLAAR` | Fundering klaar | Ruwbouw onderbouw | G | `FUNDERINGEN` | nee |
| 32 | `VLOERPLAAT_KELDER` | Vloerplaat kelder gegoten | Ruwbouw onderbouw | G | `VLOERPLAAT_KELDER` | ja |
| 33 | `WANDEN_KELDER` | Kelderwanden klaar | Ruwbouw onderbouw | G | `WANDEN_KELDER` | nee |
| 40 | `VLOERPLAAT_GLVL` | Vloerplaat gelijkvloers gegoten (zonder kelder) | Ruwbouw bovenbouw | G | `VLOERPLAAT_GLVL` | ja (zonder kelder) |
| 41 | `PREDALLEN` | Predallen geplaatst (per bouwlaag) | Ruwbouw bovenbouw | G | `PREDALLEN` | nee |
| 42 | `OPSTORT` | Opstort gegoten (per bouwlaag) | Ruwbouw bovenbouw | G | `OPSTORT` | ja, per bouwlaag |
| 43 | `METSELWERK_RUWBOUW` | Metselwerk ruwbouw klaar (per bouwlaag) | Ruwbouw bovenbouw | G | `METSELWERK_RUWBOUW` | nee |
| 44 | `TERRAS_LUIFEL` | Terrassen en luifels klaar (per bouwlaag) | Ruwbouw bovenbouw | G | `TERRAS_LUIFEL` | nee |
| 45 | `PREDALLEN_DAK` / `OPSTORT_DAK` | Dakplaat (plat dak) | Ruwbouw bovenbouw | G | `PREDALLEN_DAK`, `OPSTORT_DAK` | nee |
| 46 | `GEVELMETSELWERK` | Gevelmetselwerk klaar | Ruwbouw bovenbouw | G | `GEVELMETSELWERK` | nee |
| 50 | `DAK_PLAT_DICHT` | Plat dak waterdicht | Dak | G | `DAKBEDEKKING_PLAT`, `DAKBEDEKKING` | ja |
| 51 | `DAK_HELLEND_DICHT` | Hellend dak gedekt | Dak | G | `DAKSTRUCTUUR`, `DAKBEDEKKING_HELLEND` | ja |
| 52 | `DAKOPSTANDEN` | Dak- en liftopstanden klaar | Dak | G | `DAKOPSTANDEN` | nee |
| 60 | `BUITENSCHRIJNWERK` | Buitenschrijnwerk en glas geplaatst | Gesloten gebouw | G | `RAMEN` | ja |
| 70 | `TECHNIEKEN_RUW` | Leidingen technieken geplaatst | Technieken | G | `LEIDINGEN` | ja (vast 5 %) |
| 80 | `PLEISTER` | Pleisterwerk klaar | Afwerking | **E** | `PLEISTER` | ja |
| 81 | `ISOLATIE_VLOER` | Vloerisolatie geplaatst | Afwerking | **E** | `ISOLATIE` | nee |
| 82 | `CHAPE` | Chape geplaatst | Afwerking | **E** | `CHAPE` | ja |
| 83 | `TEGELWERK` | Tegelwerk klaar | Afwerking | **E** | `TEGELWERK` | nee |
| 84 | `VLOEREN_PARKET` | Parket/vloeren klaar | Afwerking | **E** | `PARKET` | nee |
| 85 | `KEUKEN` | Keuken geplaatst | Afwerking | **E** | `KEUKEN` | ja (vast 5 %) |
| 86 | `BINNENSCHRIJNWERK` | Binnenschrijnwerk klaar | Afwerking | **E** | `BINNENSCHRIJNWERK`, `PLINTEN` | nee |
| 90 | `OPKUIS` | Opgekuist | Oplevering | **E** | `OPKUIS` | nee |
| 91 | `VOORLOPIGE_OPLEVERING` | Voorlopige oplevering (sleutels) | Oplevering | **E** | `VOORLOPIGE_OPLEVERING` | ja (vast 5 %) |
| 92 | `DEFINITIEVE_OPLEVERING` | Definitieve oplevering (einde waarborg) | Oplevering | E | nieuw | nee |

Parking en verharding (eigen onderdeel in stap C) gebruiken dezelfde lijst, met extra bouwstaten voor `AANVULLEN_BOUWPUT`, `FUNDERING_VERHARDING`, `VERHARDING`, `CARPORTS`, `PARKEERPLAATSEN`. Die staan nu als schijven zonder planningcode; ze krijgen een bouwstaat zodra het onderdeel een planning heeft.

## 3. Disciplines = jouw activiteitgroepen en activiteiten (aangepast 24/11/2026)
Na jouw opmerking: een aparte tabel Discipline dubbelt wat er al is. Vandaag bestaat:
- `ActivityGroup` (lot, bv. 5) met daaronder `Activity` (lotnummer "5.11"). Budgetten en calculaties werken ermee.
- `ContractActivity`: een contract bevat activiteiten met prijs. Een aannemer heeft dus al "zijn" activiteiten per project.
- `CompanyInfo.Activity`: de activiteiten waarin een bedrijf werkt.
- `PlanningSectie.ActivityGroupId`: een fase kan al aan een lot hangen (voor de budgetvoortgang).

**Voorstel:** er komt **geen** Discipline-tabel. De discipline van een taak is een **activiteit** (of, grover, een activiteitgroep).
- Sjabloontaak en planningtaak krijgen een optionele `ActivityId`. De groep volgt er automatisch uit (en bepaalt het lot).
- **Aannemer koppelen:** voor elke taak met een activiteit zoekt de planning het contract in het project dat die activiteit bevat (`ContractActivity`) en zet dat bedrijf als uitvoerder. Zijn er twee contracten voor dezelfde activiteit (bv. twee woningen, twee aannemers), dan kies je per taak of per eenheid.
- Heeft een taak enkel een groep (te grof voor een activiteit), dan geldt hetzelfde op groepsniveau.
- Geen nieuwe kolom op `Contract`: het contract heeft zijn activiteiten al. Geen migratie voor disciplines, enkel `ActivityId` op taken.
- Splitsing "leidingen technieken" wordt dus twee taken met elk een eigen activiteit (elektriciteit, sanitair/verwarming), zoals jij al deed.

**Lijst ontvangen (24/11/2026):** groepen 0 Algemeen, 1 Onderbouw, 2 Bovenbouw, 3 Dakwerken, 4 Gevelsluiting, 5 Binnenafwerking, 6 Technieken Fluida, 7 Technieken Elektro, 8 Schilderwerken, 9 Omgevingswerken, 99 Bureel algemeen. De lotnummers per activiteit zijn leeg (migratie 118), dat doet er hier niet toe.

### Voorstel: activiteit per taak (uit de testsjablonen)

| Taak (code) | Activiteit | Groep | Opmerking |
|---|---|---|---|
| `AFBRAAK` | Afbraakwerken | 0 Algemeen | 5 contracten |
| `WERF_UITZETTEN`, `UITZETTEN` | Werfinrichting | 0 Algemeen | nu in 0 contracten |
| nieuwe taak `UITZETTEN_LANDMETER` (vóór werf inrichten) | Landmeter | 0 Algemeen | beslist 24/11: eigen taak, 4 contracten |
| `UITGRAVEN_KELDER` | Grondwerken | 1 Onderbouw | |
| `FUNDERINGEN` | Ruwbouw | 2 Bovenbouw | beslist 24/11: funderingen horen bij Ruwbouw |
| `VLOERPLAAT_KELDER`, `WANDEN_KELDER`, `AFDEK_KELDER` | Ondergrondse kelder | 1 Onderbouw | |
| `VLOERPLAAT_GLVL`, `OPSTORT`, `METSELWERK_RUWBOUW`, `TERRAS_LUIFEL`, `OPSTORT_DAK` | Ruwbouw | 2 Bovenbouw | 7 contracten; alles van beton en metselwerk |
| `PREDALLEN`, `PREDALLEN_DAK` | Ruwbouw | 2 Bovenbouw | beslist 24/11 |
| `GEVELMETSELWERK` | Ruwbouw | 2 Bovenbouw | beslist 24/11 |
| `DAKOPSTANDEN`, `DAKBEDEKKING_PLAT`, `DAKBEDEKKING` | Dakbedekking - Plat dak - EPDM | 3 Dakwerken | PVC, Roofing en TPO zijn varianten |
| `DAKSTRUCTUUR` | Daktimmerwerken | 3 Dakwerken | |
| `DAKBEDEKKING_HELLEND` | Dakbedekking - Hellend Dak | 3 Dakwerken | |
| `RAMEN` | Buitenschrijnwerk - Aluminium (of Hout, PVC) | 4 Gevelsluiting | beglazing aparte activiteit |
| `LEIDINGEN` (elektriciteit) | Elektrische installatie | 7 Technieken Elektro | |
| `LEIDINGEN` (sanitair, verwarming) | Sanitaire installatie, Verwarmings installatie | 6 Technieken Fluida | twee taken |
| `PLEISTER` | Binnenbepleistering (naam wordt rechtgezet, zie hieronder) | 5 Binnenafwerking | |
| `ISOLATIE` | Vloer - Isolatie - PUR | 5 Binnenafwerking | |
| `CHAPE` | Vloer - Dekvloer | 5 Binnenafwerking | |
| `TEGELWERK` | Vloer - Keramisch | 5 Binnenafwerking | |
| `PARKET` | Vloer - Hout | 5 Binnenafwerking | |
| `KEUKEN` | Vast Meubilair - Keukens | 5 Binnenafwerking | |
| `BINNENSCHRIJNWERK`, `PLINTEN` | Binnendeuren | 5 Binnenafwerking | plinten mogelijk Houten binnenafwerking |
| `OPKUIS` | Schoonmaak | 5 Binnenafwerking | |
| `VOORLOPIGE_OPLEVERING` | geen | | mijlpaal, geen uitvoerder |
| parking: verharding, carports, parkeerplaatsen | Buitenverhardingen, Carports, Wegenis - Infrastructuur | 9 Omgevingswerken | |

Groepen 0 en 99 (Algemeen, Bureel) bevatten studie, kosten en diensten; dat zijn geen uitvoeringstaken, op Afbraak, Werfinrichting en Schoonmaak na.

**Beslissingen van Niels (24/11/2026):** gevelmetselwerk, predallen en funderingen zijn Ruwbouw; het uitzetten door de Landmeter is een eigen taak; "gesloten gebouw/winddicht" is **geen** bouwstaat voor nu (mag later, als inactieve bouwstaat bij te maken); de typfout "Binnnenbepleistering" mag rechtgezet worden (hand-run script `UPDATE Activity SET Omschrijving = N'Binnenbepleistering' WHERE Omschrijving = N'Binnnenbepleistering'`, ook zichtbaar in contracten en budgetten).

**Gevolgen van de lijst:**
- Activiteiten zijn grover dan de taken: de activiteit **Ruwbouw** dekt zowel vloerplaat, opstort, metselwerk als gevelmetselwerk. Dat is geen probleem (veel taken, één activiteit en dus één aannemer), maar je kunt daardoor niet zien dat het gevelmetselwerk door een andere aannemer wordt gedaan tenzij je een activiteit toevoegt of per taak een uitvoerder overschrijft. Dat laatste kan al.
- Per taak is één activiteit voldoende. De groep (lot) volgt daaruit en valt samen met het lot van de fase in de budgetvoortgang.

**Bouwstaat en activiteit zijn twee dingen:** de bouwstaat zegt *wat bereikt is* (schijf, traject, poort); de activiteit zegt *wie het werk doet en waarmee het budget meetelt*. Een bouwstaat kan door taken van meerdere activiteiten geleverd worden; dan geldt de regel van jou: **als beide voltooid zijn (of gestart, in te stellen per schijf) wordt de schijf betaalbaar**.

## 4. Wat dit in de bestaande codes verandert
- `PlanningTaak.Code`, `PlanningSectie.Code`, `PlanningSjabloonTaak.Code` en `SchijfSjabloonSchijf.PlanningCode` verwijzen straks naar een bouwstaat, niet meer naar vrije tekst. De taakcode blijft daarnaast bestaan als sleutel voor voorgangers binnen het sjabloon.
- **Mapping bij de migratie:** de kolom "Vandaag in sjabloon" hierboven. Codes zonder match blijven staan als projecteigen bouwstaat en komen op een lijst "nog te benoemen".
- Samengevoegde codes: `WERF_UITZETTEN` en `UITZETTEN`, `DAKBEDEKKING` en `DAKBEDEKKING_PLAT`, `VLOERPLAAT_V1` en `OPSTORT` per bouwlaag, `DAKPLAAT` en `OPSTORT_DAK`. Dit zijn de plekken waar de migratie een keuze moet maken; ik zou ze eerst met jou nalopen.
- **Let op:** het woningsjabloon gebruikt eigen codes (`VLOERPLAAT_GLVL`, `VLOERPLAAT_V1`, `DAKPLAAT` voor "afdek 1ste verdieping"). Die worden bouwstaten per bouwlaag (`OPSTORT` of de vloerplaat, en de dakplaat), zodat gebouw en woning dezelfde lijst delen.

## 5. Antwoorden van Niels (24/11/2026) en wat nog open staat
1. **Leidingen:** de taak is al gesplitst in elektriciteit en sanitair/verwarming. **Schijf betaalbaar** zodra beide taken voltooid zijn, of gestart (instelbaar per schijf): de bouwstaat wordt dus geleverd door **meerdere taken**, met een regel "alle voltooid" of "alle gestart". Dit moet in het model: een bouwstaat is bereikt als alle leverende taken voldoen.
2. **Bereik:** in de planning per gebouw, voor de facturatie per eenheid. Dus: een bouwstaat uit de gebouwplanning geldt voor alle eenheden, en de schijf wordt per eenheid factureerbaar (bevestigen kan voor alle eenheden tegelijk).
3. **Afwerking:** dezelfde percentages voor alle appartementen, akkoord.
4. **Disciplines:** zie §3, ze vallen samen met de activiteitgroepen en activiteiten.

**Nog open:**
- De lijst activiteitgroepen en activiteiten (zie §3).
- Is "gesloten gebouw/winddicht" een bouwstaat of enkel een tussentitel? En klopt de volgorde en groepering van §2?
- Gevelmetselwerk zonder voorganger (start dag 1) blijft zoals beslist tot je anders zegt.
- Rest van de open vragen van §7 in het herziene ontwerp (foto of nota bij bevestigen, ongedaan maken, rollen, poorten en traject): horen bij stap B en D.

## 6. Als je akkoord bent: wat ik dan bouw (stap A)
1. Migratie `158_Bouwstaten.sql`: tabel `Bouwstaat` (code, naam, groep, volgorde, bereik, actief, `ProjectId` null = bedrijfsbreed); kolommen `BouwstaatId` op taak, fase, sjabloontaak en schijf; `ActivityId` op sjabloontaak en planningtaak; regel "alle leverende taken voltooid of gestart" op de schijf. Alles nullable en additief; mapping uit §4 als hand-run script.
2. Instellingen › Bouwstaten (lijst en editor, in dezelfde stijl als de sjablonen), recht `Settings.Planningssjablonen`.
3. Sjablooneditor en schijvensjabloon-editor kiezen bouwstaat en activiteit in een keuzelijst; vrije code blijft zichtbaar als "nog te benoemen".
4. `PlanningSchijfAutoKoppeling` en `PlanningTrajectKoppeling.KoppelOpCode` lezen de bouwstaat; `laag:boven` en het volgnummer worden vervangen zodra de bouwstaten per bouwlaag werken. Aannemer volgt uit het contract via de activiteit (stap F, nu al voorbereid).
5. Tests naast de bestaande 153.
