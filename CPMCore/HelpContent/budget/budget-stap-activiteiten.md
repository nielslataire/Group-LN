---
title: De kost per activiteit controleren en corrigeren
topic: budget
parent: eerste-budget
treeOrder: 6
treeLabel: Stap 6 · Activiteiten
role: Projectleider
device: Desktop · tablet
minutes: 7
updated: 2026-10-13
screens: budget-stap-6
synonyms: stap 6, activiteiten, lot, nacalculatie, referentie, referentieproject, correctie, verschil, toon berekeningen, formule
description: de bouwkost per activiteit, de vergelijking met afgewerkte projecten en de correctie
related: eerste-budget, budget-stap-dak-afbraak, budget-stap-parameters, budget-meldingen
intro: |
  In stap 6 rekent CPM de bouwkost uit per activiteit, gegroepeerd per lot. Je vergelijkt de bedragen met afgewerkte projecten.
  Waar nodig corrigeer je een bedrag met een percentage.
before: |
  Stap 1 tot 5 zijn ingevuld. Stap 6 gebruikt die gegevens om de bedragen te berekenen.
faq:
  - q: Waarom is een bedrag grijs en kan ik het niet intikken?
    a: Het bedrag in de kolom Basis is berekend uit de vorige stappen. Je past het niet zelf aan. Gebruik de kolom Correctie om het te verhogen of te verlagen.
  - q: Wat betekent de correctie 100 %?
    a: Er is geen correctie. Het gecorrigeerde bedrag is dan gelijk aan het basisbedrag. Bij 120 % wordt het bedrag 20 % hoger, bij 50 % wordt het de helft.
  - q: Wat is het driehoekje bij een activiteit?
    a: Het bedrag wijkt meer dan 50 % af van de referentie. Controleer of de hoeveelheid of de prijs klopt, of pas de correctie aan.
  - q: Waarom heeft een activiteit geen referentie?
    a: Het gekozen referentieproject heeft die activiteit niet. Bovenaan staat hoeveel activiteiten geen referentie hebben.
  - q: Wat doet Referentie overnemen?
    a: Het pijltje bij een activiteit neemt het bedrag over van het referentieproject dat je geselecteerd hebt. Selecteerde je meerdere referentieprojecten, dan neemt het hun bedrag over.
---
## De kengetallen bovenaan lezen

![De kaart Nacalculatie met het gekozen referentieproject](/img/help/budget/budget-stap-activiteiten/1-nacalculatie.png)

Bovenaan staan drie blokken:

- **Totaal activiteiten · gecorrigeerd**: de totale bouwkost na je correcties.
- **Per m² GBA**: de bouwkost per m² gebouwde oppervlakte.
- **Referentie nacalc**: het totaal van de referentieprojecten, geïndexeerd naar vandaag, en het verschil in procent.

## Een referentieproject kiezen

De kaart **Nacalculatie** vergelijkt het budget met afgewerkte projecten. Zo zie je of je bedragen realistisch zijn.

1. Kies in de kaart een referentieproject, bijvoorbeeld een project uit 2023.
2. Klik op **Toepassen**.

Onder de kaart staat hoe CPM de referentie berekent: de werkelijke kost, maal een indexfactor, gedeeld door het aantal eenheden van het referentieproject, maal het aantal eenheden van dit budget.
Met **Referentieprojecten** rechtsboven in de kaart open je de lijst met projecten die je kunt kiezen. [VRAAG: wie beheert die lijst, en welke projecten komen erin?]

## De activiteiten per lot lezen

![De tabel met activiteiten per lot](/img/help/budget/budget-stap-activiteiten/2-activiteiten.png)

De tabel toont per lot de activiteiten. Een lot is een groep van werken, bijvoorbeeld **Lot 2 Bovenbouw**. Per rij zie je:

| Kolom | Betekent |
|---|---|
| Activiteit | de naam van de activiteit |
| € Basis | het bedrag dat CPM berekent uit de vorige stappen |
| Referentie | het bedrag van het referentieproject, voor hetzelfde aantal eenheden |
| Verschil | het verschil tussen basis en referentie in procent |
| Correctie | jouw correctie in procent. Standaard 100 |
| Gecorrigeerd | het basisbedrag maal de correctie |

Een lot toont ook zijn totalen. Met het pijltje voor de lotnaam klap je een lot open of dicht. Met **Alles dicht** klap je alle loten dicht.
Met de schakelaar **Enkel afwijking > 25 %** zie je alleen de activiteiten die meer dan 25 % afwijken van de referentie.

## Een bedrag corrigeren

1. Zoek de activiteit.
2. Verander de **Correctie**, bijvoorbeeld naar **120** voor 20 % meer.
3. Vul de **opmerking** in die onder de activiteit verschijnt. Ze is verplicht zodra de correctie niet 100 is.
4. Klik onderaan op **Opslaan**.

Een correctie zonder opmerking geeft een waarschuwing. Schrijf kort waarom je het bedrag aanpast, bijvoorbeeld dat het project luxueuzer is.

## Zien hoe een bedrag is berekend

![De berekening onder een activiteit](/img/help/budget/budget-stap-activiteiten/3-berekening.png)

Zet bovenaan de schakelaar **Toon berekeningen** aan. Onder elke activiteit verschijnt dan de berekening in blokjes. Bijvoorbeeld de eenheidsprijs,
de index en de hoeveelheid uit stap 1 tot 5, met onderaan de bron, zoals **bron: stap 1 · Gegevens**. Met **Formule** open je de formule van die activiteit.

## Resultaat

Het totaal van de gecorrigeerde bedragen is de bouwkost van het budget. Stap 7 gebruikt dat totaal voor de percentages. Klik onderaan op **Volgende: Parameters**.

## Let op

- Een activiteit met een bedrag van € 0 staat grijs. Dat is enkel informatie.
- Een bedrag dat meer dan 50 % afwijkt van de referentie geeft een waarschuwing.
- EPB, veiligheidscoördinatie en ventilatieverslaggeving staan als activiteit onder **Lot 0 Algemeen**. Ze staan niet meer bij de percentages in stap 7.
