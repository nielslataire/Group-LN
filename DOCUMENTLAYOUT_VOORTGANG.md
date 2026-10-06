# PDF-documentlayout (gl-v2) — voortgangsstatus

Doorlopend statusdocument voor de projectwijde QuestPDF-documentlayout (Niels, okt. 2026, op basis
van design-handoff punt 35 "Documentlayout" en punt 36 "Opmaakspecificaties", `CRM
Documentlayout.dc.html`). Zie ook DESIGN.md "PDF-documenten (gl-v2)" voor de korte, projectwijde
samenvatting; dit bestand is het volledige logboek: wat er klaar is, wat bewust afwijkt van het
ontwerp, en wat nog moet gebeuren.

**Laatste update:** 2026-10-05 — offerte (35d) werkt, is visueel vergeleken met het echte
design-handoff-voorbeeld en zit qua kop/voet/metadata-blok/voorwaarden-akkoord dicht tegen dat
voorbeeld aan. **Scope is nu bewust vernauwd tot enkel de offerte** (zie "Scope vanaf nu"); de
wijzigingsopdracht-variant (`IsQuote == false`) compileert en rendert nog mee omdat het dezelfde
klasse is, maar wordt niet meer actief getest/gevolgd tot de offerte helemaal af is. Nog niet
gecommit — zie "Verdergaan op een andere pc" hieronder vóór je begint.

## Verdergaan op een andere pc
1. `git status`/`git diff` — alles hieronder staat als niet-gecommitte wijziging in de working tree
   (nieuwe `CPMCore/Documents/GlV2/` map, `CPMCore/Configuration/GlV2PdfCompanyOptions.cs`, 5
   font-bestanden in `CPMCore/wwwroot/fonts/`, wijzigingen aan `ChangeOrderDocument.cs` (model,
   enkel nieuwe velden), `ChangeOrderPdfBuilder.cs`, `ProjectenController.ChangeOrders.cs`,
   `Program.cs`). `appsettings.json` (sectie `GlV2PdfCompany`) is gitignored — op een andere pc
   moet die sectie daar opnieuw manueel bij (zie "Bedrijfsgegevens" verderop; lege strings volstaan,
   de echte facturatiebedrijf-gegevens komen uit de database, niet uit deze sectie).
2. **Het testprogramma (`scratchpad/PdfSmokeTest/`) bestaat enkel op déze machine** — de scratchpad
   is sessie-/machinegebonden en zit niet in git. Op een andere pc eerst opnieuw aanmaken: de
   volledige inhoud staat onderaan dit bestand ("Smoke-test opnieuw aanmaken"), gewoon kopiëren.
3. Bouw-wisselwerking met Visual Studio: als VS het project open heeft (debuggen of gewoon
   geladen), houdt het de `bin/`-DLL's vast en faalt `dotnet build`/`dotnet run` met
   MSB3021/MSB3027. Geen code-fout — even wachten tot VS vrijgeeft, of vragen of het mag stoppen.

## Visueel testen — de werkwijze
Playwright is voor browsers/HTML, niet voor PDF's. In plaats daarvan: QuestPDF kan een document
zelf naar PNG exporteren (`document.GenerateImages(new ImageGenerationSettings { RasterDpi = … })`)
— dat is letterlijk dezelfde rasterisatie als het afdrukken, dus de meest directe test die er is.
**Val wel terug op het .pdf-bestand zelf (gelezen met de Read-tool) voor de uiteindelijke controle**:
de losse PNG-export heeft een **transparante achtergrond** (geen witte pagina getekend), wat er in
een viewer met een donkere achtergrond uitziet als een zwarte pagina met spookachtige tekst — geen
echte bug, gewoon een eigenschap van losse PNG's. Het testprogramma in `scratchpad/PdfSmokeTest/`
schrijft nu zowel het `.pdf`-bestand als een `_p1.png`/`_p2.png`/… per pagina; de PNG's zijn snel om
te doorbladeren, het `.pdf`-bestand is de betrouwbare eindcontrole.

## Scope vanaf nu: enkel de offerte (Niels, 2026-10-05)
De wijzigingsopdracht (`IsQuote == false`) deelt vandaag dezelfde `ChangeOrderDocumentV2`-klasse en
dezelfde opmaak als de offerte, via `_m.IsQuote` (titel/kicker-tekst). Niels wil voorlopig enkel de
offerte afwerken — "deze ziet er anders uit" (de wijzigingsopdracht heeft volgens het ontwerp een
ander opbouw dan de offerte, o.a. het facturatieplan-blok en een handtekenvak per mede-eigenaar uit
punt 35c). Tot de offerte volledig afgewerkt is, geen tijd steken in de wijzigingsopdracht-variant;
de bestaande wijzigingsopdracht-smoke-test blijft bestaan maar wordt niet actief gevolgd. Wanneer de
wijzigingsopdracht aan de beurt is, wordt dit waarschijnlijk een eigen `Kicker`/`Content`-pad binnen
dezelfde klasse, of een eigen klasse — ter plekke te beslissen.

