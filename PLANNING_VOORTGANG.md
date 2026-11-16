# Bouwplanning (Projecten › Planning) — beslissingen en voortgang

Ontwerp: `design-handoff/Menu en toast design updates – CRM Planning.html` (59a werkend prototype, 59b import, 59c toestanden, 59d PDF).
Status: **fase 1 gebouwd (13 tot 15/11/2026); stap 0 meerdere plannings gebouwd 16/11/2026, niet in de echte app getest. Zie de sectie "Meerdere plannings per project".**

## ⚠ Vóór app-start: migratie `_migrations/144_BouwplanningUitbreiding.sql` draaien
De entiteiten `PlanningSectie`/`PlanningTaak` kennen de nieuwe kolommen al. Zonder de migratie falen élke query op die tabellen
(`ProjectVoortgangService`, `TrajectRecalculationService` → Projectdetail, dashboards, traject). Niet door mij uitgevoerd.

## Gebouwd (13/11/2026)
- `ServiceCore/Planning/WerkKalender.cs`: gedeelde werkkalender (ma–vr, `VacationDays`, verlet apart schakelbaar). `ProjectService.AddWorkDays`, `BusinessDaysUntil`,
  `GetFinalConstructionDay`, `GetWorkingDaysLeft` delegeren ernaar (nieuw: `LaadWerkKalender(projectId)`). **Gedragswijziging:** zonder weerstation rekent
  `GetFinalConstructionDay` nu zonder verlet i.p.v. `DateOnly.MinValue` terug te geven.
- Waarschuwing bij ontbrekend weerstation: `WeatherStationMissing` op `ProjectCoreModel`/`ClientModel`, gezet in `ProjectenController.Core` en `KlantenController.Core`;
  getoond in `Projecten/Core/DetailV2` (meldingskader boven de statistiekenrij + "zonder verlet" bij de tegel Werkdagen) en als hulptekst bij het veld Weerstation in `Projecten/Core/EditV2`.
  Klantdetail toont nog geen waarschuwing.
- Migratie 144 + entiteiten: `PlanningSectie.ParentSectieId/TeltMeeVoorVoortgang`; `PlanningTaak.Duur/BeperkingType/BeperkingDatum/Deadline/WerkelijkStart/VerantwoordelijkeUserId/BasisStart/BasisEinde`;
  nieuw `PlanningAfhankelijkheid` (voorganger → opvolger, type FS/SS/FF/SF, wachttijd) en `ProjectPlanning` (versie, basis vastgelegd, verlet meetellen).
  Werkelijk einde = bestaande `AfgerondOp`; mijlpaal = `Duur` 0; start/einde zijn berekende waarden.
- `ProjectVoortgangService`: taakvoortgang per lot nu enkel voor fases met `TeltMeeVoorVoortgang`, deelfases tellen mee, gewogen op duur (mijlpalen niet).
- `ServiceCore/Planning/PlanningCalculator.cs` (zuiver, zonder database): FS/SS/FF/SF met wachttijd (ook negatief), beperkingen, werkelijke data, speling, kritiek pad,
  bepalende voorganger, waarschuwingen (deadline, "niet later dan", geen voorganger), cyclusdetectie. 22 tests in `ServiceCore.Tests/PlanningCalculatorTests.cs` slagen
  (gedraaid in een los testproject, want `CPMCore` was in Visual Studio gestart en vergrendelde de bin-map; `CPMCore` zelf bouwt wel, inclusief Razor).

