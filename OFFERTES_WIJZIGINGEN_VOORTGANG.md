# Offertes & wijzigingen (gl-v2) — voortgangsstatus

Doorlopend statusdocument voor de "Offertes & wijzigingen"-herwerking (design-handoff 20b/20c/20d) en
de aanpalende fixes op Betalingsschijven/Facturatie uit dezelfde sessie. Plan zoals goedgekeurd staat
in `C:\Users\latni\.claude\plans\smooth-greeting-origami.md` (lokaal op deze pc, reist niet mee via
git — de kern staat hieronder herhaald). **Reist mee via git: commit + push vóór je van machine
wisselt**, anders begint de volgende sessie zonder dit werk.

**Laatste update:** 2026-09-30 — de hele "Offertes & wijzigingen"-flow (20b/20c/20d) is gebouwd en
compileert groen, plus twee bugfixes op bestaande pagina's (PaymentStagesV2-verfijningen en een
InvoicingV2-bug waarbij per-eenheid "bereikt aangeduide" schijven niet in Facturatie verschenen). **Niets
van dit alles is in de browser getest** — dat is het allereerste wat moet gebeuren op de volgende pc.
Azure Document Intelligence-resource is vandaag door Niels aangemaakt in de Azure Portal; de
endpoint/key moeten nog via `dotnet user-secrets` lokaal gezet worden (zie onderaan) vóór 20c se
OCR-knop iets doet.

## 1. Betalingsschijven (PaymentStagesV2) — verfijningsronde, klaar
Bovenop de eerder gebouwde basis (21g/21h): kleinere legende-bolletjes, rechtstreeks op een bol klikken
markeert dat ene lot als bereikt (`.js-ps-mark-one`, naast de bestaande "Bereikt aanduiden"-knop voor
meerdere loten), een bereikt-bol is ook weer uit te zetten zolang niet gefactureerd
(`UnmarkStageReached`, enkel bij een per-eenheid rij, niet bij de groep-brede vlag), groep-header
opnieuw opgebouwd (divider, rechts uitgelijnde "Groep bewerken"-link naar de bestaande legacy
`PaymentStagesAddUpdate`), vaste kolombreedtes op grote schermen, een "+ Betalingsgroep"-knop, de
21h-modal herwerkt (schijfinfo in de modal-header i.p.v. de body, gedeeld `.gl-v2-notice`-component
i.p.v. een eigen `.gl-v2-ps-notice` — **Niels heeft die laatste zelf al aangepast** in
`_ModalMarkStageReachedV2.cshtml`, zie de live-edit-melding tijdens de sessie), een "Bewijs"-foto-
kiezer uit de project-Media (nieuwe kolom `ProofMediaId`, migratie 063), groep-pillen bij meer dan één
groep, en een nieuwe eenheid-kolom-toestand "Akte nog niet verleden" (eenheid gelinkt aan een groep
maar `UnitConstructionValue.ValueSold` nog leeg — toont "—" i.p.v. volledig te verdwijnen zoals
voorheen).

Bestanden: `ProjectenController.PaymentStagesV2.cs`, `PaymentStagesV2.cshtml` +
`Modals/_ModalMarkStageReachedV2.cshtml` + `Modals/_ModalPickProofPhotoV2.cshtml`,
`PaymentStagesV2Vm.cs`, `gl-v2-projecten-paymentstages.css/.js`. Migraties **062**
(`UnitPaymentStageReached`) en **063** (`ProofMediaId`) — **beide al uitgevoerd** door Niels op de
testdb. Testdata: `_migrations/testdata/TESTDATA_Financieel_Part3.sql` (akte-niet-verleden-eenheid +
één al-gefactureerde schijf) — **geschreven maar NIET dry-run gevalideerd** (geen DB-toegang meer in
die sessie) en voor zover bekend nog niet uitgevoerd.

