---
title: Een planningssjabloon bewaren en toepassen
topic: projecten
parent: planning-handleiding
treeOrder: 10
role: Projectleider
device: Desktop · tablet
minutes: 6
updated: 2026-11-24
newsTag: Nieuw
screens: planning-index, instellingen-planningssjablonen, instellingen-planningssjablonen-bewerken
related: planning-per-bouwdeel, planning-opbouwen, voorgangers-en-wachttijd-invullen
synonyms: proefdraaien, kritiek pad, reageert op, dupliceren, inactief, sjablooneditor, sjabloon bewerken, codes, voorganger, sjabloon, standaardplanning, planning kopiëren, meerdere woningen, estafette, verdiepingen, kelder, dak
description: een goede planning bewaren en met één handeling toepassen op tientallen woningen of gebouwen
intro: |
  Bouw je dezelfde woning twintig keer, dan wil je de planning niet twintig keer intypen. Bewaar een goede planning als sjabloon en pas ze toe op de woningen of gebouwen die nog geen planning hebben.
  CPM maakt dan per woning een eigen planning. Op wens wacht een taak op dezelfde taak in de woning ervoor, zodat de ploegen van woning naar woning schuiven.
before: |
  Je hebt schrijfrecht op de planning. Om een sjabloon te bewaren of te verwijderen heb je ook het recht Planningssjablonen nodig. De woningen of gebouwen bestaan al bij Facturatie en hebben nog geen planning.
faq:
  - q: Waarom staat er geen sjabloon in de lijst?
    a: Er is nog geen sjabloon bewaard. Iemand met het recht Planningssjablonen bewaart eerst een bestaande planning als sjabloon via **Sjablonen › Als sjabloon**.
  - q: Wat neemt een sjabloon mee van mijn planning?
    a: De fases, de taken met hun duur en notitie, en de voorgangers binnen die planning. Voorgangers uit de hoofdplanning of een andere woning gaan niet mee. Voortgang, werkelijke data en de basisplanning ook niet.
  - q: Waar pas ik een sjabloon aan?
    a: Onder Instellingen › Sjablonen › Planning. Daar maak je een sjabloon, wijzig je blokken en taken, en verwijder je het. Je hebt daarvoor het recht Planningssjablonen nodig.
  - q: Waarom kan ik een sjabloon niet verwijderen?
    a: Het is al gebruikt in een project (kolom **Gebruikt in**). Zet het dan op inactief: het blijft staan, maar wordt niet meer aangeboden bij het toepassen.
  - q: Wat doet "Reageert op"?
    a: Het legt vast wanneer CPM het sjabloon later automatisch voorstelt, bijvoorbeeld bij het starten van de werken. Voorlopig bewaart CPM enkel je keuze. Je past het sjabloon nog altijd zelf toe.
  - q: Ik krijg een fout dat een voorganger niet bestaat. Wat nu?
    a: De code van de voorganger staat niet in dit sjabloon, bijvoorbeeld omdat je de taak of het blok verwijderd hebt. Kies de voorganger opnieuw in de lijst onder Voorgangers.
  - q: Hoe hangt een mijlpaal in het traject aan een taak uit het sjabloon?
    a: Geef de taak (of het blok) in het sjabloon een code, bijvoorbeeld DAK_KLAAR, en geef de mijlpaal in het traject dezelfde code. CPM koppelt ze dan vanzelf, ook als je het traject pas later aanmaakt. De mijlpaal volgt daarna het einde van die taak. Heeft de mijlpaal een eigen woning of appartement (een mijlpaal per eenheid), dan zoekt CPM de code enkel in de planning van die woning. Zo volgt elke woning haar eigen taak, ook al staat dezelfde code in de planning van elke woning. Alle appartementen van één gebouw volgen dezelfde taak. Een mijlpaal van het hele project met een code die op meer dan één plaats voorkomt (bijvoorbeeld in twee woningen) laat CPM met rust, want het is dan niet te zeggen welke taak bedoeld is. Hetzelfde geldt voor een woning die in geen enkele planning zit. Koppel die handmatig in het taakpaneel. Open daarvoor de planning van de woning: de mijlpalen van die woning staan in de lijst Koppeling met trajectmijlpaal, met de naam van de woning erachter. Een mijlpaal van een woning kan enkel aan een taak of fase in de planning van die woning.
  - q: Kan ik de toepassing ongedaan maken?
    a: Niet met Ongedaan maken. Verwijder de gemaakte plannings met het tandwiel **Instellen** rechtsboven de planning.
  - q: Een woning heeft een andere planning nodig dan de rest. Kan dat?
    a: Ja. Pas het sjabloon toe en wijzig daarna de planning van die woning zoals elke andere planning.
