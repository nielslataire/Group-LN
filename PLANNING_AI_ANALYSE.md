# Analyse: een AI die een volledig projectvoorstel maakt

> **Bijgewerkt op 22/11/2026:** zie hoofdstuk 14 voor de besluiten van Niels (aannemers vervallen, relatieve datums, voorstel per projectfase, privacy). Waar hoofdstuk 14 afwijkt van de tekst hierboven, geldt hoofdstuk 14.

Datum: 22/11/2026. Opgesteld na de vaststelling dat het huidige AI-voorstel bij het projectsjabloon "niets zinnigs doet".
Alles hieronder komt uit het lezen van de code en het logboek (`PLANNING_VOORTGANG.md`, `docs/archive/PLANNING_SJABLOON_OVERDRACHT.md`). Er is niets gebouwd of gewijzigd. Wat ik niet kon nagaan (de inhoud van de live database, of de AI-aanroep echt werkt) staat in hoofdstuk 11.

## 1. Conclusie in het kort

1. **Het huidige AI-voorstel is geen voorstel maar een invulhulp.** Het leest een beschrijving en vult vier velden in (verdiepingen, kelder, dak, één planning of per woning) in een formulier dat je ook in vijf seconden zelf invult. Het kent je sjablonen niet, ziet je project niet, en raakt traject, schijven, deadlines en aannemers niet aan.
2. **Je bouwstenen zijn goed genoeg om een echt voorstel te dragen.** De sjablonen hebben al codes als lijm, voorwaarden (kelder, dak), herhaling per verdieping, estafette, schijven met gewichten, mijlpalen met ankers en triggers, en een rekenmotor die deadlines controleert. Wat ontbreekt is een laag erboven die dit samenstelt.
3. **Het juiste ontwerp is: AI stelt samen, code controleert, jij keurt goed.** De AI schrijft een *concept* (een gestructureerd document), een deterministische controle rekent het door met de echte planningsmotor, jij ziet per onderdeel wat er komt en wat de aannames zijn, en pas dan gaat het via de bestaande services de database in.
4. **Eén ontwerpkeuze maakt het veel eenvoudiger:** de AI levert altijd zijn werk in *sjabloonvorm* (een tijdelijk, inactief planning-, traject- en schijvensjabloon), ook als hij vanaf een leeg schema begint. Dan hergebruiken we het bestaande toepassen, de codes, de koppelingen en de proefberekening, en kan een goed voorstel met één klik een nieuw sjabloon worden. Zo groeit je bibliotheek vanzelf.
5. **Deadlines blijven een uitkomst van de berekening, niet van de AI.** Dat is al jouw besluit (16/11). De AI mag duur, volgorde en parallellisatie voorstellen om een doeldatum te halen; de motor zegt of het lukt.
6. **Aanbevolen aanpak: in fases bouwen**, te beginnen met "kies de juiste sjablonen en pas ze aan" (hoofdstuk 9). Dat geeft al waarde zonder de moeilijkste deel (volledig leeg schema) te moeten oplossen.

## 2. Waarom het huidige voorstel niets zinnigs doet

Bron: `CPMCore/Services/ProjectSjabloon/ProjectSjabloonAiService.cs`, eindpunt in `ProjectPlanningController.cs` (rond regel 197).

| Wat | Hoe het nu is | Gevolg |
|---|---|---|
| Uitvoer | Vier waarden: `verdiepingen`, `kelder`, `dak`, `eenPlanningVoorAlles` plus een toelichting | Je krijgt terug wat je zelf in een formulier had kunnen kiezen |
| Kennis van je sjablonen | Geen. De prompt noemt geen enkel sjabloon | De AI kan niet kiezen tussen "Woning" en "Appartementsgebouw", dat doet de gebruiker al vooraf |
| Kennis van het project | Geen. Enkel de tekst die je intikt | Hij ziet niet hoeveel eenheden er zijn, welke betalingsgroepen, welke klanten een eigen termijn hebben, welke aannemers een contract hebben |
| Eenheden | Niet in beeld | "12 appartementen op 4 verdiepingen" wordt enkel `verdiepingen = 4` |
| Datums en deadlines | Geen startdatum, geen opleverdatum, geen termijn | Het voorstel kan niets over timing zeggen |
| Traject, schijven | Geen | Geen mijlpalen, geen betalingsschijven, geen aannemers |
| Onzekerheid | `kelder` en `dak` mogen `null` zijn, maar de server maakt van een onbekende kelder gewoon `false` (`kv.TryGetValue... && kb`) | "Niet gezegd" wordt stil "geen kelder". Een goede assistent zou vragen of aannemen *en dat tonen* |
| Vragen stellen | Niet mogelijk (één aanroep, `max_tokens` 600, `tool_choice` auto) | Geen dialoog |
| Controle | Alleen begrenzen (1 tot 20 verdiepingen) | Prima voor vier velden, onvoldoende voor een heel project |

Het patroon is dus: een correcte kleine stap (tekst naar variabelen), maar niet de stap die jij bedoelt (beschrijving naar volledig plan).

## 3. Wat jij wil, in één zin

> Ik beschrijf een project. De AI maakt, vertrekkend van mijn eerdere sjablonen of van een leeg schema, alles op zoals een projectontwikkelaar het zou doen: planning, traject, betalingsschijven, aannemers, deadlines, en laat mij dat nakijken.

Dit is een andere categorie dan wat er nu staat. Het is geen invulhulp meer maar een **samensteller**. Dat vraagt een andere architectuur (hoofdstuk 6).

### Hoe een projectontwikkelaar denkt (wat de AI moet navolgen)

