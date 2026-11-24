# Verzekeringen (gl-v2, design-handoff punt 43) — voortgang en overdracht

Laatst bijgewerkt: 22/10/2026. Bron van het ontwerp: `design-handoff/CRM Verzekeringen.dc.html` (43a overzicht, 43b attesten aannemers, 43c verzekering toevoegen). Uitleg van de bouw: `CPMCore/DESIGN.md`, sectie "Projecten/DetailInsurancesV2 en AddInsuranceV2" (zoek op die titel, de regelnummers verschuiven).

## START HIER

**Stand: alle drie de schermen zijn gebouwd en de build is groen** (`dotnet build CPMCore/CPMCore.csproj`, enkel `error CS|RZ` tellen). **Niets is in de browser getest en de code is nooit tegen de database gedraaid**: er was geen databasetoegang in de sessie. Niets is gecommit.

1. **Migraties `_migrations/110_Verzekeringen.sql`, `111_VerzekeringenAttestEenhedenEnTienjarigeKeuze.sql` en `112_ProjectTienjarigeKeuze.sql` draaien, in die volgorde, VOOR de app start.** 111 voegt `Contract.TienjarigeBron` en de tabel `InsuranceAttestUnit` toe; zonder 111 falen alle pagina's die contracten laden. 112 voegt `Project.TienjarigeBron` en `Project.TienjarigeOpmerking` toe; zonder 112 falen alle pagina's die een project laden, dus ook het dashboard.
   Over 110: Ze maakt `Insurances.ContractActivityID` NULL-baar (de unieke index wordt een gefilterde unieke index), voegt kolommen toe aan `Insurances`, vult `ProjectId` voor bestaande polissen en maakt de tabel `InsuranceAttest`. De dev-app gebruikt dezelfde database als productie: zonder de migratie falen alle pagina's die verzekeringen laden (projectdetail, het legacy-scherm en de V2-pagina's), omdat de entiteit de nieuwe kolommen selecteert. De migratie zoekt de unieke index of constraint zelf op naam; controleer na afloop in SSMS dat `UQ_Insurances_ContractActivity` nu een gefilterde index is.
2. **Dan testen, in deze volgorde:**
   a. Het legacy scherm `Projecten/DetailInsurances` (V2-layout uit) en het projectdetail: ze moeten nog laden en de bestaande polissen tonen.
   b. V2-overzicht `Projecten/DetailInsurances`: totalen, aandachtspunten, polissentabel (zoeken, filters, `···`-menu), tijdlijn.
   c. Formulier: een ABR-polis met makelaar en een brandpolis voor nieuwbouw zonder makelaar (rechtstreeks), een polis "Bewaren als aangevraagd", bewerken, beëindigen met reden (moet als opgelost aandachtspunt terugkomen), verwijderen.
   d. Tabblad Attesten aannemers: selecteren, **Attesten opvragen**, E-mail openen, daarna de datum bij Laatst opgevraagd; **Vastleggen** van een tienjarige met woningen aangevinkt en een bestand (controleer in Documenten of het attest bij de woning en de koper hangt en gedeeld is), en een BA-attest.
   d2. Contractformulier (nieuw en bewerken, gl-v2): de keuze **Tienjarige aansprakelijkheid** opslaan, heropenen, en daarna het oude contractformulier opslaan: de keuze moet blijven staan. Kies Via projectpolis zonder polis: het aandachtspunt moet verschijnen.
   d3. Verkoop een woning nadat het attest is vastgelegd: het aandachtspunt "Tienjarige attesten bezorgen aan de kopers" met de knop **Aan kopers koppelen**.
   d3b. Een polis voor één eenheid (met document) en een polis voor het hele project (met document), dan een klant aan een eenheid zonder koper koppelen (klant toevoegen, eenheid bewerken, en een ondertekend verkoopcontract): in Documenten moet het document bij eenheid en klant hangen en gedeeld zijn, zonder de knop Aan kopers koppelen.
   d4. Dashboard van de projectleider: rode en gouden verzekeringspunten van projecten in uitvoering bij de meldingen, zonder ABR-dubbels.
   e. Een project met een definitief budget: het aandachtspunt over de verzekerde waarde van de ABR.
3. **Let op de bekende lay-outvallen** (CPMCore/DESIGN.md): veld zonder `.gl-v2-field-box`, `position: sticky` in een scrollcontainer, de actiebalk die niet onderaan hangt, en modals waarvan de keuzelijst of kalender wordt afgeknipt.

