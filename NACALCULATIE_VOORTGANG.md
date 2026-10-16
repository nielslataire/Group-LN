# Nacalculatie (gl-v2, design-handoff punt 42) — voortgang en overdracht

Laatst bijgewerkt: 16/10/2026. Bron van het ontwerp: `design-handoff/CRM Nacalculatie.dc.html` (42a–42g). Uitleg per scherm: `DESIGN.md`, secties "Projecten/RecalculationV2", "Projecten/RecalculationDetailV2" en "Projecten/AddIncommingInvoiceV2" (zoek op die titels, de regelnummers verschuiven).

## START HIER — overdracht naar een andere pc

**Stand: alle schermen van punt 42 zijn gebouwd en de build is groen** (`dotnet build CPMCore/CPMCore.csproj`, enkel `error CS|RZ` tellen). **Niets is in de browser getest.** Niets uit de sessie van 16/10 is gecommit: commit en push op de eerste pc, pull op de tweede.

1. **Eerst controleren (Niels):** zijn migraties `087_ContractBorgVrijgave.sql` en `088_IncommingInvoiceKind.sql` op de live database gedraaid? Borg vrijgeven en het factuurtype hebben ze nodig. Er is voor 42d geen migratie.
2. **Dan testen, in deze volgorde:** (a) Nacalculatie-overzicht 42a, (b) een categorie en activiteit 42b/42f, (c) Budget instellen 42d, (d) factuur toevoegen 42c incl. "Opslaan en nieuwe", (e) een bestaande factuur bewerken, (f) borg vrijgeven. Let op de bekende lay-outvallen: veld zonder `.gl-v2-field-box`, 4-koloms detailgrid in een smalle zijbalk, actiebalk die niet onderaan hangt.
3. **Open voor Niels:** het ontwerp voor borgen en inhoudingen is gemaakt (16/10) maar nog **niet verwerkt** in de design-handoff. Zodra het in `design-handoff/` staat, de borg-rij op `RecalculationDetailV2` ermee vergelijken en omzetten (zie "Open punten", punt 1).

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
  - Na feedback: "Activiteit toevoegen" is de keuzelijst met zoekveld en meerdere keuzes uit DESIGN.md (meer activiteiten in één keer toevoegen). Een categorie zonder budget toont het gele `is-warning`-kader i.p.v. gewone tekst.
  - Bewust anders dan het ontwerp: de bron-toggle "Definitief / Handmatig" is een label, geen keuze (een bewaarde exclusieve bron vraagt een extra kolom en migratie). "+ N activiteiten tonen" en een verwijderknop per rij zijn niet gebouwd.
- **Borg vrijgeven** (42e punt 04): rij onder de contractstrook op `RecalculationDetailV2`, een formulier per fase (voorlopige en definitieve oplevering: datum + bedrag, voorstel 50 % resp. het restant, "Wissen"). Actie `ReleaseContractGuaranteeV2` schrijft de vier `Contract.GuaranteeReleased*`-kolommen (migratie 087) en weigert meer dan ingehouden; borg wordt over het hele contract berekend. **Dit is mijn eigen voorstel: het ontwerp 42 toont de borg enkel (strook, KPI, contractkaart) en zegt alleen "vrijgave 50/50 bij VO en DO".** Niels heeft op 16/10 een ontwerp voor borgen en inhoudingen gemaakt, nog te verwerken; daarna deze rij omzetten.
- **Factuurformulier 42c** gecontroleerd tegen het ontwerp:
  - Fout opgelost: bij bewerken telde "Reeds gefactureerd" de factuur zelf mee (`GetContractInfoForInvoiceFormV2` krijgt nu `invoiceId`).
  - "Soort" heet nu "Type" en staat naast Factuurnummer.
  - Knop "Opslaan en nieuwe" (enkel bij toevoegen, `AddIncommingInvoice(..., saveAndNew)`, behoudt de herkomst via `TempData["KeepReferrer"]`).
  - Bewust niet gebouwd (besluit Niels): referentie/omschrijving, btw-veld en bedrag excl. btw, eenheid per factuurregel (enkel nodig voor een wijzigingsopdracht, bestaat al via de WO-kolom). De contractkeuze blijft de schakelaar Met/Zonder contract.

## Open punten
1. **Borgen en inhoudingen:** ontwerp van Niels bestaat (gemaakt 16/10) maar is **nog niet verwerkt** in `design-handoff/`. Volgende stappen: (a) ontwerp in `design-handoff/` zetten, (b) lezen wat het dekt (alleen borg vrijgeven, of ook andere inhoudingen), (c) vergelijken met wat er nu staat (borg-rij op `RecalculationDetailV2`, strook, KPI "Ingehouden borg", contractkaart 42c) en bepalen of er velden/migraties bij moeten (nu enkel de vier `Contract.GuaranteeReleased*`-kolommen uit migratie 087), (d) de rij omzetten of vervangen. "Inhoudingen" gaat mogelijk verder dan borg: niets bouwen voor het ontwerp gelezen is.
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
- Betaalstatus per factuur: Betaald, Vervallen of "Korting tot dd/MM", afgeleid van `StatusId`/`DueDate`.
- Nieuwe gedeelde componenten uit dit werk: `GlV2/_PillSelector` (+ `pills.css`, `GlV2NavCombo`, max 6 pills, daarboven een doorzoekbare combo). De naam `GlV2Combo` was al in gebruik voor het AJAX-zoekveld, daarom is de nav-variant hernoemd.
- 42c post naar de **bestaande** `AddIncommingInvoice`/`EditIncommingInvoice`-acties (zelfde model en validatie). "Factuur inlezen (PDF)" gebruikt `AddIncommingInvoiceV2ExtractPdf` (Azure Document Intelligence) en vult de leverancier bewust NIET automatisch in.

## Werkafspraken
Nederlandse UI-teksten; gl-v2-klassenprefix per pagina (`gln-nc-*`, `gln-ai-*`, `gln-cs-*`, `gln-rd-*`); page-CSS laadt vóór `gl-v2-shell.css` (extra specificiteit nodig); velden altijd `.gl-v2-field-box > .gl-v2-field-input`; `[hidden]` expliciet guarden. Keuzelijsten uit DESIGN.md hergebruiken (zoekende meerkeuze: markup van Leveranciers/Klanten, de JS is per pagina gekopieerd). Build: `dotnet build CPMCore/CPMCore.csproj`, enkel `error CS|RZ` tellen. Geheugennotities van Claude staan lokaal op de eerste pc: dit bestand is de bron.
