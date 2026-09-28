# Elektronisch ondertekenen — voortgangsstatus

Doorlopend statusdocument voor de signingmodule (SES voor wijzigingsopdrachten, generiek uitbreidbaar).
Het ontwerp staat in `ONDERTEKENEN_VOORSTEL.md`; dit bestand is de werkende samenvatting om op een
andere machine of in een volgende sessie meteen verder te kunnen. **Bijwerken na elke stap.**
Reist mee via git: commit + push vóór je van machine wisselt.

**Laatste update:** 2026-09-27 — fase 0 én fase 1 gebouwd (build groen, 31/33 tests; de 2 falende
zijn de bestaande Invoicing-tests). Fase 1 is nog **niet in de browser doorlopen** — dat is het eerste
wat op de volgende machine moet gebeuren (zie "Eerst te doen" hieronder). Daarna fase 2.

## Eerst te doen (volgende sessie)
1. **Commit + push** van alles wat nu uncommitted staat (fase 0 + fase 1; `git status` toont ~45 bestanden).
2. Module aanzetten op de testomgeving: `Features:EnableSigning=true`, `Signing:PublicBaseUrl`,
   `Signing:TestRecipientOverride=<eigen adres>` in appsettings; `Signing:OtpHmacKey` in user-secrets.
3. Permissie `Signing` toekennen aan je eigen rol (lezen + schrijven) — anders zie je niets.
4. Doorlopen: Projecten › Wijzigingsopdrachten › icoon "Elektronisch laten ondertekenen" → Start
   ("Bekijk de pdf" — **de QuestPDF-wijzigingsopdracht is nog niet visueel nagekeken**, enkel via een
   rooktest gerenderd) → Aanbieden → Dossier (partijen "Uitgenodigd", audit trail, herinnering,
   nieuwe link, annuleren met reden, downloads) → Index via het menu-item "Ondertekeningen".
   De uitnodigingsmail komt op het testadres; de link erin werkt pas in fase 2.
5. Controleer ook de legacy lijst: status "Ter ondertekening (elektronisch)", slotje i.p.v. bewerken,
   en dat `Projecten/ChangeOrderPDF` (afdrukken) dezelfde PDF geeft.

## Hoe je verder werkt op een andere machine
1. `git pull`. Build: `dotnet build CPMCore/CPMCore.csproj`. Tests: `dotnet test ServiceCore.Tests`
   (30 slagen; de 2 falende `StructuredReferenceServiceTests` zijn een bestaand probleem in
   `ServiceCore.Invoicing`, los van signing).
2. User-secrets op die machine (zelfde `UserSecretsId`, staan NIET in git): `CPMRUNNING:*` (bestaand)
   én, zodra je de module aanzet, `Signing:OtpHmacKey` (≥ 32 bytes base64). `Signing:PublicBaseUrl` en
   `Features:EnableSigning` staan in `appsettings.json` (dat bestand is git-genegeerd → ook daar
   overnemen: secties `Features` en `Signing`, zie DEPLOY.md "Elektronisch ondertekenen").
3. Testdb (`db_ab5fbb_testdb`) heeft migraties 047 en 048 al. Nieuwe migraties: `_migrations/NNN_*.sql`,
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
- [x] Migratie `047_Signing.sql` — toegepast op testdb; append-only trigger getest (UPDATE/DELETE geweigerd, scrub toegelaten)
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
- [x] `SigningCase.SourceFingerprint` (migratie 048, op testdb) + `ISigningDocumentSource.ComputeFingerprintAsync`
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
- [x] `SigningAdminController` + `Views/SigningAdmin/{Start,Dossier,Index}.cshtml`, `gl-v2-signing.css/.js`,
      `Models/Signing/SigningAdminVms.cs` (incl. `SigningLabels`), kruimelpaden
- [x] Ingang op de legacy lijst (`Klanten/Partials/ChangeOrders.cshtml`: status + icoon + slotje) en
      "Ondertekeningen" in `GlV2/_ProjectInnerMenuV2` (feature-vlag + permissie `Signing`)
- [x] `Projecten/ChangeOrderPDF` rendert via `ChangeOrderPdfBuilder` (Rotativa-view blijft ongebruikt staan)
- [x] `_LayoutV2`: `ViewData["GlV2NoProjectMenu"]` → `IgnoreSection("ProjectMenu")` (Razor kan geen `@section` in `@if`)
- [x] DESIGN.md, DEPLOY.md, ONDERTEKENEN_VOORSTEL.md bijgewerkt; build groen; tests 31/33 (2 bestaande fouten)
- [ ] **Browsertest** van alles hierboven (zie "Eerst te doen"); visuele controle van de PDF
- [ ] Beslissen: `Views/Projecten/ChangeOrderPDF.cshtml` (+ `ChangeOrderFooter`-actie) verwijderen zodra de
      QuestPDF-versie goedgekeurd is

### Fase 2 — ondertekenpagina ⬜
- [ ] `OndertekenenController` ([AllowAnonymous], `/ondertekenen/{token}` → sessiecookie → `/ondertekenen/document`)
- [ ] Views op `_LayoutPublic`: document (PDF inline via sessie-route), verificatie, akkoord + handtekeningpad
      (eigen canvas-JS, geen CDN), bevestiging, weigeren, neutrale foutpagina
- [ ] `Documents/SignedDocumentComposer` (origineel + ondertekeningsblad via PdfSharpCore + QuestPDF)
      en `Documents/SigningAuditReportDocument` → echte `ISigningDocumentRenderer`
- [ ] Bevestigingsmails met bijlage + downloadlink

### Fase 3 — opvolging & hardening ⬜
- [ ] `SigningHostedService` (verlopen, herinneringen, retentiescrub, hervat finalisatie) + `/api/trigger/signing`
- [ ] Verificatiepagina `/verifieer/{id}` + QR op het blad
- [ ] `ClientContactChangeLog` vullen vanuit de klantformulieren (e-mail/gsm-wijzigingen)
- [ ] `ForwardedHeaders` voor het rate-limiter-IP achter de proxy

### Fase 4 — SMS-gereed ⬜
- [ ] Eerste `ISmsProvider`-adapter + delivery-status-webhook, achter `Signing:SmsProvider`

## Open punten / te bevestigen
- Akkoordtekst juridisch laten nakijken (`SigningPolicy.ConsentText`, seed in 047).
- Wie zijn de interne ontvangers van "ondertekend"/"geweigerd"? Voorlopig: verkoopverantwoordelijke van
  het project (`Project.SalesResponsibleUserID`), terugval op de aanmaker van het dossier.
- BTW: de bestaande PDF rekent met het btw-% van het project (`ProjectSalesSettings.VatPercentage`),
  niet met het %-veld per lijn; de QuestPDF-versie doet hetzelfde tot anders beslist.
