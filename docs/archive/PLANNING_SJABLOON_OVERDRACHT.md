# Overdracht: planning, sjablonen en koppelingen (16/11/2026)

Branch: `layout-experiment`. Niets van het werk hieronder is gecommit. Commit en push eerst op de eerste pc, anders staat het niet op de tweede.

## 1. Wat al gebouwd is (niet gecommit)

- Planningpagina: scrollen en overzicht verbeterd (sticky datumbalk, schaal "Alles", Begin/Vandaag/Volledig scherm, versleepbare scheiding lijst/Gantt). Enkel getest in een statische testpagina, niet in de echte app.
- Planning van project 79 gevuld in de database.
- Fix: mijlpalen met duur 0 blijven bewaard (`ValueGeneratedNever()` in `DALCore/Models/cpmRunningContext.cs`).
- Mijlpaal van het Traject koppelen aan een taak of fase vanuit het paneel ("Koppeling met trajectmijlpaal"). `PlanningService.Koppel`, `POST Projects/{id}/Planning/Koppeling`. 43 planningtests slagen.
- Helpboom voor een volledige planning (`CPMCore/HelpContent/projecten/planning-*.md` en andere). Screenshots staan als verwijzing in `CPMCore/HELP_SCREENSHOTS.md` en moeten nog met Playwright genomen worden.
- `CPMCore/DESIGN.md` en `PLANNING_VOORTGANG.md` zijn bijgewerkt.
- Migratie 144 (`_migrations/144_BouwplanningUitbreiding.sql`) is op de database uitgevoerd.

Nog te doen aan jouw kant: de planningpagina en de nieuwe keuze in de echte app testen, screenshots maken.

## 2. Nieuwe feature: projectsjabloon, meerdere plannings, automatische koppelingen

Wens: bij een nieuw project een projecttype kiezen (verkaveling met woningen, appartementsgebouw, ...) en dan al een standaardplanning, een traject en facturatieschijven hebben, automatisch aan elkaar gekoppeld. Je zegt "fase bereikt" in de planning en de rest volgt.

Status: de grilling-sessie is afgerond en door Niels bevestigd (16/11/2026). **Stap 0 is gebouwd** (zie onderaan), niet in de echte app getest. Stappen 1 tot 6 volgen.

### Harde randvoorwaarde

De huidige versie van CPMCore moet steeds blijven werken. Enkel de planning zelf (gegevens, pagina) is niet van belang en mag gewist en hervuld worden. Alles buiten de planning (traject, facturatie, eenheden, dashboards, `ProjectVoortgangService`) mag door de bouw niet breken. Migraties blijven additief en idempotent (zie `db_migration_workflow`).

### Bevestigde beslissingen

**Bouwdelen en plannings**
- Een **bouwdeel** is een woning of een gebouw. Elk bouwdeel heeft een eigen planning, eigen schijven (`InvoicingPaymentGroup`) en een eigen deadline. Een woning is een bouwdeel met één eenheid, een gebouw een bouwdeel met meerdere appartementen. Alle eenheden van een gebouw hebben dezelfde schijven, ongeacht de verdieping.
- Daarnaast heeft elk project een **hoofdplanning zonder bouwdeel** (vergunningen, wegenis, nutsvoorzieningen). Een taak in een bouwdeelplanning mag een voorganger in de hoofdplanning hebben. Verder lopen koppelingen tussen plannings enkel via de estafette.
- **Estafette** per taak in het sjabloon: de taak start pas als dezelfde taak in de vorige woning klaar is, met instelbare wachttijd. Alle plannings van het project worden samen doorgerekend, het einde van de laatste woning bepaalt het projecteinde. De volgorde van de woningen kies je bij het toepassen, standaard op naam.
- **Datamodel:** nieuwe entiteit `Planning` (project, naam, optioneel bouwdeel/betalingsgroep, startdatum, volgorde, herkomstsjabloon). `PlanningSectie` krijgt `PlanningId`. `ProjectPlanning` blijft één rij per project (versie, basisplanning, verlet). De startdatum van een planning is standaard berekend (voorgangers, estafette), enkel handmatig als je ze vastlegt.
- Bestaande planningsgegevens worden gewist en project 79 wordt hervuld met een voorbeeld. Dit gebeurt pas in stap 0 van de bouw, na bevestiging van Niels.

**Sjablonen en variabelen**
- Variabelen: aantal verdiepingen (gevraagd bij het toepassen), kelder (ja/nee), dak (hellend of plat). Ze worden bewaard op de planning van het bouwdeel, niet op `Unit`.
- Elk blok in een sjabloon is: altijd aanwezig, alleen als een variabele een waarde heeft, of herhaald per verdieping. Meer soorten voorwaarden niet.
- Beheer: sjablooneditor onder Instellingen › Planning en Instellingen › Schijven, met een eigen permissiecode (`Settings.Planningssjablonen`). Een projecttype-sjabloon bundelt een planningssjabloon, trajectsjabloon en schijvensjabloon. Wie planning mag schrijven kan sjablonen toepassen, enkel wie het sjabloonrecht heeft kan ze maken of wijzigen. Ook "Opslaan als sjabloon" vanuit een bestaande planning.
- **Codes:** taken en fases in een planningssjabloon krijgen een vaste leesbare code, uniek binnen het sjabloon (bv. `RUWBOUW_KLAAR`). Trajectmijlpalen (`Mijlpaal.Code`) en schijven verwijzen naar dezelfde code. Volgorde bij het toepassen: eerst planning, dan traject (`BronRefId` wordt via de code ingevuld), dan schijven. Een code die niet gevonden wordt geeft een duidelijke waarschuwing.