## Fase 1, stap 2: de pagina (13/11/2026, migratie 144 is gedraaid)
Gebouwd, nog **niet in de echte app getest** (enkel een statische render met headless Edge van de pagina op echte servicegegevens, plus 39 tests die slagen: `ServiceCore.Tests/PlanningCalculatorTests.cs` en `PlanningServiceTests.cs`, gedraaid in een los testproject omdat Visual Studio `CPMCore/bin` vergrendelt; `CPMCore` zelf bouwt, inclusief Razor).
- Permissiecode `Projects.Planning` (`PermissionCodes.vb`, `PermissionCatalog.vb`, `PermissionResolver`), menu-item "Planning" onder Opvolging in `_ProjectInnerMenuV2`. **Niemand behalve admins heeft het recht tot je het in Gebruikersbeheer toekent.**
- `ProjectPlanningController` (`Projects/{id}/Planning`, `Commando`, `Staat`), `IPlanningService`/`PlanningService`, `Views/Projecten/Planning/IndexV2.cshtml`, `gl-v2-projecten-planning.css/.js`.
- Nieuw projectwijd component: Gantt (`wwwroot/css/gl-v2/gantt.css`, `wwwroot/js/gl-v2-gantt.js`), gedocumenteerd in `CPMCore/DESIGN.md` ("Projecten/Planning").
- Wat de pagina kan: kop (start, einde, duur, deadline met marge, verletdagen, basisplanning), meldingen, lijst en Gantt (dag/week/maand), fases in- en uitklappen, taken en fases toevoegen/bewerken/verwijderen/verplaatsen (omhoog, omlaag, inspringen, uitspringen), inline bewerken van naam/duur/voorgangers,
  afvinken, beperkingen, deadline per taak, werkelijke data, voortgang, verantwoordelijke, notitie, kritiek pad, basisplanning vastleggen, verlet meetellen aan/uit, wijzigingsbalk met Ongedaan maken/Opnieuw, conflictmelding via `ProjectPlanning.Versie`.
- Traject: `TrajectRecalculationService.GeplandUitPlanning` zet de geplande einddatum van een gekoppelde taak/fase in `Mijlpaal.DoeldatumBerekend`. De koppeling zelf (`BronBinding` 4/5 + `BronRefId`) werd eerst enkel via de trajectsjablonen gelegd; sinds 15/11/2026 ook vanuit het taak- en fasepaneel (zie hieronder).
- Help: `HelpContent/projecten/planning-opbouwen.md`, `voorgangers-en-wachttijd-invullen.md`, `planning-lezen-en-opvolgen.md`.

## Scrollen en overzicht verbeterd (15/11/2026)
Aanleiding: de datumbalk bleef niet staan en je kon moeilijk door de planning scrollen. Oorzaak: lijst en Gantt zaten in een horizontaal scrollvlak zonder begrensde hoogte, waardoor niets aan de bovenkant kon vastplakken en de scrollbalk onderaan een lange pagina lag. Nu vult de kaart de schermhoogte met één scrollvlak (kop en lijst plakken), versleepbare lijstbreedte met kolommen die meeschalen (container query), zoom **Alles**, Begin/Vandaag, rijmarkering over lijst en grafiek, grafiek slepen, volledig scherm en dichtgeklapte aandachtspunten. Details in `CPMCore/DESIGN.md` (Projecten/Planning, "Scrollen en overzicht"). Getest met een statische render in headless Edge op de echte data van project 79 (1440×900, 1280×720, 1100×760), niet in de echte app.

## Mijlpaal ↔ taak koppelen vanuit de UI (15/11/2026)
Taak- en fasepaneel kregen "Koppeling met trajectmijlpaal" (ontwerp 59a): `PlanningService.Koppel` + `POST …/Planning/Koppeling` (recht Planning schrijven én Traject schrijven), keuzelijst `PlanningStaat.TrajectMijlpalen` (enkel vrije of al aan de planning gekoppelde mijlpalen van het project; een mijlpaal die iets anders volgt wordt geweigerd). Na koppelen en na elke planningswijziging herrekent het Traject (`HerberekenTrajectAsync`, enkel bij gekoppelde mijlpalen), zodat streefdatum en "bereikt" meelopen. 5 nieuwe tests in `PlanningServiceTests` (43 slagen). Help: `taak-datums-en-verantwoordelijke.md`, `planning-handleiding.md`. **Niet gebouwd:** automatisch koppelen op `Mijlpaal.Code` bij het aanmaken van een planning of traject; planningstaken hebben geen code, dus dat vraagt eerst een beslissing (codes op taken, of een koppeltabel per sjabloon).