## 2. Facturatie (InvoicingV2) — twee fixes, klaar
- **Rijen gegroepeerd per betalingsschijf**: als een account meerdere eenheden in dezelfde
  betalingsgroep heeft, stonden "Schijf 3" van Lot 1 en Lot 2 voorheen los door elkaar (de controller
  vult rijen per eenheid, dan per schijf). Nu een groepskop per schijf-Id, rij-markup uitgetrokken naar
  `Partials/_InvoicingRowV2.cshtml` (hergebruikt voor zowel de gegroepeerde schijf-rijen als de losse
  WO-rijen erna).
- **Bug gevonden en gefixt**: `ProjectService.GetProjectInvoicableUnits` (gebruikt door zowel de oude
  Facturatie-pagina als impliciet door `MakeInvoices`, dat vertrouwt op de posted StageId zonder eigen
  herverificatie) keek enkel naar de groep-brede `InvoicingPaymentStages.Invoicable`-vlag, nooit naar de
  nieuwere per-eenheid `UnitPaymentStageReached`-tabel. Een via Betalingsschijven "bereikt aangeduide"
  schijf voor één lot kwam daardoor nooit in Facturatie terecht. Fix in twee lagen:
  `DALCore/UnitOfWorkCore.cs` (nieuwe `UnitPaymentStageReached`-repository, ontbrak volledig) en
  `ServiceCore/ProjectService.cs` (dezelfde OR-logica als `PaymentStagesV2.IsReached`). Ook dezelfde fix
  toegepast op `ProjectenController.InvoicingV2.cs` se eigen parallelle query.

Geen nieuwe migratie nodig (pure logicafix). **Nog te testen**: een per-eenheid bereikte schijf moet nu
verschijnen onder "Klaar om te factureren".

## 3. Offertes & wijzigingen (20b/20c/20d) — volledig nieuw gebouwd
Twee scopebeslissingen van Niels die de hele bouw sturen:
- **Een offerte heeft géén eigen tabel** — het is een `ChangeOrder`-rij in een vroege fase
  (`IsQuote = true`), "Omzetten" zet 'm in-place om (`IsQuote = false` + `QuoteConvertedAt`), geen
  kopieerstap, geen apart brondocument-record.
- **20c ("Offerte inlezen") is het volledige interactieve OCR-hulpmiddel**, niet een vereenvoudigde
  upload-vorm — kader rond een tabel leest regels in via Azure Document Intelligence, kader rond een
  foto hangt die aan een regel.

### 3.1 Schema (migratie 064, **al uitgevoerd** door Niels)
`_migrations/064_ChangeOrderOffertesEnFacturatieplan.sql`: `ChangeOrder` krijgt `IsQuote`,
`QuoteSupplierReference`, `QuoteVatPercentage`, `QuoteConvertedAt`; `ChangeOrderDetail` krijgt
`NeedsReview` (de goud "controleer"-vlag) en `SourceImagePath` (bijgesneden foto uit 20c); nieuwe
tabel `ChangeOrderPaymentTerm` (het facturatieplan: voorschot/tussentijds/saldo, trigger-type, optionele
schijf-koppeling); `InvoicesDetails` krijgt `ChangeOrderPaymentTermId` (nieuwe `LineType=
'ChangeOrderTerm'`, naast de bestaande lump-sum `'ChangeOrders'` — backwards compatible). Entiteiten:
nieuw `ChangeOrderPaymentTerm.cs`, uitbreidingen op `ChangeOrder.cs`/`ChangeOrderDetail.cs`/
`InvoicesDetails.cs`, alles geregistreerd in `cpmRunningContext.cs`.