Een ontwikkelaar begint niet bij "kelder ja/nee" maar bij de volgorde van het hele project. Een voorstel dat overtuigt, volgt zijn denkwijze:

1. **Wat bouwen we en voor wie?** Aantal bouwdelen, eenheden, type (woning, appartement, parking), bouwwijze.
2. **Wanneer moet het af?** Een opleverdatum of -termijn bepaalt alles; de koper-termijn kan strenger zijn.
3. **Wat moet eerst geregeld zijn?** Vergunning, nutsaansluitingen, werfmelding, veiligheidscoördinator, EPB, grondwerken. Deze hebben lange en onzekere doorlooptijden en zitten in de hoofdplanning en het traject.
4. **Hoe loopt de bouw?** Ruwbouw per bouwdeel (en per verdieping), dak, gevel, technieken, afwerking; woningen als estafette (de ploeg schuift door).
5. **Wie doet wat?** Aannemers per discipline, bij voorkeur uit de contracten van het project.
6. **Wanneer komt het geld binnen?** Schijven gekoppeld aan bouwmijlpalen, in de volgorde van de bouw.
7. **Wat kan misgaan?** Kritiek pad, verlet door weer, wachttijden op keuringen, de termijn van de koper.

Als de AI deze volgorde volgt en per stap zegt wat hij aanneemt, voelt het voorstel als werk van een collega in plaats van een formulier.

## 4. De bouwstenen en wat elk kan

### 4.1 Planningssjabloon
Bron: `DALCore/Models/PlanningSjabloon.cs`, `ServiceCore/Planning/PlanningSjabloonUitrol.cs`.
- **Blok** (`PlanningSjabloonBlok`): code, naam, volgorde, `Herhaling` (per verdieping), `VolgtVorigeVerdieping`, voorwaarde (`kelder` ja/nee, `dak` hellend/plat).
- **Taak**: code, naam, duur in werkdagen, volgorde, estafette (met wachttijd), voorgangers (code, type FS/SS/FF/SF, wachttijd).
- **Uitrol** is zuivere logica zonder database: blokken met een niet-geldende voorwaarde vallen weg, herhaalde blokken worden per verdieping gekopieerd.
- **Proefdraaien** (`PlanningSjabloonProef.Rekenen`) geeft de werkdagen zonder iets te bewaren.

Wat het niet kent: een uitvoerder/discipline per taak, deadlines, en aannemers. (In je open lijst staat al "een sjabloontaak met discipline/soort werk".)

### 4.2 De planningsmotor
Bron: `ServiceCore/Planning/PlanningCalculator.cs`, `PlanningService.cs`.
- Rekent alle plannings van het project samen door (voorgangers over plannings heen, estafette), met de projectkalender en weerverlet.
- Kent **beperkingen** (moet beginnen op, niet eerder dan, ...) en een **deadline per taak**. Is het einde na de deadline, dan komt er een waarschuwing "x dagen te laat".
- Waarschuwt bij een afwijkende **uitvoeringstermijn van een klant** (rond `PlanningService.cs` regel 554).
- Commando's: `taakToevoegen`, `faseToevoegen`, `wijzig`, `verplaats`, `planningToevoegen`, `sjabloonToepassen`, `importeren`, enz.

Dit is de scheidsrechter: wat de AI voorstelt kan hier altijd tegen gerekend worden.

### 4.3 De importroute (leeg schema)
`importeren` neemt een lijst rijen (fase/taak/mijlpaal, duur, voorgangers, beperking, deadline, resources) en maakt er een planning van. Dat is in feite al een "leeg schema vullen"-route. **Maar:** de import kent geen codes, geen herhaling per verdieping en geen estafette (`PlanningService.cs` rond regel 1604 zet geen `Code`). Daarom is sjabloonvorm voor de AI beter (zie 6.3).

### 4.4 Trajectsjabloon
Bron: `TrajectSjabloon*.cs`, `TrajectInstantiationService.cs`.
- **Fases** en **mijlpalen**: code, type (vergunning, werf, verkoop, financieel, keuring, oplevering, ...), scope (project of per eenheid), verantwoordelijke rol (projectleider, projectontwikkelaar, boekhouder, verkoper, architect, extern, ...).
- **Doeldatum**: een anker (`DoeldatumAnkerCode`, standaard `PROJECT_CREATED`) plus offset in dagen, of gebonden aan een bron (`BronBinding`: o.a. planningstaak = 4, planningssectie = 5, facturatieschijf = 6, klantdatum = 7, nutsafrekening = 8, dossier = 11).
- **Afhankelijkheden** tussen mijlpalen (FS, SS, FF).
- **Triggers**: bij bereiken, bij overschrijding, X dagen voor de streefdatum, bij statuswijziging, bij afronding van de fase; acties zoals verwittig rol of gebruiker, maak taak, maak dossier, zet projectvlag of -status, plan herinnering, deblokkeer volgende fase, stuur dossier-aanvraagmail.
- Er zijn al standaardlijsten voor vergunning en nuts (`VergunningChecklistDefaults`, `NutsChecklistDefaults`). Dat is precies het type domeinkennis dat een ontwikkelaar-AI nodig heeft.

### 4.5 Schijvensjabloon
Bron: `SchijfSjabloon*.cs`, `SchijfSjabloonUitrol.cs`.
- Schijf: naam, `PlanningCode`, `VastPercentage` of `Gewicht`, `PerVerdieping`, voorwaarde (kelder, dak).
- Rekenregels: vaste percentages eerst, rest naar gewicht, afrondingsrest op de laatste schijf met gewicht, minimum 5 % als waarschuwing.
- `PlanningSchijfAutoKoppeling` hangt de schijven aan de taak of fase met dezelfde code. Stap 3 maakt een taak voor de projectleider zodra een schijf bereikt is.

