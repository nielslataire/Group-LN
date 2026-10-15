# Nacalculatie (gl-v2, design-handoff punt 42) — voortgang en overdracht

Laatst bijgewerkt: 14/10/2026. Bron van het ontwerp: design-handoff punt 42a–42f. Uitleg per scherm: `DESIGN.md`, secties "Projecten/RecalculationV2", "Projecten/RecalculationDetailV2" en "Projecten/AddIncommingInvoiceV2" (zoek op die titels, de regelnummers verschuiven).

## Stand in één oogopslag
| Punt | Scherm | Bestand | Stand |
|---|---|---|---|
| 42a | Nacalculatie, overzicht | `Views/Projecten/Contracts/RecalculationV2.cshtml` | gebouwd |
| 42b + 42f | Categorie-/activiteit-detail (met activiteit-pills) | `Views/Projecten/Contracts/RecalculationDetailV2.cshtml` | gebouwd |
| 42c | Factuur toevoegen | `Views/Projecten/IncomingInvoices/AddIncommingInvoiceV2.cshtml` | gebouwd, vier correctierondes op 11/10 |
| 42d | Budget instellen (bron definitief budget of handmatig, verschilkolom) | — | **nog te bouwen** |
| 42e | "Wat ontbrak"-lijst | verspreid | grotendeels gebouwd, zie hieronder |

Niets is in de browser getest door Niels (volgens de DESIGN.md-teksten). Legacy-schermen (`Recalculation`, `RecalculationDetail`, `AddIncommingInvoice`, `CalculationSettings`) zijn ongewijzigd en blijven werken.

## Beslissingen en afspraken
- Sommen en statuslogica zijn 1-op-1 uit het legacy-scherm overgenomen en vooraf berekend in `ProjectenController.Contracts` (`BuildRecalculationIndexV2Vm`, `BuildRecalculationDetailV2Vm`, gedeelde `RecalcContractSom/FactuurSom/BudgetSom/Geld/BuildCategorieenV2`).
- Route van `RecalculationDetail(projectId, activityId, groupid)` is ongewijzigd. In de V2-tak betekent `activityId == 0` "alle activiteiten van de categorie".
- Op 10/10 beslist (op verzoek van Niels): ook de 42e-lijst wordt meegebouwd, met waar nodig **additieve** migraties (hand-gedraaide SQL in `_migrations/`, nooit een bestaande kolom of BO-eigenschap wijzigen).
- Niets verzinnen wat niet in de data bestaat: contractnummer, ondertekend-datum, "alle loten", actief/inactief, prijsherzieningsformule, BTW-regimetekst, de borg-50/50-splitsing en de "LOT"-kolom zijn bewust weggelaten.
- Begroot komt uit het **definitieve budget** (`BudgetVersie.Status == "Definitief"`, via `GetBegrootRegelsV2` en `BudgetActivityService.GetLotGroepenAsync`). Zonder definitief budget valt het terug op het oude `ProjectContractsModel.BudgetActivities`. Die twee bronnen blijven gescheiden datamodellen.
- Badge-prioriteit per categorie: Niet begroot, dan Zonder contract, dan Overschrijding, anders geen badge. Een lege categorie krijgt geen badge en wordt gedimd.
- Voortgangsbalk = gefactureerd / gecontracteerd (niet t.o.v. begroot).
- Contractstrook per leverancier: Gefactureerd = Contract + Meerwerk samen, Saldo = Contract − Gefactureerd, Borg = Gefactureerd × waarborg% (enkel bij financiële waarborg).
- Betaalstatus per factuur is afgeleid van `StatusId`/`DueDate` (Betaald, Vervallen, "Korting tot dd/MM"). Er is **geen schrijfpad** voor `DueDate`/`StatusId` en geen echte betalingsmodule.
- Nieuwe gedeelde componenten uit dit werk: `GlV2/_PillSelector` (+ `pills.css`, `GlV2NavCombo`, max 6 pills, daarboven een doorzoekbare combo). De oude naam `GlV2Combo` was al in gebruik voor het AJAX-zoekveld, daarom is de nav-variant hernoemd.
- 42c: de view post naar de **bestaande** `AddIncommingInvoice`-actie (zelfde model en validatie). De actie kiest enkel de V2-view op `ViewData["UseGlV2Layout"]`. "Factuur inlezen (PDF)" gebruikt `AddIncommingInvoiceV2ExtractPdf` (Azure Document Intelligence) en vult de leverancier bewust NIET automatisch in.

## Migraties
- `_migrations/087_ContractBorgVrijgave.sql` (velden voor borg vrijgeven) en `_migrations/088_IncommingInvoiceKind.sql` (soort Vordering/Eindvordering/Creditnota): controleer of Niels die op de live database gedraaid heeft vóór gebruik. In deze sessie kon ik dat niet nagaan.

## Nog te doen
1. **42d, Budget instellen:** bron definitief budget vs. handmatig, plus verschilkolom. Bij het overzicht (42a) staat ook de bron-pil "Vergeleken met [definitief budget]" uit het ontwerp nog niet, enkel de hint bij de Begroot-KPI.
2. **42e punt 04, borg vrijgeven:** de knoppen in de UI. De velden en migratie 087 bestaan al.
3. **42e punt 09, betaalstatus:** echte koppeling bestaat niet. Beslissen of er een schrijfpad voor `StatusId`/`DueDate` komt.
4. **Edit-pagina:** `EditIncommingInvoice` heeft nog geen V2-tegenhanger.
5. **42c-restjes in de detailpagina:** de uitklaplink "+ 5 facturen tonen" (nu worden alle rijen getoond) en de losse WO-kolom bij meerwerk-klant-rijen.
6. **Browsertest** van 42a, 42b/42f en 42c: eerst één project met echte data van overzicht tot factuur toevoegen doorlopen. Let op de geschiedenis van lay-outfouten (veld zonder `.gl-v2-field-box`, 4-koloms detailgrid in een smalle zijbalk, actiebalk die niet onderaan hangt).
7. **Eventueel projectbreed maken:** de gesplitste voortgangsbalk (twee fills naast elkaar) in de 42c-contractkaart bestaat enkel page-scoped.

## Werkafspraken
Nederlandse UI-teksten; gl-v2-klassenprefix per pagina (`gln-nc-*`, `gln-ai-*`); page-CSS laadt vóór `gl-v2-shell.css` (extra specificiteit nodig); velden altijd `.gl-v2-field-box > .gl-v2-field-input`; `[hidden]` expliciet guarden. Build: `dotnet build CPMCore/CPMCore.csproj`, enkel `error CS|RZ` tellen.
