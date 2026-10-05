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

## ExtendVertical werkt niet voor "body vult de ruimte op, blok hangt boven de voet" (2026-10-05)
Geprobeerd voor de Voorwaarden/Voor-akkoord-sectie (punt 35a: lege ruimte groeit, het blok hangt net
boven de voet in plaats van meteen op de totalen te volgen). Twee varianten getest, geen van beide
werkte: (1) een lege `col.Item().ExtendVertical()`-spacer tussen het lichaam en het blok, (2)
`ExtendVertical()` rechtstreeks op het lichaam zelf. **Bewijs via een tijdelijke rode achtergrond op
de spacer**: die vulde wel degelijk de volledige resterende paginaruimte op — maar daardoor bleef er
nul plaats over voor wat erna komt, en schoof het hele Voorwaarden/Akkoord-blok alsnog naar een
volgende pagina, zelfs op een pagina met duidelijk zichtbare restruimte. Conclusie:
`ExtendVertical()` reserveert geen ruimte voor latere, vastgroottes siblings in dezelfde `Column` —
het claimt altijd ALLES wat rest op de huidige pagina, wat het geschikt maakt als allerlaatste
element (zoals het label onderaan `Handtekeningvak`, dat wél werkt), maar niet om twee dingen op
dezelfde pagina te houden mét opvulling ertussen.

Voorlopig: gewoon een vaste afstand (16pt) na de totalen, geen dynamische opvulling. Verdere opties
als dit toch nodig blijkt: (a) de resterende paginahoogte zelf berekenen (paginaformaat minus
band/kop/voet, per paginatype anders) en daarmee een expliciete `MinHeight` zetten in plaats van
`ExtendVertical` te vertrouwen — precies, maar fragiel bij elke toekomstige maatwijziging; (b)
aanvaarden dat het blok gewoon na de inhoud komt, zonder opvulling — simpel en robuust, wat nu staat.

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
| `GlV2PdfTheme.cs` | Kleuren (`Groen #00532D`, `Inkt`, `Gedempt`, `Licht`, `Lijn`, `VlakGroen`, `VlakGroepsrij`, `VlakWarm`, `AccentGroupLn`) en maten (zijmarge 18mm, band 6mm, logo 16/10mm) — letterlijk punt 36.1/36.2/36.4. |
| `GlV2PdfFonts.cs` | Registreert Playfair Display (enkel gewicht 500, het enige dat het ontwerp gebruikt) en IBM Plex Sans (400/500/600/700) uit `wwwroot/fonts/*.ttf`. Losstaand van `GroupLnFonts` (Avenir, legacy). |
| `GlV2PdfFormat.cs` | Enum `A4Staand`/`A4Liggend`/`A3Staand`/`A3Liggend` → QuestPDF `PageSize`. |
| `GlV2PdfCompanyInfo.cs` + `Configuration/GlV2PdfCompanyOptions.cs` | Bedrijfsgegevens voor kop/voet. Niet de primaire bron (zie hieronder) — enkel het laatste-redmiddel-fallback als er voor een project écht geen facturatiebedrijf te vinden is. Sectie `GlV2PdfCompany` in appsettings; `VatNumber`/`Iban`/`Phone` staan er bewust leeg in (zie "Bedrijfsgegevens" hieronder). |
| `GlV2PdfDocumentBase.cs` | Abstracte `IDocument`-basis: groene band boven/onder, volledige kop (pagina 1) vs. compacte kop (vervolgpagina's, via QuestPDF's `ShowOnce()`/`SkipOnce()`), voet (adres · contact · rechtsvorm+btw+IBAN · "pagina x/y"), `SectionLabel`. |
| `GlV2PdfComponents.cs` | Projectwijde bouwstenen die geen eigen documenttype hebben: `Fiche` (metadata-rooster), `Totalenblok` (78mm, groene eindbalk), `Handtekeningvak` (18mm, één vak, geen bedrijfsvak). |
| `ChangeOrderDocumentV2.cs` | Het eerste echte document: offerte/wijzigingsopdracht, punt 35c/35d. |

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