**Schijven**
- Een schijf wordt gekoppeld aan een gewone taak of fase, niet aan een aparte mijlpaal van 0 werkdagen. In het taakpaneel komt "Koppeling met schijf" naast "Koppeling met trajectmijlpaal".
- Schijven bestaan ook per verdieping en hangen dan vast aan de bouwfase van die verdieping (herhaald blok). Het percentage van het hele blok staat in het sjabloon en wordt bij het toepassen gelijk verdeeld over de verdiepingen, afrondingsrest op de laatste.
- Percentages: elke schijf heeft een minimum (standaard 5%, aanpasbaar) en een vast richtpercentage (bv. ruwbouw ongeveer 25%) of een gewicht waarmee de rest over de grootte verdeeld wordt. Vaste percentages gaan eerst af van de 100%, de laatste schijf krijgt de afrondingsrest. Alles is aanpasbaar voor het toepassen. Onder het minimum is een waarschuwing, geen blokkade.
- **Schijf bereikt:** niet automatisch. Als de gekoppelde taak of fase klaar is, krijgt de verantwoordelijke een melding (of punt) "Schijf X van bouwdeel Y is bereikt, markeren?". Met één klik markeer je de schijf voor alle eenheden van het bouwdeel (`UnitPaymentStageReached`). Later per sjabloonschijf een optie "automatisch markeren". De trajectmijlpaal volgt automatisch. De trigger-actie "schijf bereikt" bestaat nog niet.

**Deadlines**
- Deadline per woning: uitvoeringstermijn van de koper als die er is, anders die van het project. De planning waarschuwt als de woning daar niet binnen blijft. `Project.DeliveryDate` blijft de effectieve oplevering.

**AI (open gedachte van Niels, nu als eerste versie vastgelegd)**
- De AI doet enkel een **voorstel** en schrijft nooit rechtstreeks weg. Niets wordt bewaard voor Niels het in de gewone editor heeft nagekeken.
- Eerste versie: in de **sjablooneditor**. De AI stelt enkele gerichte vragen (type project, verdiepingen, kelder, dak, bouwwijze) en geeft een gestructureerd concept terug: blokken, duur, voorgangers, schijfkoppelingen, mijlpalen met codes en triggers en acties (verwittig rol of gebruiker, maak taak, enz.). Het concept wordt gevalideerd tegen de echte codes en acties voor het in de editor verschijnt.
- Later: de AI bij het toepassen op een project (voorstel van de schijven), en daarna waarschuwingen over deadlines. De AI bepaalt nooit zelf een deadline, die komt uit de berekening. Hij kan wel duur per taak, volgorde van woningen, richtpercentages en startdatum per bouwdeel voorstellen.
- Techniek: hergebruik de bestaande Anthropic-koppeling (`AnthropicQuoteAnalysisService`, `PuntenSpraakService`, `AnthropicModel` in `appsettings.json`) via een nieuwe kleine service, bv. `PlanningSjabloonAiService`.

### Stand van de bouw

- **Stap 0 gebouwd (16/11/2026):** migratie `145_MeerderePlannings.sql` (vóór app-start), entiteit `BouwPlanning`, `PlanningSectie.BouwPlanningId`, gezamenlijk doorrekenen, `H:`-voorgangers, tabs en planningbeheer op de pagina, 11 nieuwe tests. Project 79 is gewist en hervuld met een voorbeeld (hoofdplanning, woning 1, woning 2 met estafette; zie `PLANNING_VOORTGANG.md`). Niet gebouwd in stap 0: de estafette-vlag in een sjabloon (hoort bij stap 1).
- Verwerkt op 17/11/2026 (niet in de echte app getest):  (a) voorgangers uit een ander bouwdeel zichtbaar maken als `Planningsnaam:3.2` en per taak een keuzevakje "Wacht op dezelfde taak in de vorige planning" (de estafette is dus per taak te kiezen, niet verplicht), (b) blokkerende fouten als opvallende gecentreerde inline melding (4g) in plaats van een toast rechtsonder. Details in `PLANNING_VOORTGANG.md`.
- Het voorgangersveld toont nu alle voorgangers, ook uit andere plannings (`Planningsnaam:3.2`). De sjablooneditor (stap 1) moet de estafette per taak als vlag bewaren en bij het toepassen omzetten naar zulke koppelingen.

