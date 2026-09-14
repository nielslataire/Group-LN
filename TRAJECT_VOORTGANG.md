# Trajectopvolging — voortgangsstatus

Doorlopend statusdocument voor de grote "trajectopvolging"-feature (werf/project-mijlpalen, dossiers,
triggers, taken). Origineel plan: `.claude`-sessie-plan (zie chatgeschiedenis) — dit bestand is de
werkende samenvatting om een volgende sessie snel weer op te starten. Bijwerken bij elke increment.

**Laatste update:** 2026-09-13 (increment 7 klaar; migratie 039 en de vergunning-koppel-bugfix
bevestigd werkend door de gebruiker op de live DB).

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
