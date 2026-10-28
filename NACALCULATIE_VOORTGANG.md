# Nacalculatie (gl-v2, design-handoff punt 42) — voortgang en overdracht

Laatst bijgewerkt: 16/10/2026. Bron van het ontwerp: `design-handoff/CRM Nacalculatie.dc.html` (42a–42g). Uitleg per scherm: `CPMCore/DESIGN.md`, secties "Projecten/RecalculationV2", "Projecten/RecalculationDetailV2" en "Projecten/AddIncommingInvoiceV2" (zoek op die titels, de regelnummers verschuiven).

## START HIER — overdracht naar een andere pc

**Stand: alle schermen van punt 42 zijn gebouwd en de build is groen** (`dotnet build CPMCore/CPMCore.csproj`, enkel `error CS|RZ` tellen). **Niets is in de browser getest.** Niets uit de sessie van 16/10 is gecommit: commit en push op de eerste pc, pull op de tweede.

1. **Migraties 105, 106, 108 en 109 draaien VOOR je de app start** (`_migrations/105_BorgenEnInhoudingen.sql`, `106_BorgZonderContract.sql`, `108_BorgVrijgaveIngetrokken.sql`, `109_FactuurKortingContant.sql`, in die volgorde; 105 na 087). OPGELET: de dev-app gebruikt via de user-secrets dezelfde database als productie (`db_ab5fbb_testdb`), dus draai ze bewust: de code selecteert nu nieuwe kolommen op `Contract` en `IncommingInvoices`, dus zonder deze migraties falen álle pagina's die contracten of inkomende facturen laden. Controleer ook of 087 en 088 gedraaid zijn.
2. **Dan testen, in deze volgorde:** (a) Nacalculatie-overzicht 42a (knop "Borgen & inhoudingen"), (b) een categorie 42b/42f (strook en KPI borg), (c) Budget instellen 42d, (d) factuur toevoegen met en zonder contract: borg, extra inhouding, vervaldatum (42c/42h/42k), (e) een bestaande factuur bewerken (42l), (f) pagina Borgen: selecteren en vrijgeven, tab Extra inhoudingen en het afhandelvenster (42i/42j). Let op de bekende lay-outvallen: veld zonder `.gl-v2-field-box`, 4-koloms detailgrid in een smalle zijbalk, actiebalk die niet onderaan hangt.
3. **Wat je nog moet doen:** zie "Wat Niels nog moet doen" hieronder.

## Wat Niels nog moet doen (stand 22/10/2026)
**Eerst**
1. **Commit en push.** Er is niets gecommit (ruim 130 gewijzigde bestanden op de branch `layout-experiment`: nacalculatie, borgen, schakelaar, help, maar ook Punten en Budget van andere werkstromen). Beslis of alles in één commit gaat of per onderwerp.
2. **Migraties** 105, 106 en 108 zijn gedraaid op `db_ab5fbb_testdb` (gecontroleerd op 21/10). **Migratie 109 (korting contant) moet nog gedraaid worden voor je de app herstart**, anders falen alle factuurpagina's. Let op: de dev-app gebruikt via de user-secrets dezelfde database als productie. Zet je de nieuwe code live, dan hoeft er in de database niets meer te gebeuren; draai je ze op een andere database, doe dan 105, 106 en 108 in die volgorde (het nummer 107 is bezet door de budgetmigratie).
3. **Wissel het databasewachtwoord en de trigger-sleutel** van `CPMRUNNING`/`TriggerKeys` als je de sessie van 21/10 deelt: ze stonden per ongeluk in de uitvoer van een opdracht.