### 4.6 Projectsjabloon
Een bundel van één planning-, één traject- en één schijvensjabloon, plus standaardantwoorden (verdiepingen, kelder, dak). Toepassen **vult enkel aan wat leeg is** en laat bestaande planningen, trajecten en schijven ongemoeid.

### 4.7 Deadlines, aannemers, taken
- **Deadlinebronnen die al bestaan:** `PlanningTaak.Deadline`, klant-uitvoeringstermijn, `Project.DeliveryDate` / `DeliveryDateDef` / `ExecutionDays`, `Mijlpaal.Doeldatum` en `DoeldatumBerekend`, het portfolio-overzicht `Deadlines` (mijlpalen over alle projecten).
- **Aannemers:** `Contract` per project, `UitvoerderCompanyId` op fase en taak (nu beperkt tot contractbedrijven).
- **Taken:** `ProjectTaak` (Mijn taken) met `BronSleutel`, door de planning aangemaakt bij een bereikte schijf.

## 5. Hoe ze samenwerken: de lijm

```
                        ┌──────────────────────────────┐
   beschrijving ──────► │  Projectsjabloon (bundel)    │
                        └───────┬───────────┬──────────┘
                                │           │
                  ┌─────────────▼──┐   ┌────▼────────────┐   ┌────────────────┐
                  │ Planning       │   │ Traject         │   │ Schijven       │
                  │ blokken/taken  │   │ fases/mijlpalen │   │ %, gewicht,    │
                  │ + CODES        │◄──┤ mijlpaal.Code   │   │ PlanningCode   │
                  │                │◄──┼─────────────────┼───┤                │
                  └───────┬────────┘   └─────────────────┘   └────────────────┘
                          │  rekent door (werkkalender, verlet, estafette)
                          ▼
              einddatum per bouwdeel ─► deadline-controle ─► waarschuwing / taak
```

- **De code is de sleutel.** Een taak `RUWBOUW_KLAAR` in de planning, een mijlpaal met dezelfde code in het traject en een schijf met dezelfde `PlanningCode` horen samen. Toepassen volgt een vaste volgorde: planning, dan traject (`BronRefId` ingevuld via de code), dan schijven.
- **De planning is de bron van waarheid voor datums.** Mijlpalen met `BronBinding` 4 of 5 volgen de planning; schijven worden "bereikt" via de gekoppelde taak.
- **Deadlines lopen één kant op:** uit de berekening, naar waarschuwingen en taken. De AI schrijft er nooit in.

### Waar de lijm nu nog niet volstaat voor een volledig voorstel
Dit zijn de plekken waar een AI-voorstel zou stuiten, ook al is de AI perfect. Ze zijn belangrijk omdat het voorstel er anders mooi uitziet maar niet aan elkaar hangt.

1. **Codes die vaker voorkomen worden niet gekoppeld.** `PlanningTrajectKoppeling.KoppelOpCode` laat een mijlpaal met rust als dezelfde code in meer dan één taak of fase voorkomt (meerdere woningen, herhaald blok per verdieping). Voor een project met veel woningen blijven mijlpalen als `RUWBOUW_KLAAR` dus ongekoppeld. De oplossing ligt waarschijnlijk in de bestaande scope **per eenheid** (`MijlpaalScope.PerEenheid`, `Mijlpaal.UnitId`): één mijlpaal per woning, gekoppeld aan de planning van die woning. Dit is een voorwaarde voor een goed voorstel en moet eerst geverifieerd en gebouwd worden.
2. **Twee tijdlijnen.** Trajectmijlpalen zonder bindingen rekenen met anker en offset vanaf `PROJECT_CREATED` of `StartDateConstruction` (`TrajectInstantiationService`), terwijl de planning haar eigen berekende start heeft. Zonder koppeling kunnen die uit elkaar lopen. Het voorstel moet daarom elke mijlpaal die een bouwmoment is, aan de planning binden, en enkel de echte administratieve mijlpalen op anker en offset laten staan.
3. **Sjabloontaken kennen geen discipline.** Zonder discipline kan de AI geen aannemer voorstellen. Dit staat al op je open lijst.
4. **Geen doeldatum in het sjabloon of de antwoorden.** `ProjectSjabloonAntwoorden` heeft geen startdatum, geen opleverdatum en geen volgorde van woningen. Een voorstel dat "terugrekent vanaf de oplevering" heeft die nodig.
5. **Aanmaken van projecten.** Het projectsjabloon kiezen bij het aanmaken van een project is bewust niet gebouwd. Het voorstel past het best bij die plek.

## 6. Het gewenste ontwerp

### 6.1 Principes
1. **Voorstel, nooit rechtstreeks wegschrijven.** (Bevestigd besluit van 16/11.) Niets in de database voor jij hebt nagekeken.
2. **De AI bepaalt inhoud, de code bepaalt waarheid.** Datums, percentages die moeten optellen tot 100 %, geldige codes, geen cycli: dat is deterministisch en wordt gecontroleerd, niet vertrouwd.
3. **Eerst je eigen sjablonen, dan pas verzinnen.** Een bestaand sjabloon is bewezen werk. De AI vertrekt daarvan en wijkt af waar het project afwijkt.
4. **Elke aanname zichtbaar.** Wat de AI niet wist en invulde, staat er expliciet bij, met een knop om het te corrigeren.
5. **Hergebruik wat bestaat.** Toepassen gaat via dezelfde commando's en services als nu.