---
## Een planning bewaren als sjabloon

1. Open de planning die je wilt bewaren, bijvoorbeeld die van woning 1.
2. Klik rechtsboven op **Sjablonen** en kies **Als sjabloon**.
3. Geef het sjabloon een naam, bij voorkeur duidelijk, zoals "Eengezinswoning, 2 verdiepingen".
4. Klik op **Bewaren**.

CPM geeft elke fase en taak een vaste code, afgeleid van de naam, bijvoorbeeld RUWBOUW. De voorgangers worden met die codes bewaard.

## Een sjabloon toepassen

1. Klik rechtsboven op **Sjablonen** en kies **Sjabloon**.
2. Kies het sjabloon.
3. Zet een vinkje bij de woningen of plannings waarvoor je het sjabloon wilt gebruiken. Een woning zonder planning staat er als "nieuwe planning" bij en krijgt een eigen planning. Wil je meerdere woningen in één planning, maak die dan eerst met **Planning** aan en kies daar de eenheden. Heeft een planning al taken, dan staat er "vervangt de planning" bij. Kies je die, dan vraagt CPM eerst om bevestiging, en de bestaande fases en taken gaan verloren. Een lege planning vult het sjabloon gewoon.
4. Bepaal de volgorde met de pijltjes. Die volgorde bepaalt de estafette.
5. Vraagt het sjabloon naar bouwlagen, kelder, dak of terrassen en luifels, vul dat dan in per woning. In de rij **Alle gekozen** vul je het één keer voor alle woningen in en pas je daarna enkele af.
6. Klik op **Toepassen**.

Elke woning krijgt een planning met haar eigen naam (of houdt de planning die ze al had, nu met de taken van het sjabloon). De betalingsgroep speelt hierbij geen rol. Taken die in het sjabloon de estafettevlag hebben, wachten op dezelfde taak in de woning erboven in de lijst. CPM rekent alles samen door, dus het einde van de laatste woning is ook het einde van het project.

## Bouwlagen, kelder, dak en terrassen

Wij tellen zoals in Vlaanderen: gelijkvloers, 1ste verdieping, 2de verdieping. Het gelijkvloers is geen verdieping, maar telt wel als bouwlaag. Gelijkvloers + 2 verdiepingen vul je dus in als **3 bouwlagen**.

Een sjabloon kan blokken bevatten die alleen bij een kelder of een bepaald dak horen, en blokken die per bouwlaag terugkomen als "Ruwbouw gelijkvloers", "Ruwbouw 1ste verdieping", "Ruwbouw 2de verdieping" enzovoort. Bij één bouwlaag blijft de naam zonder toevoeging.

De voorganger **Eerste bouwlaag: …** wijst naar dezelfde taak in de eerste bouwlaag en kan vanuit elke taak gekozen worden. Zo start het gevelmetselwerk pas na het metselwerk van de eerste bouwlaag, en niet na dat van de bovenste. De afdek van een bouwlaag is de vloerplaat van de volgende. In het sjabloon kies je bij het metselwerk als voorganger **Vorige bouwlaag: Afdek**: het metselwerk van een bouwlaag start dan pas na de afdek van de vorige, en niet na de hele vorige bouwlaag. Met een kelder is de afdek van de kelder de vloerplaat van het gelijkvloers, dus dan komt er geen aparte vloerplaat gelijkvloers.

Een taak onder Ruwbouw zonder voorganger, zoals het gevelmetselwerk, start op de eerste dag van de planning. Geef ze zelf een voorganger als ze later moet beginnen, bijvoorbeeld "Start samen" met het metselwerk.

Heeft het sjabloon een taak voor **terrassen of luifels**, dan kies je bij het toepassen met de knoppen (GV, 1, 2 …) bij de afdek van welke bouwlaag ze komen. Die taak staat in dezelfde fase Ruwbouw, enkel bij de gekozen bouwlagen. Een terras op de 1ste verdieping hoort bij de afdek van het gelijkvloers.

## De lijst met sjablonen