- **Stap 1a gebouwd (17/11/2026):** migratie `146_PlanningSjablonen.sql`, entiteiten `PlanningSjabloon/Blok/Taak/Voorganger`, `PlanningSjabloonUitrol` (zuivere uitrol), `PlanningSjabloonService` (lijst, bewaren vanuit een planning, verwijderen), commando `sjabloonToepassen`, knoppen en modals op de planningpagina. **Stap 1b herwerkt naar de design-handoff op 18/11/2026 (migratie 147, proefdraai via `PlanningSjabloonProef`, lijst met gebruik/gewijzigd/reageert op, dupliceren, inactief zetten).** **Stap 1b gebouwd (17/11/2026, niet in de browser getest):** `PlanningSjabloonAdminController`, `IndexV2`/`EditV2`, `gl-v2-instellingen-planningsjabloon.js/.css`, `PlanningSjabloonService.Haal/Bewaar/BeheerLijst`, kaart bij Automatisatie. (Oorspronkelijke opdracht stap 1b:) sjablooneditor onder Instellingen › Planning (blokken, voorwaarden, herhaling per verdieping, codes, estafettevlag met wachttijd, voorgangers via codes), met rechtencontrole `Settings.Planningssjablonen`. Daarna stap 2 (schijven).

### Bevestigde bouwvolgorde

Bij elke stap hoort een help- en `DESIGN.md`-update, en de huidige CPMCore moet blijven werken.

0. Basis voor meerdere plannings: `Planning`, bouwdeel (betalingsgroep), hoofdplanning, gezamenlijk doorrekenen, estafette. Project 79 wissen en hervullen met een voorbeeld (eerst bevestiging vragen).
1. Planningssjablonen met codes, variabelen en blokken (altijd, voorwaardelijk, herhaald per verdieping).
2. Schijvensjabloon (minimum, richtpercentage, gewicht, herhaling per verdieping) en koppeling aan een taak of fase.
3. Trajectsjabloon met codes en acties, en de nieuwe actie "schijf bereikt" met melding.
4. Projectsjabloon en de keuze bij het aanmaken van een project.
5. AI in de sjablooneditor.
6. Later: AI bij het toepassen op een project, daarna deadlinewaarschuwingen.

## 3. Kennis van de codebase

- Planning: `PlanningSectie`, `PlanningTaak`, `PlanningAfhankelijkheid`, `ProjectPlanning` (één rij per project). Rekenlogica in `ServiceCore/Planning` (`PlanningCalculator`, `PlanningService`, `WerkKalender`).
- Traject: `TrajectSjabloon/Fase/Mijlpaal/Trigger`, `Projecttraject`, `Mijlpaal` (`BronBinding` 4 = taak, 5 = fase; `BronRefId`). `TrajectInstantiationService.Instantiate` kopieert `BronBinding` en `BronParam`, maar niet `BronRefId`. Er is nog geen trigger-actie "schijf bereikt". Bestaande `TriggerEvent`: bij bereiken, bij overschrijding, X dagen voor streefdatum, bij statuswijziging, bij afronding van de fase. Bestaande `TriggerActie`: verwittig rol of gebruiker, maak taak, maak dossier, zet projectvlag of status, plan herinnering, deblokkeer volgende fase, stuur dossier-aanvraagmail.
- Schijven: `InvoicingPaymentGroup`, `InvoicingPaymentStages`, `UnitPaymentStageReached` (`MarkStageReached` / `UnmarkStageReached`).
- Eenheden: `Unit` heeft `Level/LevelId`, `PaymentGroupId`, `BuildingType`. Er is geen gebouw- of blokbegrip. `Project.ProjectType` kent alleen Woonproject en Commercieel.
- AI: Anthropic-API al in gebruik in CPMCore (offerteanalyse, Punten-spraak), model in `appsettings.json` (`AnthropicModel`).
- Bestanden zijn CRLF. Patch ze met een node-script dat de regeleinden normaliseert.


## Besluit 18/11/2026: planning los van betalingsgroep (richting B)
Een betalingsgroep heeft voor alle woningen dezelfde schijven, maar elke woning heeft een eigen planning, dus een schijf wordt per woning op een ander moment bereikt. Planning krijgt een eigen bouwdeel (woning/gebouw), schijven verwijzen naar taken of fases per woning. Uitwerking in stap 2, eerst ontwerpvragen.

## Besluit 18/11/2026: globaal overzicht en aannemerplanning (vóór stap 2)
Volgorde: (a) tab **Globaal** = alle plannings samen in één alleen-lezen Gantt, per planning een groep (gebouwd 18/11/2026); (b) veld **Uitvoerder** bij fase en taak (optioneel, kan later ingevuld worden zodra een aannemer aangesteld is; kiezen uit de leveranciers/contracten van het project; taak erft van fase); (c) weergave **Planning van [aannemer]** (alle taken uit alle plannings, met woning/planning, start, einde; print/PDF; portaal later); (d) pas daarna stap 2 (schijven). Planning per woning als je timing per woning wilt zien; een gebundelde planning enkel bij gelijktijdig werk. Sjabloon-taak met discipline/soort werk: later.
