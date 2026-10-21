# Punten (design-handoff 40/41) — voortgang en overdracht

Laatst bijgewerkt: 14/10/2026 (avond). Bron van het ontwerp: `design-handoff/CRM Punten.dc.html` (40a–40t, 41a–d) en `CRM Verslag verwerken.dc.html` (44). Uitleg per scherm: `DESIGN.md`, secties "Update 08/10/2026 — Punten" tot "Update 14/10/2026 — Punten, fase 6".

## START HIER — overdracht naar een andere pc (14/10/2026)

**Stand: alle fases 1 t/m 6 zijn gebouwd.** Niets is nog gecommit in deze sessie (branch `layout-experiment`, ~175 gewijzigde bestanden): commit en push op de eerste pc, pull op de tweede. De codebase compileert (`dotnet build CPMCore/CPMCore.csproj`, enkel `error CS|RZ` tellen). Werfverslag, punten op gsm, plan-sheets, Inspreken en Rondgang zijn door Niels deels getest (verslag, PDF, voorbereid-pagina en verlaat-modal werkten); Inspreken/Rondgang/gsm-camera zijn **nog niet** op een toestel of met echte sleutels getest.

**1. Door Niels uit te voeren (database en configuratie)**
- Migraties 079, 081, 082, 097, 098 zijn gedraaid. **Nog te draaien: `_migrations/100_PuntRondgang.sql`** (tabellen `PuntRondgang`, `PuntRondgangFoto`) vóór het gebruik van Rondgang. (099 is de documentmap Budgetten, een andere werkstroom.)
- Sleutel voor spraak: `PuntenSpraak:OpenAiApiKey` (user-secrets lokaal: `dotnet user-secrets set "PuntenSpraak:OpenAiApiKey" "…"`, op de server `PuntenSpraak__OpenAiApiKey`). Anthropic gebruikt automatisch `QuoteExtraction:AnthropicApiKey`. Modellen en limieten staan in `Services/PuntenSpraak/PuntenSpraakOptions.cs`.
- Microfoon en camera werken enkel via https.

**2. Wat er allemaal is (kort)**
- Punten lijst/detail/selectie/goedkeuren & doorsturen/op plan/status/verwijderen (detail heeft Status + Verwijderen met bevestiging, ook op gsm in de snelactiesbar).
- Werfverslagen: bewerken (autosave + verlaat-modal), PDF volgens 35k/35l/35m (`Documents/GlV2/WerfverslagDocumentV2.cs`, bijlage per plan, doorlopende paginanummers), voorbereid-pagina volgens punt 44 (geen overlay; `#vs-prep`), afronden keurt de nieuwe punten goed en mailt de PDF.
- Gsm (fase 5): snelactiesbar (Punt, Foto, Inspreken, Rondgang, daarna Meer), kaartlijst, KPI's verborgen, camera + markeren, eenheid-chips, plankiezer (40s) en nieuw plan (40t) als bottom sheets met "Foto van plan" + perspectiefcorrectie. Bestanden: `gl-v2-punt-mobile.js`, `gl-v2-punt-plansheets-m.js/.css`, `gl-v2-projecten-punten-mobile.css`.
- Spraak (fase 6): Inspreken (41c/40f), titel inspreken, Rondgang (41d/40g/40h). Backend `Services/PuntenSpraak/*` + `Controllers/ProjectIssuesController.Spraak.cs`, front `gl-v2-punt-spraak-m.js/.css`. Rondgangen staan in de database + bestandsopslag (map `docs`), worden verwerkt door `RondgangHostedService`, 14 dagen bewaard.
- Help: `HelpContent/punten-verslagen/` heeft artikels voor werfverslag opmaken/afronden, punt toevoegen + plan, goedkeuren + statusflow, avondmelding, op de werf met foto, inspreken en rondgang.

**3. Wat Niels nog moet testen** (volgorde)
1. Werfverslag: verslag met plan + pin + foto, Voorbeeld PDF, afronden, mail.
2. Gsm: kaartlijst, Punt, Foto (camera + markeren), plan kiezen/nieuw plan met Foto van plan en rechttrekken, Status/Verwijderen op detail.
3. Inspreken: bericht met twee punten, titel inspreken.
4. Rondgang: korte opname met foto's; pagina sluiten tijdens verwerken; balk terug; Beluister; bewaren.
5. Help via het "?"-paneel.

