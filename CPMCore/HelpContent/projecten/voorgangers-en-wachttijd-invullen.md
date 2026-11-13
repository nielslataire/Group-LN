---
title: Voorgangers en wachttijd invullen
topic: projecten
role: Projectleider
device: Desktop · tablet
minutes: 4
updated: 2026-11-13
newsTag: Nieuw
screens: planning-index
related: planning-opbouwen, planning-lezen-en-opvolgen
synonyms: voorganger, afhankelijkheid, wachttijd, overlap, FS, SS, FF, SF, droogtijd, kritiek pad, beperking
description: bepalen welke taak op welke wacht en hoe lang
intro: |
  Een voorganger is een taak die eerst klaar of gestart moet zijn voor een andere taak kan beginnen. Daardoor schuift de planning vanzelf mee als iets uitloopt.
before: |
  Je hebt schrijfrecht op de planning.
faq:
  - q: Wat doet een negatieve wachttijd?
    a: Dan mag de taak vroeger beginnen. Bij 5.1FS-2d start de taak twee werkdagen eerder dan na het einde van 5.1, dus met een overlap.
  - q: Wat als ik een cyclus maak?
    a: CPM weigert dat en toont welke taken op elkaar zouden wachten. Er wordt niets bewaard.
  - q: Wat is een beperking?
    a: Een beperking dwingt een datum af, bijvoorbeeld Moet beginnen op. Zo een taak schuift niet mee met haar voorgangers. Niet later dan geeft enkel een waarschuwing.
  - q: Kan ik naar een fase verwijzen?
    a: Ja. Typ het nummer van de fase. De taak wacht dan op alle taken in die fase.
---
## Het formaat

Je typt het nummer van de voorganger, daarna eventueel het soort en de wachttijd. Meerdere voorgangers scheid je met een puntkomma.

| Je typt | Betekenis |
|---|---|
| **5.1** | Start na het einde van taak 5.1 |
| **5.1SS** | Start tegelijk met het begin van 5.1 |
| **5.1FF** | Eindigt tegelijk met het einde van 5.1 |
| **5.1SF** | Eindigt tegelijk met het begin van 5.1 |
| **5.1FS+3d** | Start drie werkdagen na het einde van 5.1, bijvoorbeeld voor droogtijd |
| **5.1FS-2d** | Start twee werkdagen eerder, dus een overlap |
| **2.8; 3.4** | Wacht op twee taken. De laatste bepaalt de start |

Zonder soort neemt CPM **FS**: de taak start de werkdag na het einde van de voorganger.

## Een voorganger wijzigen

1. Klik in de lijst op de kolom **Voorgangers** van de taak.
2. Typ de nieuwe waarde en druk op **Enter**.
3. De balk bovenaan toont hoeveel andere taken zijn verschoven en wat het einde van de planning doet.

Je ziet de voorgangers ook als pijlen in de grafiek.

## Een datum afdwingen

Open de taak en kies bij **Beperking** bijvoorbeeld **Niet eerder beginnen dan** en een datum. Een taak met **Moet beginnen op** is vastgepind en krijgt een punaise.

## Let op

Een taak zonder voorganger start op de eerste projectdag. CPM toont daarvoor een aandachtspunt.
