# Elektronisch ondertekenen — voortgangsstatus

Doorlopend statusdocument voor de signingmodule (SES voor wijzigingsopdrachten, generiek uitbreidbaar).
Het ontwerp staat in `ONDERTEKENEN_VOORSTEL.md`; dit bestand is de werkende samenvatting om op een
andere machine of in een volgende sessie meteen verder te kunnen. **Bijwerken na elke stap.**
Reist mee via git: commit + push vóór je van machine wisselt.

**Laatste update:** 2026-09-28 — samenvoegplan uitgewerkt en per onderdeel door Niels bevestigd (A wordt
overal het vertrekpunt), fase 2 gebouwd en na feedback bijgeschaafd (lettertype-bug, akkoord/code-
volgorde, geen audit-download voor de klant, verplicht handtekeningvak — zie Fase 2 hieronder), en
**fase 3 gebouwd**: `SigningHostedService` (achtergrondjob + `/api/trigger/signing`), de publieke
verificatiepagina `/verifieer/{id}` + QR terug op het ondertekende document, `ForwardedHeaders`
(rate limiter/audit-IP's zien nu het echte client-IP, niet het IIS-adres), en `ClientContactChangeLog`
die nu gevuld wordt vanuit de klantformulieren. Build groen, tests ongewijzigd (85/87, 2 bestaande
Invoicing-fouten). **Nog niets van fase 2/3 samen is opnieuw in de browser getest** na al deze
wijzigingen — dat is het eerste wat moet gebeuren. Side B (`/ondertekenen`) is niet aangeraakt en
blijft de live flow tot de cutover. Open architectuurvraag van fase 2 staat nog open: moet de
handtekening letterlijk in het blanco vak van de originele PDF, of volstaat het aparte blad erna?
Nieuw open punt: `TriggerKeys:Signing` moet nog handmatig gezet worden (user-secrets lokaal, config
op test/live) vóór `/api/trigger/signing` bruikbaar is.

## ⚠️ Samenloop: twee ondertekenimplementaties na de merge van 28/09/2026
Bij het mergen van `origin/layout-experiment` (commits 8b973887 + 5ce94e6a van de andere pc) bleek
daar **een tweede, onafhankelijke ondertekenflow** gebouwd te zijn. Beide staan nu naast elkaar in de
code en compileren samen; er is **geen beslissing genomen** welke blijft. Dat is de eerste keuze voor
de volgende sessie.

| | Signingmodule (deze voortgang) | Link-per-e-mail-flow (andere pc) |
|---|---|---|
| Code | `FacadeCore.Signing.*`, `ServiceCore.Signing.*`, `CPMCore/Services/Signing`, `SigningAdminController` | `FacadeCore.ISigningService`, `ServiceCore.Documents.SigningService`, `SigningController` (`/ondertekenen/{token}`), `ProjectenController.ChangeOrderSign.cs` (`ChangeOrderSignV2`), `ChangeOrderPdfService` |
| Data | 8 eigen tabellen (`SigningCase`… + append-only `SigningEvent`), migraties **055** + **056** (hernummerd bij de merge, waren 047/048) | `ProjectDocs.ChangeOrderId` + `DocumentSignatures` op het Documenten-model, migratie 049 + **054** |
| PDF | QuestPDF `ChangeOrderDocument` (huisstijl), ondertekeningsblad in fase 2 | Rotativa `ChangeOrderPDF.cshtml` mét handtekeningblok (Signatures/DigitalSigningPending) |
| Bewijs | hash-ketting, OTP-HMAC, sessiecookie, bewaarregel, testmodus, feature-vlag, rate limiter | SHA-256 van token/code, 10 min/5 pogingen/5 codes per uur, IP + X-Forwarded-For, evidence per handtekening |
| Status | interne schermen klaar (fase 1), publieke pagina nog niet (fase 2) | publieke pagina + intern startscherm bestaan; zie hun DESIGN.md-sectie "Projecten/ChangeOrderSignV2" en `JURIDISCH_ELEKTRONISCH_ONDERTEKENEN.md` |

Wat bij de merge gedaan is om beide te laten samenleven (geen inhoudelijke keuze):
- `Klanten/Partials/ChangeOrders.cshtml`: beide actie-iconen staan er (hun `fa-signature` altijd, mijn
  `fa-file-signature` enkel met `Features:EnableSigning`); mijn slotje/vergrendeling blijft.
- `SigningSecurityHeadersMiddleware` beperkt tot `/verifieer`: `/ondertekenen` is nu hun route
  (Layout = null + inline script; mijn CSP zou die pagina breken). Fase 2 kiest een eigen prefix.
- `SigningAdminController` gebruikt `FacadeCore.Signing.ISigningService` volledig gekwalificeerd
  (naamsconflict met hun `FacadeCore.ISigningService`).
- Migraties 047/048 van de signingmodule → 055/056 (hun 047–054 kwamen eerst). Op testdb zijn ze al
  uitgevoerd onder de oude naam; de scripts zijn idempotent, inhoud ongewijzigd.
- `Projecten/ChangeOrderPDF` (afdrukken) rendert via QuestPDF; hun `ChangeOrderPdfService` rendert
  de Rotativa-view zelf en gebruikt die actie niet. Twee renderers voor hetzelfde document totdat beslist is.

**Richting (Niels, 28/09/2026): samenvoegen tot één geheel, niet kiezen-en-weggooien.** De
link-per-e-mail-flow van de andere pc is functioneel wat er nodig is (dat is de werkende, meest recente
flow); de signingmodule levert de structuur — datamodel, bewijs (hash-ketting, OTP-HMAC, append-only
events), abstracties (`ISigningDocumentSource`/provider/verificatie/kanaal), gl-v2-schermen. Het
samenvoegen zelf gebeurt in een volgende sessie. Voorbereidend denkwerk voor dan:
- Publieke pagina: hun `SigningController` (werkt, route `/ondertekenen/{token}`) als vertrekpunt voor
  fase 2, maar tegen de signingmodule (`RedeemTokenAsync` → sessie → `RequestVerificationAsync`/
  `VerifyCodeAsync`/`SignAsync`) i.p.v. `ServiceCore.Documents.SigningService`.
- Intern: hun `ChangeOrderSignV2` en mijn `SigningAdmin/Start` doen hetzelfde; één ervan blijft (Start
  is generiek per documenttype, ChangeOrderSignV2 is wijzigingsopdracht-specifiek).
- Documenten: hun koppeling `ProjectDocs.ChangeOrderId` + het ondertekende PDF als documentrevisie is
  waardevol — de signingmodule zou bij voltooiing het definitieve PDF óók als `ProjectDocs`-revisie
  moeten wegschrijven (`OnCaseCompletedAsync` in `ChangeOrderSigningSource`), zodat het in Documenten
  verschijnt. `DocumentSignatures` (054) wordt dan overbodig.
- PDF: één renderer. Hun handtekeningblok (naam, tijdstip, methode, ref, SHA-256) is inhoudelijk het
  model voor het QuestPDF-ondertekeningsblad van fase 2.
- Juridische tekst: `JURIDISCH_ELEKTRONISCH_ONDERTEKENEN.md` (andere pc) en `SigningPolicy.ConsentText`
  (055-seed) op elkaar afstemmen.

**Werkregel bij het samenvoegen (Niels, 28/09/2026): niets zelf beslissen — bij elk stuk dat uit
één van beide kanten komt, eerst aan Niels vragen wat hij wil behouden en van welke commit.** Concreet:
per onderdeel (publieke pagina, intern startscherm, datamodel/bewijs, PDF + handtekeningblok,
documentenkoppeling, e-mails, juridische tekst, migraties) de twee varianten kort naast elkaar zetten
met verwijzing naar de commit/bestanden (signingmodule: lokale commit 42ce3b26 "10.7" + merge cbd50c6e;
e-mailflow: 8b973887 + 5ce94e6a "10.7" van de andere pc) en pas na zijn keuze code aanpassen of
verwijderen. Geldt ook voor kleine dingen (teksten, routes, iconen): niet stilzwijgend één kant kiezen.

### Keuzes vastgelegd (Niels, 28/09/2026)
Ter controle: `8b973887`/`5ce94e6a` zijn van 2026-09-25, `42ce3b26` van 2026-09-28 — B is dus
bevestigd de oudere versie. Vertrekpunt wordt overal A (signingmodule), niet B.
1. **Publieke pagina:** vertrekken van A, **niet** van B's `SigningController` (wijkt af van de
   eerder genoteerde "Richting" hierboven, die B als vertrekpunt nam — dat is hiermee overruled).
