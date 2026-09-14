# Trajectopvolging — voortgangsstatus

Doorlopend statusdocument voor de grote "trajectopvolging"-feature (werf/project-mijlpalen, dossiers,
triggers, taken). Origineel plan: `.claude`-sessie-plan (zie chatgeschiedenis) — dit bestand is de
werkende samenvatting om een volgende sessie snel weer op te starten. Bijwerken bij elke increment.

**Laatste update:** 2026-09-14 (increment 7 klaar en bevestigd; nu bezig met UX-verfijningspassen
over de bestaande admin-pagina's — twee passen op `Instellingen/Trajectsjablonen/Bewerken` deze
sessie, zie de twee secties hieronder, nieuwste eerst).

## Herwerking: sjabloon-editor naar master/detail — 2026-09-14

**Aanleiding:** de gebruiker leverde een referentiescreenshot van de gewenste `Instellingen/
Trajectsjablonen/Bewerken`-pagina (master/detail met een fases-en-mijlpalenboom links en een
genummerd sectie-formulier rechts) en vroeg om zowel de styling als de functionaliteit ervan over
te nemen, met behoud van het bestaande `_Layout`-stramien. Aanpak: rechtstreeks gebouwd volgens de
al bestaande, gedocumenteerde conventies in `DESIGN.md` (`gl-page-header`, `gl-form-shell__tabs`,
`gl-field-grid`/`gl-field`) — geen losstaande stylingpas nodig, de bouwstenen lagen er al.

**Wat er veranderd is:**
- **Paginakop herbouwd** met `gl-page-header` (partial `_PageHeader`): titel = sjabloonnaam,
  ondertitel = `"N fases · M mijlpalen · K acties · gebruikt door P projecten"` (nieuw berekend in
  `TrajectSjabloonAdminController.Bewerken`, opgeslagen op `TrajectSjabloonEditVm`). Acties-rij kreeg
  een live "Niet-bewaarde wijzigingen"-indicator (JS, dirty-tracking op elke input/change binnen de
  pagina) naast Terug/Dupliceren/Sjabloon opslaan.
- **Nieuwe "Dupliceren"-actie**: `TrajectSjabloonAdminController.Dupliceren` (POST
  `{id}/Dupliceren`) kloont het volledige sjabloon (fases, mijlpalen, triggers, alle Id's op
  `null`, naam + " (kopie)", `IsStandaard` bewust `false` om dubbele standaardsjablonen per
  projecttype te vermijden) via de bestaande `ITrajectSjabloonService.Upsert`, en redirect naar de
  nieuwe kopie.
- **Tabs** (`gl-form-shell__tabs`, hergebruikt patroon): **Structuur** (de bestaande editor,
  herwerkt — zie hieronder), **Tijdlijn & simulatie** (nieuw — berekent per mijlpaal de streefdag via
  de anker/offset-keten en toont die uitgezet vanaf een instelbare "project aangemaakt op"-datum;
  puur een preview, bewaart niets), **Controle** (nieuw — client-side validatiepas met een
  foutenbadge op de tab: dubbele technische codes, ontbrekende naam/code, onbekende of cyclische
  ankerreferenties, automatische bron zonder bron-parameter, trigger "X dagen voor streefdatum"
  zonder ingevulde dagen, ongeldige trigger-Parameters-JSON; elke melding is klikbaar en springt naar
  de betrokken mijlpaal in de Structuur-tab).