Onder **Instellingen › Sjablonen › Planning** zie je alle sjablonen met het aantal blokken en taken, de duur in werkdagen (van één woning), waarop het sjabloon reageert, waar het gebruikt is, wie het laatst wijzigde en de status. Bij sjablonen met verdiepingen staat de duur bij twee verdiepingen.
![Lijst met sjablonen](/img/help/projecten/planning-sjablonen/2-lijst.png)

Via **⋯** op een rij kun je het sjabloon **dupliceren** (een kopie met dezelfde blokken en codes), **inactief zetten** of **verwijderen**. Verwijderen kan enkel als het nergens gebruikt is en vraagt eerst om bevestiging.

## Een sjabloon bewerken

1. Klik op **Nieuw sjabloon**, of open een bestaand sjabloon in de lijst.
2. Geef het sjabloon een naam en eventueel een korte omschrijving. Zet **Actief** uit als je het niet meer wilt aanbieden bij het toepassen.
3. Voeg blokken toe met **Blok toevoegen**. Een blok wordt een fase in de planning, bijvoorbeeld Fundering of Ruwbouw. Klap een blok in of uit met het pijltje, of alles tegelijk met **Alles inklappen** en **Alles uitklappen**.
4. Kies per blok bij **Komt voor** wanneer het meedoet: altijd, alleen met kelder, of alleen bij een hellend of plat dak.
5. Kies bij een blok onder **Herhaling per bouwlaag** hoe het per bouwlaag terugkomt: **Niet per bouwlaag**, **Een fase per bouwlaag** ("Ruwbouw gelijkvloers", "Ruwbouw 1ste verdieping" …) of **Eén fase, taken per bouwlaag**. Bij die laatste stand blijft het één fase, bijvoorbeeld **Ruwbouw - Bovenbouw**, met per bouwlaag een eigen deelfase ("Gelijkvloers", "1ste verdieping", "2de verdieping" …, enkel bij meer dan één bouwlaag) en de taken erin: "Predallen gelijkvloers", "Opstort gelijkvloers", "Metselwerk gelijkvloers", "Predallen 1ste verdieping" … Je zet de taken maar één keer in het sjabloon.
6. Achter de duur van een taak staat **d**. Klik erop om te wisselen naar **d/eenh.**: de duur is dan het aantal dagen per eenheid × het aantal woningen of appartementen van de planning, bijvoorbeeld 1 dag per eenheid voor het plaatsen van de sanitaire toestellen. Bij het toepassen telt CPM de hoofdeenheden (woning, appartement, handelsruimte) van de planning, niet de parkings en bergingen. In het voorbeeld van de editor rekent het met een gebouw van 10 eenheden.
7. Een taak voor een gevelafwerking krijgt de voorwaarde **Alleen bij een stenen gevel**, **Alleen bij gevelpleister**, **Alleen bij houten gevelbekleding** of **Alleen bij aluminium gevelbekleding**. Ze komt dan enkel in de planning als die afwerking bij het toepassen gekozen is; alle vier zijn optioneel. De gevelpleister en de bekleding laat je wachten op het blok van de bovenbouw (kies bij Voorgangers het blok): ze starten dan pas als de hele bovenbouw af is, ook het gevelmetselwerk als dat gekozen is. Laat de houten of aluminium bekleding ook op de gevelpleister wachten: is de pleister niet gekozen, dan valt die voorganger vanzelf weg.
7. Kies bij een taak onder haar naam **Komt voor** als ze niet op elke bouwlaag hoort: alleen op het gelijkvloers, gelijkvloers zonder kelder, alleen boven het gelijkvloers, alleen op de bovenste bouwlaag, gelijkvloers zonder kelder (op de grond), alleen bij terrassen of luifels, alleen met of zonder kelder, of een bepaald dak. In een blok "Eén fase, taken per bouwlaag" komt een taak met **Eén keer, na de bouwlagen** maar één keer voor, zonder bouwlaag in de naam: bijvoorbeeld het gevelmetselwerk, of de predallen en opstort van een plat dak (**Eén keer, enkel bij plat dak**). Onderaan het blok bepaal je of elke verdieping start na de vorige.
8. Voeg taken toe met **Taak toevoegen** en vul de duur in werkdagen in. Herschik taken met het handvat links van de naam, of via **⋯**.
9. Klik bij een taak op **+ voorganger** en kies een taak of een heel blok. Zoek eventueel op naam. Een blok als voorganger betekent al zijn taken.
10. Klik op een voorganger om het soort afhankelijkheid en de wachttijd te kiezen. FS = na het einde, SS = samen starten, FF = samen eindigen, SF = start voor einde. De wachttijd mag negatief zijn. Een zin onderaan zegt wat het betekent, bijvoorbeeld “Start samen met Funderingen, 3 werkdagen later”.
11. Zet de knop met de twee woningen aan bij taken die bij meerdere woningen na elkaar komen (estafette). Via **⋯** voeg je extra wachtdagen toe.
12. Klik op **Sjabloon opslaan**.
![Sjabloon bewerken](/img/help/projecten/planning-sjablonen/3-bewerken.png)
![Voorganger kiezen](/img/help/projecten/planning-sjablonen/4-voorganger.png)