## "Body vult de ruimte op, blok hangt boven de voet" — niet gelukt, drie pogingen (2026-10-05)
Doel (punt 35a): lege ruimte onderaan groeit mee, Voorwaarden/Voor-akkoord hangt altijd net boven de
voet i.p.v. meteen op de totalen te volgen. Drie onafhankelijke technieken geprobeerd, **geen enkele
werkte in deze QuestPDF-versie** (pakket `QuestPDF 2025.7.1`, zie `CPMCore.csproj`):

1. Lege `col.Item().ExtendVertical()`-spacer tussen lichaam en blok.
2. `ExtendVertical()` rechtstreeks op het lichaam (`Body`) zelf.
3. `col.Item().Extend().AlignBottom().ShowEntire().Element(VoorwaardenEnAkkoord)` — een extern
   aangereikte aanpak (Extend() op het láátste item zelf, niet op iets ervóór), die in theorie net
   dit probleem (geen ruimte reserveren voor wat erna komt) zou omzeilen.

**Bewijs, niet enkel theorie**: bij elke poging een tijdelijke felroze achtergrond (`Background(
"#FFCCCC")`) gezet op het element dat zogezegd zou uitrekken, dan gerenderd en bekeken.
- Pogingen 1/2: de roze vlek vulde wél degelijk de volledige resterende paginaruimte — maar net
  daardoor bleef er nul plaats over voor wat erna komt, en schoof het hele Voorwaarden/Akkoord-blok
  alsnog in zijn geheel naar de volgende pagina, zelfs op een pagina met duidelijk zichtbare
  restruimte.
- Poging 3: de roze vlek bleef strak rond de natuurlijke inhoud (geen uitrekking te zien), zowel met
  de achtergrond binnenin `VoorwaardenEnAkkoord` als — om een `.Element()`-grenseffect uit te
  sluiten — met de achtergrond vóór `.Element(...)` in dezelfde keten. Extend() had hier dus
  zichtbaar geen enkel effect.

Conclusie: dit specifieke "groeiend lichaam + vastgrootte-blok op dezelfde pagina"-patroon lukt niet
betrouwbaar in deze omgeving met de geprobeerde QuestPDF-aanroepen. Mogelijk een andere combinatie
werkt wel (bv. een expliciete `MinHeight` berekend uit het paginaformaat in plaats van op
Extend()/ExtendVertical() te vertrouwen), maar dat vergt per paginatype (volledige vs. compacte kop)
een aparte berekening — niet geprobeerd, risico op evenveel fragiliteit voor weinig visuele winst.

