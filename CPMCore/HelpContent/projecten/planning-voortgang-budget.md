---
title: De planning laten meetellen voor de fysieke voortgang
topic: projecten
parent: planning-handleiding
treeOrder: 10
role: Projectleider
device: Desktop · tablet
minutes: 4
updated: 2026-11-15
newsTag: Nieuw
screens: planning-index
related: planning-handleiding, planning-bijhouden-tijdens-de-bouw, project-detail-overzicht, nacalculatie-lezen
synonyms: fysieke voortgang, lot, budgetlot, activiteitengroep, telt mee, voortgang, gewogen, project detail, factuur
description: een fase koppelen aan een lot uit het budget zodat de taakvoortgang de fysieke voortgang bepaalt
intro: |
  De fysieke voortgang van een project toont hoe ver de werf echt staat. Standaard schat CPM dat uit wat per lot gefactureerd is. Met de planning kan het nauwkeuriger.
  Koppel een fase aan een lot en laat haar meetellen. Dan bepaalt de voortgang van de taken in die fase de voortgang van dat lot.
before: |
  Je hebt schrijfrecht op de planning. Het project heeft een budget, anders is er niets om de voortgang aan te koppelen.
faq:
  - q: Wat is een lot?
    a: Een groep activiteiten uit het budget, zoals Ruwbouw of Elektriciteit. In de planning heet het veld Budgetlot.
  - q: Kan ik één fase aan meerdere loten koppelen?
    a: Nee. Een fase hoort bij één lot. Wil je twee loten volgen, maak dan twee fases. Meerdere fases mogen wel hetzelfde lot kiezen. Hun taken tellen dan samen.
  - q: Tellen mijlpalen mee?
    a: Nee. Enkel taken met een duur tellen mee. Een lange taak weegt zwaarder dan een korte, gewogen op haar duur in werkdagen.
  - q: Tellen deelfases mee?
    a: Ja. De taken van de deelfases van een gekoppelde fase tellen mee.
  - q: Waarom blijft de voortgang van een lot nog steeds op de facturen steunen?
    a: Dan heb je Telt mee voor de fysieke voortgang niet aangezet bij een gekoppelde fase, of de fase bevat geen taken met een duur. Enkel gekoppelde fases die meetellen vervangen de schatting op basis van facturen.
  - q: Waarom kan ik Telt mee niet aanzetten?
    a: Kies eerst een budgetlot. Zonder lot weigert CPM het en toont ze "Kies eerst een budgetlot om de voortgang te laten meetellen."
---
## Een fase koppelen aan een lot

![Het paneel van een fase met het budgetlot en de schakelaar Telt mee voor de fysieke voortgang](/img/help/projecten/planning-voortgang-budget/1-fasepaneel-lot.png)

1. Klik in de lijst op de fase, of kies **Bewerken** in haar menu.
2. Kies bij **Budgetlot (optioneel)** het lot dat bij de fase past.
3. Zet **Telt mee voor de fysieke voortgang** aan.
4. Klik op **Opslaan**.

Kies je geen lot, dan staat de schakelaar uit en blijft de fase enkel een planningsgroep.

## Wat er gebeurt

Voor elk gekoppeld lot rekent CPM de voortgang uit als het gemiddelde van de voortgang van de taken, gewogen op hun duur. De schatting op basis van facturen verdwijnt dan voor dat lot. Je ziet het resultaat als **Fysieke voortgang** op het projectdetail.

Een voorbeeld. De fase Ruwbouw heeft twee taken. Metselwerk (12 werkdagen) staat op 50 % en Funderingen (10 werkdagen) op 100 %. De voortgang van het lot is (12 × 50 + 10 × 100) gedeeld door 22, ongeveer 73 %.

## De voortgang bijhouden

De voortgang is enkel zo goed als de gegevens in de planning. Vink taken af en vul het percentage bij gedeeltelijke taken in. Lees "De planning bijhouden tijdens de bouw".

## Let op

Wil je de koppeling stoppen, kies dan bij het budgetlot geen lot of zet de schakelaar uit. Het lot rekent dan opnieuw op wat gefactureerd is.
Het budget zelf wijzigt hierdoor niet. Enkel de fysieke voortgang verandert.