### 6.2 De drie routes (jouw "eerdere sjablonen of een leeg schema")
Per onderdeel (planning, traject, schijven) kiest de AI één van drie:

| Route | Wanneer | Wat de AI doet |
|---|---|---|
| **A. Sjabloon gebruiken** | Een sjabloon past | Kiest het sjabloon, zet de variabelen (verdiepingen, kelder, dak), geeft de reden |
| **B. Sjabloon aanpassen** | Een sjabloon past voor 80 % | Kiest het sjabloon en levert een **patch**: blok toevoegen of weglaten, duur van een taak wijzigen, voorganger toevoegen, schijf toevoegen |
| **C. Vanaf leeg** | Geen sjabloon past (bv. een nieuw projecttype) | Schrijft een volledig nieuw concept-sjabloon, met codes, voorgangers en duren |

Een patch (B) is veel beter te controleren dan een volledig nieuw schema (C): jij ziet enkel het verschil met iets dat je vertrouwt. Dat is de reden om met A en B te beginnen.

### 6.3 Altijd in sjabloonvorm werken
Ook route C levert de AI als een **tijdelijk, inactief sjabloon** (`IsActief = false`, herkenbaar als "AI-concept"). Voordelen:
- Het bestaande toepassen (`sjabloonToepassen`, `ProjectSjabloonService.Verwerk`) werkt ongewijzigd, inclusief codes, herhaling per verdieping, estafette en de automatische koppelingen.
- De proefberekening (`PlanningSjabloonProef`) en de controle in de sjablooneditor (voorwaarden, 100 %-controle van schijven) vangen fouten op.
- Een goed voorstel is met één klik **"Bewaren als sjabloon"**. Je bibliotheek groeit met elk project, en de volgende keer is route A of B vaker mogelijk.

### 6.4 Het conceptdocument
De AI levert één JSON-document, geen losse velden. Schets (niet definitief):

```json
{
  "samenvatting": "Residentie met 12 appartementen, 4 bouwlagen, kelderparking, plat dak.",
  "aannames": [
    { "tekst": "Geen kelder vermeld, maar 'ondergrondse parking' impliceert een kelder.", "zeker": false, "veld": "bouwdelen[0].kelder" }
  ],
  "vragen": [
    { "tekst": "Moet de oplevering voor of na de bouwverlofperiode vallen?", "invloed": "deadline" }
  ],
  "bouwdelen": [
    { "naam": "Gebouw A", "eenheden": ["App 1.01", "..."], "verdiepingen": 4, "kelder": true, "dak": "plat",
      "startVolgorde": 1, "gewensteOplevering": "2028-09-30" }
  ],
  "planning": {
    "hoofdplanning": { "route": "A", "sjabloonId": 12, "patch": [] },
    "perBouwdeel":  { "route": "B", "sjabloonId": 7,
      "patch": [
        { "actie": "duurWijzigen", "code": "LEIDINGEN", "duur": 25, "reden": "Meer technieken door liftschacht." },
        { "actie": "blokToevoegen", "na": "B_GEVEL", "blok": { "code": "B_LIFT", "naam": "Lift plaatsen", "taken": [ "..." ] } }
      ] }
  },
  "traject": { "route": "A", "sjabloonId": 3, "extraMijlpalen": [
      { "code": "OMGEVINGSVERGUNNING", "naam": "Omgevingsvergunning", "type": "Vergunning", "rol": "Projectontwikkelaar",
        "anker": "PROJECT_CREATED", "offsetDagen": 120 } ] },
  "schijven": { "route": "A", "sjabloonId": 5, "aanpassingen": [] },
  "uitvoerders": [ { "discipline": "ruwbouw", "voorstelCompanyId": 41, "reden": "Contract in dit project, ruwbouw." } ],
  "deadlines": { "doel": "2028-09-30", "bron": "project", "strengsteKlantTermijn": null },
  "onzekerheid": { "planning": "midden", "traject": "laag", "schijven": "laag" }
}
```

Belangrijk: **elk feit in het document is een verwijzing naar iets dat de code kan controleren** (een sjabloon-id, een code, een rol, een contract). De AI mag geen vrije tekst gebruiken waar de code een waarde verwacht.

### 6.5 De pijplijn

```
1 Intake       beschrijving + projectcontext + sjabloonbibliotheek
2 Vragen       enkel als iets doorslaggevend ontbreekt (max. 3-5), anders aannames tonen
3 Samenstellen AI levert het conceptdocument (route A/B/C per onderdeel)
4 Controleren  code valideert en rekent door met de echte motor
5 Herstellen   fouten terug naar de AI, max. 2 rondes
6 Nakijken     jij ziet per onderdeel voorstel, aannames, verschil met sjabloon, Gantt-voorbeeld
7 Toepassen    via bestaande services, in vaste volgorde
8 Leren        wat je wijzigde en of je het bewaarde als sjabloon, wordt onthouden
```

**Stap 1, intake: wat de AI te zien krijgt.**
- Je beschrijving.
- Projectcontext uit de database: aantal en namen van eenheden, betalingsgroepen en hun huidige schijven, bestaande plannings en traject, startdatum bouw, opleverdatum, klanten met een eigen uitvoeringstermijn, weerstation en kalender, contracten (welke aannemers, welke discipline).
- Een compacte samenvatting van **alle actieve sjablonen**: naam, beschrijving, variabelen, blokken, codes, totale duur. Dat zijn tientallen, geen duizenden, dus alles past in de prompt en hoeft geen zoekindex.
- Eigen historiek: voor taken met een afgeronde `WerkelijkStart` en `AfgerondOp` de werkelijke duur uit eerdere projecten (zie 8.3).

