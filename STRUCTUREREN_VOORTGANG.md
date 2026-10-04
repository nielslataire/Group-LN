# Views / Controllers / Models structureren — voortgangsstatus

Doorlopend statusdocument voor het herstructureren van CPMCore (Niels, okt. 2026: "mijn pagina's
onder Views zijn een chaos, niets staat meer gestructureerd"). Beslissingen (2026-10-04):
**Projecten eerst, als pilot**; daarna hetzelfde patroon op Klanten, Invoices, Leveranciers.
**Views in submappen per functie**, dezelfde indeling als de controller-partials. **Satellieten
onder hun module** (31 → 15 mappen rechtstreeks onder Views/). **Bijwerken na elke groep.** Reist
mee via git: commit + push vóór je van machine wisselt.

**Laatste update:** 2026-10-04 — **Projecten ✅, Instellingen ✅, clusters ✅, Klanten ✅, Invoices ✅, Leveranciers ✅, kleine opruiming ✅, gl-v2-shell.css gesplitst ✅** (build groen, nog
niet gecommit). Alle monolieten zijn opgesplitst; zie "Nog te doen" voor de losse eindjes.

## Twee mechanismen, allebei in `Helpers/CustomViewLocationExpander.cs`
1. **Functiemappen** (`FeatureFolders`): `Views/<Module>/<Groep>/<Pagina>.cshtml` naast
   `<Module>Controller.<Groep>.cs`. Geldt voor élke controller; een viewnaam mag maar in één
   functiemap van die module voorkomen (anders wint de eerste in `FeatureFolders`-volgorde).
2. **Satellieten** (`ControllerFolders`): controller → map, bv. `UserAdmin` →
   `Views/Instellingen/UserAdmin/`. Routes, controllers en URL's veranderen niet. Deze mappen staan
   bewust NIET in `FeatureFolders` (ze hebben een eigen `Index`/`Details` die anders zou botsen met
   die van de module).

Gevolg: geen enkele `View("…")`/`PartialView("…")`/`ViewAsPdf("…")`/`FindView(...)`-aanroep hoeft
herschreven te worden; ook `return View(model)` en relatieve partial-namen (`"Modals/_X"`,
`<partial name="_Form">`) blijven werken. **Enige uitzondering: absolute paden** (`"~/Views/…"`)
omzeilen de expander — grep daarop vóór je bouwt:
`grep -rn "~/Views/" --include=*.cs --include=*.cshtml CPMCore | grep -v Shared`.
Bekende absolute paden (bijgewerkt): `Services/ChangeOrderPdfService.cs` (ChangeOrderPDF),
`Views/Werfportaal/ContractorPortal/_ViewStart.cshtml` (eigen `_Layout`),
`Views/Instellingen/Facturatie/InvoiceTemplates{Create,Edit}.cshtml` (→ `Instellingen/Partials/`, niet verhuisd).
`_ViewStart`/`_ViewImports` in een satellietmap verhuizen gewoon mee (Razor zoekt ze per map omhoog).

## Het patroon (per module, in deze volgorde)
1. **Inventaris**: banner-comments en acties in `<X>Controller.cs` → groepsindeling in een tabel
   hier, met regelnummers van VÓÓR het knippen.
2. **Controller**: blok per groep knippen naar `<X>Controller.<Groep>.cs` (`public partial class`,
   zelfde namespace — file-scoped of met accolades, volg het hoofdbestand —, volledige using-lijst
   overnemen). Knip van **onder naar boven**. Hoofdbestand houdt velden, ctor, gedeelde filters en
   een markerregel per groep. Hoofdklasse `partial` maken.
3. **Views**: `git mv Views/<X>/<Pagina>.cshtml Views/<X>/<Groep>/`. `Partials/` en `Modals/` blijven.
4. **Expander**: nieuwe groepsnaam in `FeatureFolders`; nieuwe satelliet in `ControllerFolders`.
5. **Controle**: botsende namen over de functiemappen van één module (script in sessie-scratchpad,
   of: `find Views/<X> -maxdepth 2 -name "*.cshtml" | sed 's#.*/##' | sort | uniq -d`); absolute paden.
6. **Build** — 0 errors vóór de volgende module.
7. Dit bestand + .md-verwijzingen bijwerken (DESIGN.md "Waar staat een pagina?", DEPLOY.md,
   DEVNOTES.md, *_VOORTGANG.md, `.claude/skills/groupln/SKILL.md`).

### Scripts (scratchpad van sessie 445ba133…, eenmalig, niet in de repo)
`split_projectmodel.ps1`, `extract_group.ps1` + `run_all_groups.ps1` (Projecten),
`split_instellingen.ps1` (Instellingen). PowerShell 5.1-valkuilen: scripts als UTF-8 **met BOM**
bewaren (anders worden `—`/`──` vermangeld tot slimme aanhalingstekens); een pipeline pakt een enkel
`@(a,b)`-element uit, sorteer via indexen; controleer vóór het knippen op inhoud van grensregels in
plaats van op het totale aantal regels (`wc -l` en `ReadAllLines` tellen anders bij een ontbrekende
slotnewline).

## Stand per module

### Views/ top-level (15): Account, Deadlines, DocumentenCentrum, Home, Instellingen, Invoices, Klanten, Leveranciers, Marktanalyse, MijnTaken, Ondertekenen, Projecten, Search, Werfportaal, Shared

### Projecten ✅ (2026-10-04)
- **Modellen**: `ProjectModel.cs` (4.441 regels, 88 klassen) → 14 bestanden `Project<Groep>Model.cs`.
- **Controller**: `ProjectenController.cs` 12.400 → 244 regels (velden, ctor, `OnActionExecuted`-
  sidebarfilter, markers, top-level request-DTO's `VergelijkRequest` e.a. + `PathExtensions` — die
  staan buiten de klasse en zijn bewust gebleven).
- **Functiemappen** (`Views/Projecten/<Groep>/` ↔ `ProjectenController.<Groep>.cs`):

| Groep | Partial(s) | Views |
|---|---|---|
| Core | `.Core.cs` | Index, IndexV2, Toevoegen, ToevoegenV2, Detail, DetailV2, Edit, EditV2 |
| Clients | `.Clients.cs` | DetailClients(V2), DetailContacts, ContactDetails, AddContact, EditContact |
| Units | `.Units.cs`, `.UnitFormV2.cs` | DetailUnits(V2), AddUnit, EditUnit, LandsharesV2, UnitFormV2 |
| Contracts | `.Contracts.cs` (+ `PdfController` → PrintRecalculation) | DetailContracts(V2), DetailContract(V2), AddContract, EditContract(V2), Recalculation, RecalculationDetail, CalculationSettings, PrintRecalculation |
| IncomingInvoices | `.IncomingInvoices.cs` | AddIncommingInvoice, EditIncommingInvoice, IncommingInvoiceDetail(V2) |
| ChangeOrders | `.ChangeOrders.cs` (legacy), `.ChangeOrdersV2/.ChangeOrderDetailV2/.ChangeOrderFlowV2/.QuoteIntakeV2/.ChangeOrderSign.cs` | DetailsChangeOrder, AddChangeOrder, EditChangeOrder, ChangeOrderPDF, ChangeOrderFooter, MinimalTestPDF, ChangeOrdersV2, ChangeOrderDetailV2, ChangeOrderSignV2, QuoteIntakeV2 |
| Weather | `.Weather.cs` | Weather, WeatherV2 |
| Media | `.Media.cs` | DetailPhotos(V2), DetailNews |
| Docs | `.Docs.cs`, `.DocsV2.cs` | DetailDocs, DetailDocsV2 |
| Insurances | `.Insurances.cs` | DetailInsurances |
| Sales | `.Sales.cs` | DetailSales, SalesSettings |
| Coordinatie | `.Coordinatie.cs`, `.CoordinatieV2.cs` | DetailCoordinatie(V2), CoordinatieInstellingen |
| Invoicing | `.Invoicing.cs`, `.InvoicingV2.cs`, `.PaymentStagesV2.cs` | Invoicing(V2), PaymentStages(V2), PaymentStagesAddUpdate, PaymentGroupLink |
| Lookups | `.Lookups.cs` | — |
| Budget | `.Budget.cs` | 12 Budget*-views |

- **Satellieten** in `Views/Projecten/`: `Dossiers/` (ProjectDossiersController), `Traject/`
  (ProjectTrajectController), `Issues/` (ProjectsIssuesController — let op: bestand heet
  `ProjectIssuesController.cs`, klasse `ProjectsIssuesController`).
- **Top-level gebleven**: `_ProjectForm*.cshtml` (gedeeld, FORMULIER-STRAMIEN.md), `Partials/`,
  `Modals/`, en `AddClient.cshtml` (**wees**: geen actie verwijst ernaar — aan Niels vragen of weg).
- **Opgemerkt, niet aangepakt**: `ProjectenController.Sales.cs` doet `ViewAsPdf("SalesListPDF")`,
  maar die view bestaat enkel als `.vbhtml` in het oude CPM-project (was al zo).

### Instellingen ✅ (2026-10-04)
- **Controller**: `InstellingenController.cs` 2.454 → 109 regels (velden, serializer-opties, ctor,
  markers; `OctopusRelationLinkRequest` erna). File-scoped namespace, dus de partials ook.
- **Functiemappen** (`Views/Instellingen/<Groep>/` ↔ `InstellingenController.<Groep>.cs`):

| Groep | Views |
|---|---|
| Algemeen | Index, IndexV2, Activities, VacationDays |
| Facturatie | IssuerCompanies(V2), IssuerCompaniesCreate, IssuerCompaniesEdit(V2), InvoiceTemplates, InvoiceTemplatesCreate, InvoiceTemplatesEdit, OctopusRelationSuggestions, BankAccountCreate, BankAccountEdit, SeriesCreate, SeriesEdit, Sequences |
| Budget | KostprijsMaterialen, KostprijsUpdatePreview, BudgetFormules, Bouwindexen |
| Marktdata | MarketDataStatus |

- **Satellieten** (de 9 beheerschermen waar de Instellingen-hub naar linkt): `UserAdmin/`,
  `AppRoles/`, `EmailTemplateBeheer/`, `BlogBeheer/`, `VacatureBeheer/`, `CookieConsentStats/`,
  `HomeHeroProject/`, `IssueNotificationAdmin/`, `TrajectSjabloonAdmin/`. `Partials/` gebleven.
- **Modellen**: niet opgesplitst (geen monoliet zoals ProjectModel.cs).

### Ondertekenen ✅ (enkel bij elkaar gezet, 2026-10-04)
`Views/Ondertekenen/` = eigen views van OndertekenenController (route `/tekenen`) op de root +
satellieten `Signing/` (SigningController, route `/ondertekenen`, e-mailflow andere pc),
`Verifieer/`, `SigningAdmin/`. **De twee implementaties zijn niet samengevoegd** — dat blijft een
aparte stap waarbij per onderdeel aan Niels gevraagd wordt wat hij houdt (ONDERTEKENEN_VOORTGANG.md).

### Werfportaal ✅ (2026-10-04)
`Views/Werfportaal/ContractorPortal/` (eigen `_Layout`, `_ViewStart` bijgewerkt naar het nieuwe
absolute pad) en `Views/Werfportaal/ContractorInvite/`.

### Klanten ✅ (2026-10-04)
- **Controller**: `KlantenController.cs` 2.787 → 94 regels. Partials: `.Core.cs` (lijst, detail, aanmaken,
  bewerken, snel aanmaken + formulier-/Octopus-helpers; ook `DetailCO`, een redirect), `.ClientAccount.cs`
  (klant op een project: toevoegen, bewerken, mede-eigenaars/eenheden/schenkingen/volmachten, verwijderen),
  `.Lookups.cs` (VIES, postcode; geen views).
- **Views**: `Core/` Index(V2), Form, DetailsV2, Create(V2), Edit(V2), Detail(V2), DetailCO;
  `ClientAccount/` AddClientAccount(V2), EditProject(V2). `Partials/`, `Modals/` gebleven.
  **Wees**: `Core/DetailCO.cshtml` — de actie `DetailCO` redirect altijd naar Projecten, de view wordt nooit
  gerenderd (aan Niels vragen of weg).
- **Modellen**: `ClientModel.cs` (1.477 regels) → `ClientModel.cs` (ClientModel, EditClientModel),
  `ClientAccountModel.cs` (AddClientAccountModel, UnitChoiceVm, AddUpdateClientCoOwnerModel, AddUnitToClient-,
  AddGiftToClient-, AddPoaToClientModel), `ClientProjectDetailModel.cs` (DetailClients*-, ClientCalendar-,
  DetailInvoicing-, Export*ToPdf-, DeliveryModel — gebruikt door Projecten/Clients).

### Invoices ✅ (2026-10-04)
- **Controller**: `InvoicesController.cs` 5.302 → 138 regels. Partials: `.Core.cs` (lijst, boeken, detail,
  PDF/UBL/Octopus-download, verwijderen), `.Send.cs` (verzenden, opnieuw versturen, verzend-viewmodel),
  `.Editor.cs` (definitief maken, aanmaken, ontwerp bewaren/bewerken, lijnen samenstellen, formulier-
  opzoekingen), `.Octopus.cs` (boekhoudkoppeling: hoofdflow, relaties, bijlagen, workflowstatus, OGM/EPC-QR;
  geen views), `.Helpers.cs` (breadcrumbs, e-mailsjabloon, sortering, custom fields, status; geen views).
- **Views**: `Core/` Index(V2), Detail(V2); `Editor/` Create, Edit, EditDraft; `Send/` Send.
  `Partials/`, `Modals/` gebleven. Modellen (`Models/Invoicing/`, 10 bestanden) waren al opgesplitst.

### Leveranciers ✅ (2026-10-04)
- **Controller**: `LeveranciersController.cs` 2.025 → 66 regels (file-scoped namespace). Partials: `.Core.cs`
  (lijst, detail, aanmaken, bewerken, verwijderen + formulier-/Octopus-helpers), `.Portal.cs`
  (werfportaal-toegang voor contacten: uitnodigen/intrekken; geen views), `.Lookups.cs` (zoeken, VIES,
  postcode; geen views).
- **Views**: `Core/` Index(V2), Details(V2), Create(V2), Edit(V2). `Partials/`, `Modals/` gebleven.
- Valkuil die hier beet: het bestand had géén slotnewline, waardoor `wc -l` één regel te weinig telde en de
  klasse-sluitaccolade in het verkeerde bestand belandde (CS1513/CS1022). Controleer grensregels op inhoud
  mét inspringing (`    }` ≠ `}`), niet enkel getrimd.

### Kleine opruiming ✅ (2026-10-04, analysepunt 4)
- `Service/` (SmtpEmailSender, ServiceFactory, WordHtmlSanitizer) samengevoegd met `Services/`; namespace
  `CPMCore.Service` → `CPMCore.Services`, usings in Program.cs, Invoices-partials, ContractorInvite/-Digest
  en `ProjectenController.Clients.cs` aangepast.
- Losse `Models/*.cs` (10) naar submappen, namespaces ongewijzigd: `Models/Shared/` (Breadcrumb,
  PageHeaderModel, FormShellActionsModel, ErrorViewModel, MeldingType, PostalcodeModel),
  `Models/Account/` (AccountViewModels), `Models/Home/` (DashboardType),
  `Models/Instellingen/UserAdmin/` (UserAdminViewModel, PermissionViewModel — spiegelt Views/Instellingen/UserAdmin/).
- `Extensions/testdb.cs` (ongebruikte `SqlDebugInterceptor`) verwijderd.

### gl-v2-shell.css opgesplitst ✅ (2026-10-04, analysepunt 5)
- 5.339 regels → `gl-v2-shell.css` (1.512 regels chrome: rail, flyout, topbar, body-kaart, knoppen, userbox,
  mobiel menu, breakpoints, snelactiebalk) + 13 componentbestanden in `wwwroot/css/gl-v2/`: forms, modals,
  toasts, kpi, contextmenu, meldingen, progress, projectcard, forms-extra, page, notices, mobile-search, steps.
- **Aaneengesloten geknipt, niets herschikt**: `_LayoutV2.cshtml` laadt de 14 bestanden in exact de oude
  volgorde, dus geen enkele specificiteits-/volgordeverschuiving. Geverifieerd: de stukken samengevoegd zijn
  byte-voor-byte het origineel (diff leeg); elk bestand heeft accoladebalans 0.
- **Bug gevonden en hersteld**: het `@media (max-width: 1023.98px)`-blok voor de dashboardkolom
  (`.gl-v2-mc-col`, nu onderaan `mobile-search.css`) was nooit gesloten, waardoor het hele Stappenplan-blok
  erin genest zat en op desktop (≥1024px) geen opmaak kreeg. Sluit-accolade toegevoegd.
- Verwijstabel bovenaan `gl-v2-shell.css` en in DESIGN.md ("Waar staat gl-v2-CSS?"); vier component-
  specifieke DESIGN.md-verwijzingen (foutoverzicht, meldingskaders, stappenplan, inline fout) omgezet. De
  overige ~45 "gl-v2-shell.css"-vermeldingen in DESIGN.md en view-comments gaan over laadvolgorde of zijn
  historisch; de notitie dekt ze.
- Niet gedaan: pagina-CSS/-JS (`gl-v2-<module>-<pagina>.*`) naar een `pages/`-submap — vraagt ~60
  href/src-wijzigingen in views; apart te beslissen.

### Nog te doen
- [ ] Beslissing Niels: wezen `Views/Projecten/AddClient.cshtml` en `Views/Klanten/Core/DetailCO.cshtml` verwijderen?
- [ ] Grensgevallen (niet dringend): UserAdminController 1.124 regels, ProjectIssuesController 906 regels.
- [ ] Verdere structuurpunten uit de analyse van 2026-10-04: legacy/V2-dubbels opruimen,
      Program.cs-registraties per module, dode projecten uit de repo.
