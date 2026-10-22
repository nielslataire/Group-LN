---
title: De prijscodes en de vraagprijs per eenheid bepalen
topic: budget
parent: eerste-budget
treeOrder: 8
treeLabel: Stap 8 · Verkoop
role: Projectleider
device: Desktop · tablet
minutes: 8
updated: 2026-10-22
newsTag: Bijgewerkt
screens: budget-stap-8
synonyms: stap 8, verkoop, vraagprijs, prijscode, bouwprijscode, grondprijscode, bouwwaarde, grondwaarde, minimumprijs, marktprijs, voorstel, vraagprijs volgt, bulk, selecteren, doorzetten naar units, zo laten, op minimum zetten
description: het verkoopvoorstel, de prijscodes en de vraagprijs per eenheid
related: eerste-budget, budget-stap-parameters, budget-stap-resultaat, budget-meldingen
intro: |
  In stap 8 bepaal je de verkoopprijs van elke eenheid. CPM maakt een voorstel op basis van de kostprijs en de marge. Je vergelijkt dat met de markt
  en legt de vraagprijs per eenheid vast. Dat doe je met prijscodes of met eigen waarden.
before: |
  Stap 2 en stap 7 zijn ingevuld. Het voorstel gebruikt de oppervlaktes, de grondkost en de marges daaruit.
faq:
  - q: Wat is het verschil tussen de bouwwaarde en de grondwaarde?
    a: De bouwwaarde is de waarde van de constructie van een eenheid. De grondwaarde is de waarde van de grond. Samen zijn ze de verkoopprijs.
  - q: Mag mijn vraagprijs onder de minimumprijs liggen?
    a: Ja. CPM geeft dan een waarschuwing, maar je kunt verder. Met Zo laten onthoudt CPM dat je dit bewust zo laat.
  - q: Wat zijn de twee getallen bij Grond, zoals 575 + 201?
    a: Het eerste getal is de oppervlakte van de eerste grondzone in m². Het tweede is die van de tweede zone. Die tweede zone telt voor een deel mee.
  - q: Ik zet een eenheid op Eigen. Gaat mijn code verloren?
    a: Nee. CPM onthoudt de gekozen code en de eigen waarden. Schakel je terug, dan staan ze er weer. Dat geldt ook voor de tweede grondzone en voor Vraagprijs volgt.
  - q: Wat betekent het teken P achter een code?
    a: Het is een projectcode. Je maakte hem voor dit project. Een code zonder dat teken is een algemene code uit Instellingen.
  - q: Waarom heeft een eenheid geen marktprijs?
    a: Dit antwoord volgt nog. [VRAAG: wanneer toont CPM geen marktprijs voor een eenheid, en uit welke bron komt de marktdata?]
---
## De samenvatting lezen

![Het verkoopvoorstel met de kengetallen](/img/help/budget/budget-stap-verkoop/2-voorstel.png)

Bovenaan staat één balk met de belangrijkste cijfers:

- **Kostprijs incl. grond** en het aantal eenheden
- **Min. verkoopwaarde**: de kostprijs plus de marge, met de marge in procent en in euro
- **Vraagprijzen**: het totaal van wat je invult, en het verschil met het minimum
- **Marktwaarde**: wat vergelijkbare eenheden in de buurt waard zijn
- **Eenheden onder minimum**: dit vak verschijnt alleen als een vraagprijs onder het minimum ligt

Onder de balk staat een regel over de markt in de gemeente: het aantal te koop en verkocht, de mediaan per m² en de doorlooptijd. Met **Gemeenteanalyse** open je meer cijfers.
Met **Marges** ga je naar stap 7 om de marges aan te passen.

## De prijscodes kiezen en toevoegen

![De bouw- en grondprijscodes als chips](/img/help/budget/budget-stap-verkoop/1-prijscodes.png)

Een prijscode is een vaste prijs per m² met een code, bijvoorbeeld **B-01** of **G-03**. De bouwcodes rekenen op de gereduceerde oppervlakte, de grondcodes op de grond.
Een code die je in een eenheid gebruikt, staat in het groen. Een code met een P erachter is een projectcode.

1. Klik op **Code toevoegen** voor een code die enkel voor dit project geldt.
2. Kies de soort, vul het nummer, de omschrijving en de prijs per m² in en kies de datum.
3. Klik op **Code toevoegen**.

Met het kruisje op een projectcode verwijder je ze. Een code die in een eenheid gebruikt wordt, kun je niet verwijderen.
Met **Beheren** open je de algemene codes in Instellingen. Die gelden voor alle budgetten. Een code die ouder is dan 12 maanden geeft een waarschuwing.

## De vraagprijs per eenheid bepalen

