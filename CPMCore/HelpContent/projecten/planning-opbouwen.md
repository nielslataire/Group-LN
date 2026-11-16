---
title: Een planning opbouwen
topic: projecten
parent: planning-handleiding
treeOrder: 3
role: Projectleider
device: Desktop · tablet
minutes: 5
updated: 2026-11-15
newsTag: Nieuw
screens: planning-index
related: planning-handleiding, voorgangers-en-wachttijd-invullen, taak-datums-en-verantwoordelijke, planning-navigeren
synonyms: planning, bouwplanning, gantt, fase, deelfase, taak, mijlpaal, duur, werkdagen, verplaatsen, inspringen, uitspringen, verwijderen, nummering, wbs, ms project
description: fases en taken aanmaken, met een duur in werkdagen
intro: |
  Op de pagina **Planning** bouw je de planning van de bouw op. Je geeft elke taak een duur in werkdagen en een voorganger. CPM rekent alle data zelf uit en schuift alles mee als je iets wijzigt.
before: |
  Je hebt schrijfrecht op de planning. Vul bij het project de **Aanvangsdatum** in, anders rekent de planning vanaf vandaag.
faq:
  - q: Kan ik een planning laten vertrekken van een schema?
    a: Nog niet. Je bouwt de planning nu zelf op met fases en taken. Een sjabloon en het importeren van een MS Project-bestand volgen later.
  - q: Wat is het verschil tussen een fase en een taak?
    a: Een fase is een groep taken, zoals Ruwbouw. Ze neemt het begin en het einde over van de taken erin. Een taak heeft zelf een duur.
  - q: Hoe maak ik een mijlpaal?
    a: Geef een taak een duur van 0 werkdagen. Ze verschijnt dan als een ruitje op één dag.
  - q: Wat is een deelfase?
    a: Een fase binnen een andere fase, bijvoorbeeld Buitenschrijnwerk binnen Technieken. Een deelfase telt mee in het begin, het einde en de voortgang van de fase erboven. Kies bij Plaats in de planning de keuze Deelfase in.
  - q: Wat betekenen de nummers 1, 1.1, 1.2?
    a: Het nummer volgt de plaats in de lijst. Verplaats je een rij, dan veranderen de nummers mee. Voorgangers verwijzen naar die nummers en blijven automatisch kloppen.
  - q: Wie mag fases en taken verwijderen?
    a: Daarvoor heb je het recht om te verwijderen op de planning. Zonder dat recht zie je Verwijderen niet in het menu.
  - q: Kan een taak buiten een fase staan?
    a: Nee. Elke taak hoort in een fase. Maak je een taak zonder fase aan, dan maakt CPM er zelf een.
---
## Een fase maken

1. Klik rechtsboven op **Fase**.
2. Typ de **Naam**, bijvoorbeeld Ruwbouw.
3. Kies bij **Plaats in de planning** waar de fase komt. Een deelfase komt binnen een andere fase.
4. Typ eventueel de naam en de duur van de eerste taak en na welke taak de fase start.
5. Klik op **Fase toevoegen**.

## Een deelfase maken

1. Klik rechtsboven op **Fase**.
2. Typ de **Naam**.
3. Kies bij **Plaats in de planning** de keuze **Deelfase in** met de fase waarin ze komt.
4. Klik op **Fase toevoegen**.

Een deelfase springt in onder haar fase. Met **Inspringen** en **Uitspringen** in het menu van de rij verplaats je een bestaande fase of taak een niveau.

## Een taak toevoegen

1. Klik rechtsboven op **Taak toevoegen**. Of klik in een fase op **+ Taak**.
2. Typ de **Naam** en de **Duur** in werkdagen.
3. Kies de **Fase** en de **Plaats in de fase**.
4. Vul bij **Voorgangers** in welke taak eerst klaar moet zijn. CPM vult de taak erboven alvast in.
5. Klik op **Toevoegen**. Met **Toevoegen en nog één** blijft het paneel open voor de volgende taak.

Een weekend, een feestdag, bouwverlof of een weerverletdag telt niet als werkdag.

## Iets snel aanpassen

Klik in de lijst op de naam, de duur of de voorgangers van een taak en typ de nieuwe waarde. Druk op **Enter** om te bewaren. Klik op het rondje voor de naam om een taak als voltooid aan te duiden. Dat zet vandaag als werkelijk einde. Lees daarvoor "De planning bijhouden tijdens de bouw". Open een taak voor alle andere velden, zoals beperking, deadline en notitie.

## Het menu van een rij

Klik op de drie puntjes bij een rij.

| Keuze | Wat ze doet |
|---|---|
| **Bewerken** | Opent het paneel van de taak of de fase. |
| **Taak eronder toevoegen** of **Taak toevoegen in de fase** | Voegt een taak toe op die plaats. |
| **Fase eronder toevoegen** | Enkel bij een fase. Voegt een fase toe na deze. |
| **Markeren als voltooid** | Enkel bij een taak. Zet de voortgang op 100 %. |
| **Omhoog** en **Omlaag** | Verplaatst de rij binnen haar fase. |
| **Inspringen** | De rij komt achteraan in de fase erboven. Dat moet een fase zijn. |
| **Uitspringen** | De rij komt een niveau hoger, direct na haar fase. Een taak kan enkel in een andere fase landen. |
| **Verwijderen** | Verwijdert de rij na bevestiging. |

Taken verplaats je enkel binnen of tussen fases. Een taak blijft altijd in een fase.

## Verwijderen

Kies **Verwijderen** in het menu van de rij en bevestig. Verwijder je een fase, dan verdwijnen ook haar deelfases en taken. Taken die op een verwijderde taak wachtten, verliezen die voorganger. Je kunt dit ongedaan maken via de balk bovenaan.

## Let op

Je wijzigingen worden meteen bewaard. De nummers (1, 1.1, 1.2) volgen de volgorde in de lijst en veranderen mee als je rijen verplaatst.
