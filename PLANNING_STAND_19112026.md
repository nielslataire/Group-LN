# Bouwplanning – stand van zaken 19/11/2026 (overdracht naar een andere pc)

Lees dit eerst, daarna `PLANNING_SJABLOON_OVERDRACHT.md` (bevestigde bouwvolgorde en besluiten) en `PLANNING_VOORTGANG.md` (logboek per dag).
Alles hieronder is gebouwd, gebuild en getest met unit tests (**82 planningtests slagen**), maar **nog niet systematisch in de browser getest**. Er is **niets gecommit**: commit en push eerst op de oude pc, anders staat het werk niet op de nieuwe.

## 1. Eerst doen op de nieuwe pc
1. Haal de branch `layout-experiment` binnen (na commit/push op de oude pc).
2. **Migraties vóór app-start** (hand-run, idempotent, additief; zie `_migrations/`):
   - `145_MeerderePlannings.sql`, `146_PlanningSjablonen.sql`, `147_PlanningSjabloonBeheer.sql` (al gedraaid op de live DB).
   - `149_PlanningEenheden.sql`: planning hoort bij eenheden (tabel `BouwPlanningEenheid`). (148 is `ProjectSlugHistoriek`, niet van de planning.)
   - `150_PlanningUitvoerder.sql`: `UitvoerderCompanyId` bij `PlanningSectie` en `PlanningTaak`.
3. Rechten: `Settings.Planningssjablonen` toekennen aan rollen die sjablonen mogen beheren.
4. Build/test: CPMCore bouwen met een tijdelijke uitvoermap als de app draait (`-p:OutDir="$TEMP/cpmout/"`); tests met `dotnet build ServiceCore.Tests --no-dependencies` en dan `dotnet test ServiceCore.Tests --no-build --filter "FullyQualifiedName~Planning"`.
5. Na een wijziging aan een `.cshtml`: de app herstarten en Ctrl+F5.

## 2. Wat er staat
**Meerdere plannings** (Projecten › Planning): hoofdplanning + plannings voor één of meer **eenheden** (woning/gebouw, tabel `BouwPlanningEenheid`, een eenheid in hoogstens één planning), los van de betalingsgroep. Alle plannings worden samen doorgerekend; voorgangers over plannings heen (`H:1.1`, `Woning 1:3.2`); estafette per taak.
**Tab Globaal** (planning-id -1): alle plannings in één alleen-lezen Gantt, per planning een groep.
**Sjablonen** (Instellingen › Planningssjablonen, design 63a/63b): lijst + editor met blokken, taken, voorgangers, estafette, proefdraaien, controle; dupliceren, (de)activeren, verwijderen alleen als niet gebruikt. Toepassen: een woning zonder planning krijgt een nieuwe planning; een bestaande planning met taken kan na bevestiging vervangen worden.
**Importeren uit MS Project-XML** (design 59b, knop "Importeren"): stappen Bestand · Controle · Uitvoerders (alleen met resources) · Opties · Resultaat. Doel: nieuwe planning (met eenheden) of toevoegen/vervangen in de huidige. Parser `ServiceCore/Planning/MsProjectXmlParser.cs`, commando `importeren`, endpoint `ProjectPlanningController.ImportControle`. Wordt niet overgenomen: kosten, werk, de kalender uit het bestand (CPM gebruikt de projectkalender, Projecten › Weather: **voer verlof 2026 én 2027 in**).
**Uitvoerder** (aannemer, optioneel) bij fase en taak; een taak erft van haar fase; keuze uit bedrijven met een `Contract` in het project. Bij de import worden resources uit het bestand aan een bedrijf gekoppeld (voorstel op naamgelijkenis); niet-gekoppelde namen komen in de notitie.

Test van de eigen planning (Villa/EM, 73 taken, 19 fases, 88 voorgangers): einde nu 18/10/2027 tegenover 19/10/2027 in MS Project (verschil door de kalender).

## 3. Volgende stappen (afgesproken volgorde)
1. **Weergave "Planning van [aannemer]"**: alle taken van één uitvoerder uit alle plannings, met woning/planning, start en einde; print/PDF; delen via het leveranciersportaal later. (Effectieve uitvoerder per rij staat al in de staat: `UitvoerderEffectiefId/Naam`.)
2. **Stap 2 uit de bouwvolgorde: schijvensjabloon.** Een betalingsgroep heeft voor al haar eenheden dezelfde schijven, maar elke eenheid bereikt een schijf op een ander moment (via de taak/fase in de planning van die eenheid). Minimum 5% (aanpasbaar), vast richtpercentage of gewicht, herhaling per verdieping; vaste percentages gaan eerst van de 100% af en de laatste schijf krijgt de afrondingsrest; onder het minimum = enkel waarschuwing. "Schijf bereikt" = melding + één klik per eenheid of voor alle eenheden van de groep (`UnitPaymentStageReached`), later eventueel automatisch.
3. Trajectsjabloon met codes en de actie "schijf bereikt"; 4. projectsjabloon; 5. AI in de sjablooneditor; 6. later AI bij toepassen en deadlinewaarschuwingen. "Reageert op" bij een sjabloon wordt nu enkel bewaard en getoond.

## 4. Nog open of bewust uitgesteld
- Import: Excel/CSV, splitsen per hoofdfase in meerdere plannings, verlofdagen uit het bestand toevoegen aan de kalender (jij geeft ze zelf in), resources als echte resources.
- Een sjabloon-taak met discipline/soort werk (om de uitvoerder sneller in te vullen).
- Help-schermafbeeldingen nemen in één batch (lijst in `CPMCore/HELP_SCREENSHOTS.md`, rijen met "ja"/"opnieuw nemen").
- Browsertest van alles hierboven, vooral: import met een echt bestand, "Sjabloon toepassen" op woningen, de tab Globaal, het veld Uitvoerder.

## 5. Waar de code staat
- Service: `ServiceCore/Planning/PlanningService.cs` (commando's, staat, `Importeren`, `SjabloonToepassen`, `GlobaleBoom`), `MsProjectXmlParser.cs`, `PlanningSjabloon*Service.cs`, `PlanningCalculator.cs`.
- DTO's: `FacadeCore/Planning/PlanningDtos.cs`, `PlanningImportDtos.cs`, `PlanningSjabloonBewerkDtos.cs`.
- Model: `DALCore/Models/BouwPlanning*.cs`, `PlanningSectie.cs`, `PlanningTaak.cs`.
- Pagina: `CPMCore/Controllers/ProjectPlanningController.cs`, `Views/Projecten/Planning/IndexV2.cshtml`, `wwwroot/js/gl-v2-projecten-planning.js`, `wwwroot/css/gl-v2-projecten-planning.css`; sjablonen: `PlanningSjabloonAdminController.cs`, `Views/Instellingen/PlanningSjabloonAdmin/`.
- Tests: `ServiceCore.Tests/PlanningImportTests.cs`, `PlanningMeerderePlanningsTests.cs`, `PlanningSjabloonTests.cs`.
- Documentatie: `CPMCore/DESIGN.md` (secties over meerdere plannings, import, uitvoerder, globaal overzicht, sjablooneditor), help in `CPMCore/HelpContent/projecten/planning-*.md` (o.a. `planning-per-bouwdeel`, `planning-sjablonen`, `planning-importeren-ms-project`).