**4. Risico's om als eerste na te kijken**
- Bestandsopslag moet de map `docs` voor audio aanvaarden (anders: "De opname kon niet opgeslagen worden").
- Safari/iPhone: MediaRecorder-ondersteuning.
- Kwaliteit van de AI-voorstellen (prompt in `PuntenSpraakService.SystemPrompt`) na echte proeven bijstellen.

**5. Bewust niet gebouwd / open**
- Verzenden per aannemer met voortgang (44b) en automatisch doorgaan naar het verslag na afronden.
- Automatische hoekdetectie bij plan rechttrekken; aannemervoorstel op type punt (op kamer is afgewezen); live markeringen en video bij de rondgang; "Ongedaan maken" na bewaren (vervalt).
- Niet in de verslag-PDF/pagina: list-pagina "Voorbeeld PDF" (GET) heeft geen plannen-bijlage; "Verontschuldigd" en eind-uur worden niet bewaard.

**6. Werkafspraken**
- Dutch UI-teksten; gl-v2 klassenprefix per pagina; page-CSS laadt vóór `gl-v2-shell.css`; `[hidden]` expliciet guarden.
- Schemawijzigingen = hand-gedraaide idempotente SQL in `_migrations/NNN_*.sql`, nooit EF-migraties.
- Help schrijven: `HELP_SCHRIJFGIDS.md` (je-vorm, geen gedachtestreepjes, onbekend = `[VRAAG: …]`) en DESIGN.md "Een nieuw help-artikel schrijven".
- Bash-heredocs met apostrofs falen soms: schrijf een script met de Write-tool en voer het uit. Memory-notities van Claude staan lokaal op de eerste pc (`~/.claude/projects/e--TFS/memory`) en gaan niet mee: deze nota is de bron.

---

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
| 5 | Gsm-flows (41a Typen, 41b Foto) + snelactiesbar (40e) + plan-sheets (40s/40t) | **Gebouwd** 14/10/2026, niet op toestel getest |
| 6 | Spraak (41c, 40f) en rondgang (41d, 40g/h): transcriptie + puntherkenning | **Gebouwd** 14/10/2026, migratie 100 + OpenAI-sleutel nodig, niet getest |

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

