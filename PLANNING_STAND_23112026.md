# Bouwplanning – stand van zaken 23/11/2026 (overdracht naar een andere pc)

Lees dit eerst. Daarna `PLANNING_VOORTGANG.md` (logboek per dag, onderaan de sectie's van 22 en 23/11), `PLANNING_AI_ANALYSE.md` (hoe een echt AI-voorstel moet werken, met alle besluiten) en `CPMCore/DESIGN.md` (alinea "Bouwlagen, voorwaarden per taak en terrassen of luifels"). Dit bestand vervangt `PLANNING_STAND_19112026.md` als startpunt; dat blijft geldig voor alles van vóór 20/11.

**Status:** alles hieronder is gebouwd, gebuild en getest (**153 planning-, sjabloon-, schijf- en trajecttests slagen**) en door Niels in de browser doorlopen met het testproject (project 79, "TEST Appartementsgebouw"). Niet in de browser getest: de echte AI-aanroep (er is een Anthropic-sleutel nodig). **Er is niets gecommit**: commit en push eerst op de oude pc (branch `layout-experiment`), anders staat het werk niet op de nieuwe. Pak niet blind alles mee: `skills-lock.json` en `.claude/skills/modern-web-guidance/` horen niet bij de planning.

## 1. Eerst doen op de nieuwe pc
1. Haal de branch `layout-experiment` binnen (na commit/push).
2. **Migraties vóór app-start** (hand-run, idempotent, additief; map `_migrations/`): alles tot en met `151_PlanningSchijfKoppeling.sql` zoals in de vorige stand, plus **`157_PlanningSjabloonTaakVoorwaarde.sql`** (kolom `PlanningSjabloonTaak.Voorwaarde`, nullable). Daartussen: `152_ContractPercentagesDecimaal`, `153_ProjectTaakBronSleutel`, `154_PlanningCodes`, `155_Schijvensjablonen`, `156_Projectsjablonen`. Draai ze als ze nog niet op de database staan.
3. Testgegevens (geen migraties, enkel voor een testdatabase): `TEST_project79_sjablonen.sql` en daarna `TEST_project79_sjablonen_gelijkvloers.sql` (herhaalbaar; bouwt het testsjabloon "TEST Appartementsgebouw (naar Res. Moleken)" en het schijvensjabloon "TEST Appartement (met verdiepingen)" om naar de structuur hieronder). De uitvoer van een script staat in SSMS onder het tabblad **Messages**.
4. Bouwen en testen terwijl de app draait (DLL's zijn dan vergrendeld): `dotnet test ServiceCore.Tests -p:OutDir=<tijdelijke map>/ --filter "FullyQualifiedName~Planning|FullyQualifiedName~Sjabloon|FullyQualifiedName~Schijf"`.
5. Na een wijziging aan een `.cshtml`: de app herstarten en Ctrl+F5 (ook voor `.js`).

## 2. Wat er sinds 19/11 bijgekomen is
**Aannemers in de import en de Uitvoerder-velden:** alleen bedrijven met een `Contract` in het project; v2-keuzelijst met zoekveld (`GlV2/_UitvoerderCombo`, `gl-v2-combo`).

**Projectsjabloon met AI-voorstel** (Projecten › Planning › Projectsjabloon): de AI kiest een sjabloon, vult bouwlagen, kelder, dak en terrassen in en toont aannames, vragen en de doorgerekende duur tegenover `Project.ExecutionDays`. Privacy: de beschrijving wordt gecontroleerd (geen e-mail, telefoon, ondernemingsnummer, adres, bedrag) en de AI krijgt enkel anonieme sjabloongegevens. Code: `ProjectSjabloonAiVerwerker` (zuiver, getest), `ProjectSjabloonAiService` (HTTP), endpoint `ProjectPlanningController.ProjectSjabloonAi`. Analyse en besluiten: `PLANNING_AI_ANALYSE.md`.

**Bouwlagen en naamgeving (Vlaams):** gelijkvloers, 1ste verdieping, 2de verdieping; de variabele `Verdiepingen` is het aantal **bouwlagen** (gelijkvloers meegeteld). `PlanningSjabloonUitrol.BouwlaagNaam`. Terrassen of luifels: `Terrassen` (lijst bouwlagen) met knoppen GV · 1 · 2 in de schermen.

**Sjabloonstructuur (blok = fase):**
- `PlanningSjabloonBlok.Herhaling`: 0 = niet herhaald, 1 = een fase per bouwlaag, **2 = één fase met per bouwlaag een deelfase** (`PerBouwlaagEenFase`; geen migratie, TINYINT). In de editor een keuzelijst "Herhaling per bouwlaag".
- Bij 2 maakt de uitrol één fase ("Ruwbouw - Bovenbouw") met per bouwlaag een **deelfase** (Gelijkvloers, 1ste verdieping, …, enkel bij meer dan één bouwlaag) via `UitgerolTaak.Subfase` en `PlanningSectie.ParentSectieId` (`PlanningService.SjabloonToepassen`). Taaknamen houden de bouwlaag ("Predallen 1ste verdieping").
- **Voorwaarde per taak** (`PlanningSjabloonTaak.Voorwaarde`, komma-gescheiden): `kelder:ja|nee`, `dak:hellend|plat`, `terras:ja|nee`, `laag:eerste|laatste|boven|eenmalig`, `grond:ja|nee` (gelijkvloers zonder kelder). `laag:eenmalig` = één keer na de bouwlagen (gevelmetselwerk, predallen/opstort van een plat dak).
- Voorganger **`VORIGE:CODE`** = dezelfde taak in de vorige bouwlaag. Een verwijzing naar een taak die op die bouwlaag niet bestaat valt weg.
- Het testsjabloon: Onderbouw (kelder, zonder afdek); Bovenbouw met per bouwlaag Predallen, Opstort, Metselwerk (zonder kelder is het gelijkvloers één "Vloerplaat"), Terrassen en luifels, bij plat dak Predallen dak en Opstort dak, en het gevelmetselwerk (40 werkdagen, **zonder voorganger**, dus start dag 1).

**Schijven:**
- Percentages van schijven met een gewicht gaan **in stappen van 5 %**, minstens 5 % per schijf (`SchijfSjabloonUitrol.Stap`); vaste percentages blijven zoals ingevoerd; te veel schijven geven een waarschuwing "onder het minimum". Spiegel in `gl-v2-instellingen-schijvensjabloon.js`.
- Nieuwe schijfvoorwaarde **`laag:boven`**: één schijf per bouwlaag boven het gelijkvloers, gewicht per bouwlaag. Het volgnummer in de planning is `kelder ? bouwlaag : bouwlaag - 1`, omdat de opstort met een kelder ook op het gelijkvloers staat en zonder kelder niet.
- Testschijven: "Opstort gelijkvloers" (kelder ja), "Gieten vloerplaat gelijkvloers" (kelder nee), "Opstort {verdieping}" (laag:boven). De schijf hangt automatisch aan de taak met dezelfde code (`PlanningSchijfAutoKoppeling`, in volgorde); handmatig in het paneel van de taak of fase onder "Koppeling met schijven".
- Betalingsgroepen met schijven blijven bij het toepassen van een projectsjabloon ongemoeid: wil je nieuwe schijven, verwijder dan eerst de oude (zolang er niets bereikt of gefactureerd is).

**Mijlpalen per woning:** een trajectmijlpaal van één woning koppelt enkel aan een taak of fase van de planning van die woning (`PlanningTrajectKoppeling.KoppelOpCode`, `PlanningService.Koppel`).

> **Let op (23/11 later):** de stappen in dit hoofdstuk zijn vervangen door het herziene ontwerp in `PLANNING_ONTWERP_HERZIEN.md` (bouwstaten, schijven bevestigen, onderdelen, poorten, marge, aannemer via discipline, AI). Lees dat eerst; onderstaande lijst blijft enkel als historiek.

## 3. Volgende stappen (voorstel)
1. **Commit en push** (zie hierboven), draai migratie 157 en de testscripts op de nieuwe pc.
2. **De echte AI-aanroep testen** (Anthropic-sleutel): voorbeeldbeschrijving, privacyweigering, een beschrijving waar geen sjabloon bij past ("kies zelf"). Controleer kosten en foutafhandeling.
3. **Voorstel per projectfase** (uit de analyse, besluit van Niels): traject bij het starten van het project, schijven bij de verkoop, planning bij de uitvoering; uitbreidbaar per fase.
4. **AI route B en C** uit `PLANNING_AI_ANALYSE.md`: aanpassingen op een bestaand sjabloon, en een leeg schema als inactief concept-sjabloon met "Bewaren als sjabloon" (de AI mag concept-sjablonen maken).
5. **Volledigheidscontrole en ontwikkelaarsregels** op een voorstel, discipline per sjabloontaak, normduren uit de historiek.
6. Het **gevelmetselwerk** heeft geen voorganger en start op dag 1. Beslis of dat zo blijft of dat het start samen met het metselwerk gelijkvloers (bv. SS-voorganger).
7. **Dak-keuzelijst** in het projectsjabloon-venster is nog een gewone `<select>` (hoort een v2-keuzelijst te zijn).
8. **Help-schermafbeeldingen** nemen in één batch (`CPMCore/HELP_SCREENSHOTS.md`: de sjablooneditor en het projectsjabloon-venster staan op "opnieuw nemen" of moeten er nog bij). De help voor schijven is niet bijgewerkt voor `laag:boven` en de stappen van 5 %; doe dat mee.
9. Uit de vorige stand nog open: weergave "Planning van [aannemer]" voor het portaal, Excel/CSV-import, import splitsen per hoofdfase.

## 4. Let op (valkuilen)
- Een planning die al toegepast is verandert niet mee met een gewijzigd sjabloon: pas het sjabloon opnieuw toe (met "Vervang").
- De schijvenvoorwaarde `laag:boven` en haar volgnummerregel veronderstellen dat de gekoppelde taak (de opstort) enkel op het gelijkvloers ontbreekt zonder kelder. Verandert die structuur, pas dan `SchijfSjabloonUitrol` aan.
- `SchijfSjabloonUitrol` (C#) en `gl-v2-instellingen-schijvensjabloon.js` rekenen hetzelfde: wijzig ze samen.
- Blok `Herhaling = 2` en taakvoorwaarden zitten ook in de AI-context (`GebruiktVariabele`): een sjabloon dat naar kelder, dak of terrassen vraagt, wordt zo herkend, ook via `grond:`.
- Nieuwe taakvoorwaarden en blokstanden moeten op vier plaatsen mee: `PlanningSjabloonUitrol` (uitrol), `PlanningSjabloonBewerkService` (validatie), `gl-v2-instellingen-planningsjabloon.js` (keuzes) en de help.
