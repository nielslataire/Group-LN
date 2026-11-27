# Plan verder: planning, traject en betalingsschijven (stand 27/11/2026)

Doel van dit document: één plek met alles wat nog open staat, in een volgorde waarin we kunnen werken. Het is samengesteld uit alle secties van `PLANNING_VOORTGANG.md` (dat bestand blijft het logboek; dit is de to-dolijst). Werkwijze: per punt een korte branch vanaf `master`, testen, samenvoegen. Een punt is pas "klaar" als de code getest is, de help bijgewerkt is en Niels het in de browser bevestigd heeft.

Legenda: **Wie** = wie het volgende doet (C = Claude bouwt, N = Niels beslist of test, CN = beide). **Staat**: nieuw / gebouwd, niet getest / wacht op Niels.

## Fase A. Nu bezig (branch `planning-gevel-mails-voorbeeld`)

| # | Punt | Staat | Wie | Klaar als |
|---|---|---|---|---|
| A1 | Gevelafwerking als projectkeuze (pleister, hout, aluminium; steen altijd eerst, daarna pleister, daarna hout of aluminium) | Gebouwd 27/11, 189 tests groen, migratie 170 gedraaid op de testdatabase (sjablonen Appartementsgebouw en Woning, activiteiten 216/212/213), niet in de browser | N: migratie `170_Planningssjabloon_Gevelafwerkingen.sql` draaien en testen (zie hieronder) | Niels kiest in het toepassen-venster meerdere afwerkingen en ziet de juiste volgorde in de planning |
| A2 | Mails naar Boekhouder en bestuurders, en duidelijke inhoud van alle trigger-mails en -taken | Gebouwd 27/11, gecommit (74ab6a59, c2d49e99, a018ee4d), 10 tests groen. Ontvangers volgen de gebruikersrollen: Boekhouding, Bestuurder en Admin; Projectleider en Verkoper blijven de persoon op het project; Extern niets automatisch. Mail met "Wat moet je doen?" en knop naar het traject; ongeveer 100 standaardteksten per mijlpaalcode | N: de teksten nalezen en een testtrigger laten afgaan met `_migrations/testdata/TEST_trigger_rolmail.sql`; nog te bekijken of de triggereditor het veld `omschrijving` toont | Een testtrigger verstuurt een duidelijke mail naar de juiste gebruikers |
| A3 | Voorbeeld in het toepassen-venster verbeteren: per groep tonen wat ongemoeid blijft en waarom; "Eén planning voor het gebouw" als standaard bij een appartementsgebouw; "Woningen per planning" enkel bij woningen; verschil 13/14 schijven uitleggen | Nieuw | C (N bevestigt de standaard) | Het voorbeeld toont per betalingsgroep wat er gebeurt, en bij een gebouw staat "Eén planning" voorgeselecteerd |

**A1 testen:** migratie 170 draaien, projectsjabloon "Appartementsgebouw" toepassen op een testproject (Kelder, Dak, gevelkeuzes) en controleren dat bij elke gekozen afwerking een taak in Gevel en buitenschrijnwerk staat. Pleister wacht op het gevelmetselwerk; hout en aluminium wachten op metselwerk en pleister (bij een niet gekozen pleister alleen op het metselwerk). De opkuis wacht op alle gekozen afwerkingen. Aannames die Niels mag corrigeren: hout en aluminium lopen parallel (geen volgorde tussen elkaar), duren 20/15/15 werkdagen, activiteit gekoppeld (pleister 216, aluminium 213, hout 212). Het sjabloon "Woning" (verkaveling) krijgt dezelfde drie taken (besluit Niels 27/11): migratie 170 vult beide sjablonen.

## Fase B. Appartementsgebouw afmaken

| # | Punt | Staat | Wie |
|---|---|---|---|
| B1 | Lijst "verkeerde zaken" van het planningssjabloon Appartementsgebouw (duren, volgorde, parallelle taken) verwerken | Niels zei 27/11 "1 done": bevestigen of de lijst al verwerkt is, anders opnieuw aanleveren | N dan C |
| B2 | Afwerking deels parallel (nu 103 werkdagen in serie) | Wacht op B1 | CN |
| B3 | Aparte grondschijf voor appartementen (grondvennootschap, zoals bij verkaveling) in de schijvensjabloon en de projectsjablonen | Nieuw, besluit staat in de sectie "verkaveling met woningen" | C |
| B4 | Stap C parking/berging: traject-mijlpalen per eenheid (nu 250, meeste van parkings/bergingen) en planning | Schijven 30/40/30 zijn klaar (27/11, eenheden nog te koppelen aan de groep, zie onder); traject en planning wachten op keuzes | N dan C |
| B5 | Project 81: de 32 parkings/bergingen koppelen aan de groep "Parkings en bergingen" | Gebruikersactie op pagina Betalingsschijven > Groep bewerken | N |

