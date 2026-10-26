---
title: Een trajectsjabloon bewerken
topic: instellingen
parent: trajectsjablonen-overzicht
treeOrder: 1
device: Desktop
minutes: 8
updated: 2026-10-25
newsTag: Nieuw
screens: instellingen-trajectsjablonen-bewerken
synonyms: sjabloon, fase, mijlpaal, technische code, streefdatum, anker, actie, trigger, bron, handmatig, automatisch, tijdlijn, simulatie, controle
description: fases en mijlpalen opbouwen, de streefdatum en acties instellen, en het sjabloon controleren
related: trajectsjablonen-overzicht
intro: |
  In de editor bouw je het traject op. Links staan de fases met hun mijlpalen als boom. Rechts stel je de gekozen fase of mijlpaal in, in vijf genummerde stappen.
  Op de tabs **Tijdlijn & simulatie** en **Controle** zie je het resultaat en wat nog niet klopt.
before: |
  Je hebt schrijfrecht op **Trajectsjablonen**. Je opent de pagina via **Instellingen**, **Trajectsjablonen** en dan **Nieuw sjabloon** of een bestaand sjabloon.
faq:
  - q: Wanneer geldt mijn wijziging?
    a: Pas na **Sjabloon opslaan**. Het label "Niet-opgeslagen wijzigingen" bovenaan herinnert je eraan.
  - q: Wat is de technische code van een mijlpaal?
    a: Een unieke naam in hoofdletters, zoals VERGUNNING_INGEDIEND. Ankers en acties gebruiken ze om de mijlpaal te herkennen. Klik op "afleiden uit de naam" voor een voorstel.
  - q: Wat is het verschil tussen handmatig en automatisch?
    a: Handmatig zet de verantwoordelijke de datum zelf. Automatisch is bereikt zodra een gegeven in het project verandert, zoals een document of een datum.
  - q: Wat is het anker bij de streefdatum?
    a: Het punt waarvanaf CPM de dagen telt: de aanmaak van het project of een andere mijlpaal. De streefdatum is het anker plus het aantal dagen.
  - q: Wat is het verschil tussen Heel het project en Per eenheid?
    a: Per eenheid komt de mijlpaal voor elke eenheid van het project apart voor.
  - q: Hoe verplaats ik een fase of mijlpaal?
    a: Sleep ze aan de greep, of selecteer ze en druk op Alt met de pijl omhoog of omlaag.
  - q: Wat betekent het getal bij Controle?
    a: Het aantal punten dat nog aandacht vraagt. Klik op een punt om naar de plek te springen.
  - q: Kan ik een sjabloon kopiëren?
    a: Ja, met **Dupliceren** bovenaan. De kopie is niet standaard en krijgt "(kopie)" achter de naam.
---
## Het sjabloon zelf

Bovenaan vul je de **naam**, het **projecttype** en een korte omschrijving in. Zet **Standaard voor dit type** aan als nieuwe projecten van dat type dit sjabloon moeten krijgen. Met **Actief** bepaal je of het sjabloon te kiezen is.

## Fases en mijlpalen

Klik op **Fase toevoegen** voor een nieuwe fase en op **Mijlpaal** onder een fase voor een nieuwe mijlpaal. Klik op een rij om ze rechts in te stellen.
Een fase heeft een naam, een code, een volgorde en een kleur. Het pijltje klapt een fase in of uit.

## De mijlpaal in vijf stappen

1. **Wat is deze mijlpaal?** De naam, de technische code, de soort, de verantwoordelijke rol, of ze voor het hele project of per eenheid geldt, en of ze verplicht is vóór de fase kan worden afgerond.
2. **Wanneer is ze bereikt?** Kies **Handmatig afvinken** of **Automatisch uit CPM**. Bij automatisch kies je de bron en de parameter, bijvoorbeeld het type document.
3. **Streefdatum.** Kies het anker en het aantal dagen. Rechts staat de berekende dag en datum voor een project dat vandaag is aangemaakt.
4. **Acties bij deze mijlpaal.** Klik op **Actie** en kies de gebeurtenis en de actie, zoals een e-mail naar de rol of een taak. De velden verschillen per actie.
5. **Toelichting.** Deze tekst staat als tooltip op het project.

## Tijdlijn en simulatie

Kies een fictieve projectstart en bekijk alle mijlpalen op een tijdlijn per fase. Wissel tussen **Dagen na start** en **Datums**. Er wordt niets bewaard.

## Controle

Hier staan fouten (rood) en aandachtspunten (oker), bijvoorbeeld een mijlpaal zonder naam of code, een dubbele code, een onbekend anker of ankers die rondjes draaien. Klik op een punt om ernaartoe te gaan.

## Opslaan

Klik op **Sjabloon opslaan**. Een fase of mijlpaal zonder naam wordt bij het opslaan weggelaten.

## Let op

Wijzig je de technische code van een mijlpaal die als anker dient, pas dan ook de ankers van de andere mijlpalen aan. De controle toont onbekende ankers.
