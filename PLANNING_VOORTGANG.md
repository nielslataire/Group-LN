# Bouwplanning (Projecten › Planning) — beslissingen en voortgang

Ontwerp: `design-handoff/Menu en toast design updates – CRM Planning.html` (59a werkend prototype, 59b import, 59c toestanden, 59d PDF).
Status: **fase 1, stap 1 (fundament) gebouwd 13/11/2026; de pagina zelf nog niet.**

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
- Traject: `TrajectRecalculationService.GeplandUitPlanning` zet de geplande einddatum van een gekoppelde taak/fase in `Mijlpaal.DoeldatumBerekend`. De koppeling zelf (`BronBinding` 4/5 + `BronRefId`) wordt nog enkel via de trajectsjablonen gelegd, **nergens vanuit de planning of de mijlpaalmodal**.
- Help: `HelpContent/projecten/planning-opbouwen.md`, `voorgangers-en-wachttijd-invullen.md`, `planning-lezen-en-opvolgen.md`.

## Nog te doen
1. Planning in de echte app uitproberen (permissie toekennen, taak aanmaken, voorgangers, verlet, deadline, klantwaarschuwing).
2. Mijlpaal ↔ taak koppelen vanuit de UI (planningspaneel of mijlpaalmodal) en automatisch op `Mijlpaal.Code` bij het aanmaken van een planning/traject.
3. Fase 2: werkkalender-paneel (bedrijfsdagen lezen + projecteigen dagen), planningssjablonen ("Opslaan als sjabloon", Instellingen › Planning), "Fases uit het budget" in de lege staat, dashboard/Projectdetail-kaart.
4. Fase 3: import (MS Project-XML, Excel) met voorbeeldscherm, export (Excel, PDF A3 in Documentlayout-stijl).
5. Fase 4: balken slepen (verschuiven, duur, afhankelijkheid trekken), rechtsklikmenu, samenvoegen bij een conflict, tablet-/gsm-verfijning, toetsenbord (pijltjes op een balk).
6. Klantdetail toont nog geen waarschuwing bij een ontbrekend weerstation.
7. De vooraf ingestelde scroll van de Gantt bij het openen springt naar "vandaag"; bij een planning die geheel in het verleden ligt toont ze de eerste taken dan buiten beeld.