## Fase C. Bestaande planning: controleren wat nooit afgevinkt werd

| # | Punt | Staat | Wie |
|---|---|---|---|
| C1 | Planning in de echte app uitproberen: rechten, taken, voorgangers, weerverlet, deadline, klantwaarschuwing | Nooit systematisch gedaan | N (C maakt een testlijst) |
| C2 | Controleren of de feedback van 16/11 gebouwd is: estafette per taak kiezen en voorgangers uit een ander bouwdeel zichtbaar; opvallende foutmelding (`.gl-v2-inline-error`) in plaats van een toast | Onzeker: in het logboek als "nog niet gebouwd" genoteerd | C onderzoekt, dan bouwen |
| C3 | Klantdetail: waarschuwing bij een ontbrekend weerstation | Nieuw | C |
| C4 | Mijlpaal koppelen aan een taak vanuit de mijlpaalmodal van het Traject | Nieuw | C |

## Fase D. Planning uitbreiden (oorspronkelijk plan, fase 2 tot 4)

| # | Punt | Wie |
|---|---|---|
| D1 | Werkkalender-paneel (bedrijfsdagen lezen en projecteigen dagen) | C |
| D2 | "Fases uit het budget" in de lege staat van de planning | C |
| D3 | Dashboard- en Projectdetail-kaart voor de planning | C |
| D4 | Export: Excel en PDF A3 in Documentlayout-stijl (design 59d) | C |
| D5 | Import uit Excel/CSV (MS Project-XML bestaat al) | C |
| D6 | Fase 4 rest: een hele fase slepen, afhankelijkheid trekken, selectiebalk met meerdere rijen, samenvoegen bij een conflict, tablet- en gsm-verfijning | C |

## Fase E. Sjablonen en AI

| # | Punt | Wie |
|---|---|---|
| E1 | Projectsjabloon kiezen bij het aanmaken van een project en bewaren op het project (eigen kolom voor het gekozen sjabloon) | CN (ontwerp eerst) |
| E2 | De AI-aanroep van het projectsjabloon echt testen met een sleutel en een echte beschrijving | N levert sleutel, C test |
| E3 | AI-uitbreidingen: voorstel per projectfase (traject bij start, schijven bij verkoop, planning bij uitvoering), patches op een sjabloon (route B), leeg schema (route C), voorstel opslaan; ook de gevelkeuze in het AI-voorstel | C |
| E4 | AI in de sjablooneditor (stap 5) | C |
| E5 | Deadlinewaarschuwingen (stap 6) | C |

## Fase F. Later (besluit Niels: niet nu)

B te bevestigen schijven en boekhoudlijst · D poorten · E marge en kritieke weg · F aannemer via activiteit (de gevelafwerkingstaken hebben nog geen activiteit) · G AI-duren naar eenheden, bouwlagen en gebouwgrootte, op basis van de historiek van afgewerkte projecten.

## Fase G. Afsluiten en opruimen (doorlopend)

| # | Punt | Wie |
|---|---|---|
| G1 | Alle help-screenshots in één batch met Playwright nemen volgens `docs/archive/help/HELP_SCREENSHOTS.md` (o.a. toepassen-venster, aktefacturen, bouwstaten, gevelafwerking) | C |
| G2 | Facturatietests van Niels (instellingen Aktefacturen, werkenfactuur-termijn, geen extra vermelding): Niels zei 27/11 klaar | N bevestigd |
| G3 | Migratielijst bijhouden: wat moet gedraaid zijn vóór een deploy. Nu nog te draaien: **170**. Gedraaid: 166 t/m 169 | CN |
| G4 | De uitgebreide tests in de browser bij elke afgewerkte fase; veel is nu alleen met unit-tests gecontroleerd | N |
| G5 | Branch-hygiëne: `master` is de stabiele lijn, per taak een korte branch; `layout-experiment` verwijderen als back-up niet meer nodig is | CN |

## Beslissingen die ik van Niels nodig heb

1. A1: kloppen de aannames (hout en aluminium parallel, duren, geen activiteit)? Moet het sjabloon "Woning" ook een gevelkeuze krijgen?
2. A3: akkoord dat bij een appartementsgebouw standaard "Eén planning" voorgeselecteerd staat?
3. B1: is de lijst "verkeerde zaken" verwerkt, of moet ik hem nog krijgen?
4. B4: eigen compromis/akte/oplevering per parking en berging of niet, en wel of niet in de planning?
5. Welke fase na A: B (appartementen afmaken) of C (bestaande planning controleren)? Mijn advies: eerst C1 en C2, omdat die nooit afgevinkt zijn en de basis van alles zijn.

## Voorgestelde volgorde

A2 en A3 afwerken → C1/C2 (controle) → B1 tot B4 (appartementsgebouw) → E1 (projectsjabloon bij aanmaken) → D (uitbreiding) → G1 (screenshots) → F.