## Meerdere plannings per project (16/11/2026, stap 0 van het projectsjabloon)
Zie `PLANNING_SJABLOON_OVERDRACHT.md` voor het hele plan (sjablonen, schijven, AI). Stap 0 is gebouwd, **niet in de echte app getest**: 55 planningtests slagen, CPMCore compileert (incl. Razor), de UI is enkel op syntax gecontroleerd.
- Migratie `_migrations/145_MeerderePlannings.sql` (gedraaid op 16/11/2026; nieuwe tabel `BouwPlanning`, kolom `PlanningSectie.BouwPlanningId`; bestaande fases komen in een hoofdplanning). Niet door mij uitgevoerd.
- Hoofdplanning + één planning per bouwdeel (betalingsgroep), samen doorgerekend, startdatum per planning als ondergrens, `H:` voor voorgangers uit de hoofdplanning, tabs op de pagina, planning toevoegen/instellen/verwijderen. Details in `CPMCore/DESIGN.md`.
- Migratie 145 is gedraaid. **Project 79 is hervuld met een voorbeeld (16/11/2026, na bevestiging van Niels):** hoofdplanning (voorbereiding, 3 taken), "Woning 1" (bouwdeel Constructiewaarde 21%, 5 fases, 24 taken) en "Woning 2" (bouwdeel Constructiewaarde 6%, kopie van woning 1 zonder voortgang) met estafette: elke taak van woning 2 wacht op dezelfde taak in woning 1 (FS, 0 dagen). De planning van project 68 (één lege fase) is gewist. Reservekopie van de oude gegevens: scratchpad `planning-backup-16112026`. De opgeslagen start- en einddatums van de taken van woning 2 zijn leeg tot de planning één keer gewijzigd wordt (de pagina rekent ze wel meteen uit).
- Eerste test door Niels (16/11/2026): punten 1 (bestaande functies), 3, 4 en 6 van de testlijst in orde.
- **Feedback van Niels na de eerste test (16/11/2026), nog NIET gebouwd:**
  1. **Estafette zichtbaar en per taak te kiezen.** Voorgangers uit een ander bouwdeel moeten in het veld Voorgangers staan (nu verborgen, wel behouden). Niels wil per taak kiezen of ze wacht op dezelfde taak in de vorige woning (bv. de elektriciteit in woning 1 moet klaar zijn voor ze in woning 2 start), niet verplicht voor elke taak. Plan: voorgangers tonen en typen als `Planningsnaam:3.2` (`H:` blijft de hoofdplanning; komma, puntkomma en dubbelpunt niet toestaan in een planningsnaam), plus in het taakpaneel een keuzevakje "Wacht op dezelfde taak in de vorige planning" dat die verwijzing invult (zelfde naam, anders zelfde nummer). Elke rij krijgt daarvoor een `estafetteVoorstel`.
  2. **Duidelijkere foutmelding.** Een fout die een actie tegenhoudt (bv. voorganger 9.9 bestaat niet) toont nu als toast rechtsonder en valt niet op. Niels wil ze in het midden en opvallend, conform design-handoff 4g (blokkerende fout inline, niet als toast). Plan: `.gl-v2-inline-error` (bestaat in `gl-v2/toasts.css`, nog zonder gebruiker) als gecentreerde banner bovenaan de planningkaart voor alle blokkerende fouten van de planningpagina (inline bewerken, paneel, koppelen), met sluitknop. Eventueel ook als gedeeld patroon opnemen in `DESIGN.md`.
- Nog te doen in stap 0: de rest van de pagina in de echte app uitproberen (vooral punt 5 na de nieuwe foutmelding).
- Help: `HelpContent/projecten/planning-per-bouwdeel.md`.

## Nog te doen
1. Planning in de echte app uitproberen (permissie toekennen, taak aanmaken, voorgangers, verlet, deadline, klantwaarschuwing).
2. ~~Mijlpaal ↔ taak koppelen vanuit de UI~~ gedaan op 15/11/2026. Nog open: automatisch koppelen op `Mijlpaal.Code` (zie hierboven) en koppelen vanuit de mijlpaalmodal van het Traject.
3. Fase 2: werkkalender-paneel (bedrijfsdagen lezen + projecteigen dagen), planningssjablonen ("Opslaan als sjabloon", Instellingen › Planning), "Fases uit het budget" in de lege staat, dashboard/Projectdetail-kaart.
4. Fase 3: import (MS Project-XML, Excel) met voorbeeldscherm, export (Excel, PDF A3 in Documentlayout-stijl).
5. Fase 4: balken slepen (verschuiven, duur, afhankelijkheid trekken), rechtsklikmenu, samenvoegen bij een conflict, tablet-/gsm-verfijning, toetsenbord (pijltjes op een balk).
6. Klantdetail toont nog geen waarschuwing bij een ontbrekend weerstation.
7. ~~Scroll bij het openen~~ opgelost op 15/11/2026: opent op vandaag, of op het begin van de planning als vandaag buiten het bereik valt.
