# Elektronisch ondertekenen in CPMCore — analyse en implementatievoorstel

Status: **goedgekeurd op 27/09/2026 voor beslissingen 1, 2 en 3** (opslag in SQL én Storage API; bron vergrendelen; token → sessiecookie). Overige beslissingen (§9.3–9.11) volgen de aanbeveling tenzij anders beslist.
**Fase 0 (fundament) is gebouwd op 27/09/2026**: enums, entiteiten + `cpmRunningContext.Signing.cs`, migratie 055 (toegepast op testdb, trigger getest), contracten in `FacadeCore.Signing`, services in `ServiceCore.Signing` (SigningService, evidence store met hash-ketting, tokens/OTP, SES-provider, e-mail-OTP, kanalen, notifier, storage-client), CPMCore-bedrading (DI, `Features:EnableSigning`, rate limiter, security-headers-middleware, `_LayoutPublic`), 30 unit-tests. Twee toevoegingen op vraag: **testmodus** (`Signing:TestRecipientOverride`, alle mails naar één adres) en de **bewaarregel** (§3.3: enkel ondertekende dossiers houden hun PDF-bytes). **Fase 1 is gebouwd op 27/09/2026**: `ChangeOrderSigningSource` (eerste `ISigningDocumentSource`), de wijzigingsopdracht als QuestPDF (`ChangeOrderDocument`, ook voor de legacy afdrukactie), vingerafdruk van de bron i.p.v. "gewijzigd sinds" (`SigningCase.SourceFingerprint`, migratie 056), vergrendeling in `ProjectenController`, en de interne gl-v2-schermen `SigningAdmin` Start/Dossier/Index + menu-item "Ondertekeningen". Nog niet: de publieke ondertekenpagina (fase 2) — een uitnodigingslink leidt tot dan nergens heen. Werkende status: `ONDERTEKENEN_VOORTGANG.md`.
Eerste toepassing: wijzigingsopdrachten laten goedkeuren en ondertekenen door klanten (SES).
Doel van dit document: vastleggen wat er al is, hoe de module daarop aansluit, welke gegevens en
abstracties nodig zijn, welke scenario's in de opdracht ontbraken, en welke keuzes eerst een
beslissing vragen. Na akkoord volgt de gefaseerde implementatie (§10).

---

## 1. Wat de codebase vandaag al heeft (en wat de module daarvan hergebruikt)

