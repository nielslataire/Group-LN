---
title: Referentieprojecten voor de nacalculatie beheren
topic: instellingen
treeOrder: 26
device: Desktop · tablet
minutes: 5
updated: 2026-10-25
newsTag: Nieuw
screens: instellingen-referentieprojecten
synonyms: referentieproject, nacalculatie, snapshot, excel, peildatum, gba, index, kost per lot, stap 6
description: afgewerkte projecten als referentie toevoegen, uit de app of uit Excel
intro: |
  Een **referentieproject** toont de werkelijke kost per activiteit van een afgewerkt project. Op stap 6 van de budgetwizard kies je welke je vergelijkt.
  De bedragen worden met de S- en I-index van de peildatum naar vandaag omgerekend.
before: |
  Je hebt schrijfrecht op **Kostprijs materialen**. Je opent de pagina via **Instellingen** en dan **Referentieprojecten**.
faq:
  - q: Wat is het verschil tussen een snapshot en Excel?
    a: Een snapshot neemt de kosten over uit een project in de app. Excel gebruik je voor projecten van vóór de app.
  - q: Wat neemt de snapshot mee?
    a: De inkomende facturen per activiteit. Heeft het project geen facturen, dan gebruikt CPM de contracten. Meerwerken voor klanten blijven uitgesloten.
  - q: Wat is de peildatum?
    a: De datum van de prijzen. Standaard is dat de opleverdatum van het project.
  - q: Waarom is Snapshot maken grijs?
    a: Kies eerst een project met facturen of contracten die een bedrag per activiteit hebben.
  - q: Waarom staat er geen € per m²?
    a: Zonder GBA kan CPM geen prijs per m² berekenen. Dat project telt dan enkel mee voor € per eenheid.
  - q: Welke kolommen heeft het Excel-bestand?
    a: ActivityId, Activiteit en Bedrag. CPM koppelt eerst op id en anders op naam. Download het sjabloon voor een voorbeeld.
---
## Een snapshot maken van een project uit de app

1. Kies bovenaan **Project uit de app**.
2. Zoek het project. Onder **De snapshot neemt mee** zie je het aantal eenheden, de GBA, de facturen en het totaal.
3. Pas de peildatum, het aantal eenheden of de GBA aan als iets ontbreekt.
4. Klik op **Snapshot maken**.

## Een project uit Excel inladen

1. Kies **Uit Excel**.
2. Typ de naam, de peildatum en het aantal eenheden. De GBA is optioneel.
3. Sleep het .xlsx-bestand in het vak.
4. Klik op **Inladen en controleren**.

CPM toont eerst een controlestap. Daar kun je rijen die niet gekoppeld raken nog zelf aan een activiteit hangen voor je bewaart.

## De lijst lezen

Per referentieproject zie je de peildatum, het aantal eenheden, de GBA, het totaal, de kost per eenheid en per m², het aantal activiteiten en de indexfactoren.
Klik op de pijl om de kost per lot te zien, met het aandeel van elk lot. Daaronder open je **Bekijk alle activiteiten**.

## Een referentieproject verwijderen

Klik op de prullenbak en bevestig. Budgetversies die dit project vergelijken, verliezen die vergelijking.

## Let op

Controleer de peildatum. De indexering naar vandaag hangt ervan af.
