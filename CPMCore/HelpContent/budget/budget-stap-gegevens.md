---
title: De projectgegevens en de indexen invullen
topic: budget
parent: eerste-budget
treeOrder: 1
treeLabel: Stap 1 · Gegevens
role: Projectleider
device: Desktop · tablet
minutes: 6
updated: 2026-10-13
screens: budget-stap-1
synonyms: stap 1, gegevens, liften, trappen, fundering, grondwerk, berlinerwanden, secanspalen, index, s-index, i-index, peildatum, basisprijs
description: aantallen, funderingen, grondwerk en bouwkostindexering
related: eerste-budget, budget-stap-oppervlaktes, budget-meldingen
intro: |
  In stap 1 vul je de gegevens in die voor het hele gebouw gelden. Ze voeden alle volgende stappen. Je geeft de aantallen,
  de funderingen en de prijzen met hun index in.
before: |
  Je hebt een budget geopend. De eenheden zelf vul je in stap 2 in.
faq:
  - q: Waar komt het grondwerk vandaan?
    a: CPM rekent het automatisch uit de oppervlakte van de funderingen en de ondergrondse verdiepingen. Je vult enkel de oppervlaktes en de diepte in.
  - q: Wat als ik de oppervlakte van de funderingen niet in één getal weet?
    a: Klik op Berekening naast het veld. Voeg per type een regel toe met omschrijving, aantal en oppervlakte per stuk. CPM telt ze samen.
  - q: Wat is een peildatum?
    a: De peildatum is de datum waarop de indexen gelden. Is ze ouder dan 6 maanden, dan geeft CPM een waarschuwing.
  - q: Wat zijn de S-index en de I-index?
    a: De S-index is de index van de lonen. De I-index is de index van de materiaalprijzen. Samen passen ze de prijzen uit de nacalculatie aan naar vandaag.
  - q: Wat doet Overnemen onder een prijs?
    a: Het zet de standaardprijs terug in het veld, nadat je de prijs zelf hebt aangepast.
---
## De aantallen invullen

![De kaart Projectgegevens](/img/help/budget/budget-stap-gegevens/1-projectgegevens.png)

Bovenaan de kaart **Projectgegevens** staan de aantallen. Het aantal en de samenstelling van de eenheden komen uit stap 2. Daar vul je ze niet opnieuw in.

1. Vul het aantal **Liften**, **Binnentrappen**, **Trapzalen** en **Toegangspoorten** in.
2. Vul het aantal **Aan te bouwen buren** in. Dat zijn buren waar je tegenaan bouwt of waar er een risico is op schade.
3. Vul het aantal **Bovengrondse verdiepingen** en **Ondergrondse verdiepingen** in.
4. Kies het **Type poorten**, bijvoorbeeld **Sectionaalpoort**. Heb je garages, dan is dit nodig. Anders krijg je een waarschuwing.

## De funderingen en het grondwerk invullen

Onder **Dimensies** vul je de afmetingen van de funderingen in.

1. Vul de **Oppervlakte funderingen** in. Weet je ze niet als één getal, klik dan op **Berekening**.
2. Voeg in de berekening per type een regel toe met **Omschrijving**, **Aantal** en **m² per stuk**. De som is de oppervlakte van de funderingen. Zolang er regels zijn, kun je het veld zelf niet meer aanpassen.
3. Controleer de **Diepte fundering**. CPM stelt 40 cm voor.
4. Vul de **Oppervlakte ondergrondse verdiepingen** in als er ondergrondse verdiepingen zijn. Laat je het veld leeg, dan rekent CPM het aantal verdiepingen maal de oppervlakte van de funderingen.
5. Controleer de **Diepte per ondergrondse verdieping**. CPM stelt 300 cm voor.

Het veld **Grondwerk (automatisch)** toont het resultaat in m³. Het is de oppervlakte van de funderingen maal de diepte, plus de ondergrondse verdiepingen maal hun diepte.
Je past het niet zelf aan. Wil je een ander grondwerk, pas dan de oppervlaktes of de dieptes aan.

Vul ook **Berlinerwanden** (in lopende meter), **Secanspalen** (in lopende meter) en **Onderschoeiingen** (in m³) in als het project ze heeft.
Dit zijn specifieke funderingstechnieken.

Kies tot slot het **Type dak**, bijvoorbeeld **Combinatie plat/hellend**. De dakoppervlaktes vul je in stap 5 in.

## De bouwkostindexering invullen

![De kaart Bouwkostindexering](/img/help/budget/budget-stap-gegevens/2-indexering.png)

De kaart **Bouwkostindexering (S/I)** toont per materiaal de prijs uit de nacalculatie. CPM past die prijs aan met de index van vandaag.

1. Controleer de prijzen voor **Ruwbouw**, **Gevelmetselwerk**, **Gipsblokken** en **Terras**. Onder elk veld staat de basisprijs uit de nacalculatie.
2. Heb je een prijs zelf aangepast, dan klik je op **Overnemen** onder het veld om de standaardprijs terug te zetten.
3. Controleer de **S-index (lonen)** en de **I2021-index (materialen)**. Onder elk veld staan de peildatum en de bron. Met **Historiek** open je de eerdere waarden en kies je een andere index.

Rechts staat **Live berekening**. Daar zie je meteen wat elke prijs wordt na indexering, bijvoorbeeld **Ruwbouw /m² geïnd.**
De formule staat eronder: de verhouding van de I-index tegenover de basis-index, de verhouding van de S-index tegenover de basis-index, plus een vast deel.

## Resultaat

CPM slaat elk veld op zodra je het invult. De waarden gebruikt CPM in alle volgende stappen. Klik onderaan op **Volgende: Oppervlaktes**.

## Let op

- Een index zonder peildatum of met een peildatum ouder dan 6 maanden geeft een waarschuwing.
- Zijn de funderingen of het grondwerk leeg terwijl er eenheden zijn, dan krijg je een waarschuwing.
- Heb je een dakoppervlakte in stap 5 maar geen daktype gekozen, dan is dat een fout. Je kunt dan niet afronden.