- **Structuur-tab herbouwd als master/detail** i.p.v. één lange, alles-tegelijk-getoonde pagina:
  - Links **"Fases & mijlpalen"**: een sleepbare boom (jQuery UI Sortable, al elders in de app
    gebruikt) van fases (met een kleurstip — nieuw gebruik van het al bestaande maar tot nu toe
    ongebruikte `TrajectSjabloonFase.KleurCode`-veld, instelbaar via een swatch-rij op de
    fase-editor) en, ingeklapt/uitgeklapt per fase, hun mijlpalen (met een automatisch-vs-handmatig
    icoon en een badge met het aantal acties).
  - Rechts een **detailpaneel** dat meekomt met de selectie: een simpele fase-editor (naam, code,
    volgorde, ProjectStatusId, kleur, verwijderen) of — voor een mijlpaal — het volledige,
    genummerde sectieformulier uit het referentiebeeld: *1 Wat is deze mijlpaal?* (naam, technische
    code + "afleiden uit de naam"-knop, soort, verantwoordelijke rol — dit laatste veld bestond al
    in de data maar had nog geen UI —, "Geldt voor"-segmentcontrol, Verplicht-toggle — ook nieuw in
    de UI), *2 Wanneer is ze bereikt?* (Handmatig/Automatisch-keuzekaarten i.p.v. één
    binding-dropdown; bij Automatisch de bestaande Bron + Bron-parameter-velden uit de vorige
    verfijningspas), *3 Streefdatum* (de bestaande anker+offset-regel, nu met een live berekend
    "dag N · datum"-label), *4 Acties bij deze mijlpaal* (triggers, herwerkt als genummerde
    kaartjes met een leesbare "Gebeurtenis → Actie"-samenvatting en een toggle), *5 Toelichting*
    (het `Omschrijving`-veld op mijlpaal-niveau — bestond al in de data/BO maar werd tot nu toe
    nergens getoond of bewaard in de admin-UI).
  - Mijlpaal en fase zijn nu ook **dupliceerbaar** (knop in het detailpaneel), niet enkel
    verwijderbaar.
- **Architectuurwissel in de JS** (`trajectsjabloon.admin.js`, volledig herschreven): van "de DOM is
  de brontabel, bij Opslaan alles uitlezen" naar een in-memory state-object (`state.fases`) dat de
  boom en het detailpaneel rendert; bij Opslaan wordt de payload rechtstreeks uit `state`
  geserialiseerd. Nodig omdat een master/detail-scherm maar één mijlpaal-editor tegelijk toont — de
  oude aanpak (één grote vorm met alle mijlpalen als verborgen/getoonde DOM-nodes) paste daar niet
  meer bij. Tekstvelden gebruiken gerichte live DOM-patches (boomlabel, streefdatum-badge) i.p.v. een
  volledige her-render, zodat de cursor/focus niet verspringt tijdens het typen.
- **Bijvangst-fix**: `DossierKind` op een mijlpaal werd door de oude JS bij elke Opslaan stil op
  `null` gezet (nooit gelezen/geschreven in de vorige versie van het script) — de nieuwe
  state-gebaseerde aanpak leest en bewaart dit veld nu gewoon door, ook al heeft het nog geen eigen
  UI-control.
- Bestanden: `CPMCore/Controllers/TrajectSjabloonAdminController.cs` (stats + Dupliceren-actie),
  `CPMCore/Models/Traject/TrajectSjabloonAdminVm.cs` (stats-properties + `StatsSubtitle`),
  `CPMCore/Views/TrajectSjabloonAdmin/Edit.cshtml` (herbouwd), `CPMCore/wwwroot/js/
  trajectsjabloon.admin.js` (volledig herschreven), `CPMCore/wwwroot/css/traject.css` (nieuwe
  `.gl-tsa-*`-klassen toegevoegd, bestaande klassen ongewijzigd/hergebruikt).
- **Geen DB-migratie nodig** — geen schemawijziging, enkel presentatie/UX + twee al bestaande maar
  ongebruikte velden (`KleurCode`, `Omschrijving`) alsnog een UI gegeven, plus de nieuwe
  Dupliceren-actie die de bestaande `Upsert` hergebruikt.
