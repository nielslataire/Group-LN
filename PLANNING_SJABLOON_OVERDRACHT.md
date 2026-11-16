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

Status: de grilling-sessie loopt. Er is nog niets gebouwd voor deze feature. Bouw niets voor je bevestigt dat alles klopt.

### Beslist (ronde 1)

- Meerdere plannings per project: een hoofdplanning plus extra plannings (per groep woningen of per woning).
- Verdiepingen worden gevraagd bij het toepassen van het sjabloon.
- Variabelen: aantal verdiepingen, kelder (ja/nee), dak (hellend of plat).
- Een woning kan niet door dezelfde aannemer tegelijk met een andere woning gebouwd worden. De woningen volgen elkaar op.
- Geen aparte mijlpaal van 0 werkdagen in de planning. De schijf wordt aan een gewone taak of fase gekoppeld.

### Voorgesteld in ronde 2 (Q10 tot Q15), nog niet beantwoord

| Q | Voorstel |
|---|----------|
| 10 | Schijf koppelen aan een taak of fase, geen aparte mijlpaal. Koppeling in het taakpaneel onder "Koppeling met schijf". |
| 11 | "Estafette" per taak in het sjabloon: de taak start pas als dezelfde taak in de vorige woning klaar is (met wachttijd). Alle plannings van het project worden samen doorgerekend. Het einde van de laatste woning bepaalt het projecteinde. De volgorde van de woningen kies je bij het toepassen (standaard op naam). |
| 12 | Elk blok in het sjabloon is: altijd aanwezig, alleen als een variabele een waarde heeft, of herhaald per verdieping. |
| 13 | Gewicht per schijf plus vast percentage voor vaste schijven. CPM verdeelt de rest, de laatste schijf krijgt de afrondingsrest, aanpasbaar voor het toepassen. |
| 14 | Deadline per woning: termijn van de koper als die er is, anders die van het project. |
| 15 | Appartementsgebouw: één planning per gebouw met een herhaald blok per verdieping. Een appartement bereikt zijn schijven als het blok van zijn verdieping klaar is. Ontbreekt de verdieping, dan vragen we ze bij het toepassen. |

### Nog te bespreken na ronde 2

- Waar elke variabele wordt opgeslagen.
- Rechten voor het beheren van sjablonen en de plek van de sjablooneditor (Instellingen).
- Hoe bestaande plannings en projecten worden omgezet.
- Datamodel voor meerdere plannings (bijvoorbeeld een `Planning`-entiteit, `PlanningSectie.PlanningId`, startdatum per planning, planningkeuze in de UI).
- Automatisch koppelen via codes (planning-code, `Mijlpaal.Code`, schijf) en de keten die bij "schijf bereikt" een melding geeft.

### Voorgestelde bouwvolgorde (na bevestiging)

0. Basis voor meerdere plannings, inclusief gezamenlijk doorrekenen.
1. Planningsjablonen met codes en automatische koppeling aan trajectmijlpalen.
2. Schijvensjabloon en koppeling.
3. Projectsjabloon en keuze bij het aanmaken van een project.
4. Automatisch "schijf bereikt" met melding.

Bij elke stap hoort een help- en `DESIGN.md`-update.

## 3. Kennis van de codebase

- Planning: `PlanningSectie`, `PlanningTaak`, `PlanningAfhankelijkheid`, `ProjectPlanning` (één rij per project). Rekenlogica in `ServiceCore/Planning` (`PlanningCalculator`, `PlanningService`, `WerkKalender`).
- Traject: `TrajectSjabloon/Fase/Mijlpaal/Trigger`, `Projecttraject`, `Mijlpaal` (`BronBinding` 4 = taak, 5 = fase; `BronRefId`). `TrajectInstantiationService.Instantiate` kopieert `BronBinding` en `BronParam`, maar niet `BronRefId`. Er is nog geen trigger-actie "schijf bereikt".
- Schijven: `InvoicingPaymentGroup`, `InvoicingPaymentStages`, `UnitPaymentStageReached` (`MarkStageReached` / `UnmarkStageReached`).
- Eenheden: `Unit` heeft `Level/LevelId`, `PaymentGroupId`, `BuildingType`. Er is geen gebouw- of blokbegrip. `Project.ProjectType` kent alleen Woonproject en Commercieel.
- Bestanden zijn CRLF. Patch ze met een node-script dat de regeleinden normaliseert.
