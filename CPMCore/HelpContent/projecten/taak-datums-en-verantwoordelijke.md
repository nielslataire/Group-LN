---
title: Een datum, deadline of verantwoordelijke bij een taak instellen
topic: projecten
parent: planning-handleiding
treeOrder: 5
role: Projectleider
device: Desktop · tablet
minutes: 5
updated: 2026-11-15
newsTag: Nieuw
screens: planning-index
related: planning-handleiding, voorgangers-en-wachttijd-invullen, planning-bijhouden-tijdens-de-bouw, planning-problemen-oplossen, project-traject-eenheden
synonyms: trajectmijlpaal, mijlpaal koppelen, traject, koppeling, beperking, moet beginnen op, niet eerder beginnen dan, niet later beginnen dan, niet eerder eindigen dan, niet later eindigen dan, moet eindigen op, vastpinnen, punaise, deadline, verantwoordelijke, notitie, taak openen
description: een taak openen en een datum afdwingen, een deadline zetten, een verantwoordelijke kiezen of een mijlpaal uit het Traject koppelen
intro: |
  Meestal bepalen de voorgangers wanneer een taak start. Soms ligt een datum vast, bijvoorbeeld de levering van ramen of de dag van een kraan.
  Dan geef je de taak een beperking. Je kunt er ook een deadline, een verantwoordelijke en een notitie bij zetten.
before: |
  Je hebt schrijfrecht op de planning.
faq:
  - q: Wat is het verschil tussen een deadline bij een taak en de deadline oplevering?
    a: De deadline oplevering hoort bij het project. CPM rekent ze uit en toont ze bovenaan. De deadline bij een taak vul je zelf in. CPM toont enkel een aandachtspunt als de taak daarna eindigt. De taak schuift niet.
  - q: Moet beginnen op, wat gebeurt er met de voorgangers?
    a: De taak start op die datum, ook als een voorganger dan nog bezig is. CPM waarschuwt daar niet voor. Controleer daarom zelf of de voorgangers op tijd klaar zijn.
  - q: Wat is het verschil tussen een beperking en een deadline?
    a: Een beperking verplaatst de taak zodat de datum klopt, behalve bij de twee "niet later dan"-keuzes. Een deadline verplaatst nooit iets. Ze waarschuwt enkel.
  - q: Krijgt de verantwoordelijke een bericht?
    a: Nee. De verantwoordelijke is voorlopig enkel een aanduiding bij de taak. CPM stuurt er geen bericht mee en toont de taak ook niet in Mijn taken.
  - q: Waarom staat mijn mijlpaal niet in de lijst met trajectmijlpalen?
    a: De lijst toont enkel mijlpalen van het hele project die je met de hand bijhoudt of die al aan de planning hangen. Een mijlpaal die automatisch iets anders volgt, zoals een document of een dossier, staat er niet in. Een mijlpaal van één eenheid staat er ook niet in.
  - q: Wat als de taak al klaar is op het moment dat ik koppel?
    a: Dan staat de mijlpaal in het Traject na het koppelen meteen op Bereikt, met als datum het werkelijk einde van de taak. Een mijlpaal die bereikt is, zet CPM nooit terug.
  - q: Wat gebeurt er met de mijlpaal als ik de taak verwijder?
    a: De mijlpaal verliest haar datum uit de planning en staat verder open. Koppel haar aan een andere taak of haal de koppeling weg.
  - q: Waarom heeft mijn taak een punaise?
    a: De taak heeft de beperking Moet beginnen op. Ze is vastgepind en schuift niet mee met haar voorgangers.
---
## Een taak openen

1. Zoek de taak in de lijst.
2. Klik op het nummer, op de start of het einde, of op haar balk in de grafiek. Of kies **Bewerken** in het menu van de rij.

Klik je op de naam, de duur of de voorgangers, dan pas je enkel dat veld snel aan. Bovenaan het paneel staat een samenvatting: de datums, de speling, welke taak de start bepaalt en of er een aandachtspunt is.

## Een beperking kiezen

![Het paneel van een taak met de beperking, de deadline en de verantwoordelijke](/img/help/projecten/taak-datums-en-verantwoordelijke/1-taakpaneel.png)

1. Kies bij **Beperking** wat je wilt afdwingen.
2. Kies de **Datum van de beperking**.
3. Klik op **Opslaan**.

| Beperking | Wat CPM doet |
|---|---|
| **Zo vroeg mogelijk** | De standaard. De taak start zodra haar voorgangers klaar zijn. |
| **Moet beginnen op** | De taak start op die dag, wat haar voorgangers ook doen. Ze krijgt een punaise. |
| **Niet eerder beginnen dan** | De taak start op die dag of later. Zijn de voorgangers later klaar, dan wint dat. Handig voor een levering. |
| **Niet later beginnen dan** | De taak verschuift niet. Start ze later, dan krijg je een aandachtspunt. |
| **Niet eerder eindigen dan** | De taak eindigt op die dag of later. Zo nodig start ze later. |
| **Niet later eindigen dan** | De taak verschuift niet. Eindigt ze later, dan krijg je een aandachtspunt. |
| **Moet eindigen op** | De taak eindigt op die dag. CPM rekent haar start terug. |

Valt de gekozen datum op een weekend of een verlofdag, dan telt de eerstvolgende werkdag. Kies je een beperking maar geen datum, dan weigert CPM het.
Een taak met een beperking heeft geen aandachtspunt "Heeft geen voorganger", ook al heeft ze geen voorganger.

## Een deadline bij de taak zetten

Vul bij **Deadline (optioneel)** een datum in. Eindigt de taak daarna, dan zie je een aandachtspunt met het aantal werkdagen dat ze te laat is. De taak verschuift niet. Je ziet de deadline dus als een afspraak, niet als een dwang.

## Een verantwoordelijke en een notitie toevoegen

1. Kies bij **Verantwoordelijke** een gebruiker. Kies **Niemand** om de keuze te wissen.
2. Typ eventueel een **Notitie**, bijvoorbeeld "Bestelling ramen bevestigd tegen 3 maart".

## Een mijlpaal uit het Traject koppelen

![Het paneel van een taak met de keuze voor een trajectmijlpaal](/img/help/projecten/taak-datums-en-verantwoordelijke/2-koppeling-traject.png)

1. Open de taak, of de fase die je wilt koppelen.
2. Kies bij **Koppeling met trajectmijlpaal** de mijlpaal uit het Traject van het project.
3. Klik op **Opslaan**.

De mijlpaal in het Traject volgt dan de planning.

- Haar streefdatum is het einde van de taak. Bij een fase is dat het einde van haar laatste taak.
- Zodra de taak afgerond is, staat de mijlpaal op **Bereikt**. Bij een fase moeten alle taken klaar zijn.
- Schuift de planning, dan schuift de streefdatum in het Traject mee. Een datum die je in het Traject zelf met de hand invulde, wint van de planning.

Kies **Geen koppeling** om de koppeling weg te halen. De mijlpaal wordt dan weer een gewone mijlpaal die je met de hand bijhoudt.
Mijlpalen die al aan een andere taak hangen, staan in de lijst met de taak erbij. Kies je zo'n mijlpaal, dan verhuist de koppeling naar deze taak.

Je hebt voor deze keuze ook schrijfrecht op het Traject nodig. Zonder dat recht zie je de keuze niet.

## Let op

De taak, de fase, de duur en de voorgangers pas je ook hier aan. Een nieuwe taak start altijd zonder werkelijke data en met 0 % voortgang. Werkelijke data en voortgang vul je pas in zodra er gewerkt is. Lees "De planning bijhouden tijdens de bouw".
