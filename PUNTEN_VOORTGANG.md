# Punten (design-handoff 40/41) — voortgang en overdracht

Laatst bijgewerkt: 09/10/2026 (avond). Bron van het ontwerp: `design-handoff/CRM Punten.dc.html` (punt 40a–40o, 41a–d). Uitleg per scherm staat ook in `DESIGN.md` (sectie "Update 08/10/2026 — Punten").

## Beslissingen van Niels
- Alles bouwen, inclusief Verslagen en spraak/rondgang; **in fasen met een tussentijdse stop** na elke fase.
- Statussen: nieuwe reeks + omzetting van bestaande punten (migratie 079).
- Plannen: bestaande `UnitExecutionPlan` + plancoördinaten op het punt hergebruiken (geen nieuw plan-model).
- "In de wacht" (deadline pauzeert, reden, opvolgdatum) en berichten intern/naar aannemer: ja.
- Spraak: OpenAI `gpt-4o-mini-transcribe` voor transcriptie + Claude API voor het herkennen van punten, achter een interface. **Nodig: API-sleutels `OpenAI:ApiKey` en `Anthropic:ApiKey`** (ChatGPT Pro / Claude Max abonnementen werken hiervoor niet; wel een API-account met bijgestort tegoed en een maandlimiet). Tot dan: stub / uitgeschakelde knoppen.
- De lijst moet een echte DataTable zijn (paginagrootte uit schermhoogte, zoals Leveranciers/Facturen) — gedaan.

## Fases
| Fase | Inhoud | Stand |
|---|---|---|
| 1 | Statussen + migratie 079, lijst (40a), selectiebalk (40n), detail (40o) | **Gebouwd**, niet browser-getest |
| 2 | Snel ingeven-paneel (40c) + Goedkeuren & doorsturen (40d) | **Gebouwd** 08/10/2026, niet browser-getest |
| 3 | Op plan (40b) + tablet plaatsen (40k), bestaande plannen hergebruiken | **Gebouwd** 09/10/2026, niet browser-getest, migratie 081 |
| 4 | Verslagen (40i, 40l): nieuwe tabellen + migratie + menu-item "Verslagen" | **Gebouwd** 09/10/2026, niet browser-getest, migratie 082 |
| 5 | Gsm-flows (41a Typen, 41b Foto) + snelactiesbar (40e) | Open |
| 6 | Spraak (41c, 40f) en rondgang (41d, 40g/h): transcriptie + puntherkenning | Open, sleutels nodig |

## Wat fase 1 heeft aangepast
**Database (migratie `_migrations/079_PuntenStatussenEnOpvolging.sql`, nog door Niels uit te voeren op de live DB vóór gebruik):**
`ConstructionIssue`: `PuntNr`, `OnHoldSince`, `OnHoldReason`, `FollowUpDate`; `ConstructionIssueHistory.IsInternal`. Eenmalige statusomzetting: Open/Toegewezen/Gepland/Heropend → Doorgestuurd (10), Klaar voor controle → Gemeld uitgevoerd (11), Opgelost → Afgesloten (5).

**Statusreeks** (`BOCore/Enum/ConstructionIssue/ConstructionIssueStatus.vb`): Concept 8 · Ter goedkeuring 9 · Doorgestuurd 10 · Gemeld uitgevoerd 11 · In de wacht 12 · Afgesloten 5 · Afgewezen 6 (legacy waarden 0–4 en 7 blijven bestaan voor compatibiliteit). `CPMCore/Helpers/PuntenWeergave.cs`: label, badge-toon, `Canon()` (legacy → nieuwe status), statusverloop `Next()`.

**Backend:** `ServiceCore/ConstructionIssueService.cs` (PuntNr bij aanmaken, `PutOnHold`, `AddMessage`, deadline schuift op bij hervatten), `FacadeCore/IConstructionIssueService.cs`; `ConstructionIssueReportService.cs` (statuslabels/PDF); `ContractorPortalController.cs` en `ContractorPortalDigestService.cs` (nieuwe statussen, interne berichten verborgen); `ProjectIssuesController.cs` is nu `partial`, nieuw `ProjectIssuesController.V2.cs` (BuildIndexV2, BuildDetailV2, `HoldV2`, `MessageV2`, `BulkV2`); `Index`/`Details` nemen `classic=true` om de oude pagina te tonen.