**Stap 2, vragen.** Een aparte, goedkope aanroep beslist: kan ik zonder vraag verder met redelijke aannames, of is er iets dat het plan fundamenteel verandert (aantal bouwdelen, oplevermoment, kelder)? Het antwoord is geen blokkade: je kan "gebruik je aannames" kiezen.

**Stap 4, controleren.** Dit is de belangrijkste stap en volledig deterministisch:
- Sjabloon-ids bestaan en zijn actief; codes zijn uniek; voorgangers verwijzen naar bestaande codes; geen cycli (de motor weigert die al).
- Schijven tellen op tot 100 % (bestaande controle); elke `PlanningCode` bestaat in de planning (bestaande melding "vindt geen taak in ...").
- Mijlpaalcodes bestaan in de planning of zijn bewust administratief (anker plus offset).
- Rollen en mijlpaaltypes zijn geldige waarden uit de enums.
- **Doorrekenen** met de werkkalender van het project: einde per bouwdeel, kritiek pad, vergelijking met de deadline en de strengste klanttermijn.
- Resultaat: groen, geel (aandachtspunt) of rood (moet opgelost). Rode fouten gaan terug naar de AI met de exacte melding.

**Stap 6, nakijken (het scherm).** Per onderdeel een kaart: *Planning*, *Traject*, *Schijven*, *Aannemers*, *Deadlines*. Per kaart: wat de AI voorstelt, de aannames, het verschil met het sjabloon, en per item Aanvaarden, Aanpassen of Weglaten. Bovenaan een Gantt-voorbeeld met de berekende einddatum tegenover de deadline. Onderaan "Wat gebeurt er als ik toepas" in gewone taal (dezelfde stijl als het bestaande voorbeeld).

**Stap 7, toepassen.** In vaste volgorde: sjablonen vastleggen (alleen bij route B/C en enkel als je dat wil), planning toepassen, traject aanmaken, schijven zetten, uitvoerders invullen, koppelingen op code leggen. Dit bestaat grotendeels al in `ProjectSjabloonService.Verwerk`.

**Stap 8, leren.** Zie 8.3.

## 7. Deadlines: wat de AI wel en niet doet

Het besluit van 16/11 blijft staan: **de AI bepaalt nooit zelf een deadline.** Een voorstel mag er wel mee werken:

| De AI mag | De AI mag niet |
|---|---|
| Een doeldatum uit je beschrijving of het project overnemen | Een deadline verzinnen die nergens uit volgt |
| Duur per taak, volgorde van woningen, parallellisatie en startdatum per bouwdeel voorstellen | De berekende einddatum overschrijven |
| Terugrekenen: "om op 30/09/2028 op te leveren moet de ruwbouw uiterlijk x klaar zijn" (via de motor, niet uit het hoofd) | Een klanttermijn negeren |
| Een scenario tonen: "als je woning 3 en 4 parallel bouwt, haal je het, met 1 ploeg extra" | Zonder jouw akkoord iets toepassen |
| Bij een niet haalbaar plan zeggen wat het kost om het wel te halen | Een mislukte controle wegmoffelen |

**Hiërarchie van deadlines in het voorstel** (de strengste wint, volgens je besluit): termijn van de koper (per woning) vóór de termijn van het project (`DeliveryDate`/`ExecutionDays`) vóór een door jou opgegeven doeldatum.

**Wat de AI aan deadlines toevoegt dat nu ontbreekt:**
- Realistische **wachttijden voor externen** als aandachtspunt (vergunningen, nutsaanvragen, keuringen), op basis van wat je eigen projecten werkelijk duurden (8.3), niet op basis van algemene kennis.
- **Controlemijlpalen in het traject** die bij de volgende fase horen (X dagen voor een streefdatum een herinnering naar de projectleider), via de bestaande triggers.
- **Waarschuwingen** (stap 6 van je bouwvolgorde) komen uit de motor; de AI kan ze enkel uitleggen en oplossingen voorstellen.

## 8. Kennis: waar komt "zoals een projectontwikkelaar" vandaan?

Dit is het zwakste punt van elke AI-aanpak als je het niet bewust regelt. Drie bronnen, in volgorde van betrouwbaarheid:

### 8.1 Je eigen sjablonen en projecten (hoogste betrouwbaarheid)
De sjablonen zijn al afgeleid van echte projecten (Koksijde, Erpe-Mere, RDR, Res. Moleken, zie `_migrations/TEST_project79_sjablonen.sql`). Dit is wat de AI als eerste moet gebruiken.

### 8.2 Een vaste "ontwikkelaarsregels"-lijst (middelmatig, maar controleerbaar)
Domeinkennis die niet in je sjablonen staat, hoort niet verstopt in de prompt maar in **een lijst die jij kan lezen en aanpassen**: wat bij een appartementsgebouw altijd hoort (liftkeuring, brandweeradvies, akoestiek), wat bij een kelder hoort (waterdichting, bemaling), welke externe stappen een lange doorlooptijd hebben. Een deel staat al in `VergunningChecklistDefaults` en `NutsChecklistDefaults`. Voordeel: je kan de regel corrigeren in plaats van de AI te vertrouwen. Dit past in het bestaande sjabloonbeheer als "aandachtspunten per projecttype".

