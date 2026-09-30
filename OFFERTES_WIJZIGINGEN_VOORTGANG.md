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