**Views / CSS / JS:** `Views/Projecten/Issues/IndexV2.cshtml`, `DetailsV2.cshtml`; `Models/Issues/PuntenV2Vm.cs`; `wwwroot/css/gl-v2-projecten-punten.css` + `gl-v2-projecten-punt-detail.css`; `wwwroot/js/gl-v2-projecten-punten.js` (DataTable, filters, selectiebalk) + `gl-v2-projecten-punt-detail.js`.

**Bekende beperkingen:** geen Bewerken in het detail (wel snel ingeven voor nieuwe punten); geen kaartweergave op gsm (tabel scrolt horizontaal); plan toont nog geen pin.

## Fase 2 — gebouwd
**40c Snel ingeven:** knop "Punt toevoegen" (en gsm-quickaction) opent een zijpaneel in `IndexV2.cshtml`: titel, eenheid, zone (met recent gebruikte chips), aannemer, foto's, en onder "Meer" beschrijving/categorie/type/fase/prioriteit/deadline. Opslaan / Opslaan & volgende (Enter) posten via fetch naar `CreateV2` (`ProjectIssuesController.V2.cs`); het punt wordt Concept met een nieuw `P-0xx`. Standaardcategorie = eerste categorie als er geen gekozen is. JS: `gl-v2-projecten-punten.js` (blok "Snel ingeven"), CSS: `gl-v2-projecten-punten.css`. Nog niet: "Locatie op plan" (fase 3), spraak/bijlage.
**40d Goedkeuren & doorsturen:** `GET/POST Projects/{id}/Issues/Send` (`SendV2` / `SendV2Post`), view `SendV2.cshtml`, `gl-v2-projecten-punten-send.css/.js`. Keuze "Welke punten" (nieuw + open / enkel nieuw / enkel reeds goedgekeurd), bericht, aannemerstabel met aantallen, live mailvoorbeeld, samenvatting + "Verzenden (N)". De server herberekent de punten, zet Concept/Ter goedkeuring op Doorgestuurd en roept `SendSelectedIssues` aan (PDF + mail per aannemer) en nodigt portaalgebruikers uit. Aannemers zonder e-mailadres worden overgeslagen en gemeld. De knop "Goedkeuren" in de selectiebalk opent deze pagina met de geselecteerde punten (`?ids=`), de knop "Goedkeuren & doorsturen" staat ook in de topbalk van de lijst.
Let op: aannemers komen uit het contract van het project (site manager e-mail, dan factuur-/bedrijfsmail); punten zonder aannemer worden niet verstuurd. "Gemeld uitgevoerd", "In de wacht" en "Afgesloten" gaan nooit mee.

## Fase 3 — gebouwd (Op plan)
- **Migratie `_migrations/081_PuntenPlannenAlgemeen.sql`** (vóór gebruik uitvoeren): `UnitExecutionPlan.ProjectId` + `UnitId` wordt NULL-baar voor algemene plannen (inplantingsplan, gevels). Entity `DALCore/Models/UnitExecutionPlan.cs` aangepast.
- **Pagina Op plan** (`PlanV2.cshtml`, route `Projects/{id}/Issues/Plan?key=&issue=`): tabs Lijst | Op plan (`Partials/_PuntTabsV2`), plannen links (groep Algemeen + per eenheid, met aantal punten, zoek, "Plan uploaden"), PDF-plan in het midden (pdf.js, paginanavigatie + zoom) met pins in statuskleur, detail van de gekozen pin rechts (Heropenen / Goedkeuren & afsluiten / Openen). Klik op het plan = nieuw punt met eenheid, plan, pagina en positie al ingevuld (dat is ook de tablet-variant 40k).
- **Plan-id-conventie:** `PlanDocumentId` = `UnitExecutionPlan.Id` (enkel uitvoeringsplannen; het verkoopplan `Units.Plan` wordt niet getoond); positie is genormaliseerd (`PlanXnormalized/Ynormalized` 0..1) + `PlanPageNumber`. Plansleutel in de UI: `u{unitId}:{planId}` of `a:{planId}`.
- **Zijpaneel** is nu een gedeelde partial (`Partials/_PuntPanelV2`) + `wwwroot/js/gl-v2-punt-panel.js` (`window.PuntPanel.open(mode, id, preset)`), met een sectie "Locatie op plan" (plan kiezen volgens de eenheid, tik op het plan om de pin te plaatsen, "Locatie wissen"). `CreateV2`/`EditV2` nemen `planId/planPage/planX/planY` (`clearPlan` bij bewerken).
- **Detail van een punt** toont het plan met de pin (alleen lezen) + "Openen op plan".
- Nieuwe/gewijzigde bestanden: `ProjectIssuesController.V2.cs` (PlansV2, UploadPlanV2, PlanV2, LoadPlans, PlanBelongs), `gl-v2-punt-planviewer.js/.css` (herbruikbare viewer `PuntPlanViewer`), `gl-v2-projecten-punten-plan.js/.css`.
- Beperkingen: pins verplaatsen kan enkel via Bewerken; plannen hernoemen/verwijderen kan nog niet vanuit Op plan; algemene plannen kunnen enkel hier geüpload worden.
- Avondupdate (aannemersmeldingen) meldt nu elke wijziging van vandaag, behalve punten waarvan de eerste mail al vandaag verstuurd is en die daarna niet meer gewijzigd werden (`IssueNotificationSenderService`).