- Build geverifieerd (0 `error CS`/`error RZ`, enkel de routineuze MSB3021/3027-file-lock-ruis van de
  VS-sessie van de gebruiker) + JS-syntax gecontroleerd (`node --check`) + alle gebruikte
  Boxicons-klassenamen geverifieerd tegen de gebundelde `boxicons.css` (drie namen bestonden niet in
  deze versie en zijn vervangen: `bx-check-shield`→`bx-shield-alt`, `bx-time-five`→`bx-timer`,
  `bx-grid-vertical`→`bx-dots-vertical-rounded`, `bx-error(-circle)`→`bx-alert-triangle`/
  `bx-alert-circle`). **Nog niet live in de browser getest** — vereist een ingelogde sessie;
  graag zelf verifiëren, in het bijzonder: het slepen (fases/mijlpalen herordenen), select2 in het
  dynamisch her-gerenderde detailpaneel, en de Dupliceren-actie.

## Verfijningspas: sjabloon-editor (Instellingen/Trajectsjablonen/Bewerken) — 2026-09-14

**Aanleiding:** de gebruiker vond het koppelen van een mijlpaal aan een andere (voor de streefdatum)
verwarrend omdat je de technische code moest **typen**, en het onderscheid met het losse "Code"-veld
was onduidelijk; ook `BronParam` was een vrij tekstveld terwijl de meeste bindingen een vaste, gekende
set geldige waarden hebben. Uitgevoerd via de `impeccable`-skill (`shape` + een lichte, code-gegronde
`critique` — de volledige dubbele-subagent+browser-pijplijn is bewust overgeslagen: dit is een
authenticatie-vereisende, live-DB-gebonden interne adminpagina, geen losstaand te draaien frontend, en
de gebruiker had zelf al precieze, eerstehands probleembeschrijvingen aangeleverd).

**Wat er veranderd is:**
- **Ankermijlpaal (streefdatum) is nu een doorzoekbare select2-keuzelijst** i.p.v. een vrij tekstveld:
  toont elke andere mijlpaal in het sjabloon als "Fasenaam · Mijlpaalnaam (CODE)", plus een vaste optie
  "Bij aanmaken van het project". Lijst herbouwt live terwijl je typt/toevoegt/verwijdert elders op de
  pagina (`buildRegistry()`/`refreshAllAnchors()` in `trajectsjabloon.admin.js`, gedebouncet).
  **Niet-destructief**: een reeds opgeslagen ankercode die niet (meer) bij een gekende mijlpaal hoort
  wordt niet stil overschreven — verschijnt als een aparte "⚠ Onbekende mijlpaal-code"-optie zodat de
  waarde bewaard blijft en zichtbaar blijft dat ze niet herkend is.