### 8.3 Werkelijke gegevens uit afgeronde taken (hoogste meerwaarde, nog niet benut)
`PlanningTaak` bewaart `BasisStart`/`BasisEinde` (plan) en `WerkelijkStart`/`AfgerondOp` (werkelijk). Over afgeronde projecten heen geeft dat per taakcode een **echte normduur** en de afwijking tegenover het plan. Dat is wat een ervaren ontwikkelaar "in zijn hoofd" heeft. Het kan als een gewone query (mediaan en spreiding per code) aan de AI meegegeven worden. Dit maakt het verschil tussen "redelijke schatting" en "jullie eigen cijfers", maar werkt pas goed als er genoeg afgeronde, juist bijgehouden taken zijn. Of dat zo is, kon ik niet nagaan (hoofdstuk 11).

### 8.4 Algemene kennis van het model (laagste betrouwbaarheid)
Het model weet veel over bouw, maar niet over jouw regio, jouw ploegen of de actuele doorlooptijden bij je netbeheerder. Het moet **altijd als aanname gemarkeerd** worden, nooit als feit.

## 9. Aanbevolen fasering

| Fase | Wat | Waarde | Inspanning (grof) | Risico |
|---|---|---|---|---|
| 0 | Huidige AI niet verder uitbreiden. Houd de vier velden eventueel als snelle invulhulp tot fase 1 klaar is | Geen | Geen | Geen |
| 1 | **Bibliotheek-voorstel (route A).** AI krijgt alle sjablonen plus projectcontext, kiest per onderdeel het juiste sjabloon, zet variabelen, stelt een **schijvenaanpassing** voor, toont aannames. Controleren, nakijkscherm, toepassen | Hoog: de AI neemt de keuze en het aanvullen over, jij keurt goed | Middel | Laag |
| 2 | **Aanpassen (route B).** Patches op een sjabloon (blok erbij of eraf, duur wijzigen, voorganger toevoegen). Verschilweergave | Hoog: past een sjabloon aan een afwijkend project aan | Middel | Laag tot middel |
| 3 | **Deadlines en startdatums.** Doeldatum in de antwoorden, terugrekenen, scenario's, volgorde van woningen, per-eenheid-mijlpalen (zie 5.1) | Hoog | Middel tot groot | Middel (bouwt op de koppeling van punt 5.1) |
| 4 | **Traject en aannemers.** Extra mijlpalen, triggers, discipline op sjabloontaken, uitvoerders uit contracten voorstellen | Middel | Middel | Middel |
| 5 | **Leeg schema (route C)** als concept-sjabloon, "Bewaren als sjabloon" | Hoog bij nieuwe projecttypes | Groot | Hoger: kwaliteit hangt af van kennisbron (8) |
| 6 | **Leren**: onthouden wat je wijzigde, normduren uit historiek, feedbacklus | Groeit in de tijd | Middel | Laag |

Waarom deze volgorde: fase 1 en 2 bewijzen het principe (AI kiest en past aan, code controleert, jij keurt goed) op terrein dat je al vertrouwt. Route C (fase 5) is het aantrekkelijkst maar ook het moeilijkst te controleren; het wordt veel veiliger als fase 1 tot 4 de controlemachine en het nakijkscherm al hebben.

## 10. Technische keuzes

