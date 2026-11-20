---
title: Een planningssjabloon bewaren en toepassen
topic: projecten
parent: planning-handleiding
treeOrder: 10
role: Projectleider
device: Desktop · tablet
minutes: 6
updated: 2026-11-18
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
    a: Er is nog geen sjabloon bewaard. Iemand met het recht Planningssjablonen bewaart eerst een bestaande planning als sjabloon met de knop **Als sjabloon** naast de tabs.
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
    a: Geef de taak (of het blok) in het sjabloon een code, bijvoorbeeld DAK_KLAAR, en geef de mijlpaal in het traject dezelfde code. CPM koppelt ze dan vanzelf, ook als je het traject pas later aanmaakt. De mijlpaal volgt daarna het einde van die taak. Komt dezelfde code op meer dan één plaats voor (bijvoorbeeld in twee woningen), dan laat CPM de mijlpaal met rust, want het is dan niet te zeggen welke taak bedoeld is. Koppel die handmatig in het taakpaneel.
  - q: Kan ik de toepassing ongedaan maken?
    a: Niet met Ongedaan maken. Verwijder de gemaakte plannings met **Instellen** naast de tabs.
  - q: Een woning heeft een andere planning nodig dan de rest. Kan dat?
    a: Ja. Pas het sjabloon toe en wijzig daarna de planning van die woning zoals elke andere planning.
---
## Een planning bewaren als sjabloon

1. Open de planning die je wilt bewaren, bijvoorbeeld die van woning 1.
2. Klik naast de tabs op **Als sjabloon**.
3. Geef het sjabloon een naam, bij voorkeur duidelijk, zoals "Eengezinswoning, 2 verdiepingen".
4. Klik op **Bewaren**.

CPM geeft elke fase en taak een vaste code, afgeleid van de naam, bijvoorbeeld RUWBOUW. De voorgangers worden met die codes bewaard.

## Een sjabloon toepassen

1. Klik naast de tabs op **Sjabloon**.
2. Kies het sjabloon.
3. Zet een vinkje bij de woningen of plannings waarvoor je het sjabloon wilt gebruiken. Een woning zonder planning staat er als "nieuwe planning" bij en krijgt een eigen planning. Wil je meerdere woningen in één planning, maak die dan eerst met **Planning** aan en kies daar de eenheden. Heeft een planning al taken, dan staat er "vervangt de planning" bij. Kies je die, dan vraagt CPM eerst om bevestiging, en de bestaande fases en taken gaan verloren. Een lege planning vult het sjabloon gewoon.
4. Bepaal de volgorde met de pijltjes. Die volgorde bepaalt de estafette.
5. Vraagt het sjabloon naar verdiepingen, kelder of dak, vul dat dan in per woning. In de rij **Alle gekozen** vul je het één keer voor alle woningen in en pas je daarna enkele af.
6. Klik op **Toepassen**.

Elke woning krijgt een planning met haar eigen naam (of houdt de planning die ze al had, nu met de taken van het sjabloon). De betalingsgroep speelt hierbij geen rol. Taken die in het sjabloon de estafettevlag hebben, wachten op dezelfde taak in de woning erboven in de lijst. CPM rekent alles samen door, dus het einde van de laatste woning is ook het einde van het project.

## Verdiepingen, kelder en dak

Een sjabloon kan blokken bevatten die alleen bij een kelder of een bepaald dak horen, en blokken die per verdieping terugkomen als "Ruwbouw verdieping 1", "Ruwbouw verdieping 2" enzovoort. Bij één verdieping blijft de naam zonder nummer.

## De lijst met sjablonen

Onder **Instellingen › Sjablonen › Planning** zie je alle sjablonen met het aantal blokken en taken, de duur in werkdagen (van één woning), waarop het sjabloon reageert, waar het gebruikt is, wie het laatst wijzigde en de status. Bij sjablonen met verdiepingen staat de duur bij twee verdiepingen.
![Lijst met sjablonen](/img/help/projecten/planning-sjablonen/2-lijst.png)

