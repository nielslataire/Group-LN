# Help-dekking van de gl-v2-pagina's (audit 26/10/2026)

Gemaakt door de code te lezen: elke `View("…V2")`, `glV2 ? "…V2"`, `ViewJson("…V2")` en de `ViewData["HelpScreenId"]`-plekken
in controllers en views, gekruist met de `screens:`-velden van `CPMCore/HelpContent/**/*.md`.

## Samenvatting

| Categorie | Aantal pagina's | Betekenis |
|---|---|---|
| A | 74 | id gezet en artikel bestaat |
| B | 10 (nu bedraad) | artikel bestond, pagina zette het id niet; vandaag bedraad |
| C | 30 | geen id en geen artikel: **ontbrekend artikel** |
| D | 0 | id gezet zonder artikel (dode id) |
| Totaal | 114 | pagina's (volledige gl-v2-views; zonder partials, modals, Help, Ondertekenen, Werfportaal) |

Dode artikels (`screens:` door geen enkele pagina gezet): **0** na de bedrading. Daarvoor waren er twee probleemgevallen (zie "Omgekeerde controle").
Verzonnen/dode ids in code: 0. Alle 111 artikels met `screens:` worden nu door minstens één pagina gezet.

## Bedrading van vandaag (categorie B en herstel)

| Bestand | Actie / view | Id |
|---|---|---|
| `Controllers/ProjectIssuesController.cs` | `Details` (DetailsV2) | `issues-index` |
| `Controllers/ProjectIssuesController.V2.cs` | `PlanV2`, `SendV2` | `issues-index` |
| `Controllers/ProjectenController.IncomingInvoices.cs` | `IncommingInvoiceDetail` | `nacalculatie-factuur` |
| `Controllers/ProjectenController.ChangeOrdersV2.cs` | `ChangeOrdersV2` (lijst) | `wijzigingen-opmaken` (nieuw) |
| `Controllers/ProjectenController.ChangeOrderDetailV2.cs` | `ChangeOrderDetailV2` | `wijzigingen-opmaken` |
| `Controllers/ProjectenController.QuoteIntakeV2.cs` | `QuoteIntakeV2` | `wijzigingen-opmaken` |
| `Controllers/ProjectenController.ChangeOrderSign.cs` | `ChangeOrderSign` | `wijzigingen-opmaken` |
| `Controllers/InstellingenController.Budget.cs` | `BudgetReferentieProjectImportExcel` (ImportControleV2) | `instellingen-referentieprojecten` |
| `Controllers/ProjectenController.Units.cs` | `Landshares` | `projecten-grondaandelen` (nieuw) |
| `HelpContent/facturatie-wijzigingen/wijzigingsopdracht-opmaken.md` | frontmatter | `screens: invoices-index` -> `wijzigingen-opmaken` |
| `HelpContent/projecten/grondaandelen-verdelen.md` | frontmatter | `screens: projecten-grondaandelen` toegevoegd (had geen `screens:`) |

Herdisplay-paden (POST met validatiefout) die het id verloren, nu ook bedraad: `Projecten/Index` via `ProjectsByUserId` (`projecten-algemeen`),
`Toevoegen` POST (`projecten-toevoegen`), `Edit` POST x2 (`projecten-bewerken`), `AddContract` POST x3 en `EditContract` POST x3
(`projecten-leverancier-toevoegen`), `AddClientAccount` POST x3 (`projecten-klant-toevoegen`).

Let op: de B-bedrading voor Issues Details/Plan/Send en `IncommingInvoiceDetail` is "dichtstbijzijnd artikel": de artikels
`punten-*`/`avondmelding-*` en `borg-op-een-factuur`/`extra-inhouding-*`/`factuur-van-leverancier-*` horen bij dezelfde module, maar
beschrijven die pagina niet apart. Een eigen artikel blijft beter (zie C).

## Categorie A (id gezet + artikel OK), per pagina