## Beslissingen die ik nam (pas aan als ze niet kloppen)
- **Eén tabel, geen aparte V2-tabel.** Polissen blijven in `Insurances`, zodat het legacy scherm, het projectdetail en de ABR-waarschuwingen op het dashboard (`InsuranceService.CheckInsurances`) hetzelfde blijven zien. Een polis zonder makelaar heeft geen contractactiviteit en bewaart zelf zijn project.
- **Makelaar blijft een contract met de activiteit 142 "Verzekeringen".** Kies je een makelaar, dan maakt CPM (zoals het legacy scherm) het contract en de activiteit aan. Zonder makelaar komt er niets in de nacalculatie.
- **Aandachtspunten worden afgeleid, niet bewaard.** Elke pagina-opening rekent ze opnieuw uit; het definitieve budget wordt daarvoor berekend (`BudgetBerekeningService.BerekenAsync`). Op een project met een groot budget kan dat het openen trager maken; cache het resultaat als dat hinderlijk blijkt.
- **"Tienjarige vereist" staat nu bij het contract** (Via de aannemer, Via projectpolis, Niet vereist, Nog niet bepaald). Bij Nog niet bepaald blijft CPM raden uit de namen van activiteitengroep en activiteit; bestaande contracten blijven zo ongewijzigd.
- **Attesten per woning, met koppeling aan de koper.** Bij het vastleggen vink je woningen aan; met een bestand koppelt CPM het aan woning en koper en deelt het meteen in het klantenportaal. Wie later koper wordt, krijgt het attest via de knop in het aandachtspunt.
- **Dashboard**: rood en goud van projecten in uitvoering, ingeslikte fouten, zonder budgetberekening.
- **Brand nieuwbouw alleen voor onverkochte eenheden** (23/10/2026): pas na de VO van het project, voor woningen en appartementen zonder koper en zonder eigen of globale brandpolis. De ABR-verlenging is een grijze mededeling.
- **Tienjarige-keuze voor het hele project** (23/10/2026): één keer bij Attesten aannemers kiezen (per aannemer of via een globale projectpolis, met opmerking); een contract met een eigen keuze wint.
- **Globale tienjarige polis ook bij woningen** (23/10/2026): één polis voor het hele project, of per eenheid een eigen polis (meerdere eenheden aanvinken maakt er één per eenheid). Contracten met Via projectpolis vragen geen attest van de aannemer.
- **Opvragen verstuurt niets.** E-mail openen legt de datum vast en opent het e-mailprogramma, zoals "Herinnering sturen" bij contracten.
- **Herinneringen zijn aandachtspunten, geen mails.**
- **Tijdlijn zonder bibliotheek**: balken met percentages van een jarenschaal (maximaal 8 jaar), tienjarige loopt met een pijl door.

## Wat Niels nog moet doen
1. Migratie 110 draaien en testen zoals hierboven.
2. **Help-afbeeldingen maken** voor de drie artikels (Playwright-recept in `CPMCore/DESIGN.md`): `wwwroot/img/help/projecten/verzekeringen-overzicht/` (1-totalen, 2-aandachtspunten, 3-polissen, 4-tijdlijn), `verzekering-toevoegen/` (1-type, 2-looptijd) en `attesten-aannemers-opvragen/` (1-tabel, 2-selectie, 3-vastleggen). De artikels verwijzen er al naar; een ontbrekende afbeelding toont de gestippelde placeholder.
3. **Herinneringen per mail** komen later (besluit Niels, 22/10/2026): nog te bouwen, vraagt een planner en de Graph-mailtoegang (nog niet ingesteld).
4. Beslis over wat bewust niet gebouwd is: een uploadlink voor aannemers, mails die CPM zelf verstuurt en een verzekerde-waardevoorstel per eenheid.

## Stand per scherm
| Punt | Scherm | Bestand | Stand |
|---|---|---|---|
| 43a | Overzicht met aandachtspunten, polissen, dekking in de tijd | `Views/Projecten/Insurances/DetailInsurancesV2.cshtml` | gebouwd, niet getest |
| 43b | Attesten aannemers | zelfde view, tab `?tab=attesten` | gebouwd, niet getest |
| 43c | Verzekering toevoegen en bewerken | `Views/Projecten/Insurances/AddInsuranceV2.cshtml` | gebouwd, niet getest |
| Help | 3 artikels | `CPMCore/HelpContent/projecten/` | geschreven, zonder afbeeldingen |

## Waar staat wat
- Service en berekeningen: `CPMCore/Services/Verzekeringen/VerzekeringService.cs`
- Viewmodels: `CPMCore/Models/Projecten/VerzekeringenV2Vm.cs`
- Controller: `CPMCore/Controllers/ProjectenController.Insurances.cs` (sectie gl-v2)
- CSS en JS: `CPMCore/wwwroot/css/gl-v2-projecten-insurances.css`, `CPMCore/wwwroot/js/gl-v2-projecten-insurances.js`
- Entiteiten: `DALCore/Models/Insurances.cs`, `DALCore/Models/InsuranceAttest.cs`, mapping in `cpmRunningContext.cs`
- Legacy aangepast (null-veilig): `ServiceCore/InsuranceService.cs`, `ServiceCore/Translators/InsuranceTranslator.cs`, `ServiceCore/ProjectService.cs` (`GetProjectInsurances`), enum `BOCore/Enum/InsuranceType.vb`
