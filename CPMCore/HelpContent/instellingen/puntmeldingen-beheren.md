---
title: De automatische puntmeldingen beheren
topic: instellingen
treeOrder: 31
device: Desktop · tablet
minutes: 4
updated: 2026-10-25
newsTag: Nieuw
screens: instellingen-puntmeldingen
synonyms: puntmeldingen, herinnering, dagelijkse update, schema, aannemer, werfpunten, mails, uitvoeringen, logboek, testmail
description: de schema's voor herinneringen aan aannemers bekijken, versturen, testen en de uitvoeringen volgen
related: puntmelding-schema-bewerken
intro: |
  Met **Puntmeldingen** stuurt CPM aannemers automatisch een herinnering met hun openstaande werfpunten en een dagelijkse update van de wijzigingen. Elk schema staat als een leesbare zin in de lijst.
  Onder de schema's zie je de recente uitvoeringen.
before: |
  Je hebt schrijfrecht op **Puntmeldingen**. Je opent de pagina via **Instellingen** en dan **Puntmeldingen**. Zonder schrijfrecht kun je kijken, maar niets versturen.
faq:
  - q: Welk schema wint als er meerdere gelden?
    a: Het meest specifieke. Niveau 1 (aannemer en project) wint van niveau 2 (enkel aannemer), dat wint van niveau 3 (enkel project). Het Standaard-schema geldt voor alle aannemers en projecten en komt als laatste.
  - q: Wat doet Nu versturen?
    a: Het verstuurt de batch van dat schema meteen, los van het schema. Gebruik het alleen als je zeker bent.
  - q: Wat doen Testmail en Test uitnodigingsmail?
    a: Ze sturen een proefmail naar het vaste testadres. Er gaat niets naar aannemers.
  - q: Wat betekent een gedempte uitvoering?
    a: Een geslaagde run zonder verstuurde mails. Er was niets te melden.
  - q: Wat is een vastgelopen uitvoering?
    a: Een run die al meer dan een uur op Bezig staat zonder einde. Meld dit als het blijft gebeuren.
  - q: Waar zie ik de oudere uitvoeringen?
    a: Klik onder de tabel op **Alle logs bekijken**.
---
## De lijst met schema's

Per schema zie je voor wie het geldt, wanneer de herinnering vertrekt, of er een dagelijkse update is, de volgende herinnering en de status. Het label **Niveau** of **Standaard** toont hoe specifiek het schema is.

## Een schema aanmaken of aanpassen

Klik op **Nieuw schema**, of op het potlood bij een bestaand schema. Zie *Een puntmelding-schema bewerken*.

## Een schema verwijderen

Klik op de prullenbak en bevestig. Aannemers vallen daarna terug op het volgende meest specifieke schema.

## De recente uitvoeringen

Bovenaan staat een samenvatting van de laatste 10 dagen: aantal runs, verstuurde mails, herinneringen en vastgelopen runs.
Filter de tabel met **Alle**, **Geslaagd**, **Fout** of **Vastgelopen**. Tijden zijn in Brusselse tijd.

## Let op

Controleer de uitvoeringen na een wijziging in een schema. Een schema dat niets verstuurt, vind je terug als gedempte rij zonder mails.