2. **Intern startscherm:** `SigningAdmin/Start` (A) blijft; `ChangeOrderSignV2`
   (`ProjectenController.ChangeOrderSign.cs`, B) verdwijnt.
3. **Datamodel/bewijs:** A's 8 tabellen (055/056) worden de structuur; `DocumentSignatures` (B,
   049 §5 + 054) wordt uitgefaseerd.
4. **PDF + handtekeningblok:** QuestPDF (`ChangeOrderDocument.cs`, A) blijft de enige renderer;
   B's handtekeningblok (`Views/Projecten/ChangeOrders/ChangeOrderPDF.cshtml`) is het inhoudelijke model voor
   het QuestPDF-ondertekeningsblad. Rotativa-view + `ChangeOrderPdfService` (B) verdwijnen.
5. **Documentenkoppeling:** `OnCaseCompletedAsync` in `ChangeOrderSigningSource.cs` (A) schrijft
   het definitieve PDF als `ProjectDocs`-revisie (zoals B deed via `AttachSignedRevision`).
6. **E-mails:** `SigningNotifier` (A) wordt gebruikt; B's inline mails in
   `ProjectenController.ChangeOrderSign.cs` / `SigningController.cs` vervallen.
7. **Juridische tekst:** `SigningPolicy.ConsentText` (A, DB-editable, seed in 055) blijft het
   mechanisme; de tekst zelf wordt bijgewerkt op basis van `JURIDISCH_ELEKTRONISCH_ONDERTEKENEN.md`
   en de hardcoded `ConsentText` in `ServiceCore/Documents/SigningService.cs` (B).
8. **Migraties:** nieuwe migratie om `DocumentSignatures` (+ evt. `ProjectDocs.ChangeOrderId`) echt
   te verwijderen, ná controle dat er geen data/afhankelijkheden meer op staan.