## Andere open punten (buiten Punten)
- Migraties die Niels nog moet uitvoeren: 074 (mijlpaal relatieve datum), 079 (punten); 075–078 en 080 zijn budget-gerelateerd en komen van een andere werkstroom.
- Niets van deze sessie is in een browser getest (Traject/Dossiers, bedrijf-bewerken-tabs, Punten): eerst door-klikken.
- Zimmo geeft 403 vanaf Hetzner; Exchange-toegangsbeleid voor Graph Mail.Send nog niet ingesteld (zie memory-notities op de andere pc, niet in de repo).

## Werkafspraken die ik tijdens deze sessie volgde
- Dutch in alle UI-teksten; gl-v2 klassenprefix per pagina (`gl-v2-pt-*`, `gl-v2-pd-*`); page-CSS laadt vóór `gl-v2-shell.css` (extra specificiteit nodig); `[hidden]` expliciet guarden.
- Hand-gedraaide idempotente SQL in `_migrations/NNN_*.sql`, nooit EF-migraties.
- Build: `dotnet build CPMCore/CPMCore.csproj`; MSB3021/3027-fouten zijn bestandsvergrendeling van Visual Studio en mogen genegeerd worden, enkel `error CS|RZ` tellen. JS: `node --check`.
- Bash-heredocs met apostrofs kunnen falen in deze omgeving: schrijf een script met de Write-tool en voer het uit.

## Statusflow bijgewerkt 09/10/2026 — nieuwe status "Goedgekeurd" (13)
Concept → Ter goedkeuring → **Goedgekeurd** (klaar om te versturen) → Doorgestuurd → Gemeld uitgevoerd → Afgesloten (+ Afgewezen, In de wacht). De aannemer ziet een punt pas na het versturen.
- **Goedkeuren** (selectiebalk, snel-ingeven-paneel "Meteen goedkeuren", of status kiezen in legacy) zet enkel Goedgekeurd, zonder mail.
- **Avondmelding** (`IssueNotificationSenderService`) neemt Goedgekeurd mee (herinneringen niet), mailt, en zet het punt daarna op Doorgestuurd (met historiekregel).
- **Doorsturen… / Goedkeuren & doorsturen** (pagina Send) verstuurt meteen; Concept, Ter goedkeuring en Goedgekeurd tellen daar als "nieuw".
- Geen migratie nodig (statuswaarde 13 in de bestaande kolom). Legacy lijst/modals/KPI's kennen de status ook.