Elke fase en taak heeft een vaste code onder zijn naam. De code verandert niet als je de naam wijzigt, zodat voorgangers blijven kloppen. CPM controleert bij het opslaan of elke voorganger bestaat en of taken niet op elkaar blijven wachten. Is er een rood punt, dan wordt er niets bewaard.

## Bouwstaat en activiteit per taak

Onder elke taak staan drie keuzelijsten: **Komt voor**, **Bouwstaat** en **Activiteit**. Is **Komt voor** iets anders dan "Altijd", dan kleurt het veld groen. Is de bouwstaat of de activiteit nog leeg, dan heeft het veld een gouden stippellijn.

- De **bouwstaat** is de toestand die de taak oplevert, bijvoorbeeld "Opstort gegoten". Een betalingsschijf of trajectmijlpaal wacht op een bouwstaat. Leveren twee taken dezelfde bouwstaat (bv. de leidingen van elektriciteit en van sanitair), dan is de bouwstaat bereikt als beide klaar zijn. Je beheert de lijst onder **Instellingen › Automatisatie › Bouwstaten**.
- De **activiteit** is de discipline: wie het werk doet en op welk lot van het budget het meetelt. Het is dezelfde lijst als bij contracten en budgetten. Later volgt de uitvoerder van een taak uit het contract dat die activiteit bevat.
- In een gouden balk boven de blokken zie je hoeveel taken nog geen bouwstaat of activiteit hebben, met de namen van die taken. Opslaan kan ook zonder.
- Een voorganger **Vorige bouwlaag: …** of **Eerste bouwlaag: …** staat als blauwe chip bij de taak, zodat je ze niet verwart met een gewone voorganger.
- Bij het toepassen van het sjabloon nemen de planningtaken de bouwstaat en de activiteit over.

## Voorbeeld, proefdraaien en controle

Rechts naast de blokken zie je meteen wat je sjabloon oplevert. Bij elke taak staat de startdag in een voorbeeldplanning, taken op het kritieke pad hebben een **K**.

- **Voorbeeld** toont de duur per blok, de totale duur en het kritieke pad. Een blok met een deelfase per bouwlaag toont ook de duur per bouwlaag en de eenmalige taken.
- **Proefdraaien** rekent het sjabloon door voor 1 tot 4 woningen en 1 tot 3 verdiepingen, met of zonder kelder, zoals bij het toepassen op een project. Zo zie je het effect van de estafette en van blokken per verdieping. Taken die in die proef wegvallen, bijvoorbeeld "alleen met kelder" bij **Zonder**, worden grijs en tonen als start een streepje.
- **Controle** meldt rode punten (een kring van taken die op elkaar wachten, een onbekende code) en aandachtspunten, zoals een taak zonder voorganger. Die start op dag 1 van het project.
![Proefdraaien](/img/help/projecten/planning-sjablonen/5-proefdraaien.png)

## Een sjabloon verwijderen

Kies het sjabloon bij **Sjabloon** op de planningpagina en klik op **Sjabloon verwijderen**, of gebruik **⋯** in Instellingen › Sjablonen › Planning. CPM vraagt eerst om bevestiging. Een sjabloon dat al in een project gebruikt is, kan niet verwijderd worden. Zet het dan op inactief.

## Let op

Controleer na het toepassen de duur en de voorgangers. Het sjabloon neemt over wat in de bron stond.
![Sjabloon toepassen](/img/help/projecten/planning-sjablonen/1-toepassen.png)