**Gevolg:** `ProjectenController.ChangeOrderSign.cs`, `Views/Projecten/ChangeOrders/ChangeOrderSignV2.cshtml`,
`ChangeOrderPdfService.cs`, `Views/Projecten/ChangeOrders/ChangeOrderPDF.cshtml` (Rotativa), `FacadeCore/ISigningService.cs`
(root) en `ServiceCore/Documents/SigningService.cs` zijn kandidaat voor verwijdering zodra hun
functionaliteit in A herbouwd is. Niet vooraf verwijderen — pas nadat de vervangende functionaliteit
in A werkt (anders staat de app zonder werkende signing-flow).

## Eerst te doen (volgende sessie) — hoe en waar je dit ziet en test
0. ~~Samenvoegplan uitwerken~~ — gedaan. Fase 2 én fase 3 zijn gebouwd; wat nu volgt is de eerste
   **browsertest van fase 1 + 2 + 3 samen** (nog niets ervan is in de browser bevestigd).
1. **Commit + push** van alles wat nu uncommitted staat (`git status`).
2. Module aanzetten — **lokaal/testomgeving, niet in git**: `Features:EnableSigning=true` en
   `Signing:PublicBaseUrl=https://localhost:<jouw-poort>` (of het testadres) in
   `appsettings.json`/`appsettings.Development.json` (git-genegeerd); in user-secrets van `CPMCore`
   (`dotnet user-secrets set "<key>" "<waarde>"` vanuit `CPMCore/`, of rechtsklik project → Manage User
   Secrets): `Signing:OtpHmacKey` (≥ 32 bytes base64), `Signing:TestRecipientOverride=<eigen adres>`
   (zonder dit gaan mails naar de echte klant), en — **nieuw voor fase 3** —
   `TriggerKeys:Signing=<zelf een waarde kiezen>` (anders geeft `/api/trigger/signing` altijd 401).
3. Permissie `Signing` toekennen aan je eigen rol (Instellingen › Rollen, lezen + schrijven) — anders
   zie je het menu-item en de knoppen niet.