## Plan kiezen / nieuw plan (design-handoff 40c, 40p, 40q, 40r) — gebouwd 13/10/2026
Opnieuw opgebouwd naar het bijgewerkte ontwerp (de eerste versie met "+ Plan toevoegen" in een dropdown is vervangen):
- **Migratie `_migrations/097_PuntenPlanSoortNiveauVersie.sql`** (vóór gebruik uitvoeren): `UnitExecutionPlan.Soort`, `Niveau`, `Versie` (zelfde naam in dezelfde eenheid/het project = nieuwe versie).
- **40r lege staat** in het paneel ("Nog geen plan gekozen", aantal beschikbare plannen voor de eenheid, knoppen *Plan kiezen* en *Nieuw plan*, uitleg dat het punt zonder plan niet in Op plan en niet op de plannen van het verslag verschijnt).
- **40c plan gekozen**: klein voorbeeld met de pin, titel van het plan, knop *Ander plan*; klikken op het voorbeeld vergroot het plan (modal met paginanavigatie/zoom) om het punt te plaatsen → *Hier plaatsen*.
- **40p Plan kiezen** (modal): zoeken, segment "<eenheid> + algemeen" / "Alle eenheden", gegroepeerd per eenheid met soort · niveau · versie · datum en aantal punten, enkel de nieuwste versie, onderaan "Nieuw plan toevoegen", *Dit plan gebruiken*.
- **40q Nieuw plan** (modal): Hoort bij (eenheid of Algemeen), Soort, Naam (standaard eenheid + niveau), Niveau, Bestand (PDF, PNG of JPG; toont grootte · pagina's · papierformaat), info over nieuwe versie, *Terug naar plannen*, *Toevoegen & gebruiken* (plan wordt meteen bij het punt gekozen).
- Viewer (`gl-v2-punt-planviewer.js`) kan nu ook PNG/JPG tonen (`kind` per plan). Server: `UploadPlanV2` neemt soort/niveau, bepaalt de versie; `LoadPlans` levert soort, niveau, versie, datum, aantal punten, kind, isLatest.
- **Afwijkingen t.o.v. het ontwerp:** het plan wordt opgeslagen bij de uitvoeringsplannen van het project/de eenheid (niet in Documenten › Plannen); bij een PDF met meerdere pagina's kies je de pagina bij het plaatsen van de pin (niet bij het uploaden); de plannen achteraan de verslag-PDF zijn nog niet gebouwd.

## Verslag-PDF volgens 35k/35l/35m + afronden keurt goed — 14/10/2026
- **Afronden** keurt de nieuwe punten (Concept / Ter goedkeuring) weer goed → Goedgekeurd (de eerdere waarschuwing/`forceer`-flow van 10/10 is vervangen, op vraag van Niels). Opgelost ter plaatse → Gemeld uitgevoerd, Afgesloten → Afgesloten (zoals voorheen).
- **Migratie `_migrations/098_WerfverslagPuntStatusBijStart.sql`** (vóór gebruik uitvoeren — `WerfverslagPunt` leest die kolom nu): status bij start van het verslag, voor "vorig verslag" vs "nu".
- **PDF** (`Documents/GlV2/WerfverslagDocumentV2.cs`, basis = `GlV2PdfDocumentBase` zoals de wijzigingsopdracht): pagina 1 (35k) kerngegevens (datum, project + adres, vorig bezoek, aanwezig, weer, volgend bezoek), samenvatting (nieuw / open / afgesloten sinds vorig / kritiek), nieuwe punten met genummerde bol, beschrijving, deadline, plan en tot 3 foto's; pagina 2 (35l) openstaande punten (vorig verslag → nu, met notitie ter plaatse), afgesloten sinds vorig verslag, algemene opmerkingen, verdeling; bijlage (35m, `WerfverslagBijlageDocumentV2`, A4 liggend) = één pagina per plan met punten en de bolletjes met dezelfde nummers (rood = kritiek, "O" = open uit vorig verslag). De bijlage wordt achter het verslag gemerged (PdfSharpCore).
- **Plannen in de bijlage**: de server kan geen PDF-plan renderen, dus de bewerkpagina rastert elk plan met pins in de browser (pdf.js, `gl-v2-projecten-verslag-edit.js`) en post de afbeeldingen mee (`planimg:{planId}:{pagina}`) bij **Voorbeeld PDF** (POST `PdfPreview`, nieuw venster) en **Afronden**. Het voorbeeld-linkje op de verslagenlijst (GET `Pdf`) toont het verslag zonder plannen-bijlage.
- Niet in het ontwerp overgenomen: "Verontschuldigd" (niet bijgehouden), eind-uur van het bezoek, algemene opmerkingen per rubriek, "aannemers krijgen enkel hun eigen punten" (alle aanwezigen krijgen het volledige verslag).

## 14/10 — verslag-PDF herbouwd naar 35k/35l/35m + voorbereid-animatie
- `WerfverslagDocumentV2` herschreven: kerngegevens-raster, samenvatting, kaarten per nieuw punt (ShowEntire), groene tabelkop + statuspillen, opmerkingen/verdeling als samenhoudend blok.
- Bijlage: één document per plan (A4 liggend, logo-kop op elke pagina, geen losse voetnootpagina meer); paginanummers lopen door (`PageNumberOffset`/`TotalPagesOverride` in `GlV2PdfDocumentBase`, additief).
- Pins: kritiek #8B2A2A, "O" goud #8A6A32.
- Overlay "Verslag voorbereiden…" (bouwplan tekent zichzelf, pins vallen, wisselende werf-teksten; reduced-motion ondersteund) in `gl-v2-projecten-verslag-edit.js` + `gl-v2-projecten-verslagen.css`. Niet browser-getest.
- 14/10 (later): voorbereid-animatie omgebouwd naar design punt 44 (volle pagina `#vs-prep`, geen overlay, voorbeeld via fetch + knop "Voorbeeld openen"); pillen-hoogte en lege kaartbody in de PDF gefixt; DESIGN.md-sectie "Update 14/10/2026" en helpartikels `werfverslag-opmaken` / `werfverslag-afronden-en-versturen` toegevoegd. Open: 44b (verzenden per aannemer), automatisch doorgaan naar het verslag, alles browser-testen.
- 14/10 (avond): Niels meldt dat de migraties 079/081/082/097/098 zijn uitgevoerd en dat de Punten/Verslagen in de browser zijn getest. Verlaat-bewaking op het verslag werkt. Help toegevoegd: `punt-toevoegen-en-op-plan-aanduiden`, `punten-goedkeuren-en-statusflow`, `avondmelding-aan-aannemers` (met [VRAAG]-regels voor o.a. hoe de aannemer "gemeld uitgevoerd" meldt en wanneer de avondmelding gaat). Volgende: fase 5 (gsm), fase 6 (spraak/rondgang, API-sleutels nodig), help-vragen beantwoorden, 44b.
- 14/10 (later): Fase 5 (gsm) gebouwd: snelactiesbar (Punt/Foto/Doorsturen/Afdrukken), kaartlijst, camera + markeren (41b), eenheid-chips en laatst-gebruikt in het nieuw-puntformulier, bevestiging na bewaren. Bestanden: `gl-v2-punt-mobile.js`, `gl-v2-projecten-punten-mobile.css`, haken in `gl-v2-punt-panel.js`/`IndexV2`/`_PuntPanelV2`. Niet getest op een echt toestel. Help: `punt-op-de-werf-met-foto`. Open: ongedaan maken na bewaren, aannemervoorstel op basis van kamer. Volgende: fase 6 (spraak/rondgang; OpenAI + Anthropic sleutels nodig).
- 14/10 (later): punt-detail kreeg Verwijderen (met bevestiging) + Status in de actiebalk, ook als tegels op gsm; 'ongedaan maken' vervalt. KPI's verborgen op gsm. Open: eigen plankiezer voor gsm (design van Niels).
- 14/10 (later): 40s/40t gebouwd: Plan kiezen en Nieuw plan als bottom sheets op gsm (gl-v2-punt-plansheets-m.css/.js). Niet getest op toestel. Volgende: fase 6.
- 14/10 (later): Foto van plan heeft nu perspectiefcorrectie (Plan rechttrekken, vier hoeken, homografie in de browser). Niet op toestel getest.
- 14/10 (later): Fase 6 gebouwd: Inspreken (41c/40f), titel inspreken, Rondgang (41d/40g/40h) met achtergrondverwerking op de server (in het geheugen, 24 u). Backend `Services/PuntenSpraak/*` + `ProjectIssuesController.Spraak.cs`, front `gl-v2-punt-spraak-m.js/.css`. Help: `punt-met-spraakbericht` herschreven, `rondgang-opnemen` nieuw. **Nog te doen door Niels**: `PuntenSpraak:OpenAiApiKey` instellen (user-secrets lokaal, `PuntenSpraak__OpenAiApiKey` op de server); Anthropic valt terug op de bestaande offerte-sleutel; testen op een echt toestel over https. Bewuste afwijkingen: geen video, markeringen na verwerking, jobs niet persistent.
- 14/10 (later): Rondgangen staan nu in de database + bestandsopslag (**migratie 100_PuntRondgang.sql, door Niels te draaien vóór gebruik van Rondgang**), verwerkt door `RondgangHostedService`, 14 dagen bewaard, overleeft herstart. Vervangt de in-memory `RondgangJobStore`.
- 16/10: Inspreken/Rondgang visueel naar 41c/41d herbouwd; per voorstel Foto (camera of galerij) en Op plan (plannen van de eenheid + punt aanduiden); rondgang-foto's: cameraknop + pointerup + servermatching op tijdstip. Niet op toestel getest.
- 17/10: eerste toestel-testfeedback. Twee meldingen:
  1. **Rondgang: enkel de eerste foto komt door, de rest niet** (de flits op het scherm verschijnt wel telkens, dus `snap()` vangt elke foto client-side correct op). Niels bevestigd: de ontbrekende foto's staan nergens, ook niet per ongeluk bij het eerste voorstel geplakt — dus geen "verkeerd gekoppeld", echt "onvindbaar". Twee gerichte fixes in `RondgangService.cs` (nog **niet opnieuw getest**):
     - `KoppelFotos` kon een foto stilzwijgend laten vallen wanneer GEEN ENKEL voorstel een `atSeconds` had (de AI zet dat niet altijd consequent op elk voorstel, een gekend risico — zie "Kwaliteit van de AI-voorstellen… bijstellen" hierboven) én er meer dan één voorstel was: `best` bleef dan `null`. De foto stond op dat moment wél al in de bestandsopslag/database (`CreateAsync` bewaart sowieso elke upload die lukt), maar hing aan geen enkel voorstel en was dus in de review-stap onvindbaar — exact het gemelde gedrag.
       Eerste poging (altijd terugvallen op het eerste voorstel) was fout, terecht aangekaart door Niels: een foto die pas tijdens/na een LÁTER punt genomen is hoort daar niet bij punt 1 te belanden. Herzien naar: als geen enkel voorstel een echt `atSeconds` heeft, krijgen de voorstellen een VERONDERSTELD tijdstip door ze gelijk te verdelen over de tijdspanne van de genomen foto's (voorstel 1 = eerste fototijdstip, laatste voorstel = laatste fototijdstip, ertussenin gelijk verdeeld) — uitgaand van dezelfde aanname als de AI-prompt zelf (voorstellen komen terug in de volgorde waarin ze ingesproken zijn). Een foto gaat dan naar het voorstel waarvan dat veronderstelde tijdstip er net vóór ligt, zelfde principe als de normale (wel-atSeconds) tak.
     - `CreateAsync`'s upload-lus loggde een mislukte foto-upload (bv. bestandsopslag-API tijdelijk onbereikbaar) voorheen helemaal niet — enkel "overgeslagen, de rest niet laten vallen". Toegevoegd: een `LogWarning` per mislukte foto, zodat als dit toch (ook) het probleem is, het serverlog dat nu toont i.p.v. dat er geen enkel spoor is.
     - Build is clean (`dotnet build`, geen `error CS`). **Volgende stap: opnieuw een rondgang met meerdere foto's proberen en controleren of alle foto's nu ergens in de review verschijnen** (al dan niet bij het juiste voorstel) — als er nog steeds foto's volledig ontbreken, dan ligt het toch in de upload/opslag zelf en toont het serverlog nu welke foto en waarom.
  3. **Waarom geeft de AI geen atSeconds per punt, terwijl dat net het idee was?** Nagekeken: de plumbing zit er wel degelijk — Rondgang gebruikt bewust `whisper-1` (niet `gpt-4o-mini-transcribe`, dat geeft geen tijdstempels terug) met `verbose_json`/segment-tijdstempels, en die `[Ns]`-tijdmerken komen effectief in het transcript terecht dat naar Claude gaat (`PuntenSpraakService.ExtractAsync`), samen met de fototijdstippen. De AI heeft dus wel degelijk de ruwe data. Het probleem: `atSeconds` stond in de systeemprompt als iets beschrijvends/optioneels ("enkel als tijdstippen gegeven zijn") en is een `nullable` schemaveld — een LLM laat zo'n "bijkomstig" veld in een lijst met meerdere punten in de praktijk makkelijk vallen voor latere/moeilijkere items, zonder dat dit een schendig van de schema is. Prompt aangescherpt (`PuntenSpraakService.SystemPrompt`): atSeconds is nu expliciet VERPLICHT voor elk punt zodra het transcript tijdmerken bevat (met uitleg waarom: anders kunnen foto's niet gekoppeld worden), met een duidelijke terugvalregel (tijdmerk van de dichtstbijzijnde voorgaande zin). Build clean. **Nog niet getest** — dit is een prompt-wijziging, dus de verbetering is pas te zien na een nieuwe rondgang-opname; als de AI het nog steeds laat liggen, is een volgende stap mogelijk het schema zelf aanscherpen of expliciet per punt vragen "welke zin (met tijdmerk) hoort hierbij".
  2. **Inspreken: "Op plan" doet niets na het verwerken, "Wijzigen" werkt wel.** Root cause gevonden en **gefixt**: `window.PuntPanel.pickPlan()` (aangeroepen vanuit de review-kaart) toont bij geen beschikbaar plan voor de eenheid een melding via `toast()` (`.gl-v2-pt-toast`, z-index 2500) — maar dat ligt ACHTER het volledig-scherm Inspreken/Rondgang-scherm (`.gl-v2-ptv`, z-index 3000) en de rondgang-opname/camera-overlays (3100/3200). De melding verscheen dus wel, maar onzichtbaar: leek alsof de knop niets deed. `.gl-v2-pt-toast` kreeg z-index 3300 (`gl-v2-projecten-punten-mobile.css`). Als het project/de eenheid intussen wél een plan heeft, zou de knop daarvoor al gewerkt moeten hebben (de Bootstrap-modal zelf krijgt via `body.pt-over` al terecht z-index 100010); dus eerst opnieuw testen met deze fix vóór verder te zoeken.
- 21/10: kleine punten afgewerkt: einde + verontschuldigd (**migratie 104_WerfverslagEindUur.sql, nog te draaien**), Voorbeeld PDF vanuit de lijst met plannen, afronden in stappen met echte voortgang + klaar-scherm (44b/44c). Rondgang-transcriptie met twee modellen is bewust niet gedaan (werkt).