| Pagina (view) | Actie | Id | Artikel(s) |
|---|---|---|---|
| Account/MijnProfielV2 | Account.RenderProfiel | account-profiel, -handtekening, -meldingen, -beveiliging (view) | mijn-profiel-bijwerken, mijn-handtekening-instellen, meldingen-instellen, wachtwoord-en-beveiliging |
| MijnTaken/Vandaag-, Week-, Inbox-, OverzichtV2 | MijnTaken.* | mijn-taken-vandaag/-week/-inbox/-overzicht | mijn-taken-* + snel-een-taak-starten |
| Deadlines/IndexV2 | Deadlines.Index | deadlines-overzicht | deadlines-overzicht |
| Invoices/Core/IndexV2 | Invoices.Index | invoices-index | facturen-doorboeken, factuur-nummeren-en-verzenden |
| Invoices/Editor/CreateV2 | Invoices.Create/Edit (concept) | facturen-nieuw / facturen-concept-bewerken (view) | factuur-aanmaken, klant-aanmaken-vanuit-factuur / concept-bewerken |
| Invoices/Editor/EditV2 | Invoices.Edit | facturen-bewerken (view) | factuur-bewerken |
| Invoices/Send/SendV2 | Invoices.Send | facturen-verzenden (view) | factuur-verzenden |
| Marktanalyse/GemeenteanalyseV2, VergelijkbarePandenV2 | Marktanalyse.* | marktanalyse-gemeente / -vergelijkbaar | gemeenteanalyse-lezen / vergelijkbare-panden-zoeken, zoekprofiel-opslaan |
| Projecten/Budget/BudgetIndexV2 | BudgetIndex | budget-index | budget-nieuw, budget-overzicht |
| Projecten/Budget/Budget{Gegevens…Resultaat}V2 (9 stappen) | `PrepareWizardV2` -> `"budget-stap-" + step` | budget-stap-1 … 9 | budget-stap-*, eerste-budget, budget-definitief, budget-meldingen |
| Projecten/Budget/BudgetVergelijkV2 | BudgetVersiesVergelijken | budget-vergelijken | budgetten-vergelijken |
| Projecten/Core/IndexV2 / ToevoegenV2 / DetailV2 / EditV2 | Index / Toevoegen / Detail / Edit | projecten-algemeen / -toevoegen / -detail / -bewerken | zie topic projecten |
| Projecten/Clients/DetailClientsV2, DetailContactsV2, ContactV2, ContactFormV2 | DetailClients, ContactsListV2, ContactV2, ContactFormV2 | projecten-klanten / -contacten / -contact / -contact-nieuw | project-klanten-overzicht / contacten-overzicht + contactenlijsten-en-export / contact-opvolgen / contact-toevoegen |
| Klanten/Core/DetailV2, ClientAccount/AddClientAccountV2 | Klanten.Detail, AddClientAccount | projecten-klant-detail / -klant-toevoegen | project-klant-detail / project-klant-toevoegen |
| Projecten/Contracts/DetailContractsV2, DetailContractV2, AddContractV2, EditContractV2 | DetailContracts, DetailContract/DetailSupplier, AddContract, EditContract | projecten-leveranciers / -leverancier-detail / -leverancier-toevoegen (x2) | project-leveranciers-overzicht / project-leverancier-detail / project-leverancier-toevoegen |
| Projecten/Contracts/RecalculationV2, RecalculationDetailV2, CalculationSettingsV2, BorgenV2 | Recalculation, RecalculationDetail, CalculationSettings, Borgen | nacalculatie-overzicht / -detail / -budget / -borgen | nacalculatie-lezen / nacalculatie-lezen / budget-instellen-nacalculatie / borgen-vrijgeven |
| Projecten/IncomingInvoices/Add-, EditIncommingInvoiceV2 | Add/EditIncommingInvoice | nacalculatie-factuur | factuur-van-leverancier-toevoegen, borg-op-een-factuur, extra-inhouding-op-een-factuur |
| Projecten/Insurances/DetailInsurancesV2, AddInsuranceV2 | DetailInsurances (tab-afhankelijk), AddInsurance/SaveInsurance | verzekeringen-overzicht / -attesten / verzekering-toevoegen | verzekeringen-overzicht / attesten-aannemers-opvragen / verzekering-toevoegen |
| Projecten/Sales/DetailSalesV2, SalesSettingsV2, SalesCalculateV2 | VerkoopListV2, VerkoopSettingsV2, SalesCalculateV2 | projecten-te-koop / -verkoopinstellingen / -eenheid-berekenen | te-koop-overzicht / verkoopinstellingen-aanpassen / eenheid-berekenen |
| Projecten/Issues/IndexV2 | ProjectsIssues.Index | issues-index | 7 artikels in punten-verslagen |
| Projecten/Verslagen/EditV2 | ProjectVerslagen.Edit | verslagen-edit | werfverslag-opmaken, werfverslag-afronden-en-versturen |
| Instellingen: Activities, VacationDays, BlogBeheer (2), Bouwindexen, BudgetFormules, BudgetPrijsReferenties, BudgetReferentieProjecten, KostprijsMaterialen, CookieConsentStats, EmailTemplateBeheer, HomeHeroProject, IssueNotificationAdmin (3), Marktdata, TrajectSjabloonAdmin (2), UserAdmin (3), VacatureBeheer (3) | zie views | `instellingen-*` (in de view gezet) | artikels in topic instellingen |

