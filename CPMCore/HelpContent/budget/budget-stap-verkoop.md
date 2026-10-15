---
title: De prijscodes en de vraagprijs per eenheid bepalen
topic: budget
parent: eerste-budget
treeOrder: 8
treeLabel: Stap 8 · Verkoop
role: Projectleider
device: Desktop · tablet
minutes: 8
updated: 2026-10-13
screens: budget-stap-8
synonyms: stap 8, verkoop, vraagprijs, prijscode, bouwprijscode, grondprijscode, bouwwaarde, grondwaarde, minimumprijs, marktprijs, voorstel overnemen, doorzetten naar units
description: de bouw- en grondprijscodes, het verkoopvoorstel en de vraagprijs per eenheid
related: eerste-budget, budget-stap-parameters, budget-stap-resultaat, budget-meldingen
intro: |
  In stap 8 bepaal je de verkoopprijs van elke eenheid. CPM maakt een voorstel op basis van de kostprijs en de marge. Je vergelijkt dat met de markt
  en legt de vraagprijs per eenheid vast.
before: |
  Stap 2 en stap 7 zijn ingevuld. Het voorstel gebruikt de oppervlaktes, de grondkost en de marges daaruit.
faq:
  - q: Wat is het verschil tussen de bouwwaarde en de grondwaarde?
    a: De bouwwaarde is de waarde van de constructie van een eenheid. De grondwaarde is de waarde van de grond. Samen zijn ze de verkoopprijs.
  - q: Mag mijn vraagprijs onder de minimumprijs liggen?
    a: Ja. CPM geeft dan een waarschuwing, maar je kunt verder.
  - q: Wat zijn de twee getallen bij Grond, zoals 575 + 201?
    a: Het eerste getal is de oppervlakte van de eerste grondzone in m². Het tweede is die van de tweede zone. Die tweede zone telt voor een deel mee.
  - q: Waarom heeft een eenheid geen marktprijs?
    a: Dit antwoord volgt nog. [VRAAG: wanneer toont CPM geen marktprijs voor een eenheid, en uit welke bron komt de marktdata?]
---
## De prijscodes invullen

![De bouwprijscodes en de grondprijscodes](/img/help/budget/budget-stap-verkoop/1-prijscodes.png)

Bovenaan staan twee kaarten: **Bouwprijscodes** (€/m² gereduceerd) en **Grondprijscodes** (€/m² grond). Een prijscode is een vaste prijs per m² met een code,
bijvoorbeeld **B-01** of **G-03**. Je kiest ze later per eenheid.

1. Typ in de onderste rij het nummer van de code, bijvoorbeeld **01**.
2. Vul de omschrijving en de prijs per m² in. Kies de datum.
3. Klik op **Toevoegen**.

Met **Algemene codes** open je de codes die voor alle budgetten gelden. [VRAAG: waar beheer je de algemene codes, en wat betekent het teken P achter een code?]
Een code die ouder is dan 12 maanden geeft een waarschuwing.

## Het verkoopvoorstel bekijken

![Het verkoopvoorstel met de kengetallen](/img/help/budget/budget-stap-verkoop/2-voorstel.png)

CPM maakt een voorstel: de kostprijs plus de marge. De bouwwaarde rekent op de gereduceerde oppervlakte en de grondwaarde op de grondoppervlakte.
De tweede grondzone telt voor het percentage uit stap 7. De blokken tonen:

- **Kostprijs incl. grond**
- **Grondwaarde** en **Bouwwaarde**, met de marge in procent
- **Min. verkoopwaarde**: de kostprijs plus de marge
- **Marge**: het verschil tussen de minimale verkoopwaarde en de kostprijs
- **Marktwaarde**: wat vergelijkbare eenheden in de buurt waard zijn

Onder de blokken staat een regel over de markt in de gemeente. Je ziet het aantal te koop en verkocht, de mediaan per m² en de doorlooptijd. Met **Gemeenteanalyse** open je meer cijfers.

Met **Voorstel overnemen** neem je het verkoopprijsvoorstel van CPM over in de vraagprijzen. Met **Markt overnemen** neem je de gemiddelde marktprijs over.
Met **Marges** ga je naar stap 7 om de marges aan te passen.

## De vraagprijs per eenheid bepalen

![De tabel Per eenheid met de vraagprijs](/img/help/budget/budget-stap-verkoop/3-per-eenheid.png)

Elke eenheid heeft een rij met kostprijs, bouwwaarde, minimumprijs, marktprijs en een invoerveld **Vraagprijs**.

1. Vergelijk de **Min. prijs** met de **Marktprijs**. Het getal naast de marktprijs is het verschil in procent met de minimumprijs.
2. Vul de **Vraagprijs** in.
3. Controleer onderaan het totaal. Daar staat het verschil tussen de vraagprijzen en de minimale verkoopwaarde.

Ligt een vraagprijs onder de minimumprijs, dan staat de rij in het goud. Dat is een waarschuwing.

## De prijs van een eenheid aanpassen

![Het detail van een eenheid](/img/help/budget/budget-stap-verkoop/4-details.png)

Klik op de pijl voor een eenheid voor meer opties. Je ziet dan de prijs voor de grond en voor de bouw:

1. Kies bij **Code** een prijscode. CPM vult de prijs per m² in en rekent de waarde uit. Je kunt de prijs ook zelf intikken.
2. Heeft de woning een tweede grondzone, vul dan bij **Grond 2** een eigen prijs per m² in voor die zone.
3. Zet **Grondruil** aan als de grondwaarde nul is en de compensatie in een forfait zit.
4. Vul eventueel een **extra forfait** in en koppel de rij aan een unit.

Het label **Bron** toont waar de prijs vandaan komt, bijvoorbeeld **CODE**. Met **Voorstel** en **Markt** neem je het voorstel of de marktprijs over voor deze eenheid.

## De vraagprijzen doorzetten

Klik op **Doorzetten naar units** om de grondwaarde en de bouwwaarde naar de eenheden van het project te schrijven.

1. Koppel eerst elke rij aan een eenheid. Kies die in het detail van de rij bij **geen unit**.
2. Klik op **Doorzetten naar units**.
3. Bevestig in het venster. CPM slaat de rijen op en schrijft de waarden naar de gekoppelde eenheden.

Eenheden die al verkocht zijn, blijven ongemoeid. Ook de grond van de tweede zone telt mee in de grondwaarde. Is er geen rij met een eenheid en een grond- of bouwwaarde, dan meldt CPM een fout.
Je schrijft hiermee de waarden, niet de vraagprijzen. [VRAAG: hoe zet je de vraagprijzen zelf op de eenheden?]

## Resultaat

CPM slaat de vraagprijzen op. Stap 9 toont de marge die ze opleveren. Klik onderaan op **Opslaan & volgende: Resultaat**.

## Let op

- Een eenheid zonder vraagprijs geeft een waarschuwing.
- Een vraagprijs onder de minimumprijs geeft een waarschuwing. Je kunt verder.