### 3.2 Statusmodel — `ServiceCore/Helpers/ChangeOrderStatusHelper.cs`
Eén functie, afgeleid (niet opgeslagen) uit bestaande velden + `SigningCase`-status +
termijn-facturatiestatus: **Offerte → Verlopen → Opgemaakt → Verzonden → Geweigerd → Geannuleerd →
Ondertekend → Factureerbaar → Gefactureerd → Betaald**. Gebruikt door zowel 20b (funnelbalk/rijbadge)
als 20d (stepper) — zelfde bron, geen twee schermen die uit sync kunnen raken.
**Bekende beperking**: "Factureerbaar"/"Gefactureerd"/"Betaald" checken vandaag enkel of er
`ChangeOrderPaymentTerm`-rijen bestaan en of ze een `InvoicesDetails`-rij hebben — de trigger-check zelf
("is de schijf waaraan deze termijn hangt effectief bereikt?") en de betaald-check (`Invoices.StatusId`)
zijn nog niet geïmplementeerd (`TODO stap 5` in de code). Voor een net aangemaakte offerte/WO zonder
termijnen is dit geen probleem; het wordt relevant zodra iemand een facturatieplan invult én een schijf
bereikt.

### 3.3 20d — Wijzigingsopdracht (`ChangeOrderDetailV2`)
`ProjectenController.ChangeOrderDetailV2.cs` (GET + `ChangeOrderDetailV2Save`/`-Convert`/`-AddRow`/
`-AddTerm`), `ChangeOrderDetailV2Vm.cs`, `Views/Projecten/ChangeOrderDetailV2.cshtml` +
`Partials/_ChangeOrderDetailRowV2.cshtml` + `_ChangeOrderTermRowV2.cshtml`,
`gl-v2-projecten-changeorderdetail.css/.js`. Bevat: 7-staps stepper, Opdracht-kaart (leverancier/
contract-dropdown hergebruikt van de legacy pagina, btw-klant vergrendeld/afgeleid uit de
betalingsgroep, "Te factureren door bouwheer" — bleek al te bestaan als `ChangeOrder.Invoiceable`, geen
nieuw veld), Regels-kaart (prijs-leverancier/commissie/prijs-klant-berekening 1-op-1 overgenomen uit de
werkende legacy `EditChangeOrder.cshtml`-JS), Facturatieplan-kaart (4 snelkeuzes, termijntabel,
%-validator), rechterkolom Ondertekening/Bron/Historiek (Ondertekening linkt bewust naar de bestaande
`SigningAdmin/Dossier`-acties i.p.v. die knoppen een tweede keer te bouwen).

