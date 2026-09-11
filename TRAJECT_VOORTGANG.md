# Trajectopvolging — voortgangsstatus

Doorlopend statusdocument voor de grote "trajectopvolging"-feature (werf/project-mijlpalen, dossiers,
triggers, taken). Origineel plan: `.claude`-sessie-plan (zie chatgeschiedenis) — dit bestand is de
werkende samenvatting om een volgende sessie snel weer op te starten. Bijwerken bij elke increment.

**Laatste update:** 2026-09-11 (increment 6 klaar; increment 7 gestart: /Deadlines,
Ontwikkelaar/Verkoper-dashboards en _MijnKeypointsWidget klaar).

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
| 7 | Rol-overzichten/dashboards/permissies/polish | 🔶 Gestart — Deadlines-pagina + Ontwikkelaar/Verkoper-dashboards + keypoints-widget klaar, rest nog open |

## Migraties — **nog uit te voeren op de live DB indien nog niet gebeurd**

Volgorde is belangrijk (`_migrations/029` t/m `039`). Controleer met `sqlcmd` of `sys.tables` de tabel al
bevat vóór je een script opnieuw draait — alle scripts zijn idempotent (`PRINT ... overgeslagen` bij
tweede run, geen destructieve DDL).

- `029_TrajectMijlpalen.sql` … `038_TrajectDossierSubstappen.sql` — increments 1 t/m 5, zouden al
  gedraaid moeten zijn (increment 5 was live getest deze sessie).
- **`039_ProjectTaak.sql`** — **NIEUW, nog niet bevestigd gedraaid.** Maakt tabel `ProjectTaak` aan
  (increment 6, "Mijn taken"). Geen destructieve wijzigingen, veilig meermaals uitvoerbaar.

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
handmatige stap nodig). **Nog te bevestigen door de gebruiker**: na rebuild op project 73 "Sync met
sjabloon" draaien + herberekenen, dan moet `VERGUNNING_INGEDIEND` op Bereikt springen (12/09/2026).

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
  herbruikbare "Taak aanmaken"-knop/modal met context (project/mijlpaal/dossier/punt); nu ingebed op
  de dossier-detailpagina (`ProjectDossiers/Details.cshtml`). **Nog niet** ingebed op mijlpaal-detail of
  punt-detail — zie openstaande punten.
- Navigatie-item "Mijn taken" toegevoegd aan beide sidebar-varianten
  (`_LeftSidebarPartial.cshtml`/`_LeftSidebarPartialA.cshtml`), permissie-gated op
  `PermissionCodes.MijnTaken` (stond al in `PermissionCatalog.vb`, geen migratie nodig — permissies zijn
  code-gedreven, niet DB-geseed).

Build geverifieerd: 0 `error CS`/`error RZ` op dat moment (enkel routineuze MSB3021/3027-file-lock-ruis
van de VS-sessie van de gebruiker, en MVC1000-warnings).

## Increment 7 — in uitvoering

**Klaar:**
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
   "vergunningen in opvolging"-tabel over de hele portfolio + keypoints/taken-widgets) en
   `_DashboardVerkoper.cshtml` (KPI-strip + "verkooppijplijn per eenheid"-tabel, per-eenheid mijlpalen
   via `IMijlpaalService.SearchPortfolio(..., AlleenProjectniveau:false)` + keypoints/taken-widgets).
   Beide zijn een **eerste, functionele versie** — geen "fase-funnel" of aparte `ClientAccount`-query
   (leunen bewust op de al bestaande mijlpaal-bindingen i.p.v. een parallel datapad).
3. ✅ **`_MijnKeypointsWidget.cshtml`** ("mijn mijlpalen", apart van "mijn taken" — achterstallig/rood,
   ≤14 dagen/oranje, verder/grijs; filtert op `VerantwoordelijkeUserId == ik OR VerantwoordelijkeRol ==
   mijn (uit DashboardType afgeleide) rol`) — zelfvoorzienend zoals `_MijnTakenWidget`, nu ingebed in
   **alle 5** rol-dashboards (de 3 bestaande + de 2 nieuwe).

Build na dit alles opnieuw geverifieerd: 0 `error CS`/`error RZ`. **Nog niet live boot-getest** door mij
deze ronde (geen nieuwe DI-registraties nodig, dus laag risico, maar nog te bevestigen door de
gebruiker net als de rest).

**Nog te doen:**
4. **"Aandacht vereist"** op `Projecten/Detail.cshtml` voeden met mijlpaal-waarschuwingen (`gl-mc-*`).
5. Permissie-/rol-polish, perf-indexen, "Punten" zichtbaar als gekoppelde component (tellingen op
   mijlpalen, `ProjectTaak.ConstructionIssueId`) — geen tabelmigratie.
6. "Maak taak"-quickadd ook op mijlpaal-detail en punt (`ConstructionIssue`)-detail (nu enkel op
   dossier-detail).
7. Ontwikkelaar-dashboard: échte "projecten per fase"-funnel (nu enkel vergunning-board); Verkoper-
   dashboard: rechtstreekse `ClientAccount`-koppeling i.p.v. enkel de per-eenheid-mijlpaal-bindingen.

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
