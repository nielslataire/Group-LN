---
title: De marktdata-status lezen
topic: instellingen
treeOrder: 30
device: Desktop · tablet
minutes: 3
updated: 2026-10-25
newsTag: Nieuw
screens: instellingen-marktdata
synonyms: marktdata, crawler, bron, immoweb, zimmo, run, geblokkeerd, vastgelopen, status
description: zien of de crawlers van de marktdata goed draaien en wat er mis is gegaan
intro: |
  Bij **Marktdata-status** zie je of de crawlers die advertenties van vastgoedsites ophalen goed draaien. Actieve bronnen staan groot met hun cijfers, inactieve bronnen compact eronder.
before: |
  Je hebt leesrecht op **Marktdata-status**. Je opent de pagina via **Instellingen** en dan **Marktdata-status**. Er valt niets in te stellen.
faq:
  - q: Wat betekent Geblokkeerd?
    a: De run is als geslaagd gemeld, maar vond 0 advertenties. Meestal blokkeert de website onze crawler met een beveiliging (HTTP 403). Bij Zimmo gebeurt dit regelmatig.
  - q: Wat betekent Vastgelopen?
    a: Een run staat al meer dan 6 uur op Bezig en is nog niet afgerond. De worker is dan waarschijnlijk gestopt.
  - q: Wat betekent Al 6 runs op rij?
    a: De laatste runs van die bron vonden allemaal niets. Het probleem is dus niet toevallig.
  - q: Waarom staat een bron bij Inactieve bronnen?
    a: De bron is uitgeschakeld in de crawler of heeft nog nooit gedraaid.
  - q: Kan ik een bron nu laten crawlen?
    a: Nog niet vanuit CPM. De worker start volgens zijn eigen schema.
  - q: Welke tijden zie ik?
    a: Brusselse tijd, dag en maand met uur en minuut.
---
## Een bron lezen

Elke actieve bron toont een label met de toestand: **Geslaagd**, **Mislukt**, **Bezig**, **Geblokkeerd** of **Vastgelopen**.
Daaronder staan de laatste run, de duur, het aantal **gevonden**, **nieuwe** en **bijgewerkte** advertenties en het aantal **fouten**.
Bij een probleem staat er een melding onder, met de reden als die gekend is.

## De recente runs

Onderaan staan de laatste 50 runs van alle bronnen, nieuwste eerst. De status, het aantal gevonden, nieuwe en bijgewerkte advertenties en de melding van elke run staan erbij.
Een run met 0 gevonden advertenties krijgt het label **Geblokkeerd**.

## Wat moet ik doen bij een probleem?

Een bron die meermaals na elkaar geblokkeerd of vastgelopen is, vraagt aandacht van wie de crawler beheert.
Meld de bron, het tijdstip en de melding die erbij staat.

## Let op

Cijfers van een geblokkeerde bron zijn niet betrouwbaar: de marktanalyse mist dan advertenties van die site.