| Gebied | Wat er is | Hergebruik in de signingmodule |
|---|---|---|
| **Lagen** | `DALCore` (EF Core 9, `cpmRunningContext` + partial-bestanden per feature, `UnitOfWorkCore` met één `GenericRepository<T>` per entiteit) → `ServiceCore` (services, C# DTO's zoals `ServiceCore.Invoicing`) → `FacadeCore` (interfaces) → `CPMCore` (MVC). | Zelfde lagen. Nieuwe partial `cpmRunningContext.Signing.cs` met `ConfigureSigningEntities`, aangeroepen uit `OnModelCreatingPartial` (Seeding.cs) — exact zoals Taak/Dossier/Traject. |
| **Migraties** | Handmatige, idempotente T-SQL-scripts in `_migrations/NNN_*.sql` (laatste vóór de merge: 054). | Migratie `055_Signing.sql`. |
| **Authenticatie** | Cookie + Entra OIDC via `Microsoft.Identity.Web`. `FallbackPolicy` = ingelogd én claim `cpm:user-id`. `UserType`-claim kent al `"customer"`, maar er is nog géén klantportaal (de filter verwijst naar `/klantenportaal`, die controller bestaat niet). | De ondertekenpagina is per definitie anoniem → eigen controller met `[AllowAnonymous]` op de hele controller, buiten de FallbackPolicy. Precedent: `ContractorInviteController.Hernieuw` ([AllowAnonymous], `Layout = null`). |
| **Permissies** | `PermissionConventionFilter` + `PermissionCodes` (o.a. `Projects.ChangeOrders`, `DocumentCenter`). | Starten/annuleren van een dossier valt onder het permissiecode van het brondocument (`Projects.ChangeOrders`); het projectbrede overzicht krijgt een nieuw code `Signing`. |
| **Tokens/URL-signing** | `ResendInviteUrlBuilder`: HMAC-SHA256, base64url, `CryptographicOperations.FixedTimeEquals`. Aannemersuitnodigingen lopen via Entra-gastaccounts, niet via eigen tokens. `SHA256.HashData` in `CpmUserAccessService`. | Zelfde primitieven (base64url, fixed-time compare). **Niet** overnemen: de fallback-secret `"GroupLN-ResendInvite-Fallback"` in die builder — een signingmodule mag nooit met een ingebakken sleutel draaien. |
| **E-mail** | `IEmailSender` → `SmtpEmailSender` (Office 365 SMTP, bijlagen, inline-images). Beheerbare templates in DB (`EmailTemplateBO`: Naam/Onderwerp/BodyHtml, plaatshouders `{Voornaam}`, `{Naam}`, `{ProjectNaam}`, `{Gemeente}` — vervangen met `string.Replace` in de controller). `EmailSendLog` per project/contact. Versturen gebeurt synchroon in de request. | `IEmailSender` wordt de *e-mailkanaal-adapter* achter een kanaalabstractie (§4.3). Templates: de vijf signing-mails komen als code-templates met eigen plaatshouders (`{OndertekenLink}`, `{Code}`, …), niet als vrij bewerkbare DB-templates — een beheerder mag de OTP-mail niet per ongeluk zonder `{Code}` opslaan. Optioneel later: DB-override per template. |
| **PDF** | Drie engines naast elkaar: **Rotativa** (wkhtmltopdf) voor `ChangeOrderPDF` (Razor-view → PDF, alleen als download, nooit als bytes bewaard), **QuestPDF** voor facturen (`IInvoicePdfService.Render → byte[]`) en de werf-PDF's (`Documents/GroupLnPdfDocument` met aannemers-, klanten- en eenhedenlijst), **PdfSharpCore** voor het samenvoegen van PDF's (`InvoicesController.AppendPages`). Ook aanwezig: PdfPig (lezen), QRCoder. | Origineel = de bestaande `ChangeOrderPDF`-view via Rotativa's `BuildFile()` naar bytes. Ondertekeningsblad en auditrapport = QuestPDF op de `GroupLnPdfDocument`-basis (huisstijl, kop/voet, fiche-raster). Definitieve PDF = origineel + ondertekeningsblad samengevoegd met PdfSharpCore (precedent `AppendPages`). QR-code op het blad met QRCoder. |
| **Documentopslag** | Externe Storage API (`StorageApi:BaseUrl` + read/write-keys): `POST /api/assets/upload` (map + bestand → `fileName`), `POST /api/assets/{map}/{file}/sign` → tijdelijk ondertekende URL. De helpers zijn **privé in `ProjectenController`** en gedupliceerd in `ProjectIssuesController`. Het `Storage/`-project in deze repo is een lege stub; de echte API staat elders. | Eerst refactor naar één `IAssetStorageClient` (ServiceCore) — de signingmodule mag geen derde kopie van die helpers worden. Map `signing/`. Zie beslissing §9.1 over onveranderlijkheid. |
| **Audit-achtige tabellen** | `ProjectDossierGebeurtenis` (Datum/Type/Titel/Tekst/UserId), `EmailSendLog`, `UserGuestInvitationAudit` (Action/Details/PerformedBy/PerformedAt), `MijlpaalHistoriek`. Allemaal gewone tabellen zonder append-only-afdwinging. | Zelfde vorm, maar mét afdwinging: DB-trigger die UPDATE/DELETE weigert + hash-ketting (§4.8). |
| **Achtergrondwerk** | `BackgroundService`-pollers (`TrajectHostedService`, `IssueNotificationHostedService`, `VoortgangHostedService`) met `IServiceScopeFactory`, plus `/api/trigger/...?key=` als handmatige/externe trigger. | `SigningHostedService` (verlopen, herinneringen, retentie). |
| **Strategy-patroon via DI** | `ITrajectTriggerAction` wordt meermaals geregistreerd en op sleutel gekozen (`TrajectTriggerDispatcher`). | Zelfde recept voor ondertekenmethodes, verificatiemethodes, kanalen en documentbronnen (§4). |
| **Webhooks** | `/api/trigger` (query-string-sleutel, GET). | Onvoldoende als patroon voor provider-callbacks: die krijgen een eigen controller met HMAC-handtekening op de body, replay-venster en idempotentie (§4.9). |
| **Ontbreekt volledig** | Rate limiting (geen `AddRateLimiter`), security headers (geen CSP/X-Frame-Options/Referrer-Policy), publieke layout, canvas/handtekening-JS, provider-onafhankelijke berichtenlaag. | Allemaal nieuw, maar generiek (niet signing-specifiek) waar dat kan. |

### 1.1 Hoe wijzigingsopdrachten vandaag werken
`ChangeOrder` (klantaccount, omschrijving, datum, vervaldatum, opmerking, `DateSendToClient`, `DateAgreement`, `Invoiceable`, contractactiviteit, voorwaarden) met `ChangeOrderDetail`-lijnen (omschrijving, aantal, prijs, commissie, btw-% per lijn). Er is **geen statusveld en geen nummer** (het Id is het nummer). "Verzonden naar klant" en "Akkoord" zijn **handmatig ingetypte datums** op het bewerkformulier; er wordt geen mail verstuurd; de PDF wordt on-the-fly gegenereerd en nooit bewaard. `DuplicateChangeOrder` wist die twee datums. Klantzijde: `ClientAccount` (naam, e-mail, bedrijf/btw) met `ClientContacts` (naam, e-mail, **gsm**, `IsCoOwner`, `IsPrimaryContact`, `CoOwnerPercentage`) en `ClientPoa` (volmachten).

De signingmodule vervangt die handmatige datums door een procedure; de velden blijven bestaan voor papieren akkoorden en worden bij voltooiing automatisch gevuld, met een herkomstmarkering (§5.4).

---

## 2. Ontwerpprincipes

1. **Eén generieke kern, nul kennis van wijzigingsopdrachten.** De kern kent alleen "een document van type X met bytes, metadata en ondertekenaars". Wat een wijzigingsopdracht is, weet uitsluitend `ChangeOrderSigningSource` (§4.4). Een aannemerscontract is later één extra klasse.
2. **Bewijs is server-side.** Niets dat uit de browser komt telt als bewijs, behalve de handtekeningafbeelding (die is expliciet enkel visueel) en de akkoord-checkbox (waarvan de server de tekst en het tijdstip zelf vastlegt).
3. **Snapshots, geen verwijzingen.** Documentbytes, hash, akkoordtekst, beleid (OTP vereist?, regel), naam/e-mail van de ondertekenaar: alles wordt in het dossier *gekopieerd* op het moment van aanbieden. Latere wijzigingen aan klant, template of beleid raken een lopend of afgesloten dossier niet.
4. **Append-only en tamper-evident.** Gebeurtenissen worden nooit gewijzigd; de database weigert het, en een hash-ketting maakt verwijdering achteraf zichtbaar.
5. **Geen geheimen in rust, geen geheimen in logs.** Tokens en OTP's bestaan alleen als hash/HMAC in de database; logging krijgt enkel id's.
6. **Falen zonder te lekken.** Elke fout op de publieke pagina is dezelfde neutrale melding.
7. **SES, en dat zeggen we ook.** Nergens "gekwalificeerd" of "AES"; de methode staat als `SES` in het dossier en op het ondertekeningsblad.

---

## 3. Gegevensstructuur (migratie `055_Signing.sql`)

Alle tijdstippen `datetime2` in **UTC** (`sysutcdatetime()`), zoals `ProjectTaak`. Gebruikers-id's als `int` (`Users.Id`). Namen in het Nederlands, zoals de recente tabellen.

### 3.1 `SigningPolicy` — beleid per documenttype
Beheerbaar (later via instellingen), met seed voor `ChangeOrder`.
`Id`, `DocumentType` (nvarchar 50, uniek: `ChangeOrder`, later `ContractorContract`, …), `SignatureMethod` (`SES`), `SigningRule` (`ALL`/`ANY`/`ORDERED`), `OtpRequired` (bit), `VerificationMethod` (`EmailOtp`/`SmsOtp`/…), `OtpValiditySeconds` (600), `OtpMaxAttempts` (5), `LinkValidityDays` (30), `ReminderAfterDays` (3), `ReminderRepeatDays` (7), `ConsentText` (nvarchar max — de *huidige* standaardtekst), `RetentionDays` (null = onbeperkt), `IsActive`.

### 3.2 `SigningCase` — het ondertekeningsdossier (aggregate root)
`Id`, `PublicVerificationId` (uniqueidentifier, uniek — verschijnt op het blad en de verificatiepagina; nooit het interne Id), `DocumentType`, `SourceEntityId` (int — bv. ChangeOrder.Id), `ProjectId`, `ClientAccountId` (null bij niet-klantdocumenten), `Title`, `DocumentNumber` (nvarchar 50), `Summary` (nvarchar max — korte omschrijving), `AmountExclVat`, `VatAmount`, `AmountInclVat` (decimal 18,2, nullable), `Status` (int-enum: `Draft`=0 → `Open`=1 → `Completed`=2 / `Declined`=3 / `Expired`=4 / `Cancelled`=5), `SigningRule`, `SignatureMethod`, `ProviderKey` (`internal-ses`), `ProviderCaseRef` (nvarchar 200, voor externe providers), **beleidssnapshot**: `OtpRequired`, `VerificationMethod`, `ConsentTextSnapshot`, `OriginalDocumentId` → `SigningDocument`, `FinalDocumentId`, `AuditReportDocumentId`, `CreatedByUserId`, `CreatedAt`, `OpenedAt` (moment van aanbieden), `ExpiresAt`, `CompletedAt`, `CancelledAt`, `CancelledByUserId`, `CancelReason`, `SupersededByCaseId` (het nieuwe dossier na een inhoudelijke wijziging), `RetentionUntil`, `RowVersion` (rowversion).
Indexen: `(DocumentType, SourceEntityId, Status)`, `ProjectId`, `ClientAccountId`, `Status`, `ExpiresAt`, uniek op `PublicVerificationId`.
**Regel**: hoogstens één dossier met `Status IN (Draft, Open)` per `(DocumentType, SourceEntityId)` — gefilterde unieke index.

### 3.3 `SigningDocument` — onveranderlijke bestanden
`Id`, `SigningCaseId`, `Kind` (`Original`=0, `Final`=1, `AuditReport`=2, `Attachment`=3, `SignatureImage`=4), `FileName`, `ContentType`, `ByteLength`, `Sha256` (char 64, hex), `StorageFileName` (de naam die de Storage API teruggaf), `Content` (varbinary(max), **nullable — zie beslissing §9.1**), `CreatedAt`, `CreatedByUserId` (null = systeem).

**Bewaarregel (beslist 27/09/2026): enkel dossiers die effectief ondertekend werden houden hun PDF-bytes.** Tijdens een lopende procedure staan origineel en bijlagen tijdelijk in de tabel (en als spiegel in de Storage API). Sluit het dossier zonder voltooiing — verlopen, geweigerd of geannuleerd — dan verwijdert de service meteen `Content` én de spiegelkopie (`PurgeUnsignedDocumentsAsync`, event `DocumentContentPurged`). Naam, grootte en SHA-256 blijven staan, zodat de audit trail leesbaar blijft en de hash later nog aantoonbaar is. Daarbuiten worden rijen nooit gewijzigd of verwijderd.

### 3.4 `SigningParty` — ondertekenaar
`Id`, `SigningCaseId`, `SortOrder` (voor ORDERED), `PartyType` (`ClientAccount`/`ClientContact`/`InternalUser`/`External`), `SourceRefId` (ClientAccountId/ClientContactId/UserId), `DisplayName`, `Email` (snapshot), `PhoneMasked` (`•••• 47 82`; het echte nummer wordt **niet** hier bewaard maar op het moment van SMS-verzending uit `ClientContacts.Cellphone` gelezen — §6.3), `Capacity` (nvarchar 200: "eigenaar", "mede-eigenaar", "namens BV X", "volmachthouder"), `Status` (`Pending`=0, `Invited`=1, `Opened`=2, `Verified`=3, `Signed`=4, `Declined`=5, `Expired`=6, `Revoked`=7), `InvitedAt`, `LastReminderAt`, `ReminderCount`, `FirstOpenedAt`, `VerifiedAt`, `ConsentAcceptedAt`, `ConsentTextSnapshot` (nvarchar max — per ondertekenaar, want de tekst kan tussen twee ondertekenaars in gewijzigd zijn *als* er een nieuw dossier komt; binnen één dossier is het dezelfde tekst, maar de kopie per persoon is wat juridisch telt), `SignedAt`, `SignedIp`, `SignedUserAgent`, `SignatureImageDocumentId`, `DeclinedAt`, `DeclineReason`, `PartyVerificationId` (uniqueidentifier — het unieke verificatie-ID op het ondertekeningsblad), `ProviderPartyRef`, `RowVersion`.

### 3.5 `SigningAccessToken` — de persoonlijke link
`Id`, `SigningPartyId`, `TokenHash` (char 64 — SHA-256 van het ruwe token, uniek index), `Purpose` (`Invite`/`Reminder`/`Regenerated`/`Download`), `CreatedAt`, `ExpiresAt`, `FirstUsedAt`, `LastUsedAt`, `UseCount`, `RevokedAt`, `RevokedReason`, `CreatedByUserId`.
Meerdere tokens per ondertekenaar zijn normaal (herinnering, nieuwe link); "nieuwe link" = oude token(s) intrekken + nieuwe uitgeven. `Download`-tokens hebben `Purpose = Download` en kunnen **alleen** downloaden, nooit ondertekenen.

### 3.6 `SigningVerification` — één OTP-cyclus
`Id`, `SigningPartyId`, `Method` (`EmailOtp`/`SmsOtp`), `ChannelKey` (`email`/`sms`), `ProviderKey` (`smtp-o365`, later `bird`/`twilio`/`cm`), `DestinationMasked` (`j•••@peeters.be`, `•••• 47 82`), `CodeHmac` (char 64 — §6.2), `RequestedAt`, `RequestedIp`, `ExpiresAt`, `AttemptCount`, `MaxAttempts`, `Status` (`Sent`=0, `Accepted`=1 (provider aanvaardde), `Delivered`=2, `Verified`=3, `Failed`=4 (te veel pogingen), `Expired`=5, `Superseded`=6), `ProviderMessageId`, `ProviderStatus`, `ProviderAcceptedAt`, `DeliveredAt`, `VerifiedAt`, `VerifiedIp`.
De OTP zelf staat nergens.

### 3.7 `SigningEvent` — de audit trail (append-only)
`Id` (bigint), `SigningCaseId`, `SigningPartyId` (null bij dossierbrede events), `EventType` (nvarchar 60 — vaste sleutels, §7), `OccurredAtUtc`, `ActorType` (`System`/`Internal`/`Party`/`Provider`), `ActorUserId` (int, intern), `ActorLabel` (nvarchar 200 — naam/e-mail op dat moment), `Ip` (nvarchar 45), `IpHash` (char 64), `UserAgent` (nvarchar 500), `UserAgentHash` (char 64), `DocumentSha256` (char 64 — welk document op dat moment "het document" was), `DataJson` (nvarchar max — gemaskeerde details: `DestinationMasked`, `ProviderMessageId`, reden van weigering, …; **nooit** tokens of codes), `PrevEventHash` (char 64), `EventHash` (char 64).
- **Afdwinging**: `INSTEAD OF UPDATE, DELETE`-trigger die `THROW` doet. De applicatie heeft geen update-pad.
- **Hash-ketting**: `EventHash = SHA256(PrevEventHash | CaseId | PartyId | EventType | OccurredAtUtc | ActorType | ActorUserId | DocumentSha256 | IpHash | UserAgentHash | DataJson)`. De ruwe `Ip`/`UserAgent` zitten **niet** in de ketting, enkel hun hash — zodat een retentiescrub (§8) die ruwe waarden mag nullen zonder de ketting te breken. Verifieerbaar via een interne "Controleer audit trail"-actie én in het auditrapport.
- Index `(SigningCaseId, Id)`.

### 3.8 Bronkoppeling — géén nieuwe kolommen op `ChangeOrder`
De relatie loopt via `SigningCase.DocumentType + SourceEntityId`. `ChangeOrder` krijgt geen FK naar signing; `DateSendToClient`/`DateAgreement` worden bij voltooiing gevuld door de completion-handler (§4.4). Zo blijft `ChangeOrder` onwetend van de module — dezelfde reden waarom `Units` geen kennis heeft van dossiers.

### 3.9 Configuratie (user-secrets/appsettings, sectie `Signing`)
`OtpHmacKey` (≥ 32 bytes, **verplicht, geen fallback**), `PublicBaseUrl` (voor absolute links in mails; de app kent vandaag geen eigen basis-URL in config), `TokenBytes` (32), `SigningSessionMinutes` (30), rate-limits (§6.5). Ontbreekt de key → de module weigert te starten (fail closed), zoals `CPMRUNNING:DbUser` dat al doet.

---

## 4. Abstracties — de acht onderscheiden verantwoordelijkheden

Namespace `ServiceCore.Signing` (implementaties) + `FacadeCore.Signing` (contracten en DTO's). Registratie in `Program.cs` als multi-registratie op interface + keuze op `Key` via een registry — het `ITrajectTriggerAction`-recept.

### 4.1 `ISigningService` — aanmaken en beheren van de procedure (1, 3, 4)
```
CreateCaseAsync(CreateSigningCaseRequest)   → dossier in Draft: bron ophalen, PDF bouwen, hash, opslaan, partijen, beleidssnapshot
OpenCaseAsync(caseId, byUserId)             → Draft → Open, tokens uitgeven, uitnodigingen versturen (ALL/ANY: iedereen; ORDERED: enkel de eerste)
CancelCaseAsync(caseId, byUserId, reason)   → tokens intrekken, partijen Revoked, bron ontgrendelen, info-mail
SendReminderAsync(partyId, byUserId)        → nieuw Reminder-token (oude blijft geldig), herinneringsmail
RegenerateLinkAsync(partyId, byUserId)      → alle tokens van die partij intrekken, nieuw token, mail
GetCaseStatusAsync(caseId)                  → status per partij (het overzicht uit de opdracht)
GetActiveCaseForSourceAsync(type, sourceId) → voor de bron-UI ("er loopt een procedure")
VerifyAuditChainAsync(caseId)
```
Alle overgangen onder een transactie met `RowVersion`-controle. De service kent geen HTTP.

### 4.2 `ISigningDocumentSource` — het document (2)
Per documenttype één implementatie, gekozen op `DocumentType`:
```
string DocumentType { get; }
Task<SigningDocumentPackage> BuildAsync(int sourceEntityId, int byUserId)
    → Pdf (bytes), Title, DocumentNumber, Summary, bedragen, Attachments[], ProjectId, ClientAccountId,
      SuggestedParties[] (naam/e-mail/type/capacity), SuggestedRule
Task OnCaseOpenedAsync(case)      → bron vergrendelen (§9.2)
Task OnCaseCompletedAsync(case)   → bron bijwerken (ChangeOrder.DateAgreement = datum laatste handtekening, DateSendToClient = OpenedAt)
Task OnCaseClosedAsync(case)      → bron ontgrendelen bij Cancelled/Declined/Expired
Task<bool> HasChangedSinceAsync(sourceEntityId, since)  → vangnet naast de vergrendeling
```
`ChangeOrderSigningSource` bouwt de PDF met de bestaande `ChangeOrderPDF`-view via Rotativa `ViewAsPdf.BuildFile(ControllerContext)`. Omdat dat een MVC-afhankelijkheid is, zit het renderen zelf achter `IChangeOrderPdfRenderer` in CPMCore; de source in ServiceCore roept die interface aan. (Zie §9.8 over QuestPDF op termijn.)

### 4.3 `ISignatureMethodProvider` — de ondertekenmethode (5)
```
string Key { get; }                                   // "internal-ses", later "itsme", "oksign", …
SignatureCapabilities Capabilities { get; }           // HostedUi? RequiresOwnOtp? SupportsWebhooks? ProducesSignedPdf?
Task<ProviderStart> StartAsync(SigningCase)           // SES: no-op; extern: dossier aanmaken bij provider, redirect-URL
Task<SignatureEvidence> CompleteAsync(SigningParty, SignRequestEvidence)  // SES: bewijs = OTP-verificatie + akkoord + hash
Task<ProviderCallbackResult> HandleCallbackAsync(rawBody, headers)         // extern: status/document terug
Task<byte[]> ComposeFinalDocumentAsync(SigningCase, original)              // SES: origineel + ondertekeningsblad; extern: het door de provider ondertekende PDF
```
`InternalSesProvider` is de enige v1-implementatie. De publieke pagina vraagt aan de provider *wat* ze moet tonen: bij `HostedUi = true` (itsme/OK!Sign) rendert ze een "Ga verder bij <provider>"-knop in plaats van OTP+pad. Zo raakt provider-logica nooit de ChangeOrder-code.

### 4.4 `IVerificationMethod` — de verificatiemethode (6)
```
string Key { get; }                                   // "EmailOtp", "SmsOtp", later "ItsmeIdentify", …
Task<VerificationDestination> ResolveDestinationAsync(SigningParty)  // waar naartoe: e-mail uit snapshot / gsm uit ClientContacts
Task<VerificationRequestResult> RequestAsync(SigningParty, RequestContext)
Task<VerificationResult> VerifyAsync(SigningParty, string code, RequestContext)
```
`OtpVerificationMethodBase` bevat het OTP-mechanisme (genereren, HMAC, geldigheid, pogingen, superseden) **eenmaal**; `EmailOtpMethod` en `SmsOtpMethod` verschillen alleen in bestemming en kanaal. CPM genereert en valideert; het kanaal levert enkel af.

### 4.5 `IMessageChannel` + providers — het communicatiekanaal (7)
```
IMessageChannel { string Key; Task<DeliveryReceipt> SendAsync(OutboundMessage) }   // "email", "sms"
ISmsProvider   { string Key; Task<DeliveryReceipt> SendAsync(SmsMessage); Task<DeliveryStatus?> ParseStatusWebhookAsync(...) }
DeliveryReceipt { ProviderKey, ProviderMessageId, AcceptedAt, Status }
```
`EmailChannel` wikkelt de bestaande `IEmailSender`. `SmsChannel` delegeert naar de geconfigureerde `ISmsProvider` (`Signing:Sms:Provider = "bird"`); in v1 wordt géén SMS-provider geregistreerd, wel de interface, de kanaalkeuze en het webhook-endpoint voor delivery-status, zodat een provider later één adapterklasse is. Notificatiemails (uitnodiging, herinnering, bevestiging) gaan via een aparte `ISigningNotifier`; OTP-berichten via `IMessageChannel`. Twee wegen, bewust: een template-fout in een notificatie kan nooit een OTP tegenhouden, en omgekeerd.

### 4.6 `ISigningEvidenceStore` — bewijs en audit (8)
```
Task AppendAsync(SigningEventDraft)           // berekent PrevEventHash/EventHash, schrijft
Task<IReadOnlyList<SigningEvent>> ListAsync(caseId)
Task<ChainVerification> VerifyAsync(caseId)
```
Niemand schrijft rechtstreeks in `SigningEvent`; alles gaat via deze store, zodat de ketting nooit een gat krijgt.

### 4.7 Documentopslag — `IAssetStorageClient` (refactor)
`UploadAsync(stream, fileName, contentType, folder) → storedName`, `GetSignedUrlAsync(folder, storedName, ttl)`, `DownloadAsync(folder, storedName) → bytes`. De bestaande privé-helpers uit `ProjectenController`/`ProjectIssuesController` verhuizen hierheen; beide controllers roepen voortaan de client aan. Dit is een op zichzelf staande opruiming die ook zonder signing de moeite is.

### 4.8 Provider-callbacks (toekomst, maar het endpoint bestaat vanaf dag één)
`SigningWebhookController` (`[AllowAnonymous]`, `POST /api/signing/webhook/{providerKey}`): leest de ruwe body, laat de provider de handtekening op de body verifiëren (HMAC/secret per provider, replay-venster op timestamp, `ProviderMessageId`-idempotentie via `SigningEvent`), en pas dan `HandleCallbackAsync`. Geen query-string-sleutels zoals `/api/trigger`.

---

## 5. Gebruikersstromen

### 5.1 Intern: aanbieden ter ondertekening (wijzigingsopdracht)
`DetailsChangeOrder` (en later de gl-v2-variant) krijgt per wijzigingsopdracht een knop **"Ter ondertekening aanbieden"** → modal:
- ondertekenaars vooringevuld uit `ChangeOrderSigningSource.SuggestedParties`: de `ClientAccount` (hoofdondertekenaar, e-mail = `ClientAccount.Email` of het primaire contact) + elke `ClientContacts` met `IsCoOwner = 1` die een e-mailadres heeft; regel **ALL** (beslissing §9.9);
- ontbreekt een e-mailadres bij een mede-eigenaar → blokkerende melding met link naar het contact (geen ondertekenaar stil overslaan);
- hoedanigheid per persoon (eigenaar / mede-eigenaar / namens bedrijf / volmacht);
- vervaldatum (standaard beleid: 30 dagen), OTP-kanaal (v1: e-mail, alleen-lezen).
Bevestigen → `CreateCaseAsync` (PDF, hash, opslag) → `OpenCaseAsync` (tokens, mails). De wijzigingsopdracht is vanaf dan vergrendeld (§9.2).

### 5.2 Publiek: de ondertekenpagina
1. **Link** `https://<cpm>/ondertekenen/{token}` → server: `SHA256(token)` opzoeken (fixed-time), token niet ingetrokken/verlopen, partij en dossier `Open`, partij niet `Signed/Declined/Revoked`. Fout → één neutrale pagina ("Deze ondertekenlink is ongeldig of niet meer beschikbaar."), HTTP 200, geen onderscheid tussen oorzaken, zelfde responstijd.
2. **Sessie-wissel (hardening, §9.6)**: geldige token → `SigningSession`-cookie (HttpOnly, Secure, SameSite=Strict, 30 min glijdend, inhoud = versleuteld sessie-id ↔ partij) → 302 naar `/ondertekenen/document` **zonder token in de URL**. Het token verdwijnt zo uit browsergeschiedenis, proxylogs en `Referer`.
3. **Documentpagina** (event `LinkOpened`, eerste keer `PartyOpened`): kop met documenttype, nummer, project, omschrijving, bedragen excl./btw/incl., bijlagen, de volledige PDF inline (`/ondertekenen/document/pdf` — streamt de bytes uit opslag *na hash-controle*, `Cache-Control: no-store`, `Content-Disposition: inline`, alleen met geldige sessie; nooit een storage-URL naar buiten). Pas nadat de PDF-viewer geladen is, wordt "Verder" actief (event `DocumentViewed` bij scrollen/klik — client-signaal, dus enkel informatief; het server-side `PartyOpened` + het serveren van de PDF is het bewijs).
4. **Verificatie** (indien `OtpRequired`): "Verstuur verificatiecode naar j•••@peeters.be" → `RequestAsync` → mail → code invoeren → `VerifyAsync`. Bij SMS later identiek, met "•••• 47 82".
5. **Akkoord**: de exacte `ConsentTextSnapshot` van het dossier, met checkbox. Geen voorafgevinkt vakje.
6. **Handtekening**: canvas (muis/touch/stylus; eigen ~150 regels JS, geen externe lib), wissen en opnieuw. Wordt als PNG meegestuurd; server valideert formaat, afmetingen, maximumgrootte en dat het niet leeg is.
7. **"Ondertekenen en goedkeuren"** (POST, antiforgery, `IdempotencyKey` per sessie): server controleert opnieuw — sessie → partij → dossier `Open`, partij niet `Signed`, niet verlopen/geannuleerd, `SigningVerification` met `Status = Verified` **van deze partij**, verifiëring niet ouder dan `SigningSessionMinutes`, akkoord aangevinkt, `Original.Sha256` == herberekende hash van de opgeslagen bytes, bron niet gewijzigd (`HasChangedSinceAsync`). Dan in één transactie: `UPDATE SigningParty SET Status=Signed, SignedAt, … WHERE Id=@id AND Status=Verified AND RowVersion=@rv` (0 rijen = race of dubbel → neutrale "al verwerkt"-pagina), handtekening opslaan, events `ConsentAccepted`, `SignatureImageCaptured`, `PartySigned`. Sessiecookie ongeldig maken.
8. **Regel evalueren**: ALL → wachten op de rest; ANY → dossier voltooid, andere partijen `Revoked` + info-mail; ORDERED → volgende partij uitnodigen. Voltooid → §5.3.
9. **Weigeren**: eigen knop met verplichte reden → partij `Declined`, dossier `Declined`, intern verwittigd, bron ontgrendeld.
10. **Bevestigingspagina** met downloadknop (sessie nog 30 min geldig voor download) en uitleg dat de bevestigingsmail volgt.

### 5.3 Voltooiing (systeem)
Definitieve PDF = origineel + ondertekeningsblad (QuestPDF, `GroupLnPdfDocument`-stijl): "Elektronisch ondertekend via CPM", per ondertekenaar naam, gemaskeerd e-mailadres, hoedanigheid, datum/tijd (Europe/Brussels, met UTC erbij), documentnummer, `PartyVerificationId`, handtekeningafbeelding; onderaan `PublicVerificationId`, SHA-256 van het origineel, QR naar de verificatiepagina (§9.4). Hash van de definitieve PDF → `SigningDocument(Final)`. Auditrapport (QuestPDF): documentfiche, beide hashes, ondertekenaars, methode/kanaal per verificatie, geaccepteerde akkoordtekst (letterlijk), volledige tijdlijn, hash-ketting-status → `SigningDocument(AuditReport)` met eigen hash. Dan `OnCaseCompletedAsync` (ChangeOrder-datums), bevestigingsmail per ondertekenaar (§9.5), interne notificatie, events `CaseCompleted`, `FinalDocumentCreated`, `AuditReportCreated`.

### 5.4 Intern: opvolging
- Op de wijzigingsopdracht: statusblok zoals in de opdracht (per persoon: uitnodiging verzonden / bekeken / geverifieerd / ondertekend / geweigerd), acties **Herinnering**, **Nieuwe link** (ook wanneer de klant meldt dat de link verloren is), **Annuleren** (reden verplicht), **Ondertekende PDF**, **Auditrapport**, **Audit trail controleren**. Downloads door interne gebruikers zijn ook events (`DocumentDownloaded`, ActorType `Internal`).
- Projectbreed: "Ondertekeningen"-item in het inner menu van het projectdossier (gl-v2), lijst over alle documenttypes met filters op status.
- `ChangeOrder.DateAgreement` toont naast de datum "elektronisch ondertekend" met link naar het dossier, versus een handmatig ingevulde datum.

---

## 6. Beveiliging

### 6.1 Tokens
32 bytes uit `RandomNumberGenerator` → base64url (43 tekens). Opslag: `SHA256(token)` (hex). Geen salt of pepper nodig bij 256 bits entropie; wel fixed-time vergelijking en opzoeken op hash-index. Nooit gelogd; in mails alleen in de link. Geldigheid uit beleid; `Download`-tokens 90 dagen (§9.5). Elke tokengebruik is een event (`LinkOpened`) met `IpHash`.

### 6.2 OTP
6 cijfers uit `RandomNumberGenerator` (uniform, geen modulo-bias). Opslag: `HMAC-SHA256(OtpHmacKey, caseId|partyId|verificationId|code)` — **niet** een kale hash: een 6-cijferige code heeft maar 10⁶ mogelijkheden, dus zonder geheime sleutel is een gelekte tabel offline te kraken. De HMAC bindt de code ook aan dossier én partij én verificatiecyclus: een code van A kan nooit bij B geldig zijn. Geldigheid 10 min; 5 pogingen; daarna `Failed` en een nieuwe code nodig; nieuwe aanvraag → vorige `Superseded`; na `Verified` nooit herbruikbaar (statuscheck + de sign-transactie leest de verificatie enkel bij `Verified` en markeert ze `Consumed` in `DataJson`).

### 6.3 SMS-gereed zonder SMS
`SmsOtpMethod.ResolveDestinationAsync` leest het gsm-nummer **op dat moment** uit `ClientContacts.Cellphone`/`ClientAccount` — de ondertekenaar kan er tijdens de procedure geen ander nummer intypen. Traceerbaarheid van wijzigingen: kleine wijzigingslog `ClientContactChangeLog` (contact, veld, oude/nieuwe waarde gemaskeerd, wie, wanneer) die de bestaande klantformulieren vullen bij een wijziging van `Cellphone`/`Email`; het auditrapport toont een waarschuwing wanneer het gebruikte nummer minder dan N dagen vóór de handtekening gewijzigd werd. Dit is de enige wijziging buiten de module zelf.

### 6.4 Rate limiting (nieuw, .NET 8 `AddRateLimiter`)
Partities per **IP** én per **token-/sessie-id**: link openen 30/10 min per IP; OTP aanvragen 3/10 min per partij en 10/10 min per IP; OTP controleren 10/10 min per partij (los van de 5 pogingen per code); ondertekenen 5/10 min per sessie; verificatiepagina 60/10 min per IP. Boven de limiet: dezelfde neutrale pagina (geen 429 met details).

### 6.5 Headers en transport op de publieke pagina's
`Cache-Control: no-store`, `Pragma: no-cache`, `Referrer-Policy: no-referrer`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` (de PDF-iframe is same-origin en krijgt `SAMEORIGIN` op zijn eigen respons), `Content-Security-Policy: default-src 'self'; img-src 'self' data:; frame-src 'self'` — dus **geen CDN's** (geen Phosphor/DataTables/Google Fonts) op deze pagina's; iconen en lettertypes lokaal. HSTS staat al aan buiten Development. Publieke layout `_LayoutPublic.cshtml` in gl-v2-tokens maar zonder rail/topbar/userbox.

### 6.6 Autorisatie en IDOR
Publiek: geen enkel id in URL of formulier; alles volgt uit de sessie. Intern: bestaande `PermissionConventionFilter` + expliciete controle dat het dossier bij een project hoort waar de gebruiker rechten op heeft (via de bron: `Projects.ChangeOrders`). Downloads intern via de app (streaming na hash-controle), nooit via een gedeelde storage-URL.

### 6.7 Concurrency en replay
`RowVersion` op `SigningCase` en `SigningParty`; alle statusovergangen als conditionele `UPDATE`; `IdempotencyKey` per sessie op de sign-POST (tweede submit = zelfde antwoord, geen tweede event); gefilterde unieke index "één open dossier per bron"; voltooiing onder `SERIALIZABLE`-transactie op het dossier zodat twee gelijktijdige laatste handtekeningen (ANY, of ALL met twee tabbladen) precies één keer finaliseren.

### 6.8 Logging en geheimen
`ILogger` krijgt alleen id's en statussen. Een `SigningSecrets`-helper is de enige plek die ruwe tokens/codes aanraakt en heeft geen `ToString`. `OtpHmacKey` uit user-secrets; ontbreken = opstartfout. Geen fallback-waarden (contrast: `ResendInviteUrlBuilder`).

### 6.9 Aanvalsvectoren die niet in de opdracht stonden
- **Referer-lek van het token** via externe assets op de ondertekenpagina → §6.5 + sessiewissel.
- **Link doorgestuurd of mailbox gedeeld** → in v1 komt de OTP in dezelfde mailbox als de link; e-mail-OTP bewijst dus vooral *controle over die mailbox op het ogenblik van ondertekenen*, geen tweede factor. Eerlijk benoemen in het auditrapport ("verificatie: e-mail"); SMS brengt de echte tweede factor. Mitigaties nu: korte OTP-geldigheid, sessiebinding, aparte mails.
- **Documentwissel na aanbieden** → hash-controle bij elke serve én bij ondertekenen; opslag alleen via de module.
- **Downgrade van beleid** (OTP uitzetten terwijl een dossier loopt) → beleidssnapshot in het dossier.
- **Tijdmanipulatie** → alle tijdstippen server-side UTC; hostklok via NTP (operationele vereiste, in DEPLOY.md op te nemen).
- **Enumeratie van verificatie-ID's** → GUID's, rate limit, en de verificatiepagina toont enkel niet-gevoelige velden (§9.4).
- **Interne misbruik** (medewerker die "namens" ondertekent) → interne acties zijn events met `ActorUserId`; ondertekenen kan uitsluitend via een partijsessie; "Nieuwe link" is een event dat in het auditrapport verschijnt.
- **XSS via klantgegevens op de publieke pagina** → Razor-encoding + CSP zonder inline scripts (nonce voor het ene canvas-script).
- **PDF met actieve inhoud** → de bron-PDF's zijn eigen renders; bij toekomstige uploads (bijlagen): type-controle, geen JavaScript-toestaan in de viewer (`sandbox`-iframe).
- **Mailspoofing naar klanten** → SPF/DKIM/DMARC op het verzenddomein zijn een randvoorwaarde (buiten de code).

---

## 7. Gebeurtenistypes (audit trail)
`CaseCreated`, `DocumentStored`, `CaseOpened`, `InvitationSent`, `ReminderSent`, `LinkRegenerated`, `LinkRevoked`, `LinkOpened`, `LinkRejected` (ongeldig/verlopen — enkel `IpHash`, geen token), `PartyOpened`, `DocumentViewed`, `DocumentServed`, `VerificationRequested`, `VerificationMessageAccepted`, `VerificationMessageDelivered`, `VerificationFailedAttempt`, `VerificationLocked`, `VerificationSucceeded`, `ConsentAccepted`, `SignatureImageCaptured`, `PartySigned`, `PartyDeclined`, `CaseCompleted`, `FinalDocumentCreated`, `AuditReportCreated`, `DocumentDownloaded`, `CaseCancelled`, `CaseExpired`, `PartyRevoked`, `ProviderCallbackReceived`, `RetentionScrubApplied`, `AuditChainVerified`.

---

## 8. Achtergrondwerk en retentie
`SigningHostedService` (poller, zoals `TrajectHostedService`): (a) dossiers voorbij `ExpiresAt` → `Expired` + `OnCaseClosedAsync` + info-mail; (b) herinneringen volgens beleid (`ReminderAfterDays`, `ReminderRepeatDays`, max 3) voor partijen die nog niet tekenden; (c) retentiescrub: na `RetentionUntil` worden `Ip`/`UserAgent` genuld (hashes blijven, ketting blijft geldig) en `SignatureImage`-documenten verwijderd; documenten en het rapport blijven zolang het beleid dat zegt; elke scrub is zelf een event. Ook bereikbaar via `/api/trigger/signing`.

---

## 9. Beslissingen die ik eerst aan jou voorleg

1. **Onveranderlijke opslag.** De Storage API biedt (voor zover bekend) geen WORM/versiebeheer. Voorstel: origineel, definitieve PDF en auditrapport **én** in de Storage API (map `signing/`) **én** als `varbinary` in `SigningDocument.Content` (klein: doorgaans 100–500 KB; de SQL-back-up dekt ze dan mee), met hash-controle bij elke serve. Alternatief: enkel Storage API + hash. **Aanbeveling: beide.**
2. **Vergrendeling van de bron.** Zolang een dossier `Open` is, weigert `EditChangeOrder`/`DeleteChangeOrder`/`DuplicateChangeOrder`-naar-zichzelf te bewaren (banner: "Er loopt een ondertekening; annuleer die eerst"). Dat is eenvoudiger en sluitender dan "inhoudelijke wijziging" detecteren via een diff. Plus `HasChangedSinceAsync` als vangnet. **Aanbeveling: vergrendelen.**
3. **Interne tegenondertekening.** Wil Group LN zelf ook tekenen (aannemerscontracten later zeker)? Het model voorziet `PartyType = InternalUser` (ingelogde gebruiker, Entra als verificatie, geen OTP). **Aanbeveling: in het model vanaf v1, UI in een latere fase.**
4. **Publieke verificatiepagina** `/verifieer/{PublicVerificationId}` (+ QR op het blad): toont documenttype, nummer, datum van ondertekening, status en de hash van de definitieve PDF, zodat een ontvanger van een afdruk de echtheid kan nagaan — geen namen, geen bedragen, geen download. **Aanbeveling: ja, minimaal.**
5. **Bevestigingsmail:** ondertekende PDF als bijlage én een downloadlink (Download-token, 90 dagen) voor PDF + auditrapport. **Aanbeveling: beide.**
6. **Token → sessiecookie-redirect** (§5.2 stap 2). Iets meer code, veel minder lekpaden. **Aanbeveling: ja.**
7. **Permissies:** nieuw `Signing` (overzicht + instellingen) naast het bestaande code van de bron voor starten/annuleren. **Aanbeveling: zo.**
8. **PDF-engine van de wijzigingsopdracht.** Rotativa blijft in v1 (de hash is van de bytes op dat moment, dus rendering-verschillen tussen versies zijn geen probleem). Op termijn de ChangeOrder-PDF naar QuestPDF, zoals facturen — dan verdwijnt de wkhtmltopdf-afhankelijkheid uit de bewijsketen. **Aanbeveling: nu niet, wel op de roadmap.**
9. **Standaardondertekenaars bij een wijzigingsopdracht:** klantaccount + alle mede-eigenaars (`IsCoOwner`), regel **ALL**; volmachthouders (`ClientPoa`) niet automatisch, wel handmatig toe te voegen. Bevestigen?
10. **Akkoordtekst en SES-bewoording** laten nakijken door jullie juridisch adviseur vóór de eerste echte verzending; de tekst is beleid (`SigningPolicy.ConsentText`) en komt niet hardgecodeerd in de code.
11. **Wie mag een dossier annuleren** — iedereen met schrijfrecht op de bron, of enkel de aanmaker/admin? **Aanbeveling: schrijfrecht op de bron, altijd met reden, altijd een event.**

Zonder antwoord op 1, 2 en 6 kan ik niet zinvol beginnen; de rest kan tijdens fase 0/1 nog.

---

## 10. Fasering

| Fase | Inhoud | Resultaat |
|---|---|---|
| **0 — Fundament** | `IAssetStorageClient`-refactor; migratie 055 + entiteiten + UoW-repo's; `Signing:*`-config; `ISigningService`, evidence store met trigger + hash-ketting; `InternalSesProvider`, `EmailOtpMethod`, `EmailChannel`; `ISmsProvider`/`SmsChannel`-contracten (leeg); rate limiter + security-header-middleware voor `/ondertekenen/*`; `_LayoutPublic`. | Alles compileert, unit-tests op token/OTP/ketting/regels (ALL/ANY/ORDERED), nog geen UI. |
| **1 — Wijzigingsopdracht** | `ChangeOrderSigningSource` + `IChangeOrderPdfRenderer`; bronvergrendeling; interne UI: modal "Ter ondertekening aanbieden", statusblok, herinnering/nieuwe link/annuleren; projectbreed overzicht (gl-v2). | Een dossier kan aangemaakt, verstuurd en opgevolgd worden. |
| **2 — Ondertekenpagina** | Publieke controller + views (document, verificatie, akkoord, handtekeningpad, bevestiging, weigeren, neutrale foutpagina); sign-transactie; definitieve PDF + ondertekeningsblad; auditrapport; bevestigingsmails. | End-to-end SES-ondertekening. |
| **3 — Opvolging & hardening** | `SigningHostedService` (verlopen, herinneringen, retentiescrub); verificatiepagina + QR; "Audit trail controleren"; `ClientContactChangeLog`; DESIGN.md/DEPLOY.md (NTP, SPF/DKIM, secrets). | Productieklaar. |
| **4 — SMS-gereed** | `SmsOtpMethod`, delivery-status-webhook, eerste `ISmsProvider`-adapter (Bird/Twilio/CM naar keuze) achter een feature flag. | SMS inschakelen = configuratie + één adapter. |
| **later** | Externe providers via `ISignatureMethodProvider` + `SigningWebhookController`; aannemerscontracten via een tweede `ISigningDocumentSource`; interne tegenondertekening-UI; meertaligheid (snapshots per taal). | |

Elke fase eindigt met een werkende build en een DESIGN.md-sectie, zoals bij punt 16.

---

## 11. Wat ik bewust níét voorstel
- Geen eigen crypto: enkel `RandomNumberGenerator`, `SHA256`, `HMACSHA256`, `CryptographicOperations` uit .NET.
- Geen PAdES/digitale certificaten in v1: dat is AES/QES-terrein en zou de "SES"-bewoording tegenspreken; de abstractie laat het later toe via een provider.
- Geen hergebruik van `EmailTemplateBO` voor de OTP-mail (beheerder kan `{Code}` wegknippen).
- Geen JavaScript-bibliotheek voor het handtekeningpad (CSP, geen CDN, ~150 regels eigen code volstaan).
- Geen opslag van het gsm-nummer in het dossier; enkel de gemaskeerde vorm en een verwijzing.