## Categorie C: ontbrekende artikels (pagina heeft geen id en geen artikel)

Voorstel: id = `screens`-waarde; zet hem daarna bovenaan de view of in de controllerplek (`ViewData["HelpScreenId"] = "…";`).

### aan-de-slag

| Pagina | View | Controller + actie | Wat de pagina doet | Voorstel slug / topic / screens |
|---|---|---|---|---|
| Dashboard | `Views/Home/Index.cshtml` (+ `_Dashboard*`-partials per rol) | `HomeController.Index` | Startpagina per rol (projectleider, CEO/CFO, boekhouding, verkoper, ontwikkelaar) met projectkaarten en meldingen. | `dashboard-lezen` / aan-de-slag / `dashboard` |
| Instellingen-overzicht | `Views/Instellingen/Algemeen/IndexV2.cshtml` | `InstellingenController.Index` (Algemeen) | Hub met alle instellingengroepen als doorzoekbare kaarten (punt 24a). | `instellingen-vinden` / instellingen / `instellingen-overzicht` |

### budget

| Pagina | View | Controller + actie | Wat de pagina doet | Voorstel |
|---|---|---|---|---|
| Budget-PDF opmaken | `Views/Projecten/Budget/BudgetPdfOpmakenV2.cshtml` | `ProjectenController.BudgetPdfOpmaken` (en `BudgetExcelOpmaken`) | Volle pagina die de budget-PDF aanmaakt (intern/bank), de hoofdstukken toont en de PDF opent of downloadt. | `budget-pdf-opmaken` / budget / `budget-pdf-opmaken` |

### facturatie-wijzigingen

| Pagina | View | Controller + actie | Wat de pagina doet | Voorstel |
|---|---|---|---|---|
| Factuurdetail | `Views/Invoices/Core/DetailV2.cshtml` | `InvoicesController.Detail` | Eén verkoopfactuur bekijken: regels, status, verzendkanalen, boeking en acties. | `factuur-bekijken` / facturatie-wijzigingen / `facturen-detail` |
| Facturatie van een project | `Views/Projecten/Invoicing/InvoicingV2.cshtml` | `ProjectenController.InvoicingV2` | Alles wat nu factureerbaar is per klantaccount; facturen opmaken en reeds gefactureerd (tab). | `project-factureren` / facturatie-wijzigingen / `projecten-facturatie` |
| Betalingsschijven | `Views/Projecten/Invoicing/PaymentStagesV2.cshtml` | `ProjectenController.PaymentStagesV2` | Matrix schijf x eenheid per betalingsgroep; schijf als bereikt aanduiden. | `betalingsschijven-volgen` / facturatie-wijzigingen / `projecten-betalingsschijven` |
| Betalingsgroep bewerken | `Views/Projecten/Invoicing/PaymentGroupEditV2.cshtml` | `ProjectenController.PaymentGroupEditV2` (GET + `SavePaymentGroupV2`) | Een betalingsgroep en haar schijven aanmaken of aanpassen. | `betalingsgroep-instellen` / facturatie-wijzigingen / `projecten-betalingsgroep-bewerken` |
| Offertes & wijzigingen (lijst) | `Views/Projecten/ChangeOrders/ChangeOrdersV2.cshtml` | `ProjectenController.ChangeOrdersV2` | Lijst van offertes en wijzigingsopdrachten met statusteller. Nu bedraad op `wijzigingen-opmaken`; eigen artikel nog wenselijk: `offertes-en-wijzigingen-volgen` / `wijzigingen-overzicht`. |

### instellingen