![De tabel per eenheid met de vraagprijs](/img/help/budget/budget-stap-verkoop/3-per-eenheid.png)

Elke eenheid heeft een rij met de oppervlaktes, de grondcode, de bouwcode, de minimumprijs, de marktprijs en een invoerveld **Vraagprijs**.

1. Kies bij **Grondcode** en **Bouwcode** een prijscode. CPM rekent de waarde uit en toont ze onder de keuze.
2. Kies je **Eigen waarde**, dan tik je zelf het bedrag in. Het chipje **EIGEN** laat dat zien. Klik erop om terug te gaan naar een code.
3. Vergelijk de **Min. prijs** met de **Markt**. Het getal onder de marktprijs is het verschil met de minimumprijs.
4. Controleer de **Vraagprijs**. Het label eronder toont waar de prijs vandaan komt: **VOORSTEL**, **MARKT**, **CODE** of **EIGEN**.
5. Onderaan staat het totaal van de minimumprijzen, de marktprijzen en de vraagprijzen.

Met **Alles: vraagprijs = voorstel** neem je het voorstel van CPM over in elke eenheid.

## De berekening van een eenheid openen

![De open rij met grond, bouw, uitzonderingen en vraagprijs volgt](/img/help/budget/budget-stap-verkoop/4-details.png)

Klik op het pijltje voor een eenheid. Je ziet vier blokken:

- **Grond**: kies **Code** of **Eigen**. Bij een code zie je zone 1 maal de prijs. Heeft de eenheid een tweede grondzone, dan vul je daar een eigen prijs per m² in. Bij **Eigen** vul je de prijs per m² óf het bedrag in. Het andere rekent mee.
- **Bouw**: dezelfde keuze, op de gereduceerde oppervlakte.
- **Uitzonderingen**: zet **Grondruil** aan als de grondwaarde nul is. Dan verschijnt een veld voor het extra forfait.
- **Vraagprijs volgt**: kies **Voorstel**, **Markt** of **Eigen**. Bij Voorstel en Markt neemt CPM de waarden over. Bij Eigen gelden jouw waarden. Onderaan koppel je de eenheid aan een unit.

Wissel je tussen Code en Eigen, of tussen Voorstel, Markt en Eigen, dan gaat er niets verloren. CPM onthoudt wat je had en zet het terug.

## Een vraagprijs onder het minimum bekijken

![Een eenheid onder het minimum, met een goudkleurige waarschuwing](/img/help/budget/budget-stap-verkoop/5-onder-minimum.png)

Ligt een vraagprijs onder de minimumprijs, dan kleurt de rij goud en staat eronder wat het verschil is. Het totaal bovenaan toont het vak **Eenheden onder minimum**.
Je kunt verder. Kies wat je wilt:

1. **Op minimum zetten** zet de vraagprijs op de minimumprijs.
2. **Zo laten** onthoudt dat je het bewust zo laat. De waarschuwing telt dan niet meer mee. Op stap 9 kun je ze terugzetten.

## Meerdere eenheden tegelijk aanpassen

![De groene balk bij een selectie](/img/help/budget/budget-stap-verkoop/6-bulk.png)

Vink eenheden aan, of alle eenheden met het vakje in de kop. Er verschijnt een groene balk. Kies daar een **Grondcode** of **Bouwcode** voor de selectie, of klik op **Vraagprijs = voorstel** of **Vraagprijs = markt**.
Dit past alleen de geselecteerde eenheden aan. Met het kruisje rechts wis je de selectie.

## De vraagprijzen doorzetten

Klik op **Doorzetten naar units** om de grondwaarde en de bouwwaarde naar de eenheden van het project te schrijven.

1. Koppel eerst elke rij aan een eenheid. Open de rij en kies de unit onderaan bij **Vraagprijs volgt**.
2. Klik op **Doorzetten naar units**.
3. Bevestig in het venster. CPM slaat de rijen op en schrijft de waarden naar de gekoppelde eenheden.

Eenheden die al verkocht zijn, blijven ongemoeid. Ook de grond van de tweede zone telt mee in de grondwaarde. Is er geen rij met een eenheid en een grond- of bouwwaarde, dan meldt CPM een fout.
Je schrijft hiermee de waarden, niet de vraagprijzen. [VRAAG: hoe zet je de vraagprijzen zelf op de eenheden?]

## Resultaat

CPM slaat alles automatisch op. Stap 9 toont de marge die de vraagprijzen opleveren. Klik onderaan op **Opslaan & volgende: Resultaat**.

## Let op

- Een eenheid zonder vraagprijs geeft een waarschuwing.
- Een vraagprijs onder de minimumprijs geeft een waarschuwing. Je kunt verder.
- De waarschuwing blijft bij een versie staan, tenzij je ze negeert met **Zo laten**.