- **Model en API.** Hergebruik de Anthropic-koppeling en sleutel (zoals nu: `PuntenSpraakOptions`, standaard `claude-sonnet-5-5`). Voor het samenstellen is een sterker model aan te raden dan voor de intake; het model per stap instelbaar maken.
- **Gestructureerde uitvoer.** Het conceptdocument via een tool met een strikt JSON-schema (zoals nu), maar met volledige schema's per onderdeel. Het huidige `max_tokens = 600` en de 60-secondengrens zijn veel te klein; een volledig voorstel is een lange uitvoer.
- **Asynchroon.** Een volledig voorstel duurt langer dan een gewone aanvraag. Aanbevolen: opdracht starten, voortgang tonen (intake, samenstellen, controleren), resultaat openen. Geen blokkerende POST.
- **Prompt caching.** De sjabloonbibliotheek en de regels veranderen zelden en komen bij elke aanroep terug; ze horen in het gecachete deel van de prompt.
- **Opslag van het voorstel.** Nieuwe tabel (bv. `ProjectVoorstel`: project, status, concept-JSON, controlerapport, wie, wanneer, welk model). Zo kan je een voorstel later heropenen, vergelijken en achteraf zien wat de AI voorstelde en wat jij toepaste. Past bij de harde randvoorwaarde: additief, idempotente migratie.
- **Veiligheid.** De beschrijving is vrije tekst van een gebruiker en kan instructies bevatten. Omdat de AI enkel een document levert dat door code gecontroleerd wordt, en nooit zelf schrijft, is de schade beperkt; houd dat zo. Geen rechtstreekse databasetools voor de AI.
- **Privacy.** Beslis wat er naar de externe dienst mag: namen van klanten en aannemers zijn niet nodig voor het voorstel (id's volstaan). Dit hoort bij je beslissing in hoofdstuk 12.
- **Rechten.** Toepassen vraagt wat het nu al vraagt (planning én facturatie schrijven). Een voorstel *maken* kan met leesrecht; sjablonen *bewaren* vraagt `Settings.Planningssjablonen`.
- **Testen.** Een vaste set van referentieprojecten (de vier TEST-sjablonen en hun oorspronkelijke projecten) met verwachte uitkomsten: "dit gebouw moet dit sjabloon geven, dit einde ± x dagen". Zonder zo'n set kan je een nieuwe prompt of een nieuw model niet verantwoord vervangen. De AI-aanroep zelf is nu nergens echt getest.
- **Kosten.** Te meten, niet te schatten. De sjabloonbibliotheek is klein en kan gecachet worden; de grootste kost is de lange uitvoer van route C. Een teller per voorstel (tokens, euro) hoort van het begin in de opslag.

## 11. Wat ik niet kon nagaan

Zo kan je de analyse op waarde schatten:
- **De live database.** Hoeveel sjablonen er echt zijn, of er afgeronde taken met werkelijke datums zijn (voor 8.3), welke contracten en disciplines bestaan.
- **Of de huidige AI-aanroep in de praktijk werkt.** Het logboek zegt zelf: "de AI-aanroep is nergens echt getest". Dat de uitkomst weinig zinnigs doet, volg ik uit het ontwerp, niet uit een test.
- **Het gedrag in de browser.** Niets van de planning en sjablonen is in de browser getest (volgens het logboek).
- **De precieze werking van de ankers en per-eenheid-mijlpalen** (5.1 en 5.2). Ik heb de code en het logboek gelezen, maar niet doorgerekend. Verifieer dit eerst met een testproject voor je fase 3 plant.
- **Exacte inspanning en kosten.** De schalen in hoofdstuk 9 zijn een grove inschatting, geen offerte.

## 12. Beslissingen die ik van jou nodig heb

1. **Waar start het voorstel?** Bij het aanmaken van een nieuw project, op de planningpagina, of beide? (Het projectsjabloon kiezen bij het aanmaken is nu bewust niet gebouwd.)
2. **Mag de AI nieuwe sjablonen laten aanmaken?** Als inactief concept met "Bewaren als sjabloon" door jou (mijn voorstel), of helemaal niet.
3. **Hoe gedetailleerd?** Een project zoals Res. Moleken heeft zo'n 70 taken. Wil je dat niveau van de AI, of grover (fases en kerntaken) met detail toevoegen door jou?
4. **Mag de AI aannemers voorstellen**, en op welke basis (enkel contracten van het project, plus discipline)? Dit vraagt een discipline op sjabloontaken.
5. **Welke gegevens mogen naar de externe AI?** Namen van klanten en aannemers, of enkel id's en tekst.
6. **Wie keurt goed?** Alleen de projectleider, of ook de projectontwikkelaar voor je iets toepast?
7. **Vragen of aannames?** Wil je liever dat de AI eerst een paar vragen stelt, of meteen een voorstel met zichtbare aannames?
8. **Kennisbron.** Wil je de lijst "ontwikkelaarsregels" (8.2) zelf onderhouden, en vertrouw je de werkelijke duren uit je eigen projecten (8.3) genoeg om ze als basis te gebruiken?
9. **Bouwvolgorde.** Eerst de koppeling per eenheid en de ankers in orde brengen (5.1, 5.2), of eerst fase 1 en 2 en dat later oplossen?

## 13. Wat dit betekent voor de bestaande code

- **Behouden:** `ProjectSjabloonService` (toepassen, overzicht, bewaren), de sjablooneditors, `PlanningSjabloonUitrol`, `SchijfSjabloonUitrol`, `PlanningSchijfAutoKoppeling`, `PlanningTrajectKoppeling`, de planningsmotor.
- **Vervangen:** `ProjectSjabloonAiService` (de vier velden). Het kan als eerste, kleine intake-stap blijven bestaan.
- **Nieuw:** een conceptmodel (DTO's), een controleur/compiler die een concept valideert en doorrekent, een opslag voor voorstellen, het nakijkscherm, en een asynchrone opdrachtverwerking.
- **Aan te passen:** `PlanningTrajectKoppeling` (per eenheid, zie 5.1), `ProjectSjabloonAntwoorden` (doeldatum, startdatum, volgorde van woningen), sjabloontaken (discipline).

## 14. Besluiten van Niels (22/11/2026) en wat ze veranderen

**Op "wat een goed voorstel in de weg staat"**
- **Mijlpalen met een code die vaker voorkomt (5.1):** geen mijlpaal per woning als standaard. Uitleg en voorbeeld: bij 3 woningen met elk een taak `RUWBOUW_KLAAR` weet de koppeling niet welke bedoeld is en laat ze de mijlpaal los. Voorstel: een regel per mijlpaal, *volgt de laatste* (standaard voor projectmijlpalen), *volgt de eerste*, of *per eenheid* (enkel voor wat per woning telt, zoals voorlopige oplevering). Nog te bevestigen door Niels welke woorden hij wil.
- **Aannemers:** worden bij het opstellen nooit gekoppeld aan een planning. Punt 4.7, 5.3, fase 4 en de vraag over uitvoerders in hoofdstuk 12 vervallen voor het voorstel. De uitvoerder wordt later handmatig gekozen (zoals nu, enkel contractbedrijven).
- **Datums:** een startdatum bestaat bij het opstellen niet; ze wordt later ingegeven. De planning werkt dus met **relatieve duren** (werkdagen) en een datum komt pas zodra de start bekend is. De opleverdatum is nooit exact: er is een **maximum aantal werkdagen** (`Project.ExecutionDays`) en de planning hoort daar normaal onder te blijven. De controle wordt dus: "past de berekende duur onder het maximum, met welke marge", niet "haalt de einddatum de deadline". Dit vervangt de deadlinelogica in hoofdstuk 7; de regel dat de AI nooit zelf een deadline bepaalt blijft.
- **Volgorde van woningen:** de AI stelt een standaardvolgorde voor (op naam); Niels past ze zo nodig zelf aan.
- **AI-aanroep testen:** gebeurt in de eerste fase.

**Op de vragen van hoofdstuk 12**
1. **Waar start het voorstel:** op de projectpagina, **uitbreidbaar per projectfase**. Bij het starten van een project bestaat meteen een traject; schijven komen als het project in verkoop gaat; de planning als het in uitvoering gaat. Het voorstel is dus geen eenmalige actie maar **een voorstel per fase**, dat voortbouwt op wat er al is. Het traject kent al projectfases en statussen (`TrajectSjabloonFase.StandaardProjectStatusId`, trigger-actie "zet projectstatus"), wat een haak is om per fase een voorstel aan te bieden.
2. **Concept-sjablonen:** nog niet beantwoord (mag de AI een inactief concept-sjabloon aanmaken dat Niels bewaart?).
3. **Detailniveau:** dat van Res. Moleken, maar **volledig**. Het Moleken-sjabloon mist bv. de vloerplaat van het gelijkvloers. Gevolg: een AI die enkel het sjabloon volgt, herhaalt die fout. Daarom komt er een **volledigheidscontrole** (lijst "wat hoort bij dit type gebouw", hoofdstuk 8.2) en moet het eerst genoemde sjabloon zelf aangevuld worden voor het als basis dient.
4. **Privacy:** niets met namen van klanten of aannemers, geen persoonsgegevens, geen bedragen, geen adressen. Naar de AI gaan enkel anonieme feiten: aantallen, soorten, verdiepingen, kelder, dak, sjabloonstructuur, werkdagen. Eenheden worden aangeduid als "Eenheid 1, 2, 3" en terugvertaald na het antwoord. Percentages van schijven zijn geen bedragen en mogen mee. Dit is een harde regel in de dataverzameling (hoofdstuk 6.5, stap 1) en geldt ook voor de beschrijving die Niels intikt: ze wordt voor verzending gecontroleerd en de gebruiker krijgt een waarschuwing als er een naam of adres in lijkt te staan. Het opgeslagen voorstel bevat dezelfde anonieme gegevens.

**Gevolgen voor de fasering (hoofdstuk 9)**
- **Fase 1 start met het traject en met de variabelen.** Het traject bestaat meteen bij het starten van een project; het voorstel voor schijven en planning komt in latere stappen.
- De fase "Traject en aannemers" wordt "Traject" (zonder aannemers).
- De fase "Deadlines en startdatums" wordt "Duur en maximum aantal werkdagen" (relatieve datums).
- Een nieuwe vroege taak: de **volledigheidscontrole** en het aanvullen van de bestaande sjablonen, want ze bepalen de kwaliteit van elk voorstel.
- Het toevoegen van een **regel voor mijlpalen** (laatste, eerste, per eenheid) komt vóór fase 3.

## 15. Besluiten van Niels, vervolg (22/11/2026)

1. **Een mijlpaal per woning is nodig.** (Mijn lezing van het antwoord "we hebben een mijlpaal per woning nodig".) Dit vervangt het voorstel "volgt de laatste woning" uit hoofdstuk 14 als enige regel: de mijlpaal per woning moet bestaan. Een mijlpaal van het hele project (bv. "ruwbouw van het project klaar") kan daarnaast blijven bestaan en dan de laatste woning volgen; dat is een aparte keuze en hoeft nu niet beslist.
2. **De AI mag een concept-sjabloon aanmaken**, als inactief concept dat Niels naloopt en bewaart (route C, hoofdstuk 6.3). Er wordt dus niets actief zonder zijn akkoord.

### Wat de code hierover al doet en wat ontbreekt (gelezen, niet uitgeprobeerd)
- **Al aanwezig:** een trajectsjabloon-mijlpaal met scope *per eenheid* maakt bij het aanmaken van het traject één mijlpaal per eenheid (`TrajectInstantiationService.cs`, rond regels 83 en 220; ook de synchronisatie kent het). Het model heeft hiervoor `Mijlpaal.UnitId`.
- **Ontbreekt:** het koppelen van zo'n mijlpaal aan de planning van die woning. `PlanningService` weigert dat zelfs uitdrukkelijk (rond regel 778: enkel een mijlpaal van het hele project kan aan de planning gekoppeld worden, niet een van één eenheid). `PlanningTrajectKoppeling.KoppelOpCode` heeft er ook geen regel voor: dezelfde code staat in elke woningplanning en wordt daarom als dubbelzinnig overgeslagen.
- **De oplossing ligt in het datamodel, niet in de AI:** een mijlpaal met `UnitId` hoort de planning te volgen waarvan die eenheid deel uitmaakt (via `BouwPlanningEenheid`, eenheid hoort in hoogstens één planning). Daarmee is de code binnen die planning wel uniek en kan de bestaande koppeling op code (taak of fase, `BronBinding` 4 of 5) per woning werken. Een bouwdeel met meerdere eenheden (appartementsgebouw) valt in dezelfde planning: elke appartement-mijlpaal volgt dan dezelfde taak, wat klopt.
- **Gevolgen voor de volgorde van bouwen:** dit is een **voorwaarde voor elk voorstel dat traject en planning aan elkaar hangt** en komt dus vóór (of samen met) het traject-deel van het voorstel (fase 3 in hoofdstuk 9). Het is klein genoeg om apart te doen en te testen: weigering in de planningcode opheffen voor mijlpalen met een eenheid, koppelen per planning, en een test met een verkaveling van drie woningen.
- **Nog te beslissen:** wat er gebeurt als een mijlpaal per eenheid gekoppeld moet worden en de eenheid nog in geen enkele planning zit (nu: blijft ongekoppeld met een melding, wat ik als standaard aanraad).