| Pagina | View | Controller + actie | Wat de pagina doet | Voorstel |
|---|---|---|---|---|
| Mijn bedrijven | `Views/Instellingen/Facturatie/IssuerCompaniesV2.cshtml` | `InstellingenController.IssuerCompanies` | Tabel van de factuur-uitgevende bedrijven met koppeling en standaarden (24b). | `bedrijven-beheren` / instellingen / `instellingen-bedrijven` |
| Bedrijf bewerken / nieuw | `Views/Instellingen/Facturatie/IssuerCompaniesEditV2.cshtml` | `InstellingenController.IssuerCompaniesCreate` / `IssuerCompaniesEdit` | Zes tabs: algemeen, facturatie, bankrekeningen, boekhouding, lay-out & e-mail, uurtarieven (24c). | `bedrijf-bewerken` / instellingen / `instellingen-bedrijven-bewerken` |

### marktanalyse

| Pagina | View | Controller + actie | Wat de pagina doet | Voorstel |
|---|---|---|---|---|
| Projectdetail marktanalyse | `Views/Marktanalyse/ProjectdetailV2.cshtml` | `MarktanalyseController.Projectdetail` | Eén bouwproject te koop: kerncijfers, projectinfo en units/bronnen (46d); bereikbaar vanuit Gemeenteanalyse of Vergelijkbare panden. | `marktproject-bekijken` / marktanalyse / `marktanalyse-projectdetail` |

### projecten

| Pagina | View | Controller + actie | Wat de pagina doet | Voorstel |
|---|---|---|---|---|
| Klanten (alle) | `Views/Klanten/Core/IndexV2.cshtml` | `KlantenController.Index` | Zoekbare klantenlijst over alle projecten, optioneel per uitgevend bedrijf. | `klanten-overzicht` / projecten / `klanten-overzicht` |
| Klant (read-only) | `Views/Klanten/Core/DetailsV2.cshtml` | `KlantenController.Details` | Alleen-lezen klantgegevens en contacten. | `klant-bekijken` / projecten / `klanten-details` |
| Klant toevoegen / bewerken (Klanten-module) | `Views/Klanten/Core/CreateV2.cshtml`, `EditV2.cshtml` | `KlantenController.Create` / `Edit` | Formulier met algemene gegevens en contacten van een klant. | `klant-beheren` / projecten / `klanten-nieuw, klanten-bewerken` |
| Klant bewerken in een project | `Views/Klanten/ClientAccount/EditProjectV2.cshtml` | `KlantenController.EditProject` | Klantaccount van een project bewerken: eenheden, mede-eigenaars, facturatie. | `project-klant-bewerken` / projecten / `projecten-klant-bewerken` |
| Leveranciers (alle) | `Views/Leveranciers/Core/IndexV2.cshtml` | `LeveranciersController.Index` | Zoekbare leverancierslijst. | `leveranciers-overzicht` / projecten / `leveranciers-overzicht` |
| Leverancier detail | `Views/Leveranciers/Core/DetailsV2.cshtml` | `LeveranciersController.Details` | Algemene gegevens, afdelingen, contacten, contracten, facturen. | `leverancier-bekijken` / projecten / `leveranciers-details` |
| Leverancier toevoegen / bewerken | `Views/Leveranciers/Core/CreateV2.cshtml`, `EditV2.cshtml` | `LeveranciersController.Create` / `Edit` | Formulier met drie tabs (algemeen, afdelingen, contacten). | `leverancier-beheren` / projecten / `leveranciers-nieuw, leveranciers-bewerken` |
| Coördinatie | `Views/Projecten/Coordinatie/DetailCoordinatieV2.cshtml` | `ProjectenController.DetailCoordinatie` | Schijven, regie of beide, met kerncijfers en facturen. (Bestaand `project-coordinatie` beschrijft het tabblad in Bewerken, niet deze pagina.) | `coordinatie-opvolgen` / projecten / `projecten-coordinatie` |
| Documenten | `Views/Projecten/Docs/DetailDocsV2.cshtml` | `ProjectenController.DetailDocs` | Mappen en slimme lijsten, documentdetail, revisies en aanvragen (punt 17). | `documenten-beheren` / projecten / `projecten-documenten` |
| Foto's en video's | `Views/Projecten/Media/DetailPhotosV2.cshtml` | `ProjectenController.DetailPhotos` | Media per sectie met detailpaneel. | `fotos-beheren` / projecten / `projecten-fotos` |
| Eenheden | `Views/Projecten/Units/DetailUnitsV2.cshtml` | `ProjectenController.DetailUnits` | Eenhedenlijst, met of zonder basisakte. | `eenheden-beheren` / projecten / `projecten-eenheden` |
| Eenheid toevoegen / bewerken | `Views/Projecten/Units/UnitFormV2.cshtml` | `ProjectenController.AddUnit` / `EditUnit` | Eén formulier met zes secties en live prijsberekening. | `eenheid-bewerken` / projecten / `projecten-eenheid-formulier` |
| Slecht weer | `Views/Projecten/Weather/WeatherV2.cshtml` | `ProjectenController.Weather` | Jaarkalender om slechtweerdagen per dag te markeren. | `slechtweerdagen-registreren` / projecten / `projecten-slecht-weer` |