**Vierde poging — WERKT (2026-10-06)**: `Extend().AlignBottom().ShowEntire()` op het column-item dat
de volledige Row (Voorwaarden + Voor akkoord) bevat, met `AlignBottom()` op het Voorwaarden-item binnen
de Row, en 4mm `PaddingBottom` op de Row zodat het blok de voetlijn niet raakt. Visueel gecontroleerd
op de korte offerte (1 pagina) en de lange (4 pagina's, blok onderaan de laatste pagina). Het verschil
met poging 3: Extend() stond daar op een item mét een ander binnenwerk; nu staat de keten op het item
dat de Row omvat. De vorige fallback (vaste 16pt afstand) is vervangen. Niet getest: een lichaam dat
precies tegen de paginagrens zit (blok moet dan in zijn geheel naar de volgende pagina schuiven).

## Tabel, voorwaarden en algemene regels (2026-10-06)
- **Standaard-typetabel** = `GlV2PdfComponents.Tabel` (+ `TabelKolom`, `TabelCel`), algemeen voor alle
  gl-v2-PDF's: kop zonder vlak met groene lijn 0,5mm, rijen 8pt/1,7mm/lijn 0,2mm, kolomafstand 3mm,
  btw-tag als pil (6 % `VlakGroepsrij`, andere `VlakWarm`), `ShowEntire` per cel (rij splitst nooit),
  kop herhaalt per pagina. Offerte gebruikt kolommen Omschrijving · Eenh. 12 · Hoev. 14 · Eenheidspr. 22 ·
  Btw 12 · Totaal 24 mm. **Weggelaten t.o.v. 35d**: kolom Code (Niels: overbodig) en de kolom Type (staat
  niet in 35d). **Voor later**: groepsrijen (code + titel, vlak `#EEF4EA`) en subtotaalrijen — extra rijtype
  in `Tabel`. Btw-tag toont voorlopig het ene document-btw-tarief voor alle regels (geen btw per regel in het model).
- **Voorwaarden/Voor akkoord**: bovenaan naast elkaar uitgelijnd, blok onderaan via
  `Extend().AlignBottom().ShowEntire()` (zie vierde poging hierboven). Voorwaarden-tekst =
  `ChangeOrder.ChangeOrderConditions` (veld "Voorwaarden" op ChangeOrderDetailV2, standaardtekst
  `DefaultChangeOrderConditions`). De vaste vetgedrukte "Deze offerte is geldig tot…"-zin is verwijderd
  (enkel offerte; die van de wijzigingsopdracht staat er nog).
- **Onderwerp + inleiding (offerte, 35d)**: nieuw veld `ChangeOrder.Subject` (migratie `072_ChangeOrderSubject.sql`,
  NVARCHAR(150), **vóór gebruik uitvoeren — EF selecteert de kolom, zonder migratie falen alle ChangeOrder-queries**).
  Tekstvak "Onderwerp" op ChangeOrderDetailV2 boven "Omschrijving voor de klant"; wordt meegenomen bij
  omzetten offerte→WO en bij kopiëren. PDF (offerte): onderwerp 9pt 600 Inkt + inleiding (= Omschrijving voor
  de klant) 8pt/1,5 Gedempt, 1,5mm ertussen; leeg veld = regel weggelaten. WO houdt het oude "Opdracht"-blok.
- **Btw**: per regel `ChangeOrderDetail.VatPercentage` (anders `QuoteVatPercentage`), totaalblok toont één Btw-rij
  per tarief (`ChangeOrderPdfModel.VatBreakdown`); legacy-document blijft de projectinstelling gebruiken.
- **Afsluitrij totalen/btw (35c + 35i)**: `GlV2PdfComponents.TotalenBtw` (verving `Totalenblok`): links BTW-VERMELDING
  per tarief = de factuurvermelding van het facturatiebedrijf (`Vattype.InvoiceMention` van het bedrijf uit
  `ResolveIssuerCompanyAsync`, op `BasePercentage` = tarief van de regels; niets ingesteld = geen regel, geen vaste fallbacktekst),
  rechts 92mm: één tarief = "Totaal excl. btw" + "Btw x %" (35i), meerdere = tabel TARIEF/MAATSTAF/BTW/TOTAAL +
  Totaal-rij (35c), beide met de groene balk "Totaal incl. btw". Visueel gecontroleerd voor beide varianten.
- **Standaardvoorwaarden offerte**: `ChangeOrderStandardTexts.QuoteConditions(vervaldatum)` = "Deze offerte is geldig
  tot dd/MM/yyyy. Indien u akkoord bent … ondertekenen." Prefill in het scherm voor een nieuwe/lege offerte, bij Opslaan
  wordt een nog-standaard of lege tekst opnieuw opgemaakt met de actuele vervaldatum (een eigen aangepaste tekst blijft
  staan), de PDF valt voor een lege offertevoorwaarde terug op dezelfde tekst. **Omzetten offerte → WO neemt de
  offertevoorwaarden niet mee**: de WO krijgt de WO-standaardtekst (`DefaultChangeOrderConditions`).
- **Btw-codes per regel (migratie 073, `ChangeOrderDetail.VatTypeId`)**: op ChangeOrderDetailV2 kies je per regel een btw-code
  (Vattype) van het facturatiebedrijf (`ChangeOrderIssuerResolver`); standaard de code van de betalingsgroep van de klant
  (`InvoicingPaymentGroup.VatTypeId`, via `ResolveVatForClientAsync`), de klantkeuze zet alle regels mee om. Het percentage
  (`VatPercentage`) wordt server-side uit de code afgeleid. Heeft het bedrijf geen btw-codes, dan blijft het oude %-veld.
  PDF-vermelding komt van de gekozen code (`InvoiceMention`), zonder code van alle codes met hetzelfde %. Kopiëren/omzetten
  nemen de code mee (kopie naar andere klant: code van die klant). Niet in de browser getest.
- **Wijzigingsopdracht (35i, 2026-10-06)**: zelfde opbouw als de offerte (titel "Wijzigingsopdracht", onderwerp + inleiding,
  tabel, totalen/btw), plus **FACTURATIEPLAN** (`ChangeOrderPdfModel.Terms`: label + trigger, %, bedrag **incl.** btw — excl. bewust
  niet, voetnoot "bedragen incl. btw") en een eigen onderblok `VoorwaardenEnAkkoordWijziging`: eventuele VOORWAARDEN, dan VOOR AKKOORD met
  de tekst "Door te ondertekenen … terugsturen." (+ " Iedere vermelde eigenaar dient te ondertekenen." bij >1 eigenaar en
  regel Alle/Volgorde) en een handtekenvak per eigenaar (naam + "eigenaar · x %"; 2 per rij). Eigenaars = klantenaccount + mede-eigenaars
  (zelfde dedupe op e-mail als `SuggestPartiesAsync`); regel = `ClientAccount.DefaultSigningRule` ?? `SigningPolicy` (ChangeOrder) ?? Alle.
  Regel "één volstaat" of geen eigenaars = één vak zonder naam/percentage. De oude zin "Gelieve … terug te bezorgen tegen …" is weg.
  Het ondertekendossier (`ChangeOrderSigningSource`) rendert sinds 2026-10-06 ook de gl-v2-PDF (nieuwe dossiers; Niels akkoord). Voorwaarden blijven boven VOOR AKKOORD staan (Niels akkoord).
- **Ondertekeningsblad (35j, 2026-10-06)**: `SigningEvidenceDocumentV2` (gl-v2, kicker "BIJLAGE · PAGINA n", intro, Document/Bedrag/Status,
  blok per eigenaar, Echtheid controleren + QR). `SignedDocumentComposer` gebruikt het voor `ChangeOrder`-dossiers (gegevens via
  `ChangeOrderPdfBuilder.LoadAsync` + `BuildGlV2Company`, paginatelling van het origineel via PdfSharpCore, voet toont "n / n" via
  `PageLabelOverride`); bij een fout of ander documenttype valt het terug op het oude `SigningEvidenceDocument`. Tijdstippen in Belgische tijd.
  SHA-256 = origineel zoals ondertekend, verificatie-ID = eerste 23 tekens van de GUID. Enkel visueel getest met testdata, niet via een echt dossier.
- **Bedragen**: nl-BE-standaard (`€ -640,00`) bewust behouden (Niels, 2026-10-06), niet het "– € 640,00" van het ontwerp.
- **Algemene regels punt 35/36 altijd mee lezen** (Niels, 2026-10-06). Nog niet afgedekt voor de offerte:
  btw-overzicht + wettelijke vermelding ("zoals factuur", 35d), IBAN in groepen van 4, "6 %" met spatie (✓ in de tag).

## Verfijning na vergelijking met het 35d-voorbeeld (2026-10-05)
Niels deelde een screenshot van de echte 35d-pagina uit het design-handoff-canvas; rechtstreeks
ernaast gelegd met de eigen render leverde deze correcties op:
- **Kop**: enkel het logo (geen bedrijfsnaam ernaast), over een grotere hoogte dan het ontwerp's
  eigen 16mm-plaatshouder (nu 28mm — `GlV2PdfTheme.KopHoogteMm`) zodat een echt logobeeld met
  interne witruimte toch duidelijk leesbaar is. Kicker "Meerwerk" boven de titel, voor zowel
  offerte als wijzigingsopdracht.
- **Afstand kop/voet tot de groene banden**: de kop-inhoud startte op 4mm onder de bovenste band;
  het voorbeeld en punt 36.4's eigen tabel zeggen beide 10mm — gecorrigeerd. De voet had geen
  ruimte tussen de tekst en de onderste band; er staat nu een vaste 4mm-tussenruimte (punt 35a
  "voet... 3mm boven/4mm onder").
- **Voet, twee regels per kolom**: "tel · mail" op regel 1, website op regel 2 (kolom contact);
  "rechtsvorm · btw" op regel 1, IBAN op regel 2 (kolom rechtsvorm/btw/IBAN) — eerder stond dit elk
  als één samengevoegde regel, het voorbeeld splitst ze.
- **Metadata-blok volledig herbouwd**: was een omkaderd rooster met 3 vakken (Project/Klant/
  Referentie, hergebruikt van het legacy-document); is nu `GlV2PdfComponents.MetaAdres` — exact
  de 35d-indeling: links een ongekaderd label/waarde-rooster (Datum, **Geldig tot** in groen/vet,
  Werfadres, Referentie), rechts een 72mm adresblok (klantnaam vet, eenheid eronder). Geen rand,
  geen sectielabel. Bewuste afwijking: het voorbeeld toont er het eigen postadres van de klant;
  `ChangeOrderPdfModel` draagt dat niet (enkel het projectadres), dus de eenheid (`UnitsLine`)
  staat er in de plaats, als het enige wél-beschikbare identificerende gegeven.
- **RPR-vermelding** (bv. "RPR Gent") in de voet: bewust niet toegevoegd — staat nergens in
  `IssuerCompany` en Niels bevestigde dat dit mag wegblijven.
- **Gecodeerde/gegroepeerde tabel** (postnummer + "01 Afbraak"-secties, 35d): Niels wil dit later
  alsnog toevoegen aan offertes. Blijft een openstaand punt — vergt een uitbreiding van
  `ChangeOrderDetail` (code + sectie per regel), geen ontwerpvraag. Zie "Bekende afwijkingen" onder.

**Nevenwerking om in de gaten te houden**: de extra kop-/voetruimte (+6mm boven, +4mm onder) en het
grotere logo duwen een kort document net over de paginagrens — de offerte-smoke-test (4 regels)
die eerst op 1 pagina paste, spreidt het "voor akkoord"-vak nu over een eigen 2e pagina. Dit is het
rechtstreekse gevolg van de net gevraagde grotere afstanden, niet van iets anders; gemeld, niet
zelf teruggedraaid.

## Uitgangspunten (van Niels)
- Alle documenten in QuestPDF, volgens de opmaak uit punt 35/36.
- Eerst de globale, projectwijde layout-bouwstenen; dan pas, document per document, invullen.
- **Enkel voor gl-v2** (`ViewData["UseGlV2Layout"]`): wie niet in gl-v2 zit, blijft exact de
  bestaande PDF zien — niets aan de legacy-opmaak verandert.
- **LayoutB (en LayoutA/HE) niet aanraken**: dat is de JSON-gebaseerde factuur-opmaak in
  `ServiceCore/Invoicing/Pdf/` (`DefaultLayouts.cs`, per `IssuerCompanyBO.TemplateKey`
  instelbaar) — een volledig aparte, al bestaande pipeline voor de échte, verstuurde facturen van
  zowel BCO als Group LN. De gl-v2-documentlayout hieronder is een nieuw, apart systeem
  (`CPMCore/Documents/GlV2/`) voor de andere documenten (offerte/WO, en later klantenlijst,
  aannemerslijst, prijslijst, budget). Factuur-opmaak in de LayoutA/B/HE-stijl trekken is een
  aparte, latere beslissing — niet iets wat dit systeem vervangt of raakt.
- Begonnen met offerte/wijzigingsopdracht (ChangeOrderDetailV2); de rest volgt pas als dit
  document goed zit.

## Architectuur: `CPMCore/Documents/GlV2/`
Alles hier is nieuw en onafhankelijk van `GroupLnPdfDocument`/`ChangeOrderDocument` (die blijven
100% ongewijzigd — de legacy opmaak) en van `ServiceCore/Invoicing/Pdf/` (LayoutA/B/HE).

| Bestand | Inhoud |
|---|---|
| `GlV2PdfTheme.cs` | Kleuren (`Groen #00532D`, `Inkt`, `Gedempt`, `Licht`, `Lijn`, `VlakGroen`, `VlakGroepsrij`, `VlakWarm`, `AccentGroupLn`) en maten (zijmarge 18mm, band 6mm, logo volledige kop 28mm — groter dan het ontwerp's 16mm, zie "Verfijning" — compacte kop 10mm) — letterlijk punt 36.1/36.2/36.4, logo-maat bewust aangepast. |
| `GlV2PdfFonts.cs` | Registreert Playfair Display (enkel gewicht 500, het enige dat het ontwerp gebruikt) en IBM Plex Sans (400/500/600/700) uit `wwwroot/fonts/*.ttf`. Losstaand van `GroupLnFonts` (Avenir, legacy). |
| `GlV2PdfFormat.cs` | Enum `A4Staand`/`A4Liggend`/`A3Staand`/`A3Liggend` → QuestPDF `PageSize`. |
| `GlV2PdfCompanyInfo.cs` + `Configuration/GlV2PdfCompanyOptions.cs` | Bedrijfsgegevens voor kop/voet. Niet de primaire bron (zie hieronder) — enkel het laatste-redmiddel-fallback als er voor een project écht geen facturatiebedrijf te vinden is. Sectie `GlV2PdfCompany` in appsettings; `VatNumber`/`Iban`/`Phone` staan er bewust leeg in (zie "Bedrijfsgegevens" hieronder). |
| `GlV2PdfDocumentBase.cs` | Abstracte `IDocument`-basis: groene band boven/onder, 10mm kop-inzet onder de band, volledige kop (pagina 1, enkel logo + titel/kicker/nummer) vs. compacte kop (vervolgpagina's, via QuestPDF's `ShowOnce()`/`SkipOnce()`), voet (adres · contact · rechtsvorm+btw, IBAN op een eigen regel rechts uitgelijnd · enkel "x / y" zonder "pagina"-tekst), `SectionLabel`. |
| `GlV2PdfComponents.cs` | Projectwijde bouwstenen die geen eigen documenttype hebben: `MetaAdres` (label/waarde-rooster links + 72mm adresblok rechts, géén rand/titel — verving de eerdere `Fiche`, zie "Verfijning"), `Totalenblok` (78mm, groene eindbalk), `Handtekeningvak` (18mm, leeg vak, label onderaan, geen vooringevulde naam). |
| `ChangeOrderDocumentV2.cs` | Het eerste echte document: offerte (35d). Wijzigingsopdracht (`IsQuote == false`) rendert nog mee via dezelfde klasse maar wordt niet actief getest (zie "Scope vanaf nu"). |

**Nog te bouwen** (volgen bij het volgende document): btw-overzicht + betaalblok (factuur, 35b),
projectfiche-rooster voor lijsten (35e/35f), kerncijfer-tegels (35g/35h), verbruiksbalk (35h, met
een rode variant >100%).

## Hoe het document gekozen wordt
`ProjectenController.ChangeOrders.ChangeOrderPDF` (actie achter `/Projecten/ChangeOrderPDF`, door
élke pagina gebruikt — legacy én V2 linken naar dezelfde URL) roept
`ChangeOrderPdfBuilder.Render(model, useGlV2Layout: ViewData["UseGlV2Layout"] as bool? == true)`.
`UseGlV2Layout` komt van `BaseController.OnActionExecuting` (de `gl_v2_preview`-cookie) — exact
dezelfde schakelaar die overal al **V2.cshtml* vs. *.cshtml* kiest. Zo bepaalt de sessie van de
kijker, niet de pagina waarvandaan geklikt werd, welke opmaak er komt; wie niet in gl-v2 zit krijgt
altijd de legacy-PDF, ook als die toevallig via een V2-achtige link kwam.

`ChangeOrderSigningSource` (fase 2, het digitale ondertekendossier) roept `Render` zonder
`useGlV2Layout` en krijgt dus altijd de legacy-opmaak — bewust: dat draait buiten een HTTP-context
(achtergrondjob), waar geen sessie/cookie bestaat om op te beslissen. Als de nieuwe opmaak ooit ook
voor het ondertekendossier moet gelden, is dat een aparte beslissing (zie "Open punten").

## Bedrijfsgegevens: altijd van het échte facturatiebedrijf, nooit verzonnen
Eerste versie gebruikte een lege `appsettings`-sectie als bron. **Niels wees erop dat dit moet
komen van het echte facturatiebedrijf van het project** — dat bestaat al
(`IssuerCompanyBO`/`IssuerCompanyService`, dezelfde tabel als de factuur-pipeline). Nu:

`ChangeOrderPdfBuilder.ResolveIssuerCompanyAsync` past **exact dezelfde regel** toe als
`ProjectenController.Coordinatie.EnsureSupplierIssuerLink` (zie het doc-comment daar — dat is de
oorspronkelijke, al bestaande business-regel, hier enkel hergebruikt):

1. `Project.IssuerCompanyIdBuilder` — het facturatiebedrijf-aannemingen (bv. BCO). **Dit is de
   hoofdregel: een offerte/wijzigingsopdracht voor meerwerken komt altijd van dit bedrijf.**
2. Is dit een coördinatieproject (`IsCoordinationProject || IsOnlyCoordinationProject`) en is 1
   leeg, dan `Project.CoordinationIssuerCompanyId` (het coördinatiebedrijf).
3. Is ook dat leeg, dan het als `IsExternalCoordinationDefault` gemarkeerde facturatiebedrijf
   ("Coördinatie", Id 7 in de testdb).
4. Levert geen van de drie iets op (zou niet mogen voorkomen — `EnsureSupplierIssuerLink` logt dit
   ook als warning), dan pas de `GlV2PdfCompanyOptions`-fallback uit appsettings, zodat de PDF
   tenminste nog gegenereerd wordt in plaats van te crashen.

Getest met echte testdb-data (project 64 "Verkaveling Ketenhoekstraat", `IssuerCompanyIdBuilder=4`
→ BCO: naam, logo, adres, btw BE0464670778, IBAN uit `IssuerBankAccount` komen allemaal uit de
database, niets hardcoded of verzonnen).

## Open punt (Niels, 2026-10-05) — nog niet verwerkt
Niels wees op een aanvullende regel die nog niet in de code/het formulier zit:
> "offerte voor meerwerken moeten ook altijd van het facturatiebedrijf aannemingen komen of indien
> er geen facturatiebedrijf is omdat het een coördinatieproject is dan vanuit het
> coördinatiebedrijf maar dan wel geen optie om te factureren door bouwheer aangezien we zelf geen
> bouwheer zijn dan en misschien een vermelding op de wijzigingsopdracht/offerte."

Het **bedrijf dat op de PDF staat** is hierboven al correct opgelost (de resolutieregel dekt beide
takken). Twee dingen staan nog open, buiten de scope van vandaag (ze raken `ChangeOrderDetailV2`,
niet de PDF-layout zelf) en wachten op een gerichte beslissing vóór ze gebouwd worden:

1. **`InvoiceableByBouwheer`** (`ChangeOrder.Invoiceable`, in te stellen op
   `ChangeOrderDetailV2`/`ChangeOrderDetailV2Vm.cs:44`): voor een coördinatieproject kan deze optie
   ("factureren via het facturatieplan van de bouwheer") eigenlijk nooit `true` zijn — de
   coördinatiebedrijf/Group LN is in dat geval zelf niet de bouwheer. Vandaag staat er geen enkele
   beperking op; `vm.InvoiceableByBouwheer = co?.Invoiceable ?? true` staat open voor élk project.
2. **Een vermelding op het document zelf** wanneer het facturatiebedrijf het coördinatiebedrijf is
   (tak 2/3 hierboven) — Niels noemde dit zelf "misschien", dus geen vaste tekst afgesproken.

Vraag aan Niels vóór bouwen: moet (1) de optie onzichtbaar/uitgeschakeld worden op
`ChangeOrderDetailV2` wanneer het project coördinatie is, of enkel een waarschuwing tonen? En voor
(2): welke tekst, en enkel zichtbaar wanneer `Invoiceable == true` én het bedrijf uit tak 2/3 komt
(een mogelijk tegenstrijdige combinatie), of in alle coördinatiegevallen?

## Getest
Build groen. Geen live omgeving met inlog-flow beschikbaar voor een HTTP-smoke-test (de actie zit
achter `[Authorize]`); in plaats daarvan een losstaand testprogramma
(`scratchpad/PdfSmokeTest/`, niet in de repo) dat `ChangeOrderDocumentV2` rechtstreeks aanroept met
een model gebouwd uit echte testdb-waarden (project 64, facturatiebedrijf BCO). Drie gevallen
gecontroleerd, alle drie zonder QuestPDF-layoutfouten:
- Korte offerte (4 regels, 1 pagina).
- Lange wijzigingsopdracht (35 regels, 4 pagina's) — bevestigt: volledige kop enkel op pagina 1,
  compacte kop + groene lijn op vervolgpagina's, tabelkop herhaalt per pagina, het
  "voor akkoord"-vak splitst niet en schuift in zijn geheel naar de volgende pagina als het niet
  meer past.
- Lege regels ("Geen regels.").

Eén bug gevonden en gefixt tijdens het testen: de IBAN in de voet brak af over twee regels
(kolom te smal). Voetkolommen kregen relatieve breedtes (0.8/1.0/1.3 i.p.v. gelijk) en de IBAN
krijgt onderling niet-brekende spaties, zodat hij nooit meer middenin breekt.

**Nog te doen vóór commit/gebruik in productie**: een echte browsertest (inloggen, `gl_v2_preview`-
cookie aanzetten, de PDF-knop op `ChangeOrderDetailV2`/`ChangeOrderSignV2` gebruiken) — de
losstaande test dekt enkel de QuestPDF-compositie, niet de volledige HTTP/DI-keten
(`IIssuerCompanyService`-registratie, cookie-detectie, bestandsnaam/`Content-Disposition`).

## Bewuste afwijkingen van het ontwerp (met reden)
- **Offerte-tabel heeft geen postnummer/groepscodes** (punt 35d vraagt "14mm minmax(0,1fr) 12mm
  14mm 22mm 24mm" met code vooraan en gegroepeerde secties zoals "01 Afbraak"). `ChangeOrderDetail`
  draagt geen budget-activiteitcode of sectiegroepering — dat zou een nieuw datamodel vergen, geen
  layoutwerk. Offerte en wijzigingsopdracht delen daarom dezelfde kolomopbouw (35c): Omschrijving/
  Eenheid/Type/Hoev./EH-prijs/Totaal. **Niels wil dit later alsnog toevoegen aan offertes** (2026-10-05) —
  blijft open tot `ChangeOrderDetail` een code + sectie per regel krijgt.
- **Eén handtekenvak, niet per mede-eigenaar**: 35c vraagt een vak van 18mm per eigenaar. Het model
  kent enkel één `ClientName` (geen lijst mede-eigenaars) — dat hoort bij de Klanten-module, niet
  bij `ChangeOrderPdfModel`. Eén vak voor de hoofdklant, zoals 35d dat al voor offerte voorschrijft.
- **Geen RPR-vermelding** in de voet (bv. "RPR Gent", 35d): staat nergens in `IssuerCompany`;
  bevestigd door Niels dat dit mag wegblijven in plaats van verzonnen te worden.
- **Voorwaarden/Voor akkoord herbouwd als twee kolommen** (2026-10-05, Niels), letterlijk zoals
  35d: Voorwaarden links (gewone tekst, geen vak), "Voor akkoord" rechts — géén groene SectionLabel-
  stijl voor "Voor akkoord" zelf (600 7.5pt Inkt, niet-hoofdletters), met "naam, datum en
  handtekening" als instructie erboven. `Handtekeningvak` toont niet langer de klantnaam vooraf
  ingevuld in het vak — enkel het lege vak met "handtekening · datum" onderaan, zoals het voorbeeld
  (het vak is voor de klant om in te vullen, geen bevestigingstekst).
- **Geen "vetgedrukte slotregel"** bij de omschrijving (punt 36.5 "Beschrijving"-bouwsteen): de
  omschrijving is vrije, door de gebruiker ingevoerde rijke tekst — een vaste laatste regel vet
  maken op tekst die ik niet structureel ken (en soms leeg/midden-zin eindigt) zou net zo goed
  verkeerd kunnen ogen. Bewust weggelaten in plaats van te fabriceren.
- **Logo**: hergebruikt het bestaande PNG-logobestand (of het `LogoBytes` van het facturatiebedrijf
  uit de database) via `.Image().FitWidth()`, geen SVG zoals punt 36.8 adviseert — er is geen
  SVG-asset, en een PNG op 16mm/10mm oogt op schermresolutie prima.

## Patroon voor het volgende document (zelfde aanpak als STRUCTUREREN_VOORTGANG.md)
1. Welk ontwerp-subpunt (35b t/m 35h) en welk paginaformaat.
2. Welke bouwstenen uit `GlV2PdfComponents` al bestaan, welke nieuw moeten (bv. btw-overzicht,
   betaalblok voor de factuur).
3. Nieuwe `GlV2PdfXxxDocument : GlV2PdfDocumentBase`, hergebruik het bestaande datamodel van het
   legacy-document waar mogelijk (zoals `ChangeOrderPdfModel` hier) — geen nieuwe query's bouwen
   die al bestaan.
4. Gated op `useGlV2Layout`/`ViewData["UseGlV2Layout"]`, legacy-pad ongewijzigd laten.
5. Smoke-test met echte of realistische data vóór commit.
6. Dit bestand bijwerken; nieuwe afwijkingen van het ontwerp hier documenteren, niet stilzwijgend
   laten passeren.

## Smoke-test opnieuw aanmaken (op een andere pc of nieuwe sessie)
Niet in git (bewust — eenmalig hulpmiddel, geen productiecode). Twee bestanden in een nieuwe map,
bv. in de scratchpad-map van de sessie of gewoon ergens lokaal buiten de repo:

**`PdfSmokeTest.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="VOLLEDIG_PAD_NAAR\CPMCore\CPMCore.csproj" />
  </ItemGroup>
</Project>
```
Pas `VOLLEDIG_PAD_NAAR` aan naar waar de repo op die machine staat
(bv. `C:\Users\niels\source\repos\nielslataire\Group-LN\CPMCore\CPMCore.csproj`).

**`Program.cs`**
```csharp
using CPMCore.Documents;
using CPMCore.Documents.GlV2;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var fontsRoot = @"VOLLEDIG_PAD_NAAR\CPMCore\wwwroot\fonts";
foreach (var f in new[] { "PlayfairDisplay-Medium.ttf", "IBMPlexSans-Regular.ttf", "IBMPlexSans-Medium.ttf", "IBMPlexSans-SemiBold.ttf", "IBMPlexSans-Bold.ttf" })
{
    using var s = File.OpenRead(Path.Combine(fontsRoot, f));
    QuestPDF.Drawing.FontManager.RegisterFont(s);
}

var logoBco = File.ReadAllBytes(@"VOLLEDIG_PAD_NAAR\CPMCore\wwwroot\Img\groupln-logo.png");

ChangeOrderPdfModel BuildModel(bool isQuote, int lineCount) => new()
{
    Id = 33,
    PublicNumber = isQuote ? "OF-2026-014-v2" : null,
    ProjectId = 64,
    ClientAccountId = 174,
    IsQuote = isQuote,
    Date = DateOnly.FromDateTime(DateTime.Today),
    ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
    ProjectName = "Verkaveling Ketenhoekstraat",
    ProjectAddressLine = "Ketenhoekstraat 12",
    ProjectCityLine = "9000 Gent",
    ProjectMunicipality = "Gent",
    ClientSalutation = "Mevrouw",
    ClientName = "Peeters Marie",
    ClientEmail = "marie.peeters@example.be",
    UnitsLine = "Woning Lot 3",
    ClientStreetLine = "Kerkstraat 45",
    ClientCityLine = "8630 Veurne",
    Description = "Aanpassing van de keukeninrichting en toevoegen van een extra stopcontact in de woonkamer, zoals besproken tijdens het werfoverleg van vorige week. Dit omvat ook het verplaatsen van de spot boven het kookeiland.",
    CommentHtml = "<p>Uitvoering voorzien in fase 2.</p><ul><li>Elektriciteit eerst</li><li>Dan afwerking</li></ul>",
    Conditions = "Prijzen geldig tot de vervaldatum. Meerwerken worden gefactureerd volgens het facturatieplan van het project.",
    VatPercentage = 21m,
    Lines = Enumerable.Range(1, lineCount).Select(i => new ChangeOrderPdfLine
    {
        Description = $"Regel {i} — omschrijving van de werken die uitgevoerd worden inclusief materiaal en plaatsing",
        UnitLabel = "m²",
        TypeLabel = "Meerwerk",
        Number = 3,
        Price = 125.50m,
        CommissionPercentage = 15m,
    }).ToList(),
    IssuerCompanyId = 4,
    IssuerCompanyName = "BCO",
    IssuerCompanyLegalLine = "BCO",
    IssuerCompanyVatNumber = "BE0464670778",
    IssuerCompanyIban = "BE68 0015 1882 9434",
    IssuerCompanyStreet = "Klaverdries 53",
    IssuerCompanyPostalCity = "9031 Drongen",
    IssuerCompanyPhone = "09/216.49.50",
    IssuerCompanyEmail = "info@bouwenconstructie.be",
    IssuerCompanyWebsite = "www.bouwenconstructie.be",
    IssuerCompanyLogoBytes = logoBco,
};

var company = new GlV2PdfCompanyInfo
{
    Name = "BCO",
    Tagline = "a part of Group LN",
    Street = "Klaverdries 53",
    PostalCity = "9031 Drongen",
    Phone = "09/216.49.50",
    Email = "info@bouwenconstructie.be",
    Website = "www.bouwenconstructie.be",
    LegalForm = "BCO",
    VatNumber = "BE0464670778",
    Iban = "BE68 0015 1882 9434",
    LogoBytes = logoBco,
};

var outDir = Path.Combine(AppContext.BaseDirectory, "out");
Directory.CreateDirectory(outDir);

void Render(string name, bool isQuote, int lineCount)
{
    try
    {
        var model = BuildModel(isQuote, lineCount);
        var doc = new ChangeOrderDocumentV2(model, company, fontsAvailable: true);
        var bytes = doc.GeneratePdf();
        var path = Path.Combine(outDir, name + ".pdf");
        File.WriteAllBytes(path, bytes);
        var images = doc.GenerateImages(new ImageGenerationSettings { RasterDpi = 220 }).ToList();
        for (var i = 0; i < images.Count; i++)
            File.WriteAllBytes(Path.Combine(outDir, $"{name}_p{i + 1}.png"), images[i]);
        Console.WriteLine($"OK  {name}: {bytes.Length} bytes, {images.Count} pagina('s) -> {path}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FOUT {name}: {ex}");
    }
}

Render("offerte_kort", isQuote: true, lineCount: 4);
Render("offerte_leeg", isQuote: true, lineCount: 0);
// Render("wijzigingsopdracht_lang", isQuote: false, lineCount: 35); // niet meer actief gevolgd, zie "Scope vanaf nu"

Console.WriteLine("Klaar.");
```

Draaien: `dotnet run` in die map. Resultaat: `.pdf` + `_p1.png`/`_p2.png`/… per document in een
`out/`-submap naast de build-output. **Lees het `.pdf`-bestand** (met de Read-tool of een PDF-viewer)
voor de echte controle — de losse PNG's hebben een transparante achtergrond (zie "Visueel testen").