Via **⋯** op een rij kun je het sjabloon **dupliceren** (een kopie met dezelfde blokken en codes), **inactief zetten** of **verwijderen**. Verwijderen kan enkel als het nergens gebruikt is en vraagt eerst om bevestiging.

## Een sjabloon bewerken

1. Klik op **Nieuw sjabloon**, of open een bestaand sjabloon in de lijst.
2. Geef het sjabloon een naam en eventueel een korte omschrijving. Zet **Actief** uit als je het niet meer wilt aanbieden bij het toepassen.
3. Voeg blokken toe met **Blok toevoegen**. Een blok wordt een fase in de planning, bijvoorbeeld Fundering of Ruwbouw. Klap een blok in of uit met het pijltje, of alles tegelijk met **Alles inklappen** en **Alles uitklappen**.
4. Kies per blok bij **Komt voor** wanneer het meedoet: altijd, alleen met kelder, of alleen bij een hellend of plat dak.
5. Zet **Per verdieping** aan voor een blok dat per verdieping terugkomt. Onderaan het blok bepaal je of elke verdieping start na de vorige.
6. Voeg taken toe met **Taak toevoegen** en vul de duur in werkdagen in. Herschik taken met het handvat links van de naam, of via **⋯**.
7. Klik bij een taak op **+ voorganger** en kies een taak of een heel blok. Zoek eventueel op naam. Een blok als voorganger betekent al zijn taken.
8. Klik op een voorganger om het soort afhankelijkheid en de wachttijd te kiezen. FS = na het einde, SS = samen starten, FF = samen eindigen, SF = start voor einde. De wachttijd mag negatief zijn. Een zin onderaan zegt wat het betekent, bijvoorbeeld “Start samen met Funderingen, 3 werkdagen later”.
9. Zet de knop met de twee woningen aan bij taken die bij meerdere woningen na elkaar komen (estafette). Via **⋯** voeg je extra wachtdagen toe.
10. Klik op **Sjabloon opslaan**.
![Sjabloon bewerken](/img/help/projecten/planning-sjablonen/3-bewerken.png)
![Voorganger kiezen](/img/help/projecten/planning-sjablonen/4-voorganger.png)

Elke fase en taak heeft een vaste code onder zijn naam. De code verandert niet als je de naam wijzigt, zodat voorgangers blijven kloppen. CPM controleert bij het opslaan of elke voorganger bestaat en of taken niet op elkaar blijven wachten. Is er een rood punt, dan wordt er niets bewaard.

## Voorbeeld, proefdraaien en controle

Rechts naast de blokken zie je meteen wat je sjabloon oplevert. Bij elke taak staat de startdag in een voorbeeldplanning, taken op het kritieke pad hebben een **K**.

- **Voorbeeld** toont de duur per blok, de totale duur en het kritieke pad. Blokken met een voorwaarde doen mee alsof de kelder er is en het dak hellend is.
- **Proefdraaien** rekent het sjabloon door voor 1 tot 4 woningen en 1 tot 3 verdiepingen, zoals bij het toepassen op een project. Zo zie je het effect van de estafette en van blokken per verdieping.
- **Controle** meldt rode punten (een kring van taken die op elkaar wachten, een onbekende code) en aandachtspunten, zoals een taak zonder voorganger. Die start op dag 1 van het project.
![Proefdraaien](/img/help/projecten/planning-sjablonen/5-proefdraaien.png)

## Een sjabloon verwijderen

Kies het sjabloon bij **Sjabloon** op de planningpagina en klik op **Sjabloon verwijderen**, of gebruik **⋯** in Instellingen › Sjablonen › Planning. CPM vraagt eerst om bevestiging. Een sjabloon dat al in een project gebruikt is, kan niet verwijderd worden. Zet het dan op inactief.

## Let op

Controleer na het toepassen de duur en de voorgangers. Het sjabloon neemt over wat in de bron stond.
![Sjabloon toepassen](/img/help/projecten/planning-sjablonen/1-toepassen.png)