## Fase 4 — gebouwd (Verslagen)
- **Migratie `_migrations/082_Werfverslagen.sql`** (vóór gebruik uitvoeren): tabellen `Werfverslag` (type, nummer, naam, datum, uur, aanwezigen als JSON, weer, opmerkingen, volgend bezoek, status Concept/Verstuurd, hernomen van) en `WerfverslagPunt` (punt in verslag, `IsNieuw`, `TerPlaatse` 0 blijft open / 1 opgelost / 2 afgesloten, opmerking). Entities: `DALCore/Models/Werfverslag.cs` + `cpmRunningContext.Werfverslag.cs`.
- **Nieuwe controller `ProjectVerslagenController`** (route `Projects/{id}/Verslagen`, views in `Views/Projecten/Verslagen`, rechten = ProjectsIssues, menu-item "Verslagen" onder Opvolging). Acties: Index (lijst + samenvatting, 40i), Start (hernemen van het vorige verslag: aanwezigen, opmerkingen, alle open punten), Edit (40l), Save/Point/AddPoint (JSON, autosave), Pdf (voorbeeld), Finish, Delete (enkel concept).
- **Werfbezoek hernemen:** per openstaand punt Blijft open / Opgelost / Afgesloten + opmerking; nieuwe punten typen + Enter (concept, daarna Bewerken via het gedeelde zijpaneel voor eenheid/aannemer). Eén lopend concept per type.
- **Afronden:** Afgesloten → status Afgesloten; Opgelost → Gemeld uitgevoerd; opmerkingen komen intern in de historiek van het punt; nieuwe punten blijven **Concept** (niet automatisch goedgekeurd: er kunnen nog foto's/details bij; goedkeuren daarna bij Punten, dan gaan ze naar de aannemer die op het punt gekozen is); verslag-PDF (`Documents/GlV2/WerfverslagDocumentV2.cs`, A4, gl-v2-opmaak) per e-mail (`IEmailSender`) naar de aanwezigen met e-mailadres; status Verstuurd.
- **Punten ↔ verslagen:** filter "Verslag" op de puntenlijst en "Aan verslag…" in de selectiebalk (voegt punten toe aan een lopend concept-verslag).
- Beperkingen: oplevering start met een eigen naam maar verder dezelfde flow; verslag-PDF gebruikt de bedrijfsgegevens uit appsettings `GlV2PdfCompany` (niet het facturatiebedrijf van het project); aanwezigen zijn namen + optioneel e-mail (geen koppeling met contactenlijsten, wel suggesties uit de werfleiders van de aannemers).
- **Aanwezigen:** zoekveld (gl-v2-combo, POST `Aanwezigen`) met enkel interne medewerkers (geen portaalgebruikers) en de aannemers met een contract voor de werf + hun contactpersonen en werfleider (geen klanten); het e-mailadres wordt automatisch overgenomen (id = e-mailadres).
- **Nieuw punt in een verslag:** naast de titel een aannemer-keuze (`AddPoint` neemt `contractorId` mee); eenheid/details nog via het potloodje (zijpaneel).
- **Verwijderen:** concept = schrijfrecht; verstuurd = enkel beheerder (`ISecurityService.UserIsAdmin`).
- **Aannemer bij een punt** is overal een keuzelijst met zoekveld die enkel aannemers met een contract voor de werf toont (zijpaneel + nieuw punt in een verslag).
- **Verslag-bewerkpagina onderaan:** "Opslaan & terug naar verslagen" (schrijft openstaande wijzigingen eerst weg) + "Afronden: …"; een verstuurd verslag heeft enkel "Terug naar verslagen".

## Stand van zaken 09/10/2026 (avond)
| Onderdeel | Stand |
|---|---|
| Fase 1 lijst/detail/selectie, statussen | gebouwd (incl. nieuwe status Goedgekeurd 13) |
| Fase 2 snel ingeven + goedkeuren & doorsturen | gebouwd |
| Fase 3 Op plan (enkel uitvoeringsplannen) | gebouwd |
| Fase 4 Verslagen | gebouwd, incl. aanwezigen-zoekveld, verwijderen (admin voor verstuurd) |
| Legacy puntenpagina + aannemersportaal + e-mails + avondmelding | aangepast aan de nieuwe statussen |
| **Fase 5 gsm (snelactiesbar, kaartlijst, foto-flow)** | **open — volgende stap** |
| **Fase 6 spraak/rondgang** | open, wacht op OpenAI + Anthropic API-sleutels |

**Nog uit te voeren door Niels (live DB):** migraties 079, 081, 082 (083–085 en 080 zijn andere werkstromen). Fout "Byte → Int32" bij verslagen is opgelost in het datamodel (kolommen zijn TINYINT, `HasConversion<byte>`); app opnieuw starten.
**Niet browser-getest:** alles uit fase 1–4 — eerst één punt en één verslag van begin tot einde doorlopen (aanmaken, goedkeuren, doorsturen, afronden).
**Bekende beperkingen:** pins verplaatsen enkel via Bewerken; plannen hernoemen/verwijderen niet vanuit Op plan; geen kaartweergave op gsm; "Per aannemer"-tab uit het ontwerp niet gebouwd; verslag-PDF gebruikt bedrijfsgegevens uit appsettings (`GlV2PdfCompany`).
