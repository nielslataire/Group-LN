---
title: Aandachtspunten en fouten in de planning oplossen
topic: projecten
parent: planning-handleiding
treeOrder: 11
role: Projectleider
device: Desktop · tablet
minutes: 6
updated: 2026-11-15
newsTag: Nieuw
screens: planning-index
related: planning-handleiding, planning-lezen-en-opvolgen, voorgangers-en-wachttijd-invullen, taak-datums-en-verantwoordelijke
synonyms: aandachtspunt, melding, waarschuwing, fout, cyclus, geen voorganger, te laat, deadline overschreden, conflict, herladen, opslaan mislukt, kan niet
description: wat elke melding en foutboodschap in de planning betekent en hoe je ze oplost
intro: |
  Bovenaan de planning kunnen meldingen staan. Dat zijn waarschuwingen over je planning. Een foutboodschap verschijnt als CPM een wijziging weigert.
  Hier staat wat ze betekenen en wat je doet.
before: |
  Je hebt leesrecht op de planning om de meldingen te zien.
faq:
  - q: Moet ik elk aandachtspunt oplossen?
    a: Nee. Het zijn waarschuwingen. Een taak zonder voorganger, bijvoorbeeld de eerste van de planning, is vaak in orde. De rode melding over de deadline oplevering verdient wel aandacht.
  - q: Waarom is een taak niet verschoven nadat ik haar voorganger wijzigde?
    a: De taak heeft een beperking zoals Moet beginnen op, of een werkelijke start. Die wint van de voorgangers. Open de taak om het te zien.
  - q: Waarom start mijn taak later dan ik verwacht?
    a: Open de taak. Bovenaan staat "Start bepaald door taak", met het nummer van de voorganger die het laatst klaar is. Weekends, feestdagen, bouwverlof en weerverlet tellen niet mee.
  - q: Kan ik een gewiste taak terughalen?
    a: Ja, direct na de wijziging met Ongedaan maken in de balk bovenaan. Na het herladen van de pagina niet meer.
---
## De meldingen openen

Bovenaan de planning staat een regel met het aantal aandachtspunten. Klik op **Tonen** om ze te lezen. Klik bij een taak op **Toon taak** om naar haar rij te gaan. Een ingeklapte fase klapt dan open.

## Meldingen over het hele project

| Melding | Wat het betekent | Wat je doet |
|---|---|---|
| **Geen aanvangsdatum** | Het project heeft geen aanvangsdatum. De planning start vandaag. | Vul de aanvangsdatum in bij het project. |
| **Geen weerstation** | De werkdagen en de deadline rekenen zonder weerverlet. | Kies een weerstation bij het project. |
| **Einde valt na de voorziene oplevering** | De planning eindigt na de deadline oplevering. Je ziet hoeveel werkdagen te laat. | Kort taken op het kritieke pad in of laat ze overlappen. Of pas de uitvoeringstermijn aan als die niet meer klopt. |
| **Einde valt na de voorziene oplevering van een klant** | Een klant heeft een eigen termijn en de planning eindigt daarna. | Plan sneller, of bespreek de termijn met de klant. |

## Meldingen bij een taak

| Melding | Wat het betekent | Wat je doet |
|---|---|---|
| **Heeft geen voorganger en start op de eerste projectdag** | Niets bepaalt wanneer de taak start. | Vul een voorganger in. Start de taak bewust op een vaste datum, kies dan een beperking. De melding verdwijnt. |
| **Einde na de deadline (n werkdagen te laat)** | De taak eindigt na haar eigen deadline. | Kort de taak of haar voorgangers in, of pas de deadline aan. |
| **Valt na de beperking (n werkdagen te laat)** | De taak start of eindigt later dan haar beperking Niet later dan. | Kort haar voorgangers in of pas de beperking aan. |
| **Wacht op zichzelf (cyclus in de voorgangers)** | Taken wachten rechtstreeks of via andere taken op elkaar. | Haal een voorganger weg in de keten. De datums van deze taken zijn niet betrouwbaar tot je dat doet. |

## Wijzigingen die CPM weigert

Bij een foutboodschap is er niets bewaard.

| Boodschap | Oplossing |
|---|---|
| "…is geen geldige voorganger. Typ bv. 5.1, 5.1SS of 5.1FS+3d." | Typ het nummer, daarna eventueel SS, FF, SF of FS en een wachttijd met d. Zie "Voorgangers en wachttijd invullen". |
| "Taak 9.9 bestaat niet." | Het nummer bestaat niet. De nummers volgen de volgorde in de lijst. Controleer ze. |
| "Een taak kan niet op zichzelf wachten." | Je typte het eigen nummer van de taak. |
| "Dat kan niet: de taken … zouden op elkaar wachten (cyclus)." | De nieuwe voorganger zou een kring maken. Kies een andere voorganger. |
| "De duur moet een aantal werkdagen zijn tussen 0 en 2000." | Typ een geheel getal van 0 tot 2000. |
| "Kies een datum bij de beperking." | Een beperking zonder datum kan niet. Kies een datum of kies Zo vroeg mogelijk. |
| "Inspringen kan enkel onder een fase." | De rij erboven is geen fase. Zet eerst een fase boven de rij. |
| "Een taak hoort altijd in een fase." | Je probeerde een taak uit haar hoogste fase te halen. Verplaats ze naar een andere fase. |
| "Kies eerst een budgetlot om de voortgang te laten meetellen." | Kies eerst een lot bij de fase. |
| "De voortgang moet tussen 0 en 100 liggen." | Typ een percentage van 0 tot 100. |

## Als iemand anders ook werkt

Wijzigt een collega de planning terwijl jij ze open hebt, dan verschijnt "Planning herladen". CPM toont dan de nieuwste planning. Je laatste wijziging is niet bewaard. Voer ze opnieuw uit.
Een melding "Opslaan mislukt" of "Je hebt hier geen recht toe" betekent dat de verbinding wegviel of dat je geen schrijfrecht hebt. Probeer het opnieuw of vraag het recht aan.

## Let op

Een wijziging terugdraaien kan enkel direct erna, met **Ongedaan maken** in de balk bovenaan. Daarna kies je **Opnieuw** om ze toch uit te voeren. Herlaad je de pagina, dan kan het niet meer.