### punten-verslagen

| Pagina | View | Controller + actie | Wat de pagina doet | Voorstel |
|---|---|---|---|---|
| Verslagen (lijst) | `Views/Projecten/Verslagen/IndexV2.cshtml` | `ProjectVerslagenController.Index` | Werfverslagen en opleveringen met samenvatting; "Werfbezoek nr. N starten". | `verslagen-overzicht` / punten-verslagen / `verslagen-index` |

### traject-dossiers

| Pagina | View | Controller + actie | Wat de pagina doet | Voorstel |
|---|---|---|---|---|
| Traject | `Views/Projecten/Traject/IndexV2.cshtml` | `ProjectTrajectController.Index` | Fases met mijlpalen in vier weergaven (tijdlijn en andere), kerncijfers. | `traject-volgen` / traject-dossiers / `traject-index` |
| Dossiers | `Views/Projecten/Dossiers/IndexV2.cshtml` | `ProjectDossiersController.Index` | Nutsaanvragen als matrix en alle dossiers als lijst. | `dossiers-overzicht` / traject-dossiers / `dossiers-index` |
| Dossierdetail | `Views/Projecten/Dossiers/DetailsV2.cshtml` | `ProjectDossiersController.Details` | Stappenbalk, checklist keuring/overdracht, koppeling met mijlpaal en nacalculatie. | `dossier-opvolgen` / traject-dossiers / `dossiers-detail` |
| Nutsaansluiting | `Views/Projecten/Dossiers/NutsFormV2.cshtml` | `ProjectDossiersController.NutsNieuw` / `NutsBewerken` | Nieuwe nutsaansluiting of bewerken met werkstroom, kosten en status. | `nutsaansluiting-aanvragen` / traject-dossiers / `dossiers-nuts-formulier` |

## Omgekeerde controle (artikels)

- Dode artikels na bedrading: **0**.
- `facturatie-wijzigingen/wijzigingsopdracht-opmaken.md` stond op `invoices-index` (de factuurlijst) maar beschrijft offerte inlezen, regels en ondertekenen: verplaatst naar `wijzigingen-opmaken`, nu gezet op `ChangeOrdersV2`, `ChangeOrderDetailV2`, `QuoteIntakeV2` en `ChangeOrderSign`. Het veld `related: punten-lijst-overzicht` van dat artikel lijkt een kopieerfout (aanrader: `related: factuur-nummeren-en-verzenden` of leeg).
- `projecten/grondaandelen-verdelen.md` had geen `screens:` (dode pagina-koppeling): `projecten-grondaandelen` toegevoegd en op `Landshares` gezet.
- Zonder `screens:` en bewust zo gelaten: `aan-de-slag/account-en-rol.md` (algemeen artikel) en `projecten/project-contactpersonen.md` (contactaanvragen; volgens HELPCENTRUM_VOORTGANG hoorde die bij de legacy `DetailContacts`, niet bij `DetailContactsV2`; check of dit artikel nog klopt).
- Meerdere artikels op één id is normaal (bv. `issues-index` x7, `projecten-bewerken` x6, `invoices-index` x2 na de fix); die tonen allemaal in het zijpaneel.
- Niet-gl-v2 en dus buiten scope: `Search/Index`, `DocumentenCentrum/*`, `Account/MijnHandtekening`, `Account/Login`.
- Niet gecontroleerd (browser): of het "?"-paneel op de net bedraade pagina's effectief artikels toont; uit de code volgt het wel.