### 3.4 20b — Offertes & wijzigingen (`ChangeOrdersV2`, de hub-lijst)
`ProjectenController.ChangeOrdersV2.cs`, `ChangeOrdersV2Vm.cs`, `Views/Projecten/ChangeOrdersV2.cshtml`
+ `Partials/_ChangeOrdersRowV2.cshtml`, `gl-v2-projecten-changeorders.css/.js`. Tabs (Alles/Offertes/
WO's), 6-staps funnelbalk, zoekveld + open/alles-status-filter (client-side), twee tabellen
(Offertes/Wijzigingsopdrachten) met 6-bolletjes-verloopstrip per rij, "Omzetten →" op offerte-rijen,
"Herinneren →" linkt naar `SigningAdmin/Dossier` (niet rechtstreeks `Remind` aangeroepen — die actie
heeft een specifieke `partyId` nodig die de lijst niet kent). Projectmenu-item "Wijzigingsopdrachten"
hernoemd naar "Offertes & wijzigingen" en wijst nu hierheen (`_ProjectInnerMenuV2.cshtml`); de oude
`/Projecten/DetailsChangeOrder` blijft ongewijzigd bereikbaar.

### 3.5 20c — Offerte inlezen (`QuoteIntakeV2`, het OCR-hulpmiddel)
`ProjectenController.QuoteIntakeV2.cs`, `QuoteIntakeV2Vm.cs`, `Views/Projecten/QuoteIntakeV2.cshtml` +
`Partials/_QuoteIntakeRowV2.cshtml`, `gl-v2-projecten-quoteintake.css/.js`. Nieuw:
`CPMCore/Services/QuoteExtraction/` (`IQuoteRegionAnalysisService`/`AzureQuoteAnalysisService`, model
`"prebuilt-layout"` — herbruikt dezelfde `InvoiceExtractionOptions`/Azure-resource als de bestaande
`AzureInvoiceAnalysisService`, geregistreerd in `Program.cs`). pdf.js gevendord in
`wwwroot/lib/pdfjs/pdf.min.js` + `pdf.worker.min.js` (versie 3.11.174, checksums geverifieerd tegen
cdnjs) — er bestond nog geen PDF-rendering/canvas-crop-tool in de codebase.

Werking: canvas laadt een PDF-pagina (pdf.js) of foto/plakresultaat; twee sleep-kader-modi ("tabel" →
stuurt de bijgesneden regio naar `QuoteIntakeV2ExtractTable`, Azure prebuilt-layout, vult regels in;
"foto" → stuurt naar `QuoteIntakeV2AttachPhoto`, slaat op via de bestaande `DocStorageService`, laat je
via een popover kiezen aan welke regel de foto hangt). Onzekere OCR-regels krijgen `NeedsReview=true`
("controleer"-knop, klikken bevestigt). "Opslaan als offerte"/"Omzetten naar wijzigingsopdracht" posten
bewust naar dezelfde `ChangeOrderDetailV2Save`/`-Convert`-acties als 20d (nieuw veld
`ConvertAfterSave` op het save-model bespaart een aparte round-trip als je vanuit 20c meteen omzet
zonder eerst afzonderlijk op te slaan).

**Bewuste scope-uitsnede, niet stilzwijgend weggelaten**: het originele geüploade bestand (de PDF/foto
zelf) wordt NIET automatisch gearchiveerd in Documentencentrum als "Offerte leverancier" — enkel de
bijgesneden tabel-/foto-regio's gaan naar de server. Dat archiveringsstuk is een aparte, in zichzelf
kleine vervolgstap als gewenst.

**Kolomtoewijzing van de OCR is een heuristiek**, geen semantische AI-herkenning (`prebuilt-layout`
geeft enkel rij/kolom-cellen terug, geen "dit is de prijskolom"): laatste numerieke kolom = prijs, kolom
ervoor = aantal, een cel die op een gekende eenheid lijkt (m²/m³/lm/st/sog) = eenheid. Rijen waar dit
niet overtuigend lukt krijgen `NeedsReview=true`.

## 4. Azure Document Intelligence — configuratiestatus
Resource vandaag aangemaakt in de Azure Portal (regio West Europe werd geweigerd wegens
capaciteitsbeperking op het abonnement — regio gewijzigd, daarna gelukt). **Endpoint/key nog NIET
lokaal geconfigureerd** voor zover bekend. Nodig vóór 20c se "Gebied selecteren — tabel" iets doet
(foto-aan-regel-hangen werkt sowieso al, dat gebruikt Azure niet):

```powershell
cd E:\TFS\CPMCore
dotnet user-secrets set "InvoiceExtraction:Azure:Endpoint" "https://<resource-naam>.cognitiveservices.azure.com/"
dotnet user-secrets set "InvoiceExtraction:Azure:ApiKey" "<Sleutel 1 uit de Azure Portal>"
```

**Niet in `appsettings.Development.json` zetten** — dat bestand zit onder git-beheer, user-secrets niet.
Gratis tier (F0) volstaat voor testen: 500 pagina's/maand, ~2 aanvragen/minuut.

## 5. Openstaande punten voor de volgende sessie
1. **Browser-testen, in deze volgorde** (niets van onderstaande is ooit in een browser bekeken):
   - PaymentStagesV2: bewijs-fotokiezer, groep-pillen, un-mark, akte-niet-verleden-kolom.
   - InvoicingV2: de per-eenheid-bereikt-bugfix (schijf verschijnt nu wel), de schijf-groepering.
   - 20d (`ChangeOrderDetailV2`) los, op een bestaand testdata-WO.
   - 20b (`ChangeOrdersV2`): funnelbalk-tellingen, tabs, filters, "Omzetten →".
   - 20c (`QuoteIntakeV2`): PDF/foto laden, kader-tabel (na de Azure-config hierboven), kader-foto,
     "controleer"-flag, Opslaan/Omzetten.
2. **Stap 5 van het plan — Facturatie-integratie**: `MakeInvoicesCO`/`BuildChangeOrderInvoiceDraft`
   (`ProjectenController.cs:8406`/`:9199`) en `InvoicingV2.cs` moeten nog per-facturatieplan-termijn
   factureren i.p.v. de hele WO ineens (zodat "WO-003 · voorschot 30%" als eigen regel in Facturatie
   verschijnt, zoals 20a in de wireframe toont). Nog niet aangeraakt.
3. **Migratie 064 dry-run-status**: draaide al bij Niels (bevestigd), maar testdata Part3 (062/063-kant)
   nog niet bevestigd uitgevoerd — nakijken.
4. Optioneel, niet gevraagd: Documentencentrum-archivering van het 20c-bronbestand (zie 3.5 hierboven).

## Bestandenoverzicht (nieuw deze sessie)
```
_migrations/063_UnitPaymentStageReachedProofMedia.sql
_migrations/064_ChangeOrderOffertesEnFacturatieplan.sql
_migrations/testdata/TESTDATA_Financieel_Part3.sql

DALCore/UnitOfWorkCore.cs (gewijzigd)
DALCore/Models/ChangeOrderPaymentTerm.cs (nieuw)
DALCore/Models/ChangeOrder.cs, ChangeOrderDetail.cs, InvoicesDetails.cs, cpmRunningContext.cs (gewijzigd)

ServiceCore/ProjectService.cs (gewijzigd — GetProjectInvoicableUnits-bugfix)
ServiceCore/Helpers/ChangeOrderStatusHelper.cs (nieuw)

CPMCore/Program.cs (gewijzigd — AzureQuoteAnalysisService geregistreerd)
CPMCore/Services/QuoteExtraction/IQuoteRegionAnalysisService.cs, AzureQuoteAnalysisService.cs (nieuw)
CPMCore/Controllers/ProjectenController.ChangeOrderDetailV2.cs, ChangeOrdersV2.cs, QuoteIntakeV2.cs (nieuw)
CPMCore/Controllers/ProjectenController.InvoicingV2.cs (gewijzigd — bugfix)
CPMCore/Controllers/ProjectenController.PaymentStagesV2.cs (gewijzigd — verfijningen)
CPMCore/Models/Projecten/ChangeOrderDetailV2Vm.cs, ChangeOrdersV2Vm.cs, QuoteIntakeV2Vm.cs (nieuw)
CPMCore/Models/Projecten/PaymentStagesV2Vm.cs (gewijzigd)
CPMCore/Views/Projecten/ChangeOrderDetailV2.cshtml, ChangeOrdersV2.cshtml, QuoteIntakeV2.cshtml (nieuw)
CPMCore/Views/Projecten/Partials/_ChangeOrderDetailRowV2.cshtml, _ChangeOrderTermRowV2.cshtml,
  _ChangeOrdersRowV2.cshtml, _QuoteIntakeRowV2.cshtml, _InvoicingRowV2.cshtml (nieuw)
CPMCore/Views/Projecten/PaymentStagesV2.cshtml, InvoicingV2.cshtml (gewijzigd)
CPMCore/Views/Projecten/Modals/_ModalPickProofPhotoV2.cshtml (nieuw)
CPMCore/Views/Shared/GlV2/_ProjectInnerMenuV2.cshtml (gewijzigd — menu-item hernoemd/omgeleid)
CPMCore/wwwroot/css/gl-v2-projecten-changeorderdetail.css, gl-v2-projecten-changeorders.css,
  gl-v2-projecten-quoteintake.css (nieuw)
CPMCore/wwwroot/css/gl-v2-projecten-paymentstages.css, gl-v2-projecten-invoicing.css (gewijzigd)
CPMCore/wwwroot/js/gl-v2-projecten-changeorderdetail.js, gl-v2-projecten-changeorders.js,
  gl-v2-projecten-quoteintake.js (nieuw)
CPMCore/wwwroot/js/gl-v2-projecten-paymentstages.js, gl-v2-shell.js (gewijzigd)
CPMCore/wwwroot/lib/pdfjs/pdf.min.js, pdf.worker.min.js (nieuw, gevendord)
```

---

## 6. Flow volgens design-handoff punt 28/29 (gebouwd 02/10/2026) — NIET browser-getest

Punt 29 = de flow vanaf de lijst, punt 28 (28a–28i) = één scherm per stap. Alles hieronder compileert
(`dotnet build` 0 fouten) en de stijl is nagekeken op een statische testpagina, maar er is niets tegen de
databank of in de echte app getest.

**Eerst uitvoeren: `_migrations/066_ChangeOrderBronEnVersie.sql`** (4 nullable kolommen op `ChangeOrder`:
`QuoteSourcePath`, `QuoteSourceFileName`, `SourceChangeOrderId`, `SourceKind`). Zonder 066 faalt elke
ChangeOrder-query (ook de oude schermen). Migratie 065 moet ook gedraaid zijn.

### 6.1 Flow (29a/29b)
- **Lijst (`ChangeOrdersV2`)**: één knop "+ Nieuw" (gedeeld `.gl-v2-menu`) met Offerte inlezen · Leeg beginnen ·
  Kopie van een wijzigingsopdracht · Offerte zonder omzetten. Offerte-rij opent 20c, "Omzetten →" opent de
  21c-modal; WO-rij opent het scherm per stap; "···" op een WO-rij: Openen · Kopie maken · PDF.
- **21c Omzetten** (`ConvertQuoteModalV2` + `ChangeOrderConvertV2`, modal in `Modals/_ModalConvertQuoteV2`):
  klant · eenheid, commissie, btw (vergrendeld, uit de betalingsgroep), facturatieplan-snelkeuze, omschrijving,
  voorwaarden; "Aanmaken en openen" / "Aanmaken en verzenden". Vanuit 20c gaat dezelfde modal mee in de
  Save-POST (opslaan + omzetten in één keer). Omzetten blijft in-place (zelfde rij, OF-012 → WO-012).
- **Kopie** (`CopyChangeOrderModalV2` + `ChangeOrderCopyV2`): kies WO + eenheid → nieuwe WO als concept,
  regels en plan mee, `SourceChangeOrderId` = bron, `SourceKind` = 1. **Versie 2** = zelfde actie met
  `asVersion=true` (`SourceKind` = 2): trekt een lopende ondertekening in en maakt een nieuwe WO-rij.
- **20c**: bewaart nu ook het originele bestand (`QuoteIntakeV2UploadSource`, map "quotes"); `intent=keep` =
  "Offerte zonder omzetten"; op een bestaande WO (vanuit Bron) koppelt het een offerte zonder omschrijving/
  plan/commissie te overschrijven.

### 6.2 Scherm per stap (`ChangeOrderDetailV2`, 28a–28i)
`BuildScreenState` (ProjectenController.ChangeOrderDetailV2.cs) leidt `Model.Phase` af:
concept (28a met offerte / 28b zonder) · verzonden (28c) · wacht (28d) · ondertekend/factureerbaar (28e) ·
gefactureerd (28f) · betaald (28g) · geweigerd (28h) · telaat (28i), plus ingetrokken en verlopen (terug
bewerkbaar). Gedeelde statusfeiten voor lijst én detail: `ProjectenController.ChangeOrderFlowV2.cs`
(`LoadChangeOrderFlowFactsAsync` + `ComputeFlowStatus`) — termijn-triggers (na ondertekening / schijf bereikt /
manueel vrijgegeven), facturen per termijn, betaald/vervallen.
Acties op het scherm: `ChangeOrderDetailV2Remind`, `-Withdraw`, `-UploadSigned`, `-ReleaseTerm`, `-SetStep`
(enkel als elektronisch ondertekenen uitstaat: verzonden/akkoord met de hand zetten).

### 6.3 Bewuste afwijkingen van het ontwerp
- Een offerte heeft nog altijd een klant nodig om bewaard te worden (`ChangeOrder.ClientAccountId` is NOT NULL;
  nullable maken is geen additieve wijziging en raakt de oude schermen). Bij omzetten kies/wijzig je de klant
  in 21c.
- 21d (verzendmodal) is niet apart gebouwd: "Verzenden" gaat naar het bestaande startscherm van de
  ondertekenmodule (`SigningAdmin/Start`).
- "+ Uit artikellijst" en de sleepgreep op regels ontbreken (geen artikellijst, geen volgordekolom).
- Versie 2 krijgt een nieuw WO-nummer (nieuwe rij) i.p.v. "WO-006 v2".
- "Afsluiten" bij geweigerd is weggelaten (geweigerd is al een eindstatus); factuurherinnering/aanmaning/
  betaling registreren linken naar de factuur (lopen via Facturatie, zoals 28i zelf zegt).

### 6.4 Nog open
- **Per-termijn factureren** (stap 5 van het oorspronkelijke plan): "Voorschot factureren" linkt naar
  Facturatie, maar `MakeInvoicesCO`/`BuildChangeOrderInvoiceDraft`/`InvoicingV2` factureren een WO nog in één
  keer. Het scherm toont termijnfacturen correct zodra er `InvoicesDetails`-rijen met
  `ChangeOrderPaymentTermId` bestaan.
- Het bewaarde bronbestand wordt in 20c niet terug in de viewer geladen (enkel als link/voorbeeld op 28a).
- Opgeloste bugs onderweg: rijen na een verwijderde rij gingen verloren bij opslaan (gat in `rows[i]`), de
  bouwheer-schakelaar postte "on" i.p.v. "true", en de "controleer"-vlag was op 20d niet weg te klikken.

### 6.5 Aanvulling 02/10/2026 (na feedback Niels) — NIET browser-getest

**Eerst uitvoeren: `_migrations/067_ChangeOrderDetailSortOrder.sql`** (`ChangeOrderDetail.SortOrder INT NULL`)
en **`_migrations/068_SigningCaseInvitationMessage.sql`** (`SigningCase.InvitationMessage`), naast 066.

- **Regelvolgorde (28a/28b)**: sleepgreep op elke regel (muis/vinger, of focus + pijl omhoog/omlaag). De
  volgorde van het formulier wordt bewaard in `SortOrder` (SyncRows); het opmaakscherm, 20c, de kopie en de
  PDF (`ChangeOrderPdfBuilder`) volgen ze. NULL = nooit herschikt → aanmaakvolgorde (Id).
- **21d Verzenden ter ondertekening**: nu een modal op het scherm zelf (`SendChangeOrderModalV2` +
  `ChangeOrderSendV2`, `Modals/_ModalSendChangeOrderV2`). "Verzenden naar klant" (28a) en "Aanmaken en
  verzenden" (21c) slaan eerst op en openen dan de modal (`?send=true`). Kanaal "Online ondertekenen" maakt
  en opent het ondertekendossier (zelfde twee stappen als `SigningAdmin/Start`), "Alleen PDF" zet enkel de
  verzenddatum. Ontvangers aan/uit, Alle eigenaars / Eén volstaat, vervaldatum, voorbeeld van de PDF.
  **BERICHT** (toegevoegd op vraag van Niels, migratie `068_SigningCaseInvitationMessage.sql`): vrij bericht
  van de afzender, bewaard op het dossier (`SigningCase.InvitationMessage`), doorgegeven via
  `CreateSigningCaseRequest.InvitationMessage` en door `SigningNotifier` na de aanhef gezet in de
  uitnodigingsmail en in elke herinnering (HTML-gecodeerd, regeleinden behouden). Het oude startscherm
  `SigningAdmin/Start` heeft het veld nog niet.
  Afwijkingen van het ontwerp, omdat de ondertekenmodule het niet kan: geen apart kanaal "Klantenportaal",
  HERINNERING is een beleidsinstelling (getoond, niet aanpasbaar), geen eigendomspercentages bij de
  ontvangers.
- Bekende rand: na een ingetrokken online ondertekening opnieuw verzenden als "Alleen PDF" laat de status op
  "Ingetrokken" staan (de status volgt het recentste dossier).

**Volgende stap: per termijn factureren.** Het facturatieplan (voorschot/tussentijds/saldo) wordt bewaard en
getoond, maar Facturatie (`InvoicingV2` → `MakeInvoicesCO`/`BuildChangeOrderInvoiceDraft`) factureert een
getekende WO nog in één keer voor het volledige bedrag. Te bouwen: in Facturatie één regel per
factureerbare termijn ("WO-006 · voorschot 30 %"), de factuurregel koppelen via
`InvoicesDetails.ChangeOrderPaymentTermId` (LineType 'ChangeOrderTerm'), WO's zonder plan blijven in één keer.

### 6.6 Tussenstap "offerte aan de klant" (02/10/2026, beslissing Niels) — NIET browser-getest

**Eerst uitvoeren: `_migrations/069_ChangeOrderDetailSourceDetail.sql`** (`ChangeOrderDetail.SourceDetailId INT NULL`),
naast 066/067/068.

Nieuwe flow: leveranciersofferte inlezen (20c) → **offerte aan de klant opmaken** → per mail verzenden →
**omzetten** (21c) naar een NIEUWE wijzigingsopdracht → verzenden ter ondertekening (21d).
- Een offerte aan de klant is nog altijd een `ChangeOrder` met `IsQuote=1`, maar "Omzetten" is niet meer in-place:
  het maakt een nieuwe WO-rij (`SourceKind=3`, `SourceChangeOrderId` = offerte) en zet `QuoteConvertedAt` op de
  offerte, die ongewijzigd blijft bestaan (status "Omgezet"). Oudere rijen die nog in-place omgezet werden
  (IsQuote=0 mét QuoteConvertedAt) blijven gewoon werken.
- **Zelfde scherm** (`ChangeOrderDetailV2`) voor offerte en WO. Offerte: `?quote=true` voor een nieuwe, geen
  facturatieplan, kaart "Verzending" i.p.v. "Ondertekening", fase `offerte` → `offerte-verzonden` →
  `offerte-omgezet` (`BuildQuoteScreenState`). Regels volledig vrij; "Geldig tot" instelbaar.
- **Offerte per mail** (`SendQuoteModalV2` + `ChangeOrderQuoteSendV2`): één mail per eigenaar met eigen aanhef, vrij
  bericht en de offerte-PDF als bijlage; testmodus (`Signing:TestRecipientOverride`) geldt ook hier. Daarna ligt
  de offerte vast; "Aanpassen" zet ze terug naar concept, "Opnieuw mailen" mailt opnieuw.
- **PDF**: `ChangeOrderDocument` kent nu `IsQuote` (titel "Offerte", nummer OF-…, geen handtekeningblok).
- **21c** vraagt enkel nog facturatieplan, omschrijving en voorwaarden; klant, btw en bedragen zijn uitlezing.
  In de nieuwe WO liggen de prijzen van de overgenomen regels vast (`SourceDetailId` gevuld: prijs, commissie,
  btw, meetmethode en eenheid niet aanpasbaar — ook niet via een geknoeid formulier, de server negeert het);
  aantal en omschrijving wel, regels mogen weg, nieuwe regels hebben een vrije prijs.
- **Lijst**: "+ Nieuw" = Offerte inlezen · Offerte opmaken · Wijzigingsopdracht zonder offerte · Kopie · Offerte
  inlezen en bewaren. Offerte-rijen openen het offertescherm, een omgezette offerte linkt naar haar WO.
- **20c** eindigt nu in "Opslaan en offerte opmaken" (of terug naar de lijst); het omzetten zit er niet meer in.
  Voorlopige omschrijving van een ingelezen offerte: "Leveranciersofferte …" (verdwijnt zodra je de omschrijving
  voor de klant invult; verzenden/omzetten blokkeert zolang ze er nog staat).