**Zelf in de browser testen** (niets daarvan is door mij op een toestel of met echte gegevens geprobeerd)
4. Factuur toevoegen met contract: bedrag typen (saldo, borg en kaart "Te betalen" rekenen mee), afwijkend borgpercentage met reden, extra inhouding, **Factuur opslaan** en **Opslaan en nieuwe**; daarna dezelfde factuur bewerken en opnieuw bewaren (vervaldatum, borg en inhouding blijven staan).
5. Factuur zonder contract, met borg aangezet, en kijken of ze als rij "Zonder contract" bij Borgen verschijnt.
6. Borgen: een of meer rijen vrijgeven (ook een kleiner bedrag), **Ongedaan maken** bij een vrijgave, en op het tabblad Extra inhoudingen afhandelen (vrijgeven, verrekenen met creditnota, rest afboeken) en een afhandeling ongedaan maken.
7. Budget instellen: bedrag aanpassen, activiteiten (meerdere) toevoegen, opslaan, en nagaan of de nacalculatie het aangepaste Begroot toont.
8. **Schakelaars na de omzetting** op de schermen die ik niet kon bekijken: het venster om een offerte om te zetten en de verzendmodals van wijzigingsopdracht en offerte (radio's); foto's en weerverlet zijn wel bekeken.
9. Contractformulier: het nieuwe veld **Borg vrij bij voorlopige oplevering**, ook bij een bankwaarborg (het percentage is dan invulbaar). Het legacy-contractformulier kent dat veld niet en leegt het bij opslaan; werk daar dus niet mee op contracten waar je 50 % wilt wijzigen.

**Testdata in project 79**
10. De voorbeelddata blijft staan tot je ze opruimt met `_migrations/testdata/TESTDATA_Borgen_Project79_Opruimen.sql` (zet ook de opleveringsdata en het budget van het project terug naar leeg). Maak voor je opruimt eventueel eerst een definitief budget aan in dat project als je het beeld van Budget instellen met de kolommen Definitief en Verschil wilt.

**Bewust nog niet gebouwd (beslis wanneer)**
11. **Vrijgave automatisch doorgeven aan de boekhouding** (nu op papier: hoeveel en welke facturen). Daarvoor moet een vrijgave weten bij welke facturen ze hoort, plus een overzicht of export.
12. **Betaalstatus** (42e punt 09): geen schrijfpad en geen betalingsmodule.
13. **42c-restjes in de detailpagina:** de uitklaplink "+ 5 facturen tonen" en de losse WO-kolom bij meerwerk-klant-rijen.
14. **42a:** de bron-pil "Vergeleken met [definitief budget]" boven het overzicht (nu enkel de hint bij Begroot).
15. De knop **Punten bekijken** bij Borgen opent de puntenlijst van het project zonder filter op de aannemer.
16. Beelden van het omzetten van een offerte en de verzendmodals bestaan niet; ze horen ook niet bij deze help.

## Stand per scherm
| Punt | Scherm | Bestand | Stand |
|---|---|---|---|
| 42a | Overzicht | `Views/Projecten/Contracts/RecalculationV2.cshtml` | gebouwd |
| 42b + 42f | Categorie/activiteit-detail met pills | `Views/Projecten/Contracts/RecalculationDetailV2.cshtml` | gebouwd |
| 42c | Factuur toevoegen / bewerken | `Views/Projecten/IncomingInvoices/AddIncommingInvoiceV2.cshtml`, `EditIncommingInvoiceV2.cshtml` | gebouwd, vier correctierondes op 11/10, afwijkingen besloten op 16/10 |
| 42d | Budget instellen | `Views/Projecten/Contracts/CalculationSettingsV2.cshtml`, `ProjectenController.CalculationSettingsV2.cs`, `Models/Projecten/CalculationSettingsV2Vm.cs`, `wwwroot/js|css/gl-v2-projecten-calculation-settings.*` | gebouwd 16/10 |
| 42e | "Wat ontbrak"-lijst | verspreid | 01, 02, 03, 05, 06, 07, 08 gebouwd; 09 deels; 04 is een voorstel van mij zonder ontwerp |

Legacy-schermen (`Recalculation`, `RecalculationDetail`, `AddIncommingInvoice`, `EditIncommingInvoice`, `CalculationSettings`) blijven werken: de V2-views worden alleen gekozen bij `ViewData["UseGlV2Layout"]`.

## Gedaan op 16/10/2026
- **42d Budget instellen** gebouwd. Keuze van Niels: **overschrijven per activiteit**, geen migratie. De kolom Budget start met het definitieve bedrag (som van de lijnen per activiteit). Wat afwijkt wordt bewaard als handmatige `ProjectBudget`-regel. Een rij die weer gelijk is aan definitief wordt verwijderd. `GetBegrootRegelsV2` laat een handmatige regel het definitieve bedrag per activiteit vervangen en voegt activiteiten toe die niet in het definitief staan, dus 42a en 42b tonen meteen de aangepaste Begroot (hint "· N aangepast"). Opslaan = `CalculationSettingsV2Post` (schrijfrecht `ProjectsPostCalculation`, het definitieve bedrag wordt server-side herberekend).
  - Na feedback: "Activiteit toevoegen" is de keuzelijst met zoekveld en meerdere keuzes uit CPMCore/DESIGN.md (meer activiteiten in één keer toevoegen). Een categorie zonder budget toont het gele `is-warning`-kader i.p.v. gewone tekst.
  - Bewust anders dan het ontwerp: de bron-toggle "Definitief / Handmatig" is een label, geen keuze (een bewaarde exclusieve bron vraagt een extra kolom en migratie). "+ N activiteiten tonen" en een verwijderknop per rij zijn niet gebouwd.
- **Borg vrijgeven** (42e punt 04, inmiddels vervangen door het Borgen-scherm, zie hieronder): rij onder de contractstrook op `RecalculationDetailV2`, een formulier per fase (voorlopige en definitieve oplevering: datum + bedrag, voorstel 50 % resp. het restant, "Wissen"). Actie `ReleaseContractGuaranteeV2` schrijft de vier `Contract.GuaranteeReleased*`-kolommen (migratie 087) en weigert meer dan ingehouden; borg wordt over het hele contract berekend. **Dit is mijn eigen voorstel: het ontwerp 42 toont de borg enkel (strook, KPI, contractkaart) en zegt alleen "vrijgave 50/50 bij VO en DO".** Niels heeft op 16/10 een ontwerp voor borgen en inhoudingen gemaakt, nog te verwerken; daarna deze rij omzetten.
- **Factuurformulier 42c** gecontroleerd tegen het ontwerp:
  - Fout opgelost: bij bewerken telde "Reeds gefactureerd" de factuur zelf mee (`GetContractInfoForInvoiceFormV2` krijgt nu `invoiceId`).
  - "Soort" heet nu "Type" en staat naast Factuurnummer.
  - Knop "Opslaan en nieuwe" (enkel bij toevoegen, `AddIncommingInvoice(..., saveAndNew)`, behoudt de herkomst via `TempData["KeepReferrer"]`).
  - Bewust niet gebouwd (besluit Niels): referentie/omschrijving, btw-veld en bedrag excl. btw, eenheid per factuurregel (enkel nodig voor een wijzigingsopdracht, bestaat al via de WO-kolom). De contractkeuze blijft de schakelaar Met/Zonder contract.

## Borgen en inhoudingen — gebouwd 21/10/2026 (design-handoff 42h–42l)
Het ontwerp (42a-aanpassing, 42c-uitbreiding, 42h, 42i, 42j, 42k, 42l) is verwerkt. Eerdere keuzes van Niels: vrijgaveregel als **veld op het contract**, vrijgaves in een **nieuwe tabel**, bankwaarborg = **percentage x contractbedrag**, vrijgeven **enkel registreren** (de vrijgavebrief als pdf later).

**Hoe borg en inhouding werken (zo gebouwd)**
- *Financiële waarborg* (borg via inhouding): op elke factuur met contract staat "Borg inhouden" standaard aan met het contractpercentage; borg = % x (factuurbedrag − extra inhouding). Afwijken kan met verplichte reden (server controleert). Zonder contract staat het standaard uit en kan je het zelf aanzetten, met zelf te kiezen vrijgave (100 % VO / 50-50 / 100 % DO, kolom `BorgVoPercentage`).
- *Bankwaarborg*: niets in te houden op de factuur; bedrag = percentage x contractbedrag; status "Bankwaarborg", vrijgave via brief bij DO (niet in de app).
- *Extra inhouding*: een bedrag dat tijdelijk niet wordt uitbetaald (reden: creditnota te ontvangen, werk niet volledig uitgevoerd, andere). Geen borg op dat bedrag. Afhandelen: **vrijgeven** (daarbij blijft het contractpercentage als borg ingehouden), **verrekenen met een creditnota** (deels kan; niets betaald, niets naar de borg; de rest vrijgeven, open laten of afboeken) of afboeken. Zodra er iets afgehandeld is, zijn bedrag en reden vergrendeld.
- *Vrij te geven*: borg x (VO bereikt ? VO-% : 0) + (DO bereikt ? rest : 0) − al vrijgegeven; VO/DO komen uit `Project.DeliveryDate` / `DeliveryDateDef`. Open punten van de aannemer (open werfpunten, `ConstructionIssue.ResponsiblePartyId` = bedrijf) blokkeren niet, worden wel getoond.
- Facturen van vóór deze module (`BorgInhouden` NULL) rekenen nog met het contractpercentage, zoals voorheen.

**Waar het staat**
- Migratie `_migrations/106_BorgZonderContract.sql`: `ContractBorgVrijgave.ContractId` nullable + `CompanyId` + `ProjectId`, voor borg zonder contract.
- Migratie `_migrations/105_BorgenEnInhoudingen.sql`: `Contract.GuaranteeReleaseVOPercentage`; `IncommingInvoices.BorgInhouden/BorgPercentage/BorgBedrag/BorgAfwijkReden/BorgVoPercentage`; tabellen `IncommingInvoiceInhouding`, `IncommingInvoiceInhoudingAfhandeling`, `ContractBorgVrijgave` (neemt de vrijgaves van migratie 087 over; die 4 kolommen worden niet meer gelezen).
- Entiteiten `DALCore/Models/Borg.cs` + `cpmRunningContext.Borg.cs`; BO-velden op `IncommingInvoiceBO` (`FormV2`-vlag: alleen het gl-v2-formulier schrijft borg, inhouding en vervaldatum weg) en `ContractBO.GuaranteeReleaseVOPercentage`.
- Alle berekeningen: `CPMCore/Services/Borg/BorgService.cs` (overzicht, inhoudingen, afhandelen, vrijgeven). Controller `ProjectenController.Borgen.cs` (`Borgen`, `BorgenVrijgeven`, `InhoudingAfhandelen`, `InhoudingCreditnotas`); validatie op de factuur in `ValidateBorgInhoudingV2`.
- Schermen: `Views/Projecten/Contracts/BorgenV2.cshtml` (42i/42j), `Views/Projecten/Partials/_InhoudingAfhandelenV2.cshtml` (venster rechts, ook op factuur bewerken, 42l), `Views/Projecten/Partials/_IncommingInvoiceBorgV2.cshtml` (borg + extra inhouding in het factuurformulier). JS `gl-v2-projecten-borgen.js`, `gl-v2-projecten-inhouding.js`, borgblok in `gl-v2-projecten-incommininvoice-add.js`; CSS `gl-v2-projecten-borgen.css`, `gl-v2-projecten-inhouding.css`.
- 42a: knop "Borgen & inhoudingen" met badge "N vrij te geven". 42b: BORG-cel in de strook en de KPI "Ingehouden borg" komen nu uit `BorgService` (hele contract). Mijn eerdere vrijgave-rij op de detailpagina en `ReleaseContractGuaranteeV2` zijn verwijderd.
- Contractformulier (Add/Edit V2): nieuw veld "Borg vrij bij voorlopige oplevering (%)" (leeg = 50); het percentage is nu ook bij een bankwaarborg invulbaar.
- Vervaldatum (42k): echt datumveld dat gepost wordt; met contract berekend en alleen-lezen, zonder contract of zonder betaaltermijn zelf in te vullen; een bestaande waarde blijft staan tot je datum of contract wijzigt. Dit is het eerste schrijfpad voor `DueDate` (enkel vanuit het gl-v2-formulier).

**Bewust niet / afwijkend**
- Vrijgavebrief (pdf): niet gebouwd (besluit Niels). "Punten bekijken" gaat naar de puntenlijst van het project, zonder filter op de aannemer.
- "Open borgen" (KPI) = wat nog vastzit (ingehouden − vrijgegeven); het ontwerp telt de ingehouden bedragen op en zou dus ook al vrijgegeven delen meetellen.
- Het ontwerp is op twee plaatsen intern tegenstrijdig (borg € 16.819 in de KPI vs € 16.980 in de strook; € 1.371,73 in het formulier vs € 1.471,73 in de contractkaart). Overal wordt hetzelfde berekend: % x (factuur − extra inhouding); de contractkaart volgt het formulier.
- Borg op facturen **zonder contract** staat in het overzicht als een rij per leverancier met "Zonder contract" (vrijgaveregel per factuur gekozen; zijn die verschillend, dan staat er "per factuur gekozen"). Geen percentage in die rij.

## Beslissingen over borgen en inhoudingen (21/10/2026)
1. Borg zonder contract staat in het overzicht als een rij per leverancier met "Zonder contract" (migratie 106).
2. Vrijgeven mag een kleiner bedrag dan wat vrij te geven is: bij een aangevinkte rij verschijnt een bedragveld (standaard het volledige bedrag, nooit meer dan vrij te geven). Een deelvrijgave krijgt in `ContractBorgVrijgave` fase 3 ("andere"), een volledige volgt de oplevering (VO of DO).
3. Geen vrijgavebrief (pdf).
4. De tegenstrijdige cijfers in het ontwerp (borg € 16.819 / € 16.980, € 1.371,73 / € 1.471,73) blijven zoals gebouwd: overal % x (factuur − extra inhouding).
5. "Open borgen" (KPI) = wat nog vastzit (ingehouden − vrijgegeven).

## Open punten
1. **Borgen en inhoudingen:** gebouwd, nog niet getest (migraties 105 en 106 eerst draaien).
2. **Betaalstatus (42e punt 09):** afgeleid van `StatusId`/`DueDate`, geen schrijfpad en geen betalingsmodule. Beslissen of dat er komt.
3. **42c-restjes in de detailpagina:** de uitklaplink "+ 5 facturen tonen" (nu worden alle rijen getoond) en de losse WO-kolom bij meerwerk-klant-rijen.
4. **42a:** de bron-pil "Vergeleken met [definitief budget]" uit het ontwerp ontbreekt, enkel de hint bij de Begroot-KPI.
5. **Browsertest** van alles (zie "Start hier").
6. **Eventueel projectbreed maken:** de gesplitste voortgangsbalk (twee fills naast elkaar) in de 42c-contractkaart bestaat enkel page-scoped.

## Beslissingen en afspraken
- Sommen en statuslogica zijn 1-op-1 uit het legacy-scherm overgenomen en vooraf berekend in `ProjectenController.Contracts` (`BuildRecalculationIndexV2Vm`, `BuildRecalculationDetailV2Vm`, gedeelde `RecalcContractSom/FactuurSom/BudgetSom/Geld/BuildCategorieenV2`).
- Route van `RecalculationDetail(projectId, activityId, groupid)` is ongewijzigd. In de V2-tak betekent `activityId == 0` "alle activiteiten van de categorie".
- Op 10/10 beslist: ook de 42e-lijst wordt meegebouwd, met waar nodig **additieve** migraties (hand-gedraaide SQL in `_migrations/`, nooit een bestaande kolom of BO-eigenschap wijzigen).
- Niets verzinnen wat niet in de data bestaat: contractnummer, ondertekend-datum, "alle loten", actief/inactief, prijsherzieningsformule, BTW-regimetekst, de borg-50/50-splitsing en de "LOT"-kolom zijn bewust weggelaten.
- Begroot komt uit het **definitieve budget** (`BudgetVersie.Status == "Definitief"`, via `GetBegrootRegelsV2` en `BudgetActivityService.GetLotGroepenAsync`), met handmatige regels als overschrijving per activiteit (zie 42d). Zonder definitief budget geldt het oude `ProjectContractsModel.BudgetActivities`.
- Badge-prioriteit per categorie: Niet begroot, dan Zonder contract, dan Overschrijding, anders geen badge. Een lege categorie krijgt geen badge en wordt gedimd.
- Voortgangsbalk = gefactureerd / gecontracteerd. Contractstrook: Gefactureerd = Contract + Meerwerk samen, Saldo = Contract − Gefactureerd, Borg = Gefactureerd × waarborg% (enkel bij financiële waarborg).
- Borgvrijgaves staan in `ContractBorgVrijgave` (meerdere per contract); de vier kolommen van migratie 087 zijn verouderd.
- Betaalstatus per factuur: Betaald, Vervallen of "Korting tot dd/MM", afgeleid van `StatusId`/`DueDate`.
- Nieuwe gedeelde componenten uit dit werk: `GlV2/_PillSelector` (+ `pills.css`, `GlV2NavCombo`, max 6 pills, daarboven een doorzoekbare combo). De naam `GlV2Combo` was al in gebruik voor het AJAX-zoekveld, daarom is de nav-variant hernoemd.
- 42c post naar de **bestaande** `AddIncommingInvoice`/`EditIncommingInvoice`-acties (zelfde model en validatie). "Factuur inlezen (PDF)" gebruikt `AddIncommingInvoiceV2ExtractPdf` (Azure Document Intelligence) en vult de leverancier bewust NIET automatisch in.

## Werkafspraken
Nederlandse UI-teksten; gl-v2-klassenprefix per pagina (`gln-nc-*`, `gln-ai-*`, `gln-cs-*`, `gln-rd-*`); page-CSS laadt vóór `gl-v2-shell.css` (extra specificiteit nodig); velden altijd `.gl-v2-field-box > .gl-v2-field-input`; `[hidden]` expliciet guarden. Keuzelijsten uit CPMCore/DESIGN.md hergebruiken (zoekende meerkeuze: markup van Leveranciers/Klanten, de JS is per pagina gekopieerd). Build: `dotnet build CPMCore/CPMCore.csproj`, enkel `error CS|RZ` tellen. Geheugennotities van Claude staan lokaal op de eerste pc: dit bestand is de bron.

## Correctie van een foute vrijgave (21/10/2026)
Een foute borgvrijgave en een foute afhandeling van een extra inhouding kunnen ongedaan gemaakt worden (migratie 108: `IngetrokkenOp/IngetrokkenDoor`). De rij blijft als spoor staan en telt niet meer mee (EF query filter op beide entiteiten). Borgen: klik in de rij op "N vrijgaves", dan **Ongedaan maken** (`BorgVrijgaveIntrekken`). Factuur bewerken: **Ongedaan maken** bij een stap in de historiek (`InhoudingAfhandelingIntrekken`). Daarna is een inhouding weer te bewerken en een creditnota weer vrij. Bevestiging met een gewone browserbevestiging.

## Later: vrijgave automatisch doorgeven aan de boekhouding
Nu geeft Niels de vrijgave (hoeveel en voor welke facturen) op papier door aan de boekhouding. Dat moet later automatisch. Nog niets gebouwd: de vrijgave kent het bedrag per contract, maar niet welke facturen erbij horen. Daarvoor is een koppeling per factuur nodig (en een overzicht of export voor de boekhouding).

## Help (21/10/2026)
Zes artikels in `CPMCore/HelpContent/projecten/`: `nacalculatie-lezen`, `budget-instellen-nacalculatie`, `factuur-van-leverancier-toevoegen`, `borg-op-een-factuur`, `extra-inhouding-op-een-factuur`, `borgen-vrijgeven`; `project-leverancier-toevoegen` is aangevuld (borgregel; het waarborgdocument hoort bij een bankwaarborg, de oude tekst zei financiële waarborg). Screen-id's `nacalculatie-overzicht|detail|budget|factuur|borgen` staan op de V2-pagina's. De `[VRAAG]`-regels zijn beantwoord. Schermafbeeldingen gemaakt op 21/10/2026 (28 beelden in `CPMCore/wwwroot/img/help/projecten/<artikel>/`, bedrijfsnamen vervangen door fictieve namen, gecontroleerd en in de artikels ingevoegd; alle zes renderen in de app). Ze komen uit project 79 met `TESTDATA_Borgen_Project79.sql` (opruimen met het `_Opruimen`-script). Beperking: project 79 heeft geen definitief budget, dus Budget instellen toont de kolommen Definitief en Verschil niet (de tekst zegt dat). Opnieuw maken: eigen Playwright-script in de scratchpad volgens het recept in CPMCore/DESIGN.md; typ bedragen echt (geldmasker) in plaats van `fill()`.

## Kaart "Te betalen" en korting contant (22/10/2026, design-handoff 42c)
Onder de contractkaart in het factuurformulier staat nu de kaart "Te betalen" (alleen bij een factuur met contract): factuurbedrag, extra inhouding, borg, te betalen, en bij een korting contant het aantal dagen, het kortingsbedrag, de uiterste betaaldatum en "Te betalen met korting". De korting is het percentage van het factuurbedrag excl. btw (2 % van € 29.434,56 = € 588,69, zoals in het ontwerp); is de termijn verstreken, dan telt de korting niet meer mee. De korting in de contractkaart zelf rekent nu ook op het factuurbedrag (vroeger op de factuurregels, en die lazen nooit een bedrag). Code: markup in `Add|EditIncommingInvoiceV2.cshtml`, berekening in `refreshBorg()` in `gl-v2-projecten-incommininvoice-add.js`, stijl `.gln-ai-pay-*`. Opmaak overgenomen uit het ontwerp (22/10): de borg staat in een oranje waarschuwingskader (pil "Borg 5 %", vrijgaveregel, bedrag; tokens `--gl-v2-warning` en `--gl-v2-warning-tint`), "Te betalen" heeft een dikke lijn erboven en een groot groen bedrag, en de korting contant staat in een groen infokader met een witte rij (bedrag en uiterste datum) en de pil "nog N dagen". Het beeld `6-te-betalen` in de help moet na een herstart opnieuw gemaakt worden. Razor-wijziging: app herstarten. Help bijgewerkt (`factuur-van-leverancier-toevoegen`, `borg-op-een-factuur`); het beeld van de kaart is gemaakt en in het artikel gezet (`6-te-betalen`, cijfers gelijk aan het ontwerp) en de andere factuurbeelden zijn vernieuwd.

## Gevonden en opgelost tijdens het maken van de beelden (21/10/2026)
- Het borgbedrag in het factuurformulier bleef 0,00 zolang er geen factuurregels waren (de borg volgt nu het factuurbedrag).
- **Oude fout uit 42c:** de som van de factuurregels ("N regels = € …", de contractkaart "deze factuur" en het saldo) las nooit een bedrag, omdat de selector het omhullende vakje i.p.v. het invoerveld pakte. De controle en het saldo werkten dus niet. Opgelost in `gl-v2-projecten-incommininvoice-add.js`.
- De app die draait blokkeert de dll's: een `dotnet build` terwijl de app draait compileert wel, maar levert de nieuwe build niet af. Herstart de app na wijzigingen aan C# of Razor; js en css laden live.

## Testdata voor de helpbeelden (21/10/2026)
Project 79 ("TEST Financieel - Facturatie"): `_migrations/testdata/TESTDATA_Borgen_Project79.sql` zet voorbeelddata klaar (4 contracten, facturen met borg, een eindvordering met extra inhouding en creditnota's, een deelvrijgave, borg zonder contract, 3 open punten, handmatig budget, opleveringsdata). Alles is gemarkeerd; `TESTDATA_Borgen_Project79_Opruimen.sql` haalt het weer weg. Niels draait het script zelf in SSMS (rechtstreeks schrijven naar de database vanuit de sessie werd geweigerd). Daarna: app starten, Niels logt in, schermafbeeldingen maken volgens het recept in CPMCore/DESIGN.md. Migratie `107_BorgVrijgaveIngetrokken` is hernoemd naar `108_BorgVrijgaveIngetrokken`, omdat 107 al bezet was door de budgetmigratie; de inhoud is gelijk en idempotent.
- 22/10: de schakelaars Contract en Type in het factuurformulier staan nu even ver onder hun label als de tekstvelden (5px, `.gl-v2-field-label + .gln-ai-segmented`); enkel css, geen herstart nodig.
- 22/10: schakelaar (segmented control) projectbreed gemaakt: `css/gl-v2/segmented.css` + partial `GlV2/_Segmented` + sectie in CPMCore/DESIGN.md. Overgezet: factuurformulier, afhandelvenster, omzetten offerte, verzendmodals (WO en offerte), foto's en weerverlet (eigen klassen blijven bestaan voor hun JS). Niet overgezet: de oudere `.gl-segmented` (traject) en `.btn-group-segmented` (marktanalyse). Razor-wijzigingen: app herstarten en elk van die schermen even bekijken.
- 22/10: de contractkaart in het factuurformulier volgt nu het ingevulde Factuurbedrag: "deze" (balk, percentage) en het saldo na deze factuur veranderen terwijl je typt, ook zonder factuurregels (zoals in het ontwerp: deze € 29.435 = het hele factuurbedrag). Een creditnota telt negatief en het saldo springt mee als je het type wisselt. Voorheen telde alleen de som van de regels. Alleen js, geen herstart nodig.
- 22/10: de kaart "Te betalen" staat nu ook bij een factuur zonder contract (onder het infokader), met borg, extra inhouding en een zelf in te vullen **Korting contant** (percentage en aantal dagen, een derde schakelrij onder de borg die enkel zonder contract verschijnt). Die korting wordt bewaard bij de factuur (migratie 109: `IncommingInvoices.KortingPercentage` en `KortingDagen`, enkel zonder contract; met contract komt ze uit het contract en wordt een eigen korting gewist). De melding "Factuur zonder contract" heeft de tekst uit ontwerp 42k. Help bijgewerkt; de beelden van de kaart zonder contract moeten nog gemaakt worden na een herstart (Razor-wijziging).
- 22/10: **fout bij bewerken van een factuur opgelost** (gevonden bij het testen van de korting): de bedragen van de factuurregels kwamen leeg uit de server, omdat `IncommingInvoiceDetailBO.Price` een valutaformaat heeft (`"€ 26.224,92"`) dat het geldmasker niet leest. Daardoor klopte de som van de regels niet meer met het factuurbedrag en kon je een bestaande factuur niet meer bewaren. De rij zet het bedrag nu als kaal getal (`_IncommingInvoiceDetailRowV2.cshtml`). Razor-wijziging: herstart nodig; test daarna een bestaande factuur bewaren (bewerken, opslaan, heropenen).