- **Anker + offset zijn visueel samengevoegd tot één leesbare regel** ("Streefdatum telt vanaf
  [ankerkeuze] + [N] dagen") in een Mist-Green-getint blok (`gl-sm-daterule*`), i.p.v. twee losse,
  ongerelateerde velden — het rekenmodel is nu meteen zichtbaar.
- **Het mijlpaal-"Code"-veld is herlabeld en gescheiden** ("Technische code" + een hint-regel wat het
  doet) van de ankerkeuzelijst, zodat de twee niet langer met elkaar te verwarren zijn.
- **BronParam is nu een dynamische, van de gekozen binding afhankelijke keuzelijst** i.p.v. één vrij
  tekstveld voor alle 13 bindingen: vaste select2-dropdown met leesbare labels voor `ProjectDoc`
  (`ProjectDocType`-enum), `ProjectDatum`/`ProjectVlag`/`ClientAccountDatum` (vaste kolomnamen, nu
  Nederlandse labels i.p.v. rauwe C#-veldnamen), `ConstructionIssue` (`ConstructionIssuePhase`-enum),
  `ProjectVoortgangFase` (`VoortgangFase`-enum — kreeg nu ook `<Display>`-namen), en `DossierSubstap`
  (de 7 standaard vergunning-checklist-stappen). Voor `PlanningTaak`/`PlanningSectie`/
  `InvoicingPaymentStage` (verwijzen naar een numeriek ID van één specifiek project — niet zinvol te
  kiezen in een projectonafhankelijk sjabloon) blijft een gewoon tekstveld met een uitleg-hint; voor
  `Handmatig`/`ConnectionSettlement`/`Dossier` (geen parameter nodig) verdwijnt het veld en toont enkel
  een korte uitleg. Ook hier: een reeds opgeslagen waarde die niet in de nieuwe lijst voorkomt wordt niet
  stilzwijgend gewist, maar als aparte "⚠ Huidige waarde ... (niet in lijst)"-optie bewaard.
- **Dedup-bijvangst**: de standaard-vergunningschecklist (7 stappen + Code-mapping) stond dubbel
  gedefinieerd (privé in `ProjectDossierService.cs`, en nu ook nodig voor de BronParam-keuzelijst) —
  verplaatst naar één gedeelde bron `BOCore/BO/Project/Traject/VergunningChecklistDefaults.vb`;
  `ProjectDossierService.cs` verwijst er nu ook naar (geen gedragswijziging, wel één bron van waarheid).
- **Scope, bewust**: dit is enkel de eerste verfijningspas. Trigger-"Parameters JSON" (nog een vrij
  tekstveld met dezelfde "type de juiste sleutels"-vraag), het rauwe `ProjectStatusId`-veld op fases, en
  een volledige `gl-form-shell`-migratie van deze pagina zijn expliciet **niet** meegenomen — apart, later.
- Bestanden: `BOCore/BO/Project/Traject/VergunningChecklistDefaults.vb` (nieuw),
  `BOCore/Enum/VoortgangFase.vb` (Display-namen toegevoegd), `ServiceCore/Traject/ProjectDossierService.cs`
  (dedup), `CPMCore/Views/TrajectSjabloonAdmin/Edit.cshtml`, `CPMCore/wwwroot/js/trajectsjabloon.admin.js`
  (grotendeels herschreven), `CPMCore/wwwroot/css/traject.css`. Geen DB-migratie nodig — puur
  presentatie/UX over dezelfde bestaande payload-contract (BOCore-BO's ongewijzigd).
- Build geverifieerd (0 `error CS`/`error RZ`) + JS-syntax gecontroleerd (`node --check`). **Nog niet
  live in de browser getest** — vereist een ingelogde sessie op een sjabloon met echte fases/mijlpalen;
  graag zelf verifiëren en teruggeven wat er nog niet klopt.

## Architectuur in één oogopslag

- Namespace-segment `Traject` doorheen alle lagen (DALCore/BOCore/FacadeCore/ServiceCore/CPMCore).
- **Sjabloonlaag:** `TrajectSjabloon → TrajectSjabloonFase → TrajectSjabloonMijlpaal` (+ triggers,
  afhankelijkheden) — beheerd via admin-UI `Instellingen/Trajectsjablonen`.
- **Instantielaag:** `Projecttraject → ProjecttrajectFase → Mijlpaal` (+ `MijlpaalHistoriek`,
  `MijlpaalTrigger`/`MijlpaalTriggerRun`) — per project, per-eenheid-mijlpalen via `Mijlpaal.UnitId`.
- **Dossierlaag:** generieke `ProjectDossier` (+ `ProjectDossierGebeurtenis/Document/Mijlpaal/Substap`)
  + getypeerde companion `ProjectNutsAansluiting`. Vergunningsdossiers krijgen automatisch een
  standaard-checklist (`ProjectDossierSubstap`) en koppelen zichzelf aan de bijhorende mijlpalen.
- **Bindingen:** `MijlpaalBindingResolver` leest bestaande data (ProjectDocs, Project-kolommen/vlaggen,
  PlanningTaak, ClientAccount, InvoicingPaymentStage, ConnectionSettlement, ConstructionIssue,
  ProjectVoortgang, Dossier(Substap)) read-only uit om een mijlpaal automatisch te bereiken.
- **Triggers:** `MijlpaalTrigger` (event × actie) → `TrajectTriggerDispatcher` → registry van
  `ITrajectTriggerAction`-handlers, gedraaid via `TrajectHostedService` (dagelijkse tick) of
  `GET /api/trigger/traject-recalc?key=...` (HTTP-backup, zie `Program.cs` — **niet**
  `TriggerController.cs`, dat is dode code voor dit path).
- **Takenlaag:** `ProjectTaak` ("Mijn taken"), los van `ConstructionIssue` ("Punten"), gekoppeld via
  nullable FK's naar project/eenheid/mijlpaal/dossier/punt.

## Increment-status

| # | Increment | Status |
|---|---|---|
| 1 | Framework-fundament (sjabloon + instantie + admin-UI) | ✅ Klaar |
| 1b | Per-eenheid mijlpalen | ✅ Klaar |
| 2 | Bindingen + herberekening (`TrajectHostedService`, `MijlpaalBindingResolver`) | ✅ Klaar |
| 3 | Trigger-engine (event × actie, dispatcher, admin-UI) | ✅ Klaar |
| 4 | Generiek dossier-model + nutsmaatschappijen | ✅ Klaar |
| 5 | Omgevingsvergunning-dossier (checklist + auto-koppeling) | ✅ Klaar (zie bugfix hieronder) |
| 6 | Interne taken ("Mijn taken") | ✅ Klaar |
| 7 | Rol-overzichten/dashboards/permissies/polish | ✅ Klaar |

## Migraties — **nog uit te voeren op de live DB indien nog niet gebeurd**

Volgorde is belangrijk (`_migrations/029` t/m `039`). Controleer met `sqlcmd` of `sys.tables` de tabel al
bevat vóór je een script opnieuw draait — alle scripts zijn idempotent (`PRINT ... overgeslagen` bij
tweede run, geen destructieve DDL).

- `029_TrajectMijlpalen.sql` … `039_ProjectTaak.sql` — increments 1 t/m 6, **bevestigd gedraaid en
  werkend** op de live DB (incl. de vergunning-koppel-bugfix hieronder, live getest door de gebruiker).
- **`040_TrajectPerfIndexen.sql`** — **NIEUW, nog niet bevestigd gedraaid.** 2 extra indexen op
  `Mijlpaal` (`VerantwoordelijkeUserId`, `VerantwoordelijkeRol`) — increment 7 draait op elke
  dashboard-load een portfolio-brede rol-/gebruiker-gefilterde mijlpalenscan (5 rol-dashboards +
  Deadlines), die kolommen hadden nog geen index. Geen destructieve wijzigingen.

Na elke migratie: rebuild + herstart de app (VS-sessie van de gebruiker) om de nieuwe code op te pikken.

## Bugfix deze sessie: vergunning-mijlpalen die niet koppelden

**Symptoom:** substap "Ingediend" op Afgerond zetten in het dossier veranderde niets in het traject.
**Oorzaak:** het dossier (project 73) was aangemaakt vóórdat de vergunning-mijlpalen van het traject
bestonden — de auto-koppel-logica bij dossier-aanmaak (`SeedVergunningStappenEnKoppelMijlpalen`) vond op
dat moment simpelweg geen kandidaten en faalde stil (geen retry).
**Fix:** `ProjectDossierService.RelinkVergunningMijlpalen(projectId, userId)` toegevoegd — herkoppelt
alsnog niet-gekoppelde VERGUNNING_*-mijlpalen aan het (oudste actieve) vergunningsdossier van het
project. Wordt nu automatisch aangeroepen vanuit `TrajectInstantiationService.SyncMissing`, dus élke
"Sync met sjabloon"-actie op de trajectpagina herstelt dit soort gevallen vanzelf (self-healing, geen
handmatige stap nodig). **Bevestigd werkend door de gebruiker** op de live DB (2026-09-13).

## Increment 6 — wat er is bijgekomen ("Mijn taken")

- Enums: `BOCore/Enum/Traject/{TaakStatus,TaakPrioriteit,TaakHerkomst}.vb`
- Entiteit `DALCore/Models/ProjectTaak.cs` (+ partial `GetIdName`) + EF-config
  `DALCore/Models/cpmRunningContext.Taak.cs` (gewired in `cpmRunningContext.Seeding.cs`)
- Migratie `_migrations/039_ProjectTaak.sql` (zie hierboven — nog te draaien)
- BO's: `BOCore/BO/Project/Traject/TaakUpsertBO.vb`, `BOCore/Filter/TaakFilterBO.vb`
- `FacadeCore/IProjectTaakService.cs` + `ServiceCore/Traject/ProjectTaakService.cs`
- `ServiceCore/Traject/TriggerActions/MaakTaakAction.cs` — vult de tot dan toe ontbrekende
  `TriggerActie.MaakTaak`-handler in, geregistreerd in `Program.cs`
- `CPMCore/Controllers/MijnTakenController.cs` (`/MijnTaken`, cross-project met dezelfde
  projectzichtbaarheid-scoping als `HomeController`/`ProjectenController` via
  `IProjectService.GetProjectsForList`)
- `CPMCore/Views/MijnTaken/Index.cshtml` — KPI's, filters, inline status wijzigen, "nieuwe taak"-modal
- `CPMCore/Views/Shared/_MijnTakenWidget.cshtml` — zelfvoorzienend widget (injecteert
  `IProjectTaakService` zelf), ingebed in alle 3 bestaande dashboards
  (`_DashboardProjectleider/CeoCfo/Boekhouding.cshtml`)
- `CPMCore/Views/Shared/_ModalTaakQuickAdd.cshtml` + `CPMCore/Models/Traject/TaakQuickAddVm.cs` —
  herbruikbare "Taak aanmaken"-knop/modal met context (project/mijlpaal/dossier/punt); ingebed op de
  dossier-detailpagina (`ProjectDossiers/Details.cshtml`); increment 7 voegde de mijlpaal- en
  punt-detail-embeds toe (zie increment 7, punt 6).
- Navigatie-item "Mijn taken" toegevoegd aan beide sidebar-varianten
  (`_LeftSidebarPartial.cshtml`/`_LeftSidebarPartialA.cshtml`), permissie-gated op
  `PermissionCodes.MijnTaken` (stond al in `PermissionCatalog.vb`, geen migratie nodig — permissies zijn
  code-gedreven, niet DB-geseed).

Build geverifieerd: 0 `error CS`/`error RZ` op dat moment (enkel routineuze MSB3021/3027-file-lock-ruis
van de VS-sessie van de gebruiker, en MVC1000-warnings).

## Increment 7 — klaar

1. ✅ **`/Deadlines`** — portfolio-brede pagina, af. `IMijlpaalService.SearchPortfolio(projectIds, filters)`
   toegevoegd (nieuwe methode, `MijlpaalService.cs`); `MijlpaalFilterBO.ProjectId` filterveld toegevoegd.
   `CPMCore/Controllers/DeadlinesController.cs` (`/Deadlines`, permissie `PortfolioDeadlines`, zelfde
   zichtbaarheid-scoping als `MijnTakenController` via `IProjectService.GetProjectsForList`) +
   `Views/Deadlines/Index.cshtml` (KPI-strip, filters op project/status/rol/achterstallig, DataTable) +
   `Models/Traject/DeadlinesIndexVm.cs`. Nav-item "Deadlines" toegevoegd aan beide sidebars. **Geen
   nieuwe tabel/migratie nodig** — leest bestaande `Mijlpaal`-data. Build geverifieerd (0 errors).
2. ✅ **`DashboardType.Ontwikkelaar` (4) + `DashboardType.Verkoper` (5)** toegevoegd
   (`CPMCore/Models/DashboardType.cs`) + geselecteerbaar gemaakt in de admin-UI
   (`Views/UserAdmin/Modals/_UserAdminModals.cshtml`, twee `<select id="edit-dashboard-type">`'s).
   `Views/Home/Index.cshtml` dispatcht nu ook naar `_DashboardOntwikkelaar.cshtml` (KPI-strip +
   "vergunningen in opvolging"-tabel + "projecten per fase"-funnel + keypoints/taken-widgets) en
   `_DashboardVerkoper.cshtml` (KPI-strip + "verkooppijplijn per eenheid"-tabel + "eenheden per
   verkoopstatus"-funnel + keypoints/taken-widgets).
3. ✅ **`_MijnKeypointsWidget.cshtml`** ("mijn mijlpalen", apart van "mijn taken" — achterstallig/rood,
   ≤14 dagen/oranje, verder/grijs; filtert op `VerantwoordelijkeUserId == ik OR VerantwoordelijkeRol ==
   mijn (uit DashboardType afgeleide) rol`) — zelfvoorzienend zoals `_MijnTakenWidget`, nu ingebed in
   **alle 5** rol-dashboards (de 3 bestaande + de 2 nieuwe).
4. ✅ **"Aandacht vereist"** op `Projecten/Detail.cshtml` gevoed met mijlpaal-waarschuwingen. Nieuwe
   `IMijlpaalService`-dependency in `ProjectenController` (was er nog niet); `Detail()` haalt de nog
   niet bereikte/n.v.t. mijlpalen van het project op (`ShowProjectDetail.AttentionMijlpalen`,
   `ProjectModel.cs`) en toont ze als "Mijlpaal achterstallig" (urgent, rood) / "Mijlpaal nadert"
   (≤14 dagen, "op te lossen") in het bestaande `gl-mc-*`-paneel, met een nieuwe filterchip
   "Traject" naast Contracten/Werf/Financieel. Link gaat naar `ProjectTraject/Index` (er bestaat geen
   losse mijlpaal-detailpagina om naar door te linken).
5. ✅ **Permissie-/rol-polish + perf-indexen + "Punten" als gekoppelde component.**
   - **Rol-mapping-bug gevonden en gefixt**: de `DashboardType → InterneRol`-switch (bepaalt "mijn rol"
     voor de keypoints/taken-widgets) had nog geen cases voor de twee nieuwe dashboardtypes —
     Ontwikkelaar en Verkoper kregen dus `rol = null` en zagen enkel taken/mijlpalen op hun eigen
     `UserId`, nooit op hun rol. Gefixt op de twee plekken waar die switch stond:
     `_MijnKeypointsWidget.cshtml` en `MijnTakenController.BepaalRollen`.
   - **`_migrations/040_TrajectPerfIndexen.sql`** (nog te draaien, zie migratie-sectie) — 2 indexen op
     `Mijlpaal.VerantwoordelijkeUserId`/`VerantwoordelijkeRol`, de kolommen waarop de
     portfolio-brede `SearchPortfolio`-scan nu op elke dashboard-load filtert (5 dashboards).
   - **"Punten" zichtbaar als gekoppelde component**: `TrajectIndexVm.PuntenPerMijlpaal` (nieuw,
     `Dictionary<MijlpaalId, aantal>`) geeft het aantal distinct `ConstructionIssue`'s dat via een
     `ProjectTaak` (met zowel `MijlpaalId` als `ConstructionIssueId` gezet) aan een mijlpaal hangt.
     Gevuld in `ProjectTrajectController.Index()`, getoond als klein badge-icoontje naast de mijlpaal
     in zowel de hoofdtabel (`ProjectTraject/Index.cshtml`) als de per-eenheid-matrix
     (`_UnitMatrix.cshtml`) — enkel zichtbaar als er effectief gekoppelde punten zijn.
6. ✅ **"Maak taak"-quickadd op mijlpaal-detail en punt-detail.**
   - **Punt-detail** (`ProjectsIssues/Details.cshtml`): identiek patroon als dossier-detail — knop in
     de header + `_ModalTaakQuickAdd` met `ConstructionIssueId` vast ingevuld.
   - **Mijlpaal**: er bestaat geen losse mijlpaal-detailpagina (mijlpalen worden bewerkt via de
     AJAX-gevoede `_ModalMijlpaalStatus`-modal op `ProjectTraject/Index`), dus de quickadd zit als
     knop in de footer van díe modal (`mps-btn-taak`). `_ModalTaakQuickAdd.cshtml`'s hidden velden
     kregen `id`-attributen (`{ModalId}-project-id`/`-mijlpaal-id`/etc.) zodat `traject.index.js` het
     `MijlpaalId` en een default-titel kan invullen vanuit de al-geladen status-modal (`mps-id`/
     `mps-naam`) vóór het openen van de quickadd-modal.
7. ✅ **Ontwikkelaar-dashboard: "projecten per fase"-funnel** — actieve fase (`FaseStatus.Actief`) per
   projecttraject in de portfolio, gegroepeerd op naam+volgorde, als horizontale bar-funnel
   (`.gl-funnel*`, nieuw in `dashboard-projectleider.css`). **Verkoper-dashboard: rechtstreekse
   `ClientAccount`-koppeling** — "eenheden per verkoopstatus"-funnel (Beschikbaar/In optie/Verkocht/
   Akte verleden), dezelfde categorisering als de unit-rijen op `Projecten/Detail.cshtml`
   (`Units.ClientAccountId`/`IsOption` + `ClientAccount.DateDeedOfSale`), rechtstreeks via
   `cpmRunningContext.Units` i.p.v. enkel afgeleid uit de per-eenheid-mijlpaal-bindingen. Beide
   funnels zijn een **eerste, functionele versie** (geen sjabloon-brede fase-normalisatie voor
   Ontwikkelaar; geen aparte pijplijn-trechter-UI met percentages voor Verkoper).

Build geverifieerd: 0 `error CS`/`error RZ` (enkel de routineuze MSB3021/3027-file-lock-ruis van de
VS-sessie van de gebruiker). **Nog niet live boot-getest** door mij — punt 5's indexmigratie (040) moet
nog draaien, en de nieuwe UI (aandachtspaneel-mijlpalen, funnels, mijlpaal-quickadd-knop) is nog niet
in de browser bevestigd.

## Live-testwerkwijze (gebruikt doorheen dit hele traject, blijft gelden)

- Schema-wijzigingen: **rechtstreeks op de live DB**, altijd via nieuwe genummerde, idempotente
  `_migrations/NNN_*.sql`-scripts. Nooit EF Core migrations. Bij een echt niet-additieve wijziging: DB
  kopiëren + connectiestring omleggen (nog niet nodig geweest in dit traject).
- Live verifiëren via `sqlcmd` (credentials via `dotnet user-secrets list --project CPMCore/CPMCore.csproj`)
  en/of een eigen `dotnet run --launch-profile https`-testinstance + `curl -sk` — niet enkel op
  "zou moeten werken" vertrouwen; dit vond meerdere echte bugs (dode `TriggerController`, cascade-path
  SQL-fout, ontbrekende sync-propagatie, select2-in-modal-bug, de vergunning-koppel-bug hierboven).
- Build-ruis: lang draaiende sessie kan resulteren in `MSB3021`/`MSB3027` "file locked by Visual Studio
  2022" fouten in de build-output — dit zijn **geen** echte compile-fouten zolang er geen `error CS`/
  `error RZ` bij staat; komt van de VS-sessie van de gebruiker die de DLL's vasthoudt.

## Referentie

Zie ook `.claude/plans/cd-cpmcore-tender-pizza.md` voor het volledige oorspronkelijke architectuurplan
(bredere visie-catalogus, volledig domeinmodel, risico's) — dit document hier is de "wat is er al, wat
nu" samenvatting daarbovenop.