4. `dotnet build CPMCore/CPMCore.csproj`, dan de app starten (F5 of `dotnet run --project CPMCore`) en
   in de browser:
   - **Intern (fase 1):** Projecten › een project met een wijzigingsopdracht › Wijzigingsopdrachten ›
     icoon "Elektronisch laten ondertekenen" (enkel zichtbaar met de permissie + flag) → **Start**
     ("Bekijk de pdf" toont de QuestPDF-versie — **nog niet visueel nagekeken**) → **Aanbieden** →
     **Dossier** (partijen, audit trail, herinnering, nieuwe link, annuleren) → menu-item
     "Ondertekeningen" (Index) voor het overzicht. De uitnodigingsmail komt op je testadres.
   - **Publiek (fase 2, bijgeschaafd):** open de link uit die testmail —
     `<PublicBaseUrl>/tekenen/<token>` (niet `/ondertekenen`, dat blijft Side B). Op
     `/tekenen/document`: eerst enkel **Akkoord**/**Weigeren** te zien (geen code, geen
     handtekeningvak). Na **Akkoord**: melding "code verstuurd" + code-veld verschijnt (code komt op
     het testadres) én het handtekeningvak — **Ondertekenen** blijft uit tot er getekend is. Met één
     ondertekenaar is het dossier meteen **Voltooid**: enkel het ondertekende PDF is downloadbaar op de
     pagina (het auditrapport niet meer — dat is intern). Open dat PDF en controleer: geen rare tekens
     meer (lettertype-fix), en onderaan een stempel per ondertekenaar met de getekende handtekening +
     een QR-code (**nieuw, fase 3**) — scan die of open de link eronder (die staat nog wel op het
     interne auditrapport) en controleer dat `/verifieer/{id}` het dossier toont. Controleer ook
     **Dossier** (intern): audit trail toont de nieuwe events, en de wijzigingsopdracht kreeg een
     nieuwe revisie onder **Projecten › Documenten** (map "Contracten").
   - Meerdere ondertekenaars: na de eerste handtekening blijft de status "Wacht op andere
     ondertekenaar(s)" tot iedereen tekende.
5. **Fase 3, achtergrondjob:** `https://localhost:<poort>/api/trigger/signing?key=<TriggerKeys:Signing>`
   openen → moet `202 Accepted` geven; in de consolelogs (of Output-venster in VS) zoeken naar
   "Signing ExpireOverdueCasesAsync"/"SendDueRemindersAsync"/"ApplyRetentionScrubAsync"/
   "RetryStuckFinalizationsAsync" — geen exceptions. Met een verkeerde/lege `key`: `401`.
6. **Fase 3, klantformulier:** een klant of een van zijn contacten bewerken (`Klanten/Edit` of via de
   project-clienttab `Klanten/EditProject`) en het e-mailadres of gsm-nummer wijzigen → controleer in
   de databank dat er een rij in `ClientContactChangeLog` verscheen met gemaskeerde oude/nieuwe waarde.
7. Controleer ook dat **Side B blijft werken** en niet per ongeluk raakte: legacy lijst, status
   "Ter ondertekening (elektronisch)", `/ondertekenen/{token}` (hun eigen testflow) en
   `Projecten/ChangeOrderPDF` (afdrukken) geven nog steeds hetzelfde als voor deze sessie.
8. Werkt alles: dan is de volgende stap de **cutover** — beslissen of `/tekenen` de definitieve route
   wordt of dat A `/ondertekenen` overneemt, en pas dan Side B verwijderen (zie "Gevolg" hierboven).
   Niet doen zolang fase 2 niet getest is.

## Hoe je verder werkt op een andere machine
1. `git pull`. Build: `dotnet build CPMCore/CPMCore.csproj`. Tests: `dotnet test ServiceCore.Tests`
   (30 slagen; de 2 falende `StructuredReferenceServiceTests` zijn een bestaand probleem in
   `ServiceCore.Invoicing`, los van signing).
2. User-secrets op die machine (zelfde `UserSecretsId`, staan NIET in git): `CPMRUNNING:*` (bestaand)
   én, zodra je de module aanzet, `Signing:OtpHmacKey` (≥ 32 bytes base64). `Signing:PublicBaseUrl` en
   `Features:EnableSigning` staan in `appsettings.json` (dat bestand is git-genegeerd → ook daar
   overnemen: secties `Features` en `Signing`, zie DEPLOY.md "Elektronisch ondertekenen").
3. Testdb (`db_ab5fbb_testdb`) heeft migraties 055 en 056 al. Nieuwe migraties: `_migrations/NNN_*.sql`,
   handmatig uitvoeren (SSMS), zoals altijd. Live heeft nog géén van beide.
4. Lees §"Waar we staan" hieronder en ga verder bij het eerste onafgevinkte punt.

## Beslissingen (vastgelegd)
- §9.1 opslag: PDF-bytes in SQL (`SigningDocument.Content`, bewijsbron) + spiegel in Storage API.
- **Bewaarregel:** enkel ondertekende dossiers houden bytes; verlopen/geweigerd/geannuleerd → bytes en
  spiegel meteen weg, metadata + SHA-256 blijven (`PurgeUnsignedDocumentsAsync`).
- §9.2 bron vergrendelen zolang een dossier loopt (afgeleid: `GetActiveCaseForSourceAsync`, geen kolom
  op `ChangeOrder`).
- §9.6 token → sessiecookie-redirect.
- Testmodus: `Signing:TestRecipientOverride` → alle signing-mails naar één adres.
- **PDF-engine: QuestPDF** voor de wijzigingsopdracht (beslist 2026-09-27, zie hieronder) — niet Rotativa.
- Overige §9-punten: aanbeveling gevolgd (interne tegenondertekening in model, verificatiepagina + QR,
  bijlage + downloadlink, permissiecode `Signing`, standaardondertekenaars = klantaccount + mede-eigenaars
  met regel ALL, annuleren = schrijfrecht op de bron).

## Waar we staan

### Fase 0 — fundament ✅ (2026-09-27)
- [x] Enums `BOCore/Enum/Signing/*.vb`; permissiecode `Signing` (+ catalogus, resolver voor `SigningAdmin`)
- [x] Entiteiten `DALCore/Models/Signing*.cs`, `ClientContactChangeLog.cs`, `cpmRunningContext.Signing.cs`
- [x] Migratie `055_Signing.sql` — toegepast op testdb; append-only trigger getest (UPDATE/DELETE geweigerd, scrub toegelaten)
- [x] Contracten `FacadeCore/Signing/` (ISigningService, ISigningDocumentSource, ISignatureMethodProvider,
      IVerificationMethod, IMessageChannel/ISmsProvider, ISigningEvidenceStore, IAssetStorageClient,
      ISigningNotifier, ISigningDocumentRenderer + DTO's)
- [x] Services `ServiceCore/Signing/` (SigningService, SigningEvidenceStore, SigningCrypto, SigningRuleEvaluator,
      InternalSesProvider, EmailOtpMethod/SmsOtpMethod, EmailChannel/SmsChannel, SigningNotifier,
      AssetStorageClient, SigningRegistry, SigningOptions, NotAvailableSigningDocumentRenderer)
- [x] CPMCore: DI + `Features:EnableSigning` (fail closed), rate limiter, `SigningSecurityHeadersMiddleware`,
      `_LayoutPublic.cshtml` + `gl-v2-public.css`, storage-helpers in beide controllers delegeren
- [x] Tests `ServiceCore.Tests/Signing/` (crypto, regels, hash-ketting)
- [x] Docs: DESIGN.md-sectie, DEPLOY.md-sectie, voorstel bijgewerkt

### Fase 1 — wijzigingsopdracht + interne schermen ✅ gebouwd (2026-09-27), ⚠️ nog niet in de browser getest
- [x] `SigningCase.SourceFingerprint` (migratie 056, op testdb) + `ISigningDocumentSource.ComputeFingerprintAsync`
      i.p.v. `HasChangedSinceAsync`; `SigningCrypto.ComputeFingerprint` (lengteprefix per onderdeel) + test
- [x] `CPMCore/Documents/ChangeOrderDocument.cs` — QuestPDF, staand A4 op `GroupLnPdfDocument` (basis kreeg
      `PageSize` en `FooterNote` als virtual). Rooktest rendert 111 KB / 18 regels zonder lay-outfout.
- [x] `CPMCore/Services/Signing/ChangeOrderPdfBuilder.cs` (EF-query + render, Avenir één keer registreren)
- [x] `CPMCore/Services/Signing/ChangeOrderSigningSource.cs` (pakket, voorgestelde partijen = klantaccount +
      mede-eigenaars, fingerprint, `DateSendToClient` bij aanbieden, `DateAgreement` bij voltooiing,
      interne ontvangers = verkoopverantwoordelijke → projectverantwoordelijke)
- [x] Vergrendeling: `EditChangeOrder` GET/POST en `DeleteChangeOrder` weigeren bij dossier Draft/Open
      (`ActiveSigningCaseAsync`, enkel als de module aanstaat). `DuplicateChangeOrder` blijft toegelaten:
      dupliceren wijzigt de bron niet.
- [x] `SigningAdminController` + `Views/Ondertekenen/SigningAdmin/{Start,Dossier,Index}.cshtml`, `gl-v2-signing.css/.js`,
      `Models/Signing/SigningAdminVms.cs` (incl. `SigningLabels`), kruimelpaden
- [x] Ingang op de legacy lijst (`Klanten/Partials/ChangeOrders.cshtml`: status + icoon + slotje) en
      "Ondertekeningen" in `GlV2/_ProjectInnerMenuV2` (feature-vlag + permissie `Signing`)
- [x] `Projecten/ChangeOrderPDF` rendert via `ChangeOrderPdfBuilder` (Rotativa-view blijft ongebruikt staan)
- [x] `_LayoutV2`: `ViewData["GlV2NoProjectMenu"]` → `IgnoreSection("ProjectMenu")` (Razor kan geen `@section` in `@if`)
- [x] DESIGN.md, DEPLOY.md, ONDERTEKENEN_VOORSTEL.md bijgewerkt; build groen; tests 31/33 (2 bestaande fouten)
- [ ] **Browsertest** van alles hierboven (zie "Eerst te doen"); visuele controle van de PDF
- [ ] Beslissen: `Views/Projecten/ChangeOrders/ChangeOrderPDF.cshtml` (+ `ChangeOrderFooter`-actie) verwijderen zodra de
      QuestPDF-versie goedgekeurd is

### Fase 2 — ondertekenpagina ✅ gebouwd + eerste browsertest gedaan (2026-09-28)
- [x] Route is **`/tekenen`**, niet `/ondertekenen` zoals hier eerder stond — dat blijft de route van
      `SigningController` (Side B) tot de cutover. `SigningService.SignUrl` en
      `SigningSecurityHeadersMiddleware.Prefixes` zijn aangepast. Side B is niet aangeraakt.
- [x] `OndertekenenController` ([AllowAnonymous], `/tekenen/{token}` → sessiecookie `gl_tekenen_sid`
      → `/tekenen/document`); fail-closed op `Features:EnableSigning` zoals `SigningAdminController`.
- [x] `CPMCore/Documents/SigningEvidenceDocument.cs` + `SigningAuditReportDocument.cs` en
      `CPMCore/Services/Signing/SignedDocumentComposer.cs` (`ISigningDocumentRenderer`, PdfSharpCore-merge
      zoals `InvoicesController.MergePdfDocuments`) → vervangt de stub in `Program.cs`.
- [x] `ChangeOrderSigningSource.OnCaseCompletedAsync` schrijft het definitieve PDF nu ook als
      `ProjectDocs`-revisie (map "contracten", koppeling via `ChangeOrderId`) — best effort, blokkeert de
      akkoorddatum niet. Geen `ISigningService`-injectie in de bron (kringafhankelijkheid via
      `SigningRegistry`); het definitieve document wordt rechtstreeks via `_db` gelezen.
- [x] **Eerste browsertest (Niels, 28/09/2026) — volledige keten werkte** (dossier → mail → `/tekenen` →
      OTP → tekenen → voltooid → ProjectDocs-revisie → audit trail), maar met 4 gemelde problemen,
      allemaal opgelost in dezelfde sessie:
  - **Lettertype-bug**: `SigningEvidenceDocument`/`SigningAuditReportDocument` gebruikten het
    niet-geregistreerde "Lato" i.p.v. Avenir → QuestPDF's fallback-font liet "ti"/"tt"-ligaturen weg
    ("bevesgt" i.p.v. "bevestigt", "hps://" i.p.v. "https://"). Fix: nieuwe gedeelde
    `CPMCore/Documents/GroupLnFonts.cs` (Avenir één keer per proces registreren, geen dubbele
    registratie meer tussen `ChangeOrderPdfBuilder` en de fase-2-renderer); beide nieuwe documenten
    en `ChangeOrderPdfBuilder` gebruiken 'm nu.
  - **Volgorde akkoord/code onlogisch**: stond eerst code+veld, dan pas akkoord. Nu twee stappen:
    stap 1 = enkel Akkoord/Weigeren (geen code, geen handtekeningvak zichtbaar); pas na "Akkoord"
    verschijnt (indien `OtpRequired`) de melding "code verstuurd" + code-veld, én het handtekeningvak.
  - **Auditrapport was door de klant downloadbaar**: mag niet, enkel intern. `Audit`-actie uit
    `OndertekenenController` verwijderd; enkel `Finaal` (ondertekend document) blijft publiek.
  - **Geen handtekeningvak**: `<canvas>` toegevoegd (`gl-v2-tekenen.js`, pointer-events, geen CDN).
    "Ondertekenen" blijft uitgeschakeld tot er getekend is (en, indien nodig, een code ingevuld) —
    en dat wordt ook **server-side** afgedwongen in `OndertekenenController.Tekenen`
    (`SignatureImagePng` is verplicht, niet enkel de knop uitgeschakeld). De tekening komt terecht in
    `SigningEvidenceDocument.SignatureBlock` — visueel hetzelfde "Voorwaarden / Datum / Handtekening
    voor akkoord"-vak als op het origineel, nu ingevuld, als apart blad ná het origineel (dat blijft
    zelf ongewijzigd — zie de architectuurregel bij Fase 1: geen retroactieve wijziging van wat de
    klant zag/hashte). **Open vraag aan Niels**: volstaat dit, of moet de handtekening letterlijk in
    het blanco vak van de originele wijzigingsopdracht-PDF komen (zou de "origineel verandert nooit"
    -regel doorbreken)?
- [x] **Tweede feedbackronde (Niels, 2026-09-28)**, zelfde sessie: het evidence-blad moest geen
      uitgebreide tabel zijn maar een compacte stempel ("Elektronisch ondertekend via CPM", naam,
      datum/tijd, verificatie-ID, SHA-256 — zie screenshot-voorbeeld) — `SigningEvidenceDocument`
      herschreven (`SignatureStamp` i.p.v. `SignersTable`+`SignatureBlock`); de hash op die stempel is
      bewust die van het **origineel**, niet van het finale PDF (kip-en-ei: een pagina die zelf deel
      uitmaakt van het finale bestand kan zijn eigen hash niet vooraf tonen — die staat wel op het
      interne auditrapport, `Origineel:` + `Ondertekend:` SHA-256 samen). De verificatielink
      (`/verifieer/{id}`, fase 3, **nog niet gebouwd**) is van het klant-blad gehaald — een dode link
      op een klantdocument kan niet — en blijft enkel op het interne auditrapport; komt terug als QR
      zodra `/verifieer` bestaat.
- [x] **Derde feedbackronde (Niels, 2026-09-28)**, ná fase 3: twee problemen met "Weigeren":
  - **`window.prompt()` i.p.v. een eigen modal**: DESIGN.md's modal-taal (TYPE 1 "Bevestiging") leunt
    normaal op Bootstrap/`gl-v2-shell.css`, die `_LayoutPublic` bewust niet laadt (CSP, geen externe
    bronnen). Daarom een eigen, zelfstandige `.gl-v2-public-modal-*`-component toegevoegd aan
    `gl-v2-public.css` (zelfde tokens/radius/schaduw/taal als DESIGN.md, `.is-warning`-accentstreep,
    mobiel een bottom sheet) + vanilla-JS open/sluit/Esc/backdrop-klik in `gl-v2-tekenen.js` — geen
    Bootstrap-afhankelijkheid, past bij de rest van deze pagina.
  - **Na "Weigeren" verscheen "Uw sessie is verlopen"**: `SigningService.DeclineAsync` roept
    `RevokeTokens` aan zodra iemand weigert — de sessietoken is dus meteen ongeldig, waardoor de oude
    `window.location.reload()` na een geslaagde weigering altijd op de neutrale verlopen-pagina
    uitkwam (er is geen geldige sessie meer om opnieuw op te vragen). Fix: geen reload meer — de
    bevestiging ("U hebt geweigerd te ondertekenen") staat al verborgen in `Document.cshtml` en wordt
    door de JS rechtstreeks getoond zodra de server `ok:true` teruggeeft, samen met een bijgewerkte
    statusbadge ("Geweigerd").
  - **Eén klik in het handtekeningvak volstond**: `hasInk` stond al op `true` bij het eerste
    contactpunt (`pointerdown`), vóór er iets getekend was. Nu telt `gl-v2-tekenen.js` de opgetelde
    lengte van de getekende lijn(en) (`Math.hypot` per stap) en pas vanaf 80px telt het als een echte
    handtekening — een tik zonder beweging draagt 0px bij. Enkel client-side (UX-gate); server-side
    blijft enkel "is er een PNG meegestuurd" gecontroleerd, geen pixelanalyse — dat woog niet op tegen
    de complexiteit, de eigenlijke bewijskracht zit in OTP + consent, niet in de tekening zelf.
  - **Voorbeeldvenster toonde na voltooiing nog het origineel**: de iframe op `Document.cshtml` wees
    altijd naar `Bestand` (het origineel); nu naar `Finaal` (het ondertekende PDF) zodra het dossier
    `Completed` is. `OndertekenenController.Finaal` zette bovendien `Content-Disposition: attachment`
    (forceert een download) — nu `inline`, zoals `Bestand`, anders toont de iframe niets.
  - **Verwarring over de twee verschillende SHA-256's** op `/verifieer/{id}`: "origineel" en
    "ondertekend document" zijn *bewust* verschillend (het ondertekende PDF heeft het
    ondertekeningsblad erachter geplakt, dat verandert de hash) — maar de pagina legde dat nergens uit.
    Tekst op `Views/Ondertekenen/Verifieer/Index.cshtml` aangevuld: welke hash je met welke kopie vergelijkt, en
    waarom ze altijd verschillen.
- [ ] **Herbevestigen in de browser** dat alle fixes kloppen (nieuw dossier doorlopen: lettertype,
      stap-volgorde, geen audit-download, handtekeningvak verplicht + zichtbaar als stempel op het blad).
- [ ] `Spiegelkopie naar opslag mislukt`-events in de audit trail van de eerste test (rode regels) —
      nog niet onderzocht, mogelijk `StorageApi:BaseUrl` niet bereikbaar vanaf de lokale/testomgeving.
      Blokkeert niets (bewaarregel is best-effort), maar wel na te kijken vóór productie.
- [ ] Bevestigingsmails (`SigningNotifier`) nog niet expliciet gecontroleerd met bijlage/downloadlink.
- [ ] Fast-follow (niet in deze scope): `Declined.cshtml` als apart scherm, typed-name als echte
      contractwijziging (staat los van de handtekening-canvas, die is nu wél gebouwd).

### Fase 3 — opvolging & hardening ✅ gebouwd (2026-09-28), ⚠️ nog niet in de browser getest
- [x] **`SigningHostedService`** (`CPMCore/Services/`, nieuw): elke 15 min `ExpireOverdueCasesAsync` +
      `SendDueRemindersAsync` + `ApplyRetentionScrubAsync` (alle drie al sinds fase 0 geïmplementeerd,
      niets riep ze nog aan) + nieuwe `RetryStuckFinalizationsAsync` (regel-complete dossiers met een
      eerder mislukte finalisatie, die `ExpireOverdueCasesAsync` alleen na de vervaldatum oppikte).
      Zelfde patroon als `IssueNotificationHostedService` (`SemaphoreSlim`-guard, scope per sub-job).
- [x] **`/api/trigger/signing?key=...`** in het bestaande `app.Map("/api/trigger", ...)`-blok
      (`Program.cs`) — fire-and-forget, 202 Accepted. **`TriggerKeys:Signing` staat nog nergens
      ingesteld**, zie "Eerst te doen".
- [x] **Verificatiepagina `/verifieer/{id}`** (`VerifieerController`, nieuw) — toont
      `PublicVerificationView` (label, nummer, status, voltooiingsdatum, aantal ondertekenaars, beide
      SHA-256's), geen namen/IP's. `GetPublicVerificationAsync` bestond al sinds fase 0.
- [x] **QR terug op het klantblad**: `SignedDocumentComposer` genereert 'm via het al bestaande
      `IEpcQrService` (QRCoder, hergebruikt van de facturatie-QR's) en geeft 'm door aan
      `SigningEvidenceDocument`. Was in fase 2 bewust weggelaten zolang `/verifieer` niet bestond.
- [x] **`ClientContactChangeLog` vullen** — twee schrijfpunten: `KlantenController.Edit` (ASP.NET Core-
      pad) en `ServiceCore/ClientService.InsertUpdate` (het oudere BO/translator-pad vanuit
      `Klanten/EditProject`). Enkel gemaskeerde oude/nieuwe waarden (`SigningCrypto.MaskEmail`/
      `MaskPhone`). **Bekende beperking**: het `InsertUpdate`-pad heeft geen gebruikerscontext
      (`ClientService`-constructor neemt enkel `UnitOfWorkCore`) → `ChangedByUserId` blijft daar leeg.
      Ook: een parallelle .NET-Framework-4.8-app (`CPM/Controllers/KlantenController.vb`) kan dezelfde
      tabellen bewerken buiten deze log om, als die nog in gebruik is — buiten scope van deze module.
- [x] **`ForwardedHeaders`** (`Program.cs`, allereerste middleware): `ForwardLimit=1`, lege
      `KnownNetworks`/`KnownProxies` — aanname dat IIS de enige hop is vóór de app (SmarterASP.NET,
      gedeelde IIS-hosting). Onbevestigd: als er nog een laag vóór IIS zit, moet `ForwardLimit` naar 2.
- [ ] **Browsertest**: `TriggerKeys:Signing` zetten, dossier voltooien, `/api/trigger/signing`
      aanroepen en de logs controleren, QR scannen → `/verifieer/{id}` checken, een klant-/contact-
      e-mail of -gsm wijzigen en de rij in `ClientContactChangeLog` controleren, en nagaan dat het IP
      in de audit trail nu een echt client-IP is i.p.v. steeds hetzelfde interne adres.

### Fase 4 — SMS-gereed ⬜
- [ ] Eerste `ISmsProvider`-adapter + delivery-status-webhook, achter `Signing:SmsProvider`

## Vierde feedbackronde (Niels, 2026-09-28) — Storage API + verwijderen
- [x] **"Spiegelkopie mislukt" opgelost**: `Storage/Program.cs` (het aparte, zelfstandig gedeployde
      Storage-API-project in deze solution) had een expliciete whitelist van toegelaten mappen
      (`AssetFolders.IsValid`) — **"signing" stond daar niet bij**, dus elke upload kreeg een 400. Map
      `Signing = "signing"` toegevoegd (privé, net als docs/plans/guarantees) + directory-aanmaak bij
      opstart. **Vereist een herdeploy van het `Storage`-project** — een lokale build van CPMCore
      verandert daar niets aan.
- [x] **Verwijderen van een ooit-ondertekende wijzigingsopdracht blokkeren**: `ProjectDocs.ChangeOrderId`
      heeft een FK naar `ChangeOrder` zonder `ON DELETE CASCADE` (migratie 054) — sinds fase 2 het
      ondertekende PDF daaraan koppelt, zou `DeleteChangeOrder` op een voltooid dossier crashen op een
      FK-fout. Nieuwe `ISigningService.GetCompletedCaseForSourceAsync` +
      `ProjectenController.CompletedSigningCaseAsync`/`SigningCompletedLockedMessage`: verwijderen wordt
      geweigerd zodra er ooit een `Completed`-dossier was voor die wijzigingsopdracht — zelfde soort
      melding als bij een lopend dossier. **Enkel een applicatie-check, geen DB-constraint**: rechtstreeks
      via SQL kan een beheerder dit nog altijd, met de FK-volgorde in het achterhoofd (eerst de
      `ProjectDocs`-rij/koppeling, dan pas `ChangeOrder`). Bewust *niet* gekozen: automatisch de
      SigningCase/SigningDocument/audit trail mee verwijderen — dat zou het juridische bewijs van de
      handtekening vernietigen, wat de hele opzet van de append-only hash-ketting tegenspreekt.
- [ ] Herdeploy `Storage`-project en herbevestigen dat een nieuw dossier geen
      `DocumentStorageMirrorFailed`-events meer krijgt.
- [ ] Testen: een voltooide wijzigingsopdracht proberen verwijderen → moet geweigerd worden met
      "Deze wijzigingsopdracht is elektronisch ondertekend en kan niet meer verwijderd worden. Het
      ondertekenbewijs (audit trail) blijft bewaard."

## Vijfde ronde (Niels, 2026-09-29) — uitbreiding t.b.v. Financieel/scherm 21b
Onderdeel van de bredere Financieel-herwerking (gl-v2, design-handoff 20-22): scherm 21b
("WO blokkeert de eindafrekening") heeft 4 opties, waarvan Herinneren/Annuleren al bestonden
(`SendReminderAsync`/`CancelCaseAsync`). De 2 ontbrekende zijn nu gebouwd:
- [x] `ISigningService.UploadSignedDocumentAsync(caseId, pdfBytes, fileName, ctx, ct)` — "Getekende
      versie opladen": het opgeladen PDF wordt zelf het definitieve document (géén door
      `SignedDocumentComposer` samengestelde evidence-pagina, dat zou het papieren bewijs vervalsen).
      Alle nog openstaande partijen worden als `Signed` geregistreerd (event `PartySigned`,
      `method: "Paper"`), het dossier gaat naar `Completed`, er wordt alsnog een auditrapport +
      dezelfde voltooiingsmails gegenereerd, en `ISigningDocumentSource.OnCaseCompletedAsync` wordt
      aangeroepen (zelfde hook als de digitale flow — geen apart code-pad voor `ChangeOrder.
      DateAgreement`/ProjectDocs-koppeling). Nieuw event-type `PaperDocumentUploaded`. PDF-check via
      magic bytes (`%PDF`) + nieuwe optie `Signing:MaxUploadedDocumentBytes` (default 20MB).
- [x] `ISigningService.DeclineByStaffAsync(partyId, reason, ctx, ct)` — "Weigering registreren": zelfde
      overgang als `DeclineAsync` (party → Declined, tokens intrekken, `CanStillComplete`-check, dossier
      evt. sluiten), maar aangestuurd door een beheerder via `partyId` i.p.v. de sessie van de
      ondertekenaar zelf (bv. telefonische weigering).
- [x] `SigningAdminController`: nieuwe acties `UploadSigned` (multipart POST) en `Decline` (POST reden).
      `Views/Ondertekenen/SigningAdmin/Dossier.cshtml`: "Getekende versie opladen" in het dossier-menu (multipart-
      modal), "Weigering registreren" per partij-rij (gedeelde modal, partyId via data-attributen,
      Bootstrap `relatedTarget`) — naast de al bestaande Herinneren/Nieuwe link/Annuleren.
- [ ] Browsertest: beide nieuwe acties in `SigningAdmin/Dossier` uittesten (upload met een echte PDF,
      weigering met en zonder reden), en nagaan dat de Facturatie-pagina (in aanbouw, zie
      `smooth-greeting-origami`-plan) een geblokkeerde WO na elke van de 4 opties correct bijwerkt.
- Vervolg: scherm 21b zelf (de 4-opties-keuzemodal op de Facturatie-pagina) is nog niet gebouwd — dat
  is stap E van het Facturatie-bouwplan, roept deze acties gewoon aan zoals `SigningAdmin/Dossier` al
  deed.

## Open punten / te bevestigen
- Akkoordtekst juridisch laten nakijken (`SigningPolicy.ConsentText`, seed in 047).
- Wie zijn de interne ontvangers van "ondertekend"/"geweigerd"? Voorlopig: verkoopverantwoordelijke van
  het project (`Project.SalesResponsibleUserID`), terugval op de aanmaker van het dossier.
- BTW: de bestaande PDF rekent met het btw-% van het project (`ProjectSalesSettings.VatPercentage`),
  niet met het %-veld per lijn; de QuestPDF-versie doet hetzelfde tot anders beslist.
