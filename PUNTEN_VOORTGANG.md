# Punten (design-handoff 40/41) — voortgang en overdracht

Laatst bijgewerkt: 08/10/2026. Bron van het ontwerp: `design-handoff/CRM Punten.dc.html` (punt 40a–40o, 41a–d). Uitleg per scherm staat ook in `DESIGN.md` (sectie "Update 08/10/2026 — Punten").

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
| 3 | Op plan (40b) + tablet plaatsen (40k), bestaande plannen hergebruiken | Open |
| 4 | Verslagen (40i, 40l): nieuwe tabellen + migratie + menu-item "Verslagen" | Open |
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

## Andere open punten (buiten Punten)
- Migraties die Niels nog moet uitvoeren: 074 (mijlpaal relatieve datum), 079 (punten); 075–078 en 080 zijn budget-gerelateerd en komen van een andere werkstroom.
- Niets van deze sessie is in een browser getest (Traject/Dossiers, bedrijf-bewerken-tabs, Punten): eerst door-klikken.
- Zimmo geeft 403 vanaf Hetzner; Exchange-toegangsbeleid voor Graph Mail.Send nog niet ingesteld (zie memory-notities op de andere pc, niet in de repo).

## Werkafspraken die ik tijdens deze sessie volgde
- Dutch in alle UI-teksten; gl-v2 klassenprefix per pagina (`gl-v2-pt-*`, `gl-v2-pd-*`); page-CSS laadt vóór `gl-v2-shell.css` (extra specificiteit nodig); `[hidden]` expliciet guarden.
- Hand-gedraaide idempotente SQL in `_migrations/NNN_*.sql`, nooit EF-migraties.
- Build: `dotnet build CPMCore/CPMCore.csproj`; MSB3021/3027-fouten zijn bestandsvergrendeling van Visual Studio en mogen genegeerd worden, enkel `error CS|RZ` tellen. JS: `node --check`.
- Bash-heredocs met apostrofs kunnen falen in deze omgeving: schrijf een script met de Write-tool en voer het uit.
